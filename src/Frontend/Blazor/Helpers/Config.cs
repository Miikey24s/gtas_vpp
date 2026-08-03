namespace gtas_vpp_fe.Helpers
{
    public class Config
    {
        // Cấu hình HTTP client.
        public const string HttpClientName = "VPP_API";

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
