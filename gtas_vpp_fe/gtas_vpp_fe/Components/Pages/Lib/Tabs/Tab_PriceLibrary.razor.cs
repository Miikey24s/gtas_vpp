using gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog;
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
    public partial class Tab_PriceLibrary : IDisposable
    {
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public ICustomNotificationService _notificationService { get; set; } = default!;
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private List<L07_PriceListResDTO> priceLists = [];
        private List<L04_VPPResDTO> vppItems = [];
        private List<L05_VPPSupplierResDTO> suppliers = [];
        private List<L06_VPPSupplierMappingResDTO> prices = [];
        private List<VppItemPriceDisplayModel> displayItems = [];
        private List<VppItemPriceDisplayModel> filteredDisplayItems = [];
        private RadzenDataGrid<VppItemPriceDisplayModel> grid = default!;
        private Guid? selectedPriceListId;
        private Guid? selectedSupplierId;
        private string searchText = "";
        private bool isLoading;
        private bool HasPriceLists => priceLists.Count > 0;
        private bool HasPriceListSelected => selectedPriceListId.HasValue;
        private string GridEmptyText => !HasPriceLists
            ? Loc["NoPriceListAvailable"].Value
            : selectedSupplierId.HasValue ? Loc["NoPricesFound"].Value : Loc["LoadPricesPrompt"].Value;

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
                var priceListsTask = _apiServices.GetFromApiAsync<List<L07_PriceListResDTO>>($"{Config.LibraryApi.L07_PriceList}?showDeleted=true");
                var vppItemsTask = _apiServices.GetFromApiAsync<List<L04_VPPResDTO>>($"{Config.LibraryApi.L04_Item}?showDeleted=true");
                var suppliersTask = _apiServices.GetFromApiAsync<List<L05_VPPSupplierResDTO>>($"{Config.LibraryApi.L05_Supplier}?showDeleted=true");

                await Task.WhenAll(priceListsTask, vppItemsTask, suppliersTask);

                priceLists = await priceListsTask ?? [];
                vppItems = await vppItemsTask ?? [];
                suppliers = await suppliersTask ?? [];
                
                selectedPriceListId = ResolveSelectedPriceListId();
                selectedSupplierId = suppliers.FirstOrDefault(s => s.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName)?.Id
                                     ?? suppliers.FirstOrDefault()?.Id;

                await LoadPricesAsync();
            }
            catch (Exception ex)
            {
                Notify(NotificationSeverity.Error, Loc["Error"].Value, ex.Message);
            }
        }

        private async Task LoadPricesAsync()
        {
            prices = [];
            displayItems = [];
            filteredDisplayItems = [];

            if (!selectedSupplierId.HasValue || !selectedPriceListId.HasValue)
            {
                await ReloadGridAsync();
                return;
            }

            isLoading = true;
            try
            {
                prices = await _apiServices.GetFromApiAsync<List<L06_VPPSupplierMappingResDTO>>(
                    $"{Config.LibraryApi.VPPPrice_BySupplier}/{selectedSupplierId.Value}?priceListId={selectedPriceListId.Value}&showDeleted=true") ?? [];

                var priceMap = prices
                    .GroupBy(p => p.L04_VPPId)
                    .ToDictionary(g => g.Key, g => g.First());

                displayItems = vppItems.Select(vpp =>
                {
                    priceMap.TryGetValue(vpp.Id, out var mapping);
                    return new VppItemPriceDisplayModel
                    {
                        VPPId = vpp.Id,
                        VPPCode = vpp.VPPCode,
                        VPPName = vpp.VPPName,
                        CategoryName = vpp.VPPCategory?.VPPCategoryName,
                        UOMName = vpp.UOM?.ClassDetailValue,
                        PriceMappingId = mapping?.Id,
                        Price = mapping?.Price,
                        IsDefault = mapping?.IsDefault ?? false,
                        Description = mapping?.Description
                    };
                }).ToList();

                FilterDisplayItems();
            }
            catch (Exception ex)
            {
                Notify(NotificationSeverity.Error, Loc["Error"].Value, ex.Message);
            }
            finally
            {
                isLoading = false;
                await ReloadGridAsync();
            }
        }

        private void FilterDisplayItems()
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredDisplayItems = [.. displayItems];
            }
            else
            {
                var query = searchText.Trim().ToLowerInvariant();
                filteredDisplayItems = displayItems.Where(x =>
                    (x.VPPCode?.ToLowerInvariant().Contains(query) ?? false) ||
                    (x.VPPName?.ToLowerInvariant().Contains(query) ?? false) ||
                    (x.CategoryName?.ToLowerInvariant().Contains(query) ?? false) ||
                    (x.UOMName?.ToLowerInvariant().Contains(query) ?? false)
                ).ToList();
            }
        }

        private void OnSearchInput(string? value)
        {
            searchText = value ?? "";
            FilterDisplayItems();
        }

        private async Task EditPriceAsync(VppItemPriceDisplayModel row)
        {
            if (!selectedSupplierId.HasValue || !selectedPriceListId.HasValue) return;

            if (!row.PriceMappingId.HasValue)
            {
                var model = new L06_PriceUpdateReqDTO
                {
                    L04_VPPId = row.VPPId,
                    L07_PriceListId = selectedPriceListId.Value,
                    L05_VPPSupplierId = selectedSupplierId.Value,
                    Price = 0,
                    IsDefault = false,
                    Description = ""
                };
                var result = await OpenEditorAsync(Loc["AddNewPrice"].Value, model);
                if (result is null) return;

                try
                {
                    var req = new L06_PriceCreateReqDTO
                    {
                        L04_VPPId = row.VPPId,
                        L05_VPPSupplierId = result.L05_VPPSupplierId,
                        L07_PriceListId = selectedPriceListId.Value,
                        Price = result.Price,
                        IsDefault = result.IsDefault,
                        Description = result.Description
                    };
                    await _apiServices.PostFromApiAsync<L06_VPPSupplierMappingResDTO>(Config.LibraryApi.VPPPriceBase, req);
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
                var model = new L06_PriceUpdateReqDTO
                {
                    Id = row.PriceMappingId.Value,
                    L04_VPPId = row.VPPId,
                    L05_VPPSupplierId = selectedSupplierId.Value,
                    L07_PriceListId = selectedPriceListId.Value,
                    Price = row.Price ?? 0,
                    IsDefault = row.IsDefault,
                    Description = row.Description
                };
                var result = await OpenEditorAsync(Loc["EditPrice"].Value, model);
                if (result is null) return;

                try
                {
                    await _apiServices.PutFromApiAsync<L06_VPPSupplierMappingResDTO>(
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

        private async Task DeletePriceAsync(VppItemPriceDisplayModel row)
        {
            if (!row.PriceMappingId.HasValue) return;

            try
            {
                var deleted = await _apiServices.DeleteFromApiAsync($"{Config.LibraryApi.VPPPriceBase}/{row.PriceMappingId.Value}");
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
                Notify(NotificationSeverity.Error, Loc["Error"].Value, ex.Message);
            }
        }

        private async Task SetDefaultAsync(VppItemPriceDisplayModel row)
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

        private async Task<L06_PriceUpdateReqDTO?> OpenEditorAsync(string title, L06_PriceUpdateReqDTO model)
        {
            var result = await DialogService.OpenAsync<Dialog_PriceEditor>(
                title,
                new Dictionary<string, object?>
                {
                    [nameof(Dialog_PriceEditor.Model)] = model,
                    [nameof(Dialog_PriceEditor.Suppliers)] = suppliers
                },
                new DialogOptions { Width = "520px", Resizable = true, Draggable = true });

            return result as L06_PriceUpdateReqDTO;
        }

        private async Task OnPriceListChangedAsync()
        {
            await LoadPricesAsync();
        }

        private async Task OnSupplierChangedAsync()
        {
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
            var detail = ex.Message.Contains("Conflict", StringComparison.OrdinalIgnoreCase)
                ? Loc["OnlyOneDefaultPerVPPAllowed"].Value
                : ex.Message;
            Notify(NotificationSeverity.Error, Loc["Error"].Value, detail);
        }

        private void Notify(NotificationSeverity severity, string summary, string detail)
        {
            _notificationService.CustomContentNotification(severity, summary, detail, 5000, false);
        }
    }

    public class VppItemPriceDisplayModel
    {
        public Guid VPPId { get; set; }
        public string? VPPCode { get; set; }
        public string? VPPName { get; set; }
        public string? CategoryName { get; set; }
        public string? UOMName { get; set; }
        public Guid? PriceMappingId { get; set; }
        public decimal? Price { get; set; }
        public bool IsDefault { get; set; }
        public string? Description { get; set; }
    }
}
