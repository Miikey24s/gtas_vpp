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
        var tombstoneOnlyAccount = AddAccount(context, 1_000_000_202, "legacy.tombstone");
        var systemGroup = new P02_Group
        {
            Id = CanonicalRbac.SystemAdmin.GroupId,
            GroupCode = CanonicalRbac.SystemAdmin.GroupCode,
            GroupName = "A display name that is not Admin",
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        };
        var department = new gtas_vpp_be.Model.Library.LEX02_CompanyDepartmentLocation
        {
            Id = Guid.NewGuid(),
            LEX02Code = "IT",
            LEX02Name = "IT",
            LEX02Type = "PhongBan",
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        };
        context.AddRange(systemGroup, department);
        context.P04_UserGroups.AddRange(
            new P04_UserGroup
            {
                Id = Guid.NewGuid(),
                AccountId = activeAccount.Id,
                UserId = activeAccount.Id,
                P02_GroupId = systemGroup.Id,
                LEX02_CompanyDepartmentLocationId = department.Id,
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow,
                IsDeleted = false
            },
            new P04_UserGroup
            {
                Id = Guid.NewGuid(),
                AccountId = tombstoneOnlyAccount.Id,
                UserId = tombstoneOnlyAccount.Id,
                P02_GroupId = systemGroup.Id,
                LEX02_CompanyDepartmentLocationId = department.Id,
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow,
                IsDeleted = true
            });
        await context.SaveChangesAsync();

        var resolver = new Mock<IUserNameResolver>();
        resolver
            .Setup(service => service.WithUserNamesAsync(
                It.IsAny<List<sp_Authentication_TabUser_UserList>>(),
                It.IsAny<DbContext>()))
            .ReturnsAsync((List<sp_Authentication_TabUser_UserList> users, DbContext _) => users);
        var controller = new PermissionController(
            Mock.Of<IGenericRepository<P02_Group>>(),
            Mock.Of<IGenericRepository<P06_GroupPageComponentMapping>>(),
            Mock.Of<IGenericRepository<P04_UserGroup>>(),
            resolver.Object,
            ServiceTestHelpers.CreateUnitOfWorkMock(context).Object,
            new FakeDateTimeProvider(DateTime.UtcNow),
            Mock.Of<IPermissionChangeNotifier>(),
            Mock.Of<IMembershipAdministrationService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var action = await controller.GetUsers(top: 20);

        var response = Assert.IsType<OkObjectResult>(action);
        var users = Assert.IsType<List<sp_Authentication_TabUser_UserList>>(response.Value);
        var active = Assert.Single(users, item => item.UserId == activeAccount.Id);
        Assert.True(active.IsAdmin);
        Assert.Equal(systemGroup.Id, active.GroupId);
        Assert.False(active.IsDeleted);
        var tombstone = Assert.Single(users, item => item.UserId == tombstoneOnlyAccount.Id);
        Assert.False(tombstone.IsAdmin);
        Assert.Equal(Guid.Empty, tombstone.GroupId);
        Assert.True(tombstone.IsDeleted);
    }

    private static AppUser AddAccount(VPPContext context, int id, string userName)
    {
        var account = new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            FullName = userName,
            AccountStatus = AppAccountStatus.Active,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Users.Add(account);
        return account;
    }
}
