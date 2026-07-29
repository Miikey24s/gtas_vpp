using System.Globalization;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components;

public partial class PeriodSettlementPanel : IDisposable
{
    [Inject] private IAPIServices ApiServices { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private PeriodSettlementState State { get; set; } = default!;

    [Parameter] public int Year { get; set; }
    [Parameter] public int Month { get; set; }
    [Parameter] public EventCallback OnBackToSupply { get; set; }
    [Parameter] public EventCallback OnSettled { get; set; }

    private PeriodSettlementResDTO? status;
    private bool isLoading = true;
    private bool isSettling;
    private bool isCorrecting;
    private bool canCorrect;
    private string correctionReason = string.Empty;
    private int loadedYear;
    private int loadedMonth;

    private SettlementPreviewResDTO? Preview => State.Preview is { } preview
        && preview.Year == Year && preview.Month == Month ? preview : null;
    private bool CanConfirm => !isLoading
        && status?.PendingAdditionalCount == 0
        && Preview is { PrimaryQuote: { IsEligible: true }, Blockers.Count: 0 }
        && !string.IsNullOrWhiteSpace(State.IdempotencyKey);
    private string ShortInputHash => string.IsNullOrWhiteSpace(Preview?.InputHash)
        ? "—"
        : Preview.InputHash[..Math.Min(12, Preview.InputHash.Length)];
    private string StatusCardClass => status switch
    {
        { IsSettled: true } => "is-settled",
        { PendingAdditionalCount: > 0 } => "is-blocked",
        _ when CanConfirm => "is-ready",
        _ => "is-waiting"
    };
    private string StatusTitle => status switch
    {
        null => Loc["CheckingPeriodStatus"].Value,
        { IsSettled: true } => Loc["ReviewHeroSettled"].Value,
        { PendingAdditionalCount: > 0 } => Loc["ReviewHeroBlocked"].Value,
        _ when CanConfirm => Loc["PeriodReadyToSettle"].Value,
        _ => Loc["SupplyRunPreview"].Value
    };
    private string StatusDescription => status switch
    {
        null => Loc["Loading"].Value,
        { IsSettled: true } => string.Format(Loc["Info_AlreadySettled"].Value, DateFormatter.Format(status.SettledAt, DateFormatter.LongDate), status.SettledByUserName ?? "-", status.PriceListName ?? "-"),
        { PendingAdditionalCount: > 0 } => string.Format(Loc["Warning_PendingAdditional"].Value, status.PendingAdditionalCount),
        _ when CanConfirm => Loc["SupplyReadyMessage"].Value,
        _ => Loc["SupplySelectionOrderHint"].Value
    };

    protected override void OnInitialized() => State.Changed += OnStateChanged;

    protected override async Task OnParametersSetAsync()
    {
        if (Year < 2024 || Month is < 1 or > 12 || (loadedYear == Year && loadedMonth == Month)) return;
        loadedYear = Year;
        loadedMonth = Month;
        await LoadStatusAsync();
    }

    private void OnStateChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task LoadStatusAsync()
    {
        isLoading = true;
        try
        {
            status = await ApiServices.GetFromApiAsync<PeriodSettlementResDTO>(
                string.Format(Config.RequestApi.PeriodSettlement.Status, Year, Month));
            canCorrect = status is { IsSettled: true, SettlementId: not null };
        }
        catch (Exception ex)
        {
            status = null;
            canCorrect = false;
            Toast.Error(ex, Loc);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SettleAsync()
    {
        if (!CanConfirm || Preview is null) return;
        var confirmed = await DialogService.Confirm(
            string.Format(Loc["Confirm_Settle"].Value, Month, Year, Preview.PrimaryQuote?.PriceListCode ?? "-"),
            Loc["PeriodSettlement"],
            new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
        if (confirmed != true) return;

        isSettling = true;
        try
        {
            await ApiServices.PostFromApiAsync<SettlementRevisionResDTO>(
                Config.RequestApi.PeriodSettlement.Confirm,
                BuildConfirmRequest(Preview));
            State.CompleteConfirmation();
            Toast.Notify(NotificationSeverity.Success, Loc["Success"], Loc["PeriodSettlement"]);
            await LoadStatusAsync();
            await OnSettled.InvokeAsync();
        }
        catch (Exception ex)
        {
            Toast.Error(ex, Loc);
        }
        finally
        {
            isSettling = false;
        }
    }

    private async Task CorrectAsync()
    {
        if (!CanConfirm || Preview is null || status?.SettlementId is null) return;
        var reason = correctionReason.Trim();
        if (reason.Length is < 5 or > 500)
        {
            Toast.Notify(NotificationSeverity.Warning, Loc["PeriodSettlement"], Loc["CorrectionReasonLengthWarning"]);
            return;
        }

        var confirmed = await DialogService.Confirm(
            string.Format(Loc["CorrectionConfirmFormat"].Value, Month, Year),
            Loc["CorrectionDialogTitle"],
            new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
        if (confirmed != true) return;

        isCorrecting = true;
        try
        {
            var request = new SettlementCorrectionReqDTO
            {
                Year = Year,
                Month = Month,
                PriceAsOfUtc = Preview.PriceAsOfUtc,
                InputHash = Preview.InputHash,
                PrimarySupplierId = Preview.PrimarySupplierId!.Value,
                PriceListId = Preview.PrimaryPriceListId!.Value,
                IdempotencyKey = State.IdempotencyKey!,
                Exceptions = State.Exceptions.Select(CloneException).ToList(),
                Reason = reason
            };
            await ApiServices.PostFromApiAsync<SettlementRevisionResDTO>(
                string.Format(Config.RequestApi.PeriodSettlement.Correct, status.SettlementId.Value),
                request);
            State.CompleteConfirmation();
            correctionReason = string.Empty;
            Toast.Notify(NotificationSeverity.Success, Loc["Success"], Loc["CorrectionCreated"]);
            await LoadStatusAsync();
            await OnSettled.InvokeAsync();
        }
        catch (Exception ex)
        {
            Toast.Error(ex, Loc);
        }
        finally
        {
            isCorrecting = false;
        }
    }

    private SettlementConfirmReqDTO BuildConfirmRequest(SettlementPreviewResDTO preview) => new()
    {
        Year = Year,
        Month = Month,
        PriceAsOfUtc = preview.PriceAsOfUtc,
        InputHash = preview.InputHash,
        PrimarySupplierId = preview.PrimarySupplierId!.Value,
        PriceListId = preview.PrimaryPriceListId!.Value,
        IdempotencyKey = State.IdempotencyKey!,
        Exceptions = State.Exceptions.Select(CloneException).ToList()
    };

    private static SettlementExceptionReqDTO CloneException(SettlementExceptionReqDTO source) => new()
    {
        VppId = source.VppId,
        SupplierId = source.SupplierId,
        Reason = source.Reason?.Trim()
    };

    private static string FormatMoney(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    public void Dispose() => State.Changed -= OnStateChanged;
}
