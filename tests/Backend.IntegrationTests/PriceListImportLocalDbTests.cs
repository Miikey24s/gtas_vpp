using System.Text;
using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_test_support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace gtas_vpp_be.IntegrationTests;

public sealed class PriceListImportLocalDbTests
{
    private const string OptInEnvironmentVariable = "GTAS_QA_SQL_INTEGRATION";
    private const string PreviousMigration = "20260810095841_AddPostCloseAdjustmentWindow";
    private const string ImportMigration = "20260810223137_AddPriceListImportBatches";

    [Fact]
    public async Task ImportMigration_UpgradesRollsBackReappliesAndImportsOnDisposableSqlServer()
    {
        SkipUnlessOptedIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken);
        var migrationOptions = new DbContextOptionsBuilder<VPPMigrationDbContext>()
            .UseSqlServer(
                fixture.ConnectionString,
                sql => sql.MigrationsAssembly(Config.DatabaseSettings.MigrationsAssembly))
            .Options;

        await using (var migrationContext = new VPPMigrationDbContext(migrationOptions))
        {
            var migrator = migrationContext.GetService<IMigrator>();
            Assert.Equal(1, await TableCountAsync(fixture.ConnectionString, cancellationToken));
            await migrator.MigrateAsync(PreviousMigration, cancellationToken);
            Assert.Equal(0, await TableCountAsync(fixture.ConnectionString, cancellationToken));
            await migrator.MigrateAsync(ImportMigration, cancellationToken);
            Assert.Equal(1, await TableCountAsync(fixture.ConnectionString, cancellationToken));
        }

        using var unitOfWork = new UnitOfWork(new TestDbContextFactory(fixture.ConnectionString));
        var priceList = await unitOfWork.VPPContext.Set<PriceList>()
            .AsNoTracking()
            .FirstAsync(item => !item.IsDeleted
                                && item.Status == PriceListStatus.Published
                                && item.SupplierId.HasValue,
                cancellationToken);
        var item = await unitOfWork.VPPContext.Set<VppItem>()
            .AsNoTracking()
            .FirstAsync(product => !product.IsDeleted, cancellationToken);
        var service = new PriceListImportService(
            unitOfWork,
            new DateTimeProvider(),
            new PriceListImportFileParser());
        var csv = $"ItemCode,UnitPrice,VatRate\n{item.VppCode},12345,8";
        var preview = await service.PreviewAsync(
            priceList.Id,
            "sql-import.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)),
            fixture.Accounts.SystemAdmin.UserId,
            cancellationToken);

        var completed = await service.ConfirmAsync(
            priceList.Id,
            preview.Id,
            preview.RowVersion,
            fixture.Accounts.SystemAdmin.UserId,
            cancellationToken);

        Assert.True(preview.CanConfirm);
        Assert.Equal("Completed", completed.Status);
        Assert.Equal(1, await unitOfWork.VPPContext.Set<PriceListImportBatch>()
            .CountAsync(batch => batch.Id == preview.Id && batch.Status == PriceListImportBatchStatus.Completed, cancellationToken));
    }

    private static async Task<int> TableCountAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = N'PriceListImportBatches';";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
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
