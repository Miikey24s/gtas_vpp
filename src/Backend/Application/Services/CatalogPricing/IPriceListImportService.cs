using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_be.Service.Services;

public sealed record PriceListImportTemplateResult(byte[] Content, string FileName, string ContentType);

public interface IPriceListImportService
{
    Task<PriceListImportPreviewResDTO> PreviewAsync(
        Guid priceListId,
        string fileName,
        Stream content,
        int userId,
        CancellationToken cancellationToken = default);

    Task<PriceListImportBatchResDTO> ConfirmAsync(
        Guid priceListId,
        Guid batchId,
        byte[]? rowVersion,
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceListImportBatchResDTO>> ListAsync(
        Guid priceListId,
        int top = 20,
        CancellationToken cancellationToken = default);

    Task<PriceListImportTemplateResult> BuildTemplateAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default);
}
