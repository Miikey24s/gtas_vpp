using gtas_vpp_shared.Constants;
using System.Globalization;
using Xunit;

namespace gtas_vpp_be.Tests.Contracts;

/// <summary>
/// Locks the status-code → resource/text contract consumed by API responses and reports.
/// </summary>
public class VppStatusContractTests
{
    [Theory]
    [InlineData(1, false, false, "Submitted")]
    [InlineData(1, true, false, "SubmittedPeriodClosed")]
    [InlineData(1, true, true, "Submitted")]
    [InlineData(4, false, false, "Cancelled")]
    [InlineData(6, false, false, "Pending")]
    [InlineData(7, false, false, "Approved")]
    [InlineData(8, false, false, "Rejected")]
    [InlineData(99, false, false, "StatusUnknown")]
    public void GetResourceKey_ReturnsExpectedContractValue(
        int status,
        bool isDeadlinePassed,
        bool isAdditionalOrder,
        string expected)
    {
        Assert.Equal(
            expected,
            VppStatusContract.GetResourceKey(status, isDeadlinePassed, isAdditionalOrder));
    }

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
        Assert.Equal(expected, VppStatusContract.GetText(status));
    }

    [Theory]
    [InlineData(1, false, false, "Submitted")]
    [InlineData(1, true, false, "Submitted (Period Closed)")]
    [InlineData(4, false, false, "Cancelled")]
    public void GetText_WithCulture_ReturnsEnglishLabels(int status, bool isDeadlinePassed, bool isAdditionalOrder, string expected)
    {
        Assert.Equal(expected, VppStatusContract.GetText(status, isDeadlinePassed, isAdditionalOrder, CultureInfo.GetCultureInfo("en-US")));
    }

    [Theory]
    [InlineData(1, false, false, "Đã gửi")]
    [InlineData(1, true, false, "Đã gửi (đã khóa kỳ)")]
    [InlineData(6, false, false, "Chờ duyệt")]
    public void GetText_WithCulture_ReturnsVietnameseLabels(int status, bool isDeadlinePassed, bool isAdditionalOrder, string expected)
    {
        Assert.Equal(expected, VppStatusContract.GetText(status, isDeadlinePassed, isAdditionalOrder, CultureInfo.GetCultureInfo("vi-VN")));
    }
}
