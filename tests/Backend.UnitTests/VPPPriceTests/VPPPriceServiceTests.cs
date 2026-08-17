using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gtas_vpp_be.Tests.VppItemPriceTests;

public class VPPPriceServiceTests
{
    [Fact]
    public async Task Create_FirstPriceWithIsDefaultTrue_Succeeds()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 20, 9, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var priceListId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context);
        var supplierId = await SeedSupplierAsync(context, "Supplier 1", now);
        var service = CreatePriceService(context, now);

        var result = await service.CreateAsync(CreateReq(vppId, supplierId, priceListId, 1000, isDefault: true), 5615);

        Assert.True(result.IsDefault);
        var row = Assert.Single(context.Set<SupplierProductMapping>());
        Assert.True(row.IsDefault);
        Assert.False(row.IsDeleted);
    }

    [Fact]
    public async Task Create_SecondPriceWithIsDefaultTrue_DemotesPrevious_OnlyOneDefaultRemains()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 20, 9, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var priceListId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context);
        var supplier1Id = await SeedSupplierAsync(context, "Supplier 1", now);
        var supplier2Id = await SeedSupplierAsync(context, "Supplier 2", now);
        var service = CreatePriceService(context, now);

        var first = await service.CreateAsync(CreateReq(vppId, supplier1Id, priceListId, 1000, isDefault: true), 5615);
        var second = await service.CreateAsync(CreateReq(vppId, supplier2Id, priceListId, 2000, isDefault: true), 5615);

        var rows = await context.Set<SupplierProductMapping>().OrderBy(x => x.Price).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.False(rows.Single(x => x.Id == first.Id).IsDefault);
        Assert.True(rows.Single(x => x.Id == second.Id).IsDefault);
        Assert.Equal(1, rows.Count(x => x.IsDefault && !x.IsDeleted));
    }

    [Fact]
    public async Task Update_FlipDefaultToTrue_DemotesPrevious()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 20, 9, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var priceListId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context);
        var supplier1Id = await SeedSupplierAsync(context, "Supplier 1", now);
        var supplier2Id = await SeedSupplierAsync(context, "Supplier 2", now);
        var service = CreatePriceService(context, now);
        var first = await service.CreateAsync(CreateReq(vppId, supplier1Id, priceListId, 1000, isDefault: true), 5615);
        var second = await service.CreateAsync(CreateReq(vppId, supplier2Id, priceListId, 2000, isDefault: false), 5615);

        await service.UpdateAsync(new SupplierProductPriceUpdateReqDTO
        {
            Id = second.Id,
            VppItemId = vppId,
            SupplierId = supplier2Id,
            PriceListId = priceListId,
            Price = 2500,
            IsDefault = true,
            Description = "Updated"
        }, 5615);

        var rows = await context.Set<SupplierProductMapping>().ToListAsync();
        Assert.False(rows.Single(x => x.Id == first.Id).IsDefault);
        Assert.True(rows.Single(x => x.Id == second.Id).IsDefault);
        Assert.Equal(1, rows.Count(x => x.IsDefault && !x.IsDeleted));
    }

    [Fact]
    public async Task SetDefault_SwitchesDefaultBetweenSuppliers()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 20, 9, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var priceListId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context);
        var supplier1Id = await SeedSupplierAsync(context, "Supplier 1", now);
        var supplier2Id = await SeedSupplierAsync(context, "Supplier 2", now);
        var service = CreatePriceService(context, now);
        var first = await service.CreateAsync(CreateReq(vppId, supplier1Id, priceListId, 1000, isDefault: true), 5615);
        var second = await service.CreateAsync(CreateReq(vppId, supplier2Id, priceListId, 2000, isDefault: false), 5615);

        await service.SetDefaultAsync(second.Id, 5615);

        var rows = await context.Set<SupplierProductMapping>().ToListAsync();
        Assert.False(rows.Single(x => x.Id == first.Id).IsDefault);
        Assert.True(rows.Single(x => x.Id == second.Id).IsDefault);
        Assert.Equal(1, rows.Count(x => x.IsDefault && !x.IsDeleted));
    }

    [Fact]
    public async Task Delete_Soft_HidesRowFromList()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 20, 9, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var priceListId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context);
        var supplier1Id = await SeedSupplierAsync(context, "Supplier 1", now);
        var supplier2Id = await SeedSupplierAsync(context, "Supplier 2", now);
        var service = CreatePriceService(context, now);
        var kept = await service.CreateAsync(CreateReq(vppId, supplier1Id, priceListId, 1000, isDefault: true), 5615);
        var deleted = await service.CreateAsync(CreateReq(vppId, supplier2Id, priceListId, 2000, isDefault: false), 5615);

        await service.DeleteAsync(deleted.Id, 5615);

        var deletedRow = await context.Set<SupplierProductMapping>().SingleAsync(x => x.Id == deleted.Id);
        Assert.True(deletedRow.IsDeleted);
        var listed = await service.ListByVPPAsync(vppId);
        var onlyVisible = Assert.Single(listed);
        Assert.Equal(kept.Id, onlyVisible.Id);
    }

    [Fact]
    public async Task SetDeleted_RestoresArchivedPriceMapping()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 7, 20, 9, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var priceListId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context);
        (await context.Set<PriceList>().SingleAsync(x => x.Id == priceListId)).Status = PriceListStatus.Published;
        await context.SaveChangesAsync();
        var supplierId = await SeedSupplierAsync(context, "Supplier 1", now);
        var service = CreatePriceService(context, now);
        var created = await service.CreateAsync(
            CreateReq(vppId, supplierId, priceListId, 1000, isDefault: true),
            5615);

        var archived = await service.SetDeletedAsync(created.Id, true, 5615);
        var restored = await service.SetDeletedAsync(created.Id, false, 5615);

        Assert.True(archived.IsDeleted);
        Assert.False(archived.IsDefault);
        Assert.False(restored.IsDeleted);
        Assert.False(restored.IsDefault);
        Assert.False((await context.Set<SupplierProductMapping>().SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task ListBySupplier_FiltersCorrectly()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 20, 9, 0, 0);
        var vppId1 = Guid.NewGuid();
        var vppId2 = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId1);
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId2);
        var priceListId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context);
        var supplier1Id = await SeedSupplierAsync(context, "Supplier 1", now);
        var supplier2Id = await SeedSupplierAsync(context, "Supplier 2", now);
        var service = CreatePriceService(context, now);

        await service.CreateAsync(CreateReq(vppId1, supplier1Id, priceListId, 1000, isDefault: true), 5615);
        await service.CreateAsync(CreateReq(vppId2, supplier1Id, priceListId, 2000, isDefault: false), 5615);
        await service.CreateAsync(CreateReq(vppId1, supplier2Id, priceListId, 1500, isDefault: true), 5615);

        var listed = await service.ListBySupplierAsync(supplier1Id, priceListId);
        Assert.Equal(2, listed.Count);
        Assert.All(listed, x => Assert.Equal(supplier1Id, x.SupplierId));
    }

    [Fact]
    public async Task QueryItemPrices_ProjectsMappingUpdatedTime()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 8, 14, 9, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var vpp = await context.Set<VppItem>().SingleAsync(item => item.Id == vppId);
        context.Set<LookupValue>().Add(new LookupValue
        {
            Id = vpp.UomId,
            Code = "EA",
            Value = "Cái",
            Sort = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted = false
        });
        var priceListId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context);
        var supplierId = await SeedSupplierAsync(context, "Supplier", now);
        context.Set<SupplierProductMapping>().Add(PriceRow(
            vppId,
            supplierId,
            priceListId,
            1000,
            isDefault: true,
            now));
        await context.SaveChangesAsync();

        var result = await CreatePriceService(context, now)
            .QueryItemPricesAsync(supplierId, priceListId);

        Assert.Equal(now, Assert.Single(result.Data).UpdatedAtUtc);
    }

    [Fact]
    public async Task GetCurrentSinglePrice_WithDefault_PrefersDefault()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 4, 1, 9, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var priceListId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context);
        var supplier1Id = await SeedSupplierAsync(context, "Supplier 1", now);
        var supplier2Id = await SeedSupplierAsync(context, "Supplier 2", now);
        context.Set<SupplierProductMapping>().AddRange(
            PriceRow(vppId, supplier1Id, priceListId, 100000, isDefault: false, now),
            PriceRow(vppId, supplier2Id, priceListId, 250000, isDefault: true, now));
        await context.SaveChangesAsync();
        var service = CreateRequestService(context, now);
        var request = new VppRequestCreateReqDTO
        {
            Year = 2026,
            Month = 3,
            Description = "Test order",
            IsAdditionalOrder = false,
            Items = new List<VppRequestDetailItemReqDTO>
            {
                new() { VppId = vppId, Qty = 1, Description = "Item" }
            }
        };

        await service.CreateOrderAsync(request, 5615, "IT", "77500");

        var detail = Assert.Single(context.Set<VppRequestDetail>());
        Assert.Equal(250000L, detail.CurrentSinglePrice);
    }

    private static VPPPriceService CreatePriceService(gtas_vpp_be.Service.Helpers.Context.VPPContext context, DateTime now)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        return new VPPPriceService(unitOfWork.Object, new FakeDateTimeProvider(now));
    }

    private static VPPRequestService CreateRequestService(gtas_vpp_be.Service.Helpers.Context.VPPContext context, DateTime now)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();

        return new VPPRequestService(
            unitOfWork.Object,
            new FakeDateTimeProvider(now),
            config);
    }

    private static SupplierProductPriceCreateReqDTO CreateReq(Guid vppId, Guid supplierId, Guid priceListId, decimal price, bool isDefault)
        => new()
        {
            VppItemId = vppId,
            SupplierId = supplierId,
            PriceListId = priceListId,
            Price = price,
            IsDefault = isDefault,
            Description = "Seed price"
        };

    private static async Task<Guid> SeedSupplierAsync(gtas_vpp_be.Service.Helpers.Context.VPPContext context, string name, DateTime now)
    {
        var id = Guid.NewGuid();
        context.Set<Supplier>().Add(new Supplier
        {
            Id = id,
            SupplierShortName = name,
            SupplierName = name,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now,
            IsDeleted = false
        });
        await context.SaveChangesAsync();
        return id;
    }

    private static SupplierProductMapping PriceRow(Guid vppId, Guid supplierId, Guid priceListId, decimal price, bool isDefault, DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            VppItemId = vppId,
            SupplierId = supplierId,
            PriceListId = priceListId,
            Price = price,
            IsDefault = isDefault,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now,
            IsDeleted = false
        };
}
