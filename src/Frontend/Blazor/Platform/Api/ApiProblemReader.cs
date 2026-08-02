using System.Net;
using System.Text.Json;

namespace gtas_vpp_fe.Platform.Api;

public static class ApiProblemReader
{
    public static ApiProblem Read(string content, HttpStatusCode statusCode)
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
            // ASP.NET thường serialize Extensions thành property cấp cao nhất; gateway/test double
            // vẫn có thể trả object "extensions" lồng nhau theo cùng contract an toàn.
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
}

public sealed record ApiProblem(string ErrorCode, string? TraceId, string? SafeDetail);
