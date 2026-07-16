using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Permission;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace gtas_vpp_be.Authorization;

public sealed record MembershipAdministrationResult(
    int StatusCode,
    string Code,
    string Message,
    MembershipAdministrationResDTO? Membership = null)
{
    public bool Succeeded => StatusCode is >= 200 and < 300;

    public static MembershipAdministrationResult Success(MembershipAdministrationResDTO membership) =>
        new(StatusCodes.Status200OK, "MEMBERSHIP_UPDATED", "Membership updated.", membership);

    public static MembershipAdministrationResult BadRequest(string code, string message) =>
        new(StatusCodes.Status400BadRequest, code, message);

    public static MembershipAdministrationResult NotFound(string code, string message) =>
        new(StatusCodes.Status404NotFound, code, message);

    public static MembershipAdministrationResult Conflict(string code, string message) =>
        new(StatusCodes.Status409Conflict, code, message);
}

public interface IMembershipAdministrationService
{
    Task<MembershipAdministrationResult> UpsertAsync(
        int actorAccountId,
        MembershipUpsertReqDTO command,
        CancellationToken cancellationToken = default);

    Task<MembershipAdministrationResult> DeactivateAsync(
        int actorAccountId,
        MembershipDeactivateReqDTO command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The single mutation boundary for account-to-persona membership. All audit
/// fields, soft-delete state and session invalidation are owned by this service.
/// </summary>
public sealed class MembershipAdministrationService(
    VPPContext context,
    IPermissionChangeNotifier permissionChangeNotifier,
    IDateTimeProvider dateTimeProvider,
    ILogger<MembershipAdministrationService> logger) : IMembershipAdministrationService
{
    private const string DepartmentType = "PhongBan";
    private const string LastSystemAdminLock = "GTAS_VPP:AUTH:LAST_SYSTEM_ADMIN";
    private readonly VPPContext _context = context;
    private readonly IPermissionChangeNotifier _permissionChangeNotifier = permissionChangeNotifier;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly ILogger<MembershipAdministrationService> _logger = logger;

    public async Task<MembershipAdministrationResult> UpsertAsync(
        int actorAccountId,
        MembershipUpsertReqDTO command,
        CancellationToken cancellationToken = default)
    {
        var basicValidation = ValidateCommand(actorAccountId, command.AccountId, command.Reason);
        if (basicValidation is not null)
        {
            return basicValidation;
        }

        if (command.GroupId == Guid.Empty)
        {
            return MembershipAdministrationResult.BadRequest(
                "GROUP_REQUIRED",
                "A canonical permission group is required.");
        }

        if (command.PrimaryDepartmentId == Guid.Empty)
        {
            return MembershipAdministrationResult.BadRequest(
                "PRIMARY_DEPARTMENT_REQUIRED",
                "A primary department is required.");
        }

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await BeginSerializableTransactionAsync(cancellationToken);
            if (!await AcquireAdministrationLocksAsync(command.AccountId, cancellationToken))
            {
                return MembershipAdministrationResult.Conflict(
                    "MEMBERSHIP_BUSY",
                    "Another membership change is in progress. Please retry.");
            }

            var accountResult = await GetActiveAccountAsync(command.AccountId, cancellationToken);
            if (accountResult.Account is null)
            {
                return accountResult.Failure!;
            }

            var groupResult = await GetCanonicalGroupAsync(command.GroupId, cancellationToken);
            if (groupResult.Group is null)
            {
                return groupResult.Failure!;
            }

            var departmentResult = await GetActiveDepartmentAsync(
                command.PrimaryDepartmentId,
                cancellationToken);
            if (departmentResult.Department is null)
            {
                return departmentResult.Failure!;
            }

            var memberships = await GetCurrentMembershipsAsync(command.AccountId, cancellationToken);
            if (memberships.Count > 1)
            {
                return MembershipAdministrationResult.Conflict(
                    "MULTIPLE_ACTIVE_MEMBERSHIPS",
                    "The account has conflicting active memberships and requires data repair.");
            }

            var current = memberships.SingleOrDefault();
            if (current is not null
                && (current.AccountId != command.AccountId || current.UserId != command.AccountId))
            {
                return MembershipAdministrationResult.Conflict(
                    "LEGACY_ACTIVE_MEMBERSHIP",
                    "The account has an incompatible active legacy membership and requires data repair.");
            }

            byte[]? expectedRowVersion = null;
            if (current is null)
            {
                if (!string.IsNullOrWhiteSpace(command.ExpectedRowVersion))
                {
                    return MembershipAdministrationResult.Conflict(
                        "MEMBERSHIP_CHANGED",
                        "The membership state changed. Reload the account before trying again.");
                }
            }
            else
            {
                var rowVersionValidation = ValidateExpectedRowVersion(
                    command.ExpectedRowVersion,
                    current.RowVersion,
                    out expectedRowVersion);
                if (rowVersionValidation is not null)
                {
                    return rowVersionValidation;
                }
            }

            var oldGroupId = current?.P02_GroupId;
            if (current is not null
                && current.P02_GroupId != command.GroupId
                && await IsFinalEffectiveSystemAdminAsync(command.AccountId, cancellationToken))
            {
                return MembershipAdministrationResult.Conflict(
                    "LAST_SYSTEM_ADMIN",
                    "The final effective system administrator cannot be reassigned.");
            }

            var account = accountResult.Account;
            var group = groupResult.Group;
            var department = departmentResult.Department;
            var now = _dateTimeProvider.Now;
            var nowUtc = DateTime.UtcNow;
            var reason = NormalizeReason(command.Reason);
            var membership = current ?? new P04_UserGroup
            {
                Id = Guid.NewGuid(),
                UserId = account.Id,
                AccountId = account.Id,
                P02_GroupId = group.Id,
                LEX02_CompanyDepartmentLocationId = department.Id,
                CreateUserId = actorAccountId,
                CreateDate = now,
                UpdateUserId = actorAccountId,
                UpdateDate = now,
                IsDeleted = false
            };

            if (current is null)
            {
                _context.P04_UserGroups.Add(membership);
            }
            else
            {
                _context.Entry(current).Property(x => x.RowVersion).OriginalValue = expectedRowVersion!;
                membership.P02_GroupId = group.Id;
                membership.LEX02_CompanyDepartmentLocationId = department.Id;
                membership.UpdateUserId = actorAccountId;
                membership.UpdateDate = now;
            }

            InvalidateSessions(account, nowUtc);
            _context.A01_SecurityAudits.Add(new A01_SecurityAudit
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorAccountId,
                TargetUserId = account.Id,
                Action = current is null ? "MEMBERSHIP_CREATED" : "MEMBERSHIP_UPDATED",
                ResourceType = "P04_UserGroup",
                ResourceId = membership.Id.ToString(),
                Outcome = "Succeeded",
                Summary = current is null
                    ? $"Assigned {group.GroupCode} with primary department {department.LEX02Code}."
                    : $"Changed membership from {oldGroupId} to {group.GroupCode} with primary department {department.LEX02Code}.",
                Reason = reason,
                CorrelationId = Activity.Current?.TraceId.ToString(),
                OccurredAtUtc = nowUtc
            });

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            var response = MapResponse(account, membership, group, department, isActive: true);
            await NotifyAfterCommitAsync(
                account.Id,
                oldGroupId.HasValue ? [oldGroupId.Value, group.Id] : [group.Id]);
            return MembershipAdministrationResult.Success(response);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await RollbackAndClearAsync(transaction);
            _logger.LogWarning(
                exception,
                "Membership concurrency conflict for AccountId={AccountId}.",
                command.AccountId);
            return MembershipAdministrationResult.Conflict(
                "MEMBERSHIP_CHANGED",
                "The membership was changed by another administrator. Reload and try again.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await RollbackAndClearAsync(transaction);
            _logger.LogWarning(
                exception,
                "Membership uniqueness conflict for AccountId={AccountId}.",
                command.AccountId);
            return MembershipAdministrationResult.Conflict(
                "ACTIVE_MEMBERSHIP_EXISTS",
                "The account already has an active membership. Reload and try again.");
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<MembershipAdministrationResult> DeactivateAsync(
        int actorAccountId,
        MembershipDeactivateReqDTO command,
        CancellationToken cancellationToken = default)
    {
        var basicValidation = ValidateCommand(actorAccountId, command.AccountId, command.Reason);
        if (basicValidation is not null)
        {
            return basicValidation;
        }

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await BeginSerializableTransactionAsync(cancellationToken);
            if (!await AcquireAdministrationLocksAsync(command.AccountId, cancellationToken))
            {
                return MembershipAdministrationResult.Conflict(
                    "MEMBERSHIP_BUSY",
                    "Another membership change is in progress. Please retry.");
            }

            var accountResult = await GetActiveAccountAsync(command.AccountId, cancellationToken);
            if (accountResult.Account is null)
            {
                return accountResult.Failure!;
            }

            var memberships = await GetCurrentMembershipsAsync(command.AccountId, cancellationToken);
            if (memberships.Count == 0)
            {
                return MembershipAdministrationResult.NotFound(
                    "ACTIVE_MEMBERSHIP_NOT_FOUND",
                    "The account does not have an active membership.");
            }

            if (memberships.Count > 1)
            {
                return MembershipAdministrationResult.Conflict(
                    "MULTIPLE_ACTIVE_MEMBERSHIPS",
                    "The account has conflicting active memberships and requires data repair.");
            }

            var membership = memberships[0];
            if (membership.AccountId != command.AccountId || membership.UserId != command.AccountId)
            {
                return MembershipAdministrationResult.Conflict(
                    "LEGACY_ACTIVE_MEMBERSHIP",
                    "The account has an incompatible active legacy membership and requires data repair.");
            }

            var rowVersionValidation = ValidateExpectedRowVersion(
                command.ExpectedRowVersion,
                membership.RowVersion,
                out var expectedRowVersion);
            if (rowVersionValidation is not null)
            {
                return rowVersionValidation;
            }

            var groupResult = await GetCanonicalGroupAsync(membership.P02_GroupId, cancellationToken);
            if (groupResult.Group is null)
            {
                return MembershipAdministrationResult.Conflict(
                    "INVALID_ACTIVE_GROUP",
                    "The active membership has an invalid group and requires data repair.");
            }

            var departmentResult = await GetActiveDepartmentAsync(
                membership.LEX02_CompanyDepartmentLocationId,
                cancellationToken);
            if (departmentResult.Department is null)
            {
                return MembershipAdministrationResult.Conflict(
                    "INVALID_ACTIVE_DEPARTMENT",
                    "The active membership has an invalid primary department and requires data repair.");
            }

            if (await IsFinalEffectiveSystemAdminAsync(command.AccountId, cancellationToken))
            {
                return MembershipAdministrationResult.Conflict(
                    "LAST_SYSTEM_ADMIN",
                    "The final effective system administrator cannot be deactivated.");
            }

            var account = accountResult.Account;
            var group = groupResult.Group;
            var department = departmentResult.Department;
            var now = _dateTimeProvider.Now;
            var nowUtc = DateTime.UtcNow;
            _context.Entry(membership).Property(x => x.RowVersion).OriginalValue = expectedRowVersion!;
            membership.IsDeleted = true;
            membership.UpdateUserId = actorAccountId;
            membership.UpdateDate = now;
            InvalidateSessions(account, nowUtc);

            _context.A01_SecurityAudits.Add(new A01_SecurityAudit
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorAccountId,
                TargetUserId = account.Id,
                Action = "MEMBERSHIP_DEACTIVATED",
                ResourceType = "P04_UserGroup",
                ResourceId = membership.Id.ToString(),
                Outcome = "Succeeded",
                Summary = $"Deactivated {group.GroupCode} membership from primary department {department.LEX02Code}.",
                Reason = NormalizeReason(command.Reason),
                CorrelationId = Activity.Current?.TraceId.ToString(),
                OccurredAtUtc = nowUtc
            });

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            var response = MapResponse(account, membership, group, department, isActive: false);
            await NotifyAfterCommitAsync(account.Id, [group.Id]);
            return MembershipAdministrationResult.Success(response);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await RollbackAndClearAsync(transaction);
            _logger.LogWarning(
                exception,
                "Membership deactivation concurrency conflict for AccountId={AccountId}.",
                command.AccountId);
            return MembershipAdministrationResult.Conflict(
                "MEMBERSHIP_CHANGED",
                "The membership was changed by another administrator. Reload and try again.");
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private static MembershipAdministrationResult? ValidateCommand(
        int actorAccountId,
        int targetAccountId,
        string? reason)
    {
        if (actorAccountId <= 0)
        {
            return MembershipAdministrationResult.Conflict(
                "ACTOR_NOT_AVAILABLE",
                "The current administrator identity is unavailable.");
        }

        if (targetAccountId <= 0)
        {
            return MembershipAdministrationResult.BadRequest(
                "ACCOUNT_REQUIRED",
                "A target account is required.");
        }

        if (actorAccountId == targetAccountId)
        {
            return MembershipAdministrationResult.Conflict(
                "SELF_MEMBERSHIP_CHANGE",
                "Administrators cannot change their own membership.");
        }

        if (reason?.Trim().Length > 500)
        {
            return MembershipAdministrationResult.BadRequest(
                "REASON_TOO_LONG",
                "The reason cannot exceed 500 characters.");
        }

        return null;
    }

    private async Task<(AppUser? Account, MembershipAdministrationResult? Failure)> GetActiveAccountAsync(
        int accountId,
        CancellationToken cancellationToken)
    {
        var account = await _context.Users.SingleOrDefaultAsync(
            user => user.Id == accountId,
            cancellationToken);
        if (account is null)
        {
            return (null, MembershipAdministrationResult.NotFound(
                "ACCOUNT_NOT_FOUND",
                "The application account was not found."));
        }

        if (account.AccountStatus != AppAccountStatus.Active
            || (account.LockoutEnd.HasValue && account.LockoutEnd > DateTimeOffset.UtcNow))
        {
            return (null, MembershipAdministrationResult.Conflict(
                "ACCOUNT_NOT_ACTIVE",
                "Only an active, unlocked application account can receive a membership change."));
        }

        return (account, null);
    }

    private async Task<(P02_Group? Group, MembershipAdministrationResult? Failure)> GetCanonicalGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var canonical = CanonicalRbac.Personas.SingleOrDefault(persona => persona.GroupId == groupId);
        if (canonical is null)
        {
            return (null, MembershipAdministrationResult.BadRequest(
                "GROUP_NOT_CANONICAL",
                "The selected group is not one of the supported flat personas."));
        }

        var group = await _context.P02_Groups
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == groupId, cancellationToken);
        if (group is null)
        {
            return (null, MembershipAdministrationResult.NotFound(
                "GROUP_NOT_FOUND",
                "The selected permission group was not found."));
        }

        if (group.IsDeleted
            || group.ParentGroupId.HasValue
            || !string.Equals(group.GroupCode, canonical.GroupCode, StringComparison.Ordinal))
        {
            return (null, MembershipAdministrationResult.Conflict(
                "GROUP_NOT_ACTIVE_CANONICAL",
                "The selected group is not an active canonical flat persona."));
        }

        return (group, null);
    }

    private async Task<(LEX02_CompanyDepartmentLocation? Department, MembershipAdministrationResult? Failure)>
        GetActiveDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        if (departmentId == Guid.Empty)
        {
            return (null, MembershipAdministrationResult.BadRequest(
                "PRIMARY_DEPARTMENT_REQUIRED",
                "A primary department is required."));
        }

        var department = await _context.LEX02_CompanyDepartmentLocations
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == departmentId, cancellationToken);
        if (department is null)
        {
            return (null, MembershipAdministrationResult.NotFound(
                "PRIMARY_DEPARTMENT_NOT_FOUND",
                "The selected primary department was not found."));
        }

