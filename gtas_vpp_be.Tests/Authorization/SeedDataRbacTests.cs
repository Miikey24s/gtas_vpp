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
        Assert.Equal(4, await context.P02_Groups.CountAsync(x => personaIds.Contains(x.Id)));
        foreach (var persona in CanonicalRbac.Personas)
        {
            var group = await context.P02_Groups.SingleAsync(x => x.Id == persona.GroupId);
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

        var employee = await context.P02_Groups.SingleAsync(x => x.Id == CanonicalRbac.Employee.GroupId);
        employee.GroupCode = "DRIFTED";
        employee.ParentGroupId = CanonicalRbac.SystemAdmin.GroupId;
        employee.IsDeleted = true;

        var requestCreate = await context.P03_Components
            .SingleAsync(x => x.ComponentCode == Permissions.RequestCreate);
        requestCreate.ComponentName = "Drifted action";
        requestCreate.IsDeleted = true;

        var requestCreateMappingId = await MappingIdAsync(context, Permissions.RequestCreate);
        var employeeRequestCreate = await context.P06_GroupPageComponentMappings.SingleAsync(x =>
            x.P02_GroupId == CanonicalRbac.Employee.GroupId
            && x.P05_PageComponentMappingId == requestCreateMappingId
            && x.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode);
        employeeRequestCreate.IsEnable = false;
        employeeRequestCreate.IsVisible = false;

        var permissionUserMappingId = await MappingIdAsync(context, Permissions.PermissionUser);
        context.P06_GroupPageComponentMappings.Add(new P06_GroupPageComponentMapping
        {
            P02_GroupId = CanonicalRbac.Employee.GroupId,
            P05_PageComponentMappingId = permissionUserMappingId,
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            IsEnable = true,
            IsVisible = true,
            CreateUserId = 5615,
            CreateDate = DateTime.UtcNow,
            UpdateUserId = 5615,
            UpdateDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await InvokeSeedAsync(context, "SeedP02_Group");
        await InvokeSeedAsync(context, "SeedP03_Component");
        await InvokeSeedAsync(context, "SeedP05_PageComponentMapping");
        await InvokeSeedAsync(context, "SeedP06_GroupPageComponentMapping");

        Assert.Equal(CanonicalRbac.Employee.GroupCode, employee.GroupCode);
        Assert.Null(employee.ParentGroupId);
        Assert.False(employee.IsDeleted);
        Assert.Equal(
            CanonicalRbac.Actions.Single(x => x.PermissionCode == Permissions.RequestCreate).Name,
            requestCreate.ComponentName);
        Assert.False(requestCreate.IsDeleted);
        Assert.True(employeeRequestCreate.IsEnable);
        Assert.True(employeeRequestCreate.IsVisible);

        var excess = await context.P06_GroupPageComponentMappings.SingleAsync(x =>
            x.P02_GroupId == CanonicalRbac.Employee.GroupId
            && x.P05_PageComponentMappingId == permissionUserMappingId
            && x.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode);
        Assert.False(excess.IsEnable);
        Assert.False(excess.IsVisible);
        await AssertActiveComponentsAsync(context, CanonicalRbac.Employee.GroupId);
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
        await InvokeSeedAsync(context, "SeedP01_Page");
        await InvokeSeedAsync(context, "SeedP02_Group");
        await InvokeSeedAsync(context, "SeedP03_Component");
        await InvokeSeedAsync(context, "SeedP05_PageComponentMapping");
        await InvokeSeedAsync(context, "SeedP06_GroupPageComponentMapping");
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
        await context.P05_PageComponentMappings
            .Where(x => x.P03_Component != null && x.P03_Component.ComponentCode == componentCode)
            .Select(x => x.Id)
            .SingleAsync();

    private static async Task AssertActiveComponentsAsync(VPPMigrationDbContext context, Guid groupId)
    {
        var actual = await context.P06_GroupPageComponentMappings
            .Where(x => x.P02_GroupId == groupId
                && x.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode
                && x.IsEnable
                && x.IsVisible
                && x.P05_PageComponentMapping != null
                && x.P05_PageComponentMapping.P03_Component != null)
            .Select(x => x.P05_PageComponentMapping!.P03_Component!.ComponentCode)
            .ToListAsync();
        Assert.Equal(
            CanonicalRbac.GetAllSeedComponents(groupId).Order(StringComparer.Ordinal),
            actual.Order(StringComparer.Ordinal));
    }

    private static async Task<(int Groups, int Components, int PageMappings, int GroupMappings)> ReadCountsAsync(
        VPPMigrationDbContext context) =>
        (
            await context.P02_Groups.CountAsync(),
            await context.P03_Components.CountAsync(),
            await context.P05_PageComponentMappings.CountAsync(),
            await context.P06_GroupPageComponentMappings.CountAsync());
}
