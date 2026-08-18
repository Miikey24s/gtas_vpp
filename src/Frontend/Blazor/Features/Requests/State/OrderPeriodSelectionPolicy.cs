using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Features.Requests.State;

public sealed record OrderPeriodSelection(Guid? RegularPeriodId, Guid? SupplementPeriodId);

/// <summary>
/// Chọn kỳ mặc định độc lập cho hai loại đơn: kỳ đang mở cho đơn thường và
/// kỳ vừa đóng gần nhất còn trong danh sách hợp lệ cho đơn bổ sung.
/// </summary>
public static class OrderPeriodSelectionPolicy
{
    public static OrderPeriodSelection Select(
        IReadOnlyList<VppOpenPeriodOptionResDTO> periods,
        Guid? requestedRegularPeriodId,
        Guid? requestedSupplementPeriodId)
    {
        var regularPeriods = periods
            .Where(IsRegular)
            .OrderByDescending(period => period.IsAnchor)
            .ThenBy(period => period.Year)
            .ThenBy(period => period.Month)
            .ToArray();
        var supplementPeriods = periods
            .Where(IsSupplement)
            .OrderByDescending(period => period.Year)
            .ThenByDescending(period => period.Month)
            .ToArray();

        return new OrderPeriodSelection(
            SelectRequestedOrDefault(regularPeriods, requestedRegularPeriodId),
            SelectRequestedOrDefault(supplementPeriods, requestedSupplementPeriodId));
    }

    private static Guid? SelectRequestedOrDefault(
        IReadOnlyList<VppOpenPeriodOptionResDTO> periods,
        Guid? requestedPeriodId)
    {
        if (requestedPeriodId is Guid requested
            && periods.Any(period => period.PeriodId == requested))
        {
            return requested;
        }

        return periods.FirstOrDefault()?.PeriodId;
    }

    private static bool IsRegular(VppOpenPeriodOptionResDTO period) =>
        string.Equals(period.State, "Open", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupplement(VppOpenPeriodOptionResDTO period) =>
        period.State is not null
        && (period.State.Equals("SubmissionClosed", StringComparison.OrdinalIgnoreCase)
            || period.State.Equals("Pricing", StringComparison.OrdinalIgnoreCase)
            || period.State.Equals("Settled", StringComparison.OrdinalIgnoreCase));
}
