using gtas_vpp_fe.Components.Pages.Lib;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;
using PermissionGroupDto = gtas_vpp_shared.DTOs.Res.Permission.PermissionGroupResDTO;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs;

public partial class Tab_PagePermission
{
    private string GetGroupDisplayName(PermissionGroupDto group) =>
        GetRoleDisplayName(group.GroupCode, group.GroupName);

    private static string GetGroupCode(PermissionGroupDto group) =>
        string.IsNullOrWhiteSpace(group.GroupCode) ? "—" : group.GroupCode;

    [Inject] public IToastService _toastService { get; set; } = default!;
    [Inject] public IAPIServices _apiServices { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;

    [Parameter] public IEnumerable<Claim> claims { get; set; } = [];
    [Parameter]
    public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();

    public List<PermissionGroupDto> list_Group { get; set; } = [];
    public RadzenDataGrid<PermissionGroupDto> grid { get; set; } = default!;
    public IList<PermissionGroupDto> selected_Group { get; set; } = [];
    public List<PermissionPageComponentResDTO> groupPermissions { get; set; } = [];
    public IList<PermissionComponentAccessResDTO> selectedComponents { get; set; } = [];
    public RadzenDataGrid<PermissionComponentAccessResDTO> componentGrid { get; set; } = default!;

    public int selectedTab { get; set; }
    public bool IsLoading { get; set; }
    public bool IsLoading_Child { get; set; }

    private int groupCount;
    private int currentGroupSkip;
    private string? currentGroupFilterExpression;
    private bool hasRequestedInitialGroupGridLoad;

    private PermissionPageComponentResDTO? SelectedPermissionPage =>
        groupPermissions.ElementAtOrDefault(selectedTab);

    private IReadOnlyList<VppSegmentedOption<int>> PermissionPageOptions => groupPermissions
        .Select((page, index) => new VppSegmentedOption<int>(
            index,
            $"{page.PageName} ({page.PageCode})"))
        .ToArray();

    private Task SelectPermissionPageAsync(int index)
    {
        selectedTab = index;
        selectedComponents = [];
        return Task.CompletedTask;
    }

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

    protected async Task LoadBaseData()
    {
        groupPermissions = [];
        selectedTab = 0;
        await grid.Reload();
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
        }
        catch (Exception ex)
        {
            list_Group = [];
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

    protected Task GroupRowExpand(PermissionGroupDto group) => LoadGroupPermissionsAsync(group.Id, true);

    protected async Task SetComponentVisibilityAsync(
        PermissionComponentAccessResDTO component)
    {
        if (!component.IsVisible)
        {
            component.IsEnable = false;
        }

        await PersistComponentAsync(component);
    }

    protected async Task SetComponentEnabledAsync(
        PermissionComponentAccessResDTO component)
    {
        if (component.IsEnable)
        {
            component.IsVisible = true;
        }

        await PersistComponentAsync(component);
    }

    private async Task PersistComponentAsync(
        PermissionComponentAccessResDTO component)
    {
        if (!CanToggleComponent(component))
        {
            await LoadGroupPermissionsAsync(component.GroupId, false);
            return;
        }

        glb.isBusyPage = true;
        IsLoading_Child = true;
        StateHasChanged();

        try
        {
            var request = new PatchComponentMappingReqDTO
            {
                PageComponentMappingId = component.GroupPageComponentMappingId,
                PermissionGroupId = component.GroupId,
                IsEnable = component.IsEnable,
                IsVisible = component.IsVisible
            };

            await _apiServices.PatchFromApiAsync<object>("/api/Permission/component-mapping", request);

            if (component.GroupId == PermissionState.CurrentGroupId)
            {
                await PermissionState.RefreshAsync();

                if (!PermissionState.HasPageAccess(Config.Page_ComponentCode.PageCode.Permission))
                {
                    NavigationManager.NavigateTo("/dashboard?tab=0", true);
                    return;
                }
            }

            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = Loc["PermissionUpdated"].Value,
                Detail = Loc["PermissionComponentUpdated", component.ComponentName ?? string.Empty].Value,
                Duration = 3000
            });
        }
        catch (Exception ex)
        {
            NotifyError(UiErrorMapper.GetMessage(ex, Loc));
            await LoadGroupPermissionsAsync(component.GroupId, false);
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
            groupPermissions = await _apiServices.GetFromApiAsync<
                List<PermissionPageComponentResDTO>>(
                    $"/api/Permission/groups/{groupId}/page-components")
                ?? [];
            selectedTab = 0;
        }
        catch (Exception ex)
        {
            groupPermissions = [];
            if (notifyErrors)
            {
                NotifyError(UiErrorMapper.GetMessage(ex, Loc));
            }
        }
        finally
        {
            IsLoading_Child = false;
            glb.isBusyPage = false;
            StateHasChanged();
        }
    }

