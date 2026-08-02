using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_fe.Features.CatalogPricing.Api;

public sealed record LookupQuery(
    int Skip,
    int Top,
    string Search = "",
    string Status = "",
    string? OrderBy = null);

public sealed record LookupPage<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int AppliedSkip);

public sealed record LookupStatusChange(
    bool IsDeleted,
    DateTime UpdatedAtUtc,
    int UpdatedByUserId);

public sealed class LookupApiClient(IAPIServices api)
{
    private const string CategoryEndpoint = "/api/Library/lookup-categories";
    private const string ValueEndpoint = "/api/Library/lookup-values";

    public Task<LookupPage<LookupCategoryResDTO>> GetCategoriesAsync(LookupQuery query) =>
        GetPageAsync<LookupCategoryResDTO>(CategoryEndpoint, query, "Code", "Name");

    public Task<LookupPage<LookupValueResDTO>> GetValuesAsync(
        Guid categoryId,
        LookupQuery query) =>
        GetPageAsync<LookupValueResDTO>(
            ValueEndpoint,
            query,
            "Code",
            "Value",
            $"lookupCategoryId={categoryId}");

    public Task<LibraryDependencyImpactResDTO?> GetCategoryDependencyImpactAsync(Guid id) =>
        api.GetFromApiAsync<LibraryDependencyImpactResDTO>($"{CategoryEndpoint}/{id}/dependency-impact");

    public Task<LibraryDependencyImpactResDTO?> GetValueDependencyImpactAsync(Guid id) =>
        api.GetFromApiAsync<LibraryDependencyImpactResDTO>($"{ValueEndpoint}/{id}/dependency-impact");

    public Task<LookupCategoryResDTO?> SetCategoryDeletedAsync(
        Guid id,
        LookupStatusChange change) =>
        api.PatchFromApiAsync<LookupCategoryResDTO>($"{CategoryEndpoint}/{id}", change);

    public Task<LookupValueResDTO?> SetValueDeletedAsync(
        Guid id,
        LookupStatusChange change) =>
        api.PatchFromApiAsync<LookupValueResDTO>($"{ValueEndpoint}/{id}", change);

    public Task<LookupCategoryResDTO?> CreateCategoryAsync(LookupCategoryResDTO model) =>
        api.PostFromApiAsync<LookupCategoryResDTO>(CategoryEndpoint, model);

    public Task<LookupCategoryResDTO?> UpdateCategoryAsync(LookupCategoryResDTO model) =>
        api.PutFromApiAsync<LookupCategoryResDTO>(CategoryEndpoint, model);

    public Task<LookupValueResDTO?> CreateValueAsync(LookupValueResDTO model) =>
        api.PostFromApiAsync<LookupValueResDTO>(ValueEndpoint, model);

    public Task<LookupValueResDTO?> UpdateValueAsync(LookupValueResDTO model) =>
        api.PutFromApiAsync<LookupValueResDTO>(ValueEndpoint, model);

    public Task<bool> DeleteCategoryAsync(Guid id) =>
        api.DeleteFromApiAsync($"{CategoryEndpoint}/{id}");

    public Task<bool> DeleteValueAsync(Guid id) =>
        api.DeleteFromApiAsync($"{ValueEndpoint}/{id}");

    private async Task<LookupPage<T>> GetPageAsync<T>(
        string endpoint,
        LookupQuery query,
        string codeProperty,
        string nameProperty,
        string? fixedQuery = null)
    {
        var appliedSkip = Math.Max(0, query.Skip);
        var requestEndpoint = BuildEndpoint(
            endpoint,
            query with { Skip = appliedSkip },
            codeProperty,
            nameProperty,
            fixedQuery);
        var result = await api.GetFromApiWithTotalCountAsync<List<T>>(requestEndpoint);

        if (result.TotalCount > 0 && (result.Data?.Count ?? 0) == 0 && appliedSkip > 0)
        {
            appliedSkip = 0;
            requestEndpoint = BuildEndpoint(
                endpoint,
                query with { Skip = 0 },
                codeProperty,
                nameProperty,
                fixedQuery);
            result = await api.GetFromApiWithTotalCountAsync<List<T>>(requestEndpoint);
        }

        return new LookupPage<T>(result.Data ?? [], result.TotalCount, appliedSkip);
    }

    internal static string BuildEndpoint(
        string endpoint,
        LookupQuery query,
        string codeProperty,
        string nameProperty,
        string? fixedQuery = null)
    {
        var queryParams = new List<string> { "showDeleted=true" };
        if (!string.IsNullOrWhiteSpace(fixedQuery))
        {
            queryParams.Add(fixedQuery);
        }

        var toolbarFilter = BuildFilter(query.Search, query.Status, codeProperty, nameProperty);
        if (!string.IsNullOrWhiteSpace(toolbarFilter))
        {
            queryParams.Add($"filter={Uri.EscapeDataString(toolbarFilter)}");
        }

        queryParams.Add($"skip={Math.Max(0, query.Skip)}");
        queryParams.Add($"top={Math.Max(1, query.Top)}");
        if (!string.IsNullOrWhiteSpace(query.OrderBy))
        {
            queryParams.Add($"orderby={Uri.EscapeDataString(query.OrderBy)}");
        }

        return $"{endpoint}?{string.Join("&", queryParams)}";
    }

    private static string? BuildFilter(
        string search,
        string status,
        string codeProperty,
        string nameProperty)
    {
        var clauses = new List<string>();
        var normalizedSearch = search.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var escaped = normalizedSearch
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal)
                .ToLowerInvariant();
            clauses.Add(
                $"(({codeProperty} ?? \"\").ToLower().Contains(\"{escaped}\") || " +
                $"({nameProperty} ?? \"\").ToLower().Contains(\"{escaped}\"))");
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            clauses.Add("IsDeleted == false");
        }
        else if (string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            clauses.Add("IsDeleted == true");
        }

        return clauses.Count == 0 ? null : string.Join(" && ", clauses);
    }
}
