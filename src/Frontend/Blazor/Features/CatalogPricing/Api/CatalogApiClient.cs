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
    private const string SupplierEndpoint = "/api/Library/suppliers";
    private const string DepartmentEndpoint = "/api/Library/departments";

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

    public Task<CatalogPage<SupplierResDTO>> GetSuppliersAsync(CatalogQuery query) =>
        GetPageAsync<SupplierResDTO>(
            SupplierEndpoint,
            query,
            "SupplierShortName",
            "SupplierName");

    public Task<LibraryDependencyImpactResDTO?> GetSupplierDependencyImpactAsync(Guid id) =>
        api.GetFromApiAsync<LibraryDependencyImpactResDTO>($"{SupplierEndpoint}/{id}/dependency-impact");

    public Task<SupplierResDTO?> CreateSupplierAsync(SupplierResDTO model) =>
        api.PostFromApiAsync<SupplierResDTO>(SupplierEndpoint, model);

    public Task<SupplierResDTO?> UpdateSupplierAsync(SupplierResDTO model) =>
        api.PatchFromApiAsync<SupplierResDTO>($"{SupplierEndpoint}/{model.Id}", model);

    public Task<SupplierResDTO?> SetSupplierDeletedAsync(
        Guid id,
        CatalogStatusChange change) =>
        api.PatchFromApiAsync<SupplierResDTO>($"{SupplierEndpoint}/{id}", change);

    public Task<bool> DeleteSupplierAsync(Guid id) =>
        api.DeleteFromApiAsync($"{SupplierEndpoint}/{id}");

    public Task<CatalogPage<DepartmentResDTO>> GetDepartmentsAsync(CatalogQuery query) =>
        GetPageAsync<DepartmentResDTO>(
            DepartmentEndpoint,
            query,
            "Code",
            "Name");

    public Task<List<DepartmentResDTO>?> GetActiveDepartmentsAsync() =>
        api.GetFromApiAsync<List<DepartmentResDTO>>($"{DepartmentEndpoint}?showDeleted=false");

    public Task<LibraryDependencyImpactResDTO?> GetDepartmentDependencyImpactAsync(Guid id) =>
        api.GetFromApiAsync<LibraryDependencyImpactResDTO>($"{DepartmentEndpoint}/{id}/dependency-impact");

    public Task<DepartmentResDTO?> CreateDepartmentAsync(DepartmentResDTO model) =>
        api.PostFromApiAsync<DepartmentResDTO>(DepartmentEndpoint, model);

    public Task<DepartmentResDTO?> UpdateDepartmentAsync(DepartmentResDTO model) =>
        api.PatchFromApiAsync<DepartmentResDTO>($"{DepartmentEndpoint}/{model.Id}", model);

    public Task<DepartmentResDTO?> SetDepartmentDeletedAsync(
        Guid id,
        CatalogStatusChange change) =>
        api.PatchFromApiAsync<DepartmentResDTO>($"{DepartmentEndpoint}/{id}", change);

    public Task<bool> DeleteDepartmentAsync(Guid id) =>
        api.DeleteFromApiAsync($"{DepartmentEndpoint}/{id}");

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
