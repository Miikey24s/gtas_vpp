using gtas_vpp_be.Model;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_test_support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace gtas_vpp_be.IntegrationTests;

public sealed class SupplierSpecificItemCodeRemovalMigrationLocalDbTests
{
    private const string OptInEnvironmentVariable = "GTAS_QA_SQL_INTEGRATION";
    private const string PreviousMigration = "20260810233259_AddPriceListImportBatches";
    private const string RemovalMigration = "20260817181622_RemoveSupplierSku";
    private static readonly string RemovedColumn = string.Concat("Supplier", "Sku");

    [Fact]
    public async Task RemovalMigration_UpgradesRollsBackAndReappliesWithoutDroppingRows()
    {
        SkipUnlessOptedIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken);
        var options = new DbContextOptionsBuilder<VPPMigrationDbContext>()
            .UseSqlServer(
                fixture.ConnectionString,
                sql => sql.MigrationsAssembly(Config.DatabaseSettings.MigrationsAssembly))
            .Options;

        await using var context = new VPPMigrationDbContext(options);
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(PreviousMigration, cancellationToken);
        Assert.True(await ColumnExistsAsync(fixture.ConnectionString, "SupplierProductMappings", cancellationToken));
        Assert.True(await ColumnExistsAsync(fixture.ConnectionString, "SettlementItems", cancellationToken));
        var priceRowsBefore = await RowCountAsync(fixture.ConnectionString, "SupplierProductMappings", cancellationToken);
        var settlementRowsBefore = await RowCountAsync(fixture.ConnectionString, "SettlementItems", cancellationToken);

        await migrator.MigrateAsync(RemovalMigration, cancellationToken);
        Assert.False(await ColumnExistsAsync(fixture.ConnectionString, "SupplierProductMappings", cancellationToken));
        Assert.False(await ColumnExistsAsync(fixture.ConnectionString, "SettlementItems", cancellationToken));
        Assert.Equal(priceRowsBefore, await RowCountAsync(fixture.ConnectionString, "SupplierProductMappings", cancellationToken));
        Assert.Equal(settlementRowsBefore, await RowCountAsync(fixture.ConnectionString, "SettlementItems", cancellationToken));

        await migrator.MigrateAsync(PreviousMigration, cancellationToken);
        Assert.True(await ColumnExistsAsync(fixture.ConnectionString, "SupplierProductMappings", cancellationToken));
        Assert.True(await ColumnExistsAsync(fixture.ConnectionString, "SettlementItems", cancellationToken));

        await migrator.MigrateAsync(RemovalMigration, cancellationToken);
        Assert.False(await ColumnExistsAsync(fixture.ConnectionString, "SupplierProductMappings", cancellationToken));
        Assert.False(await ColumnExistsAsync(fixture.ConnectionString, "SettlementItems", cancellationToken));
    }

    private static async Task<bool> ColumnExistsAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(@tableName) AND name = @columnName;";
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", RemovedColumn);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    private static async Task<int> RowCountAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken)
    {
        var allowedTable = tableName switch
        {
            "SupplierProductMappings" => "[SupplierProductMappings]",
            "SettlementItems" => "[SettlementItems]",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName))
        };
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {allowedTable};";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static void SkipUnlessOptedIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            Assert.Skip($"Set {OptInEnvironmentVariable}=1 to run disposable LocalDB integration tests.");
        }
    }
}
