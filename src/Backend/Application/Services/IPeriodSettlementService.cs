using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_be.Service.Services
{
    public sealed record SettlementExportResult(byte[] Content, string FileName, string ContentType);

    public interface IPeriodSettlementService
    {
        Task<PeriodSettlementResDTO> SettleAsync(PeriodSettlementReqDTO req, int userId);
        Task<PeriodSettlementResDTO> GetStatusAsync(int y, int m);
        Task<List<PeriodSettlementResDTO>> ListSettledAsync();
        Task<SettlementPreviewResDTO> PreviewAsync(
            SettlementPreviewReqDTO req,
            CancellationToken cancellationToken = default);
        Task<SettlementRevisionResDTO> ConfirmAsync(
            SettlementConfirmReqDTO req,
            int userId,
            CancellationToken cancellationToken = default);
        Task<SettlementRevisionResDTO> CorrectAsync(
            Guid settlementId,
            SettlementCorrectionReqDTO req,
            int userId,
            CancellationToken cancellationToken = default);
        Task<SettlementRevisionResDTO?> GetCurrentAsync(
            int y,
            int m,
            CancellationToken cancellationToken = default);
        Task<List<SettlementRevisionResDTO>> ListRevisionsAsync(
            int y,
            int m,
            CancellationToken cancellationToken = default);
        Task<SettlementExportResult?> ExportPdfAsync(
            Guid settlementId,
            CancellationToken cancellationToken = default);
        Task<SettlementExportResult?> ExportWorkbookAsync(
            Guid settlementId,
            CancellationToken cancellationToken = default);
    }
}
