using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Features.Requests.Api;

public sealed record RequestPage<T>(
    IReadOnlyList<T> Items,
    int TotalCount);

public sealed record RequestStatsPage<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int TotalLines,
    int TotalQuantity);

public sealed record ProductCatalogQuery(
    int Skip,
    int Top,
    Guid? CategoryId = null,
    string Search = "",
    string? UnitName = null,
    string? OrderBy = null);

public sealed record OrderPeriod(int Year, int Month);

public enum OrderHistoryScope
{
    Own,
    Department
}

public enum OrderFilterScope
{
    Own,
    Department,
    Pending
}

public sealed record OrderHistoryQuery(
    int Skip,
    int Top,
    int? FromPeriod = null,
    int? ToPeriod = null,
    int? ExactPeriod = null,
    string Search = "",
    int? Status = null,
    bool? IsAdditionalOrder = null);

public sealed record PendingAdditionalOrdersQuery(
    int Skip,
    int Top,
    string? Filter = null,
    string? OrderBy = null);

public sealed record OrderFilterValuesQuery(
    string Column,
    OrderFilterScope Scope,
    int? FromPeriod = null,
    int? ToPeriod = null,
    string? FiltersJson = null,
    string? Filter = null,
    string? DistinctFilter = null);

public sealed class RequestsQueryClient(IAPIServices api)
{
    private const string RequestsBase = "/api/VPPRequest";

    public async Task<IReadOnlyList<VppCategoryResDTO>> GetCatalogCategoriesAsync() =>
        await api.GetFromApiAsync<List<VppCategoryResDTO>>($"{RequestsBase}/categories") ?? [];

