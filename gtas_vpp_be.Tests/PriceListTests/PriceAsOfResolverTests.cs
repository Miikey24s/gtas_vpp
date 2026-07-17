using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.PriceListTests;

public sealed class PriceAsOfResolverTests
{
    private static readonly DateTime EffectiveFrom = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EffectiveTo = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Resolve_UsesHalfOpenWindowAndCalculatesNetVatGross()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context, netPrice: 100m, vatRate: 8m);
        var resolver = CreateResolver(context);

        var atStart = await resolver.ResolveAsync(Request(seed, EffectiveFrom, quantity: 2m));
        var atEnd = await resolver.ResolveAsync(Request(seed, EffectiveTo, quantity: 2m));

        Assert.True(atStart.IsResolved);
        Assert.Equal(200m, atStart.NetAmount);
        Assert.Equal(16m, atStart.VatAmount);
        Assert.Equal(216m, atStart.GrossAmount);
        Assert.Equal(PriceAsOfResolver.CurrentCalculationVersion, atStart.CalculationVersion);
        Assert.False(atEnd.IsResolved);
        Assert.Equal(PriceResolutionBlockerCode.ExpiredPrice, atEnd.BlockerCode);
    }

    [Fact]
    public async Task Resolve_OverlappingPublishedBooks_ReturnsTypedAmbiguity()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context);
        await AddBookAndItemAsync(context, seed.SupplierId, seed.VppId, "OVERLAP", version: 2);
        var resolver = CreateResolver(context);

        var result = await resolver.ResolveAsync(Request(seed, EffectiveFrom.AddDays(2)));

        Assert.False(result.IsResolved);
        Assert.Equal(PriceResolutionBlockerCode.AmbiguousPrice, result.BlockerCode);
    }

    [Fact]
    public async Task Resolve_ContractBookBeatsDefaultFallback()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context, isDefault: true, netPrice: 120m);
        var contractId = await AddBookAndItemAsync(
            context, seed.SupplierId, seed.VppId, "CONTRACT", version: 2, contractCode: "HD-2026", netPrice: 100m);
        var resolver = CreateResolver(context);

        var result = await resolver.ResolveAsync(Request(seed, EffectiveFrom.AddDays(2)));

        Assert.True(result.IsResolved);
        Assert.Equal(contractId, result.PriceListId);
        Assert.Equal(100m, result.NetUnitPrice);
    }

    [Fact]
    public async Task Resolve_LockedBookWinsAndRejectsSupplierMismatch()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context, isDefault: true, netPrice: 120m);
        var lockedId = await AddBookAndItemAsync(
            context, seed.SupplierId, seed.VppId, "LOCKED", version: 2, netPrice: 90m);
        var resolver = CreateResolver(context);

        var locked = Request(seed, EffectiveFrom.AddDays(2));
        locked.LockedPriceListId = lockedId;
        var resolved = await resolver.ResolveAsync(locked);

        locked.SupplierId = Guid.NewGuid();
        var mismatch = await resolver.ResolveAsync(locked);

        Assert.True(resolved.IsResolved);
        Assert.Equal(lockedId, resolved.PriceListId);
        Assert.Equal(90m, resolved.NetUnitPrice);
        Assert.Equal(PriceResolutionBlockerCode.SupplierMismatch, mismatch.BlockerCode);
    }

    [Fact]
    public async Task Resolve_LegacyBookWithoutSupplier_ReturnsBackfillBlocker()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context);
        var book = await context.Set<L07_PriceList>().SingleAsync(x => x.Id == seed.PriceListId);
        book.SupplierId = null;
        book.LegacyBackfillStatus = "MULTIPLE_SUPPLIERS";
        await context.SaveChangesAsync();
        var resolver = CreateResolver(context);

        var request = Request(seed, EffectiveFrom.AddDays(2));
        request.SupplierId = null;
        var result = await resolver.ResolveAsync(request);

        Assert.False(result.IsResolved);
        Assert.Equal(PriceResolutionBlockerCode.IncompleteLegacyBackfill, result.BlockerCode);
    }

    [Fact]
    public async Task Resolve_BelowMoq_ReturnsTypedBlocker()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context, minimumOrderQuantity: 10m);
        var resolver = CreateResolver(context);

        var result = await resolver.ResolveAsync(Request(seed, EffectiveFrom.AddDays(2), quantity: 9m));

        Assert.False(result.IsResolved);
        Assert.Equal(PriceResolutionBlockerCode.MinimumOrderQuantity, result.BlockerCode);
    }

    [Fact]
    public async Task Resolve_DraftOrMissingBook_NeverFallsBackToFirstRow()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context);
        var book = await context.Set<L07_PriceList>().SingleAsync(x => x.Id == seed.PriceListId);
        book.Status = L07_PriceListStatus.Draft;
        await context.SaveChangesAsync();
        var resolver = CreateResolver(context);

        var draft = await resolver.ResolveAsync(Request(seed, EffectiveFrom.AddDays(2)));
        var missing = await resolver.ResolveAsync(new PriceResolutionReqDTO
        {
            VppId = Guid.NewGuid(),
            SupplierId = seed.SupplierId,
            PriceAsOfUtc = EffectiveFrom.AddDays(2),
            Quantity = 1m
        });

        Assert.Equal(PriceResolutionBlockerCode.ExpiredPrice, draft.BlockerCode);
        Assert.Equal(PriceResolutionBlockerCode.MissingPrice, missing.BlockerCode);
    }

    private static PriceAsOfResolver CreateResolver(gtas_vpp_be.Service.Helpers.Context.VPPContext context)
        => new(ServiceTestHelpers.CreateUnitOfWorkMock(context).Object);

    private static PriceResolutionReqDTO Request(SeedIds seed, DateTime asOfUtc, decimal quantity = 1m)
        => new()
        {
            VppId = seed.VppId,
            SupplierId = seed.SupplierId,
            PriceAsOfUtc = asOfUtc,
            Quantity = quantity
        };

    private static async Task<SeedIds> SeedAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        bool isDefault = false,
        decimal netPrice = 100m,
        decimal vatRate = 10m,
        decimal minimumOrderQuantity = 0m)
    {
        var supplierId = Guid.NewGuid();
        var vppId = Guid.NewGuid();
        context.Set<L05_VPPSupplier>().Add(new L05_VPPSupplier
        {
            Id = supplierId,
            SupplierShortName = "NCC",
            SupplierName = "Nhà cung cấp",
            CreateUserId = 1,
            CreateDate = EffectiveFrom,
            UpdateUserId = 1,
            UpdateDate = EffectiveFrom
        });
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var priceListId = await AddBookAndItemAsync(
            context, supplierId, vppId, "BASE", version: 1, isDefault: isDefault,
            netPrice: netPrice, vatRate: vatRate, minimumOrderQuantity: minimumOrderQuantity);
        return new SeedIds(supplierId, vppId, priceListId);
    }

    private static async Task<Guid> AddBookAndItemAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        Guid supplierId,
        Guid vppId,
        string code,
        int version,
        bool isDefault = false,
        string? contractCode = null,
        decimal netPrice = 100m,
        decimal vatRate = 10m,
        decimal minimumOrderQuantity = 0m)
    {
        var priceListId = Guid.NewGuid();
        context.Set<L07_PriceList>().Add(new L07_PriceList
        {
            Id = priceListId,
            PriceListCode = code,
            PriceListName = code,
            SupplierId = supplierId,
            Version = version,
            EffectiveFromUtc = EffectiveFrom,
            EffectiveToUtc = EffectiveTo,
            Status = L07_PriceListStatus.Published,
            CurrencyCode = "VND",
            VatPolicy = "item-rate",
            ContractCode = contractCode,
            IsDefault = isDefault,
            CreateUserId = 1,
            CreateDate = EffectiveFrom,
            UpdateUserId = 1,
            UpdateDate = EffectiveFrom
        });
        context.Set<L06_VPPSupplierMapping>().Add(new L06_VPPSupplierMapping
        {
            Id = Guid.NewGuid(),
            L07_PriceListId = priceListId,
            L05_VPPSupplierId = supplierId,
            L04_VPPId = vppId,
            Price = netPrice,
            NetPrice = netPrice,
            VatRate = vatRate,
            MinimumOrderQuantity = minimumOrderQuantity,
            LeadTimeDays = 3,
            IsDefault = true,
            CreateUserId = 1,
            CreateDate = EffectiveFrom,
            UpdateUserId = 1,
            UpdateDate = EffectiveFrom
        });
        await context.SaveChangesAsync();
        return priceListId;
    }

    private sealed record SeedIds(Guid SupplierId, Guid VppId, Guid PriceListId);
}
