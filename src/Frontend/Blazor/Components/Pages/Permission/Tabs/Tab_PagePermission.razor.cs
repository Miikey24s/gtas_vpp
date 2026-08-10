// PAGE LOGIC: Permission/Tabs/Tab_PagePermission.razor.cs
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
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Permission;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Radzen.Blazor;
using PermissionGroupDto = gtas_vpp_shared.DTOs.Res.Permission.PermissionGroupResDTO;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs;

public partial class Tab_PagePermission
{
    [Inject] public PermissionAdministrationApiClient PermissionAdminApi { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;
    [Inject] public UiBusyState BusyState { get; set; } = default!;

    [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();

    public List<PermissionGroupDto> list_Group { get; set; } = [];
    public RadzenDataGrid<PermissionGroupDto> grid { get; set; } = default!;
    public List<PermissionPageComponentResDTO> groupPermissions { get; set; } = [];
    public bool IsLoading { get; set; }
    public bool IsLoading_Child { get; set; }

    private int selectedPermissionView;
    private int groupCount;
    private int currentGroupSkip;
    private string groupSearchText = string.Empty;
    private bool hasRequestedInitialGroupGridLoad;

    private IReadOnlyList<VppSegmentedOption<int>> PermissionViewOptions =>
    [
        new(0, Loc["PermissionUiView"]),
        new(1, Loc["PermissionApiReference"])
    ];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            NavigationManager.NavigateTo("Home", true);
            return;
        }

    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !hasRequestedInitialGroupGridLoad && grid is not null)
        {
            hasRequestedInitialGroupGridLoad = true;
            await grid.Reload();
        }
    }

    private Task SelectPermissionViewAsync(int view)
    {
        selectedPermissionView = view;
        return Task.CompletedTask;
    }

    protected async Task LoadGroupsAsync(LoadDataArgs args)
    {
        IsLoading = true;
        currentGroupSkip = args.Skip ?? 0;
        StateHasChanged();

        try
        {
            var result = await PermissionAdminApi.GetGroupsAsync(new PermissionGroupQuery(
                args.Skip ?? 0,
                args.Top ?? VppPagingProfiles.Collection.DefaultPageSize,
                groupSearchText,
                args.OrderBy));
            list_Group = result.Items.ToList();
            groupCount = result.TotalCount;

        }
        catch (Exception ex)
        {
            list_Group = [];
            groupPermissions = [];
            groupCount = 0;
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnGroupSearchInputAsync(ChangeEventArgs args)
    {
        groupSearchText = args.Value?.ToString() ?? string.Empty;
        await grid.FirstPage(true);
    }

    private async Task ClearGroupFiltersAsync()
    {
        groupSearchText = string.Empty;
        await grid.FirstPage(true);
    }

    private async Task OpenPermissionEditorAsync(PermissionGroupDto group)
    {
        if (!PermissionState.HasPermission(Permissions.PermissionManage)
            || !groupPermissions.SelectMany(page => page.Components ?? []).Any(component => component.CanConfigure))
        {
            return;
        }

        var result = await DialogService.OpenAsync<Dialog_PermissionUiBatchEditor>(
            Loc["ConfigureUiPermissions"],
            new Dictionary<string, object?>
            {
                [nameof(Dialog_PermissionUiBatchEditor.Group)] = group,
                [nameof(Dialog_PermissionUiBatchEditor.Pages)] = groupPermissions
            },
            VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Workspace,
                Loc["ConfigureUiPermissions"],
                closeAriaLabel: Loc["Close"].Value));
        if (result is not BatchPatchComponentMappingsReqDTO request) return;

        using var busy = BusyState.Begin();
        IsLoading_Child = true;
        try
        {
            var response = await PermissionAdminApi.PatchComponentMappingsAsync(request);

            if (request.PermissionGroupId == PermissionState.CurrentGroupId)
            {
                await PermissionState.RefreshAsync();
                if (!PermissionState.HasPageAccess(Config.Page_ComponentCode.PageCode.Permission))
                {
                    NavigationManager.NavigateTo("/dashboard?tab=0", true);
                    return;
                }
            }

            await LoadGroupPermissionsAsync(request.PermissionGroupId, notifyErrors: false);
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = Loc["PermissionUpdated"],
                Detail = Loc["PermissionBatchUpdated", response?.UpdatedCount ?? 0],
                Duration = 4000
            });
        }
        catch (Exception ex)
        {
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
            await LoadGroupPermissionsAsync(request.PermissionGroupId, notifyErrors: false);
        }
        finally
        {
            IsLoading_Child = false;
            StateHasChanged();
        }
    }

    private async Task OpenPermissionEditorForGroupAsync(PermissionGroupDto group)
    {
        await LoadGroupPermissionsAsync(group.Id, notifyErrors: true);
        await OpenPermissionEditorAsync(group);
    }

    private async Task LoadGroupPermissionsAsync(Guid groupId, bool notifyErrors)
    {
        using var busy = BusyState.Begin();
        IsLoading_Child = true;
        StateHasChanged();
        try
        {
            groupPermissions = await PermissionAdminApi.GetGroupPageComponentsAsync(groupId) ?? [];
        }
        catch (Exception ex)
        {
            groupPermissions = [];
            if (notifyErrors) NotifyError(UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            IsLoading_Child = false;
            StateHasChanged();
        }
    }

    private string GetGroupDisplayName(PermissionGroupDto group) => GetRoleDisplayName(group.GroupCode, group.GroupName);
    private static string GetGroupCode(PermissionGroupDto group) => string.IsNullOrWhiteSpace(group.GroupCode) ? "—" : group.GroupCode;

    private string GetAccessStateLabel(PermissionComponentAccessResDTO component) =>
        !component.IsVisible ? Loc["AccessHidden"]
        : component.IsEnable ? Loc["AccessEnabled"]
        : Loc["AccessReadOnly"];

    private static VppStatusTone GetAccessStateTone(PermissionComponentAccessResDTO component) =>
        !component.IsVisible ? VppStatusTone.Neutral
        : component.IsEnable ? VppStatusTone.Info
        : VppStatusTone.Info;

    private string GetAdministrationModeLabel(PermissionComponentAccessResDTO component) => component.AdministrationMode switch
    {
        "ActionMatrix" => Loc["CanonicalAction"],
        "Required" => Loc["RequiredAccess"],
        "Configurable" => Loc["ConfigurableAccess"],
        "OutsideRoleCeiling" => Loc["OutsideRoleCeiling"],
        _ => Loc["ReadOnlyReference"]
    };

    protected string GetPermissionKind(PermissionComponentAccessResDTO component) =>
        Permissions.IsActionCode(component.ComponentCode)
            ? Loc["PermissionKindFixedApi"]
            : Loc["PermissionKindUiDisplay"];

    private void NotifyError(string detail) => Toast.Notify(new NotificationMessage
    {
        Severity = NotificationSeverity.Error,
        Summary = Loc["Error"],
        Detail = detail,
        Duration = 10000
    });

    private bool IsDevPersona(RbacPersonaDefinition persona) =>
        string.Equals(persona.GroupCode, CanonicalRbac.Dev.GroupCode, StringComparison.OrdinalIgnoreCase);

    private string GetRoleDisplayName(string? groupCode, string? fallback = null) => groupCode switch
    {
        "EMPLOYEE" => Loc["RoleEmployee"],
        "MANAGER" => Loc["RoleManager"],
        "DEV" => Loc["RoleDev"],
        _ => fallback ?? groupCode ?? "—"
    };

    private string GetActionDisplayName(string permissionCode) => permissionCode switch
    {
        Permissions.RequestViewOwn => Loc["PermissionActionRequestViewOwn"],
        Permissions.RequestViewDepartment => Loc["PermissionActionRequestViewDepartment"],
        Permissions.RequestViewAll => Loc["PermissionActionRequestViewAll"],
        Permissions.RequestCreate => Loc["PermissionActionRequestCreate"],
        Permissions.RequestUpdateOwn => Loc["PermissionActionRequestUpdateOwn"],
        Permissions.RequestCancelOwn => Loc["PermissionActionRequestCancelOwn"],
        Permissions.RequestApprove => Loc["PermissionActionRequestApprove"],
        Permissions.RequestReject => Loc["PermissionActionRequestReject"],
        Permissions.RequestCatalogView => Loc["PermissionActionRequestCatalogView"],
        Permissions.LibraryView => Loc["PermissionActionLibraryView"],
        Permissions.LibraryManage => Loc["PermissionActionLibraryManage"],
        Permissions.PermissionView => Loc["PermissionActionPermissionView"],
        Permissions.PermissionManage => Loc["PermissionActionPermissionManage"],
        Permissions.ReportViewOwn => Loc["PermissionActionReportViewOwn"],
        Permissions.ReportViewDepartment => Loc["PermissionActionReportViewDepartment"],
        Permissions.ReportViewAll => Loc["PermissionActionReportViewAll"],
        Permissions.ReportExport => Loc["PermissionActionReportExport"],
        Permissions.PeriodSettle => Loc["PermissionActionPeriodSettle"],
        _ => permissionCode
    };
}
