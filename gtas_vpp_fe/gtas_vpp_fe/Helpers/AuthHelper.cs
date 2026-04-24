using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace gtas_vpp_fe.Helpers
{
    public class AuthHelper
    {
        private readonly AuthenticationStateProvider _authProvider;
        private readonly IAPIServices _api;
        private readonly GlobalClass _glb;

        public AuthHelper(AuthenticationStateProvider auth, IAPIServices api, GlobalClass glb)
        {
            _authProvider = auth;
            _api = api;
            _glb = glb;
        }

        public async Task<(bool IsAuthenticated, IEnumerable<Claim> Claims)> EnsureAuthenticatedAsync()
        {
            var state = await _authProvider.GetAuthenticationStateAsync();
            var user = state.User;

            if (user.Identity == null || user.Identity.IsAuthenticated == false)
            {
                return (false, Array.Empty<Claim>());
            }
            
            try
            {
                _glb.UserInfo = user.Claims.Claims_To_sp_AuthenticationLogin();
                _glb.Server = user.Claims.Get(ClaimKeys.Server);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ToDto ERROR: {ex}");
                throw;
            }

            return (true, user.Claims);
        }
        
        public Task<sp_Authentication_GetPermissionSinglePage> LoadGlbPermissionAsync(string pageCode)
        {
            if (_glb.UserInfo == null || _glb.UserInfo.UserID <= 0)
            {
                return Task.FromResult(new sp_Authentication_GetPermissionSinglePage());
            }

            return GetPermissionSinglePageAsync(_glb.UserInfo.UserID, pageCode);
        }

        public async Task<sp_Authentication_GetPermissionSinglePage> GetPermissionSinglePageAsync(int userId, string pageCode)
        {
            string spType = nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_GetPermissionSinglePage);

            var permissionResult = await _api.APIFrom_sp_Authen_Typed<sp_Authentication_GetPermissionSinglePage>(
                spType,
                new { userId, pageCode });

            return permissionResult ?? new sp_Authentication_GetPermissionSinglePage();
        }
    }
}
