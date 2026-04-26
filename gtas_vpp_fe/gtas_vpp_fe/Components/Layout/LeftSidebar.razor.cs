using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Layout
{
    public partial class LeftSidebar : IDisposable
    {
        [Inject] public ThemeService ThemeService { get; set; } = default!;
        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        public bool _sideBarExpanded { get; set; } = false;
        private string? currentUrl { get; set; }
        public const string QueryParameter = "theme";
        public string theme = "material3-base";
        public List<DropdownModel> dropdownDataModels_Company { get; set; } = new List<DropdownModel>();
        public DropdownModel selected_Company { get; set; } = default!;
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
        }
        protected async Task LoadTheme()
        {
            try
            {
                var theme = await ProtectedLocalStore.GetAsync<string>("CostingTheme");

                if (theme.Success && !string.IsNullOrWhiteSpace(theme.Value))
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

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
            timer?.Dispose();
        }
    }
}

