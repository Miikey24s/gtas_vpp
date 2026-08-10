using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_fe.Features.CatalogPricing.Api;

public sealed record PriceListQuery(
    int Skip,
    int Top,
    string Search = "",
    string? Activity = null,
    string? OrderBy = null);

public sealed record PriceListDeletedChange(bool IsDeleted);

public sealed record PricingReferenceData(
    IReadOnlyList<PriceListResDTO> PriceLists,
    IReadOnlyList<SupplierResDTO> Suppliers);

public sealed record ItemPriceQuery(
    Guid SupplierId,
    Guid PriceListId,
    int Skip,
    int Top,
    string Search = "",
    string Category = "",
    string MappingStatus = "",
    string? OrderBy = null);

public sealed record PriceMappingDeletedChange(bool IsDeleted);

public sealed class PricingApiClient(IAPIServices api)
{
    private const string SupplierEndpoint = "/api/Library/suppliers";
    private const string PriceListEndpoint = "/api/vpppricelist";
    private const string PriceEndpoint = "/api/vppprice";
    private const string ItemPriceEndpoint = "/api/vppprice/item-prices";
    private const string SupplierProductMappingEndpoint = "/api/Library/supplier-product-mappings";

    public Task<List<SupplierResDTO>?> GetActiveSuppliersAsync() =>
        api.GetFromApiAsync<List<SupplierResDTO>>($"{SupplierEndpoint}?showDeleted=false");

    public async Task<CatalogPage<PriceListResDTO>> GetPriceListsAsync(PriceListQuery query)
    {
        var endpoint = BuildPriceListEndpoint(query);
        var result = await api.GetFromApiWithTotalCountAsync<List<PriceListResDTO>>(endpoint);
        return new CatalogPage<PriceListResDTO>(result.Data ?? [], result.TotalCount);
    }

    public Task<PriceListResDTO?> CreatePriceListAsync(PriceListCreateReqDTO request) =>
        api.PostFromApiAsync<PriceListResDTO>(PriceListEndpoint, request);

    public Task<PriceListResDTO?> UpdatePriceListAsync(Guid id, PriceListUpdateReqDTO request) =>
        api.PutFromApiAsync<PriceListResDTO>($"{PriceListEndpoint}/{id}", request);

    public Task<PriceListResDTO?> SetPriceListDeletedAsync(Guid id, bool isDeleted) =>
        api.PatchFromApiAsync<PriceListResDTO>(
            $"{PriceListEndpoint}/{id}/deleted",
            new PriceListDeletedChange(isDeleted));

    public Task<bool> HardDeletePriceListAsync(Guid id) =>
        api.DeleteFromApiAsync($"{PriceListEndpoint}/{id}/hard");

    public Task<object?> SetDefaultPriceListAsync(Guid id) =>
        api.PostFromApiAsync<object>($"{PriceListEndpoint}/{id}/set-default", null);

    public Task<PriceListResDTO?> PublishPriceListAsync(
        Guid id,
        PriceBookStatusReqDTO request) =>
        api.PostFromApiAsync<PriceListResDTO>($"{PriceListEndpoint}/{id}/publish", request);

    public Task<PriceListResDTO?> ExpirePriceListAsync(
        Guid id,
        PriceBookStatusReqDTO request) =>
        api.PostFromApiAsync<PriceListResDTO>($"{PriceListEndpoint}/{id}/expire", request);

    public Task<PriceListResDTO?> ClonePriceListAsync(PriceListCloneReqDTO request) =>
        api.PostFromApiAsync<PriceListResDTO>($"{PriceListEndpoint}/clone", request);

    public async Task<PricingReferenceData> GetPricingReferenceDataAsync()
    {
        var priceListsTask = api.GetFromApiAsync<List<PriceListResDTO>>(
            $"{PriceListEndpoint}?showDeleted=true");
        var suppliersTask = api.GetFromApiAsync<List<SupplierResDTO>>(
            $"{SupplierEndpoint}?showDeleted=true");
        await Task.WhenAll(priceListsTask, suppliersTask);
        return new PricingReferenceData(
            await priceListsTask ?? [],
            await suppliersTask ?? []);
    }

    public async Task<CatalogPage<VppItemPriceResDTO>> GetItemPricesAsync(ItemPriceQuery query)
    {
        var endpoint = BuildItemPriceEndpoint(query);
        var result = await api.GetFromApiWithTotalCountAsync<List<VppItemPriceResDTO>>(endpoint);
        return new CatalogPage<VppItemPriceResDTO>(result.Data ?? [], result.TotalCount);
    }

