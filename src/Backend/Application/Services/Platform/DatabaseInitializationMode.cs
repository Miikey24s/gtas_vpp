namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Kiểm soát startup/migrator process được phép làm gì với database.
/// Dữ liệu demo chủ động nằm trong mode riêng và tường minh để migration production
/// không thể âm thầm tạo tài khoản người/demo hoặc company fixture.
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
            // Tương thích với cấu hình local cũ. Giá trị được chuyển sang demo mode
            // tường minh, vẫn yêu cầu opt-in và target chỉ là TestEnv trong ValidateForEnvironment.
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
/// Đánh dấu lỗi bootstrap/seed xác định. Host không được retry như thể database
/// chỉ đang chờ sẵn sàng.
/// </summary>
public sealed class DatabaseInitializationException : Exception
{
    public DatabaseInitializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
