using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Res.Library;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.CatalogPricing;

public sealed class LookupApiClientTests
{
    [Fact]
    public async Task GetCategoriesAsync_BuildsEncodedFilterSortAndPaging()
    {
        string? endpoint = null;
        var api = new StubApiServices
        {
            GetWithTotalCountAsync = (capturedEndpoint, _) =>
            {
                endpoint = capturedEndpoint;
                return Task.FromResult<(object?, int)>((new List<LookupCategoryResDTO>(), 0));
            }
        };
        var client = new LookupApiClient(api);

        await client.GetCategoriesAsync(new LookupQuery(
            20,
            15,
            "A/B",
            "inactive",
            "Name desc"));

        Assert.NotNull(endpoint);
        Assert.StartsWith("/api/Library/lookup-categories?showDeleted=true&", endpoint);
        Assert.Contains("skip=20", endpoint);
        Assert.Contains("top=15", endpoint);
        Assert.Contains("orderby=Name%20desc", endpoint);
        Assert.Contains("filter=", endpoint);
        Assert.Contains("IsDeleted%20%3D%3D%20true", endpoint);
    }

    [Fact]
    public async Task GetValuesAsync_UsesCategoryAndRetriesFirstPageWhenCurrentPageIsEmpty()
    {
        var categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetWithTotalCountAsync = (endpoint, _) =>
            {
                endpoints.Add(endpoint);
                object data = endpoints.Count == 1
                    ? new List<LookupValueResDTO>()
                    : new List<LookupValueResDTO> { new() { Id = Guid.NewGuid(), Code = "A", Value = "Alpha" } };
                return Task.FromResult<(object?, int)>((data, 1));
            }
        };
        var client = new LookupApiClient(api);

        var result = await client.GetValuesAsync(categoryId, new LookupQuery(30, 15));

        Assert.Equal(2, endpoints.Count);
        Assert.Contains($"lookupCategoryId={categoryId}", endpoints[0]);
        Assert.Contains("skip=30", endpoints[0]);
        Assert.Contains("skip=0", endpoints[1]);
        Assert.Equal(0, result.AppliedSkip);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task MutationMethods_UseCanonicalTypedEndpoints()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var calls = new List<(string Method, string Endpoint, object? Body)>();
        var api = new StubApiServices
        {
            PatchAsync = (endpoint, body, _) =>
            {
                calls.Add(("PATCH", endpoint, body));
                return Task.FromResult<object?>(new LookupCategoryResDTO());
            },
            PostAsync = (endpoint, body, _) =>
            {
                calls.Add(("POST", endpoint, body));
                return Task.FromResult<object?>(new LookupValueResDTO());
            },
            DeleteAsync = endpoint =>
            {
                calls.Add(("DELETE", endpoint, null));
                return Task.FromResult(true);
            }
        };
        var client = new LookupApiClient(api);
        var change = new LookupStatusChange(true, DateTime.UnixEpoch, 42);

        await client.SetCategoryDeletedAsync(id, change);
        await client.CreateValueAsync(new LookupValueResDTO());
        await client.DeleteValueAsync(id);

        Assert.Contains(calls, call =>
            call.Method == "PATCH"
            && call.Endpoint == $"/api/Library/lookup-categories/{id}"
            && call.Body is LookupStatusChange);
        Assert.Contains(calls, call => call.Method == "POST" && call.Endpoint == "/api/Library/lookup-values");
        Assert.Contains(calls, call => call.Method == "DELETE" && call.Endpoint == $"/api/Library/lookup-values/{id}");
    }
}
