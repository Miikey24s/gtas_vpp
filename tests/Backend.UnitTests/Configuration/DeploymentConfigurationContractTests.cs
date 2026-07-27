using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace gtas_vpp_be.Tests.Configuration;

public sealed class DeploymentConfigurationContractTests
{
    private const string TestConnection =
        "Server=localhost;Database=GTAS_VPP_TEST;User Id=test;Password=not-used;Encrypt=False";
    private const string LiveConnection =
        "Server=live.invalid;Database=GTAS_VPP_LIVE;User Id=test;Password=not-used;Encrypt=True";
    private const string JwtKey = "ENV001_JWT_CONFIGURATION_TEST_KEY_0123456789";

    [Fact]
    public void Create_RequiresExplicitEnvironment()
    {
        var configuration = BuildConfiguration(("ConnectionStrings:TestEnv", TestConnection));

        Assert.Throws<InvalidOperationException>(() => DatabaseBinding.Create(configuration));
    }

    [Fact]
    public void Create_RequiresSelectedConnection()
    {
        var configuration = BuildConfiguration(("DatabaseSettings:DefaultEnvironment", "TestEnv"));

        Assert.Throws<InvalidOperationException>(() => DatabaseBinding.Create(configuration));
    }

    [Fact]
    public void Create_RejectsOppositeConnectionEvenWhenSelectedConnectionExists()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", TestConnection),
            ("ConnectionStrings:LiveEnv", LiveConnection));

        Assert.Throws<InvalidOperationException>(() => DatabaseBinding.Create(configuration));
    }

    [Theory]
    [InlineData("Server=localhost", "TestEnv")]
    [InlineData("UnsupportedKeyword=value", "TestEnv")]
    public void Create_RejectsIncompleteOrMalformedConnection(
        string connectionString,
        string environmentName)
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", environmentName),
            ($"ConnectionStrings:{environmentName}", connectionString));

        Assert.Throws<InvalidOperationException>(() => DatabaseBinding.Create(configuration));
    }

    [Fact]
    public void Create_MalformedConnectionErrorDoesNotExposeSecret()
    {
        const string sentinel = "ENV001_PASSWORD_MUST_NOT_LEAK";
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv",
                $"Server=localhost;Database=GTAS_VPP_TEST;Password={sentinel};UnsupportedKeyword=value"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => DatabaseBinding.Create(configuration));

        Assert.DoesNotContain(sentinel, exception.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, "gtas_vpp_be", "gtas_vpp_test_clients")]
    [InlineData("short", "gtas_vpp_be", "gtas_vpp_test_clients")]
    [InlineData(JwtKey, null, "gtas_vpp_test_clients")]
    [InlineData(JwtKey, "gtas_vpp_be", null)]
    public void JwtSettings_RejectMissingOrWeakRequiredValuesWithoutLeakingKey(
        string? key,
        string? issuer,
        string? audience)
    {
        const string sentinel = "ENV001_SENTINEL_KEY_MUST_NOT_LEAK_0123456789";
        var effectiveKey = key == JwtKey ? sentinel : key;
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", TestConnection),
            ("JwtSettings:Key", effectiveKey),
            ("JwtSettings:Issuer", issuer),
            ("JwtSettings:Audience", audience));
        var binding = DatabaseBinding.Create(configuration);

        var exception = Assert.Throws<InvalidOperationException>(
            () => JwtDeploymentSettings.Create(configuration, binding));

        Assert.DoesNotContain(sentinel, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Create_NormalizesExplicitTestBinding()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", " testenv "),
            ("ConnectionStrings:TestEnv", TestConnection));

        var binding = DatabaseBinding.Create(configuration);

        Assert.Equal(DatabaseBinding.TestEnvironment, binding.EnvironmentName);
        Assert.Equal("localhost", binding.DataSource);
        Assert.Equal("GTAS_VPP_TEST", binding.DatabaseName);
        Assert.True(binding.IsTestEnvironment);
    }

    [Fact]
    public void InitializationTargets_DefaultToTheImmutableBindingWhenInitializationIsDisabled()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", TestConnection));
        var binding = DatabaseBinding.Create(configuration);

        var targets = DeploymentConfigurationContract.GetDatabaseInitializationEnvironments(
            configuration,
            binding,
            requireExplicitTarget: false);

        Assert.Equal([DatabaseBinding.TestEnvironment], targets);
    }

    [Fact]
    public void InitializationTargets_AcceptOneExplicitMatchingBinding()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", TestConnection),
            ("DatabaseInitialization:Environments:0", "TestEnv"));
        var binding = DatabaseBinding.Create(configuration);

        var targets = DeploymentConfigurationContract.GetDatabaseInitializationEnvironments(
            configuration,
            binding,
            requireExplicitTarget: true);

        Assert.Equal([DatabaseBinding.TestEnvironment], targets);
    }

    [Theory]
    [InlineData("LiveEnv", null)]
    [InlineData("TestEnv", "LiveEnv")]
    [InlineData("TestEnv", "TestEnv")]
    public void InitializationTargets_RejectAnythingBeyondTheBinding(
        string firstTarget,
        string? secondTarget)
    {
        var values = new List<(string Key, string? Value)>
        {
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", TestConnection),
            ("DatabaseInitialization:Environments:0", firstTarget)
        };
        if (secondTarget is not null)
        {
            values.Add(("DatabaseInitialization:Environments:1", secondTarget));
        }

        var configuration = BuildConfiguration(values.ToArray());
        var binding = DatabaseBinding.Create(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            DeploymentConfigurationContract.GetDatabaseInitializationEnvironments(
                configuration,
                binding,
                requireExplicitTarget: true));
    }

    [Fact]
    public void InitializationTargets_RequireExplicitBindingWhenMigrationIsEnabled()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", TestConnection));
        var binding = DatabaseBinding.Create(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            DeploymentConfigurationContract.GetDatabaseInitializationEnvironments(
                configuration,
                binding,
                requireExplicitTarget: true));
    }

    [Fact]
    public void HostAndAudience_MustMatchTheBinding()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "LiveEnv"),
            ("ConnectionStrings:LiveEnv", LiveConnection),
            ("JwtSettings:Key", JwtKey),
            ("JwtSettings:Issuer", "gtas_vpp_be"),
            ("JwtSettings:Audience", "gtas_vpp_live_clients"));
        var binding = DatabaseBinding.Create(configuration);

        DeploymentConfigurationContract.ValidateDatabaseBindingForHost(
            "Production",
            isProduction: true,
            binding);
        var jwtSettings = JwtDeploymentSettings.Create(configuration, binding);
        Assert.Equal("gtas_vpp_live_clients", jwtSettings.Audience);

        Assert.Throws<InvalidOperationException>(() =>
            DeploymentConfigurationContract.ValidateDatabaseBindingForHost(
                "Development",
                isProduction: false,
                binding));
        var invalidJwtConfiguration = BuildConfiguration(
            ("JwtSettings:Key", JwtKey),
            ("JwtSettings:Issuer", "gtas_vpp_be"),
            ("JwtSettings:Audience", "gtas_vpp_test_clients"));
        Assert.Throws<InvalidOperationException>(() =>
            JwtDeploymentSettings.Create(invalidJwtConfiguration, binding));
    }

    [Fact]
    public void TestHostAndAudience_MustMatchTheBinding()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", TestConnection),
            ("JwtSettings:Key", JwtKey),
            ("JwtSettings:Issuer", "gtas_vpp_be"),
            ("JwtSettings:Audience", "gtas_vpp_test_clients"));
        var binding = DatabaseBinding.Create(configuration);

        DeploymentConfigurationContract.ValidateDatabaseBindingForHost(
            "Development",
            isProduction: false,
            binding);
        var jwtSettings = JwtDeploymentSettings.Create(configuration, binding);
        Assert.Equal("gtas_vpp_test_clients", jwtSettings.Audience);

        Assert.Throws<InvalidOperationException>(() =>
            DeploymentConfigurationContract.ValidateDatabaseBindingForHost(
                "Production",
                isProduction: true,
                binding));
        var invalidJwtConfiguration = BuildConfiguration(
            ("JwtSettings:Key", JwtKey),
            ("JwtSettings:Issuer", "gtas_vpp_be"),
            ("JwtSettings:Audience", "gtas_vpp_live_clients"));
        Assert.Throws<InvalidOperationException>(() =>
            JwtDeploymentSettings.Create(invalidJwtConfiguration, binding));
    }

    [Fact]
    public void JwtAudience_PreventsTestTokenReplayAgainstLiveValidation()
    {
        const string issuer = "gtas_vpp_be";
        const string testAudience = "gtas_vpp_test_clients";
        const string liveAudience = "gtas_vpp_live_clients";
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("ENV001_SHARED_SIGNING_KEY_TEST_ONLY_0123456789"));
        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")]),
            Issuer = issuer,
            Audience = testAudience,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        }));

        var matchingParameters = CreateTokenValidationParameters(issuer, testAudience, key);
        var principal = handler.ValidateToken(token, matchingParameters, out _);

        Assert.Equal("1", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Throws<SecurityTokenInvalidAudienceException>(() =>
            handler.ValidateToken(
                token,
                CreateTokenValidationParameters(issuer, liveAudience, key),
                out _));
    }

    [Fact]
    public void TestBinding_RejectsProductionPhysicalDatabaseName()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", LiveConnection));
        var binding = DatabaseBinding.Create(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            DeploymentConfigurationContract.ValidateDatabaseBindingForHost(
                "Development",
                isProduction: false,
                binding));
    }

    [Fact]
    public void ProductionLiveBinding_RejectsTestPhysicalDatabaseName()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "LiveEnv"),
            ("ConnectionStrings:LiveEnv", TestConnection));
        var binding = DatabaseBinding.Create(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            DeploymentConfigurationContract.ValidateDatabaseBindingForHost(
                "Production",
                isProduction: true,
                binding));
    }

    [Theory]
    [InlineData("GTAS_VPP_DEV")]
    [InlineData("LATEST_PROD")]
    [InlineData("CONTEST_DATA")]
    public void TestBinding_RejectsDatabaseWithoutTestOrDemoToken(string databaseName)
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv",
                $"Server=localhost;Database={databaseName};Integrated Security=True"));
        var binding = DatabaseBinding.Create(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            DeploymentConfigurationContract.ValidateDatabaseBindingForHost(
                "Development",
                isProduction: false,
                binding));
    }

    [Theory]
    [InlineData("Server=localhost;Database=GTAS_VPP_TEST;Integrated Security=True")]
    [InlineData("Server=127.0.0.1,1433;Database=GTAS_VPP_TEST;User Id=test;Password=not-used")]
    [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=GTAS_ENV001_DEMO;Integrated Security=True")]
    [InlineData("Server=db,1433;Database=GTAS_VPP_TEST;User Id=test;Password=not-used")]
    public void DemoTarget_AllowsExplicitLocalDisposableTestDatabase(string connectionString)
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", connectionString));
        var binding = DatabaseBinding.Create(configuration);

        DeploymentConfigurationContract.ValidateDemoDatabaseTarget(
            DatabaseInitializationMode.MigrateAndDemo,
            binding);
    }

    [Theory]
    [InlineData("Server=developer-sql;Database=GTAS_VPP_TEST;Integrated Security=True")]
    [InlineData("Server=sql.internal;Database=GTAS_ENV001_DEMO;Integrated Security=True")]
    [InlineData("Server=localhost;Database=GTAS_VPP_DEV;Integrated Security=True")]
    public void DemoTarget_RejectsRemoteAliasOrDatabaseWithoutDisposableMarker(string connectionString)
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", connectionString));
        var binding = DatabaseBinding.Create(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            DeploymentConfigurationContract.ValidateDemoDatabaseTarget(
                DatabaseInitializationMode.MigrateAndDemo,
                binding));
    }

    [Fact]
    public void DemoTarget_RejectsLiveBindingEvenWhenPhysicalTargetLooksDisposableAndLocal()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "LiveEnv"),
            ("ConnectionStrings:LiveEnv", "Server=localhost;Database=GTAS_VPP_DEMO;Integrated Security=True"));
        var binding = DatabaseBinding.Create(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            DeploymentConfigurationContract.ValidateDemoDatabaseTarget(
                DatabaseInitializationMode.MigrateAndDemo,
                binding));
    }

    [Fact]
    public void DemoTargetValidation_DoesNotRestrictReferenceInitialization()
    {
        var configuration = BuildConfiguration(
            ("DatabaseSettings:DefaultEnvironment", "TestEnv"),
            ("ConnectionStrings:TestEnv", "Server=developer-machine;Database=GTAS_VPP_DEV;Integrated Security=True"));
        var binding = DatabaseBinding.Create(configuration);

        DeploymentConfigurationContract.ValidateDemoDatabaseTarget(
            DatabaseInitializationMode.MigrateAndReference,
            binding);
    }

    [Fact]
    public void RepositoryConfiguration_UsesOneEnvironmentSpecificBinding()
    {
        var repositoryRoot = FindRepositoryRoot();
        var localCompose = File.ReadAllText(Path.Combine(repositoryRoot, "docker-compose.yml"));
        var productionCompose = File.ReadAllText(Path.Combine(repositoryRoot, "docker-compose.prod.yml"));
        var appHost = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Hosting", "AppHost", "AppHost.cs"));
        var backendProgram = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Backend",
            "Api",
            "Program.cs"));
        var developmentSettings = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Backend",
            "Api",
            "appsettings.Development.json"));

        Assert.Contains("DatabaseSettings__DefaultEnvironment: \"TestEnv\"", localCompose);
        Assert.Contains("ConnectionStrings__TestEnv:", localCompose);
        Assert.DoesNotContain("ConnectionStrings__LiveEnv:", localCompose);
        Assert.Contains("Database=GTAS_VPP_TEST", localCompose);
        Assert.DoesNotContain("Database=GTAS_VPP_LIVE", localCompose);
        Assert.Contains("JwtSettings__Audience: \"gtas_vpp_test_clients\"", localCompose);

        Assert.Contains("DatabaseSettings__DefaultEnvironment: \"LiveEnv\"", productionCompose);
        Assert.Contains("ConnectionStrings__LiveEnv:", productionCompose);
        Assert.DoesNotContain("ConnectionStrings__TestEnv:", productionCompose);
        Assert.Contains("JwtSettings__Audience: \"gtas_vpp_live_clients\"", productionCompose);
        Assert.DoesNotContain("DatabaseInitialization__AllowDemoData", productionCompose);
        var productionConnectionBindings = productionCompose
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("ConnectionStrings__LiveEnv:", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(2, productionConnectionBindings.Length);
        Assert.Single(productionConnectionBindings.Distinct(StringComparer.Ordinal));

        Assert.Contains("AddParameter(\"test-database-connection-string\", secret: true)", appHost);
        Assert.Contains("WithEnvironment(\"ConnectionStrings__TestEnv\"", appHost);
        Assert.Contains("WithEnvironment(\"JwtSettings__Audience\", \"gtas_vpp_test_clients\")", appHost);

        Assert.DoesNotContain("\"ConnectionStrings\"", developmentSettings);
        Assert.Contains("\"Audience\": \"gtas_vpp_test_clients\"", developmentSettings);

        Assert.Contains("DatabaseBinding.Create(Configuration)", backendProgram);
        Assert.Contains("databaseBinding.ConnectionString", backendProgram);
        Assert.DoesNotContain("LoadDotEnvValues", backendProgram);
        Assert.DoesNotContain("GetLocalDevelopmentConnectionOverrides", backendProgram);
        Assert.DoesNotContain("DISABLE_DOCKER_DB_OVERRIDE", backendProgram);
    }

    private static IConfiguration BuildConfiguration(params (string Key, string? Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(item => item.Key, item => item.Value))
            .Build();
    }

    private static TokenValidationParameters CreateTokenValidationParameters(
        string issuer,
        string audience,
        SecurityKey key)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
