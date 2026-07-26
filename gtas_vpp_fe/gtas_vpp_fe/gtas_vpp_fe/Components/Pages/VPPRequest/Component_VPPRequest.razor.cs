using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest
{
    public partial class Component_VPPRequest : IDisposable
    {
        private const int ManagementTabIndex = 3;

        private sealed record DashboardTabDefinition(int QueryIndex, params string[] Permissions);

        // D2/D8: tab 4 (Tổng hợp toàn công ty) đã gỡ — nội dung là chế độ "Theo đơn"
        // của bước Gom nhu cầu (tab=5&periodTab=demand). URL cũ được redirect bên dưới.
        private static readonly DashboardTabDefinition[] DashboardTabs =
        [
            new(0, Permissions.RequestOrder),
            new(1, Permissions.RequestHistory),
            new(2, Permissions.RequestProductCatalog),
            new(ManagementTabIndex, Permissions.RequestDepartmentSummary),
            new(5, Permissions.RequestAdminApproval, Permissions.PeriodSettle)
        ];

        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private PermissionState PermissionState { get; set; } = default!;

        private readonly TabPosition tabPosition = TabPosition.Top;
        private int SelectedIndex { get; set; }

        private IReadOnlyList<DashboardTabDefinition> AuthorizedTabs =>
            DashboardTabs.Where(tab => CanViewDashboardTab(tab.Permissions)).ToArray();

        private bool HasAnyAuthorizedDashboardTab => AuthorizedTabs.Count > 0;

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
            PermissionState.Changed += OnPermissionStateChanged;
            await PermissionState.EnsureLoadedAsync();
            SetSelectedIndexFromUri(NavigationManager.Uri);
        }

        protected override void OnParametersSet()
        {
            SetSelectedIndexFromUri(NavigationManager.Uri);
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            if (!IsDashboardUri(args.Location))
            {
                return;
            }

            SetSelectedIndexFromUri(args.Location);
            _ = InvokeAsync(StateHasChanged);
        }

        private void OnPermissionStateChanged()
        {
            SetSelectedIndexFromUri(NavigationManager.Uri);
            _ = InvokeAsync(StateHasChanged);
        }

        private void TabOnChange(int index)
        {
            var tab = AuthorizedTabs.ElementAtOrDefault(index);
            if (tab is null)
            {
                return;
            }

            SelectedIndex = index;
            NavigationManager.NavigateTo(tab.QueryIndex == ManagementTabIndex
                ? $"/dashboard?tab={ManagementTabIndex}&managementTab=department"
                : $"/dashboard?tab={tab.QueryIndex}");
        }

        private bool IsActiveTab(int queryIndex)
        {
            return AuthorizedTabs.ElementAtOrDefault(SelectedIndex)?.QueryIndex == queryIndex;
        }

        private void SetSelectedIndexFromUri(string location)
        {
            var authorizedTabs = AuthorizedTabs;
            if (authorizedTabs.Count == 0)
            {
                SelectedIndex = 0;
                return;
            }

            // Redirect URL cũ của tab "Tổng hợp toàn công ty" (tab=4 / managementTab=all)
            // sang chế độ "Theo đơn" của bước Gom nhu cầu để bookmark/thông báo cũ không chết.
            if (IsLegacyAllOrdersUri(location))
            {
                NavigationManager.NavigateTo("/dashboard?tab=5&periodTab=demand", replace: true);
                return;
            }

            var requestedTab = GetRequestedTabIndex(location);
            var selectedIndex = requestedTab.HasValue
                ? authorizedTabs.ToList().FindIndex(tab => tab.QueryIndex == requestedTab.Value)
                : 0;

            SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        private bool IsLegacyAllOrdersUri(string location)
        {
            var uri = NavigationManager.ToAbsoluteUri(location);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("managementTab", out var managementValues)
                && string.Equals(managementValues.FirstOrDefault(), "all", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return query.TryGetValue("tab", out var tabValues)
                && int.TryParse(tabValues.FirstOrDefault(), out var tabIndex)
                && tabIndex == 4;
        }

        private int? GetRequestedTabIndex(string location)
        {
            var uri = NavigationManager.ToAbsoluteUri(location);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("tab", out var values) &&
                int.TryParse(values.FirstOrDefault(), out var tabIndex))
            {
                return tabIndex;
            }

            return null;
        }

        private bool IsDashboardUri(string location)
        {
            var uri = NavigationManager.ToAbsoluteUri(location);
            return uri.AbsolutePath.TrimEnd('/').EndsWith("/dashboard", StringComparison.OrdinalIgnoreCase);
        }

        private bool CanViewDashboardTab(params string[] permissions)
        {
            return permissions.Any(permission =>
                PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Dashboard, permission));
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
            PermissionState.Changed -= OnPermissionStateChanged;
        }
    }
}
