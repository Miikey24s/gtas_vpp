using gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs
{
    public partial class Tab_PriceLibrary
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
        private RadzenDataGrid<L06_VPPSupplierMappingResDTO> grid = default!;
        private Guid? selectedPriceListId;
        private Guid? selectedVppId;
        private bool isLoading;
        private bool HasPriceLists => priceLists.Count > 0;
        private bool HasPriceListSelected => selectedPriceListId.HasValue;
        private string GridEmptyText => !HasPriceLists
            ? Loc["NoPriceListAvailable"].Value
            : selectedVppId.HasValue ? Loc["NoPricesFound"].Value : Loc["LoadPricesPrompt"].Value;

        protected override async Task OnInitializedAsync()
        {
            await LoadLookupsAsync();
        }

        private async Task LoadLookupsAsync()
        {
            try
            {
                priceLists = await _apiServices.GetFromApiAsync<List<L07_PriceListResDTO>>(Config.LibraryApi.L07_PriceList) ?? [];
                vppItems = await _apiServices.GetFromApiAsync<List<L04_VPPResDTO>>(Config.LibraryApi.L04_Item) ?? [];
                suppliers = await _apiServices.GetFromApiAsync<List<L05_VPPSupplierResDTO>>(Config.LibraryApi.L05_Supplier) ?? [];
                selectedPriceListId = ResolveSelectedPriceListId();
            }
            catch (Exception ex)
            {
                Notify(NotificationSeverity.Error, Loc["Error"].Value, ex.Message);
            }
        }

        private async Task LoadPricesAsync()
        {
            prices = [];
            if (!selectedVppId.HasValue || !selectedPriceListId.HasValue)
            {
                await ReloadGridAsync();
                return;
            }

            isLoading = true;
            try
            {
                prices = await _apiServices.GetFromApiAsync<List<L06_VPPSupplierMappingResDTO>>(
                    $"{Config.LibraryApi.VPPPrice_ByVpp}/{selectedVppId.Value}?priceListId={selectedPriceListId.Value}") ?? [];
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

        private async Task AddPriceAsync()
        {
            if (!selectedVppId.HasValue || !selectedPriceListId.HasValue) return;

            var model = new L06_PriceUpdateReqDTO
            {
                L04_VPPId = selectedVppId.Value,
                L07_PriceListId = selectedPriceListId.Value
            };
            var result = await OpenEditorAsync(Loc["AddNewPrice"].Value, model);
            if (result is null) return;

            try
            {
                var req = new L06_PriceCreateReqDTO
                {
                    L04_VPPId = selectedVppId.Value,
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

        private async Task EditPriceAsync(L06_VPPSupplierMappingResDTO row)
        {
            var model = new L06_PriceUpdateReqDTO
            {
                Id = row.Id,
                L04_VPPId = row.L04_VPPId,
                L05_VPPSupplierId = row.L05_VPPSupplierId,
                L07_PriceListId = selectedPriceListId ?? row.L07_PriceListId,
                Price = row.Price,
                IsDefault = row.IsDefault,
                Description = row.Description
            };
            var result = await OpenEditorAsync(Loc["EditPrice"].Value, model);
            if (result is null) return;

            try
            {
                await _apiServices.PutFromApiAsync<L06_VPPSupplierMappingResDTO>(
                    $"{Config.LibraryApi.VPPPriceBase}/{row.Id}", result);
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceSaved"].Value);
                await LoadPricesAsync();
            }
            catch (Exception ex)
            {
                NotifyPriceError(ex);
            }
        }

        private async Task DeletePriceAsync(L06_VPPSupplierMappingResDTO row)
        {
            try
            {
                var deleted = await _apiServices.DeleteFromApiAsync($"{Config.LibraryApi.VPPPriceBase}/{row.Id}");
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

        private async Task SetDefaultAsync(L06_VPPSupplierMappingResDTO row)
        {
            try
            {
                await _apiServices.PostFromApiAsync<object>(
                    string.Format(Config.LibraryApi.VPPPrice_SetDefault, row.Id), null);
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
}
