using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Radzen;
using Serilog;
using System.Net.Http.Json;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Authen
{
    public partial class Login
    {
        [Parameter] public string? username { get; set; }
        [Parameter] public string? password { get; set; }
        [Parameter] public string? isremember { get; set; }
        [Parameter] public string? server { get; set; }
        [Inject] public IHttpContextAccessor? HttpContextAccessor { get; set; }
        [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var rs = await LoginAccess();
            if (rs)
            {
                UriHelper.NavigateTo("/", true);
            }
            else
            {
                UriHelper.NavigateTo(Config.LoginPagePath, true);
            }
        }
        protected async Task<bool> LoginAccess()
        {
            string errorMessage = string.Empty;
            bool isLogin;
            if (!string.IsNullOrEmpty(username)
                && !string.IsNullOrEmpty(password)
                && !string.IsNullOrEmpty(isremember)
                && !string.IsNullOrEmpty(server))
            {
                try
                {
                    var sp_Authentication_Login = await DoLogin(username, password, server);
                    if (sp_Authentication_Login is not null)
                    {
                        isLogin = true;
                        await DoSignInAsync(sp_Authentication_Login, server);
                    }
                    else
                    {
                        isLogin = false;
                    }
                }
                catch (Exception ex)
                {
                    isLogin = false;
                    errorMessage = ex.Message;
                    Log.Error(ex, "Lỗi khi login, gọi ef sp_Authentication_Login với {username}", username);
                }
            }
            else
            {
                isLogin = false;
                ShowError(errorMessage);
            }
            return isLogin;
        }
        private async Task<sp_Authentication_Login?> DoLogin(string username, string password, string server)
        {
            var client = HttpClientFactory.CreateClient(Config.HttpClientName);
            var response = await client.PostAsJsonAsync(Config.ApiLoginEndpoint, new
            {
                Username = Uri.UnescapeDataString(username),
                Password = Uri.UnescapeDataString(password),
                Server = server
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                ShowError(errorMsg);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<sp_Authentication_Login>();
        }
        private async Task DoSignInAsync(sp_Authentication_Login sp_Authentication_Login, string server)
        {
            var claims = sp_Authentication_Login.sp_AuthenticationLogin_To_Claims();
            claims.Add(new Claim(ClaimKeys.Server, server));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                RedirectUri = HttpContextAccessor!.HttpContext!.Request.Host.Value,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                authProperties);
        }
        private void ShowError(string detail)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Login failed",
                Detail = detail,
                Duration = 8000
            });
        }
    }
}
