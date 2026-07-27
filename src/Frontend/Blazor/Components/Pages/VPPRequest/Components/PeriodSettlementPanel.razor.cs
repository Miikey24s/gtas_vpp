using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components
{
    public partial class PeriodSettlementPanel
    {
        [Inject] private IAPIServices ApiServices { get; set; } = default!;
        [Inject] private IToastService Toast { get; set; } = default!;
        [Inject] private DialogService DialogService { get; set; } = default!;
        [Parameter] public EventCallback OnSettled { get; set; }

        private List<PriceListResDTO> priceLists = [];
        private Guid? selectedPriceListId;
        private int selectedYear = 2024;
        private int selectedMonth = 1;
        private bool canSettle;
        private bool canPreview;
        private bool canCorrect;
        private bool canSettlePreview;
        private bool isLoading;
        private bool isSettling;
        private bool isCorrecting;
        private bool isPreviewing;
        private SettlementPreviewResDTO? preview;
        private string correctionReason = string.Empty;
        private string? settlementIdempotencyKey;
        private string? alertMessage;
        private AlertStyle alertStyle = AlertStyle.Info;

        private string ShortInputHash => string.IsNullOrWhiteSpace(preview?.InputHash)
            ? "—"
            : preview.InputHash[..Math.Min(12, preview.InputHash.Length)];

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

        private string StatusCardClass
        {
            get
            {
                if (isLoading) return "status-info";
                if (!string.IsNullOrWhiteSpace(alertMessage))
                {
                    return alertStyle switch
                    {
                        AlertStyle.Danger => "status-danger",
                        AlertStyle.Warning => "status-warning",
                        AlertStyle.Success => "status-success",
                        _ => "status-info"
                    };
                }
                return canSettle ? "status-success" : "status-info";
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadPriceListsAsync();
            if (await LoadCurrentPeriodAsync())
            {
                await LoadStatusAsync();
            }
        }

        private async Task LoadPriceListsAsync()
        {
            try
            {
                priceLists = (await ApiServices.GetFromApiAsync<List<PriceListResDTO>>(Config.LibraryApi.PriceList) ?? [])
                    .Where(x => x.Status == "Published" && !x.IsDeleted)
                    .ToList();
            }
            catch (Exception ex)
            {
                SetAlert(AlertStyle.Warning, UiErrorMapper.GetMessage(ex, Loc));
            }
        }

        private async Task<bool> LoadCurrentPeriodAsync()
        {
            try
            {
                var period = await ApiServices.GetFromApiAsync<VppPeriodInfoResDTO>(Config.VppApi.PeriodInfo);
                if (period is null)
                {
                    canSettle = false;
                    SetAlert(AlertStyle.Warning, Loc["Error"].Value);
                    return false;
                }

                // Chốt kỳ luôn nhắm đến kỳ vừa đóng (Previous), không phải kỳ còn mở
                // (Current). User vẫn gửi đơn vào kỳ hiện tại nên khóa kỳ đó sẽ chặn
                // luồng gửi đơn bình thường.
                selectedYear = period.PreviousPeriodYear;
                selectedMonth = period.PreviousPeriodMonth;
                return true;
            }
            catch (Exception ex)
            {
                canSettle = false;
                SetAlert(AlertStyle.Danger, UiErrorMapper.GetMessage(ex, Loc));
                return false;
            }
        }

        private async Task LoadStatusAsync()
        {
            if (selectedYear < 2024 || selectedYear > 2030 || selectedMonth < 1 || selectedMonth > 12)
            {
                canSettle = false;
                return;
            }

            isLoading = true;
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
            finally
            {
                isLoading = false;
                StateHasChanged();
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
                canPreview = false;
                canCorrect = false;
                canSettlePreview = false;
                SetAlert(AlertStyle.Warning, string.Format(Loc["Warning_PendingAdditional"], status.PendingAdditionalCount));
                return;
            }

            canPreview = true;
            canSettle = false;
            canCorrect = status.IsSettled && status.SettlementId.HasValue;
            canSettlePreview = false;
            if (status.IsSettled)
            {
                SetAlert(
                    AlertStyle.Info,
                    string.Format(
                        Loc["Info_AlreadySettled"],
                        DateFormatter.Format(status.SettledAt, DateFormatter.LongDate),
                        status.SettledByUserName ?? "-",
                        status.PriceListName ?? "-"));
                return;
            }

            SetAlert(AlertStyle.Info, Loc["SettleRunPreviewHint"].Value);
        }

        private Task OnPriceListChangedAsync(Guid? value)
        {
            selectedPriceListId = value;
            canSettle = false;
            canSettlePreview = false;
            preview = null;
            settlementIdempotencyKey = null;
            return Task.CompletedTask;
        }

        private async Task PreviewAsync()
        {
            isPreviewing = true;
            try
            {
                preview = await ApiServices.PostFromApiAsync<SettlementPreviewResDTO>(
                    Config.RequestApi.PeriodSettlement.Preview,
                    new SettlementPreviewReqDTO
                    {
                        Year = selectedYear,
                        Month = selectedMonth,
                        PriceListId = selectedPriceListId,
                        PriceAsOfUtc = DateTime.UtcNow
                    });

                if (preview is null)
                {
                    canSettle = false;
                    SetAlert(AlertStyle.Warning, Loc["SettlePreviewNoData"].Value);
                    return;
                }

                canSettlePreview = preview.Blockers.Count == 0 && preview.PrimaryQuote is not null;
                canSettle = canSettlePreview && !canCorrect;
                if (!canSettlePreview)
                {
                    SetAlert(AlertStyle.Warning, string.Join("; ", preview.Blockers.Take(3)));
                    return;
                }

                var quote = preview.PrimaryQuote!;
                selectedPriceListId = quote.PriceListId;
                settlementIdempotencyKey ??= $"{selectedYear:D4}{selectedMonth:D2}-{Guid.NewGuid():N}";
                SetAlert(
                    AlertStyle.Success,
                    string.Format(
                        Loc["SettlePreviewCoverageSummary"].Value,
                        quote.SupplierName ?? Loc["SettleSupplierFallback"].Value,
                        quote.CoveredItemCount,
                        quote.RequestedItemCount,
                        $"{quote.GrandTotal:N0}"));
            }
            catch (Exception ex)
            {
                canSettle = false;
                SetAlert(AlertStyle.Danger, UiErrorMapper.GetMessage(ex, Loc));
            }
            finally
            {
                isPreviewing = false;
            }
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
                await ApiServices.PostFromApiAsync<SettlementRevisionResDTO>(
                    Config.RequestApi.PeriodSettlement.Confirm,
                    new SettlementConfirmReqDTO
                    {
                        Year = selectedYear,
                        Month = selectedMonth,
                        PriceAsOfUtc = preview!.PriceAsOfUtc,
                        InputHash = preview.InputHash,
                        PrimarySupplierId = preview.PrimarySupplierId!.Value,
                        PriceListId = preview.PrimaryPriceListId!.Value,
                        IdempotencyKey = settlementIdempotencyKey!
                    });

                Toast.Notify(NotificationSeverity.Success, Loc["Success"], Loc["PeriodSettlement"]);
                await LoadStatusAsync();
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

        private async Task CorrectAsync()
        {
            var reason = correctionReason.Trim();
            if (reason.Length is < 5 or > 500)
            {
                SetAlert(AlertStyle.Warning, Loc["CorrectionReasonLengthWarning"].Value);
                return;
            }
            if (preview is null || !canSettlePreview || string.IsNullOrWhiteSpace(settlementIdempotencyKey))
            {
                SetAlert(AlertStyle.Warning, Loc["CorrectionRerunPreviewWarning"].Value);
                return;
            }

            var confirmed = await DialogService.Confirm(
                string.Format(Loc["CorrectionConfirmFormat"].Value, selectedMonth, selectedYear),
                Loc["CorrectionDialogTitle"].Value,
                new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
            if (confirmed != true)
            {
                return;
            }

            isCorrecting = true;
            try
            {
                var current = await ApiServices.GetFromApiAsync<PeriodSettlementResDTO>(
                    string.Format(Config.RequestApi.PeriodSettlement.Status, selectedYear, selectedMonth));
                if (current?.SettlementId is null)
                {
                    SetAlert(AlertStyle.Warning, Loc["CorrectionCurrentRevisionMissing"].Value);
                    return;
                }

                await ApiServices.PostFromApiAsync<SettlementRevisionResDTO>(
                    string.Format(Config.RequestApi.PeriodSettlement.Correct, current.SettlementId.Value),
                    new SettlementCorrectionReqDTO
                    {
                        Year = selectedYear,
                        Month = selectedMonth,
                        PriceAsOfUtc = preview.PriceAsOfUtc,
                        InputHash = preview.InputHash,
                        PrimarySupplierId = preview.PrimarySupplierId!.Value,
                        PriceListId = preview.PrimaryPriceListId!.Value,
                        IdempotencyKey = settlementIdempotencyKey,
                        Reason = reason
                    });
                Toast.Notify(NotificationSeverity.Success, Loc["Success"], Loc["CorrectionCreated"]);
                correctionReason = string.Empty;
                settlementIdempotencyKey = null;
                await LoadStatusAsync();
                await OnSettled.InvokeAsync();
            }
            catch (Exception ex)
            {
                SetAlert(AlertStyle.Danger, UiErrorMapper.GetMessage(ex, Loc));
            }
            finally
            {
                isCorrecting = false;
            }
        }

        private void SetAlert(AlertStyle style, string message)
        {
            alertStyle = style;
            alertMessage = message;
        }

        private static string DescribeBlocker(string blocker) => PeriodSettlementSupport.DescribeBlocker(blocker);

        private static string FormatMoney(decimal value) => $"{value:N0} VND";
    }
}
