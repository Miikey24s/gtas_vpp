//using gtas_costing.Model.Models.Auth;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Share;
//using gtas_vpp_fe.Services.Services;
//using gtas_vpp_fe.WebServersideService;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
//using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;
using System.Text.Json;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs
{
    public partial class Tab_PagePermission
    {
        //[Inject] public IBussinessService _bussinessService { get; set; }
        [Inject] public ICustomNotificationService _notificationService { get; set; } = default!;
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();

        public List<P02_GroupResDTO> list_Group { get; set; } = new List<P02_GroupResDTO>();
        public RadzenDataGrid<P02_GroupResDTO> grid { get; set; } = default!;
        public IList<P02_GroupResDTO> selected_Group { get; set; } = new List<P02_GroupResDTO>();
        public P02_GroupResDTO? selected_Group_To_Copy { get; set; } = new P02_GroupResDTO();
        public List<sp_Authen_Permission_GetPageWithComponentByGroupId> list_PermissionOfGroup { get; set; } = new List<sp_Authen_Permission_GetPageWithComponentByGroupId> { };
        public IList<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component> selected_Component { get; set; } = new List<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component>();
        public RadzenDataGrid<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component> child_grid { get; set; } = default!;

        public bool isEditing { get; set; } = false;
        public List<DropdownModel> list_Company { get; set; } = new List<DropdownModel>();
        public DropdownModel selected_Company { get; set; } = default!;
        public P02_GroupResDTO _P02_GroupResDTOReqDTO { get; set; } = new P02_GroupResDTO();
        List<P02_GroupResDTO> ordersToUpdate = new List<P02_GroupResDTO>();
        public int selectedTab { get; set; } = 0;

        public bool IsLoading { get; set; } = false;
        public bool IsLoading_Child { get; set; } = false;

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
                try
                {
                    //sp_Authentication_GetPermissionSinglePage = await _bussinessService.SPServiceRead<sp_Authentication_GetPermissionSinglePage>(Config.SPENUM_ResType.Single,
                    //                                                            nameof(Config.sp_AuthenClass.sp_Authen.sp_Authen),
                    //                                                            nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_GetPermissionSinglePage),
                    //                                                            new { userId = glb.UserInfo.UserID, pageCode = "0001" })
                    //                                                .ContinueWith(x => x.Result.FirstOrDefault() ?? new sp_Authentication_GetPermissionSinglePage());
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
                    //_bussinessService.WriteLog(ex, "sp_Authentication_GetPermissionSinglePage", new Dictionary<string, object>() { { "UserId", claims.FirstOrDefault(x => x.Type == "UserID")?.Value }, { "PageCode", "0003" } });
                    NotificationService.Notify(new NotificationMessage() { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "Error when call api sp_Library_GetL01Class:" + ex.Message, Duration = 10000 });
                }
                finally
                {
                    StateHasChanged();
                }
                if (sp_Authentication_GetPermissionSinglePage?.List_Component.Count == 0)
                {
                    NavigationManager.NavigateTo("Home", true);
                }
                await LoadBaseData();
                _P02_GroupResDTOReqDTO = new P02_GroupResDTO()
                {
                    GroupName = "NewGroupName",
                    Description = ""
                };
            }
            else
            {
                NavigationManager.NavigateTo("Home", true);
            }
        }
        protected async Task LoadBaseData()
        {
            glb.isBusyPage = true;
            try
            {
                //list_Group = await _bussinessService.BaseService<P02_GroupResDTO>(Config.EF_BASEMETHOD.EF_GetTAsync, true) ?? new List<P02_GroupResDTO>();
                list_Group = await _apiServices.GetFromApiAsync<List<P02_GroupResDTO>>(Config.ApiPermissionGroupsEndpoint)
             ?? new List<P02_GroupResDTO>();
            }
            catch
            {
                throw;
            }
            glb.isBusyPage = false;
        }
        public void Reset(P02_GroupResDTO group)
        {
            ordersToUpdate.Remove(group);
        }
        protected async Task GroupRowExpand(P02_GroupResDTO group)
        {
            glb.isBusyPage = true;
            IsLoading_Child = true;
            StateHasChanged();

            try
            {
                //list_PermissionOfGroup = await _bussinessService.SPServiceRead<sp_Authen_Permission_GetPageWithComponentByGroupId>
                //                                                    (Config.SPENUM_ResType.Multiple,
                //                                                    nameof(Config.sp_AuthenClass.sp_Authen),
                //                                                    nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_Permission_GetPageWithComponentByGroupId),
                //                                                    new { GroupId = group.Id }) ?? new List<sp_Authen_Permission_GetPageWithComponentByGroupId>();
                list_PermissionOfGroup = await _apiServices.APIFrom_sp_Authen_Typed<
                    List<sp_Authen_Permission_GetPageWithComponentByGroupId>
                >(
                    nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_Permission_GetPageWithComponentByGroupId),
                    new { GroupId = group.Id },
                    jsonOptions: new JsonSerializerOptions { PropertyNamingPolicy = null }
                ) ?? new List<sp_Authen_Permission_GetPageWithComponentByGroupId>();
            }
            catch (Exception ex)
            {
                //_bussinessService.WriteLog(ex, "sp_Authentication_Permission_GetPageWithComponentByGroupId", new Dictionary<string, object>() { { "GroupId", group.Id } });
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when call sp_Authentication_Permission_GetPageWithComponentByGroupId:" + ex.Message, 10000, true);
            }
            IsLoading_Child = false;
            glb.isBusyPage = false;
            StateHasChanged();
        }
        protected async Task OnUpdateRow(P02_GroupResDTO group)
        {
            glb.isBusyPage = true;
            IsLoading = true;
            Reset(group);
            P02_GroupResDTO? res = null;
            try
            {
                //res = await _bussinessService.BaseService<P02_GroupResDTO>(Config.EF_BASEMETHOD.EF_Update, null, null, null, new List<P02_GroupResDTO> { group })
                //                                .ContinueWith(x => x.Result?.FirstOrDefault());
                _ = int.TryParse(claims?.FirstOrDefault(x => x.Type.Equals("UserID"))?.Value, out int userId);

                var req = new P02_GroupUpdateReqDTO
                {
                    GroupName = group.GroupName,
                    Description = group.Description,
                    ParentGroupId = group.ParentGroupId,
                    IsDeleted = group.IsDeleted,
                    UpdateUserId = userId == 0 ? glb.UserInfo.UserID : userId,
                    UpdateDate = DateTime.Now
                };

                res = await _apiServices.PutFromApiAsync<P02_GroupResDTO>(
                    $"/api/Permission/groups/{group.Id}",
                    req);

                if (res is not null)
                {
                    var idx = list_Group.FindIndex(x => x.Id == res.Id);
                    if (idx >= 0) list_Group[idx] = res;

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Group updated",
                        Detail = "Group information updated successfully",
                        Duration = 3000
                    });
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Handle HTTP errors (including 400 Bad Request for circular reference)
                var errorMessage = httpEx.Message;
                if (errorMessage.Contains("circular reference"))
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Invalid Parent Group",
                        Detail = "Cannot set parent group: This would create a circular reference in the group hierarchy.",
                        Duration = 8000
                    });
                }
                else if (errorMessage.Contains("too deep"))
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hierarchy Too Deep",
                        Detail = "Cannot set parent group: The hierarchy depth would exceed the maximum allowed (10 levels).",
                        Duration = 8000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Update failed",
                        Detail = "Error when update group: " + errorMessage,
                        Duration = 8000
                    });
                }
                
                // Reload to revert changes
                await LoadBaseData();
            }
            catch (Exception ex)
            {
                //_bussinessService.WriteLog(ex, "EF Update P02_GroupResDTO", new Dictionary<string, object> { { "Param", JsonConvert.SerializeObject(group) } });
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error when update group: " + ex.Message,
                    Duration = 10000
                });
                
                // Reload to revert changes
                await LoadBaseData();
            }
            glb.isBusyPage = false;
            IsLoading = false;
            StateHasChanged();
        }
        protected async Task SwitchOnChange(P02_GroupResDTO data)
        {
            glb.isBusyPage = true;
            try
            {
                _ = int.TryParse(claims?.FirstOrDefault(x => x.Type.Equals("UserID"))?.Value, out int userId);

                var req = new P02_GroupUpdateReqDTO
                {
                    GroupName = data.GroupName,
                    Description = data.Description,
                    ParentGroupId = data.ParentGroupId,
                    IsDeleted = data.IsDeleted,
                    UpdateUserId = userId == 0 ? glb.UserInfo.UserID : userId,
                    UpdateDate = DateTime.Now
                };

                var res = await _apiServices.PutFromApiAsync<P02_GroupResDTO>(
                    $"/api/Permission/groups/{data.Id}",
                    req);

                if (res is not null)
                {
                    var idx = list_Group.FindIndex(x => x.Id == res.Id);
                    if (idx >= 0) list_Group[idx] = res;

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = data.IsDeleted ? "Group deleted" : "Group restored",
                        Duration = 3000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error when update group: " + ex.Message,
                    Duration = 10000
                });
            }
            finally
            {
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        protected async Task EditRow(P02_GroupResDTO data)
        {
            isEditing = true;
            if (!grid.IsValid) return;

            ordersToUpdate.Add(data);
            await grid.EditRow(data);
            StateHasChanged();
        }
        protected async Task SaveRow(P02_GroupResDTO order)
        {
            isEditing = false;
            await grid.UpdateRow(order);
            StateHasChanged();
        }
        protected void CancelEdit(P02_GroupResDTO order)
        {
            isEditing = false;
            Reset(order);

            grid.CancelEditRow(order);
            StateHasChanged();
        }
        protected async Task SwitchOnChange(sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component data)
        {
            glb.isBusyPage = true;
            IsLoading_Child = true;
            _ = int.TryParse(claims?.FirstOrDefault(x => x.Type.Equals("UserID"))?.Value, out int userid);
            
            var body = new PatchComponentMappingReqDTO
            {
                P05_PageComponentMappingId = data.GroupPageComponentMappingId,
                P02_GroupId = data.GroupId,
                IsEnable = data.IsEnable,
                IsVisible = data.IsVisible,
                UpdateUserId = userid == 0 ? glb.UserInfo.UserID : userid,
                UpdateDate = DateTime.Now
            };
            
            try
            {
                var res = await _apiServices.PatchFromApiAsync<object>("/api/Permission/component-mapping", body);
                if (res is not null)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Permission updated",
                        Detail = $"Component '{data.ComponentName}' updated successfully",
                        Duration = 3000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Update failed",
                        Detail = "No response from server",
                        Duration = 5000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error when update component permission: " + ex.Message,
                    Duration = 10000
                });
            }
            finally
            {
                IsLoading_Child = false;
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        protected async Task Submit(P02_GroupResDTO reqDto)
        {
            IsLoading = true;

            try
            {
                bool isCopy = selected_Group_To_Copy is not null;
                reqDto.Id = isCopy ? selected_Group_To_Copy?.Id ?? Guid.Empty : Guid.Empty;

                string spType = isCopy
                    ? nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_CopyFromGroup)
                    : nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_CreateNewGroup);

                var rs = await _apiServices.APIFrom_sp_Authen_Typed<string>(
                    spType,
                    new
                    {
                        GroupId = reqDto.Id,
                        GroupName = reqDto.GroupName,
                        Description = reqDto.Description,
                        CreateUserId = reqDto.CreateUserId
                    });

                if (!string.IsNullOrWhiteSpace(rs))
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = isCopy ? "Copy success" : "Create success",
                        Duration = 8000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = isCopy ? "Copy failed" : "Create failed",
                        Duration = 8000
                    });
                }

                await LoadBaseData();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error when submit group: " + ex.Message,
                    Duration = 10000
                });
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
    }
}

