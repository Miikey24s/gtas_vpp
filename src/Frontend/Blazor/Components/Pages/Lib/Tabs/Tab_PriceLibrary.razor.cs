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
        [Inject] public IToastService _toastService { get; set; } = default!;
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private List<PriceListResDTO> priceLists = [];
        private List<SupplierResDTO> suppliers = [];
        private List<VppItemPriceResDTO> displayItems = [];
        private List<string> categories = [];
        private RadzenDataGrid<VppItemPriceResDTO> grid = default!;
        private Guid? selectedPriceListId;
        private Guid? selectedSupplierId;
        private string searchText = "";
        private string selectedCategory = "";
        private string selectedMappingStatus = "";
        private string? loadError;
        private bool interactiveLookupsRefreshed;
        private bool isLoading;
        private int priceCount;
        private int currentSkip;
        private bool HasPriceLists => priceLists.Count > 0;
        private bool HasPriceListSelected => selectedPriceListId.HasValue;
        private bool HasPriceContext => selectedPriceListId.HasValue && selectedSupplierId.HasValue;
        private bool CanModify => PagePermissionResDTO.Components.Any(component => component.IsVisible && component.IsEnable);
        private bool CanImportPrices => CanModify && IsSelectedPriceListActive && selectedPriceListId.HasValue;
        private bool HasPriceFilters => !string.IsNullOrWhiteSpace(searchText)
            || !string.IsNullOrWhiteSpace(selectedCategory)
            || !string.IsNullOrWhiteSpace(selectedMappingStatus);
        private PriceListResDTO? SelectedPriceList => priceLists.FirstOrDefault(x => x.Id == selectedPriceListId);
        private bool IsSelectedPriceListEditable
            => SelectedPriceList is { IsDeleted: false } row && row.Status != "Expired";
        private IReadOnlyList<VppFilterOption<Guid?>> PriceListFilterOptions
            => priceLists.Select(row => new VppFilterOption<Guid?>(row.Id, FormatPriceListOption(row))).ToList();
        private IReadOnlyList<VppFilterOption<string>> CategoryFilterOptions =>
            [new(string.Empty, Loc["AllCategories"].Value), .. categories.Select(category => new VppFilterOption<string>(category, category))];
        private IReadOnlyList<VppFilterOption<string>> MappingStatusOptions =>
        [
            new(string.Empty, Loc["AllPriceMappings"].Value),
            new("active", Loc["PriceMappingActive"].Value),
            new("missing", Loc["PriceMappingMissing"].Value),
            new("inactive", Loc["PriceMappingInactive"].Value)
        ];
        private string SelectedSupplierName => SelectedPriceList?.SupplierName
            ?? suppliers.FirstOrDefault(row => row.Id == selectedSupplierId)?.SupplierName
            ?? suppliers.FirstOrDefault(row => row.Id == selectedSupplierId)?.SupplierShortName
            ?? "–";
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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // OnAfterRender không chạy trong prerender. Vì vậy đây là một lần refresh
            // có chủ đích sau khi circuit đã nhận auth state, kể cả prerender từng trả
            // danh sách rỗng mà không ném lỗi.
            if (firstRender && !interactiveLookupsRefreshed)
            {
                interactiveLookupsRefreshed = true;
                await LoadLookupsAsync();
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            var newPriceListId = ResolveSelectedPriceListId();
            if (newPriceListId != selectedPriceListId)
            {
                selectedPriceListId = newPriceListId;
                selectedSupplierId = priceLists.FirstOrDefault(x => x.Id == selectedPriceListId)?.SupplierId;
                _ = InvokeAsync(async () =>
                {
                    await LoadCategoryOptionsAsync();
                    await LoadPricesAsync();
                    StateHasChanged();
                });
            }
        }

        public void Dispose()
        {
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

                await LoadCategoryOptionsAsync();
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
                    selectedMappingStatus,
                    args.OrderBy));

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
                StateHasChanged();
            }
        }

        private async Task OnSearchInputAsync(ChangeEventArgs args)
        {
            searchText = args.Value?.ToString() ?? "";
            if (grid is not null)
            {
                await grid.FirstPage(true);
            }
        }

        private async Task OnCategoryChangedAsync(string value)
        {
            selectedCategory = value;
            if (grid is not null) await grid.FirstPage(true);
        }

        private async Task OnMappingStatusChangedAsync(string value)
        {
            selectedMappingStatus = value;
            if (grid is not null) await grid.FirstPage(true);
        }

        private async Task ClearFiltersAsync()
        {
            searchText = string.Empty;
            selectedCategory = string.Empty;
            selectedMappingStatus = string.Empty;
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
                        SupplierSku = result.SupplierSku,
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
                    SupplierSku = row.SupplierSku,
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
                "set-default",
                Loc["SetDefault"].Value,
                "star",
                () => SetDefaultAsync(row),
                !row.PriceMappingId.HasValue || row.IsDefault || row.IsDeleted || !IsSelectedPriceListEditable),
            new(
                "toggle-active",
                row.IsDeleted ? Loc["Restore"].Value : Loc["Deactivate"].Value,
                row.IsDeleted ? "restore_from_trash" : "block",
                () => SetDeletedPriceAsync(row, !row.IsDeleted),
                !row.PriceMappingId.HasValue || !IsSelectedPriceListEditable),
            new(
                "hard-delete",
                Loc["HardDelete"].Value,
                "delete_forever",
                () => HardDeletePriceAsync(row),
                !row.PriceMappingId.HasValue || !row.IsDeleted || !IsSelectedPriceListEditable,
                VppAdminActionTone.Danger)
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

        private async Task SetDefaultAsync(VppItemPriceResDTO row)
        {
            if (!row.PriceMappingId.HasValue) return;

            try
            {
                await PricingApi.SetDefaultItemPriceAsync(row.PriceMappingId.Value);
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["DefaultUpdated"].Value);
                await LoadPricesAsync();
            }
            catch (Exception ex)
            {
                NotifyPriceError(ex);
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
            selectedPriceListId = value;
            selectedSupplierId = priceLists.FirstOrDefault(x => x.Id == selectedPriceListId)?.SupplierId;
            selectedCategory = string.Empty;
            selectedMappingStatus = string.Empty;
            await LoadCategoryOptionsAsync();
            await LoadPricesAsync();
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

        private async Task LoadCategoryOptionsAsync()
        {
            categories = [];
            if (!selectedSupplierId.HasValue || !selectedPriceListId.HasValue)
            {
                return;
            }

            categories = (await PricingApi.GetItemPriceCategoriesAsync(
                selectedSupplierId.Value,
                selectedPriceListId.Value)).ToList();
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

        private async Task ImportPricesAsync(MouseEventArgs _)
        {
            if (!CanImportPrices || SelectedPriceList is null || !selectedPriceListId.HasValue)
            {
                return;
            }

            var imported = await DialogService.OpenAsync<Dialog_PriceListImport>(
                Loc["ImportPriceList"].Value,
                new Dictionary<string, object?>
                {
                    [nameof(Dialog_PriceListImport.PriceListId)] = selectedPriceListId.Value,
                    [nameof(Dialog_PriceListImport.PriceListName)] = SelectedPriceList.PriceListName ?? SelectedPriceList.PriceListCode ?? "–",
                    [nameof(Dialog_PriceListImport.SupplierName)] = SelectedSupplierName
                },
                VppAdminDialogProfiles.Create(
                    VppAdminDialogSize.Workspace,
                    Loc["ImportPriceList"].Value,
                    closeAriaLabel: Loc["Close"].Value));

            if (imported is true)
            {
                await LoadCategoryOptionsAsync();
                await LoadPricesAsync();
            }
        }
    }
}
