using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Notifications;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.Constants;
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
        var page = new P01_Page
        {
            Id = Guid.NewGuid(), PageCode = "DASHBOARD", Type = "PAGE",
            CreateDate = now, UpdateDate = now
        };
        var component = new P03_Component
        {
            Id = Guid.NewGuid(), ComponentCode = Permissions.RequestAdminApproval,
            ComponentName = "Approval", CreateDate = now, UpdateDate = now
        };
        var pageMapping = new P05_PageComponentMapping
        {
            Id = Guid.NewGuid(), P01_PageId = page.Id, P01_Page = page,
            P03_ComponentId = component.Id, P03_Component = component
        };
        context.AddRange(
            new P02_Group
            {
                Id = groupId, GroupName = "Approvers", CreateDate = now, UpdateDate = now
            },
            new P04_UserGroup
            {
                Id = Guid.NewGuid(), UserId = 42, P02_GroupId = groupId,
                LEX02_CompanyDepartmentLocationId = Guid.NewGuid(),
                CreateDate = now, UpdateDate = now
            },
            page,
            component,
            pageMapping,
            new P06_GroupPageComponentMapping
            {
                P02_GroupId = groupId,
                P05_PageComponentMappingId = pageMapping.Id,
                P05_PageComponentMapping = pageMapping,
                MemberCompanyCode = 77500,
                IsVisible = true,
                IsEnable = true,
                CreateDate = now,
                UpdateDate = now
            });
        await context.SaveChangesAsync();

        var recipients = await service.GetRecipientsWithPermissionAsync(
            "77500", Permissions.RequestApprove);

        Assert.Equal([42], recipients);
    }
}
