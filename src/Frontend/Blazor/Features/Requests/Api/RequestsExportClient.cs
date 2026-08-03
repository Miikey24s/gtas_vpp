using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Platform.Browser;
using gtas_vpp_fe.Services;

namespace gtas_vpp_fe.Features.Requests.Api;

public sealed class RequestsExportClient(IBrowserFileDownloadService fileDownloads)
{
    private const string OrdersBase = "/api/VPPRequest/orders";

    public Task<BrowserFileDownloadResult> ExportOrderAsync(
        Guid orderId,
        VppFileExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var suffix = format switch
        {
            VppFileExportFormat.Pdf => "export.pdf",
            VppFileExportFormat.Excel => "export.xlsx",
            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Order export supports only PDF and Excel.")
        };

        return fileDownloads.DownloadFromApiAsync(
            $"{OrdersBase}/{orderId}/{suffix}",
            cancellationToken);
    }
}
