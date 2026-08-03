using System.Globalization;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Features.Settlement.Api;
using gtas_vpp_fe.Features.Settlement.Projection;
using gtas_vpp_fe.Features.Settlement.State;
using gtas_vpp_fe.Features.Settlement.Submission;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components;

public partial class PeriodSettlementPanel : IDisposable
{
    private const string CurrentPeriodScope = "current";
    private const string PreviousPeriodScope = "previous";
    private const string CustomPeriodScope = "custom";
    private const string ItemsView = "items";
    private const string DepartmentsView = "departments";

    [Inject] private SettlementApiClient Settlement { get; set; } = default!;
    [Inject] private CatalogApiClient Catalog { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private PeriodSettlementState State { get; set; } = default!;

    [Parameter] public int Year { get; set; }
    [Parameter] public int Month { get; set; }
    [Parameter] public int CurrentPeriodYear { get; set; }
    [Parameter] public int CurrentPeriodMonth { get; set; }
    [Parameter] public int PreviousPeriodYear { get; set; }
    [Parameter] public int PreviousPeriodMonth { get; set; }
    [Parameter] public EventCallback<PeriodTargetSelection> PeriodChanged { get; set; }
    [Parameter] public EventCallback SettlementChanged { get; set; }

    private CancellationTokenSource? searchDebounce;
    private List<VppRequestResDTO> periodOrdersSnapshot = [];
    private List<DepartmentResDTO> departmentDirectory = [];
    private AggregatedVppResDTO? periodDemand;
    private PeriodSettlementResDTO? status;
    private bool isLoading = true;
    private bool isGridLoading;
    private bool isPreviewLoading;
    private bool isSettling;
    private bool isCorrecting;
    private VppFileExportFormat? exportingSettlementFormat;
    private bool hasCorrectionTarget;
    private bool showCustomPeriodPicker;
    private bool hasExplicitSupplierSelection;
    private bool hasExplicitPriceListSelection;
    private string? alertMessage;
    private string periodScope = CurrentPeriodScope;
    private string pendingCustomPeriod = string.Empty;
    private string viewMode = DepartmentsView;
    private string searchText = string.Empty;
    private string selectedOrderType = string.Empty;
    private int? selectedStatus;
    private string selectedDepartment = string.Empty;
    private string selectedItemCategory = string.Empty;
    private string selectedItemUom = string.Empty;
    private int loadedYear;
    private int loadedMonth;

    private SettlementPreviewResDTO? Preview => State.Preview is { } preview
        && preview.Year == Year && preview.Month == Month ? preview : null;

    private bool CanSubmitCurrentPreview => !isLoading
        && !isPreviewLoading
        && status?.PendingAdditionalCount == 0
        && Preview is { PrimaryQuote: { IsEligible: true }, Blockers.Count: 0 }
        && !string.IsNullOrWhiteSpace(State.IdempotencyKey);

    private bool CanExportSettlement => status is { IsSettled: true, SettlementId: not null };
    private string SettlementStatusText => status?.IsSettled == true
        ? Loc["Settled"].Value
        : Loc["NotSettled"].Value;
    private VppStatusTone SettlementStatusTone => status?.IsSettled == true
        ? VppStatusTone.Success
        : VppStatusTone.Neutral;
    private string SettlementStatusTitle => status?.IsSettled == true
        ? string.Format(
            Loc["Info_AlreadySettled"].Value,
            DateFormatter.Format(status.SettledAt, DateFormatter.LongDate),
            status.SettledByUserName ?? "-",
            status.PriceListName ?? "-")
        : Loc["NotSettled"].Value;

    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText)
        || (viewMode == ItemsView
            ? !string.IsNullOrWhiteSpace(selectedItemCategory) || !string.IsNullOrWhiteSpace(selectedItemUom)
            : !string.IsNullOrWhiteSpace(selectedOrderType) || selectedStatus.HasValue || !string.IsNullOrWhiteSpace(selectedDepartment));

    private bool HasPeriodBlockers => Preview?.Blockers.Count > 0 || status?.PendingAdditionalCount > 0;
    private string SettlementPageClass => HasPeriodBlockers
        ? "vpp-period-settlement-page has-blockers"
        : "vpp-period-settlement-page";

