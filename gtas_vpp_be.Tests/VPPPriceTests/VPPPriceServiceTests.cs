using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests.VPPPriceTests;

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
        var row = Assert.Single(context.Set<L06_VPPSupplierMapping>());
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

        var rows = await context.Set<L06_VPPSupplierMapping>().OrderBy(x => x.Price).ToListAsync();
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

        await service.UpdateAsync(new L06_PriceUpdateReqDTO
        {
            Id = second.Id,
            L04_VPPId = vppId,
            L05_VPPSupplierId = supplier2Id,
            L07_PriceListId = priceListId,
            Price = 2500,
            IsDefault = true,
            Description = "Updated"
        }, 5615);

        var rows = await context.Set<L06_VPPSupplierMapping>().ToListAsync();
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

        var rows = await context.Set<L06_VPPSupplierMapping>().ToListAsync();
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

        var deletedRow = await context.Set<L06_VPPSupplierMapping>().SingleAsync(x => x.Id == deleted.Id);
        Assert.True(deletedRow.IsDeleted);
        var listed = await service.ListByVPPAsync(vppId);
        var onlyVisible = Assert.Single(listed);
        Assert.Equal(kept.Id, onlyVisible.Id);
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
        context.Set<L06_VPPSupplierMapping>().AddRange(
            PriceRow(vppId, supplier1Id, priceListId, 100000, isDefault: false, now),
            PriceRow(vppId, supplier2Id, priceListId, 250000, isDefault: true, now));
        await context.SaveChangesAsync();
        var service = CreateRequestService(context, now);
        var request = new VPP01_CreateReqDTO
        {
            Y = 2026,
            M = 3,
            Description = "Test order",
            IsAdditionalOrder = false,
            Items = new List<VPP02_ItemReqDTO>
            {
                new() { VPPId = vppId, Qty = 1, Description = "Item" }
            }
        };

        await service.CreateOrderAsync(request, 5615, "IT", "77500");

        var detail = Assert.Single(context.Set<VPP02_RequestDetail>());
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
        var factory = ServiceTestHelpers.CreateUnitOfWorkFactoryMock(unitOfWork.Object);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();

        return new VPPRequestService(
            factory.Object,
            ServiceTestHelpers.CreateHttpContextAccessor(),
            unitOfWork.Object,
            new FakeDateTimeProvider(now),
            config,
            new EnvironmentResolver(),
            new UserNameResolver(),
            NullLogger<BaseServices>.Instance,
            Options.Create(new JiraSettings()));
    }

    private static L06_PriceCreateReqDTO CreateReq(Guid vppId, Guid supplierId, Guid priceListId, decimal price, bool isDefault)
        => new()
        {
            L04_VPPId = vppId,
            L05_VPPSupplierId = supplierId,
            L07_PriceListId = priceListId,
            Price = price,
            IsDefault = isDefault,
            Description = "Seed price"
        };

    private static async Task<Guid> SeedSupplierAsync(gtas_vpp_be.Service.Helpers.Context.VPPContext context, string name, DateTime now)
    {
        var id = Guid.NewGuid();
        context.Set<L05_VPPSupplier>().Add(new L05_VPPSupplier
        {
            Id = id,
            SupplierShortName = name,
            SupplierName = name,
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now,
            IsDeleted = false
        });
        await context.SaveChangesAsync();
        return id;
    }

    private static L06_VPPSupplierMapping PriceRow(Guid vppId, Guid supplierId, Guid priceListId, decimal price, bool isDefault, DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            L04_VPPId = vppId,
            L05_VPPSupplierId = supplierId,
            L07_PriceListId = priceListId,
            Price = price,
            IsDefault = isDefault,
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now,
            IsDeleted = false
        };
}
