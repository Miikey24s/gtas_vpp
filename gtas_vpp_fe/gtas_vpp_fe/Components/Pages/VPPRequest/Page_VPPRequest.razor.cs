using gtas_vpp_fe.Helpers;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest
{
    public partial class Page_VPPRequest
    {
        private static readonly Dictionary<string, int> LegacyDashboardTabs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["orders"] = 0,
            ["history"] = 1,
            ["products"] = 2,
            ["departments"] = 3,
            ["all-orders"] = 4,
            ["admin-approval"] = 5
        };

        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        [Parameter] public string? Per { get; set; }

        public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            try
            {
                await LoadAuthenticationState();
            }
            catch (Exception)
            {
                // Design-time or API unavailable.
            }
        }

        protected override void OnParametersSet()
        {
            if (!string.IsNullOrWhiteSpace(Per) &&
                LegacyDashboardTabs.TryGetValue(Per, out var tabIndex))
            {
                NavigationManager.NavigateTo($"/dashboard?tab={tabIndex}", replace: true);
            }
        }

        private async Task LoadAuthenticationState()
        {
            var (isAuthenticated, userClaims) = await AuthHelper.EnsureAuthenticatedAsync();
            if (!isAuthenticated)
            {
                NavigationManager.NavigateTo("logoutprocess", true);
                return;
            }

            claims = userClaims;
        }
    }
}
