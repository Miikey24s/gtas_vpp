using gtas_vpp_fe.Components.Pages.Lib;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Account;
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
    public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
    [Inject] public IAPIServices _apiServices { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;

    private string SearchText { get; set; } = string.Empty;
    public List<UserAdministrationResDTO> users { get; set; } = [];
    public IList<UserAdministrationResDTO> selectedUsers { get; set; } = [];
    public RadzenDataGrid<UserAdministrationResDTO>? userGrid { get; set; }
    public List<PermissionGroupResDTO> permissionGroups { get; set; } = [];
    public List<DepartmentResDTO> departments { get; set; } = [];

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
        if (firstRender && !hasRequestedInitialUserGridLoad && userGrid is not null)
        {
            hasRequestedInitialUserGridLoad = true;
            await userGrid.Reload();
        }
    }

    protected async Task LoadBaseData()
    {
        await LoadGroupLookupsAsync();
        if (userGrid is not null)
        {
            await userGrid.Reload();
        }
    }

    private async Task LoadGroupLookupsAsync()
    {
        isUserLookupLoading = true;
        try
        {
            var groupTask = _apiServices.GetFromApiAsync<List<PermissionGroupResDTO>>(Config.ApiPermissionGroupsEndpoint);
            var departmentTask = _apiServices.GetFromApiAsync<List<DepartmentResDTO>>(
                "/api/Library/departments?top=1000&showDeleted=false&orderby=Name");
            await Task.WhenAll(groupTask, departmentTask);
            permissionGroups = await groupTask ?? [];
            departments = (await departmentTask ?? [])
                .OrderBy(department => department.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            permissionGroups = [];
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

        if (userGrid is not null)
        {
            await userGrid.FirstPage(true);
        }
    }

    protected async Task ButtonOnClick_Clear()
    {
        SearchText = string.Empty;
        if (userGrid is not null)
        {
            await userGrid.FirstPage(true);
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
            var result = await _apiServices.GetFromApiWithTotalCountAsync<List<UserAdministrationResDTO>>(
                BuildUsersEndpoint(args.Filter, args.Skip, args.Top, args.OrderBy));
            users = result.Data ?? [];
            foreach (var user in users)
            {
                user.UserGroup = permissionGroups.FirstOrDefault(group => group.Id == user.GroupId);
            }
            userCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            users = [];
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
        DataGridLoadColumnFilterDataEventArgs<UserAdministrationResDTO> args)
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

            var result = await _apiServices.GetFromApiWithTotalCountAsync<List<UserAdministrationResDTO>>(
                $"/api/Permission/users?{string.Join("&", queryParams)}");
            args.Data = result.Data ?? [];
            args.Count = result.TotalCount;
        }
        catch (Exception ex)
        {
            NotifyError("Error when loading user filters: " + ex.Message);
        }
    }

    protected async Task OnRowDoubleClick(DataGridRowMouseEventArgs<UserAdministrationResDTO> args)
    {
        if (args.Data is null)
        {
            return;
        }

        await DialogService.OpenSideAsync<Component_RecordInspector<UserAdministrationResDTO>>(
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

    protected Task DropdownOnChange_Group(UserAdministrationResDTO user) =>
        CanPrepareActivation(user)
            ? Task.CompletedTask
            : PersistMembershipAsync(user, "Role changed by permission administrator.");

    protected Task DropdownOnChange_Department(UserAdministrationResDTO user) =>
        CanPrepareActivation(user)
            ? Task.CompletedTask
            : PersistMembershipAsync(user, "Primary department changed by permission administrator.");

    protected async Task ActivateAccountAsync(UserAdministrationResDTO user)
    {
        if (!CanPrepareActivation(user)
            || user.UserGroup is null
            || user.UserGroup.Id == Guid.Empty
            || user.DepartmentId is not Guid departmentId
            || departmentId == Guid.Empty)
        {
            NotifyError("Hãy chọn nhóm quyền và phòng ban trước khi kích hoạt tài khoản.");
            return;
        }

        var confirmed = await DialogService.Confirm(
            $"Kích hoạt tài khoản {user.FullName ?? user.UserLogin}?",
            "Kích hoạt tài khoản",
            new ConfirmOptions { OkButtonText = "Kích hoạt", CancelButtonText = Loc["Cancel"].Value });
        if (confirmed != true)
        {
            return;
        }

        glb.isBusyPage = true;
        isUserLoading = true;
        try
        {
            await _apiServices.PostFromApiAsync<MembershipAdministrationResDTO>(
                Config.ApiAccountAdminActivateEndpoint,
                new AdminAccountActivationReqDTO
                {
                    AccountId = user.UserId,
                    GroupId = user.UserGroup.Id,
                    PrimaryDepartmentId = departmentId,
                    Reason = "Initial account approval by permission administrator."
                });
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Tài khoản đã được kích hoạt",
                Duration = 4000
            });
            await ReloadUsersAsync();
        }
        catch (Exception ex)
        {
            NotifyError("Kích hoạt tài khoản thất bại: " + ex.Message);
            await ReloadUsersAsync();
        }
        finally
        {
            isUserLoading = false;
            glb.isBusyPage = false;
            StateHasChanged();
        }
    }

    protected async Task ResetPasswordAsync(UserAdministrationResDTO user)
    {
        if (!CanResetPassword(user))
        {
            return;
        }

        var confirmed = await DialogService.Confirm(
            $"Tạo mật khẩu tạm thời cho {user.FullName ?? user.UserLogin}? Mật khẩu sẽ chỉ hiển thị một lần cho quản trị viên.",
            "Reset mật khẩu",
            new ConfirmOptions { OkButtonText = "Reset", CancelButtonText = Loc["Cancel"].Value });
        if (confirmed != true)
        {
            return;
        }

        var temporaryPassword = CreateTemporaryPassword();
        glb.isBusyPage = true;
        isUserLoading = true;
        try
        {
            await _apiServices.PostFromApiAsync<AccountLifecycleResDTO>(
                Config.ApiAccountAdminResetPasswordEndpoint,
                new AdminPasswordResetReqDTO
                {
                    AccountId = user.UserId,
                    TemporaryPassword = temporaryPassword,
                    ConfirmPassword = temporaryPassword,
                    Reason = "Password reset by permission administrator."
                });
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Mật khẩu tạm thời",
                Detail = $"Gửi mật khẩu này qua kênh nội bộ an toàn: {temporaryPassword}",
                Duration = 30000
            });
        }
        catch (Exception ex)
        {
            NotifyError("Reset mật khẩu thất bại: " + ex.Message);
        }
        finally
        {
            isUserLoading = false;
            glb.isBusyPage = false;
            StateHasChanged();
        }
    }

    protected async Task DeactivateMembershipAsync(UserAdministrationResDTO user)
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

    protected bool CanEditMembership(UserAdministrationResDTO user) =>
        PermissionState.HasPermission(Permissions.PermissionManage)
        && user.UserId > 0
        && user.UserId != UserClaims
        && string.Equals(user.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase)
        && user.IsActive
        && user.RowVersion is { Length: > 0 };

    protected bool CanPrepareActivation(UserAdministrationResDTO user) =>
        PermissionState.HasPermission(Permissions.PermissionManage)
        && user.UserId > 0
        && user.UserId != UserClaims
        && string.Equals(user.AccountStatus, "PendingApproval", StringComparison.OrdinalIgnoreCase);

    protected bool CanResetPassword(UserAdministrationResDTO user) =>
        PermissionState.HasPermission(Permissions.PermissionManage)
        && user.UserId > 0
        && user.UserId != UserClaims
        && string.Equals(user.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase);

    protected bool CanDeactivateMembership(UserAdministrationResDTO user) =>
        CanEditMembership(user);

    private async Task PersistMembershipAsync(
        UserAdministrationResDTO user,
        string reason)
    {
        if (!CanEditMembership(user)
            || user.UserGroup is null
            || user.DepartmentId is not Guid departmentId
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
        if (userGrid is not null)
        {
            await userGrid.Reload();
        }
    }

    private static string GetRowVersion(UserAdministrationResDTO user) =>
        user.RowVersion is { Length: > 0 }
            ? Convert.ToBase64String(user.RowVersion)
            : string.Empty;

    private static string GetAccountStatusLabel(UserAdministrationResDTO user) =>
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

    private static string CreateTemporaryPassword()
    {
        Span<byte> bytes = stackalloc byte[12];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return $"Tmp-{Convert.ToHexString(bytes)[..12]}aA1!";
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
}
