using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Globalization;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components
{
    public partial class PeriodReviewPanel
    {
        [Inject] private IAPIServices ApiServices { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;
        [Inject] private DialogService DialogService { get; set; } = default!;
        [Parameter] public EventCallback OnSettled { get; set; }

        private readonly List<int> months = Enumerable.Range(1, 12).ToList();
        private readonly HashSet<Guid> loadedDetailOrderIds = new();
        private readonly HashSet<Guid> loadingDetailOrderIds = new();
        private List<L07_PriceListResDTO> priceLists = [];
        private List<VPP01_RequestHeaderResDTO> orders = [];
        private Guid? selectedPriceListId;
        private int selectedYear = 2024;
        private int selectedMonth = 1;
        private bool canSettle;
        private bool isLoading = true;
        private bool isGridLoading;
        private bool isSettling;
        private string? alertMessage;
        private AlertStyle alertStyle = AlertStyle.Info;
        private int totalCount;
        private int totalLines;
        private int totalQty;
        private long totalAmount;
        private int currentSkip;
        private int pageSize = 20;
        private string? currentFilterExpression;
        private string? currentOrderByExpression;

        protected override async Task OnInitializedAsync()
        {
            await LoadPriceListsAsync();
            await LoadDefaultPeriodAsync();
            await ReloadPeriodAsync();
        }

        private async Task LoadPriceListsAsync()
        {
            try
            {
                priceLists = await ApiServices.GetFromApiAsync<List<L07_PriceListResDTO>>(Config.LibraryApi.L07_PriceList) ?? [];
            }
            catch (Exception ex)
            {
                SetAlert(AlertStyle.Warning, ex.Message);
            }
        }

        private async Task LoadDefaultPeriodAsync()
        {
            var period = await ApiServices.GetFromApiAsync<VPP_PeriodInfoResDTO>(Config.VppApi.PeriodInfo);
            if (period is null)
            {
                return;
            }

            selectedYear = period.PreviousPeriodYear;
            selectedMonth = period.PreviousPeriodMonth;
        }

        private async Task ReloadPeriodAsync()
        {
            currentSkip = 0;
            currentFilterExpression = null;
            currentOrderByExpression = null;
            await LoadSettlementStatusAsync();
            await LoadOrdersAsync(firstLoad: true);
        }

        private async Task LoadSettlementStatusAsync()
        {
            try
            {
                var status = await ApiServices.GetFromApiAsync<VPP_PeriodSettlementResDTO>(
                    string.Format(Config.RequestApi.PeriodSettlement.Status, selectedYear, selectedMonth));
                ApplyStatus(status);
            }
            catch (Exception ex)
            {
                canSettle = false;
                SetAlert(AlertStyle.Danger, ex.Message);
            }
        }

        private void ApplyStatus(VPP_PeriodSettlementResDTO? status)
        {
            if (status is null)
            {
                canSettle = false;
                SetAlert(AlertStyle.Warning, Loc["Error"].Value);
                return;
            }

            if (status.PendingAdditionalCount > 0)
            {
                canSettle = false;
                SetAlert(AlertStyle.Warning, string.Format(Loc["Warning_PendingAdditional"], status.PendingAdditionalCount));
                return;
            }

            canSettle = true;
            if (status.IsSettled)
            {
                canSettle = false;
                SetAlert(
                    AlertStyle.Info,
                    string.Format(
                        Loc["Info_AlreadySettled"],
                        DateFormatter.Format(status.SettledAt, DateFormatter.LongDate),
                        status.SettledByUserName ?? "-",
                        status.PriceListName ?? "-"));
                return;
            }

            alertMessage = null;
        }

        private async Task LoadOrdersAsync(bool firstLoad)
        {
            if (firstLoad)
            {
                isLoading = true;
            }
            else
            {
                isGridLoading = true;
            }

            try
            {
                var endpoint = BuildOrdersEndpoint();
                var (data, count, lines, qty, amount) = await ApiServices.GetFromApiWithAmountStatsAsync<List<VPP01_RequestHeaderResDTO>>(endpoint);
                orders = data ?? [];
                totalCount = count;
                totalLines = lines;
                totalQty = qty;
                totalAmount = amount;
                loadedDetailOrderIds.Clear();
                loadingDetailOrderIds.Clear();
            }
            catch (Exception ex)
            {
                SetAlert(AlertStyle.Danger, ex.Message);
                NotificationService.Notify(NotificationSeverity.Error, Loc["Error"], ex.Message);
            }
            finally
            {
                isLoading = false;
                isGridLoading = false;
                StateHasChanged();
            }
        }

        private string BuildOrdersEndpoint()
        {
            var query = new List<string>
            {
                $"year={selectedYear}",
                $"month={selectedMonth}",
                $"skip={currentSkip}",
                $"top={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(currentFilterExpression)) query.Add($"filter={Uri.EscapeDataString(currentFilterExpression)}");
            if (!string.IsNullOrWhiteSpace(currentOrderByExpression)) query.Add($"orderby={Uri.EscapeDataString(currentOrderByExpression)}");

            return $"{Config.VppApi.AllOrders}?{string.Join("&", query)}";
        }

        private async Task OnLoadData(LoadDataArgs args)
        {
            currentSkip = args.Skip ?? 0;
            if (args.Top.HasValue && args.Top.Value > 0) pageSize = args.Top.Value;
            currentFilterExpression = args.Filter;
            currentOrderByExpression = args.OrderBy;
            await LoadOrdersAsync(firstLoad: false);
        }

        private async Task OnYearChangedAsync(int value)
        {
            selectedYear = value;
            await ReloadPeriodAsync();
        }

        private async Task OnMonthChangedAsync(int value)
        {
            selectedMonth = value;
            await ReloadPeriodAsync();
        }

        private Task OnPriceListChangedAsync(Guid? value)
        {
            selectedPriceListId = value;
            return Task.CompletedTask;
        }

        private async Task SettleAsync()
        {
            var priceListName = selectedPriceListId.HasValue
                ? priceLists.FirstOrDefault(x => x.Id == selectedPriceListId.Value)?.PriceListName ?? selectedPriceListId.Value.ToString()
                : Loc["PriceList_UseDefault"].Value;

            var confirmed = await DialogService.Confirm(
                string.Format(Loc["Confirm_Settle"], selectedMonth, selectedYear, priceListName),
                Loc["PeriodSettlement"],
                new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });

            if (confirmed != true)
            {
                return;
            }

            isSettling = true;
            try
            {
                await ApiServices.PostFromApiAsync<VPP_PeriodSettlementResDTO>(
                    Config.RequestApi.PeriodSettlement.Settle,
                    new VPP_SettlePeriodReqDTO
                    {
                        Y = selectedYear,
                        M = selectedMonth,
                        PriceListId = selectedPriceListId
                    });

                NotificationService.Notify(NotificationSeverity.Success, Loc["Success"], Loc["PeriodSettlement"]);
                await LoadSettlementStatusAsync();
                await LoadOrdersAsync(firstLoad: true);
                await OnSettled.InvokeAsync();
            }
            catch (Exception ex)
            {
                SetAlert(AlertStyle.Danger, ex.Message);
            }
            finally
            {
                isSettling = false;
            }
        }

        private async Task OnRowExpandAsync(VPP01_RequestHeaderResDTO row)
        {
            if (row == null || row.Id == Guid.Empty
                || loadedDetailOrderIds.Contains(row.Id)
                || loadingDetailOrderIds.Contains(row.Id))
            {
                return;
            }

            loadingDetailOrderIds.Add(row.Id);
            try
            {
                var detail = await ApiServices.GetFromApiAsync<VPP01_RequestHeaderResDTO>(
                    $"{Config.VppApi.Orders}/{row.Id}");
                row.Items = detail?.Items ?? [];
                loadedDetailOrderIds.Add(row.Id);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, Loc["Error"], ex.Message);
            }
            finally
            {
                loadingDetailOrderIds.Remove(row.Id);
                StateHasChanged();
            }
        }

        private bool IsRowDetailLoading(Guid orderId) => loadingDetailOrderIds.Contains(orderId);

        private static string FormatMoney(long value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

        private void SetAlert(AlertStyle style, string message)
        {
            alertStyle = style;
            alertMessage = message;
        }
    }
}
