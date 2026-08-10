namespace gtas_vpp_fe.Components.DesignSystem.Primitives;

/// <summary>
/// Contract màu semantic dùng chung cho badge trạng thái toàn frontend.
/// Route chỉ giữ copy và nghiệp vụ; không tự diễn giải lại cùng một trạng thái.
/// </summary>
public static class VppStatusToneContract
{
    public static VppStatusTone Resolve(string? state) => state?.Trim().ToUpperInvariant() switch
    {
        "ACTIVE" or "OPEN" or "SCHEDULED" or "SUBMITTED" => VppStatusTone.Info,
        "APPROVED" or "SETTLED" or "PUBLISHED" or "SUCCEEDED" or "CONFIRMED"
            or "COMPLETED" or "FULL" => VppStatusTone.Success,
        "PENDING" or "PENDINGAPPROVAL" or "PRICING" or "NEEDSREVIEW"
            or "EXPIRING" => VppStatusTone.Warning,
        "REJECTED" or "CANCELLED" or "CANCELED" or "FAILED" or "ERROR"
            or "DELETED" or "REVOKED" => VppStatusTone.Danger,
        "DRAFT" or "INACTIVE" or "DISABLED" or "EXPIRED" or "SUBMISSIONCLOSED"
            or "CLOSED" or "NOTSETTLED" => VppStatusTone.Neutral,
        _ => VppStatusTone.Neutral
    };
}
