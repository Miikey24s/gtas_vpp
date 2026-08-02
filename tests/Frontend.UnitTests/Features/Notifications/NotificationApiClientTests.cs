using gtas_vpp_fe.Features.Notifications.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Res.Notifications;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Notifications;

public sealed class NotificationApiClientTests
{
    [Fact]
    public async Task InboxQuery_UsesBoundedCanonicalParameters()
    {
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetAsync = (endpoint, type) =>
            {
                endpoints.Add(endpoint);
                Assert.Equal(typeof(NotificationInboxResDTO), type);
                return Task.FromResult<object?>(new NotificationInboxResDTO());
            }
        };
        var client = new NotificationApiClient(api);

        await client.GetInboxAsync(new NotificationInboxQuery(-3, 100, UnreadOnly: true));
        await client.GetInboxAsync(new NotificationInboxQuery());

        Assert.Equal(
            [
                "/api/notifications?skip=0&take=50&unreadOnly=true",
                "/api/notifications?skip=0&take=20"
            ],
            endpoints);
    }

    [Fact]
    public async Task ReadCommands_UseExpectedMethodEndpointAndResponseType()
    {
        var notificationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var calls = new List<(string Endpoint, object? Body, Type ResponseType)>();
        var api = new StubApiServices
        {
            PostAsync = (endpoint, body, responseType) =>
            {
                calls.Add((endpoint, body, responseType));
                return Task.FromResult<object?>(null);
            }
        };
        var client = new NotificationApiClient(api);

        await client.MarkReadAsync(notificationId);
        await client.MarkAllReadAsync();

        Assert.Collection(
            calls,
            call =>
            {
                Assert.Equal($"/api/notifications/{notificationId}/read", call.Endpoint);
                Assert.Null(call.Body);
                Assert.Equal(typeof(object), call.ResponseType);
            },
            call =>
            {
                Assert.Equal("/api/notifications/read-all", call.Endpoint);
                Assert.Null(call.Body);
                Assert.Equal(typeof(NotificationReadAllResDTO), call.ResponseType);
            });
    }
}
