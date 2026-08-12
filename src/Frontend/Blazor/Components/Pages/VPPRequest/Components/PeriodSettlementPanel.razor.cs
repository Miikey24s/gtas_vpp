// PAGE LOGIC: VPPRequest/Components/PeriodSettlementPanel.razor.cs
using System.Globalization;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Features.Settlement.Api;
using gtas_vpp_fe.Features.Settlement.Projection;
using gtas_vpp_fe.Features.Settlement.State;
using gtas_vpp_fe.Features.Settlement.Submission;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components;

public partial class PeriodSettlementPanel : IDisposable
{
    // PERIOD STATE: Kỳ mục tiêu, chế độ xem và các bộ lọc hiện tại.
    private const string CurrentPeriodScope = "current";
    private const string PreviousPeriodScope = "previous";
    private const string CustomPeriodScope = "custom";
    private const string ItemsView = "items";
    private const string DepartmentsView = "departments";
    private const string RequestersView = "requesters";

    // DEPENDENCIES: API chốt kỳ, catalog, dialog, toast và state dùng chung.
    [Inject] private SettlementApiClient Settlement { get; set; } = default!;
    [Inject] private OrderPeriodApiClient Periods { get; set; } = default!;
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

    // DATA STATE: Snapshot đơn hàng, trạng thái chốt, preview và cờ loading.
    private CancellationTokenSource? searchDebounce;
    private List<VppRequestResDTO> periodOrdersSnapshot = [];
    private List<DepartmentResDTO> departmentDirectory = [];
    private bool hasLoadedDepartmentDirectory;
    private bool hasLoadedPeriodOrders;
    private bool hasLoadedPeriodDemand;
    private List<AggregatedVppItemResDTO> filteredItemRows = [];
    private Dictionary<Guid, SettlementItemFinancialView> itemFinancials = [];
    private List<DepartmentSettlementRow> filteredDepartmentRows = [];
    private List<RequesterSettlementRow> filteredRequesterRows = [];
    private AggregatedVppResDTO? periodDemand;
    private PeriodSettlementResDTO? status;
    private bool isLoading = true;
    private bool isGridLoading;
    private bool isPreviewLoading;
    private bool isSettling;
    private bool isCorrectingSettlement;
    private bool isLoadingSettlementHistory;
    private VppFileExportFormat? exportingSettlementFormat;
    private bool hasCorrectionTarget;
    private SettlementRevisionResDTO? currentSettlementRevision;
    private VppManagedPeriodResDTO? managedPeriod;
    private bool showCustomPeriodPicker;
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

    // VALIDATION: Điều kiện cho phép chốt hoặc xuất dữ liệu kỳ.
    private SettlementPreviewResDTO? Preview => State.Preview is { } preview
        && preview.Year == Year && preview.Month == Month ? preview : null;

    private bool CanSubmitCurrentPreview => !isLoading
        && !isPreviewLoading
        && (hasCorrectionTarget
            || managedPeriod?.State is "SubmissionClosed" or "Pricing")
        && status?.PendingAdditionalCount == 0
        && Preview is { PrimaryQuote: { IsEligible: true }, Blockers.Count: 0 }
        && !string.IsNullOrWhiteSpace(State.IdempotencyKey);

    private bool CanExportSettlement => status is { IsSettled: true, SettlementId: not null };
    private bool CanCorrectSettlement => status is { IsSettled: true, SettlementId: not null }
        && currentSettlementRevision is { IsCurrentRevision: true, HasExternalProcurementImpact: false }
        && CanSubmitCurrentPreview
        && !isLoadingSettlementHistory
        && !isCorrectingSettlement;
    private string SettlementVersionText => currentSettlementRevision is { RevisionNumber: > 0 } revision
        ? string.Format(Loc["SettlementVersionLabel"], revision.RevisionNumber)
        : string.Empty;
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

    private string CurrentFilterClass => viewMode switch
    {
        ItemsView => "is-item-view",
        RequestersView => "is-requester-view",
        _ => "is-department-view"
    };

