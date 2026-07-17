using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Radzen;
using System.Net.Http.Json;

namespace gtas_vpp_fe.Components.Pages.Authen
{
    public partial class LoginPage
    {
        [Inject] public IHttpContextAccessor? HttpContextAccessor { get; set; }
        [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] public LoginTicketCache TicketCache { get; set; } = default!;
        [Inject] public IStringLocalizerFactory LocalizerFactory { get; set; } = default!;
        [SupplyParameterFromQuery(Name = "returnUrl")]
        public string? ReturnUrl { get; set; }

        public sp_Authentication_LoginReqDTO sp_Authentication_Login { get; set; } = new sp_Authentication_LoginReqDTO();
        bool isLoading = false;
        bool isShowPass = true;
        private bool hasSubmittedValidation;
        private bool usernameTouched;
        private bool passwordTouched;
        private string? UsernameValidationMessage { get; set; }
        private string? PasswordValidationMessage { get; set; }
        private bool HasUsernameValidation => !string.IsNullOrWhiteSpace(UsernameValidationMessage);
        private bool HasPasswordValidation => !string.IsNullOrWhiteSpace(PasswordValidationMessage);
        private IStringLocalizer ComponentLoc => LocalizerFactory.Create("Components.App", typeof(LoginPage).Assembly.GetName().Name!);

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (HttpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                UriHelper.NavigateTo("/", true);
                return;
            }

            StateHasChanged();
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

                if (loginReqDTO.Username is not { Length: > 0 } username
                    || loginReqDTO.Password is not { Length: > 0 } password)
                {
                    return;
                }

                var loginData = await DoLogin(username, password);
                if (loginData is null)
                {
                    return;
                }

                var ticketId = TicketCache.Add(loginData, loginReqDTO.isRememberPass);
                var redirectUrl = $"/perform-login?id={ticketId}";
                var requestedReturnUrl = AccountLoginRedirectPolicy.Resolve(
                    loginData.MustChangePassword,
                    ReturnUrl);
                if (!string.IsNullOrWhiteSpace(requestedReturnUrl))
                {
                    redirectUrl += $"&returnUrl={Uri.EscapeDataString(requestedReturnUrl)}";
                }

                UriHelper.NavigateTo(redirectUrl, true);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task<sp_Authentication_Login?> DoLogin(string username, string password)
        {
            var client = HttpClientFactory.CreateClient(Config.HttpClientName);
            try
            {
                var response = await client.PostAsJsonAsync(
                    Config.ApiLoginEndpoint,
                    new AuthenticationLoginRequest(username, password));

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

            return !HasUsernameValidation
                && !HasPasswordValidation;
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

    }
}
