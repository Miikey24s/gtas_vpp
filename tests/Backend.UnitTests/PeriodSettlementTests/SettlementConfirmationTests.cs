using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.PeriodSettlementTests;

public sealed class SettlementConfirmationTests
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0);
    private static readonly DateTime AsOfUtc = new(2026, 7, 16, 5, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Confirm_CreatesImmutableReconciledSnapshots_TransitionsPeriod_AndReplaysIdempotently()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context, netPrice: 101.25m, vatRate: 10m, discountRate: 5m);
        var service = CreateService(context);
        var preview = await PreviewAsync(service, seed);
        var request = ConfirmRequest(seed, preview.InputHash, "confirm-202607-0001");

        var first = await service.ConfirmAsync(request, 5615);
        var replay = await service.ConfirmAsync(request, 5615);

        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(1, first.RevisionNumber);
        Assert.Equal(2, first.ItemCount);
        Assert.Equal(3, first.AllocationCount);
        Assert.Equal(first.GrandTotal,
            await context.Set<SettlementAllocation>().SumAsync(x => x.GrossAmount));
        Assert.Equal(first.Subtotal,
            await context.Set<SettlementItem>().SumAsync(x => x.NetAmount));
        Assert.Equal(first.VatAmount,
            await context.Set<SettlementItem>().SumAsync(x => x.VatAmount));
        Assert.Equal(VppPeriodState.Settled,
            (await context.Set<VppPeriod>().SingleAsync()).State);
        Assert.Single(await context.Set<Settlement>().ToListAsync());
        Assert.Equal(5, await context.Set<SettlementCharge>().CountAsync());
    }

    [Fact]
    public async Task Confirm_RejectsStalePreviewHash()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context, netPrice: 100m, vatRate: 10m);
        var service = CreateService(context);
        var preview = await PreviewAsync(service, seed);
        var request = ConfirmRequest(seed, preview.InputHash, "confirm-202607-0002");
        (await context.Set<VppRequestDetail>().FirstAsync()).Qty++;
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ConfirmAsync(request, 5615));

        Assert.Contains("changed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await context.Set<Settlement>().ToListAsync());
    }

    [Fact]
    public async Task Confirm_AcceptsQuoteAutoSelectedByPreview()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context, netPrice: 100m, vatRate: 10m);
        var service = CreateService(context);
        var preview = await service.PreviewAsync(new SettlementPreviewReqDTO
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = AsOfUtc
        });

        Assert.Equal(seed.SupplierId, preview.PrimarySupplierId);
        Assert.Equal(seed.BookId, preview.PrimaryPriceListId);

        var result = await service.ConfirmAsync(
            ConfirmRequest(seed, preview.InputHash, "confirm-202607-auto-selected"),
            5615);

        Assert.Equal(seed.SupplierId, result.PrimarySupplierId);
        Assert.Equal(seed.BookId, result.PriceListId);
    }

    [Fact]
    public async Task Correct_RequiresFourEyes_AndCreatesNewRevisionWithoutOverwritingPriorSnapshot()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context, netPrice: 100m, vatRate: 10m);
        var alternate = await SeedBookAsync(context, seed.VppIds, 150m, 8m, "ALT");
        var service = CreateService(context);
        var preview = await PreviewAsync(service, seed);
        var first = await service.ConfirmAsync(
            ConfirmRequest(seed, preview.InputHash, "confirm-202607-0003"), 5615);

        var correctionPreview = await service.PreviewAsync(new SettlementPreviewReqDTO
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = AsOfUtc,
            PrimarySupplierId = alternate.SupplierId,
            PriceListId = alternate.BookId
        });
        var correction = new SettlementCorrectionReqDTO
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = AsOfUtc,
            PrimarySupplierId = alternate.SupplierId,
            PriceListId = alternate.BookId,
            InputHash = correctionPreview.InputHash,
            IdempotencyKey = "correct-202607-0001",
            Reason = "Supplier contract correction"
        };

        await Assert.ThrowsAsync<ConflictException>(() => service.CorrectAsync(first.Id, correction, 5615));
        var second = await service.CorrectAsync(first.Id, correction, 5616);

        Assert.Equal(2, second.RevisionNumber);
        Assert.True(second.IsCorrection);
        Assert.Equal(first.Id, second.SupersedesSettlementId);
        var revisions = await context.Set<Settlement>()
            .OrderBy(x => x.RevisionNumber)
            .ToListAsync();
        Assert.Equal(2, revisions.Count);
        Assert.False(revisions[0].IsCurrentRevision);
        Assert.True(revisions[1].IsCurrentRevision);
        Assert.Equal(first.GrandTotal, revisions[0].GrandTotal);
        Assert.NotEqual(revisions[0].PriceListId, revisions[1].PriceListId);
        Assert.Equal(VppPeriodState.Settled, (await context.Set<VppPeriod>().SingleAsync()).State);

        var history = await service.ListRevisionsAsync(2026, 7);
        Assert.Collection(
            history,
            latest =>
            {
                Assert.Equal(2, latest.RevisionNumber);
                Assert.True(latest.IsCurrentRevision);
                Assert.Equal(first.Id, latest.SupersedesSettlementId);
            },
            original =>
            {
                Assert.Equal(1, original.RevisionNumber);
                Assert.False(original.IsCurrentRevision);
            });
    }

    private static async Task<SeedData> SeedAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        decimal netPrice,
        decimal vatRate,
        decimal discountRate = 0m)
    {
        var vppIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppIds);
        var book = await SeedBookAsync(context, vppIds, netPrice, vatRate, "PRIMARY", discountRate);
        var period = new VppPeriod
        {
            Id = Guid.NewGuid(),
            MemberCompanyCode = "77500",
            Year = 2026,
            Month = 7,
            StartAtUtc = AsOfUtc.AddDays(-40),
            SubmissionDeadlineUtc = AsOfUtc.AddDays(-10),
            SupplementApprovalDeadlineUtc = AsOfUtc.AddDays(-5),
            State = VppPeriodState.Pricing,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        };
        context.Set<VppPeriod>().Add(period);
        AddHeader(context, period.Id, vppIds[0], 2, 101, "IT");
        AddHeader(context, period.Id, vppIds[0], 1, 102, "HR");
        AddHeader(context, period.Id, vppIds[1], 3, 103, "OPS");
        await context.SaveChangesAsync();
        return new SeedData(period.Id, book.BookId, book.SupplierId, vppIds);
    }

    private static async Task<SeedBook> SeedBookAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        IReadOnlyList<Guid> vppIds,
        decimal netPrice,
        decimal vatRate,
        string code,
        decimal discountRate = 0m)
    {
        var supplierId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        context.Set<Supplier>().Add(new Supplier
        {
            Id = supplierId,
            SupplierShortName = code,
            SupplierName = $"Supplier {code}",
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        });
        context.Set<PriceList>().Add(new PriceList
        {
            Id = bookId,
            PriceListCode = code,
            PriceListName = $"Book {code}",
            SupplierId = supplierId,
            Version = 1,
            EffectiveFromUtc = AsOfUtc.AddDays(-1),
            EffectiveToUtc = AsOfUtc.AddDays(1),
            Status = PriceListStatus.Published,
            CurrencyCode = "VND",
            VatPolicy = "item-rate",
            DiscountRate = discountRate,
            RebateAmount = 3m,
            FeeAmount = 7m,
            ShippingAmount = 11m,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        });
        context.Set<SupplierProductMapping>().AddRange(vppIds.Select((vppId, index) =>
            new SupplierProductMapping
            {
                Id = Guid.NewGuid(),
                PriceListId = bookId,
                VppItemId = vppId,
                SupplierId = supplierId,
                Price = netPrice + index,
                NetPrice = netPrice + index,
                VatRate = vatRate,
                LeadTimeDays = index + 1,
                SupplierSku = $"{code}-{index + 1}",
                CreatedByUserId = 1,
                CreatedAtUtc = Now,
                UpdatedByUserId = 1,
                UpdatedAtUtc = Now
            }));
        await context.SaveChangesAsync();
        return new SeedBook(bookId, supplierId);
    }

    private static void AddHeader(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Guid periodId,
        Guid vppId,
        int quantity,
        int userId,
        string department)
    {
        var header = new VppRequest
        {
            Id = Guid.NewGuid(),
            PeriodId = periodId,
            Year = 2026,
            Month = 7,
            Status = (int)VPPStatus.Submitted,
            IsCurrentRevision = true,
            RequestSeriesId = Guid.NewGuid(),
            DepartmentCode = department,
            MemberCompanyCode = "77500",
            CreatedByUserId = userId,
            CreatedAtUtc = Now,
            UpdatedByUserId = userId,
            UpdatedAtUtc = Now,
            RequestDetails = []
        };
        header.RequestDetails.Add(new VppRequestDetail
        {
            Id = Guid.NewGuid(),
            RequestId = header.Id,
            VppId = vppId,
            Qty = quantity,
            CreatedByUserId = userId,
            CreatedAtUtc = Now,
            UpdatedByUserId = userId,
            UpdatedAtUtc = Now
        });
        context.Set<VppRequest>().Add(header);
    }

    private static PeriodSettlementService CreateService(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var clock = new FakeDateTimeProvider(Now);
        var listService = new PriceListService(unitOfWork.Object, clock, new UserNameResolver());
        var workflow = new PriceBookWorkflowService(unitOfWork.Object, clock, listService);
        var resolver = new PriceAsOfResolver(unitOfWork.Object);
        return new PeriodSettlementService(unitOfWork.Object, clock, workflow, resolver);
    }

    private static Task<gtas_vpp_shared.DTOs.Res.VPP.SettlementPreviewResDTO> PreviewAsync(
        PeriodSettlementService service,
        SeedData seed)
        => service.PreviewAsync(new SettlementPreviewReqDTO
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = AsOfUtc,
            PrimarySupplierId = seed.SupplierId,
            PriceListId = seed.BookId
        });

    private static SettlementConfirmReqDTO ConfirmRequest(
        SeedData seed,
        string inputHash,
        string idempotencyKey)
        => new()
        {
            Year = 2026,
            Month = 7,
            PriceAsOfUtc = AsOfUtc,
            PrimarySupplierId = seed.SupplierId,
            PriceListId = seed.BookId,
            InputHash = inputHash,
            IdempotencyKey = idempotencyKey
        };

    private sealed record SeedBook(Guid BookId, Guid SupplierId);
    private sealed record SeedData(Guid PeriodId, Guid BookId, Guid SupplierId, Guid[] VppIds);
}
