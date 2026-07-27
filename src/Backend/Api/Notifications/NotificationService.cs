using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Notifications;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Notifications;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Notifications;

public interface IAppNotificationService
{
    Task<NotificationInboxResDTO> GetInboxAsync(
        int userId,
        string memberCompanyCode,
        int skip,
        int take,
        bool unreadOnly,
        CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(
        Guid id,
        int userId,
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(
        int userId,
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetRecipientsWithPermissionAsync(
        string memberCompanyCode,
        string permission,
        CancellationToken cancellationToken = default);

    Task PublishAsync(
        IEnumerable<int> recipientUserIds,
        string memberCompanyCode,
        string type,
        string title,
        string message,
        string? route,
        string? correlationId,
        CancellationToken cancellationToken = default);
}

public sealed class AppNotificationService(
    VPPContext context,
    INotificationRealtimeNotifier realtimeNotifier)
    : IAppNotificationService
{
    private readonly VPPContext _context = context;
    private readonly INotificationRealtimeNotifier _realtimeNotifier = realtimeNotifier;

    public async Task<NotificationInboxResDTO> GetInboxAsync(
        int userId,
        string memberCompanyCode,
        int skip,
        int take,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 50);

        var scoped = _context.Set<Notification>()
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && item.MemberCompanyCode == memberCompanyCode);

        var unreadCount = await scoped.CountAsync(item => item.ReadAt == null, cancellationToken);
        if (unreadOnly)
        {
            scoped = scoped.Where(item => item.ReadAt == null);
        }

        var totalCount = await scoped.CountAsync(cancellationToken);
        var items = await scoped
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Skip(skip)
            .Take(take)
            .Select(item => new NotificationResDTO
            {
                Id = item.Id,
                Type = item.Type,
                Title = item.Title,
                Message = item.Message,
                Route = item.Route,
                CorrelationId = item.CorrelationId,
                CreatedAt = item.CreatedAt,
                ReadAt = item.ReadAt
            })
            .ToListAsync(cancellationToken);

        return new NotificationInboxResDTO
        {
            Items = items,
            TotalCount = totalCount,
            UnreadCount = unreadCount
        };
    }

    public async Task<bool> MarkReadAsync(
        Guid id,
        int userId,
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<Notification>()
            .FirstOrDefaultAsync(notification => notification.Id == id
                && notification.UserId == userId
                && notification.MemberCompanyCode == memberCompanyCode,
                cancellationToken);

        if (item is null)
        {
            return false;
        }

        if (item.ReadAt is null)
        {
            item.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _realtimeNotifier.NotifyUserAsync(userId, cancellationToken);
        return true;
    }

    public async Task<int> MarkAllReadAsync(
        int userId,
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var unreadItems = await _context.Set<Notification>()
            .Where(item => item.UserId == userId
                && item.MemberCompanyCode == memberCompanyCode
                && item.ReadAt == null)
            .ToListAsync(cancellationToken);

        foreach (var item in unreadItems)
        {
            item.ReadAt = now;
        }

        var changed = unreadItems.Count;

        if (changed > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _realtimeNotifier.NotifyUserAsync(userId, cancellationToken);
        }

        return changed;
    }

    public async Task<IReadOnlyList<int>> GetRecipientsWithPermissionAsync(
        string memberCompanyCode,
        string permission,
        CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(memberCompanyCode, out var companyCode))
        {
            return [];
        }

        var compatibleCodes = permission switch
        {
            Permissions.RequestApprove => new[] { Permissions.RequestApprove, Permissions.RequestAdminApproval },
            Permissions.RequestReject => new[] { Permissions.RequestReject, Permissions.RequestAdminApproval },
            _ => new[] { permission }
        };

        var groupIds = await _context.Set<GroupPageComponentMapping>()
            .AsNoTracking()
            .Where(mapping => mapping.MemberCompanyCode == companyCode
                && mapping.IsVisible
                && mapping.IsEnable
                && mapping.PageComponentMapping != null
                && mapping.PageComponentMapping.PermissionComponent != null
                && compatibleCodes.Contains(mapping.PageComponentMapping.PermissionComponent.ComponentCode))
            .Select(mapping => mapping.PermissionGroupId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (groupIds.Count == 0)
        {
            return [];
        }

        return await _context.Set<UserGroupMembership>()
            .AsNoTracking()
            .Where(mapping => !mapping.IsDeleted
                && mapping.PermissionGroup != null
                && !mapping.PermissionGroup.IsDeleted
                && groupIds.Contains(mapping.PermissionGroupId))
            .Select(mapping => mapping.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task PublishAsync(
        IEnumerable<int> recipientUserIds,
        string memberCompanyCode,
        string type,
        string title,
        string message,
        string? route,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        var recipients = recipientUserIds.Where(id => id > 0).Distinct().ToArray();
        if (recipients.Length == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            var existingRecipients = await _context.Set<Notification>()
                .AsNoTracking()
                .Where(item => recipients.Contains(item.UserId)
                    && item.MemberCompanyCode == memberCompanyCode
                    && item.Type == type
                    && item.CorrelationId == correlationId)
                .Select(item => item.UserId)
                .ToListAsync(cancellationToken);
            recipients = recipients.Except(existingRecipients).ToArray();
            if (recipients.Length == 0)
            {
                return;
            }
        }

        var now = DateTime.UtcNow;
        var notifications = recipients.Select(userId => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MemberCompanyCode = memberCompanyCode,
            Type = type,
            Title = title,
            Message = message,
            Route = route,
            CorrelationId = correlationId,
            CreatedAt = now
        });

        _context.Set<Notification>().AddRange(notifications);
        await _context.SaveChangesAsync(cancellationToken);
        await Task.WhenAll(recipients.Select(userId =>
            _realtimeNotifier.NotifyUserAsync(userId, cancellationToken)));
    }
}
