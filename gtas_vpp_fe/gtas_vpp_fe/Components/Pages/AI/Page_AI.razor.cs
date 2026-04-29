using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.AI
{
    public partial class Page_AI
    {
        [Parameter] public string? Per { get; set; }
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        public IEnumerable<Claim> claims { get; set; } = new List<Claim>();
        public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new sp_Authentication_GetPermissionSinglePage();
        
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            try
            {
                await LoadAuthenticationState();
            }
            catch (Exception)
            {
            }
        }

        private async Task LoadAuthenticationState()
        {
            var (isAuthenticated, userClaims) = await AuthHelper.EnsureAuthenticatedAsync();
            if (!isAuthenticated)
            {
                NavigationManager.NavigateTo("logoutprocess", true);
                return;
            }
            claims = userClaims;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (!firstRender) return;
            glb.isBusyPage = true;
            try
            {
                var userIdString = claims.FirstOrDefault(x => x.Type == "UserID")?.Value;
                if (int.TryParse(userIdString, out int validUserId) == true)
                {
                    sp_Authentication_GetPermissionSinglePage = await AuthHelper.GetPermissionSinglePageAsync(validUserId, Config.Page_ComponentCode.PageCode.AI);
                    
                    if (sp_Authentication_GetPermissionSinglePage?.List_Component == null || 
                        !sp_Authentication_GetPermissionSinglePage.List_Component.Any())
                    {
                        NotificationService.Notify(new NotificationMessage() 
                        { 
                            Severity = NotificationSeverity.Warning, 
                            Summary = "Access Denied", 
                            Detail = "You do not have permission to access this page.", 
                            Duration = 5000 
                        });
                        NavigationManager.NavigateTo("/dashboard?tab=0", true);
                        return;
                    }
                    
                    if (sp_Authentication_GetPermissionSinglePage is not null)
                    {
                        StateHasChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when call SP sp_Authentication_GetPermissionSinglePage:" + ex.Message);
                NotificationService.Notify(new NotificationMessage() { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "Error when call api:" + ex.Message, Duration = 10000 });
            }
            finally
            {
                glb.isBusyPage = false;
            }
        }
    }
}
