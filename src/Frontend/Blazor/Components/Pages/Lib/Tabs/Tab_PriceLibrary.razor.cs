// PAGE LOGIC: Lib/Tabs/Tab_PriceLibrary.razor.cs
using gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs
{
    public partial class Tab_PriceLibrary : VppServerGridComponentBase<VppItemPriceResDTO>, IDisposable
    {
        [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
        [Inject] public PricingApiClient PricingApi { get; set; } = default!;
        [Inject] public PriceListImportApiClient PriceListFiles { get; set; } = default!;
        [Inject] public IToastService _toastService { get; set; } = default!;
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private List<PriceListResDTO> priceLists = [];
        private List<SupplierResDTO> suppliers = [];
        private List<VppItemPriceResDTO> displayItems = [];
        private List<string> categories = [];
        private List<string> units = [];
        private RadzenDataGrid<VppItemPriceResDTO> grid = default!;
        private IReadOnlyList<VppDecisionOption<Guid?>> SupplierDecisionOptions = [];
        private IReadOnlyList<VppDecisionOption<Guid?>> PriceListDecisionOptions = [];
        private IReadOnlyList<VppFilterOption<string>> CategoryFilterOptions = [];
        private IReadOnlyList<VppFilterOption<string>> UomFilterOptions = [];
        private Guid? selectedPriceListId;
        private Guid? selectedSupplierId;
        private string searchText = "";
        private string selectedCategory = "";
        private string selectedUom = "";
        private string? loadError;
        private CancellationTokenSource? searchDebounce;
        private bool isLoading;
        private bool isFileActionBusy;
        private VppFileExportFormat? exportingPriceListFormat;
        private int priceCount;
        private int currentSkip;
        private bool HasPriceLists => priceLists.Count > 0;
        private bool HasPriceListSelected => selectedPriceListId.HasValue;
        private bool HasPriceContext => selectedPriceListId.HasValue && selectedSupplierId.HasValue;
        private bool CanModify => PagePermissionResDTO.Components.Any(component => component.IsVisible && component.IsEnable);
        private bool CanImportPrices => CanModify && IsSelectedPriceListActive && selectedPriceListId.HasValue;
        private bool CanExportPrices => selectedPriceListId.HasValue;
        private static readonly IReadOnlyList<VppFileExportFormat> PriceExportFormats = [VppFileExportFormat.Excel];
        private bool HasPriceFilters => !string.IsNullOrWhiteSpace(searchText)
            || !string.IsNullOrWhiteSpace(selectedCategory)
            || !string.IsNullOrWhiteSpace(selectedUom);
        private VppDataSurfaceState PriceSurfaceState => !string.IsNullOrWhiteSpace(loadError)
            ? VppDataSurfaceState.Error
            : isLoading
                ? VppDataSurfaceState.Loading
                : priceCount == 0
                    ? HasPriceFilters ? VppDataSurfaceState.FilteredEmpty : VppDataSurfaceState.Empty
                    : VppDataSurfaceState.Populated;
        private PriceListResDTO? SelectedPriceList => priceLists.FirstOrDefault(x => x.Id == selectedPriceListId);
        private bool IsSelectedPriceListEditable
            => SelectedPriceList is { IsDeleted: false } row && row.Status != "Expired";
        private string SelectedSupplierName => SelectedPriceList?.SupplierName
            ?? suppliers.FirstOrDefault(row => row.Id == selectedSupplierId)?.SupplierName
            ?? suppliers.FirstOrDefault(row => row.Id == selectedSupplierId)?.SupplierShortName
            ?? "–";
        private string SelectedPriceListDisplayName => SelectedPriceList is null
            ? Loc["PriceList"].Value
            : FormatPriceListOption(SelectedPriceList);
        private bool IsSelectedPriceListActive
            => SelectedPriceList is { IsDeleted: false, Status: "Published" };
        private string SelectedPriceListStatusLabel
            => IsSelectedPriceListActive ? Loc["LibraryStatusActive"] : Loc["LibraryStatusInactive"];
        private VppStatusTone SelectedPriceListStatusTone
            => IsSelectedPriceListActive ? VppStatusTone.Success : VppStatusTone.Neutral;
        private string GridEmptyText => !HasPriceLists
            ? Loc["NoPriceListAvailable"].Value
            : HasPriceContext ? Loc["NoPricesFound"].Value : Loc["LoadPriceListPrompt"].Value;
        protected override RadzenDataGrid<VppItemPriceResDTO>? InitialGrid => grid;
        protected override bool CanRequestInitialGridLoad => selectedSupplierId.HasValue && selectedPriceListId.HasValue;

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
            await LoadLookupsAsync();
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            var newPriceListId = ResolveSelectedPriceListId();
            if (newPriceListId != selectedPriceListId)
            {
                CancelPendingSearch();
                selectedPriceListId = newPriceListId;
                selectedSupplierId = priceLists.FirstOrDefault(x => x.Id == selectedPriceListId)?.SupplierId;
                RefreshDecisionOptions();
                _ = InvokeAsync(async () =>
                {
                    await LoadFilterOptionsAsync();
                    await LoadPricesAsync();
                    StateHasChanged();
                });
            }
        }

        public void Dispose()
        {
            CancelPendingSearch();
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        private async Task LoadLookupsAsync()
        {
            try
            {
                var referenceData = await PricingApi.GetPricingReferenceDataAsync();
                priceLists = referenceData.PriceLists.ToList();
                suppliers = referenceData.Suppliers.ToList();

                selectedPriceListId = ResolveSelectedPriceListId();
                selectedSupplierId = priceLists.FirstOrDefault(x => x.Id == selectedPriceListId)?.SupplierId;
                RefreshDecisionOptions();

                await LoadFilterOptionsAsync();
                await LoadPricesAsync();
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "LoadLibraryDataFailed");
            }
        }

        private async Task LoadPricesAsync()
        {
            displayItems = [];
            priceCount = 0;
            loadError = null;

            if (!selectedSupplierId.HasValue || !selectedPriceListId.HasValue)
            {
                await ReloadGridAsync();
                return;
            }

            if (grid is not null)
            {
                await grid.FirstPage(true);
            }
        }

        private async Task LoadPriceRowsAsync(LoadDataArgs args)
        {
            if (!selectedSupplierId.HasValue || !selectedPriceListId.HasValue)
            {
                displayItems = [];
                priceCount = 0;
                return;
            }

            isLoading = true;
            currentSkip = args.Skip ?? 0;
            StateHasChanged();

            try
            {
                var result = await PricingApi.GetItemPricesAsync(new ItemPriceQuery(
                    selectedSupplierId.Value,
                    selectedPriceListId.Value,
                    args.Skip ?? 0,
                    args.Top ?? 20,
                    searchText,
                    selectedCategory,
                    string.Empty,
                    args.OrderBy,
                    selectedUom));

                displayItems = result.Items.ToList();
                priceCount = result.TotalCount;
                loadError = null;
            }
            catch (Exception ex)
            {
                displayItems = [];
                priceCount = 0;
                loadError = UiErrorMapper.GetMessage(ex, Loc, "LoadLibraryDataFailed");
                _toastService.Error(ex, Loc, "LoadLibraryDataFailed");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task OnSearchInputAsync(ChangeEventArgs args)
        {
            searchText = args.Value?.ToString() ?? "";
            CancelPendingSearch();
            var debounce = searchDebounce = new CancellationTokenSource();
            try
            {
                await Task.Delay(280, debounce.Token);
                if (grid is not null)
                {
                    await grid.FirstPage(true);
                }
            }
            catch (OperationCanceledException) when (debounce.IsCancellationRequested)
            {
            }
        }

        private async Task OnCategoryChangedAsync(string value)
        {
            CancelPendingSearch();
            selectedCategory = value;
            if (grid is not null) await grid.FirstPage(true);
        }

        private async Task OnUomChangedAsync(string value)
        {
            CancelPendingSearch();
            selectedUom = value;
            if (grid is not null) await grid.FirstPage(true);
        }

        private async Task ClearFiltersAsync()
        {
            CancelPendingSearch();
            searchText = string.Empty;
            selectedCategory = string.Empty;
            selectedUom = string.Empty;
            if (grid is not null) await grid.FirstPage(true);
        }

        private async Task EditPriceAsync(VppItemPriceResDTO row)
        {
            if (!selectedSupplierId.HasValue || !selectedPriceListId.HasValue) return;

            if (!row.PriceMappingId.HasValue)
            {
                var model = new SupplierProductPriceUpdateReqDTO
                {
                    VppItemId = row.VppId,
                    PriceListId = selectedPriceListId.Value,
                    SupplierId = selectedSupplierId.Value,
                    Price = 0,
                    IsDefault = false,
                    Description = "",
                    VatRate = 0,
                    MinimumOrderQuantity = 0,
                    LeadTimeDays = 0
                };
                var result = await OpenEditorAsync(Loc["AddNewPrice"].Value, model);
                if (result is null) return;

                try
                {
                    var req = new SupplierProductPriceCreateReqDTO
                    {
                        VppItemId = row.VppId,
                        SupplierId = result.SupplierId,
                        PriceListId = selectedPriceListId.Value,
                        Price = result.Price,
                        NetPrice = result.NetPrice,
                        VatRate = result.VatRate,
                        MinimumOrderQuantity = result.MinimumOrderQuantity,
                        LeadTimeDays = result.LeadTimeDays,
                        IsDefault = result.IsDefault,
                        Description = result.Description
                    };
                    await PricingApi.CreateItemPriceAsync(req);
                    Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceSaved"].Value);
                    await LoadPricesAsync();
                }
                catch (Exception ex)
                {
                    NotifyPriceError(ex);
                }
            }
            else
            {
                var model = new SupplierProductPriceUpdateReqDTO
                {
                    Id = row.PriceMappingId.Value,
                    VppItemId = row.VppId,
                    SupplierId = selectedSupplierId.Value,
                    PriceListId = selectedPriceListId.Value,
                    Price = row.Price ?? 0,
                    NetPrice = row.NetPrice,
                    VatRate = row.VatRate,
                    MinimumOrderQuantity = row.MinimumOrderQuantity,
                    LeadTimeDays = row.LeadTimeDays,
                    IsDefault = row.IsDefault,
                    Description = row.Description
                };
                var result = await OpenEditorAsync(Loc["EditPrice"].Value, model);
                if (result is null) return;

                try
                {
                    await PricingApi.UpdateItemPriceAsync(row.PriceMappingId.Value, result);
                    Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceSaved"].Value);
                    await LoadPricesAsync();
                }
                catch (Exception ex)
                {
                    NotifyPriceError(ex);
                }
            }
        }

        private string GetEditPriceActionTitle(VppItemPriceResDTO row)
        {
            return row.PriceMappingId.HasValue
                ? Loc["Edit"].Value
                : Loc["Create"].Value;
        }

        private IReadOnlyList<VppAdminActionMenuItem> PriceRowSecondaryActions(VppItemPriceResDTO row) =>
        [
            new(
                "toggle-active",
                row.IsDeleted ? Loc["Restore"].Value : Loc["Deactivate"].Value,
                 row.IsDeleted ? "restore_from_trash" : "block",
                 () => SetDeletedPriceAsync(row, !row.IsDeleted),
                 !row.PriceMappingId.HasValue || !IsSelectedPriceListEditable,
                 DisabledReason: Loc["RequestActionUnavailable"].Value),
            new(
                "hard-delete",
                Loc["HardDelete"].Value,
                "delete_forever",
                 () => HardDeletePriceAsync(row),
                 !row.PriceMappingId.HasValue || !row.IsDeleted || !IsSelectedPriceListEditable,
                 VppAdminActionTone.Danger,
                 Loc["RequestActionUnavailable"].Value)
         ];

        private async Task SetDeletedPriceAsync(VppItemPriceResDTO row, bool isDeleted)
        {
            if (!row.PriceMappingId.HasValue) return;

            var previous = row.IsDeleted;
            row.IsDeleted = isDeleted;

            try
            {
                var result = await PricingApi.SetItemPriceDeletedAsync(
                    row.PriceMappingId.Value,
                    isDeleted);

                if (result is null)
                {
                    row.IsDeleted = previous;
                    Notify(NotificationSeverity.Error, Loc["Error"].Value, Loc["DeleteFailed"].Value);
                    return;
                }

                Notify(NotificationSeverity.Success, Loc["Success"].Value, isDeleted ? Loc["PriceMappingDeactivated"].Value : Loc["PriceMappingRestored"].Value);
                await LoadPricesAsync();
            }
            catch (Exception ex)
            {
                row.IsDeleted = previous;
                _toastService.Error(ex, Loc, "ChangeRecordStatusFailed");
            }
        }

        private async Task HardDeletePriceAsync(VppItemPriceResDTO row)
        {
            if (!row.PriceMappingId.HasValue) return;

            var confirm = await DialogService.Confirm(
                Loc["PriceMappingHardDeleteConfirm"].Value,
                Loc["HardDelete"].Value,
                new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });

            if (confirm != true) return;

            try
            {
                var deleted = await PricingApi.DeleteItemPriceAsync(row.PriceMappingId.Value);
                if (deleted)
                {
                    Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceDeleted"].Value);
                    await LoadPricesAsync();
                }
                else
                {
                    Notify(NotificationSeverity.Error, Loc["Error"].Value, Loc["DeleteFailed"].Value);
                }
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "DeleteRecordFailed");
            }
        }

        private void OnRowRenderPrice(RowRenderEventArgs<VppItemPriceResDTO> args)
        {
            if (args.Data?.IsDeleted == true)
            {
                AppendRowClass(args.Attributes, "vpp-admin-row-deleted");
            }
        }

        private async Task<SupplierProductPriceUpdateReqDTO?> OpenEditorAsync(string title, SupplierProductPriceUpdateReqDTO model)
        {
            var result = await DialogService.OpenAsync<Dialog_PriceEditor>(
                title,
                new Dictionary<string, object?>
                {
                    [nameof(Dialog_PriceEditor.Model)] = model,
                    [nameof(Dialog_PriceEditor.Suppliers)] = suppliers
                },
                VppAdminDialogProfiles.Create(VppAdminDialogSize.Standard, title, closeAriaLabel: Loc["Close"].Value));

            return result as SupplierProductPriceUpdateReqDTO;
        }

        private async Task OnPriceListChangedAsync(Guid? value)
        {
            CancelPendingSearch();
            selectedPriceListId = value;
            selectedSupplierId = priceLists.FirstOrDefault(x => x.Id == selectedPriceListId)?.SupplierId;
            RefreshDecisionOptions();
            selectedCategory = string.Empty;
            selectedUom = string.Empty;
            await LoadFilterOptionsAsync();
            await LoadPricesAsync();
        }

        private async Task OnSupplierChangedAsync(Guid? value)
        {
            CancelPendingSearch();
            selectedSupplierId = value;
            var supplierPriceLists = priceLists
                .Where(row => row.SupplierId == selectedSupplierId)
                .ToArray();
            if (!supplierPriceLists.Any(row => row.Id == selectedPriceListId))
            {
                selectedPriceListId = supplierPriceLists.FirstOrDefault(row => row.IsDefault)?.Id
                    ?? supplierPriceLists.FirstOrDefault()?.Id;
            }

            RefreshDecisionOptions();
            selectedCategory = string.Empty;
            selectedUom = string.Empty;
            await LoadFilterOptionsAsync();
            await LoadPricesAsync();
        }

        private void CancelPendingSearch()
        {
            searchDebounce?.Cancel();
            searchDebounce?.Dispose();
            searchDebounce = null;
        }

        private Guid? ResolveSelectedPriceListId()
        {
            var query = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
            if (query.TryGetValue("priceListId", out var values)
                && Guid.TryParse(values.FirstOrDefault(), out var requestedId)
                && priceLists.Any(x => x.Id == requestedId))
            {
                return requestedId;
            }

            return priceLists.FirstOrDefault(x => x.IsDefault)?.Id
                ?? priceLists.FirstOrDefault()?.Id;
        }

        private async Task LoadFilterOptionsAsync()
        {
            categories = [];
            units = [];
            RefreshDistinctFilterOptions();
            if (!selectedSupplierId.HasValue || !selectedPriceListId.HasValue)
            {
                return;
            }

            var categoriesTask = PricingApi.GetItemPriceCategoriesAsync(
                selectedSupplierId.Value,
                selectedPriceListId.Value);
            var unitsTask = PricingApi.GetItemPriceUnitsAsync(
                selectedSupplierId.Value,
                selectedPriceListId.Value);
            await Task.WhenAll(categoriesTask, unitsTask);
            categories = (await categoriesTask).ToList();
            units = (await unitsTask).ToList();
            RefreshDistinctFilterOptions();
        }

        private void RefreshDistinctFilterOptions()
        {
            CategoryFilterOptions =
            [
                new(string.Empty, Loc["AllCategories"].Value),
                .. categories.Select(category => new VppFilterOption<string>(category, category))
            ];
            UomFilterOptions =
            [
                new(string.Empty, Loc["AllUnits"].Value),
                .. units.Select(unit => new VppFilterOption<string>(unit, unit))
            ];
        }

        private async Task ReloadGridAsync()
        {
            if (grid is not null)
            {
                await grid.Reload();
            }
            StateHasChanged();
        }

        private void NotifyPriceError(Exception ex)
        {
            var detail = UiErrorMapper.GetErrorCode(ex) == "Conflict"
                ? Loc["OnlyOneDefaultPerVPPAllowed"].Value
                : UiErrorMapper.GetMessage(ex, Loc, "UpdateRecordFailed");
            Notify(NotificationSeverity.Error, Loc["Error"].Value, detail);
        }

        private void Notify(NotificationSeverity severity, string summary, string detail)
        {
            _toastService.Show(severity, summary, detail, 5000, false);
        }

        private static void AppendRowClass(IDictionary<string, object> attributes, string className)
        {
            if (attributes.TryGetValue("class", out var current) && current is not null)
            {
                attributes["class"] = $"{current} {className}";
                return;
            }

            attributes["class"] = className;
        }

        private static string FormatPriceListOption(PriceListResDTO row)
        {
            return row.PriceListName ?? row.PriceListCode ?? "–";
        }

        private void RefreshDecisionOptions()
        {
            SupplierDecisionOptions = priceLists
                .Where(row => row.SupplierId.HasValue)
                .GroupBy(row => row.SupplierId!.Value)
                .Select(group => new VppDecisionOption<Guid?>(
                    group.Key,
                    suppliers.FirstOrDefault(row => row.Id == group.Key)?.SupplierName
                        ?? suppliers.FirstOrDefault(row => row.Id == group.Key)?.SupplierShortName
                        ?? group.First().SupplierName
                        ?? "–"))
                .OrderBy(option => option.Label)
                .ToArray();

            PriceListDecisionOptions = priceLists
                .Where(row => row.SupplierId == selectedSupplierId)
                .OrderByDescending(row => row.IsDefault)
                .ThenBy(row => row.PriceListName ?? row.PriceListCode)
                .Select(row => new VppDecisionOption<Guid?>(row.Id, FormatPriceListOption(row)))
                .ToArray();
        }

        private async Task ImportPricesAsync(MouseEventArgs _)
        {
            if (!CanImportPrices || isFileActionBusy || SelectedPriceList is null || !selectedPriceListId.HasValue)
            {
                return;
            }

            isFileActionBusy = true;
            try
            {
                var imported = await DialogService.OpenAsync<Dialog_PriceListImport>(
                    Loc["UpdatePricesFromFile"].Value,
                    new Dictionary<string, object?>
                    {
                        [nameof(Dialog_PriceListImport.PriceListId)] = selectedPriceListId.Value,
                        [nameof(Dialog_PriceListImport.PriceListName)] = SelectedPriceList.PriceListName ?? SelectedPriceList.PriceListCode ?? "–",
                        [nameof(Dialog_PriceListImport.SupplierName)] = SelectedSupplierName
                    },
                    VppAdminDialogProfiles.Create(
                        VppAdminDialogSize.Workspace,
                        Loc["UpdatePricesFromFile"].Value,
                        closeAriaLabel: Loc["Close"].Value));

                if (imported is true)
                {
                    await LoadLookupsAsync();
                }
            }
            finally
            {
                isFileActionBusy = false;
            }
        }

        private async Task ExportPricesAsync(VppFileExportFormat format)
        {
            if (format != VppFileExportFormat.Excel
                || !CanExportPrices
                || isFileActionBusy
                || !selectedPriceListId.HasValue)
            {
                return;
            }

            isFileActionBusy = true;
            exportingPriceListFormat = format;
            try
            {
                await PriceListFiles.ExportExcelAsync(selectedPriceListId.Value);
            }
            catch (Exception ex)
            {
                _toastService.Error(Loc["ExportExcel"], UiErrorMapper.GetMessage(ex, Loc));
            }
            finally
            {
                exportingPriceListFormat = null;
                isFileActionBusy = false;
            }
        }
    }
}
