using System.Reflection;
using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.Authorization;

public sealed class SeedDataRbacTests
{
    [Fact]
    public async Task ReferenceRbacSeed_IsIdempotentAndCreatesExactMatrix()
    {
        await using var context = CreateContext();

        await SeedRbacAsync(context);
        var firstCounts = await ReadCountsAsync(context);
        await SeedRbacAsync(context);
        var secondCounts = await ReadCountsAsync(context);
        var personaIds = CanonicalRbac.Personas.Select(x => x.GroupId).ToArray();

        Assert.Equal(firstCounts, secondCounts);
        Assert.Equal(CanonicalRbac.Personas.Count, await context.PermissionGroups.CountAsync(x => personaIds.Contains(x.Id)));
        foreach (var persona in CanonicalRbac.Personas)
        {
            var group = await context.PermissionGroups.SingleAsync(x => x.Id == persona.GroupId);
            Assert.Equal(persona.GroupCode, group.GroupCode);
            Assert.Equal(persona.GroupName, group.GroupName);
            Assert.Null(group.ParentGroupId);
            Assert.False(group.IsDeleted);
            await AssertActiveComponentsAsync(context, persona.GroupId);
        }
    }

    [Fact]
    public async Task ReferenceRbacSeed_RepairsDriftAndRevokesExcessGrant()
    {
        await using var context = CreateContext();
        await SeedRbacAsync(context);

        var employee = await context.PermissionGroups.SingleAsync(x => x.Id == CanonicalRbac.Employee.GroupId);
        employee.GroupCode = "DRIFTED";
        employee.ParentGroupId = CanonicalRbac.SystemAdmin.GroupId;
        employee.IsDeleted = true;

        var requestCreate = await context.PermissionComponents
            .SingleAsync(x => x.ComponentCode == Permissions.RequestCreate);
        requestCreate.ComponentName = "Drifted action";
        requestCreate.IsDeleted = true;

        var requestCreateMappingId = await MappingIdAsync(context, Permissions.RequestCreate);
        var employeeRequestCreate = await context.GroupPageComponentMappings.SingleAsync(x =>
            x.PermissionGroupId == CanonicalRbac.Employee.GroupId
            && x.PageComponentMappingId == requestCreateMappingId
            && x.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode);
        employeeRequestCreate.IsEnable = false;
        employeeRequestCreate.IsVisible = false;

        var permissionUserMappingId = await MappingIdAsync(context, Permissions.PermissionUser);
        context.GroupPageComponentMappings.Add(new GroupPageComponentMapping
        {
            PermissionGroupId = CanonicalRbac.Employee.GroupId,
            PageComponentMappingId = permissionUserMappingId,
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            IsEnable = true,
            IsVisible = true,
            CreatedByUserId = 5615,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedByUserId = 5615,
            UpdatedAtUtc = DateTime.UtcNow
        });
        var libraryMenuMappingId = await MappingIdAsync(context, Permissions.MenuLibrary);
        context.GroupPageComponentMappings.Add(new GroupPageComponentMapping
        {
            PermissionGroupId = CanonicalRbac.Employee.GroupId,
            PageComponentMappingId = libraryMenuMappingId,
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            IsEnable = true,
            IsVisible = true,
            CreatedByUserId = 5615,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedByUserId = 5615,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await InvokeSeedAsync(context, "SeedPermissionGroup");
        await InvokeSeedAsync(context, "SeedPermissionComponent");
        await InvokeSeedAsync(context, "SeedPageComponentMapping");
        await InvokeSeedAsync(context, "SeedGroupPageComponentMapping");

        Assert.Equal(CanonicalRbac.Employee.GroupCode, employee.GroupCode);
        Assert.Null(employee.ParentGroupId);
        Assert.False(employee.IsDeleted);
        Assert.Equal(
            CanonicalRbac.Actions.Single(x => x.PermissionCode == Permissions.RequestCreate).Name,
            requestCreate.ComponentName);
        Assert.False(requestCreate.IsDeleted);
        Assert.True(employeeRequestCreate.IsEnable);
        Assert.True(employeeRequestCreate.IsVisible);

        var excess = await context.GroupPageComponentMappings.SingleAsync(x =>
            x.PermissionGroupId == CanonicalRbac.Employee.GroupId
            && x.PageComponentMappingId == permissionUserMappingId
            && x.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode);
        Assert.False(excess.IsEnable);
        Assert.False(excess.IsVisible);
        var excessLibraryMenu = await context.GroupPageComponentMappings.SingleAsync(x =>
            x.PermissionGroupId == CanonicalRbac.Employee.GroupId
            && x.PageComponentMappingId == libraryMenuMappingId
            && x.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode);
        Assert.False(excessLibraryMenu.IsEnable);
        Assert.False(excessLibraryMenu.IsVisible);
        await AssertActiveComponentsAsync(context, CanonicalRbac.Employee.GroupId);
    }

    [Fact]
    public async Task ReferenceRbacSeed_ReconcilesLegacyProcurementMembershipsIntoManager()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        var legacyGroup = new PermissionGroup
        {
            Id = CanonicalRbac.LegacyProcurementAdminGroupId,
            GroupCode = "PROCUREMENT_ADMIN",
            GroupName = "Legacy procurement admin",
            CreatedByUserId = 5615,
            CreatedAtUtc = now,
            UpdatedByUserId = 5615,
            UpdatedAtUtc = now
        };
        var remappedMembership = CreateMembership(1_001, legacyGroup.Id, now);
        var existingManagerMembership = CreateMembership(1_002, CanonicalRbac.Manager.GroupId, now);
        var duplicateLegacyMembership = CreateMembership(1_002, legacyGroup.Id, now);
        context.PermissionGroups.Add(legacyGroup);
        context.UserGroupMemberships.AddRange(
            remappedMembership,
            existingManagerMembership,
            duplicateLegacyMembership);
        await context.SaveChangesAsync();

        await SeedRbacAsync(context);

        Assert.True(legacyGroup.IsDeleted);
        Assert.False(remappedMembership.IsDeleted);
        Assert.Equal(CanonicalRbac.Manager.GroupId, remappedMembership.PermissionGroupId);
        Assert.False(existingManagerMembership.IsDeleted);
        Assert.True(duplicateLegacyMembership.IsDeleted);
        Assert.Empty(await context.UserGroupMemberships
            .Where(membership => !membership.IsDeleted
                && membership.PermissionGroupId == CanonicalRbac.LegacyProcurementAdminGroupId)
            .ToListAsync());
    }

    private static VPPMigrationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VPPMigrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VPPMigrationDbContext(options);
    }

