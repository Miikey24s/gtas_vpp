using gtas_vpp_fe.Components;
using gtas_vpp_fe.Helpers;
using Microsoft.Extensions.Localization;

namespace gtas_vpp_fe.Services;

public static class ToastServiceExtensions
{
    /// <summary>
    /// Displays a localized, safe error without exposing exception messages or
    /// backend error codes to the user.
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
