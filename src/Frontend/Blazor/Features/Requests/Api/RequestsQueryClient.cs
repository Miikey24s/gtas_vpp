using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_fe.Features.Requests.Api;

public sealed record RequestPage<T>(
    IReadOnlyList<T> Items,
    int TotalCount);

public sealed record ProductCatalogQuery(
    int Skip,
    int Top,
    Guid? CategoryId = null,
    string Search = "",
    string? UnitName = null,
    string? OrderBy = null);

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

    private static string BuildUnitFilter(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"UomName == \"{escaped}\"";
    }
}
