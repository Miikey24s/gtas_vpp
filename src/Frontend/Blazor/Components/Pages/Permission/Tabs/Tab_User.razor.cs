// PAGE LOGIC: Permission/Tabs/Tab_User.razor.cs
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Components.Pages.Permission.Dialogs;
using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Platform.State;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Account;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs;

public partial class Tab_User : IDisposable
{
    [Inject] public UserAdministrationApiClient UserAdminApi { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;
    [Inject] public UiBusyState BusyState { get; set; } = default!;

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
    private IReadOnlyList<VppFilterOption<Guid?>> GroupFilterOptions = [];
    private IReadOnlyList<VppFilterOption<Guid?>> DepartmentFilterOptions = [];
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
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UserClaims = PermissionState.CurrentUserId;
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
            var lookups = await UserAdminApi.GetLookupsAsync(CanManageUsers);
            permissionGroups = lookups.Groups.ToList();
            departments = lookups.Departments.ToList();
            accountCapabilities = lookups.Capabilities;
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
            GroupFilterOptions = permissionGroups
                .Select(group => new VppFilterOption<Guid?>(group.Id, group.GroupName ?? "–"))
                .ToArray();
            DepartmentFilterOptions = departments
                .Select(department => new VppFilterOption<Guid?>(department.Id, department.Name ?? "–"))
                .ToArray();
            isUserLookupLoading = false;
        }
    }

    protected async Task ButtonOnClick_Clear()
    {
        CancelPendingSearch();
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
        CancelPendingSearch();
        SelectedAccountStatus = string.IsNullOrWhiteSpace(value) ? null : value;
        if (userGrid is not null)
        {
            await userGrid.FirstPage(true);
        }
    }

    private async Task OnGroupFilterChangedAsync(Guid? value)
    {
        CancelPendingSearch();
        SelectedGroupId = value;
        if (userGrid is not null) await userGrid.FirstPage(true);
    }

    private async Task OnDepartmentFilterChangedAsync(Guid? value)
    {
        CancelPendingSearch();
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
            var result = await UserAdminApi.GetUsersAsync(new UserAdministrationQuery(
                args.Skip ?? 0,
                args.Top ?? VppPagingProfiles.Collection.DefaultPageSize,
                SearchText,
                SelectedAccountStatus,
                SelectedGroupId,
                SelectedDepartmentId,
                args.OrderBy));
            users = result.Items.ToList();
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
        }
    }

    protected async Task SearchTextOnInput(ChangeEventArgs args)
    {
        SearchText = args.Value?.ToString() ?? string.Empty;
        CancelPendingSearch();
        var debounce = searchDebounceCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, debounce.Token);
            if (userGrid is not null)
            {
                await userGrid.FirstPage(true);
            }
        }
        catch (OperationCanceledException) when (debounce.IsCancellationRequested)
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
            await UserAdminApi.InviteAsync(request);
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

        using var busy = BusyState.Begin();
        isUserLoading = true;
        try
        {
            await UserAdminApi.ActivateAsync(
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

        using var busy = BusyState.Begin();
        isUserLoading = true;
        try
        {
            await UserAdminApi.SendPasswordResetLinkAsync(
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

        using var busy = BusyState.Begin();
        isUserLoading = true;
        try
        {
            var request = new MembershipDeactivateReqDTO
            {
                AccountId = user.UserId,
                ExpectedRowVersion = GetRowVersion(user),
                Reason = "Membership deactivated by permission administrator."
            };
            await UserAdminApi.DeactivateMembershipAsync(request);
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

        using var busy = BusyState.Begin();
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
            await UserAdminApi.UpsertMembershipAsync(request);
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
        "Active" when user.IsActive => VppStatusToneContract.Resolve("Active"),
        "PendingApproval" => VppStatusToneContract.Resolve("PendingApproval"),
        "Disabled" => VppStatusToneContract.Resolve("Disabled"),
        "Active" => VppStatusTone.Neutral,
        _ => VppStatusTone.Neutral
    };

    private static VppStatusTone GetInvitationTone(UserAdministrationResDTO user)
        => !user.EmailConfirmed && user.MustChangePassword
            ? VppStatusTone.Warning
            : user.EmailConfirmed && !user.MustChangePassword ? VppStatusTone.Success : VppStatusTone.Info;

    private bool IsPendingApproval(UserAdministrationResDTO user) =>
        string.Equals(user.AccountStatus, "PendingApproval", StringComparison.OrdinalIgnoreCase);

    private bool IsCurrentUser(UserAdministrationResDTO user) => user.UserId == UserClaims;

    private static string GetUserSecondaryIdentity(UserAdministrationResDTO user)
    {
        var parts = new[] { user.UserLogin, user.EmployeeCode }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var value = string.Join(" · ", parts);
        return string.IsNullOrWhiteSpace(value) ? "–" : value;
    }

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

    private IReadOnlyList<VppAdminActionMenuItem> UserSecondaryActions(UserAdministrationResDTO user) =>
    [
        new(
            "send-password-link",
            Loc["SendPasswordLink"].Value,
            "mark_email_unread",
            () => SendPasswordResetLinkAsync(user),
            !CanSendPasswordLink(user),
            DisabledReason: GetPasswordLinkTitle(user))
    ];

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

    public void Dispose()
    {
        CancelPendingSearch();
    }

    private void CancelPendingSearch()
    {
        searchDebounceCts?.Cancel();
        searchDebounceCts?.Dispose();
        searchDebounceCts = null;
    }
}
