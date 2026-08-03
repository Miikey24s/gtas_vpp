using gtas_vpp_fe.Features.Settlement.Projection;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Settlement;

public sealed class SettlementWorkspaceProjectionTests
{
    [Fact]
    public void BuildDepartmentRows_FiltersBeforeGroupingAndPreservesStatusPrecedenceAndTotals()
    {
        var departments = new List<DepartmentResDTO>
        {
            new() { Code = "A", Name = "Phòng Alpha" },
            new() { Code = "B", Name = "Phòng Beta" },
            new() { Code = "C", Name = "Phòng Gamma" },
            new() { Code = "D", Name = "Phòng Delta" }
        };
        var orders = new List<VppRequestResDTO>
        {
            CreateOrder("A", 6, false, 1, 10, 100, "REQ-A1"),
            CreateOrder("A", 7, true, 2, 5, 50, "REQ-A2"),
            CreateOrder("B", 7, false, 3, 12, 120, "REQ-B1"),
            CreateOrder("B", 7, true, 4, 8, 80, "REQ-B2"),
            CreateOrder("C", 4, false, 5, 9, 90, "REQ-C1"),
            CreateOrder("D", 1, false, 6, 11, 110, "REQ-D1")
        };

        var rows = SettlementWorkspaceProjection.BuildDepartmentRows(
            orders,
            departments,
            new SettlementDepartmentFilter("", "", null, ""));

        Assert.Equal(4, rows.Count);
        var alpha = Assert.Single(rows, row => row.DepartmentCode == "A");
        Assert.Equal("Phòng Alpha", alpha.DepartmentName);
        Assert.Equal(2, alpha.OrderCount);
        Assert.Equal(1, alpha.RegularOrderCount);
        Assert.Equal(1, alpha.AdditionalOrderCount);
        Assert.Equal(3, alpha.TotalLines);
        Assert.Equal(15, alpha.TotalQuantity);
        Assert.Equal(150, alpha.TotalAmount);
        Assert.Equal(SettlementDepartmentStatus.Pending, alpha.Status);
        Assert.Equal(
            SettlementDepartmentStatus.Approved,
            Assert.Single(rows, row => row.DepartmentCode == "B").Status);
        Assert.Equal(
            SettlementDepartmentStatus.NeedsReview,
            Assert.Single(rows, row => row.DepartmentCode == "C").Status);
        Assert.Equal(
            SettlementDepartmentStatus.Submitted,
            Assert.Single(rows, row => row.DepartmentCode == "D").Status);

        var additionalOnly = SettlementWorkspaceProjection.BuildDepartmentRows(
            orders,
            departments,
            new SettlementDepartmentFilter("alpha", "additional", null, "A"));
        var filteredAlpha = Assert.Single(additionalOnly);
        Assert.Equal(1, filteredAlpha.OrderCount);
        Assert.Equal(0, filteredAlpha.RegularOrderCount);
        Assert.Equal(1, filteredAlpha.AdditionalOrderCount);
        Assert.Equal(SettlementDepartmentStatus.Approved, filteredAlpha.Status);
    }

    [Fact]
    public void BuildDepartmentOptions_UsesAuthorizedOrderCodesAndDirectoryFallback()
    {
        var orders = new List<VppRequestResDTO>
        {
            new() { DepartmentCode = "B" },
            new() { DepartmentCode = "a" },
            new() { DepartmentCode = "A" },
            new() { DepartmentCode = null },
            new() { DepartmentCode = "UNKNOWN" }
        };
        var departments = new List<DepartmentResDTO>
        {
            new() { Code = "A", Name = "Alpha" },
            new() { Code = "B", Name = "Beta" }
        };

        var options = SettlementWorkspaceProjection.BuildDepartmentOptions(orders, departments);

        Assert.Equal(3, options.Count);
        Assert.Equal(["Alpha", "Beta", "UNKNOWN"], options.Select(option => option.Name));
        Assert.Equal("a", options[0].Code);
    }

    [Fact]
    public void ItemProjection_BuildsDistinctOptionsAndAppliesSearchCategoryAndUnitTogether()
    {
        var items = new List<AggregatedVppItemResDTO>
        {
            new() { VppCode = "PAPER-A4", VppName = "Giấy A4", CategoryName = "Giấy", UomName = "Ram" },
            new() { VppCode = "PEN-BLUE", VppName = "Bút xanh", CategoryName = "Bút", UomName = "Cây" },
            new() { VppCode = "PAPER-A3", VppName = "Giấy A3", CategoryName = "giấy", UomName = "Ram" },
            new() { VppCode = "NO-META", VppName = "Không phân loại", CategoryName = null, UomName = "" }
        };

        var categories = SettlementWorkspaceProjection.GetDistinctItemCategories(items);
        var units = SettlementWorkspaceProjection.GetDistinctItemUnits(items);
        var filtered = SettlementWorkspaceProjection.FilterItems(
            items,
            new SettlementItemFilter("paper", "Giấy", "Ram"));

        Assert.Equal(2, categories.Count);
        Assert.Equal(2, units.Count);
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, item => Assert.Equal("Ram", item.UomName));
    }

    private static VppRequestResDTO CreateOrder(
        string departmentCode,
        int status,
        bool isAdditional,
        int totalLines,
        int totalQuantity,
        long totalAmount,
        string requestCode) => new()
        {
            DepartmentCode = departmentCode,
            Status = status,
            IsAdditionalOrder = isAdditional,
            TotalLines = totalLines,
            TotalQty = totalQuantity,
            TotalAmount = totalAmount,
            VppCode = requestCode,
            RequesterName = $"Người đặt {departmentCode}",
            Description = $"Đơn {departmentCode}"
        };
}
