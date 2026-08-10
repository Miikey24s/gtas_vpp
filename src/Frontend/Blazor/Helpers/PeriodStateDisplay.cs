using gtas_vpp_fe.Components.DesignSystem.Primitives;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Ánh xạ vòng đời kỳ sang resource UI; trạng thái đơn được xử lý độc lập bởi StatusDisplay.
/// </summary>
public static class PeriodStateDisplay
{
    public static bool IsKnown(string? state) => state is
        "Open" or "SubmissionClosed" or "Pricing" or "Settled" or "Draft" or "Scheduled";

    public static string GetResourceKey(string? state) => state switch
    {
        "Open" => "PeriodStateOpen",
        "SubmissionClosed" => "PeriodStateSubmissionClosed",
        "Pricing" => "PeriodStatePricing",
        "Settled" => "PeriodStateSettled",
        "Draft" => "PeriodStateDraft",
        "Scheduled" => "PeriodStateScheduled",
        _ => "StatusUnknown"
    };

    public static string GetCompactResourceKey(string? state) => state switch
    {
        "Open" => "PeriodStateOpenCompact",
        "SubmissionClosed" => "PeriodStateSubmissionClosedCompact",
        "Pricing" => "PeriodStatePricingCompact",
        "Settled" => "PeriodStateSettledCompact",
        "Draft" => "PeriodStateDraft",
        "Scheduled" => "PeriodStateScheduled",
        _ => "StatusUnknown"
    };

    public static VppStatusTone GetTone(string? state) => VppStatusToneContract.Resolve(state);
}
