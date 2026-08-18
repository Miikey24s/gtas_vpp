using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.Library;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.PriceListTests;

public sealed class PriceBookWorkflowTests
{
    private static readonly DateTime FromUtc = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToUtc = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Publish_ValidDraft_RecordsImmutableStatusAudit()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedBookAsync(context, supplierId: Guid.NewGuid(), status: PriceListStatus.Draft);
        var workflow = CreateWorkflow(context);

        var result = await workflow.PublishAsync(seed.BookId, new PriceBookStatusReqDTO
        {
            RowVersion = [1],
            Reason = "Approved quote"
        }, 5615);

        Assert.Equal("Published", result.Status);
        Assert.Equal(5615, result.PublishedByUserId);
        Assert.Equal("Approved quote", result.StatusReason);
        Assert.Equal(PriceListStatus.Published,
            (await context.Set<PriceList>().SingleAsync(x => x.Id == seed.BookId)).Status);
    }

    [Fact]
    public async Task Publish_StaleRowVersion_IsRejected()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedBookAsync(context, supplierId: Guid.NewGuid(), status: PriceListStatus.Draft);
        var workflow = CreateWorkflow(context);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => workflow.PublishAsync(seed.BookId, new PriceBookStatusReqDTO
        {
            RowVersion = [9],
            Reason = "Approved quote"
        }, 5615));

        Assert.Contains("changed", ex.Message);
    }

    [Fact]
    public async Task Publish_OverlappingPublishedItem_IsBlocked()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var supplierId = Guid.NewGuid();
        var existing = await SeedBookAsync(context, supplierId, PriceListStatus.Published, code: "OLD");
        var draft = await SeedBookAsync(context, supplierId, PriceListStatus.Draft, code: "NEW", vppId: existing.VppId);
        var workflow = CreateWorkflow(context);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => workflow.PublishAsync(draft.BookId, new PriceBookStatusReqDTO
        {
            RowVersion = [1],
            Reason = "Replacement quote"
        }, 5615));

        Assert.Contains("overlap", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Expire_PublishedBook_SetsHalfOpenEndAndAudit()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedBookAsync(context, Guid.NewGuid(), PriceListStatus.Published);
        var workflow = CreateWorkflow(context);

        var result = await workflow.ExpireAsync(seed.BookId, new PriceBookStatusReqDTO
        {
            RowVersion = [1],
            Reason = "Contract ended",
            EffectiveToUtc = ToUtc
        }, 5615);

        Assert.Equal("Expired", result.Status);
        Assert.Equal(ToUtc, result.EffectiveToUtc);
        Assert.Equal(5615, result.ExpiredByUserId);
    }

    [Fact]
    public async Task Compare_RanksEligibleCoverageAndReusesCalculationVersion()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var supplierA = Guid.NewGuid();
        var supplierB = Guid.NewGuid();
        var complete = await SeedBookAsync(context, supplierA, PriceListStatus.Published, code: "A", netPrice: 100m);
        var bookWithLegacyTerms = await context.Set<PriceList>().SingleAsync(item => item.Id == complete.BookId);
        bookWithLegacyTerms.DiscountRate = 5m;
        bookWithLegacyTerms.RebateAmount = 10m;
        bookWithLegacyTerms.FeeAmount = 20m;
        bookWithLegacyTerms.ShippingAmount = 30m;
        await SeedBookAsync(context, supplierB, PriceListStatus.Published, code: "B", netPrice: 80m, vppId: complete.VppId);
        var secondVpp = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, secondVpp);
        context.Set<SupplierProductMapping>().Add(new SupplierProductMapping
        {
            Id = Guid.NewGuid(),
            PriceListId = complete.BookId,
            VppItemId = secondVpp,
            SupplierId = supplierA,
            Price = 50m,
            NetPrice = 50m,
            VatRate = 8m,
            CreatedByUserId = 1,
            CreatedAtUtc = FromUtc,
            UpdatedByUserId = 1,
            UpdatedAtUtc = FromUtc
        });
        await context.SaveChangesAsync();
        var workflow = CreateWorkflow(context);

        var result = await workflow.CompareAsync(new PriceBookComparisonReqDTO
        {
            PriceAsOfUtc = FromUtc.AddDays(2),
            Items =
            [
                new() { VppId = complete.VppId, Quantity = 1m },
                new() { VppId = secondVpp, Quantity = 1m }
            ]
        });

        Assert.Equal(2, result.Quotes.Count);
        Assert.True(result.Quotes[0].IsEligible);
        Assert.Equal(150m, result.Quotes[0].Subtotal);
        Assert.Equal(0m, result.Quotes[0].DiscountAmount);
        Assert.Equal(0m, result.Quotes[0].RebateAmount);
        Assert.Equal(0m, result.Quotes[0].FeeAmount);
        Assert.Equal(0m, result.Quotes[0].ShippingAmount);
        Assert.Equal(PriceCalculationEngine.CurrentVersion, result.CalculationVersion);
        Assert.False(result.Quotes[1].IsEligible);
        Assert.NotEmpty(result.Quotes[1].MissingVppIds);
    }

    private static PriceBookWorkflowService CreateWorkflow(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var priceListService = new PriceListService(
            unitOfWork.Object,
            new FakeDateTimeProvider(new DateTime(2026, 7, 16, 12, 0, 0)),
            new UserNameResolver());
        return new PriceBookWorkflowService(
            unitOfWork.Object,
            new FakeDateTimeProvider(new DateTime(2026, 7, 16, 12, 0, 0)),
            priceListService);
    }

    private static async Task<SeedIds> SeedBookAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Guid supplierId,
        PriceListStatus status,
        string code = "BOOK",
        Guid? vppId = null,
        decimal netPrice = 100m)
    {
        vppId ??= Guid.NewGuid();
        if (!await context.Set<VppItem>().AnyAsync(x => x.Id == vppId.Value))
        {
            await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId.Value);
        }
        if (!await context.Set<Supplier>().AnyAsync(x => x.Id == supplierId))
        {
            context.Set<Supplier>().Add(new Supplier
            {
                Id = supplierId,
                SupplierName = $"Supplier {supplierId:N}"[..20],
                SupplierShortName = code,
                CreatedByUserId = 1,
                CreatedAtUtc = FromUtc,
                UpdatedByUserId = 1,
                UpdatedAtUtc = FromUtc
            });
        }
        var bookId = Guid.NewGuid();
        context.Set<PriceList>().Add(new PriceList
        {
            Id = bookId,
            PriceListCode = code,
            PriceListName = code,
            SupplierId = supplierId,
            Version = 1,
            EffectiveFromUtc = FromUtc,
            EffectiveToUtc = ToUtc,
            Status = status,
            CurrencyCode = "VND",
            VatPolicy = "item-rate",
            CreatedByUserId = 1,
            CreatedAtUtc = FromUtc,
            UpdatedByUserId = 1,
            UpdatedAtUtc = FromUtc,
            RowVersion = [1]
        });
        context.Set<SupplierProductMapping>().Add(new SupplierProductMapping
        {
            Id = Guid.NewGuid(),
            PriceListId = bookId,
            VppItemId = vppId.Value,
            SupplierId = supplierId,
            Price = netPrice,
            NetPrice = netPrice,
            VatRate = 10m,
            CreatedByUserId = 1,
            CreatedAtUtc = FromUtc,
            UpdatedByUserId = 1,
            UpdatedAtUtc = FromUtc
        });
        await context.SaveChangesAsync();
        return new SeedIds(bookId, vppId.Value);
    }

    private sealed record SeedIds(Guid BookId, Guid VppId);
}
