using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services.AuthBootstrap;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace gtas_vpp_be.Tests.AuthBootstrap;

public sealed class AuthBootstrapProvisionerTests
{
    [Fact]
    public async Task ProvisionAsync_IsDisabledByDefault_AndDoesNotMutateState()
    {
        await using var fixture = await BootstrapFixture.CreateAsync();
        var options = ValidOptions();
        options.Enabled = false;

        var exception = await Assert.ThrowsAsync<AuthBootstrapProvisioningException>(
            () => fixture.CreateProvisioner(options).ProvisionAsync());

        Assert.Equal(AuthBootstrapFailure.Disabled, exception.Failure);
        Assert.Empty(await fixture.Context.Users.ToListAsync());
        Assert.Empty(await fixture.Context.P04_UserGroups.ToListAsync());
        Assert.Empty(await fixture.Context.A02_AuthBootstrapOperations.ToListAsync());
        Assert.Empty(await fixture.Context.A01_SecurityAudits.ToListAsync());
    }

    [Fact]
    public async Task ProvisionAsync_CreatesOneActiveOwnerMembershipLedgerAndAudit_WithoutReturningPassword()
    {
        await using var fixture = await BootstrapFixture.CreateAsync();
        var options = ValidOptions();

        var result = await fixture.CreateProvisioner(options).ProvisionAsync();

        Assert.Equal(AuthBootstrapOutcome.Provisioned, result.Outcome);
        var owner = Assert.Single(await fixture.Context.Users.AsNoTracking().ToListAsync());
        Assert.Equal(result.AccountId, owner.Id);
        Assert.Equal(AppAccountStatus.Active, owner.AccountStatus);
        Assert.True(owner.EmailConfirmed);
        Assert.True(owner.MustChangePassword);
        Assert.True(owner.LockoutEnabled);
        Assert.Equal(CanonicalRbac.DefaultMemberCompanyCode, owner.MemberCompanyCode);
        Assert.Equal(1, owner.SessionVersion);
        Assert.NotNull(owner.PasswordHash);
        Assert.NotEqual(options.InitialPassword, owner.PasswordHash);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            fixture.UserManager.PasswordHasher.VerifyHashedPassword(
                owner,
                owner.PasswordHash!,
                options.InitialPassword));

        var membership = Assert.Single(await fixture.Context.P04_UserGroups.AsNoTracking().ToListAsync());
        Assert.Equal(owner.Id, membership.AccountId);
        Assert.Equal(owner.Id, membership.UserId);
        Assert.Equal(CanonicalRbac.SystemAdmin.GroupId, membership.P02_GroupId);
        Assert.Equal(BootstrapFixture.DepartmentId, membership.LEX02_CompanyDepartmentLocationId);
        Assert.False(membership.IsDeleted);

        var ledger = Assert.Single(await fixture.Context.A02_AuthBootstrapOperations.AsNoTracking().ToListAsync());
        Assert.Equal(options.OperationKey, ledger.OperationKey);
        Assert.Equal(owner.Id, ledger.AccountId);
        Assert.Equal("Completed", ledger.Status);
        Assert.Equal(64, ledger.InputFingerprint.Length);
        Assert.DoesNotContain(options.InitialPassword, ledger.InputFingerprint, StringComparison.Ordinal);

