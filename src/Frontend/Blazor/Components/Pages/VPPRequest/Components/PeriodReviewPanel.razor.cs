using System.Globalization;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components;

public partial class PeriodReviewPanel : IDisposable
{
    [Inject] private IAPIServices ApiServices { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter] public int Year { get; set; }
    [Parameter] public int Month { get; set; }
    [Parameter] public EventCallback OnContinueToDemand { get; set; }

    private readonly HashSet<Guid> loadedDetailOrderIds = [];
    private readonly HashSet<Guid> loadingDetailOrderIds = [];
    private CancellationTokenSource? searchDebounce;
    private List<VppRequestResDTO> orders = [];
    private PeriodSettlementResDTO? periodStatus;
    private bool isLoading = true;
    private bool isGridLoading;
    private string? alertMessage;
    private int totalCount;
    private int totalLines;
    private int totalQty;
    private long totalAmount;
    private int currentSkip;
    private int pageSize = VppPagingProfiles.SplitList.DefaultPageSize;
    private string? currentOrderByExpression;
    private string searchText = string.Empty;
    private string selectedOrderType = string.Empty;
    private int? selectedStatus;
    private int loadedYear;
    private int loadedMonth;
    private Guid? activeCodeOrderId;
    private Guid? activeNoteOrderId;

    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText)
        || !string.IsNullOrWhiteSpace(selectedOrderType)
        || selectedStatus.HasValue;
    private bool HasNoValidOrders => !isLoading && totalCount == 0;

    private string HeroStateClass => periodStatus switch
    {
        null => string.Empty,
        { IsSettled: true } => "is-settled",
        { PendingAdditionalCount: > 0 } => "is-blocked",
        _ when HasNoValidOrders => "is-blocked",
        _ => "is-ready"
    };
    private string HeroTitle => periodStatus switch
    {
        null => string.Empty,
        { IsSettled: true } => Loc["ReviewHeroSettled"].Value,
        { PendingAdditionalCount: > 0 } => Loc["ReviewHeroBlocked"].Value,
        _ when HasNoValidOrders => Loc["ReviewNoValidOrders"].Value,
        _ => Loc["ReviewHeroReady"].Value
    };
    private string HeroMeta => periodStatus switch
    {
        null => string.Empty,
        { IsSettled: true } => Loc["ReviewHeroSettledMeta"].Value,
        { PendingAdditionalCount: > 0 } => Loc["ReviewHeroBlockedMeta"].Value,
        _ when HasNoValidOrders => Loc["DemandEmptyDescription"].Value,
        _ => Loc["ReviewHeroReadyMeta"].Value
    };

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

    protected override async Task OnParametersSetAsync()
    {
        if (Year < 2024 || Month is < 1 or > 12 || (loadedYear == Year && loadedMonth == Month))
        {
            return;
        }

        loadedYear = Year;
        loadedMonth = Month;
        await ReloadPeriodAsync();
    }

    private async Task ReloadPeriodAsync()
    {
        currentSkip = 0;
        currentOrderByExpression = null;
        alertMessage = null;
        await LoadSettlementStatusAsync();
        await LoadOrdersAsync(firstLoad: true);
    }

    private async Task LoadSettlementStatusAsync()
    {
        try
        {
            periodStatus = await ApiServices.GetFromApiAsync<PeriodSettlementResDTO>(
                string.Format(Config.RequestApi.PeriodSettlement.Status, Year, Month));
        }
        catch (Exception ex)
        {
            alertMessage = UiErrorMapper.GetMessage(ex, Loc);
        }
    }

    private async Task LoadOrdersAsync(bool firstLoad)
    {
        isLoading = firstLoad;
        isGridLoading = !firstLoad;
        try
        {
            var (data, count, lines, qty, amount) = await ApiServices
                .GetFromApiWithAmountStatsAsync<List<VppRequestResDTO>>(BuildOrdersEndpoint());
            orders = data ?? [];
            totalCount = count;
            totalLines = lines;
            totalQty = qty;
            totalAmount = amount;
            alertMessage = null;
            loadedDetailOrderIds.Clear();
            loadingDetailOrderIds.Clear();
        }
        catch (Exception ex)
        {
            alertMessage = UiErrorMapper.GetMessage(ex, Loc);
            Toast.Error(ex, Loc);
        }
        finally
        {
            isLoading = false;
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
        return clauses.Count == 0 ? null : string.Join(" && ", clauses);
    }

    private static string EscapeDynamicString(string value) => value
        .ToLowerInvariant()
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal);

    private async Task OnLoadData(LoadDataArgs args)
    {
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
            currentSkip = 0;
            await LoadOrdersAsync(firstLoad: false);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task OnOrderTypeChangedAsync(string value)
    {
        selectedOrderType = value;
        currentSkip = 0;
        await LoadOrdersAsync(firstLoad: false);
    }

    private async Task OnStatusChangedAsync(int? value)
    {
        selectedStatus = value;
        currentSkip = 0;
        await LoadOrdersAsync(firstLoad: false);
    }

    private async Task ClearFiltersAsync()
    {
        searchText = string.Empty;
        selectedOrderType = string.Empty;
        selectedStatus = null;
        currentSkip = 0;
        await LoadOrdersAsync(firstLoad: false);
    }

    private Task OpenApprovalsAsync()
    {
        NavigationManager.NavigateTo("/dashboard?tab=5&periodTab=pending");
        return Task.CompletedTask;
    }

    private async Task OnRowExpandAsync(VppRequestResDTO row)
    {
        if (row.Id == Guid.Empty || loadedDetailOrderIds.Contains(row.Id) || !loadingDetailOrderIds.Add(row.Id)) return;
        try
        {
            var detail = await ApiServices.GetFromApiAsync<VppRequestResDTO>($"{Config.VppApi.Orders}/{row.Id}");
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
        }
    }

    private bool IsRowDetailLoading(Guid orderId) => loadingDetailOrderIds.Contains(orderId);
    private static string FormatMoney(long value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    private void ToggleCode(Guid orderId)
    {
        activeNoteOrderId = null;
        activeCodeOrderId = activeCodeOrderId == orderId ? null : orderId;
    }

    private void ToggleNote(Guid orderId)
    {
        activeCodeOrderId = null;
        activeNoteOrderId = activeNoteOrderId == orderId ? null : orderId;
    }

    private async Task CopyToClipboard(string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
    }

    public void Dispose()
    {
        searchDebounce?.Cancel();
        searchDebounce?.Dispose();
    }
}
