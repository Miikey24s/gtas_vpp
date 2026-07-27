namespace gtas_vpp_fe.Helpers;

public static class AccountLoginRedirectPolicy
{
    public const string RequiredPasswordChangeRoute = "/Account/ChangePassword?required=1";

    public static string? Resolve(bool mustChangePassword, string? requestedReturnUrl) =>
        mustChangePassword ? RequiredPasswordChangeRoute : requestedReturnUrl;
}
