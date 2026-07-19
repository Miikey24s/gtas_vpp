using gtas_vpp_fe.Services;
using Microsoft.Extensions.Localization;
using System.Net;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Converts transport failures into safe, localized UI text. The mapper is the
/// only place where a trace identifier may be appended to a user-facing error.
/// </summary>
public static class UiErrorMapper
{
    public static string GetErrorCode(Exception exception)
    {
        if (exception is TaskCanceledException)
        {
            return "RequestTimeout";
        }

        if (exception is OperationCanceledException)
        {
            return "RequestCancelled";
        }

        if (exception is ApiRequestException apiException)
        {
            return apiException.ErrorCode switch
            {
                "ValidationError" or "UnprocessableEntity" or "BusinessRuleViolated" => "ValidationFailed",
                "ConcurrencyConflict" => "Conflict",
                "OperationInvalid" or "BadRequest" => "RequestInvalid",
                "InternalServerError" => "ServerError",
                "TooManyRequests" => "RateLimited",
                _ => apiException.ErrorCode
            };
        }

        if (exception is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized })
        {
            return "Unauthorized";
        }

        if (exception is HttpRequestException { StatusCode: HttpStatusCode.Forbidden })
        {
            return "Forbidden";
        }

        if (exception is HttpRequestException { StatusCode: HttpStatusCode.NotFound })
        {
            return "NotFound";
        }

        if (exception is HttpRequestException { StatusCode: HttpStatusCode.Conflict })
        {
            return "Conflict";
        }

        if (exception is HttpRequestException { StatusCode: HttpStatusCode.UnprocessableEntity })
        {
            return "ValidationFailed";
        }

        if (exception is HttpRequestException { StatusCode: HttpStatusCode.BadRequest })
        {
            return "RequestInvalid";
        }

        if (exception is HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests })
        {
            return "RateLimited";
        }

        return "RequestFailed";
    }

    public static string GetMessage(
        Exception exception,
        IStringLocalizer localizer,
        string? fallbackKey = null)
    {
        var key = GetErrorCode(exception);
        var localizedKey = !string.IsNullOrWhiteSpace(fallbackKey)
            && key is "RequestFailed" or "ServerError"
                ? fallbackKey
                : key;
        var localized = localizer[localizedKey];
        var message = localized.ResourceNotFound ? localizer["RequestFailed"].Value : localized.Value;

        if (exception is ApiRequestException { SafeDetail: { Length: > 0 } safeDetail }
            && GetErrorCode(exception) is "Conflict" or "ValidationFailed" or "RequestInvalid")
        {
            message = safeDetail;
        }

        if (exception is ApiRequestException { TraceId: { Length: > 0 } traceId })
        {
            var traceLabel = localizer["TraceId"];
            message = $"{message} ({traceLabel}: {traceId})";
        }

        return message;
    }
}
