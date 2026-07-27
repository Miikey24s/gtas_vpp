using Microsoft.Extensions.Localization;
using System.Net;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Ánh xạ lỗi transport xác thực sang thông báo UI an toàn đã localization.
/// Response đăng nhập có thể chứa chi tiết implementation và không bao giờ được
/// hiển thị trực tiếp cho user.
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
