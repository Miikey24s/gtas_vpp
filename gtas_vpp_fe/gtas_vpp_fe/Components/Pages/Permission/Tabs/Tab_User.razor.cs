using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using gtas_vpp_fe.Components.Pages.Lib;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Extensions;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;
using System.Text.Json;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs
{
    public partial class Tab_User
    {
        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage? sp_Authentication_GetPermissionSinglePage { get; set; }
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        private string SearchText { get; set; } = string.Empty;
        public List<sp_Authentication_TabUser_UserList> _sp_Authentication_TabUser_UserList { get; set; } = new List<sp_Authentication_TabUser_UserList>();
        public IList<sp_Authentication_TabUser_UserList> selected_UserList { get; set; } = new List<sp_Authentication_TabUser_UserList>();
        public RadzenDataGrid<sp_Authentication_TabUser_UserList>? griduser { get; set; }
        public List<P02_GroupResDTO> p02_Groups { get; set; } = new List<P02_GroupResDTO>();
        public int UserClaims { get; set; } = 0;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var authState = await AuthenticationStateProvider
            .GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is not null && user.Identity.IsAuthenticated)
            {
                claims = user.Claims;
                _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out var userClaims);
                UserClaims = userClaims;
                try
                {
                    string sptype = nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_GetPermissionSinglePage);
                    var body = new { userId = glb.UserInfo.UserID, pageCode = Config.Page_ComponentCode.PageCode.Permission };
                    var parsedData = await _apiServices.APIFrom_sp_Authen_Typed<sp_Authentication_GetPermissionSinglePage>(sptype, body);
                    
                    if (parsedData is not null)
                    {
                        sp_Authentication_GetPermissionSinglePage = parsedData;
                        if (sp_Authentication_GetPermissionSinglePage == null || sp_Authentication_GetPermissionSinglePage.List_Component == null || sp_Authentication_GetPermissionSinglePage.List_Component.Count == 0)
                        {
                            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Warning, Summary = "No permission", Detail = "User chưa được phân quyền.", Duration = 5000 });
                            NavigationManager.NavigateTo("/", true);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error when call SP sp_Library_GetL01Class:" + ex.Message);
                    NotificationService.Notify(new NotificationMessage() { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "Error when call api sp_Library_GetL01Class:" + ex.Message, Duration = 10000 });
                }
                finally
                {
                    StateHasChanged();
                }
                if (sp_Authentication_GetPermissionSinglePage?.List_Component.Count == 0)
                {
                    NavigationManager.NavigateTo("/", true);
                }
                await LoadBaseData();
            }
            else
            {
                UriHelper.NavigateTo("Home", true);
            }
        }
        protected async Task LoadBaseData()
        {
            glb.isBusyPage = true;
            SearchText = string.Empty;

            try
            {
                p02_Groups = await _apiServices.GetFromApiAsync<List<P02_GroupResDTO>>(Config.ApiPermissionGroupsEndpoint)
             ?? new List<P02_GroupResDTO>();
            }
            catch
            {
            }
            try
            {
                _sp_Authentication_TabUser_UserList = await _apiServices.APIFrom_sp_Authen_Typed<
                    List<sp_Authentication_TabUser_UserList>
                >(
                    nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_TabUser_UserList),
                    new { }
                ) ?? new List<sp_Authentication_TabUser_UserList>();
            }
            catch
            {
            }
            glb.isBusyPage = false;
            StateHasChanged();
        }
        protected async Task SwitchOnChange_IsDelete(sp_Authentication_TabUser_UserList data) => await Func_CreateOrUpdateP04UserGroup(data);
        protected async Task DropdownOnChange_Group(sp_Authentication_TabUser_UserList data) => await Func_CreateOrUpdateP04UserGroup(data);
        protected async Task DropdownOnChange_Department(sp_Authentication_TabUser_UserList data) => await Func_CreateOrUpdateP04UserGroup(data);
        protected async Task ButtonOnClick_SearchUser()
        {
            // Nếu search text trống, gọi clear thay vì search
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await ButtonOnClick_Clear();
                return;
            }

            glb.isBusyPage = true;
            try
            {
                _sp_Authentication_TabUser_UserList = await _apiServices.APIFrom_sp_Authen_Typed<
                    List<sp_Authentication_TabUser_UserList>
                >(
                    nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_TabUser_SearchUser),
                    new { SearchText = SearchText },
                    jsonOptions: new JsonSerializerOptions { PropertyNamingPolicy = null }
                ) ?? new List<sp_Authentication_TabUser_UserList>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when search user: {ex.Message}");
                NotificationService.Notify(new NotificationMessage() 
                { 
                    Severity = NotificationSeverity.Error, 
                    Summary = "Error", 
                    Detail = $"Error when searching user: {ex.Message}", 
                    Duration = 5000 
                });
            }
            finally
            {
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        protected async Task ButtonOnClick_Clear()
        {
            SearchText = string.Empty;
            glb.isBusyPage = true;
            try
            {
                _sp_Authentication_TabUser_UserList = await _apiServices.APIFrom_sp_Authen_Typed<
                    List<sp_Authentication_TabUser_UserList>
                >(
                    nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_TabUser_UserList),
                    new { }
                ) ?? new List<sp_Authentication_TabUser_UserList>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when loading user list: {ex.Message}");
                NotificationService.Notify(new NotificationMessage() 
                { 
                    Severity = NotificationSeverity.Error, 
                    Summary = "Error", 
                    Detail = $"Error when loading user list: {ex.Message}", 
                    Duration = 5000 
                });
            }
            finally
            {
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        protected async Task ButtonOnClick_Reload() => await LoadBaseData();

        protected async Task OnRowDoubleClick(DataGridRowMouseEventArgs<sp_Authentication_TabUser_UserList> args)
        {
            if (args.Data != null)
            {
                await DialogService.OpenSideAsync<Component_RecordInspector<sp_Authentication_TabUser_UserList>>(
                    $"User: {args.Data.UserLogin}",
                    new Dictionary<string, object?> { { "Record", args.Data } },
                    options: new SideDialogOptions { Position = DialogPosition.Right, Width = "500px" }
                );
            }
        }

        protected async Task TextBoxOnChange(string arg)
        {
            SearchText = arg;
            if (string.IsNullOrEmpty(SearchText))
            {
                await ButtonOnClick_Clear();
            }
        }

        protected async Task SearchTextOnKeyUp(KeyboardEventArgs arg)
        {
            if (arg.Code == "Enter" || arg.Code == "NumpadEnter")
            {
                if (!string.IsNullOrEmpty(SearchText))
                {
                    await ButtonOnClick_SearchUser();
                }
                else
                {
                    // Nếu search text trống, gọi clear thay vì search
                    await ButtonOnClick_Clear();
                }
            }
        }

        #region Function
        protected async Task Func_CreateOrUpdateP04UserGroup(sp_Authentication_TabUser_UserList data)
        {
            glb.isBusyPage = true;
            P04_UserGroupReqDTO? req = null;
            P04_UserGroupResDTO? res = null;
            try
            {
                if (data.Id != Guid.Empty)
                {
                    req = new P04_UserGroupReqDTO()
                    {
                        UserId = data.UserId,
                        P02_GroupId = data.UserGroup?.Id ?? data.GroupId,
                        Id = data.Id,
                        // P5/timezone: send the original CreateDate from the server load
                        // (no host-time fallback). UpdateDate is set authoritatively
                        // by the BE via IDateTimeProvider, so we leave it default.
                        CreateDate = data.CreateDate ?? default,
                        CreateUserId = data.CreateUserId,
                        UpdateUserId = UserClaims == 0 ? glb.UserInfo.UserID : UserClaims,
                        UpdateDate = default,
                        IsDeleted = data.IsDeleted
                    };
                    res = await _apiServices.PutFromApiAsync<P04_UserGroupResDTO>(
                        $"/api/Permission/user-groups/{data.Id}",
                        req
                    );
                }
                else
                {
                    req = new P04_UserGroupReqDTO
                    {
                        UserId = data.UserId,
                        P02_GroupId = data.UserGroup?.Id ?? data.GroupId,
                        Id = data.Id,
                        // P5/timezone: BE assigns CreateDate/UpdateDate via the
                        // shared IDateTimeProvider, so we don't send host time here.
                        CreateDate = default,
                        CreateUserId = UserClaims == 0 ? glb.UserInfo.UserID : UserClaims,
                        UpdateUserId = UserClaims == 0 ? glb.UserInfo.UserID : UserClaims,
                        UpdateDate = default,
                        IsDeleted = data.IsDeleted
                    };
                    res = await _apiServices.PostFromApiAsync<P04_UserGroupResDTO>(
                        "/api/Permission/user-groups",
                        req
                    );
                }
                if (res != null)
                {
                    if (data.Id == Guid.Empty)
                    {
                        if (_sp_Authentication_TabUser_UserList.Any(x => x.UserId == data.UserId))
                        {
                            _sp_Authentication_TabUser_UserList.FirstOrDefault(x => x.UserId == data.UserId)!.Id = res.Id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage() { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "Error when call EF_Update P04_UserGroup:" + ex.Message, Duration = 10000 });
                throw;
            }
            finally
            {
                glb.isBusyPage = false;
                req?.Dispose();
                res?.Dispose();
                StateHasChanged();
            }
        }
        #endregion
    }
}

