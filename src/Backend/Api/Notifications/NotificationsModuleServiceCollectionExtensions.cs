namespace gtas_vpp_be.Notifications;

public static class NotificationsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AccountEmailOptions>()
            .Bind(configuration.GetSection(AccountEmailOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out _),
                "EmailNotifications:PublicBaseUrl must be an absolute URL.")
            .Validate(options => options.SmtpPort is >= 1 and <= 65535,
                "EmailNotifications:SmtpPort must be a valid TCP port.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.SmtpHost),
                "EmailNotifications:SmtpHost is required when email is enabled.")
            .Validate(options => !options.Enabled
                    || System.Net.Mail.MailAddress.TryCreate(options.FromAddress, out _),
                "EmailNotifications:FromAddress must be a valid email address when email is enabled.")
            .ValidateOnStart();

        services.AddScoped<IAppNotificationService, AppNotificationService>();
        services.AddSingleton<INotificationRealtimeNotifier, NotificationRealtimeNotifier>();
        services.AddScoped<IEmailOutboxService, EmailOutboxService>();
        services.AddScoped<ICurrentMemberCompanyProvider, DefaultMemberCompanyProvider>();
        services.AddScoped<SmtpAccountEmailSender>();
        services.AddScoped<IAccountEmailSender, OutboxAccountEmailSender>();
        services.AddHostedService<EmailOutboxWorker>();
        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationHub>("/hubs/notifications");
        return endpoints;
    }
}
