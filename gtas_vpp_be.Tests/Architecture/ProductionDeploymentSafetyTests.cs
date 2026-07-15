using Xunit;

namespace gtas_vpp_be.Tests.Architecture;

public sealed class ProductionDeploymentSafetyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void SqlCredentialRecovery_IsRollForwardOnly()
    {
        var deploy = ReadRepositoryFile("deploy", "deploy.sh");

        Assert.Contains("roll_forward_db_password()", deploy, StringComparison.Ordinal);
        Assert.Contains(
            "if ! roll_forward_db_password",
            deploy,
            StringComparison.Ordinal);
        Assert.Contains(
            "reconcile_db_container_secret_metadata",
            deploy,
            StringComparison.Ordinal);
        Assert.Contains(
            "db_container_matches_expected_runtime",
            deploy,
            StringComparison.Ordinal);
        Assert.Contains(
            "elif [[ -n \"$active_db_password\" ]]",
            deploy,
            StringComparison.Ordinal);
        Assert.DoesNotContain("rollback_db_password", deploy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ROLLBACK_DB_PASSWORD", deploy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "NEW_DB_PASSWORD=\"$current_db_password\"",
            deploy,
            StringComparison.Ordinal);
    }

    [Fact]
    public void OperationalBackupEntryPoints_UseTheVerifiedDatabasePair()
    {
        var deploy = ReadRepositoryFile("deploy", "deploy.sh");
        var restore = ReadRepositoryFile("deploy", "restore-db.sh");
        var service = ReadRepositoryFile(
            "deploy",
            "systemd",
            "gtas-vpp-backup.service");

        Assert.Contains(
            "backup-db-pair.sh before-db-reconcile",
            deploy,
            StringComparison.Ordinal);
        Assert.Contains("backup-db-pair.sh pre-deploy", deploy, StringComparison.Ordinal);
        Assert.DoesNotContain("bash deploy/backup-db.sh", deploy, StringComparison.Ordinal);
        Assert.Contains("backup-db-pair.sh\" before-restore", restore, StringComparison.Ordinal);
        Assert.Contains(
            "^${DB_NAME}_[A-Za-z0-9_-]+_[0-9]{8}T[0-9]{6}Z",
            restore,
            StringComparison.Ordinal);
        Assert.Contains("backup-db-pair.sh scheduled", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Restore_QuiescesWritesAndRecoversApplicationsOnFailure()
    {
        var restore = ReadRepositoryFile("deploy", "restore-db.sh");
        var stopIndex = restore.IndexOf("stop frontend backend", StringComparison.Ordinal);
        var backupIndex = restore.IndexOf("backup-db-pair.sh\" before-restore", StringComparison.Ordinal);

        Assert.True(stopIndex >= 0, "Restore must stop application writers.");
        Assert.True(
            backupIndex > stopIndex,
            "The paired before-restore backup must run after application writers stop.");
        Assert.Contains("trap recover_on_exit EXIT", restore, StringComparison.Ordinal);
        Assert.Contains("SET MULTI_USER", restore, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("up -d --wait --wait-timeout 180", restore, StringComparison.Ordinal);
    }

    [Fact]
    public void BackupPair_RequiresAndVerifiesBothProductionDatabases()
    {
        var pair = ReadRepositoryFile("deploy", "backup-db-pair.sh");
        var member = ReadRepositoryFile("deploy", "backup-db.sh");

        Assert.Contains("PRIMARY_DB_NAME:-GTAS_VPP_LIVE", pair, StringComparison.Ordinal);
        Assert.Contains("IDENTITY_DB_NAME:-GTAS_MENU", pair, StringComparison.Ordinal);
        Assert.Contains("BASH_SOURCE[0]", pair, StringComparison.Ordinal);
        Assert.Contains("DB_ID", pair, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COPY_ONLY", member, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CHECKSUM", member, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RESTORE VERIFYONLY", member, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WITH CHECKSUM", member, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BACKUP_RETENTION_DAYS < 1", member, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([RepositoryRoot, .. pathSegments]));

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory);
             current is not null;
             current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "gtas_vpp.sln")))
            {
                return current.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"Cannot find gtas_vpp.sln above {AppContext.BaseDirectory}.");
    }
}
