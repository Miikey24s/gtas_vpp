using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Share;
using gtas_vpp_fe.Components.Pages.Lib;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs
{
    public partial class Tab_PagePermission
    {
        [Inject] public ICustomNotificationService _notificationService { get; set; } = default!;
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public PermissionState PermissionState { get; set; } = default!;
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();

        public List<P02_GroupResDTO> list_Group { get; set; } = new List<P02_GroupResDTO>();
        public List<P02_GroupResDTO> allGroupsForLookup { get; set; } = new List<P02_GroupResDTO>();
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
        private int groupCount { get; set; } = 0;
        private int currentGroupSkip { get; set; } = 0;
        private string? currentGroupFilterExpression { get; set; }
        private bool isGroupLookupLoading { get; set; } = false;
        private bool hasRequestedInitialGroupGridLoad = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                NavigationManager.NavigateTo("Home", true);
                return;
            }

            claims = user.Claims;
            await LoadGroupLookupsAsync();
            _P02_GroupResDTOReqDTO = new P02_GroupResDTO
            {
                GroupName = "NewGroupName",
                Description = string.Empty
            };
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !hasRequestedInitialGroupGridLoad && grid is not null)
            {
                hasRequestedInitialGroupGridLoad = true;
                await grid.Reload();
            }
        }

        protected async Task LoadBaseData()
        {
            await LoadGroupLookupsAsync();

            if (grid is not null)
            {
                await grid.Reload();
            }
        }

        private async Task LoadGroupLookupsAsync()
        {
            isGroupLookupLoading = true;

            try
            {
                allGroupsForLookup = await _apiServices.GetFromApiAsync<List<P02_GroupResDTO>>(Config.ApiPermissionGroupsEndpoint)
                    ?? new List<P02_GroupResDTO>();
            }
            catch (Exception ex)
            {
                allGroupsForLookup = [];
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error when loading groups: " + ex.Message,
                    Duration = 10000
                });
            }
            finally
            {
                isGroupLookupLoading = false;
            }

            StateHasChanged();
        }

        protected async Task LoadGroupsAsync(LoadDataArgs args)
        {
            IsLoading = true;
            currentGroupSkip = args.Skip ?? 0;
            currentGroupFilterExpression = args.Filter;
            StateHasChanged();

            try
            {
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<P02_GroupResDTO>>(
                    BuildGroupsEndpoint(args.Filter, args.Skip, args.Top, args.OrderBy));

                list_Group = result.Data ?? new List<P02_GroupResDTO>();
                groupCount = result.TotalCount;

                if (allGroupsForLookup.Count == 0)
                {
                    allGroupsForLookup = await _apiServices.GetFromApiAsync<List<P02_GroupResDTO>>(Config.ApiPermissionGroupsEndpoint)
                        ?? new List<P02_GroupResDTO>();
                }
            }
            catch (Exception ex)
            {
                list_Group = [];
                groupCount = 0;
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error when load groups: " + ex.Message,
                    Duration = 10000
                });
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected async Task LoadGroupFilterDataAsync(DataGridLoadColumnFilterDataEventArgs<P02_GroupResDTO> args)
        {
            if (args.Column is null)
            {
                return;
            }

            try
            {
                var queryParams = new List<string>
                {
                    "getFullName=true",
                    $"distinct={Uri.EscapeDataString(args.Column.GetFilterProperty())}"
                };

                if (!string.IsNullOrWhiteSpace(currentGroupFilterExpression))
                {
                    queryParams.Add($"filter={Uri.EscapeDataString(currentGroupFilterExpression)}");
                }

                if (!string.IsNullOrWhiteSpace(args.Filter))
                {
                    queryParams.Add($"distinctFilter={Uri.EscapeDataString(args.Filter)}");
                }

                if (args.Skip.HasValue)
                {
                    queryParams.Add($"skip={args.Skip.Value}");
                }

                if (args.Top.HasValue)
                {
                    queryParams.Add($"top={args.Top.Value}");
                }

                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<P02_GroupResDTO>>(
                    $"/api/Permission/groups?{string.Join("&", queryParams)}");

                args.Data = result.Data ?? [];
                args.Count = result.TotalCount;
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error when load group filter data: " + ex.Message,
                    Duration = 10000
                });
            }
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
                list_PermissionOfGroup = await _apiServices.GetFromApiAsync<
                    List<sp_Authen_Permission_GetPageWithComponentByGroupId>
                >($"/api/Permission/groups/{group.Id}/page-components?showDeleted=true")
                ?? new List<sp_Authen_Permission_GetPageWithComponentByGroupId>();
            }
            catch (Exception ex)
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when load group page permissions:" + ex.Message, 10000, true);
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

                    var lookupIdx = allGroupsForLookup.FindIndex(x => x.Id == res.Id);
                    if (lookupIdx >= 0) allGroupsForLookup[lookupIdx] = res;

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

                    var lookupIdx = allGroupsForLookup.FindIndex(x => x.Id == res.Id);
                    if (lookupIdx >= 0) allGroupsForLookup[lookupIdx] = res;

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

        protected async Task HardDeleteGroupAsync(P02_GroupResDTO group)
        {
            var confirm = await DialogService.Confirm(
                Loc["PermanentDeleteWarning"].Value,
                Loc["HardDelete"].Value,
                new ConfirmOptions { OkButtonText = Loc["Delete"].Value, CancelButtonText = Loc["Cancel"].Value });

            if (confirm != true)
            {
                return;
            }

            glb.isBusyPage = true;
            IsLoading = true;

            try
            {
                var deleted = await _apiServices.DeleteFromApiAsync($"/api/Permission/groups/{group.Id}");
                if (deleted)
                {
                    list_Group.RemoveAll(x => x.Id == group.Id);
                    allGroupsForLookup.RemoveAll(x => x.Id == group.Id);
                    await grid.Reload();

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = Loc["Success"].Value,
                        Detail = Loc["GroupPermanentlyDeleted"].Value,
                        Duration = 3000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["DeleteFailed"].Value,
                    Detail = ex.Message,
                    Duration = 8000
                });
            }
            finally
            {
                glb.isBusyPage = false;
                IsLoading = false;
                StateHasChanged();
            }
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
                    if (data.GroupId == PermissionState.CurrentGroupId)
                    {
                        await PermissionState.RefreshAsync();

                        if (!PermissionState.HasPageAccess(Config.Page_ComponentCode.PageCode.Permission))
                        {
                            NavigationManager.NavigateTo("/dashboard?tab=0", true);
                            return;
                        }
                    }

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

        protected async Task OnGroupRowDoubleClick(DataGridRowMouseEventArgs<P02_GroupResDTO> args)
        {
            if (args.Data != null)
            {
                await DialogService.OpenSideAsync<Component_RecordInspector<P02_GroupResDTO>>(
                    $"Group: {args.Data.GroupName}",
                    new Dictionary<string, object?> { { "Record", args.Data } },
                    options: new SideDialogOptions { Position = DialogPosition.Right, Width = "500px" }
                );
            }
        }

        protected async Task OnComponentRowDoubleClick(DataGridRowMouseEventArgs<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component> args)
        {
            if (args.Data != null)
            {
                await DialogService.OpenSideAsync<Component_RecordInspector<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component>>(
                    $"Component: {args.Data.ComponentName}",
                    new Dictionary<string, object?> { { "Record", args.Data } },
                    options: new SideDialogOptions { Position = DialogPosition.Right, Width = "500px" }
                );
            }
        }

        protected void OnRowRenderGroup(RowRenderEventArgs<P02_GroupResDTO> args)
        {
            if (args.Data != null && args.Data.IsDeleted)
            {
                AppendRowClass(args.Attributes, "vpp-admin-row-deleted");
            }
        }

        protected void OnRowRenderComponent(RowRenderEventArgs<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component> args)
        {
            if (args.Data != null && args.Data.IsDeleted)
            {
                AppendRowClass(args.Attributes, "vpp-admin-row-deleted");
            }
        }

        private static void AppendRowClass(IDictionary<string, object> attributes, string className)
        {
            if (attributes.TryGetValue("class", out var current) && current is not null)
            {
                attributes["class"] = $"{current} {className}";
                return;
            }

            attributes["class"] = className;
        }

        private static string BuildGroupsEndpoint(string? filter, int? skip, int? top, string? orderBy)
        {
            var queryParams = new List<string> { "getFullName=true" };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                queryParams.Add($"filter={Uri.EscapeDataString(filter)}");
            }

            if (skip.HasValue)
            {
                queryParams.Add($"skip={skip.Value}");
            }

            if (top.HasValue)
            {
                queryParams.Add($"top={top.Value}");
            }

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                queryParams.Add($"orderby={Uri.EscapeDataString(orderBy)}");
            }

            return $"/api/Permission/groups?{string.Join("&", queryParams)}";
        }
    }
}