    private static async Task SeedRbacAsync(VPPMigrationDbContext context)
    {
        await InvokeSeedAsync(context, "SeedPermissionPage");
        await InvokeSeedAsync(context, "SeedPermissionGroup");
        await InvokeSeedAsync(context, "ReconcileLegacyManagementRoles");
        await InvokeSeedAsync(context, "SeedPermissionComponent");
        await InvokeSeedAsync(context, "SeedPageComponentMapping");
        await InvokeSeedAsync(context, "SeedGroupPageComponentMapping");
    }

    private static async Task InvokeSeedAsync(VPPMigrationDbContext context, string methodName)
    {
        var method = typeof(SeedData).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Seed method '{methodName}' was not found.");
        var task = method.Invoke(null, [context]) as Task
            ?? throw new InvalidOperationException($"Seed method '{methodName}' did not return a Task.");
        await task;
    }

    private static async Task<Guid> MappingIdAsync(VPPMigrationDbContext context, string componentCode) =>
        await context.PageComponentMappings
            .Where(x => x.PermissionComponent != null && x.PermissionComponent.ComponentCode == componentCode)
            .Select(x => x.Id)
            .SingleAsync();

    private static async Task AssertActiveComponentsAsync(VPPMigrationDbContext context, Guid groupId)
    {
        var actual = await context.GroupPageComponentMappings
            .Where(x => x.PermissionGroupId == groupId
                && x.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode
                && x.IsEnable
                && x.IsVisible
                && x.PageComponentMapping != null
                && x.PageComponentMapping.PermissionComponent != null)
            .Select(x => x.PageComponentMapping!.PermissionComponent!.ComponentCode)
            .ToListAsync();
        Assert.Equal(
            CanonicalRbac.GetAllSeedComponents(groupId).Order(StringComparer.Ordinal),
            actual.Order(StringComparer.Ordinal));
    }

    private static UserGroupMembership CreateMembership(int accountId, Guid groupId, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = accountId,
            PermissionGroupId = groupId,
            DepartmentId = Guid.NewGuid(),
            CreatedByUserId = 5615,
            CreatedAtUtc = now,
            UpdatedByUserId = 5615,
            UpdatedAtUtc = now
        };

    private static async Task<(int Groups, int Components, int PageMappings, int GroupMappings)> ReadCountsAsync(
        VPPMigrationDbContext context) =>
        (
            await context.PermissionGroups.CountAsync(),
            await context.PermissionComponents.CountAsync(),
            await context.PageComponentMappings.CountAsync(),
            await context.GroupPageComponentMappings.CountAsync());
}
