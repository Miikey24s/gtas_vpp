using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using gtas_vpp_fe.Helpers;

namespace gtas_vpp_fe.Components.Pages.AI
{
    public partial class Component_AI
    {
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public string? Per { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();

        public int selectedTab = 0;

        public class TabPermissionInfo
        {
            public string ComponentCode { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
        }

        private List<TabPermissionInfo> TabPermissions = new();

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            UpdateTabPermissions();
            SetSelectedTabFromUrl();
        }

        private void UpdateTabPermissions()
        {
            TabPermissions.Clear();
            if (sp_Authentication_GetPermissionSinglePage?.List_Component == null) return;

            var compCodes = sp_Authentication_GetPermissionSinglePage.List_Component
                .Select(c => c.ComponentCode)
                .ToHashSet();

            if (compCodes.Contains(Permissions.RequestAIChat))
            {
                TabPermissions.Add(new TabPermissionInfo
                {
                    ComponentCode = Permissions.RequestAIChat,
                    Title = "AI Chat",
                    Icon = "chat",
                    Path = "chat"
                });
            }

            if (compCodes.Contains(Permissions.RequestAIVppChat))
            {
                TabPermissions.Add(new TabPermissionInfo
                {
                    ComponentCode = Permissions.RequestAIVppChat,
                    Title = "VPP Chat",
                    Icon = "inventory_2",
                    Path = "vpp-chat"
                });
            }
        }

        private void SetSelectedTabFromUrl()
        {
            if (string.IsNullOrEmpty(Per) || TabPermissions.Count == 0)
            {
                selectedTab = 0;
                return;
            }

            var index = TabPermissions.FindIndex(t => string.Equals(t.Path, Per, StringComparison.OrdinalIgnoreCase));
            selectedTab = index >= 0 ? index : 0;
        }

        public void OnChange(int index)
        {
            if (index >= 0 && index < TabPermissions.Count)
            {
                var targetPath = TabPermissions[index].Path;
                NavigationManager.NavigateTo($"/ai/{targetPath}");
            }
        }
    }
}
