using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.PeriodSettlementTests;

public sealed class PeriodSettlementPreviewTests
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0);
    private static readonly DateTime AsOfUtc = new(2026, 7, 16, 5, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Preview_AggregatesRegularAndApprovedStandaloneSupplement_WithProvenanceHash()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        await SeedPublishedBookAsync(context, vppId, 100m);
        AddHeader(context, vppId, VPPStatus.Submitted, isAdditional: false, quantity: 2);
        AddHeader(context, vppId, VPPStatus.Approved, isAdditional: true, quantity: 3);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.PreviewAsync(new SettlementPreviewReqDTO
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = AsOfUtc
        });

        Assert.Equal(1, result.RequestedItemCount);
        Assert.Equal(2, result.RequestedLineCount);
        Assert.Equal(500m, result.Quotes.Single().Subtotal);
        Assert.NotNull(result.PrimaryQuote);
        Assert.Equal(result.PrimaryQuote!.SupplierId, result.PrimarySupplierId);
        Assert.False(string.IsNullOrWhiteSpace(result.InputHash));
        Assert.DoesNotContain(result.Blockers, x => x.StartsWith("NO_COMPLETE", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Blockers, x => x.StartsWith("SUPPLEMENT_WITHOUT_BASE", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Preview_MissingCoverage_ReturnsBlockerInsteadOfSelectingFirstBook()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var pricedVpp = Guid.NewGuid();
        var missingVpp = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, pricedVpp, missingVpp);
        await SeedPublishedBookAsync(context, pricedVpp, 100m);
        AddHeader(context, missingVpp, VPPStatus.Submitted, isAdditional: false, quantity: 1);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.PreviewAsync(new SettlementPreviewReqDTO
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = AsOfUtc
        });

        Assert.Null(result.PrimaryQuote);
        Assert.Contains(result.Blockers, x => x == "NO_COMPLETE_PRICE_COVERAGE");
        Assert.Contains(result.Quotes.Single().MissingVppIds, x => x == missingVpp);
    }

    [Fact]
    public async Task Preview_InvalidExceptionReason_IsBlockedAndPrimaryCanBePinned()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var supplierA = await SeedPublishedBookAsync(context, vppId, 100m, "A");
        var supplierB = await SeedPublishedBookAsync(context, vppId, 90m, "B");
        AddHeader(context, vppId, VPPStatus.Submitted, isAdditional: false, quantity: 1);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.PreviewAsync(new SettlementPreviewReqDTO
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = AsOfUtc,
            PrimarySupplierId = supplierA.SupplierId,
            Exceptions = [new() { VppId = vppId, SupplierId = supplierB.SupplierId, Reason = "bad" }]
        });

        Assert.Equal(supplierA.SupplierId, result.PrimarySupplierId);
        Assert.Contains(result.Blockers, x => x.StartsWith("INVALID_SUPPLIER_EXCEPTION", StringComparison.Ordinal));
    }

    private static PeriodSettlementService CreateService(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var priceListService = new PriceListService(unitOfWork.Object, new FakeDateTimeProvider(Now), new UserNameResolver());
        var workflow = new PriceBookWorkflowService(unitOfWork.Object, new FakeDateTimeProvider(Now), priceListService);
        return new PeriodSettlementService(unitOfWork.Object, new FakeDateTimeProvider(Now), workflow);
    }

    private static async Task<SeedBook> SeedPublishedBookAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Guid vppId,
        decimal netPrice,
        string code = "BOOK")
    {
        var supplierId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        context.Set<Supplier>().Add(new Supplier
        {
            Id = supplierId,
            SupplierShortName = code,
            SupplierName = code,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        });
        context.Set<PriceList>().Add(new PriceList
        {
            Id = bookId,
            PriceListCode = code,
            PriceListName = code,
            SupplierId = supplierId,
            Version = 1,
            EffectiveFromUtc = AsOfUtc.AddDays(-1),
            EffectiveToUtc = AsOfUtc.AddDays(1),
            Status = PriceListStatus.Published,
            CurrencyCode = "VND",
            VatPolicy = "item-rate",
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        });
        context.Set<SupplierProductMapping>().Add(new SupplierProductMapping
        {
            Id = Guid.NewGuid(),
            PriceListId = bookId,
            VppItemId = vppId,
            SupplierId = supplierId,
            Price = netPrice,
            NetPrice = netPrice,
            VatRate = 10m,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        });
        await context.SaveChangesAsync();
        return new SeedBook(bookId, supplierId);
    }

    private static VppRequest AddHeader(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Guid vppId,
        VPPStatus status,
        bool isAdditional,
        int quantity,
        Guid? baseRequestId = null)
    {
        var header = new VppRequest
        {
            Id = Guid.NewGuid(),
            Year = 2026,
            Month = 7,
            Status = (int)status,
            IsAdditionalOrder = isAdditional,
            BaseRequestId = baseRequestId,
            IsCurrentRevision = true,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now,
            RequestDetails = []
        };
        header.RequestDetails.Add(new VppRequestDetail
        {
            Id = Guid.NewGuid(),
            RequestId = header.Id,
            VppId = vppId,
            Qty = quantity,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        });
        context.Set<VppRequest>().Add(header);
        return header;
    }

    private sealed record SeedBook(Guid BookId, Guid SupplierId);
}
