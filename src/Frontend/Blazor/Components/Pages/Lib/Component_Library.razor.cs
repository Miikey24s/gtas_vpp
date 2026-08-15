// PAGE LOGIC: Lib/Component_Library.razor.cs
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Lib
{
    public partial class Component_Library : IDisposable
    {
        private const int PriceTabIndex = 4;
        private const int DepartmentTabIndex = 5;
        private const int PricingTabIndex = 6;
        private const int PriceListTabIndex = 6;

        private sealed record LibraryTabDefinition(int QueryIndex, params string[] Permissions);

        private static readonly LibraryTabDefinition[] LibraryTabs =
        [
            new(0, Permissions.LibraryClass),
            new(1, Permissions.LibraryCategory),
            new(2, Permissions.LibraryItem),
            new(3, Permissions.LibrarySupplier),
            new(PricingTabIndex, Permissions.LibraryPriceList, Permissions.LibraryPrice),
            new(DepartmentTabIndex, Permissions.LibraryDepartment)
        ];

        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private PermissionState PermissionState { get; set; } = default!;

        private readonly TabPosition tabPosition = TabPosition.Top;
        private IReadOnlyList<LibraryTabDefinition> authorizedTabs = [];
        private IReadOnlyList<int> authorizedPricingTabs = [];
        private int SelectedIndex { get; set; }
        private int PricingSelectedIndex { get; set; }

        private IReadOnlyList<LibraryTabDefinition> AuthorizedTabs => authorizedTabs;

        private bool HasAnyVisibleLibraryTab => AuthorizedTabs.Count > 0;

        private IReadOnlyList<int> AuthorizedPricingTabs => authorizedPricingTabs;

        private bool ShowPricingNavigation => AuthorizedPricingTabs.Count > 0;

        private string DefaultPricingPath => BuildPricingUrl(AuthorizedPricingTabs.FirstOrDefault());

        private IReadOnlyList<VppHeaderSubTab> PricingHeaderTabs => AuthorizedPricingTabs
            .Select(tab => new VppHeaderSubTab(
                tab == PriceTabIndex ? Loc["Prices"].Value : Loc["PriceLists"].Value,
                BuildPricingUrl(tab),
                IsActivePricingTab(tab)))
            .ToArray();

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
            PermissionState.Changed += OnPermissionStateChanged;
            await PermissionState.EnsureLoadedAsync();
            RefreshAuthorizedTabs();
            SetSelectedIndexFromUri(NavigationManager.Uri);
        }

        protected override void OnParametersSet()
        {
            SetSelectedIndexFromUri(NavigationManager.Uri);
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            if (!IsLibraryUri(args.Location))
            {
                return;
            }

            SetSelectedIndexFromUri(args.Location);
            _ = InvokeAsync(StateHasChanged);
        }

        private void OnPermissionStateChanged()
        {
            RefreshAuthorizedTabs();
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
            NavigationManager.NavigateTo(tab.QueryIndex == PricingTabIndex
                ? DefaultPricingPath
                : $"/library?tab={tab.QueryIndex}");
        }

        private bool IsActiveTab(int queryIndex)
        {
            return AuthorizedTabs.ElementAtOrDefault(SelectedIndex)?.QueryIndex == queryIndex;
        }

        private bool IsActivePricingTab(int queryIndex)
        {
            return AuthorizedPricingTabs.ElementAtOrDefault(PricingSelectedIndex) == queryIndex;
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
            var normalizedRequestedTab = requestedTab == PriceTabIndex ? PricingTabIndex : requestedTab;
            var selectedIndex = normalizedRequestedTab.HasValue
                ? FindTabIndex(authorizedTabs, normalizedRequestedTab.Value)
                : 0;

            SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            SetPricingSelectedIndexFromUri(location, requestedTab);

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

        private void SetPricingSelectedIndexFromUri(string location, int? requestedTab)
        {
            var pricingTabs = AuthorizedPricingTabs;
            if (pricingTabs.Count == 0)
            {
                PricingSelectedIndex = 0;
                return;
            }

            var requestedPricingTab = GetRequestedPricingTabIndex(location, requestedTab);
            var selectedIndex = requestedPricingTab.HasValue
                ? FindPricingTabIndex(pricingTabs, requestedPricingTab.Value)
                : 0;

            PricingSelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        private int? GetRequestedPricingTabIndex(string location, int? requestedTab)
        {
            var uri = NavigationManager.ToAbsoluteUri(location);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("pricingTab", out var values))
            {
                return values.FirstOrDefault()?.ToLowerInvariant() switch
                {
                    "prices" => PriceTabIndex,
                    "price-lists" => PriceListTabIndex,
                    _ => null
                };
            }

            if (requestedTab == PriceTabIndex)
            {
                return PriceTabIndex;
            }

            if (requestedTab == PriceListTabIndex)
            {
                return PriceListTabIndex;
            }

            return null;
        }

        private bool IsLibraryUri(string location)
        {
            var uri = NavigationManager.ToAbsoluteUri(location);
            return uri.AbsolutePath.TrimEnd('/').EndsWith("/library", StringComparison.OrdinalIgnoreCase);
        }

        private bool CanViewLibraryTab(params string[] permissions)
        {
            return permissions.Any(permission =>
                PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Library, permission));
        }

        private bool HasAuthorizedLibraryTab(int queryIndex)
            => AuthorizedTabs.Any(tab => tab.QueryIndex == queryIndex);

        private void RefreshAuthorizedTabs()
        {
            authorizedTabs = LibraryTabs
                .Where(tab => CanViewLibraryTab(tab.Permissions))
                .OrderBy(GetVisualTabOrder)
                .ToArray();

            var pricingTabs = new List<int>(2);
            if (CanViewLibraryTab(Permissions.LibraryPriceList))
            {
                pricingTabs.Add(PriceListTabIndex);
            }
            if (CanViewLibraryTab(Permissions.LibraryPrice))
            {
                pricingTabs.Add(PriceTabIndex);
            }
            authorizedPricingTabs = pricingTabs;
        }

        private static int FindTabIndex(IReadOnlyList<LibraryTabDefinition> tabs, int queryIndex)
        {
            for (var index = 0; index < tabs.Count; index++)
            {
                if (tabs[index].QueryIndex == queryIndex)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindPricingTabIndex(IReadOnlyList<int> tabs, int queryIndex)
        {
            for (var index = 0; index < tabs.Count; index++)
            {
                if (tabs[index] == queryIndex)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int GetVisualTabOrder(LibraryTabDefinition tab)
        {
            return tab.QueryIndex switch
            {
                PricingTabIndex => 4,
                DepartmentTabIndex => 5,
                _ => tab.QueryIndex
            };
        }

        private static string BuildPricingUrl(int pricingTab)
        {
            var pricingTabQuery = pricingTab == PriceTabIndex ? "prices" : "price-lists";

            return $"/library?tab={PricingTabIndex}&pricingTab={pricingTabQuery}";
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
            PermissionState.Changed -= OnPermissionStateChanged;
        }
    }
}
