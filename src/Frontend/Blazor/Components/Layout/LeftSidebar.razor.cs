using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Platform.State;
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
        [Inject] public ThemeService ThemeService { get; set; } = default!;
        [Inject] public ThemeState ThemeState { get; set; } = default!;
        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        [Inject] public CurrentUserState CurrentUserState { get; set; } = default!;
        [Inject] public PermissionState PermissionState { get; set; } = default!;
        [Inject] public UiBusyState BusyState { get; set; } = default!;
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
        private bool _isPrerendering = true;

        private bool CanViewDashboardMenu => CanViewSection(ShellNavigationCatalog.Dashboard);
        private bool CanViewLibraryMenu => CanViewSection(ShellNavigationCatalog.Library);
        private bool CanViewReportMenu => CanViewShellItem(ShellNavigationCatalog.Reports);
        private bool CanViewPermissionMenu => CanViewSection(ShellNavigationCatalog.Permission);
        private bool CanViewPeriodMenu => CanViewShellItem(ShellNavigationCatalog.PendingApproval)
            || CanViewShellItem(ShellNavigationCatalog.PeriodReview);
        private bool CanViewPricingMenu => CanViewShellItem(ShellNavigationCatalog.PriceLists)
            || CanViewShellItem(ShellNavigationCatalog.Prices);
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

            BusyState.Changed += OnBusyStateChanged;
            CurrentUserState.Changed += OnCurrentUserStateChanged;

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
            catch (Exception exception) when (IsExpectedJsInteropLifecycleException(exception))
            {
                return false;
            }
            catch (JSException)
            {
                return false;
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
            catch (Exception exception) when (IsExpectedJsInteropLifecycleException(exception))
            {
                return null;
            }
            catch (JSException)
            {
                return null;
            }
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
            PermissionState.Changed -= OnPermissionStateChanged;
            BusyState.Changed -= OnBusyStateChanged;
            CurrentUserState.Changed -= OnCurrentUserStateChanged;
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
            catch (Exception exception) when (IsExpectedJsInteropLifecycleException(exception))
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
            catch (Exception exception) when (IsExpectedJsInteropLifecycleException(exception))
            {
            }
            catch (JSException)
            {
            }
        }

        private static bool IsExpectedJsInteropLifecycleException(Exception exception) =>
            exception is InvalidOperationException
                or JSDisconnectedException
                or OperationCanceledException;

        public string GetUserInitials()
        {
            var name = CurrentUserState.Current?.FullName ?? string.Empty;
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
                var groupId = CurrentUserState.Current?.GroupId ?? Guid.Empty;
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

                return CurrentUserState.Current?.GroupName?.Trim() ?? string.Empty;
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
                        if (CanViewShellItem(ShellNavigationCatalog.MyOrders))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.MyOrders.LabelKey],
                                ShellNavigationCatalog.MyOrders.Path,
                                myOrdersActive));
                        }

                        if (CanViewShellItem(ShellNavigationCatalog.History))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.History.LabelKey],
                                ShellNavigationCatalog.History.Path,
                                tab == "1"));
                        }

                        if (CanViewShellItem(ShellNavigationCatalog.DepartmentSummary))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.DepartmentSummary.LabelKey],
                                ShellNavigationCatalog.DepartmentSummary.Path,
                                tab == "3"));
                        }

                        if (CanViewShellItem(ShellNavigationCatalog.Catalog))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.Catalog.LabelKey],
                                ShellNavigationCatalog.Catalog.Path,
                                tab == "2"));
                        }

                        var canSettle = CanViewShellItem(ShellNavigationCatalog.PeriodReview);
                        var canApproval = CanViewShellItem(ShellNavigationCatalog.PendingApproval);
                        if (canSettle || canApproval)
                        {
                            var periodPath = canSettle
                                ? ShellNavigationCatalog.PeriodReview.Path
                                : ShellNavigationCatalog.PendingApproval.Path;
                            var periodChildren = new List<VppHeaderSubTab>();
                            var pendingActive = tab == "5"
                                && string.Equals(periodTab, "pending", StringComparison.OrdinalIgnoreCase)
                                && canApproval;

                            if (canSettle)
                            {
                                periodChildren.Add(new(
                                    Loc[ShellNavigationCatalog.PeriodReview.LabelKey],
                                    ShellNavigationCatalog.PeriodReview.Path,
                                    tab == "5" && !pendingActive));
                            }

                            if (canApproval)
                            {
                                periodChildren.Add(new(
                                    Loc[ShellNavigationCatalog.PendingApproval.LabelKey],
                                    ShellNavigationCatalog.PendingApproval.Path,
                                    tab == "5" && (pendingActive || !canSettle)));
                            }

                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.PeriodOperations.LabelKey],
                                periodPath,
                                tab == "5",
                                periodChildren));
                        }

                        break;
                    case "library":
                        if (CanViewShellItem(ShellNavigationCatalog.Classes))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.Classes.LabelKey],
                                ShellNavigationCatalog.Classes.Path,
                                tab is null or "" or "0"));
                        }

                        if (CanViewShellItem(ShellNavigationCatalog.Categories))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.Categories.LabelKey],
                                ShellNavigationCatalog.Categories.Path,
                                tab == "1"));
                        }

                        if (CanViewShellItem(ShellNavigationCatalog.Items))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.Items.LabelKey],
                                ShellNavigationCatalog.Items.Path,
                                tab == "2"));
                        }

                        if (CanViewShellItem(ShellNavigationCatalog.Suppliers))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.Suppliers.LabelKey],
                                ShellNavigationCatalog.Suppliers.Path,
                                tab == "3"));
                        }

                        if (CanViewPricingMenu)
                        {
                            var canViewPriceLists = CanViewShellItem(ShellNavigationCatalog.PriceLists);
                            var canViewPrices = CanViewShellItem(ShellNavigationCatalog.Prices);
                            var pricingPath = canViewPriceLists
                                ? ShellNavigationCatalog.PriceLists.Path
                                : ShellNavigationCatalog.Prices.Path;
                            var pricingActive = tab is "4" or "6";
                            var pricesActive = pricingActive
                                && canViewPrices
                                && (tab == "4" || string.Equals(pricingTab, "prices", StringComparison.OrdinalIgnoreCase));
                            var pricingChildren = new List<VppHeaderSubTab>();

                            if (canViewPriceLists)
                            {
                                pricingChildren.Add(new(
                                    Loc[ShellNavigationCatalog.PriceLists.LabelKey],
                                    ShellNavigationCatalog.PriceLists.Path,
                                    pricingActive && !pricesActive));
                            }

                            if (canViewPrices)
                            {
                                pricingChildren.Add(new(
                                    Loc[ShellNavigationCatalog.Prices.LabelKey],
                                    ShellNavigationCatalog.Prices.Path,
                                    pricingActive && (pricesActive || !canViewPriceLists)));
                            }

                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.Pricing.LabelKey],
                                pricingPath,
                                pricingActive,
                                pricingChildren));
                        }

                        if (CanViewShellItem(ShellNavigationCatalog.Departments))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.Departments.LabelKey],
                                ShellNavigationCatalog.Departments.Path,
                                tab == "5"));
                        }

                        break;
                    case "permission":
                        if (CanViewShellItem(ShellNavigationCatalog.Users))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.Users.LabelKey],
                                ShellNavigationCatalog.Users.Path,
                                tab is null or "" or "0"));
                        }

                        if (CanViewShellItem(ShellNavigationCatalog.GroupsAndPermissions))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.GroupsAndPermissions.LabelKey],
                                ShellNavigationCatalog.GroupsAndPermissions.Path,
                                tab == "1"));
                        }

                        if (CanViewShellItem(ShellNavigationCatalog.SecurityAudit))
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.SecurityAudit.LabelKey],
                                ShellNavigationCatalog.SecurityAudit.Path,
                                tab == "2"));
                        }

                        break;
                    case "report":
                        if (CanViewReportMenu)
                        {
                            tabs.Add(new(
                                Loc[ShellNavigationCatalog.Reports.LabelKey],
                                ShellNavigationCatalog.Reports.Path,
                                true));
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
                    var t when t == Loc[ShellNavigationCatalog.Dashboard.LabelKey].Value =>
                        GetFirstAccessiblePath(ShellNavigationCatalog.Dashboard),
                    var t when t == Loc[ShellNavigationCatalog.Library.LabelKey].Value =>
                        GetFirstAccessiblePath(ShellNavigationCatalog.Library),
                    var t when t == Loc[ShellNavigationCatalog.Permission.LabelKey].Value =>
                        GetFirstAccessiblePath(ShellNavigationCatalog.Permission),
                    _ => null
                };

                if (defaultPath != null)
                {
                    NavigationManager.NavigateTo(defaultPath);
                }
            }
        }

        private bool CanViewSection(ShellNavigationCatalog.Section section)
            => (section.MenuPermission is null || PermissionState.HasMenuAccess(section.MenuPermission))
                && section.Items.Any(CanViewShellItem);

        private bool CanViewShellItem(ShellNavigationCatalog.Item item)
        {
            if (string.IsNullOrWhiteSpace(item.Permission))
            {
                return PermissionState.HasPageAccess(item.Route.PageCode);
            }

            return Permissions.IsActionCode(item.Permission)
                ? PermissionState.HasPermission(item.Permission)
                : PermissionState.HasVisibleComponent(item.Route.PageCode, item.Permission);
        }

        private void OnPermissionStateChanged()
        {
            _ = InvokeAsync(StateHasChanged);
        }

        private void OnCurrentUserStateChanged()
        {
            _ = InvokeAsync(StateHasChanged);
        }

        private void OnBusyStateChanged()
        {
            _ = InvokeAsync(StateHasChanged);
        }

        private string? GetFirstAccessiblePath(ShellNavigationCatalog.Section section)
        {
            foreach (var routeKey in section.DefaultRouteKeys)
            {
                var item = section.Items.First(candidate => candidate.RouteKey == routeKey);
                if (CanViewShellItem(item))
                {
                    return item.Path;
                }
            }

            return null;
        }
    }
}
