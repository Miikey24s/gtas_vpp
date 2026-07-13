using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Radzen;
using System.Net.Http.Json;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Authen
{
    public partial class LoginPage
    {
        [Inject] public IHttpContextAccessor? HttpContextAccessor { get; set; }
        [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] public LoginTicketCache TicketCache { get; set; } = default!;
        [Inject] public IWebHostEnvironment env { get; set; } = default!;
        [Inject] public IStringLocalizerFactory LocalizerFactory { get; set; } = default!;
        [SupplyParameterFromQuery(Name = "returnUrl")]
        public string? ReturnUrl { get; set; }

        public sp_Authentication_LoginReqDTO sp_Authentication_Login { get; set; } = new sp_Authentication_LoginReqDTO();
        bool isLoading = false;
        bool isShowPass = true;
        private List<string> Servers = new() { "Live" };
        public bool isShowServer { get; set; } = false;
        private bool hasSubmittedValidation;
        private bool usernameTouched;
        private bool passwordTouched;
        private string? UsernameValidationMessage { get; set; }
        private string? PasswordValidationMessage { get; set; }
        private string? ServerValidationMessage { get; set; }
        private bool HasUsernameValidation => !string.IsNullOrWhiteSpace(UsernameValidationMessage);
        private bool HasPasswordValidation => !string.IsNullOrWhiteSpace(PasswordValidationMessage);
        private bool HasServerValidation => !string.IsNullOrWhiteSpace(ServerValidationMessage);
        private IStringLocalizer ComponentLoc => LocalizerFactory.Create("Components.App", typeof(LoginPage).Assembly.GetName().Name!);

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            isShowServer = UriHelper.BaseUri.Contains("localhost");
            // Force off in production — even if localhost due to reverse proxy
            if (!env.IsDevelopment()) isShowServer = false;
            SetupServerEnv();
            sp_Authentication_Login.selected_server = isShowServer ? "Test" : "Live";

            if (HttpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                UriHelper.NavigateTo("/", true);
                return;
            }

            StateHasChanged();
        }

        private void SetupServerEnv()
        {
            Servers = new List<string> { "Test", "Live" };
        }

        private void TogglePasswordVisibility()
        {
            isShowPass = !isShowPass;
        }

        private void HandleUsernameInput(ChangeEventArgs _)
        {
            if (usernameTouched || hasSubmittedValidation)
            {
                ValidateUsername();
            }
        }

        private void HandleUsernameBlur(FocusEventArgs _)
        {
            usernameTouched = true;
            ValidateUsername();
        }

        private void HandlePasswordInput(ChangeEventArgs _)
        {
            if (passwordTouched || hasSubmittedValidation)
            {
                ValidatePassword();
            }
        }

        private void HandlePasswordBlur(FocusEventArgs _)
        {
            passwordTouched = true;
            ValidatePassword();
        }

        private void HandleServerChange()
        {
            ValidateServer();
        }

        private async Task LoginOnkeyup(KeyboardEventArgs e, sp_Authentication_LoginReqDTO loginReqDTO)
        {
            if (e.Code == "Enter" || e.Code == "NumpadEnter")
            {
                await LoginSubmit(loginReqDTO);
            }
        }

        public async Task LoginSubmit(sp_Authentication_LoginReqDTO loginReqDTO)
        {
            isLoading = true;
            try
            {
                if (!ValidateLoginForm())
                {
                    return;
                }

                if (!isShowServer)
                {
                    if (UriHelper.BaseUri.Contains("dev.") || UriHelper.BaseUri.Contains("localhost"))
                    {
                        loginReqDTO.selected_server = "Test";
                    }
                    else if (UriHelper.BaseUri.Contains("transport.") || UriHelper.BaseUri.Contains("annam.id.vn") || UriHelper.BaseUri.Contains("209.") || UriHelper.BaseUri.Contains("172.") || UriHelper.BaseUri.Contains("100.") || UriHelper.BaseUri.Contains("128.") || UriHelper.BaseUri.Contains("192.") || UriHelper.BaseUri.Contains("10.") || System.Text.RegularExpressions.Regex.IsMatch(UriHelper.BaseUri, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b"))
                    {
                        // Fallback to Live for production domains or any IP address (like DigitalOcean droplet IP)
                        loginReqDTO.selected_server = "Live";
                    }
                }

                if (loginReqDTO.Username is not { Length: > 0 } username
                    || loginReqDTO.Password is not { Length: > 0 } password
                    || loginReqDTO.selected_server is not { Length: > 0 } server)
                {
                    return;
                }

                var loginData = await DoLogin(username, password, server);
                if (loginData is null)
                {
                    return;
                }

                var ticketId = TicketCache.Add(loginData, server, loginReqDTO.isRememberPass);
                var redirectUrl = $"/perform-login?id={ticketId}";
                if (!string.IsNullOrWhiteSpace(ReturnUrl))
                {
                    redirectUrl += $"&returnUrl={Uri.EscapeDataString(ReturnUrl)}";
                }
                
                UriHelper.NavigateTo(redirectUrl, true);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task<sp_Authentication_Login?> DoLogin(string username, string password, string server)
        {
            var client = HttpClientFactory.CreateClient(Config.HttpClientName);
            try
            {
                var response = await client.PostAsJsonAsync(Config.ApiLoginEndpoint, new
                {
                    Username = username,
                    Password = password,
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
            catch (TaskCanceledException)
            {
                ShowError("Login request timed out. Please verify the API server is running and try again.");
                return null;
            }
            catch (HttpRequestException)
            {
                ShowError("Cannot connect to the authentication API. Verify the API server is running and ApiSettings:BaseUrl is correct, then try again.");
                return null;
            }
        }

        private void ShowError(string detail)
        {
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Login failed",
                Detail = detail,
                Duration = 8000
            });
        }

        private bool ValidateLoginForm()
        {
            hasSubmittedValidation = true;
            usernameTouched = true;
            passwordTouched = true;

            ValidateUsername();
            ValidatePassword();
            ValidateServer();

            return !HasUsernameValidation
                && !HasPasswordValidation
                && !HasServerValidation;
        }

        private void ValidateUsername()
        {
            UsernameValidationMessage = string.IsNullOrWhiteSpace(sp_Authentication_Login.Username)
                ? ComponentLoc["LoginUsernameRequired"].Value
                : null;
        }

        private void ValidatePassword()
        {
            PasswordValidationMessage = string.IsNullOrWhiteSpace(sp_Authentication_Login.Password)
                ? ComponentLoc["LoginPasswordRequired"].Value
                : null;
        }

        private void ValidateServer()
        {
            if (!isShowServer)
            {
                ServerValidationMessage = null;
                return;
            }

            ServerValidationMessage = string.IsNullOrWhiteSpace(sp_Authentication_Login.selected_server)
                ? ComponentLoc["LoginServerRequired"].Value
                : null;
        }
    }
}
