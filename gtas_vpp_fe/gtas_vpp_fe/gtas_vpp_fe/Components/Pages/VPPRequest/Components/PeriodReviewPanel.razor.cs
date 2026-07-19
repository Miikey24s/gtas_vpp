using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Globalization;
using System.Text.Json;
using Microsoft.JSInterop;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components
{
    public partial class PeriodReviewPanel
    {
        [Inject] private IAPIServices ApiServices { get; set; } = default!;
        [Inject] private IToastService Toast { get; set; } = default!;
        [Inject] private DialogService DialogService { get; set; } = default!;
        [Inject] private Microsoft.JSInterop.IJSRuntime JSRuntime { get; set; } = default!;
        [Parameter] public EventCallback OnSettled { get; set; }

        private readonly List<int> months = Enumerable.Range(1, 12).ToList();
        private readonly HashSet<Guid> loadedDetailOrderIds = new();
        private readonly HashSet<Guid> loadingDetailOrderIds = new();
        private List<PriceListResDTO> priceLists = [];
        private List<VppRequestResDTO> orders = [];
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
        private IReadOnlyList<FilterDescriptor> currentFilters = [];

        private string SettlePeriodTitle
        {
            get
            {
                if (isLoading)
                {
                    return Loc["CheckingPeriodStatus"].Value;
                }

                if (isSettling)
                {
                    return Loc["Loading"].Value;
                }

                if (!canSettle && !string.IsNullOrWhiteSpace(alertMessage))
                {
                    return alertMessage;
                }

                return Loc["SettlePeriod"].Value;
            }
        }

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
                priceLists = await ApiServices.GetFromApiAsync<List<PriceListResDTO>>(Config.LibraryApi.PriceList) ?? [];
            }
            catch (Exception ex)
            {
                SetAlert(AlertStyle.Warning, UiErrorMapper.GetMessage(ex, Loc));
            }
        }

        private async Task LoadDefaultPeriodAsync()
        {
            var period = await ApiServices.GetFromApiAsync<VppPeriodInfoResDTO>(Config.VppApi.PeriodInfo);
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
                var status = await ApiServices.GetFromApiAsync<PeriodSettlementResDTO>(
                    string.Format(Config.RequestApi.PeriodSettlement.Status, selectedYear, selectedMonth));
                ApplyStatus(status);
            }
            catch (Exception ex)
            {
                canSettle = false;
                SetAlert(AlertStyle.Danger, UiErrorMapper.GetMessage(ex, Loc));
            }
        }

        private void ApplyStatus(PeriodSettlementResDTO? status)
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
                var (data, count, lines, qty, amount) = await ApiServices.GetFromApiWithAmountStatsAsync<List<VppRequestResDTO>>(endpoint);
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
                SetAlert(AlertStyle.Danger, UiErrorMapper.GetMessage(ex, Loc));
                Toast.Error(ex, Loc);
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
            currentFilters = args.Filters?.ToList() ?? [];
            await LoadOrdersAsync(firstLoad: false);
        }

        private async Task OnLoadColumnFilterData(DataGridLoadColumnFilterDataEventArgs<VppRequestResDTO> args)
        {
            try
            {
                if (args.Column == null) return;

                var property = args.Column.GetFilterProperty();
                if (string.IsNullOrWhiteSpace(property)) return;

                var query = new List<string>
                {
                    $"column={Uri.EscapeDataString(property)}",
                    "scope=all",
                    $"year={selectedYear}",
                    $"month={selectedMonth}"
                };

                var scopedFilters = VppOrderGridFilterHelper.BuildColumnFilterScopes(currentFilters, property);

                if (scopedFilters.Count > 0)
                {
                    query.Add($"filters={Uri.EscapeDataString(JsonSerializer.Serialize(scopedFilters))}");
                }
                else if (!string.IsNullOrWhiteSpace(currentFilterExpression))
                {
                    query.Add($"filter={Uri.EscapeDataString(currentFilterExpression)}");
                }

                if (!string.IsNullOrWhiteSpace(args.Filter))
                {
                    query.Add($"distinctFilter={Uri.EscapeDataString(args.Filter)}");
                }

                var apiUrl = $"/api/VPPRequest/order-filter-values?{string.Join("&", query)}";
                var response = await ApiServices.GetFromApiAsync<List<Dictionary<string, object?>>>(apiUrl);

                if (response != null)
                {
                    var distinctDtos = response
                        .Select(VppOrderGridFilterHelper.BuildFilterValueDto)
                        .ToList();

                    args.Data = distinctDtos;
                    args.Count = distinctDtos.Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PeriodReviewPanel] LoadColumnFilterData failed: {ex.Message}");
            }
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
                await ApiServices.PostFromApiAsync<PeriodSettlementResDTO>(
                    Config.RequestApi.PeriodSettlement.Settle,
                    new PeriodSettlementReqDTO
                    {
                        Year = selectedYear,
                        Month = selectedMonth,
                        PriceListId = selectedPriceListId
                    });

                Toast.Notify(NotificationSeverity.Success, Loc["Success"], Loc["PeriodSettlement"]);
                await LoadSettlementStatusAsync();
                await LoadOrdersAsync(firstLoad: true);
                await OnSettled.InvokeAsync();
            }
            catch (Exception ex)
            {
                SetAlert(AlertStyle.Danger, UiErrorMapper.GetMessage(ex, Loc));
            }
            finally
            {
                isSettling = false;
            }
        }

        private async Task OnRowExpandAsync(VppRequestResDTO row)
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
                var detail = await ApiServices.GetFromApiAsync<VppRequestResDTO>(
                    $"{Config.VppApi.Orders}/{row.Id}");
                row.Items = detail?.Items ?? [];
                loadedDetailOrderIds.Add(row.Id);
            }
            catch (Exception ex)
            {
                Toast.Error(ex, Loc);
            }
            finally
            {
                loadingDetailOrderIds.Remove(row.Id);
                StateHasChanged();
            }
        }

        private bool IsRowDetailLoading(Guid orderId) => loadingDetailOrderIds.Contains(orderId);

        private static string FormatMoney(long value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

        public HashSet<Guid> ExpandedOrderIds { get; set; } = new();

        public void ToggleOrderCode(Guid orderId)
        {
            if (ExpandedOrderIds.Contains(orderId))
                ExpandedOrderIds.Remove(orderId);
            else
                ExpandedOrderIds.Add(orderId);
        }

        public string GetShortCode(VppRequestResDTO order)
        {
            var code = order.VppCode;
            if (string.IsNullOrEmpty(code)) return "";
            var parts = code.Split('-');
            if (parts.Length >= 2)
            {
                if (order.IsAdditionalOrder)
                {
                    return $"{parts[0]}-ADD-{parts[1]}";
                }
                return $"{parts[0]}-{parts[1]}";
            }
            return code.Length > 10 ? code.Substring(0, 10) : code;
        }

        public async Task CopyToClipboard(string? text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
                var isVi = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = isVi ? "Đã sao chép" : "Copied",
                    Detail = isVi ? $"Đã sao chép mã đơn hàng: {text}!" : $"Copied order code: {text}!",
                    Duration = 4000
                });
            }
            catch (Exception) { }
        }

        private void SetAlert(AlertStyle style, string message)
        {
            alertStyle = style;
            alertMessage = message;
        }
    }
}
