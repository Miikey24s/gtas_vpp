using gtas_vpp_be.Service.Helpers;
using gtas_vpp_test_support;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace gtas_vpp_be.IntegrationTests;

public sealed class QaSafetyContractTests
{
    private const string RunId = "ABCDEF123456";

    [Fact]
    public void Generated_fixture_identity_is_accepted()
    {
        var options = QaFixtureOptions.Create(RunId);

        QaFixtureIdentityContract.ValidateConnectionString(RunId, options.ConnectionString);

        Assert.Equal($"GTASVPP_QA_{RunId}", options.InstanceName);
        Assert.Equal($"GTAS_VPP_TEST_QA_{RunId}", options.DatabaseName);
    }

    [Theory]
    [MemberData(nameof(UnsafeConnectionStrings))]
    public void Unsafe_or_mismatched_database_identity_is_rejected(string connectionString)
    {
        Assert.Throws<InvalidOperationException>(
            () => QaFixtureIdentityContract.ValidateConnectionString(RunId, connectionString));
    }

    [Fact]
    public void Cleanup_policy_rejects_unexpected_or_orphan_companion_database()
    {
        var options = QaFixtureOptions.Create(RunId);

        Assert.Throws<InvalidOperationException>(
            () => QaCleanupSafetyContract.ValidateUserDatabases(
                options,
                [options.DatabaseName, QaCleanupSafetyContract.CompanionDatabaseName, "SHARED_DATA"]));
        Assert.Throws<InvalidOperationException>(
            () => QaCleanupSafetyContract.ValidateUserDatabases(
                options,
                [QaCleanupSafetyContract.CompanionDatabaseName]));

        var valid = QaCleanupSafetyContract.ValidateUserDatabases(
            options,
            [options.DatabaseName, QaCleanupSafetyContract.CompanionDatabaseName]);
        Assert.True(valid.HasMain);
        Assert.True(valid.HasCompanion);
    }

    [Fact]
    public void Authenticated_and_mutating_UI_runs_fail_closed_without_exact_opt_ins()
    {
        Assert.Throws<InvalidOperationException>(
            () => QaUiSafetyContract.EnsureRunAllowed(true, false, null, null));
        Assert.Throws<InvalidOperationException>(
            () => QaUiSafetyContract.EnsureRunAllowed(true, true, "1", "yes"));

        QaUiSafetyContract.EnsureRunAllowed(
            true,
            true,
            "1",
            QaUiSafetyContract.RequiredMutationOptIn);
    }

    [Fact]
    public void UI_endpoint_guard_accepts_loopback_only()
    {
        QaUiSafetyContract.EnsureLoopbackUrl(new Uri("https://localhost:7123"), "backend");
        QaUiSafetyContract.EnsureLoopbackUrl(new Uri("http://127.0.0.1:5000"), "frontend");

        Assert.Throws<InvalidOperationException>(
            () => QaUiSafetyContract.EnsureLoopbackUrl(new Uri("https://demo.example.com"), "backend"));
        Assert.Throws<InvalidOperationException>(
            () => QaUiSafetyContract.EnsureLoopbackUrl(new Uri("ftp://localhost:2121"), "backend"));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public void Enabled_fixture_is_rejected_outside_the_testing_host(string environmentName)
    {
        var options = QaFixtureOptions.Create(RunId);
        var configuration = BuildConfiguration(
            DatabaseBinding.TestEnvironment,
            options.ConnectionString,
            fixtureEnabled: true);
        var binding = DatabaseBinding.Create(configuration);

        Assert.Throws<InvalidOperationException>(() => QaFixtureIdentityContract.Resolve(
            new StubHostEnvironment(environmentName),
            configuration,
            binding));
    }

    [Fact]
    public void Enabled_fixture_is_rejected_for_a_live_database_binding()
    {
        var options = QaFixtureOptions.Create(RunId);
        var configuration = BuildConfiguration(
            DatabaseBinding.LiveEnvironment,
            options.ConnectionString,
            fixtureEnabled: true);
        var binding = DatabaseBinding.Create(configuration);

        Assert.Throws<InvalidOperationException>(() => QaFixtureIdentityContract.Resolve(
            new StubHostEnvironment(QaFixtureIdentityContract.HostEnvironment),
            configuration,
            binding));
    }

    [Fact]
    public void Disabled_fixture_does_not_expose_an_identity_in_a_normal_host()
    {
        var options = QaFixtureOptions.Create(RunId);
        var configuration = BuildConfiguration(
            DatabaseBinding.TestEnvironment,
            options.ConnectionString,
            fixtureEnabled: false);
        var binding = DatabaseBinding.Create(configuration);

        Assert.Null(QaFixtureIdentityContract.Resolve(
            new StubHostEnvironment(Environments.Production),
            configuration,
            binding));
    }

    public static TheoryData<string> UnsafeConnectionStrings()
    {
        var data = new TheoryData<string>();
        data.Add(Build("sql.example.com", $"GTAS_VPP_TEST_QA_{RunId}"));
        data.Add(Build("(localdb)\\MSSQLLocalDB", $"GTAS_VPP_TEST_QA_{RunId}"));
        data.Add(Build($"(localdb)\\GTASVPP_QA_{RunId}", "GTAS_VPP_LIVE"));
        data.Add(Build("(localdb)\\GTASVPP_QA_000000000000", $"GTAS_VPP_TEST_QA_{RunId}"));
        data.Add(
            $"Server=(localdb)\\GTASVPP_QA_{RunId};Database=GTAS_VPP_TEST_QA_{RunId};User Id=sa;Password=not-a-real-secret;");
        data.Add(
            $"Server=(localdb)\\GTASVPP_QA_{RunId};Database=GTAS_VPP_TEST_QA_{RunId};Integrated Security=true;AttachDbFilename=C:\\temp\\qa.mdf;");
        return data;
    }

    private static string Build(string dataSource, string databaseName)
    {
        return new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = databaseName,
            IntegratedSecurity = true
        }.ConnectionString;
    }

    private static IConfiguration BuildConfiguration(
        string databaseEnvironment,
        string connectionString,
        bool fixtureEnabled)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultEnvironment"] = databaseEnvironment,
                [$"ConnectionStrings:{databaseEnvironment}"] = connectionString,
                [$"{QaFixtureIdentityContract.ConfigurationSection}:Enabled"] = fixtureEnabled.ToString(),
                [$"{QaFixtureIdentityContract.ConfigurationSection}:RunId"] = RunId
            })
            .Build();
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "GTAS VPP QA fixture contract tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