        var audit = Assert.Single(await fixture.Context.A01_SecurityAudits.AsNoTracking().ToListAsync());
        Assert.Equal(owner.Id, audit.TargetUserId);
        Assert.Equal(-1, audit.ActorUserId);
        Assert.Equal("AUTH_BOOTSTRAP_OWNER_CREATED", audit.Action);
        Assert.DoesNotContain(options.InitialPassword, audit.Summary ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(options.InitialPassword, audit.Reason ?? string.Empty, StringComparison.Ordinal);

        Assert.DoesNotContain(
            typeof(AuthBootstrapResult).GetProperties(),
            property => property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(options.InitialPassword, result.ToString(), StringComparison.Ordinal);
        Assert.Equal(nameof(AuthBootstrapOptions), options.ToString());
    }

    [Fact]
    public async Task ProvisionAsync_ExactCompletedRerun_IsIdempotent()
    {
        await using var fixture = await BootstrapFixture.CreateAsync();
        var options = ValidOptions();
        var provisioner = fixture.CreateProvisioner(options);

        var first = await provisioner.ProvisionAsync();
        var second = await provisioner.ProvisionAsync();

        Assert.Equal(AuthBootstrapOutcome.Provisioned, first.Outcome);
        Assert.Equal(AuthBootstrapOutcome.AlreadyCompleted, second.Outcome);
        Assert.Equal(first.AccountId, second.AccountId);
        Assert.Equal(1, await fixture.Context.Users.CountAsync());
        Assert.Equal(1, await fixture.Context.P04_UserGroups.CountAsync(membership => !membership.IsDeleted));
        Assert.Equal(1, await fixture.Context.A02_AuthBootstrapOperations.CountAsync());
        Assert.Equal(1, await fixture.Context.A01_SecurityAudits.CountAsync());
    }

    [Fact]
    public async Task ProvisionAsync_ReusedOperationKeyWithDifferentFingerprint_FailsClosed()
    {
        await using var fixture = await BootstrapFixture.CreateAsync();
        var original = ValidOptions();
        await fixture.CreateProvisioner(original).ProvisionAsync();
        var changed = ValidOptions();
        changed.FullName = "A Different Owner";

        var exception = await Assert.ThrowsAsync<AuthBootstrapProvisioningException>(
            () => fixture.CreateProvisioner(changed).ProvisionAsync());

        Assert.Equal(AuthBootstrapFailure.OperationFingerprintMismatch, exception.Failure);
        Assert.Equal(1, await fixture.Context.Users.CountAsync());
        Assert.Equal(1, await fixture.Context.P04_UserGroups.CountAsync(membership => !membership.IsDeleted));
        Assert.Equal(1, await fixture.Context.A02_AuthBootstrapOperations.CountAsync());
        Assert.Equal(1, await fixture.Context.A01_SecurityAudits.CountAsync());
    }

    [Fact]
    public async Task ProvisionAsync_ExactInputsButDifferentPassword_FailsWithoutResettingCredential()
    {
        await using var fixture = await BootstrapFixture.CreateAsync();
        var original = ValidOptions();
        var first = await fixture.CreateProvisioner(original).ProvisionAsync();
        var changed = ValidOptions();
        changed.InitialPassword = SyntheticPassword("different-owner");

        var exception = await Assert.ThrowsAsync<AuthBootstrapProvisioningException>(
            () => fixture.CreateProvisioner(changed).ProvisionAsync());

        Assert.Equal(AuthBootstrapFailure.CompletedStateMismatch, exception.Failure);
        var owner = await fixture.Context.Users.AsNoTracking().SingleAsync(user => user.Id == first.AccountId);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            fixture.UserManager.PasswordHasher.VerifyHashedPassword(
                owner,
                owner.PasswordHash!,
                original.InitialPassword));
        Assert.Equal(
            PasswordVerificationResult.Failed,
            fixture.UserManager.PasswordHasher.VerifyHashedPassword(
                owner,
                owner.PasswordHash!,
                changed.InitialPassword));
    }

    [Fact]
    public async Task ProvisionAsync_PreexistingActiveAppAccount_FailsClosed()
    {
        await using var fixture = await BootstrapFixture.CreateAsync();
        var now = DateTime.UtcNow;
        var existing = new AppUser
        {
            UserName = "existing.owner",
            Email = "existing.owner@example.test",
            FullName = "Existing Owner",
            AccountStatus = AppAccountStatus.Active,
            EmailConfirmed = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ActivatedAtUtc = now
        };
        var createResult = await fixture.UserManager.CreateAsync(existing, SyntheticPassword("existing-owner"));
        Assert.True(createResult.Succeeded);

        var exception = await Assert.ThrowsAsync<AuthBootstrapProvisioningException>(
            () => fixture.CreateProvisioner(ValidOptions()).ProvisionAsync());

        Assert.Equal(AuthBootstrapFailure.PreexistingActiveAccount, exception.Failure);
        Assert.Equal(1, await fixture.Context.Users.CountAsync());
        Assert.Empty(await fixture.Context.A02_AuthBootstrapOperations.ToListAsync());
    }

    [Fact]
    public async Task ProvisionAsync_PreexistingActiveP04_FailsClosed()
    {
        await using var fixture = await BootstrapFixture.CreateAsync();
        fixture.Context.P04_UserGroups.Add(new P04_UserGroup
        {
            Id = Guid.NewGuid(),
            UserId = 42,
            AccountId = null,
            P02_GroupId = CanonicalRbac.SystemAdmin.GroupId,
            LEX02_CompanyDepartmentLocationId = BootstrapFixture.DepartmentId,
            CreateUserId = -1,
            UpdateUserId = -1,
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow,
            IsDeleted = false
        });
        await fixture.Context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AuthBootstrapProvisioningException>(
            () => fixture.CreateProvisioner(ValidOptions()).ProvisionAsync());

        Assert.Equal(AuthBootstrapFailure.PreexistingActiveMembership, exception.Failure);
        Assert.Empty(await fixture.Context.Users.ToListAsync());
        Assert.Empty(await fixture.Context.A02_AuthBootstrapOperations.ToListAsync());
    }

