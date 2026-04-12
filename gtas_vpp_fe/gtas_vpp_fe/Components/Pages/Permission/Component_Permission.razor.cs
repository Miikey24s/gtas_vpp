using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Permission
{
    public partial class Component_Permission
    {
        [Parameter] public string Per { get; set; }
        [Parameter] public IEnumerable<Claim> claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; }
        [Inject] NavigationManager? NavigationManager { get; set; }
        TabPosition tabPosition = TabPosition.Top;
        int SelectedIndex = 0;
        List<string> libStrings = new List<string> { "user", "pagepermission" };
        protected override async Task OnInitializedAsync()
        {
            SelectedIndex = libStrings.IndexOf(Per?.ToLower() ?? "user");
            NavigationManager.LocationChanged += OnLocationChanged;
        }
        public void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            var uri = new Uri(args.Location);
            if (uri.AbsolutePath.Contains("permission"))
            {
                string currtab = uri.AbsolutePath.Split('/')[uri.AbsolutePath.Split('/').Length - 1];
                SelectedIndex = libStrings.IndexOf(currtab?.ToLower() ?? "user");
                StateHasChanged();
            }
        }
        void TabOnChange(int index)
        {
            NavigationManager.NavigateTo($"/permission/{libStrings[index]}");
        }
    }
}

