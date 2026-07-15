using gtas_vpp_test_support;
using Microsoft.Data.SqlClient;

namespace gtas_vpp_be.IntegrationTests;

public sealed class LocalDbQaFixtureTests
{
    private const string OptInEnvironmentVariable = "GTAS_QA_SQL_INTEGRATION";

    [Fact]
    public async Task Fixture_migrates_seeds_reseeds_resets_twice_and_cleans_up()
    {
        SkipUnlessOptedIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        var fixture = await LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken);

        try
        {
            var initial = await ReadSnapshotAsync(fixture, cancellationToken);
            AssertSnapshot(initial);

            await fixture.SeedAgainAsync(cancellationToken);
            await fixture.SeedAgainAsync(cancellationToken);
            Assert.Equal(initial, await ReadSnapshotAsync(fixture, cancellationToken));

            await fixture.ResetAsync(cancellationToken);
            AssertSnapshot(await ReadSnapshotAsync(fixture, cancellationToken));
            await fixture.ResetAsync(cancellationToken);
            Assert.Equal(initial, await ReadSnapshotAsync(fixture, cancellationToken));
        }
        finally
        {
            await fixture.DisposeAsync();
        }

        Assert.False(await fixture.IsInstancePresentAsync(cancellationToken));
        Assert.False(File.Exists(fixture.Options.ManifestPath));
    }

    [Fact]
    public async Task Concurrent_fixtures_are_isolated_by_instance_and_database()
    {
        SkipUnlessOptedIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        var fixtures = await Task.WhenAll(
            LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken),
            LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken));

        try
        {
            Assert.NotEqual(fixtures[0].Options.RunId, fixtures[1].Options.RunId);
            Assert.NotEqual(fixtures[0].Options.DataSource, fixtures[1].Options.DataSource);
            Assert.NotEqual(fixtures[0].Options.DatabaseName, fixtures[1].Options.DatabaseName);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => LocalDbQaFixture.CreateAsync(fixtures[0].Options, cancellationToken));
            await fixtures[0].EnsureIdentityAsync(cancellationToken);

            await ExecuteAsync(
                fixtures[0].ConnectionString,
                "CREATE TABLE [dbo].[__QAIsolationCanary] ([Value] int NOT NULL); INSERT INTO [dbo].[__QAIsolationCanary] VALUES (1);",
                cancellationToken);

            Assert.Equal(1, await ScalarIntAsync(
                fixtures[0].ConnectionString,
                "SELECT COUNT(*) FROM [dbo].[__QAIsolationCanary];",
                cancellationToken));
            Assert.Equal(0, await ScalarIntAsync(
                fixtures[1].ConnectionString,
                "SELECT COUNT(*) FROM [sys].[tables] WHERE [name] = N'__QAIsolationCanary';",
                cancellationToken));
        }
        finally
        {
            foreach (var fixture in fixtures)
            {
                await fixture.DisposeAsync();
            }
        }

        foreach (var fixture in fixtures)
        {
            Assert.False(await fixture.IsInstancePresentAsync(cancellationToken));
        }
    }

    [Fact]
    public async Task Stale_manifest_recovery_validates_marker_and_removes_crash_state()
    {
        SkipUnlessOptedIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        var recoveryTestRoot = Path.Combine(
            Path.GetTempPath(),
            "gtas-vpp-qa-recovery-tests");
        var manifestDirectory = Path.Combine(recoveryTestRoot, Guid.NewGuid().ToString("N"));
        var fixture = await LocalDbQaFixture.CreateAsync(
            QaFixtureOptions.Create(manifestDirectory: manifestDirectory),
            cancellationToken);

        try
        {
            fixture.SimulateCrashForRecoveryTest();
            await fixture.DisposeAsync();
            Assert.True(await fixture.IsInstancePresentAsync(cancellationToken));
            Assert.True(File.Exists(fixture.Options.ManifestPath));

            var recovered = await LocalDbQaFixture.RecoverStaleAsync(
                manifestDirectory,
                includeLiveOwnerForRecoveryTest: true,
                cancellationToken);

            Assert.Contains(fixture.Options.RunId, recovered);
            Assert.False(await fixture.IsInstancePresentAsync(cancellationToken));
            Assert.False(File.Exists(fixture.Options.ManifestPath));
            Assert.False(Directory.Exists(manifestDirectory));
        }
        finally
        {
            if (await fixture.IsInstancePresentAsync(cancellationToken))
            {
                await LocalDbQaFixture.RecoverStaleAsync(
                    manifestDirectory,
                    includeLiveOwnerForRecoveryTest: true,
                    cancellationToken);
            }

            if (Directory.Exists(manifestDirectory)
                && !Directory.EnumerateFileSystemEntries(manifestDirectory).Any())
            {
                Directory.Delete(manifestDirectory);
            }

            if (Directory.Exists(recoveryTestRoot)
                && !Directory.EnumerateFileSystemEntries(recoveryTestRoot).Any())
            {
                Directory.Delete(recoveryTestRoot);
            }
        }
    }

    private static async Task<FixtureSnapshot> ReadSnapshotAsync(
        LocalDbQaFixture fixture,
        CancellationToken cancellationToken)
    {
        await fixture.EnsureIdentityAsync(cancellationToken);
        return new FixtureSnapshot(
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[__EFMigrationsHistory];", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [sys].[procedures] WHERE [name] = N'sp_Authen_Login';", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [GTAS_MENU].[dbo].[tblUsers] WHERE [UserID] BETWEEN 910001 AND 910006;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[P02_Group] WHERE [GroupName] IN (N'User', N'QA Manager', N'QA Procurement', N'Admin') AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[P04_UserGroup] WHERE [UserId] BETWEEN 910001 AND 910006 AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[VPP01_RequestHeader] WHERE [Id] IN ('40000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000002', '40000000-0000-0000-0000-000000000003');", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[VPP01_RequestHeader] WHERE [CreateUserId] = 910001 AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[VPP01_RequestHeader] WHERE [DepartmentCode] = N'QA-D01' AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[VPP01_RequestHeader] WHERE [MemberCompanyCode] = N'77500' AND [IsDeleted] = 0;", cancellationToken));
    }

    private static void AssertSnapshot(FixtureSnapshot snapshot)
    {
        Assert.True(snapshot.Migrations > 0);
        Assert.Equal(1, snapshot.LoginStoredProcedure);
        Assert.Equal(6, snapshot.Accounts);
        Assert.Equal(4, snapshot.RequiredRoles);
        Assert.Equal(6, snapshot.UserRoleMappings);
        Assert.Equal(3, snapshot.ScopeRequests);
        Assert.Equal(1, snapshot.OwnScopeRows);
        Assert.Equal(2, snapshot.DepartmentScopeRows);
        Assert.Equal(3, snapshot.CompanyScopeRows);
    }

    private static async Task<int> ScalarIntAsync(
        string connectionString,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 120;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task ExecuteAsync(
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

    private static void SkipUnlessOptedIn()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(OptInEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            Assert.Skip($"Set {OptInEnvironmentVariable}=1 to run disposable LocalDB integration tests.");
        }
    }

    private sealed record FixtureSnapshot(
        int Migrations,
        int LoginStoredProcedure,
        int Accounts,
        int RequiredRoles,
        int UserRoleMappings,
        int ScopeRequests,
        int OwnScopeRows,
        int DepartmentScopeRows,
        int CompanyScopeRows);
}
