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
        private const string WindowsVietnamTimeZone = "SE Asia Standard Time";
        private const string IanaVietnamTimeZone = "Asia/Ho_Chi_Minh";
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

        /// <summary>
        /// Resolves the business timezone used by VPP.  Windows and Linux use
        /// different identifiers; keeping the lookup here prevents period
        /// boundary calculations from depending on the host's local timezone.
        /// </summary>
        public static System.TimeZoneInfo BusinessTimeZone
        {
            get
            {
                try
                {
                    return System.TimeZoneInfo.FindSystemTimeZoneById(
                        System.OperatingSystem.IsWindows()
                            ? WindowsVietnamTimeZone
                            : IanaVietnamTimeZone);
                }
                catch (System.TimeZoneNotFoundException)
                {
                    return System.TimeZoneInfo.CreateCustomTimeZone(
                        IanaVietnamTimeZone,
                        System.TimeSpan.FromHours(7),
                        "Vietnam time",
                        "Vietnam time");
                }
                catch (System.InvalidTimeZoneException)
                {
                    return System.TimeZoneInfo.CreateCustomTimeZone(
                        IanaVietnamTimeZone,
                        System.TimeSpan.FromHours(7),
                        "Vietnam time",
                        "Vietnam time");
                }
            }
        }

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

        /// <summary>Returns the local business-date start of a period.</summary>
        public System.DateTime StartAtLocal(Period period)
            => new System.DateTime(period.Year, period.Month, _deadlineDay,
                0, 0, 0, System.DateTimeKind.Unspecified);

        /// <summary>Returns the local business deadline of a period.</summary>
        public System.DateTime SubmissionDeadlineLocal(Period period)
            => new System.DateTime(period.Year, period.Month, 1,
                0, 0, 0, System.DateTimeKind.Unspecified)
                .AddMonths(1)
                .AddDays(_deadlineDay - 1);

        /// <summary>
        /// Converts a local Vietnam business timestamp to UTC.  An unspecified
        /// kind is deliberate: the input is a wall-clock business time, not a
        /// timestamp in the machine's timezone.
        /// </summary>
        public static System.DateTime ToUtc(System.DateTime localBusinessTime)
        {
            var unspecified = System.DateTime.SpecifyKind(
                localBusinessTime, System.DateTimeKind.Unspecified);
            return System.TimeZoneInfo.ConvertTimeToUtc(unspecified, BusinessTimeZone);
        }

        /// <summary>Returns the UTC start of the supplied period.</summary>
        public System.DateTime StartAtUtc(Period period)
            => ToUtc(StartAtLocal(period));

        /// <summary>Returns the UTC regular submission deadline.</summary>
        public System.DateTime SubmissionDeadlineUtc(Period period)
            => ToUtc(SubmissionDeadlineLocal(period));

        /// <summary>
        /// Returns the UTC deadline for approving a pending supplement.  The
        /// grace is applied after the regular deadline and must be nonnegative.
        /// </summary>
        public System.DateTime SupplementApprovalDeadlineUtc(
            Period period,
            System.TimeSpan approvalGrace)
        {
            if (approvalGrace < System.TimeSpan.Zero)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(approvalGrace), approvalGrace,
                    "Approval grace cannot be negative.");
            }

            return ToUtc(SubmissionDeadlineLocal(period).Add(approvalGrace));
        }

        /// <summary>
        /// Converts the provider's Vietnam wall-clock value to UTC.  Existing
        /// callers expose only <c>IDateTimeProvider.Now</c>, so this helper
        /// centralizes the kind normalization needed by persisted timestamps.
        /// </summary>
        public static System.DateTime NormalizeNowUtc(System.DateTime now)
            => now.Kind == System.DateTimeKind.Utc ? now : ToUtc(now);
    }

    /// <summary>Calendar period (year + month) — inclusive of M.</summary>
    public readonly record struct Period(int Year, int Month)
    {
        public override string ToString() => $"{Year:D4}-{Month:D2}";
    }
}
