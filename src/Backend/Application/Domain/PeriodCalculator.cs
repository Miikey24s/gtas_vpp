namespace gtas_vpp_be.Service.Domain
{
    /// <summary>
    /// Nguồn sự thật duy nhất để tính kỳ VPP (năm/tháng).
    /// Domain logic thuần, không I/O và không phụ thuộc clock. Caller truyền vào
    /// <see cref="System.DateTime"/> có thẩm quyền.
    /// </summary>
    /// <remarks>
    /// Quy tắc nghiệp vụ đã được người dùng xác nhận:
    /// <list type="bullet">
    ///   <item>Deadline = ngày <c>DeadlineDay</c> lúc 00:00:00.</item>
    ///   <item>Từ <c>00:00:00</c> ngày <c>DeadlineDay</c>/N: kỳ hiện tại = tháng N, kỳ trước = N-1.</item>
    ///   <item>Trước <c>00:00:00</c> ngày <c>DeadlineDay</c>/N: kỳ hiện tại = N-1, kỳ trước = N-2.</item>
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
        /// Resolve múi giờ nghiệp vụ của VPP. Windows và Linux dùng identifier khác nhau;
        /// đặt lookup tại đây giúp phép tính biên kỳ không phụ thuộc múi giờ local của host.
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

        /// <summary>Tính kỳ đang mở tại thời điểm được truyền vào.</summary>
        public Period Current(System.DateTime now)
        {
            var anchor = new System.DateTime(now.Year, now.Month, 1);
            var current = now.Day >= _deadlineDay ? anchor : anchor.AddMonths(-1);
            return new Period(current.Year, current.Month);
        }

        /// <summary>Tính kỳ vừa đóng, đứng trước <see cref="Current"/>.</summary>
        public Period Previous(System.DateTime now)
        {
            var current = Current(now);
            var anchor = new System.DateTime(current.Year, current.Month, 1).AddMonths(-1);
            return new Period(anchor.Year, anchor.Month);
        }

        /// <summary>Trả về true khi kỳ được truyền vào đã qua hạn gửi yêu cầu thường.</summary>
        public bool IsDeadlinePassed(System.DateTime now, Period period)
        {
            // Yêu cầu thường của kỳ (Year, Month) đóng vào đầu ngày _deadlineDay
            // của tháng KẾ TIẾP, để toàn bộ tháng N có thể gửi trong các ngày
            // 1..(deadlineDay-1) của tháng N+1.
            var deadline = new System.DateTime(period.Year, period.Month, 1)
                .AddMonths(1)
                .AddDays(_deadlineDay - 1);
            return now >= deadline;
        }

        /// <summary>Tính deadline timestamp cho kỳ được truyền vào.</summary>
        public System.DateTime DeadlineFor(Period period)
            => new System.DateTime(period.Year, period.Month, 1)
                .AddMonths(1)
                .AddDays(_deadlineDay - 1);

        /// <summary>Trả về ngày giờ nghiệp vụ local bắt đầu kỳ.</summary>
        public System.DateTime StartAtLocal(Period period)
            => new System.DateTime(period.Year, period.Month, _deadlineDay,
                0, 0, 0, System.DateTimeKind.Unspecified);

        /// <summary>Trả về deadline nghiệp vụ local của kỳ.</summary>
        public System.DateTime SubmissionDeadlineLocal(Period period)
            => new System.DateTime(period.Year, period.Month, 1,
                0, 0, 0, System.DateTimeKind.Unspecified)
                .AddMonths(1)
                .AddDays(_deadlineDay - 1);

        /// <summary>
        /// Chuyển timestamp nghiệp vụ giờ Việt Nam sang UTC. DateTimeKind.Unspecified
        /// là chủ ý vì input là giờ nghiệp vụ hiển thị trên đồng hồ, không phải timestamp
        /// theo múi giờ của máy.
        /// </summary>
        public static System.DateTime ToUtc(System.DateTime localBusinessTime)
        {
            var unspecified = System.DateTime.SpecifyKind(
                localBusinessTime, System.DateTimeKind.Unspecified);
            return System.TimeZoneInfo.ConvertTimeToUtc(unspecified, BusinessTimeZone);
        }

        /// <summary>Trả về thời điểm UTC bắt đầu kỳ được truyền vào.</summary>
        public System.DateTime StartAtUtc(Period period)
            => ToUtc(StartAtLocal(period));

        /// <summary>Trả về hạn gửi yêu cầu thường theo UTC.</summary>
        public System.DateTime SubmissionDeadlineUtc(Period period)
            => ToUtc(SubmissionDeadlineLocal(period));

        /// <summary>
        /// Trả về hạn UTC để duyệt yêu cầu bổ sung đang chờ. Khoảng grace được cộng
        /// sau hạn thường và không được âm.
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
        /// Chuyển giờ Việt Nam từ provider sang UTC. Caller hiện có chỉ expose
        /// <c>IDateTimeProvider.Now</c>, vì vậy helper này tập trung việc chuẩn hóa
        /// DateTimeKind cần cho timestamp được lưu.
        /// </summary>
        public static System.DateTime NormalizeNowUtc(System.DateTime now)
            => now.Kind == System.DateTimeKind.Utc ? now : ToUtc(now);
    }

    /// <summary>Kỳ theo lịch gồm năm và tháng, có tính cả Month.</summary>
    public readonly record struct Period(int Year, int Month)
    {
        public override string ToString() => $"{Year:D4}-{Month:D2}";
    }
}
