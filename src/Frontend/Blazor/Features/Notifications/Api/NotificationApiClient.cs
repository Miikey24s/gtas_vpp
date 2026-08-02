using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Notifications;

namespace gtas_vpp_fe.Features.Notifications.Api;

public sealed record NotificationInboxQuery(
    int Skip = 0,
    int Take = 20,
    bool UnreadOnly = false);

public sealed class NotificationApiClient(IAPIServices api)
{
    private const string NotificationsBase = "/api/notifications";

    public Task<NotificationInboxResDTO?> GetInboxAsync(NotificationInboxQuery query) =>
        api.GetFromApiAsync<NotificationInboxResDTO>(BuildInboxEndpoint(query));

    public Task<object?> MarkReadAsync(Guid notificationId) =>
        api.PostFromApiAsync<object>($"{NotificationsBase}/{notificationId}/read", null);

    public Task<NotificationReadAllResDTO?> MarkAllReadAsync() =>
        api.PostFromApiAsync<NotificationReadAllResDTO>($"{NotificationsBase}/read-all", null);

    internal static string BuildInboxEndpoint(NotificationInboxQuery query)
    {
        var queryParams = new List<string>
        {
            $"skip={Math.Max(0, query.Skip)}",
            $"take={Math.Clamp(query.Take, 1, 50)}"
        };
        if (query.UnreadOnly)
        {
            queryParams.Add("unreadOnly=true");
        }

        return $"{NotificationsBase}?{string.Join("&", queryParams)}";
    }
}
