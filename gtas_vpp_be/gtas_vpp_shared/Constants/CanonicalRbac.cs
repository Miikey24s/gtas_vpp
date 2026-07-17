using System.Security.Cryptography;
using System.Text;

namespace gtas_vpp_shared.Constants;

public sealed record RbacPersonaDefinition(
    Guid GroupId,
    string GroupCode,
    string GroupName,
    string Description);

public sealed record RbacActionDefinition(
    string PermissionCode,
    string PageCode,
    string Name,
    string Description);

/// <summary>
/// Stable, flat v1 personas. GroupCode is the security identity; GroupName is
/// only the user-facing display name.
/// </summary>
public static class CanonicalRbac
{
    public const long DefaultMemberCompanyCode = 77500;

    public static RbacPersonaDefinition Employee { get; } = new(
        Guid.Parse("388C6C3A-2801-42DC-BFC0-8A7741264596"),
        "EMPLOYEE",
        "User",
        "Employee with own-request and read-only catalog access.");

    public static RbacPersonaDefinition DepartmentApprover { get; } = new(
        Guid.Parse("E170FF76-F46E-460B-BC20-1D525E585DF8"),
        "DEPARTMENT_APPROVER",
        "Department Approver",
        "Department-scoped approver for requests and reports.");

    public static RbacPersonaDefinition ProcurementAdmin { get; } = new(
        Guid.Parse("2F048784-AFC2-4322-906F-D645B982BE49"),
        "PROCUREMENT_ADMIN",
        "Procurement Admin",
        "Company procurement, catalog pricing, reporting and period settlement.");

    public static RbacPersonaDefinition SystemAdmin { get; } = new(
        Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
        "SYSTEM_ADMIN",
        "Admin",
        "System account, membership and permission administrator without procurement authority.");

    public static IReadOnlyList<RbacPersonaDefinition> Personas { get; } = Array.AsReadOnly(
        new RbacPersonaDefinition[]
        {
            Employee,
            DepartmentApprover,
            ProcurementAdmin,
            SystemAdmin
        });

    public static IReadOnlyList<RbacActionDefinition> Actions { get; } = Array.AsReadOnly(
        new RbacActionDefinition[]
        {
            new(Permissions.RequestViewOwn, "DASHBOARD", "View own requests", "Read requests created by the current user."),
            new(Permissions.RequestViewDepartment, "DASHBOARD", "View department requests", "Read requests in the current primary department."),
            new(Permissions.RequestViewAll, "DASHBOARD", "View company requests", "Read requests across the company."),
            new(Permissions.RequestCreate, "DASHBOARD", "Create request", "Create a request for the current user."),
            new(Permissions.RequestUpdateOwn, "DASHBOARD", "Update own request", "Update an eligible request owned by the current user."),
            new(Permissions.RequestCancelOwn, "DASHBOARD", "Cancel own request", "Cancel an eligible request owned by the current user."),
            new(Permissions.RequestApprove, "DASHBOARD", "Approve supplement", "Approve an eligible department supplement from another requester."),
            new(Permissions.RequestReject, "DASHBOARD", "Reject supplement", "Reject an eligible department supplement from another requester."),
            new(Permissions.RequestCatalogView, "DASHBOARD", "View request catalog", "Read products available for request entry."),
            new(Permissions.LibraryView, "LIBRARY", "View library", "Read catalog, supplier and pricing reference data."),
            new(Permissions.LibraryManage, "LIBRARY", "Manage library", "Create or change catalog, supplier and pricing reference data."),
            new(Permissions.PermissionView, "PERMISSION", "View access administration", "Read personas, memberships and explicit permission mappings."),
            new(Permissions.PermissionManage, "PERMISSION", "Manage access", "Change memberships and guarded permission mappings."),
            new(Permissions.ReportViewOwn, "REPORT", "View own report", "Read report data owned by the current user."),
            new(Permissions.ReportViewDepartment, "REPORT", "View department report", "Read report data in the current primary department."),
            new(Permissions.ReportViewAll, "REPORT", "View company report", "Read report data across the company."),
            new(Permissions.ReportExport, "REPORT", "Export report", "Export data within another explicitly granted report scope."),
            new(Permissions.PeriodSettle, "DASHBOARD", "Settle period", "Preview and settle a company procurement period.")
        });

