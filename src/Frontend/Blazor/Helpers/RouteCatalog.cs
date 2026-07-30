using gtas_vpp_shared.Constants;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Catalog nguồn sự thật của mọi route FE cho audit, test và tài liệu.
///
/// Phản chiếu:
///   - chỉ thị <c>@page</c> trong <c>Components/Pages/**</c>.
///   - Điều hướng sidebar trong <c>LeftSidebar.razor</c>.
///   - điều hướng tab/tab con trong <c>Component_VPPRequest</c>, <c>Component_Library</c>,
///     <c>Component_Permission</c>.
///
/// Cập nhật file này mỗi khi thêm/xóa route, tab hoặc sub-tab để audit agent
/// (Playwright + accessibility scanner) có thể suy ra tập route duy nhất mà không
/// phải crawl lại source Razor.
///
/// Class này chỉ chứa metadata, không thay đổi hành vi runtime.
/// </summary>
public static class RouteCatalog
{
    /// <summary>
    /// Entry route logic. Mỗi audit target duy nhất có một <see cref="Route"/>.
    /// Mỗi tổ hợp tab và sub-tab có entry riêng.
    /// </summary>
    /// <param name="Key">Khóa duy nhất ổn định theo snake/dot case.</param>
    /// <param name="Path">Đường dẫn điều hướng, gồm cả query parameter.</param>
    /// <param name="Title">Localization key dùng làm tiêu đề route trên UI.</param>
    /// <param name="PageCode">Mã page backend, xem <see cref="Config.Page_ComponentCode.PageCode"/>.</param>
    /// <param name="AnyOfPermissions">User phải có ít nhất một quyền để truy cập route.</param>
    /// <param name="IsDynamic">True nếu path chứa placeholder mẫu tại runtime.</param>
    /// <param name="IsAuthenticated">False với route anonymous như login, error, not-found.</param>
    /// <param name="Notes">Ghi chú tự do cho auditor.</param>
    public sealed record Route(
        string Key,
        string Path,
        string Title,
        string PageCode,
        string[] AnyOfPermissions,
        bool IsDynamic = false,
        bool IsAuthenticated = true,
        string? Notes = null);

    /// <summary>
    /// Mọi route logic cần xác thực. Mỗi entry là một tổ hợp tab/sub-tab.
    /// Audit agent duyệt danh sách, khử trùng theo <see cref="Route.Key"/> và truy cập <see cref="Route.Path"/>.
    /// </summary>
    public static readonly Route[] Authenticated =
    [
        new(
            Key: "account.change-password",
            Path: "/Account/ChangePassword",
            Title: "ChangePasswordTitle",
            PageCode: "",
            AnyOfPermissions: [],
            Notes: "Authenticated self-service route exposed from UserMenu."),

        new(
            Key: "account.change-password.required",
            Path: "/Account/ChangePassword?required=1",
            Title: "ChangePasswordTitle",
            PageCode: "",
            AnyOfPermissions: [],
            Notes: "Mandatory password-change state after login."),

        // ── Bảng điều khiển ────────────────────────────────────────
        new(
            Key: "dashboard.my-orders",
            Path: "/dashboard?tab=0",
            Title: "MyOrders",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestOrder]),

