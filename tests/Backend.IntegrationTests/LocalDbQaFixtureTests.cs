using gtas_vpp_test_support;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_shared.Constants;
using BackendPeriodState = gtas_vpp_be.Model.VPP.VppPeriodState;
using SharedVppStatus = gtas_vpp_shared.Enums.VPPStatus;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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
    public async Task Concurrent_period_ensure_creates_one_company_period()
    {
        SkipUnlessOptedIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        var fixture = await LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken);

        try
        {
            const string companyCode = "QA-RACE";
            var period = new Period(2030, 7);
            using var firstUnitOfWork = CreateUnitOfWork(fixture.ConnectionString);
            using var secondUnitOfWork = CreateUnitOfWork(fixture.ConnectionString);
            var clock = new FixedDateTimeProvider(new DateTime(2030, 7, 16, 9, 0, 0));
            var calculator = new PeriodCalculator();
            var policy = new VppRequestPolicy();
            var firstService = new VppPeriodService(
                firstUnitOfWork,
                clock,
                calculator,
                policy,
                NullLogger<VppPeriodService>.Instance);
            var secondService = new VppPeriodService(
                secondUnitOfWork,
                clock,
                calculator,
                policy,
                NullLogger<VppPeriodService>.Instance);

            var results = await Task.WhenAll(
                firstService.EnsureAsync(companyCode, period, cancellationToken),
                secondService.EnsureAsync(companyCode, period, cancellationToken));

            Assert.Equal(results[0].Id, results[1].Id);
            Assert.Equal(1, await ScalarIntAsync(
                fixture.ConnectionString,
                "SELECT COUNT(*) FROM [dbo].[Periods] WHERE [MemberCompanyCode] = N'QA-RACE' AND [Year] = 2030 AND [Month] = 7 AND [IsDeleted] = 0;",
                cancellationToken));
        }
        finally
        {
            await fixture.DisposeAsync();
        }

        Assert.False(await fixture.IsInstancePresentAsync(cancellationToken));
    }

    [Fact]
    public async Task Pending_supplement_index_is_unique_per_user_and_period_without_requiring_a_base()
    {
        SkipUnlessOptedIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        var fixture = await LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken);

        try
        {
            using var unitOfWork = CreateUnitOfWork(fixture.ConnectionString);
            var context = unitOfWork.VPPContext;
            var period = await context.Set<VppPeriod>()
                .SingleAsync(x => x.Id == QaTestData.CurrentPeriodId, cancellationToken);
            var regular = await context.Set<VppRequest>()
                .SingleAsync(x => x.Id == QaTestData.OwnRequestId, cancellationToken);
            var now = DateTime.SpecifyKind(new DateTime(2026, 8, 5, 9, 0, 0), DateTimeKind.Utc);

            context.Set<VppRequest>().Add(CreatePendingSupplement(
                period,
                regular,
                baseRequestId: null,
                baseRequestSeriesId: null,
                attemptNumber: 1,
                now));
            await context.SaveChangesAsync(cancellationToken);

            context.Set<VppRequest>().Add(CreatePendingSupplement(
                period,
                regular,
                baseRequestId: regular.Id,
                baseRequestSeriesId: regular.RequestSeriesId,
                attemptNumber: 2,
                now.AddMinutes(1)));

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync(cancellationToken));
        }
        finally
        {
            await fixture.DisposeAsync();
        }

        Assert.False(await fixture.IsInstancePresentAsync(cancellationToken));
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
        var canonicalGroupIds = string.Join(", ", CanonicalRbac.Personas.Select(persona => $"'{persona.GroupId:D}'"));

        await fixture.EnsureIdentityAsync(cancellationToken);
        return new FixtureSnapshot(
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[__EFMigrationsHistory];", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [sys].[procedures] WHERE [name] = N'sp_Authen_Login';", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[AspNetUsers] WHERE [Id] BETWEEN 1000001001 AND 1000001006 AND [AccountStatus] = N'Active';", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, $"SELECT COUNT(*) FROM [dbo].[PermissionGroups] WHERE [Id] IN ({canonicalGroupIds}) AND [ParentGroupId] IS NULL AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, $"SELECT COUNT(*) FROM [dbo].[PermissionGroups] WHERE [Id] = '{CanonicalRbac.LegacyProcurementAdminGroupId:D}' AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, $"SELECT COUNT(*) FROM [dbo].[UserGroupMemberships] WHERE [PermissionGroupId] = '{CanonicalRbac.LegacyProcurementAdminGroupId:D}' AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[UserGroupMemberships] WHERE [AccountId] BETWEEN 1000001001 AND 1000001006 AND [UserId] = [AccountId] AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[Periods] WHERE [Id] = '20000000-0000-0000-0000-000000000001' AND [MemberCompanyCode] = N'77500' AND [State] = 0 AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, $"SELECT COUNT(*) FROM [dbo].[Periods] WHERE [Id] = '{QaTestData.PreviousSettlementPeriodId:D}' AND [MemberCompanyCode] = N'77500' AND [State] = {(int)BackendPeriodState.Pricing} AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, $"SELECT COUNT(*) FROM [dbo].[Requests] WHERE [Id] = '{QaTestData.SettlementRequestId:D}' AND [PeriodId] = '{QaTestData.PreviousSettlementPeriodId:D}' AND [Status] = {(int)SharedVppStatus.Submitted} AND [IsCurrentRevision] = 1 AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[Requests] WHERE [Id] IN ('40000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000002', '40000000-0000-0000-0000-000000000003');", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[Requests] WHERE [Id] IN ('40000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000002', '40000000-0000-0000-0000-000000000003') AND [PeriodId] IS NOT NULL AND [RequestSeriesId] = [Id] AND [RevisionNumber] = 1 AND [IsCurrentRevision] = 1;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[Requests] WHERE [CreatedByUserId] IN (1000001001, 1000001004, 1000001006) AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[Requests] WHERE [CreatedByUserId] = 1000001001 AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[Requests] WHERE [CreatedByUserId] = 1000001004 AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[Requests] WHERE [CreatedByUserId] = 1000001006 AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[Requests] WHERE [DepartmentCode] = N'QA-D01' AND [IsDeleted] = 0;", cancellationToken),
            await ScalarIntAsync(fixture.ConnectionString, "SELECT COUNT(*) FROM [dbo].[Requests] WHERE [MemberCompanyCode] = N'77500' AND [IsDeleted] = 0;", cancellationToken));
    }

    private static VppRequest CreatePendingSupplement(
        VppPeriod period,
        VppRequest regular,
        Guid? baseRequestId,
        Guid? baseRequestSeriesId,
        int attemptNumber,
        DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            Year = period.Year,
            Month = period.Month,
            PeriodId = period.Id,
            RequestSeriesId = Guid.NewGuid(),
            RevisionNumber = 1,
            IsCurrentRevision = true,
            VppCode = $"QA-SUP-{Guid.NewGuid():N}",
            Status = (int)SharedVppStatus.Pending,
            IsAdditionalOrder = true,
            BaseRequestId = baseRequestId,
            BaseRequestSeriesId = baseRequestSeriesId,
            SupplementAttemptNumber = attemptNumber,
            SupplementSequence = attemptNumber,
            SupplementReason = "Standalone supplement integration check",
            DepartmentCode = regular.DepartmentCode,
            MemberCompanyCode = regular.MemberCompanyCode,
            CreatedByUserId = regular.CreatedByUserId,
            CreatedAtUtc = now,
            UpdatedByUserId = regular.CreatedByUserId,
            UpdatedAtUtc = now,
            SubmittedDate = now
        };

    private static void AssertSnapshot(FixtureSnapshot snapshot)
    {
        Assert.True(snapshot.Migrations > 0);
        Assert.Equal(0, snapshot.LegacyLoginStoredProcedure);
        Assert.Equal(6, snapshot.Accounts);
        Assert.Equal(CanonicalRbac.Personas.Count, snapshot.RequiredRoles);
        Assert.Equal(0, snapshot.ActiveLegacyProcurementGroups);
        Assert.Equal(0, snapshot.ActiveLegacyProcurementMemberships);
        Assert.Equal(6, snapshot.UserRoleMappings);
        Assert.Equal(1, snapshot.CurrentPeriods);
        Assert.Equal(1, snapshot.SettlementPeriods);
        Assert.Equal(1, snapshot.SettlementRequests);
        Assert.Equal(3, snapshot.ScopeRequests);
        Assert.Equal(3, snapshot.PeriodAwareCurrentRevisions);
        Assert.Equal(24, snapshot.PersonaWorkflowRows);
        Assert.Equal(8, snapshot.OwnScopeRows);
        Assert.Equal(8, snapshot.ManagerOwnRows);
        Assert.Equal(8, snapshot.DevOwnRows);
        Assert.Equal(26, snapshot.DepartmentScopeRows);
        Assert.Equal(27, snapshot.CompanyScopeRows);
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

    private static UnitOfWork CreateUnitOfWork(string connectionString)
        => new(new TestDbContextFactory(connectionString));

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
        int LegacyLoginStoredProcedure,
        int Accounts,
        int RequiredRoles,
        int ActiveLegacyProcurementGroups,
        int ActiveLegacyProcurementMemberships,
        int UserRoleMappings,
        int CurrentPeriods,
        int SettlementPeriods,
        int SettlementRequests,
        int ScopeRequests,
        int PeriodAwareCurrentRevisions,
        int PersonaWorkflowRows,
        int OwnScopeRows,
        int ManagerOwnRows,
        int DevOwnRows,
        int DepartmentScopeRows,
        int CompanyScopeRows);

    private sealed class FixedDateTimeProvider(DateTime now) : IDateTimeProvider
    {
        public DateTime Now { get; } = now;
    }

    private sealed class TestDbContextFactory(string connectionString) : IDynamicDbContextFactory
    {
        public VPPContext CreateVPPContext()
        {
            var options = new DbContextOptionsBuilder<VPPContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new VPPContext(options);
        }
    }
}
