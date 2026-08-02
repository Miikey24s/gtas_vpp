using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
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

    [Fact]
    public async Task OrderReads_UseCanonicalTypedEndpoints()
    {
        var orderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetAsync = (endpoint, type) =>
            {
                endpoints.Add(endpoint);
                object? data = type == typeof(VppPeriodInfoResDTO)
                    ? new VppPeriodInfoResDTO()
                    : type == typeof(List<VppRequestResDTO>)
                        ? new List<VppRequestResDTO>()
                        : type == typeof(VppRequestResDTO)
                            ? new VppRequestResDTO()
                            : type == typeof(VppRequestHistoryResDTO)
                                ? new VppRequestHistoryResDTO()
                                : new VppOrderHistorySummaryResDTO();
                return Task.FromResult<object?>(data);
            }
        };
        var client = new RequestsQueryClient(api);

        await client.GetPeriodInfoAsync();
        await client.GetMyOrdersAsync([new OrderPeriod(2026, 8), new OrderPeriod(2026, 7)]);
        await client.GetOrderAsync(orderId);
        await client.GetOrderHistoryAsync(orderId);
        await client.GetHistorySummaryAsync(OrderHistoryScope.Own, 202601, 202608);

        Assert.Equal(
            [
                "/api/VPPRequest/period-info",
                "/api/VPPRequest/my-orders?years=2026&months=8&years=2026&months=7",
                $"/api/VPPRequest/orders/{orderId}",
                $"/api/VPPRequest/orders/{orderId}/history",
                "/api/VPPRequest/my-order-history-summary?fromPeriod=202601&toPeriod=202608"
            ],
            endpoints);
    }

    [Fact]
    public async Task HistoryAndPendingQueries_PreserveScopeFiltersAndStats()
    {
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetWithStatsAsync = (endpoint, type) =>
            {
                endpoints.Add(endpoint);
                Assert.Equal(typeof(List<VppRequestResDTO>), type);
                return Task.FromResult<(object?, int, int, int)>(
                    (new List<VppRequestResDTO>(), 12, 34, 56));
            }
        };
        var client = new RequestsQueryClient(api);

        var history = await client.GetHistoryOrdersAsync(
            OrderHistoryScope.Department,
            new OrderHistoryQuery(
                20,
                10,
                202601,
                202608,
                202607,
                "REQ 42",
                2,
                true));
        var pending = await client.GetPendingAdditionalOrdersAsync(
            new PendingAdditionalOrdersQuery(0, 20, "DepartmentCode == \"IT\""));

        var historyEndpoint = Uri.UnescapeDataString(endpoints[0]);
        Assert.StartsWith("/api/VPPRequest/department-order-history?", historyEndpoint, StringComparison.Ordinal);
        Assert.Contains("fromPeriod=202601", historyEndpoint, StringComparison.Ordinal);
        Assert.Contains("toPeriod=202608", historyEndpoint, StringComparison.Ordinal);
        Assert.Contains("exactPeriod=202607", historyEndpoint, StringComparison.Ordinal);
        Assert.Contains("search=REQ 42", historyEndpoint, StringComparison.Ordinal);
        Assert.Contains("status=2", historyEndpoint, StringComparison.Ordinal);
        Assert.Contains("isAdditionalOrder=true", historyEndpoint, StringComparison.Ordinal);
        Assert.Contains("filter=DepartmentCode == \"IT\"", Uri.UnescapeDataString(endpoints[1]), StringComparison.Ordinal);
        Assert.Contains("orderby=SubmittedDate asc", Uri.UnescapeDataString(endpoints[1]), StringComparison.Ordinal);
        Assert.Equal((12, 34, 56), (history.TotalCount, history.TotalLines, history.TotalQuantity));
        Assert.Equal((12, 34, 56), (pending.TotalCount, pending.TotalLines, pending.TotalQuantity));
    }

    [Fact]
    public async Task FilterValues_EncodeTypedScopeAndPreferStructuredFilters()
    {
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetAsync = (endpoint, type) =>
            {
                endpoints.Add(endpoint);
                Assert.Equal(typeof(List<Dictionary<string, object?>>), type);
                return Task.FromResult<object?>(new List<Dictionary<string, object?>>());
            }
        };
        var client = new RequestsQueryClient(api);

        await client.GetOrderFilterValuesAsync(new OrderFilterValuesQuery(
            "DepartmentCode",
            OrderFilterScope.Department,
            202601,
            202608,
            "[{\"property\":\"Status\"}]",
            "ignored-filter",
            "IT"));
        await client.GetOrderFilterValuesAsync(new OrderFilterValuesQuery(
            "Status",
            OrderFilterScope.Pending,
            Filter: "Status == 1"));

        var structured = Uri.UnescapeDataString(endpoints[0]);
        Assert.Contains("scope=department", structured, StringComparison.Ordinal);
        Assert.Contains("filters=[{\"property\":\"Status\"}]", structured, StringComparison.Ordinal);
        Assert.DoesNotContain("ignored-filter", structured, StringComparison.Ordinal);
        Assert.Contains("distinctFilter=IT", structured, StringComparison.Ordinal);
        Assert.Contains("scope=pending", endpoints[1], StringComparison.Ordinal);
        Assert.Contains("filter=Status == 1", Uri.UnescapeDataString(endpoints[1]), StringComparison.Ordinal);
    }
}
