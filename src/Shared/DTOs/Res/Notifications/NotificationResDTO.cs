namespace gtas_vpp_shared.DTOs.Res.Notifications;

public sealed class NotificationResDTO
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead => ReadAt.HasValue;
}

public sealed class NotificationInboxResDTO
{
    public List<NotificationResDTO> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class NotificationReadAllResDTO
{
    public int Changed { get; set; }
}
