using System;

namespace gtas_vpp_be.Service.Helpers
{
    public interface IDateTimeProvider
    {
        DateTime Now { get; }
    }

    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime Now
        {
            get
            {
                // Cố định múi giờ Việt Nam (Asia/Ho_Chi_Minh).
                try
                {
                    // ID múi giờ Việt Nam tương thích đa nền tảng.
                    // Windows dùng "SE Asia Standard Time", Linux/Docker dùng IANA "Asia/Ho_Chi_Minh".
                    var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(
                        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh"
                    );
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Fallback về UTC+7 nếu database múi giờ bị thiếu.
                    return DateTime.UtcNow.AddHours(7);
                }
            }
        }
    }
}
