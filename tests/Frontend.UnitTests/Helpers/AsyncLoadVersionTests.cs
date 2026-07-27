using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class AsyncLoadVersionTests
{
    [Fact]
    public void Begin_MakesPreviousVersionStale()
    {
        var versions = new AsyncLoadVersion();

        var first = versions.Begin();
        var second = versions.Begin();

        Assert.False(versions.IsCurrent(first));
        Assert.True(versions.IsCurrent(second));
    }
}
