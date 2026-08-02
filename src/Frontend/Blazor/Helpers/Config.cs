namespace gtas_vpp_fe.Helpers
{
    public class Config
    {
        // Cấu hình HTTP client.
        public const string HttpClientName = "VPP_API";
        public const string ApiAccountChangePasswordEndpoint = "/api/account/password/change";
        public const string ApiAccountAdminActivateEndpoint = "/api/account/admin/activate";
        public const string ApiAccountAdminResetPasswordEndpoint = "/api/account/admin/reset-password";
        public const string ApiAccountAdminInviteEndpoint = "/api/account/admin/invite";
        public const string ApiAccountAdminCapabilitiesEndpoint = "/api/account/admin/capabilities";
        public const string ApiAccountAdminSendPasswordResetLinkEndpoint = "/api/account/admin/send-password-reset-link";
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
            public const string DepartmentOrderHistory = $"{ApiVppBase}/department-order-history";
            public const string DepartmentOrderHistorySummary = $"{ApiVppBase}/department-order-history-summary";
            public const string DepartmentOrders = $"{ApiVppBase}/department-orders";
            public const string AllOrders = $"{ApiVppBase}/all-orders";
            public const string Orders = $"{ApiVppBase}/orders";
            public const string PeriodInfo = $"{ApiVppBase}/period-info";
            public const string PeriodDemand = $"{ApiVppBase}/period-demand";
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
                public const string ExportPdf = $"{Base}/{{0}}/export.pdf";
                public const string ExportExcel = $"{Base}/{{0}}/export.xlsx";
                public const string Status = $"{Base}/{{0}}/{{1}}";
                public const string ListAll = Base;
            }
        }

        // Cấu hình route.
        public const string LoginPagePath = "/Account/Login";
        public const string LoginProcessPath = "/loginprocess";
        public const string LogoutProcessPath = "/logoutprocess";
        public const string PerformLogoutPath = "/perform-logout";

        // Cấu hình cookie.
        // ENV-001 thay đổi JWT audience của backend. Version hóa tên cookie để cookie
        // frontend cũ không giữ user trong access token mà backend theo deployment
        // hiện tại từ chối đúng cách.
        public const string CookieName = "VPP_AuthCookie_v3";
        public const int CookieExpireMinutes = 60;
        // Mã page cho payload phân quyền page.
        // Lưu ý: mã quyền cấp component đã được khử trùng với
        // gtas_vpp_shared.Constants.Permissions (P4.C / F-24). Dùng trực tiếp
        // class đó thay vì khai báo lại hằng REQUEST_* / PERMISSION_* tại đây.
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
