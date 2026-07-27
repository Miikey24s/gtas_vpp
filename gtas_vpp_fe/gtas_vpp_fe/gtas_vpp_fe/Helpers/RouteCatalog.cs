using gtas_vpp_shared.Constants;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Source-of-truth catalog of all FE routes for audit, testing, and documentation.
///
/// Mirrors:
///   - <c>@page</c> directives in Components/Pages/**.
///   - Sidebar navigation in <c>LeftSidebar.razor</c>.
///   - Tab/sub-tab routing in <c>Component_VPPRequest</c>, <c>Component_Library</c>,
///     <c>Component_Permission</c>.
///
/// Update this file whenever a route, tab, or sub-tab is added or removed so that
/// audit agents (Playwright + accessibility scanner) can derive the unique-route
/// set without re-crawling Razor source.
///
/// This class is metadata only. It does not change runtime behavior.
/// </summary>
public static class RouteCatalog
{
    /// <summary>
    /// Logical route entry. One <see cref="Route"/> per unique audit target.
    /// Tab and sub-tab combinations each get their own entry.
    /// </summary>
    /// <param name="Key">Stable uniqueness key (snake/dot case).</param>
    /// <param name="Path">Path to navigate to, including navigation query params.</param>
    /// <param name="Title">Localization key used in the UI for the route's title.</param>
    /// <param name="PageCode">Backend page code (see <see cref="Config.Page_ComponentCode.PageCode"/>).</param>
    /// <param name="AnyOfPermissions">User must have at least one of these to access the route.</param>
    /// <param name="IsDynamic">True if the path contains a runtime sample placeholder.</param>
    /// <param name="IsAuthenticated">False for anonymous routes (login, error, not-found).</param>
    /// <param name="Notes">Free-form notes for auditors.</param>
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
    /// All authenticated logical routes. Each entry is one tab/sub-tab combination.
    /// Audit agents iterate this list, dedupe by <see cref="Route.Key"/>, and visit <see cref="Route.Path"/>.
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

        // ── Dashboard ──────────────────────────────────────────────
        new(
            Key: "dashboard.my-orders",
            Path: "/dashboard?tab=0",
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
            Title: "AllOrdersSummary",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestAllOrdersSummary]),

        new(
            Key: "dashboard.period-operations",
            Path: "/dashboard?tab=5",
            Title: "PeriodOperations",
            PageCode: Config.Page_ComponentCode.PageCode.Dashboard,
            AnyOfPermissions: [Permissions.RequestAdminApproval, Permissions.PeriodSettle],
            Notes: "Internal sub-tabs: PeriodReviewPanel (PeriodSettle), additional-approval queue (RequestAdminApproval). Render depends on which permissions the user has."),

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

        // ── Order Create / Edit (dynamic) ──────────────────────────
        new(
            Key: "dashboard.order-create.new",
            Path: "/dashboard/order-create",
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
            Notes: "Replace {SAMPLE_ORDER_ID} with first order id available to the test user. Variants: ?copyFromOrderId=, ?additional=true."),

        // ── Library ────────────────────────────────────────────────
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

        // ── Permission ─────────────────────────────────────────────
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

        // ── Report ─────────────────────────────────────────────────
        new(
            Key: "report",
            Path: "/report",
            Title: "Reports",
            PageCode: Config.Page_ComponentCode.PageCode.Report,
            AnyOfPermissions: [],
            Notes: "Page-level access only; no per-component permission required."),
    ];

    /// <summary>
    /// Anonymous routes. Listed for completeness; not part of the default audit scope.
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
    /// Query parameters that change the logical view. Audit uniqueness key includes these.
    /// </summary>
    public static readonly string[] NavigationQueryParams = ["tab", "managementTab", "pricingTab"];

    /// <summary>
    /// Query parameters that DO NOT change the audit uniqueness key (filters, paging).
    /// </summary>
    public static readonly string[] FilterQueryParams = ["search", "page", "sort", "fromDate", "toDate"];
}