        if (department.IsDeleted
            || string.IsNullOrWhiteSpace(department.LEX02Code)
            || string.IsNullOrWhiteSpace(department.LEX02Name)
            || !string.Equals(department.LEX02Type, DepartmentType, StringComparison.OrdinalIgnoreCase))
        {
            return (null, MembershipAdministrationResult.Conflict(
                "PRIMARY_DEPARTMENT_NOT_ACTIVE",
                "The selected primary department is not active."));
        }

        return (department, null);
    }

    private Task<List<P04_UserGroup>> GetCurrentMembershipsAsync(
        int accountId,
        CancellationToken cancellationToken) =>
        _context.P04_UserGroups
            .Where(membership => !membership.IsDeleted
                && (membership.AccountId == accountId || membership.UserId == accountId))
            .OrderBy(membership => membership.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

    private async Task<bool> IsFinalEffectiveSystemAdminAsync(
        int targetAccountId,
        CancellationToken cancellationToken)
    {
        var effectiveAdminIds = await (
                from account in _context.Users.AsNoTracking()
                join membership in _context.P04_UserGroups.AsNoTracking()
                    on account.Id equals membership.AccountId
                join roleGroup in _context.P02_Groups.AsNoTracking()
                    on membership.P02_GroupId equals roleGroup.Id
                join department in _context.LEX02_CompanyDepartmentLocations.AsNoTracking()
                    on membership.LEX02_CompanyDepartmentLocationId equals department.Id
                where account.AccountStatus == AppAccountStatus.Active
                      && (account.LockoutEnd == null || account.LockoutEnd <= DateTimeOffset.UtcNow)
                      && membership.UserId == account.Id
                      && !membership.IsDeleted
                      && !roleGroup.IsDeleted
                      && roleGroup.ParentGroupId == null
                      && roleGroup.GroupCode == CanonicalRbac.SystemAdmin.GroupCode
                      && !department.IsDeleted
                      && department.Id != Guid.Empty
                      && _context.P06_GroupPageComponentMappings.Any(mapping =>
                          mapping.P02_GroupId == roleGroup.Id
                          && mapping.MemberCompanyCode == account.MemberCompanyCode
                          && mapping.IsVisible
                          && mapping.IsEnable
                          && mapping.P05_PageComponentMapping != null
                          && mapping.P05_PageComponentMapping.P01_Page != null
                          && !mapping.P05_PageComponentMapping.P01_Page.IsDeleted
                          && mapping.P05_PageComponentMapping.P03_Component != null
                          && !mapping.P05_PageComponentMapping.P03_Component.IsDeleted
                          && mapping.P05_PageComponentMapping.P03_Component.ComponentCode
                              == Permissions.PermissionManage)
                select account.Id)
            .Distinct()
            .Take(2)
            .ToListAsync(cancellationToken);

        return effectiveAdminIds.Count == 1 && effectiveAdminIds[0] == targetAccountId;
    }

    private async Task<IDbContextTransaction?> BeginSerializableTransactionAsync(
        CancellationToken cancellationToken)
    {
        if (!_context.Database.IsRelational())
        {
            return null;
        }

        return await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
    }

    private async Task<bool> AcquireAdministrationLocksAsync(
        int accountId,
        CancellationToken cancellationToken)
    {
        if (!UsesSqlServer())
        {
            return true;
        }

        // Always acquire in the same order to avoid deadlocks between two
        // concurrent system-administrator mutations.
        return await AcquireSqlServerTransactionLockAsync(LastSystemAdminLock, cancellationToken)
            && await AcquireSqlServerTransactionLockAsync(
                $"GTAS_VPP:AUTH:MEMBERSHIP:{accountId}",
                cancellationToken);
    }

    private async Task<bool> AcquireSqlServerTransactionLockAsync(
        string resource,
        CancellationToken cancellationToken)
    {
        var transaction = _context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("A transaction is required for membership locks.");
        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            SELECT @result;
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.Value = resource;
        command.Parameters.Add(parameter);
        var result = Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken),
            CultureInfo.InvariantCulture);
        return result >= 0;
    }

    private bool UsesSqlServer() =>
        _context.Database.ProviderName?.Contains(
            "SqlServer",
            StringComparison.OrdinalIgnoreCase) == true;

    private static MembershipAdministrationResult? ValidateExpectedRowVersion(
        string? encodedExpected,
        byte[] current,
        out byte[]? expected)
    {
        expected = null;
        if (string.IsNullOrWhiteSpace(encodedExpected))
        {
            return MembershipAdministrationResult.BadRequest(
                "ROW_VERSION_REQUIRED",
                "The current membership row version is required.");
        }

        try
        {
            expected = Convert.FromBase64String(encodedExpected);
        }
        catch (FormatException)
        {
            return MembershipAdministrationResult.BadRequest(
                "ROW_VERSION_INVALID",
                "The membership row version is invalid.");
        }

        if (expected.Length != 8)
        {
            return MembershipAdministrationResult.BadRequest(
                "ROW_VERSION_INVALID",
                "The membership row version is invalid.");
        }

        if (current.Length != expected.Length
            || !CryptographicOperations.FixedTimeEquals(current, expected))
        {
            return MembershipAdministrationResult.Conflict(
                "MEMBERSHIP_CHANGED",
                "The membership was changed by another administrator. Reload and try again.");
        }

        return null;
    }

    private static void InvalidateSessions(AppUser account, DateTime nowUtc)
    {
        account.SessionVersion = checked(account.SessionVersion + 1);
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        account.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        account.UpdatedAtUtc = nowUtc;
    }

    private static MembershipAdministrationResDTO MapResponse(
        AppUser account,
        P04_UserGroup membership,
        P02_Group group,
        LEX02_CompanyDepartmentLocation department,
        bool isActive) => new()
    {
        MembershipId = membership.Id,
        AccountId = account.Id,
        UserLogin = account.UserName ?? string.Empty,
        FullName = account.FullName,
        Email = account.Email,
        AccountStatus = account.AccountStatus.ToString(),
        SessionVersion = account.SessionVersion,
        GroupId = group.Id,
        GroupCode = group.GroupCode,
        GroupName = group.GroupName ?? string.Empty,
        PrimaryDepartmentId = department.Id,
        DepartmentCode = department.LEX02Code ?? string.Empty,
        DepartmentName = department.LEX02Name ?? string.Empty,
        RowVersion = Convert.ToBase64String(membership.RowVersion),
        IsActive = isActive,
        CreatedAt = membership.CreateDate,
        UpdatedAt = membership.UpdateDate
    };

    private async Task NotifyAfterCommitAsync(int accountId, IReadOnlyCollection<Guid> groupIds)
    {
        try
        {
            var notifications = groupIds
                .Distinct()
                .Select(groupId => _permissionChangeNotifier.NotifyGroupChangedAsync(
                    groupId,
                    CancellationToken.None))
                .Append(_permissionChangeNotifier.NotifyUserChangedAsync(
                    accountId,
                    CancellationToken.None));
            await Task.WhenAll(notifications);
        }
        catch (Exception exception)
        {
            // The database transaction is already committed. SignalR delivery
            // is best-effort; session-version validation still revokes tokens.
            _logger.LogError(
                exception,
                "Membership committed but live notification failed for AccountId={AccountId}.",
                accountId);
        }
    }

    private async Task RollbackAndClearAsync(IDbContextTransaction? transaction)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }

        _context.ChangeTracker.Clear();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            var number = current.GetType().GetProperty("Number")?.GetValue(current);
            if (number is 2601 or 2627)
            {
                return true;
            }

            var sqliteErrorCode = current.GetType().GetProperty("SqliteErrorCode")?.GetValue(current);
            if (sqliteErrorCode is 19)
            {
                return true;
            }

            if (current.Message.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
}
