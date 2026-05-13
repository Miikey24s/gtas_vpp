using gtas_vpp_shared.UI;
using Xunit;

namespace gtas_vpp_be.Tests.UI;

/// <summary>
/// Locks the status-code → label/css/badge mapping so the helper can never
/// silently drift from what the UI tabs expect.
/// </summary>
public class StatusDisplayTests
{
    [Theory]
    [InlineData(1, "Submitted")]
    [InlineData(4, "Cancelled")]
    [InlineData(6, "Pending")]
    [InlineData(7, "Approved")]
    [InlineData(8, "Rejected")]
    [InlineData(0, "-")]
    [InlineData(99, "-")]
    public void GetText_ReturnsExpectedLabel(int status, string expected)
    {
        Assert.Equal(expected, StatusDisplay.GetText(status));
    }

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
    [InlineData(1, "Success")]
    [InlineData(4, "Danger")]
    [InlineData(6, "Warning")]
    [InlineData(7, "Success")]
    [InlineData(8, "Danger")]
    [InlineData(0, "Light")]
    public void GetBadgeStyleName_ReturnsExpectedEnumName(int status, string expected)
    {
        Assert.Equal(expected, StatusDisplay.GetBadgeStyleName(status));
    }
}
