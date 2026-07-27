using gtas_vpp_be.Service.Helpers;
using Microsoft.Data.SqlClient;

namespace gtas_vpp_test_support;

public sealed class LocalDbQaFixture : IAsyncDisposable
{
    private readonly LocalDbProcess _localDb;
    private bool _ownsCleanup = true;
    private bool _disposed;

    private LocalDbQaFixture(
        QaFixtureOptions options,
        QaFixtureSecrets secrets,
        QaTestAccounts accounts,
        LocalDbProcess localDb)
    {
        Options = options;
        Secrets = secrets;
        Accounts = accounts;
        _localDb = localDb;
    }

    public QaFixtureOptions Options { get; }

    public QaFixtureSecrets Secrets { get; }

    public QaTestAccounts Accounts { get; }

    public string ConnectionString => Options.ConnectionString;

    public QaFixtureIdentityOptions ExpectedIdentity => new(
        QaFixtureIdentityContract.Purpose,
        Options.RunId,
        QaFixtureIdentityContract.FixtureVersion,
        QaFixtureIdentityContract.HostEnvironment,
        Options.DatabaseName);

    public static async Task<LocalDbQaFixture> CreateAsync(
        QaFixtureOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= QaFixtureOptions.Create();
        QaFixtureIdentityContract.ValidateConnectionString(options.RunId, options.ConnectionString);

        var localDb = LocalDbProcess.Create();
        var secrets = QaFixtureSecrets.Create();
        var fixture = new LocalDbQaFixture(
            options,
            secrets,
            QaTestAccounts.Create(secrets),
            localDb);
        var manifestWritten = false;

        try
        {
            if (await localDb.InstanceExistsAsync(options.InstanceName, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Refusing to reuse existing LocalDB instance '{options.InstanceName}'.");
            }

            await QaFixtureManifest.Create(options).WriteAsync(options.ManifestPath, cancellationToken);
            manifestWritten = true;
            await localDb.CreateAndStartAsync(options.InstanceName, cancellationToken);
            await fixture.InitializeDatabasesAsync(cancellationToken);
            return fixture;
        }
        catch (Exception createException)
        {
            if (!manifestWritten)
            {
                throw;
            }

            try
            {
                await fixture.CleanupAsync(cancellationToken);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "QA fixture creation failed and guarded cleanup also failed.",
                    createException,
                    cleanupException);
            }

            throw;
        }
    }

    public async Task SeedAgainAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await EnsureIdentityAsync(cancellationToken);
        await QaFixtureSeeder.MigrateAndSeedAsync(
            Options,
            Secrets,
            Accounts,
            cancellationToken);
        await EnsureIdentityAsync(cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await DropFixtureDatabasesAsync(cancellationToken);
        await InitializeDatabasesAsync(cancellationToken);
    }

