using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Res.Reports;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task Summary_RejectsInvalidScope()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetSummaryAsync(
            Query("invalid", year: 2026, month: 7)));
    }

    [Fact]
    public async Task Summary_RequiresMemberCompany()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetSummaryAsync(
            Query(ReportScopes.All, companyCode: "", year: 2026, month: 7)));
    }

    [Fact]
    public async Task Summary_RequiresDepartmentForDepartmentScope()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetSummaryAsync(
            Query(ReportScopes.Department, departmentCode: "", year: 2026, month: 7)));
    }

    [Theory]
    [InlineData(1999, 7)]
    [InlineData(2101, 7)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task Summary_RejectsOutOfRangePeriod(int year, int month)
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context);

        await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => service.GetSummaryAsync(
            Query(ReportScopes.All, year: year, month: month)));
    }

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
        var service = CreateService(context);

        var own = await service.GetSummaryAsync(Query(ReportScopes.Own, year: 2026));
        var department = await service.GetSummaryAsync(Query(ReportScopes.Department, year: 2026));
        var all = await service.GetSummaryAsync(Query(ReportScopes.All, year: 2026));

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
    public async Task ExportCsv_ReturnsBomCsvWithCanonicalMetadata()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var productId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, productId);
        await AddOrderAsync(context, productId, 10, "IT", "77500", 2, 100);
        var service = CreateService(context);

        var export = await service.ExportCsvAsync(Query(ReportScopes.All, year: 2026, month: 7));

        Assert.Equal("GTAS-VPP-Bao-cao-all-2026-07.csv", export.FileName);
        Assert.Equal("text/csv; charset=utf-8", export.ContentType);
        Assert.True(export.Content.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        var content = Encoding.UTF8.GetString(export.Content);
        Assert.StartsWith("\uFEFFsep=,", content, StringComparison.Ordinal);
        Assert.Contains(
            "Kỳ,Phòng ban,Mã đơn,Trạng thái,Đơn bổ sung,Mã mặt hàng,Tên mặt hàng,Số lượng,Đơn giá,Thành tiền",
            content,
            StringComparison.Ordinal);
        Assert.Contains($"TEST-VPP-{productId:N}", content, StringComparison.Ordinal);
        Assert.Contains(",2,100,200", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportWorkbook_ReturnsMultiSheetXlsxWithFormulaSafeMetadata()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var productId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, productId);
        await AddOrderAsync(context, productId, 10, "IT", "77500", 2, 100);
        var service = CreateService(context);

        var export = await service.ExportWorkbookAsync(Query(ReportScopes.All, year: 2026, month: 7));

        Assert.EndsWith(".xlsx", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", export.ContentType);
        using var archive = new ZipArchive(new MemoryStream(export.Content), ZipArchiveMode.Read);
        Assert.Contains(archive.Entries, entry => entry.FullName == "xl/workbook.xml");
        Assert.Contains(archive.Entries, entry => entry.FullName == "xl/worksheets/sheet1.xml");
        Assert.Contains(archive.Entries, entry => entry.FullName == "xl/worksheets/sheet2.xml");
        Assert.Contains(archive.Entries, entry => entry.FullName == "xl/worksheets/sheet5.xml");
        var workbook = ReadEntry(archive, "xl/workbook.xml");
        var firstSheet = ReadEntry(archive, "xl/worksheets/sheet1.xml");
        Assert.Contains("Tổng quan", workbook);
        Assert.Contains("state=\"frozen\"", firstSheet);
        Assert.Contains("autoFilter", firstSheet);
    }

    [Fact]
    public async Task ExportPdf_ReturnsNamedVietnameseSummaryDocument()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var productId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, productId);
        await AddOrderAsync(context, productId, 10, "IT", "77500", 2, 100);
        var service = CreateService(context);

        var export = await service.ExportPdfAsync(Query(ReportScopes.All, year: 2026, month: 7));

        Assert.Equal(ExportFileContract.PdfContentType, export.ContentType);
        Assert.Equal("GTAS-VPP-Bao-cao-all-2026-07.pdf", export.FileName);
        Assert.True(export.Content.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(export.Content, 0, 4));
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
        context.Set<VppRequest>().Add(new VppRequest
        {
            Id = headerId,
            Year = 2026,
            Month = 7,
            Status = (int)VPPStatus.Submitted,
            DepartmentCode = "IT",
            MemberCompanyCode = "77500",
            CreatedByUserId = 10,
            CreatedAtUtc = now,
            UpdatedByUserId = 10,
            UpdatedAtUtc = now,
            RequestDetails =
            [
                new VppRequestDetail
                {
                    Id = detailId,
                    VppId = productId,
                    Qty = 2,
                    CurrentSinglePrice = 100,
                    CreatedByUserId = 10,
                    CreatedAtUtc = now,
                    UpdatedByUserId = 10,
                    UpdatedAtUtc = now
                }
            ]
        });
        var settlementId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        context.Set<Settlement>().Add(CreateSettlement(
            productId,
            headerId,
            detailId,
            revisionNumber: 1,
            isCurrentRevision: false,
            grossAmount: 999,
            productCode: "P-OLD"));
        context.Set<Settlement>().Add(new Settlement
        {
            Id = settlementId,
            PeriodId = Guid.NewGuid(),
            MemberCompanyCode = "77500",
            Year = 2026,
            Month = 7,
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
            CreatedByUserId = 5615,
            CreatedAtUtc = now,
            UpdatedByUserId = 5615,
            UpdatedAtUtc = now,
            Items =
            [
                new SettlementItem
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
                    CreatedByUserId = 5615,
                    CreatedAtUtc = now,
                    UpdatedByUserId = 5615,
                    UpdatedAtUtc = now
                }
            ],
            Allocations =
            [
                new SettlementAllocation
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
                    CreatedByUserId = 5615,
                    CreatedAtUtc = now,
                    UpdatedByUserId = 5615,
                    UpdatedAtUtc = now
                }
            ]
        });
        await context.SaveChangesAsync();

        var summary = await CreateService(context).GetSummaryAsync(
            Query(ReportScopes.All, year: 2026, month: 7));

        Assert.True(summary.IsSettlementReconciled);
        Assert.Equal(1234, summary.TotalAmount);
        Assert.Equal(1234, summary.SettlementAllocationTotal);
        Assert.Equal(0, summary.SettlementVariance);
        Assert.Equal(1, summary.SettlementRevisionNumber);
        Assert.Equal("P-1", Assert.Single(summary.TopProducts).ProductCode);
    }

    [Fact]
    public async Task ExportWorkbook_AppliesOwnScopeToSettlementAllocations()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, firstProductId);
        await ServiceTestHelpers.SeedActiveVPPAsync(context, secondProductId);
        var ownHeaderId = await AddOrderAsync(context, firstProductId, 10, "IT", "77500", 2, 100);
        var otherHeaderId = await AddOrderAsync(context, secondProductId, 11, "IT", "77500", 3, 100);
        var ownDetailId = context.Set<VppRequestDetail>().Single(x => x.RequestId == ownHeaderId).Id;
        var otherDetailId = context.Set<VppRequestDetail>().Single(x => x.RequestId == otherHeaderId).Id;
        var settlementId = Guid.NewGuid();
        var ownItemId = Guid.NewGuid();
        var otherItemId = Guid.NewGuid();
        var now = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc);
        context.Set<Settlement>().Add(new Settlement
        {
            Id = settlementId,
            PeriodId = Guid.NewGuid(),
            MemberCompanyCode = "77500",
            Year = 2026,
            Month = 7,
            RevisionNumber = 2,
            IsCurrentRevision = true,
            PrimarySupplierId = Guid.NewGuid(),
            PrimarySupplierName = "Supplier",
            PriceListId = Guid.NewGuid(),
            PriceListName = "Book",
            PriceListVersion = 1,
            PriceAsOfUtc = now,
            CalculationVersion = "price-vat-v2-vnd-whole",
            InputHash = new string('A', 64),
            IdempotencyKey = "report-scope-0001",
            CommandPayloadHash = new string('B', 64),
            CurrencyCode = "VND",
            Subtotal = 500,
            VatAmount = 50,
            GrandTotal = 550,
            ConfirmedAtUtc = now,
            ConfirmedByUserId = 5615,
            CreatedByUserId = 5615,
            CreatedAtUtc = now,
            UpdatedByUserId = 5615,
            UpdatedAtUtc = now,
            Items =
            [
                CreateSettlementItem(settlementId, ownItemId, firstProductId, "P-OWN", 2, 200, now),
                CreateSettlementItem(settlementId, otherItemId, secondProductId, "P-OTHER", 3, 300, now)
            ],
            Allocations =
            [
                CreateAllocation(settlementId, ownItemId, ownHeaderId, ownDetailId, 10, "IT", 2, 220, now),
                CreateAllocation(settlementId, otherItemId, otherHeaderId, otherDetailId, 11, "IT", 3, 330, now)
            ]
        });
        await context.SaveChangesAsync();

        var export = await CreateService(context).ExportWorkbookAsync(
            Query(ReportScopes.Own, year: 2026, month: 7));

        using var archive = new ZipArchive(new MemoryStream(export.Content), ZipArchiveMode.Read);
        var itemSheet = ReadEntry(archive, "xl/worksheets/sheet2.xml");
        Assert.Contains("P-OWN", itemSheet, StringComparison.Ordinal);
        Assert.DoesNotContain("P-OTHER", itemSheet, StringComparison.Ordinal);
    }

    private static async Task<Guid> AddOrderAsync(
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
        var header = new VppRequest
        {
            Id = headerId,
            VppCode = $"VPP-{headerId:N}",
            Year = 2026,
            Month = 7,
            Status = (int)VPPStatus.Submitted,
            DepartmentCode = department,
            MemberCompanyCode = company,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedByUserId = userId,
            UpdatedAtUtc = now,
            SubmittedDate = now
        };
        header.RequestDetails.Add(new VppRequestDetail
        {
            Id = Guid.NewGuid(),
            VppId = productId,
            RequestId = headerId,
            Qty = quantity,
            CurrentSinglePrice = price,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedByUserId = userId,
            UpdatedAtUtc = now
        });
        context.Add(header);
        await context.SaveChangesAsync();
        return headerId;
    }

    private static ReportService CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context) =>
        new(context, new CurrentSettlementReportReader(context));

    private static ReportQueryContext Query(
        string scope,
        int userId = 10,
        string departmentCode = "IT",
        string companyCode = "77500",
        int? year = null,
        int? month = null) => new(
            scope,
            userId,
            departmentCode,
            companyCode,
            year,
            month);

    private static Settlement CreateSettlement(
        Guid productId,
        Guid headerId,
        Guid detailId,
        int revisionNumber,
        bool isCurrentRevision,
        decimal grossAmount,
        string productCode)
    {
        var settlementId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var now = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);
        return new Settlement
        {
            Id = settlementId,
            PeriodId = Guid.NewGuid(),
            MemberCompanyCode = "77500",
            Year = 2026,
            Month = 7,
            RevisionNumber = revisionNumber,
            IsCurrentRevision = isCurrentRevision,
            PrimarySupplierId = Guid.NewGuid(),
            PrimarySupplierName = "Old supplier",
            PriceListId = Guid.NewGuid(),
            PriceListName = "Old book",
            PriceListVersion = 1,
            PriceAsOfUtc = now,
            CalculationVersion = "price-vat-v2-vnd-whole",
            InputHash = new string('C', 64),
            IdempotencyKey = $"report-old-{Guid.NewGuid():N}",
            CommandPayloadHash = new string('D', 64),
            CurrencyCode = "VND",
            Subtotal = grossAmount,
            VatAmount = 0,
            GrandTotal = grossAmount,
            ConfirmedAtUtc = now,
            ConfirmedByUserId = 5615,
            CreatedByUserId = 5615,
            CreatedAtUtc = now,
            UpdatedByUserId = 5615,
            UpdatedAtUtc = now,
            Items = [CreateSettlementItem(settlementId, itemId, productId, productCode, 1, grossAmount, now)],
            Allocations = [CreateAllocation(settlementId, itemId, headerId, detailId, 10, "IT", 1, grossAmount, now)]
        };
    }

    private static SettlementItem CreateSettlementItem(
        Guid settlementId,
        Guid itemId,
        Guid productId,
        string productCode,
        decimal quantity,
        decimal grossAmount,
        DateTime now) => new()
        {
            Id = itemId,
            SettlementId = settlementId,
            VppId = productId,
            VppCode = productCode,
            VppName = productCode,
            UomId = Guid.NewGuid(),
            UomCode = "EA",
            UomName = "Each",
            SupplierId = Guid.NewGuid(),
            PriceListId = Guid.NewGuid(),
            PriceBookItemId = Guid.NewGuid(),
            Quantity = quantity,
            NetUnitPrice = grossAmount / quantity,
            VatRate = 0,
            NetAmount = grossAmount,
            VatAmount = 0,
            GrossAmount = grossAmount,
            CreatedByUserId = 5615,
            CreatedAtUtc = now,
            UpdatedByUserId = 5615,
            UpdatedAtUtc = now
        };

    private static SettlementAllocation CreateAllocation(
        Guid settlementId,
        Guid itemId,
        Guid headerId,
        Guid detailId,
        int requesterUserId,
        string departmentCode,
        decimal quantity,
        decimal grossAmount,
        DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            SettlementId = settlementId,
            SettlementItemId = itemId,
            RequestHeaderId = headerId,
            RequestDetailId = detailId,
            DepartmentCode = departmentCode,
            RequesterUserId = requesterUserId,
            Quantity = quantity,
            NetAmount = grossAmount,
            VatAmount = 0,
            GrossAmount = grossAmount,
            CreatedByUserId = 5615,
            CreatedAtUtc = now,
            UpdatedByUserId = 5615,
            UpdatedAtUtc = now
        };

    private static string ReadEntry(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path);
        Assert.NotNull(entry);
        using var reader = new StreamReader(entry!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
