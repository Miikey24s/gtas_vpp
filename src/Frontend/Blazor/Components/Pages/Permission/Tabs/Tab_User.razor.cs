using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Components.Pages.Permission.Dialogs;
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
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;
using MembershipAdministrationResDTO = gtas_vpp_shared.DTOs.Res.Permission.MembershipAdministrationResDTO;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs;

public partial class Tab_User : IDisposable
{
    [Parameter] public IEnumerable<Claim> claims { get; set; } = [];
    [Parameter]
    public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
    [Inject] public IAPIServices _apiServices { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;

    private string SearchText { get; set; } = string.Empty;
    private string? SelectedAccountStatus { get; set; }
    private Guid? SelectedGroupId { get; set; }
    private Guid? SelectedDepartmentId { get; set; }
    public List<UserAdministrationResDTO> users { get; set; } = [];
    public RadzenDataGrid<UserAdministrationResDTO>? userGrid { get; set; }
    public List<PermissionGroupResDTO> permissionGroups { get; set; } = [];
    public List<DepartmentResDTO> departments { get; set; } = [];
    private readonly Dictionary<int, Guid?> pendingGroupSelections = [];
    private readonly Dictionary<int, Guid?> pendingDepartmentSelections = [];

    private int UserClaims { get; set; }
    private int userCount;
    private int currentUserSkip;
    private bool isUserLoading;
    private bool isUserLookupLoading;
    private bool hasRequestedInitialUserGridLoad;
    private AccountAdministrationCapabilitiesResDTO accountCapabilities = new();
    private CancellationTokenSource? searchDebounceCts;
    private bool HasUserFilters => !string.IsNullOrWhiteSpace(SearchText)
                                   || !string.IsNullOrWhiteSpace(SelectedAccountStatus)
                                   || SelectedGroupId.HasValue
                                   || SelectedDepartmentId.HasValue;
    private bool CanManageUsers => PermissionState.HasPermission(Permissions.PermissionManage);
    private bool CanInviteUsers => CanManageUsers && accountCapabilities.InvitationEnabled;
    private string InvitationCapabilityMessage => accountCapabilities.InvitationEnabled
        ? Loc["AddUser"].Value
        : Loc["EmailInvitationUnavailable"].Value;
    private IReadOnlyList<VppFilterOption<string>> AccountStatusOptions =>
    [
        new(string.Empty, Loc["AllAccountStatuses"].Value),
        new("Active", Loc["AccountStatusActive"].Value),
        new("PendingApproval", Loc["AccountStatusPendingApproval"].Value),
        new("Disabled", Loc["AccountStatusDisabled"].Value)
    ];
    private IReadOnlyList<VppFilterOption<Guid?>> GroupFilterOptions
        => permissionGroups.Select(group => new VppFilterOption<Guid?>(group.Id, group.GroupName ?? "–")).ToList();
    private IReadOnlyList<VppFilterOption<Guid?>> DepartmentFilterOptions
        => departments.Select(department => new VppFilterOption<Guid?>(department.Id, department.Name ?? "–")).ToList();

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
            // Radzen grid được prerender trước khi circuit tương tác sẵn sàng;
            // chủ động reload đúng một lần để LoadData chạy trên route thật.
            hasRequestedInitialUserGridLoad = true;
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
            var capabilityTask = CanManageUsers
                ? _apiServices.GetFromApiAsync<AccountAdministrationCapabilitiesResDTO>(Config.ApiAccountAdminCapabilitiesEndpoint)
                : Task.FromResult<AccountAdministrationCapabilitiesResDTO?>(null);
            await Task.WhenAll(groupTask, departmentTask, capabilityTask);
            permissionGroups = await groupTask ?? [];
            departments = (await departmentTask ?? [])
                .OrderBy(department => department.Name)
                .ToList();
            accountCapabilities = await capabilityTask ?? new AccountAdministrationCapabilitiesResDTO();
        }
        catch (Exception ex)
        {
            permissionGroups = [];
            departments = [];
            accountCapabilities = new AccountAdministrationCapabilitiesResDTO();
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            isUserLookupLoading = false;
            StateHasChanged();
        }
    }

    protected async Task ButtonOnClick_Clear()
    {
        SearchText = string.Empty;
        SelectedAccountStatus = null;
        SelectedGroupId = null;
        SelectedDepartmentId = null;
        if (userGrid is not null)
        {
            await userGrid.FirstPage(true);
        }
    }

    protected async Task OnAccountStatusChangedAsync(string value)
    {
        SelectedAccountStatus = string.IsNullOrWhiteSpace(value) ? null : value;
        if (userGrid is not null)
        {
            await userGrid.FirstPage(true);
        }
    }

    private async Task OnGroupFilterChangedAsync(Guid? value)
    {
        SelectedGroupId = value;
        if (userGrid is not null) await userGrid.FirstPage(true);
    }

    private async Task OnDepartmentFilterChangedAsync(Guid? value)
    {
        SelectedDepartmentId = value;
        if (userGrid is not null) await userGrid.FirstPage(true);
    }

    protected async Task LoadUsersAsync(LoadDataArgs args)
    {
        isUserLoading = true;
        currentUserSkip = args.Skip ?? 0;
        StateHasChanged();

        try
        {
            var result = await _apiServices.GetFromApiWithTotalCountAsync<List<UserAdministrationResDTO>>(
                BuildUsersEndpoint(args.Skip, args.Top, args.OrderBy));
            users = result.Data ?? [];
            userCount = result.TotalCount;
            pendingGroupSelections.Clear();
            pendingDepartmentSelections.Clear();
        }
        catch (Exception ex)
        {
            users = [];
            userCount = 0;
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            isUserLoading = false;
            StateHasChanged();
        }
    }

    protected async Task SearchTextOnInput(ChangeEventArgs args)
    {
        SearchText = args.Value?.ToString() ?? string.Empty;
        searchDebounceCts?.Cancel();
        searchDebounceCts?.Dispose();
        searchDebounceCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, searchDebounceCts.Token);
            if (userGrid is not null)
            {
                await userGrid.FirstPage(true);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task OpenInvitationAsync()
    {
        if (!CanInviteUsers) return;
        var result = await DialogService.OpenAsync<Dialog_UserInvitationEditor>(
            Loc["AddUser"],
            new Dictionary<string, object?>
            {
                [nameof(Dialog_UserInvitationEditor.Model)] = new AdminAccountInvitationReqDTO(),
                [nameof(Dialog_UserInvitationEditor.Groups)] = permissionGroups,
                [nameof(Dialog_UserInvitationEditor.Departments)] = departments
            },
            VppAdminDialogProfiles.Create(VppAdminDialogSize.Standard, Loc["AddUser"], closeAriaLabel: Loc["Close"].Value));
        if (result is not AdminAccountInvitationReqDTO request) return;

        isUserLoading = true;
        try
        {
            await _apiServices.PostFromApiAsync<AccountLifecycleResDTO>(Config.ApiAccountAdminInviteEndpoint, request);
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = Loc["InvitationSentSummary"],
                Detail = Loc["InvitationSentDetail"],
                Duration = 6000
            });
            await ReloadUsersAsync();
        }
        catch (Exception ex)
        {
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            isUserLoading = false;
            StateHasChanged();
        }
    }

    private Guid? GetSelectedGroupId(UserAdministrationResDTO user) =>
        pendingGroupSelections.TryGetValue(user.UserId, out var value)
            ? value
            : user.GroupId == Guid.Empty ? null : user.GroupId;

    private Guid? GetSelectedDepartmentId(UserAdministrationResDTO user) =>
        pendingDepartmentSelections.TryGetValue(user.UserId, out var value)
            ? value
            : user.DepartmentId;

    private static Guid? ConvertToNullableGuid(object? value) => value switch
    {
        Guid id => id,
        string text when Guid.TryParse(text, out var id) => id,
        _ => null
    };

    private async Task OnGroupAssignmentChangedAsync(UserAdministrationResDTO user, Guid? groupId)
    {
        pendingGroupSelections[user.UserId] = groupId;
        if (CanEditMembership(user)) await ApplyInlineMembershipAsync(user);
    }

    private async Task OnDepartmentAssignmentChangedAsync(UserAdministrationResDTO user, Guid? departmentId)
    {
        pendingDepartmentSelections[user.UserId] = departmentId;
        if (CanEditMembership(user)) await ApplyInlineMembershipAsync(user);
    }

    private async Task ApplyInlineMembershipAsync(UserAdministrationResDTO user)
    {
        var groupId = GetSelectedGroupId(user);
        var departmentId = GetSelectedDepartmentId(user);
        if (!groupId.HasValue || !departmentId.HasValue)
        {
            return;
        }

        if (CanEditMembership(user)
            && (user.GroupId != groupId.Value || user.DepartmentId != departmentId.Value))
        {
            await UpsertMembershipAsync(user, groupId.Value, departmentId.Value);
        }
    }

    private async Task ApproveAccountAsync(UserAdministrationResDTO user)
    {
        if (!CanApproveAccount(user))
        {
            NotifyError(Loc["ActivateAccountRequiresAssignment"].Value);
            return;
        }

        await ActivateAccountAsync(user, GetSelectedGroupId(user)!.Value, GetSelectedDepartmentId(user)!.Value);
    }

    private async Task ToggleUserAccessAsync(UserAdministrationResDTO user, bool isActive)
    {
        if (isActive == user.IsActive) return;

        if (!isActive)
        {
            await DeactivateMembershipAsync(user);
            return;
        }

        await ReactivateMembershipAsync(user);
    }

    private async Task ReactivateMembershipAsync(UserAdministrationResDTO user)
    {
        var groupId = GetSelectedGroupId(user);
        var departmentId = GetSelectedDepartmentId(user);
        if (!CanReactivateMembership(user) || !groupId.HasValue || !departmentId.HasValue)
        {
            NotifyError(Loc["EnableUserAccessRequiresAssignment"].Value);
            await ReloadUsersAsync();
            return;
        }

        var confirmed = await DialogService.Confirm(
            Loc["EnableUserAccessConfirm", user.FullName ?? user.UserLogin ?? string.Empty].Value,
            Loc["EnableUserAccess"].Value,
            new ConfirmOptions { OkButtonText = Loc["Enable"].Value, CancelButtonText = Loc["Cancel"].Value });
        if (confirmed != true)
        {
            await ReloadUsersAsync();
            return;
        }

        await UpsertMembershipAsync(user, groupId.Value, departmentId.Value);
    }

    private async Task ActivateAccountAsync(UserAdministrationResDTO user, Guid groupId, Guid departmentId)
    {
        if (!CanPrepareActivation(user)
            || groupId == Guid.Empty
            || departmentId == Guid.Empty)
        {
            NotifyError(Loc["ActivateAccountRequiresAssignment"].Value);
            return;
        }

        var confirmed = await DialogService.Confirm(
            Loc["ActivateAccountConfirm", user.FullName ?? user.UserLogin ?? string.Empty].Value,
            Loc["ActivateAccount"].Value,
            new ConfirmOptions { OkButtonText = Loc["Activate"].Value, CancelButtonText = Loc["Cancel"].Value });
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
                    GroupId = groupId,
                    PrimaryDepartmentId = departmentId,
                    Reason = Loc["MembershipInitialActivationReason"]
                });
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = Loc["AccountActivated"].Value,
                Duration = 4000
            });
            await ReloadUsersAsync();
        }
        catch (Exception ex)
        {
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
            await ReloadUsersAsync();
        }
        finally
        {
            isUserLoading = false;
            glb.isBusyPage = false;
            StateHasChanged();
        }
    }

    private async Task SendPasswordResetLinkAsync(UserAdministrationResDTO user)
    {
        if (!CanSendPasswordLink(user))
        {
            return;
        }

        var confirmed = await DialogService.Confirm(
            Loc["ConfirmSendPasswordLink", user.Email ?? string.Empty],
            Loc["SendOneTimeLink"],
            new ConfirmOptions { OkButtonText = Loc["Send"].Value, CancelButtonText = Loc["Cancel"].Value });
        if (confirmed != true)
        {
            return;
        }

        glb.isBusyPage = true;
        isUserLoading = true;
        try
        {
            await _apiServices.PostFromApiAsync<AccountLifecycleResDTO>(
                Config.ApiAccountAdminSendPasswordResetLinkEndpoint,
                new AdminPasswordResetLinkReqDTO
                {
                    AccountId = user.UserId,
                    Reason = Loc["PasswordLinkAuditReason"]
                });
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = Loc["LinkSentSummary"],
                Detail = Loc["LinkSentDetail"],
                Duration = 6000
            });
        }
        catch (Exception ex)
        {
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
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
            Loc["DeactivateMembershipConfirm", user.FullName ?? user.UserLogin ?? string.Empty].Value,
            Loc["DeactivateMembership"].Value,
            new ConfirmOptions { OkButtonText = Loc["Deactivate"].Value, CancelButtonText = Loc["Cancel"].Value });
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
                Summary = Loc["MembershipDeactivated"].Value,
                Duration = 3000
            });
            await ReloadUsersAsync();
        }
        catch (Exception ex)
        {
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
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

    private bool CanReactivateMembership(UserAdministrationResDTO user) =>
        PermissionState.HasPermission(Permissions.PermissionManage)
        && user.UserId > 0
        && user.UserId != UserClaims
        && string.Equals(user.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase)
        && !user.IsActive;

    protected bool CanAssignMembership(UserAdministrationResDTO user) =>
        CanEditMembership(user) || CanPrepareActivation(user) || CanReactivateMembership(user);

    private bool CanApproveAccount(UserAdministrationResDTO user) =>
        CanPrepareActivation(user)
        && GetSelectedGroupId(user).HasValue
        && GetSelectedDepartmentId(user).HasValue;

    private bool CanToggleUserAccess(UserAdministrationResDTO user) =>
        user.IsActive
            ? CanDeactivateMembership(user)
            : CanReactivateMembership(user)
              && GetSelectedGroupId(user).HasValue
              && GetSelectedDepartmentId(user).HasValue;

    private bool CanSendPasswordLink(UserAdministrationResDTO user) =>
        CanManageUsers
        && accountCapabilities.InvitationEnabled
        && user.UserId > 0
        && user.UserId != UserClaims
        && !string.IsNullOrWhiteSpace(user.Email)
        && !string.Equals(user.AccountStatus, "Disabled", StringComparison.OrdinalIgnoreCase);

    protected bool CanDeactivateMembership(UserAdministrationResDTO user) =>
        CanEditMembership(user);

    private async Task UpsertMembershipAsync(
        UserAdministrationResDTO user,
        Guid groupId,
        Guid departmentId)
    {
        var isReactivation = CanReactivateMembership(user);
        if (!(CanEditMembership(user) || isReactivation)
            || groupId == Guid.Empty
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
                GroupId = groupId,
                PrimaryDepartmentId = departmentId,
                ExpectedRowVersion = user.IsActive ? GetRowVersion(user) : null,
                Reason = isReactivation ? Loc["MembershipReactivationReason"] : Loc["MembershipUpdateReason"]
            };
            await _apiServices.PutFromApiAsync<MembershipAdministrationResDTO>(
                "/api/Permission/memberships",
                request);
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = Loc[isReactivation ? "UserAccessEnabled" : "MembershipUpdated"].Value,
                Duration = 3000
            });
            await ReloadUsersAsync();
        }
        catch (Exception ex)
        {
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
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

    private string GetAccountStatusLabel(UserAdministrationResDTO user) =>
        user.AccountStatus switch
        {
            "Active" when user.IsActive => Loc["AccountStatusActive"].Value,
            "Active" => Loc["AccountStatusUnassigned"].Value,
            "PendingApproval" => Loc["AccountStatusPendingApproval"].Value,
            "Disabled" => Loc["AccountStatusDisabled"].Value,
            _ => user.AccountStatus ?? Loc["StatusUnknown"].Value
        };

    private string GetInvitationStatusLabel(UserAdministrationResDTO user)
        => !user.EmailConfirmed && user.MustChangePassword
            ? Loc["InvitationPendingPassword"]
            : user.EmailConfirmed && user.MustChangePassword
                ? Loc["InvitationMustChangePassword"]
                : user.EmailConfirmed ? Loc["InvitationConfirmed"] : Loc["InvitationUnconfirmed"];

    private static VppStatusTone GetAccountStatusTone(UserAdministrationResDTO user) => user.AccountStatus switch
    {
        "Active" when user.IsActive => VppStatusTone.Success,
        "PendingApproval" => VppStatusTone.Warning,
        "Disabled" => VppStatusTone.Neutral,
        "Active" => VppStatusTone.Info,
        _ => VppStatusTone.Neutral
    };

    private static VppStatusTone GetInvitationTone(UserAdministrationResDTO user)
        => !user.EmailConfirmed && user.MustChangePassword
            ? VppStatusTone.Warning
            : user.EmailConfirmed && !user.MustChangePassword ? VppStatusTone.Success : VppStatusTone.Info;

    private bool IsPendingApproval(UserAdministrationResDTO user) =>
        string.Equals(user.AccountStatus, "PendingApproval", StringComparison.OrdinalIgnoreCase);

    private bool IsCurrentUser(UserAdministrationResDTO user) => user.UserId == UserClaims;

    private string GetAssignmentTitle(UserAdministrationResDTO user) =>
        IsCurrentUser(user)
            ? Loc["SelfMembershipChangeBlocked"].Value
            : CanAssignMembership(user)
                ? IsPendingApproval(user)
                    ? Loc["SelectGroupAndDepartmentBeforeApproval"].Value
                    : Loc["EditMembershipAssignment"].Value
                : Loc["MembershipChangeUnavailable"].Value;

    private string GetApprovalActionTitle(UserAdministrationResDTO user)
    {
        if (IsCurrentUser(user)) return Loc["SelfMembershipChangeBlocked"].Value;
        if (!GetSelectedGroupId(user).HasValue && !GetSelectedDepartmentId(user).HasValue)
        {
            return Loc["SelectGroupAndDepartmentBeforeApproval"].Value;
        }

        if (!GetSelectedGroupId(user).HasValue) return Loc["SelectPermissionGroupBeforeApproval"].Value;
        if (!GetSelectedDepartmentId(user).HasValue) return Loc["SelectDepartmentBeforeApproval"].Value;
        return Loc["ApproveAccount"].Value;
    }

    private string GetPasswordLinkTitle(UserAdministrationResDTO user) =>
        IsCurrentUser(user)
            ? Loc["SelfPasswordLinkBlocked"].Value
            : Loc["SendPasswordLink"].Value;

    private string GetAccessToggleTitle(UserAdministrationResDTO user) =>
        user.IsActive ? Loc["DisableUserAccess"].Value
        : IsPendingApproval(user) ? Loc["ApproveAccountBeforeAccess"].Value
        : string.Equals(user.AccountStatus, "Disabled", StringComparison.OrdinalIgnoreCase)
            ? Loc["DisabledAccountAccessLocked"].Value
            : Loc["EnableUserAccess"].Value;

    private void NotifyError(string detail) => Toast.Notify(new NotificationMessage
    {
        Severity = NotificationSeverity.Error,
        Summary = Loc["Error"].Value,
        Detail = detail,
        Duration = 10000
    });

    private string BuildUsersEndpoint(int? skip, int? top, string? orderBy)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            queryParams.Add($"search={Uri.EscapeDataString(SearchText.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(SelectedAccountStatus))
        {
            queryParams.Add($"accountStatus={Uri.EscapeDataString(SelectedAccountStatus)}");
        }

        if (SelectedGroupId.HasValue)
        {
            queryParams.Add($"groupId={SelectedGroupId.Value}");
        }

        if (SelectedDepartmentId.HasValue)
        {
            queryParams.Add($"departmentId={SelectedDepartmentId.Value}");
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

    public void Dispose()
    {
        searchDebounceCts?.Cancel();
        searchDebounceCts?.Dispose();
    }
}
