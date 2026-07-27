using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using gtas_vpp_be.Authorization;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gtas_vpp_be.Tests.Authorization;

public sealed class AppAuthenticationServiceTests
{
    private const string ValidPassword = "Valid-Pass1!";

    [Fact]
    public async Task Authenticate_ValidIdentity_IssuesStableClaimsOnly()
    {
        await using var fixture = await AuthFixture.CreateAsync();

        var result = await fixture.Service.AuthenticateAsync(
            fixture.Account.UserName!,
            ValidPassword);

        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.Equal(fixture.Account.Id, result.UserID);
        Assert.Equal(CanonicalRbac.SystemAdmin.GroupId, result.GroupId);
        Assert.True(result.IsAdmin);
        Assert.NotEqual(ValidPassword, fixture.Account.PasswordHash);
        Assert.DoesNotContain(ValidPassword, fixture.Account.PasswordHash!, StringComparison.Ordinal);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Contains(token.Claims, x => x.Type == JwtRegisteredClaimNames.Sub
            && x.Value == fixture.Account.Id.ToString());
        Assert.Contains(token.Claims, x => x.Type == AppClaimTypes.SessionVersion && x.Value == "1");
        Assert.DoesNotContain(token.Claims, x => x.Type == AppClaimTypes.GroupId);
        Assert.DoesNotContain(token.Claims, x => x.Type == AppClaimTypes.MemberCompanyCode);
        Assert.DoesNotContain(token.Claims, x => x.Type == AppClaimTypes.DepartmentCode);
        Assert.DoesNotContain(token.Claims, x => x.Type == AppClaimTypes.IsAdmin);
    }

    [Fact]
    public async Task Authenticate_DisabledAccount_IsRejected()
    {
        await using var fixture = await AuthFixture.CreateAsync(AppAccountStatus.Disabled);

        var result = await fixture.Service.AuthenticateAsync(
            fixture.Account.UserName!,
            ValidPassword);

        Assert.Null(result);
    }

    [Fact]
    public async Task Authenticate_PendingApprovalAccount_IsRejectedEvenWithValidPasswordAndMembership()
    {
        await using var fixture = await AuthFixture.CreateAsync(AppAccountStatus.PendingApproval);

        var result = await fixture.Service.AuthenticateAsync(
            fixture.Account.UserName!,
            ValidPassword);

        Assert.Null(result);
    }

    [Fact]
    public async Task CurrentUser_OldSessionVersion_IsRejectedOnNextScope()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var principal = CreatePrincipal(fixture.Account.Id, sessionVersion: 1);
        Assert.NotNull(await fixture.CurrentUserContext.GetAsync(principal));

        fixture.Account.SessionVersion = 2;
        await fixture.Context.SaveChangesAsync();
        var nextRequestContext = new CurrentUserContext(fixture.Context);

