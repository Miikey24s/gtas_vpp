using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Platform.Browser;
using gtas_vpp_fe.Services;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class RequestsExportClientTests
{
    [Theory]
    [InlineData(VppFileExportFormat.Pdf, "export.pdf")]
    [InlineData(VppFileExportFormat.Excel, "export.xlsx")]
    public async Task ExportOrderAsync_UsesCanonicalOrderEndpoint(
        VppFileExportFormat format,
        string suffix)
    {
        var downloads = new RecordingFileDownloadService();
        var client = new RequestsExportClient(downloads);
        var orderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var result = await client.ExportOrderAsync(
            orderId,
            format,
            TestContext.Current.CancellationToken);

        Assert.Equal($"/api/VPPRequest/orders/{orderId}/{suffix}", downloads.Endpoint);
        Assert.Equal("order-export", result.FileName);
        Assert.Equal(3, result.Size);
    }

    [Fact]
    public async Task ExportOrderAsync_Csv_ThrowsBeforeStartingDownload()
    {
        var downloads = new RecordingFileDownloadService();
        var client = new RequestsExportClient(downloads);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.ExportOrderAsync(
            Guid.NewGuid(),
            VppFileExportFormat.Csv,
            TestContext.Current.CancellationToken));

        Assert.Null(downloads.Endpoint);
    }

    private sealed class RecordingFileDownloadService : IBrowserFileDownloadService
    {
        public string? Endpoint { get; private set; }

        public Task<BrowserFileDownloadResult> DownloadFromApiAsync(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            Endpoint = endpoint;
            return Task.FromResult(new BrowserFileDownloadResult(
                "order-export",
                "application/octet-stream",
                3));
        }
    }
}
