using Radzen;

namespace gtas_vpp_fe.Helpers;

public static class VppToast
{
    private static NotificationService? _notif;

    public static void Initialize(NotificationService notificationService)
    {
        _notif = notificationService;
    }

    public static void Success(string summary, string detail = "", int duration = 3000)
        => Notify(NotificationSeverity.Success, summary, detail, duration);

    public static void Error(string summary, string detail = "", int duration = 6000)
        => Notify(NotificationSeverity.Error, summary, detail, duration);

    public static void Warning(string summary, string detail = "", int duration = 4000)
        => Notify(NotificationSeverity.Warning, summary, detail, duration);

    public static void Info(string summary, string detail = "", int duration = 3000)
        => Notify(NotificationSeverity.Info, summary, detail, duration);

    private static void Notify(NotificationSeverity severity, string summary, string detail, int duration)
    {
        _notif?.Notify(new NotificationMessage
        {
            Severity = severity,
            Summary = summary,
            Detail = detail,
            Duration = duration,
        });
    }
}
