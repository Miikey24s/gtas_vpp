using Radzen;

namespace gtas_vpp_fe.Services;

public interface IToastService
{
    void Notify(NotificationMessage message);
    void Notify(NotificationSeverity severity, string summary, string detail);
    void Success(string summary, string? detail = null);
    void Info(string summary, string? detail = null);
    void Warning(string summary, string? detail = null);
    void Error(string summary, string? detail = null);
    void Show(
        NotificationSeverity severity,
        string? summary = null,
        string? detail = null,
        int? duration = null,
        bool? showprogress = null);
}

/// <summary>
/// Single entry point for transient user feedback. Business notifications that
/// must survive a page refresh belong to NotificationInboxState instead.
/// </summary>
public sealed class ToastService(NotificationService notificationService) : IToastService
{
    private readonly NotificationService _notificationService = notificationService;

    public void Notify(NotificationMessage message)
    {
        if (message.Duration is null or 3000)
        {
            message.Duration = message.Severity == NotificationSeverity.Error ? 7000 : 4000;
        }
        message.ShowProgress = true;
        message.CloseOnClick = true;
        _notificationService.Notify(message);
    }

    public void Notify(NotificationSeverity severity, string summary, string detail) =>
        ShowCore(severity, summary, detail);

    public void Success(string summary, string? detail = null) =>
        ShowCore(NotificationSeverity.Success, summary, detail);

    public void Info(string summary, string? detail = null) =>
        ShowCore(NotificationSeverity.Info, summary, detail);

    public void Warning(string summary, string? detail = null) =>
        ShowCore(NotificationSeverity.Warning, summary, detail);

    public void Error(string summary, string? detail = null) =>
        ShowCore(NotificationSeverity.Error, summary, detail);

    public void Show(
        NotificationSeverity severity,
        string? summary = null,
        string? detail = null,
        int? duration = null,
        bool? showprogress = null)
    {
        Notify(new NotificationMessage
        {
            Severity = severity,
            Summary = summary ?? string.Empty,
            Detail = detail ?? string.Empty,
            Duration = duration,
            ShowProgress = showprogress ?? true
        });
    }

    private void ShowCore(NotificationSeverity severity, string summary, string? detail)
    {
        Notify(new NotificationMessage
        {
            Severity = severity,
            Summary = summary,
            Detail = detail ?? string.Empty
        });
    }
}
