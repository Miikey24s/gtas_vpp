using System.IO.Compression;
using System.Text;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Res.VPP;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class OrderExportBuilderTests
{
    [Fact]
    public void Workbook_ContainsOrderMetadataAndItemsWithoutPrices()
    {
        var order = SampleOrder();

        var bytes = OrderWorkbookBuilder.Build(order);

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var sheet1 = ReadEntry(archive, "xl/worksheets/sheet1.xml");
        var sheet2 = ReadEntry(archive, "xl/worksheets/sheet2.xml");

        Assert.Contains("DEMO-PPJ-0726", sheet1);
        Assert.Contains("Kiểm thử phòng họp", sheet1);
        Assert.Contains("Bút bi Thiên Long TL-027", sheet2);
        Assert.Contains("Mực xanh", sheet2);
        // Quy tắc quyền: file xuất theo đơn không bao giờ chứa giá.
        Assert.DoesNotContain("price", sheet2, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("142000", sheet2);
    }

    [Fact]
    public void Workbook_HasExactlyTwoWorksheets()
    {
        var bytes = OrderWorkbookBuilder.Build(SampleOrder());

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var worksheetEntries = archive.Entries
            .Count(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal));

        Assert.Equal(2, worksheetEntries);
    }

    [Fact]
    public void Pdf_ProducesValidDocumentForVietnameseContent()
    {
        var bytes = OrderPdfBuilder.Build(SampleOrder());

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Pdf_SupportsAdditionalOrderWithReason()
    {
        var order = SampleOrder();
        order.IsAdditionalOrder = true;
        order.SupplementReason = "Bổ sung vật tư cho đợt tuyển dụng tháng 8";

        var bytes = OrderPdfBuilder.Build(order);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private static VppRequestResDTO SampleOrder() => new()
    {
        Id = Guid.NewGuid(),
        VppCode = "DEMO-PPJ-0726",
        Year = 2026,
        Month = 7,
        RevisionNumber = 1,
        Status = 1,
        DepartmentCode = "IT",
        RequesterName = "Nguyễn Văn Kiểm Thử",
        SubmittedDate = new DateTime(2026, 7, 23, 9, 42, 0, DateTimeKind.Utc),
        TotalLines = 2,
        TotalQty = 10,
        Description = "Kiểm thử phòng họp",
        Items =
        [
            new VppRequestDetailResDTO
            {
                VppId = Guid.NewGuid(),
                VppCode = "VPP_74CBE20A69F8",
                VppName = "Bút bi Thiên Long TL-027",
                CategoryName = "Bút viết",
                UomName = "Cây",
                Qty = 8,
                CurrentSinglePrice = 142000,
                Description = "Mực xanh"
            },
            new VppRequestDetailResDTO
            {
                VppId = Guid.NewGuid(),
                VppCode = "VPP_B5DF2B25582F",
                VppName = "Pin đồng hồ Pin 2A Energizer",
                CategoryName = "Phục vụ văn phòng",
                UomName = "Vỉ",
                Qty = 2
            }
        ]
    };

    private static string ReadEntry(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path);
        Assert.NotNull(entry);
        using var reader = new StreamReader(entry!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
