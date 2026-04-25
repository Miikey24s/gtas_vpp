using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using gtas_vpp_fe.Helpers;
using Microsoft.AspNetCore.Components.Routing;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest
{
    public partial class Component_VPPRequest
    {
        [Parameter] public string? Per { get; set; }
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();
        [Inject] NavigationManager NavigationManager { get; set; } = default!;
        TabPosition tabPosition = TabPosition.Top;
        int SelectedIndex = 0;
        List<string> libStrings = new List<string> { "orders", "history", "products", "departments", "all-orders", "admin-approval" };
        private bool HasVisibleComponent(string componentCode)
            => sp_Authentication_GetPermissionSinglePage.List_Component.Any(x => x.ComponentCode == componentCode && x.IsVisible);

        private bool HasAnyVisibleDashboardTab => DashboardTabCodes.Any(HasVisibleComponent);

        private string[] DashboardTabCodes =>
        [
            Config.Page_ComponentCode.ComponentCode.RequestOrder,
            Config.Page_ComponentCode.ComponentCode.RequestHistory,
            Config.Page_ComponentCode.ComponentCode.RequestProductCatalog,
            Config.Page_ComponentCode.ComponentCode.RequestDepartmentSummary,
            Config.Page_ComponentCode.ComponentCode.RequestAllOrdersSummary,
            Config.Page_ComponentCode.ComponentCode.RequestApproval
        ];

        private int ResolveTabIndex(string? tab)
        {
            var index = libStrings.IndexOf(tab?.ToLower() ?? "orders");
            if (index >= 0 && HasVisibleComponent(DashboardTabCodes[index]))
            {
                return index;
            }

            for (var i = 0; i < DashboardTabCodes.Length; i++)
            {
                if (HasVisibleComponent(DashboardTabCodes[i]))
                {
                    return i;
                }
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
            if (uri.AbsolutePath.Contains("dashboard"))
            {
                string currtab = uri.AbsolutePath.Split('/')[uri.AbsolutePath.Split('/').Length - 1];
                SelectedIndex = ResolveTabIndex(currtab);
                StateHasChanged();
            }
        }
        void TabOnChange(int index)
        {
            NavigationManager.NavigateTo($"/dashboard/{libStrings[index]}");
        }
    }
}

