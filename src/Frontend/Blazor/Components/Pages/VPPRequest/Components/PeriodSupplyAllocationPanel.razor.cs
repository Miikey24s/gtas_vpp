using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components
{
    public partial class PeriodSupplyAllocationPanel : IDisposable
    {
        [Inject] private IAPIServices ApiServices { get; set; } = default!;
        [Inject] private IToastService Toast { get; set; } = default!;
        [Inject] private PeriodSettlementState State { get; set; } = default!;
        [Parameter] public int Year { get; set; }
        [Parameter] public int Month { get; set; }
        [Parameter] public EventCallback OnContinueToSettle { get; set; }

        private List<SupplierResDTO> suppliers = [];
        private List<VppItemPriceResDTO> primaryItemPrices = [];
        private AggregatedVppResDTO? demand;
        private Guid? selectedSupplierId;
        private bool isBusy;
        private bool isPriceComparisonLoading;
        private int loadedYear;
        private int loadedMonth;
        private string searchText = string.Empty;
        private SupplyPriceResult? selectedResult;

        private bool HasTableFilters => !string.IsNullOrWhiteSpace(searchText) || selectedResult.HasValue;
        private bool CanContinue => State.Preview is { PrimaryQuote: not null, Blockers.Count: 0 };
        private List<SupplyPriceComparisonRow> FilteredPriceRows => PriceComparisonRows
            .Where(row => string.IsNullOrWhiteSpace(searchText)
                || (row.VppName?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (row.VppCode?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false))
            .Where(row => !selectedResult.HasValue || row.Result == selectedResult.Value)
            .ToList();

        private IReadOnlyList<VppFilterOption<SupplyPriceResult?>> ResultOptions =>
        [
            new(null, Loc["All"]),
            new(SupplyPriceResult.Available, Loc["SupplyPriceAvailable"]),
            new(SupplyPriceResult.Exception, Loc["SupplyPriceException"]),
            new(SupplyPriceResult.Missing, Loc["SupplyPriceMissing"])
        ];

        private List<SupplyPriceComparisonRow> PriceComparisonRows => demand?.Items
            .Select(item =>
            {
                var exception = State.Preview?.Exceptions.FirstOrDefault(x => x.VppId == item.VppId && x.IsValid);
                var primaryPrice = primaryItemPrices.FirstOrDefault(x => x.VppId == item.VppId);
                var unitPrice = exception?.NetUnitPrice ?? primaryPrice?.NetPrice ?? primaryPrice?.Price;
                var result = exception is not null
                    ? SupplyPriceResult.Exception
                    : unitPrice.HasValue
                        ? SupplyPriceResult.Available
                        : SupplyPriceResult.Missing;

                return new SupplyPriceComparisonRow(
                    item.VppId,
                    item.VppCode,
                    item.VppName,
                    item.TotalQty,
                    unitPrice,
                    result);
            })
            .ToList() ?? [];

        private string ShortInputHash => string.IsNullOrWhiteSpace(State.Preview?.InputHash)
            ? "—"
            : State.Preview.InputHash[..Math.Min(12, State.Preview.InputHash.Length)];

        private string EffectivePriceListLabel
        {
            get
            {
                var primary = State.Preview?.PrimaryQuote;
                return primary is null
                    ? Loc["SupplyPriceListPending"].Value
                    : $"{primary.PriceListCode} · v{primary.Version}";
            }
        }

        private IEnumerable<SupplierResDTO> AlternativeSuppliers => suppliers
            .Where(x => State.Preview?.PrimarySupplierId is null || x.Id != State.Preview.PrimarySupplierId.Value);

        private int PendingExceptionCount
        {
            get
            {
                var missing = State.Preview?.PrimaryQuote?.MissingVppIds ?? [];
                return missing.Count(vppId =>
                {
                    var row = State.Exceptions.FirstOrDefault(x => x.VppId == vppId);
                    return row is null || row.SupplierId == Guid.Empty || string.IsNullOrWhiteSpace(row.Reason);
                });
            }
        }

        protected override async Task OnInitializedAsync()
        {
            State.Changed += OnStateChanged;
            selectedSupplierId = State.SelectedSupplierId;
            await LoadSuppliersAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (Year < 2024 || Month is < 1 or > 12 || (loadedYear == Year && loadedMonth == Month)) return;
            loadedYear = Year;
            loadedMonth = Month;
            selectedSupplierId = State.SelectedSupplierId;
            await LoadDemandNamesAsync();
            if (State.Preview is not null) await LoadPrimaryItemPricesAsync(State.Preview);
        }

        private void OnStateChanged() => _ = InvokeAsync(StateHasChanged);

        private async Task LoadSuppliersAsync()
        {
            try
            {
                suppliers = (await ApiServices.GetFromApiAsync<List<SupplierResDTO>>(Config.LibraryApi.Suppliers) ?? [])
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.SupplierName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                Toast.Error(ex, Loc);
            }
        }

        // Tên mặt hàng cho panel ngoại lệ: preview chỉ trả VppId nên tra qua bản gom
        // nhu cầu (D7) thay vì thêm endpoint mới.
        private async Task LoadDemandNamesAsync()
        {
            try
            {
                demand = await ApiServices.GetFromApiAsync<AggregatedVppResDTO>(
                    $"{Config.VppApi.PeriodDemand}?year={Year}&month={Month}");
            }
            catch
            {
                demand = null;
            }
        }

        private string GetItemName(Guid vppId) =>
            demand?.Items.FirstOrDefault(x => x.VppId == vppId)?.VppName ?? vppId.ToString("N")[..8];

        private async Task OnSupplierChangedAsync(Guid? value)
        {
            selectedSupplierId = value;
            State.SelectedSupplierId = value;
            State.Exceptions.Clear();
            State.SetPreview(null);
            primaryItemPrices.Clear();
            await InvokeAsync(StateHasChanged);
        }

        private SettlementExceptionReqDTO GetOrCreateExceptionRow(Guid vppId)
        {
            var row = State.Exceptions.FirstOrDefault(x => x.VppId == vppId);
            if (row is null)
            {
                row = new SettlementExceptionReqDTO { VppId = vppId };
                State.Exceptions.Add(row);
            }

            return row;
        }

        private void OnExceptionSupplierChanged(Guid vppId, Guid? supplierId)
        {
            var row = GetOrCreateExceptionRow(vppId);
            row.SupplierId = supplierId ?? Guid.Empty;
        }

        private void OnExceptionReasonChanged(Guid vppId, string? reason)
        {
            var row = GetOrCreateExceptionRow(vppId);
            row.Reason = reason;
        }

        private async Task RunPreviewAsync()
        {
            isBusy = true;
            try
            {
                var exceptions = State.Exceptions
                    .Where(x => x.SupplierId != Guid.Empty && !string.IsNullOrWhiteSpace(x.Reason))
                    .ToList();

                var preview = await ApiServices.PostFromApiAsync<SettlementPreviewResDTO>(
                    Config.RequestApi.PeriodSettlement.Preview,
                    new SettlementPreviewReqDTO
                    {
                        Year = Year,
                        Month = Month,
                        PrimarySupplierId = selectedSupplierId,
                        PriceAsOfUtc = DateTime.UtcNow,
                        Exceptions = exceptions
                    });

                State.SetPreview(preview);
                if (preview is not null)
                {
                    await LoadPrimaryItemPricesAsync(preview);
                }
            }
            catch (Exception ex)
            {
                Toast.Error(ex, Loc);
            }
            finally
            {
                isBusy = false;
                StateHasChanged();
            }
        }

        private async Task ContinueToSettle()
        {
            await OnContinueToSettle.InvokeAsync();
        }

        private static string FormatMoney(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

        private async Task LoadPrimaryItemPricesAsync(SettlementPreviewResDTO preview)
        {
            primaryItemPrices.Clear();
            if (!preview.PrimarySupplierId.HasValue || !preview.PrimaryPriceListId.HasValue)
            {
                return;
            }

            isPriceComparisonLoading = true;
            try
            {
                var endpoint = $"{Config.LibraryApi.VPPPrice_ItemPrices}?supplierId={preview.PrimarySupplierId.Value}&priceListId={preview.PrimaryPriceListId.Value}&showDeleted=false&top=5000";
                primaryItemPrices = await ApiServices.GetFromApiAsync<List<VppItemPriceResDTO>>(endpoint) ?? [];
            }
            catch (Exception ex)
            {
                Toast.Error(ex, Loc);
            }
            finally
            {
                isPriceComparisonLoading = false;
            }
        }

        private void OnSearchInput(ChangeEventArgs args) => searchText = args.Value?.ToString() ?? string.Empty;
        private Task OnResultChanged(SupplyPriceResult? value) { selectedResult = value; return Task.CompletedTask; }
        private Task ClearTableFilters() { searchText = string.Empty; selectedResult = null; return Task.CompletedTask; }

        private string GetPriceResultText(SupplyPriceResult result) => result switch
        {
            SupplyPriceResult.Available => Loc["SupplyPriceAvailable"],
            SupplyPriceResult.Exception => Loc["SupplyPriceException"],
            _ => Loc["SupplyPriceMissing"]
        };

        private static string GetPriceResultCss(SupplyPriceResult result) => result switch
        {
            SupplyPriceResult.Available => "vpp-badge-success",
            SupplyPriceResult.Exception => "vpp-badge-warning",
            _ => "vpp-badge-danger"
        };

        public void Dispose()
        {
            State.Changed -= OnStateChanged;
        }

        private sealed record SupplyPriceComparisonRow(
            Guid VppId,
            string? VppCode,
            string? VppName,
            int TotalQuantity,
            decimal? UnitPrice,
            SupplyPriceResult Result);

        private enum SupplyPriceResult
        {
            Missing,
            Available,
            Exception
        }
    }
}
