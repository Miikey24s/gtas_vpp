using gtas_vpp_be.Authorization;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Notifications;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests.Authorization;

public sealed class AccountLifecycleServiceTests
{
    private const string ValidPassword = "Valid-Pass1!";

    [Fact]
    public async Task Register_CreatesPendingAccountWithoutMembership_AndDuplicateIsAntiEnumerating()
    {
        await using var fixture = await LifecycleFixture.CreateAsync();

        var request = new AccountRegistrationReqDTO
        {
            Username = "new.employee",
            Email = "new.employee@example.test",
            FullName = "New Employee",
            EmployeeCode = "E-001",
            Password = ValidPassword,
            ConfirmPassword = ValidPassword
        };

        var first = await fixture.Service.RegisterAsync(request);
        var duplicate = await fixture.Service.RegisterAsync(request);

        Assert.Equal(202, first.StatusCode);
        Assert.Equal(202, duplicate.StatusCode);
        Assert.Equal(first.Code, duplicate.Code);
        var account = Assert.Single(await fixture.Context.Users.AsNoTracking().ToListAsync());
        Assert.Equal(AppAccountStatus.PendingApproval, account.AccountStatus);
        Assert.False(account.EmailConfirmed);
        Assert.Empty(await fixture.Context.UserGroupMemberships.ToListAsync());
        Assert.Contains(
            await fixture.Context.SecurityAudits.AsNoTracking().ToListAsync(),
            audit => audit.Action == "ACCOUNT_REGISTERED");
        Assert.Contains(
            await fixture.Context.SecurityAudits.AsNoTracking().ToListAsync(),
            audit => audit.Action == "ACCOUNT_REGISTRATION_REJECTED" && audit.Outcome == "Duplicate");
    }

    [Fact]
    public async Task Register_ValidatesPasswordBeforeDuplicateLookup_ToAvoidWeakPasswordEnumeration()
    {
        await using var fixture = await LifecycleFixture.CreateAsync();
        await fixture.CreateAccountAsync("existing.user", AppAccountStatus.Active);
        const string weakPassword = "weak";

        var existing = await fixture.Service.RegisterAsync(new AccountRegistrationReqDTO
        {
            Username = "existing.user",
            Email = "existing.user@example.test",
            FullName = "Existing User",
            Password = weakPassword,
            ConfirmPassword = weakPassword
        });
        var unknown = await fixture.Service.RegisterAsync(new AccountRegistrationReqDTO
        {
            Username = "unknown.user",
            Email = "unknown.user@example.test",
            FullName = "Unknown User",
            Password = weakPassword,
            ConfirmPassword = weakPassword
        });

        Assert.Equal(400, existing.StatusCode);
        Assert.Equal(400, unknown.StatusCode);
        Assert.Equal(existing.Code, unknown.Code);
    }

    [Fact]
    public async Task PasswordRecovery_UsesSameAcceptedResponseForUnknownAndKnownEmail()
    {
        await using var fixture = await LifecycleFixture.CreateAsync(emailEnabled: true);
        var account = await fixture.CreateAccountAsync("recovery.user", AppAccountStatus.Active);

        var known = await fixture.Service.RequestPasswordResetAsync(
            new PasswordRecoveryReqDTO { Email = account.Email! });
        var unknown = await fixture.Service.RequestPasswordResetAsync(
            new PasswordRecoveryReqDTO { Email = "does-not-exist@example.test" });

        Assert.Equal(202, known.StatusCode);
        Assert.Equal(202, unknown.StatusCode);
        Assert.Equal(known.Code, unknown.Code);
        Assert.Single(fixture.EmailSender.Messages);
        Assert.Contains("ResetPassword", fixture.EmailSender.Messages[0].TextBody, StringComparison.Ordinal);
        Assert.Equal(
            2,
            await fixture.Context.SecurityAudits.CountAsync(
                audit => audit.Action == "ACCOUNT_PASSWORD_RESET_REQUESTED"));
    }

