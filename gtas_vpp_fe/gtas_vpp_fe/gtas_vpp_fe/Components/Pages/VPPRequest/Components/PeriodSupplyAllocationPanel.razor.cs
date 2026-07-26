using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
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
        [Parameter] public EventCallback OnContinueToSettle { get; set; }

        private List<SupplierResDTO> suppliers = [];
        private AggregatedVppResDTO? demand;
        private Guid? selectedSupplierId;
        private int selectedYear = DateTime.Now.Year;
        private int selectedMonth = DateTime.Now.Month;
        private bool isBusy;

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

            var period = await ApiServices.GetFromApiAsync<VppPeriodInfoResDTO>(Config.VppApi.PeriodInfo);
            if (period is not null)
            {
                selectedYear = period.PreviousPeriodYear;
                selectedMonth = period.PreviousPeriodMonth;
            }

            selectedSupplierId = State.SelectedSupplierId;
            await LoadSuppliersAsync();
            await LoadDemandNamesAsync();
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
                    $"{Config.VppApi.PeriodDemand}?year={selectedYear}&month={selectedMonth}");
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
                        Year = selectedYear,
                        Month = selectedMonth,
                        PrimarySupplierId = selectedSupplierId,
                        PriceAsOfUtc = DateTime.UtcNow,
                        Exceptions = exceptions
                    });

                State.SetPreview(preview);
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

        public void Dispose()
        {
            State.Changed -= OnStateChanged;
        }
    }
}
