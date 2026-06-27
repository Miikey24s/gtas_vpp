using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Services
{
    public interface ICustomNotificationService
    {
        // Define methods for the custom notification service
        void CustomContentNotification(NotificationSeverity severity, string? summary = null, string? detail = null, int? duration = null, bool? showprogress = null);
        NotificationService _NotificationService { get; }
    }
    public class CustomNotificationService : ICustomNotificationService
    {
        public NotificationService _NotificationService { get; set; }
        public CustomNotificationService(NotificationService notificationService)
        {
            _NotificationService = notificationService;
        }
        public void CustomContentNotification(NotificationSeverity severity, string? summary = null, string? detail = null, int? duration = null, bool? showprogress = null)
        {
            RenderFragment<NotificationService> summaryContent = ns => builder =>
            {
                builder.OpenComponent(0, typeof(RadzenText));
                builder.AddAttribute(1, "TextStyle", TextStyle.H6);
                if (severity == NotificationSeverity.Success)
                {
                    builder.AddAttribute(2, "Style", "color: var(--rz-on-success);");
                }
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
                {
                    b.AddContent(4, summary);
                }));
                builder.CloseComponent();
            };
            RenderFragment<NotificationService> detailContent = ns => builder =>
            {
                builder.OpenComponent(0, typeof(RadzenText));
                builder.AddAttribute(1, "TextStyle", TextStyle.Body1);
                if (severity == NotificationSeverity.Success)
                {
                    builder.AddAttribute(2, "Style", "color: var(--rz-on-success);");
                }
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
                {
                    b.AddContent(4, detail);
                    b.AddMarkupContent(5, "<br/>");
                    b.AddContent(6, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                }));
                builder.CloseComponent();
            };
            _NotificationService.Notify(new NotificationMessage
            {
                Severity = severity,
                Duration = duration ?? 15000,
                ShowProgress = showprogress ?? true,
                Payload = DateTime.Now,
                CloseOnClick = true,
                SummaryContent = summaryContent,
                DetailContent = detailContent
            });
        }
    }
}