    private string FooterConditionText
    {
        get
        {
            if (status?.PendingAdditionalCount > 0)
            {
                return string.Format(Loc["Warning_PendingAdditional"].Value, status.PendingAdditionalCount);
            }

            if (Preview?.Blockers.FirstOrDefault() is { } blocker)
            {
                return PeriodSettlementSupport.DescribeBlocker(blocker);
            }

            if (status?.IsSettled == true)
            {
                return string.Format(
                    Loc["Info_AlreadySettled"].Value,
                    DateFormatter.Format(status.SettledAt, DateFormatter.LongDate),
                    status.SettledByUserName ?? "-",
                    status.PriceListName ?? "-");
            }

            return CanSubmitCurrentPreview
                ? Loc["SettlementReadyCondition"].Value
                : Loc["SettlementSelectSupplierCondition"].Value;
        }
    }

    private IReadOnlyList<PriceBookQuoteResDTO> SupplierQuotes => Preview?.Quotes
        .OrderBy(quote => quote.Rank)
        .ToArray() ?? [];

    private Guid? SelectedSupplierId => Preview?.PrimarySupplierId;
    private Guid? SelectedPriceListId => Preview?.PrimaryPriceListId;

    private IReadOnlyList<VppFilterOption<Guid?>> SupplierOptions => SupplierQuotes
        .Where(quote => quote.IsEligible)
        .GroupBy(quote => quote.SupplierId)
        .Select(group => group.OrderBy(quote => quote.Rank).First())
        .OrderBy(quote => quote.Rank)
        .Select(quote => new VppFilterOption<Guid?>(quote.SupplierId, quote.SupplierName ?? Loc["SettlementNotSelected"]))
        .ToArray();

    private IReadOnlyList<VppFilterOption<Guid?>> PriceListOptions => SupplierQuotes
        .Where(quote => quote.IsEligible && quote.SupplierId == SelectedSupplierId)
        .OrderBy(quote => quote.Rank)
        .Select(quote => new VppFilterOption<Guid?>(quote.PriceListId, $"{quote.PriceListCode} · v{quote.Version}"))
        .ToArray();

    private IReadOnlyList<VppSegmentedOption<string>> PeriodScopeOptions =>
    [
        new(PreviousPeriodScope, Loc["PreviousOrderPeriod"]),
        new(CurrentPeriodScope, Loc["SettlementCurrentPeriod"]),
        new(CustomPeriodScope, Loc["SettlementCustomPeriod"])
    ];

    private string DisplayedPeriodScope => showCustomPeriodPicker ? CustomPeriodScope : periodScope;
    private string CustomPeriodSummary => $"{Loc["Period"]} {Month:00}/{Year}";
    private string CurrentViewLabel => ViewModeOptions
        .First(option => string.Equals(option.Value, viewMode, StringComparison.Ordinal))
        .Label;

    private IReadOnlyList<VppSegmentedOption<string>> ViewModeOptions =>
    [
        new(DepartmentsView, Loc["SettlementByDepartment"]),
        new(ItemsView, Loc["SettlementByItem"])
    ];

    private IReadOnlyList<VppFilterOption<string>> OrderTypeOptions =>
    [
        new(string.Empty, Loc["HistoryAllOrderTypes"]),
        new("regular", Loc["Regular"]),
        new("additional", Loc["AdditionalOrder"])
    ];

    private IReadOnlyList<VppFilterOption<int?>> StatusOptions =>
    [
        new(null, Loc["HistoryAllStatuses"]),
        new(1, Loc["Submitted"]),
        new(4, Loc["Cancelled"]),
        new(6, Loc["Pending"]),
        new(7, Loc["Approved"]),
        new(8, Loc["Rejected"])
    ];

    private IReadOnlyList<VppFilterOption<string>> DepartmentOptions =>
        new[] { new VppFilterOption<string>(string.Empty, Loc["SettlementAllDepartments"]) }
            .Concat(SettlementWorkspaceProjection
                .BuildDepartmentOptions(periodOrdersSnapshot, departmentDirectory)
                .Select(option => new VppFilterOption<string>(option.Code, option.Name)))
            .ToArray();

    private IReadOnlyList<VppFilterOption<string>> ItemCategoryOptions =>
        new[] { new VppFilterOption<string>(string.Empty, Loc["AllCategories"]) }
            .Concat(SettlementWorkspaceProjection
                .GetDistinctItemCategories(periodDemand?.Items ?? [])
                .Select(value => new VppFilterOption<string>(value, value)))
            .ToArray();

