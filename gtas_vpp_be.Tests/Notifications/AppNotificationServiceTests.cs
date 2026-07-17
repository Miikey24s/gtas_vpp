using System.Net;
using System.Net.Sockets;
using System.Text;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Notifications;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.Notifications;

public sealed class AppNotificationServiceTests
{
    [Fact]
    public async Task Inbox_IsIsolatedByUserAndCompany_AndReadStatePersists()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var realtime = new Mock<INotificationRealtimeNotifier>();
        realtime.Setup(x => x.NotifyUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new AppNotificationService(context, realtime.Object);

        await service.PublishAsync(
            [10, 11], "77500", "order.test", "Test", "Message", "/dashboard", "abc");
        await service.PublishAsync(
            [10], "88000", "order.other-company", "Other", "Message", null, null);

        var user10 = await service.GetInboxAsync(10, "77500", 0, 20, false);
        var user11 = await service.GetInboxAsync(11, "77500", 0, 20, false);

        Assert.Single(user10.Items);
        Assert.Single(user11.Items);
        Assert.Equal(1, user10.UnreadCount);
        Assert.True(await service.MarkReadAsync(user10.Items[0].Id, 10, "77500"));
        Assert.False(await service.MarkReadAsync(user10.Items[0].Id, 11, "77500"));

        var refreshed = await service.GetInboxAsync(10, "77500", 0, 20, false);
        Assert.Equal(0, refreshed.UnreadCount);
        Assert.True(refreshed.Items[0].IsRead);
    }

    [Fact]
    public async Task LegacyApprovalPermission_ResolvesActiveRecipientUsers()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = new AppNotificationService(context, Mock.Of<INotificationRealtimeNotifier>());
        var now = DateTime.UtcNow;
        var groupId = Guid.NewGuid();
        var page = new PermissionPage
        {
            Id = Guid.NewGuid(),
            PageCode = "DASHBOARD",
            Type = "PAGE",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var component = new PermissionComponent
        {
            Id = Guid.NewGuid(),
            ComponentCode = Permissions.RequestAdminApproval,
            ComponentName = "Approval",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var pageMapping = new PageComponentMapping
        {
            Id = Guid.NewGuid(),
            PermissionPageId = page.Id,
            PermissionPage = page,
            PermissionComponentId = component.Id,
            PermissionComponent = component
        };
        context.AddRange(
            new PermissionGroup
            {
                Id = groupId,
                GroupName = "Approvers",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new UserGroupMembership
            {
                Id = Guid.NewGuid(),
                UserId = 42,
                PermissionGroupId = groupId,
                DepartmentId = Guid.NewGuid(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            page,
            component,
            pageMapping,
            new GroupPageComponentMapping
            {
                PermissionGroupId = groupId,
                PageComponentMappingId = pageMapping.Id,
                PageComponentMapping = pageMapping,
                MemberCompanyCode = 77500,
                IsVisible = true,
                IsEnable = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        await context.SaveChangesAsync();

        var recipients = await service.GetRecipientsWithPermissionAsync(
            "77500", Permissions.RequestApprove);

        Assert.Equal([42], recipients);
    }

    [Fact]
    public async Task Publish_IsIdempotentPerRecipientCompanyTypeAndCorrelation()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var realtime = new Mock<INotificationRealtimeNotifier>();
        realtime.Setup(x => x.NotifyUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new AppNotificationService(context, realtime.Object);

        await service.PublishAsync([10, 11], "77500", "order.test", "Test", "Message", "/dashboard", "order-1");
        await service.PublishAsync([10, 11], "77500", "order.test", "Test", "Message", "/dashboard", "order-1");

        Assert.Equal(2, await context.Set<gtas_vpp_be.Model.Notifications.Notification>().CountAsync());
        realtime.Verify(x => x.NotifyUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EmailOutbox_IsDurableAndDeduplicated_WithRetryState()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var outbox = new EmailOutboxService(context);
        var message = new AccountEmailMessage(
            "employee@example.test",
            "GTAS VPP",
            "Xin chao");

        var first = await outbox.EnqueueAsync("77500", message);
        var replay = await outbox.EnqueueAsync("77500", message);
        Assert.Equal(first, replay);
        Assert.Single(await context.Set<gtas_vpp_be.Model.Notifications.EmailOutboxMessage>().ToListAsync());

        await outbox.MarkFailedAsync(first, "Mailpit unavailable", DateTime.UtcNow.AddMinutes(5));
        var pending = await outbox.GetDueAsync(DateTime.UtcNow.AddMinutes(6), 10);
        Assert.Single(pending);
        Assert.Equal(1, pending[0].AttemptCount);

        await outbox.MarkSentAsync(first, DateTime.UtcNow);
        Assert.Empty(await outbox.GetDueAsync(DateTime.UtcNow.AddHours(1), 10));
    }

    [Fact]
    public async Task SmtpAdapter_DeliversMessageToLoopbackSandbox()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var captureTask = CaptureSmtpMessageAsync(listener);
        var sender = new SmtpAccountEmailSender(
            Options.Create(new AccountEmailOptions
            {
                Enabled = true,
                FromAddress = "noreply@example.invalid",
                FromDisplayName = "GTAS VPP",
                SmtpHost = IPAddress.Loopback.ToString(),
                SmtpPort = port,
                UseSsl = false
            }),
            NullLogger<SmtpAccountEmailSender>.Instance);

        await sender.SendAsync(new AccountEmailMessage(
            "employee@example.invalid",
            "GTAS VPP SMTP proof",
            "sandbox-proof-20260717"));

        var captured = await captureTask.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Contains("RCPT TO:<employee@example.invalid>", captured.Commands);
        Assert.Contains("Subject: GTAS VPP SMTP proof", captured.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sandbox-proof-20260717", captured.Message);
    }

    private static async Task<(IReadOnlyList<string> Commands, string Message)> CaptureSmtpMessageAsync(
        TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(15));
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, false, leaveOpen: true);
        await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };
        var commands = new List<string>();
        var message = new StringBuilder();

        await writer.WriteLineAsync("220 localhost GTAS VPP test SMTP");
        while (true)
        {
            var line = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
            Assert.NotNull(line);

            if (line.StartsWith("EHLO ", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("HELO ", StringComparison.OrdinalIgnoreCase))
            {
                commands.Add(line);
                await writer.WriteLineAsync("250 localhost");
                continue;
            }

            commands.Add(line);
            if (line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("250 OK");
                continue;
            }

            if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                while (true)
                {
                    var dataLine = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
                    Assert.NotNull(dataLine);
                    if (dataLine == ".")
                    {
                        break;
                    }

                    message.AppendLine(dataLine.StartsWith("..", StringComparison.Ordinal)
                        ? dataLine[1..]
                        : dataLine);
                }

                await writer.WriteLineAsync("250 Queued");
                return (commands, message.ToString());
            }

            await writer.WriteLineAsync("250 OK");
        }
    }
}
