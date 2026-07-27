using gtas_vpp_be.Service.Helpers;

namespace gtas_vpp_be.Tests.TestSupport;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTime now)
    {
        Now = now;
    }

    public DateTime Now { get; set; }
}
