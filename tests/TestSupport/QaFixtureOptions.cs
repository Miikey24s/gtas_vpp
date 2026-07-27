using gtas_vpp_be.Service.Helpers;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

namespace gtas_vpp_test_support;

public sealed class QaFixtureOptions
{
    private QaFixtureOptions(string runId, string manifestDirectory)
    {
        RunId = runId;
        InstanceName = $"GTASVPP_QA_{runId}";
        DatabaseName = $"GTAS_VPP_TEST_QA_{runId}";
        DataSource = $"(localdb)\\{InstanceName}";
        ManifestDirectory = Path.GetFullPath(manifestDirectory);
        DataDirectory = Path.Combine(ManifestDirectory, "data", InstanceName);

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = DataSource,
            InitialCatalog = DatabaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            Pooling = false,
            ConnectTimeout = 30
        };
        ConnectionString = builder.ConnectionString;
        builder.InitialCatalog = "master";
        MasterConnectionString = builder.ConnectionString;

        QaFixtureIdentityContract.ValidateConnectionString(RunId, ConnectionString);
    }

    public string RunId { get; }

    public string InstanceName { get; }

    public string DatabaseName { get; }

    public string DataSource { get; }

    public string ConnectionString { get; }

    public string MasterConnectionString { get; }

    public string ManifestDirectory { get; }

    public string DataDirectory { get; }

    public string ManifestPath => Path.Combine(ManifestDirectory, $"{InstanceName}.json");

    public static QaFixtureOptions Create(
        string? runId = null,
        string? manifestDirectory = null)
    {
        var normalizedRunId = string.IsNullOrWhiteSpace(runId)
            ? Convert.ToHexString(RandomNumberGenerator.GetBytes(6))
            : runId.Trim().ToUpperInvariant();
        var resolvedManifestDirectory = manifestDirectory
            ?? Path.Combine(Path.GetTempPath(), "gtas-vpp-qa");

        return new QaFixtureOptions(normalizedRunId, resolvedManifestDirectory);
    }

    internal static QaFixtureOptions FromManifest(QaFixtureManifest manifest, string manifestDirectory)
    {
        var options = Create(manifest.RunId, manifestDirectory);
        if (!string.Equals(options.InstanceName, manifest.InstanceName, StringComparison.Ordinal)
            || !string.Equals(options.DatabaseName, manifest.DatabaseName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("QA fixture manifest identity is inconsistent.");
        }

        return options;
    }
}

public sealed class QaFixtureSecrets
{
    private QaFixtureSecrets(string accountPassword, string jwtKey)
    {
        AccountPassword = accountPassword;
        JwtKey = jwtKey;
    }

    public string AccountPassword { get; }

    public string JwtKey { get; }

    public static QaFixtureSecrets Create()
    {
        return new QaFixtureSecrets(
            $"Qa!{Convert.ToHexString(RandomNumberGenerator.GetBytes(12))}",
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
    }

    public override string ToString() => "QA fixture secrets: [REDACTED]";
}
