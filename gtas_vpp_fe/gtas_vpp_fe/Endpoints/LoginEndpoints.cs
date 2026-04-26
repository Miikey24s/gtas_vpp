using System.Security.Claims;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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

                var (loginData, server, rememberMe) = data.Value;

                var claims = loginData.sp_AuthenticationLogin_To_Claims();
                claims.Add(new Claim(ClaimKeys.Server, server));

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    AllowRefresh = true,
                };

                if (rememberMe)
                {
                    authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(Config.AuthPropertyExpireHours);
                }

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    authProperties);

                if (!string.IsNullOrWhiteSpace(returnUrl))
                {
                    return Results.Redirect(returnUrl);
                }

                return Results.Redirect("/");
            });
        }
    }
}
