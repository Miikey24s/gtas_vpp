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
    private const string ImportMigration = "20260810233259_AddPriceListImportBatches";

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
            new PriceListImportFileParser(),
            new DisabledPriceListColumnMappingSuggester());
        var csv = $"ItemCode,UnitPrice,VatRate\n{item.VppCode},12345,8";
        var preview = await service.PreviewAsync(
            priceList.Id,
            "sql-import.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)),
            null,
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

    [Fact]
    public async Task Confirm_RollsBackEveryPriceChangeWhenDatabaseRejectsOneRow()
    {
        SkipUnlessOptedIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken);
        using var unitOfWork = new UnitOfWork(new TestDbContextFactory(fixture.ConnectionString));
        var existing = await unitOfWork.VPPContext.Set<SupplierProductMapping>()
            .AsNoTracking()
            .Include(mapping => mapping.VppItem)
            .Include(mapping => mapping.PriceList)
            .FirstAsync(mapping => !mapping.IsDeleted
                                   && mapping.PriceList != null
                                   && mapping.PriceList!.Status == PriceListStatus.Published
                                   && mapping.PriceList.SupplierId.HasValue,
                cancellationToken);
        var sourceItem = await unitOfWork.VPPContext.Set<VppItem>()
            .AsNoTracking()
            .FirstAsync(item => !item.IsDeleted, cancellationToken);
        var newItem = new VppItem
        {
            Id = Guid.NewGuid(),
            VppCode = $"QA-ROLLBACK-{Guid.NewGuid():N}"[..32],
            VppName = "Mặt hàng kiểm tra rollback",
            UomId = sourceItem.UomId,
            VppCategoryId = sourceItem.VppCategoryId,
            CreatedByUserId = fixture.Accounts.SystemAdmin.UserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedByUserId = fixture.Accounts.SystemAdmin.UserId,
            UpdatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
        unitOfWork.VPPContext.Set<VppItem>().Add(newItem);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var service = new PriceListImportService(
            unitOfWork,
            new DateTimeProvider(),
            new PriceListImportFileParser(),
            new DisabledPriceListColumnMappingSuggester());
        var originalPrice = existing.Price;
        var csv = $"ItemCode,UnitPrice,VatRate\n{existing.VppItem!.VppCode},{originalPrice + 111},8\n{newItem.VppCode},9999,8";
        var preview = await service.PreviewAsync(
            existing.PriceListId,
            "rollback.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)),
            null,
            fixture.Accounts.SystemAdmin.UserId,
            cancellationToken);
        Assert.Equal(1, preview.AddedRows);
        Assert.Equal(1, preview.UpdatedRows);

        await ExecuteSqlAsync(
            fixture.ConnectionString,
            """
            CREATE TRIGGER [TR_QA_RejectPriceImport]
            ON [SupplierProductMappings]
            AFTER INSERT
            AS
            BEGIN
                THROW 51000, 'QA forced rollback', 1;
            END
            """,
            cancellationToken);
        try
        {
            await Assert.ThrowsAnyAsync<Exception>(() => service.ConfirmAsync(
                existing.PriceListId,
                preview.Id,
                preview.RowVersion,
                fixture.Accounts.SystemAdmin.UserId,
                cancellationToken));
        }
        finally
        {
            await ExecuteSqlAsync(
                fixture.ConnectionString,
                "DROP TRIGGER IF EXISTS [TR_QA_RejectPriceImport];",
                cancellationToken);
        }

        var unchanged = await unitOfWork.VPPContext.Set<SupplierProductMapping>()
            .AsNoTracking()
            .SingleAsync(mapping => mapping.Id == existing.Id, cancellationToken);
        Assert.Equal(originalPrice, unchanged.Price);
        Assert.False(await unitOfWork.VPPContext.Set<SupplierProductMapping>()
            .AsNoTracking()
            .AnyAsync(mapping => mapping.PriceListId == existing.PriceListId
                                 && mapping.VppItemId == newItem.Id
                                 && !mapping.IsDeleted,
                cancellationToken));
    }

    private static async Task<int> TableCountAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = N'PriceListImportBatches';";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task ExecuteSqlAsync(
        string connectionString,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
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
