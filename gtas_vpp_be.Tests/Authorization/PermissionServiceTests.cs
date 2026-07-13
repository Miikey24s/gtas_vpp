using System.Security.Claims;
using gtas_vpp_be.Authorization;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.Constants;
using Xunit;

namespace gtas_vpp_be.Tests.Authorization;

public sealed class PermissionServiceTests
{
    [Fact]
    public void Compatibility_ExpandsLegacyRequestPermissionToActions()
    {
        var permissions = PermissionCompatibility.Expand([Permissions.RequestOrder]);

        Assert.Contains(Permissions.RequestCreate, permissions);
        Assert.Contains(Permissions.RequestUpdateOwn, permissions);
        Assert.Contains(Permissions.RequestCancelOwn, permissions);
        Assert.Contains(Permissions.RequestViewOwn, permissions);
    }

    [Fact]
    public async Task RevokedPermission_IsDeniedOnNextCheck()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedPermissionAsync(context, userId: 10, companyCode: 77500);
        var service = new PermissionService(context);
        var user = CreateUser(10, 77500);

        Assert.True(await service.HasPermissionAsync(user, Permissions.RequestCreate));

        seed.GroupMapping.IsEnable = false;
        seed.GroupMapping.UpdateDate = seed.GroupMapping.UpdateDate.AddSeconds(1);
        await context.SaveChangesAsync();

        Assert.False(await service.HasPermissionAsync(user, Permissions.RequestCreate));
    }

    [Fact]
    public async Task CompanyClaim_FiltersMappingsFromOtherCompanies()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        await SeedPermissionAsync(context, userId: 11, companyCode: 77500);
        var service = new PermissionService(context);

        var snapshot = await service.GetSnapshotAsync(CreateUser(11, 88000));

        Assert.Empty(snapshot.Permissions);
    }

    [Fact]
    public async Task MultipleActiveGroups_FailClosed()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        await SeedPermissionAsync(context, userId: 12, companyCode: 77500);
        var secondGroupId = Guid.NewGuid();
        context.Set<P02_Group>().Add(new P02_Group
        {
            Id = secondGroupId,
            GroupName = "Second group",
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        });
        context.Set<P04_UserGroup>().Add(new P04_UserGroup
        {
            Id = Guid.NewGuid(),
            UserId = 12,
            P02_GroupId = secondGroupId,
            LEX02_CompanyDepartmentLocationId = Guid.NewGuid(),
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var service = new PermissionService(context);

        var snapshot = await service.GetSnapshotAsync(CreateUser(12, 77500));

        Assert.Empty(snapshot.Permissions);
        Assert.Equal(Guid.Empty, snapshot.GroupId);
    }

    private static ClaimsPrincipal CreateUser(int userId, long companyCode) =>
        new(new ClaimsIdentity(
        [
            new Claim("UserID", userId.ToString()),
            new Claim("MemberCompanyCode", companyCode.ToString())
        ], "Test"));

    private static async Task<PermissionSeed> SeedPermissionAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        int userId,
        long companyCode)
    {
        var now = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);
        var groupId = Guid.NewGuid();
        var page = new P01_Page
        {
            Id = Guid.NewGuid(),
            PageCode = "DASHBOARD",
            PageName = "Dashboard",
            Type = "PAGE",
            CreateDate = now,
            UpdateDate = now
        };
        var component = new P03_Component
        {
            Id = Guid.NewGuid(),
            ComponentCode = Permissions.RequestOrder,
            ComponentName = "Request order",
            CreateDate = now,
            UpdateDate = now
        };
        var pageMapping = new P05_PageComponentMapping
        {
            Id = Guid.NewGuid(),
            P01_PageId = page.Id,
            P01_Page = page,
            P03_ComponentId = component.Id,
            P03_Component = component
        };
        var groupMapping = new P06_GroupPageComponentMapping
        {
            P02_GroupId = groupId,
            P05_PageComponentMappingId = pageMapping.Id,
            P05_PageComponentMapping = pageMapping,
            MemberCompanyCode = companyCode,
            IsVisible = true,
            IsEnable = true,
            CreateDate = now,
            UpdateDate = now
        };

        context.AddRange(
            new P02_Group
            {
                Id = groupId,
                GroupName = "Test",
                CreateDate = now,
                UpdateDate = now
            },
            new P04_UserGroup
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                P02_GroupId = groupId,
                LEX02_CompanyDepartmentLocationId = Guid.NewGuid(),
                CreateDate = now,
                UpdateDate = now
            },
            page,
            component,
            pageMapping,
            groupMapping);
        await context.SaveChangesAsync();

        return new PermissionSeed(groupMapping);
    }

    private sealed record PermissionSeed(P06_GroupPageComponentMapping GroupMapping);
}
