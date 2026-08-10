namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components;

internal static class OrderPeriodDisplay
{
    public static string FormatTransitionReason(
        string? reason,
        string emptyText = "–")
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return emptyText;
        }

        var trimmed = reason.Trim();
        if (trimmed.Equals("rolling-horizon-top-up", StringComparison.OrdinalIgnoreCase))
        {
            return "Hệ thống tự mở bù kỳ";
        }
        if (trimmed.Equals("period-recovery", StringComparison.OrdinalIgnoreCase))
        {
            return "Hệ thống tự chuyển trạng thái theo lịch";
        }

        const string settlementPrefix = "Settlement confirmed revision ";
        return trimmed.StartsWith(settlementPrefix, StringComparison.OrdinalIgnoreCase)
            ? $"Đã chốt kỳ · phiên bản {trimmed[settlementPrefix.Length..]}"
            : trimmed;
    }
}
