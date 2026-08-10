using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.CatalogPricing;

public sealed class PricingApiClientTests
{
    [Fact]
    public async Task PriceListQuery_EncodesActivitySearchPagingAndSort()
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

        await client.GetPriceListsAsync(new PriceListQuery(20, 15, "Office", "active", "Version desc"));

        Assert.NotNull(endpoint);
        Assert.StartsWith("/api/vpppricelist?showDeleted=true&", endpoint);
        Assert.Contains("IsDeleted%20%3D%3D%20false%20%26%26%20Status%20%3D%3D%20%22Published%22", endpoint);
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

    [Fact]
    public async Task ItemPriceContracts_KeepFiltersCategoriesAndMutationEndpoints()
    {
        var supplierId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var priceListId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var mappingId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var calls = new List<(string Method, string Endpoint, object? Body)>();
        var pageCall = 0;
        var api = new StubApiServices
        {
            GetAsync = (endpoint, type) =>
            {
                calls.Add(("GET", endpoint, null));
                object data = type == typeof(List<PriceListResDTO>)
                    ? new List<PriceListResDTO>()
                    : new List<SupplierResDTO>();
                return Task.FromResult<object?>(data);
            },
            GetWithTotalCountAsync = (endpoint, _) =>
            {
                calls.Add(("GET_PAGE", endpoint, null));
                pageCall++;
                object data = pageCall == 2
                    ? new List<VppItemPriceResDTO> { new() { CategoryName = "Paper" } }
                    : new List<VppItemPriceResDTO>();
                return Task.FromResult<(object?, int)>((data, pageCall == 2 ? 1 : 0));
            },
            PostAsync = (endpoint, body, _) =>
            {
                calls.Add(("POST", endpoint, body));
                return Task.FromResult<object?>(new SupplierProductMappingResDTO());
            },
            PutAsync = (endpoint, body, _) =>
            {
                calls.Add(("PUT", endpoint, body));
                return Task.FromResult<object?>(new SupplierProductMappingResDTO());
            },
            PatchAsync = (endpoint, body, _) =>
            {
                calls.Add(("PATCH", endpoint, body));
                return Task.FromResult<object?>(new SupplierProductMappingResDTO());
            },
            DeleteAsync = endpoint =>
            {
                calls.Add(("DELETE", endpoint, null));
                return Task.FromResult(true);
            }
        };
        var client = new PricingApiClient(api);

        await client.GetPricingReferenceDataAsync();
        await client.GetItemPricesAsync(new ItemPriceQuery(
            supplierId,
            priceListId,
            10,
            20,
            "pen",
            "Paper",
            "active",
            "VppName"));
        await client.GetItemPriceCategoriesAsync(supplierId, priceListId);
        await client.CreateItemPriceAsync(new SupplierProductPriceCreateReqDTO());
        await client.UpdateItemPriceAsync(mappingId, new SupplierProductPriceUpdateReqDTO { Id = mappingId });
        await client.SetItemPriceDeletedAsync(mappingId, true);
        await client.SetDefaultItemPriceAsync(mappingId);
        await client.DeleteItemPriceAsync(mappingId);

        Assert.Contains(calls, call => call.Endpoint == "/api/vpppricelist?showDeleted=true");
        Assert.Contains(calls, call => call.Endpoint == "/api/Library/suppliers?showDeleted=true");
        var itemPage = calls.First(call => call.Method == "GET_PAGE").Endpoint;
        Assert.Contains($"supplierId={supplierId}", itemPage);
        Assert.Contains("CategoryName%20%3D%3D%20%22Paper%22", itemPage);
        Assert.Contains("PriceMappingId%20%21%3D%20null", itemPage);
        Assert.Contains(calls, call => call.Endpoint == "/api/vppprice" && call.Body is SupplierProductPriceCreateReqDTO);
        Assert.Contains(calls, call => call.Endpoint == $"/api/vppprice/{mappingId}" && call.Body is SupplierProductPriceUpdateReqDTO);
        Assert.Contains(calls, call => call.Endpoint == $"/api/Library/supplier-product-mappings/{mappingId}"
            && call.Body is PriceMappingDeletedChange { IsDeleted: true });
        Assert.Contains(calls, call => call.Endpoint == $"/api/vppprice/{mappingId}/set-default");
        Assert.Contains(calls, call => call.Method == "DELETE"
            && call.Endpoint == $"/api/Library/supplier-product-mappings/{mappingId}");
    }
}
