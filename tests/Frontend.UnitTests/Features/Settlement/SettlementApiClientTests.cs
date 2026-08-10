using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Features.Settlement.Api;
using gtas_vpp_fe.Platform.Browser;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Settlement;

public sealed class SettlementApiClientTests
{
    [Fact]
    public async Task SettlementQueries_UseCanonicalEndpointsAndLoadCompleteOrderSnapshot()
    {
        var getCalls = new List<(string Endpoint, Type ResponseType)>();
        var pageCalls = new List<string>();
        var api = new StubApiServices
        {
            GetAsync = (endpoint, responseType) =>
            {
                getCalls.Add((endpoint, responseType));
                object response = responseType == typeof(PeriodSettlementResDTO)
                    ? new PeriodSettlementResDTO()
                    : responseType == typeof(SettlementRevisionResDTO)
                        ? new SettlementRevisionResDTO()
                    : responseType == typeof(List<SettlementRevisionResDTO>)
                        ? new List<SettlementRevisionResDTO>()
                    : new AggregatedVppResDTO();
                return Task.FromResult<object?>(response);
            },
            GetWithAmountStatsAsync = (endpoint, _) =>
            {
                pageCalls.Add(endpoint);
                object data = pageCalls.Count == 1
                    ? new List<VppRequestResDTO> { new(), new() }
                    : new List<VppRequestResDTO> { new() };
                return Task.FromResult<(object?, int, int, int, long)>((data, 3, 0, 0, 0));
            }
        };
        var client = new SettlementApiClient(api, new RecordingFileDownloadService());

        await client.GetStatusAsync(2026, 7);
        await client.GetCurrentAsync(2026, 7);
        await client.ListVersionsAsync(2026, 7);
        await client.GetDemandAsync(2026, 7);
        var snapshot = await client.GetPeriodOrdersSnapshotAsync(2026, 7, 2);

        Assert.Contains(("/api/periodsettlement/2026/7", typeof(PeriodSettlementResDTO)), getCalls);
        Assert.Contains(("/api/periodsettlement/current/2026/7", typeof(SettlementRevisionResDTO)), getCalls);
        Assert.Contains(("/api/periodsettlement/revisions/2026/7", typeof(List<SettlementRevisionResDTO>)), getCalls);
        Assert.Contains(("/api/VPPRequest/period-demand?year=2026&month=7", typeof(AggregatedVppResDTO)), getCalls);
        Assert.Equal(3, snapshot.Count);
        Assert.Equal(2, pageCalls.Count);
        Assert.Equal(
            "/api/VPPRequest/all-orders?year=2026&month=7&skip=0&top=2&summaryOnly=true",
            pageCalls[0]);
        Assert.Equal(
            "/api/VPPRequest/all-orders?year=2026&month=7&skip=2&top=2&summaryOnly=true",
            pageCalls[1]);
    }

    [Fact]
    public async Task SettlementMutations_PreserveTypedRequestAndResponseContracts()
    {
        var settlementId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var preview = new SettlementPreviewReqDTO
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = DateTime.UnixEpoch
        };
        var confirm = new SettlementConfirmReqDTO
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = DateTime.UnixEpoch,
            InputHash = "preview-hash",
            IdempotencyKey = "confirm-key"
        };
        var correction = new SettlementCorrectionReqDTO
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = DateTime.UnixEpoch,
            InputHash = "preview-hash",
            IdempotencyKey = "correction-key",
            Reason = "Điều chỉnh theo biên bản"
        };
        var calls = new List<(string Endpoint, object? Body, Type ResponseType)>();
        var api = new StubApiServices
        {
            PostAsync = (endpoint, body, responseType) =>
            {
                calls.Add((endpoint, body, responseType));
                return Task.FromResult<object?>(null);
            }
        };
        var client = new SettlementApiClient(api, new RecordingFileDownloadService());

        await client.PreviewAsync(preview);
        await client.ConfirmAsync(confirm);
        await client.CorrectAsync(settlementId, correction);

        Assert.Collection(
            calls,
            call =>
            {
                Assert.Equal("/api/periodsettlement/preview", call.Endpoint);
                Assert.Same(preview, call.Body);
                Assert.Equal(typeof(SettlementPreviewResDTO), call.ResponseType);
            },
            call =>
            {
                Assert.Equal("/api/periodsettlement/confirm", call.Endpoint);
                Assert.Same(confirm, call.Body);
                Assert.Equal(typeof(SettlementRevisionResDTO), call.ResponseType);
            },
            call =>
            {
                Assert.Equal($"/api/periodsettlement/{settlementId}/correct", call.Endpoint);
                Assert.Same(correction, call.Body);
                Assert.Equal(typeof(SettlementRevisionResDTO), call.ResponseType);
            });
    }

    [Fact]
    public async Task SettlementExport_MapsPdfAndExcelAndRejectsUnsupportedFormat()
    {
        var settlementId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var downloads = new RecordingFileDownloadService();
        var client = new SettlementApiClient(new StubApiServices(), downloads);

        await client.ExportAsync(
            settlementId,
            VppFileExportFormat.Pdf,
            TestContext.Current.CancellationToken);
        await client.ExportAsync(
            settlementId,
            VppFileExportFormat.Excel,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                $"/api/periodsettlement/{settlementId}/export.pdf",
                $"/api/periodsettlement/{settlementId}/export.xlsx"
            ],
            downloads.Endpoints);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.ExportAsync(
                settlementId,
                VppFileExportFormat.Csv,
                TestContext.Current.CancellationToken));
    }

    private sealed class RecordingFileDownloadService : IBrowserFileDownloadService
    {
        public List<string> Endpoints { get; } = [];

        public Task<BrowserFileDownloadResult> DownloadFromApiAsync(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            Endpoints.Add(endpoint);
            return Task.FromResult(new BrowserFileDownloadResult(
                "settlement.bin",
                "application/octet-stream",
                42));
        }
    }
}
