using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Share;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Radzen;
using System.Security.Claims;
using System.Text.Json;

namespace gtas_vpp_fe.Components.Layout
{
    public partial class LeftSidebar
    {
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public ThemeService ThemeService { get; set; } = default!;
        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        public bool _sideBarExpanded { get; set; } = false;
        private string? currentUrl { get; set; }
        public const string QueryParameter = "theme";
        public string theme = "material3-base";
        public List<DropdownModel> dropdownDataModels_Company { get; set; } = new List<DropdownModel>();
        public DropdownModel selected_Company { get; set; } = default!;
        public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new sp_Authentication_GetPermissionSinglePage();
        private IEnumerable<Claim> claims = Enumerable.Empty<Claim>();
        public string State { get; set; } = "normal";

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            try
            {
                await LoadAuthenticationState();
            }
            catch (Exception)
            {
                // Design-time hoặc API chưa sẵn sàng
            }

            currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            NavigationManager.LocationChanged += OnLocationChanged;
        }
        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
            StateHasChanged();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await LoadTheme();
                //await LoadCompany();
                await LoadStateAsync();
                StateHasChanged();
            }
        }
        protected async Task LoadStateAsync()
        {
            try
            {
                var result = await ProtectedLocalStore.GetAsync<GlobalStorageModel>("CostingSetting");
                if (result.Success && result.Value is not null)
                {
                    //State = result.Header?.FirstOrDefault(x => x.PageName == "0014")?.Fields?.FirstOrDefault(x => x.FieldName == "RequestPageViewType")?.FieldValue ?? "normal";
                    State = result.Value?.Header?.FirstOrDefault(x => x.PageName == Config.Page_ComponentCode.PageCode.Sidebar)?.Fields?.FirstOrDefault(x => x.FieldName == "RequestPageViewType")?.FieldValue ?? "normal";
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        protected async Task ThemeOnChange()
        {
            LightTheme = !LightTheme;
            if (LightTheme)
            {
                ThemeService.SetTheme("material3");
                await ProtectedLocalStore.SetAsync("TransTheme", "material3");

            }
            else
            {
                ThemeService.SetTheme("material3-dark");
                await ProtectedLocalStore.SetAsync("TransTheme", "material3-dark");
            }
        }
        protected async Task LoadAuthenticationState()
        {
            // Load Authenticated
            var (isAuthenticated, userClaims) = await AuthHelper.EnsureAuthenticatedAsync();
            if (!isAuthenticated)
            {
                NavigationManager.NavigateTo("logoutprocess", true);
                return;
            }
            claims = userClaims;
            
            // Load permission
            sp_Authentication_GetPermissionSinglePage = await AuthHelper.LoadGlbPermissionAsync(Config.Page_ComponentCode.PageCode.Sidebar);

            if (sp_Authentication_GetPermissionSinglePage.List_Component.Count == 0)
            {
                NavigationManager.NavigateTo("Home", true);
            }
        }
        protected async Task LoadTheme()
        {
            try
            {
                var theme = await ProtectedLocalStore.GetAsync<string>("CostingTheme");

                if (theme.Success)
                {
                    ThemeService.SetTheme(theme.Value);
                    if (theme.Value == "material3")
                    {
                        LightTheme = true;
                    }
                    else
                    {
                        LightTheme = false;
                    }
                }
                else
                {
                    ThemeService.SetTheme("material3");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading theme: {ex.Message}");
            }
        }
    }
}

