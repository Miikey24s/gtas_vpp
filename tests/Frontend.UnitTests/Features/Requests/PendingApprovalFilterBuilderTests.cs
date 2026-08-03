using gtas_vpp_fe.Features.Requests.Approval;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class PendingApprovalFilterBuilderTests
{
    [Fact]
    public void Build_SearchAndDepartment_PreservesCanonicalExpressionOrder()
    {
        var filter = PendingApprovalFilterBuilder.Build(" Paper ", " D01 ");

        Assert.Equal(
            "((VppCode != null && VppCode.ToLower().Contains(\"paper\")) || "
            + "(RequesterName != null && RequesterName.ToLower().Contains(\"paper\")) || "
            + "(DepartmentCode != null && DepartmentCode.ToLower().Contains(\"paper\")) || "
            + "(Description != null && Description.ToLower().Contains(\"paper\")))"
            + " && (DepartmentCode != null && DepartmentCode.ToLower() == \"d01\")",
            filter);
    }

    [Fact]
    public void Build_EscapesQuotesAndBackslashesAfterNormalizingCase()
    {
        var quoteFilter = PendingApprovalFilterBuilder.Build(" A\"B ", "");
        var slashFilter = PendingApprovalFilterBuilder.Build(" A\\B ", "");

        Assert.Contains("a\\\"b", quoteFilter, StringComparison.Ordinal);
        Assert.Contains("a\\\\b", slashFilter, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_DepartmentOnly_UsesExactNormalizedEquality()
    {
        Assert.Equal(
            "(DepartmentCode != null && DepartmentCode.ToLower() == \"it-01\")",
            PendingApprovalFilterBuilder.Build("", " IT-01 "));
    }

    [Fact]
    public void Build_WithoutActiveFilters_ReturnsNull()
    {
        Assert.Null(PendingApprovalFilterBuilder.Build("  ", ""));
    }
}