    public async Task<IReadOnlyList<string>> GetCatalogUnitNamesAsync()
    {
        const int batchSize = 100;
        var items = new List<VppItemResDTO>();
        for (var skip = 0; ; skip += batchSize)
        {
            var result = await api.GetFromApiWithTotalCountAsync<List<VppItemResDTO>>(
                $"{RequestsBase}/products?distinct=UomName&skip={skip}&top={batchSize}");
            var batch = result.Data ?? [];
            items.AddRange(batch);
            if (batch.Count == 0 || items.Count >= result.TotalCount)
            {
                break;
            }
        }

        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.UomName))
            .Select(item => item.UomName!.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public async Task<RequestPage<VppItemResDTO>> GetCatalogItemsAsync(ProductCatalogQuery query)
    {
        var result = await api.GetFromApiWithTotalCountAsync<List<VppItemResDTO>>(
            BuildCatalogItemsEndpoint(query));
        return new RequestPage<VppItemResDTO>(result.Data ?? [], result.TotalCount);
    }

    public Task<VppPeriodInfoResDTO?> GetPeriodInfoAsync() =>
        api.GetFromApiAsync<VppPeriodInfoResDTO>($"{RequestsBase}/period-info");

    public async Task<IReadOnlyList<VppRequestResDTO>> GetMyOrdersAsync(
        IReadOnlyList<OrderPeriod> periods)
    {
        var queryParams = periods
            .SelectMany(period => new[]
            {
                $"years={period.Year}",
                $"months={period.Month}"
            });
        var endpoint = $"{RequestsBase}/my-orders?{string.Join("&", queryParams)}";
        return await api.GetFromApiAsync<List<VppRequestResDTO>>(endpoint) ?? [];
    }

    public Task<VppRequestResDTO?> GetOrderAsync(Guid orderId) =>
        api.GetFromApiAsync<VppRequestResDTO>($"{RequestsBase}/orders/{orderId}");

    public Task<VppRequestHistoryResDTO?> GetOrderHistoryAsync(Guid orderId) =>
        api.GetFromApiAsync<VppRequestHistoryResDTO>($"{RequestsBase}/orders/{orderId}/history");

    public async Task<RequestStatsPage<VppRequestResDTO>> GetHistoryOrdersAsync(
        OrderHistoryScope scope,
        OrderHistoryQuery query)
    {
        var result = await api.GetFromApiWithStatsAsync<List<VppRequestResDTO>>(
            BuildHistoryOrdersEndpoint(scope, query));
        return new RequestStatsPage<VppRequestResDTO>(
            result.Data ?? [],
            result.TotalCount,
            result.TotalLines,
            result.TotalQty);
    }

    public Task<VppOrderHistorySummaryResDTO?> GetHistorySummaryAsync(
        OrderHistoryScope scope,
        int? fromPeriod,
        int? toPeriod) =>
        api.GetFromApiAsync<VppOrderHistorySummaryResDTO>(
            BuildHistorySummaryEndpoint(scope, fromPeriod, toPeriod));

    public async Task<RequestStatsPage<VppRequestResDTO>> GetPendingAdditionalOrdersAsync(
        PendingAdditionalOrdersQuery query)
    {
        var result = await api.GetFromApiWithStatsAsync<List<VppRequestResDTO>>(
            BuildPendingAdditionalOrdersEndpoint(query));
        return new RequestStatsPage<VppRequestResDTO>(
            result.Data ?? [],
            result.TotalCount,
            result.TotalLines,
            result.TotalQty);
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetOrderFilterValuesAsync(
        OrderFilterValuesQuery query) =>
        await api.GetFromApiAsync<List<Dictionary<string, object?>>>(
            BuildOrderFilterValuesEndpoint(query)) ?? [];

    internal static string BuildCatalogItemsEndpoint(ProductCatalogQuery query)
    {
        var queryParams = new List<string>();
        if (query.CategoryId.HasValue)
        {
            queryParams.Add($"categoryId={query.CategoryId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(query.UnitName))
        {
            var unitFilter = BuildUnitFilter(query.UnitName);
            queryParams.Add($"filter={Uri.EscapeDataString(unitFilter)}");
        }

        queryParams.Add($"skip={Math.Max(0, query.Skip)}");
        queryParams.Add($"top={Math.Max(1, query.Top)}");
        if (!string.IsNullOrWhiteSpace(query.OrderBy))
        {
            queryParams.Add($"orderby={Uri.EscapeDataString(query.OrderBy)}");
        }

        return $"{RequestsBase}/products?{string.Join("&", queryParams)}";
    }

    internal static string BuildHistoryOrdersEndpoint(
        OrderHistoryScope scope,
        OrderHistoryQuery query)
    {
        var queryParams = new List<string>
        {
            $"skip={Math.Max(0, query.Skip)}",
            $"top={Math.Max(1, query.Top)}"
        };
        AppendPeriodRange(queryParams, query.FromPeriod, query.ToPeriod);
        if (query.ExactPeriod.HasValue) queryParams.Add($"exactPeriod={query.ExactPeriod.Value}");
        AddTextQuery(queryParams, "search", query.Search);
        if (query.Status.HasValue) queryParams.Add($"status={query.Status.Value}");
        if (query.IsAdditionalOrder.HasValue)
        {
            queryParams.Add($"isAdditionalOrder={query.IsAdditionalOrder.Value.ToString().ToLowerInvariant()}");
        }

        var action = scope == OrderHistoryScope.Department
            ? "department-order-history"
            : "my-order-history";
        return $"{RequestsBase}/{action}?{string.Join("&", queryParams)}";
    }

    internal static string BuildHistorySummaryEndpoint(
        OrderHistoryScope scope,
        int? fromPeriod,
        int? toPeriod)
    {
        var queryParams = new List<string>();
        AppendPeriodRange(queryParams, fromPeriod, toPeriod);
        var action = scope == OrderHistoryScope.Department
            ? "department-order-history-summary"
            : "my-order-history-summary";
        return queryParams.Count == 0
            ? $"{RequestsBase}/{action}"
            : $"{RequestsBase}/{action}?{string.Join("&", queryParams)}";
    }

    internal static string BuildPendingAdditionalOrdersEndpoint(PendingAdditionalOrdersQuery query)
    {
        var queryParams = new List<string>
        {
            $"skip={Math.Max(0, query.Skip)}",
            $"top={Math.Max(1, query.Top)}"
        };
        AddTextQuery(queryParams, "filter", query.Filter);
        AddTextQuery(
            queryParams,
            "orderby",
            string.IsNullOrWhiteSpace(query.OrderBy) ? "SubmittedDate asc" : query.OrderBy);
        return $"{RequestsBase}/additional-orders/pending?{string.Join("&", queryParams)}";
    }

    internal static string BuildOrderFilterValuesEndpoint(OrderFilterValuesQuery query)
    {
        var queryParams = new List<string>
        {
            $"column={Uri.EscapeDataString(query.Column)}",
            $"scope={GetFilterScopeValue(query.Scope)}"
        };
        AppendPeriodRange(queryParams, query.FromPeriod, query.ToPeriod);
        AddTextQuery(queryParams, "filters", query.FiltersJson);
        if (string.IsNullOrWhiteSpace(query.FiltersJson))
        {
            AddTextQuery(queryParams, "filter", query.Filter);
        }
        AddTextQuery(queryParams, "distinctFilter", query.DistinctFilter);
        return $"{RequestsBase}/order-filter-values?{string.Join("&", queryParams)}";
    }

    private static string BuildUnitFilter(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"UomName == \"{escaped}\"";
    }

    private static void AppendPeriodRange(
        ICollection<string> queryParams,
        int? fromPeriod,
        int? toPeriod)
    {
        if (fromPeriod.HasValue) queryParams.Add($"fromPeriod={fromPeriod.Value}");
        if (toPeriod.HasValue) queryParams.Add($"toPeriod={toPeriod.Value}");
    }

    private static void AddTextQuery(ICollection<string> queryParams, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            queryParams.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
        }
    }

    private static string GetFilterScopeValue(OrderFilterScope scope) => scope switch
    {
        OrderFilterScope.Department => "department",
        OrderFilterScope.Pending => "pending",
        _ => "my-orders"
    };
}
