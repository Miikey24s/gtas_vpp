namespace gtas_vpp_shared.Constants
{
    public static class Permissions
    {
        // Menus
        public const string MenuDashboard = "MENU_DASHBOARD";
        public const string MenuLibrary = "MENU_LIBRARY";
        public const string MenuPermission = "MENU_PERMISSION";
        public const string MenuReport = "MENU_REPORT";
        public const string MenuAI = "MENU_AI";

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
        public const string LibraryDepartment = "LIBRARY_DEPARTMENT";

        // Permissions
        public const string PermissionUser = "PERMISSION_USER";
        public const string PermissionComponent = "PERMISSION_COMPONENT";
        public const string RequestAIKeyManage = "AI_KEY_MANAGE";
        public const string RequestAIChat = "AI_CHAT";
        public const string RequestAIVppChat = "AI_VPP_CHAT";

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
            LibraryDepartment,
            PermissionUser,
            PermissionComponent,
            ReportView,
            MenuAI,
            RequestAIKeyManage,
            RequestAIChat,
            RequestAIVppChat
        ];
    }
}
