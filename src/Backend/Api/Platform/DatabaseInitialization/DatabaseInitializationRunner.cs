using gtas_vpp_be.Model;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// Chạy migration và seed theo một luồng duy nhất, độc lập với composition root trong Program.cs.
// Khóa SQL theo tên database để hai tiến trình khởi tạo không ghi schema đồng thời.
public static class DatabaseInitializationRunner
{
    public static async Task RunAsync(
        DatabaseBinding databaseBinding,
        DatabaseInitializationMode initializationMode,
        bool shouldSeedReference,
        bool shouldSeedDemo,
        DemoWorkbookSeedOptions? demoSeedOptions)
    {
        const int maxRetries = 5;
        for (var retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                await using var migrationLock = await AcquireMigrationLockAsync(databaseBinding.ConnectionString);
                var optionsBuilder = new DbContextOptionsBuilder<VPPMigrationDbContext>();
                optionsBuilder.UseSqlServer(
                    databaseBinding.ConnectionString,
                    action => action.MigrationsAssembly(Config.DatabaseSettings.MigrationsAssembly));

                await using var dbContext = new VPPMigrationDbContext(optionsBuilder.Options);
                Console.WriteLine(
                    $"[Migration] Applying mode {initializationMode} for environment {databaseBinding.EnvironmentName}...");
                await dbContext.Database.MigrateAsync();
                await SeedAsync(
                    dbContext,
                    databaseBinding,
                    initializationMode,
                    shouldSeedReference,
                    shouldSeedDemo,
                    demoSeedOptions);

                Console.WriteLine(
                    $"[Migration] Mode {initializationMode} for environment {databaseBinding.EnvironmentName} completed successfully.");
                return;
            }
            catch (DatabaseInitializationException ex)
            {
                // Seed sai dữ liệu là lỗi xác định; retry không thể tự sửa nên dừng ngay.
                Console.WriteLine(
                    $"[Migration] Deterministic initialization failure for {databaseBinding.EnvironmentName}: {ex.Message}");
                throw;
            }
            catch (Exception) when (retry < maxRetries - 1)
            {
                Console.WriteLine(
                    $"[Migration] DB {databaseBinding.EnvironmentName} is not ready, " +
                    $"retrying ({retry + 1}/{maxRetries}) in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[Migration] Migration for {databaseBinding.EnvironmentName} failed after {maxRetries} attempts. " +
                    $"Exception: {ex.Message}");
                throw;
            }
        }
    }

    private static async Task SeedAsync(
        VPPMigrationDbContext dbContext,
        DatabaseBinding databaseBinding,
        DatabaseInitializationMode initializationMode,
        bool shouldSeedReference,
        bool shouldSeedDemo,
        DemoWorkbookSeedOptions? demoSeedOptions)
    {
        if (!shouldSeedReference && !shouldSeedDemo)
        {
            return;
        }

        try
        {
            if (shouldSeedDemo)
            {
                await SeedData.SeedDemo(dbContext, demoSeedOptions);
                return;
            }

            await SeedData.SeedReference(dbContext);
        }
        catch (Exception ex)
        {
            throw new DatabaseInitializationException(
                $"Database seed mode {initializationMode} failed for environment {databaseBinding.EnvironmentName}.",
                ex);
        }
    }

    private static async Task<SqlConnection> AcquireMigrationLockAsync(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = @resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Session',
                    @LockTimeout = 120000;
                SELECT @result;
                """;
            command.Parameters.AddWithValue("@resource", $"GTAS_VPP_SCHEMA_INIT:{databaseName}");

            var result = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (result < 0)
            {
                throw new InvalidOperationException(
                    $"Could not acquire the database migration lock for {databaseName}. SQL result: {result}.");
            }

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
