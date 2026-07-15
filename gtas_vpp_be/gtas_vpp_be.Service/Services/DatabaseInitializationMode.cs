namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Controls what the startup/migrator process is allowed to do to a database.
/// Demo data is deliberately a separate, explicit mode so a production
/// migration cannot silently create human/demo accounts or company fixtures.
/// </summary>
public enum DatabaseInitializationMode
{
    None,
    Migrate,
    MigrateAndReference,
    MigrateAndDemo
}

public static class DatabaseInitializationModeParser
{
    public static DatabaseInitializationMode Parse(string? configuredValue, bool isDevelopment)
    {
        var value = string.IsNullOrWhiteSpace(configuredValue)
            ? (isDevelopment ? "MigrateAndReference" : "None")
            : configuredValue.Trim();

        return value.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant() switch
        {
            "NONE" => DatabaseInitializationMode.None,
            "MIGRATE" => DatabaseInitializationMode.Migrate,
            "MIGRATEANDREFERENCE" => DatabaseInitializationMode.MigrateAndReference,
            "MIGRATEANDREFERENCESEED" => DatabaseInitializationMode.MigrateAndReference,
            "MIGRATEANDDEMO" => DatabaseInitializationMode.MigrateAndDemo,
            "MIGRATEANDDEMOSEED" => DatabaseInitializationMode.MigrateAndDemo,
            "DEMOSEED" => DatabaseInitializationMode.MigrateAndDemo,
            // Compatibility for old local configuration. It is converted to
            // the explicit demo mode, which still requires an opt-in and an
            // exclusively TestEnv target in ValidateForEnvironment.
            "MIGRATEANDSEED" => DatabaseInitializationMode.MigrateAndDemo,
            _ => throw new InvalidOperationException(
                $"Unsupported DatabaseInitialization:Mode '{configuredValue}'. " +
                "Expected None, Migrate, MigrateAndReference, or MigrateAndDemo.")
        };
    }

    public static bool RequiresMigration(DatabaseInitializationMode mode)
        => mode != DatabaseInitializationMode.None;

    public static bool IncludesReferenceSeed(DatabaseInitializationMode mode)
        => mode is DatabaseInitializationMode.MigrateAndReference
            or DatabaseInitializationMode.MigrateAndDemo;

    public static bool IncludesDemoSeed(DatabaseInitializationMode mode)
        => mode == DatabaseInitializationMode.MigrateAndDemo;

    public static void ValidateForEnvironment(
        DatabaseInitializationMode mode,
        bool isProduction,
        IEnumerable<string>? databaseEnvironments = null,
        bool allowDemoData = false)
    {
        if (!IncludesDemoSeed(mode))
        {
            return;
        }

        if (!allowDemoData)
        {
            throw new InvalidOperationException(
                "Demo database seed requires DatabaseInitialization:AllowDemoData=true. " +
                "Enable it only for a disposable TestEnv database.");
        }

        var targets = databaseEnvironments?
            .Where(environment => !string.IsNullOrWhiteSpace(environment))
            .Select(environment => environment.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];
        var targetsOnlyTestEnvironment = targets.Length > 0
            && targets.All(environment =>
                string.Equals(environment, "TestEnv", StringComparison.OrdinalIgnoreCase));

        if (isProduction || !targetsOnlyTestEnvironment)
        {
            throw new InvalidOperationException(
                "Demo database seed is not permitted in Production and requires exclusively TestEnv database targets. " +
                "Use MigrateAndReference for live/other environments and run demo seed only against a disposable TestEnv database.");
        }
    }
}

/// <summary>
/// Marks a deterministic bootstrap/seed failure. The host must not retry this
/// as if the database were merely waiting to become ready.
/// </summary>
public sealed class DatabaseInitializationException : Exception
{
    public DatabaseInitializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
