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
                // Force the timezone to Vietnam (Asia/Ho_Chi_Minh)
                try
                {
                    // Cross-platform compatible timezone ID for Vietnam
                    // Windows uses "SE Asia Standard Time", Linux/Docker uses IANA "Asia/Ho_Chi_Minh"
                    var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(
                        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh"
                    );
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Fallback to UTC + 7 if the timezone database is somehow missing
                    return DateTime.UtcNow.AddHours(7);
                }
            }
        }
    }
}
