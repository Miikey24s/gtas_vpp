// PAGE LOGIC: Permission/Tabs/Tab_SecurityAudit.razor.cs
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Components.Pages.Permission.Dialogs;
using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Permission;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs;

public partial class Tab_SecurityAudit : IDisposable
{
    [Inject] public PermissionAdministrationApiClient PermissionAdminApi { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;
    [Inject] public IToastService Toast { get; set; } = default!;

    private readonly List<SecurityAuditResDTO> audits = [];
    private RadzenDataGrid<SecurityAuditResDTO>? auditGrid;
    private SecurityAuditFilterOptionsResDTO filterOptions = new();
    private string SearchText { get; set; } = string.Empty;
    private string? SelectedAction { get; set; }
    private string? SelectedOutcome { get; set; }
    private int auditCount;
    private int currentSkip;
    private bool isLoading;
    private bool isLoadingOptions;
    private bool hasRequestedInitialLoad;
    private CancellationTokenSource? searchDebounceCts;

    private bool HasFilters => !string.IsNullOrWhiteSpace(SearchText)
                               || !string.IsNullOrWhiteSpace(SelectedAction)
                               || !string.IsNullOrWhiteSpace(SelectedOutcome);
    private IReadOnlyList<VppFilterOption<string>> ActionOptions =>
        [new(string.Empty, Loc["AllAuditActions"]), .. filterOptions.Actions.Select(value => new VppFilterOption<string>(value, GetActionLabel(value)))];
    private IReadOnlyList<VppFilterOption<string>> OutcomeOptions =>
        [new(string.Empty, Loc["AllAuditOutcomes"]), .. filterOptions.Outcomes.Select(value => new VppFilterOption<string>(value, GetOutcomeLabel(value)))];

    protected override async Task OnInitializedAsync()
    {
        isLoadingOptions = true;
        try
        {
            filterOptions = await PermissionAdminApi.GetSecurityAuditFilterOptionsAsync() ?? new();
        }
        catch (Exception ex)
        {
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            isLoadingOptions = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !hasRequestedInitialLoad && auditGrid is not null)
        {
            hasRequestedInitialLoad = true;
            await auditGrid.Reload();
        }
    }

    private async Task LoadAuditsAsync(LoadDataArgs args)
    {
        isLoading = true;
        currentSkip = args.Skip ?? 0;
        StateHasChanged();
        try
        {
            var result = await PermissionAdminApi.GetSecurityAuditsAsync(new SecurityAuditQuery(
                args.Skip ?? 0,
                args.Top ?? VppPagingProfiles.Collection.DefaultPageSize,
                SearchText,
                SelectedAction,
                SelectedOutcome,
                args.OrderBy));
            audits.Clear();
            audits.AddRange(result.Items);
            auditCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            audits.Clear();
            auditCount = 0;
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SearchTextOnInput(ChangeEventArgs args)
    {
        SearchText = args.Value?.ToString() ?? string.Empty;
        CancelPendingSearch();
        var debounce = searchDebounceCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, debounce.Token);
            if (auditGrid is not null) await auditGrid.FirstPage(true);
        }
        catch (OperationCanceledException) when (debounce.IsCancellationRequested)
        {
        }
    }

    private async Task OnActionChangedAsync(string value)
    {
        CancelPendingSearch();
        SelectedAction = string.IsNullOrWhiteSpace(value) ? null : value;
        if (auditGrid is not null) await auditGrid.FirstPage(true);
    }

    private async Task OnOutcomeChangedAsync(string value)
    {
        CancelPendingSearch();
        SelectedOutcome = string.IsNullOrWhiteSpace(value) ? null : value;
        if (auditGrid is not null) await auditGrid.FirstPage(true);
    }

    private async Task ClearFiltersAsync()
    {
        CancelPendingSearch();
        SearchText = string.Empty;
        SelectedAction = null;
        SelectedOutcome = null;
        if (auditGrid is not null) await auditGrid.FirstPage(true);
    }

    private Task OpenAuditDetailAsync(SecurityAuditResDTO audit) => DialogService.OpenAsync<Dialog_SecurityAuditDetail>(
        Loc["SecurityAuditDetail"],
        new Dictionary<string, object?>
        {
            [nameof(Dialog_SecurityAuditDetail.Audit)] = audit,
            [nameof(Dialog_SecurityAuditDetail.ActionLabel)] = GetActionLabel(audit.Action),
            [nameof(Dialog_SecurityAuditDetail.OutcomeLabel)] = GetOutcomeLabel(audit.Outcome),
            [nameof(Dialog_SecurityAuditDetail.OutcomeTone)] = GetOutcomeTone(audit.Outcome),
            [nameof(Dialog_SecurityAuditDetail.ActorLabel)] = GetActorLabel(audit),
            [nameof(Dialog_SecurityAuditDetail.TargetLabel)] = GetTargetLabel(audit),
            [nameof(Dialog_SecurityAuditDetail.ResourceLabel)] = $"{audit.ResourceType} · {audit.ResourceId ?? "—"}"
        },
        VppAdminDialogProfiles.Create(
            VppAdminDialogSize.Standard,
            Loc["SecurityAuditDetail"],
            closeAriaLabel: Loc["Close"].Value));

    private string GetActionLabel(string action) => action switch
    {
        "AUTH_BOOTSTRAP_OWNER_CREATED" => Loc["AuditActionBootstrapOwner"],
        "ACCOUNT_INVITED" => Loc["AuditActionAccountInvited"],
        "ACCOUNT_INVITATION_ACCEPTED" => Loc["AuditActionInvitationAccepted"],
        "ACCOUNT_ACTIVATED" => Loc["AuditActionAccountActivated"],
        "ACCOUNT_REGISTERED" => Loc["AuditActionAccountRegistered"],
        "ACCOUNT_EMAIL_CONFIRMED" => Loc["AuditActionEmailConfirmed"],
        "ACCOUNT_EMAIL_CONFIRMATION_RESEND_REQUESTED" => Loc["AuditActionEmailConfirmationResend"],
        "ACCOUNT_PASSWORD_CHANGED" => Loc["AuditActionPasswordChanged"],
        "ACCOUNT_PASSWORD_RESET" => Loc["AuditActionPasswordReset"],
        "ACCOUNT_ADMIN_RESET_LINK_SENT" => Loc["AuditActionResetLinkSent"],
        "MEMBERSHIP_CREATED" => Loc["AuditActionMembershipCreated"],
        "MEMBERSHIP_UPDATED" => Loc["AuditActionMembershipUpdated"],
        "MEMBERSHIP_DEACTIVATED" => Loc["AuditActionMembershipDeactivated"],
        "PERMISSION_UI_BATCH_UPDATED" => Loc["AuditActionPermissionUpdated"],
        "SESSION_REVOKED" => Loc["AuditActionSessionRevoked"],
        _ => HumanizeCode(action)
    };

    private string GetOutcomeLabel(string outcome) => outcome switch
    {
        "Succeeded" => Loc["AuditOutcomeSucceeded"],
        "Failed" => Loc["AuditOutcomeFailed"],
        "Rejected" => Loc["AuditOutcomeRejected"],
        _ => HumanizeCode(outcome)
    };

    private static VppStatusTone GetOutcomeTone(string outcome) =>
        VppStatusToneContract.Resolve(outcome);

    private string GetActorLabel(SecurityAuditResDTO audit) => DisplayUser(audit.ActorFullName, audit.ActorUserName, audit.ActorUserId, Loc["SystemActor"]);
    private string GetActorSecondaryLabel(SecurityAuditResDTO audit) => audit.ActorUserName ?? audit.ActorUserId?.ToString() ?? Loc["SystemActor"];
    private string GetTargetLabel(SecurityAuditResDTO audit) => DisplayUser(audit.TargetFullName, audit.TargetUserName, audit.TargetUserId, Loc["NotApplicable"]);
    private string GetTargetSecondaryLabel(SecurityAuditResDTO audit) => audit.TargetUserName ?? audit.TargetUserId?.ToString() ?? "—";

    private static string DisplayUser(string? fullName, string? userName, int? userId, string fallback)
        => !string.IsNullOrWhiteSpace(fullName) ? fullName
            : !string.IsNullOrWhiteSpace(userName) ? userName
            : userId?.ToString() ?? fallback;

    private static string HumanizeCode(string value)
        => string.Join(' ', value.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));

    private void NotifyError(string detail) => Toast.Notify(new NotificationMessage
    {
        Severity = NotificationSeverity.Error,
        Summary = Loc["Error"],
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