    private IReadOnlyList<VppFilterOption<string>> ItemUomOptions =>
        new[] { new VppFilterOption<string>(string.Empty, Loc["AllUnits"]) }
            .Concat(SettlementWorkspaceProjection
                .GetDistinctItemUnits(periodDemand?.Items ?? [])
                .Select(value => new VppFilterOption<string>(value, value)))
            .ToArray();

    private List<AggregatedVppItemResDTO> FilteredItemRows =>
        SettlementWorkspaceProjection.FilterItems(
            periodDemand?.Items ?? [],
            new SettlementItemFilter(searchText, selectedItemCategory, selectedItemUom));

    private List<DepartmentSettlementRow> FilteredDepartmentRows =>
        SettlementWorkspaceProjection.BuildDepartmentRows(
                periodOrdersSnapshot,
                departmentDirectory,
                new SettlementDepartmentFilter(
                    searchText,
                    selectedOrderType,
                    selectedStatus,
                    selectedDepartment))
            .Select(ToDepartmentSettlementRow)
            .ToList();

    protected override void OnInitialized() => State.Changed += OnStateChanged;

    protected override async Task OnParametersSetAsync()
    {
        if (Year < 2024 || Month is < 1 or > 12 || (loadedYear == Year && loadedMonth == Month))
        {
            return;
        }

        loadedYear = Year;
        loadedMonth = Month;
        periodScope = ResolvePeriodScope(Year, Month);
        await ReloadPeriodAsync();
    }

    private void OnStateChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task ReloadPeriodAsync()
    {
        isLoading = true;
        isGridLoading = true;
        alertMessage = null;
        hasExplicitSupplierSelection = false;
        hasExplicitPriceListSelection = false;

        try
        {
            await LoadStatusAsync();
            await LoadPreviewAsync();
            await LoadPeriodDemandAsync();
            await LoadDepartmentDirectoryAsync();
            await LoadAllPeriodOrdersAsync();
        }
        catch (Exception ex)
        {
            alertMessage = UiErrorMapper.GetMessage(ex, Loc);
            Toast.Error(ex, Loc);
        }
        finally
        {
            isGridLoading = false;
            isLoading = false;
        }
    }

    private async Task LoadStatusAsync()
    {
        status = await Settlement.GetStatusAsync(Year, Month);
        hasCorrectionTarget = status is { IsSettled: true, SettlementId: not null };
    }

    private async Task LoadPreviewAsync(Guid? supplierId = null, Guid? priceListId = null)
    {
        isPreviewLoading = true;
        try
        {
            var preview = await Settlement.PreviewAsync(SettlementRequestFactory.BuildPreview(
                Year,
                Month,
                supplierId,
                priceListId,
                DateTime.UtcNow,
                State.Exceptions));

            State.SelectedSupplierId = preview?.PrimarySupplierId;
            State.SetPreview(preview);
        }
        finally
        {
            isPreviewLoading = false;
        }
    }

    private async Task LoadAllPeriodOrdersAsync()
    {
        periodOrdersSnapshot = (await Settlement.GetPeriodOrdersSnapshotAsync(Year, Month)).ToList();
    }

    private async Task LoadPeriodDemandAsync()
    {
        periodDemand = await Settlement.GetDemandAsync(Year, Month);
    }

    private async Task LoadDepartmentDirectoryAsync()
    {
        departmentDirectory = await Catalog.GetActiveDepartmentsAsync(1000, "Name") ?? [];
    }

    private DepartmentSettlementRow ToDepartmentSettlementRow(SettlementDepartmentRow row)
    {
        var statusValue = row.Status switch
        {
            SettlementDepartmentStatus.Pending => (Loc["Pending"].Value, VppStatusTone.Warning),
            SettlementDepartmentStatus.Approved => (Loc["Approved"].Value, VppStatusTone.Success),
            SettlementDepartmentStatus.NeedsReview => (Loc["SettlementNeedsReview"].Value, VppStatusTone.Warning),
            _ => (Loc["Submitted"].Value, VppStatusTone.Info)
        };

        return new DepartmentSettlementRow(
            row.DepartmentCode,
            row.DepartmentName,
            row.OrderCount,
            row.RegularOrderCount,
            row.AdditionalOrderCount,
            row.TotalLines,
            row.TotalQuantity,
            row.TotalAmount,
            statusValue.Item1,
            statusValue.Item2);
    }

