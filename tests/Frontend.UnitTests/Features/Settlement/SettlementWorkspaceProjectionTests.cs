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
            new SettlementOrderGroupFilter("", "", null, ""));

        Assert.Equal(4, rows.Count);
        var alpha = Assert.Single(rows, row => row.DepartmentCode == "A");
        Assert.Equal("Phòng Alpha", alpha.DepartmentName);
        Assert.Equal(2, alpha.OrderCount);
        Assert.Equal(1, alpha.RegularOrderCount);
        Assert.Equal(1, alpha.AdditionalOrderCount);
        Assert.Equal(3, alpha.TotalLines);
        Assert.Equal(15, alpha.TotalQuantity);
        Assert.Equal(150, alpha.TotalAmount);
        Assert.Equal(SettlementOrderGroupStatus.Pending, alpha.Status);
        Assert.Equal(
            SettlementOrderGroupStatus.Approved,
            Assert.Single(rows, row => row.DepartmentCode == "B").Status);
        Assert.Equal(
            SettlementOrderGroupStatus.NeedsReview,
            Assert.Single(rows, row => row.DepartmentCode == "C").Status);
        Assert.Equal(
            SettlementOrderGroupStatus.Submitted,
            Assert.Single(rows, row => row.DepartmentCode == "D").Status);

        var additionalOnly = SettlementWorkspaceProjection.BuildDepartmentRows(
            orders,
            departments,
            new SettlementOrderGroupFilter("alpha", "additional", null, "A"));
        var filteredAlpha = Assert.Single(additionalOnly);
        Assert.Equal(1, filteredAlpha.OrderCount);
        Assert.Equal(0, filteredAlpha.RegularOrderCount);
        Assert.Equal(1, filteredAlpha.AdditionalOrderCount);
        Assert.Equal(SettlementOrderGroupStatus.Approved, filteredAlpha.Status);
    }

    [Fact]
    public void BuildRequesterRows_GroupsOrdersByUserAndKeepsDepartmentContext()
    {
        var departments = new List<DepartmentResDTO>
        {
            new() { Code = "IT", Name = "Công nghệ thông tin" },
            new() { Code = "HR", Name = "Nhân sự" }
        };
        var orders = new List<VppRequestResDTO>
        {
            CreateOrder("IT", 7, false, 2, 5, 50, "REQ-1", 101, "Nguyễn An Nam"),
            CreateOrder("IT", 7, true, 3, 7, 70, "REQ-2", 101, "Nguyễn An Nam"),
            CreateOrder("HR", 6, false, 4, 9, 90, "REQ-3", 202, "Trần Minh Anh")
        };

        var rows = SettlementWorkspaceProjection.BuildRequesterRows(
            orders,
            departments,
            new SettlementOrderGroupFilter("", "", null, ""));

        Assert.Equal(2, rows.Count);
        var nam = Assert.Single(rows, row => row.UserId == 101);
        Assert.Equal("Nguyễn An Nam", nam.RequesterName);
        Assert.Equal("Công nghệ thông tin · IT", nam.DepartmentSummary);
        Assert.Equal(2, nam.OrderCount);
        Assert.Equal(1, nam.RegularOrderCount);
        Assert.Equal(1, nam.AdditionalOrderCount);
        Assert.Equal(5, nam.TotalLines);
        Assert.Equal(12, nam.TotalQuantity);
        Assert.Equal(120, nam.TotalAmount);
        Assert.Equal(SettlementOrderGroupStatus.Approved, nam.Status);

        var filtered = SettlementWorkspaceProjection.BuildRequesterRows(
            orders,
            departments,
            new SettlementOrderGroupFilter("minh anh", "regular", 6, "HR"));
        Assert.Equal(202, Assert.Single(filtered).UserId);
    }

    [Fact]
    public void GroupRows_UseSettlementAllocationsForNetVatAndGrossAmounts()
    {
        var departments = new List<DepartmentResDTO>
        {
            new() { Code = "IT", Name = "Công nghệ thông tin" }
        };
        var first = CreateOrder("IT", 7, false, 1, 2, 999, "REQ-1", 101, "Nguyễn An Nam");
        var second = CreateOrder("IT", 7, true, 1, 1, 999, "REQ-2", 101, "Nguyễn An Nam");
        var allocations = new List<SettlementFinancialAllocationResDTO>
        {
            new() { RequestHeaderId = first.Id, NetAmount = 200m, VatAmount = 20m, GrossAmount = 215m },
            new() { RequestHeaderId = second.Id, NetAmount = 100m, VatAmount = 10m, GrossAmount = 108m }
        };

        var department = Assert.Single(SettlementWorkspaceProjection.BuildDepartmentRows(
            [first, second],
            departments,
            new SettlementOrderGroupFilter("", "", null, ""),
            allocations));
        var requester = Assert.Single(SettlementWorkspaceProjection.BuildRequesterRows(
            [first, second],
            departments,
            new SettlementOrderGroupFilter("", "", null, ""),
            allocations));

        Assert.Equal(300m, department.NetAmount);
        Assert.Equal(30m, department.VatAmount);
        Assert.Equal(330m, department.GrossAmount);
        Assert.Equal(department.NetAmount, requester.NetAmount);
        Assert.Equal(department.VatAmount, requester.VatAmount);
        Assert.Equal(department.GrossAmount, requester.GrossAmount);
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

    [Fact]
    public void SummarizeRows_AddsAllVisibleGroupMetrics()
    {
        var departments = new[]
        {
            new SettlementDepartmentRow("A", "Alpha", 2, 1, 1, 5, 12, 100, 200, 20, 220, SettlementOrderGroupStatus.Approved),
            new SettlementDepartmentRow("B", "Beta", 1, 1, 0, 3, 7, 50, 80, 8, 88, SettlementOrderGroupStatus.Submitted)
        };
        var requesters = new[]
        {
            new SettlementRequesterRow(1, "An", "Alpha · A", 2, 1, 1, 5, 12, 100, 200, 20, 220, SettlementOrderGroupStatus.Approved),
            new SettlementRequesterRow(2, "Bình", "Beta · B", 1, 1, 0, 3, 7, 50, 80, 8, 88, SettlementOrderGroupStatus.Submitted)
        };

        var departmentTotals = SettlementWorkspaceProjection.SummarizeRows(departments);
        var requesterTotals = SettlementWorkspaceProjection.SummarizeRows(requesters);

        Assert.Equal(new SettlementGroupTotals(3, 2, 1, 8, 19, 280, 28, 308), departmentTotals);
        Assert.Equal(departmentTotals, requesterTotals);
    }

    [Fact]
    public void SummarizeItems_CountsDistinctOrdersAndUsesVisibleFinancialRows()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var items = new[]
        {
            new AggregatedVppItemResDTO
            {
                VppId = firstId,
                TotalQty = 7,
                Breakdown =
                [
                    new AggregatedVppItemBreakdownResDTO { Code = "REQ-1" },
                    new AggregatedVppItemBreakdownResDTO { Code = "REQ-2" }
                ]
            },
            new AggregatedVppItemResDTO
            {
                VppId = secondId,
                TotalQty = 5,
                Breakdown =
                [
                    new AggregatedVppItemBreakdownResDTO { Code = "req-2" },
                    new AggregatedVppItemBreakdownResDTO { Code = "REQ-3" }
                ]
            }
        };
        var financials = new Dictionary<Guid, SettlementItemFinancialValues>
        {
            [firstId] = new(10, 8, 70, 75.6m),
            [secondId] = new(20, 8, 100, 108)
        };

        var totals = SettlementWorkspaceProjection.SummarizeItems(items, financials);

        Assert.Equal(new SettlementItemTotals(3, 12, 170, 183.6m), totals);
    }

    private static VppRequestResDTO CreateOrder(
        string departmentCode,
        int status,
        bool isAdditional,
        int totalLines,
        int totalQuantity,
        long totalAmount,
        string requestCode,
        int userId = 1,
        string? requesterName = null) => new()
        {
            Id = Guid.NewGuid(),
            DepartmentCode = departmentCode,
            Status = status,
            IsAdditionalOrder = isAdditional,
            TotalLines = totalLines,
            TotalQty = totalQuantity,
            TotalAmount = totalAmount,
            VppCode = requestCode,
            CreatedByUserId = userId,
            RequesterName = requesterName ?? $"Người đặt {departmentCode}",
            Description = $"Đơn {departmentCode}"
        };
}
