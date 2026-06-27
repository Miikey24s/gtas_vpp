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
        private int userCount { get; set; } = 0;
        private int currentUserSkip { get; set; } = 0;
        private string? currentUserFilterExpression { get; set; }
        private bool isUserLoading { get; set; } = false;
        private bool isUserLookupLoading { get; set; } = false;
        private bool hasRequestedInitialUserGridLoad = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                UriHelper.NavigateTo("Home", true);
                return;
            }

            claims = user.Claims;
            _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out var userClaims);
            UserClaims = userClaims;
            await LoadGroupLookupsAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !hasRequestedInitialUserGridLoad && griduser is not null)
            {
                hasRequestedInitialUserGridLoad = true;
                await griduser.Reload();
            }
        }

        protected async Task LoadBaseData()
        {
            await LoadGroupLookupsAsync();

            if (griduser is not null)
            {
                await griduser.Reload();
            }
        }

        private async Task LoadGroupLookupsAsync()
        {
            isUserLookupLoading = true;

            try
            {
                p02_Groups = await _apiServices.GetFromApiAsync<List<P02_GroupResDTO>>(Config.ApiPermissionGroupsEndpoint)
             ?? new List<P02_GroupResDTO>();
            }
            catch (Exception ex)
            {
                p02_Groups = [];
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
                isUserLookupLoading = false;
            }

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

            if (griduser is not null)
            {
                await griduser.FirstPage(true);
            }
        }
        protected async Task ButtonOnClick_Clear()
        {
            SearchText = string.Empty;

            if (griduser is not null)
            {
                await griduser.FirstPage(true);
            }
        }
        protected async Task ButtonOnClick_Reload() => await LoadBaseData();

        protected async Task LoadUsersAsync(LoadDataArgs args)
        {
            isUserLoading = true;
            currentUserSkip = args.Skip ?? 0;
            currentUserFilterExpression = args.Filter;
            StateHasChanged();

            try
            {
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<sp_Authentication_TabUser_UserList>>(
                    BuildUsersEndpoint(args.Filter, args.Skip, args.Top, args.OrderBy));

                _sp_Authentication_TabUser_UserList = result.Data ?? new List<sp_Authentication_TabUser_UserList>();
                userCount = result.TotalCount;
            }
            catch (Exception ex)
            {
                _sp_Authentication_TabUser_UserList = [];
                userCount = 0;
                NotificationService.Notify(new NotificationMessage()
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error when loading users: " + ex.Message,
                    Duration = 10000
                });
            }
            finally
            {
                isUserLoading = false;
                StateHasChanged();
            }
        }

        protected async Task LoadUserFilterDataAsync(DataGridLoadColumnFilterDataEventArgs<sp_Authentication_TabUser_UserList> args)
        {
            if (args.Column is null)
            {
                return;
            }

            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    queryParams.Add($"search={Uri.EscapeDataString(SearchText.Trim())}");
                }

                queryParams.Add($"distinct={Uri.EscapeDataString(args.Column.GetFilterProperty())}");

                if (!string.IsNullOrWhiteSpace(currentUserFilterExpression))
                {
                    queryParams.Add($"filter={Uri.EscapeDataString(currentUserFilterExpression)}");
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

                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<sp_Authentication_TabUser_UserList>>(
                    $"/api/Permission/users?{string.Join("&", queryParams)}");

                args.Data = result.Data ?? [];
                args.Count = result.TotalCount;
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage()
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error when loading user filter data: " + ex.Message,
                    Duration = 10000
                });
            }
        }

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

        protected void OnRowRenderUser(RowRenderEventArgs<sp_Authentication_TabUser_UserList> args)
        {
            if (args.Data?.IsDeleted == true)
            {
                AppendRowClass(args.Attributes, "vpp-admin-row-deleted");
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
                    data.Id = res.Id;
                    data.GroupId = res.P02_GroupId;
                    data.UserGroup = p02_Groups.FirstOrDefault(x => x.Id == res.P02_GroupId);
                    data.GroupName = data.UserGroup?.GroupName;
                    data.IsDeleted = res.IsDeleted;
                    data.UpdateUserId = req.UpdateUserId;
                    data.UpdateDate = DateTime.Now;

                    if (griduser is not null)
                    {
                        await griduser.Reload();
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

        private static void AppendRowClass(IDictionary<string, object> attributes, string className)
        {
            if (attributes.TryGetValue("class", out var current) && current is not null)
            {
                attributes["class"] = $"{current} {className}";
                return;
            }

            attributes["class"] = className;
        }

        private string BuildUsersEndpoint(string? filter, int? skip, int? top, string? orderBy)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                queryParams.Add($"search={Uri.EscapeDataString(SearchText.Trim())}");
            }

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

            var queryString = queryParams.Count == 0 ? string.Empty : $"?{string.Join("&", queryParams)}";
            return $"/api/Permission/users{queryString}";
        }
        #endregion
    }
}
