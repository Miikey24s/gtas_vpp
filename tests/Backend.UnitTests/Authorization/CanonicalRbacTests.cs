using gtas_vpp_shared.Constants;
using Xunit;

namespace gtas_vpp_be.Tests.Authorization;

public sealed class CanonicalRbacTests
{
    [Fact]
    public void Personas_HaveStableIdsAndExplicitCodes()
    {
        Assert.Equal(Guid.Parse("388C6C3A-2801-42DC-BFC0-8A7741264596"), CanonicalRbac.Employee.GroupId);
        Assert.Equal(Guid.Parse("E170FF76-F46E-460B-BC20-1D525E585DF8"), CanonicalRbac.Manager.GroupId);
        Assert.Equal(Guid.Parse("2F048784-AFC2-4322-906F-D645B982BE49"), CanonicalRbac.LegacyProcurementAdminGroupId);
        Assert.Equal(Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"), CanonicalRbac.Dev.GroupId);

        Assert.Equal("EMPLOYEE", CanonicalRbac.Employee.GroupCode);
        Assert.Equal("MANAGER", CanonicalRbac.Manager.GroupCode);
        Assert.Equal("DEV", CanonicalRbac.Dev.GroupCode);
        Assert.Equal(3, CanonicalRbac.Personas.Select(x => x.GroupId).Distinct().Count());
        Assert.Equal(3, CanonicalRbac.Personas.Select(x => x.GroupCode).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Personas_ExcludeLegacyProcurementGroup()
    {
        Assert.DoesNotContain(
            CanonicalRbac.LegacyProcurementAdminGroupId,
            CanonicalRbac.Personas.Select(persona => persona.GroupId));
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
    public void Manager_CombinesDepartmentApprovalAndCompanyProcurement()
    {
        AssertActions(
            CanonicalRbac.Manager.GroupId,
            Permissions.RequestViewOwn,
            Permissions.RequestCreate,
            Permissions.RequestUpdateOwn,
            Permissions.RequestCancelOwn,
            Permissions.RequestCatalogView,
            Permissions.LibraryView,
            Permissions.ReportViewOwn,
            Permissions.RequestViewDepartment,
            Permissions.RequestViewAll,
            Permissions.RequestApprove,
            Permissions.RequestReject,
            Permissions.LibraryManage,
            Permissions.ReportViewDepartment,
            Permissions.ReportViewAll,
            Permissions.ReportExport,
            Permissions.PeriodSettle);

        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.Manager.GroupId, Permissions.PermissionView));
        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.Manager.GroupId, Permissions.PermissionManage));
        Assert.False(CanonicalRbac.HasAction(CanonicalRbac.Manager.GroupId, Permissions.PeriodSettingsManage));
    }

    [Fact]
    public void Dev_HasEveryCanonicalAction()
    {
        AssertActions(CanonicalRbac.Dev.GroupId, Permissions.ActionCodes.ToArray());
    }

    [Fact]
    public void UiMatrix_IsExplicitAndKeepsAdministrativeAreasSeparated()
    {
        AssertUi(
            CanonicalRbac.Employee.GroupId,
            Permissions.MenuDashboard,
            Permissions.MenuReport,
            Permissions.RequestOrder,
            Permissions.RequestHistory,
            Permissions.RequestProductCatalog,
            Permissions.ReportView);

        AssertUi(
            CanonicalRbac.Manager.GroupId,
            Permissions.MenuDashboard,
            Permissions.MenuLibrary,
            Permissions.MenuReport,
            Permissions.RequestOrder,
            Permissions.RequestHistory,
            Permissions.RequestProductCatalog,
            Permissions.RequestDepartmentSummary,
            Permissions.RequestAllOrdersSummary,
            Permissions.RequestAdminApproval,
            Permissions.LibraryClass,
            Permissions.LibraryCategory,
            Permissions.LibraryItem,
            Permissions.LibrarySupplier,
            Permissions.LibraryPrice,
            Permissions.LibraryPriceList,
            Permissions.LibraryDepartment,
            Permissions.ReportView,
            Permissions.PeriodSettle);

        AssertUi(
            CanonicalRbac.Dev.GroupId,
            Permissions.MenuDashboard,
            Permissions.MenuLibrary,
            Permissions.MenuReport,
            Permissions.MenuPermission,
            Permissions.RequestOrder,
            Permissions.RequestHistory,
            Permissions.RequestProductCatalog,
            Permissions.RequestDepartmentSummary,
            Permissions.RequestAllOrdersSummary,
            Permissions.RequestAdminApproval,
            Permissions.LibraryClass,
            Permissions.LibraryCategory,
            Permissions.LibraryItem,
            Permissions.LibrarySupplier,
            Permissions.LibraryPrice,
            Permissions.LibraryPriceList,
            Permissions.LibraryDepartment,
            Permissions.ReportView,
            Permissions.PeriodSettle,
            Permissions.PermissionUser,
            Permissions.PermissionComponent);
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

    private static void AssertUi(Guid groupId, params string[] expected)
    {
        Assert.Equal(
            expected.Order(StringComparer.Ordinal),
            CanonicalRbac.GetUiComponents(groupId).Order(StringComparer.Ordinal));
    }
}
