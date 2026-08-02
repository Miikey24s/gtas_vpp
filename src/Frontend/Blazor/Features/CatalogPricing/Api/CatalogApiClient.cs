using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_fe.Features.CatalogPricing.Api;

public sealed record CatalogQuery(
    int Skip,
    int Top,
    string Search = "",
    string? OrderBy = null);

public sealed record CatalogPage<T>(
    IReadOnlyList<T> Items,
    int TotalCount);

public sealed record CatalogStatusChange(
    bool IsDeleted,
    DateTime UpdatedAtUtc,
    int UpdatedByUserId);

public sealed class CatalogApiClient(IAPIServices api)
{
    private const string CategoryEndpoint = "/api/Library/vpp-categories";

    public Task<CatalogPage<VppCategoryResDTO>> GetCategoriesAsync(CatalogQuery query) =>
        GetPageAsync<VppCategoryResDTO>(
            CategoryEndpoint,
            query,
            "VppCategoryCode",
            "VppCategoryName");

    public Task<VppCategoryResDTO?> CreateCategoryAsync(VppCategoryResDTO model) =>
        api.PostFromApiAsync<VppCategoryResDTO>(CategoryEndpoint, model);

    public Task<VppCategoryResDTO?> UpdateCategoryAsync(VppCategoryResDTO model) =>
        api.PatchFromApiAsync<VppCategoryResDTO>($"{CategoryEndpoint}/{model.Id}", model);

    public Task<VppCategoryResDTO?> SetCategoryDeletedAsync(
        Guid id,
        CatalogStatusChange change) =>
        api.PatchFromApiAsync<VppCategoryResDTO>($"{CategoryEndpoint}/{id}", change);

    public Task<bool> DeleteCategoryAsync(Guid id) =>
        api.DeleteFromApiAsync($"{CategoryEndpoint}/{id}");

    private async Task<CatalogPage<T>> GetPageAsync<T>(
        string endpoint,
        CatalogQuery query,
        string codeProperty,
        string nameProperty)
    {
        var requestEndpoint = BuildFilteredEndpoint(
            endpoint,
            query,
            codeProperty,
            nameProperty);
        var result = await api.GetFromApiWithTotalCountAsync<List<T>>(requestEndpoint);
        return new CatalogPage<T>(result.Data ?? [], result.TotalCount);
    }

    internal static string BuildFilteredEndpoint(
        string endpoint,
        CatalogQuery query,
        string codeProperty,
        string nameProperty)
    {
        var queryParams = new List<string> { "showDeleted=true" };
        var search = query.Search.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = search
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal)
                .ToLowerInvariant();
            var filter =
                $"(({codeProperty} ?? \"\").ToLower().Contains(\"{escaped}\") || " +
                $"({nameProperty} ?? \"\").ToLower().Contains(\"{escaped}\"))";
            queryParams.Add($"filter={Uri.EscapeDataString(filter)}");
        }

        queryParams.Add($"skip={Math.Max(0, query.Skip)}");
        queryParams.Add($"top={Math.Max(1, query.Top)}");
        if (!string.IsNullOrWhiteSpace(query.OrderBy))
        {
            queryParams.Add($"orderby={Uri.EscapeDataString(query.OrderBy)}");
        }

        return $"{endpoint}?{string.Join("&", queryParams)}";
    }
}
