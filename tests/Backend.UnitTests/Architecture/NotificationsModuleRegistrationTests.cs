using gtas_vpp_be.Notifications;
using gtas_vpp_be.Service.Helpers.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace gtas_vpp_be.Tests.Architecture;

public sealed class NotificationsModuleRegistrationTests
{
    [Fact]
    public void AddNotificationsModule_ResolvesApplicationAndDeliveryServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailNotifications:PublicBaseUrl"] = "https://localhost",
                ["EmailNotifications:SmtpPort"] = "1025"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSignalR();
        services.AddDbContext<VPPContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddNotificationsModule(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<AppNotificationService>(
            scope.ServiceProvider.GetRequiredService<IAppNotificationService>());
        Assert.IsType<NotificationRealtimeNotifier>(
            scope.ServiceProvider.GetRequiredService<INotificationRealtimeNotifier>());
        Assert.IsType<EmailOutboxService>(
            scope.ServiceProvider.GetRequiredService<IEmailOutboxService>());
        Assert.IsType<OutboxAccountEmailSender>(
            scope.ServiceProvider.GetRequiredService<IAccountEmailSender>());
    }
}
