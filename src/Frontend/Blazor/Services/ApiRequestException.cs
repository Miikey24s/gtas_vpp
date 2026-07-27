using System.Net;

namespace gtas_vpp_fe.Services;

/// <summary>
/// Mang phần không nhạy cảm của response ProblemDetails qua boundary UI.
/// Chủ động không dùng raw response body làm thông báo cho người dùng.
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
