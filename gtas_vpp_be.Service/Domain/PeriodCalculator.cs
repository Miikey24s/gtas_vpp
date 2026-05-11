namespace gtas_vpp_be.Service.Domain
{
    /// <summary>
    /// Single source of truth for VPP period (year/month) calculation.
    /// Pure domain logic — no I/O, no clock dependency. Caller passes the
    /// authoritative <see cref="System.DateTime"/> in.
    /// </summary>
    /// <remarks>
    /// Business rule (user-confirmed):
    /// <list type="bullet">
    ///   <item>Deadline = day <c>DeadlineDay</c> at 00:00:00.</item>
    ///   <item>From <c>00:00:00</c> day <c>DeadlineDay</c>/N: current period = month N, previous = N-1.</item>
    ///   <item>Before <c>00:00:00</c> day <c>DeadlineDay</c>/N: current period = N-1, previous = N-2.</item>
    /// </list>
    /// </remarks>
    public sealed class PeriodCalculator
    {
        private readonly int _deadlineDay;

        public PeriodCalculator(int deadlineDay = 5)
        {
            if (deadlineDay < 1 || deadlineDay > 28)
                throw new System.ArgumentOutOfRangeException(
                    nameof(deadlineDay),
                    deadlineDay,
                    "DeadlineDay must be between 1 and 28 (to avoid month-length edge cases).");
            _deadlineDay = deadlineDay;
        }

        public int DeadlineDay => _deadlineDay;

        /// <summary>Compute the current open period at the supplied moment.</summary>
        public Period Current(System.DateTime now)
        {
            var anchor = new System.DateTime(now.Year, now.Month, 1);
            var current = now.Day >= _deadlineDay ? anchor : anchor.AddMonths(-1);
            return new Period(current.Year, current.Month);
        }

        /// <summary>Compute the just-closed period (previous to <see cref="Current"/>).</summary>
        public Period Previous(System.DateTime now)
        {
            var current = Current(now);
            var anchor = new System.DateTime(current.Year, current.Month, 1).AddMonths(-1);
            return new Period(anchor.Year, anchor.Month);
        }

        /// <summary>Returns true once the regular submission deadline has passed for the supplied period.</summary>
        public bool IsDeadlinePassed(System.DateTime now, Period period)
        {
            // Regular orders for period (Y, M) close at the start of day _deadlineDay
            // of the FOLLOWING month (so that the entire month N can submit during
            // days 1..(deadlineDay-1) of month N+1).
            var deadline = new System.DateTime(period.Year, period.Month, 1)
                .AddMonths(1)
                .AddDays(_deadlineDay - 1);
            return now >= deadline;
        }

        /// <summary>Compute deadline timestamp for the given period.</summary>
        public System.DateTime DeadlineFor(Period period)
            => new System.DateTime(period.Year, period.Month, 1)
                .AddMonths(1)
                .AddDays(_deadlineDay - 1);
    }

    /// <summary>Calendar period (year + month) — inclusive of M.</summary>
    public readonly record struct Period(int Year, int Month)
    {
        public override string ToString() => $"{Year:D4}-{Month:D2}";
    }
}
