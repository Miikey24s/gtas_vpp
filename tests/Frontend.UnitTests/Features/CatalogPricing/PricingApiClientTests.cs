using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.CatalogPricing;

public sealed class PricingApiClientTests
{
    [Fact]
    public async Task PriceListQuery_EncodesStatusSearchPagingAndSort()
    {
        string? endpoint = null;
        var api = new StubApiServices
        {
            GetWithTotalCountAsync = (capturedEndpoint, _) =>
            {
                endpoint = capturedEndpoint;
                return Task.FromResult<(object?, int)>((new List<PriceListResDTO>(), 0));
            }
        };
        var client = new PricingApiClient(api);

        await client.GetPriceListsAsync(new PriceListQuery(20, 15, "Office", "Published", "Version desc"));

        Assert.NotNull(endpoint);
        Assert.StartsWith("/api/vpppricelist?showDeleted=true&", endpoint);
        Assert.Contains("Status%20%3D%3D%20%22Published%22", endpoint);
        Assert.Contains("skip=20", endpoint);
        Assert.Contains("top=15", endpoint);
        Assert.Contains("orderby=Version%20desc", endpoint);
        Assert.Contains("search=Office", endpoint);
    }

    [Fact]
    public async Task PriceListLifecycle_UsesCanonicalEndpointsAndTypedBodies()
    {
        var id = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var calls = new List<(string Method, string Endpoint, object? Body)>();
        var api = new StubApiServices
        {
            PostAsync = (endpoint, body, _) =>
            {
                calls.Add(("POST", endpoint, body));
                return Task.FromResult<object?>(new PriceListResDTO());
            },
            PutAsync = (endpoint, body, _) =>
            {
                calls.Add(("PUT", endpoint, body));
                return Task.FromResult<object?>(new PriceListResDTO());
            },
            PatchAsync = (endpoint, body, _) =>
            {
                calls.Add(("PATCH", endpoint, body));
                return Task.FromResult<object?>(new PriceListResDTO());
            },
            DeleteAsync = endpoint =>
            {
                calls.Add(("DELETE", endpoint, null));
                return Task.FromResult(true);
            }
        };
        var client = new PricingApiClient(api);

        await client.CreatePriceListAsync(new PriceListCreateReqDTO());
        await client.UpdatePriceListAsync(id, new PriceListUpdateReqDTO { Id = id });
        await client.SetPriceListDeletedAsync(id, true);
        await client.SetDefaultPriceListAsync(id);
        await client.PublishPriceListAsync(id, new PriceBookStatusReqDTO());
        await client.ExpirePriceListAsync(id, new PriceBookStatusReqDTO());
        await client.ClonePriceListAsync(new PriceListCloneReqDTO { SourceId = id });
        await client.HardDeletePriceListAsync(id);

        Assert.Contains(calls, call => call.Endpoint == "/api/vpppricelist" && call.Body is PriceListCreateReqDTO);
        Assert.Contains(calls, call => call.Endpoint == $"/api/vpppricelist/{id}" && call.Body is PriceListUpdateReqDTO);
        Assert.Contains(calls, call => call.Endpoint == $"/api/vpppricelist/{id}/deleted" && call.Body is PriceListDeletedChange { IsDeleted: true });
        Assert.Contains(calls, call => call.Endpoint == $"/api/vpppricelist/{id}/set-default");
        Assert.Contains(calls, call => call.Endpoint == $"/api/vpppricelist/{id}/publish" && call.Body is PriceBookStatusReqDTO);
        Assert.Contains(calls, call => call.Endpoint == $"/api/vpppricelist/{id}/expire" && call.Body is PriceBookStatusReqDTO);
        Assert.Contains(calls, call => call.Endpoint == "/api/vpppricelist/clone" && call.Body is PriceListCloneReqDTO);
        Assert.Contains(calls, call => call.Method == "DELETE" && call.Endpoint == $"/api/vpppricelist/{id}/hard");
    }
}
