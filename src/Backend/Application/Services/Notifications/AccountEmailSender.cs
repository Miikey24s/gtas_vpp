using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Notifications;

public sealed class AccountEmailOptions
{
    public const string SectionName = "EmailNotifications";

    public bool Enabled { get; set; }

    public bool RequireConfirmationWhenEnabled { get; set; } = true;

    public string PublicBaseUrl { get; set; } = "https://gtas-vpp.annam.id.vn";

    public string FromAddress { get; set; } = "noreply@gtas-vpp.local";

    public string FromDisplayName { get; set; } = "GTAS VPP";

    public string SmtpHost { get; set; } = "localhost";

    public int SmtpPort { get; set; } = 1025;

    public bool UseSsl { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }
}

public sealed record AccountEmailMessage(
    string Recipient,
    string Subject,
    string TextBody,
    string? HtmlBody = null);

public interface IAccountEmailSender
{
    Task SendAsync(AccountEmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// SMTP adapter cho Mailpit hoặc relay nội bộ đã cấu hình. Khi Enabled=false,
/// adapter chủ động không làm gì: inbox bền vững trong ứng dụng vẫn là fallback
/// và môi trường local/demo không cần email credential.
/// </summary>
public sealed class SmtpAccountEmailSender(
    IOptions<AccountEmailOptions> options,
    ILogger<SmtpAccountEmailSender> logger) : IAccountEmailSender
{
    private readonly AccountEmailOptions _options = options.Value;
    private readonly ILogger<SmtpAccountEmailSender> _logger = logger;

    public async Task SendAsync(
        AccountEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Account email delivery is disabled; durable in-app notification remains authoritative.");
            return;
        }

        if (string.IsNullOrWhiteSpace(message.Recipient))
        {
            return;
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false
        };
        mail.To.Add(new MailAddress(message.Recipient));
        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            mail.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    message.HtmlBody,
                    null,
                    System.Net.Mime.MediaTypeNames.Text.Html));
        }

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(_options.Username)
        };
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        await client.SendMailAsync(mail, cancellationToken);
    }
}
