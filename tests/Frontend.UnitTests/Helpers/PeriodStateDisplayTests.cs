using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class PeriodStateDisplayTests
{
    [Theory]
    [InlineData("Open", "PeriodStateOpen")]
    [InlineData("SubmissionClosed", "PeriodStateSubmissionClosed")]
    [InlineData("Pricing", "PeriodStatePricing")]
    [InlineData("Settled", "PeriodStateSettled")]
    [InlineData("Draft", "PeriodStateDraft")]
    [InlineData("Scheduled", "PeriodStateScheduled")]
    public void GetResourceKey_ReturnsPeriodLifecycleResource(string state, string expected)
    {
        Assert.True(PeriodStateDisplay.IsKnown(state));
        Assert.Equal(expected, PeriodStateDisplay.GetResourceKey(state));
    }

    [Theory]
    [InlineData("Open", "PeriodStateOpenCompact")]
    [InlineData("SubmissionClosed", "PeriodStateSubmissionClosedCompact")]
    [InlineData("Pricing", "PeriodStatePricingCompact")]
    [InlineData("Settled", "PeriodStateSettledCompact")]
    public void GetCompactResourceKey_ReturnsDenseTableCopy(string state, string expected)
    {
        Assert.Equal(expected, PeriodStateDisplay.GetCompactResourceKey(state));
    }

    [Theory]
    [InlineData("Open", VppStatusTone.Info)]
    [InlineData("SubmissionClosed", VppStatusTone.Neutral)]
    [InlineData("Pricing", VppStatusTone.Warning)]
    [InlineData("Settled", VppStatusTone.Success)]
    [InlineData("Draft", VppStatusTone.Neutral)]
    [InlineData("Scheduled", VppStatusTone.Info)]
    public void GetTone_ReturnsLifecycleSemanticTone(string state, VppStatusTone expected)
    {
        Assert.Equal(expected, PeriodStateDisplay.GetTone(state));
    }

    [Fact]
    public void UnknownState_IsHiddenAndUsesFallbackResource()
    {
        Assert.False(PeriodStateDisplay.IsKnown(null));
        Assert.False(PeriodStateDisplay.IsKnown("Archived"));
        Assert.Equal("StatusUnknown", PeriodStateDisplay.GetResourceKey("Archived"));
    }
}
