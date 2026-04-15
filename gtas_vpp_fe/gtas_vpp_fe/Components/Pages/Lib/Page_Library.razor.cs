using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Lib
{
    public partial class Page_Library
    {
        [Parameter] public string? Lib { get; set; }
        //[Inject] public IBussinessService _bussinessService { get; set; } = default!;
        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        public IEnumerable<Claim> claims { get; set; } = new List<Claim>();
        public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new sp_Authentication_GetPermissionSinglePage();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            // Load Authenticated
            var (isAuthenticated, userClaims) = await AuthHelper.EnsureAuthenticatedAsync();
            if (!isAuthenticated)
            {
                NavigationManager.NavigateTo("logoutprocess", true);
                return;
            }
            claims = userClaims;

            glb.isBusyPage = true;
            try
            {
                var userIdString = claims.FirstOrDefault(x => x.Type == "UserID")?.Value;
                if (int.TryParse(userIdString, out int validUserId) == true)
                {
                    //glb.UserInfo.UserID = validUserId;
                    sp_Authentication_GetPermissionSinglePage = await AuthHelper.GetPermissionSinglePageAsync(validUserId, Config.Page_ComponentCode.PageCode.Sidebar);
                }

                if (sp_Authentication_GetPermissionSinglePage.List_Component.Count == 0)
                {
                    NavigationManager.NavigateTo("Home", true);
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
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            
        }
    }
}
