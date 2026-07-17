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
    public async Task UiComponent_DoesNotGrantBackendAction()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        await SeedPermissionAsync(
            context,
            userId: 9,
            companyCode: 77500,
            componentCode: Permissions.RequestOrder);
        var service = new PermissionService(context);
        var user = CreateUser(9, 77500);

        var snapshot = await service.GetSnapshotAsync(user);

        Assert.Empty(snapshot.Permissions);
        Assert.False(await service.HasPermissionAsync(user, Permissions.RequestCreate));
    }

    [Fact]
    public async Task ExplicitAction_GrantsOnlyThatAction()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        await SeedPermissionAsync(context, userId: 8, companyCode: 77500);
        var service = new PermissionService(context);
        var user = CreateUser(8, 77500);

        Assert.True(await service.HasPermissionAsync(user, Permissions.RequestCreate));
        Assert.False(await service.HasPermissionAsync(user, Permissions.RequestUpdateOwn));
    }

    [Fact]
    public async Task StoredActionOutsideCanonicalRoleCeiling_IsDenied()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        await SeedPermissionAsync(
            context,
            userId: 13,
            companyCode: 77500,
            componentCode: Permissions.PermissionManage,
            persona: CanonicalRbac.Employee);
        var service = new PermissionService(context);

        var snapshot = await service.GetSnapshotAsync(CreateUser(13, 77500));

        Assert.Empty(snapshot.Permissions);
        Assert.False(await service.HasPermissionAsync(CreateUser(13, 77500), Permissions.PermissionManage));
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
        seed.GroupMapping.UpdatedAtUtc = seed.GroupMapping.UpdatedAtUtc.AddSeconds(1);
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
        var secondGroupId = CanonicalRbac.DepartmentApprover.GroupId;
        context.Set<PermissionGroup>().Add(new PermissionGroup
        {
            Id = secondGroupId,
            GroupCode = CanonicalRbac.DepartmentApprover.GroupCode,
            GroupName = CanonicalRbac.DepartmentApprover.GroupName,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        context.Set<UserGroupMembership>().Add(new UserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = 12,
            AccountId = 12,
            PermissionGroupId = secondGroupId,
            DepartmentId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
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
        long companyCode,
        string componentCode = Permissions.RequestCreate,
        RbacPersonaDefinition? persona = null)
    {
        persona ??= CanonicalRbac.Employee;
        var now = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);
        var groupId = persona.GroupId;
        var page = new PermissionPage
        {
            Id = Guid.NewGuid(),
            PageCode = "DASHBOARD",
            PageName = "Dashboard",
            Type = "PAGE",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var component = new PermissionComponent
        {
            Id = Guid.NewGuid(),
            ComponentCode = componentCode,
            ComponentName = componentCode,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
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
            PermissionGroupId = groupId,
            PageComponentMappingId = pageMapping.Id,
            PageComponentMapping = pageMapping,
            MemberCompanyCode = companyCode,
            IsVisible = true,
            IsEnable = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.AddRange(
            new PermissionGroup
            {
                Id = groupId,
                GroupCode = persona.GroupCode,
                GroupName = persona.GroupName,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new UserGroupMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = userId,
                PermissionGroupId = groupId,
                DepartmentId = Guid.NewGuid(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            page,
            component,
            pageMapping,
            groupMapping);
        await context.SaveChangesAsync();

        return new PermissionSeed(groupMapping);
    }

    private sealed record PermissionSeed(GroupPageComponentMapping GroupMapping);
}
