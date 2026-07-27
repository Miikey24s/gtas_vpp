using gtas_vpp_fe.Helpers;
using Radzen;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class StatusDisplayTests
{
    [Theory]
    [InlineData(1, "vpp-badge-submitted")]
    [InlineData(4, "vpp-badge-cancelled")]
    [InlineData(6, "vpp-badge-pending")]
    [InlineData(7, "vpp-badge-approved")]
    [InlineData(8, "vpp-badge-rejected")]
    [InlineData(0, "vpp-badge-default")]
    public void GetCssClass_ReturnsExpectedClass(int status, string expected)
    {
        Assert.Equal(expected, StatusDisplay.GetCssClass(status));
    }

    [Theory]
    [InlineData(1, "Success", BadgeStyle.Success)]
    [InlineData(4, "Danger", BadgeStyle.Danger)]
    [InlineData(6, "Warning", BadgeStyle.Warning)]
    [InlineData(7, "Success", BadgeStyle.Success)]
    [InlineData(8, "Danger", BadgeStyle.Danger)]
    [InlineData(0, "Light", BadgeStyle.Light)]
    public void BadgeMappings_ReturnExpectedPresentation(
        int status,
        string expectedName,
        BadgeStyle expectedStyle)
    {
        Assert.Equal(expectedName, StatusDisplay.GetBadgeStyleName(status));
        Assert.Equal(expectedStyle, StatusDisplayRadzen.BadgeStyleFor(status));
    }

    [Fact]
    public void GetResourceKey_DelegatesSharedContractSemantics()
    {
        Assert.Equal("Submitted", StatusDisplay.GetResourceKey(1));
        Assert.Equal("SubmittedPeriodClosed", StatusDisplay.GetResourceKey(1, true, false));
        Assert.Equal("StatusUnknown", StatusDisplay.GetResourceKey(99));
    }
}
