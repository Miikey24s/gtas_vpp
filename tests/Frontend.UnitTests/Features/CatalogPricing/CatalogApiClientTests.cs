using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Res.Library;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.CatalogPricing;

public sealed class CatalogApiClientTests
{
    [Fact]
    public async Task GetCategoriesAsync_BuildsCanonicalServerQuery()
    {
        string? endpoint = null;
        var api = new StubApiServices
        {
            GetWithTotalCountAsync = (capturedEndpoint, _) =>
            {
                endpoint = capturedEndpoint;
                return Task.FromResult<(object?, int)>((new List<VppCategoryResDTO>(), 0));
            }
        };
        var client = new CatalogApiClient(api);

        await client.GetCategoriesAsync(new CatalogQuery(10, 25, "Paper", "VppCategoryName desc"));

        Assert.NotNull(endpoint);
        Assert.StartsWith("/api/Library/vpp-categories?showDeleted=true&", endpoint);
        Assert.Contains("skip=10", endpoint);
        Assert.Contains("top=25", endpoint);
        Assert.Contains("orderby=VppCategoryName%20desc", endpoint);
        Assert.Contains("VppCategoryCode", Uri.UnescapeDataString(endpoint));
        Assert.Contains("VppCategoryName", Uri.UnescapeDataString(endpoint));
    }

    [Fact]
    public async Task CategoryMutations_UseTypedCanonicalEndpoints()
    {
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var calls = new List<(string Method, string Endpoint, object? Body)>();
        var api = new StubApiServices
        {
            PostAsync = (endpoint, body, _) =>
            {
                calls.Add(("POST", endpoint, body));
                return Task.FromResult<object?>(new VppCategoryResDTO());
            },
            PatchAsync = (endpoint, body, _) =>
            {
                calls.Add(("PATCH", endpoint, body));
                return Task.FromResult<object?>(new VppCategoryResDTO());
            },
            DeleteAsync = endpoint =>
            {
                calls.Add(("DELETE", endpoint, null));
                return Task.FromResult(true);
            }
        };
        var client = new CatalogApiClient(api);
        var model = new VppCategoryResDTO { Id = id };

        await client.CreateCategoryAsync(model);
        await client.UpdateCategoryAsync(model);
        await client.SetCategoryDeletedAsync(id, new CatalogStatusChange(true, DateTime.UnixEpoch, 42));
        await client.DeleteCategoryAsync(id);

        Assert.Contains(calls, call => call.Method == "POST" && call.Endpoint == "/api/Library/vpp-categories");
        Assert.Equal(2, calls.Count(call => call.Method == "PATCH" && call.Endpoint == $"/api/Library/vpp-categories/{id}"));
        Assert.Contains(calls, call => call.Method == "DELETE" && call.Endpoint == $"/api/Library/vpp-categories/{id}");
    }

    [Fact]
    public async Task SupplierQueriesAndMutations_UseCanonicalEndpoints()
    {
        var id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetWithTotalCountAsync = (endpoint, _) =>
            {
                endpoints.Add(endpoint);
                return Task.FromResult<(object?, int)>((new List<SupplierResDTO>(), 0));
            },
            GetAsync = (endpoint, _) =>
            {
                endpoints.Add(endpoint);
                return Task.FromResult<object?>(new LibraryDependencyImpactResDTO());
            },
            PatchAsync = (endpoint, _, _) =>
            {
                endpoints.Add(endpoint);
                return Task.FromResult<object?>(new SupplierResDTO());
            }
        };
        var client = new CatalogApiClient(api);

        await client.GetSuppliersAsync(new CatalogQuery(0, 15, "office"));
        await client.GetSupplierDependencyImpactAsync(id);
        await client.SetSupplierDeletedAsync(id, new CatalogStatusChange(true, DateTime.UnixEpoch, 42));

        Assert.Contains(endpoints, endpoint => endpoint.StartsWith("/api/Library/suppliers?showDeleted=true&", StringComparison.Ordinal));
        Assert.Contains($"/api/Library/suppliers/{id}/dependency-impact", endpoints);
        Assert.Contains($"/api/Library/suppliers/{id}", endpoints);
    }

    [Fact]
    public async Task DepartmentMethods_PreserveActiveListAndDependencyImpactContracts()
    {
        var id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetAsync = (endpoint, type) =>
            {
                endpoints.Add(endpoint);
                return Task.FromResult<object?>(type == typeof(List<DepartmentResDTO>)
                    ? new List<DepartmentResDTO>()
                    : new LibraryDependencyImpactResDTO());
            },
            GetWithTotalCountAsync = (endpoint, _) =>
            {
                endpoints.Add(endpoint);
                return Task.FromResult<(object?, int)>((new List<DepartmentResDTO>(), 0));
            }
        };
        var client = new CatalogApiClient(api);

        await client.GetActiveDepartmentsAsync();
        await client.GetDepartmentsAsync(new CatalogQuery(0, 15, "IT"));
        await client.GetDepartmentDependencyImpactAsync(id);

        Assert.Contains("/api/Library/departments?showDeleted=false", endpoints);
        Assert.Contains(endpoints, endpoint => endpoint.StartsWith("/api/Library/departments?showDeleted=true&", StringComparison.Ordinal));
        Assert.Contains($"/api/Library/departments/{id}/dependency-impact", endpoints);
    }
}
