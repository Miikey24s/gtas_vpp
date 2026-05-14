using gtas_vpp_fe.Components.Pages;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Lib
{
    public partial class Page_Library : PermissionAwarePageBase, IDisposable
    {
        private static readonly string LibraryPageCode = Config.Page_ComponentCode.PageCode.Library;
        private static readonly PermissionPageOptions PageOptions = new(
            LibraryPageCode,
            "You do not have permission to access Library.",
            ErrorDetailPrefix: "Error when call api sp_Library_GetL01Class:");

        [Parameter] public string? Lib { get; set; }
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
            }
        }
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            
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
