using gtas_vpp_fe.State;
using Xunit;

namespace gtas_vpp_fe.Tests.State;

public sealed class GlobalClassTests
{
    [Fact]
    public void BusyChanged_fires_only_when_visible_busy_state_changes()
    {
        var state = new GlobalClass();
        var notifications = 0;
        state.BusyChanged += () => notifications++;

        state.isBusyPage = true;
        state.isBusyPage = true;
        state.isBusyPage = false;

        Assert.True(state.isBusyPage);
        Assert.Equal(1, notifications);

        state.isBusyPage = false;

        Assert.False(state.isBusyPage);
        Assert.Equal(2, notifications);
    }
}
