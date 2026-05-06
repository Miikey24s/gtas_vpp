namespace gtas_vpp_fe.Helpers
{
    public class Config
    {
        // HttpClient
        public const string HttpClientName = "VPP_API";
        public const string ApiLoginEndpoint = "/api/Auth/login";
        public const string ApiPermissionGroupsEndpoint = "/api/Permission/groups?getFullName=true";
        public const string ApiLibraryBase = "/api/Library";
        public const string ApiVppBase = "/api/VPPRequest";
        
        public static class VppApi
        {
            public const string ApiVppBase = "/api/VPPRequest";
            public const string Categories = $"{ApiVppBase}/categories";
            public const string Products = $"{ApiVppBase}/products";
            public const string MyOrders = $"{ApiVppBase}/my-orders";
            public const string MyOrdersSummary = $"{ApiVppBase}/my-orders-summary";
            public const string DepartmentOrders = $"{ApiVppBase}/department-orders";
            public const string AllOrders = $"{ApiVppBase}/all-orders";
            public const string Orders = $"{ApiVppBase}/orders";
            public const string PeriodInfo = $"{ApiVppBase}/period-info";
            public const string PreviousItems = $"{ApiVppBase}/orders/previous-items";
            public const string PendingAdditional = $"{ApiVppBase}/additional-orders/pending";
        }

        public static class LibraryApi
        {
            public const string L01_Class = $"{ApiLibraryBase}/l01";
            public const string L02_ClassDetail = $"{ApiLibraryBase}/l02";
            public const string L03_Category = $"{ApiLibraryBase}/l03";
            public const string L04_Item = $"{ApiLibraryBase}/l04";
            public const string L05_Supplier = $"{ApiLibraryBase}/l05";
            public const string L06_SupplierMapping = $"{ApiLibraryBase}/l06";
        }

        // Routes
        public const string LoginPagePath = "/Account/Login";
        public const string LoginProcessPath = "/loginprocess";
        public const string LogoutProcessPath = "/logoutprocess";

        // Cookie
        public const string CookieName = "VPP_AuthCookie";
        public const int CookieExpireMinutes = 30;
        public const int ClaimExpireHours = 12;
        public const int AuthPropertyExpireHours = 24;
        public static class sp_AuthenClass
        {
            public enum sp_Authen
            {
                sp_Authen
            }
            public enum sp_Authen_Type
            {
                sp_Authen_Login,
                sp_Authen_GetPermissionSinglePage,
                sp_Authen_TabUser_UserList,
                sp_Authen_TabUser_SearchUser,
                sp_Authen_Permission_GetPageWithComponentByGroupId,
                sp_Authen_CreateNewGroup,
                sp_Authen_CopyFromGroup
            }
        }
        public static class Page_ComponentCode
        {
            public static class PageCode
            {
                public const string Sidebar = "SIDEBAR";
                public const string Dashboard = "DASHBOARD";
                public const string Library = "LIBRARY";
                public const string Report = "REPORT";
                public const string Permission = "PERMISSION";
                public const string PageHaveAdminView = "PERMISSION";
                public const string VPPRequest = "DASHBOARD";
                public const string AI = "AI";
            }

            public static class ComponentCode
            {
                public const string UserView = "PERMISSION_USER";
                public const string LibraryClass = "LIBRARY_CLASS";
                public const string LibraryOperation = "LIBRARY_ITEM";
                public const string LibrarySupplier = "LIBRARY_SUPPLIER";
                public const string PurchaseConsumption = "MENU_DASHBOARD";
                public const string BuyerConsumption = "MENU_DASHBOARD";
                public const string Report = "REPORT_VIEW";
                public const string Setting = "MENU_PERMISSION";
                public const string LibraryOperationCategory = "LIBRARY_CATEGORY";
                public const string LibraryDepartment = "LIBRARY_DEPARTMENT";
                public const string LibraryRoute = "LIBRARY_ITEM";
                public const string ActualConsumption = "MENU_DASHBOARD";
                public const string LibraryEquipment = "LIBRARY_ITEM";
                public const string HomeDashboard = "MENU_DASHBOARD";
                public const string AdminView = "PERMISSION_USER";
                public const string PermissionViewable = "PERMISSION_COMPONENT";
                public const string RequestOrder = "REQUEST_ORDER";
                public const string RequestHistory = "REQUEST_HISTORY";
                public const string RequestProductCatalog = "REQUEST_PRODUCT_CATALOG";
                public const string RequestDepartmentSummary = "REQUEST_DEPARTMENT_SUMMARY";
                public const string RequestAllOrdersSummary = "REQUEST_ALL_ORDERS_SUMMARY";
                public const string RequestApproval = "REQUEST_ADMIN_APPROVAL";
                public const string RequestAIKeyManage = "AI_KEY_MANAGE";
                public const string RequestAIChat = "AI_CHAT";
                public const string RequestAIVppChat = "AI_VPP_CHAT";
                public const string AIToggle = "AI_TOGGLE";
            }
        }
    }
}
