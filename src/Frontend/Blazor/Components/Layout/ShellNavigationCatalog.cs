using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_fe.Components.Layout;

/// <summary>
/// Một nguồn metadata cho sidebar và primary header. Route thật vẫn thuộc
/// <see cref="RouteCatalog"/>; catalog này chỉ bổ sung label, icon, nhóm hiển thị
/// và thứ tự fallback của shell.
/// </summary>
public static class ShellNavigationCatalog
{
    // ITEMS: Metadata của từng route hiển thị trong sidebar/header.
    public sealed record Item(
        string Key,
        string RouteKey,
        string LabelKey,
        string Icon,
        string Permission,
        string? ParentGroupKey = null,
        string[]? ActiveAliases = null)
    {
        public RouteCatalog.Route Route => RouteCatalog.GetRequired(RouteKey);

        public string Path => Route.Path;
    }

    // GROUPS: Nhóm con dùng để gom các item liên quan trong menu.
    public sealed record Group(string Key, string LabelKey, string Icon);

    // SECTIONS: Nhóm menu cấp cao, quyền truy cập và route mặc định.
    public sealed record Section(
        string Key,
        string LabelKey,
        string Icon,
        string? MenuPermission,
        IReadOnlyList<Item> Items,
        IReadOnlyList<string> DefaultRouteKeys);

    // GROUP DEFINITIONS: Các nhóm menu con có thể mở rộng.
    public static readonly Group PeriodOperations = new(
        "period-operations",
        "PeriodOperations",
        VppIcons.PeriodOperations);

    public static readonly Group Pricing = new(
        "pricing",
        "Pricing",
        VppIcons.Pricing);

    // ITEM DEFINITIONS: Các route được dùng bởi sidebar và header tabs.
    public static readonly Item MyOrders = new(
        "my-orders",
        "dashboard.my-orders",
        "MyOrders",
        VppIcons.NavMyOrders,
        Permissions.RequestOrder,
        ActiveAliases: ["dashboard/order-create"]);

    public static readonly Item History = new(
        "history",
        "dashboard.history",
        "History",
        VppIcons.History,
        Permissions.RequestHistory);

    public static readonly Item DepartmentSummary = new(
        "department-summary",
        "dashboard.management.department",
        "DepartmentSummary",
        VppIcons.Department,
        Permissions.RequestDepartmentSummary);

    public static readonly Item Catalog = new(
        "catalog",
        "dashboard.catalog",
        "Catalog",
        VppIcons.Catalog,
        Permissions.RequestProductCatalog);

    public static readonly Item PeriodReview = new(
        "period-review",
        "dashboard.period.review",
        "PeriodSettleStep",
        VppIcons.Review,
        Permissions.PeriodSettle,
        PeriodOperations.Key,
        ["periodTab=demand", "periodTab=supply", "periodTab=settle"]);

    public static readonly Item PeriodPolicy = new(
        "period-policy",
        "dashboard.period.periods",
        "PeriodPolicyNavigation",
        VppIcons.PeriodOperations,
        Permissions.PeriodSettle,
        PeriodOperations.Key);

    public static readonly Item PendingApproval = new(
        "pending-approval",
        "dashboard.period.pending-approval",
        "AdminApproval",
        VppIcons.NavApproval,
        Permissions.RequestAdminApproval,
        PeriodOperations.Key);

    public static readonly Item Classes = new(
        "classes",
        "library.classes",
        "ClassDefinitions",
        VppIcons.Schema,
        Permissions.LibraryClass);

    public static readonly Item Categories = new(
        "categories",
        "library.categories",
        "OperationCategories",
        VppIcons.Category,
        Permissions.LibraryCategory);

    public static readonly Item Items = new(
        "items",
        "library.items",
        "Operations",
        VppIcons.Item,
        Permissions.LibraryItem);

    public static readonly Item Suppliers = new(
        "suppliers",
        "library.suppliers",
        "Suppliers",
        VppIcons.Supplier,
        Permissions.LibrarySupplier);

    public static readonly Item PriceLists = new(
        "price-lists",
        "library.pricing.price-lists",
        "PriceLists",
        VppIcons.PriceList,
        Permissions.LibraryPriceList,
        Pricing.Key);

    public static readonly Item Prices = new(
        "prices",
        "library.pricing.prices",
        "Prices",
        VppIcons.Price,
        Permissions.LibraryPrice,
        Pricing.Key,
        ["tab=4"]);

    public static readonly Item Departments = new(
        "departments",
        "library.departments",
        "Departments",
        VppIcons.Apartment,
        Permissions.LibraryDepartment);

    public static readonly Item Reports = new(
        "reports",
        "report",
        "Reports",
        VppIcons.Reports,
        string.Empty);

    public static readonly Item Users = new(
        "users",
        "permission.user",
        "Users",
        VppIcons.Group,
        Permissions.PermissionUser);

    public static readonly Item GroupsAndPermissions = new(
        "groups-and-permissions",
        "permission.component",
        "GroupsAndPermissions",
        VppIcons.Rule,
        Permissions.PermissionComponent);

    public static readonly Item SecurityAudit = new(
        "security-audit",
        "permission.security-audit",
        "SecurityAudit",
        VppIcons.History,
        Permissions.PermissionManage);

    public static readonly Item OrderPeriodSettings = new(
        "order-period-settings",
        "permission.order-period-settings",
        "OrderPeriodSettings",
        VppIcons.PeriodOperations,
        Permissions.PeriodSettingsManage);

    // SECTION DEFINITIONS: Các nhóm menu cấp cao của ứng dụng.
    public static readonly Section Dashboard = new(
        "dashboard",
        "Dashboard",
        VppIcons.Dashboard,
        Permissions.MenuDashboard,
        [MyOrders, History, DepartmentSummary, Catalog, PeriodPolicy, PeriodReview, PendingApproval],
        [
            MyOrders.RouteKey,
            History.RouteKey,
            Catalog.RouteKey,
            DepartmentSummary.RouteKey,
            PeriodPolicy.RouteKey,
            PeriodReview.RouteKey,
            PendingApproval.RouteKey
        ]);

    public static readonly Section Library = new(
        "library",
        "Library",
        VppIcons.Folder,
        Permissions.MenuLibrary,
        [Classes, Categories, Items, Suppliers, PriceLists, Prices, Departments],
        [
            Classes.RouteKey,
            Categories.RouteKey,
            Items.RouteKey,
            Suppliers.RouteKey,
            PriceLists.RouteKey,
            Prices.RouteKey,
            Departments.RouteKey
        ]);

    public static readonly Section Report = new(
        "report",
        "Reports",
        VppIcons.Reports,
        null,
        [Reports],
        [Reports.RouteKey]);

    public static readonly Section Permission = new(
        "permission",
        "SystemAdministration",
        VppIcons.Permissions,
        Permissions.MenuPermission,
        [Users, GroupsAndPermissions, SecurityAudit, OrderPeriodSettings],
        [Users.RouteKey, GroupsAndPermissions.RouteKey, SecurityAudit.RouteKey, OrderPeriodSettings.RouteKey]);

    // CATALOG: Thứ tự section dùng khi render navigation.
    public static IReadOnlyList<Section> Sections { get; } =
        [Dashboard, Library, Report, Permission];
}
