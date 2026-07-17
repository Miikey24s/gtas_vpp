using Microsoft.Extensions.Localization;
using System.Net;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Maps authentication transport failures to safe localized UI messages.
/// Login responses can contain implementation details and must never be shown
/// directly to the user.
/// </summary>
public static class LoginFailureMapper
{
    public static string GetMessage(HttpStatusCode statusCode, IStringLocalizer localizer)
    {
        var resourceKey = statusCode switch
        {
            HttpStatusCode.BadRequest => "LoginRequestInvalid",
            HttpStatusCode.Unauthorized => "LoginInvalidCredentials",
            HttpStatusCode.TooManyRequests => "LoginRateLimited",
            _ => "LoginRequestFailed"
        };

        var localized = localizer[resourceKey];
        return localized.ResourceNotFound
            ? localizer["RequestFailed"].Value
            : localized.Value;
    }
}
