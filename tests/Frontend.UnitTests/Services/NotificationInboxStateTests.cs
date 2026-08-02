using gtas_vpp_fe.Features.Notifications.Api;
using gtas_vpp_fe.Features.Notifications.Realtime;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Res.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gtas_vpp_fe.Tests.Services;

public sealed class NotificationInboxStateTests
{
    [Fact]
    public async Task StartAsync_LoadsInboxAndStartsRealtimeOnlyOnce()
    {
        var api = new StubApiServices
        {
            GetAsync = (_, _) => Task.FromResult<object?>(CreateInbox("Initial", unreadCount: 1))
        };
        var realtime = new FakeNotificationRealtimeClient();
        await using var state = CreateState(api, realtime);

        await state.StartAsync(TestContext.Current.CancellationToken);
        await state.StartAsync(TestContext.Current.CancellationToken);

        Assert.Single(state.Items);
        Assert.Equal("Initial", state.Items[0].Title);
        Assert.Equal(1, state.UnreadCount);
        Assert.Equal(1, api.GetCallCount);
        Assert.Equal(1, realtime.StartCount);
    }

    [Fact]
    public async Task RefreshFailure_PreservesPreviouslyLoadedItemsAndExposesRetryState()
    {
        var callCount = 0;
        var api = new StubApiServices
        {
            GetAsync = (_, _) =>
            {
                callCount++;
                return callCount == 1
                    ? Task.FromResult<object?>(CreateInbox("Cached", unreadCount: 1))
                    : Task.FromException<object?>(new InvalidOperationException("offline"));
            }
        };
        await using var state = CreateState(api, new FakeNotificationRealtimeClient());

        await state.RefreshAsync(TestContext.Current.CancellationToken);
        await state.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Single(state.Items);
        Assert.Equal("Cached", state.Items[0].Title);
        Assert.Equal(1, state.UnreadCount);
        Assert.True(state.HasError);
        Assert.False(state.IsLoading);
    }

    [Fact]
    public async Task ReadCommands_SkipCompletedWorkAndRefreshAfterMutations()
    {
        var currentInbox = CreateInbox("Unread", unreadCount: 1);
        var posts = new List<string>();
        var api = new StubApiServices
        {
            GetAsync = (_, _) => Task.FromResult<object?>(currentInbox),
            PostAsync = (endpoint, _, _) =>
            {
                posts.Add(endpoint);
                return Task.FromResult<object?>(null);
            }
        };
        await using var state = CreateState(api, new FakeNotificationRealtimeClient());
        await state.RefreshAsync(TestContext.Current.CancellationToken);
        var unread = state.Items[0];

        await state.MarkReadAsync(unread);
        await state.MarkReadAsync(new NotificationResDTO
        {
            Id = Guid.NewGuid(),
            ReadAt = DateTime.UtcNow
        });
        await state.MarkAllReadAsync();

        Assert.Equal(2, posts.Count);
        Assert.EndsWith($"/{unread.Id}/read", posts[0], StringComparison.Ordinal);
        Assert.Equal("/api/notifications/read-all", posts[1]);
        Assert.Equal(3, api.GetCallCount);

        currentInbox = new NotificationInboxResDTO();
        await state.RefreshAsync(TestContext.Current.CancellationToken);
        await state.MarkAllReadAsync();
        Assert.Equal(2, posts.Count);
    }

    [Fact]
    public async Task RealtimeEvent_RefreshesAndDisposeUnsubscribesIdempotently()
    {
        var callCount = 0;
        var api = new StubApiServices
        {
            GetAsync = (_, _) => Task.FromResult<object?>(
                CreateInbox($"Version {++callCount}", unreadCount: callCount))
        };
        var realtime = new FakeNotificationRealtimeClient();
        var state = CreateState(api, realtime);
        await state.StartAsync(TestContext.Current.CancellationToken);

        await realtime.RaiseNotificationsChangedAsync();

        Assert.Equal("Version 2", state.Items[0].Title);
        Assert.Equal(2, state.UnreadCount);

        await state.DisposeAsync();
        await state.DisposeAsync();
        await realtime.RaiseNotificationsChangedAsync();

        Assert.Equal(2, api.GetCallCount);
        Assert.Equal(1, realtime.DisposeCount);
    }

    private static NotificationInboxState CreateState(
        StubApiServices api,
        FakeNotificationRealtimeClient realtime) => new(
        new NotificationApiClient(api),
        realtime,
        NullLogger<NotificationInboxState>.Instance);

    private static NotificationInboxResDTO CreateInbox(string title, int unreadCount) => new()
    {
        Items =
        [
            new NotificationResDTO
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Title = title,
                Message = "Message",
                CreatedAt = DateTime.UtcNow
            }
        ],
        TotalCount = 1,
        UnreadCount = unreadCount
    };

    private sealed class FakeNotificationRealtimeClient : INotificationRealtimeClient
    {
        public event Func<Task>? NotificationsChanged;

        public int StartCount { get; private set; }
        public int DisposeCount { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCount++;
            return Task.CompletedTask;
        }

        public async Task RaiseNotificationsChangedAsync()
        {
            var handlers = NotificationsChanged?.GetInvocationList().Cast<Func<Task>>().ToArray() ?? [];
            foreach (var handler in handlers)
            {
                await handler();
            }
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}