    private string CurrentSearchPlaceholder => viewMode switch
    {
        ItemsView => Loc["SearchCatalogItems"],
        RequestersView => Loc["SettlementRequesterSearchPlaceholder"],
        _ => Loc["SettlementDepartmentSearchPlaceholder"]
    };

    private bool HasPeriodBlockers => Preview?.Blockers.Count > 0 || status?.PendingAdditionalCount > 0;
    private bool RequiresFreshPreview => hasCorrectionTarget && State.RequiresFreshPreviewForSubmission;
    private bool HasPeriodFeedback => HasPeriodBlockers || RequiresFreshPreview;
    private string SettlementPageClass => HasPeriodFeedback
        ? "vpp-period-settlement-page has-feedback"
        : "vpp-period-settlement-page";

    private string FooterConditionText
    {
        get
        {
            if (status?.PendingAdditionalCount > 0)
            {
                return string.Format(Loc["Warning_PendingAdditional"].Value, status.PendingAdditionalCount);
            }

            if (!hasCorrectionTarget && managedPeriod?.State == "Open")
            {
                return Loc["SettlementAvailableAfterClosing"];
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
    private PriceBookQuoteResDTO? SelectedQuote => Preview?.PrimaryQuote;
    private bool HasSettlementSnapshot => status?.IsSettled == true && currentSettlementRevision is not null;
    private string SelectedSupplierName => HasSettlementSnapshot
        ? currentSettlementRevision!.PrimarySupplierName
        : SelectedQuote?.SupplierName ?? status?.PrimarySupplierName ?? Loc["SettlementNotSelected"];
    private decimal SettlementGrandTotal => HasSettlementSnapshot
        ? currentSettlementRevision!.GrandTotal
        : SelectedQuote?.GrandTotal ?? status?.GrandTotal ?? 0;
    private decimal SettlementVatAmount => HasSettlementSnapshot
        ? currentSettlementRevision!.VatAmount
        : SelectedQuote?.VatAmount ?? 0;
    private decimal SettlementBeforeVat => SettlementGrandTotal - SettlementVatAmount;
    private IReadOnlyList<SettlementFinancialAllocationResDTO> CurrentFinancialAllocations => HasSettlementSnapshot
        ? currentSettlementRevision!.Allocations
        : Preview?.Allocations ?? [];

    private IReadOnlyList<VppDecisionOption<Guid?>> SupplierOptions => SupplierQuotes
        .Where(quote => quote.IsEligible)
        .GroupBy(quote => quote.SupplierId)
        .Select(group => group.OrderBy(quote => quote.Rank).First())
        .OrderBy(quote => quote.Rank)
        .Select(quote => new VppDecisionOption<Guid?>(quote.SupplierId, quote.SupplierName ?? Loc["SettlementNotSelected"]))
        .ToArray();

    private IReadOnlyList<VppDecisionOption<Guid?>> PriceListOptions => SupplierQuotes
        .Where(quote => quote.IsEligible && quote.SupplierId == SelectedSupplierId)
        .OrderBy(quote => quote.Rank)
        .Select(quote => new VppDecisionOption<Guid?>(quote.PriceListId, quote.PriceListCode ?? "–"))
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
        new(RequestersView, Loc["SettlementByRequester"]),
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

    private List<AggregatedVppItemResDTO> FilteredItemRows => filteredItemRows;
    private List<DepartmentSettlementRow> FilteredDepartmentRows => filteredDepartmentRows;
    private List<RequesterSettlementRow> FilteredRequesterRows => filteredRequesterRows;

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
        periodOrdersSnapshot = [];
        periodDemand = null;
        hasLoadedPeriodOrders = false;
        hasLoadedPeriodDemand = false;
        filteredItemRows = [];
        itemFinancials = [];
        filteredDepartmentRows = [];
        filteredRequesterRows = [];
        pendingPostSettlementCorrections = [];
        currentSettlementRevision = null;

        var headerTask = Task.WhenAll(LoadStatusAsync(), LoadPreviewAsync());
        var gridTask = EnsureCurrentViewDataAsync();

        try
        {
            await headerTask;
        }
        catch (Exception ex)
        {
            HandleLoadError(ex);
        }
        finally
        {
            isLoading = false;
            await InvokeAsync(StateHasChanged);
        }

        try
        {
            await gridTask;
        }
        catch (Exception ex)
        {
            HandleLoadError(ex);
        }
        finally
        {
            isGridLoading = false;
            await InvokeAsync(StateHasChanged);
        }

        if (status is { IsSettled: true })
        {
            await LoadDeferredSettlementAdministrationAsync();
        }
    }

    private async Task LoadStatusAsync()
    {
        var statusTask = Settlement.GetStatusAsync(Year, Month);
        var periodsTask = Periods.ListAsync();
        await Task.WhenAll(statusTask, periodsTask);

        status = await statusTask;
        hasCorrectionTarget = status is { IsSettled: true, SettlementId: not null };
        managedPeriod = (await periodsTask)
            .FirstOrDefault(period => period.Year == Year && period.Month == Month);
    }

    private async Task LoadDeferredSettlementAdministrationAsync()
    {
        try
        {
            var revisionTask = hasCorrectionTarget
                ? Settlement.GetCurrentAsync(Year, Month)
                : Task.FromResult<SettlementRevisionResDTO?>(null);
            var correctionsTask = LoadPostSettlementCorrectionsAsync();
            await Task.WhenAll(revisionTask, correctionsTask);
            currentSettlementRevision = await revisionTask;
            RebuildItemFinancials();
            RebuildCurrentViewRows();
        }
        catch (Exception)
        {
            // Lịch sử bản chốt và yêu cầu điều chỉnh là phần quản trị phụ; lỗi ở đây không được
            // báo thất bại cho grid đã tải thành công hoặc chặn luồng chốt kỳ chính.
            currentSettlementRevision = null;
            pendingPostSettlementCorrections = [];
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OpenSettlementCorrectionDialogAsync()
    {
        if (!CanCorrectSettlement
            || status?.SettlementId is null
            || currentSettlementRevision is null
            || Preview is null)
        {
            return;
        }

        var reason = await DialogService.OpenAsync<Dialog_SettlementCorrection>(
            Loc["AdjustSettlementResult"],
            options: VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Compact,
                Loc["AdjustSettlementResult"],
                closeAriaLabel: Loc["Close"]));
        if (reason is not string correctionReason)
        {
            return;
        }

        isCorrectingSettlement = true;
        try
        {
            var revision = await Settlement.CorrectAsync(
                status.SettlementId.Value,
                SettlementRequestFactory.BuildCorrection(
                    Year,
                    Month,
                    Preview,
                    $"adjust-{Year:D4}{Month:D2}-{Guid.NewGuid():N}",
                    State.Exceptions,
                    correctionReason));
            State.RequireFreshPreviewForNextSubmission();
            await ReloadPeriodAsync();
            await SettlementChanged.InvokeAsync();
            Toast.Success(
                Loc["AdjustmentSaved"],
                string.Format(Loc["SettlementVersionSaved"], revision?.RevisionNumber ?? 0));
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["AdjustSettlementResult"], UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            isCorrectingSettlement = false;
        }
    }

    private async Task OpenSettlementHistoryDialogAsync()
    {
        if (status?.IsSettled != true || isLoadingSettlementHistory)
        {
            return;
        }

        isLoadingSettlementHistory = true;
        try
        {
            var versions = await Settlement.ListVersionsAsync(Year, Month);
            await DialogService.OpenAsync<Dialog_SettlementHistory>(
                Loc["SavedSettlementVersions"],
                new Dictionary<string, object?>
                {
                    [nameof(Dialog_SettlementHistory.Versions)] = versions
                },
                VppAdminDialogProfiles.Create(
                    VppAdminDialogSize.Standard,
                    Loc["SavedSettlementVersions"],
                    closeAriaLabel: Loc["Close"]));
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["SavedSettlementVersions"], UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            isLoadingSettlementHistory = false;
        }
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
            RebuildItemFinancials();
            if (hasLoadedPeriodOrders)
            {
                RebuildCurrentViewRows();
            }
        }
        finally
        {
            isPreviewLoading = false;
        }
    }

    private async Task LoadAllPeriodOrdersAsync()
    {
        if (hasLoadedPeriodOrders)
        {
            return;
        }

        periodOrdersSnapshot = (await Settlement.GetPeriodOrdersSnapshotAsync(Year, Month)).ToList();
        hasLoadedPeriodOrders = true;
    }

    private async Task LoadPeriodDemandAsync()
    {
        if (hasLoadedPeriodDemand)
        {
            return;
        }

        periodDemand = await Settlement.GetDemandAsync(Year, Month);
        hasLoadedPeriodDemand = true;
    }

    private async Task LoadDepartmentDirectoryAsync()
    {
        if (hasLoadedDepartmentDirectory)
        {
            return;
        }

        departmentDirectory = await Catalog.GetActiveDepartmentsAsync(1000, "Name") ?? [];
        hasLoadedDepartmentDirectory = true;
    }

    private Task EnsureCurrentViewDataAsync()
    {
        return LoadCurrentViewDataAndProjectionAsync();
    }

    private async Task LoadCurrentViewDataAndProjectionAsync()
    {
        var ordersTask = LoadAllPeriodOrdersAsync();
        var viewTask = viewMode switch
        {
            ItemsView => Task.WhenAll(ordersTask, LoadPeriodDemandAsync()),
            _ => Task.WhenAll(ordersTask, LoadDepartmentDirectoryAsync())
        };
        await viewTask;
        RebuildCurrentViewRows();
    }

    private void HandleLoadError(Exception ex)
    {
        alertMessage ??= UiErrorMapper.GetMessage(ex, Loc);
        Toast.Error(ex, Loc);
    }

    private DepartmentSettlementRow ToDepartmentSettlementRow(SettlementDepartmentRow row)
    {
        var statusValue = ResolveGroupStatus(row.Status);

        return new DepartmentSettlementRow(
            row.DepartmentCode,
            row.DepartmentName,
            row.OrderCount,
            row.RegularOrderCount,
            row.AdditionalOrderCount,
            row.TotalLines,
            row.TotalQuantity,
            row.TotalAmount,
            row.NetAmount,
            row.VatAmount,
            row.GrossAmount,
            statusValue.Item1,
            statusValue.Item2);
    }

    private RequesterSettlementRow ToRequesterSettlementRow(SettlementRequesterRow row)
    {
        var statusValue = ResolveGroupStatus(row.Status);
        return new RequesterSettlementRow(
            row.UserId,
            row.RequesterName,
            row.DepartmentSummary,
            row.OrderCount,
            row.RegularOrderCount,
            row.AdditionalOrderCount,
            row.TotalLines,
            row.TotalQuantity,
            row.TotalAmount,
            row.NetAmount,
            row.VatAmount,
            row.GrossAmount,
            statusValue.Item1,
            statusValue.Item2);
    }

    private (string Text, VppStatusTone Tone) ResolveGroupStatus(SettlementOrderGroupStatus status) => status switch
    {
        SettlementOrderGroupStatus.Pending => (Loc["Pending"].Value, VppStatusTone.Warning),
        SettlementOrderGroupStatus.Approved => (Loc["Approved"].Value, VppStatusTone.Success),
        SettlementOrderGroupStatus.NeedsReview => (Loc["SettlementNeedsReview"].Value, VppStatusTone.Warning),
        _ => (Loc["Submitted"].Value, VppStatusTone.Info)
    };

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
        RebuildCurrentViewRows();
        await InvokeAsync(StateHasChanged);
    }

    private void RebuildCurrentViewRows()
    {
        switch (viewMode)
        {
            case ItemsView:
                filteredItemRows = SettlementWorkspaceProjection.FilterItems(
                    periodDemand?.Items ?? [],
                    new SettlementItemFilter(searchText, selectedItemCategory, selectedItemUom));
                break;
            case RequestersView:
                filteredRequesterRows = SettlementWorkspaceProjection.BuildRequesterRows(
                        periodOrdersSnapshot,
                        departmentDirectory,
                        new SettlementOrderGroupFilter(
                            searchText,
                             selectedOrderType,
                             selectedStatus,
                             selectedDepartment),
                        CurrentFinancialAllocations)
                    .Select(ToRequesterSettlementRow)
                    .ToList();
                break;
            default:
                filteredDepartmentRows = SettlementWorkspaceProjection.BuildDepartmentRows(
                        periodOrdersSnapshot,
                        departmentDirectory,
                        new SettlementOrderGroupFilter(
                            searchText,
                             selectedOrderType,
                             selectedStatus,
                             selectedDepartment),
                        CurrentFinancialAllocations)
                    .Select(ToDepartmentSettlementRow)
                    .ToList();
                break;
        }
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
        if (string.Equals(viewMode, mode, StringComparison.Ordinal))
        {
            return;
        }

        viewMode = mode;
        alertMessage = null;
        isGridLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            await EnsureCurrentViewDataAsync();
        }
        catch (Exception ex)
        {
            HandleLoadError(ex);
        }
        finally
        {
            isGridLoading = false;
            await InvokeAsync(StateHasChanged);
        }
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
            await ApplySupplierQuoteAsync(quote);
        }
    }

    private async Task RefreshPreviewAsync()
    {
        if (!RequiresFreshPreview || isPreviewLoading)
        {
            return;
        }

        var supplierId = Preview?.PrimarySupplierId;
        var priceListId = Preview?.PrimaryPriceListId;
        try
        {
            // Correction thành công làm snapshot cũ hết hiệu lực; preview lại tạo key mới.
            await LoadPreviewAsync(supplierId, priceListId);
        }
        catch (Exception ex)
        {
            Toast.Error(ex, Loc);
        }
    }

    private bool IsCurrentQuote(PriceBookQuoteResDTO quote) => Preview?.PrimaryPriceListId == quote.PriceListId;

    private decimal ItemNetUnitPrice(AggregatedVppItemResDTO item) =>
        itemFinancials.GetValueOrDefault(item.VppId)?.NetUnitPrice ?? 0;

    private decimal ItemVatRate(AggregatedVppItemResDTO item) =>
        itemFinancials.GetValueOrDefault(item.VppId)?.VatRate ?? 0;

    private decimal ItemNetAmount(AggregatedVppItemResDTO item) =>
        itemFinancials.GetValueOrDefault(item.VppId)?.NetAmount ?? 0;

    private decimal ItemGrossAmount(AggregatedVppItemResDTO item) =>
        itemFinancials.GetValueOrDefault(item.VppId)?.GrossAmount ?? 0;

    private void RebuildItemFinancials()
    {
        itemFinancials = HasSettlementSnapshot
            ? currentSettlementRevision!.Items.ToDictionary(
                line => line.VppId,
                line => new SettlementItemFinancialView(
                    line.NetUnitPrice,
                    line.VatRate,
                    line.NetAmount,
                    line.GrossAmount))
            : (SelectedQuote?.Lines ?? []).ToDictionary(
                line => line.VppId,
                line => new SettlementItemFinancialView(
                    line.NetUnitPrice,
                    line.VatRate,
                    line.NetAmount,
                    line.GrossAmount));
    }

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
            State.RequireFreshPreviewForNextSubmission();
            await ReloadPeriodAsync();
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
        decimal NetAmount,
        decimal VatAmount,
        decimal GrossAmount,
        string StatusText,
        VppStatusTone StatusTone);

    private sealed record RequesterSettlementRow(
        int UserId,
        string RequesterName,
        string DepartmentSummary,
        int OrderCount,
        int RegularOrderCount,
        int AdditionalOrderCount,
        int TotalLines,
        int TotalQuantity,
        long TotalAmount,
        decimal NetAmount,
        decimal VatAmount,
        decimal GrossAmount,
        string StatusText,
        VppStatusTone StatusTone);

    private sealed record SettlementItemFinancialView(
        decimal NetUnitPrice,
        decimal VatRate,
        decimal NetAmount,
        decimal GrossAmount);
}

public sealed record PeriodTargetSelection(int Year, int Month);
