using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_test_support;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.IntegrationTests;

public sealed class PriceAsOfResolverLocalDbTests
{
    private const string OptInEnvironmentVariable = "GTAS_QA_SQL_INTEGRATION";

    [Fact]
    public async Task PriceEffectivityMigration_BackfillsLegacyAndResolverRunsOnSqlServer()
    {
        SkipUnlessOptedIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken);
        using var unitOfWork = new UnitOfWork(new TestDbContextFactory(fixture.ConnectionString));
        var context = unitOfWork.VPPContext;

        var legacy = await context.Set<PriceList>()
            .AsNoTracking()
            .OrderBy(x => x.CreatedAtUtc)
            .FirstAsync(cancellationToken);
        Assert.True(legacy.Version > 0);
        Assert.Equal("VND", legacy.CurrencyCode);
        Assert.NotEqual(default, legacy.EffectiveFromUtc);
        Assert.False(string.IsNullOrWhiteSpace(legacy.LegacyBackfillStatus));

        var supplier = await context.Set<Supplier>()
            .FirstAsync(x => !x.IsDeleted, cancellationToken);
        var vpp = await context.Set<VppItem>()
            .FirstAsync(x => !x.IsDeleted, cancellationToken);
        var asOfUtc = new DateTime(2026, 7, 16, 8, 0, 0, DateTimeKind.Utc);
        var book = new PriceList
        {
            Id = Guid.NewGuid(),
            PriceListCode = $"SQL-{fixture.Options.RunId}",
            PriceListName = "SQL resolver",
            SupplierId = supplier.Id,
            Version = 1,
            EffectiveFromUtc = asOfUtc.AddDays(-1),
            EffectiveToUtc = asOfUtc.AddDays(1),
            Status = PriceListStatus.Published,
            CurrencyCode = "VND",
            VatPolicy = "item-rate",
            CreatedByUserId = 1,
            CreatedAtUtc = asOfUtc,
            UpdatedByUserId = 1,
            UpdatedAtUtc = asOfUtc
        };
        context.Set<PriceList>().Add(book);
        context.Set<SupplierProductMapping>().Add(new SupplierProductMapping
        {
            Id = Guid.NewGuid(),
            PriceListId = book.Id,
            VppItemId = vpp.Id,
            SupplierId = supplier.Id,
            Price = 12500m,
            NetPrice = 12500m,
            VatRate = 8m,
            MinimumOrderQuantity = 2m,
            LeadTimeDays = 4,
            SupplierSku = "SQL-SKU",
            IsDefault = true,
            CreatedByUserId = 1,
            CreatedAtUtc = asOfUtc,
            UpdatedByUserId = 1,
            UpdatedAtUtc = asOfUtc
        });
        await context.SaveChangesAsync(cancellationToken);

        var resolver = new PriceAsOfResolver(unitOfWork);
        var result = await resolver.ResolveAsync(new PriceResolutionReqDTO
        {
            VppId = vpp.Id,
            SupplierId = supplier.Id,
            PriceAsOfUtc = asOfUtc,
            Quantity = 2m
        }, cancellationToken);

        Assert.True(result.IsResolved);
        Assert.Equal(book.Id, result.PriceListId);
        Assert.Equal(25000m, result.NetAmount);
        Assert.Equal(2000m, result.VatAmount);
        Assert.Equal(27000m, result.GrossAmount);
    }

    private static void SkipUnlessOptedIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            Assert.Skip($"Set {OptInEnvironmentVariable}=1 to run disposable LocalDB integration tests.");
        }
    }

    private sealed class TestDbContextFactory(string connectionString) : IDynamicDbContextFactory
    {
        public VPPContext CreateVPPContext()
        {
            var options = new DbContextOptionsBuilder<VPPContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new VPPContext(options);
        }
    }
}
