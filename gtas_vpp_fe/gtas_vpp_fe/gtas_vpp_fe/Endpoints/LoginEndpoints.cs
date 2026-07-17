using System.Security.Claims;
using gtas_vpp_fe.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http.Headers;

namespace gtas_vpp_fe.Endpoints
{
    public static class LoginEndpoints
    {
        public static void MapLoginEndpoints(this WebApplication app)
        {
            app.MapGet("/perform-login", async (
                string id,
                string? returnUrl,
                LoginTicketCache cache,
                HttpContext context) =>
            {
                var data = cache.Get(id);
                if (data == null)
                {
                    return Results.Redirect("/Account/Login");
                }

                var (loginData, rememberMe) = data.Value;

                var claims = CreateAuthenticationClaims(loginData);

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                if (loginData.AccessTokenExpiresAtUtc is not DateTime tokenExpiry
                    || string.IsNullOrWhiteSpace(loginData.AccessToken))
                {
                    return Results.Redirect(Config.LoginPagePath);
                }

                var expiresUtc = new DateTimeOffset(tokenExpiry.ToUniversalTime());
                if (expiresUtc <= DateTimeOffset.UtcNow)
                {
                    return Results.Redirect(Config.LoginPagePath);
                }

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    AllowRefresh = false,
                    ExpiresUtc = expiresUtc
                };

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    authProperties);

                return Results.Redirect(GetSafeLocalReturnUrl(returnUrl));
            });

            app.MapGet("/perform-logout", async (
                IHttpClientFactory httpClientFactory,
                HttpContext context,
                ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("FrontendLogout");
                var accessToken = context.User.Claims.Get(ClaimKeys.AccessToken);
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    try
                    {
                        var client = httpClientFactory.CreateClient(Config.HttpClientName);
                        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Auth/logout");
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        using var response = await client.SendAsync(request, context.RequestAborted);
                        if (!response.IsSuccessStatusCode
                            && response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                        {
                            logger.LogWarning(
                                "Backend logout returned status {StatusCode}; frontend cookie will still be removed.",
                                response.StatusCode);
                        }
                    }
                    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
                    {
                        logger.LogWarning(
                            exception,
                            "Backend logout was unavailable; frontend cookie will still be removed.");
                    }
                }

                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect(Config.LoginPagePath);
            }).RequireAuthorization();

            app.MapGet("/set-language", (string culture, string? returnUrl, HttpContext context) =>
            {
                var safeCulture = string.Equals(culture, "en", StringComparison.OrdinalIgnoreCase)
                    ? "en"
                    : "vi";
                context.Response.Cookies.Append(
                    ".AspNetCore.Culture",
                    $"c={safeCulture}|uic={safeCulture}",
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/", IsEssential = true }
                );
                return Results.Redirect(GetSafeLocalReturnUrl(returnUrl));
            });
        }

        public static string GetSafeLocalReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return "/";
            }

            var candidate = returnUrl.Trim();
            return candidate.StartsWith("/", StringComparison.Ordinal)
                && !candidate.StartsWith("//", StringComparison.Ordinal)
                && !candidate.StartsWith("/\\", StringComparison.Ordinal)
                && Uri.TryCreate(candidate, UriKind.Relative, out _)
                    ? candidate
                    : "/";
        }

        public static List<Claim> CreateAuthenticationClaims(
            gtas_vpp_shared.DTOs.Res.Auth.sp_Authentication_Login loginData)
            => loginData.sp_AuthenticationLogin_To_Claims();
    }
}
