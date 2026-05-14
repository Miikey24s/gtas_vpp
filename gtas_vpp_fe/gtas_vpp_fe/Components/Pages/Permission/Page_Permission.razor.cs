using gtas_vpp_fe.Components.Pages;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Permission
{
    public partial class Page_Permission : PermissionAwarePageBase, IDisposable
    {
        private static readonly string PermissionPageCode = Config.Page_ComponentCode.PageCode.Permission;
        private static readonly PermissionPageOptions PageOptions = new(
            PermissionPageCode,
            "You do not have permission to access this page.",
            ErrorDetailPrefix: "Error when call api sp_Library_GetL01Class:");

        [Parameter] public string? Per { get; set; }
        public IEnumerable<Claim> claims { get; set; } = new List<Claim>();
        public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new sp_Authentication_GetPermissionSinglePage();
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
                // Design-time hoặc API chưa sẵn sàng
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

        protected override void ApplyPagePermission(sp_Authentication_GetPermissionSinglePage permission)
        {
            sp_Authentication_GetPermissionSinglePage = permission;
        }

        public void Dispose()
        {
            PagePermissionState.Changed -= OnPermissionStateChanged;
        }
    }
}

