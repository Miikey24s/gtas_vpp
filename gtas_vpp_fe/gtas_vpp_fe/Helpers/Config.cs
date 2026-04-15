namespace gtas_vpp_fe.Helpers
{
    public class Config
    {
        // HttpClient
        public const string HttpClientName = "VPP_API";
        public const string ApiLoginEndpoint = "/api/Auth/login";
        public const string ApiPermissionGroupsEndpoint = "/api/Permission/groups?getFullName=true";
        public const string ApiLibraryBase = "/api/Library";
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
                public const string Sidebar = "0001";
                public const string Dashboard = "0002";
                public const string Permission = "0004";
                public const string PageHaveAdminView = "0002";
            }

            public static class ComponentCode
            {
                public const string UserView = "0002_UV";
                public const string LibraryClass = "0001_LIB_C";
                public const string LibraryOperation = "0001_LIB_O";
                public const string PurchaseConsumption = "0001_PUR";
                public const string BuyerConsumption = "0001_BUY";
                public const string Report = "0001_R";
                public const string Setting = "0001_S";
                public const string LibraryOperationCategory = "0001_LIB_OC";
                public const string LibraryRoute = "0001_LIB_R";
                public const string ActualConsumption = "0001_ACT";
                public const string LibraryEquipment = "0001_LIB_E";
                public const string HomeDashboard = "0001_HD";
                public const string AdminView = "0002_ADM";
                public const string PermissionViewable = "0004_V";
            }
            /*
Config.Page_ComponentCode.ComponentCode.HomeDashboard
            */
        }
    }
}
