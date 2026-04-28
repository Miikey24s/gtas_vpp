using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
        [SupplyParameterFromQuery(Name = "returnUrl")]
        public string? ReturnUrl { get; set; }

        public sp_Authentication_LoginReqDTO sp_Authentication_Login { get; set; } = new sp_Authentication_LoginReqDTO();
        bool isLoading = false;
        bool isShowPass = true;
        private List<string> Servers = new() { "Live" };
        public bool isShowServer { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            isShowServer = UriHelper.BaseUri.Contains("localhost");
            SetupServerEnv();

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
                if (UriHelper.BaseUri.Contains("dev.") || UriHelper.BaseUri.Contains("localhost"))
                {
                    loginReqDTO.selected_server = "Test";
                }
                else if (UriHelper.BaseUri.Contains("transport.") || UriHelper.BaseUri.Contains("annam.id.vn") || UriHelper.BaseUri.Contains("209.") || UriHelper.BaseUri.Contains("172.") || UriHelper.BaseUri.Contains("100.") || UriHelper.BaseUri.Contains("128.") || UriHelper.BaseUri.Contains("192.") || UriHelper.BaseUri.Contains("10.") || System.Text.RegularExpressions.Regex.IsMatch(UriHelper.BaseUri, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b"))
                {
                    // Fallback to Live for production domains or any IP address (like DigitalOcean droplet IP)
                    loginReqDTO.selected_server = "Live";
                }

                if (string.IsNullOrWhiteSpace(loginReqDTO.Username)
                    || string.IsNullOrWhiteSpace(loginReqDTO.Password)
                    || string.IsNullOrWhiteSpace(loginReqDTO.selected_server))
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Warning",
                        Detail = "Please enter Username, Password and Select Server",
                        Duration = 4000
                    });
                    return;
                }

                var loginData = await DoLogin(loginReqDTO.Username, loginReqDTO.Password, loginReqDTO.selected_server);
                if (loginData is null)
                {
                    return;
                }

                var ticketId = TicketCache.Add(loginData, loginReqDTO.selected_server, loginReqDTO.isRememberPass);
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

