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
        public const string ApiCatalogItems = "/api/catalog/items";
        public const string ApiVppBase = "/api/VPPRequest";

        public static class VppApi
        {
            public const string ApiVppBase = "/api/VPPRequest";
            public const string Categories = $"{ApiVppBase}/categories";
            public const string Products = $"{ApiVppBase}/products";
            public const string MyOrders = $"{ApiVppBase}/my-orders";
            public const string MyOrdersSummary = $"{ApiVppBase}/my-orders-summary";
            public const string MyOrderHistory = $"{ApiVppBase}/my-order-history";
            public const string MyOrderHistorySummary = $"{ApiVppBase}/my-order-history-summary";
            public const string DepartmentOrders = $"{ApiVppBase}/department-orders";
            public const string AllOrders = $"{ApiVppBase}/all-orders";
            public const string Orders = $"{ApiVppBase}/orders";
            public const string PeriodInfo = $"{ApiVppBase}/period-info";
            public const string PreviousItems = $"{ApiVppBase}/orders/previous-items";
            public const string PendingAdditional = $"{ApiVppBase}/additional-orders/pending";
        }

        public static class LibraryApi
        {
            public const string LookupCategories = $"{ApiLibraryBase}/lookup-categories";
            public const string LookupValues = $"{ApiLibraryBase}/lookup-values";
            public const string VppCategories = $"{ApiLibraryBase}/vpp-categories";
            public const string VppItems = $"{ApiLibraryBase}/vpp-items";
            public const string Suppliers = $"{ApiLibraryBase}/suppliers";
            public const string SupplierProductMappings = $"{ApiLibraryBase}/supplier-product-mappings";
            public const string Departments = $"{ApiLibraryBase}/departments";
            public const string VPPPriceBase = $"{ApiBase}/vppprice";
            public const string VPPPrice_ByVpp = $"{VPPPriceBase}/by-vpp";
            public const string VPPPrice_BySupplier = $"{VPPPriceBase}/by-supplier";
            public const string VPPPrice_ItemPrices = $"{VPPPriceBase}/item-prices";
            public const string VPPPrice_SetDefault = $"{VPPPriceBase}/{{0}}/set-default";
            public const string PriceList = $"{ApiBase}/vpppricelist";
            public const string PriceList_SetDefault = $"{PriceList}/{{0}}/set-default";
            public const string PriceList_Clone = $"{PriceList}/clone";
            public const string PriceList_Publish = $"{PriceList}/{{0}}/publish";
            public const string PriceList_Expire = $"{PriceList}/{{0}}/expire";
            public const string PriceList_Compare = $"{PriceList}/compare";
        }

        public static class RequestApi
        {
            public static class PeriodSettlement
            {
                public const string Base = $"{ApiBase}/periodsettlement";
                public const string Settle = $"{Base}/settle";
                public const string Preview = $"{Base}/preview";
                public const string Confirm = $"{Base}/confirm";
                public const string Current = $"{Base}/current/{{0}}/{{1}}";
                public const string Correct = $"{Base}/{{0}}/correct";
                public const string Status = $"{Base}/{{0}}/{{1}}";
                public const string ListAll = Base;
            }
        }

        // Routes
        public const string LoginPagePath = "/Account/Login";
        public const string LoginProcessPath = "/loginprocess";
        public const string LogoutProcessPath = "/logoutprocess";
        public const string PerformLogoutPath = "/perform-logout";

        // Cookie
        // ENV-001 changes the backend JWT audience. Version the cookie name so an
        // existing frontend cookie cannot trap users with an access token that the
        // deployment-bound backend now correctly rejects.
        public const string CookieName = "VPP_AuthCookie_v3";
        public const int CookieExpireMinutes = 60;
        // Page codes for page permission payloads.
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
