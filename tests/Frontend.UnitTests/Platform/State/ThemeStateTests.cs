using gtas_vpp_fe.Platform.State;
using Xunit;

namespace gtas_vpp_fe.Tests.Platform.State;

public sealed class ThemeStateTests
{
    [Fact]
    public void SetTheme_NotifiesOnlyWhenTheEffectiveThemeChanges()
    {
        var state = new ThemeState();
        var changeCount = 0;
        state.Changed += () => changeCount++;

        state.SetTheme(null);
        state.SetTheme("material3-dark");
        state.SetTheme("material3-dark");

        Assert.Equal("material3-dark", state.CurrentTheme);
        Assert.True(state.IsDark);
        Assert.Equal(1, changeCount);

        state.SetTheme("material3");

        Assert.False(state.IsDark);
        Assert.Equal(2, changeCount);
    }
}
