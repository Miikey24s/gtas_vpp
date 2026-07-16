using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Res.Reports;
using System.IO.Compression;
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

    [Fact]
    public async Task ExportWorkbook_ReturnsMultiSheetXlsxWithFormulaSafeMetadata()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var productId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, productId);
        await AddOrderAsync(context, productId, 10, "IT", "77500", 2, 100);
        var service = new ReportService(context);

        var export = await service.ExportWorkbookAsync(
            ReportScopes.All, 10, "IT", "77500", 2026, 7);

        Assert.EndsWith(".xlsx", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", export.ContentType);
        using var archive = new ZipArchive(new MemoryStream(export.Content), ZipArchiveMode.Read);
        Assert.Contains(archive.Entries, entry => entry.FullName == "xl/workbook.xml");
        Assert.Contains(archive.Entries, entry => entry.FullName == "xl/worksheets/sheet1.xml");
        Assert.Contains(archive.Entries, entry => entry.FullName == "xl/worksheets/sheet2.xml");
        Assert.Contains(archive.Entries, entry => entry.FullName == "xl/worksheets/sheet5.xml");
    }

    [Fact]
    public async Task Summary_UsesCurrentSettlementAllocationAndReportsZeroVariance()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var productId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, productId);
        var headerId = Guid.NewGuid();
        var detailId = Guid.NewGuid();
        var now = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc);
        context.Set<VPP01_RequestHeader>().Add(new VPP01_RequestHeader
        {
            Id = headerId,
            Y = 2026,
            M = 7,
            Status = (int)VPPStatus.Submitted,
            DepartmentCode = "IT",
            MemberCompanyCode = "77500",
            CreateUserId = 10,
            CreateDate = now,
            UpdateUserId = 10,
            UpdateDate = now,
            VPP02_RequestDetails =
            [
                new VPP02_RequestDetail
                {
                    Id = detailId,
                    VPPId = productId,
                    Qty = 2,
                    CurrentSinglePrice = 100,
                    CreateUserId = 10,
                    CreateDate = now,
                    UpdateUserId = 10,
                    UpdateDate = now
                }
            ]
        });
        var settlementId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        context.Set<VPP04_Settlement>().Add(new VPP04_Settlement
        {
            Id = settlementId,
            PeriodId = Guid.NewGuid(),
            MemberCompanyCode = "77500",
            Y = 2026,
            M = 7,
            RevisionNumber = 1,
            IsCurrentRevision = true,
            PrimarySupplierId = Guid.NewGuid(),
            PrimarySupplierName = "Supplier",
            PriceListId = Guid.NewGuid(),
            PriceListName = "Book",
            PriceListVersion = 1,
            PriceAsOfUtc = now,
            CalculationVersion = "price-vat-v2-vnd-whole",
            InputHash = new string('A', 64),
            IdempotencyKey = "report-settlement-0001",
            CommandPayloadHash = new string('B', 64),
            CurrencyCode = "VND",
            Subtotal = 200,
            VatAmount = 20,
            GrandTotal = 1234,
            ConfirmedAtUtc = now,
            ConfirmedByUserId = 5615,
            CreateUserId = 5615,
            CreateDate = now,
            UpdateUserId = 5615,
            UpdateDate = now,
            Items =
            [
                new VPP05_SettlementItem
                {
                    Id = itemId,
                    SettlementId = settlementId,
                    VppId = productId,
                    VppCode = "P-1",
                    VppName = "Paper",
                    UomId = Guid.NewGuid(),
                    UomCode = "EA",
                    UomName = "Each",
                    SupplierId = Guid.NewGuid(),
                    PriceListId = Guid.NewGuid(),
                    PriceBookItemId = Guid.NewGuid(),
                    Quantity = 2,
                    NetUnitPrice = 100,
                    VatRate = 10,
                    NetAmount = 200,
                    VatAmount = 20,
                    GrossAmount = 220,
                    CreateUserId = 5615,
                    CreateDate = now,
                    UpdateUserId = 5615,
                    UpdateDate = now
                }
            ],
            Allocations =
            [
                new VPP07_SettlementAllocation
                {
                    Id = Guid.NewGuid(),
                    SettlementId = settlementId,
                    SettlementItemId = itemId,
                    RequestHeaderId = headerId,
                    RequestDetailId = detailId,
                    DepartmentCode = "IT",
                    RequesterUserId = 10,
                    Quantity = 2,
                    NetAmount = 200,
                    VatAmount = 20,
                    CommercialAdjustmentAmount = 1014,
                    GrossAmount = 1234,
                    CreateUserId = 5615,
                    CreateDate = now,
                    UpdateUserId = 5615,
                    UpdateDate = now
                }
            ]
        });
        await context.SaveChangesAsync();

        var summary = await new ReportService(context).GetSummaryAsync(
            ReportScopes.All, 10, "IT", "77500", 2026, 7);

        Assert.True(summary.IsSettlementReconciled);
        Assert.Equal(1234, summary.TotalAmount);
        Assert.Equal(1234, summary.SettlementAllocationTotal);
        Assert.Equal(0, summary.SettlementVariance);
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
