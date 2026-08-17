using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.PriceListTests;

public sealed class PriceListExportServiceTests
{
    [Fact]
    public async Task ExportExcel_BuildsImportCompatibleWorkbookWithCurrentPrices()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var supplierId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var now = new DateTime(2026, 8, 15, 10, 0, 0);
        context.Set<Supplier>().Add(new Supplier
        {
            Id = supplierId,
            SupplierName = "VPP Gia Định",
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });
        context.Set<PriceList>().Add(new PriceList
        {
            Id = priceListId,
            PriceListCode = "BG-2026/08",
            PriceListName = "Bảng giá tháng 08",
            SupplierId = supplierId,
            Version = 1,
            Status = PriceListStatus.Published,
            CurrencyCode = "VND",
            EffectiveFromUtc = now,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });
        context.Set<LookupValue>().Add(new LookupValue
        {
            Id = unitId,
            Code = "REAM",
            Value = "Ram",
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });
        context.Set<VppItem>().Add(new VppItem
        {
            Id = itemId,
            VppCode = "A001",
            VppName = "Giấy A4",
            UomId = unitId,
            VppCategoryId = Guid.NewGuid(),
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });
        context.Set<SupplierProductMapping>().Add(new SupplierProductMapping
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            SupplierId = supplierId,
            VppItemId = itemId,
            Price = 125_000m,
            NetPrice = 125_000m,
            VatRate = 8m,
            MinimumOrderQuantity = 2m,
            LeadTimeDays = 3,
            IsDefault = true,
            Description = "Giá hợp đồng",
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new PriceListExportService(ServiceTestHelpers.CreateUnitOfWorkMock(context).Object);

        var export = await service.ExportExcelAsync(priceListId, TestContext.Current.CancellationToken);
        var parsed = await new PriceListImportFileParser().ParseAsync(
            new MemoryStream(export.Content),
            export.FileName,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ExportFileContract.ExcelContentType, export.ContentType);
        Assert.Equal("GTAS-VPP-Bang-gia-BG-2026-08.xlsx", export.FileName);
        var row = Assert.Single(parsed.Rows);
        Assert.Equal("A001", row.ItemCode);
        Assert.Equal("Giấy A4", row.ItemName);
        Assert.Equal("Ram", row.UnitName);
        Assert.Equal(125_000m, row.UnitPrice);
        Assert.Equal(8m, row.VatRate);
        Assert.Equal("Giá hợp đồng", row.Note);
        Assert.Empty(row.Issues);
    }
}
