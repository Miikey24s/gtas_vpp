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
        private const int DepartmentOrdersTabIndex = 3;
        private const int AllOrdersTabIndex = 4;

        private sealed record DashboardTabDefinition(int QueryIndex, params string[] Permissions);

        private static readonly DashboardTabDefinition[] DashboardTabs =
        [
            new(0, Permissions.RequestOrder),
            new(1, Permissions.RequestHistory),
            new(2, Permissions.RequestProductCatalog),
            new(ManagementTabIndex, Permissions.RequestDepartmentSummary, Permissions.RequestAllOrdersSummary),
            new(5, Permissions.RequestAdminApproval, Permissions.PeriodSettle)
        ];

        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private PermissionState PermissionState { get; set; } = default!;

        private readonly TabPosition tabPosition = TabPosition.Top;
        private int SelectedIndex { get; set; }
        private int ManagementSelectedIndex { get; set; }

        private IReadOnlyList<DashboardTabDefinition> AuthorizedTabs =>
            DashboardTabs.Where(tab => CanViewDashboardTab(tab.Permissions)).ToArray();

        private bool HasAnyAuthorizedDashboardTab => AuthorizedTabs.Count > 0;

        private IReadOnlyList<int> AuthorizedManagementTabs
        {
            get
            {
                var tabs = new List<int>();

                if (CanShowDepartmentOrders)
                {
                    tabs.Add(DepartmentOrdersTabIndex);
                }

                if (CanShowAllOrders)
                {
                    tabs.Add(AllOrdersTabIndex);
                }

                return tabs;
            }
        }

        private bool CanShowDepartmentOrders => CanViewDashboardTab(Permissions.RequestDepartmentSummary);

        private bool CanShowAllOrders => CanViewDashboardTab(Permissions.RequestAllOrdersSummary);

        private bool CanShowManagementTabs => AuthorizedManagementTabs.Count > 1;

        private string ManagementTabText => CanShowManagementTabs
            ? Loc["Management"].Value
            : CanShowDepartmentOrders
                ? Loc["DepartmentSummary"].Value
                : Loc["AllOrdersSummary"].Value;

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
                ? BuildManagementUrl()
                : $"/dashboard?tab={tab.QueryIndex}");
        }

        private bool IsActiveTab(int queryIndex)
        {
            return AuthorizedTabs.ElementAtOrDefault(SelectedIndex)?.QueryIndex == queryIndex;
        }

        private void ManagementTabOnChange(int index)
        {
            var managementTabs = AuthorizedManagementTabs;
            if (index < 0 || index >= managementTabs.Count)
            {
                return;
            }

            ManagementSelectedIndex = index;
            NavigationManager.NavigateTo(BuildManagementUrl());
        }

        private bool IsActiveManagementTab(int queryIndex)
        {
            return AuthorizedManagementTabs.ElementAtOrDefault(ManagementSelectedIndex) == queryIndex;
        }

        private void SetSelectedIndexFromUri(string location)
        {
            var authorizedTabs = AuthorizedTabs;
            if (authorizedTabs.Count == 0)
            {
                SelectedIndex = 0;
                return;
            }

            var requestedTab = GetRequestedTabIndex(location);
            var normalizedRequestedTab = requestedTab == AllOrdersTabIndex ? ManagementTabIndex : requestedTab;
            var selectedIndex = normalizedRequestedTab.HasValue
                ? authorizedTabs.ToList().FindIndex(tab => tab.QueryIndex == normalizedRequestedTab.Value)
                : 0;

            SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            SetManagementSelectedIndexFromUri(location, requestedTab);
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

        private void SetManagementSelectedIndexFromUri(string location, int? requestedTab)
        {
            var managementTabs = AuthorizedManagementTabs;
            if (managementTabs.Count == 0)
            {
                ManagementSelectedIndex = 0;
                return;
            }

            var requestedManagementTab = GetRequestedManagementTabIndex(location, requestedTab);
            var selectedIndex = requestedManagementTab.HasValue
                ? managementTabs.ToList().FindIndex(tab => tab == requestedManagementTab.Value)
                : 0;

            ManagementSelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        private int? GetRequestedManagementTabIndex(string location, int? requestedTab)
        {
            var uri = NavigationManager.ToAbsoluteUri(location);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("managementTab", out var values))
            {
                return values.FirstOrDefault()?.ToLowerInvariant() switch
                {
                    "all" => AllOrdersTabIndex,
                    "department" => DepartmentOrdersTabIndex,
                    _ => null
                };
            }

            if (requestedTab == AllOrdersTabIndex)
            {
                return AllOrdersTabIndex;
            }

            if (requestedTab == DepartmentOrdersTabIndex)
            {
                return DepartmentOrdersTabIndex;
            }

            return null;
        }

        private string BuildManagementUrl()
        {
            var managementTab = AuthorizedManagementTabs.ElementAtOrDefault(ManagementSelectedIndex);
            var managementTabQuery = managementTab == AllOrdersTabIndex ? "all" : "department";

            return $"/dashboard?tab={ManagementTabIndex}&managementTab={managementTabQuery}";
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
