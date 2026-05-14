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
        public const int CookieExpireMinutes = 1440;
        public const int ClaimExpireHours = 24;
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
        // Page codes for sp_Authen_GetPermissionSinglePage payloads.
        // NB: Component-level permission codes were deduplicated with
        // gtas_vpp_shared.Constants.Permissions (P4.C / F-24). Use that
        // class directly instead of re-declaring REQUEST_* / PERMISSION_*
        // constants here.
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
            }
        }
    }
}
