using gtas_vpp_fe.Services;
using Microsoft.JSInterop;

namespace gtas_vpp_fe.Platform.Browser;

public interface IBrowserFileDownloadService
{
    Task<BrowserFileDownloadResult> DownloadFromApiAsync(
        string endpoint,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Entry point duy nhất cho download file từ Blazor Server. Feature client sở hữu
/// endpoint; route giữ permission/feedback, service chỉ sở hữu transport API → browser.
/// </summary>
public sealed class BrowserFileDownloadService(
    IAPIServices apiServices,
    IJSRuntime jsRuntime) : IBrowserFileDownloadService
{
    private readonly IAPIServices _apiServices = apiServices;
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async Task<BrowserFileDownloadResult> DownloadFromApiAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        await using var file = await _apiServices.OpenFileFromApiAsync(endpoint, cancellationToken);
        using var streamReference = new DotNetStreamReference(file.Content);
        var size = await _jsRuntime.InvokeAsync<long>(
            "vppDownload.fromStream",
            cancellationToken,
            file.FileName,
            file.ContentType,
            streamReference);
        if (size <= 0)
        {
            throw new InvalidDataException("The export response was empty.");
        }

        return new BrowserFileDownloadResult(file.FileName, file.ContentType, size);
    }
}

public sealed record BrowserFileDownloadResult(string FileName, string ContentType, long Size);
