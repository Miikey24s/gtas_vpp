using gtas_vpp_fe.Components;
using gtas_vpp_fe.Helpers;
using Microsoft.Extensions.Localization;

namespace gtas_vpp_fe.Services;

public static class ToastServiceExtensions
{
    /// <summary>
    /// Hiển thị lỗi an toàn đã localization, không lộ exception message hoặc
    /// mã lỗi backend cho người dùng.
    /// </summary>
    public static void Error(
        this IToastService toastService,
        Exception exception,
        IStringLocalizer<App> localizer,
        string? fallbackKey = null)
    {
        ArgumentNullException.ThrowIfNull(toastService);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(localizer);

        toastService.Error(
            localizer["Error"].Value,
            UiErrorMapper.GetMessage(exception, localizer, fallbackKey));
    }
}
