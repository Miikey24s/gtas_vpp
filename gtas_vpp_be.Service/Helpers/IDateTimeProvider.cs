using System;

namespace gtas_vpp_be.Service.Helpers
{
    public interface IDateTimeProvider
    {
        DateTime Now { get; }
    }

    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
