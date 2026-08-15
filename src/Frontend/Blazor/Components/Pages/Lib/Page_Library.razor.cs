// PAGE LOGIC: Lib/Page_Library.razor.cs
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
            ErrorDetailPrefix: "Could not load page permissions:");

        [Parameter] public string? Lib { get; set; }
        public IEnumerable<Claim> claims { get; set; } = new List<Claim>();
        public PagePermissionResDTO PagePermissionResDTO { get; set; } = new PagePermissionResDTO();

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
        private void OnPermissionStateChanged()
        {
            HandlePermissionStateChanged(PageOptions);
        }

        protected override void ApplyClaims(IEnumerable<Claim> newClaims)
        {
            claims = newClaims;
        }

        protected override void ApplyPagePermission(PagePermissionResDTO permission)
        {
            PagePermissionResDTO = permission;
        }

        public void Dispose()
        {
            PagePermissionState.Changed -= OnPermissionStateChanged;
        }
    }
}
