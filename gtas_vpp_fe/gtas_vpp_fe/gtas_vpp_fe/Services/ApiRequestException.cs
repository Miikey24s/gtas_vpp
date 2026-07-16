using System.Net;

namespace gtas_vpp_fe.Services;

/// <summary>
/// Carries the non-sensitive part of a ProblemDetails response across the UI
/// boundary. Raw response bodies are intentionally never used as a user message.
/// </summary>
public sealed class ApiRequestException : HttpRequestException
{
    public ApiRequestException(
        HttpStatusCode statusCode,
        string errorCode,
        string? traceId = null,
        string? safeDetail = null)
        : base(errorCode, inner: null, statusCode)
    {
        ErrorCode = errorCode;
        TraceId = traceId;
        SafeDetail = safeDetail;
    }

    public string ErrorCode { get; }

    public string? TraceId { get; }

    public string? SafeDetail { get; }
}
