namespace gtas_vpp_shared.Constants
{
    public static class Permissions
    {
        // Menus
        public const string MenuDashboard = "MENU_DASHBOARD";
        public const string MenuLibrary = "MENU_LIBRARY";
        public const string MenuPermission = "MENU_PERMISSION";
        public const string MenuReport = "MENU_REPORT";

        // Requests
        public const string RequestOrder = "REQUEST_ORDER";
        public const string RequestHistory = "REQUEST_HISTORY";
        public const string RequestProductCatalog = "REQUEST_PRODUCT_CATALOG";
        public const string RequestDepartmentSummary = "REQUEST_DEPARTMENT_SUMMARY";
        public const string RequestAllOrdersSummary = "REQUEST_ALL_ORDERS_SUMMARY";
        public const string RequestAdminApproval = "REQUEST_ADMIN_APPROVAL";

        // Library
        public const string LibraryClass = "LIBRARY_CLASS";
        public const string LibraryCategory = "LIBRARY_CATEGORY";
        public const string LibraryItem = "LIBRARY_ITEM";
        public const string LibrarySupplier = "LIBRARY_SUPPLIER";
        public const string LibraryPrice = "LIBRARY_PRICE";
        public const string LibraryPriceList = "LIBRARY_PRICE_LIST";
        public const string LibraryDepartment = "LIBRARY_DEPARTMENT";

        // Settlement
        public const string PeriodSettle = "PERIOD_SETTLE";

        // Permissions
        public const string PermissionUser = "PERMISSION_USER";
        public const string PermissionComponent = "PERMISSION_COMPONENT";

        // Reports
        public const string ReportView = "REPORT_VIEW";

        // Backend action permissions. These must be granted explicitly; legacy
        // page/component visibility never implies API authority.
        public const string RequestViewOwn = "REQUEST_VIEW_OWN";
        public const string RequestViewDepartment = "REQUEST_VIEW_DEPARTMENT";
        public const string RequestViewAll = "REQUEST_VIEW_ALL";
        public const string RequestCreate = "REQUEST_CREATE";
        public const string RequestUpdateOwn = "REQUEST_UPDATE_OWN";
        public const string RequestCancelOwn = "REQUEST_CANCEL_OWN";
        public const string RequestApprove = "REQUEST_APPROVE";
        public const string RequestReject = "REQUEST_REJECT";
        public const string RequestCatalogView = "REQUEST_CATALOG_VIEW";
        public const string LibraryView = "LIBRARY_VIEW";
        public const string LibraryManage = "LIBRARY_MANAGE";
        public const string PermissionView = "PERMISSION_VIEW";
        public const string PermissionManage = "PERMISSION_MANAGE";
        public const string ReportViewOwn = "REPORT_VIEW_OWN";
        public const string ReportViewDepartment = "REPORT_VIEW_DEPARTMENT";
        public const string ReportViewAll = "REPORT_VIEW_ALL";
        public const string ReportExport = "REPORT_EXPORT";

        /// <summary>
        /// Backend authorization actions. UI/menu component codes are kept in
        /// <see cref="UiComponentCodes"/> and must never imply an action.
        /// </summary>
        public static IReadOnlyList<string> ActionCodes { get; } = Array.AsReadOnly(
            new string[]
            {
                RequestViewOwn,
                RequestViewDepartment,
                RequestViewAll,
                RequestCreate,
                RequestUpdateOwn,
                RequestCancelOwn,
                RequestApprove,
                RequestReject,
                RequestCatalogView,
                LibraryView,
                LibraryManage,
                PermissionView,
                PermissionManage,
                ReportViewOwn,
                ReportViewDepartment,
                ReportViewAll,
                ReportExport,
                PeriodSettle
            });

        /// <summary>
        /// Legacy page/component codes retained only for navigation and UI
        /// visibility. They are deliberately excluded from authorization
        /// policy registration.
        /// </summary>
        public static IReadOnlyList<string> UiComponentCodes { get; } = Array.AsReadOnly(
            new string[]
            {
                MenuDashboard,
                MenuLibrary,
                MenuPermission,
                MenuReport,
                RequestOrder,
                RequestHistory,
                RequestProductCatalog,
                RequestDepartmentSummary,
                RequestAllOrdersSummary,
                RequestAdminApproval,
                LibraryClass,
                LibraryCategory,
                LibraryItem,
                LibrarySupplier,
                LibraryPrice,
                LibraryPriceList,
                LibraryDepartment,
                PermissionUser,
                PermissionComponent,
                ReportView
            });

        // Program.cs registers one policy for every entry in All. Keep this
        // alias action-only so a visible menu can never become an API grant.
        public static IReadOnlyList<string> All => ActionCodes;

        private static readonly HashSet<string> ActionCodeSet =
            new(ActionCodes, StringComparer.OrdinalIgnoreCase);

        public static bool IsActionCode(string? code) =>
            !string.IsNullOrWhiteSpace(code) && ActionCodeSet.Contains(code);
    }
}
