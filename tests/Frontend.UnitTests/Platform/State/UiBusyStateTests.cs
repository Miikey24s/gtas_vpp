using gtas_vpp_fe.Platform.State;
using Xunit;

namespace gtas_vpp_fe.Tests.Platform.State;

public sealed class UiBusyStateTests
{
    [Fact]
    public void NestedLeases_NotifyOnlyWhenVisibleBusyStateChanges()
    {
        var state = new UiBusyState();
        var notifications = 0;
        state.Changed += () => notifications++;

        var first = state.Begin();
        var second = state.Begin();

        Assert.True(state.IsBusy);
        Assert.Equal(1, notifications);

        first.Dispose();

        Assert.True(state.IsBusy);
        Assert.Equal(1, notifications);

        second.Dispose();

        Assert.False(state.IsBusy);
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void Lease_DisposeIsIdempotent()
    {
        var state = new UiBusyState();
        var notifications = 0;
        state.Changed += () => notifications++;
        var lease = state.Begin();

        lease.Dispose();
        lease.Dispose();

        Assert.False(state.IsBusy);
        Assert.Equal(2, notifications);
    }
}
