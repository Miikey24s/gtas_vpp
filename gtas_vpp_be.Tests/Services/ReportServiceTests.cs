using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Res.Reports;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task Summary_EnforcesOwnDepartmentCompanyScopesBeforeAggregating()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var productId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, productId);
        await AddOrderAsync(context, productId, 10, "IT", "77500", 2, 100);
        await AddOrderAsync(context, productId, 11, "IT", "77500", 3, 100);
        await AddOrderAsync(context, productId, 12, "HR", "77500", 4, 100);
        await AddOrderAsync(context, productId, 10, "IT", "88000", 50, 100);
        var service = new ReportService(context);

        var own = await service.GetSummaryAsync(
            ReportScopes.Own, 10, "IT", "77500", 2026, null);
        var department = await service.GetSummaryAsync(
            ReportScopes.Department, 10, "IT", "77500", 2026, null);
        var all = await service.GetSummaryAsync(
            ReportScopes.All, 10, "IT", "77500", 2026, null);

        Assert.Equal((1, 2, 200L), (own.TotalOrders, own.TotalQuantity, own.TotalAmount));
        Assert.Equal((2, 5, 500L), (department.TotalOrders, department.TotalQuantity, department.TotalAmount));
        Assert.Equal((3, 9, 900L), (all.TotalOrders, all.TotalQuantity, all.TotalAmount));
        Assert.Equal(2, all.TotalDepartments);
        Assert.Equal(3, all.TotalRequesters);
        Assert.Single(all.PeriodTrend);
        Assert.Single(all.TopProducts);
    }

    [Theory]
    [InlineData("=1+1", "\"'=1+1\"")]
    [InlineData("@SUM(A1)", "\"'@SUM(A1)\"")]
    [InlineData("Normal, value", "\"Normal, value\"")]
    [InlineData("A\"B", "\"A\"\"B\"")]
    public void EscapeCsvCell_PreventsFormulaInjectionAndEscapesQuotes(string input, string expected)
    {
        Assert.Equal(expected, ReportService.EscapeCsvCell(input));
    }

    private static async Task AddOrderAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Guid productId,
        int userId,
        string department,
        string company,
        int quantity,
        long price)
    {
        var now = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
        var headerId = Guid.NewGuid();
        var header = new VPP01_RequestHeader
        {
            Id = headerId,
            VPPCode = $"VPP-{headerId:N}",
            Y = 2026,
            M = 7,
            Status = (int)VPPStatus.Submitted,
            DepartmentCode = department,
            MemberCompanyCode = company,
            CreateUserId = userId,
            CreateDate = now,
            UpdateUserId = userId,
            UpdateDate = now,
            SubmittedDate = now
        };
        header.VPP02_RequestDetails.Add(new VPP02_RequestDetail
        {
            Id = Guid.NewGuid(),
            VPPId = productId,
            VPP01_RequestHeaderId = headerId,
            Qty = quantity,
            CurrentSinglePrice = price,
            CreateUserId = userId,
            CreateDate = now,
            UpdateUserId = userId,
            UpdateDate = now
        });
        context.Add(header);
        await context.SaveChangesAsync();
    }
}
