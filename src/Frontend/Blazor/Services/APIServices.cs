using gtas_vpp_fe.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace gtas_vpp_fe.Services
{
    public interface IAPIServices
    {
        Task<T?> GetFromApiAsync<T>(string endpoint);
        Task<(T? Data, int TotalCount)> GetFromApiWithTotalCountAsync<T>(string endpoint);
        Task<(T? Data, int TotalCount, int TotalLines, int TotalQty)> GetFromApiWithStatsAsync<T>(string endpoint);
        Task<(T? Data, int TotalCount, int TotalLines, int TotalQty, long TotalAmount)> GetFromApiWithAmountStatsAsync<T>(string endpoint);
        Task<T?> PostFromApiAsync<T>(string endpoint, object? body);
        Task<T?> PutFromApiAsync<T>(string endpoint, object body);
        Task<T?> PatchFromApiAsync<T>(string endpoint, object body);
        Task<ApiFileStreamResult> OpenFileFromApiAsync(
            string endpoint,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteFromApiAsync(string endpoint);
    }
    public class APIServices : IAPIServices
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authProvider;
        private readonly PermissionRefreshSignal _permissionRefreshSignal;
        private readonly IAuthSessionInvalidationCoordinator _sessionInvalidationCoordinator;
        public APIServices(
            HttpClient httpClient,
            AuthenticationStateProvider authProvider,
            PermissionRefreshSignal permissionRefreshSignal,
            IAuthSessionInvalidationCoordinator sessionInvalidationCoordinator)
        {
            _httpClient = httpClient;
            _authProvider = authProvider;
            _permissionRefreshSignal = permissionRefreshSignal;
            _sessionInvalidationCoordinator = sessionInvalidationCoordinator;
        }

        private async Task ApplyAuthorizationHeaderAsync()
        {
            var authState = await _authProvider.GetAuthenticationStateAsync();
            var token = authState.User.Claims.Get(ClaimKeys.AccessToken);

            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        private async Task EnsureSuccessWithDetailsAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var reason = response.Headers.TryGetValues("X-Auth-Reason", out var values)
                        ? values.FirstOrDefault() ?? "session-invalid"
                        : "session-invalid";
                    await _sessionInvalidationCoordinator.InvalidateAsync(reason);
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    await _permissionRefreshSignal.RequestAsync();
                }

                var content = await response.Content.ReadAsStringAsync();
                var problem = ParseProblem(content, response.StatusCode);
                throw new ApiRequestException(
                    response.StatusCode,
                    problem.ErrorCode,
                    problem.TraceId,
                    problem.SafeDetail);
            }
        }

        private static ApiProblem ParseProblem(string content, HttpStatusCode statusCode)
        {
            var fallbackCode = statusCode switch
            {
                HttpStatusCode.Unauthorized => "Unauthorized",
                HttpStatusCode.Forbidden => "Forbidden",
                HttpStatusCode.NotFound => "NotFound",
                HttpStatusCode.Conflict => "Conflict",
                HttpStatusCode.UnprocessableEntity => "UnprocessableEntity",
                HttpStatusCode.BadRequest => "BadRequest",
                HttpStatusCode.TooManyRequests => "RateLimited",
                _ when (int)statusCode >= 500 => "ServerError",
                _ => "RequestFailed"
            };

            if (string.IsNullOrWhiteSpace(content))
            {
                return new ApiProblem(fallbackCode, null, null);
            }

            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                // ASP.NET serialize ProblemDetails.Extensions thành property cấp cao nhất.
                // Đồng thời chấp nhận object "extensions" lồng nhau để gateway và test double
                // có thể dùng một trong hai dạng tương thích RFC.
                var extensions = root.TryGetProperty("extensions", out var extensionNode)
                    ? extensionNode
                    : root;
                var errorCode = GetString(extensions, "errorCode")
                    ?? GetString(root, "code")
                    ?? fallbackCode;
                var traceId = GetString(extensions, "traceId")
                    ?? GetString(root, "traceId");
                var safeDetail = GetBoolean(extensions, "safeDetail")
                    ? GetString(root, "detail") ?? GetString(root, "message")
                    : null;
                return new ApiProblem(errorCode, traceId, safeDetail);
            }
            catch (JsonException)
            {
                return new ApiProblem(fallbackCode, null, null);
            }
        }

        private static string? GetString(JsonElement node, string propertyName) =>
            node.ValueKind == JsonValueKind.Object
                && node.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;

        private static bool GetBoolean(JsonElement node, string propertyName) =>
            node.ValueKind == JsonValueKind.Object
                && node.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.True;

        private sealed record ApiProblem(string ErrorCode, string? TraceId, string? SafeDetail);

        public async Task<T?> GetFromApiAsync<T>(string endpoint)
        {
            await ApplyAuthorizationHeaderAsync();
            using var response = await _httpClient.GetAsync(
                endpoint,
                HttpCompletionOption.ResponseHeadersRead);
            await EnsureSuccessWithDetailsAsync(response);
            return await ReadResponseAsJsonAsync<T>(response);
        }

        public async Task<(T? Data, int TotalCount)> GetFromApiWithTotalCountAsync<T>(string endpoint)
        {
            await ApplyAuthorizationHeaderAsync();
            using var response = await _httpClient.GetAsync(
                endpoint,
                HttpCompletionOption.ResponseHeadersRead);
            await EnsureSuccessWithDetailsAsync(response);

            var totalCount = 0;
            var hasTotalCountHeader = false;
            if (response.Headers.TryGetValues("X-Total-Count", out var headerValues)
                && int.TryParse(headerValues.FirstOrDefault(), out var parsedTotalCount))
            {
                totalCount = parsedTotalCount;
                hasTotalCountHeader = true;
            }

            var data = await ReadResponseAsJsonAsync<T>(response);
            var fallbackCount = TryGetCollectionCount(data);
            if ((!hasTotalCountHeader || totalCount == 0) && fallbackCount > 0)
            {
                totalCount = fallbackCount.Value;
            }

            return (data, totalCount);
        }

        public async Task<(T? Data, int TotalCount, int TotalLines, int TotalQty)> GetFromApiWithStatsAsync<T>(string endpoint)
        {
            await ApplyAuthorizationHeaderAsync();
            using var response = await _httpClient.GetAsync(
                endpoint,
                HttpCompletionOption.ResponseHeadersRead);
            await EnsureSuccessWithDetailsAsync(response);

            int totalCount = 0, totalLines = 0, totalQty = 0;
            if (response.Headers.TryGetValues("X-Total-Count", out var countVals))
                int.TryParse(countVals.FirstOrDefault(), out totalCount);
            if (response.Headers.TryGetValues("X-Total-Lines", out var lineVals))
                int.TryParse(lineVals.FirstOrDefault(), out totalLines);
            if (response.Headers.TryGetValues("X-Total-Qty", out var qtyVals))
                int.TryParse(qtyVals.FirstOrDefault(), out totalQty);

            var data = await ReadResponseAsJsonAsync<T>(response);
            return (data, totalCount, totalLines, totalQty);
        }

        public async Task<(T? Data, int TotalCount, int TotalLines, int TotalQty, long TotalAmount)> GetFromApiWithAmountStatsAsync<T>(string endpoint)
        {
            await ApplyAuthorizationHeaderAsync();
            using var response = await _httpClient.GetAsync(
                endpoint,
                HttpCompletionOption.ResponseHeadersRead);
            await EnsureSuccessWithDetailsAsync(response);

            int totalCount = 0, totalLines = 0, totalQty = 0;
            long totalAmount = 0;
            if (response.Headers.TryGetValues("X-Total-Count", out var countVals))
                int.TryParse(countVals.FirstOrDefault(), out totalCount);
            if (response.Headers.TryGetValues("X-Total-Lines", out var lineVals))
                int.TryParse(lineVals.FirstOrDefault(), out totalLines);
            if (response.Headers.TryGetValues("X-Total-Qty", out var qtyVals))
                int.TryParse(qtyVals.FirstOrDefault(), out totalQty);
            if (response.Headers.TryGetValues("X-Total-Amount", out var amountVals))
                long.TryParse(amountVals.FirstOrDefault(), out totalAmount);

            var data = await ReadResponseAsJsonAsync<T>(response);
            return (data, totalCount, totalLines, totalQty, totalAmount);
        }

        public async Task<T?> PostFromApiAsync<T>(string endpoint, object? body)
        {
            await ApplyAuthorizationHeaderAsync();
            using var response = await _httpClient.PostAsJsonAsync(endpoint, body);
            await EnsureSuccessWithDetailsAsync(response);
            return await ReadResponseAsJsonAsync<T>(response);
        }

        public async Task<T?> PutFromApiAsync<T>(string endpoint, object body)
        {
            await ApplyAuthorizationHeaderAsync();
            using var response = await _httpClient.PutAsJsonAsync(endpoint, body);
            await EnsureSuccessWithDetailsAsync(response);
            return await ReadResponseAsJsonAsync<T>(response);
        }

        public async Task<T?> PatchFromApiAsync<T>(string endpoint, object body)
        {
            await ApplyAuthorizationHeaderAsync();
            using var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
            {
                Content = JsonContent.Create(body)
            };

            using var response = await _httpClient.SendAsync(request);
            await EnsureSuccessWithDetailsAsync(response);
            return await ReadResponseAsJsonAsync<T>(response);
        }

        public async Task<ApiFileStreamResult> OpenFileFromApiAsync(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            await ApplyAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync(
                endpoint,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            try
            {
                await EnsureSuccessWithDetailsAsync(response);
                if (response.Content.Headers.ContentLength == 0)
                {
                    throw new InvalidDataException("The export response was empty.");
                }

                var fileName = NormalizeDownloadFileName(
                    response.Content.Headers.ContentDisposition?.FileNameStar
                    ?? response.Content.Headers.ContentDisposition?.FileName);
                var contentType = response.Content.Headers.ContentType?.ToString()
                    ?? "application/octet-stream";
                var content = await response.Content.ReadAsStreamAsync(cancellationToken);
                return new ApiFileStreamResult(response, content, fileName, contentType);
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }

        private static string NormalizeDownloadFileName(string? value)
        {
            var raw = value?.Trim('"') ?? "download.bin";
            string decoded;
            try
            {
                decoded = Uri.UnescapeDataString(raw);
            }
            catch (UriFormatException)
            {
                decoded = raw;
            }

            var fileName = Path.GetFileName(decoded);
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(character, '-');
            }

            return string.IsNullOrWhiteSpace(fileName) ? "download.bin" : fileName;
        }

        private static async Task<T?> ReadResponseAsJsonAsync<T>(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.NoContent
                || response.Content.Headers.ContentLength == 0)
            {
                return default;
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(contentStream, JsonOptions);
        }

        private static int? TryGetCollectionCount<T>(T? data)
        {
            if (data is null || data is string)
            {
                return null;
            }

            if (data is System.Collections.ICollection collection)
            {
                return collection.Count;
            }

            if (data is System.Collections.IEnumerable enumerable)
            {
                var count = 0;
                foreach (var _ in enumerable)
                {
                    count++;
                }

                return count;
            }

            return null;
        }

        public async Task<bool> DeleteFromApiAsync(string endpoint)
        {
            await ApplyAuthorizationHeaderAsync();
            using var response = await _httpClient.DeleteAsync(endpoint);
            await EnsureSuccessWithDetailsAsync(response);
            return true;
        }
    }

    public sealed class ApiFileStreamResult(
        HttpResponseMessage response,
        Stream content,
        string fileName,
        string contentType) : IAsyncDisposable
    {
        private readonly HttpResponseMessage _response = response;

        public Stream Content { get; } = content;
        public string FileName { get; } = fileName;
        public string ContentType { get; } = contentType;
        public long? ContentLength => _response.Content.Headers.ContentLength;

        public async ValueTask DisposeAsync()
        {
            await Content.DisposeAsync();
            _response.Dispose();
        }
    }
}
