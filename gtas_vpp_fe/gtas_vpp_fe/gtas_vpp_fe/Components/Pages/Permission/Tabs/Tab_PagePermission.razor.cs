using gtas_vpp_fe.Components.Pages.Lib;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs;

public partial class Tab_PagePermission
{
    [Inject] public IToastService _toastService { get; set; } = default!;
    [Inject] public IAPIServices _apiServices { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;

    [Parameter] public IEnumerable<Claim> claims { get; set; } = [];
    [Parameter]
    public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();

    public List<P02_GroupResDTO> list_Group { get; set; } = [];
    public RadzenDataGrid<P02_GroupResDTO> grid { get; set; } = default!;
    public IList<P02_GroupResDTO> selected_Group { get; set; } = [];
    public List<sp_Authen_Permission_GetPageWithComponentByGroupId> list_PermissionOfGroup { get; set; } = [];
    public IList<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component> selected_Component { get; set; } = [];
    public RadzenDataGrid<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component> child_grid { get; set; } = default!;

    public int selectedTab { get; set; }
    public bool IsLoading { get; set; }
    public bool IsLoading_Child { get; set; }

    private int groupCount;
    private int currentGroupSkip;
    private string? currentGroupFilterExpression;
    private bool hasRequestedInitialGroupGridLoad;

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
        list_PermissionOfGroup = [];
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
            var result = await _apiServices.GetFromApiWithTotalCountAsync<List<P02_GroupResDTO>>(
                BuildGroupsEndpoint(args.Filter, args.Skip, args.Top, args.OrderBy));

            list_Group = result.Data ?? [];
            groupCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            list_Group = [];
            groupCount = 0;
            NotifyError("Error when loading roles: " + ex.Message);
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
            NotifyError("Error when loading role filters: " + ex.Message);
        }
    }

    protected Task GroupRowExpand(P02_GroupResDTO group) => LoadGroupPermissionsAsync(group.Id, true);

    protected async Task SetComponentVisibilityAsync(
        sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component component)
    {
        if (!component.IsVisible)
        {
            component.IsEnable = false;
        }

        await PersistComponentAsync(component);
    }

    protected async Task SetComponentEnabledAsync(
        sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component component)
    {
        if (component.IsEnable)
        {
            component.IsVisible = true;
        }

        await PersistComponentAsync(component);
    }

    private async Task PersistComponentAsync(
        sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component component)
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
                P05_PageComponentMappingId = component.GroupPageComponentMappingId,
                P02_GroupId = component.GroupId,
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
                Summary = "Permission updated",
                Detail = $"Component '{component.ComponentName}' updated successfully",
                Duration = 3000
            });
        }
        catch (Exception ex)
        {
            NotifyError("Error when updating component permission: " + ex.Message);
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
            list_PermissionOfGroup = await _apiServices.GetFromApiAsync<
                List<sp_Authen_Permission_GetPageWithComponentByGroupId>>(
                    $"/api/Permission/groups/{groupId}/page-components")
                ?? [];
            selectedTab = 0;
        }
        catch (Exception ex)
        {
            list_PermissionOfGroup = [];
            if (notifyErrors)
            {
                NotifyError("Error when loading role permissions: " + ex.Message);
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
        sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component component) =>
        PermissionState.HasPermission(Permissions.PermissionManage)
        && !Permissions.IsActionCode(component.ComponentCode)
        && !component.IsDeleted;

    protected static string GetPermissionKind(
        sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component component) =>
        Permissions.IsActionCode(component.ComponentCode)
            ? "API cố định"
            : "Hiển thị UI";

    protected async Task OnGroupRowDoubleClick(DataGridRowMouseEventArgs<P02_GroupResDTO> args)
    {
        if (args.Data is null)
        {
            return;
        }

        await DialogService.OpenSideAsync<Component_RecordInspector<P02_GroupResDTO>>(
            $"Role: {args.Data.GroupName}",
            new Dictionary<string, object?> { { "Record", args.Data } },
            options: new SideDialogOptions { Position = DialogPosition.Right, Width = "500px" });
    }

    protected async Task OnComponentRowDoubleClick(
        DataGridRowMouseEventArgs<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component> args)
    {
        if (args.Data is null)
        {
            return;
        }

        await DialogService.OpenSideAsync<
            Component_RecordInspector<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component>>(
                $"Component: {args.Data.ComponentName}",
                new Dictionary<string, object?> { { "Record", args.Data } },
                options: new SideDialogOptions { Position = DialogPosition.Right, Width = "500px" });
    }

    private void NotifyError(string detail) => Toast.Notify(new NotificationMessage
    {
        Severity = NotificationSeverity.Error,
        Summary = "Error",
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
}
