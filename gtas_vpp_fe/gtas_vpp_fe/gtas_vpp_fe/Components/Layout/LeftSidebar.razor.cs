using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
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
        private static readonly (string Permission, string Path)[] DashboardMenuRoutes =
        [
            (Permissions.RequestOrder, "/dashboard?tab=0"),
            (Permissions.RequestHistory, "/dashboard?tab=1"),
            (Permissions.RequestProductCatalog, "/dashboard?tab=2"),
            (Permissions.RequestDepartmentSummary, "/dashboard?tab=3&managementTab=department"),
            (Permissions.RequestAllOrdersSummary, "/dashboard?tab=3&managementTab=all"),
            (Permissions.PeriodSettle, "/dashboard?tab=5&periodTab=review"),
            (Permissions.RequestAdminApproval, "/dashboard?tab=5&periodTab=pending")
        ];

        private static readonly (string Permission, string Path)[] LibraryMenuRoutes =
        [
            (Permissions.LibraryClass, "/library?tab=0"),
            (Permissions.LibraryCategory, "/library?tab=1"),
            (Permissions.LibraryItem, "/library?tab=2"),
            (Permissions.LibrarySupplier, "/library?tab=3"),
            (Permissions.LibraryPriceList, "/library?tab=6&pricingTab=price-lists"),
            (Permissions.LibraryPrice, "/library?tab=6&pricingTab=prices"),
            (Permissions.LibraryDepartment, "/library?tab=5")
        ];

        private static readonly (string Permission, string Path)[] PermissionMenuRoutes =
        [
            (Permissions.PermissionUser, "/permission?tab=0"),
            (Permissions.PermissionComponent, "/permission?tab=1")
        ];

        [Inject] public ThemeService ThemeService { get; set; } = default!;
        [Inject] public ThemeState ThemeState { get; set; } = default!;
        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        [Inject] public PermissionState PermissionState { get; set; } = default!;
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

        private bool CanViewDashboardMenu => HasSidebarMenu(Permissions.MenuDashboard) && DashboardMenuRoutes.Any(route => CanViewDashboardItem(route.Permission));
        private bool CanViewLibraryMenu => HasSidebarMenu(Permissions.MenuLibrary) && LibraryMenuRoutes.Any(route => CanViewLibraryItem(route.Permission));
        private bool CanViewReportMenu => PermissionState.HasPageAccess(Config.Page_ComponentCode.PageCode.Report);
        private bool CanViewPermissionMenu => HasSidebarMenu(Permissions.MenuPermission) && PermissionMenuRoutes.Any(route => CanViewPermissionItem(route.Permission));
        private string ServerLabel => $"SERVER {claims.FirstOrDefault(x => x.Type == "Server")?.Value?.ToUpper()}";
        private string HeaderTitle => $"GTAS VPP ({ServerLabel})";

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
                PermissionState.Changed += OnPermissionStateChanged;
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
                StateHasChanged();
                return;
            }
        }
        protected async Task LoadStateAsync()
        {
            var result = await ProtectedLocalStore.GetAsync<GlobalStorageModel>("CostingSetting");
            if (result.Success && result.Value is not null)
            {
                State = result.Value?.Header?.FirstOrDefault(x => x.PageName == Config.Page_ComponentCode.PageCode.Sidebar)?.Fields?.FirstOrDefault(x => x.FieldName == "RequestPageViewType")?.FieldValue ?? "normal";
            }
        }
        protected async Task ThemeOnChange()
        {
            LightTheme = !LightTheme;
            var newTheme = LightTheme ? "material3" : "material3-dark";
            
            ThemeState.SetTheme(newTheme);
            ThemeService.SetTheme(newTheme);
            
            if (!_isPrerendering)
            {
                await ApplyBrowserThemeAsync(newTheme);
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
            await PermissionState.EnsureLoadedAsync();
            if (PermissionState.IdentityClaims.Any())
            {
                claims = PermissionState.IdentityClaims;
            }
        }
        protected async Task LoadTheme()
        {
            string? themeCookie = null;

            if (!_isPrerendering)
            {
                themeCookie = await GetBrowserThemeAsync();
            }

            if (string.IsNullOrWhiteSpace(themeCookie) && HttpContext?.Request?.Cookies != null && HttpContext.Request.Cookies.TryGetValue("VPPTheme", out var requestTheme))
            {
                themeCookie = requestTheme;
            }

            themeCookie = string.IsNullOrWhiteSpace(themeCookie) ? "material3" : themeCookie;
            ThemeState.SetTheme(themeCookie);
            ThemeService.SetTheme(themeCookie);
            LightTheme = themeCookie == "material3";
        }

        private async Task<string?> GetBrowserThemeAsync()
        {
            try
            {
                return await JSRuntime.InvokeAsync<string>("vppTheme.current");
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (JSDisconnectedException)
            {
                return null;
            }
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
            PermissionState.Changed -= OnPermissionStateChanged;
            timer?.Dispose();
        }

        public async Task ToggleLanguage()
        {
            var currentCulture = CultureInfo.CurrentUICulture.Name;
            var newCulture = currentCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";
            
            await ProtectedLocalStore.SetAsync("VPP_Language", newCulture);
            await PrepareLanguageSwitchAsync();
            NavigationManager.NavigateTo($"/set-language?culture={newCulture}&returnUrl={Uri.EscapeDataString(NavigationManager.Uri)}", forceLoad: true);
        }

        private async Task ApplyBrowserThemeAsync(string newTheme)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("vppTheme.apply", newTheme);
            }
            catch (InvalidOperationException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
            catch (JSException)
            {
                // The server-side state remains authoritative; the next render
                // applies it even if JavaScript is temporarily unavailable.
            }
        }

        private async Task PrepareLanguageSwitchAsync()
        {
            if (_isPrerendering)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync("vppLanguage.prepareSwitch");
            }
            catch (InvalidOperationException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
            catch (JSException)
            {
            }
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

        public string GetSidebarClass() => $"vpp-sidebar vpp-layout-sidebar {(!_sideBarExpanded ? "sidebar-collapsed" : "")}";

        public void OnMenuItemClick(MenuItemEventArgs args)
        {
            // When collapsed and clicking a parent item (no Path), navigate to default tab
            if (!_sideBarExpanded && string.IsNullOrEmpty(args.Path))
            {
                string? defaultPath = args.Text switch
                {
                    var t when t == Loc["Dashboard"].Value => GetFirstAccessiblePath(DashboardMenuRoutes, CanViewDashboardItem),
                    var t when t == Loc["Library"].Value => GetFirstAccessiblePath(LibraryMenuRoutes, CanViewLibraryItem),
                    var t when t == Loc["Permissions"].Value => GetFirstAccessiblePath(PermissionMenuRoutes, CanViewPermissionItem),
                    _ => null
                };

                if (defaultPath != null)
                {
                    NavigationManager.NavigateTo(defaultPath);
                }
            }
        }

        private bool CanViewDashboardItem(string permission)
            => PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Dashboard, permission);

        private bool CanViewLibraryItem(string permission)
            => PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Library, permission);

        private bool CanViewPermissionItem(string permission)
            => PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Permission, permission);

        private bool HasSidebarMenu(string permission)
        {
            var sidebarPermission = PermissionState.GetPagePermission(Config.Page_ComponentCode.PageCode.Sidebar);
            return sidebarPermission.List_Component.Count == 0
                || PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Sidebar, permission);
        }

        private void OnPermissionStateChanged()
        {
            if (PermissionState.IdentityClaims.Any())
            {
                claims = PermissionState.IdentityClaims;
            }

            _ = InvokeAsync(StateHasChanged);
        }

        private static string? GetFirstAccessiblePath(IEnumerable<(string Permission, string Path)> routes, Func<string, bool> canView)
        {
            foreach (var route in routes)
            {
                if (canView(route.Permission))
                {
                    return route.Path;
                }
            }

            return null;
        }
    }
}
