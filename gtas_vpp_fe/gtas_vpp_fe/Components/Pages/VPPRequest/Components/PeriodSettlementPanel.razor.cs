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
        [Inject] private NotificationService NotificationService { get; set; } = default!;
        [Inject] private DialogService DialogService { get; set; } = default!;
        [Parameter] public EventCallback OnSettled { get; set; }

        private List<L07_PriceListResDTO> priceLists = [];
        private Guid? selectedPriceListId;
        private int selectedYear = 2024;
        private int selectedMonth = 1;
        private bool canSettle;
        private bool isLoading;
        private bool isSettling;
        private string? alertMessage;
        private AlertStyle alertStyle = AlertStyle.Info;

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
                priceLists = await ApiServices.GetFromApiAsync<List<L07_PriceListResDTO>>(Config.LibraryApi.L07_PriceList) ?? [];
            }
            catch (Exception ex)
            {
                SetAlert(AlertStyle.Warning, ex.Message);
            }
        }

        private async Task<bool> LoadCurrentPeriodAsync()
        {
            try
            {
                var period = await ApiServices.GetFromApiAsync<VPP_PeriodInfoResDTO>(Config.VppApi.PeriodInfo);
                if (period is null)
                {
                    canSettle = false;
                    SetAlert(AlertStyle.Warning, Loc["Error"].Value);
                    return false;
                }

                // Settlement always targets the just-closed period (Previous), not the
                // still-open one (Current). The current period is where users are still
                // submitting orders — locking it would block normal submissions.
                selectedYear = period.PreviousPeriodYear;
                selectedMonth = period.PreviousPeriodMonth;
                return true;
            }
            catch (Exception ex)
            {
                canSettle = false;
                SetAlert(AlertStyle.Danger, ex.Message);
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
                var status = await ApiServices.GetFromApiAsync<VPP_PeriodSettlementResDTO>(
                    string.Format(Config.RequestApi.PeriodSettlement.Status, selectedYear, selectedMonth));
                ApplyStatus(status);
            }
            catch (Exception ex)
            {
                canSettle = false;
                SetAlert(AlertStyle.Danger, ex.Message);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
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
                await LoadStatusAsync();
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

        private void SetAlert(AlertStyle style, string message)
        {
            alertStyle = style;
            alertMessage = message;
        }
    }
}
