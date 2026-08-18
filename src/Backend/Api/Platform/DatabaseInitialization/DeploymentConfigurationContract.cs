using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;

// Chặn startup/migrator chạy sai database theo host hiện tại.
// Mọi kiểm tra chỉ dựa trên cấu hình deployment; request hoặc claim người dùng không tham gia chọn đích.
public static class DeploymentConfigurationContract
{
    public static string[] GetDatabaseInitializationEnvironments(
        IConfiguration configuration,
        DatabaseBinding databaseBinding,
        bool requireExplicitTarget)
    {
        var configuredEnvironments = configuration
            .GetSection("DatabaseInitialization:Environments")
            .Get<string[]>()
            ?.Where(environment => !string.IsNullOrWhiteSpace(environment))
            .Select(environment => environment.Trim())
            .ToArray();

        if (configuredEnvironments is not { Length: > 0 })
        {
            if (requireExplicitTarget)
            {
                throw new InvalidOperationException(
                    "DatabaseInitialization:Environments must explicitly contain the deployment " +
                    $"database binding '{databaseBinding.EnvironmentName}'.");
            }

            return [databaseBinding.EnvironmentName];
        }

        if (configuredEnvironments.Length != 1
                || !string.Equals(
                    configuredEnvironments[0],
                    databaseBinding.EnvironmentName,
                    StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "DatabaseInitialization:Environments must contain only the deployment database binding " +
                $"'{databaseBinding.EnvironmentName}'.");
        }

        return [databaseBinding.EnvironmentName];
    }

    public static void ValidateDatabaseBindingForHost(
        string hostEnvironmentName,
        bool isProduction,
        DatabaseBinding databaseBinding)
    {
        var expectedEnvironment = isProduction
            ? DatabaseBinding.LiveEnvironment
            : DatabaseBinding.TestEnvironment;
        if (!string.Equals(
                databaseBinding.EnvironmentName,
                expectedEnvironment,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Host environment '{hostEnvironmentName}' requires database binding " +
                $"'{expectedEnvironment}', but '{databaseBinding.EnvironmentName}' was configured.");
        }

        if (databaseBinding.IsTestEnvironment
            && !HasTestOrDemoDatabaseToken(databaseBinding.DatabaseName))
        {
            throw new InvalidOperationException(
                "The TestEnv binding requires a database name containing TEST or DEMO.");
        }

        if (isProduction
            && !string.Equals(
                databaseBinding.DatabaseName,
                "GTAS_VPP_LIVE",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The Production LiveEnv binding must target database 'GTAS_VPP_LIVE'.");
        }
    }

    public static void ValidateDemoDatabaseTarget(
        DatabaseInitializationMode initializationMode,
        DatabaseBinding databaseBinding)
    {
        if (!DatabaseInitializationModeParser.IncludesDemoSeed(initializationMode))
        {
            return;
        }

        if (!databaseBinding.IsTestEnvironment
            || !IsApprovedLocalDataSource(databaseBinding.DataSource)
            || !HasTestOrDemoDatabaseToken(databaseBinding.DatabaseName))
        {
            throw new InvalidOperationException(
                "Demo database initialization requires a TestEnv binding on LocalDB, a loopback SQL Server, " +
                "or the local Compose 'db' service, and a database name containing TEST or DEMO.");
        }
    }

    private static bool IsApprovedLocalDataSource(string dataSource)
    {
        var normalized = dataSource.Trim();
        if (normalized.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[4..];
        }

        if (normalized.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var hostOrInstance = normalized.Split(',', 2)[0];
        return IsHostOrNamedInstance(hostOrInstance, "localhost")
            || IsHostOrNamedInstance(hostOrInstance, "127.0.0.1")
            || IsHostOrNamedInstance(hostOrInstance, ".")
            || IsHostOrNamedInstance(hostOrInstance, "(local)")
            || string.Equals(hostOrInstance, "::1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(hostOrInstance, "[::1]", StringComparison.OrdinalIgnoreCase)
            || string.Equals(hostOrInstance, "db", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHostOrNamedInstance(string hostOrInstance, string allowedHost)
    {
        return string.Equals(hostOrInstance, allowedHost, StringComparison.OrdinalIgnoreCase)
            || hostOrInstance.StartsWith($"{allowedHost}\\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasTestOrDemoDatabaseToken(string databaseName)
    {
        return databaseName
            .Split(['_', '-', '.', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(token => string.Equals(token, "TEST", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "DEMO", StringComparison.OrdinalIgnoreCase));
    }
}
