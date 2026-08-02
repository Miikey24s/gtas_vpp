using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs;

public partial class Tab_DepartmentLibrary : VppServerGridComponentBase<DepartmentResDTO>
{
    [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();

    [Inject] public CatalogApiClient CatalogApi { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public CurrentUserState CurrentUserState { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;

    private List<DepartmentResDTO> rows = [];
    private List<DepartmentResDTO> allDepartments = [];
    private RadzenDataGrid<DepartmentResDTO> grid = default!;
    private int totalCount;
    private int currentSkip;
    private bool isLoading;
    private string searchText = string.Empty;

    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText);

    private bool CanModify => PagePermissionResDTO.Components.Any(
        component => component.IsVisible && component.IsEnable);

    protected override RadzenDataGrid<DepartmentResDTO>? InitialGrid => grid;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            allDepartments = await CatalogApi.GetActiveDepartmentsAsync() ?? [];
        }
        catch (Exception ex)
        {
            ToastService.Error(ex, Loc, "LoadLibraryDataFailed");
        }
    }

    private async Task LoadDataAsync(LoadDataArgs args)
    {
        isLoading = true;
        currentSkip = args.Skip ?? 0;
        try
        {
            var result = await CatalogApi.GetDepartmentsAsync(new CatalogQuery(
                args.Skip ?? 0,
                args.Top ?? VppPagingProfiles.Collection.DefaultPageSize,
                searchText,
                args.OrderBy));
            rows = result.Items.ToList();
            totalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            rows = [];
            totalCount = 0;
            ToastService.Error(ex, Loc, "LoadLibraryDataFailed");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task OpenCreateAsync()
    {
        var result = await DialogService.OpenAsync<Dialog.Dialog_DepartmentEditor>(
            Loc["AddDepartment"].Value,
            new Dictionary<string, object?>
            {
                ["IsCreate"] = true,
                ["Departments"] = allDepartments
            },
            VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Standard,
                Loc["AddDepartment"].Value,
                closeAriaLabel: Loc["Close"].Value));

        if (result is DepartmentResDTO)
        {
            await grid.Reload();
        }
    }

    private async Task OpenEditAsync(DepartmentResDTO row)
    {
        var result = await DialogService.OpenAsync<Dialog.Dialog_DepartmentEditor>(
            Loc["Edit"].Value,
            new Dictionary<string, object?>
            {
                ["IsCreate"] = false,
                ["Model"] = Clone(row),
                ["Departments"] = allDepartments
            },
            VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Standard,
                Loc["Edit"].Value,
                closeAriaLabel: Loc["Close"].Value));

        if (result is DepartmentResDTO)
        {
            await grid.Reload();
        }
    }

    private async Task ToggleDeletedAsync(DepartmentResDTO row, bool value)
    {
        if (value && !await CanDeactivateAsync(row.Id))
        {
            return;
        }

        try
        {
            var result = await CatalogApi.SetDepartmentDeletedAsync(
                row.Id,
                new CatalogStatusChange(
                    value,
                    DateTime.Now,
                    CurrentUserState.Current?.UserId ?? 0));
            if (result is null)
            {
                row.IsDeleted = !value;
                ToastService.Show(
                    NotificationSeverity.Error,
                    Loc["Error"],
                    Loc["ChangeRecordStatusFailed"],
                    5000,
                    true);
                return;
            }

            row.IsDeleted = result.IsDeleted;
            await grid.Reload();
        }
        catch (Exception ex)
        {
            row.IsDeleted = !value;
            ToastService.Error(ex, Loc, "ChangeRecordStatusFailed");
        }
    }

    private async Task HardDeleteAsync(DepartmentResDTO row)
    {
        if (!CanModify || !row.IsDeleted)
        {
            return;
        }

        var confirm = await DialogService.Confirm(
            $"{row.Name ?? row.Code}\n\n{Loc["PermanentDeleteWarning"]}",
            Loc["HardDelete"].Value,
            new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
        if (confirm != true)
        {
            return;
        }

        try
        {
            await CatalogApi.DeleteDepartmentAsync(row.Id);
            ToastService.Show(
                NotificationSeverity.Success,
                Loc["Success"],
                Loc["RecordPermanentlyDeleted"],
                3000,
                false);
            await grid.Reload();
        }
        catch (Exception ex)
        {
            ToastService.Error(ex, Loc, "DeleteRecordFailed");
        }
    }

    private async Task<bool> CanDeactivateAsync(Guid id)
    {
        var impact = await CatalogApi.GetDepartmentDependencyImpactAsync(id);
        if (impact is null || impact.CanDeactivate)
        {
            return true;
        }

        ToastService.Show(
            NotificationSeverity.Warning,
            Loc["ValidationTitle"],
            string.Format(Loc["DependencyDeactivateBlocked"].Value, impact.ActiveReferenceCount),
            5000,
            false);
        return false;
    }

    private async Task OnSearchInputAsync(ChangeEventArgs args)
    {
        searchText = args.Value?.ToString() ?? string.Empty;
        await grid.FirstPage(true);
    }

    private async Task ClearFiltersAsync()
    {
        searchText = string.Empty;
        await grid.FirstPage(true);
    }

    private string GetParentName(Guid? id) => id.HasValue
        ? allDepartments.FirstOrDefault(department => department.Id == id)?.Name ?? "–"
        : "–";

    private static DepartmentResDTO Clone(DepartmentResDTO row) => new()
    {
        Id = row.Id,
        Code = row.Code,
        Name = row.Name,
        ParentDepartmentId = row.ParentDepartmentId,
        Description = row.Description,
        IsDeleted = row.IsDeleted,
        CreatedAtUtc = row.CreatedAtUtc,
        CreatedByUserId = row.CreatedByUserId,
        UpdatedAtUtc = row.UpdatedAtUtc,
        UpdatedByUserId = row.UpdatedByUserId
    };

    private static void OnRowRender(RowRenderEventArgs<DepartmentResDTO> args)
    {
        if (args.Data?.IsDeleted == true)
        {
            args.Attributes["class"] = args.Attributes.TryGetValue("class", out var current)
                ? $"{current} vpp-admin-row-deleted"
                : "vpp-admin-row-deleted";
        }
    }
}
