using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Res.Library;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class RequestsQueryClientTests
{
    [Fact]
    public async Task CatalogCategories_UseSharedDtoAndCanonicalEndpoint()
    {
        string? endpoint = null;
        var api = new StubApiServices
        {
            GetAsync = (value, type) =>
            {
                endpoint = value;
                Assert.Equal(typeof(List<VppCategoryResDTO>), type);
                return Task.FromResult<object?>(new List<VppCategoryResDTO>());
            }
        };
        var client = new RequestsQueryClient(api);

        await client.GetCatalogCategoriesAsync();

        Assert.Equal("/api/VPPRequest/categories", endpoint);
    }

    [Fact]
    public async Task CatalogUnits_LoadAllDistinctPagesAndNormalizeNames()
    {
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetWithTotalCountAsync = (endpoint, _) =>
            {
                endpoints.Add(endpoint);
                object data = endpoints.Count == 1
                    ? new List<VppItemResDTO>
                    {
                        new() { UomName = "Box" },
                        new() { UomName = "Bottle" }
                    }
                    : new List<VppItemResDTO>
                    {
                        new() { UomName = " box " }
                    };
                return Task.FromResult<(object?, int)>((data, 3));
            }
        };
        var client = new RequestsQueryClient(api);

        var names = await client.GetCatalogUnitNamesAsync();

        Assert.Equal(2, endpoints.Count);
        Assert.Contains("skip=0&top=100", endpoints[0], StringComparison.Ordinal);
        Assert.Contains("skip=100&top=100", endpoints[1], StringComparison.Ordinal);
        Assert.Equal(2, names.Count);
        Assert.Contains(names, name => string.Equals(name, "Box", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, name => string.Equals(name, "Bottle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CatalogItems_EncodeFiltersPagingAndSortAndPreserveTotalCount()
    {
        string? endpoint = null;
        var api = new StubApiServices
        {
            GetWithTotalCountAsync = (value, type) =>
            {
                endpoint = value;
                Assert.Equal(typeof(List<VppItemResDTO>), type);
                return Task.FromResult<(object?, int)>((new List<VppItemResDTO>(), 42));
            }
        };
        var client = new RequestsQueryClient(api);
        var categoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var result = await client.GetCatalogItemsAsync(new ProductCatalogQuery(
            -2,
            50,
            categoryId,
            "giay A4",
            "Box \"A\"",
            "VppName desc"));

        Assert.NotNull(endpoint);
        Assert.Contains($"categoryId={categoryId}", endpoint, StringComparison.Ordinal);
        Assert.Contains("search=giay%20A4", endpoint, StringComparison.Ordinal);
        Assert.Contains("skip=0", endpoint, StringComparison.Ordinal);
        Assert.Contains("top=50", endpoint, StringComparison.Ordinal);
        Assert.Contains("orderby=VppName%20desc", endpoint, StringComparison.Ordinal);
        Assert.Contains(
            "UomName == \"Box \\\"A\\\"\"",
            Uri.UnescapeDataString(endpoint),
            StringComparison.Ordinal);
        Assert.Equal(42, result.TotalCount);
    }
}
