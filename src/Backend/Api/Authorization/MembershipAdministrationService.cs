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

    Task<MembershipAdministrationResult> ActivateAndUpsertAsync(
        int actorAccountId,
        MembershipUpsertReqDTO command,
        CancellationToken cancellationToken = default);

    Task<MembershipAdministrationResult> DeactivateAsync(
        int actorAccountId,
        MembershipDeactivateReqDTO command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Boundary mutation duy nhất cho membership giữa tài khoản và persona. Service này
/// sở hữu toàn bộ trường audit, trạng thái soft-delete và việc vô hiệu hóa session.
/// </summary>
public sealed class MembershipAdministrationService(
    VPPContext context,
    IPermissionChangeNotifier permissionChangeNotifier,
    IDateTimeProvider dateTimeProvider,
    ILogger<MembershipAdministrationService> logger) : IMembershipAdministrationService
{
    private const string LastSystemAdminLock = "GTAS_VPP:AUTH:LAST_SYSTEM_ADMIN";
    private readonly VPPContext _context = context;
    private readonly IPermissionChangeNotifier _permissionChangeNotifier = permissionChangeNotifier;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly ILogger<MembershipAdministrationService> _logger = logger;

    public Task<MembershipAdministrationResult> UpsertAsync(
        int actorAccountId,
        MembershipUpsertReqDTO command,
        CancellationToken cancellationToken = default) =>
        UpsertInternalAsync(
            actorAccountId,
            command,
            allowPendingActivation: false,
            cancellationToken: cancellationToken);

    public Task<MembershipAdministrationResult> ActivateAndUpsertAsync(
        int actorAccountId,
        MembershipUpsertReqDTO command,
        CancellationToken cancellationToken = default) =>
        UpsertInternalAsync(
            actorAccountId,
            command,
            allowPendingActivation: true,
            cancellationToken: cancellationToken);

    private async Task<MembershipAdministrationResult> UpsertInternalAsync(
        int actorAccountId,
        MembershipUpsertReqDTO command,
        bool allowPendingActivation,
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

            var accountResult = await GetAccountAsync(
                command.AccountId,
                allowPendingActivation,
                cancellationToken);
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

            var oldGroupId = current?.PermissionGroupId;
            if (current is not null
                && current.PermissionGroupId != command.GroupId
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
            var wasPending = account.AccountStatus == AppAccountStatus.PendingApproval;
            if (wasPending)
            {
                account.AccountStatus = AppAccountStatus.Active;
                account.ActivatedAtUtc = nowUtc;
                account.DisabledAtUtc = null;
                _context.SecurityAudits.Add(new SecurityAudit
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = actorAccountId,
                    TargetUserId = account.Id,
                    Action = "ACCOUNT_ACTIVATED",
                    ResourceType = "AppUser",
                    ResourceId = account.Id.ToString(),
                    Outcome = "Succeeded",
                    Summary = "PendingApproval account activated together with its first canonical membership.",
                    Reason = reason,
                    CorrelationId = Activity.Current?.TraceId.ToString(),
                    OccurredAtUtc = nowUtc
                });
            }
            var membership = current ?? new UserGroupMembership
            {
                Id = Guid.NewGuid(),
                UserId = account.Id,
                AccountId = account.Id,
                PermissionGroupId = group.Id,
                DepartmentId = department.Id,
                CreatedByUserId = actorAccountId,
                CreatedAtUtc = now,
                UpdatedByUserId = actorAccountId,
                UpdatedAtUtc = now,
                IsDeleted = false
            };

            if (current is null)
            {
                _context.UserGroupMemberships.Add(membership);
            }
            else
            {
                _context.Entry(current).Property(x => x.RowVersion).OriginalValue = expectedRowVersion!;
                membership.PermissionGroupId = group.Id;
                membership.DepartmentId = department.Id;
                membership.UpdatedByUserId = actorAccountId;
                membership.UpdatedAtUtc = now;
            }

            InvalidateSessions(account, nowUtc);
            _context.SecurityAudits.Add(new SecurityAudit
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorAccountId,
                TargetUserId = account.Id,
                Action = current is null ? "MEMBERSHIP_CREATED" : "MEMBERSHIP_UPDATED",
                ResourceType = "UserGroupMembership",
                ResourceId = membership.Id.ToString(),
                Outcome = "Succeeded",
                Summary = current is null
                    ? $"Assigned {group.GroupCode} with primary department {department.Code}."
                    : $"Changed membership from {oldGroupId} to {group.GroupCode} with primary department {department.Code}.",
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

            var accountResult = await GetAccountAsync(
                command.AccountId,
                allowPendingActivation: false,
                cancellationToken);
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

            var groupResult = await GetCanonicalGroupAsync(membership.PermissionGroupId, cancellationToken);
            if (groupResult.Group is null)
            {
                return MembershipAdministrationResult.Conflict(
                    "INVALID_ACTIVE_GROUP",
                    "The active membership has an invalid group and requires data repair.");
            }

            var departmentResult = await GetActiveDepartmentAsync(
                membership.DepartmentId,
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
            membership.UpdatedByUserId = actorAccountId;
            membership.UpdatedAtUtc = now;
            InvalidateSessions(account, nowUtc);

            _context.SecurityAudits.Add(new SecurityAudit
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorAccountId,
                TargetUserId = account.Id,
                Action = "MEMBERSHIP_DEACTIVATED",
                ResourceType = "UserGroupMembership",
                ResourceId = membership.Id.ToString(),
                Outcome = "Succeeded",
                Summary = $"Deactivated {group.GroupCode} membership from primary department {department.Code}.",
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

    private async Task<(AppUser? Account, MembershipAdministrationResult? Failure)> GetAccountAsync(
        int accountId,
        bool allowPendingActivation,
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

        var statusAllowed = account.AccountStatus == AppAccountStatus.Active
            || (allowPendingActivation && account.AccountStatus == AppAccountStatus.PendingApproval);
        if (!statusAllowed
            || (account.LockoutEnd.HasValue && account.LockoutEnd > DateTimeOffset.UtcNow))
        {
            return (null, MembershipAdministrationResult.Conflict(
                "ACCOUNT_NOT_ACTIVE",
                allowPendingActivation
                    ? "Only an active or pending, unlocked application account can receive a membership change."
                    : "Only an active, unlocked application account can receive a membership change."));
        }

        return (account, null);
    }

    private async Task<(PermissionGroup? Group, MembershipAdministrationResult? Failure)> GetCanonicalGroupAsync(
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

        var group = await _context.PermissionGroups
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

    private async Task<(Department? Department, MembershipAdministrationResult? Failure)>
        GetActiveDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        if (departmentId == Guid.Empty)
        {
            return (null, MembershipAdministrationResult.BadRequest(
                "PRIMARY_DEPARTMENT_REQUIRED",
                "A primary department is required."));
        }

        var department = await _context.Departments
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == departmentId, cancellationToken);
        if (department is null)
        {
            return (null, MembershipAdministrationResult.NotFound(
                "PRIMARY_DEPARTMENT_NOT_FOUND",
                "The selected primary department was not found."));
        }

        if (department.IsDeleted
            || string.IsNullOrWhiteSpace(department.Code)
            || string.IsNullOrWhiteSpace(department.Name))
        {
            return (null, MembershipAdministrationResult.Conflict(
                "PRIMARY_DEPARTMENT_NOT_ACTIVE",
                "The selected primary department is not active."));
        }

        return (department, null);
    }

    private Task<List<UserGroupMembership>> GetCurrentMembershipsAsync(
        int accountId,
        CancellationToken cancellationToken) =>
        _context.UserGroupMemberships
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
                join membership in _context.UserGroupMemberships.AsNoTracking()
                    on account.Id equals membership.AccountId
                join roleGroup in _context.PermissionGroups.AsNoTracking()
                    on membership.PermissionGroupId equals roleGroup.Id
                join department in _context.Departments.AsNoTracking()
                    on membership.DepartmentId equals department.Id
                where account.AccountStatus == AppAccountStatus.Active
                      && (account.LockoutEnd == null || account.LockoutEnd <= DateTimeOffset.UtcNow)
                      && membership.UserId == account.Id
                      && !membership.IsDeleted
                      && !roleGroup.IsDeleted
                      && roleGroup.ParentGroupId == null
                      && roleGroup.GroupCode == CanonicalRbac.SystemAdmin.GroupCode
                      && !department.IsDeleted
                      && department.Id != Guid.Empty
                      && _context.GroupPageComponentMappings.Any(mapping =>
                          mapping.PermissionGroupId == roleGroup.Id
                          && mapping.MemberCompanyCode == account.MemberCompanyCode
                          && mapping.IsVisible
                          && mapping.IsEnable
                          && mapping.PageComponentMapping != null
                          && mapping.PageComponentMapping.PermissionPage != null
                          && !mapping.PageComponentMapping.PermissionPage.IsDeleted
                          && mapping.PageComponentMapping.PermissionComponent != null
                          && !mapping.PageComponentMapping.PermissionComponent.IsDeleted
                          && mapping.PageComponentMapping.PermissionComponent.ComponentCode
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

        // Luôn acquire theo cùng thứ tự để tránh deadlock giữa hai mutation
        // system administrator chạy đồng thời.
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
        UserGroupMembership membership,
        PermissionGroup group,
        Department department,
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
        Code = department.Code ?? string.Empty,
        Name = department.Name ?? string.Empty,
        RowVersion = Convert.ToBase64String(membership.RowVersion),
        IsActive = isActive,
        CreatedAt = membership.CreatedAtUtc,
        UpdatedAt = membership.UpdatedAtUtc
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
            // Database transaction đã commit. SignalR chỉ gửi theo best-effort;
            // kiểm tra session version vẫn bảo đảm thu hồi token.
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
