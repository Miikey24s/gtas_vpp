using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_fe.Features.CatalogPricing.Api;

public sealed record CatalogQuery(
    int Skip,
    int Top,
    string Search = "",
    string? OrderBy = null,
    string Activity = "",
    Guid? ParentDepartmentId = null,
    bool ParentDepartmentIsRoot = false);

public sealed record CatalogPage<T>(
    IReadOnlyList<T> Items,
    int TotalCount);

public sealed record CatalogStatusChange(
    bool IsDeleted,
    DateTime UpdatedAtUtc,
    int UpdatedByUserId);

public sealed record CatalogItemQuery(
    int Skip,
    int Top,
    string Search = "",
    Guid? CategoryId = null,
    Guid? UomId = null,
    string? OrderBy = null,
    string SupplierName = "",
    string Activity = "");

public sealed record CatalogItemReferenceData(
    IReadOnlyList<VppCategoryResDTO> Categories,
    IReadOnlyList<LookupValueResDTO> Uoms,
    IReadOnlyList<SupplierResDTO> Suppliers);

public sealed record CatalogItemStatusChange(bool IsDeleted);

public sealed class CatalogApiClient(IAPIServices api)
{
    private const string CategoryEndpoint = "/api/Library/vpp-categories";
    private const string SupplierEndpoint = "/api/Library/suppliers";
    private const string DepartmentEndpoint = "/api/Library/departments";
    private const string ItemEndpoint = "/api/catalog/items";
    private const string LookupValueEndpoint = "/api/Library/lookup-values";

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

    public Task<List<DepartmentResDTO>?> GetActiveDepartmentsAsync(
        int? top = null,
        string? orderBy = null) =>
        api.GetFromApiAsync<List<DepartmentResDTO>>(
            BuildActiveDepartmentsEndpoint(top, orderBy));

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

    public async Task<CatalogItemReferenceData> GetItemReferenceDataAsync()
    {
        var categoriesTask = api.GetFromApiAsync<List<VppCategoryResDTO>>(
            $"{CategoryEndpoint}?showDeleted=true");
        var uomsTask = api.GetFromApiAsync<List<LookupValueResDTO>>(
            $"{LookupValueEndpoint}?showDeleted=true");
        var suppliersTask = api.GetFromApiAsync<List<SupplierResDTO>>(
            $"{SupplierEndpoint}?showDeleted=true");
        await Task.WhenAll(categoriesTask, uomsTask, suppliersTask);
        return new CatalogItemReferenceData(
            await categoriesTask ?? [],
            await uomsTask ?? [],
            await suppliersTask ?? []);
    }

    public async Task<CatalogPage<VppItemResDTO>> GetItemsAsync(CatalogItemQuery query)
    {
        var endpoint = BuildItemEndpoint(query);
        var result = await api.GetFromApiWithTotalCountAsync<List<VppItemResDTO>>(endpoint);
        return new CatalogPage<VppItemResDTO>(result.Data ?? [], result.TotalCount);
    }

    public Task<VppItemResDTO?> CreateItemAsync(VppItemCreateRequest request) =>
        api.PostFromApiAsync<VppItemResDTO>(ItemEndpoint, request);

    public Task<VppItemResDTO?> UpdateItemAsync(VppItemUpdateRequest request) =>
        api.PutFromApiAsync<VppItemResDTO>($"{ItemEndpoint}/{request.Id}", request);

    public Task<VppItemResDTO?> SetItemDeletedAsync(Guid id, bool isDeleted) =>
        api.PatchFromApiAsync<VppItemResDTO>(
            $"{ItemEndpoint}/{id}/status",
            new CatalogItemStatusChange(isDeleted));

    public Task<bool> DeleteItemAsync(Guid id) =>
        api.DeleteFromApiAsync($"{ItemEndpoint}/{id}");

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
        var filters = new List<string>();
        var search = query.Search.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add($"searchText={Uri.EscapeDataString(search)}");
        }

        filters.AddRange(query.Activity switch
        {
            "active" => ["IsDeleted == false"],
            "inactive" => ["IsDeleted == true"],
            _ => []
        });

        if (query.ParentDepartmentIsRoot)
        {
            filters.Add("ParentDepartmentId == null");
        }
        else if (query.ParentDepartmentId.HasValue)
        {
            filters.Add($"ParentDepartmentId == \"{query.ParentDepartmentId.Value}\"");
        }

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

        return $"{endpoint}?{string.Join("&", queryParams)}";
    }

    internal static string BuildItemEndpoint(CatalogItemQuery query)
    {
        var queryParams = new List<string>
        {
            $"skip={Math.Max(0, query.Skip)}",
            $"top={Math.Max(1, query.Top)}",
            "showDeleted=true"
        };
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        if (query.CategoryId.HasValue)
        {
            queryParams.Add($"categoryId={query.CategoryId.Value}");
        }

        var filters = new List<string>();
        if (query.UomId.HasValue)
        {
            filters.Add($"UomId == \"{query.UomId.Value}\"");
        }

        if (!string.IsNullOrWhiteSpace(query.SupplierName))
        {
            filters.Add($"DefaultSupplierName == \"{EscapeDynamicString(query.SupplierName.Trim())}\"");
        }

        filters.AddRange(query.Activity switch
        {
            "active" => ["IsDeleted == false"],
            "inactive" => ["IsDeleted == true"],
            _ => []
        });

        if (filters.Count > 0)
        {
            queryParams.Add($"filter={Uri.EscapeDataString(string.Join(" && ", filters))}");
        }

        if (!string.IsNullOrWhiteSpace(query.OrderBy))
        {
            queryParams.Add($"orderby={Uri.EscapeDataString(query.OrderBy)}");
        }

        return $"{ItemEndpoint}?{string.Join("&", queryParams)}";
    }

    internal static string BuildActiveDepartmentsEndpoint(int? top, string? orderBy)
    {
        var queryParams = new List<string>();
        if (top.HasValue)
        {
            queryParams.Add($"top={Math.Max(1, top.Value)}");
        }

        queryParams.Add("showDeleted=false");
        if (!string.IsNullOrWhiteSpace(orderBy))
        {
            queryParams.Add($"orderby={Uri.EscapeDataString(orderBy)}");
        }

        return $"{DepartmentEndpoint}?{string.Join("&", queryParams)}";
    }

    private static string EscapeDynamicString(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal);
}
