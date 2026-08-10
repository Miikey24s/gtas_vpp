using gtas_vpp_fe.Platform.Browser;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_fe.Features.CatalogPricing.Api;

public sealed class PriceListImportApiClient(
    IAPIServices api,
    IBrowserFileDownloadService fileDownloads)
{
    private const string PriceListEndpoint = "/api/vpppricelist";

    public Task<PriceListImportPreviewResDTO?> PreviewAsync(
        Guid priceListId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
        => api.PostFileFromApiAsync<PriceListImportPreviewResDTO>(
            $"{PriceListEndpoint}/{priceListId}/imports/preview",
            fileStream,
            fileName,
            contentType,
            cancellationToken);

    public Task<PriceListImportBatchResDTO?> ConfirmAsync(
        Guid priceListId,
        Guid batchId,
        byte[]? rowVersion)
        => api.PostFromApiAsync<PriceListImportBatchResDTO>(
            $"{PriceListEndpoint}/{priceListId}/imports/{batchId}/confirm",
            new PriceListImportConfirmReqDTO { RowVersion = rowVersion });

    public async Task<IReadOnlyList<PriceListImportBatchResDTO>> ListAsync(Guid priceListId)
        => await api.GetFromApiAsync<List<PriceListImportBatchResDTO>>(
            $"{PriceListEndpoint}/{priceListId}/imports?top=20") ?? [];

    public Task<BrowserFileDownloadResult> DownloadTemplateAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default)
        => fileDownloads.DownloadFromApiAsync(
            $"{PriceListEndpoint}/{priceListId}/imports/template.xlsx",
            cancellationToken);
}
