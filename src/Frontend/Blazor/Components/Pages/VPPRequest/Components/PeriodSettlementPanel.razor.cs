using System.Globalization;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components;

public partial class PeriodSettlementPanel : IDisposable
{
    private const string CurrentPeriodScope = "current";
    private const string CustomPeriodScope = "custom";
    private const string OrdersView = "orders";
    private const string DepartmentsView = "departments";

    [Inject] private IAPIServices ApiServices { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private PeriodSettlementState State { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public int Year { get; set; }
    [Parameter] public int Month { get; set; }
    [Parameter] public int DefaultYear { get; set; }
    [Parameter] public int DefaultMonth { get; set; }
    [Parameter] public EventCallback<PeriodTargetSelection> PeriodChanged { get; set; }
    [Parameter] public EventCallback OnSettled { get; set; }

    private readonly List<VppOrderDetailItem> detailRows = [];
    private readonly List<string> detailCategories = [];
    private readonly List<string> detailUoms = [];
    private CancellationTokenSource? searchDebounce;
    private List<VppRequestResDTO> orders = [];
    private List<VppRequestResDTO> periodOrdersSnapshot = [];
    private PeriodSettlementResDTO? status;
    private bool isLoading = true;
    private bool isGridLoading;
    private bool isPreviewLoading;
    private bool isSettling;
    private bool isCorrecting;
    private bool canCorrect;
    private bool isSupplierDialogOpen;
    private bool isCorrectionDialogOpen;
    private bool isDrawerOpen;
    private bool isDetailFullscreen;
    private bool isDetailLoading;
    private bool detailError;
    private bool isExportingOrder;
    private string? alertMessage;
    private string periodScope = CurrentPeriodScope;
    private string viewMode = OrdersView;
    private string searchText = string.Empty;
    private string selectedOrderType = string.Empty;
    private int? selectedStatus;
    private string selectedDepartment = string.Empty;
    private string? currentOrderByExpression;
    private string detailSearch = string.Empty;
    private string detailCategory = string.Empty;
    private string detailUom = string.Empty;
    private string correctionReason = string.Empty;
    private int totalCount;
    private int currentSkip;
    private int pageSize = VppPagingProfiles.LargeWorkingSet.DefaultPageSize;
    private int loadedYear;
    private int loadedMonth;
    private int? activeDetailCodeNumber;
    private int? activeDetailNoteNumber;
    private Guid? pendingPriceListId;
    private VppRequestResDTO? selectedOrder;
    private HistoryOrderDetailSheet? detailSheet;

    private SettlementPreviewResDTO? Preview => State.Preview is { } preview
        && preview.Year == Year && preview.Month == Month ? preview : null;

    private bool CanConfirm => !isLoading
        && status?.PendingAdditionalCount == 0
        && Preview is { PrimaryQuote: { IsEligible: true }, Blockers.Count: 0 }
        && !string.IsNullOrWhiteSpace(State.IdempotencyKey);

    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText)
        || !string.IsNullOrWhiteSpace(selectedOrderType)
        || selectedStatus.HasValue
        || !string.IsNullOrWhiteSpace(selectedDepartment);

    private bool HasDetailFilters => !string.IsNullOrWhiteSpace(detailSearch)
        || !string.IsNullOrWhiteSpace(detailCategory)
        || !string.IsNullOrWhiteSpace(detailUom);

    private bool HasPeriodBlockers => Preview?.Blockers.Count > 0 || status?.PendingAdditionalCount > 0;
    private string SettlementPageClass => HasPeriodBlockers
        ? "vpp-period-settlement-page has-blockers"
        : "vpp-period-settlement-page";

    private string PrimarySupplierName => Preview?.PrimaryQuote?.SupplierName
        ?? status?.PrimarySupplierName
        ?? Loc["SettlementNotSelected"].Value;

    private string PrimaryPriceListLabel => Preview?.PrimaryQuote is { } quote
        ? $"{quote.PriceListCode} · v{quote.Version}"
        : status?.PriceListName ?? Loc["SettlementNotSelected"].Value;

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

            return CanConfirm
                ? Loc["SettlementReadyCondition"].Value
                : Loc["SettlementSelectSupplierCondition"].Value;
        }
    }

    private IReadOnlyList<PriceBookQuoteResDTO> SupplierQuotes => Preview?.Quotes
        .OrderBy(quote => quote.Rank)
        .ToArray() ?? [];

    private IReadOnlyList<VppSegmentedOption<string>> PeriodScopeOptions =>
    [
        new(CurrentPeriodScope, Loc["SettlementCurrentPeriod"]),
        new(CustomPeriodScope, Loc["SettlementCustomPeriod"])
    ];

    private IReadOnlyList<VppSegmentedOption<string>> ViewModeOptions =>
    [
        new(OrdersView, Loc["SettlementByOrder"]),
        new(DepartmentsView, Loc["SettlementByDepartment"])
    ];

    private IReadOnlyList<VppFilterOption<int>> YearOptions => Enumerable.Range(2024, 7)
        .Select(year => new VppFilterOption<int>(year, year.ToString(CultureInfo.InvariantCulture)))
        .ToArray();

    private IReadOnlyList<VppFilterOption<int>> MonthOptions => Enumerable.Range(1, 12)
        .Select(month => new VppFilterOption<int>(month, month.ToString("00", CultureInfo.InvariantCulture)))
        .ToArray();

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
            .Concat(periodOrdersSnapshot
                .Select(order => order.DepartmentCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(code => code, StringComparer.CurrentCultureIgnoreCase)
                .Select(code => new VppFilterOption<string>(code, code)))
            .ToArray();

    private List<DepartmentSettlementRow> FilteredDepartmentRows => periodOrdersSnapshot
        .Where(MatchesClientFilters)
        .GroupBy(order => DisplayDepartment(order.DepartmentCode), StringComparer.CurrentCultureIgnoreCase)
        .Select(group => CreateDepartmentRow(group.Key, group.ToList()))
        .OrderBy(row => row.DepartmentCode, StringComparer.CurrentCultureIgnoreCase)
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
        periodScope = Year == DefaultYear && Month == DefaultMonth ? CurrentPeriodScope : CustomPeriodScope;
        await ReloadPeriodAsync();
    }

    private void OnStateChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task ReloadPeriodAsync()
    {
        isLoading = true;
        isGridLoading = true;
        alertMessage = null;
        currentSkip = 0;
        currentOrderByExpression = null;
        CloseDrawer();

        try
        {
            await LoadStatusAsync();
            await LoadPreviewAsync();
            await LoadAllPeriodOrdersAsync();
            await LoadOrdersAsync(firstLoad: true);
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
        status = await ApiServices.GetFromApiAsync<PeriodSettlementResDTO>(
            string.Format(Config.RequestApi.PeriodSettlement.Status, Year, Month));
        canCorrect = status is { IsSettled: true, SettlementId: not null };
    }

    private async Task LoadPreviewAsync(Guid? supplierId = null, Guid? priceListId = null)
    {
        isPreviewLoading = true;
        try
        {
            var preview = await ApiServices.PostFromApiAsync<SettlementPreviewResDTO>(
                Config.RequestApi.PeriodSettlement.Preview,
                new SettlementPreviewReqDTO
                {
                    Year = Year,
                    Month = Month,
                    PrimarySupplierId = supplierId,
                    PriceListId = priceListId,
                    PriceAsOfUtc = DateTime.UtcNow,
                    Exceptions = State.Exceptions.Select(CloneException).ToList()
                });

            State.SelectedSupplierId = preview?.PrimarySupplierId;
            State.SetPreview(preview);
            pendingPriceListId = preview?.PrimaryPriceListId;
        }
        finally
        {
            isPreviewLoading = false;
        }
    }

    private async Task LoadAllPeriodOrdersAsync()
    {
        const int batchSize = 500;
        var snapshot = new List<VppRequestResDTO>();
        var skip = 0;
        var expected = int.MaxValue;

        while (skip < expected)
        {
            var endpoint = $"{Config.VppApi.AllOrders}?year={Year}&month={Month}&skip={skip}&top={batchSize}&orderby=DepartmentCode%20asc";
            var (data, count, _, _, _) = await ApiServices
                .GetFromApiWithAmountStatsAsync<List<VppRequestResDTO>>(endpoint);
            var batch = data ?? [];
            expected = count;
            snapshot.AddRange(batch);
            if (batch.Count == 0 || batch.Count < batchSize)
            {
                break;
            }

            skip += batch.Count;
        }

        periodOrdersSnapshot = snapshot;
    }

    private async Task LoadOrdersAsync(bool firstLoad)
    {
        isLoading = firstLoad && isLoading;
        isGridLoading = !firstLoad || isGridLoading;
        try
        {
            var (data, count, _, _, _) = await ApiServices
                .GetFromApiWithAmountStatsAsync<List<VppRequestResDTO>>(BuildOrdersEndpoint());
            orders = data ?? [];
            totalCount = count;
            alertMessage = null;
        }
        catch (Exception ex)
        {
            alertMessage = UiErrorMapper.GetMessage(ex, Loc);
            Toast.Error(ex, Loc);
        }
        finally
        {
            isGridLoading = false;
        }
    }

    private string BuildOrdersEndpoint()
    {
        var query = new List<string>
        {
            $"year={Year}",
            $"month={Month}",
            $"skip={currentSkip}",
            $"top={pageSize}"
        };
        if (selectedStatus.HasValue) query.Add($"status={selectedStatus.Value}");
        var filter = BuildFilterExpression();
        if (!string.IsNullOrWhiteSpace(filter)) query.Add($"filter={Uri.EscapeDataString(filter)}");
        if (!string.IsNullOrWhiteSpace(currentOrderByExpression)) query.Add($"orderby={Uri.EscapeDataString(currentOrderByExpression)}");
        return $"{Config.VppApi.AllOrders}?{string.Join("&", query)}";
    }

    private string? BuildFilterExpression()
    {
        var clauses = new List<string>();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var value = EscapeDynamicString(searchText.Trim());
            clauses.Add($"((VppCode != null && VppCode.ToLower().Contains(\"{value}\")) || (RequesterName != null && RequesterName.ToLower().Contains(\"{value}\")) || (Description != null && Description.ToLower().Contains(\"{value}\")))");
        }
        if (selectedOrderType == "regular") clauses.Add("IsAdditionalOrder == false");
        if (selectedOrderType == "additional") clauses.Add("IsAdditionalOrder == true");
        if (!string.IsNullOrWhiteSpace(selectedDepartment))
        {
            clauses.Add($"(DepartmentCode != null && DepartmentCode.ToLower() == \"{EscapeDynamicString(selectedDepartment)}\")");
        }
        return clauses.Count == 0 ? null : string.Join(" && ", clauses);
    }

    private static string EscapeDynamicString(string value) => value
        .ToLowerInvariant()
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal);

    private bool MatchesClientFilters(VppRequestResDTO order)
    {
        var search = searchText.Trim();
        return (string.IsNullOrWhiteSpace(search)
                || (order.VppCode?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (order.RequesterName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (order.Description?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (order.DepartmentCode?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false))
            && (selectedOrderType switch
            {
                "regular" => !order.IsAdditionalOrder,
                "additional" => order.IsAdditionalOrder,
                _ => true
            })
            && (!selectedStatus.HasValue || order.Status == selectedStatus.Value)
            && (string.IsNullOrWhiteSpace(selectedDepartment)
                || string.Equals(order.DepartmentCode, selectedDepartment, StringComparison.CurrentCultureIgnoreCase));
    }

    private DepartmentSettlementRow CreateDepartmentRow(string departmentCode, List<VppRequestResDTO> departmentOrders)
    {
        var statusValue = departmentOrders.Any(order => order.Status == 6)
            ? (Loc["Pending"].Value, "vpp-badge-warning")
            : departmentOrders.All(order => order.Status == 7)
                ? (Loc["Approved"].Value, "vpp-badge-success")
                : departmentOrders.Any(order => order.Status is 4 or 8)
                    ? (Loc["SettlementNeedsReview"].Value, "vpp-badge-warning")
                    : (Loc["Submitted"].Value, "vpp-badge-info");

        return new DepartmentSettlementRow(
            departmentCode,
            departmentOrders.Count,
            departmentOrders.Count(order => !order.IsAdditionalOrder),
            departmentOrders.Count(order => order.IsAdditionalOrder),
            departmentOrders.Sum(order => order.TotalLines),
            departmentOrders.Sum(order => order.TotalQty),
            departmentOrders.Sum(order => order.TotalAmount),
            statusValue.Item1,
            statusValue.Item2);
    }

    private async Task OnLoadData(LoadDataArgs args)
    {
        if (viewMode != OrdersView || isLoading)
        {
            return;
        }

        currentSkip = args.Skip ?? 0;
        if (args.Top is > 0) pageSize = args.Top.Value;
        currentOrderByExpression = args.OrderBy;
        await LoadOrdersAsync(firstLoad: false);
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

    private async Task ApplyFiltersAsync()
    {
        currentSkip = 0;
        if (viewMode == OrdersView)
        {
            await LoadOrdersAsync(firstLoad: false);
        }
        else
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ClearFiltersAsync()
    {
        searchText = string.Empty;
        selectedOrderType = string.Empty;
        selectedStatus = null;
        selectedDepartment = string.Empty;
        await ApplyFiltersAsync();
    }

    private async Task OnPeriodScopeChangedAsync(string scope)
    {
        periodScope = scope;
        if (scope == CurrentPeriodScope && DefaultYear >= 2024 && DefaultMonth is >= 1 and <= 12)
        {
            await PeriodChanged.InvokeAsync(new PeriodTargetSelection(DefaultYear, DefaultMonth));
        }
    }

    private Task OnYearChangedAsync(int year)
    {
        periodScope = CustomPeriodScope;
        return PeriodChanged.InvokeAsync(new PeriodTargetSelection(year, Month));
    }

    private Task OnMonthChangedAsync(int month)
    {
        periodScope = CustomPeriodScope;
        return PeriodChanged.InvokeAsync(new PeriodTargetSelection(Year, month));
    }

    private async Task OnViewModeChangedAsync(string mode)
    {
        viewMode = mode;
        currentSkip = 0;
        if (mode == OrdersView)
        {
            await LoadOrdersAsync(firstLoad: false);
        }
    }

    private async Task OpenDepartmentOrdersAsync(string departmentCode)
    {
        selectedDepartment = departmentCode;
        viewMode = OrdersView;
        currentSkip = 0;
        await LoadOrdersAsync(firstLoad: false);
    }

    private Task OpenSupplierDialog()
    {
        pendingPriceListId = Preview?.PrimaryPriceListId;
        isSupplierDialogOpen = true;
        return InvokeAsync(StateHasChanged);
    }

    private void CloseSupplierDialog() => isSupplierDialogOpen = false;

    private Task OpenCorrectionDialog()
    {
        isCorrectionDialogOpen = true;
        return InvokeAsync(StateHasChanged);
    }

    private void CloseCorrectionDialog() => isCorrectionDialogOpen = false;

    private void SelectSupplierQuote(PriceBookQuoteResDTO quote) => pendingPriceListId = quote.PriceListId;

    private async Task ApplySupplierQuoteAsync()
    {
        var quote = SupplierQuotes.FirstOrDefault(item => item.PriceListId == pendingPriceListId);
        if (quote is null || !quote.IsEligible)
        {
            return;
        }

        if (Preview?.PrimaryPriceListId != quote.PriceListId)
        {
            State.Exceptions.Clear();
        }
        await LoadPreviewAsync(quote.SupplierId, quote.PriceListId);
        isSupplierDialogOpen = false;
    }

    private async Task OpenOrderAsync(VppRequestResDTO order)
    {
        if (isDetailLoading || order.Id == Guid.Empty)
        {
            return;
        }

        selectedOrder = order;
        isDrawerOpen = true;
        isDetailLoading = true;
        detailError = false;
        ResetDetailFilters();
        await InvokeAsync(StateHasChanged);

        try
        {
            selectedOrder = await ApiServices.GetFromApiAsync<VppRequestResDTO>($"{Config.VppApi.Orders}/{order.Id}") ?? order;
            BuildDetailOptions();
            RebuildDetailRows();
        }
        catch (Exception ex)
        {
            detailError = true;
            Toast.Error(ex, Loc);
        }
        finally
        {
            isDetailLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task RetryDetailAsync() => selectedOrder is null ? Task.CompletedTask : OpenOrderAsync(selectedOrder);

    private void BuildDetailOptions()
    {
        detailCategories.Clear();
        detailUoms.Clear();
        detailCategories.AddRange((selectedOrder?.Items ?? [])
            .Select(item => item.CategoryName)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase));
        detailUoms.AddRange((selectedOrder?.Items ?? [])
            .Select(item => item.UomName)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase));
    }

    private void RebuildDetailRows()
    {
        var search = detailSearch.Trim();
        var filtered = (selectedOrder?.Items ?? [])
            .Where(item => (string.IsNullOrWhiteSpace(search)
                    || (item.VppCode?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || (item.VppName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false))
                && (string.IsNullOrWhiteSpace(detailCategory)
                    || string.Equals(item.CategoryName, detailCategory, StringComparison.CurrentCultureIgnoreCase))
                && (string.IsNullOrWhiteSpace(detailUom)
                    || string.Equals(item.UomName, detailUom, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();

        detailRows.Clear();
        detailRows.AddRange(filtered.Select((item, index) => new VppOrderDetailItem(
            index + 1,
            item.VppCode,
            item.VppName ?? string.Empty,
            item.CategoryName,
            item.UomName,
            item.Qty,
            item.Description)));
    }

    private async Task OnDetailSearchInput(ChangeEventArgs args)
    {
        detailSearch = args.Value?.ToString() ?? string.Empty;
        await RefreshDetailSurfaceAsync();
    }

    private Task SelectDetailCategoryAsync(string category)
    {
        detailCategory = category;
        return RefreshDetailSurfaceAsync();
    }

    private Task SelectDetailUomAsync(string uom)
    {
        detailUom = uom;
        return RefreshDetailSurfaceAsync();
    }

    private async Task ClearDetailFiltersAsync()
    {
        ResetDetailFilters();
        await RefreshDetailSurfaceAsync();
    }

    private async Task RefreshDetailSurfaceAsync()
    {
        RebuildDetailRows();
        if (detailSheet is not null)
        {
            await detailSheet.ReloadDetailGridAsync();
        }
        await InvokeAsync(StateHasChanged);
    }

    private void ResetDetailFilters()
    {
        detailSearch = string.Empty;
        detailCategory = string.Empty;
        detailUom = string.Empty;
        activeDetailCodeNumber = null;
        activeDetailNoteNumber = null;
        detailRows.Clear();
        detailCategories.Clear();
        detailUoms.Clear();
    }

    private void ToggleDetailCode(int number)
    {
        activeDetailNoteNumber = null;
        activeDetailCodeNumber = activeDetailCodeNumber == number ? null : number;
    }

    private void ToggleDetailNote(int number)
    {
        activeDetailCodeNumber = null;
        activeDetailNoteNumber = activeDetailNoteNumber == number ? null : number;
    }

    private async Task CopyToClipboard(string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
    }

    private void CloseDrawer()
    {
        isDrawerOpen = false;
        isDetailFullscreen = false;
    }

    private void ToggleDetailFullscreen() => isDetailFullscreen = !isDetailFullscreen;

    private Task ExportSelectedOrderAsync(string format) => selectedOrder is null
        ? Task.CompletedTask
        : ExportOrderAsync(selectedOrder, format);

    private async Task ExportOrderAsync(VppRequestResDTO order, string format)
    {
        if (isExportingOrder)
        {
            return;
        }

        isExportingOrder = true;
        try
        {
            var file = await ApiServices.GetFileFromApiAsync($"{Config.VppApi.Orders}/{order.Id}/{format}");
            await using var stream = new MemoryStream(file.Content, writable: false);
            using var streamReference = new DotNetStreamReference(stream);
            await JSRuntime.InvokeVoidAsync("vppDownload.fromStream", file.FileName, streamReference);
            Toast.Notify(NotificationSeverity.Success, Loc["Order"], Loc["OrderExported"]);
        }
        catch (Exception ex)
        {
            Toast.Error(ex, Loc);
        }
        finally
        {
            isExportingOrder = false;
        }
    }

    private async Task SettleAsync()
    {
        if (!CanConfirm || Preview is null)
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
        if (!CanConfirm || Preview is null || status?.SettlementId is null)
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
            await ApiServices.PostFromApiAsync<SettlementRevisionResDTO>(
                string.Format(Config.RequestApi.PeriodSettlement.Correct, status.SettlementId.Value),
                new SettlementCorrectionReqDTO
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
                });
            State.CompleteConfirmation();
            correctionReason = string.Empty;
            isCorrectionDialogOpen = false;
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

    private static string DisplayDepartment(string? value) => string.IsNullOrWhiteSpace(value) ? "–" : value;
    private static string DisplayRequester(string? value) => string.IsNullOrWhiteSpace(value) ? "–" : value;
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
        int OrderCount,
        int RegularOrderCount,
        int AdditionalOrderCount,
        int TotalLines,
        int TotalQuantity,
        long TotalAmount,
        string StatusText,
        string StatusCss);
}

public sealed record PeriodTargetSelection(int Year, int Month);