    [Fact]
    public async Task PasswordRecovery_EmailAdapterFailure_RemainsAcceptedAndAudited()
    {
        await using var fixture = await LifecycleFixture.CreateAsync(emailEnabled: true);
        var account = await fixture.CreateAccountAsync("recovery.failure", AppAccountStatus.Active);
        fixture.EmailSender.ExceptionToThrow = new IOException("Simulated transport failure.");

        var result = await fixture.Service.RequestPasswordResetAsync(
            new PasswordRecoveryReqDTO { Email = account.Email! });

        Assert.Equal(202, result.StatusCode);
        Assert.Equal("PASSWORD_RESET_REQUEST_ACCEPTED", result.Code);
        Assert.Contains(
            await fixture.Context.SecurityAudits.AsNoTracking().ToListAsync(),
            audit => audit.Action == "ACCOUNT_PASSWORD_RESET_REQUESTED"
                     && audit.Outcome == "EmailUnavailable");
    }

    [Fact]
    public async Task AdminResetForcesPasswordChange_AndSelfServiceClearsFlagAndBumpsSession()
    {
        await using var fixture = await LifecycleFixture.CreateAsync();
        var account = await fixture.CreateAccountAsync("password.user", AppAccountStatus.Active);
        var oldSession = account.SessionVersion;

        var adminReset = await fixture.Service.AdminResetPasswordAsync(
            actorAccountId: account.Id + 1,
            new AdminPasswordResetReqDTO
            {
                AccountId = account.Id,
                TemporaryPassword = "Temp-Pass2!",
                ConfirmPassword = "Temp-Pass2!",
                Reason = "Support fallback"
            });

        Assert.True(adminReset.Succeeded);
        Assert.True(account.MustChangePassword);
        Assert.True(account.SessionVersion > oldSession);
        Assert.True(await fixture.UserManager.CheckPasswordAsync(account, "Temp-Pass2!"));

        var changed = await fixture.Service.ChangePasswordAsync(
            account.Id,
            new PasswordChangeReqDTO
            {
                CurrentPassword = "Temp-Pass2!",
                NewPassword = "New-Pass3!",
                ConfirmPassword = "New-Pass3!"
            });

        Assert.True(changed.Succeeded);
        Assert.False(account.MustChangePassword);
        Assert.True(account.SessionVersion > oldSession + 1);
        Assert.True(await fixture.UserManager.CheckPasswordAsync(account, "New-Pass3!"));
        Assert.Contains(
            await fixture.Context.SecurityAudits.AsNoTracking().ToListAsync(),
            audit => audit.Action == "ACCOUNT_ADMIN_PASSWORD_RESET");
        Assert.Contains(
            await fixture.Context.SecurityAudits.AsNoTracking().ToListAsync(),
            audit => audit.Action == "ACCOUNT_PASSWORD_CHANGED");
    }

    [Fact]
    public async Task ConfirmEmail_UsesIdentityTokenProviderAndPersistsConfirmation()
    {
        await using var fixture = await LifecycleFixture.CreateAsync(emailEnabled: true);
        var account = await fixture.CreateAccountAsync("confirm.user", AppAccountStatus.PendingApproval);
        var token = await fixture.UserManager.GenerateEmailConfirmationTokenAsync(account);

        var result = await fixture.Service.ConfirmEmailAsync(
            new EmailConfirmationReqDTO { UserId = account.Id, Token = token });

        Assert.True(result.Succeeded);
        Assert.True(account.EmailConfirmed);
        Assert.Equal("EMAIL_CONFIRMED", result.Code);
        Assert.Contains(
            await fixture.Context.SecurityAudits.AsNoTracking().ToListAsync(),
            audit => audit.Action == "ACCOUNT_EMAIL_CONFIRMED");
    }

    private sealed class LifecycleFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        private LifecycleFixture(
            ServiceProvider provider,
            VPPContext context,
            UserManager<AppUser> userManager,
            AccountLifecycleService service,
            RecordingEmailSender emailSender)
        {
            _provider = provider;
            Context = context;
            UserManager = userManager;
            Service = service;
            EmailSender = emailSender;
        }

