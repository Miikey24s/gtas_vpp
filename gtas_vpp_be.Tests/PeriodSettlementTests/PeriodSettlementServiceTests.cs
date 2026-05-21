using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.PeriodSettlementTests;

public class PeriodSettlementServiceTests
{
    [Fact]
    public async Task Settle_HappyPath_UpdatesAllDetailPrices_AndStampsHeader()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 10, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var listId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context, (vppId, 1234));
        var header = AddHeaderWithDetail(context, 2026, 4, VPPStatus.Submitted, vppId, now);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var result = await service.SettleAsync(new VPP_SettlePeriodReqDTO { Y = 2026, M = 4, PriceListId = listId }, 5615);

        Assert.True(result.IsSettled);
        Assert.Equal(1, result.OrderCount);
        var savedHeader = await context.Set<VPP01_RequestHeader>().SingleAsync(x => x.Id == header.Id);
        Assert.Equal(now, savedHeader.SettledAt);
        Assert.Equal(5615, savedHeader.SettledByUserId);
        Assert.Equal(listId, savedHeader.SettledByPriceListId);
        Assert.Equal(1234L, (await context.Set<VPP02_RequestDetail>().SingleAsync()).CurrentSinglePrice);
    }

    [Fact]
    public async Task Settle_BlockedByPendingAdditional_ThrowsConflict()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 10, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        await ServiceTestHelpers.SeedDefaultPriceListAsync(context, (vppId, 100));
        AddHeaderWithDetail(context, 2026, 4, VPPStatus.Pending, vppId, now, isAdditional: true);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.SettleAsync(new VPP_SettlePeriodReqDTO { Y = 2026, M = 4 }, 5615));

        Assert.Contains("still pending approval", ex.Message);
    }

    [Fact]
    public async Task Settle_MissingPriceForVPP_Throws()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 10, 0, 0);
        var pricedVppId = Guid.NewGuid();
        var missingVppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, pricedVppId, missingVppId);
        await ServiceTestHelpers.SeedDefaultPriceListAsync(context, (pricedVppId, 100));
        AddHeaderWithDetail(context, 2026, 4, VPPStatus.Submitted, missingVppId, now);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SettleAsync(new VPP_SettlePeriodReqDTO { Y = 2026, M = 4 }, 5615));

        Assert.Contains("is missing prices", ex.Message);
    }

    [Fact]
    public async Task Settle_NullPriceListId_FallsBackToDefault()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 10, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var listId = await ServiceTestHelpers.SeedDefaultPriceListAsync(context, (vppId, 555));
        var header = AddHeaderWithDetail(context, 2026, 4, VPPStatus.Submitted, vppId, now);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        await service.SettleAsync(new VPP_SettlePeriodReqDTO { Y = 2026, M = 4 }, 5615);

        var savedHeader = await context.Set<VPP01_RequestHeader>().SingleAsync(x => x.Id == header.Id);
        Assert.Equal(listId, savedHeader.SettledByPriceListId);
        Assert.Equal(555L, (await context.Set<VPP02_RequestDetail>().SingleAsync()).CurrentSinglePrice);
    }

    [Fact]
    public async Task Settle_Idempotent_SecondCallChangesPriceList()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 10, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        await ServiceTestHelpers.SeedDefaultPriceListAsync(context, (vppId, 100));
        var secondListId = await SeedPriceListAsync(context, "SECOND", "Second", false, now, (vppId, 900));
        var header = AddHeaderWithDetail(context, 2026, 4, VPPStatus.Submitted, vppId, now);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        await service.SettleAsync(new VPP_SettlePeriodReqDTO { Y = 2026, M = 4 }, 5615);
        await service.SettleAsync(new VPP_SettlePeriodReqDTO { Y = 2026, M = 4, PriceListId = secondListId }, 5616);

        var savedHeader = await context.Set<VPP01_RequestHeader>().SingleAsync(x => x.Id == header.Id);
        Assert.Equal(secondListId, savedHeader.SettledByPriceListId);
        Assert.Equal(5616, savedHeader.SettledByUserId);
        Assert.Equal(900L, (await context.Set<VPP02_RequestDetail>().SingleAsync()).CurrentSinglePrice);
    }

    [Fact]
    public async Task Settle_SkipsCancelledAndRejected()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 10, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        await ServiceTestHelpers.SeedDefaultPriceListAsync(context, (vppId, 321));
        var submitted = AddHeaderWithDetail(context, 2026, 4, VPPStatus.Submitted, vppId, now, currentPrice: 1);
        var cancelled = AddHeaderWithDetail(context, 2026, 4, VPPStatus.Cancelled, vppId, now, currentPrice: 2);
        var rejected = AddHeaderWithDetail(context, 2026, 4, VPPStatus.Rejected, vppId, now, currentPrice: 3);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        await service.SettleAsync(new VPP_SettlePeriodReqDTO { Y = 2026, M = 4 }, 5615);

        var headers = await context.Set<VPP01_RequestHeader>().ToDictionaryAsync(x => x.Id);
        Assert.NotNull(headers[submitted.Id].SettledAt);
        Assert.Null(headers[cancelled.Id].SettledAt);
        Assert.Null(headers[rejected.Id].SettledAt);
        Assert.Equal(2L, await DetailPriceAsync(context, cancelled.Id));
        Assert.Equal(3L, await DetailPriceAsync(context, rejected.Id));
    }

    [Fact]
    public async Task Settle_LogsToVPP03_PerHeader()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 10, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        await ServiceTestHelpers.SeedDefaultPriceListAsync(context, (vppId, 100));
        AddHeaderWithDetail(context, 2026, 4, VPPStatus.Submitted, vppId, now);
        AddHeaderWithDetail(context, 2026, 4, VPPStatus.Approved, vppId, now);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        await service.SettleAsync(new VPP_SettlePeriodReqDTO { Y = 2026, M = 4 }, 5615);

        var logs = await context.Set<VPP03_Log>().ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.All(logs, log => Assert.Equal("PERIOD_SETTLED", log.LogTitle));
    }

    [Fact]
    public async Task GetStatus_ReturnsPendingCount()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 10, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        AddHeaderWithDetail(context, 2026, 4, VPPStatus.Submitted, vppId, now);
        AddHeaderWithDetail(context, 2026, 4, VPPStatus.Pending, vppId, now, isAdditional: true);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var result = await service.GetStatusAsync(2026, 4);

        Assert.False(result.IsSettled);
        Assert.Equal(1, result.OrderCount);
        Assert.Equal(1, result.PendingAdditionalCount);
    }

    private static PeriodSettlementService CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context, DateTime now)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        return new PeriodSettlementService(unitOfWork.Object, new FakeDateTimeProvider(now));
    }

    private static VPP01_RequestHeader AddHeaderWithDetail(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        int y,
        int m,
        VPPStatus status,
        Guid vppId,
        DateTime now,
        bool isAdditional = false,
        long currentPrice = 0)
    {
        var header = new VPP01_RequestHeader
        {
            Id = Guid.NewGuid(),
            Y = y,
            M = m,
            VPPCode = $"VPP-{Guid.NewGuid():N}"[..24],
            Status = (int)status,
            IsAdditionalOrder = isAdditional,
            DepartmentCode = "IT",
            MemberCompanyCode = "77500",
            CreateUserId = 1,
            CreateDate = now.AddDays(-1),
            UpdateUserId = 1,
            UpdateDate = now.AddDays(-1),
            SubmittedDate = now.AddDays(-1),
            VPP02_RequestDetails = new List<VPP02_RequestDetail>()
        };
        var detail = new VPP02_RequestDetail
        {
            Id = Guid.NewGuid(),
            VPP01_RequestHeaderId = header.Id,
            VPPId = vppId,
            Qty = 1,
            CurrentSinglePrice = currentPrice,
            CreateUserId = 1,
            CreateDate = now.AddDays(-1),
            UpdateUserId = 1,
            UpdateDate = now.AddDays(-1),
            IsDeleted = false
        };
        header.VPP02_RequestDetails.Add(detail);
        context.Set<VPP01_RequestHeader>().Add(header);
        return header;
    }

    private static async Task<Guid> SeedPriceListAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        string code,
        string name,
        bool isDefault,
        DateTime now,
        params (Guid VPPId, decimal Price)[] items)
    {
        var listId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        context.Set<L07_PriceList>().Add(new L07_PriceList
        {
            Id = listId,
            PriceListCode = code,
            PriceListName = name,
            IsDefault = isDefault,
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now,
            IsDeleted = false
        });
        context.Set<L05_VPPSupplier>().Add(new L05_VPPSupplier
        {
            Id = supplierId,
            SupplierShortName = code,
            SupplierName = name,
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now,
            IsDeleted = false
        });
        context.Set<L06_VPPSupplierMapping>().AddRange(items.Select(item => new L06_VPPSupplierMapping
        {
            Id = Guid.NewGuid(),
            L07_PriceListId = listId,
            L04_VPPId = item.VPPId,
            L05_VPPSupplierId = supplierId,
            Price = item.Price,
            IsDefault = true,
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now,
            IsDeleted = false
        }));
        await context.SaveChangesAsync();
        return listId;
    }

    private static async Task<long> DetailPriceAsync(gtas_vpp_be.Service.Helpers.Context.VPPContext context, Guid headerId)
        => await context.Set<VPP02_RequestDetail>()
            .Where(x => x.VPP01_RequestHeaderId == headerId)
            .Select(x => x.CurrentSinglePrice)
            .SingleAsync();
}