        Assert.Null(await nextRequestContext.GetAsync(principal));
    }

    [Fact]
    public async Task Authenticate_WithoutValidMembership_IsRejected()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        fixture.Membership.IsDeleted = true;
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.AuthenticateAsync(
            fixture.Account.UserName!,
            ValidPassword);

        Assert.Null(result);
    }

    [Fact]
    public async Task CurrentUser_TamperedCanonicalRoleCode_IsRejected()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var group = await fixture.Context.PermissionGroups.SingleAsync();
        group.GroupCode = "SYSTEM_ADMIN_TAMPERED";
        await fixture.Context.SaveChangesAsync();

        var currentUser = new CurrentUserContext(fixture.Context);
        var snapshot = await currentUser.GetAsync(CreatePrincipal(fixture.Account.Id, sessionVersion: 1));

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task CurrentUser_EnrichmentCarriesServerAuthoritativePasswordChangeRequirement()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        fixture.Account.MustChangePassword = true;
        await fixture.Context.SaveChangesAsync();
        var currentUser = new CurrentUserContext(fixture.Context);
        var principal = CreatePrincipal(fixture.Account.Id, sessionVersion: 1);
        var snapshot = await currentUser.GetAsync(principal);

        Assert.NotNull(snapshot);
        currentUser.EnrichPrincipal(principal, snapshot);

        Assert.Equal(
            bool.TrueString,
            principal.FindFirst(AppClaimTypes.MustChangePassword)?.Value);
    }

    private static ClaimsPrincipal CreatePrincipal(int userId, long sessionVersion) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(AppClaimTypes.SessionVersion, sessionVersion.ToString())
        ], "Test"));

    private sealed class AuthFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        private AuthFixture(
            ServiceProvider provider,
            VPPContext context,
            AppUser account,
            UserGroupMembership membership,
            CurrentUserContext currentUserContext,
            AppAuthenticationService service)
        {
            _provider = provider;
            Context = context;
            Account = account;
            Membership = membership;
            CurrentUserContext = currentUserContext;
            Service = service;
        }

        public VPPContext Context { get; }
        public AppUser Account { get; }
        public UserGroupMembership Membership { get; }
        public CurrentUserContext CurrentUserContext { get; }
        public AppAuthenticationService Service { get; }

        public static async Task<AuthFixture> CreateAsync(
            AppAccountStatus accountStatus = AppAccountStatus.Active)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<VPPContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddIdentityCore<AppUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 10;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                })
                .AddEntityFrameworkStores<VPPContext>();
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<VPPContext>();
            var userManager = provider.GetRequiredService<UserManager<AppUser>>();
            var now = DateTime.UtcNow;
            var department = new Department
            {
                Id = Guid.NewGuid(),
                Code = "IT",
                Name = "Công nghệ thông tin",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            var group = new PermissionGroup
            {
                Id = CanonicalRbac.SystemAdmin.GroupId,
                GroupCode = CanonicalRbac.SystemAdmin.GroupCode,
                GroupName = CanonicalRbac.SystemAdmin.GroupName,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            context.AddRange(department, group);
            await context.SaveChangesAsync();

            var account = new AppUser
            {
                UserName = "test.owner",
                Email = "owner@example.test",
                FullName = "Test Owner",
                MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
                AccountStatus = accountStatus,
                EmailConfirmed = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ActivatedAtUtc = accountStatus == AppAccountStatus.Active ? now : null
            };
            var createResult = await userManager.CreateAsync(account, ValidPassword);
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(x => x.Code)));

            var membership = new UserGroupMembership
            {
                Id = Guid.NewGuid(),
                UserId = account.Id,
                AccountId = account.Id,
                PermissionGroupId = group.Id,
                DepartmentId = department.Id,
                CreatedByUserId = account.Id,
                UpdatedByUserId = account.Id,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            context.UserGroupMemberships.Add(membership);
            await context.SaveChangesAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Key"] = "TEST_JWT_KEY_FOR_APP_IDENTITY_AUTH_2026",
                    ["JwtSettings:Issuer"] = "gtas_vpp_be",
                    ["JwtSettings:Audience"] = "gtas_vpp_test_clients",
                    ["DatabaseSettings:DefaultEnvironment"] = DatabaseBinding.TestEnvironment,
                    ["ConnectionStrings:TestEnv"] =
                        "Server=localhost;Database=GTAS_AUTH_TEST;Integrated Security=True"
                })
                .Build();
            var jwtSettings = JwtDeploymentSettings.Create(
                configuration,
                DatabaseBinding.Create(configuration));
            var currentUserContext = new CurrentUserContext(context);
            var service = new AppAuthenticationService(
                userManager,
                currentUserContext,
                context,
                jwtSettings,
                NullLogger<AppAuthenticationService>.Instance);
            return new AuthFixture(
                provider,
                context,
                account,
                membership,
                currentUserContext,
                service);
        }

        public async ValueTask DisposeAsync() => await _provider.DisposeAsync();
    }
}
