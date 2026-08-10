using gtas_vpp_be.Model.VPP;

namespace gtas_vpp_be.Service.Domain;

public sealed record PeriodSchedule(
    DateTime StartAtLocal,
    DateTime SubmissionDeadlineLocal,
    DateTime SupplementApprovalDeadlineLocal,
    DateTime StartAtUtc,
    DateTime SubmissionDeadlineUtc,
    DateTime SupplementApprovalDeadlineUtc);

/// <summary>
/// Bộ tính lịch thuần cho rolling horizon. Ngày 29–31 luôn clamp về cuối tháng,
/// kể cả tháng 2 năm nhuận; timestamp local được đổi UTC bằng timezone của settings.
/// </summary>
public sealed class PeriodScheduleCalculator
{
    public PeriodSchedule Build(
        Period targetPeriod,
        Period openedFromMonth,
        VppOrderPeriodSettingsVersion settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidatePeriod(targetPeriod);
        ValidatePeriod(openedFromMonth);
        ValidateSettings(settings);

        var startLocal = BoundaryLocal(
            openedFromMonth.Year,
            openedFromMonth.Month,
            settings.DefaultNewPeriodOpenDay,
            settings.LocalTimeOfDay);
        var closeMonth = new DateTime(targetPeriod.Year, targetPeriod.Month, 1).AddMonths(1);
        var deadlineLocal = BoundaryLocal(
            closeMonth.Year,
            closeMonth.Month,
            settings.DefaultPeriodCloseDay,
            settings.LocalTimeOfDay);
        if (deadlineLocal <= startLocal)
        {
            throw new InvalidOperationException(
                "Period close time must be later than its open time.");
        }

        var supplementLocal = deadlineLocal.AddDays(settings.SupplementApprovalGraceDays);
        return BuildExact(
            startLocal,
            deadlineLocal,
            supplementLocal,
            settings.TimeZoneId);
    }

    public PeriodSchedule BuildExact(
        DateTime startAtLocal,
        DateTime submissionDeadlineLocal,
        DateTime supplementApprovalDeadlineLocal,
        string timeZoneId)
    {
        if (submissionDeadlineLocal <= startAtLocal)
        {
            throw new ArgumentException(
                "Submission deadline must be later than open time.",
                nameof(submissionDeadlineLocal));
        }
        if (supplementApprovalDeadlineLocal < submissionDeadlineLocal)
        {
            throw new ArgumentException(
                "Supplement approval deadline cannot be earlier than submission deadline.",
                nameof(supplementApprovalDeadlineLocal));
        }

        _ = PeriodCalculator.ResolveTimeZone(timeZoneId);
        var start = DateTime.SpecifyKind(startAtLocal, DateTimeKind.Unspecified);
        var close = DateTime.SpecifyKind(submissionDeadlineLocal, DateTimeKind.Unspecified);
        var supplement = DateTime.SpecifyKind(
            supplementApprovalDeadlineLocal,
            DateTimeKind.Unspecified);
        return new PeriodSchedule(
            start,
            close,
            supplement,
            PeriodCalculator.ToUtc(start, timeZoneId),
            PeriodCalculator.ToUtc(close, timeZoneId),
            PeriodCalculator.ToUtc(supplement, timeZoneId));
    }

    public DateTime BoundaryLocal(
        int year,
        int month,
        int requestedDay,
        TimeSpan localTimeOfDay)
    {
        if (year is < 1 or > 9999 || month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }
        if (requestedDay is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedDay));
        }
        if (localTimeOfDay < TimeSpan.Zero || localTimeOfDay >= TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(localTimeOfDay));
        }

        var day = Math.Min(requestedDay, DateTime.DaysInMonth(year, month));
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified)
            .Add(localTimeOfDay);
    }

    public static void ValidateSettings(VppOrderPeriodSettingsVersion settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.DefaultOpenPeriodCount is < 0 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.DefaultOpenPeriodCount));
        }
        if (settings.DefaultNewPeriodOpenDay is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.DefaultNewPeriodOpenDay));
        }
        if (settings.DefaultPeriodCloseDay is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.DefaultPeriodCloseDay));
        }
        if (settings.SupplementApprovalGraceDays is < 0 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.SupplementApprovalGraceDays));
        }
        if (settings.PostCloseAdjustmentDays is < 0 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.PostCloseAdjustmentDays));
        }
        if (settings.SupplementApprovalGraceDays > settings.PostCloseAdjustmentDays)
        {
            throw new ArgumentException(
                "Supplement approval days cannot exceed post-close adjustment days.",
                nameof(settings.SupplementApprovalGraceDays));
        }
        if (settings.SettlementReopenWindowDays is < 0 or > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.SettlementReopenWindowDays));
        }
        if (settings.EffectiveFromYear is < 1 or > 9999
            || settings.EffectiveFromMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.EffectiveFromMonth));
        }
        if (settings.LocalTimeOfDay < TimeSpan.Zero
            || settings.LocalTimeOfDay >= TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(settings.LocalTimeOfDay));
        }
        _ = PeriodCalculator.ResolveTimeZone(settings.TimeZoneId);
    }

    private static void ValidatePeriod(Period period)
    {
        if (period.Year is < 1 or > 9999 || period.Month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(period));
        }
    }
}
