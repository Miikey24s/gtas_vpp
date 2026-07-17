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

        var legacy = await context.Set<L07_PriceList>()
            .AsNoTracking()
            .OrderBy(x => x.CreateDate)
            .FirstAsync(cancellationToken);
        Assert.True(legacy.Version > 0);
        Assert.Equal("VND", legacy.CurrencyCode);
        Assert.NotEqual(default, legacy.EffectiveFromUtc);
        Assert.False(string.IsNullOrWhiteSpace(legacy.LegacyBackfillStatus));

        var supplier = await context.Set<L05_VPPSupplier>()
            .FirstAsync(x => !x.IsDeleted, cancellationToken);
        var vpp = await context.Set<L04_VPP>()
            .FirstAsync(x => !x.IsDeleted, cancellationToken);
        var asOfUtc = new DateTime(2026, 7, 16, 8, 0, 0, DateTimeKind.Utc);
        var book = new L07_PriceList
        {
            Id = Guid.NewGuid(),
            PriceListCode = $"SQL-{fixture.Options.RunId}",
            PriceListName = "SQL resolver",
            SupplierId = supplier.Id,
            Version = 1,
            EffectiveFromUtc = asOfUtc.AddDays(-1),
            EffectiveToUtc = asOfUtc.AddDays(1),
            Status = L07_PriceListStatus.Published,
            CurrencyCode = "VND",
            VatPolicy = "item-rate",
            CreateUserId = 1,
            CreateDate = asOfUtc,
            UpdateUserId = 1,
            UpdateDate = asOfUtc
        };
        context.Set<L07_PriceList>().Add(book);
        context.Set<L06_VPPSupplierMapping>().Add(new L06_VPPSupplierMapping
        {
            Id = Guid.NewGuid(),
            L07_PriceListId = book.Id,
            L04_VPPId = vpp.Id,
            L05_VPPSupplierId = supplier.Id,
            Price = 12500m,
            NetPrice = 12500m,
            VatRate = 8m,
            MinimumOrderQuantity = 2m,
            LeadTimeDays = 4,
            SupplierSku = "SQL-SKU",
            IsDefault = true,
            CreateUserId = 1,
            CreateDate = asOfUtc,
            UpdateUserId = 1,
            UpdateDate = asOfUtc
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
