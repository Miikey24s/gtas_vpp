namespace gtas_vpp_shared.Constants
{
    public static class Permissions
    {
        // Trình đơn.
        public const string MenuDashboard = "MENU_DASHBOARD";
        public const string MenuLibrary = "MENU_LIBRARY";
        public const string MenuPermission = "MENU_PERMISSION";
        public const string MenuReport = "MENU_REPORT";

        // Yêu cầu.
        public const string RequestOrder = "REQUEST_ORDER";
        public const string RequestHistory = "REQUEST_HISTORY";
        public const string RequestProductCatalog = "REQUEST_PRODUCT_CATALOG";
        public const string RequestDepartmentSummary = "REQUEST_DEPARTMENT_SUMMARY";
        public const string RequestAllOrdersSummary = "REQUEST_ALL_ORDERS_SUMMARY";
        public const string RequestAdminApproval = "REQUEST_ADMIN_APPROVAL";

        // Danh mục.
        public const string LibraryClass = "LIBRARY_CLASS";
        public const string LibraryCategory = "LIBRARY_CATEGORY";
        public const string LibraryItem = "LIBRARY_ITEM";
        public const string LibrarySupplier = "LIBRARY_SUPPLIER";
        public const string LibraryPrice = "LIBRARY_PRICE";
        public const string LibraryPriceList = "LIBRARY_PRICE_LIST";
        public const string LibraryDepartment = "LIBRARY_DEPARTMENT";

        // Chốt kỳ.
        public const string PeriodSettle = "PERIOD_SETTLE";
        public const string PeriodSettingsManage = "PERIOD_SETTINGS_MANAGE";

        // Phân quyền.
        public const string PermissionUser = "PERMISSION_USER";
        public const string PermissionComponent = "PERMISSION_COMPONENT";

        // Báo cáo.
        public const string ReportView = "REPORT_VIEW";

        // Quyền hành động backend phải được cấp tường minh; khả năng hiển thị
        // page/component legacy không bao giờ đồng nghĩa với quyền gọi API.
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
        /// Các hành động phân quyền backend. Mã component UI/menu nằm trong
        /// <see cref="UiComponentCodes"/> và không bao giờ được ngầm hiểu là quyền hành động.
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
                PeriodSettle,
                PeriodSettingsManage
            });

        /// <summary>
        /// Mã page/component legacy chỉ được giữ cho điều hướng và hiển thị UI.
        /// Các mã này chủ động bị loại khỏi đăng ký authorization policy.
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

        // Program.cs đăng ký một policy cho từng phần tử trong All. Alias này chỉ
        // chứa action để menu được nhìn thấy không thể biến thành quyền gọi API.
        public static IReadOnlyList<string> All => ActionCodes;

        private static readonly HashSet<string> ActionCodeSet =
            new(ActionCodes, StringComparer.OrdinalIgnoreCase);

        public static bool IsActionCode(string? code) =>
            !string.IsNullOrWhiteSpace(code) && ActionCodeSet.Contains(code);
    }
}
