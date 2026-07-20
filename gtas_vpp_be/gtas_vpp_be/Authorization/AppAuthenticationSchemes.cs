namespace gtas_vpp_be.Authorization;

public static class AppAuthenticationSchemes
{
    public const string Default = "AppAuthentication";
    public const string Cookie = "AppCookie";
    public const string SessionCookieName = "gtas-vpp-session";
    public const string AntiforgeryCookieName = "XSRF-TOKEN";
    public const string AntiforgeryHeaderName = "X-XSRF-TOKEN";
}