        new(
            Key: "dashboard.my-orders.current",
            Path: "/dashboard?tab=0&orderView=current",
            Title: "MyOrders",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestOrder]),

        new(
            Key: "dashboard.my-orders.supplement",
            Path: "/dashboard?tab=0&orderView=supplement",
            Title: "MyOrders",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestOrder]),

        new(
            Key: "dashboard.my-orders.previous",
            Path: "/dashboard?tab=0&orderView=previous",
            Title: "MyOrders",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestOrder]),

        new(
            Key: "dashboard.history",
            Path: "/dashboard?tab=1",
            Title: "History",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestHistory]),

        new(
            Key: "dashboard.catalog",
            Path: "/dashboard?tab=2",
            Title: "Catalog",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestProductCatalog]),

        new(
            Key: "dashboard.management.department",
            Path: "/dashboard?tab=3&managementTab=department",
            Title: "DepartmentSummary",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestDepartmentSummary]),

        new(
            Key: "dashboard.management.all",
            Path: "/dashboard?tab=3&managementTab=all",
            Title: "LegacyAllOrdersRedirect",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestAllOrdersSummary],
            Notes: "Legacy URL only. Component_VPPRequest redirects to the unified period review workspace; no standalone UI consumer remains."),

        new(
            Key: "dashboard.period-operations",
            Path: "/dashboard?tab=5",
            Title: "PeriodOperations",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestAdminApproval, Permissions.PeriodSettle],
            Notes: "Unified period settlement workspace (PeriodSettle) plus additional-approval queue (RequestAdminApproval). Legacy review/demand/supply/settle query values resolve to the unified workspace."),

        new(
            Key: "dashboard.period.pending-approval",
            Path: "/dashboard?tab=5&periodTab=pending",
            Title: "PendingApproval",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestAdminApproval]),

        new(
            Key: "dashboard.period.review",
            Path: "/dashboard?tab=5&periodTab=review",
            Title: "PeriodReview",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.PeriodSettle]),

        new(
            Key: "dashboard.period.demand",
            Path: "/dashboard?tab=5&periodTab=demand",
            Title: "PeriodDemandTitle",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.PeriodSettle]),

        new(
            Key: "dashboard.period.supply",
            Path: "/dashboard?tab=5&periodTab=supply",
            Title: "PeriodSupplyTitle",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.PeriodSettle]),

        new(
            Key: "dashboard.period.settle",
            Path: "/dashboard?tab=5&periodTab=settle",
            Title: "PeriodSettlement",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.PeriodSettle]),

        // ── Tạo / sửa đơn (dynamic) ────────────────────────────────
        new(
            Key: "dashboard.order-create.new",
            Path: "/dashboard/order-create",
            Title: "OrderCreate",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestOrder]),

        new(
            Key: "dashboard.order-create.additional",
            Path: "/dashboard/order-create?isAdditional=true",
            Title: "OrderCreate",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestOrder]),

        new(
            Key: "dashboard.order-create.copy-previous",
            Path: "/dashboard/order-create?copyFrom=previous",
            Title: "OrderCreate",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestOrder]),

        new(
            Key: "dashboard.order-create.edit",
            Path: "/dashboard/order-create?orderId={SAMPLE_ORDER_ID}",
            Title: "OrderEdit",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestOrder],
            IsDynamic: true,
            Notes: "Replace {SAMPLE_ORDER_ID} with the first editable order id available to the test user."),

        new(
            Key: "dashboard.order-create.recreate",
            Path: "/dashboard/order-create?orderId={SAMPLE_CANCELLED_ORDER_ID}&mode=recreate",
            Title: "RecreateOrder",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestOrder],
            IsDynamic: true,
            Notes: "Replace {SAMPLE_CANCELLED_ORDER_ID} with a current cancelled order owned by the test user."),

        // ── Danh mục ───────────────────────────────────────────────
        new(
            Key: "library.classes",
            Path: "/library?tab=0",
            Title: "ClassDefinitions",
            PageCode: Config.Page_ComponentCode.PageCode.Library,
            AnyOfPermissions: [Permissions.LibraryClass]),

        new(
            Key: "library.categories",
            Path: "/library?tab=1",
            Title: "OperationCategories",
            PageCode: Config.Page_ComponentCode.PageCode.Library,
            AnyOfPermissions: [Permissions.LibraryCategory]),

        new(
            Key: "library.items",
            Path: "/library?tab=2",
            Title: "Operations",
            PageCode: Config.Page_ComponentCode.PageCode.Library,
            AnyOfPermissions: [Permissions.LibraryItem]),

        new(
            Key: "library.suppliers",
            Path: "/library?tab=3",
            Title: "Suppliers",
            PageCode: Config.Page_ComponentCode.PageCode.Library,
            AnyOfPermissions: [Permissions.LibrarySupplier]),

        new(
            Key: "library.departments",
            Path: "/library?tab=5",
            Title: "Departments",
            PageCode: Config.Page_ComponentCode.PageCode.Library,
            AnyOfPermissions: [Permissions.LibraryDepartment]),

        new(
            Key: "library.pricing.price-lists",
            Path: "/library?tab=6&pricingTab=price-lists",
            Title: "PriceLists",
            PageCode: Config.Page_ComponentCode.PageCode.Library,
            AnyOfPermissions: [Permissions.LibraryPriceList]),

        new(
            Key: "library.pricing.prices",
            Path: "/library?tab=6&pricingTab=prices",
            Title: "Prices",
            PageCode: Config.Page_ComponentCode.PageCode.Library,
            AnyOfPermissions: [Permissions.LibraryPrice],
            Notes: "Legacy ?tab=4 redirects to this view."),

        new(
            Key: "library.pricing.prices.selected-list",
            Path: "/library?tab=6&pricingTab=prices&priceListId={SAMPLE_PRICE_LIST_ID}",
            Title: "Prices",
            PageCode: Config.Page_ComponentCode.PageCode.Library,
            AnyOfPermissions: [Permissions.LibraryPrice],
            IsDynamic: true,
            Notes: "Replace {SAMPLE_PRICE_LIST_ID} with a price list visible to the test user."),

        // ── Phân quyền ─────────────────────────────────────────────
        new(
            Key: "permission.user",
            Path: "/permission?tab=0",
            Title: "PermissionUser",
            PageCode: Config.Page_ComponentCode.PageCode.Permission,
            AnyOfPermissions: [Permissions.PermissionUser]),

        new(
            Key: "permission.component",
            Path: "/permission?tab=1",
            Title: "PermissionComponent",
            PageCode: Config.Page_ComponentCode.PageCode.Permission,
            AnyOfPermissions: [Permissions.PermissionComponent],
            Notes: "Page-permission tab nests per-group expansion rows; each expanded row contains nested page-permission tabs."),

        new(
            Key: "permission.security-audit",
            Path: "/permission?tab=2",
            Title: "SecurityAudit",
            PageCode: Config.Page_ComponentCode.PageCode.Permission,
            AnyOfPermissions: [Permissions.PermissionManage],
            Notes: "Read-only security audit workspace for access administrators."),

        // ── Báo cáo ────────────────────────────────────────────────
        new(
            Key: "report",
            Path: "/report",
            Title: "Reports",
            PageCode: Config.Page_ComponentCode.PageCode.Report,
            AnyOfPermissions: [],
            Notes: "Page-level access only; no per-component permission required."),
    ];

    /// <summary>
    /// Route anonymous được liệt kê để đầy đủ, không thuộc phạm vi audit mặc định.
    /// </summary>
    public static readonly Route[] Anonymous =
    [
        new(
            Key: "home",
            Path: "/",
            Title: "Home",
            PageCode: "",
            AnyOfPermissions: [],
            IsAuthenticated: false,
            Notes: "Redirects authenticated users to first accessible route; otherwise to login."),

        new(
            Key: "login",
            Path: Config.LoginPagePath,
            Title: "Login",
            PageCode: "",
            AnyOfPermissions: [],
            IsAuthenticated: false),

        new(
            Key: "account.forgot-password",
            Path: "/Account/ForgotPassword",
            Title: "ForgotPasswordTitle",
            PageCode: "",
            AnyOfPermissions: [],
            IsAuthenticated: false),

        new(
            Key: "account.reset-password",
            Path: "/Account/ResetPassword",
            Title: "ResetPasswordTitle",
            PageCode: "",
            AnyOfPermissions: [],
            IsAuthenticated: false),

        new(
            Key: "account.register",
            Path: "/Account/Register",
            Title: "RegisterTitle",
            PageCode: "",
            AnyOfPermissions: [],
            IsAuthenticated: false),

        new(
            Key: "account.confirm-email",
            Path: "/Account/ConfirmEmail?userId={SAMPLE_USER_ID}&token={SAMPLE_TOKEN}",
            Title: "ConfirmEmailTitle",
            PageCode: "",
            AnyOfPermissions: [],
            IsDynamic: true,
            IsAuthenticated: false,
            Notes: "Process route; use only a disposable TEST confirmation token."),

        new(
            Key: "login-process",
            Path: Config.LoginProcessPath,
            Title: "LoginProcess",
            PageCode: "",
            AnyOfPermissions: [],
            IsAuthenticated: false),

        new(
            Key: "logout-process",
            Path: Config.LogoutProcessPath,
            Title: "LogoutProcess",
            PageCode: "",
            AnyOfPermissions: [],
            IsAuthenticated: false),

        new(
            Key: "not-found",
            Path: "/not-found",
            Title: "NotFound",
            PageCode: "",
            AnyOfPermissions: [],
            IsAuthenticated: false),

        new(
            Key: "error",
            Path: "/Error",
            Title: "Error",
            PageCode: "",
            AnyOfPermissions: [],
            IsAuthenticated: false),
    ];

    /// <summary>
    /// Query parameter làm thay đổi view logic; khóa duy nhất của audit có chứa các giá trị này.
    /// </summary>
    public static readonly string[] NavigationQueryParams =
    [
        "tab",
        "managementTab",
        "periodTab",
        "pricingTab",
        "orderView",
        "orderId",
        "isAdditional",
        "copyFrom",
        "mode",
        "priceListId",
        "required"
    ];

    /// <summary>
    /// Query parameter KHÔNG làm đổi khóa duy nhất của audit, ví dụ filter và paging.
    /// </summary>
    public static readonly string[] FilterQueryParams = ["search", "page", "sort", "fromDate", "toDate"];
}
