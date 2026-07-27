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
/// Các persona phẳng và ổn định. GroupCode là danh tính bảo mật; GroupName chỉ là
/// tên hiển thị cho người dùng.
/// </summary>
public static class CanonicalRbac
{
    public const long DefaultMemberCompanyCode = 77500;

    public static RbacPersonaDefinition Employee { get; } = new(
        Guid.Parse("388C6C3A-2801-42DC-BFC0-8A7741264596"),
        "EMPLOYEE",
        "Nhân viên",
        "Nhân viên tạo và theo dõi đơn của chính mình, đồng thời tra cứu danh mục mặt hàng.");

    public static RbacPersonaDefinition Manager { get; } = new(
        Guid.Parse("E170FF76-F46E-460B-BC20-1D525E585DF8"),
        "MANAGER",
        "Quản lý",
        "Quản lý đơn phòng ban và toàn công ty, duyệt đơn bổ sung, thư viện, báo cáo và vận hành kỳ.");

    public static Guid LegacyProcurementAdminGroupId { get; } =
        Guid.Parse("2F048784-AFC2-4322-906F-D645B982BE49");

    public static RbacPersonaDefinition Dev { get; } = new(
        Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
        "DEV",
        "DEV",
        "Tài khoản kỹ thuật có toàn quyền phục vụ phát triển và kiểm thử.");

    // Alias tương thích giúp code hiện có và ID đã serialize vẫn dễ đọc trong khi
    // seed hợp nhất hai persona quản lý legacy.
    public static RbacPersonaDefinition DepartmentApprover => Manager;
    public static RbacPersonaDefinition ProcurementAdmin => Manager;
    public static RbacPersonaDefinition SystemAdmin => Dev;

    public static IReadOnlyList<RbacPersonaDefinition> Personas { get; } = Array.AsReadOnly(
        new RbacPersonaDefinition[]
        {
            Employee,
            Manager,
            Dev
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
            [Manager.GroupId] = Explicit(
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
                Permissions.PeriodSettle),
            [Dev.GroupId] = Actions
                .Select(action => action.PermissionCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray()
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
            [Manager.GroupId] = Explicit(
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
                Permissions.PeriodSettle),
            [Dev.GroupId] = Explicit(
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
/// ID ổn định cho action component và ánh xạ page. ID UI legacy giữ nguyên;
/// action ID mới được sinh từ namespace cố định.
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
