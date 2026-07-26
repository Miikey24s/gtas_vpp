using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components
{
    public partial class PeriodDemandPanel
    {
        internal enum DemandViewMode
        {
            ByItem,
            ByOrder
        }

        [Inject] private IAPIServices ApiServices { get; set; } = default!;
        [Inject] private IToastService Toast { get; set; } = default!;
        [Parameter] public IEnumerable<Claim>? Claims { get; set; }

        private readonly List<int> months = Enumerable.Range(1, 12).ToList();
        private DemandViewMode viewMode = DemandViewMode.ByItem;
        private AggregatedVppResDTO? demand;
        private int pendingAdditionalCount;
        private int selectedYear = DateTime.Now.Year;
        private int selectedMonth = DateTime.Now.Month;
        private bool isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            // Cùng mặc định với bước Rà soát kỳ: nhắm kỳ vừa đóng nhận đơn.
            var period = await ApiServices.GetFromApiAsync<VppPeriodInfoResDTO>(Config.VppApi.PeriodInfo);
            if (period is not null)
            {
                selectedYear = period.PreviousPeriodYear;
                selectedMonth = period.PreviousPeriodMonth;
            }

            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            isLoading = true;
            try
            {
                demand = await ApiServices.GetFromApiAsync<AggregatedVppResDTO>(
                    $"{Config.VppApi.PeriodDemand}?year={selectedYear}&month={selectedMonth}");

                // KPI "Điều kiện ngăn chốt" đọc từ status chốt kỳ sẵn có — không thêm endpoint.
                var status = await ApiServices.GetFromApiAsync<PeriodSettlementResDTO>(
                    string.Format(Config.RequestApi.PeriodSettlement.Status, selectedYear, selectedMonth));
                pendingAdditionalCount = status?.PendingAdditionalCount ?? 0;
            }
            catch (Exception ex)
            {
                demand = null;
                pendingAdditionalCount = 0;
                Toast.Error(ex, Loc);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task OnYearChangedAsync(int value)
        {
            selectedYear = value;
            await ReloadAsync();
        }

        private async Task OnMonthChangedAsync(int value)
        {
            selectedMonth = value;
            await ReloadAsync();
        }

        private void SetViewMode(DemandViewMode mode)
        {
            viewMode = mode;
        }
    }
}
