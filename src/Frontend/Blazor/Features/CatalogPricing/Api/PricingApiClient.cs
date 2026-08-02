using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_fe.Features.CatalogPricing.Api;

public sealed record PriceListQuery(
    int Skip,
    int Top,
    string Search = "",
    string? Status = null,
    string? OrderBy = null);

public sealed record PriceListDeletedChange(bool IsDeleted);

public sealed class PricingApiClient(IAPIServices api)
{
    private const string SupplierEndpoint = "/api/Library/suppliers";
    private const string PriceListEndpoint = "/api/vpppricelist";

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

    internal static string BuildPriceListEndpoint(PriceListQuery query)
    {
        var queryParams = new List<string> { "showDeleted=true" };
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var filter = $"Status == \"{EscapeDynamicString(query.Status)}\"";
            queryParams.Add($"filter={Uri.EscapeDataString(filter)}");
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

    private static string EscapeDynamicString(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal);
}
