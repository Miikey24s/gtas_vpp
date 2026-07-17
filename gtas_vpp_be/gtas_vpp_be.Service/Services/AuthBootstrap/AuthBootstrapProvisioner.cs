using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Service.Services.AuthBootstrap;

/// <summary>
/// Creates the first trusted application owner after legacy demo-account
/// containment. This class is intentionally unavailable through HTTP and is
/// invoked only by the guarded one-shot migration/reference-seed startup mode.
/// </summary>
public sealed class AuthBootstrapProvisioner : IAuthBootstrapProvisioner
{
    private const string CompletedStatus = "Completed";
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";
    private const string InMemoryProvider = "Microsoft.EntityFrameworkCore.InMemory";
    private const string ProductionDatabase = "GTAS_VPP_LIVE";
    private const string BootstrapLockResource = "GTAS_VPP:auth:bootstrap-owner";
    private const string BootstrapAuditAction = "AUTH_BOOTSTRAP_OWNER_CREATED";
    private const int SystemActorId = -1;
    private const int ReservedAccountIdFloor = 1_000_000_000;
    private const long ExpectedContainedLegacyAccounts = 12;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TestLocks =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly VPPContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly DatabaseBinding _databaseBinding;
    private readonly AuthBootstrapOptions _options;

    public AuthBootstrapProvisioner(
        VPPContext context,
        UserManager<AppUser> userManager,
        DatabaseBinding databaseBinding,
        IOptions<AuthBootstrapOptions> options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _databaseBinding = databaseBinding ?? throw new ArgumentNullException(nameof(databaseBinding));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentException("Bootstrap options are required.", nameof(options));
    }

