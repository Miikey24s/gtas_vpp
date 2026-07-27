using gtas_vpp_be.Service.Services;
using Xunit;

namespace gtas_vpp_be.Tests;

public sealed class BootstrapSafetyTests
{
    [Theory]
    [InlineData(null, true, DatabaseInitializationMode.MigrateAndReference)]
    [InlineData(null, false, DatabaseInitializationMode.None)]
    [InlineData("None", true, DatabaseInitializationMode.None)]
    [InlineData("Migrate", true, DatabaseInitializationMode.Migrate)]
    [InlineData("MigrateAndReference", true, DatabaseInitializationMode.MigrateAndReference)]
    [InlineData("MigrateAndDemo", true, DatabaseInitializationMode.MigrateAndDemo)]
    [InlineData("DemoSeed", true, DatabaseInitializationMode.MigrateAndDemo)]
    public void Parse_UsesExplicitSafeModes(
        string? configuredValue,
        bool isDevelopment,
        DatabaseInitializationMode expected)
    {
        var actual = DatabaseInitializationModeParser.Parse(configuredValue, isDevelopment);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Production_RejectsDemoSeedBeforeDatabaseAccess()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DatabaseInitializationModeParser.ValidateForEnvironment(
                DatabaseInitializationMode.MigrateAndDemo,
                isProduction: true,
                ["TestEnv"],
                allowDemoData: true));
    }

    [Fact]
    public void NonProduction_RejectsDemoSeedForLiveDatabaseTarget()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DatabaseInitializationModeParser.ValidateForEnvironment(
                DatabaseInitializationMode.MigrateAndDemo,
                isProduction: false,
                ["TestEnv", "LiveEnv"],
                allowDemoData: true));
    }

    [Fact]
    public void NonProduction_RejectsDemoSeedForUnknownDatabaseTarget()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DatabaseInitializationModeParser.ValidateForEnvironment(
                DatabaseInitializationMode.MigrateAndDemo,
                isProduction: false,
                ["StagingEnv"],
                allowDemoData: true));
    }

    [Fact]
    public void NonProduction_RequiresExplicitDemoOptIn()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DatabaseInitializationModeParser.ValidateForEnvironment(
                DatabaseInitializationMode.MigrateAndDemo,
                isProduction: false,
                ["TestEnv"]));
    }

    [Fact]
    public void NonProduction_AllowsDemoSeedForTestDatabaseTarget()
    {
        DatabaseInitializationModeParser.ValidateForEnvironment(
            DatabaseInitializationMode.MigrateAndDemo,
            isProduction: false,
            ["TestEnv"],
            allowDemoData: true);
    }

    [Fact]
    public void LegacySeedAlias_IsAlsoRejectedInProduction()
    {
        var mode = DatabaseInitializationModeParser.Parse("MigrateAndSeed", isDevelopment: false);

        Assert.Equal(DatabaseInitializationMode.MigrateAndDemo, mode);
        Assert.Throws<InvalidOperationException>(() =>
            DatabaseInitializationModeParser.ValidateForEnvironment(
                mode,
                isProduction: true,
                ["TestEnv"],
                allowDemoData: true));
    }

    [Fact]
    public void Production_AllowsReferenceSeed()
    {
        DatabaseInitializationModeParser.ValidateForEnvironment(
            DatabaseInitializationMode.MigrateAndReference,
            isProduction: true,
            ["LiveEnv"]);
    }

    [Fact]
    public void SplitIntoBatches_HandlesGoSeparatorsAndBraces()
    {
        var batches = SqlBatchExecutor.SplitIntoBatches("SELECT '{x}'\r\nGO\r\nSELECT 2");

        Assert.Equal(2, batches.Count);
        Assert.Contains("{x}", batches[0]);
        Assert.Contains("SELECT 2", batches[1]);
    }

    [Fact]
    public void ReadBatches_MissingRequiredFileThrows()
    {
        var missingPath = $"Helpers/SQL/__missing_{Guid.NewGuid():N}.sql";

        Assert.Throws<FileNotFoundException>(() => SqlBatchExecutor.ReadBatches(missingPath));
    }

    [Fact]
    public void ReadBatches_EmptyRequiredFileThrows()
    {
        var relativePath = $"Helpers/SQL/__empty_{Guid.NewGuid():N}.sql";
        var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, " \r\n\t");

        try
        {
            Assert.Throws<InvalidDataException>(() => SqlBatchExecutor.ReadBatches(relativePath));
        }
        finally
        {
            File.Delete(fullPath);
        }
    }
}