    public async Task<IReadOnlyList<string>> GetItemPriceCategoriesAsync(
        Guid supplierId,
        Guid priceListId)
    {
        const int batchSize = 200;
        var skip = 0;
        var values = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        while (true)
        {
            var endpoint =
                $"{ItemPriceEndpoint}?supplierId={supplierId}&priceListId={priceListId}" +
                $"&showDeleted=true&distinct=CategoryName&skip={skip}&top={batchSize}";
            var result = await api.GetFromApiWithTotalCountAsync<List<VppItemPriceResDTO>>(endpoint);
            var rows = result.Data ?? [];
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.CategoryName))
                {
                    values.Add(row.CategoryName.Trim());
                }
            }

            skip += rows.Count;
            if (rows.Count == 0 || skip >= result.TotalCount)
            {
                break;
            }
        }

        return values.OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    public Task<SupplierProductMappingResDTO?> CreateItemPriceAsync(
        SupplierProductPriceCreateReqDTO request) =>
        api.PostFromApiAsync<SupplierProductMappingResDTO>(PriceEndpoint, request);

    public Task<SupplierProductMappingResDTO?> UpdateItemPriceAsync(
        Guid id,
        SupplierProductPriceUpdateReqDTO request) =>
        api.PutFromApiAsync<SupplierProductMappingResDTO>($"{PriceEndpoint}/{id}", request);

    public Task<SupplierProductMappingResDTO?> SetItemPriceDeletedAsync(Guid id, bool isDeleted) =>
        api.PatchFromApiAsync<SupplierProductMappingResDTO>(
            $"{SupplierProductMappingEndpoint}/{id}",
            new PriceMappingDeletedChange(isDeleted));

    public Task<bool> DeleteItemPriceAsync(Guid id) =>
        api.DeleteFromApiAsync($"{SupplierProductMappingEndpoint}/{id}");

    public Task<object?> SetDefaultItemPriceAsync(Guid id) =>
        api.PostFromApiAsync<object>($"{PriceEndpoint}/{id}/set-default", null);

    internal static string BuildPriceListEndpoint(PriceListQuery query)
    {
        var queryParams = new List<string> { "showDeleted=true" };
        if (!string.IsNullOrWhiteSpace(query.Activity))
        {
            var filter = query.Activity switch
            {
                "active" => "IsDeleted == false && Status == \"Published\"",
                "inactive" => "IsDeleted == true || Status != \"Published\"",
                _ => null
            };
            if (filter is not null)
            {
                queryParams.Add($"filter={Uri.EscapeDataString(filter)}");
            }
        }

        queryParams.Add($"skip={Math.Max(0, query.Skip)}");
        queryParams.Add($"top={Math.Max(1, query.Top)}");
        if (!string.IsNullOrWhiteSpace(query.OrderBy))
        {
            queryParams.Add($"orderby={Uri.EscapeDataString(query.OrderBy)}");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        return $"{PriceListEndpoint}?{string.Join("&", queryParams)}";
    }

    internal static string BuildItemPriceEndpoint(ItemPriceQuery query)
    {
        var queryParams = new List<string>
        {
            $"supplierId={query.SupplierId}",
            $"priceListId={query.PriceListId}",
            "showDeleted=true"
        };
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            filters.Add($"CategoryName == \"{EscapeDynamicString(query.Category)}\"");
        }

        filters.AddRange(query.MappingStatus switch
        {
            "active" => ["PriceMappingId != null", "IsDeleted == false"],
            "missing" => ["PriceMappingId == null"],
            "inactive" => ["PriceMappingId != null", "IsDeleted == true"],
            _ => []
        });
        if (filters.Count > 0)
        {
            queryParams.Add($"filter={Uri.EscapeDataString(string.Join(" && ", filters))}");
        }

        queryParams.Add($"skip={Math.Max(0, query.Skip)}");
        queryParams.Add($"top={Math.Max(1, query.Top)}");
        if (!string.IsNullOrWhiteSpace(query.OrderBy))
        {
            queryParams.Add($"orderby={Uri.EscapeDataString(query.OrderBy)}");
        }

        return $"{ItemPriceEndpoint}?{string.Join("&", queryParams)}";
    }

    private static string EscapeDynamicString(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal);
}
