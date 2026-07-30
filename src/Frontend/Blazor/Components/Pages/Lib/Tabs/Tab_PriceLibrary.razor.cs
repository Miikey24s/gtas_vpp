using gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs
{
    public partial class Tab_PriceLibrary : VppServerGridComponentBase<VppItemPriceResDTO>, IDisposable
    {
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public IToastService _toastService { get; set; } = default!;
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private List<PriceListResDTO> priceLists = [];
        private List<SupplierResDTO> suppliers = [];
        private List<VppItemPriceResDTO> displayItems = [];
        private RadzenDataGrid<VppItemPriceResDTO> grid = default!;
        private Guid? selectedPriceListId;
        private Guid? selectedSupplierId;
        private string searchText = "";
        private bool isLoading;
        private int priceCount;
        private int currentSkip;
        private string? currentFilterExpression;
        private bool HasPriceLists => priceLists.Count > 0;
        private bool HasPriceListSelected => selectedPriceListId.HasValue;
        private bool IsSelectedPriceListDraft
            => priceLists.FirstOrDefault(x => x.Id == selectedPriceListId)?.Status == "Draft";
        private IReadOnlyList<VppFilterOption<Guid?>> PriceListFilterOptions
            => priceLists.Select(row => new VppFilterOption<Guid?>(row.Id, row.PriceListName ?? row.PriceListCode ?? "–")).ToList();
        private IReadOnlyList<VppFilterOption<Guid?>> SupplierFilterOptions
            => suppliers.Select(row => new VppFilterOption<Guid?>(row.Id, row.SupplierName ?? row.SupplierShortName ?? "–")).ToList();
        private string GridEmptyText => !HasPriceLists
            ? Loc["NoPriceListAvailable"].Value
            : selectedSupplierId.HasValue ? Loc["NoPricesFound"].Value : Loc["LoadPricesPrompt"].Value;
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
                selectedPriceListId = newPriceListId;
                _ = InvokeAsync(async () =>
                {
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
                var priceListsTask = _apiServices.GetFromApiAsync<List<PriceListResDTO>>($"{Config.LibraryApi.PriceList}?showDeleted=true");
                var suppliersTask = _apiServices.GetFromApiAsync<List<SupplierResDTO>>($"{Config.LibraryApi.Suppliers}?showDeleted=true");

                await Task.WhenAll(priceListsTask, suppliersTask);

                priceLists = await priceListsTask ?? [];
                suppliers = await suppliersTask ?? [];

                selectedPriceListId = ResolveSelectedPriceListId();
                selectedSupplierId = priceLists.FirstOrDefault(x => x.Id == selectedPriceListId)?.SupplierId
                                     ?? suppliers.FirstOrDefault(s => s.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName)?.Id
                                     ?? suppliers.FirstOrDefault()?.Id;

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
            currentFilterExpression = args.Filter;
            StateHasChanged();

            try
            {
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<VppItemPriceResDTO>>(
                    BuildPriceRowsEndpoint(args.Filter, args.Skip ?? 0, args.Top ?? 20, args.OrderBy));

                displayItems = result.Data ?? [];
                priceCount = result.TotalCount;
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "LoadLibraryDataFailed");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadPriceFilterDataAsync(DataGridLoadColumnFilterDataEventArgs<VppItemPriceResDTO> args)
        {
            if (args.Column is null || !selectedSupplierId.HasValue || !selectedPriceListId.HasValue)
            {
                return;
            }

            try
            {
                var endpoint = BuildPriceRowsEndpoint(
                    currentFilterExpression,
                    args.Skip,
                    args.Top,
                    null,
                    args.Column.GetFilterProperty(),
                    args.Filter);

                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<VppItemPriceResDTO>>(endpoint);
                args.Data = result.Data ?? [];
                args.Count = result.TotalCount;
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "LoadLibraryDataFailed");
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

        private async Task ClearSearchAsync()
        {
            searchText = string.Empty;
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
                    await _apiServices.PostFromApiAsync<SupplierProductMappingResDTO>(Config.LibraryApi.VPPPriceBase, req);
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
                    await _apiServices.PutFromApiAsync<SupplierProductMappingResDTO>(
                        $"{Config.LibraryApi.VPPPriceBase}/{row.PriceMappingId.Value}", result);
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

        private async Task SetDeletedPriceAsync(VppItemPriceResDTO row, bool isDeleted)
        {
            if (!row.PriceMappingId.HasValue) return;

            var previous = row.IsDeleted;
            row.IsDeleted = isDeleted;

            try
            {
                var result = await _apiServices.PatchFromApiAsync<SupplierProductMappingResDTO>(
                    $"{Config.LibraryApi.SupplierProductMappings}/{row.PriceMappingId.Value}",
                    new { IsDeleted = isDeleted });

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
                var deleted = await _apiServices.DeleteFromApiAsync($"{Config.LibraryApi.SupplierProductMappings}/{row.PriceMappingId.Value}");
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
                await _apiServices.PostFromApiAsync<object>(
                    string.Format(Config.LibraryApi.VPPPrice_SetDefault, row.PriceMappingId.Value), null);
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
            selectedSupplierId = priceLists.FirstOrDefault(x => x.Id == selectedPriceListId)?.SupplierId ?? selectedSupplierId;
            await LoadPricesAsync();
        }

        private async Task OnSupplierChangedAsync(Guid? value)
        {
            selectedSupplierId = value;
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

            return priceLists.FirstOrDefault(x => x.IsDefault)?.Id;
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

        private string BuildPriceRowsEndpoint(
            string? filter,
            int? skip,
            int? top,
            string? orderBy,
            string? distinct = null,
            string? distinctFilter = null)
        {
            var queryParams = new List<string>
            {
                $"supplierId={selectedSupplierId!.Value}",
                $"priceListId={selectedPriceListId!.Value}",
                "showDeleted=true"
            };

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                queryParams.Add($"search={Uri.EscapeDataString(searchText.Trim())}");
            }

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

            if (!string.IsNullOrWhiteSpace(distinct))
            {
                queryParams.Add($"distinct={Uri.EscapeDataString(distinct)}");
            }

            if (!string.IsNullOrWhiteSpace(distinctFilter))
            {
                queryParams.Add($"distinctFilter={Uri.EscapeDataString(distinctFilter)}");
            }

            return $"{Config.LibraryApi.VPPPrice_ItemPrices}?{string.Join("&", queryParams)}";
        }
    }
}
