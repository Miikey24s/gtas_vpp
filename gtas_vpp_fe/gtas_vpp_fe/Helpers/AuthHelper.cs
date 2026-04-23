using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
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
        private static readonly SemaphoreSlim _departmentLoadLock = new SemaphoreSlim(1, 1);

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
                
                // Only load department location once per session/scope with thread safety
                if (!_glb.IsDepartmentLocationLoaded)
                {
                    await _departmentLoadLock.WaitAsync();
                    try
                    {
                        // Double-check after acquiring lock
                        if (!_glb.IsDepartmentLocationLoaded)
                        {
                            bool isCodeMissing = string.IsNullOrWhiteSpace(_glb.UserInfo.DepartmentCode);
                            bool isNameMissing = string.IsNullOrWhiteSpace(_glb.UserInfo.DepartmentName);
                            
                            if (isCodeMissing || isNameMissing)
                            {
                                try
                                {
                                    // Get P04_UserGroup by UserId first
                                    var userGroups = await _api.GetFromApiAsync<List<gtas_vpp_shared.DTOs.Res.Auth.P04_UserGroupResDTO>>(
                                        $"api/Permission/user-groups?userId={_glb.UserInfo.UserID}");
                                    
                                    var userGroup = userGroups?.FirstOrDefault();
                                    
                                    if (userGroup != null && userGroup.LEX02_CompanyDepartmentLocationId != Guid.Empty)
                                    {
                                        // Get LEX02 from Library endpoint
                                        var LEX02_CompanyDepartmentLocation = await _api.GetFromApiAsync<LEX02_CompanyDepartmentLocationResDTO>(
                                            $"api/Library/lex02/{userGroup.LEX02_CompanyDepartmentLocationId}");

                                        if (LEX02_CompanyDepartmentLocation != null)
                                        {
                                            if (isCodeMissing)
                                            {
                                                _glb.UserInfo.DepartmentCode = LEX02_CompanyDepartmentLocation.LEX02Code; 
                                            }

                                            if (isNameMissing)
                                            {
                                                _glb.UserInfo.DepartmentName = LEX02_CompanyDepartmentLocation.LEX02Name;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error loading department location: {ex.Message}");
                                    // Don't throw, just log - allow user to continue
                                }
                            }
                            
                            // Mark as loaded to prevent multiple calls
                            _glb.IsDepartmentLocationLoaded = true;
                        }
                    }
                    finally
                    {
                        _departmentLoadLock.Release();
                    }
                }
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