    [Fact]
    public async Task ProvisionAsync_MissingEffectivePermissionManage_FailsBeforeAccountCreation()
    {
        await using var fixture = await BootstrapFixture.CreateAsync();
        fixture.Context.P06_GroupPageComponentMappings.RemoveRange(
            await fixture.Context.P06_GroupPageComponentMappings.ToListAsync());
        await fixture.Context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AuthBootstrapProvisioningException>(
            () => fixture.CreateProvisioner(ValidOptions()).ProvisionAsync());

        Assert.Equal(AuthBootstrapFailure.MissingPermissionManage, exception.Failure);
        Assert.Empty(await fixture.Context.Users.ToListAsync());
        Assert.Empty(await fixture.Context.P04_UserGroups.ToListAsync());
    }

    [Fact]
    public async Task ProvisionAsync_InvalidDepartmentType_FailsBeforeAccountCreation()
    {
        await using var fixture = await BootstrapFixture.CreateAsync();
        var department = await fixture.Context.LEX02_CompanyDepartmentLocations
            .SingleAsync(item => item.Id == BootstrapFixture.DepartmentId);
        department.LEX02Type = "Location";
        await fixture.Context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AuthBootstrapProvisioningException>(
            () => fixture.CreateProvisioner(ValidOptions()).ProvisionAsync());

        Assert.Equal(AuthBootstrapFailure.InvalidPrimaryDepartment, exception.Failure);
        Assert.Empty(await fixture.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task ProvisionAsync_LiveBindingOnInMemoryProvider_IsRejectedBeforeMutation()
    {
        var binding = BootstrapFixture.CreateBinding(DatabaseBinding.LiveEnvironment, "GTAS_VPP_LIVE");
        await using var fixture = await BootstrapFixture.CreateAsync(binding: binding, seedPrerequisites: false);

        var exception = await Assert.ThrowsAsync<AuthBootstrapProvisioningException>(
            () => fixture.CreateProvisioner(ValidOptions()).ProvisionAsync());

        Assert.Equal(AuthBootstrapFailure.UnsupportedProvider, exception.Failure);
        Assert.Empty(await fixture.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task ProvisionAsync_UserManagerFromDifferentContext_IsRejectedBeforeMutation()
    {
        await using var serviceFixture = await BootstrapFixture.CreateAsync();
        await using var managerFixture = await BootstrapFixture.CreateAsync();
        var provisioner = new AuthBootstrapProvisioner(
            serviceFixture.Context,
            managerFixture.UserManager,
            serviceFixture.Binding,
            Options.Create(ValidOptions()));

        var exception = await Assert.ThrowsAsync<AuthBootstrapProvisioningException>(
            () => provisioner.ProvisionAsync());

        Assert.Equal(AuthBootstrapFailure.UserManagerContextMismatch, exception.Failure);
        Assert.Empty(await serviceFixture.Context.Users.ToListAsync());
        Assert.Empty(await managerFixture.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task ProvisionAsync_ConcurrentExactOperation_ProducesOneOwnerAndOneIdempotentResult()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"GTAS_VPP_BOOTSTRAP_TEST_{Guid.NewGuid():N}";
        var binding = BootstrapFixture.CreateBinding(DatabaseBinding.TestEnvironment, databaseName);
        await using (var seed = await BootstrapFixture.CreateAsync(
            binding,
            databaseRoot,
            databaseName,
            seedPrerequisites: true))
        {
        }

        await using var firstFixture = await BootstrapFixture.CreateAsync(
            binding,
            databaseRoot,
            databaseName,
            seedPrerequisites: false);
        await using var secondFixture = await BootstrapFixture.CreateAsync(
            binding,
            databaseRoot,
            databaseName,
            seedPrerequisites: false);
        var options = ValidOptions();

        var results = await Task.WhenAll(
            firstFixture.CreateProvisioner(options).ProvisionAsync(),
            secondFixture.CreateProvisioner(options).ProvisionAsync());

        Assert.Contains(results, result => result.Outcome == AuthBootstrapOutcome.Provisioned);
        Assert.Contains(results, result => result.Outcome == AuthBootstrapOutcome.AlreadyCompleted);
        Assert.Single(results.Select(result => result.AccountId).Distinct());

        await using var verifyFixture = await BootstrapFixture.CreateAsync(
            binding,
            databaseRoot,
            databaseName,
            seedPrerequisites: false);
        Assert.Equal(1, await verifyFixture.Context.Users.CountAsync());
        Assert.Equal(1, await verifyFixture.Context.P04_UserGroups.CountAsync(membership => !membership.IsDeleted));
        Assert.Equal(1, await verifyFixture.Context.A02_AuthBootstrapOperations.CountAsync());
        Assert.Equal(1, await verifyFixture.Context.A01_SecurityAudits.CountAsync());
    }

    [Fact]
    public async Task ProvisionAsync_InMemoryFailureAfterIdentityCreate_CompensatesAllState()
    {
        var databaseName = $"GTAS_VPP_BOOTSTRAP_TEST_{Guid.NewGuid():N}";
        var binding = BootstrapFixture.CreateBinding(DatabaseBinding.TestEnvironment, databaseName);
        var dbOptions = new DbContextOptionsBuilder<VPPContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var context = new FailingVppContext(dbOptions);
        await using var fixture = await BootstrapFixture.CreateAsync(
            binding,
            context: context,
            seedPrerequisites: true);
        context.FailOnSecondSubsequentSave();

        var exception = await Assert.ThrowsAsync<AuthBootstrapProvisioningException>(
            () => fixture.CreateProvisioner(ValidOptions()).ProvisionAsync());

        Assert.Equal(AuthBootstrapFailure.PersistenceFailed, exception.Failure);
        Assert.Empty(await fixture.Context.Users.AsNoTracking().ToListAsync());
        Assert.Empty(await fixture.Context.P04_UserGroups.AsNoTracking().ToListAsync());
        Assert.Empty(await fixture.Context.A02_AuthBootstrapOperations.AsNoTracking().ToListAsync());
        Assert.Empty(await fixture.Context.A01_SecurityAudits.AsNoTracking().ToListAsync());
    }

    private static AuthBootstrapOptions ValidOptions() => new()
    {
        Enabled = true,
        OperationKey = "lean-02-clean-owner-v1",
        Username = "thesis.owner",
        Email = "thesis.owner@example.test",
        FullName = "Thesis System Owner",
        InitialPassword = SyntheticPassword("clean-owner"),
        PrimaryDepartmentCode = "IT"
    };

    private static string SyntheticPassword(string purpose)
    {
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"unit-test:{purpose}")));
        return $"{digest[..20]}aA1!";
    }

    private sealed class FailingVppContext(DbContextOptions<VPPContext> options) : VPPContext(options)
    {
        private int _saveCount;
        private int? _failOnSave;

        public void FailOnSecondSubsequentSave() => _failOnSave = _saveCount + 2;

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _saveCount++;
            if (_saveCount == _failOnSave)
            {
                throw new InvalidOperationException("Synthetic persistence failure.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class BootstrapFixture : IAsyncDisposable
    {
        private static readonly Guid PermissionPageId = Guid.Parse("B6B0D8C8-0944-492A-93BD-18E3A913050E");
        private static readonly Guid PermissionComponentId =
            CanonicalRbacSeedIds.ActionComponent(Permissions.PermissionManage);
        private static readonly Guid PermissionMappingId =
            CanonicalRbacSeedIds.ActionPageMapping(Permissions.PermissionManage);

        public static readonly Guid DepartmentId = Guid.Parse("36923B6B-8B27-4812-A913-A0C178325CBF");

        private BootstrapFixture(
            VPPContext context,
            UserManager<AppUser> userManager,
            DatabaseBinding binding)
        {
            Context = context;
            UserManager = userManager;
            Binding = binding;
        }

        public VPPContext Context { get; }

        public UserManager<AppUser> UserManager { get; }

        public DatabaseBinding Binding { get; }

        public static async Task<BootstrapFixture> CreateAsync(
            DatabaseBinding? binding = null,
            InMemoryDatabaseRoot? databaseRoot = null,
            string? databaseName = null,
            bool seedPrerequisites = true,
            VPPContext? context = null)
        {
            databaseName ??= $"GTAS_VPP_BOOTSTRAP_TEST_{Guid.NewGuid():N}";
            binding ??= CreateBinding(DatabaseBinding.TestEnvironment, databaseName);
            if (context is null)
            {
                var optionsBuilder = new DbContextOptionsBuilder<VPPContext>();
                if (databaseRoot is null)
                {
                    optionsBuilder.UseInMemoryDatabase(databaseName);
                }
                else
                {
                    optionsBuilder.UseInMemoryDatabase(databaseName, databaseRoot);
                }

                context = new VPPContext(optionsBuilder.Options);
            }

            var describer = new IdentityErrorDescriber();
            var store = new UserOnlyStore<AppUser, VPPContext, int>(context, describer);
            var identityOptions = Options.Create(new IdentityOptions
            {
                User =
                {
                    RequireUniqueEmail = true
                },
                Password =
                {
                    RequiredLength = 12,
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireUppercase = true,
                    RequireNonAlphanumeric = true
                }
            });
            var userManager = new UserManager<AppUser>(
                store,
                identityOptions,
                new PasswordHasher<AppUser>(),
                [new UserValidator<AppUser>()],
                [new PasswordValidator<AppUser>()],
                new UpperInvariantLookupNormalizer(),
                describer,
                services: EmptyServiceProvider.Instance,
                NullLogger<UserManager<AppUser>>.Instance);
            var fixture = new BootstrapFixture(context, userManager, binding);

            if (seedPrerequisites)
            {
                await fixture.SeedPrerequisitesAsync();
            }

            return fixture;
        }

        public AuthBootstrapProvisioner CreateProvisioner(AuthBootstrapOptions options) => new(
            Context,
            UserManager,
            Binding,
            Options.Create(options));

        public static DatabaseBinding CreateBinding(string environment, string databaseName)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DatabaseSettings:DefaultEnvironment"] = environment,
                    [$"ConnectionStrings:{environment}"] =
                        $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=true;TrustServerCertificate=true"
                })
                .Build();
            return DatabaseBinding.Create(configuration);
        }

        public ValueTask DisposeAsync()
        {
            UserManager.Dispose();
            return Context.DisposeAsync();
        }

        private async Task SeedPrerequisitesAsync()
        {
            if (await Context.P02_Groups.AnyAsync(group => group.Id == CanonicalRbac.SystemAdmin.GroupId))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var group = new P02_Group
            {
                Id = CanonicalRbac.SystemAdmin.GroupId,
                GroupCode = CanonicalRbac.SystemAdmin.GroupCode,
                GroupName = CanonicalRbac.SystemAdmin.GroupName,
                Description = CanonicalRbac.SystemAdmin.Description,
                CreateUserId = -1,
                UpdateUserId = -1,
                CreateDate = now,
                UpdateDate = now,
                IsDeleted = false
            };
            var department = new LEX02_CompanyDepartmentLocation
            {
                Id = DepartmentId,
                LEX02Code = "IT",
                LEX02Name = "IT",
                LEX02Type = "PhongBan",
                CreateUserId = -1,
                UpdateUserId = -1,
                CreateDate = now,
                UpdateDate = now,
                IsDeleted = false
            };
            var page = new P01_Page
            {
                Id = PermissionPageId,
                PageCode = "PERMISSION",
                PageName = "Permission",
                Type = "Page",
                CreateUserId = -1,
                UpdateUserId = -1,
                CreateDate = now,
                UpdateDate = now,
                IsDeleted = false
            };
            var component = new P03_Component
            {
                Id = PermissionComponentId,
                ComponentCode = Permissions.PermissionManage,
                ComponentName = "Manage access",
                CreateUserId = -1,
                UpdateUserId = -1,
                CreateDate = now,
                UpdateDate = now,
                IsDeleted = false
            };
            var pageComponent = new P05_PageComponentMapping
            {
                Id = PermissionMappingId,
                P01_PageId = page.Id,
                P01_Page = page,
                P03_ComponentId = component.Id,
                P03_Component = component
            };
            var permission = new P06_GroupPageComponentMapping
            {
                P02_GroupId = group.Id,
                P02_Group = group,
                P05_PageComponentMappingId = pageComponent.Id,
                P05_PageComponentMapping = pageComponent,
                MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
                IsEnable = true,
                IsVisible = true,
                CreateUserId = -1,
                UpdateUserId = -1,
                CreateDate = now,
                UpdateDate = now
            };

            Context.AddRange(group, department, page, component, pageComponent, permission);
            await Context.SaveChangesAsync();
        }

        private sealed class EmptyServiceProvider : IServiceProvider
        {
            public static EmptyServiceProvider Instance { get; } = new();

            public object? GetService(Type serviceType) => null;
        }
    }
}
