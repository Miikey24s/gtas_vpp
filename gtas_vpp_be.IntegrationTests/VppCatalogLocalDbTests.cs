using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_test_support;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.IntegrationTests;

public sealed class VppCatalogLocalDbTests
{
    private const string OptInEnvironmentVariable = "GTAS_QA_SQL_INTEGRATION";

    [Fact]
    public async Task CatalogSearch_IsAccentInsensitiveAndTreatsWildcardsLiterally()
    {
        SkipUnlessOptedIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken);
        using var unitOfWork = new UnitOfWork(new TestDbContextFactory(fixture.ConnectionString));
        var context = unitOfWork.VPPContext;
        var uomId = await context.Set<L02_ClassDetail>()
            .Where(x => !x.IsDeleted)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
        var categoryId = await context.Set<L03_VPPCategory>()
            .Where(x => !x.IsDeleted)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
        var now = new DateTime(2026, 7, 16, 10, 0, 0);
        var item = new L04_VPP
        {
            Id = Guid.NewGuid(),
            VPPCode = $"LEAN06-{fixture.Options.RunId}",
            VPPName = "Bút bi 100% xanh",
            UOMId = uomId,
            VPPCategoryId = categoryId,
            CreateUserId = 1,
            UpdateUserId = 1,
            CreateDate = now,
            UpdateDate = now
        };
        context.Set<L04_VPP>().Add(item);
        await context.SaveChangesAsync(cancellationToken);

        var service = new VppCatalogService(unitOfWork, new FixedDateTimeProvider(now));
        var accentResult = await service.QueryItemsAsync(
            categoryId, "but bi", null, 0, 20, null, null, null, false, cancellationToken);
        var wildcardResult = await service.QueryItemsAsync(
            categoryId, "100%", null, 0, 20, null, null, null, false, cancellationToken);

        Assert.Contains(accentResult.Items, x => x.Id == item.Id);
        Assert.Contains(wildcardResult.Items, x => x.Id == item.Id);
    }

    private static void SkipUnlessOptedIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            Assert.Skip($"Set {OptInEnvironmentVariable}=1 to run disposable LocalDB integration tests.");
        }
    }

    private sealed class FixedDateTimeProvider(DateTime now) : IDateTimeProvider
    {
        public DateTime Now => now;
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