    public async Task<AuthBootstrapResult> ProvisionAsync(CancellationToken cancellationToken = default)
    {
        var input = ValidateAndNormalize(_options);
        var provider = ValidateExecutionBoundary();
        EnsureUserManagerUsesThisContext();

        return provider switch
        {
            ExecutionProvider.SqlServer => await ProvisionWithSqlServerTransactionAsync(input, cancellationToken),
            ExecutionProvider.InMemoryTest => await ProvisionWithTestGuardAsync(input, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported bootstrap execution provider.")
        };
    }

    private async Task<AuthBootstrapResult> ProvisionWithSqlServerTransactionAsync(
        ValidatedInput input,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await AcquireSqlServerApplicationLockAsync(transaction, cancellationToken);
            await ValidateLegacyContainmentAsync(transaction, cancellationToken);

            var result = await ProvisionCoreAsync(input, isTransactional: true, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch
            {
                // Preserve the provisioning failure. Disposal also attempts to
                // leave an uncommitted relational transaction rolled back.
            }

            _context.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task<AuthBootstrapResult> ProvisionWithTestGuardAsync(
        ValidatedInput input,
        CancellationToken cancellationToken)
    {
        var lockKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{_databaseBinding.DataSource}|{_databaseBinding.DatabaseName}|{BootstrapLockResource}");
        var gate = TestLocks.GetOrAdd(lockKey, static _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken);
        try
        {
            return await ProvisionCoreAsync(input, isTransactional: false, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<AuthBootstrapResult> ProvisionCoreAsync(
        ValidatedInput input,
        bool isTransactional,
        CancellationToken cancellationToken)
    {
        AppUser? createdOwner = null;
        var ownerPersisted = false;

        try
        {
            var existingOperation = await _context.AuthBootstrapOperations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    operation => operation.OperationKey == input.OperationKey,
                    cancellationToken);

            if (existingOperation is not null)
            {
                return await VerifyCompletedOperationAsync(existingOperation, input, cancellationToken);
            }

            if (await _context.Users
                    .AsNoTracking()
                    .AnyAsync(user => user.AccountStatus == AppAccountStatus.Active, cancellationToken))
            {
                throw Failure(
                    AuthBootstrapFailure.PreexistingActiveAccount,
                    "Owner bootstrap requires zero preexisting active application accounts.");
            }

            if (await _context.UserGroupMemberships
                    .AsNoTracking()
                    .AnyAsync(membership => !membership.IsDeleted, cancellationToken))
            {
                throw Failure(
                    AuthBootstrapFailure.PreexistingActiveMembership,
                    "Owner bootstrap requires zero preexisting active memberships.");
            }

            var prerequisites = await ResolvePrerequisitesAsync(input, cancellationToken);
            var nowUtc = DateTime.UtcNow;

            createdOwner = new AppUser
            {
                UserName = input.Username,
                Email = input.Email,
                FullName = input.FullName,
                EmailConfirmed = true,
                LockoutEnabled = true,
                AccountStatus = AppAccountStatus.Active,
                MustChangePassword = true,
                MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
                SessionVersion = 1,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc,
                ActivatedAtUtc = nowUtc
            };

            var createResult = await _userManager.CreateAsync(createdOwner, input.InitialPassword);
            if (!createResult.Succeeded)
            {
                var errorCodes = string.Join(
                    ',',
                    createResult.Errors
                        .Select(error => error.Code)
                        .Where(code => !string.IsNullOrWhiteSpace(code))
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal));
                throw Failure(
                    AuthBootstrapFailure.AccountCreationFailed,
                    string.IsNullOrEmpty(errorCodes)
                        ? "The owner account failed Identity validation."
                        : $"The owner account failed Identity validation ({errorCodes}).");
            }

            ownerPersisted = true;
            if (createdOwner.Id <= 0
                || (_databaseBinding.EnvironmentName == DatabaseBinding.LiveEnvironment
                    && createdOwner.Id < ReservedAccountIdFloor))
            {
                throw Failure(
                    AuthBootstrapFailure.InvalidGeneratedAccountId,
                    "The generated account identifier is outside the reserved application-owned range.");
            }

            var membership = new UserGroupMembership
            {
                Id = Guid.NewGuid(),
                UserId = createdOwner.Id,
                AccountId = createdOwner.Id,
                PermissionGroupId = CanonicalRbac.SystemAdmin.GroupId,
                DepartmentId = prerequisites.Department.Id,
                Description = "Initial application owner membership.",
                CreatedByUserId = SystemActorId,
                UpdatedByUserId = SystemActorId,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc,
                IsDeleted = false
            };

            var correlationId = input.Fingerprint[..32];
            _context.UserGroupMemberships.Add(membership);
            _context.AuthBootstrapOperations.Add(new AuthBootstrapOperation
            {
                OperationKey = input.OperationKey,
                InputFingerprint = input.Fingerprint,
                AccountId = createdOwner.Id,
                Status = CompletedStatus,
                CompletedAtUtc = nowUtc
            });
            _context.SecurityAudits.Add(new SecurityAudit
            {
                Id = Guid.NewGuid(),
                ActorUserId = SystemActorId,
                TargetUserId = createdOwner.Id,
                Action = BootstrapAuditAction,
                ResourceType = nameof(AppUser),
                ResourceId = createdOwner.Id.ToString(CultureInfo.InvariantCulture),
                Outcome = "Success",
                Summary = "One-time clean application owner provisioned.",
                Reason = "Trusted-access cutover after legacy demo-account containment.",
                CorrelationId = correlationId,
                OccurredAtUtc = nowUtc
            });

            await _context.SaveChangesAsync(cancellationToken);
            await VerifyNewOwnerPostconditionAsync(createdOwner.Id, prerequisites.Department.Id, cancellationToken);

            return new AuthBootstrapResult(createdOwner.Id, AuthBootstrapOutcome.Provisioned);
        }
        catch (AuthBootstrapProvisioningException)
        {
            if (!isTransactional && ownerPersisted && createdOwner is not null)
            {
                await CompensateInMemoryTestMutationAsync(createdOwner.Id, input, CancellationToken.None);
            }

            throw;
        }
        catch (Exception)
        {
            if (!isTransactional && ownerPersisted && createdOwner is not null)
            {
                try
                {
                    await CompensateInMemoryTestMutationAsync(createdOwner.Id, input, CancellationToken.None);
                }
                catch (Exception)
                {
                    throw new AuthBootstrapProvisioningException(
                        AuthBootstrapFailure.TestRollbackFailed,
                        "The non-relational test bootstrap failed and its compensating rollback also failed.");
                }
            }

            throw new AuthBootstrapProvisioningException(
                AuthBootstrapFailure.PersistenceFailed,
                "The owner bootstrap failed before its atomic postcondition was committed.");
        }
    }

    private async Task<AuthBootstrapResult> VerifyCompletedOperationAsync(
        AuthBootstrapOperation operation,
        ValidatedInput input,
        CancellationToken cancellationToken)
    {
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(operation.InputFingerprint),
                Encoding.ASCII.GetBytes(input.Fingerprint)))
        {
            throw Failure(
                AuthBootstrapFailure.OperationFingerprintMismatch,
                "The operation key was already used with different non-secret owner inputs.");
        }

        if (!string.Equals(operation.Status, CompletedStatus, StringComparison.Ordinal))
        {
            throw Failure(
                AuthBootstrapFailure.OperationStateInvalid,
                "The bootstrap ledger is not in the completed state.");
        }

        var prerequisites = await ResolvePrerequisitesAsync(input, cancellationToken);
        var owner = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == operation.AccountId, cancellationToken);

        if (owner is null
            || owner.AccountStatus != AppAccountStatus.Active
            || !owner.EmailConfirmed
            || !owner.MustChangePassword
            || !string.Equals(owner.NormalizedUserName, input.NormalizedUsername, StringComparison.Ordinal)
            || !string.Equals(owner.NormalizedEmail, input.NormalizedEmail, StringComparison.Ordinal)
            || !string.Equals(owner.FullName, input.FullName, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(owner.PasswordHash)
            || _userManager.PasswordHasher.VerifyHashedPassword(
                owner,
                owner.PasswordHash,
                input.InitialPassword) == PasswordVerificationResult.Failed)
        {
            throw Failure(
                AuthBootstrapFailure.CompletedStateMismatch,
                "The completed bootstrap ledger does not match the current owner account state.");
        }

        var matchingMemberships = await _context.UserGroupMemberships
            .AsNoTracking()
            .Where(membership => !membership.IsDeleted && membership.AccountId == owner.Id)
            .OrderBy(membership => membership.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (matchingMemberships.Count != 1
            || matchingMemberships[0].UserId != owner.Id
            || matchingMemberships[0].PermissionGroupId != CanonicalRbac.SystemAdmin.GroupId
            || matchingMemberships[0].DepartmentId != prerequisites.Department.Id)
        {
            throw Failure(
                AuthBootstrapFailure.CompletedStateMismatch,
                "The completed bootstrap ledger does not match the current owner membership state.");
        }

        return new AuthBootstrapResult(owner.Id, AuthBootstrapOutcome.AlreadyCompleted);
    }

    private async Task<BootstrapPrerequisites> ResolvePrerequisitesAsync(
        ValidatedInput input,
        CancellationToken cancellationToken)
    {
        var groupExists = await _context.PermissionGroups
            .AsNoTracking()
            .AnyAsync(
                group => group.Id == CanonicalRbac.SystemAdmin.GroupId
                    && group.GroupCode == CanonicalRbac.SystemAdmin.GroupCode
                    && !group.IsDeleted,
                cancellationToken);
        if (!groupExists)
        {
            throw Failure(
                AuthBootstrapFailure.MissingSystemAdminGroup,
                "The canonical active System Admin group is missing.");
        }

        var departments = await _context.Departments
            .Where(department => !department.IsDeleted
                && department.Code == input.PrimaryDepartmentCode)
            .OrderBy(department => department.Id)
            .Take(2)
            .ToListAsync(cancellationToken);
        if (departments.Count == 0 && input.CreatePrimaryDepartmentIfMissing)
        {
            if (!_databaseBinding.IsTestEnvironment
                || !HasTestOrDemoToken(_databaseBinding.DatabaseName)
                || !_context.Database.IsRelational())
            {
                throw Failure(
                    AuthBootstrapFailure.InvalidPrimaryDepartment,
                    "Automatic department creation is allowed only for a relational TEST or DEMO database.");
            }

            var now = DateTime.UtcNow;
            var createdDepartment = new Department
            {
                Id = Guid.NewGuid(),
                Code = input.PrimaryDepartmentCode,
                Name = input.PrimaryDepartmentName,
                CreatedByUserId = SystemActorId,
                CreatedAtUtc = now,
                UpdatedByUserId = SystemActorId,
                UpdatedAtUtc = now,
                IsDeleted = false
            };
            _context.Departments.Add(createdDepartment);
            await _context.SaveChangesAsync(cancellationToken);
            departments.Add(createdDepartment);
        }

        if (departments.Count != 1)
        {
            throw Failure(
                AuthBootstrapFailure.MissingPrimaryDepartment,
                "The primary department code must resolve to exactly one active department.");
        }

        var department = departments[0];
        if (department.Id == Guid.Empty)
        {
            throw Failure(
                AuthBootstrapFailure.InvalidPrimaryDepartment,
                "The selected department is invalid.");
        }

        var hasPermissionManage = await _context.GroupPageComponentMappings
            .AsNoTracking()
            .AnyAsync(mapping =>
                mapping.PermissionGroupId == CanonicalRbac.SystemAdmin.GroupId
                && mapping.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode
                && mapping.IsEnable
                && mapping.IsVisible
                && mapping.PageComponentMapping != null
                && mapping.PageComponentMapping.PermissionPage != null
                && !mapping.PageComponentMapping.PermissionPage.IsDeleted
                && mapping.PageComponentMapping.PermissionComponent != null
                && !mapping.PageComponentMapping.PermissionComponent.IsDeleted
                && mapping.PageComponentMapping.PermissionComponent.ComponentCode == Permissions.PermissionManage,
                cancellationToken);
        if (!hasPermissionManage)
        {
            throw Failure(
                AuthBootstrapFailure.MissingPermissionManage,
                "The canonical System Admin group lacks an effective PermissionManage mapping.");
        }

        return new BootstrapPrerequisites(department);
    }

    private async Task VerifyNewOwnerPostconditionAsync(
        int accountId,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var activeOwnerCount = await _context.Users
            .AsNoTracking()
            .CountAsync(user => user.Id == accountId
                && user.AccountStatus == AppAccountStatus.Active
                && user.EmailConfirmed
                && user.MustChangePassword,
                cancellationToken);
        var activeMembershipCount = await _context.UserGroupMemberships
            .AsNoTracking()
            .CountAsync(membership => !membership.IsDeleted
                && membership.AccountId == accountId
                && membership.UserId == accountId
                && membership.PermissionGroupId == CanonicalRbac.SystemAdmin.GroupId
                && membership.DepartmentId == departmentId,
                cancellationToken);
        var ledgerCount = await _context.AuthBootstrapOperations
            .AsNoTracking()
            .CountAsync(operation => operation.AccountId == accountId
                && operation.Status == CompletedStatus,
                cancellationToken);
        var auditCount = await _context.SecurityAudits
            .AsNoTracking()
            .CountAsync(audit => audit.TargetUserId == accountId
                && audit.Action == BootstrapAuditAction
                && audit.Outcome == "Success",
                cancellationToken);

        if (activeOwnerCount != 1
            || activeMembershipCount != 1
            || ledgerCount != 1
            || auditCount != 1)
        {
            throw Failure(
                AuthBootstrapFailure.CompletedStateMismatch,
                "The owner bootstrap postcondition is incomplete.");
        }
    }

    private async Task AcquireSqlServerApplicationLockAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 15000,
                @DbPrincipal = N'public';
            SELECT @result;
            """;
        AddParameter(command, "@resource", BootstrapLockResource);

        var rawResult = await command.ExecuteScalarAsync(cancellationToken);
        if (rawResult is null
            || rawResult is DBNull
            || Convert.ToInt32(rawResult, CultureInfo.InvariantCulture) < 0)
        {
            throw Failure(
                AuthBootstrapFailure.LockUnavailable,
                "The exclusive owner-bootstrap application lock could not be acquired.");
        }
    }

    private async Task ValidateLegacyContainmentAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (_databaseBinding.EnvironmentName != DatabaseBinding.LiveEnvironment)
        {
            return;
        }

        try
        {
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = """
                SET NOCOUNT ON;

                IF DB_ID(N'GTAS_MENU') IS NULL
                    THROW 51000, 'Legacy containment source is unavailable.', 1;

                IF OBJECT_ID(N'GTAS_MENU.dbo.tblUsers', N'U') IS NULL
                    THROW 51000, 'Legacy containment source is unavailable.', 1;

                SELECT
                    TotalAccounts = COUNT_BIG(*),
                    ActiveUnlocked = COALESCE(SUM(CONVERT(bigint, CASE
                        WHEN ISNULL(IsInactiveFlg, 0) = 0 AND ISNULL(IsLockedFlg, 0) = 0 THEN 1 ELSE 0 END)), 0),
                    InactiveAccounts = COALESCE(SUM(CONVERT(bigint, CASE WHEN IsInactiveFlg = 1 THEN 1 ELSE 0 END)), 0),
                    LockedAccounts = COALESCE(SUM(CONVERT(bigint, CASE WHEN IsLockedFlg = 1 THEN 1 ELSE 0 END)), 0)
                FROM GTAS_MENU.dbo.tblUsers WITH (UPDLOCK, HOLDLOCK);
                """;

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw Failure(
                    AuthBootstrapFailure.LegacyContainmentDrift,
                    "The legacy containment postcondition could not be verified.");
            }

            var total = reader.GetInt64(0);
            var activeUnlocked = reader.GetInt64(1);
            var inactive = reader.GetInt64(2);
            var locked = reader.GetInt64(3);
            if (total != ExpectedContainedLegacyAccounts
                || activeUnlocked != 0
                || inactive != ExpectedContainedLegacyAccounts
                || locked != ExpectedContainedLegacyAccounts)
            {
                throw Failure(
                    AuthBootstrapFailure.LegacyContainmentDrift,
                    "The legacy demo-account containment postcondition has drifted.");
            }
        }
        catch (AuthBootstrapProvisioningException)
        {
            throw;
        }
        catch (DbException exception)
        {
            throw new AuthBootstrapProvisioningException(
                AuthBootstrapFailure.LegacyContainmentDrift,
                "The legacy containment postcondition could not be verified.",
                exception);
        }
    }

    private async Task CompensateInMemoryTestMutationAsync(
        int accountId,
        ValidatedInput input,
        CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();

        var memberships = await _context.UserGroupMemberships
            .Where(membership => membership.AccountId == accountId)
            .ToListAsync(cancellationToken);
        var ledger = await _context.AuthBootstrapOperations
            .SingleOrDefaultAsync(operation => operation.OperationKey == input.OperationKey, cancellationToken);
        var audits = await _context.SecurityAudits
            .Where(audit => audit.TargetUserId == accountId && audit.Action == BootstrapAuditAction)
            .ToListAsync(cancellationToken);
        var owner = await _context.Users
            .SingleOrDefaultAsync(user => user.Id == accountId, cancellationToken);

        _context.UserGroupMemberships.RemoveRange(memberships);
        if (ledger is not null)
        {
            _context.AuthBootstrapOperations.Remove(ledger);
        }

        _context.SecurityAudits.RemoveRange(audits);
        if (owner is not null)
        {
            _context.Users.Remove(owner);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _context.ChangeTracker.Clear();
    }

    private ExecutionProvider ValidateExecutionBoundary()
    {
        var isRelational = _context.Database.IsRelational();
        var providerName = _context.Database.ProviderName;

        if (_databaseBinding.EnvironmentName == DatabaseBinding.LiveEnvironment)
        {
            if (!string.Equals(
                    _databaseBinding.DatabaseName,
                    ProductionDatabase,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw Failure(
                    AuthBootstrapFailure.UnsafeDatabaseBinding,
                    "Live owner bootstrap must target the canonical production database.");
            }

            if (!isRelational || !string.Equals(providerName, SqlServerProvider, StringComparison.Ordinal))
            {
                throw Failure(
                    AuthBootstrapFailure.UnsupportedProvider,
                    "Live owner bootstrap requires the SQL Server relational provider.");
            }
        }
        else if (_databaseBinding.IsTestEnvironment
            && !HasTestOrDemoToken(_databaseBinding.DatabaseName))
        {
            throw Failure(
                AuthBootstrapFailure.UnsafeDatabaseBinding,
                "Non-production owner bootstrap requires an explicitly named TEST or DEMO database.");
        }

        if (isRelational)
        {
            if (!string.Equals(providerName, SqlServerProvider, StringComparison.Ordinal))
            {
                throw Failure(
                    AuthBootstrapFailure.UnsupportedProvider,
                    "Relational owner bootstrap supports SQL Server only.");
            }

            var connection = _context.Database.GetDbConnection();
            if (!string.Equals(connection.Database, _databaseBinding.DatabaseName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(connection.DataSource, _databaseBinding.DataSource, StringComparison.OrdinalIgnoreCase))
            {
                throw Failure(
                    AuthBootstrapFailure.UnsafeDatabaseBinding,
                    "The EF connection does not match the explicit deployment database binding.");
            }

            return ExecutionProvider.SqlServer;
        }

        if (_databaseBinding.IsTestEnvironment
            && string.Equals(providerName, InMemoryProvider, StringComparison.Ordinal))
        {
            return ExecutionProvider.InMemoryTest;
        }

        throw Failure(
            AuthBootstrapFailure.UnsupportedProvider,
            "Only SQL Server production execution or the isolated InMemory test path is supported.");
    }

    private void EnsureUserManagerUsesThisContext()
    {
        var storeProperty = typeof(UserManager<AppUser>).GetProperty(
            "Store",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var store = storeProperty?.GetValue(_userManager);
        var contextProperty = store?.GetType().GetProperty(
            "Context",
            BindingFlags.Instance | BindingFlags.Public);
        var storeContext = contextProperty?.GetValue(store);

        if (!ReferenceEquals(storeContext, _context))
        {
            throw Failure(
                AuthBootstrapFailure.UserManagerContextMismatch,
                "UserManager must use the same VPPContext instance as the bootstrap transaction.");
        }
    }

    private ValidatedInput ValidateAndNormalize(AuthBootstrapOptions options)
    {
        if (!options.Enabled)
        {
            throw Failure(
                AuthBootstrapFailure.Disabled,
                "Owner bootstrap is disabled by default and was not explicitly enabled.");
        }

        var operationKey = options.OperationKey?.Trim() ?? string.Empty;
        var username = options.Username?.Trim() ?? string.Empty;
        var email = options.Email?.Trim() ?? string.Empty;
        var fullName = options.FullName?.Trim().Normalize(NormalizationForm.FormC) ?? string.Empty;
        var initialPassword = options.InitialPassword ?? string.Empty;
        var primaryDepartmentCode = options.PrimaryDepartmentCode?.Trim() ?? string.Empty;
        var primaryDepartmentName = options.PrimaryDepartmentName?.Trim()
            .Normalize(NormalizationForm.FormC) ?? string.Empty;

        var operationKeyValid = operationKey.Length is >= 1 and <= 128
            && operationKey.All(character => char.IsAsciiLetterOrDigit(character)
                || character is '-' or '_' or '.' or ':');
        var emailValid = MailAddress.TryCreate(email, out var parsedEmail)
            && string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase);

        if (!operationKeyValid
            || username.Length is < 1 or > 256
            || !emailValid
            || email.Length > 256
            || fullName.Length is < 1 or > 250
            || initialPassword.Length is < 1 or > 1024
            || primaryDepartmentCode.Length is < 1 or > 50
            || (options.CreatePrimaryDepartmentIfMissing
                && primaryDepartmentName.Length is < 1 or > 200))
        {
            throw Failure(
                AuthBootstrapFailure.InvalidOptions,
                "Owner bootstrap options are incomplete or invalid.");
        }

        var normalizedUsername = _userManager.NormalizeName(username);
        var normalizedEmail = _userManager.NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedUsername) || string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw Failure(
                AuthBootstrapFailure.InvalidOptions,
                "Owner bootstrap identity normalization failed.");
        }

        var fingerprint = ComputeInputFingerprint(
            operationKey,
            normalizedUsername,
            normalizedEmail,
            fullName,
            primaryDepartmentCode,
            options.CreatePrimaryDepartmentIfMissing,
            primaryDepartmentName);

        return new ValidatedInput(
            operationKey,
            username,
            normalizedUsername,
            email,
            normalizedEmail,
            fullName,
            initialPassword,
            primaryDepartmentCode,
            options.CreatePrimaryDepartmentIfMissing,
            primaryDepartmentName,
            fingerprint);
    }

    private static string ComputeInputFingerprint(
        string operationKey,
        string normalizedUsername,
        string normalizedEmail,
        string fullName,
        string primaryDepartmentCode,
        bool createPrimaryDepartmentIfMissing,
        string primaryDepartmentName)
    {
        var builder = new StringBuilder();
        AppendFingerprintField(builder, "operation", operationKey);
        AppendFingerprintField(builder, "username", normalizedUsername);
        AppendFingerprintField(builder, "email", normalizedEmail);
        AppendFingerprintField(builder, "fullName", fullName);
        AppendFingerprintField(builder, "department", primaryDepartmentCode.ToUpperInvariant());
        AppendFingerprintField(builder, "createDepartment", createPrimaryDepartmentIfMissing ? "1" : "0");
        AppendFingerprintField(builder, "departmentName", primaryDepartmentName);
        AppendFingerprintField(builder, "groupId", CanonicalRbac.SystemAdmin.GroupId.ToString("D"));
        AppendFingerprintField(builder, "groupCode", CanonicalRbac.SystemAdmin.GroupCode);
        AppendFingerprintField(
            builder,
            "company",
            CanonicalRbac.DefaultMemberCompanyCode.ToString(CultureInfo.InvariantCulture));

        // Deliberately exclude the initial password. The completed-state rerun
        // verifies it against Identity's salted hash without persisting a
        // second password-derived value in the non-secret ledger.
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void AppendFingerprintField(StringBuilder builder, string name, string value)
    {
        builder.Append(name)
            .Append(':')
            .Append(value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(value)
            .Append('\n');
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static bool HasTestOrDemoToken(string databaseName) =>
        databaseName.Contains("TEST", StringComparison.OrdinalIgnoreCase)
        || databaseName.Contains("DEMO", StringComparison.OrdinalIgnoreCase);

    private static AuthBootstrapProvisioningException Failure(
        AuthBootstrapFailure failure,
        string message) => new(failure, message);

    private enum ExecutionProvider
    {
        SqlServer,
        InMemoryTest
    }

    private sealed record ValidatedInput(
        string OperationKey,
        string Username,
        string NormalizedUsername,
        string Email,
        string NormalizedEmail,
        string FullName,
        string InitialPassword,
        string PrimaryDepartmentCode,
        bool CreatePrimaryDepartmentIfMissing,
        string PrimaryDepartmentName,
        string Fingerprint);

    private sealed record BootstrapPrerequisites(Department Department);
}
