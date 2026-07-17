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
            context.P02_Groups.Add(CreateGroup(persona));
        }

        context.P02_Groups.Add(new P02_Group
        {
            Id = Guid.NewGuid(),
            GroupCode = "LEGACY_ADMIN",
            GroupName = "Legacy admin",
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var action = await controller.GetGroups(getFullName: false, top: 100);

        var response = Assert.IsType<OkObjectResult>(action);
        var groups = Assert.IsType<List<P02_GroupResDTO>>(response.Value);
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
        var groups = new Mock<IGenericRepository<P02_Group>>();
        groups
            .Setup(repository => repository.GetByIdAsync(It.Is<object>(id => id.Equals(group.Id))))
            .ReturnsAsync(group);
        var controller = CreateController(context, groupRepository: groups);

        var action = await controller.UpdateGroup(
            group.Id,
            new P02_GroupUpdateReqDTO { GroupName = "Super administrator" });

        Assert.IsType<ConflictObjectResult>(action);
        groups.Verify(
            repository => repository.UpdateAsync(
                It.IsAny<P02_Group>(),
                It.IsAny<Expression<Func<P02_Group, object>>[]?>()),
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
            P05_PageComponentMappingId = fixture.Mapping.P05_PageComponentMappingId,
            P02_GroupId = fixture.Mapping.P02_GroupId,
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
            P05_PageComponentMappingId = fixture.Mapping.P05_PageComponentMappingId,
            P02_GroupId = fixture.Mapping.P02_GroupId,
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
            P05_PageComponentMappingId = fixture.Mapping.P05_PageComponentMappingId,
            P02_GroupId = fixture.Mapping.P02_GroupId,
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
            P05_PageComponentMappingId = fixture.Mapping.P05_PageComponentMappingId,
            P02_GroupId = fixture.Mapping.P02_GroupId,
            IsVisible = false,
            IsEnable = true
        });

        Assert.IsType<BadRequestObjectResult>(action);
        VerifyNeverUpdated(mappings);
    }

    private static PermissionController CreateController(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Mock<IGenericRepository<P02_Group>>? groupRepository = null,
        Mock<IGenericRepository<P06_GroupPageComponentMapping>>? mappingRepository = null)
    {
        var controller = new PermissionController(
            (groupRepository ?? new Mock<IGenericRepository<P02_Group>>()).Object,
            (mappingRepository ?? new Mock<IGenericRepository<P06_GroupPageComponentMapping>>()).Object,
            Mock.Of<IGenericRepository<P04_UserGroup>>(),
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

    private static P02_Group CreateGroup(RbacPersonaDefinition persona) => new()
    {
        Id = persona.GroupId,
        GroupCode = persona.GroupCode,
        GroupName = persona.GroupName,
        Description = persona.Description,
        CreateDate = DateTime.UtcNow,
        UpdateDate = DateTime.UtcNow
    };

    private static async Task<MappingFixture> CreateMappingAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Guid groupId,
        string componentCode)
    {
        var component = new P03_Component
        {
            Id = Guid.NewGuid(),
            ComponentCode = componentCode,
            ComponentName = componentCode,
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        };
        var pageMapping = new P05_PageComponentMapping
        {
            Id = Guid.NewGuid(),
            P01_PageId = Guid.NewGuid(),
            P03_ComponentId = component.Id,
            P03_Component = component
        };
        var mapping = new P06_GroupPageComponentMapping
        {
            P02_GroupId = groupId,
            P05_PageComponentMappingId = pageMapping.Id,
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            IsVisible = true,
            IsEnable = true,
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        };

        context.AddRange(component, pageMapping);
        await context.SaveChangesAsync();
        return new MappingFixture(mapping);
    }

    private static Mock<IGenericRepository<P06_GroupPageComponentMapping>> MappingRepository(
        P06_GroupPageComponentMapping mapping)
    {
        var repository = new Mock<IGenericRepository<P06_GroupPageComponentMapping>>();
        repository
            .Setup(item => item.ReadAsync(
                It.IsAny<Expression<Func<P06_GroupPageComponentMapping, bool>>>(),
                It.IsAny<Func<IQueryable<P06_GroupPageComponentMapping>, IQueryable<P06_GroupPageComponentMapping>>>(),
                It.IsAny<int?>()))
            .ReturnsAsync([mapping]);
        return repository;
    }

    private static void VerifyNeverUpdated(
        Mock<IGenericRepository<P06_GroupPageComponentMapping>> repository) =>
        repository.Verify(
            item => item.UpdateAsync(
                It.IsAny<P06_GroupPageComponentMapping>(),
                It.IsAny<Expression<Func<P06_GroupPageComponentMapping, object>>[]?>()),
            Times.Never);

    private sealed record MappingFixture(P06_GroupPageComponentMapping Mapping);
}
