using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using gtas_vpp_fe.Helpers;
using Microsoft.AspNetCore.Components.Routing;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Permission
{
    public partial class Component_Permission
    {
        [Parameter] public string? Per { get; set; }
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();
        [Inject] NavigationManager NavigationManager { get; set; } = default!;
        TabPosition tabPosition = TabPosition.Top;
        int SelectedIndex = 0;
        List<string> libStrings = new List<string> { "user", "pagepermission" };
        private bool HasVisibleComponent(string componentCode)
            => sp_Authentication_GetPermissionSinglePage.List_Component.Any(x => x.ComponentCode == componentCode && x.IsVisible);

        private bool HasAdminView => HasVisibleComponent(Config.Page_ComponentCode.ComponentCode.AdminView);
        private bool HasPermissionView => HasVisibleComponent(Config.Page_ComponentCode.ComponentCode.PermissionViewable);
        private bool HasAnyVisiblePermissionTab => HasAdminView || HasPermissionView;

        private int ResolveTabIndex(string? tab)
        {
            var index = libStrings.IndexOf(tab?.ToLower() ?? "user");
            if (index == 0 && HasAdminView)
            {
                return index;
            }

            if (index == 1 && (HasPermissionView || HasAdminView))
            {
                return index;
            }

            if (HasAdminView)
            {
                return 0;
            }

            if (HasPermissionView)
            {
                return 1;
            }

            return 0;
        }

        protected override async Task OnInitializedAsync()
        {
            SelectedIndex = ResolveTabIndex(Per);
            NavigationManager.LocationChanged += OnLocationChanged;
        }
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
            NavigationManager.NavigateTo($"/permission/{libStrings[index]}");
        }
    }
}

