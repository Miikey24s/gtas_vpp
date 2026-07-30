using System.Linq.Expressions;
using System.Security.Claims;
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
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.Authorization;

public sealed class CanonicalPermissionControllerTests
{
    [Fact]
    public async Task GetGroups_ReturnsOnlyActiveCanonicalPersonas()
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
        context.UserGroupMemberships.Add(new UserGroupMembership
        {
            Id = Guid.NewGuid(),
            AccountId = 1_000_001_006,
            UserId = 1_000_001_006,
            PermissionGroupId = CanonicalRbac.SystemAdmin.GroupId,
            DepartmentId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var action = await controller.GetGroups(getFullName: false, top: 100);

        var response = Assert.IsType<OkObjectResult>(action);
        var groups = Assert.IsType<List<PermissionGroupResDTO>>(response.Value);
        Assert.Equal(CanonicalRbac.Personas.Count, groups.Count);
        Assert.Equal(
            CanonicalRbac.Personas.Select(persona => persona.GroupId).Order(),
            groups.Select(group => group.Id).Order());
        Assert.All(groups, group => Assert.Equal(CanonicalRbac.DefaultMemberCompanyCode, group.MemberCompanyCode));
        Assert.Equal(
            CanonicalRbac.Personas.Select(persona => persona.GroupCode).Order(),
            groups.Select(group => group.GroupCode).Order());
        Assert.Equal(1, groups.Single(group => group.Id == CanonicalRbac.SystemAdmin.GroupId).UserCount);
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
            CanonicalRbac.Employee.GroupId,
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

    [Fact]
    public async Task PatchComponentMappingsBatch_UpdatesUiMappingsAtomicallyAndWritesAudit()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var uiComponents = CanonicalRbac.GetUiComponents(CanonicalRbac.Employee.GroupId).Take(2).ToArray();
        Assert.Equal(2, uiComponents.Length);
        var first = await CreateTrackedMappingAsync(context, CanonicalRbac.Employee.GroupId, uiComponents[0]);
        var second = await CreateTrackedMappingAsync(context, CanonicalRbac.Employee.GroupId, uiComponents[1]);
        var notifier = new Mock<IPermissionChangeNotifier>();
        var controller = CreateController(context, permissionChangeNotifier: notifier);

        var action = await controller.PatchComponentMappingsBatch(
            new BatchPatchComponentMappingsReqDTO
            {
                PermissionGroupId = CanonicalRbac.Employee.GroupId,
                Reason = "Batch editor test",
                Items =
                [
                    new() { PageComponentMappingId = first.Mapping.PageComponentMappingId, IsVisible = true, IsEnable = false },
                    new() { PageComponentMappingId = second.Mapping.PageComponentMappingId, IsVisible = false, IsEnable = false }
                ]
            },
            CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(action);
        var result = Assert.IsType<BatchPatchComponentMappingsResDTO>(response.Value);
        Assert.Equal(2, result.UpdatedCount);
        Assert.True(first.Mapping.IsVisible);
        Assert.False(first.Mapping.IsEnable);
        Assert.False(second.Mapping.IsVisible);
        Assert.False(second.Mapping.IsEnable);
        Assert.Single(context.SecurityAudits, audit =>
            audit.Action == "PERMISSION_UI_BATCH_UPDATED"
            && audit.ResourceId == CanonicalRbac.Employee.GroupId.ToString());
        notifier.Verify(
            service => service.NotifyGroupChangedAsync(
                CanonicalRbac.Employee.GroupId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PatchComponentMappingsBatch_RejectsActionGrantWithoutMutatingValidItems()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var uiCode = CanonicalRbac.GetUiComponents(CanonicalRbac.Employee.GroupId).First();
        var uiMapping = await CreateTrackedMappingAsync(context, CanonicalRbac.Employee.GroupId, uiCode);
        var actionMapping = await CreateTrackedMappingAsync(
            context,
            CanonicalRbac.Employee.GroupId,
            Permissions.RequestCreate);
        var controller = CreateController(context);

        var action = await controller.PatchComponentMappingsBatch(
            new BatchPatchComponentMappingsReqDTO
            {
                PermissionGroupId = CanonicalRbac.Employee.GroupId,
                Items =
                [
                    new() { PageComponentMappingId = uiMapping.Mapping.PageComponentMappingId, IsVisible = false, IsEnable = false },
                    new() { PageComponentMappingId = actionMapping.Mapping.PageComponentMappingId, IsVisible = false, IsEnable = false }
                ]
            },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(action);
        Assert.True(uiMapping.Mapping.IsVisible);
        Assert.True(uiMapping.Mapping.IsEnable);
        Assert.Empty(context.SecurityAudits);
    }

    [Fact]
    public async Task PatchComponentMappingsBatch_CannotDisableProtectedSystemAdminNavigation()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var mapping = await CreateTrackedMappingAsync(
            context,
            CanonicalRbac.SystemAdmin.GroupId,
            Permissions.MenuPermission);
        var controller = CreateController(context);

        var action = await controller.PatchComponentMappingsBatch(
            new BatchPatchComponentMappingsReqDTO
            {
                PermissionGroupId = CanonicalRbac.SystemAdmin.GroupId,
                Items =
                [
                    new() { PageComponentMappingId = mapping.Mapping.PageComponentMappingId, IsVisible = false, IsEnable = false }
                ]
            },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(action);
        Assert.True(mapping.Mapping.IsVisible);
        Assert.True(mapping.Mapping.IsEnable);
    }

    [Fact]
    public async Task GetSecurityAudits_FiltersAcrossFullQueryAndResolvesUsers()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        context.Users.AddRange(
            new AppUser { Id = 101, UserName = "admin.audit", FullName = "Quản trị Audit" },
            new AppUser { Id = 202, UserName = "target.audit", FullName = "Người dùng đích" });
        context.SecurityAudits.AddRange(
            new SecurityAudit
            {
                Id = Guid.NewGuid(),
                ActorUserId = 101,
                TargetUserId = 202,
                Action = "MEMBERSHIP_CREATED",
                ResourceType = "UserGroupMembership",
                ResourceId = "membership-1",
                Outcome = "Succeeded",
                Summary = "Assigned canonical group",
                Reason = "Test assignment",
                OccurredAtUtc = new DateTime(2026, 7, 30, 1, 0, 0, DateTimeKind.Utc)
            },
            new SecurityAudit
            {
                Id = Guid.NewGuid(),
                ActorUserId = 101,
                Action = "ACCOUNT_DISABLED",
                ResourceType = "AppUser",
                ResourceId = "202",
                Outcome = "Succeeded",
                Summary = "Disabled account",
                OccurredAtUtc = new DateTime(2026, 7, 29, 1, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var action = await controller.GetSecurityAudits(
            search: "Người dùng đích",
            action: "MEMBERSHIP_CREATED",
            outcome: "Succeeded",
            skip: 0,
            top: 50,
            orderby: "OccurredAtUtc desc");

        var result = Assert.IsType<OkObjectResult>(action);
        var rows = Assert.IsType<List<SecurityAuditResDTO>>(result.Value);
        var row = Assert.Single(rows);
        Assert.Equal("admin.audit", row.ActorUserName);
        Assert.Equal("Người dùng đích", row.TargetFullName);
        Assert.Equal("1", controller.Response.Headers["X-Total-Count"]);

        var sortedAction = await controller.GetSecurityAudits(
            skip: 0,
            top: 50,
            orderby: "TargetFullName desc");
        var sortedRows = Assert.IsType<List<SecurityAuditResDTO>>(
            Assert.IsType<OkObjectResult>(sortedAction).Value);
        Assert.Equal("Người dùng đích", sortedRows.First().TargetFullName);
    }

    [Fact]
    public async Task GetSecurityAuditFilterOptions_ReturnsDistinctSortedValues()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        context.SecurityAudits.AddRange(
            new SecurityAudit { Id = Guid.NewGuid(), Action = "Z_ACTION", ResourceType = "Test", Outcome = "Succeeded" },
            new SecurityAudit { Id = Guid.NewGuid(), Action = "A_ACTION", ResourceType = "Test", Outcome = "Failed" },
            new SecurityAudit { Id = Guid.NewGuid(), Action = "A_ACTION", ResourceType = "Test", Outcome = "Succeeded" });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var action = await controller.GetSecurityAuditFilterOptions(CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(action);
        var options = Assert.IsType<SecurityAuditFilterOptionsResDTO>(result.Value);
        Assert.Equal(["A_ACTION", "Z_ACTION"], options.Actions);
        Assert.Equal(["Failed", "Succeeded"], options.Outcomes);
    }

    private static PermissionController CreateController(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Mock<IGenericRepository<PermissionGroup>>? groupRepository = null,
        Mock<IGenericRepository<GroupPageComponentMapping>>? mappingRepository = null,
        Mock<IPermissionChangeNotifier>? permissionChangeNotifier = null)
    {
        var controller = new PermissionController(
            (groupRepository ?? new Mock<IGenericRepository<PermissionGroup>>()).Object,
            (mappingRepository ?? new Mock<IGenericRepository<GroupPageComponentMapping>>()).Object,
            Mock.Of<IGenericRepository<UserGroupMembership>>(),
            Mock.Of<IUserNameResolver>(),
            ServiceTestHelpers.CreateUnitOfWorkMock(context).Object,
            new FakeDateTimeProvider(DateTime.UtcNow),
            (permissionChangeNotifier ?? new Mock<IPermissionChangeNotifier>()).Object,
            Mock.Of<IMembershipAdministrationService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("UserID", "1000001006")
                    ], "test"))
                }
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

    private static async Task<MappingFixture> CreateTrackedMappingAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Guid groupId,
        string componentCode)
    {
        var fixture = await CreateMappingAsync(context, groupId, componentCode);
        fixture.Mapping.PageComponentMapping = await context.PageComponentMappings
            .Include(mapping => mapping.PermissionComponent)
            .SingleAsync(mapping => mapping.Id == fixture.Mapping.PageComponentMappingId);
        context.GroupPageComponentMappings.Add(fixture.Mapping);
        await context.SaveChangesAsync();
        return fixture;
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
