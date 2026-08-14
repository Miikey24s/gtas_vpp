namespace gtas_vpp_be.Service.Services;

public sealed record PriceListExportResult(byte[] Content, string FileName, string ContentType);

public interface IPriceListExportService
{
    Task<PriceListExportResult> ExportExcelAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default);
}