        public VPPContext Context { get; }

        public UserManager<AppUser> UserManager { get; }

        public AccountLifecycleService Service { get; }

        public RecordingEmailSender EmailSender { get; }

        public static async Task<LifecycleFixture> CreateAsync(bool emailEnabled = false)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection();
            services.AddDbContext<VPPContext>(options =>
                options.UseInMemoryDatabase($"GTAS_VPP_ACCOUNT_LIFECYCLE_{Guid.NewGuid():N}"));
            services.AddIdentityCore<AppUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 10;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                })
                .AddEntityFrameworkStores<VPPContext>()
                .AddDefaultTokenProviders();

            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<VPPContext>();
            var userManager = provider.GetRequiredService<UserManager<AppUser>>();
            var emailSender = new RecordingEmailSender();
            var options = Options.Create(new AccountEmailOptions
            {
                Enabled = emailEnabled,
                RequireConfirmationWhenEnabled = true,
                PublicBaseUrl = "https://example.test"
            });
            var service = new AccountLifecycleService(
                userManager,
                context,
                new NoopMembershipAdministrationService(),
                new NoopNotificationService(),
                emailSender,
                options,
                NullLogger<AccountLifecycleService>.Instance);

            await context.Database.EnsureCreatedAsync();
            return new LifecycleFixture(provider, context, userManager, service, emailSender);
        }

        public async Task<AppUser> CreateAccountAsync(string username, AppAccountStatus status)
        {
            var now = DateTime.UtcNow;
            var account = new AppUser
            {
                UserName = username,
                Email = $"{username}@example.test",
                FullName = username,
                AccountStatus = status,
                EmailConfirmed = status == AppAccountStatus.Active,
                MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ActivatedAtUtc = status == AppAccountStatus.Active ? now : null
            };
            var result = await UserManager.CreateAsync(account, ValidPassword);
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Code)));
            return account;
        }

        public async ValueTask DisposeAsync() => await _provider.DisposeAsync();
    }

    private sealed class RecordingEmailSender : IAccountEmailSender
    {
        public List<AccountEmailMessage> Messages { get; } = [];

        public Exception? ExceptionToThrow { get; set; }

        public Task SendAsync(AccountEmailMessage message, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                return Task.FromException(ExceptionToThrow);
            }

            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class NoopNotificationService : IAppNotificationService
    {
        public Task<gtas_vpp_shared.DTOs.Res.Notifications.NotificationInboxResDTO> GetInboxAsync(
            int userId, string memberCompanyCode, int skip, int take, bool unreadOnly,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new gtas_vpp_shared.DTOs.Res.Notifications.NotificationInboxResDTO());

        public Task<bool> MarkReadAsync(Guid id, int userId, string memberCompanyCode,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<int> MarkAllReadAsync(int userId, string memberCompanyCode,
            CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<IReadOnlyList<int>> GetRecipientsWithPermissionAsync(string memberCompanyCode,
            string permission, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<int>>([]);

        public Task PublishAsync(IEnumerable<int> recipientUserIds, string memberCompanyCode,
            string type, string title, string message, string? route, string? correlationId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoopMembershipAdministrationService : IMembershipAdministrationService
    {
        public Task<MembershipAdministrationResult> UpsertAsync(int actorAccountId,
            gtas_vpp_shared.DTOs.Req.Permission.MembershipUpsertReqDTO command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(MembershipAdministrationResult.BadRequest("NOT_USED", "Not used in this fixture."));

        public Task<MembershipAdministrationResult> ActivateAndUpsertAsync(int actorAccountId,
            gtas_vpp_shared.DTOs.Req.Permission.MembershipUpsertReqDTO command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(MembershipAdministrationResult.BadRequest("NOT_USED", "Not used in this fixture."));

        public Task<MembershipAdministrationResult> DeactivateAsync(int actorAccountId,
            gtas_vpp_shared.DTOs.Req.Permission.MembershipDeactivateReqDTO command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(MembershipAdministrationResult.BadRequest("NOT_USED", "Not used in this fixture."));
    }
}
