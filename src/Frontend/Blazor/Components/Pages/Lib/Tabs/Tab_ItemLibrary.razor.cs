// PAGE LOGIC: Lib/Tabs/Tab_ItemLibrary.razor.cs
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

public partial class Tab_ItemLibrary : VppServerGridComponentBase<VppItemResDTO>, IDisposable
{
    [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();

    [Inject] public CatalogApiClient CatalogApi { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;

    private List<VppItemResDTO> rows = [];
    private List<VppCategoryResDTO> categories = [];
    private List<LookupValueResDTO> uoms = [];
    private List<SupplierResDTO> suppliers = [];
    private RadzenDataGrid<VppItemResDTO> grid = default!;
    private int totalCount;
    private int currentSkip;
    private string searchText = string.Empty;
    private string categoryFilter = string.Empty;
    private string uomFilter = string.Empty;
    private string supplierFilter = string.Empty;
    private string selectedActivity = string.Empty;
    private CancellationTokenSource? searchDebounce;
    private bool isLoading;
    private IReadOnlyList<VppFilterOption<string>> categoryOptions = [];
    private IReadOnlyList<VppFilterOption<string>> uomOptions = [];
    private IReadOnlyList<VppFilterOption<string>> supplierOptions = [];

    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText)
        || !string.IsNullOrWhiteSpace(categoryFilter)
        || !string.IsNullOrWhiteSpace(uomFilter)
        || !string.IsNullOrWhiteSpace(supplierFilter)
        || !string.IsNullOrWhiteSpace(selectedActivity);

    private bool CanModify => PagePermissionResDTO.Components.Any(
        component => component.IsVisible && component.IsEnable);

    protected override RadzenDataGrid<VppItemResDTO>? InitialGrid => grid;

    private IReadOnlyList<VppFilterOption<string>> StatusOptions =>
    [
        new(string.Empty, Loc["LibraryAllStatuses"]),
        new("active", Loc["LibraryStatusActive"]),
        new("inactive", Loc["LibraryStatusInactive"])
    ];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var referenceData = await CatalogApi.GetItemReferenceDataAsync();
            categories = referenceData.Categories.ToList();
            uoms = referenceData.Uoms.ToList();
            suppliers = referenceData.Suppliers.ToList();
        }
        catch (Exception ex)
        {
            ToastService.Error(ex, Loc, "LoadLibraryDataFailed");
        }
        RefreshFilterOptions();
    }

    private void RefreshFilterOptions()
    {
        categoryOptions =
        [
            new(string.Empty, Loc["AllCategories"]),
            .. categories
                .Where(category => !category.IsDeleted)
                .Select(category => new VppFilterOption<string>(
                    category.Id.ToString(),
                    category.VppCategoryName ?? category.VppCategoryCode ?? string.Empty))
        ];
        uomOptions =
        [
            new(string.Empty, Loc["AllUnits"]),
            .. uoms
                .Where(uom => !uom.IsDeleted)
                .Select(uom => new VppFilterOption<string>(
                    uom.Id.ToString(),
                    uom.Value ?? uom.Code ?? string.Empty))
        ];
        supplierOptions =
        [
            new(string.Empty, Loc["AllSuppliers"]),
            .. suppliers
                .Where(supplier => !supplier.IsDeleted)
                .Select(supplier => supplier.SupplierName ?? supplier.SupplierShortName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(name => name)
                .Select(name => new VppFilterOption<string>(name!, name!))
        ];
    }

    private async Task LoadDataAsync(LoadDataArgs args)
    {
        isLoading = true;
        currentSkip = args.Skip ?? 0;
        try
        {
            var result = await CatalogApi.GetItemsAsync(new CatalogItemQuery(
                args.Skip ?? 0,
                args.Top ?? VppPagingProfiles.Collection.DefaultPageSize,
                searchText,
                Guid.TryParse(categoryFilter, out var categoryId) ? categoryId : null,
                Guid.TryParse(uomFilter, out var uomId) ? uomId : null,
                args.OrderBy,
                supplierFilter,
                selectedActivity));
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
        }
    }

    private async Task OpenCreateAsync()
    {
        var result = await DialogService.OpenAsync<Dialog.Dialog_ItemEditor>(
            Loc["AddItem"].Value,
            new Dictionary<string, object?>
            {
                ["IsCreate"] = true,
                ["Categories"] = categories,
                ["Uoms"] = uoms
            },
            VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Standard,
                Loc["AddItem"].Value,
                closeAriaLabel: Loc["Close"].Value));

        if (result is VppItemResDTO)
        {
            await grid.Reload();
        }
    }

    private async Task OpenEditAsync(VppItemResDTO row)
    {
        var result = await DialogService.OpenAsync<Dialog.Dialog_ItemEditor>(
            Loc["Edit"].Value,
            new Dictionary<string, object?>
            {
                ["IsCreate"] = false,
                ["Model"] = Clone(row),
                ["Categories"] = categories,
                ["Uoms"] = uoms
            },
            VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Standard,
                Loc["Edit"].Value,
                closeAriaLabel: Loc["Close"].Value));

        if (result is VppItemResDTO)
        {
            await grid.Reload();
        }
    }

    private async Task ToggleDeletedAsync(VppItemResDTO row, bool value)
    {
        try
        {
            var result = await CatalogApi.SetItemDeletedAsync(row.Id, value);
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

    private async Task HardDeleteAsync(VppItemResDTO row)
    {
        if (!CanModify || !row.IsDeleted)
        {
            return;
        }

        var confirm = await DialogService.Confirm(
            $"{row.VppName ?? row.VppCode}\n\n{Loc["PermanentDeleteWarning"]}",
            Loc["HardDelete"].Value,
            new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
        if (confirm != true)
        {
            return;
        }

        try
        {
            await CatalogApi.DeleteItemAsync(row.Id);
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

    private async Task OnSearchInputAsync(ChangeEventArgs args)
    {
        searchText = args.Value?.ToString() ?? string.Empty;
        CancelPendingSearch();
        var debounce = searchDebounce = new CancellationTokenSource();
        try
        {
            await Task.Delay(280, debounce.Token);
            await ReloadAsync();
        }
        catch (OperationCanceledException) when (debounce.IsCancellationRequested)
        {
        }
    }

    private async Task OnCategoryChangedAsync(string value)
    {
        CancelPendingSearch();
        categoryFilter = value;
        await ReloadAsync();
    }

    private async Task OnUomChangedAsync(string value)
    {
        CancelPendingSearch();
        uomFilter = value;
        await ReloadAsync();
    }

    private async Task OnSupplierChangedAsync(string value)
    {
        CancelPendingSearch();
        supplierFilter = value ?? string.Empty;
        await ReloadAsync();
    }

    private async Task OnStatusChangedAsync(string value)
    {
        CancelPendingSearch();
        selectedActivity = value ?? string.Empty;
        await ReloadAsync();
    }

    private async Task ClearFiltersAsync()
    {
        CancelPendingSearch();
        searchText = string.Empty;
        categoryFilter = string.Empty;
        uomFilter = string.Empty;
        supplierFilter = string.Empty;
        selectedActivity = string.Empty;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (grid is not null)
        {
            await grid.FirstPage(true);
        }
    }

    private void CancelPendingSearch()
    {
        searchDebounce?.Cancel();
        searchDebounce?.Dispose();
        searchDebounce = null;
    }

    public void Dispose()
    {
        CancelPendingSearch();
    }

    private static VppItemResDTO Clone(VppItemResDTO row) => new()
    {
        Id = row.Id,
        VppCode = row.VppCode,
        VppName = row.VppName,
        Description = row.Description,
        UomId = row.UomId,
        UomCode = row.UomCode,
        UomName = row.UomName,
        VppCategoryId = row.VppCategoryId,
        VppCategoryCode = row.VppCategoryCode,
        VppCategoryName = row.VppCategoryName,
        MaxQuantityPerOrder = row.MaxQuantityPerOrder,
        IsDeleted = row.IsDeleted,
        CreatedAtUtc = row.CreatedAtUtc,
        CreatedByUserId = row.CreatedByUserId,
        UpdatedAtUtc = row.UpdatedAtUtc,
        UpdatedByUserId = row.UpdatedByUserId
    };

    private static void OnRowRender(RowRenderEventArgs<VppItemResDTO> args)
    {
        if (args.Data?.IsDeleted == true)
        {
            args.Attributes["class"] = args.Attributes.TryGetValue("class", out var current)
                ? $"{current} vpp-admin-row-deleted"
                : "vpp-admin-row-deleted";
        }
    }
}
