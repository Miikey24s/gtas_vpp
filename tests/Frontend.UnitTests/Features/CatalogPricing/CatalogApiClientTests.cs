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
}