    protected bool CanToggleComponent(
        PermissionComponentAccessResDTO component) =>
        PermissionState.HasPermission(Permissions.PermissionManage)
        && !Permissions.IsActionCode(component.ComponentCode)
        && !component.IsDeleted;

    protected string GetPermissionKind(
        PermissionComponentAccessResDTO component) =>
        Permissions.IsActionCode(component.ComponentCode)
            ? Loc["PermissionKindFixedApi"].Value
            : Loc["PermissionKindUiDisplay"].Value;

    protected async Task OnGroupRowDoubleClick(DataGridRowMouseEventArgs<PermissionGroupDto> args)
    {
        if (args.Data is null)
        {
            return;
        }

        await DialogService.OpenSideAsync<Component_RecordInspector<PermissionGroupDto>>(
            Loc["RoleInspectorTitle", GetGroupDisplayName(args.Data)].Value,
            new Dictionary<string, object?> { { "Record", args.Data } },
            options: new SideDialogOptions { Position = DialogPosition.Right, Width = "500px" });
    }

    protected async Task OnComponentRowDoubleClick(
        DataGridRowMouseEventArgs<PermissionComponentAccessResDTO> args)
    {
        if (args.Data is null)
        {
            return;
        }

        await DialogService.OpenSideAsync<
            Component_RecordInspector<PermissionComponentAccessResDTO>>(
                Loc["ComponentInspectorTitle", args.Data.ComponentName ?? string.Empty].Value,
                new Dictionary<string, object?> { { "Record", args.Data } },
                options: new SideDialogOptions { Position = DialogPosition.Right, Width = "500px" });
    }

    private void NotifyError(string detail) => Toast.Notify(new NotificationMessage
    {
        Severity = NotificationSeverity.Error,
        Summary = Loc["Error"].Value,
        Detail = detail,
        Duration = 10000
    });

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

    private bool IsDevPersona(RbacPersonaDefinition persona) =>
        string.Equals(persona.GroupCode, CanonicalRbac.Dev.GroupCode, StringComparison.OrdinalIgnoreCase);

    private string GetRoleDisplayName(string? groupCode, string? fallback = null) => groupCode switch
    {
        "EMPLOYEE" => Loc["RoleEmployee"].Value,
        "MANAGER" => Loc["RoleManager"].Value,
        "DEV" => Loc["RoleDev"].Value,
        _ => fallback ?? groupCode ?? "—"
    };

    private string GetActionDisplayName(string permissionCode) => permissionCode switch
    {
        Permissions.RequestViewOwn => Loc["PermissionActionRequestViewOwn"].Value,
        Permissions.RequestViewDepartment => Loc["PermissionActionRequestViewDepartment"].Value,
        Permissions.RequestViewAll => Loc["PermissionActionRequestViewAll"].Value,
        Permissions.RequestCreate => Loc["PermissionActionRequestCreate"].Value,
        Permissions.RequestUpdateOwn => Loc["PermissionActionRequestUpdateOwn"].Value,
        Permissions.RequestCancelOwn => Loc["PermissionActionRequestCancelOwn"].Value,
        Permissions.RequestApprove => Loc["PermissionActionRequestApprove"].Value,
        Permissions.RequestReject => Loc["PermissionActionRequestReject"].Value,
        Permissions.RequestCatalogView => Loc["PermissionActionRequestCatalogView"].Value,
        Permissions.LibraryView => Loc["PermissionActionLibraryView"].Value,
        Permissions.LibraryManage => Loc["PermissionActionLibraryManage"].Value,
        Permissions.PermissionView => Loc["PermissionActionPermissionView"].Value,
        Permissions.PermissionManage => Loc["PermissionActionPermissionManage"].Value,
        Permissions.ReportViewOwn => Loc["PermissionActionReportViewOwn"].Value,
        Permissions.ReportViewDepartment => Loc["PermissionActionReportViewDepartment"].Value,
        Permissions.ReportViewAll => Loc["PermissionActionReportViewAll"].Value,
        Permissions.ReportExport => Loc["PermissionActionReportExport"].Value,
        Permissions.PeriodSettle => Loc["PermissionActionPeriodSettle"].Value,
        _ => permissionCode
    };
}
