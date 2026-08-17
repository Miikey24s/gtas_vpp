using System.IO.Compression;
using System.Text;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Services;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class SettlementExportBuilderTests
{
    [Fact]
    public void Workbook_ContainsSnapshotItemsAndAllocations()
    {
        var settlement = SampleSettlement();
        var supplierNames = settlement.Items.ToDictionary(item => item.SupplierId, _ => "VPP Gia Định");
        var priceListNames = settlement.Items.ToDictionary(item => item.PriceListId, _ => "Bảng giá miền Nam");

        var bytes = SettlementWorkbookBuilder.Build(settlement, supplierNames, priceListNames);

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.Equal(3, archive.Entries.Count(entry =>
            entry.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal)));
        Assert.Contains("VPP Gia Định", ReadEntry(archive, "xl/worksheets/sheet1.xml"));
        var itemSheet = ReadEntry(archive, "xl/worksheets/sheet2.xml");
        Assert.Contains("Bút bi Thiên Long TL-027", itemSheet);
        Assert.Contains("VPP Gia Định", itemSheet);
        Assert.Contains("Bảng giá miền Nam", itemSheet);
        Assert.Contains("IT", ReadEntry(archive, "xl/worksheets/sheet3.xml"));
        Assert.Contains("state=\"frozen\"", ReadEntry(archive, "xl/worksheets/sheet2.xml"));
        Assert.Contains("autoFilter", ReadEntry(archive, "xl/worksheets/sheet3.xml"));
    }

    [Fact]
    public void Pdf_ProducesValidSettlementDocument()
    {
        var settlement = SampleSettlement();
        var supplierNames = settlement.Items.ToDictionary(item => item.SupplierId, _ => "VPP Gia Định");
        var bytes = SettlementPdfBuilder.Build(settlement, supplierNames);

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private static Settlement SampleSettlement()
    {
        var settlementId = Guid.NewGuid();
        var item = new SettlementItem
        {
            Id = Guid.NewGuid(),
            SettlementId = settlementId,
            VppId = Guid.NewGuid(),
            VppCode = "VPP_74CBE20A69F8",
            VppName = "Bút bi Thiên Long TL-027",
            UomId = Guid.NewGuid(),
            UomCode = "PIECE",
            UomName = "Cây",
            SupplierId = Guid.NewGuid(),
            PriceListId = Guid.NewGuid(),
            PriceBookItemId = Guid.NewGuid(),
            Quantity = 8,
            NetUnitPrice = 8000,
            VatRate = 8,
            NetAmount = 64000,
            VatAmount = 5120,
            GrossAmount = 69120,
            MinimumOrderQuantity = 1,
            LeadTimeDays = 2
        };

        var allocation = new SettlementAllocation
        {
            Id = Guid.NewGuid(),
            SettlementId = settlementId,
            SettlementItemId = item.Id,
            SettlementItem = item,
            RequestHeaderId = Guid.NewGuid(),
            RequestDetailId = Guid.NewGuid(),
            DepartmentCode = "IT",
            RequesterUserId = 7,
            Quantity = 8,
            NetAmount = 64000,
            VatAmount = 5120,
            GrossAmount = 69120
        };

        return new Settlement
        {
            Id = settlementId,
            PeriodId = Guid.NewGuid(),
            MemberCompanyCode = "1",
            Year = 2026,
            Month = 7,
            RevisionNumber = 2,
            IsCurrentRevision = true,
            IsCorrection = true,
            CorrectionReason = "Điều chỉnh bảng giá đã duyệt",
            PrimarySupplierId = item.SupplierId,
            PrimarySupplierName = "VPP Gia Định",
            PriceListId = item.PriceListId,
            PriceListCode = "DEFAULT",
            PriceListName = "DEFAULT",
            PriceListVersion = 1,
            PriceAsOfUtc = new DateTime(2026, 7, 31, 1, 0, 0, DateTimeKind.Utc),
            CalculationVersion = "v1",
            InputHash = new string('A', 64),
            IdempotencyKey = "settlement-test-key",
            CommandPayloadHash = new string('B', 64),
            CurrencyCode = "VND",
            Subtotal = 64000,
            VatAmount = 5120,
            GrandTotal = 69120,
            ConfirmedAtUtc = new DateTime(2026, 7, 31, 2, 0, 0, DateTimeKind.Utc),
            ConfirmedByUserId = 11,
            Items = [item],
            Allocations = [allocation]
        };
    }

    private static string ReadEntry(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path);
        Assert.NotNull(entry);
        using var reader = new StreamReader(entry!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