    private static readonly IReadOnlyDictionary<Guid, IReadOnlyList<string>> ActionMatrix =
        new Dictionary<Guid, IReadOnlyList<string>>
        {
            [Employee.GroupId] = Explicit(
                Permissions.RequestViewOwn,
                Permissions.RequestCreate,
                Permissions.RequestUpdateOwn,
                Permissions.RequestCancelOwn,
                Permissions.RequestCatalogView,
                Permissions.LibraryView,
                Permissions.ReportViewOwn),
            [DepartmentApprover.GroupId] = Explicit(
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
                Permissions.ReportViewDepartment),
            [ProcurementAdmin.GroupId] = Explicit(
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
                Permissions.PeriodSettle),
            [SystemAdmin.GroupId] = Explicit(
                Permissions.RequestViewOwn,
                Permissions.RequestCreate,
                Permissions.RequestUpdateOwn,
                Permissions.RequestCancelOwn,
                Permissions.RequestCatalogView,
                Permissions.LibraryView,
                Permissions.ReportViewOwn,
                Permissions.PermissionView,
                Permissions.PermissionManage)
        };

    private static readonly IReadOnlyDictionary<Guid, IReadOnlyList<string>> UiMatrix =
        new Dictionary<Guid, IReadOnlyList<string>>
        {
            [Employee.GroupId] = Explicit(
                Permissions.MenuDashboard,
                Permissions.MenuLibrary,
                Permissions.MenuReport,
                Permissions.RequestOrder,
                Permissions.RequestHistory,
                Permissions.RequestProductCatalog,
                Permissions.LibraryClass,
                Permissions.LibraryCategory,
                Permissions.LibraryItem,
                Permissions.LibrarySupplier,
                Permissions.LibraryPrice,
                Permissions.LibraryPriceList,
                Permissions.LibraryDepartment,
                Permissions.ReportView),
            [DepartmentApprover.GroupId] = Explicit(
                Permissions.MenuDashboard,
                Permissions.MenuLibrary,
                Permissions.MenuReport,
                Permissions.RequestOrder,
                Permissions.RequestHistory,
                Permissions.RequestProductCatalog,
                Permissions.RequestDepartmentSummary,
                Permissions.RequestAdminApproval,
                Permissions.LibraryClass,
                Permissions.LibraryCategory,
                Permissions.LibraryItem,
                Permissions.LibrarySupplier,
                Permissions.LibraryPrice,
                Permissions.LibraryPriceList,
                Permissions.LibraryDepartment,
                Permissions.ReportView),
            [ProcurementAdmin.GroupId] = Explicit(
                Permissions.MenuDashboard,
                Permissions.MenuLibrary,
                Permissions.MenuReport,
                Permissions.RequestOrder,
                Permissions.RequestHistory,
                Permissions.RequestProductCatalog,
                Permissions.RequestDepartmentSummary,
                Permissions.RequestAllOrdersSummary,
                Permissions.LibraryClass,
                Permissions.LibraryCategory,
                Permissions.LibraryItem,
                Permissions.LibrarySupplier,
                Permissions.LibraryPrice,
                Permissions.LibraryPriceList,
                Permissions.LibraryDepartment,
                Permissions.ReportView,
                Permissions.PeriodSettle),
            [SystemAdmin.GroupId] = Explicit(
                Permissions.MenuDashboard,
                Permissions.MenuLibrary,
                Permissions.MenuReport,
                Permissions.MenuPermission,
                Permissions.RequestOrder,
                Permissions.RequestHistory,
                Permissions.RequestProductCatalog,
                Permissions.LibraryClass,
                Permissions.LibraryCategory,
                Permissions.LibraryItem,
                Permissions.LibrarySupplier,
                Permissions.LibraryPrice,
                Permissions.LibraryPriceList,
                Permissions.LibraryDepartment,
                Permissions.ReportView,
                Permissions.PermissionUser,
                Permissions.PermissionComponent)
        };

    public static IReadOnlyList<string> GetActionPermissions(Guid groupId) =>
        ActionMatrix.TryGetValue(groupId, out var permissions)
            ? permissions
            : Array.Empty<string>();

    public static IReadOnlyList<string> GetUiComponents(Guid groupId) =>
        UiMatrix.TryGetValue(groupId, out var components)
            ? components
            : Array.Empty<string>();

    public static IReadOnlyList<string> GetAllSeedComponents(Guid groupId) =>
        GetActionPermissions(groupId)
            .Concat(GetUiComponents(groupId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static bool HasAction(Guid groupId, string permissionCode) =>
        GetActionPermissions(groupId).Contains(permissionCode, StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyList<string> Explicit(params string[] codes) =>
        Array.AsReadOnly(codes);
}

/// <summary>
/// Stable IDs for action components and their page mappings. Existing legacy
/// UI IDs stay unchanged; new action IDs are derived from a fixed namespace.
/// </summary>
public static class CanonicalRbacSeedIds
{
    public static Guid ActionComponent(string permissionCode) =>
        Create("ACTION_COMPONENT", permissionCode);

    public static Guid ActionPageMapping(string permissionCode) =>
        Create("ACTION_PAGE_MAPPING", permissionCode);

    private static Guid Create(string kind, string code)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes($"GTAS_VPP|RBAC_V1|{kind}|{code.ToUpperInvariant()}"));
        return new Guid(bytes.AsSpan(0, 16));
    }
}
