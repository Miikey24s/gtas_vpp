using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using Radzen;
using System.Globalization;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Layout
{
    public partial class LeftSidebar : IDisposable
    {
        [Inject] public ThemeService ThemeService { get; set; } = default!;
        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [CascadingParameter] public HttpContext? HttpContext { get; set; }
        
        public bool _sideBarExpanded { get; set; } = false;
        public bool LightTheme { get; set; } = true;
        public bool _userMenuOpen = false;
        public DateTime currentTime = DateTime.Now;
        public System.Threading.Timer? timer;
        private string? currentUrl { get; set; }
        public const string QueryParameter = "theme";
        public string theme = "material3-base";
        public List<DropdownModel> dropdownDataModels_Company { get; set; } = new List<DropdownModel>();
        public DropdownModel selected_Company { get; set; } = default!;
        private IEnumerable<Claim> claims = Enumerable.Empty<Claim>();
        public string State { get; set; } = "normal";
        private bool _isPrerendering = true;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            
            timer = new System.Threading.Timer(_ =>
            {
                currentTime = DateTime.Now;
                InvokeAsync(StateHasChanged);
            }, null, 0, 1000);
            
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
                _isPrerendering = false;
                await LoadTheme();
                await LoadStateAsync();
                await LoadAIStateAsync();
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
            var newTheme = LightTheme ? "material3" : "material3-dark";
            
            ThemeService.SetTheme(newTheme);
            
            // Set cookie chỉ khi không prerendering
            if (!_isPrerendering)
            {
                await JSRuntime.InvokeVoidAsync("eval", $"document.cookie = 'VPPTheme={newTheme}; path=/; max-age=31536000'");
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
                // Đọc theme từ cookie thông qua HttpContext
                string? themeCookie = null;
                if (HttpContext?.Request?.Cookies != null && HttpContext.Request.Cookies.TryGetValue("VPPTheme", out themeCookie))
                {
                    ThemeService.SetTheme(themeCookie);
                    LightTheme = themeCookie == "material3";
                }
                else
                {
                    ThemeService.SetTheme("material3");
                    LightTheme = true;
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

        private async Task LoadAIStateAsync()
        {
            try
            {
                var result = await ProtectedLocalStore.GetAsync<bool>("VPP_AIEnabled");
                if (result.Success)
                {
                    glb.IsAIEnabled = result.Value;
                }
            }
            catch (Exception)
            {
                // Default: true
            }
        }

        public async Task OnAIToggleChange(bool value)
        {
            glb.IsAIEnabled = value;
            await ProtectedLocalStore.SetAsync("VPP_AIEnabled", value);
        }

        public async Task ToggleLanguage()
        {
            var currentCulture = CultureInfo.CurrentCulture.Name;
            var newCulture = currentCulture.StartsWith("en") ? "vi" : "en";
            
            await ProtectedLocalStore.SetAsync("VPP_Language", newCulture);
            NavigationManager.NavigateTo($"/set-language?culture={newCulture}&returnUrl={Uri.EscapeDataString(NavigationManager.Uri)}", forceLoad: true);
        }

        public string GetUserInitials()
        {
            var name = glb.UserInfo.FullName ?? "";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
            if (parts.Length == 1)
                return parts[0][..Math.Min(2, parts[0].Length)].ToUpper();
            return "U";
        }

        public string GetSidebarClass() => $"vpp-sidebar {(!_sideBarExpanded ? "sidebar-collapsed" : "")}";

        public void OnMenuItemClick(MenuItemEventArgs args)
        {
            // When collapsed and clicking a parent item (no Path), navigate to default tab
            if (!_sideBarExpanded && string.IsNullOrEmpty(args.Path))
            {
                string? defaultPath = args.Text switch
                {
                    var t when t == Loc["Dashboard"].Value => "/dashboard?tab=0",
                    var t when t == Loc["Library"].Value => "/library?tab=0",
                    var t when t == Loc["Permissions"].Value => "/permission?tab=0",
                    var t when t == Loc["AIManagement"].Value => "/ai/chat",
                    _ => null
                };

                if (defaultPath != null)
                {
                    NavigationManager.NavigateTo(defaultPath);
                }
            }
        }
    }
}

