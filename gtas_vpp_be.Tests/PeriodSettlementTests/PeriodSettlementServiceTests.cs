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

        var result = await service.SettleAsync(new PeriodSettlementReqDTO { Year = 2026, Month = 4, PriceListId = listId }, 5615);

        Assert.True(result.IsSettled);
        Assert.Equal(1, result.OrderCount);
        var savedHeader = await context.Set<VppRequest>().SingleAsync(x => x.Id == header.Id);
        Assert.Equal(now, savedHeader.SettledAt);
        Assert.Equal(5615, savedHeader.SettledByUserId);
        Assert.Equal(listId, savedHeader.SettledByPriceListId);
        Assert.Equal(1234L, (await context.Set<VppRequestDetail>().SingleAsync()).CurrentSinglePrice);
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
            service.SettleAsync(new PeriodSettlementReqDTO { Year = 2026, Month = 4 }, 5615));

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
            service.SettleAsync(new PeriodSettlementReqDTO { Year = 2026, Month = 4 }, 5615));

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

        await service.SettleAsync(new PeriodSettlementReqDTO { Year = 2026, Month = 4 }, 5615);

        var savedHeader = await context.Set<VppRequest>().SingleAsync(x => x.Id == header.Id);
        Assert.Equal(listId, savedHeader.SettledByPriceListId);
        Assert.Equal(555L, (await context.Set<VppRequestDetail>().SingleAsync()).CurrentSinglePrice);
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

        await service.SettleAsync(new PeriodSettlementReqDTO { Year = 2026, Month = 4 }, 5615);
        await service.SettleAsync(new PeriodSettlementReqDTO { Year = 2026, Month = 4, PriceListId = secondListId }, 5616);

        var savedHeader = await context.Set<VppRequest>().SingleAsync(x => x.Id == header.Id);
        Assert.Equal(secondListId, savedHeader.SettledByPriceListId);
        Assert.Equal(5616, savedHeader.SettledByUserId);
        Assert.Equal(900L, (await context.Set<VppRequestDetail>().SingleAsync()).CurrentSinglePrice);
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

        await service.SettleAsync(new PeriodSettlementReqDTO { Year = 2026, Month = 4 }, 5615);

        var headers = await context.Set<VppRequest>().ToDictionaryAsync(x => x.Id);
        Assert.NotNull(headers[submitted.Id].SettledAt);
        Assert.Null(headers[cancelled.Id].SettledAt);
        Assert.Null(headers[rejected.Id].SettledAt);
        Assert.Equal(2L, await DetailPriceAsync(context, cancelled.Id));
        Assert.Equal(3L, await DetailPriceAsync(context, rejected.Id));
    }

    [Fact]
    public async Task Settle_LogsToRequestLogPerHeader()
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

        await service.SettleAsync(new PeriodSettlementReqDTO { Year = 2026, Month = 4 }, 5615);

        var logs = await context.Set<RequestLog>().ToListAsync();
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

    private static VppRequest AddHeaderWithDetail(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        int y,
        int m,
        VPPStatus status,
        Guid vppId,
        DateTime now,
        bool isAdditional = false,
        long currentPrice = 0)
    {
        var header = new VppRequest
        {
            Id = Guid.NewGuid(),
            Year = y,
            Month = m,
            VppCode = $"VPP-{Guid.NewGuid():N}"[..24],
            Status = (int)status,
            IsAdditionalOrder = isAdditional,
            DepartmentCode = "IT",
            MemberCompanyCode = "77500",
            CreatedByUserId = 1,
            CreatedAtUtc = now.AddDays(-1),
            UpdatedByUserId = 1,
            UpdatedAtUtc = now.AddDays(-1),
            SubmittedDate = now.AddDays(-1),
            RequestDetails = new List<VppRequestDetail>()
        };
        var detail = new VppRequestDetail
        {
            Id = Guid.NewGuid(),
            RequestId = header.Id,
            VppId = vppId,
            Qty = 1,
            CurrentSinglePrice = currentPrice,
            CreatedByUserId = 1,
            CreatedAtUtc = now.AddDays(-1),
            UpdatedByUserId = 1,
            UpdatedAtUtc = now.AddDays(-1),
            IsDeleted = false
        };
        header.RequestDetails.Add(detail);
        context.Set<VppRequest>().Add(header);
        return header;
    }

    private static async Task<Guid> SeedPriceListAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        string code,
        string name,
        bool isDefault,
        DateTime now,
        params (Guid VppId, decimal Price)[] items)
    {
        var listId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        context.Set<PriceList>().Add(new PriceList
        {
            Id = listId,
            PriceListCode = code,
            PriceListName = name,
            IsDefault = isDefault,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now,
            IsDeleted = false
        });
        context.Set<Supplier>().Add(new Supplier
        {
            Id = supplierId,
            SupplierShortName = code,
            SupplierName = name,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now,
            IsDeleted = false
        });
        context.Set<SupplierProductMapping>().AddRange(items.Select(item => new SupplierProductMapping
        {
            Id = Guid.NewGuid(),
            PriceListId = listId,
            VppItemId = item.VppId,
            SupplierId = supplierId,
            Price = item.Price,
            IsDefault = true,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now,
            IsDeleted = false
        }));
        await context.SaveChangesAsync();
        return listId;
    }

    private static async Task<long> DetailPriceAsync(gtas_vpp_be.Service.Helpers.Context.VPPContext context, Guid headerId)
        => await context.Set<VppRequestDetail>()
            .Where(x => x.RequestId == headerId)
            .Select(x => x.CurrentSinglePrice)
            .SingleAsync();
}
