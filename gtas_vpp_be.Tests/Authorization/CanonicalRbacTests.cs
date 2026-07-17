using gtas_vpp_shared.Constants;
using Xunit;

namespace gtas_vpp_be.Tests.Authorization;

public sealed class CanonicalRbacTests
{
    [Fact]
    public void Personas_HaveStableIdsAndExplicitCodes()
    {
        Assert.Equal(Guid.Parse("388C6C3A-2801-42DC-BFC0-8A7741264596"), CanonicalRbac.Employee.GroupId);
        Assert.Equal(Guid.Parse("E170FF76-F46E-460B-BC20-1D525E585DF8"), CanonicalRbac.DepartmentApprover.GroupId);
        Assert.Equal(Guid.Parse("2F048784-AFC2-4322-906F-D645B982BE49"), CanonicalRbac.ProcurementAdmin.GroupId);
        Assert.Equal(Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"), CanonicalRbac.SystemAdmin.GroupId);

        Assert.Equal("EMPLOYEE", CanonicalRbac.Employee.GroupCode);
        Assert.Equal("DEPARTMENT_APPROVER", CanonicalRbac.DepartmentApprover.GroupCode);
        Assert.Equal("PROCUREMENT_ADMIN", CanonicalRbac.ProcurementAdmin.GroupCode);
        Assert.Equal("SYSTEM_ADMIN", CanonicalRbac.SystemAdmin.GroupCode);
        Assert.Equal(4, CanonicalRbac.Personas.Select(x => x.GroupId).Distinct().Count());
        Assert.Equal(4, CanonicalRbac.Personas.Select(x => x.GroupCode).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void PermissionCatalog_ContainsOnlyExplicitBackendActions()
    {
        Assert.Equal(
            Permissions.ActionCodes.Order(StringComparer.Ordinal),
            CanonicalRbac.Actions.Select(x => x.PermissionCode).Order(StringComparer.Ordinal));
        Assert.Equal(Permissions.ActionCodes, Permissions.All);
        Assert.Empty(Permissions.ActionCodes.Intersect(Permissions.UiComponentCodes, StringComparer.OrdinalIgnoreCase));
        Assert.DoesNotContain(Permissions.RequestOrder, Permissions.All);
        Assert.DoesNotContain(Permissions.ReportView, Permissions.All);
    }

    [Fact]
    public void Employee_HasOnlyOwnScopeAndReadOnlyReferenceActions()
    {
        AssertActions(
            CanonicalRbac.Employee.GroupId,
            Permissions.RequestViewOwn,
            Permissions.RequestCreate,
            Permissions.RequestUpdateOwn,
            Permissions.RequestCancelOwn,
            Permissions.RequestCatalogView,
            Permissions.LibraryView,
            Permissions.ReportViewOwn);
    }

    [Fact]
    public void DepartmentApprover_HasDepartmentScopeAndApprovalWithoutCompanyAuthority()
    {
        AssertActions(
            CanonicalRbac.DepartmentApprover.GroupId,
            Permissions.RequestViewOwn,
            Permissions.RequestCreate,
            Permissions.RequestUpdateOwn,
            Permissions.RequestCancelOwn,
            Permissions.RequestCatalogView,
            Permissions.LibraryView,
            Permissions.ReportViewOwn,
            Permissions.RequestViewDepartment,
            Permissions.RequestApprove,
            Permissions.RequestReject,
            Permissions.ReportViewDepartment);

        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.DepartmentApprover.GroupId, Permissions.RequestViewAll));
        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.DepartmentApprover.GroupId, Permissions.ReportExport));
        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.DepartmentApprover.GroupId, Permissions.PeriodSettle));
    }

    [Fact]
    public void ProcurementAdmin_HasCompanyProcurementWithoutPermissionAdministration()
    {
        AssertActions(
            CanonicalRbac.ProcurementAdmin.GroupId,
            Permissions.RequestViewOwn,
            Permissions.RequestCreate,
            Permissions.RequestUpdateOwn,
            Permissions.RequestCancelOwn,
            Permissions.RequestCatalogView,
            Permissions.LibraryView,
            Permissions.ReportViewOwn,
            Permissions.RequestViewDepartment,
            Permissions.RequestViewAll,
            Permissions.LibraryManage,
            Permissions.ReportViewDepartment,
            Permissions.ReportViewAll,
            Permissions.ReportExport,
            Permissions.PeriodSettle);

        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.ProcurementAdmin.GroupId, Permissions.PermissionView));
        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.ProcurementAdmin.GroupId, Permissions.PermissionManage));
    }

    [Fact]
    public void SystemAdmin_HasPermissionAdministrationWithoutProcurementAuthority()
    {
        AssertActions(
            CanonicalRbac.SystemAdmin.GroupId,
            Permissions.RequestViewOwn,
            Permissions.RequestCreate,
            Permissions.RequestUpdateOwn,
            Permissions.RequestCancelOwn,
            Permissions.RequestCatalogView,
            Permissions.LibraryView,
            Permissions.ReportViewOwn,
            Permissions.PermissionView,
            Permissions.PermissionManage);

        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.SystemAdmin.GroupId, Permissions.LibraryManage));
        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.SystemAdmin.GroupId, Permissions.ReportViewAll));
        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.SystemAdmin.GroupId, Permissions.ReportExport));
        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.SystemAdmin.GroupId, Permissions.PeriodSettle));
    }

    [Fact]
    public void UiMatrix_IsExplicitAndKeepsAdministrativeAreasSeparated()
    {
        var employee = CanonicalRbac.GetUiComponents(CanonicalRbac.Employee.GroupId);
        Assert.DoesNotContain(Permissions.RequestDepartmentSummary, employee);
        Assert.DoesNotContain(Permissions.RequestAdminApproval, employee);
        Assert.DoesNotContain(Permissions.MenuPermission, employee);

        var approver = CanonicalRbac.GetUiComponents(CanonicalRbac.DepartmentApprover.GroupId);
        Assert.Contains(Permissions.RequestDepartmentSummary, approver);
        Assert.Contains(Permissions.RequestAdminApproval, approver);
        Assert.DoesNotContain(Permissions.RequestAllOrdersSummary, approver);
        Assert.DoesNotContain(Permissions.MenuPermission, approver);

        var procurement = CanonicalRbac.GetUiComponents(CanonicalRbac.ProcurementAdmin.GroupId);
        Assert.Contains(Permissions.RequestAllOrdersSummary, procurement);
        Assert.Contains(Permissions.PeriodSettle, procurement);
        Assert.DoesNotContain(Permissions.MenuPermission, procurement);

        var system = CanonicalRbac.GetUiComponents(CanonicalRbac.SystemAdmin.GroupId);
        Assert.Contains(Permissions.MenuPermission, system);
        Assert.Contains(Permissions.PermissionUser, system);
        Assert.Contains(Permissions.PermissionComponent, system);
        Assert.DoesNotContain(Permissions.RequestAllOrdersSummary, system);
        Assert.DoesNotContain(Permissions.PeriodSettle, system);
    }

    [Fact]
    public void SeedIds_AreDeterministicAndSeparatedByEntityKind()
    {
        Assert.Equal(
            Guid.Parse("9C67D1C4-9F42-1712-7343-96C883FBD0E2"),
            CanonicalRbacSeedIds.ActionComponent(Permissions.RequestCreate));
        Assert.Equal(
            Guid.Parse("E80FA445-3387-4746-42B8-05923D774AE8"),
            CanonicalRbacSeedIds.ActionPageMapping(Permissions.RequestCreate));

        var componentIds = Permissions.ActionCodes
            .Where(x => x != Permissions.PeriodSettle)
            .Select(CanonicalRbacSeedIds.ActionComponent)
            .ToArray();
        var mappingIds = Permissions.ActionCodes
            .Where(x => x != Permissions.PeriodSettle)
            .Select(CanonicalRbacSeedIds.ActionPageMapping)
            .ToArray();
        Assert.Equal(componentIds.Length, componentIds.Distinct().Count());
        Assert.Equal(mappingIds.Length, mappingIds.Distinct().Count());
        Assert.Empty(componentIds.Intersect(mappingIds));
    }

    private static void AssertActions(Guid groupId, params string[] expected)
    {
        Assert.Equal(
            expected.Order(StringComparer.Ordinal),
            CanonicalRbac.GetActionPermissions(groupId).Order(StringComparer.Ordinal));
    }
}
