using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class StatusDisplayTests
{
    [Theory]
    [InlineData(1, VppStatusTone.Success)]
    [InlineData(4, VppStatusTone.Danger)]
    [InlineData(6, VppStatusTone.Warning)]
    [InlineData(7, VppStatusTone.Success)]
    [InlineData(8, VppStatusTone.Danger)]
    [InlineData(0, VppStatusTone.Neutral)]
    public void GetTone_ReturnsExpectedSemanticTone(int status, VppStatusTone expected)
    {
        Assert.Equal(expected, StatusDisplay.GetTone(status));
    }

    [Fact]
    public void GetResourceKey_DelegatesSharedContractSemantics()
    {
        Assert.Equal("Submitted", StatusDisplay.GetResourceKey(1));
        Assert.Equal("SubmittedPeriodClosed", StatusDisplay.GetResourceKey(1, true, false));
        Assert.Equal("StatusUnknown", StatusDisplay.GetResourceKey(99));
    }
}
