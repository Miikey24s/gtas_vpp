using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace gtas_vpp_fe.Helpers
{
    public class AuthHelper
    {
        private readonly AuthenticationStateProvider _authProvider;
        private readonly IAPIServices _api;
        private readonly CurrentUserState _currentUserState;

        public AuthHelper(
            AuthenticationStateProvider auth,
            IAPIServices api,
            CurrentUserState currentUserState)
        {
            _authProvider = auth;
            _api = api;
            _currentUserState = currentUserState;
        }

        public async Task<(bool IsAuthenticated, IEnumerable<Claim> Claims)> EnsureAuthenticatedAsync()
        {
            var state = await _authProvider.GetAuthenticationStateAsync();
            var user = state.User;

            if (user.Identity == null || user.Identity.IsAuthenticated == false)
            {
                return (false, Array.Empty<Claim>());
            }

            var currentUser = await _currentUserState.EnsureLoadedAsync();
            if (currentUser is null)
            {
                return (false, Array.Empty<Claim>());
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
