using System.Globalization;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.Pages.VPPRequest.Components;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using static gtas_vpp_fe.Components.Pages.VPPRequest.Components.HistoryUiKeys;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs;

// Các hằng khóa UI (scope/menu/KPI/series) nằm trong Components/HistorySupport.cs;
// item-detail dùng record typed của DesignSystem để chia sẻ với My Orders.
public partial class Tab_History : BaseOrderTab, IAsyncDisposable
{
    [Inject] public DialogService DialogService { get; set; } = default!;

    private ElementReference _historyRoot;
    private IJSObjectReference? _module;
    private DotNetObjectReference<Tab_History>? _dotNetReference;
    private CancellationTokenSource? _searchDebounce;
    private HistoryOrderDetailSheet? _detailSheet;
    private VppOrderHistorySummaryResDTO? _summary;
    private VppRequestResDTO? _selectedOrder;
    private readonly List<VppOrderDetailItem> _detailRows = [];
    private readonly List<string> _detailCategories = [];
    private readonly List<string> _detailUoms = [];
    private string _scope = Last1Scope;
    private string _customFrom = string.Empty;
    private string _customTo = string.Empty;
    private int? _fromPeriod;
    private int? _toPeriod;
    private int? _selectedPeriod;
    private int? _currentPeriod;
    private string _search = string.Empty;
    private int? _selectedStatus;
    private string _selectedOrderType = string.Empty;
    private string _detailSearch = string.Empty;
    private string _detailCategory = string.Empty;
    private string _detailUom = string.Empty;
    private bool _showCustomRange;
    private bool _showScopeMenu;
    private string _activeKpi = string.Empty;
    private bool _isSummaryLoading;
    private bool _isDetailLoading;
    private bool _summaryError;
    private bool _detailError;
    private bool _isDrawerOpen;
    private bool _isInitialLoading = true;
    private Guid? _activeCodeOrderId;
    private Guid? _activeNoteOrderId;
    private int? _activeDetailCodeNumber;
    private int? _activeDetailNoteNumber;
    private bool _showRegularSeries = true;
    private bool _showAdditionalSeries = true;

    private bool IsRefreshing => _isSummaryLoading || IsGridLoading || _isDetailLoading;

    protected override bool CanView => HasDashboardPermission(Permissions.RequestHistory);
    protected override string ErrorSummary => Loc["History"];