    public async Task<QaFixtureIdentityResponse> EnsureIdentityAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await QaFixtureIdentityContract.ConfirmDatabaseAsync(
                ConnectionString,
                ExpectedIdentity,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "QA fixture database marker does not match the harness identity.");
    }

    public async Task<bool> IsInstancePresentAsync(CancellationToken cancellationToken = default)
    {
        return await _localDb.InstanceExistsAsync(Options.InstanceName, cancellationToken);
    }

    /// <summary>
    /// Test-only crash simulation: leave the instance and manifest for the
    /// guarded stale-state recovery path instead of disposing it normally.
    /// </summary>
    public void SimulateCrashForRecoveryTest()
    {
        ThrowIfDisposed();
        _ownsCleanup = false;
    }

    public static async Task<IReadOnlyList<string>> RecoverStaleAsync(
        string? manifestDirectory = null,
        bool includeLiveOwnerForRecoveryTest = false,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetFullPath(
            manifestDirectory ?? Path.Combine(Path.GetTempPath(), "gtas-vpp-qa"));
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var recovered = new List<string>();
        var localDb = LocalDbProcess.Create();
        foreach (var manifestPath in Directory.EnumerateFiles(
                     directory,
                     "GTASVPP_QA_*.json",
                     SearchOption.TopDirectoryOnly))
        {
            var manifest = await QaFixtureManifest.ReadAsync(manifestPath, cancellationToken);
            if (!string.Equals(manifest.Purpose, QaFixtureIdentityContract.Purpose, StringComparison.Ordinal)
                || !string.Equals(
                    manifest.FixtureVersion,
                    QaFixtureIdentityContract.FixtureVersion,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Refusing unknown QA manifest '{Path.GetFileName(manifestPath)}'.");
            }

            if (!includeLiveOwnerForRecoveryTest && manifest.HasLiveOwner())
            {
                continue;
            }

            var options = QaFixtureOptions.FromManifest(manifest, directory);
            if (await localDb.InstanceExistsAsync(options.InstanceName, cancellationToken))
            {
                await CleanupCoreAsync(options, localDb, cancellationToken);
            }
            else
            {
                DeleteManifest(options);
            }

            recovered.Add(options.RunId);
        }

        return recovered;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsCleanup)
        {
            await CleanupCoreAsync(Options, _localDb, CancellationToken.None);
        }
    }

    private async Task InitializeDatabasesAsync(CancellationToken cancellationToken)
    {
        var existingDatabases = await ListUserDatabasesAsync(
            Options.MasterConnectionString,
            cancellationToken);
        if (existingDatabases.Count != 0)
        {
            throw new InvalidOperationException(
                "A new QA LocalDB instance unexpectedly contains user databases; refusing initialization.");
        }

        Directory.CreateDirectory(Options.DataDirectory);
        await CreateDatabaseWithIsolatedFilesAsync(
            Options,
            Options.DatabaseName,
            "GTAS_VPP_QA",
            cancellationToken);
        await CreateMarkerAsync(cancellationToken);
        await CreateDatabaseWithIsolatedFilesAsync(
            Options,
            QaCleanupSafetyContract.CompanionDatabaseName,
            "GTAS_MENU_QA",
            cancellationToken);
        await QaFixtureSeeder.MigrateAndSeedAsync(
            Options,
            Secrets,
            Accounts,
            cancellationToken);
        await EnsureIdentityAsync(cancellationToken);
    }

    private async Task CreateMarkerAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF OBJECT_ID(N'[dbo].[__GTASQARun]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[__GTASQARun]
                (
                    [RunId] nvarchar(32) NOT NULL CONSTRAINT [PK___GTASQARun] PRIMARY KEY,
                    [Purpose] nvarchar(64) NOT NULL,
                    [FixtureVersion] nvarchar(32) NOT NULL,
                    [DatabaseName] sysname NOT NULL,
                    [CreatedUtc] datetime2 NOT NULL,
                    [SeededUtc] datetime2 NULL
                );
            END;

            INSERT INTO [dbo].[__GTASQARun]
                ([RunId], [Purpose], [FixtureVersion], [DatabaseName], [CreatedUtc])
            VALUES
                (@runId, @purpose, @fixtureVersion, @databaseName, SYSUTCDATETIME());
            """;
        command.Parameters.AddWithValue("@runId", Options.RunId);
        command.Parameters.AddWithValue("@purpose", QaFixtureIdentityContract.Purpose);
        command.Parameters.AddWithValue("@fixtureVersion", QaFixtureIdentityContract.FixtureVersion);
        command.Parameters.AddWithValue("@databaseName", Options.DatabaseName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await CleanupCoreAsync(Options, _localDb, cancellationToken);
        _disposed = true;
    }

    private async Task DropFixtureDatabasesAsync(CancellationToken cancellationToken)
    {
        await ValidateDatabasesForCleanupAsync(Options, cancellationToken);
        SqlConnection.ClearAllPools();
        await DropDatabaseIfPresentAsync(
            Options.MasterConnectionString,
            Options.DatabaseName,
            cancellationToken);
        await DropDatabaseIfPresentAsync(
            Options.MasterConnectionString,
            QaCleanupSafetyContract.CompanionDatabaseName,
            cancellationToken);
        DeleteDataDirectory(Options);
    }

    private static async Task CleanupCoreAsync(
        QaFixtureOptions options,
        LocalDbProcess localDb,
        CancellationToken cancellationToken)
    {
        QaFixtureIdentityContract.ValidateConnectionString(options.RunId, options.ConnectionString);
        if (!await localDb.InstanceExistsAsync(options.InstanceName, cancellationToken))
        {
            DeleteDataDirectory(options);
            DeleteManifest(options);
            return;
        }

        await ValidateDatabasesForCleanupAsync(options, cancellationToken);
        SqlConnection.ClearAllPools();
        await DropDatabaseIfPresentAsync(
            options.MasterConnectionString,
            options.DatabaseName,
            cancellationToken);
        await DropDatabaseIfPresentAsync(
            options.MasterConnectionString,
            QaCleanupSafetyContract.CompanionDatabaseName,
            cancellationToken);
        await localDb.StopAndDeleteAsync(options.InstanceName, cancellationToken);
        DeleteDataDirectory(options);
        DeleteManifest(options);
    }

    private static async Task ValidateDatabasesForCleanupAsync(
        QaFixtureOptions options,
        CancellationToken cancellationToken)
    {
        var databases = await ListUserDatabasesAsync(options.MasterConnectionString, cancellationToken);
        var databaseSet = QaCleanupSafetyContract.ValidateUserDatabases(options, databases);

        if (databaseSet.HasMain)
        {
            var expected = new QaFixtureIdentityOptions(
                QaFixtureIdentityContract.Purpose,
                options.RunId,
                QaFixtureIdentityContract.FixtureVersion,
                QaFixtureIdentityContract.HostEnvironment,
                options.DatabaseName);
            var identity = await QaFixtureIdentityContract.ConfirmDatabaseAsync(
                options.ConnectionString,
                expected,
                cancellationToken);
            if (identity is null)
            {
                throw new InvalidOperationException(
                    "Refusing QA cleanup because the database marker is missing or mismatched.");
            }
        }
    }

    private static async Task<List<string>> ListUserDatabasesAsync(
        string masterConnectionString,
        CancellationToken cancellationToken)
    {
        var databases = new List<string>();
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT [name] FROM [sys].[databases] WHERE [database_id] > 4 ORDER BY [name];";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            databases.Add(reader.GetString(0));
        }

        return databases;
    }

    private static async Task DropDatabaseIfPresentAsync(
        string masterConnectionString,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var escapedLiteral = databaseName.Replace("'", "''", StringComparison.Ordinal);
        var escapedIdentifier = databaseName.Replace("]", "]]", StringComparison.Ordinal);
        await ExecuteNonQueryAsync(
            masterConnectionString,
            $"""
            IF DB_ID(N'{escapedLiteral}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{escapedIdentifier}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{escapedIdentifier}];
            END;
            """,
            cancellationToken);
    }

    private static async Task CreateDatabaseWithIsolatedFilesAsync(
        QaFixtureOptions options,
        string databaseName,
        string logicalNamePrefix,
        CancellationToken cancellationToken)
    {
        var identifier = databaseName.Replace("]", "]]", StringComparison.Ordinal);
        var logicalName = $"{logicalNamePrefix}_{options.RunId}";
        var dataFile = Path.Combine(options.DataDirectory, $"{logicalName}.mdf")
            .Replace("'", "''", StringComparison.Ordinal);
        var logFile = Path.Combine(options.DataDirectory, $"{logicalName}_log.ldf")
            .Replace("'", "''", StringComparison.Ordinal);
        await ExecuteNonQueryAsync(
            options.MasterConnectionString,
            $"""
            CREATE DATABASE [{identifier}]
            ON PRIMARY
            (
                NAME = N'{logicalName}',
                FILENAME = N'{dataFile}'
            )
            LOG ON
            (
                NAME = N'{logicalName}_log',
                FILENAME = N'{logFile}'
            );
            """,
            cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(
        string connectionString,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void DeleteManifest(QaFixtureOptions options)
    {
        var fullPath = Path.GetFullPath(options.ManifestPath);
        var expectedDirectory = Path.GetFullPath(options.ManifestDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(expectedDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to delete a manifest outside its QA directory.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        TryDeleteEmptyDirectory(options.ManifestDirectory);
    }

    private static void DeleteDataDirectory(QaFixtureOptions options)
    {
        var fullPath = Path.GetFullPath(options.DataDirectory);
        var expectedDirectory = Path.GetFullPath(options.ManifestDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(expectedDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to delete QA data outside its manifest directory.");
        }

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }

        var dataRoot = Path.GetDirectoryName(fullPath);
        if (dataRoot is not null)
        {
            TryDeleteEmptyDirectory(dataRoot);
        }
    }

    private static void TryDeleteEmptyDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath)
                && !Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                Directory.Delete(directoryPath);
            }
        }
        catch (IOException)
        {
            // Another concurrently completing fixture may still own or remove the directory.
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
