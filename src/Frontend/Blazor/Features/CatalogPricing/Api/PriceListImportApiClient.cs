using gtas_vpp_fe.Platform.Browser;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using System.Text.Json;

namespace gtas_vpp_fe.Features.CatalogPricing.Api;

public sealed class PriceListImportApiClient(
    IAPIServices api,
    IBrowserFileDownloadService fileDownloads)
{
    private const string PriceListEndpoint = "/api/vpppricelist";

    public Task<PriceListImportAnalysisResDTO?> AnalyzeAsync(
        Guid priceListId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
        => api.PostFileFromApiAsync<PriceListImportAnalysisResDTO>(
            $"{PriceListEndpoint}/{priceListId}/imports/analyze",
            fileStream,
            fileName,
            contentType,
            cancellationToken);

    public Task<PriceListImportPreviewResDTO?> PreviewAsync(
        Guid priceListId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
        => PreviewAsync(
            priceListId,
            fileStream,
            fileName,
            contentType,
            new Dictionary<int, string>(),
            cancellationToken);

    public Task<PriceListImportPreviewResDTO?> PreviewAsync(
        Guid priceListId,
        Stream fileStream,
        string fileName,
        string contentType,
        IReadOnlyDictionary<int, string> columnMappings,
        CancellationToken cancellationToken = default)
        => api.PostFileFromApiAsync<PriceListImportPreviewResDTO>(
            $"{PriceListEndpoint}/{priceListId}/imports/preview",
            fileStream,
            fileName,
            contentType,
            new Dictionary<string, string>
            {
                ["columnMappingsJson"] = JsonSerializer.Serialize(columnMappings)
            },
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

    public Task<BrowserFileDownloadResult> DownloadTemplateAsync(
        CancellationToken cancellationToken = default)
        => fileDownloads.DownloadFromApiAsync(
            $"{PriceListEndpoint}/imports/template.xlsx",
            cancellationToken);

    public Task<BrowserFileDownloadResult> ExportExcelAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default)
        => fileDownloads.DownloadFromApiAsync(
            $"{PriceListEndpoint}/{priceListId}/export.xlsx",
            cancellationToken);
}
