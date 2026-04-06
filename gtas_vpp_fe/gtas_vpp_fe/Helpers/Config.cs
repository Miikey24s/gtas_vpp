namespace gtas_vpp_fe.Helpers
{
    public class Config
    {
        // HttpClient
        public const string HttpClientName = "VPP_API";
        public const string ApiLoginEndpoint = "/api/Auth/login";

        // Routes
        public const string LoginPagePath = "/Account/Login";
        public const string LoginProcessPath = "/loginprocess";
        public const string LogoutProcessPath = "/logoutprocess";

        // Cookie
        public const string CookieName = "VPP_AuthCookie";
        public const int CookieExpireMinutes = 30;
        public const int ClaimExpireHours = 12;
        public const int AuthPropertyExpireHours = 24;

        // Claim keys
        public const string ClaimUserID = "UserID";
        public const string ClaimUserLogin = "UserLogin";
        public const string ClaimFullName = "FullName";
        public const string ClaimEmail = "Email";
        public const string ClaimIsAdmin = "IsAdmin";
        public const string ClaimGroupId = "GroupId";
        public const string ClaimGroupName = "GroupName";
        public const string ClaimMemberCompanyCode = "MemberCompanyCode";
        public const string ClaimMemberCompanyName = "MemberCompanyName";
        public const string ClaimMemberCompanyShortName = "MemberCompanyShortName";
        public const string ClaimExpired = "Expired";
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
    }

}
