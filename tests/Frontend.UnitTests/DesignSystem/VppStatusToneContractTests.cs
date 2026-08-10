using gtas_vpp_fe.Components.DesignSystem.Primitives;
using Xunit;

namespace gtas_vpp_fe.Tests.DesignSystem;

public sealed class VppStatusToneContractTests
{
    [Theory]
    [InlineData("Active", VppStatusTone.Info)]
    [InlineData("Open", VppStatusTone.Info)]
    [InlineData("Submitted", VppStatusTone.Info)]
    [InlineData("Scheduled", VppStatusTone.Info)]
    [InlineData("Approved", VppStatusTone.Success)]
    [InlineData("Settled", VppStatusTone.Success)]
    [InlineData("Published", VppStatusTone.Success)]
    [InlineData("Pending", VppStatusTone.Warning)]
    [InlineData("Pricing", VppStatusTone.Warning)]
    [InlineData("Rejected", VppStatusTone.Danger)]
    [InlineData("Cancelled", VppStatusTone.Danger)]
    [InlineData("Failed", VppStatusTone.Danger)]
    [InlineData("Draft", VppStatusTone.Neutral)]
    [InlineData("Expired", VppStatusTone.Neutral)]
    [InlineData("SubmissionClosed", VppStatusTone.Neutral)]
    public void Resolve_ReturnsCanonicalSemanticTone(string state, VppStatusTone expected)
    {
        Assert.Equal(expected, VppStatusToneContract.Resolve(state));
    }
}
