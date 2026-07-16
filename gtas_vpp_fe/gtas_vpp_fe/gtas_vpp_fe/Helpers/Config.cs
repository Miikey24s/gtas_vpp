namespace gtas_vpp_fe.Helpers
{
    public class Config
    {
        // HttpClient
        public const string HttpClientName = "VPP_API";
        public const string ApiLoginEndpoint = "/api/Auth/login";
        public const string ApiAccountRegisterEndpoint = "/api/account/register";
        public const string ApiAccountConfirmEmailEndpoint = "/api/account/confirm-email";
        public const string ApiAccountRecoveryEndpoint = "/api/account/password/recovery";
        public const string ApiAccountResetPasswordEndpoint = "/api/account/password/reset";
        public const string ApiAccountChangePasswordEndpoint = "/api/account/password/change";
        public const string ApiAccountAdminActivateEndpoint = "/api/account/admin/activate";
        public const string ApiAccountAdminResetPasswordEndpoint = "/api/account/admin/reset-password";
        public const string ApiPermissionGroupsEndpoint = "/api/Permission/groups?getFullName=true";
        public const string ApiBase = "/api";
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
            public const string VPPPriceBase = $"{ApiBase}/vppprice";
            public const string VPPPrice_ByVpp = $"{VPPPriceBase}/by-vpp";
            public const string VPPPrice_BySupplier = $"{VPPPriceBase}/by-supplier";
            public const string VPPPrice_ItemPrices = $"{VPPPriceBase}/item-prices";
            public const string VPPPrice_SetDefault = $"{VPPPriceBase}/{{0}}/set-default";
            public const string L07_PriceList = $"{ApiBase}/vpppricelist";
            public const string L07_PriceList_SetDefault = $"{L07_PriceList}/{{0}}/set-default";
            public const string L07_PriceList_Clone = $"{L07_PriceList}/clone";
        }

        public static class RequestApi
        {
            public static class PeriodSettlement
            {
                public const string Base = $"{ApiBase}/periodsettlement";
                public const string Settle = $"{Base}/settle";
                public const string Status = $"{Base}/{{0}}/{{1}}";
                public const string ListAll = Base;
            }
        }

        // Routes
        public const string LoginPagePath = "/Account/Login";
        public const string LoginProcessPath = "/loginprocess";
        public const string LogoutProcessPath = "/logoutprocess";

        // Cookie
        // ENV-001 changes the backend JWT audience. Version the cookie name so an
        // existing frontend cookie cannot trap users with an access token that the
        // deployment-bound backend now correctly rejects.
        public const string CookieName = "VPP_AuthCookie_v3";
        public const int CookieExpireMinutes = 60;
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