    public Tab_History()
    {
        PageSize = 6;
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadCurrentPeriodAsync();
            await base.OnInitializedAsync();
            await LoadSummaryAsync();
            await SelectFirstOrderAsync();
        }
        finally
        {
            _isInitialLoading = false;
        }
    }

    private async Task LoadCurrentPeriodAsync()
    {
        try
        {
            var periodInfo = await _apiServices.GetFromApiAsync<VppPeriodInfoResDTO>(Config.VppApi.PeriodInfo);
            if (periodInfo is { CurrentPeriodYear: > 0, CurrentPeriodMonth: >= 1 and <= 12 })
            {
                _currentPeriod = (periodInfo.CurrentPeriodYear * 100) + periodInfo.CurrentPeriodMonth;
                _fromPeriod = _currentPeriod;
                _toPeriod = _currentPeriod;
                return;
            }
        }
        catch
        {
            // Fallback về phạm vi lịch sử rộng nếu endpoint kỳ không khả dụng.
        }

        _scope = AllScope;
        _fromPeriod = null;
        _toPeriod = null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetReference = DotNetObjectReference.Create(this);
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./Components/Pages/VPPRequest/Tabs/Tab_History.razor.js?v=20260723-layout-stability-2");
            await _module.InvokeVoidAsync("observeHistoryViewport", _historyRoot, _dotNetReference);
        }

        if (_module is not null)
        {
            await _module.InvokeVoidAsync("updateHistoryScopeIndicator", _historyRoot);
            var periods = _summary?.Periods ?? [];
            await _module.InvokeVoidAsync(
                "renderHistoryChartLabels",
                _historyRoot,
                periods.Select(period => period.RegularQuantity).ToArray(),
                periods.Select(period => period.AdditionalQuantity).ToArray(),
                _showRegularSeries,
                _showAdditionalSeries);
        }

    }

    protected override string BuildEndpoint()
    {
        var query = new List<string>
        {
            $"skip={CurrentSkip}",
            $"top={PageSize}"
        };
        AppendPeriodRange(query);

        if (_selectedPeriod.HasValue) query.Add($"exactPeriod={_selectedPeriod.Value}");
        if (!string.IsNullOrWhiteSpace(_search)) query.Add($"search={Uri.EscapeDataString(_search.Trim())}");
        if (_selectedStatus.HasValue) query.Add($"status={_selectedStatus.Value}");
        if (_selectedOrderType == "regular") query.Add("isAdditionalOrder=false");
        if (_selectedOrderType == "additional") query.Add("isAdditionalOrder=true");

        return $"{Config.VppApi.MyOrderHistory}?{string.Join("&", query)}";
    }

    protected override void AppendFilterScopeQuery(List<string> query)
    {
        query.Add("scope=my-orders");
        AppendPeriodRange(query);
    }

    private void AppendPeriodRange(List<string> query)
    {
        if (_fromPeriod.HasValue) query.Add($"fromPeriod={_fromPeriod.Value}");
        if (_toPeriod.HasValue) query.Add($"toPeriod={_toPeriod.Value}");
    }

    private async Task LoadSummaryAsync()
    {
        if (!CanView)
        {
            return;
        }

        _isSummaryLoading = true;
        _summaryError = false;
        await InvokeAsync(StateHasChanged);
        try
        {
            var query = new List<string>();
            AppendPeriodRange(query);
            var endpoint = Config.VppApi.MyOrderHistorySummary;
            if (query.Count > 0) endpoint += $"?{string.Join("&", query)}";
            _summary = await _apiServices.GetFromApiAsync<VppOrderHistorySummaryResDTO>(endpoint);
        }
        catch (Exception ex)
        {
            _summaryError = true;
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = Loc["History"],
                Detail = UiErrorMapper.GetMessage(ex, BaseLoc, "LoadFailed"),
                Duration = 6000
            });
        }
        finally
        {
            _isSummaryLoading = false;
        }
    }

    private async Task SelectScopeAsync(string scope)
    {
        CloseTransientSurfaces();
        _scope = scope;
        _showCustomRange = scope == CustomScope;
        _showScopeMenu = true;
        if (_showCustomRange)
        {
            SeedCustomRange();
            return;
        }

        if (scope == Last12Scope && _summary?.LatestPeriod is int latestPeriod)
        {
            _fromPeriod = AddMonths(latestPeriod, -11);
            _toPeriod = latestPeriod;
        }
        else
        {
            _fromPeriod = null;
            _toPeriod = null;
        }

        await ReloadScopeAsync();
        _showScopeMenu = false;
    }

    private async Task SelectPresetScopeAsync(string scope)
    {
        var previousScope = _scope;
        var previousFromPeriod = _fromPeriod;
        var previousToPeriod = _toPeriod;
        var hadCustomRange = _showCustomRange;

        CloseTransientSurfaces();
        _scope = scope;
        _showCustomRange = false;
        _showScopeMenu = false;

        if (scope == Last1Scope && (_currentPeriod ?? _summary?.LatestPeriod) is int currentPeriod)
        {
            _fromPeriod = currentPeriod;
            _toPeriod = currentPeriod;
        }
        else if (_summary?.LatestPeriod is int latestPeriod && scope != AllScope)
        {
            var months = scope switch
            {
                Last1Scope => 1,
                Last3Scope => 3,
                Last6Scope => 6,
                Last12Scope => 12,
                _ => 0
            };
            _fromPeriod = months > 0 ? AddMonths(latestPeriod, -(months - 1)) : null;
            _toPeriod = months > 0 ? latestPeriod : null;
        }
        else
        {
            _fromPeriod = null;
            _toPeriod = null;
        }

        if (!hadCustomRange
            && previousScope == _scope
            && previousFromPeriod == _fromPeriod
            && previousToPeriod == _toPeriod)
        {
            return;
        }

        await ReloadScopeAsync();
    }

    private void SeedCustomRange()
    {
        var latest = _summary?.LatestPeriod ?? ((DateTime.Today.Year * 100) + DateTime.Today.Month);
        _customTo = FormatMonthInput(latest);
        _customFrom = FormatMonthInput(AddMonths(latest, -11));
    }

    private async Task ApplyCustomRangeAsync()
    {
        if (!TryParseMonthInput(_customFrom, out var from)
            || !TryParseMonthInput(_customTo, out var to)
            || from > to)
        {
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = Loc["HistoryCustomRange"],
                Detail = Loc["HistoryInvalidRange"],
                Duration = 4500
            });
            return;
        }

        _fromPeriod = from;
        _toPeriod = to;
        _showCustomRange = false;
        _showScopeMenu = false;
        await ReloadScopeAsync();
    }

    private void OnCustomFromChanged(ChangeEventArgs args) => _customFrom = args.Value?.ToString() ?? string.Empty;

    private void OnCustomToChanged(ChangeEventArgs args) => _customTo = args.Value?.ToString() ?? string.Empty;

    private async Task ReloadScopeAsync()
    {
        _selectedPeriod = null;
        CurrentSkip = 0;
        await LoadSummaryAsync();
        await LoadOrdersAndSelectAsync();
    }

    private async Task RefreshAsync()
    {
        await LoadSummaryAsync();
        await LoadOrdersAndSelectAsync();
    }

    private async Task OnChartSeriesClick(SeriesClickEventArgs args)
    {
        if (args.Data is not VppOrderHistoryPeriodResDTO period || period.OrderCount == 0)
        {
            return;
        }

        _selectedPeriod = _selectedPeriod == period.PeriodKey ? null : period.PeriodKey;
        CurrentSkip = 0;
        await LoadOrdersAndSelectAsync();
    }

    private async Task ClearSelectedPeriodAsync()
    {
        _selectedPeriod = null;
        CurrentSkip = 0;
        await LoadOrdersAndSelectAsync();
    }

    private async Task OnSearchInput(ChangeEventArgs args)
    {
        _search = args.Value?.ToString() ?? string.Empty;
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = new CancellationTokenSource();
        try
        {
            await Task.Delay(280, _searchDebounce.Token);
            CurrentSkip = 0;
            await LoadOrdersAndSelectAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    [JSInvokable]
    public async Task CloseHistoryFilterMenuAsync()
    {
        if (!_showScopeMenu
            && string.IsNullOrEmpty(_activeKpi)
            && !_activeCodeOrderId.HasValue
            && !_activeNoteOrderId.HasValue
            && !_activeDetailCodeNumber.HasValue
            && !_activeDetailNoteNumber.HasValue)
        {
            return;
        }

        CloseTransientSurfaces();
        await InvokeAsync(StateHasChanged);
    }

    private void ToggleKpi(string key)
    {
        var shouldOpen = _activeKpi != key;
        CloseTransientSurfaces();
        _activeKpi = shouldOpen ? key : string.Empty;
    }

    private void ToggleCode(Guid orderId)
    {
        var shouldOpen = _activeCodeOrderId != orderId;
        CloseTransientSurfaces();
        _activeCodeOrderId = shouldOpen ? orderId : null;
    }

    private void ToggleNote(Guid orderId)
    {
        var shouldOpen = _activeNoteOrderId != orderId;
        CloseTransientSurfaces();
        _activeNoteOrderId = shouldOpen ? orderId : null;
    }

    private void ToggleDetailCode(int number)
    {
        var shouldOpen = _activeDetailCodeNumber != number;
        CloseTransientSurfaces();
        _activeDetailCodeNumber = shouldOpen ? number : null;
    }

    private void ToggleDetailNote(int number)
    {
        var shouldOpen = _activeDetailNoteNumber != number;
        CloseTransientSurfaces();
        _activeDetailNoteNumber = shouldOpen ? number : null;
    }

    private async Task CopyTextAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
            var isVietnamese = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = isVietnamese ? "Đã sao chép" : "Copied",
                Detail = isVietnamese ? "Nội dung đã được sao chép vào bộ nhớ tạm." : "The content was copied to the clipboard.",
                Duration = 2500
            });
        }
        catch (JSException)
        {
            var isVietnamese = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = isVietnamese ? "Không thể sao chép" : "Copy failed",
                Detail = isVietnamese ? "Trình duyệt không cho phép truy cập bộ nhớ tạm." : "The browser denied clipboard access.",
                Duration = 3500
            });
        }
    }

    private void CloseTransientSurfaces()
    {
        _showScopeMenu = false;
        _showCustomRange = false;
        _activeKpi = string.Empty;
        _activeCodeOrderId = null;
        _activeNoteOrderId = null;
        _activeDetailCodeNumber = null;
        _activeDetailNoteNumber = null;
    }

    private void ToggleChartSeries(string series)
    {
        if (series == RegularSeries)
        {
            _showRegularSeries = !_showRegularSeries;
            return;
        }

        _showAdditionalSeries = !_showAdditionalSeries;
    }

    private async Task SelectStatusAsync(int? status)
    {
        _selectedStatus = status;
        CurrentSkip = 0;
        await LoadOrdersAndSelectAsync();
    }

    private async Task SelectOrderTypeAsync(string orderType)
    {
        _selectedOrderType = orderType;
        CurrentSkip = 0;
        await LoadOrdersAndSelectAsync();
    }

    private async Task ClearTableFiltersAsync()
    {
        _search = string.Empty;
        _selectedStatus = null;
        _selectedOrderType = string.Empty;
        _selectedPeriod = null;
        CurrentSkip = 0;
        await LoadOrdersAndSelectAsync();
    }

    private async Task OnHistoryLoadData(LoadDataArgs args)
    {
        await base.OnLoadData(args);
        await SelectFirstOrderAsync();
    }

    private Task OnOrderSelectedAsync(VppRequestResDTO order) => OpenOrderAsync(order);

    private async Task LoadOrdersAndSelectAsync()
    {
        await LoadAsync();
        await SelectFirstOrderAsync();
    }

    private async Task SelectFirstOrderAsync()
    {
        if (Orders.Count == 0)
        {
            return;
        }

        if (_selectedOrder is not null && Orders.Any(order => order.Id == _selectedOrder.Id))
        {
            return;
        }

        await OpenOrderAsync(Orders[0], focusPanel: false);
    }

    private async Task OpenOrderAsync(VppRequestResDTO order, bool focusPanel = true)
    {
        if (_isDetailLoading)
        {
            return;
        }

        var preserveCurrentDetail = _selectedOrder is not null && !_detailError;
        _isDrawerOpen = focusPanel;
        _isDetailLoading = true;
        _detailError = false;
        _activeDetailCodeNumber = null;
        _activeDetailNoteNumber = null;
        if (!preserveCurrentDetail)
        {
            _selectedOrder = order;
            _detailSearch = string.Empty;
            _detailCategory = string.Empty;
            _detailUom = string.Empty;
            _detailRows.Clear();
            _detailCategories.Clear();
            _detailUoms.Clear();
        }
        await InvokeAsync(StateHasChanged);

        try
        {
            var detail = await _apiServices.GetFromApiAsync<VppRequestResDTO>(
                $"{Config.VppApi.Orders}/{order.Id}");
            _selectedOrder = detail ?? order;
            _detailSearch = string.Empty;
            _detailCategory = string.Empty;
            _detailUom = string.Empty;
            _detailRows.Clear();
            _detailCategories.Clear();
            _detailUoms.Clear();
            _detailCategories.AddRange((_selectedOrder.Items ?? [])
                .Select(item => item.CategoryName)
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Select(category => category!)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(category => category, StringComparer.CurrentCultureIgnoreCase));
            _detailUoms.AddRange((_selectedOrder.Items ?? [])
                .Select(item => item.UomName)
                .Where(uom => !string.IsNullOrWhiteSpace(uom))
                .Select(uom => uom!)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(uom => uom, StringComparer.CurrentCultureIgnoreCase));
            RebuildDetailRows();
        }
        catch (Exception ex)
        {
            _selectedOrder = order;
            _detailError = true;
            _detailRows.Clear();
            _detailCategories.Clear();
            _detailUoms.Clear();
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = Loc["OrderDetails"],
                Detail = UiErrorMapper.GetMessage(ex, BaseLoc, "LoadDetailsFailed"),
                Duration = 6000
            });
        }
        finally
        {
            _isDetailLoading = false;
            await InvokeAsync(StateHasChanged);
            if (focusPanel && _isDrawerOpen && _module is not null && _detailSheet is not null)
            {
                await _module.InvokeVoidAsync("focusHistoryDrawer", _detailSheet.DrawerElement);
            }
        }
    }

    private async Task OnDetailSearchInput(ChangeEventArgs args)
    {
        _detailSearch = args.Value?.ToString() ?? string.Empty;
        RebuildDetailRows();
        if (_detailSheet is not null) await _detailSheet.ReloadDetailGridAsync();
    }

    private async Task OnDetailCategoryChanged(ChangeEventArgs args)
    {
        _detailCategory = args.Value?.ToString() ?? string.Empty;
        RebuildDetailRows();
        if (_detailSheet is not null) await _detailSheet.ReloadDetailGridAsync();
    }

    private async Task SelectDetailCategoryAsync(string category)
    {
        _detailCategory = category;
        RebuildDetailRows();
        if (_detailSheet is not null) await _detailSheet.ReloadDetailGridAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectDetailUomAsync(string uom)
    {
        _detailUom = uom;
        RebuildDetailRows();
        if (_detailSheet is not null) await _detailSheet.ReloadDetailGridAsync();
        await InvokeAsync(StateHasChanged);
    }

    private bool HasDetailFilters =>
        !string.IsNullOrWhiteSpace(_detailSearch)
        || !string.IsNullOrEmpty(_detailCategory)
        || !string.IsNullOrEmpty(_detailUom);

    private async Task ClearDetailFiltersAsync()
    {
        if (!HasDetailFilters)
        {
            return;
        }

        _detailSearch = string.Empty;
        _detailCategory = string.Empty;
        _detailUom = string.Empty;
        RebuildDetailRows();
        if (_detailSheet is not null) await _detailSheet.ReloadDetailGridAsync();
        await InvokeAsync(StateHasChanged);
    }

    private void RebuildDetailRows()
    {
        var items = _selectedOrder?.Items ?? [];
        var search = _detailSearch.Trim();
        var filtered = items.Where(item =>
            (string.IsNullOrEmpty(search)
                || (item.VppCode?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (item.VppName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false))
            && (string.IsNullOrEmpty(_detailCategory)
                || string.Equals(item.CategoryName, _detailCategory, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrEmpty(_detailUom)
                || string.Equals(item.UomName, _detailUom, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();

        _detailRows.Clear();
        _detailRows.AddRange(filtered.Select((item, index) => new VppOrderDetailItem(
            index + 1,
            item.VppCode,
            item.VppName ?? string.Empty,
            item.CategoryName,
            item.UomName,
            item.Qty,
            item.Description)));
    }

    private Task ExportSelectedOrderAsync(string format)
        => _selectedOrder is null ? Task.CompletedTask : ExportOrderAsync(_selectedOrder, format);

    private void CloseDrawer()
    {
        _isDrawerOpen = false;
    }

    private void ClearSelection()
    {
        _isDrawerOpen = false;
        _isDetailLoading = false;
        _detailError = false;
        _selectedOrder = null;
        _detailRows.Clear();
        _detailCategories.Clear();
        _detailUoms.Clear();
        _detailUom = string.Empty;
    }

    private string ScopeLabel => _scope switch
    {
        Last1Scope => Loc["HistoryCurrentPeriod"].Value,
        Last3Scope => Loc["HistoryLast3Periods"].Value,
        Last6Scope => Loc["HistoryLast6Periods"].Value,
        Last12Scope => Loc["HistoryLast12Periods"].Value,
        CustomScope => Loc["HistoryCustom"].Value,
        _ => Loc["HistoryAllPeriods"].Value
    };
    private string UnitHeaderLabel => Loc["UOM"].Value.Trim() switch
    {
        "ĐVT" or "DVT" => "Đơn vị",
        "UOM" => "Unit",
        var localizedLabel => localizedLabel
    };
    private string SelectedPeriodLabel => _selectedPeriod.HasValue
        ? $"{_selectedPeriod.Value % 100:00}/{_selectedPeriod.Value / 100}"
        : string.Empty;
    private string ChartRangeLabel => _summary?.Periods.Count > 0
        ? $"{_summary.Periods[0].Period}–{_summary.Periods[^1].Period}"
        : string.Empty;
    private string ChartAccessibleLabel => !_showRegularSeries && !_showAdditionalSeries
        ? Loc["HistoryChartNoSeriesSelected"].Value
        : Loc["HistoryChartAccessibleSummary", _summary?.PeriodCount ?? 0, _summary?.TotalQuantity ?? 0].Value;
    private bool HasTableFilters => !string.IsNullOrWhiteSpace(_search)
        || _selectedStatus.HasValue
        || !string.IsNullOrEmpty(_selectedOrderType)
        || _selectedPeriod.HasValue;

    private static int AddMonths(int period, int months)
    {
        var value = new DateTime(period / 100, period % 100, 1).AddMonths(months);
        return (value.Year * 100) + value.Month;
    }

    private static string FormatMonthInput(int period) => $"{period / 100:D4}-{period % 100:D2}";

    private static bool TryParseMonthInput(string value, out int period)
    {
        period = 0;
        if (!DateTime.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return false;
        }

        period = (date.Year * 100) + date.Month;
        return true;
    }

    [JSInvokable]
    public async Task SetHistoryViewport(int pageSize)
    {
        var normalized = Math.Clamp(pageSize, 3, 20);
        if (PageSize == normalized)
        {
            return;
        }

        PageSize = normalized;
        CurrentSkip = 0;
        await LoadOrdersAndSelectAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("disposeHistoryViewport", _historyRoot);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _dotNetReference?.Dispose();
        Dispose();
    }
}
