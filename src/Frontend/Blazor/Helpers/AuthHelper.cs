using gtas_vpp_fe.Services;
using gtas_vpp_fe.State;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace gtas_vpp_fe.Helpers
{
    public class AuthHelper
    {
        private readonly AuthenticationStateProvider _authProvider;
        private readonly IAPIServices _api;
        private readonly GlobalClass _glb;
        private readonly CurrentUserState _currentUserState;

        public AuthHelper(
            AuthenticationStateProvider auth,
            IAPIServices api,
            GlobalClass glb,
            CurrentUserState currentUserState)
        {
            _authProvider = auth;
            _api = api;
            _glb = glb;
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

            _glb.UserInfo = new AuthenticationResultDTO
            {
                UserID = currentUser.UserId,
                UserLogin = currentUser.UserLogin,
                FullName = currentUser.FullName,
                Email = currentUser.Email,
                IsAdmin = string.Equals(
                    currentUser.GroupCode,
                    gtas_vpp_shared.Constants.CanonicalRbac.SystemAdmin.GroupCode,
                    StringComparison.Ordinal),
                GroupId = currentUser.GroupId,
                GroupName = currentUser.GroupName,
                MemberCompanyCode = currentUser.MemberCompanyCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
                DepartmentName = currentUser.PrimaryDepartmentName,
                DepartmentCode = currentUser.PrimaryDepartmentCode,
                AccessToken = user.Claims.Get(ClaimKeys.AccessToken),
                SessionVersion = currentUser.SessionVersion,
                AccountStatus = currentUser.AccountStatus,
                MustChangePassword = currentUser.MustChangePassword
            };

            return (true, user.Claims);
        }

        public async Task<PermissionSnapshotResDTO> GetMyPermissionsAsync()
        {
            return await _api.GetFromApiAsync<PermissionSnapshotResDTO>("api/Auth/me/permissions")
                ?? new PermissionSnapshotResDTO();
        }
    }
}
