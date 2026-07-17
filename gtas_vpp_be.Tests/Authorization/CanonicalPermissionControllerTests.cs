using System.Linq.Expressions;
using gtas_vpp_be.Authorization;
using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Permission;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.Authorization;

public sealed class CanonicalPermissionControllerTests
{
    [Fact]
    public async Task GetGroups_ReturnsOnlyFourActiveCanonicalPersonas()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        foreach (var persona in CanonicalRbac.Personas)
        {
            context.PermissionGroups.Add(CreateGroup(persona));
        }

        context.PermissionGroups.Add(new PermissionGroup
        {
            Id = Guid.NewGuid(),
            GroupCode = "LEGACY_ADMIN",
            GroupName = "Legacy admin",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var action = await controller.GetGroups(getFullName: false, top: 100);

        var response = Assert.IsType<OkObjectResult>(action);
        var groups = Assert.IsType<List<PermissionGroupResDTO>>(response.Value);
        Assert.Equal(4, groups.Count);
        Assert.Equal(
            CanonicalRbac.Personas.Select(persona => persona.GroupId).Order(),
            groups.Select(group => group.Id).Order());
        Assert.All(groups, group => Assert.Equal(CanonicalRbac.DefaultMemberCompanyCode, group.MemberCompanyCode));
    }

    [Fact]
    public async Task UpdateGroup_RejectsRuntimeChangesToCanonicalRoleDefinition()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var group = CreateGroup(CanonicalRbac.SystemAdmin);
        var groups = new Mock<IGenericRepository<PermissionGroup>>();
        groups
            .Setup(repository => repository.GetByIdAsync(It.Is<object>(id => id.Equals(group.Id))))
            .ReturnsAsync(group);
        var controller = CreateController(context, groupRepository: groups);

        var action = await controller.UpdateGroup(
            group.Id,
            new PermissionGroupUpdateReqDTO { GroupName = "Super administrator" });

        Assert.IsType<ConflictObjectResult>(action);
        groups.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<PermissionGroup>(),
                It.IsAny<Expression<Func<PermissionGroup, object>>[]?>()),
            Times.Never);
    }

    [Fact]
    public async Task PatchComponentMapping_RejectsBackendActionGrantMutation()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var fixture = await CreateMappingAsync(
            context,
            CanonicalRbac.Employee.GroupId,
            Permissions.RequestCreate);
        var mappings = MappingRepository(fixture.Mapping);
        var controller = CreateController(context, mappingRepository: mappings);

        var action = await controller.PatchComponentMapping(new PatchComponentMappingReqDTO
        {
            PageComponentMappingId = fixture.Mapping.PageComponentMappingId,
            PermissionGroupId = fixture.Mapping.PermissionGroupId,
            IsVisible = false,
            IsEnable = false
        });

        Assert.IsType<ConflictObjectResult>(action);
        VerifyNeverUpdated(mappings);
    }

    [Fact]
    public async Task PatchComponentMapping_RejectsComponentOutsidePersonaCeiling()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var fixture = await CreateMappingAsync(
            context,
            CanonicalRbac.SystemAdmin.GroupId,
            Permissions.RequestAllOrdersSummary);
        var mappings = MappingRepository(fixture.Mapping);
        var controller = CreateController(context, mappingRepository: mappings);

        var action = await controller.PatchComponentMapping(new PatchComponentMappingReqDTO
        {
            PageComponentMappingId = fixture.Mapping.PageComponentMappingId,
            PermissionGroupId = fixture.Mapping.PermissionGroupId,
            IsVisible = true,
            IsEnable = true
        });

        Assert.IsType<ConflictObjectResult>(action);
        VerifyNeverUpdated(mappings);
    }

    [Fact]
    public async Task PatchComponentMapping_CannotDisableProtectedSystemAdminNavigation()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var fixture = await CreateMappingAsync(
            context,
            CanonicalRbac.SystemAdmin.GroupId,
            Permissions.MenuPermission);
        var mappings = MappingRepository(fixture.Mapping);
        var controller = CreateController(context, mappingRepository: mappings);

        var action = await controller.PatchComponentMapping(new PatchComponentMappingReqDTO
        {
            PageComponentMappingId = fixture.Mapping.PageComponentMappingId,
            PermissionGroupId = fixture.Mapping.PermissionGroupId,
            IsVisible = false,
            IsEnable = false
        });

        Assert.IsType<ConflictObjectResult>(action);
        VerifyNeverUpdated(mappings);
    }

    [Fact]
    public async Task PatchComponentMapping_RejectsEnabledButHiddenState()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var fixture = await CreateMappingAsync(
            context,
            CanonicalRbac.Employee.GroupId,
            Permissions.MenuDashboard);
        var mappings = MappingRepository(fixture.Mapping);
        var controller = CreateController(context, mappingRepository: mappings);

        var action = await controller.PatchComponentMapping(new PatchComponentMappingReqDTO
        {
            PageComponentMappingId = fixture.Mapping.PageComponentMappingId,
            PermissionGroupId = fixture.Mapping.PermissionGroupId,
            IsVisible = false,
            IsEnable = true
        });

        Assert.IsType<BadRequestObjectResult>(action);
        VerifyNeverUpdated(mappings);
    }

    private static PermissionController CreateController(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Mock<IGenericRepository<PermissionGroup>>? groupRepository = null,
        Mock<IGenericRepository<GroupPageComponentMapping>>? mappingRepository = null)
    {
        var controller = new PermissionController(
            (groupRepository ?? new Mock<IGenericRepository<PermissionGroup>>()).Object,
            (mappingRepository ?? new Mock<IGenericRepository<GroupPageComponentMapping>>()).Object,
            Mock.Of<IGenericRepository<UserGroupMembership>>(),
            Mock.Of<IUserNameResolver>(),
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

        return controller;
    }

    private static PermissionGroup CreateGroup(RbacPersonaDefinition persona) => new()
    {
        Id = persona.GroupId,
        GroupCode = persona.GroupCode,
        GroupName = persona.GroupName,
        Description = persona.Description,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private static async Task<MappingFixture> CreateMappingAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Guid groupId,
        string componentCode)
    {
        var component = new PermissionComponent
        {
            Id = Guid.NewGuid(),
            ComponentCode = componentCode,
            ComponentName = componentCode,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var pageMapping = new PageComponentMapping
        {
            Id = Guid.NewGuid(),
            PermissionPageId = Guid.NewGuid(),
            PermissionComponentId = component.Id,
            PermissionComponent = component
        };
        var mapping = new GroupPageComponentMapping
        {
            PermissionGroupId = groupId,
            PageComponentMappingId = pageMapping.Id,
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            IsVisible = true,
            IsEnable = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        context.AddRange(component, pageMapping);
        await context.SaveChangesAsync();
        return new MappingFixture(mapping);
    }

    private static Mock<IGenericRepository<GroupPageComponentMapping>> MappingRepository(
        GroupPageComponentMapping mapping)
    {
        var repository = new Mock<IGenericRepository<GroupPageComponentMapping>>();
        repository
            .Setup(item => item.ReadAsync(
                It.IsAny<Expression<Func<GroupPageComponentMapping, bool>>>(),
                It.IsAny<Func<IQueryable<GroupPageComponentMapping>, IQueryable<GroupPageComponentMapping>>>(),
                It.IsAny<int?>()))
            .ReturnsAsync([mapping]);
        return repository;
    }

    private static void VerifyNeverUpdated(
        Mock<IGenericRepository<GroupPageComponentMapping>> repository) =>
        repository.Verify(
            item => item.UpdateAsync(
                It.IsAny<GroupPageComponentMapping>(),
                It.IsAny<Expression<Func<GroupPageComponentMapping, object>>[]?>()),
            Times.Never);

    private sealed record MappingFixture(GroupPageComponentMapping Mapping);
}
