namespace gtas_vpp_test_support;

public static class QaCleanupSafetyContract
{
    public const string CompanionDatabaseName = "GTAS_MENU";

    public static QaDatabaseSet ValidateUserDatabases(
        QaFixtureOptions options,
        IEnumerable<string> databaseNames)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(databaseNames);

        var databases = databaseNames.ToArray();
        var unexpected = databases
            .Where(database => !string.Equals(database, options.DatabaseName, StringComparison.OrdinalIgnoreCase)
                               && !string.Equals(database, CompanionDatabaseName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (unexpected.Length != 0)
        {
            throw new InvalidOperationException(
                $"Refusing QA cleanup because the isolated instance contains unexpected database(s): {string.Join(", ", unexpected)}.");
        }

        var hasMain = databases.Contains(options.DatabaseName, StringComparer.OrdinalIgnoreCase);
        var hasCompanion = databases.Contains(CompanionDatabaseName, StringComparer.OrdinalIgnoreCase);
        if (!hasMain && hasCompanion)
        {
            throw new InvalidOperationException(
                "Refusing QA cleanup because GTAS_MENU exists without the marker-owning QA database.");
        }

        return new QaDatabaseSet(hasMain, hasCompanion);
    }
}

public sealed record QaDatabaseSet(bool HasMain, bool HasCompanion);
