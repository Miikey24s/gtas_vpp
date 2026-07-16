using gtas_vpp_fe.Components.Pages.Lib;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;
using MembershipAdministrationResDTO = gtas_vpp_shared.DTOs.Res.Permission.MembershipAdministrationResDTO;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs;

public partial class Tab_User
{
    [Parameter] public IEnumerable<Claim> claims { get; set; } = [];
    [Parameter]
    public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();
    [Inject] public IAPIServices _apiServices { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;

    private string SearchText { get; set; } = string.Empty;
    public List<sp_Authentication_TabUser_UserList> _sp_Authentication_TabUser_UserList { get; set; } = [];
    public IList<sp_Authentication_TabUser_UserList> selected_UserList { get; set; } = [];
    public RadzenDataGrid<sp_Authentication_TabUser_UserList>? griduser { get; set; }
    public List<P02_GroupResDTO> p02_Groups { get; set; } = [];
    public List<LEX02_CompanyDepartmentLocationResDTO> departments { get; set; } = [];

    private int UserClaims { get; set; }
    private int userCount;
    private int currentUserSkip;
    private string? currentUserFilterExpression;
    private bool isUserLoading;
    private bool isUserLookupLoading;
    private bool hasRequestedInitialUserGridLoad;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            NavigationManager.NavigateTo("Home", true);
            return;
        }

