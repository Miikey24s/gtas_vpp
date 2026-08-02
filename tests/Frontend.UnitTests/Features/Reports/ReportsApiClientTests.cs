using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Features.Reports.Api;
using gtas_vpp_fe.Services;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Reports;

public sealed class ReportsApiClientTests
{
    [Fact]
    public async Task GetSummaryAsync_BuildsEncodedOptionalQuery()
    {
        var api = new RecordingApiServices();
        var client = new ReportsApiClient(api, new RecordingFileDownloadService());

        await client.GetSummaryAsync(new ReportQuery("department & branch", 2026, 8));

        Assert.Equal(
            "api/reports/summary?scope=department%20%26%20branch&year=2026&month=8",
            api.LastEndpoint);
    }

    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("vi-VN", "vi")]
    public async Task GetInsightsAsync_NormalizesSupportedLanguage(string language, string expected)
    {
        var api = new RecordingApiServices();
        var client = new ReportsApiClient(api, new RecordingFileDownloadService());

        await client.GetInsightsAsync(new ReportQuery("own"), language);

        Assert.Equal($"api/reports/insights?scope=own&language={expected}", api.LastEndpoint);
    }

    [Theory]
    [InlineData(VppFileExportFormat.Pdf, "export.pdf")]
    [InlineData(VppFileExportFormat.Excel, "export.xlsx")]
    [InlineData(VppFileExportFormat.Csv, "export")]
    public async Task ExportAsync_MapsFormatToCanonicalEndpoint(
        VppFileExportFormat format,
        string expectedAction)
    {
        var downloads = new RecordingFileDownloadService();
        var client = new ReportsApiClient(new RecordingApiServices(), downloads);

        await client.ExportAsync(
            new ReportQuery("all", 2025, 12),
            format,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            $"api/reports/{expectedAction}?scope=all&year=2025&month=12",
            downloads.LastEndpoint);
    }

    private sealed class RecordingApiServices : IAPIServices
    {
        public string? LastEndpoint { get; private set; }

        public Task<T?> GetFromApiAsync<T>(string endpoint)
        {
            LastEndpoint = endpoint;
            return Task.FromResult<T?>(default);
        }

        public Task<(T? Data, int TotalCount)> GetFromApiWithTotalCountAsync<T>(string endpoint) =>
            throw new NotSupportedException();

        public Task<(T? Data, int TotalCount, int TotalLines, int TotalQty)> GetFromApiWithStatsAsync<T>(string endpoint) =>
            throw new NotSupportedException();

        public Task<(T? Data, int TotalCount, int TotalLines, int TotalQty, long TotalAmount)> GetFromApiWithAmountStatsAsync<T>(string endpoint) =>
            throw new NotSupportedException();

        public Task<T?> PostFromApiAsync<T>(string endpoint, object? body) =>
            throw new NotSupportedException();

        public Task<T?> PutFromApiAsync<T>(string endpoint, object body) =>
            throw new NotSupportedException();

        public Task<T?> PatchFromApiAsync<T>(string endpoint, object body) =>
            throw new NotSupportedException();

        public Task<ApiFileStreamResult> OpenFileFromApiAsync(
            string endpoint,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteFromApiAsync(string endpoint) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingFileDownloadService : IBrowserFileDownloadService
    {
        public string? LastEndpoint { get; private set; }

        public Task<BrowserFileDownloadResult> DownloadFromApiAsync(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            LastEndpoint = endpoint;
            return Task.FromResult(new BrowserFileDownloadResult("report.pdf", "application/pdf", 42));
        }
    }
}
