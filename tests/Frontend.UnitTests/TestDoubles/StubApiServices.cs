using gtas_vpp_fe.Services;

namespace gtas_vpp_fe.Tests.TestDoubles;

internal sealed class StubApiServices : IAPIServices
{
    public Func<string, Type, Task<object?>>? GetAsync { get; init; }
    public Func<string, Type, Task<(object? Data, int TotalCount)>>? GetWithTotalCountAsync { get; init; }
    public Func<string, Type, Task<(object? Data, int TotalCount, int TotalLines, int TotalQty)>>? GetWithStatsAsync { get; init; }
    public Func<string, Type, Task<(object? Data, int TotalCount, int TotalLines, int TotalQty, long TotalAmount)>>? GetWithAmountStatsAsync { get; init; }
    public Func<string, object?, Type, Task<object?>>? PostAsync { get; init; }
    public Func<string, Stream, string, string, Type, Task<object?>>? PostFileAsync { get; init; }
    public Func<string, Stream, string, string, IReadOnlyDictionary<string, string>, Type, Task<object?>>? PostFileWithFieldsAsync { get; init; }
    public Func<string, object, Type, Task<object?>>? PutAsync { get; init; }
    public Func<string, object, Type, Task<object?>>? PatchAsync { get; init; }
    public Func<string, Task<bool>>? DeleteAsync { get; init; }

    public int GetCallCount { get; private set; }

    public async Task<T?> GetFromApiAsync<T>(string endpoint)
    {
        GetCallCount++;
        return GetAsync is null
            ? default
            : (T?)await GetAsync(endpoint, typeof(T));
    }

    public async Task<(T? Data, int TotalCount)> GetFromApiWithTotalCountAsync<T>(string endpoint)
    {
        if (GetWithTotalCountAsync is null)
        {
            throw new NotSupportedException();
        }

        var result = await GetWithTotalCountAsync(endpoint, typeof(T));
        return ((T?)result.Data, result.TotalCount);
    }

    public async Task<(T? Data, int TotalCount, int TotalLines, int TotalQty)> GetFromApiWithStatsAsync<T>(string endpoint)
    {
        if (GetWithStatsAsync is null)
        {
            throw new NotSupportedException();
        }

        var result = await GetWithStatsAsync(endpoint, typeof(T));
        return ((T?)result.Data, result.TotalCount, result.TotalLines, result.TotalQty);
    }

    public async Task<(T? Data, int TotalCount, int TotalLines, int TotalQty, long TotalAmount)> GetFromApiWithAmountStatsAsync<T>(string endpoint)
    {
        if (GetWithAmountStatsAsync is null)
        {
            throw new NotSupportedException();
        }

        var result = await GetWithAmountStatsAsync(endpoint, typeof(T));
        return ((T?)result.Data, result.TotalCount, result.TotalLines, result.TotalQty, result.TotalAmount);
    }

    public async Task<T?> PostFromApiAsync<T>(string endpoint, object? body) =>
        PostAsync is null
            ? default
            : (T?)await PostAsync(endpoint, body, typeof(T));

    public async Task<T?> PostFileFromApiAsync<T>(
        string endpoint,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default) =>
        PostFileAsync is null
            ? default
            : (T?)await PostFileAsync(endpoint, fileStream, fileName, contentType, typeof(T));

    public async Task<T?> PostFileFromApiAsync<T>(
        string endpoint,
        Stream fileStream,
        string fileName,
        string contentType,
        IReadOnlyDictionary<string, string> formFields,
        CancellationToken cancellationToken = default) =>
        PostFileWithFieldsAsync is not null
            ? (T?)await PostFileWithFieldsAsync(endpoint, fileStream, fileName, contentType, formFields, typeof(T))
            : PostFileAsync is null
                ? default
                : (T?)await PostFileAsync(endpoint, fileStream, fileName, contentType, typeof(T));

    public async Task<T?> PutFromApiAsync<T>(string endpoint, object body) =>
        PutAsync is null
            ? default
            : (T?)await PutAsync(endpoint, body, typeof(T));

    public async Task<T?> PatchFromApiAsync<T>(string endpoint, object body) =>
        PatchAsync is null
            ? default
            : (T?)await PatchAsync(endpoint, body, typeof(T));

    public Task<ApiFileStreamResult> OpenFileFromApiAsync(
        string endpoint,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> DeleteFromApiAsync(string endpoint) =>
        DeleteAsync?.Invoke(endpoint) ?? throw new NotSupportedException();
}
