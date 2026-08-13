using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;

namespace gtas_vpp_be.Service.Helpers;

/// <summary>
/// Contract fail-closed cho database QA dùng một lần bởi integration test và browser test.
/// Contract chủ động chỉ chấp nhận LocalDB instance duy nhất được tạo cho một lần chạy test.
/// </summary>
public static partial class QaFixtureIdentityContract
{
    public const string Purpose = "GTAS_VPP_QA_TEST";
    public const string FixtureVersion = "qa-001-v1";
    public const string HostEnvironment = "Testing";
    public const string ConfigurationSection = "QaFixture";

    [GeneratedRegex("^[0-9A-F]{12}$", RegexOptions.CultureInvariant)]
    private static partial Regex RunIdRegex();

    public static QaFixtureIdentityOptions? Resolve(
        IHostEnvironment hostEnvironment,
        IConfiguration configuration,
        DatabaseBinding databaseBinding)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(databaseBinding);

        if (!configuration.GetValue<bool>($"{ConfigurationSection}:Enabled"))
        {
            return null;
        }

        if (hostEnvironment.IsProduction()
            || !string.Equals(
                hostEnvironment.EnvironmentName,
                HostEnvironment,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConfigurationSection}:Enabled is allowed only in the '{HostEnvironment}' host environment.");
        }

        if (!databaseBinding.IsTestEnvironment)
        {
            throw new InvalidOperationException(
                $"{ConfigurationSection}:Enabled requires the TestEnv database binding.");
        }

        var runId = configuration[$"{ConfigurationSection}:RunId"]?.Trim().ToUpperInvariant();
        ValidateConnectionString(runId, databaseBinding.ConnectionString);

        return new QaFixtureIdentityOptions(
            Purpose,
            runId!,
            FixtureVersion,
            hostEnvironment.EnvironmentName,
            databaseBinding.DatabaseName);
    }

    public static void ValidateConnectionString(string? runId, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(runId) || !RunIdRegex().IsMatch(runId))
        {
            throw new InvalidOperationException(
                "QA fixture RunId must be exactly 12 uppercase hexadecimal characters.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("QA fixture connection string is invalid.");
        }

        var expectedDataSource = $"(localdb)\\GTASVPP_QA_{runId}";
        var expectedDatabase = $"GTAS_VPP_TEST_QA_{runId}";

        if (!string.Equals(builder.DataSource, expectedDataSource, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(builder.InitialCatalog, expectedDatabase, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "QA fixture must target its matching unique LocalDB instance and TEST database.");
        }

        if (!builder.IntegratedSecurity
            || !string.IsNullOrWhiteSpace(builder.UserID)
            || !string.IsNullOrWhiteSpace(builder.Password)
            || !string.IsNullOrWhiteSpace(builder.AttachDBFilename))
        {
            throw new InvalidOperationException(
                "QA fixture requires integrated security and forbids SQL credentials or AttachDBFilename.");
        }
    }

    public static async Task<QaFixtureIdentityResponse?> ConfirmDatabaseAsync(
        string connectionString,
        QaFixtureIdentityOptions expected,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ValidateConnectionString(expected.RunId, connectionString);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF OBJECT_ID(N'[dbo].[__GTASQARun]', N'U') IS NULL
            BEGIN
                SELECT CAST(NULL AS nvarchar(64)) AS [Purpose],
                       CAST(NULL AS nvarchar(32)) AS [RunId],
                       CAST(NULL AS nvarchar(32)) AS [FixtureVersion],
                       CAST(NULL AS sysname) AS [DatabaseName];
                RETURN;
            END;

            SELECT TOP (1) [Purpose], [RunId], [FixtureVersion], DB_NAME() AS [DatabaseName]
            FROM [dbo].[__GTASQARun]
            WHERE [Purpose] = @purpose AND [RunId] = @runId;
            """;
        command.Parameters.AddWithValue("@purpose", expected.Purpose);
        command.Parameters.AddWithValue("@runId", expected.RunId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0))
        {
            return null;
        }

        var purpose = reader.GetString(0);
        var runId = reader.GetString(1);
        var fixtureVersion = reader.GetString(2);
        var databaseName = reader.GetString(3);

        if (!string.Equals(purpose, expected.Purpose, StringComparison.Ordinal)
            || !string.Equals(runId, expected.RunId, StringComparison.Ordinal)
            || !string.Equals(fixtureVersion, expected.FixtureVersion, StringComparison.Ordinal)
            || !string.Equals(databaseName, expected.DatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new QaFixtureIdentityResponse(
            purpose,
            runId,
            fixtureVersion,
            expected.Environment,
            databaseName);
    }
}

public sealed record QaFixtureIdentityOptions(
    string Purpose,
    string RunId,
    string FixtureVersion,
    string Environment,
    string DatabaseName);

public sealed record QaFixtureIdentityResponse(
    string Purpose,
    string RunId,
    string FixtureVersion,
    string Environment,
    string DatabaseName);
