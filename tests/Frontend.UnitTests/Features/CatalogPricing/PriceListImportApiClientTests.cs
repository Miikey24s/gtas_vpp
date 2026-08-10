using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Platform.Browser;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.CatalogPricing;

public sealed class PriceListImportApiClientTests
{
    [Fact]
    public async Task ImportClient_UsesTypedPreviewConfirmHistoryAndTemplateEndpoints()
    {
        var priceListId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var batchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var calls = new List<(string Method, string Endpoint, object? Body)>();
        var api = new StubApiServices
        {
            PostFileAsync = (endpoint, _, fileName, contentType, _) =>
            {
                calls.Add(("FILE", endpoint, (fileName, contentType)));
                return Task.FromResult<object?>(new PriceListImportPreviewResDTO { Id = batchId });
            },
            PostAsync = (endpoint, body, _) =>
            {
                calls.Add(("POST", endpoint, body));
                return Task.FromResult<object?>(new PriceListImportBatchResDTO { Id = batchId });
            },
            GetAsync = (endpoint, _) =>
            {
                calls.Add(("GET", endpoint, null));
                return Task.FromResult<object?>(new List<PriceListImportBatchResDTO>());
            }
        };
        var downloads = new RecordingDownloadService();
        var client = new PriceListImportApiClient(api, downloads);
        await using var stream = new MemoryStream([1, 2, 3]);

        await client.PreviewAsync(
            priceListId,
            stream,
            "bang-gia.csv",
            "text/csv",
            TestContext.Current.CancellationToken);
        await client.ConfirmAsync(priceListId, batchId, [1, 2, 3]);
        await client.ListAsync(priceListId);
        await client.DownloadTemplateAsync(priceListId, TestContext.Current.CancellationToken);

        Assert.Contains(calls, call => call.Method == "FILE"
            && call.Endpoint == $"/api/vpppricelist/{priceListId}/imports/preview");
        Assert.Contains(calls, call => call.Method == "POST"
            && call.Endpoint == $"/api/vpppricelist/{priceListId}/imports/{batchId}/confirm"
            && call.Body is PriceListImportConfirmReqDTO);
        Assert.Contains(calls, call => call.Method == "GET"
            && call.Endpoint == $"/api/vpppricelist/{priceListId}/imports?top=20");
        Assert.Equal($"/api/vpppricelist/{priceListId}/imports/template.xlsx", downloads.Endpoint);
    }

    private sealed class RecordingDownloadService : IBrowserFileDownloadService
    {
        public string? Endpoint { get; private set; }

        public Task<BrowserFileDownloadResult> DownloadFromApiAsync(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            Endpoint = endpoint;
            return Task.FromResult(new BrowserFileDownloadResult("template.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 42));
        }
    }
}
