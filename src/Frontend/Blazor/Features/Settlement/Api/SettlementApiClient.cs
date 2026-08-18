using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Platform.Browser;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Features.Settlement.Api;

public sealed class SettlementApiClient(
    IAPIServices api,
    IBrowserFileDownloadService fileDownloads)
{
    private const string RequestsBase = "/api/VPPRequest";
    private const string SettlementBase = "/api/periodsettlement";

    public Task<PeriodSettlementResDTO?> GetStatusAsync(int year, int month) =>
        api.GetFromApiAsync<PeriodSettlementResDTO>($"{SettlementBase}/{year}/{month}");

    public Task<AggregatedVppResDTO?> GetDemandAsync(int year, int month) =>
        api.GetFromApiAsync<AggregatedVppResDTO>(
            $"{RequestsBase}/period-demand?year={year}&month={month}");

    public async Task<IReadOnlyList<VppRequestResDTO>> GetPeriodOrdersSnapshotAsync(
        int year,
        int month,
        int batchSize = 500)
    {
        var normalizedBatchSize = Math.Max(1, batchSize);
        var snapshot = new List<VppRequestResDTO>();
        var skip = 0;
        var expected = int.MaxValue;

        while (skip < expected)
        {
            // Snapshot được sắp xếp ở projection của màn hình. Không gửi orderby để backend
            // giữ nhánh tóm tắt nhẹ, không materialize toàn bộ Items của từng đơn.
            var endpoint =
                $"{RequestsBase}/all-orders?year={year}&month={month}&skip={skip}&top={normalizedBatchSize}&summaryOnly=true";
            var (data, count, _, _, _) = await api
                .GetFromApiWithAmountStatsAsync<List<VppRequestResDTO>>(endpoint);
            var batch = data ?? [];
            expected = count;
            snapshot.AddRange(batch);
            if (batch.Count == 0 || batch.Count < normalizedBatchSize)
            {
                break;
            }

            skip += batch.Count;
        }

        return snapshot;
    }

    public Task<SettlementPreviewResDTO?> PreviewAsync(SettlementPreviewReqDTO request) =>
        api.PostFromApiAsync<SettlementPreviewResDTO>($"{SettlementBase}/preview", request);

    public Task<SettlementRevisionResDTO?> ConfirmAsync(SettlementConfirmReqDTO request) =>
        api.PostFromApiAsync<SettlementRevisionResDTO>($"{SettlementBase}/confirm", request);

    public Task<SettlementRevisionResDTO?> GetCurrentAsync(int year, int month) =>
        api.GetFromApiAsync<SettlementRevisionResDTO>($"{SettlementBase}/current/{year}/{month}");

    public async Task<IReadOnlyList<SettlementRevisionResDTO>> ListVersionsAsync(int year, int month) =>
        await api.GetFromApiAsync<List<SettlementRevisionResDTO>>(
            $"{SettlementBase}/revisions/{year}/{month}?summaryOnly=true") ?? [];

    public Task<SettlementRevisionResDTO?> CorrectAsync(
        Guid settlementId,
        SettlementCorrectionReqDTO request) =>
        api.PostFromApiAsync<SettlementRevisionResDTO>(
            $"{SettlementBase}/{settlementId}/correct",
            request);

    public Task<BrowserFileDownloadResult> ExportAsync(
        Guid settlementId,
        VppFileExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var endpoint = format switch
        {
            VppFileExportFormat.Pdf => $"{SettlementBase}/{settlementId}/export.pdf",
            VppFileExportFormat.Excel => $"{SettlementBase}/{settlementId}/export.xlsx",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
        return fileDownloads.DownloadFromApiAsync(endpoint, cancellationToken);
    }
}
