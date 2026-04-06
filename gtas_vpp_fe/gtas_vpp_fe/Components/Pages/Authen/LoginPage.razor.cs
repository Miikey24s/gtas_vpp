using gtas_vpp_fe.Helpers;
using Radzen;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using gtas_vpp_fe.Helpers.DTOs.Req;

namespace gtas_vpp_fe.Components.Pages.Authen
{
    public partial class LoginPage
    {
        [CascadingParameter] public HttpContext HttpContext { get; set; } = default!;
        [Inject] public IHttpContextAccessor? HttpContextAccessor { get; set; }
        //[Inject] public IBussinessService _bussinessService { get; set; } = default!;
        public sp_Authentication_LoginReqDTO sp_Authentication_Login { get; set; } = new sp_Authentication_LoginReqDTO();
        bool isLoading = false;
        bool isShowPass = true;
        private List<string> Servers = new List<string> { "Live" };
        public bool isShowServer { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (UriHelper.BaseUri.Contains("localhost"))
            {
                isShowServer = true;
            }
            else
            {
                isShowServer = false;
            }
            var authToken = HttpContextAccessor!.HttpContext!.Request.Cookies[Config.CookieName];
            if (authToken != null)
            {
                UriHelper.NavigateTo("/home");
            }
            SetupServerEnv();
            StateHasChanged();
        }
        private void SetupServerEnv()
        {
            Servers = new List<string> { "Test", "Live" };
        }
        private async void LoginOnkeyup(KeyboardEventArgs e, sp_Authentication_LoginReqDTO loginReqDTO)
        {
            Servers = new List<string> { "Test", "Live" };
            if (e.Code == "Enter" || e.Code == "NumpadEnter")
            {
                await LoginSubmit(loginReqDTO);
            }
        }
        public async Task LoginSubmit(sp_Authentication_LoginReqDTO loginReqDTO)
        {
            isLoading = true;
            await Task.Delay(1);
            if (UriHelper.BaseUri.Contains("dev."))
            {
                loginReqDTO.selected_server = "Test";
            }
            else if (UriHelper.BaseUri.Contains("transport."))
            {
                loginReqDTO.selected_server = "Live";
            }
            if (!string.IsNullOrWhiteSpace(loginReqDTO.Username) && !string.IsNullOrWhiteSpace(loginReqDTO.Password) && !string.IsNullOrEmpty(loginReqDTO.selected_server))
            {
                UriHelper.NavigateTo($"/loginprocess/{loginReqDTO.Username}/{loginReqDTO.Password}/{(loginReqDTO.isRememberPass ? 1 : 0)}/{loginReqDTO.selected_server}", forceLoad: true);
            }
            else
            {
                NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Warning, Summary = "Warning", Detail = "Please enter Username, Password and Select Server", Duration = 4000 });
                isLoading = false;
            }
            isLoading = false;
            StateHasChanged();
        }
    }
}
