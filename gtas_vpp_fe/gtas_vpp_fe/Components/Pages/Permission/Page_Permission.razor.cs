using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Helpers.DTOs.Res.Auth;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Permission
{
    public partial class Page_Permission
    {
        [Parameter] public string? Per { get; set; }
        //[Inject] public IBussinessService _bussinessService { get; set; }
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        public IEnumerable<Claim> claims { get; set; } = new List<Claim>();
        public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new sp_Authentication_GetPermissionSinglePage();
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var authState = await AuthenticationStateProvider
            .GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is null || !user.Identity.IsAuthenticated)
            {
                UriHelper.NavigateTo("Account/Login", true);
            }

            if (user.Identity is not null && user.Identity.IsAuthenticated)
            {
                claims = user.Claims;
            }
            else
            {
                UriHelper.NavigateTo("Account/Login", true);
            }
        }
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (!firstRender) return;
            glb.isBusyPage = true;
            try
            {
                _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out int UserId);
                //sp_Authentication_GetPermissionSinglePage = await _bussinessService.SPServiceRead<sp_Authentication_GetPermissionSinglePage>(
                //                                                                Config.SPENUM_ResType.Single,
                //                                                                nameof(Config.sp_AuthenClass.sp_Authen.sp_Authen),
                //                                                                nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_GetPermissionSinglePage),
                //                                                                new { userId = UserId != 0 ? UserId : glb.UserInfo.UserID, pageCode = "0001" })
                //                                                .ContinueWith(x => x.Result.FirstOrDefault() ?? new sp_Authentication_GetPermissionSinglePage());
                string sptype = nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_GetPermissionSinglePage);
                var body = new { userId = UserId != 0 ? UserId : glb.UserInfo.UserID, pageCode = Config.Page_ComponentCode.PageCode.Sidebar };
                var parsedData = await _apiServices.APIFrom_sp_Authen_Typed<sp_Authentication_GetPermissionSinglePage>(sptype, body);

                if (parsedData is not null)
                {
                    sp_Authentication_GetPermissionSinglePage = parsedData;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                //_bussinessService.WriteLog(ex, "sp_Authentication_GetPermissionSinglePage", new Dictionary<string, object>() { { "UserId", claims.FirstOrDefault(x => x.Type == "UserID")?.Value }, { "PageCode", "0003" } });
                Console.WriteLine("Error when call SP sp_Authentication_GetPermissionSinglePage:" + ex.Message);
                NotificationService.Notify(new NotificationMessage() { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "Error when call api sp_Library_GetL01Class:" + ex.Message, Duration = 10000 });
            }
            finally
            {
                glb.isBusyPage = false;
            }
        }
    }
}
