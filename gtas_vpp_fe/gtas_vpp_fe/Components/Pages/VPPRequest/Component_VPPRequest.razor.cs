using gtas_vpp_fe.Helpers;
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
        private sealed record DashboardTabDefinition(int QueryIndex, string Permission);

        private static readonly DashboardTabDefinition[] DashboardTabs =
        [
            new(0, Permissions.RequestOrder),
            new(1, Permissions.RequestHistory),
            new(2, Permissions.RequestProductCatalog),
            new(3, Permissions.RequestDepartmentSummary),
            new(4, Permissions.RequestAllOrdersSummary),
            new(5, Permissions.RequestAdminApproval)
        ];

        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private readonly TabPosition tabPosition = TabPosition.Top;
        private int SelectedIndex { get; set; }

        private IReadOnlyList<DashboardTabDefinition> AuthorizedTabs =>
            DashboardTabs.Where(tab => claims.HasPermission(tab.Permission)).ToArray();

        private bool HasAnyAuthorizedDashboardTab => AuthorizedTabs.Count > 0;

        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
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

        private void TabOnChange(int index)
        {
            var tab = AuthorizedTabs.ElementAtOrDefault(index);
            if (tab is null)
            {
                return;
            }

            SelectedIndex = index;
            NavigationManager.NavigateTo($"/dashboard?tab={tab.QueryIndex}");
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

            var requestedTab = GetRequestedTabIndex(location);
            var selectedIndex = requestedTab.HasValue
                ? authorizedTabs.ToList().FindIndex(tab => tab.QueryIndex == requestedTab.Value)
                : 0;

            SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
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

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
