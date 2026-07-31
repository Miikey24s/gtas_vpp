using gtas_vpp_be.Authorization;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Permission;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gtas_vpp_be.Tests.Authorization;

public sealed class MembershipAdministrationServiceTests
{
    private static readonly DateTime LocalNow = new(2026, 7, 15, 20, 30, 0);
    private static readonly byte[] RowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task Upsert_NewMembership_OwnsAuditAndInvalidatesSessionsBeforeNotification()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var account = AddActiveAccount(context, 1_000_000_101, "employee.one");
        var group = AddCanonicalGroup(context, CanonicalRbac.Employee);
        var department = AddDepartment(context, "IT");
        await context.SaveChangesAsync();
        var notifier = new PersistedStateNotifier(context);
        var service = CreateService(context, notifier);

        var result = await service.UpsertAsync(
            actorAccountId: 1_000_000_999,
            new MembershipUpsertReqDTO
            {
                AccountId = account.Id,
                GroupId = group.Id,
                PrimaryDepartmentId = department.Id,
                Reason = "Initial assignment"
            });

        Assert.True(result.Succeeded);
        var membership = Assert.Single(context.UserGroupMemberships.Where(item => !item.IsDeleted));
        Assert.Equal(account.Id, membership.AccountId);
        Assert.Equal(account.Id, membership.UserId);
        Assert.Equal(group.Id, membership.PermissionGroupId);
        Assert.Equal(department.Id, membership.DepartmentId);
        Assert.Equal(2, account.SessionVersion);
        Assert.NotEqual("security-before", account.SecurityStamp);
        Assert.NotEqual("concurrency-before", account.ConcurrencyStamp);
        var audit = Assert.Single(context.SecurityAudits);
        Assert.Equal("MEMBERSHIP_CREATED", audit.Action);
        Assert.Equal(account.Id, audit.TargetUserId);
        Assert.Equal("Initial assignment", audit.Reason);
        Assert.True(notifier.ObservedPersistedAudit);
        Assert.Contains(account.Id, notifier.UserNotifications);
        Assert.Contains(group.Id, notifier.GroupNotifications);
    }

    [Fact]
    public async Task ActivateAndUpsert_PendingAccountBecomesActiveWithCanonicalMembership()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = DateTime.UtcNow;
        var account = new AppUser
        {
            Id = 1_000_000_107,
            UserName = "pending.employee",
            NormalizedUserName = "PENDING.EMPLOYEE",
            Email = "pending.employee@example.test",
            NormalizedEmail = "PENDING.EMPLOYEE@EXAMPLE.TEST",
            FullName = "Pending Employee",
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            AccountStatus = AppAccountStatus.PendingApproval,
            SessionVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var group = AddCanonicalGroup(context, CanonicalRbac.Employee);
        var department = AddDepartment(context, "PENDING");
        context.Users.Add(account);
        await context.SaveChangesAsync();
        var notifier = new PersistedStateNotifier(context);
        var service = CreateService(context, notifier);

        var result = await service.ActivateAndUpsertAsync(
            actorAccountId: 1_000_000_999,
            new MembershipUpsertReqDTO
            {
                AccountId = account.Id,
                GroupId = group.Id,
                PrimaryDepartmentId = department.Id,
                Reason = "Initial approval"
            });

        Assert.True(result.Succeeded);
        Assert.Equal(AppAccountStatus.Active, account.AccountStatus);
        Assert.NotNull(account.ActivatedAtUtc);
        Assert.Equal(2, account.SessionVersion);
        var membership = Assert.Single(context.UserGroupMemberships.Where(item => !item.IsDeleted));
        Assert.Equal(account.Id, membership.AccountId);
        Assert.Contains(context.SecurityAudits, audit => audit.Action == "ACCOUNT_ACTIVATED");
        Assert.Contains(context.SecurityAudits, audit => audit.Action == "MEMBERSHIP_CREATED");
    }

    [Fact]
    public async Task Upsert_StaleRowVersion_ReturnsSafeConflictWithoutMutation()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var account = AddActiveAccount(context, 1_000_000_102, "employee.two");
        var employeeGroup = AddCanonicalGroup(context, CanonicalRbac.Employee);
        var approverGroup = AddCanonicalGroup(context, CanonicalRbac.DepartmentApprover);
        var department = AddDepartment(context, "QA");
        var membership = AddMembership(context, account, employeeGroup, department, RowVersion);
        await context.SaveChangesAsync();
        var notifier = new PersistedStateNotifier(context);
        var service = CreateService(context, notifier);

        var result = await service.UpsertAsync(
            actorAccountId: 1_000_000_999,
            new MembershipUpsertReqDTO
            {
                AccountId = account.Id,
                GroupId = approverGroup.Id,
                PrimaryDepartmentId = department.Id,
                ExpectedRowVersion = Convert.ToBase64String([8, 7, 6, 5, 4, 3, 2, 1])
            });

        Assert.Equal(409, result.StatusCode);
        Assert.Equal("MEMBERSHIP_CHANGED", result.Code);
        Assert.Equal(employeeGroup.Id, membership.PermissionGroupId);
        Assert.Equal(1, account.SessionVersion);
        Assert.Empty(context.SecurityAudits);
        Assert.Empty(notifier.UserNotifications);
    }

    [Fact]
    public async Task Upsert_FinalEffectiveSystemAdmin_IsBlockedUntilAnotherEffectiveAdminExists()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var target = AddActiveAccount(context, 1_000_000_103, "system.admin.one");
        var systemGroup = AddCanonicalGroup(context, CanonicalRbac.SystemAdmin);
        var employeeGroup = AddCanonicalGroup(context, CanonicalRbac.Employee);
        var department = AddDepartment(context, "SYS");
        var targetMembership = AddMembership(context, target, systemGroup, department, RowVersion);
        AddExplicitPermission(context, systemGroup, Permissions.PermissionManage);
        await context.SaveChangesAsync();
        var notifier = new PersistedStateNotifier(context);
        var service = CreateService(context, notifier);
        var command = new MembershipUpsertReqDTO
        {
            AccountId = target.Id,
            GroupId = employeeGroup.Id,
            PrimaryDepartmentId = department.Id,
            ExpectedRowVersion = Convert.ToBase64String(RowVersion),
            Reason = "Role rotation"
        };

        var blocked = await service.UpsertAsync(1_000_000_999, command);

        Assert.Equal(409, blocked.StatusCode);
        Assert.Equal("LAST_SYSTEM_ADMIN", blocked.Code);
        Assert.Equal(systemGroup.Id, targetMembership.PermissionGroupId);
        Assert.Equal(1, target.SessionVersion);

        var secondAdmin = AddActiveAccount(context, 1_000_000_104, "system.admin.two");
        AddMembership(context, secondAdmin, systemGroup, department, RowVersion);
        await context.SaveChangesAsync();

        var succeeded = await service.UpsertAsync(1_000_000_999, command);

        Assert.True(succeeded.Succeeded);
        Assert.Equal(employeeGroup.Id, targetMembership.PermissionGroupId);
        Assert.Equal(2, target.SessionVersion);
    }

    [Fact]
    public async Task Deactivate_SoftDeletesMembershipAndRevokesExistingSessions()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var account = AddActiveAccount(context, 1_000_000_105, "employee.three");
        var group = AddCanonicalGroup(context, CanonicalRbac.Employee);
        var department = AddDepartment(context, "HCQT");
        var membership = AddMembership(context, account, group, department, RowVersion);
        await context.SaveChangesAsync();
        var notifier = new PersistedStateNotifier(context);
        var service = CreateService(context, notifier);

        var result = await service.DeactivateAsync(
            actorAccountId: 1_000_000_999,
            new MembershipDeactivateReqDTO
            {
                AccountId = account.Id,
                ExpectedRowVersion = Convert.ToBase64String(RowVersion),
                Reason = "Department transfer"
            });

        Assert.True(result.Succeeded);
        Assert.False(result.Membership!.IsActive);
        Assert.True(membership.IsDeleted);
        Assert.Single(context.UserGroupMemberships);
        Assert.Equal(2, account.SessionVersion);
        var audit = Assert.Single(context.SecurityAudits);
        Assert.Equal("MEMBERSHIP_DEACTIVATED", audit.Action);
        Assert.Equal("Department transfer", audit.Reason);
        Assert.True(notifier.ObservedPersistedAudit);
    }

    [Fact]
    public async Task Upsert_AfterDeactivation_CreatesNewActiveMembership()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var account = AddActiveAccount(context, 1_000_000_108, "employee.reactivated");
        var group = AddCanonicalGroup(context, CanonicalRbac.Employee);
        var department = AddDepartment(context, "REACTIVATED");
        var oldMembership = AddMembership(context, account, group, department, RowVersion);
        await context.SaveChangesAsync();
        var service = CreateService(context, new PersistedStateNotifier(context));

        var deactivated = await service.DeactivateAsync(
            actorAccountId: 1_000_000_999,
            new MembershipDeactivateReqDTO
            {
                AccountId = account.Id,
                ExpectedRowVersion = Convert.ToBase64String(RowVersion),
                Reason = "Temporary access suspension"
            });

        var reactivated = await service.UpsertAsync(
            actorAccountId: 1_000_000_999,
            new MembershipUpsertReqDTO
            {
                AccountId = account.Id,
                GroupId = group.Id,
                PrimaryDepartmentId = department.Id,
                Reason = "Access restored"
            });

        Assert.True(deactivated.Succeeded);
        Assert.True(reactivated.Succeeded);
        Assert.True(oldMembership.IsDeleted);
        var activeMembership = Assert.Single(context.UserGroupMemberships.Where(item => !item.IsDeleted));
        Assert.NotEqual(oldMembership.Id, activeMembership.Id);
        Assert.Equal(account.Id, activeMembership.AccountId);
        Assert.Equal(group.Id, activeMembership.PermissionGroupId);
        Assert.Equal(department.Id, activeMembership.DepartmentId);
        Assert.Equal(3, account.SessionVersion);
        Assert.Contains(context.SecurityAudits, audit => audit.Action == "MEMBERSHIP_DEACTIVATED");
        Assert.Contains(context.SecurityAudits, audit => audit.Action == "MEMBERSHIP_CREATED");
    }

    [Fact]
    public async Task Upsert_SelfMembershipChange_IsRejectedBeforeReadingTargetState()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new PersistedStateNotifier(context));

        var result = await service.UpsertAsync(
            actorAccountId: 1_000_000_106,
            new MembershipUpsertReqDTO
            {
                AccountId = 1_000_000_106,
                GroupId = CanonicalRbac.Employee.GroupId,
                PrimaryDepartmentId = Guid.NewGuid()
            });

        Assert.Equal(409, result.StatusCode);
        Assert.Equal("SELF_MEMBERSHIP_CHANGE", result.Code);
    }

    private static MembershipAdministrationService CreateService(
        VPPContext context,
        IPermissionChangeNotifier notifier) =>
        new(
            context,
            notifier,
            new FakeDateTimeProvider(LocalNow),
            NullLogger<MembershipAdministrationService>.Instance);

    private static AppUser AddActiveAccount(VPPContext context, int id, string userName)
    {
        var account = new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@example.test",
            NormalizedEmail = $"{userName}@example.test".ToUpperInvariant(),
            FullName = userName,
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            AccountStatus = AppAccountStatus.Active,
            SessionVersion = 1,
            SecurityStamp = "security-before",
            ConcurrencyStamp = "concurrency-before",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Users.Add(account);
        return account;
    }

    private static PermissionGroup AddCanonicalGroup(VPPContext context, RbacPersonaDefinition persona)
    {
        var group = new PermissionGroup
        {
            Id = persona.GroupId,
            GroupCode = persona.GroupCode,
            GroupName = persona.GroupName,
            ParentGroupId = null,
            CreatedAtUtc = LocalNow,
            UpdatedAtUtc = LocalNow,
            IsDeleted = false
        };
        context.PermissionGroups.Add(group);
        return group;
    }

    private static Department AddDepartment(VPPContext context, string code)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code,
            CreatedAtUtc = LocalNow,
            UpdatedAtUtc = LocalNow,
            IsDeleted = false
        };
        context.Departments.Add(department);
        return department;
    }

    private static UserGroupMembership AddMembership(
        VPPContext context,
        AppUser account,
        PermissionGroup group,
        Department department,
        byte[] rowVersion)
    {
        var membership = new UserGroupMembership
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            UserId = account.Id,
            PermissionGroupId = group.Id,
            DepartmentId = department.Id,
            RowVersion = rowVersion.ToArray(),
            CreatedAtUtc = LocalNow,
            UpdatedAtUtc = LocalNow,
            IsDeleted = false
        };
        context.UserGroupMemberships.Add(membership);
        return membership;
    }

    private static void AddExplicitPermission(
        VPPContext context,
        PermissionGroup group,
        string permissionCode)
    {
        var page = new PermissionPage
        {
            Id = Guid.NewGuid(),
            PageCode = "PERMISSION",
            PageName = "Permission",
            Type = "PAGE",
            CreatedAtUtc = LocalNow,
            UpdatedAtUtc = LocalNow
        };
        var component = new PermissionComponent
        {
            Id = Guid.NewGuid(),
            ComponentCode = permissionCode,
            ComponentName = permissionCode,
            CreatedAtUtc = LocalNow,
            UpdatedAtUtc = LocalNow
        };
        var pageMapping = new PageComponentMapping
        {
            Id = Guid.NewGuid(),
            PermissionPageId = page.Id,
            PermissionPage = page,
            PermissionComponentId = component.Id,
            PermissionComponent = component
        };
        var groupMapping = new GroupPageComponentMapping
        {
            PermissionGroupId = group.Id,
            PermissionGroup = group,
            PageComponentMappingId = pageMapping.Id,
            PageComponentMapping = pageMapping,
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            IsVisible = true,
            IsEnable = true,
            CreatedAtUtc = LocalNow,
            UpdatedAtUtc = LocalNow
        };
        context.AddRange(page, component, pageMapping, groupMapping);
    }

    private sealed class PersistedStateNotifier(VPPContext context) : IPermissionChangeNotifier
    {
        private readonly VPPContext _context = context;
        public List<Guid> GroupNotifications { get; } = [];
        public List<int> UserNotifications { get; } = [];
        public bool ObservedPersistedAudit { get; private set; }

        public Task NotifyGroupChangedAsync(
            Guid groupId,
            CancellationToken cancellationToken = default)
        {
            ObservedPersistedAudit |= _context.SecurityAudits.Any();
            GroupNotifications.Add(groupId);
            return Task.CompletedTask;
        }

        public Task NotifyUserChangedAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            ObservedPersistedAudit |= _context.SecurityAudits.Any();
            UserNotifications.Add(userId);
            return Task.CompletedTask;
        }
    }
}
