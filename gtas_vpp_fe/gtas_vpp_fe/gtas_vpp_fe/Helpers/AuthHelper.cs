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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ToDto ERROR: {ex}");
                throw;
            }

            return (true, user.Claims);
        }

        public async Task<PermissionSnapshotResDTO> GetMyPermissionsAsync()
        {
            return await _api.GetFromApiAsync<PermissionSnapshotResDTO>("api/Auth/me/permissions")
                ?? new PermissionSnapshotResDTO();
        }
    }
}
