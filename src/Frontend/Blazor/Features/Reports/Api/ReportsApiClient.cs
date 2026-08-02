using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Reports;

namespace gtas_vpp_fe.Features.Reports.Api;

public sealed record ReportQuery(string Scope, int? Year = null, int? Month = null);

public sealed class ReportsApiClient(
    IAPIServices apiServices,
    IBrowserFileDownloadService fileDownloads)
{
    private readonly IAPIServices _apiServices = apiServices;
    private readonly IBrowserFileDownloadService _fileDownloads = fileDownloads;

    public Task<ReportSummaryResDTO?> GetSummaryAsync(ReportQuery query) =>
        _apiServices.GetFromApiAsync<ReportSummaryResDTO>(BuildEndpoint("summary", query));

    public Task<ReportInsightResDTO?> GetInsightsAsync(ReportQuery query, string language)
    {
        var normalizedLanguage = language.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "vi";
        return _apiServices.GetFromApiAsync<ReportInsightResDTO>(
            $"{BuildEndpoint("insights", query)}&language={normalizedLanguage}");
    }

    public Task<BrowserFileDownloadResult> ExportAsync(
        ReportQuery query,
        VppFileExportFormat format,
        CancellationToken cancellationToken = default) =>
        _fileDownloads.DownloadFromApiAsync(
            BuildEndpoint(format.ApiSuffix(), query),
            cancellationToken);

    private static string BuildEndpoint(string action, ReportQuery query)
    {
        var parameters = new List<string>
        {
            $"scope={Uri.EscapeDataString(query.Scope)}"
        };
        if (query.Year.HasValue)
        {
            parameters.Add($"year={query.Year.Value}");
        }

        if (query.Month.HasValue)
        {
            parameters.Add($"month={query.Month.Value}");
        }

        return $"api/reports/{action}?{string.Join("&", parameters)}";
    }
}