    private async Task OnSearchInputAsync(ChangeEventArgs args)
    {
        searchText = args.Value?.ToString() ?? string.Empty;
        searchDebounce?.Cancel();
        searchDebounce?.Dispose();
        searchDebounce = new CancellationTokenSource();
        try
        {
            await Task.Delay(280, searchDebounce.Token);
            await ApplyFiltersAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private Task OnOrderTypeChangedAsync(string value)
    {
        selectedOrderType = value;
        return ApplyFiltersAsync();
    }

    private Task OnStatusChangedAsync(int? value)
    {
        selectedStatus = value;
        return ApplyFiltersAsync();
    }

    private Task OnDepartmentChangedAsync(string value)
    {
        selectedDepartment = value;
        return ApplyFiltersAsync();
    }

    private Task OnItemCategoryChangedAsync(string value)
    {
        selectedItemCategory = value;
        return ApplyFiltersAsync();
    }

    private Task OnItemUomChangedAsync(string value)
    {
        selectedItemUom = value;
        return ApplyFiltersAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearFiltersAsync()
    {
        searchText = string.Empty;
        selectedOrderType = string.Empty;
        selectedStatus = null;
        selectedDepartment = string.Empty;
        selectedItemCategory = string.Empty;
        selectedItemUom = string.Empty;
        await ApplyFiltersAsync();
    }

    private async Task OnPeriodScopeChangedAsync(string scope)
    {
        if (scope == CustomPeriodScope)
        {
            pendingCustomPeriod = $"{Year:0000}-{Month:00}";
            showCustomPeriodPicker = true;
            return;
        }

        showCustomPeriodPicker = false;
        periodScope = scope;
        var target = scope == PreviousPeriodScope
            ? new PeriodTargetSelection(PreviousPeriodYear, PreviousPeriodMonth)
            : new PeriodTargetSelection(CurrentPeriodYear, CurrentPeriodMonth);
        if (target.Year < 2024 || target.Month is < 1 or > 12)
        {
            return;
        }

        await PeriodChanged.InvokeAsync(target);
    }

    private void OnCustomPeriodChanged(ChangeEventArgs args)
    {
        pendingCustomPeriod = args.Value?.ToString() ?? string.Empty;
    }

    private async Task ApplyCustomPeriodAsync()
    {
        if (!DateTime.TryParseExact(
                pendingCustomPeriod,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var period))
        {
            return;
        }

        periodScope = CustomPeriodScope;
        showCustomPeriodPicker = false;
        await PeriodChanged.InvokeAsync(new PeriodTargetSelection(period.Year, period.Month));
    }

    private void DismissCustomPeriod() => showCustomPeriodPicker = false;

    private async Task OnViewModeChangedAsync(string mode)
    {
        viewMode = mode;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnSupplierChangedAsync(Guid? supplierId)
    {
        if (!supplierId.HasValue || isPreviewLoading)
        {
            return;
        }

        var quote = SupplierQuotes
            .Where(item => item.SupplierId == supplierId.Value && item.IsEligible)
            .OrderBy(item => item.Rank)
            .FirstOrDefault();
        if (quote is not null)
        {
            hasExplicitSupplierSelection = true;
            hasExplicitPriceListSelection = false;
            await ApplySupplierQuoteAsync(quote);
        }
    }

    private async Task OnPriceListChangedAsync(Guid? priceListId)
    {
        if (!priceListId.HasValue || isPreviewLoading)
        {
            return;
        }

        var quote = SupplierQuotes.FirstOrDefault(item => item.PriceListId == priceListId.Value && item.IsEligible);
        if (quote is not null)
        {
            hasExplicitPriceListSelection = true;
            await ApplySupplierQuoteAsync(quote);
        }
    }

    private async Task OpenCorrectionDialog()
    {
        var reason = await DialogService.OpenAsync<Dialog_SettlementCorrection>(
            Loc["CorrectionDialogTitle"].Value,
            new Dictionary<string, object?>(),
            VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Compact,
                Loc["CorrectionDialogTitle"].Value,
                closeAriaLabel: Loc["Close"].Value));

        if (reason is string correctionReason)
        {
            await CorrectAsync(correctionReason);
        }
    }

    private bool IsCurrentQuote(PriceBookQuoteResDTO quote) => Preview?.PrimaryPriceListId == quote.PriceListId;

    private bool IsItemCovered(AggregatedVppItemResDTO item) =>
        Preview?.PrimaryQuote is { } quote && !quote.MissingVppIds.Contains(item.VppId);

    private async Task ApplySupplierQuoteAsync(PriceBookQuoteResDTO quote)
    {
        if (!quote.IsEligible || isPreviewLoading || IsCurrentQuote(quote))
        {
            return;
        }

        if (Preview?.PrimaryPriceListId != quote.PriceListId)
        {
            State.Exceptions.Clear();
        }
        await LoadPreviewAsync(quote.SupplierId, quote.PriceListId);
    }

    private async Task SettleAsync()
    {
        if (!CanSubmitCurrentPreview || Preview is null)
        {
            return;
        }

        var confirmed = await DialogService.Confirm(
            string.Format(Loc["Confirm_Settle"].Value, Month, Year, Preview.PrimaryQuote?.PriceListCode ?? "-"),
            Loc["PeriodSettlement"],
            new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
        if (confirmed != true)
        {
            return;
        }

        isSettling = true;
        try
        {
            await Settlement.ConfirmAsync(SettlementRequestFactory.BuildConfirm(
                Year,
                Month,
                Preview,
                State.IdempotencyKey!,
                State.Exceptions));
            await LoadStatusAsync();
            State.RequireFreshPreviewForNextSubmission();
            Toast.Notify(NotificationSeverity.Success, Loc["Success"], Loc["PeriodSettlement"]);
            await SettlementChanged.InvokeAsync();
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

    private async Task ExportSettlementAsync(VppFileExportFormat format)
    {
        if (exportingSettlementFormat.HasValue || !CanExportSettlement)
        {
            return;
        }

        var settlementId = status!.SettlementId!.Value;
        exportingSettlementFormat = format;
        try
        {
            var result = await Settlement.ExportAsync(settlementId, format);
            Toast.Success(Loc["PeriodSettlement"], Loc["ExportCompleted", result.FileName, FileSizeFormatter.Format(result.Size)]);
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["PeriodSettlement"], UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            exportingSettlementFormat = null;
        }
    }

    private async Task CorrectAsync(string correctionReason)
    {
        if (!CanSubmitCurrentPreview || Preview is null || status?.SettlementId is null)
        {
            return;
        }

        var reason = correctionReason.Trim();
        if (reason.Length is < 5 or > 500)
        {
            Toast.Notify(NotificationSeverity.Warning, Loc["PeriodSettlement"], Loc["CorrectionReasonLengthWarning"]);
            return;
        }

        isCorrecting = true;
        try
        {
            await Settlement.CorrectAsync(
                status.SettlementId.Value,
                SettlementRequestFactory.BuildCorrection(
                    Year,
                    Month,
                    Preview,
                    State.IdempotencyKey!,
                    State.Exceptions,
                    reason));
            await LoadStatusAsync();
            State.RequireFreshPreviewForNextSubmission();
            Toast.Notify(NotificationSeverity.Success, Loc["Success"], Loc["CorrectionCreated"]);
            await SettlementChanged.InvokeAsync();
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

    private string ResolvePeriodScope(int year, int month)
    {
        if (year == CurrentPeriodYear && month == CurrentPeriodMonth)
        {
            return CurrentPeriodScope;
        }

        if (year == PreviousPeriodYear && month == PreviousPeriodMonth)
        {
            return PreviousPeriodScope;
        }

        return CustomPeriodScope;
    }
    private static string FormatMoney(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    private static string FormatMoney(long value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    public void Dispose()
    {
        State.Changed -= OnStateChanged;
        searchDebounce?.Cancel();
        searchDebounce?.Dispose();
    }

    private sealed record DepartmentSettlementRow(
        string DepartmentCode,
        string DepartmentName,
        int OrderCount,
        int RegularOrderCount,
        int AdditionalOrderCount,
        int TotalLines,
        int TotalQuantity,
        long TotalAmount,
        string StatusText,
        VppStatusTone StatusTone);
}

public sealed record PeriodTargetSelection(int Year, int Month);
