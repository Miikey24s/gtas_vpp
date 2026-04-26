using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using gtas_vpp_fe.Helpers;
using Microsoft.AspNetCore.Components.Routing;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Permission
{
    public partial class Component_Permission : IDisposable
    {
        private const string UserTab = "user";
        private const string PagePermissionTab = "pagepermission";

        [Parameter] public string? Per { get; set; }
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();
        [Inject] NavigationManager NavigationManager { get; set; } = default!;
        TabPosition tabPosition = TabPosition.Top;
        int SelectedIndex = 0;
        private IReadOnlyList<string> AuthorizedTabs
        {
            get
            {
                var tabs = new List<string>();

                if (HasAdminView)
                {
                    tabs.Add(UserTab);
                }

                if (HasPermissionView || HasAdminView)
                {
                    tabs.Add(PagePermissionTab);
                }

                return tabs;
            }
        }

        private bool HasVisibleComponent(string componentCode)
            => sp_Authentication_GetPermissionSinglePage.List_Component.Any(x => x.ComponentCode == componentCode && x.IsVisible);

        private bool HasAdminView => HasVisibleComponent(Config.Page_ComponentCode.ComponentCode.AdminView);
        private bool HasPermissionView => HasVisibleComponent(Config.Page_ComponentCode.ComponentCode.PermissionViewable);
        private bool HasAnyVisiblePermissionTab => AuthorizedTabs.Count > 0;

        private int ResolveTabIndex(string? tab)
        {
            var tabs = AuthorizedTabs;
            if (tabs.Count == 0)
            {
                return 0;
            }

            var requestedTab = string.IsNullOrWhiteSpace(tab) ? null : tab.ToLowerInvariant();
            var index = requestedTab is null ? 0 : tabs.ToList().FindIndex(x => x == requestedTab);

            return index >= 0 ? index : 0;
        }

        protected override void OnInitialized()
        {
            SelectedIndex = ResolveTabIndex(Per);
            NavigationManager.LocationChanged += OnLocationChanged;
        }

        protected override void OnParametersSet()
        {
            SelectedIndex = ResolveTabIndex(Per);
        }

        private bool IsActiveTab(string tab)
            => AuthorizedTabs.ElementAtOrDefault(SelectedIndex) == tab;

        public void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            var uri = new Uri(args.Location);
            if (uri.AbsolutePath.Contains("permission"))
            {
                string currtab = uri.AbsolutePath.Split('/')[uri.AbsolutePath.Split('/').Length - 1];
                SelectedIndex = ResolveTabIndex(currtab);
                StateHasChanged();
            }
        }
        void TabOnChange(int index)
        {
            var tab = AuthorizedTabs.ElementAtOrDefault(index);
            if (tab is null)
            {
                return;
            }

            SelectedIndex = index;
            NavigationManager.NavigateTo($"/permission/{tab}");
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