        claims = authState.User.Claims;
        UserClaims = PermissionState.CurrentUserId > 0
            ? PermissionState.CurrentUserId
            : claims.GetInt(ClaimKeys.UserID);
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
            var groupTask = _apiServices.GetFromApiAsync<List<P02_GroupResDTO>>(Config.ApiPermissionGroupsEndpoint);
            var departmentTask = _apiServices.GetFromApiAsync<List<LEX02_CompanyDepartmentLocationResDTO>>(
                "/api/Library/lex02?top=1000&showDeleted=false&orderby=LEX02Name");
            await Task.WhenAll(groupTask, departmentTask);
            p02_Groups = await groupTask ?? [];
            departments = (await departmentTask ?? [])
                .Where(department => string.Equals(department.LEX02Type, "PhongBan", StringComparison.OrdinalIgnoreCase))
                .OrderBy(department => department.LEX02Name)
                .ToList();
        }
        catch (Exception ex)
        {
            p02_Groups = [];
            departments = [];
            NotifyError("Error when loading role/department lookups: " + ex.Message);
        }
        finally
        {
            isUserLookupLoading = false;
            StateHasChanged();
        }
    }

    protected async Task ButtonOnClick_SearchUser()
    {
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

    protected Task ButtonOnClick_Reload() => LoadBaseData();

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
            _sp_Authentication_TabUser_UserList = result.Data ?? [];
            foreach (var user in _sp_Authentication_TabUser_UserList)
            {
                user.UserGroup = p02_Groups.FirstOrDefault(group => group.Id == user.GroupId);
            }
            userCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            _sp_Authentication_TabUser_UserList = [];
            userCount = 0;
            NotifyError("Error when loading users: " + ex.Message);
        }
        finally
        {
            isUserLoading = false;
            StateHasChanged();
        }
    }

    protected async Task LoadUserFilterDataAsync(
        DataGridLoadColumnFilterDataEventArgs<sp_Authentication_TabUser_UserList> args)
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
            NotifyError("Error when loading user filters: " + ex.Message);
        }
    }

    protected async Task OnRowDoubleClick(DataGridRowMouseEventArgs<sp_Authentication_TabUser_UserList> args)
    {
        if (args.Data is null)
        {
            return;
        }

        await DialogService.OpenSideAsync<Component_RecordInspector<sp_Authentication_TabUser_UserList>>(
            $"User: {args.Data.UserLogin}",
            new Dictionary<string, object?> { { "Record", args.Data } },
            options: new SideDialogOptions { Position = DialogPosition.Right, Width = "500px" });
    }

    protected Task TextBoxOnChange(string value)
    {
        SearchText = value;
        return string.IsNullOrWhiteSpace(SearchText) ? ButtonOnClick_Clear() : Task.CompletedTask;
    }

    protected async Task SearchTextOnKeyUp(KeyboardEventArgs args)
    {
        if (args.Code is "Enter" or "NumpadEnter")
        {
            await ButtonOnClick_SearchUser();
        }
    }

    protected Task DropdownOnChange_Group(sp_Authentication_TabUser_UserList user) =>
        PersistMembershipAsync(user, "Role changed by permission administrator.");

    protected Task DropdownOnChange_Department(sp_Authentication_TabUser_UserList user) =>
        PersistMembershipAsync(user, "Primary department changed by permission administrator.");

    protected async Task DeactivateMembershipAsync(sp_Authentication_TabUser_UserList user)
    {
        if (!CanDeactivateMembership(user))
        {
            await ReloadUsersAsync();
            return;
        }

        var confirmed = await DialogService.Confirm(
            $"Deactivate the active membership for {user.FullName ?? user.UserLogin}?",
            "Deactivate membership",
            new ConfirmOptions { OkButtonText = "Deactivate", CancelButtonText = Loc["Cancel"].Value });
        if (confirmed != true)
        {
            await ReloadUsersAsync();
            return;
        }

        glb.isBusyPage = true;
        isUserLoading = true;
        try
        {
            var request = new MembershipDeactivateReqDTO
            {
                AccountId = user.UserId,
                ExpectedRowVersion = GetRowVersion(user),
                Reason = "Membership deactivated by permission administrator."
            };
            await _apiServices.PostFromApiAsync<MembershipAdministrationResDTO>(
                "/api/Permission/memberships/deactivate",
                request);
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Membership deactivated",
                Duration = 3000
            });
            await ReloadUsersAsync();
        }
        catch (Exception ex)
        {
            NotifyError("Membership deactivation failed: " + ex.Message);
            await ReloadUsersAsync();
        }
        finally
        {
            isUserLoading = false;
            glb.isBusyPage = false;
            StateHasChanged();
        }
    }

    protected bool CanEditMembership(sp_Authentication_TabUser_UserList user) =>
        PermissionState.HasPermission(Permissions.PermissionManage)
        && user.UserId > 0
        && user.UserId != UserClaims
        && string.Equals(user.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase)
        && user.IsActive
        && user.RowVersion is { Length: > 0 };

    protected bool CanDeactivateMembership(sp_Authentication_TabUser_UserList user) =>
        CanEditMembership(user);

    private async Task PersistMembershipAsync(
        sp_Authentication_TabUser_UserList user,
        string reason)
    {
        if (!CanEditMembership(user)
            || user.UserGroup is null
            || user.L05_DepartmentId is not Guid departmentId
            || departmentId == Guid.Empty)
        {
            await ReloadUsersAsync();
            return;
        }

        glb.isBusyPage = true;
        isUserLoading = true;
        try
        {
            var request = new MembershipUpsertReqDTO
            {
                AccountId = user.UserId,
                GroupId = user.UserGroup.Id,
                PrimaryDepartmentId = departmentId,
                ExpectedRowVersion = user.IsActive ? GetRowVersion(user) : null,
                Reason = reason
            };
            await _apiServices.PutFromApiAsync<MembershipAdministrationResDTO>(
                "/api/Permission/memberships",
                request);
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Membership updated",
                Duration = 3000
            });
            await ReloadUsersAsync();
        }
        catch (Exception ex)
        {
            NotifyError("Membership update failed: " + ex.Message);
            await ReloadUsersAsync();
        }
        finally
        {
            isUserLoading = false;
            glb.isBusyPage = false;
            StateHasChanged();
        }
    }

    private async Task ReloadUsersAsync()
    {
        if (griduser is not null)
        {
            await griduser.Reload();
        }
    }

    private static string GetRowVersion(sp_Authentication_TabUser_UserList user) =>
        user.RowVersion is { Length: > 0 }
            ? Convert.ToBase64String(user.RowVersion)
            : string.Empty;

    private static string GetAccountStatusLabel(sp_Authentication_TabUser_UserList user) =>
        user.AccountStatus switch
        {
            "Active" when user.IsActive => "Active",
            "Active" => "No membership",
            "PendingApproval" => "Pending approval",
            "Disabled" => "Disabled",
            _ => user.AccountStatus ?? "Unknown"
        };

    private void NotifyError(string detail) => Toast.Notify(new NotificationMessage
    {
        Severity = NotificationSeverity.Error,
        Summary = "Error",
        Detail = detail,
        Duration = 10000
    });

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
}
