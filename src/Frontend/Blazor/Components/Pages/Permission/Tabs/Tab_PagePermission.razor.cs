using System.Security.Claims;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.Pages.Permission.Dialogs;
using gtas_vpp_fe.Helpers;
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
    [Inject] public IAPIServices _apiServices { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;

    [Parameter] public IEnumerable<Claim> claims { get; set; } = [];
    [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();

    public List<PermissionGroupDto> list_Group { get; set; } = [];
    public RadzenDataGrid<PermissionGroupDto> grid { get; set; } = default!;
    public IList<PermissionGroupDto> selected_Group { get; set; } = [];
    public List<PermissionPageComponentResDTO> groupPermissions { get; set; } = [];
    public RadzenDataGrid<PermissionComponentAccessResDTO> componentGrid { get; set; } = default!;

    public int selectedTab { get; set; }
    public bool IsLoading { get; set; }
    public bool IsLoading_Child { get; set; }

    private int selectedPermissionView;
    private int groupCount;
    private int currentGroupSkip;
    private string? currentGroupFilterExpression;
    private bool hasRequestedInitialGroupGridLoad;

    private PermissionGroupDto? SelectedGroup => selected_Group.FirstOrDefault();
    private PermissionPageComponentResDTO? SelectedPermissionPage => groupPermissions.ElementAtOrDefault(selectedTab);
    private bool CanConfigureSelectedGroup =>
        PermissionState.HasPermission(Permissions.PermissionManage)
        && SelectedGroup is not null
        && groupPermissions.SelectMany(page => page.Components ?? []).Any(component => component.CanConfigure);

    private IReadOnlyList<VppSegmentedOption<int>> PermissionViewOptions =>
    [
        new(0, Loc["PermissionUiView"]),
        new(1, Loc["PermissionApiReference"])
    ];

    private IReadOnlyList<VppSegmentedOption<int>> PermissionPageOptions => groupPermissions
        .Select((page, index) => new VppSegmentedOption<int>(index, string.IsNullOrWhiteSpace(page.PageName) ? page.PageCode ?? "—" : page.PageName))
        .ToArray();

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
        currentGroupFilterExpression = args.Filter;
        StateHasChanged();

        try
        {
            var result = await _apiServices.GetFromApiWithTotalCountAsync<List<PermissionGroupDto>>(
                BuildGroupsEndpoint(args.Filter, args.Skip, args.Top, args.OrderBy));
            list_Group = result.Data ?? [];
            groupCount = result.TotalCount;

            var selectedId = SelectedGroup?.Id;
            var nextSelection = selectedId.HasValue
                ? list_Group.FirstOrDefault(group => group.Id == selectedId.Value)
                : list_Group.FirstOrDefault();
            if (nextSelection is not null
                && (SelectedGroup is null || SelectedGroup.Id != nextSelection.Id))
            {
                selected_Group = [nextSelection];
                await LoadGroupPermissionsAsync(nextSelection.Id, notifyErrors: false);
            }
            else if (nextSelection is null)
            {
                selected_Group = [];
                groupPermissions = [];
            }
        }
        catch (Exception ex)
        {
            list_Group = [];
            selected_Group = [];
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

    protected async Task LoadGroupFilterDataAsync(DataGridLoadColumnFilterDataEventArgs<PermissionGroupDto> args)
    {
        if (args.Column is null) return;

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
            if (args.Skip.HasValue) queryParams.Add($"skip={args.Skip.Value}");
            if (args.Top.HasValue) queryParams.Add($"top={args.Top.Value}");

            var result = await _apiServices.GetFromApiWithTotalCountAsync<List<PermissionGroupDto>>(
                $"/api/Permission/groups?{string.Join("&", queryParams)}");
            args.Data = result.Data ?? [];
            args.Count = result.TotalCount;
        }
        catch (Exception ex)
        {
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
        }
    }

    private async Task OnGroupSelectedAsync(PermissionGroupDto group)
    {
        selected_Group = [group];
        await LoadGroupPermissionsAsync(group.Id, notifyErrors: true);
    }

    private Task SelectPermissionPageAsync(int index)
    {
        selectedTab = index;
        return Task.CompletedTask;
    }

    private async Task OpenPermissionEditorAsync()
    {
        if (!CanConfigureSelectedGroup || SelectedGroup is null) return;

        var result = await DialogService.OpenAsync<Dialog_PermissionUiBatchEditor>(
            Loc["ConfigureUiPermissions"],
            new Dictionary<string, object?>
            {
                [nameof(Dialog_PermissionUiBatchEditor.Group)] = SelectedGroup,
                [nameof(Dialog_PermissionUiBatchEditor.Pages)] = groupPermissions
            },
            VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Workspace,
                Loc["ConfigureUiPermissions"],
                closeAriaLabel: Loc["Close"].Value));
        if (result is not BatchPatchComponentMappingsReqDTO request) return;

        glb.isBusyPage = true;
        IsLoading_Child = true;
        try
        {
            var response = await _apiServices.PatchFromApiAsync<BatchPatchComponentMappingsResDTO>(
                "/api/Permission/component-mappings/batch",
                request);

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
            glb.isBusyPage = false;
            StateHasChanged();
        }
    }

    private async Task LoadGroupPermissionsAsync(Guid groupId, bool notifyErrors)
    {
        glb.isBusyPage = true;
        IsLoading_Child = true;
        StateHasChanged();
        try
        {
            groupPermissions = await _apiServices.GetFromApiAsync<List<PermissionPageComponentResDTO>>(
                $"/api/Permission/groups/{groupId}/page-components") ?? [];
            selectedTab = Math.Clamp(selectedTab, 0, Math.Max(0, groupPermissions.Count - 1));
        }
        catch (Exception ex)
        {
            groupPermissions = [];
            selectedTab = 0;
            if (notifyErrors) NotifyError(UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            IsLoading_Child = false;
            glb.isBusyPage = false;
            StateHasChanged();
        }
    }

    private string GetGroupDisplayName(PermissionGroupDto group) => GetRoleDisplayName(group.GroupCode, group.GroupName);
    private static string GetGroupCode(PermissionGroupDto group) => string.IsNullOrWhiteSpace(group.GroupCode) ? "—" : group.GroupCode;

    private string GetAccessStateLabel(PermissionComponentAccessResDTO component) =>
        !component.IsVisible ? Loc["AccessHidden"]
        : component.IsEnable ? Loc["AccessEnabled"]
        : Loc["AccessReadOnly"];

    private static BadgeStyle GetAccessStateBadge(PermissionComponentAccessResDTO component) =>
        !component.IsVisible ? BadgeStyle.Light
        : component.IsEnable ? BadgeStyle.Success
        : BadgeStyle.Info;

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

    private static string BuildGroupsEndpoint(string? filter, int? skip, int? top, string? orderBy)
    {
        var queryParams = new List<string> { "getFullName=true" };
        if (!string.IsNullOrWhiteSpace(filter)) queryParams.Add($"filter={Uri.EscapeDataString(filter)}");
        if (skip.HasValue) queryParams.Add($"skip={skip.Value}");
        if (top.HasValue) queryParams.Add($"top={top.Value}");
        if (!string.IsNullOrWhiteSpace(orderBy)) queryParams.Add($"orderby={Uri.EscapeDataString(orderBy)}");
        return $"/api/Permission/groups?{string.Join("&", queryParams)}";
    }

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
