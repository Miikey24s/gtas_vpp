namespace gtas_vpp_fe.Features.Requests.Drafts;

/// <summary>
/// Định nghĩa boundary browser storage cho draft yêu cầu. Key và kiểm tra restore
/// được scope theo user đã xác thực và kỳ nghiệp vụ để draft không đi nhầm context.
/// </summary>
public static class OrderDraftStoragePolicy
{
    public const string StoragePrefix = "vpp.order.draft.";

    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    public static string BuildStorageKey(
        string userId,
        Guid periodId,
        bool isAdditional,
        Guid? orderId)
    {
        var normalizedUserId = NormalizeUserId(userId);
        if (periodId == Guid.Empty)
        {
            throw new ArgumentException("Period id is required.", nameof(periodId));
        }

        var requestType = isAdditional ? "additional" : "regular";
        var requestId = orderId?.ToString("N") ?? "new";
        return $"{StoragePrefix}{normalizedUserId}.{periodId:N}.{requestType}.{requestId}";
    }

    public static string BuildUserPrefix(string userId) =>
        $"{StoragePrefix}{NormalizeUserId(userId)}.";

    public static bool CanRestore(
        string? draftUserId,
        Guid draftPeriodId,
        DateTime savedAtUtc,
        string currentUserId,
        Guid currentPeriodId,
        DateTime nowUtc)
    {
        if (!string.Equals(
                draftUserId,
                NormalizeUserId(currentUserId),
                StringComparison.Ordinal)
            || draftPeriodId == Guid.Empty
            || draftPeriodId != currentPeriodId)
        {
            return false;
        }

        var normalizedSavedAt = savedAtUtc.Kind == DateTimeKind.Utc
            ? savedAtUtc
            : DateTime.SpecifyKind(savedAtUtc, DateTimeKind.Utc);
        var normalizedNow = nowUtc.Kind == DateTimeKind.Utc
            ? nowUtc
            : DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        return normalizedSavedAt <= normalizedNow.AddMinutes(5)
            && normalizedSavedAt >= normalizedNow.Subtract(MaxAge);
    }

    private static string NormalizeUserId(string userId)
    {
        var normalized = userId?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.Contains('.', StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid authenticated user id is required.", nameof(userId));
        }

        return normalized;
    }
}
