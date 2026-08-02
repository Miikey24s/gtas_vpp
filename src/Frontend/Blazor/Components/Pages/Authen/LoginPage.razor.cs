using System.Net;
using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace gtas_vpp_fe.Components.Pages.Authen
{
    public partial class LoginPage
    {
        [Inject] public IHttpContextAccessor? HttpContextAccessor { get; set; }
        [Inject] public AuthenticationApiClient AuthenticationApi { get; set; } = default!;
        [Inject] public LoginTicketCache TicketCache { get; set; } = default!;
        [Inject] public IStringLocalizerFactory LocalizerFactory { get; set; } = default!;
        [SupplyParameterFromQuery(Name = "returnUrl")]
        public string? ReturnUrl { get; set; }

        public LoginFormModel loginForm { get; set; } = new LoginFormModel();
        bool isLoading = false;
        bool isShowPass = true;
        private bool hasSubmittedValidation;
        private bool usernameTouched;
        private bool passwordTouched;
        private string? UsernameValidationMessage { get; set; }
        private string? PasswordValidationMessage { get; set; }
        private string? LoginErrorMessage { get; set; }
        private bool HasUsernameValidation => !string.IsNullOrWhiteSpace(UsernameValidationMessage);
        private bool HasPasswordValidation => !string.IsNullOrWhiteSpace(PasswordValidationMessage);
        private bool HasPasswordFeedback => HasPasswordValidation || !string.IsNullOrWhiteSpace(LoginErrorMessage);
        private string? PasswordFeedbackMessage => HasPasswordValidation
            ? PasswordValidationMessage
            : LoginErrorMessage;
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
            LoginErrorMessage = null;
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
            LoginErrorMessage = null;
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

        private async Task LoginOnkeyup(KeyboardEventArgs e, LoginFormModel loginReqDTO)
        {
            if (e.Code == "Enter" || e.Code == "NumpadEnter")
            {
                await LoginSubmit(loginReqDTO);
            }
        }

        public async Task LoginSubmit(LoginFormModel loginReqDTO)
        {
            isLoading = true;
            LoginErrorMessage = null;
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

                var ticketId = TicketCache.Add(loginData, loginReqDTO.RememberMe);
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

        private async Task<AuthenticationResultDTO?> DoLogin(string username, string password)
        {
            try
            {
                var loginData = await AuthenticationApi.SignInAsync(username, password);
                if (loginData is null)
                {
                    ShowError(ComponentLoc["LoginRequestFailed"].Value);
                }

                return loginData;
            }
            catch (ApiRequestException exception)
            {
                ShowError(LoginFailureMapper.GetMessage(
                    exception.StatusCode ?? HttpStatusCode.BadRequest,
                    ComponentLoc));
                return null;
            }
            catch (TaskCanceledException)
            {
                ShowError(ComponentLoc["LoginTimeout"].Value);
                return null;
            }
            catch (HttpRequestException)
            {
                ShowError(ComponentLoc["LoginUnavailable"].Value);
                return null;
            }
            catch (JsonException)
            {
                ShowError(ComponentLoc["LoginRequestFailed"].Value);
                return null;
            }
        }

        private void ShowError(string detail)
        {
            LoginErrorMessage = detail;
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
            UsernameValidationMessage = string.IsNullOrWhiteSpace(loginForm.Username)
                ? ComponentLoc["LoginUsernameRequired"].Value
                : null;
        }

        private void ValidatePassword()
        {
            PasswordValidationMessage = string.IsNullOrWhiteSpace(loginForm.Password)
                ? ComponentLoc["LoginPasswordRequired"].Value
                : null;
        }

    }
}
