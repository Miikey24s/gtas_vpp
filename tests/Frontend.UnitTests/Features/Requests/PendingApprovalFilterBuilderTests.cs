using gtas_vpp_fe.Features.Requests.Approval;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class PendingApprovalFilterBuilderTests
{
    [Fact]
    public void Build_Department_UsesExactNormalizedEquality()
    {
        var filter = PendingApprovalFilterBuilder.Build(" D01 ");

        Assert.Equal(
            "(DepartmentCode != null && DepartmentCode.ToLower() == \"d01\")",
            filter);
    }

    [Fact]
    public void Build_WithoutActiveFilters_ReturnsNull()
    {
        Assert.Null(PendingApprovalFilterBuilder.Build("  "));
    }
}
