using gtas_vpp_be.Authorization;
using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.Authorization;

public sealed class MembershipPermissionControllerTests
{
    [Fact]
    public async Task GetUsers_UsesAppAccountsActiveMembershipAndCanonicalGroupCode()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var activeAccount = AddAccount(context, 1_000_000_201, "active.admin");
        var lastLoginAtUtc = new DateTime(2026, 8, 14, 8, 30, 0, DateTimeKind.Utc);
        activeAccount.LastLoginAtUtc = lastLoginAtUtc;
        var tombstoneOnlyAccount = AddAccount(context, 1_000_000_202, "legacy.tombstone");
        var systemGroup = new PermissionGroup
        {
            Id = CanonicalRbac.SystemAdmin.GroupId,
            GroupCode = CanonicalRbac.SystemAdmin.GroupCode,
            GroupName = "A display name that is not Admin",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var department = new gtas_vpp_be.Model.Library.Department
        {
            Id = Guid.NewGuid(),
            Code = "IT",
            Name = "IT",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.AddRange(systemGroup, department);
        context.UserGroupMemberships.AddRange(
            new UserGroupMembership
            {
                Id = Guid.NewGuid(),
                AccountId = activeAccount.Id,
                UserId = activeAccount.Id,
                PermissionGroupId = systemGroup.Id,
                DepartmentId = department.Id,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            },
            new UserGroupMembership
            {
                Id = Guid.NewGuid(),
                AccountId = tombstoneOnlyAccount.Id,
                UserId = tombstoneOnlyAccount.Id,
                PermissionGroupId = systemGroup.Id,
                DepartmentId = department.Id,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                IsDeleted = true
            });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var action = await controller.GetUsers(top: 20);

        var response = Assert.IsType<OkObjectResult>(action);
        var users = Assert.IsType<List<UserAdministrationResDTO>>(response.Value);
        var active = Assert.Single(users, item => item.UserId == activeAccount.Id);
        Assert.True(active.IsAdmin);
        Assert.Equal(systemGroup.Id, active.GroupId);
        Assert.False(active.IsDeleted);
        Assert.Equal(lastLoginAtUtc, active.LastLoginAtUtc);
        var tombstone = Assert.Single(users, item => item.UserId == tombstoneOnlyAccount.Id);
        Assert.False(tombstone.IsAdmin);
        Assert.Equal(Guid.Empty, tombstone.GroupId);
        Assert.True(tombstone.IsDeleted);
    }

    [Fact]
    public async Task GetUsers_AppliesTypedAccountAndMembershipFilters()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var member = AddAccount(context, 1_000_000_211, "member.user");
        var noMembership = AddAccount(context, 1_000_000_212, "unassigned.user");
        var pending = AddAccount(
            context,
            1_000_000_213,
            "pending.user",
            AppAccountStatus.PendingApproval);
        var group = new PermissionGroup
        {
            Id = CanonicalRbac.Employee.GroupId,
            GroupCode = CanonicalRbac.Employee.GroupCode,
            GroupName = CanonicalRbac.Employee.GroupName,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var department = new gtas_vpp_be.Model.Library.Department
        {
            Id = Guid.NewGuid(),
            Code = "OPS",
            Name = "Operations",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.AddRange(group, department);
        context.UserGroupMemberships.Add(new UserGroupMembership
        {
            Id = Guid.NewGuid(),
            AccountId = member.Id,
            UserId = member.Id,
            PermissionGroupId = group.Id,
            DepartmentId = department.Id,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var assignedAction = await controller.GetUsers(
            accountStatus: nameof(AppAccountStatus.Active),
            groupId: group.Id,
            departmentId: department.Id,
            hasActiveMembership: true,
            top: 20);
        var assigned = Assert.IsType<List<UserAdministrationResDTO>>(
            Assert.IsType<OkObjectResult>(assignedAction).Value);
        Assert.Equal(member.Id, Assert.Single(assigned).UserId);

        var unassignedAction = await controller.GetUsers(
            accountStatus: nameof(AppAccountStatus.Active),
            hasActiveMembership: false,
            top: 20);
        var unassigned = Assert.IsType<List<UserAdministrationResDTO>>(
            Assert.IsType<OkObjectResult>(unassignedAction).Value);
        Assert.Equal(noMembership.Id, Assert.Single(unassigned).UserId);

        var pendingAction = await controller.GetUsers(
            accountStatus: nameof(AppAccountStatus.PendingApproval),
            top: 20);
        var pendingUsers = Assert.IsType<List<UserAdministrationResDTO>>(
            Assert.IsType<OkObjectResult>(pendingAction).Value);
        Assert.Equal(pending.Id, Assert.Single(pendingUsers).UserId);
    }

    [Fact]
    public async Task GetUsers_RejectsUnknownAccountStatus()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var controller = CreateController(context);

        var action = await controller.GetUsers(accountStatus: "Archived");

        Assert.IsType<BadRequestObjectResult>(action);
    }

    private static PermissionController CreateController(VPPContext context)
    {
        var resolver = new Mock<IUserNameResolver>();
        resolver
            .Setup(service => service.WithUserNamesAsync(
                It.IsAny<List<UserAdministrationResDTO>>(),
                It.IsAny<DbContext>()))
            .ReturnsAsync((List<UserAdministrationResDTO> users, DbContext _) => users);

        return new PermissionController(
            Mock.Of<IGenericRepository<PermissionGroup>>(),
            Mock.Of<IGenericRepository<GroupPageComponentMapping>>(),
            Mock.Of<IGenericRepository<UserGroupMembership>>(),
            resolver.Object,
            ServiceTestHelpers.CreateUnitOfWorkMock(context).Object,
            new FakeDateTimeProvider(DateTime.UtcNow),
            Mock.Of<IPermissionChangeNotifier>(),
            Mock.Of<IMembershipAdministrationService>(),
            new SecurityAuditQueryService(context),
            new UserAdministrationQueryService(context, resolver.Object),
            new PermissionGroupQueryService(context, resolver.Object))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static AppUser AddAccount(
        VPPContext context,
        int id,
        string userName,
        AppAccountStatus status = AppAccountStatus.Active)
    {
        var account = new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            FullName = userName,
            AccountStatus = status,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Users.Add(account);
        return account;
    }
}
