using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Composites;
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
            (Permissions.PermissionComponent, "/permission?tab=1"),
            (Permissions.PermissionManage, "/permission?tab=2")
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
        private bool _dashboardMenuExpanded = true;
        private bool _periodMenuExpanded = true;
        private bool _libraryMenuExpanded = true;
        private bool _pricingMenuExpanded = true;
        private bool _permissionMenuExpanded = true;
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
        private bool CanViewPeriodMenu => CanViewDashboardItem(Permissions.RequestAdminApproval) || CanViewDashboardItem(Permissions.PeriodSettle);
        private bool CanViewPricingMenu => CanViewLibraryItem(Permissions.LibraryPriceList) || CanViewLibraryItem(Permissions.LibraryPrice);
        private bool HasExpandableSidebarGroups => CanViewDashboardMenu || CanViewLibraryMenu || CanViewPermissionMenu;

        private bool AreAllSidebarGroupsExpanded =>
            (!CanViewDashboardMenu || _dashboardMenuExpanded)
            && (!CanViewPeriodMenu || _periodMenuExpanded)
            && (!CanViewLibraryMenu || _libraryMenuExpanded)
            && (!CanViewPricingMenu || _pricingMenuExpanded)
            && (!CanViewPermissionMenu || _permissionMenuExpanded);

        private string SidebarTreeToggleLabel => Loc[
            AreAllSidebarGroupsExpanded ? "CollapseAllNavigation" : "ExpandAllNavigation"];
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
            // Nạp trạng thái đã xác thực.
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
                // State phía server vẫn có thẩm quyền; lần render tiếp theo sẽ áp dụng
                // dù JavaScript tạm thời không khả dụng.
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

        public sealed record HeaderTab(
            string Label,
            string Path,
            bool IsActive,
            IReadOnlyList<VppHeaderSubTab>? Children = null)
        {
            public bool IsExpanded => IsActive && Children is { Count: > 0 };
        }

        private void ToggleAllSidebarGroups()
        {
            var expanded = !AreAllSidebarGroupsExpanded;

            if (CanViewDashboardMenu)
            {
                _dashboardMenuExpanded = expanded;
            }

            if (CanViewPeriodMenu)
            {
                _periodMenuExpanded = expanded;
            }

            if (CanViewLibraryMenu)
            {
                _libraryMenuExpanded = expanded;
            }

            if (CanViewPricingMenu)
            {
                _pricingMenuExpanded = expanded;
            }

            if (CanViewPermissionMenu)
            {
                _permissionMenuExpanded = expanded;
            }
        }

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
                query.TryGetValue("periodTab", out var periodTab);
                query.TryGetValue("pricingTab", out var pricingTab);
                var tabs = new List<HeaderTab>();

                switch (path)
                {
                    case "" or "dashboard" or "dashboard/order-create":
                        var isOrderCreate = path == "dashboard/order-create";
                        var myOrdersActive = isOrderCreate || tab is null or "" or "0";
                        if (CanViewDashboardItem(Permissions.RequestOrder))
                        {
                            tabs.Add(new(Loc["MyOrders"], "/dashboard?tab=0", myOrdersActive));
                        }

                        if (CanViewDashboardItem(Permissions.RequestHistory))
                        {
                            tabs.Add(new(Loc["History"], "/dashboard?tab=1", tab == "1"));
                        }

                        if (CanViewDashboardItem(Permissions.RequestDepartmentSummary))
                        {
                            tabs.Add(new(Loc["DepartmentSummary"], "/dashboard?tab=3&managementTab=department", tab == "3"));
                        }

                        if (CanViewDashboardItem(Permissions.RequestProductCatalog))
                        {
                            tabs.Add(new(Loc["Catalog"], "/dashboard?tab=2", tab == "2"));
                        }

                        var canSettle = CanViewDashboardItem(Permissions.PeriodSettle);
                        var canApproval = CanViewDashboardItem(Permissions.RequestAdminApproval);
                        if (canSettle || canApproval)
                        {
                            var periodPath = canSettle
                                ? "/dashboard?tab=5&periodTab=review"
                                : "/dashboard?tab=5&periodTab=pending";
                            var periodChildren = new List<VppHeaderSubTab>();
                            var pendingActive = tab == "5"
                                && string.Equals(periodTab, "pending", StringComparison.OrdinalIgnoreCase)
                                && canApproval;

                            if (canSettle)
                            {
                                periodChildren.Add(new(
                                    Loc["PeriodSettleStep"],
                                    "/dashboard?tab=5&periodTab=review",
                                    tab == "5" && !pendingActive));
                            }

                            if (canApproval)
                            {
                                periodChildren.Add(new(
                                    Loc["AdminApproval"],
                                    "/dashboard?tab=5&periodTab=pending",
                                    tab == "5" && (pendingActive || !canSettle)));
                            }

                            tabs.Add(new(Loc["PeriodOperations"], periodPath, tab == "5", periodChildren));
                        }

                        break;
                    case "library":
                        if (CanViewLibraryItem(Permissions.LibraryClass))
                        {
                            tabs.Add(new(Loc["ClassDefinitions"], "/library?tab=0", tab is null or "" or "0"));
                        }

                        if (CanViewLibraryItem(Permissions.LibraryCategory))
                        {
                            tabs.Add(new(Loc["OperationCategories"], "/library?tab=1", tab == "1"));
                        }

                        if (CanViewLibraryItem(Permissions.LibraryItem))
                        {
                            tabs.Add(new(Loc["Operations"], "/library?tab=2", tab == "2"));
                        }

                        if (CanViewLibraryItem(Permissions.LibrarySupplier))
                        {
                            tabs.Add(new(Loc["Suppliers"], "/library?tab=3", tab == "3"));
                        }

                        if (CanViewLibraryItem(Permissions.LibraryPriceList) || CanViewLibraryItem(Permissions.LibraryPrice))
                        {
                            var canViewPriceLists = CanViewLibraryItem(Permissions.LibraryPriceList);
                            var canViewPrices = CanViewLibraryItem(Permissions.LibraryPrice);
                            var pricingPath = canViewPriceLists
                                ? "/library?tab=6&pricingTab=price-lists"
                                : "/library?tab=6&pricingTab=prices";
                            var pricingActive = tab is "4" or "6";
                            var pricesActive = pricingActive
                                && canViewPrices
                                && (tab == "4" || string.Equals(pricingTab, "prices", StringComparison.OrdinalIgnoreCase));
                            var pricingChildren = new List<VppHeaderSubTab>();

                            if (canViewPriceLists)
                            {
                                pricingChildren.Add(new(
                                    Loc["PriceLists"],
                                    "/library?tab=6&pricingTab=price-lists",
                                    pricingActive && !pricesActive));
                            }

                            if (canViewPrices)
                            {
                                pricingChildren.Add(new(
                                    Loc["Prices"],
                                    "/library?tab=6&pricingTab=prices",
                                    pricingActive && (pricesActive || !canViewPriceLists)));
                            }

                            tabs.Add(new(Loc["Pricing"], pricingPath, pricingActive, pricingChildren));
                        }

                        if (CanViewLibraryItem(Permissions.LibraryDepartment))
                        {
                            tabs.Add(new(Loc["Departments"], "/library?tab=5", tab == "5"));
                        }

                        break;
                    case "permission":
                        if (CanViewPermissionItem(Permissions.PermissionUser))
                        {
                            tabs.Add(new(Loc["Users"], "/permission?tab=0", tab is null or "" or "0"));
                        }

                        if (CanViewPermissionItem(Permissions.PermissionComponent))
                        {
                            tabs.Add(new(Loc["GroupsAndPermissions"], "/permission?tab=1", tab == "1"));
                        }

                        if (CanViewPermissionItem(Permissions.PermissionManage))
                        {
                            tabs.Add(new(Loc["SecurityAudit"], "/permission?tab=2", tab == "2"));
                        }

                        break;
                    case "report":
                        if (CanViewReportMenu)
                        {
                            tabs.Add(new(Loc["Reports"], "/report", true));
                        }

                        break;
                }

                return tabs;
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
            // Khi sidebar thu gọn và click mục cha không có Path, điều hướng tới tab mặc định.
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
            => Permissions.IsActionCode(permission)
                ? PermissionState.HasPermission(permission)
                : PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Permission, permission);

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
