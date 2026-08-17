using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Res.Library;
using Xunit;

namespace gtas_vpp_be.Tests.PeriodSettlementTests;

public sealed class SettlementSupplierOptimizerTests
{
    [Fact]
    public void Build_RecommendsTwoSuppliers_WhenSavingReachesThreshold()
    {
        var firstItem = Guid.NewGuid();
        var secondItem = Guid.NewGuid();
        var supplierA = Quote("A", rank: 2, (firstItem, 100m), (secondItem, 300m));
        var supplierB = Quote("B", rank: 1, (firstItem, 150m), (secondItem, 100m));

        var result = SettlementSupplierOptimizer.Build([supplierA, supplierB], requestedItemCount: 2);

        Assert.NotNull(result);
        Assert.True(result!.IsRecommended);
        Assert.Equal("COST_SAVING", result.ReasonCode);
        Assert.Equal(200m, result.RecommendedGrandTotal);
        Assert.Equal(50m, result.SavingsAmount);
        Assert.Equal(20m, result.SavingsPercent);
        Assert.Equal(supplierB.SupplierId, result.BaselineSupplierId);
        Assert.Equal(supplierB.PriceListId, result.BaselinePriceListId);
        Assert.Equal(supplierB.SupplierId, result.PrimarySupplierId);
        var exception = Assert.Single(result.SuggestedExceptions);
        Assert.Equal(firstItem, exception.VppId);
        Assert.Equal(supplierA.SupplierId, exception.SupplierId);
        Assert.Equal(supplierA.PriceListId, exception.PriceListId);
    }

    [Fact]
    public void Build_DoesNotRecommend_WhenSavingIsBelowThreshold()
    {
        var firstItem = Guid.NewGuid();
        var secondItem = Guid.NewGuid();
        var supplierA = Quote("A", rank: 1, (firstItem, 100m), (secondItem, 100m));
        var supplierB = Quote("B", rank: 2, (firstItem, 99m), (secondItem, 101m));

        var result = SettlementSupplierOptimizer.Build([supplierA, supplierB], requestedItemCount: 2);

        Assert.NotNull(result);
        Assert.False(result!.IsRecommended);
        Assert.Equal("BELOW_THRESHOLD", result.ReasonCode);
        Assert.Equal(0.5m, result.SavingsPercent);
    }

    [Fact]
    public void Build_RecommendsPair_WhenNoSingleSupplierCoversAllItems()
    {
        var firstItem = Guid.NewGuid();
        var secondItem = Guid.NewGuid();
        var supplierA = Quote("A", rank: 1, isEligible: false, missingCount: 1, (firstItem, 100m));
        var supplierB = Quote("B", rank: 2, isEligible: false, missingCount: 1, (secondItem, 120m));

        var result = SettlementSupplierOptimizer.Build([supplierA, supplierB], requestedItemCount: 2);

        Assert.NotNull(result);
        Assert.True(result!.IsRecommended);
        Assert.Equal("FULL_COVERAGE", result.ReasonCode);
        Assert.Null(result.BaselineGrandTotal);
        Assert.Null(result.BaselineSupplierId);
        Assert.Null(result.BaselinePriceListId);
        Assert.Equal(220m, result.RecommendedGrandTotal);
        Assert.Equal(2, result.Suppliers.Count);
        Assert.Single(result.SuggestedExceptions);
    }

    [Fact]
    public void Build_SkipsQuotesWithUnmodeledCommercialTerms()
    {
        var firstItem = Guid.NewGuid();
        var supplierA = Quote("A", rank: 1, (firstItem, 100m));
        var supplierB = Quote("B", rank: 2, (firstItem, 80m));
        supplierB.ShippingAmount = 10m;

        var result = SettlementSupplierOptimizer.Build([supplierA, supplierB], requestedItemCount: 1);

        Assert.Null(result);
    }

    private static PriceBookQuoteResDTO Quote(
        string name,
        int rank,
        params (Guid VppId, decimal GrossAmount)[] lines)
        => Quote(name, rank, isEligible: true, missingCount: 0, lines);

    private static PriceBookQuoteResDTO Quote(
        string name,
        int rank,
        bool isEligible,
        int missingCount,
        params (Guid VppId, decimal GrossAmount)[] lines)
    {
        var quote = new PriceBookQuoteResDTO
        {
            Rank = rank,
            SupplierId = Guid.NewGuid(),
            SupplierName = name,
            PriceListId = Guid.NewGuid(),
            PriceListCode = $"BOOK-{name}",
            CurrencyCode = "VND",
            IsEligible = isEligible,
            RequestedItemCount = lines.Length + missingCount,
            CoveredItemCount = lines.Length,
            CoveragePercent = isEligible ? 100m : lines.Length * 100m / (lines.Length + missingCount),
            Lines = lines.Select(line => new PriceBookQuoteLineResDTO
            {
                VppId = line.VppId,
                Quantity = 1m,
                NetUnitPrice = line.GrossAmount,
                NetAmount = line.GrossAmount,
                GrossAmount = line.GrossAmount
            }).ToList()
        };
        if (missingCount > 0)
        {
            quote.Blockers.Add($"MISSING_ITEMS:{missingCount}");
        }
        quote.GrandTotal = quote.Lines.Sum(line => line.GrossAmount);
        return quote;
    }
}
