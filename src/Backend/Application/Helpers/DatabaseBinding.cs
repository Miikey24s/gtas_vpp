using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace gtas_vpp_be.Service.Helpers;

/// <summary>
/// Database duy nhất do cấu hình deployment lựa chọn. Dữ liệu request và user claim
/// không bao giờ được tham gia tạo giá trị này.
/// </summary>
public sealed class DatabaseBinding
{
    public const string TestEnvironment = "TestEnv";
    public const string LiveEnvironment = "LiveEnv";

    private DatabaseBinding(
        string environmentName,
        string connectionString,
        string dataSource,
        string databaseName)
    {
        EnvironmentName = environmentName;
        ConnectionString = connectionString;
        DataSource = dataSource;
        DatabaseName = databaseName;
    }

    public string EnvironmentName { get; }

    public string ConnectionString { get; }

    public string DataSource { get; }

    public string DatabaseName { get; }

    public bool IsTestEnvironment =>
        string.Equals(EnvironmentName, TestEnvironment, StringComparison.Ordinal);

    public static DatabaseBinding Create(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var environmentName = NormalizeRequiredEnvironment(
            configuration["DatabaseSettings:DefaultEnvironment"]);
        var oppositeEnvironment = environmentName == TestEnvironment
            ? LiveEnvironment
            : TestEnvironment;

        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString(oppositeEnvironment)))
        {
            throw new InvalidOperationException(
                $"Only the deployment database '{environmentName}' may be configured. " +
                $"Remove connection string '{oppositeEnvironment}'.");
        }

        var connectionString = configuration.GetConnectionString(environmentName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{environmentName}' is required for this deployment.");
        }

        SqlConnectionStringBuilder connectionBuilder;
        try
        {
            connectionBuilder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                $"Connection string '{environmentName}' is invalid.");
        }

        if (string.IsNullOrWhiteSpace(connectionBuilder.DataSource)
            || string.IsNullOrWhiteSpace(connectionBuilder.InitialCatalog))
        {
            throw new InvalidOperationException(
                $"Connection string '{environmentName}' must specify a server and database.");
        }

        return new DatabaseBinding(
            environmentName,
            connectionBuilder.ConnectionString,
            connectionBuilder.DataSource,
            connectionBuilder.InitialCatalog);
    }

    private static string NormalizeRequiredEnvironment(string? configuredEnvironment)
    {
        if (string.Equals(configuredEnvironment?.Trim(), TestEnvironment, StringComparison.OrdinalIgnoreCase))
        {
            return TestEnvironment;
        }

        if (string.Equals(configuredEnvironment?.Trim(), LiveEnvironment, StringComparison.OrdinalIgnoreCase))
        {
            return LiveEnvironment;
        }

        throw new InvalidOperationException(
            $"DatabaseSettings:DefaultEnvironment must be '{TestEnvironment}' or '{LiveEnvironment}'.");
    }
}
