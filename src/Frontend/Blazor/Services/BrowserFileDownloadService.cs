using Microsoft.JSInterop;

namespace gtas_vpp_fe.Services;

public interface IBrowserFileDownloadService
{
    Task<ApiFileResult> DownloadFromApiAsync(string endpoint);
    Task DownloadAsync(ApiFileResult file);
}

/// <summary>
/// Entry point duy nhất cho download file từ Blazor Server. Route vẫn sở hữu
/// endpoint, permission và feedback; service chỉ sở hữu transport API → browser.
/// </summary>
public sealed class BrowserFileDownloadService(
    IAPIServices apiServices,
    IJSRuntime jsRuntime) : IBrowserFileDownloadService
{
    private readonly IAPIServices _apiServices = apiServices;
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async Task<ApiFileResult> DownloadFromApiAsync(string endpoint)
    {
        var file = await _apiServices.GetFileFromApiAsync(endpoint);
        await DownloadAsync(file);
        return file;
    }

    public async Task DownloadAsync(ApiFileResult file)
    {
        ArgumentNullException.ThrowIfNull(file);
        await using var stream = new MemoryStream(file.Content, writable: false);
        using var streamReference = new DotNetStreamReference(stream);
        await _jsRuntime.InvokeVoidAsync("vppDownload.fromStream", file.FileName, streamReference);
    }
}
