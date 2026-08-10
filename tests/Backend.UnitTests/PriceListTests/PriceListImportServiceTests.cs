using System.Text;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.PriceListTests;

public sealed class PriceListImportServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 11, 9, 30, 0);

    [Fact]
    public async Task Parser_CsvSupportsVietnameseHeadersAndVietnameseNumbers()
    {
        const string csv = "Mã mặt hàng;Đơn giá;Thuế VAT;Số lượng tối thiểu;Số ngày giao;Mặc định\nA001;12.500;8,5;2,5;3;Có";
        var parser = new PriceListImportFileParser();

        var parsed = await parser.ParseAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(csv)),
            "bang-gia.csv");

        var row = Assert.Single(parsed.Rows);
        Assert.Equal("A001", row.ItemCode);
        Assert.Equal(12_500m, row.UnitPrice);
        Assert.Equal(8.5m, row.VatRate);
        Assert.Equal(2.5m, row.MinimumOrderQuantity);
        Assert.Equal(3, row.LeadTimeDays);
        Assert.True(row.IsDefault);
        Assert.Empty(row.Issues);
    }

    [Fact]
    public async Task Parser_XlsxReadsTemplateCompatibleWorkbook()
    {
        var workbook = SimpleWorkbookBuilder.Build(
        [
            new SimpleWorkbookSheet(
                "BangGia",
                [
                    new("ItemCode", 20),
                    new("UnitPrice", 18, SimpleWorkbookCellFormat.Decimal),
                    new("VatRate", 12, SimpleWorkbookCellFormat.Decimal)
                ],
                [["A001", 15000m, 8m]])
        ]);
        var parser = new PriceListImportFileParser();

        var parsed = await parser.ParseAsync(new MemoryStream(workbook), "bang-gia.xlsx");

        var row = Assert.Single(parsed.Rows);
        Assert.Equal("A001", row.ItemCode);
        Assert.Equal(15_000m, row.UnitPrice);
        Assert.Equal(8m, row.VatRate);
        Assert.Empty(parsed.GlobalIssues);
    }

    [Fact]
    public async Task Preview_ClassifiesAddUpdateUnchangedAndBlocksDuplicateCode()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context);
        var service = CreateService(context);
        const string csv = "ItemCode,ItemName,UnitPrice,VatRate\nA001,Bút A,100,8\nB001,Tên khác,250,10\nC001,Giấy C,300,8\nC001,Giấy C,300,8";

        var preview = await service.PreviewAsync(
            seed.PriceListId,
            "preview.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)),
            5615);

        Assert.Equal(4, preview.TotalRows);
        Assert.Equal(1, preview.UnchangedRows);
        Assert.Equal(1, preview.UpdatedRows);
        Assert.Equal(0, preview.AddedRows);
        Assert.Equal(2, preview.ErrorRows);
        Assert.Equal("Failed", preview.Status);
        Assert.False(preview.CanConfirm);
        Assert.Contains(preview.Rows.SelectMany(row => row.Issues), issue => issue.Code == "DUPLICATE_ITEM_CODE");
        Assert.Contains(preview.Rows.SelectMany(row => row.Issues), issue => issue.Code == "ITEM_NAME_MISMATCH");
    }

    [Fact]
    public async Task Confirm_UpsertsAtomicallyAndRetryReturnsSameCompletedBatch()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context);
        var service = CreateService(context);
        const string csv = "ItemCode,UnitPrice,VatRate,MinimumOrderQuantity,LeadTimeDays\nA001,100,8,,\nB001,250,10,2,4\nC001,300,8,3,5";
        var preview = await service.PreviewAsync(
            seed.PriceListId,
            "confirm.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)),
            5615);
        Assert.True(preview.CanConfirm);
        Assert.Equal(1, preview.AddedRows);
        Assert.Equal(1, preview.UpdatedRows);
        Assert.Equal(1, preview.UnchangedRows);

        var completed = await service.ConfirmAsync(
            seed.PriceListId,
            preview.Id,
            preview.RowVersion,
            5615);
        var retry = await service.ConfirmAsync(
            seed.PriceListId,
            preview.Id,
            preview.RowVersion,
            5615);

        Assert.Equal("Completed", completed.Status);
        Assert.Equal(completed.Id, retry.Id);
        Assert.Equal(3, await context.Set<SupplierProductMapping>().CountAsync(mapping => !mapping.IsDeleted));
        var updated = await context.Set<SupplierProductMapping>().SingleAsync(mapping => mapping.VppItemId == seed.ItemBId);
        Assert.Equal(250m, updated.Price);
        Assert.Equal(10m, updated.VatRate);
        var added = await context.Set<SupplierProductMapping>().SingleAsync(mapping => mapping.VppItemId == seed.ItemCId);
        Assert.Equal(300m, added.Price);
        Assert.Equal(3m, added.MinimumOrderQuantity);

        var duplicatePreview = await service.PreviewAsync(
            seed.PriceListId,
            "confirm.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)),
            5615);
        Assert.True(duplicatePreview.DuplicateFileWarning);
    }

    private static PriceListImportService CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context)
        => new(
            ServiceTestHelpers.CreateUnitOfWorkMock(context).Object,
            new FakeDateTimeProvider(Now),
            new PriceListImportFileParser());

    private static async Task<Seed> SeedAsync(gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var supplierId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();
        var itemAId = Guid.NewGuid();
        var itemBId = Guid.NewGuid();
        var itemCId = Guid.NewGuid();
        context.Set<Supplier>().Add(new Supplier
        {
            Id = supplierId,
            SupplierName = "Nhà cung cấp A",
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        });
        context.Set<PriceList>().Add(new PriceList
        {
            Id = priceListId,
            PriceListCode = "DEFAULT",
            PriceListName = "Bảng giá mặc định",
            SupplierId = supplierId,
            Version = 1,
            Status = PriceListStatus.Published,
            CurrencyCode = "VND",
            EffectiveFromUtc = Now,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        });
        context.Set<VppItem>().AddRange(
            Item(itemAId, "A001", "Bút A"),
            Item(itemBId, "B001", "Bút B"),
            Item(itemCId, "C001", "Giấy C"));
        context.Set<SupplierProductMapping>().AddRange(
            Mapping(priceListId, supplierId, itemAId, 100m, 8m),
            Mapping(priceListId, supplierId, itemBId, 200m, 8m));
        await context.SaveChangesAsync();
        return new Seed(priceListId, itemBId, itemCId);
    }

    private static VppItem Item(Guid id, string code, string name) => new()
    {
        Id = id,
        VppCode = code,
        VppName = name,
        UomId = Guid.NewGuid(),
        VppCategoryId = Guid.NewGuid(),
        CreatedByUserId = 1,
        CreatedAtUtc = Now,
        UpdatedByUserId = 1,
        UpdatedAtUtc = Now
    };

    private static SupplierProductMapping Mapping(
        Guid priceListId,
        Guid supplierId,
        Guid itemId,
        decimal price,
        decimal vatRate) => new()
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            SupplierId = supplierId,
            VppItemId = itemId,
            Price = price,
            NetPrice = price,
            VatRate = vatRate,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        };

    private sealed record Seed(Guid PriceListId, Guid ItemBId, Guid ItemCId);
}
