using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Models;
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
        // Bật sau khi trạng thái sidebar đã chốt (storage + viewport) — E2E chờ
        // data-shell-ready thay vì tương tác giữa lúc shell còn đang mở/thu.
        public bool _shellStateReady { get; set; }
        private const string SidebarStorageKey = "VPP_SidebarExpanded";
        public bool LightTheme { get; set; } = true;
        public bool _userMenuOpen = false;
        private string? currentUrl { get; set; }
        public const string QueryParameter = "theme";
        public string theme = "material3-base";
        public List<DropdownModel> dropdownDataModels_Company { get; set; } = new List<DropdownModel>();
        public DropdownModel selected_Company { get; set; } = default!;
        public string State { get; set; } = "normal";
        private bool _isPrerendering = true;

        private bool CanViewDashboardMenu => HasSidebarMenu(Permissions.MenuDashboard) && DashboardMenuRoutes.Any(route => CanViewDashboardItem(route.Permission));
        private bool CanViewLibraryMenu => HasSidebarMenu(Permissions.MenuLibrary) && LibraryMenuRoutes.Any(route => CanViewLibraryItem(route.Permission));
        private bool CanViewReportMenu => PermissionState.HasPageAccess(Config.Page_ComponentCode.PageCode.Report);
        private bool CanViewPermissionMenu => HasSidebarMenu(Permissions.MenuPermission) && PermissionMenuRoutes.Any(route => CanViewPermissionItem(route.Permission));
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (!RendererInfo.IsInteractive)
            {
                currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
                return;
            }

            glb.BusyChanged += OnBusyChanged;

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
                await LoadSidebarStateAsync();
                _shellStateReady = true;
                StateHasChanged();
                return;
            }
        }

        // Mặc định desktop mở rộng sidebar theo Atlas; mobile giữ thu gọn (overlay).
        // Lựa chọn của người dùng được ghi nhớ qua ProtectedLocalStorage như theme.
        protected async Task LoadSidebarStateAsync()
        {
            try
            {
                var stored = await ProtectedLocalStore.GetAsync<bool>(SidebarStorageKey);
                if (stored.Success)
                {
                    _sideBarExpanded = stored.Value && await IsDesktopViewportAsync();
                    return;
                }

                _sideBarExpanded = await IsDesktopViewportAsync();
            }
            catch (Exception)
            {
                // Giữ trạng thái thu gọn nếu storage/JS chưa sẵn sàng.
            }
        }

        public async Task SetSidebarExpandedAsync(bool expanded)
        {
            _sideBarExpanded = expanded;

            try
            {
                await ProtectedLocalStore.SetAsync(SidebarStorageKey, expanded);
            }
            catch (Exception)
            {
                // Không chặn thao tác mở/đóng nếu không ghi được storage.
            }
        }

        private async Task<bool> IsDesktopViewportAsync()
        {
            try
            {
                return await JSRuntime.InvokeAsync<bool>("vppViewport.isDesktop");
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (JSDisconnectedException)
            {
                return false;
            }
            catch (JSException)
            {
                return false;
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
            var (isAuthenticated, _) = await AuthHelper.EnsureAuthenticatedAsync();
            if (!isAuthenticated)
            {
                NavigationManager.NavigateTo("logoutprocess", true);
                return;
            }

            await PermissionState.EnsureLoadedAsync();
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
            glb.BusyChanged -= OnBusyChanged;
        }

        public async Task ToggleLanguage()
        {
            var currentCulture = CultureInfo.CurrentUICulture.Name;
            var newCulture = currentCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";

            await ProtectedLocalStore.SetAsync("VPP_Language", newCulture);
            await PrepareLanguageSwitchAsync();
            var returnUrl = $"/{NavigationManager.ToBaseRelativePath(NavigationManager.Uri)}";
            NavigationManager.NavigateTo($"/set-language?culture={newCulture}&returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
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

        // Role badge cuối header theo Atlas: 3 persona chuẩn hiển thị qua Loc
        // (không in raw GroupName "DEV" của persona kỹ thuật — §3.3.4.4);
        // nhóm tùy biến ngoài 3 persona hiển thị đúng tên nhóm của nó.
        private string RoleBadgeLabel
        {
            get
            {
                var groupId = glb.UserInfo?.GroupId ?? Guid.Empty;
                if (groupId == CanonicalRbac.Employee.GroupId)
                {
                    return Loc["RoleEmployee"];
                }

                if (groupId == CanonicalRbac.Manager.GroupId || groupId == CanonicalRbac.LegacyProcurementAdminGroupId)
                {
                    return Loc["RoleManager"];
                }

                if (groupId == CanonicalRbac.Dev.GroupId)
                {
                    return Loc["RoleDev"];
                }

                return glb.UserInfo?.GroupName?.Trim() ?? string.Empty;
            }
        }

        // Một mục tab trong primary header (Atlas headerTabs): tab active của màn
        // con/cháu render đường dẫn "cha › con" ngay trong tab (nestedHeaderTab).
        public sealed record HeaderTab(string Label, string Path, bool IsActive, IReadOnlyList<string>? Breadcrumb);

        // Tab strip theo khu vực trong primary header desktop (Atlas: dashboard 5 tab,
        // library 6, permissions 2, reports 1). Điều hướng bằng URL nên các trang
        // giữ nguyên cơ chế RadzenTabs + query param hiện có.
        private IReadOnlyList<HeaderTab> HeaderTabs
        {
            get
            {
                var url = currentUrl ?? string.Empty;
                var path = url.Split('?', 2)[0].Trim('/').ToLowerInvariant();
                var query = ParseQuery(url);
                query.TryGetValue("tab", out var tab);
                var tabs = new List<HeaderTab>();

                switch (path)
                {
                    case "" or "dashboard" or "dashboard/order-create":
                        var isOrderCreate = path == "dashboard/order-create";
                        var myOrdersActive = isOrderCreate || tab is null or "" or "0";
                        if (CanViewDashboardItem(Permissions.RequestOrder))
                        {
                            tabs.Add(new(Loc["MyOrders"], "/dashboard?tab=0", myOrdersActive,
                                isOrderCreate ? [Loc["MyOrders"].Value, Loc["CreateOrderThisCycle"].Value] : null));
                        }

                        if (CanViewDashboardItem(Permissions.RequestHistory))
                        {
                            tabs.Add(new(Loc["History"], "/dashboard?tab=1", tab == "1", null));
                        }

                        if (CanViewDashboardItem(Permissions.RequestProductCatalog))
                        {
                            tabs.Add(new(Loc["Catalog"], "/dashboard?tab=2", tab == "2", null));
                        }

                        var canDepartment = CanViewDashboardItem(Permissions.RequestDepartmentSummary);
                        var canAllOrders = CanViewDashboardItem(Permissions.RequestAllOrdersSummary);
                        if (canDepartment || canAllOrders)
                        {
                            var managementLeaf = query.GetValueOrDefault("managementTab") == "all" || (!canDepartment && canAllOrders)
                                ? Loc["AllOrdersSummary"].Value
                                : Loc["DepartmentSummary"].Value;
                            var managementPath = canDepartment
                                ? "/dashboard?tab=3&managementTab=department"
                                : "/dashboard?tab=3&managementTab=all";
                            tabs.Add(new(Loc["Management"], managementPath, tab == "3",
                                [Loc["Management"].Value, managementLeaf]));
                        }

                        var canSettle = CanViewDashboardItem(Permissions.PeriodSettle);
                        var canApproval = CanViewDashboardItem(Permissions.RequestAdminApproval);
                        if (canSettle || canApproval)
                        {
                            var periodLeaf = query.GetValueOrDefault("periodTab") == "pending" || (!canSettle && canApproval)
                                ? Loc["AdminApproval"].Value
                                : Loc["PeriodReview"].Value;
                            var periodPath = canSettle
                                ? "/dashboard?tab=5&periodTab=review"
                                : "/dashboard?tab=5&periodTab=pending";
                            tabs.Add(new(Loc["PeriodOperations"], periodPath, tab == "5",
                                [Loc["PeriodOperations"].Value, periodLeaf]));
                        }

                        break;
                    case "library":
                        if (CanViewLibraryItem(Permissions.LibraryClass))
                        {
                            tabs.Add(new(Loc["ClassDefinitions"], "/library?tab=0", tab is null or "" or "0", null));
                        }

                        if (CanViewLibraryItem(Permissions.LibraryCategory))
                        {
                            tabs.Add(new(Loc["OperationCategories"], "/library?tab=1", tab == "1", null));
                        }

                        if (CanViewLibraryItem(Permissions.LibraryItem))
                        {
                            tabs.Add(new(Loc["Operations"], "/library?tab=2", tab == "2", null));
                        }

                        if (CanViewLibraryItem(Permissions.LibrarySupplier))
                        {
                            tabs.Add(new(Loc["Suppliers"], "/library?tab=3", tab == "3", null));
                        }

                        if (CanViewLibraryItem(Permissions.LibraryPriceList) || CanViewLibraryItem(Permissions.LibraryPrice))
                        {
                            var pricingLeaf = query.GetValueOrDefault("pricingTab") == "prices"
                                ? Loc["Prices"].Value
                                : Loc["PriceLists"].Value;
                            tabs.Add(new(Loc["Pricing"], "/library?tab=6&pricingTab=price-lists", tab == "6",
                                [Loc["Pricing"].Value, pricingLeaf]));
                        }

                        if (CanViewLibraryItem(Permissions.LibraryDepartment))
                        {
                            tabs.Add(new(Loc["Departments"], "/library?tab=5", tab == "5", null));
                        }

                        break;
                    case "permission":
                        if (CanViewPermissionItem(Permissions.PermissionUser))
                        {
                            tabs.Add(new(Loc["Users"], "/permission?tab=0", tab is null or "" or "0", null));
                        }

                        if (CanViewPermissionItem(Permissions.PermissionComponent))
                        {
                            tabs.Add(new(Loc["GroupsAndPermissions"], "/permission?tab=1", tab == "1", null));
                        }

                        break;
                    case "report":
                        if (CanViewReportMenu)
                        {
                            tabs.Add(new(Loc["Reports"], "/report", true, null));
                        }

                        break;
                }

                return tabs;
            }
        }

        // Breadcrumb "cha › con" trong header theo nestedHeaderPaths của Atlas.
        private IReadOnlyList<string> HeaderPathSegments
        {
            get
            {
                var url = currentUrl ?? string.Empty;
                var path = url.Split('?', 2)[0].Trim('/').ToLowerInvariant();
                var query = ParseQuery(url);
                query.TryGetValue("tab", out var tab);

                switch (path)
                {
                    case "" or "dashboard":
                        return tab switch
                        {
                            "1" => [Loc["History"].Value],
                            "2" => [Loc["Catalog"].Value],
                            "3" => query.GetValueOrDefault("managementTab") == "all"
                                ? [Loc["Management"].Value, Loc["AllOrdersSummary"].Value]
                                : [Loc["Management"].Value, Loc["DepartmentSummary"].Value],
                            "5" => query.GetValueOrDefault("periodTab") == "pending"
                                ? [Loc["PeriodOperations"].Value, Loc["AdminApproval"].Value]
                                : [Loc["PeriodOperations"].Value, Loc["PeriodReview"].Value],
                            _ => [Loc["MyOrders"].Value]
                        };
                    case "dashboard/order-create":
                        return [Loc["MyOrders"].Value, Loc["CreateOrderThisCycle"].Value];
                    case "library":
                        return tab switch
                        {
                            "1" => [Loc["OperationCategories"].Value],
                            "2" => [Loc["Operations"].Value],
                            "3" => [Loc["Suppliers"].Value],
                            "5" => [Loc["Departments"].Value],
                            "6" => query.GetValueOrDefault("pricingTab") == "prices"
                                ? [Loc["Pricing"].Value, Loc["Prices"].Value]
                                : [Loc["Pricing"].Value, Loc["PriceLists"].Value],
                            _ => [Loc["ClassDefinitions"].Value]
                        };
                    case "permission":
                        return tab == "1"
                            ? [Loc["GroupsAndPermissions"].Value]
                            : [Loc["Users"].Value];
                    case "report":
                        return [Loc["Reports"].Value];
                    default:
                        return Array.Empty<string>();
                }
            }
        }

        private static Dictionary<string, string> ParseQuery(string url)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var queryIndex = url.IndexOf('?');
            if (queryIndex < 0)
            {
                return result;
            }

            foreach (var pair in url[(queryIndex + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                result[Uri.UnescapeDataString(parts[0])] = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            }

            return result;
        }

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
            return sidebarPermission.Components.Count == 0
                || PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Sidebar, permission);
        }

        private void OnPermissionStateChanged()
        {
            _ = InvokeAsync(StateHasChanged);
        }

        private void OnBusyChanged()
        {
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
