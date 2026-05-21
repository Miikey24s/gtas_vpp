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

        public static readonly string[] All =
        [
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
            PeriodSettle,
            PermissionUser,
            PermissionComponent,
            ReportView
        ];
    }
}
