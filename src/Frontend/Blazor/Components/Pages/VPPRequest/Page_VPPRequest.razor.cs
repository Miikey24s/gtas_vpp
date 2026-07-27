using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.Pages;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest
{
    public partial class Page_VPPRequest : PermissionAwarePageBase, IDisposable
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

        private static readonly string DashboardPageCode = Config.Page_ComponentCode.PageCode.Dashboard;
        private static readonly PermissionPageOptions PageOptions = new(
            DashboardPageCode,
            string.Empty,
            RedirectPath: "/",
            NotifyOnAccessDenied: false);
        [Parameter] public string? Per { get; set; }

        public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            try
            {
                if (await LoadPageAccessAsync(PageOptions))
                {
                    PagePermissionState.Changed += OnPermissionStateChanged;
                }
            }
            catch (Exception)
            {
                // Design-time hoặc API chưa sẵn sàng.
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

        private void OnPermissionStateChanged()
        {
            HandlePermissionStateChanged(PageOptions);
        }

        protected override void ApplyClaims(IEnumerable<Claim> newClaims)
        {
            claims = newClaims;
        }

        public void Dispose()
        {
            PagePermissionState.Changed -= OnPermissionStateChanged;
        }
    }
}
