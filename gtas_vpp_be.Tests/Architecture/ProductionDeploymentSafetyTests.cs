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
        Assert.Contains(
            "APP_ENV_ROLL_FORWARD_REQUIRED=false",
            deploy,
            StringComparison.Ordinal);
        Assert.Contains(
            "APP_ENV_ROLL_FORWARD_REQUIRED=true",
            deploy,
            StringComparison.Ordinal);
        Assert.Contains(
            "if [[ \"$DEPLOYING_APPS\" == \"true\" || \"$APP_ENV_ROLL_FORWARD_REQUIRED\" == \"true\" ]]",
            deploy,
            StringComparison.Ordinal);

        var appRecoveryFlagIndex = deploy.IndexOf(
            "APP_ENV_ROLL_FORWARD_REQUIRED=true",
            StringComparison.Ordinal);
        var alterLoginIndex = deploy.IndexOf(
            "ALTER LOGIN [sa]",
            appRecoveryFlagIndex,
            StringComparison.Ordinal);
        Assert.True(
            appRecoveryFlagIndex >= 0 && appRecoveryFlagIndex < alterLoginIndex,
            "Application recovery must be armed before ALTER LOGIN changes the credential.");
        Assert.Contains(
            "if db_can_connect \"$current_db_password\"; then",
            deploy,
            StringComparison.Ordinal);
        Assert.Contains(
            "Previous SQL Server credential was rejected as expected.",
            deploy,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MissingSqlContainer_UsesPinnedLastKnownRuntimeWithoutDestructiveRenameFallback()
    {
        var deploy = ReadRepositoryFile("deploy", "deploy.sh");

        Assert.Contains("read_deploy_state_value()", deploy, StringComparison.Ordinal);
        Assert.Contains("load_last_known_db_runtime()", deploy, StringComparison.Ordinal);
        Assert.Contains(
            "local state_file=\"$APP_ROOT/current/deploy-state.env\"",
            deploy,
            StringComparison.Ordinal);
        Assert.Contains(
            "^sha256:[0-9a-f]{64}$",
            deploy,
            StringComparison.Ordinal);
        Assert.Contains(
            "^[A-Za-z0-9_.-]+$",
            deploy,
            StringComparison.Ordinal);
        Assert.Contains("docker image inspect \"$pinned_image\"", deploy, StringComparison.Ordinal);
        Assert.Contains("docker volume inspect \"$pinned_volume\"", deploy, StringComparison.Ordinal);
        Assert.Contains("export DB_IMAGE DB_DATA_VOLUME", deploy, StringComparison.Ordinal);
        Assert.Contains("DB_RUNTIME_PINNED=false", deploy, StringComparison.Ordinal);
        Assert.Contains("DB_RUNTIME_PINNED=true", deploy, StringComparison.Ordinal);
        Assert.Contains(
            "compose up -d --no-deps --force-recreate db",
            deploy,
            StringComparison.Ordinal);

        Assert.DoesNotContain("source ", deploy, StringComparison.Ordinal);
        Assert.DoesNotContain("docker rename", deploy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "docker rm -f \"$DB_CONTAINER\"",
            deploy,
            StringComparison.Ordinal);

        var loaderDefinitionIndex = deploy.IndexOf(
            "load_last_known_db_runtime()",
            StringComparison.Ordinal);
        var loaderInvocationIndex = deploy.IndexOf(
            "load_last_known_db_runtime",
            loaderDefinitionIndex + "load_last_known_db_runtime()".Length,
            StringComparison.Ordinal);
        var composeValidationIndex = deploy.IndexOf(
            "compose config --quiet",
            loaderInvocationIndex,
            StringComparison.Ordinal);
        var missingContainerComposeIndex = deploy.IndexOf(
            "compose up -d --no-deps db",
            composeValidationIndex,
            StringComparison.Ordinal);

        Assert.True(loaderInvocationIndex > loaderDefinitionIndex,
            "The missing-container branch must invoke the pinned-runtime loader.");
        Assert.True(composeValidationIndex > loaderInvocationIndex,
            "Compose validation must run only after the pinned DB image and volume are loaded.");
        Assert.True(missingContainerComposeIndex > composeValidationIndex,
            "The missing DB container must be created only after pinned-runtime validation.");

        var ensureContainerIndex = deploy.IndexOf(
            "ensure_desired_db_container()",
            StringComparison.Ordinal);
        var ensureContainerEndIndex = deploy.IndexOf(
            "\n}",
            ensureContainerIndex,
            StringComparison.Ordinal);
        var ensureContainerBody = deploy[ensureContainerIndex..ensureContainerEndIndex];
        var pinnedGuardIndex = ensureContainerBody.IndexOf(
            "require_pinned_db_runtime",
            StringComparison.Ordinal);
        var recoveryComposeIndex = ensureContainerBody.IndexOf(
            "compose up -d --no-deps db",
            StringComparison.Ordinal);

        Assert.True(pinnedGuardIndex >= 0 && pinnedGuardIndex < recoveryComposeIndex,
            "Error recovery must refuse Compose mutation until the DB runtime is pinned.");

        var pinnedGuardDefinitionIndex = deploy.IndexOf(
            "require_pinned_db_runtime()",
            StringComparison.Ordinal);
        var pinnedGuardDefinitionEndIndex = deploy.IndexOf(
            "\n}",
            pinnedGuardDefinitionIndex,
            StringComparison.Ordinal);
        var pinnedGuardBody = deploy[pinnedGuardDefinitionIndex..pinnedGuardDefinitionEndIndex];
        Assert.Contains("DB_RUNTIME_PINNED", pinnedGuardBody, StringComparison.Ordinal);
        Assert.Contains("docker image inspect \"$DB_IMAGE\"", pinnedGuardBody, StringComparison.Ordinal);
        Assert.Contains("docker volume inspect \"$DB_DATA_VOLUME\"", pinnedGuardBody, StringComparison.Ordinal);
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
        var stopIndex = restore.IndexOf(
            "stop react-frontend frontend backend",
            StringComparison.Ordinal);
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
    public void PairedRestore_RestoresBothDatabasesOrKeepsWritersStopped()
    {
        var restorePair = ReadRepositoryFile("deploy", "restore-db-pair.sh");

        Assert.Contains("RESTORE_PAIR_CONFIRM", restorePair, StringComparison.Ordinal);
        Assert.Contains("PRIMARY_DB_NAME:-GTAS_VPP_LIVE", restorePair, StringComparison.Ordinal);
        Assert.Contains("IDENTITY_DB_NAME:-GTAS_MENU", restorePair, StringComparison.Ordinal);
        Assert.Contains(
            "stop react-frontend frontend backend",
            restorePair,
            StringComparison.Ordinal);
        Assert.Contains("backup-db-pair.sh\" before-pair-restore", restorePair, StringComparison.Ordinal);
        Assert.Contains("RESTORE VERIFYONLY", restorePair, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WITH CHECKSUM", restorePair, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("restore_database \"$PRIMARY_DB_NAME\"", restorePair, StringComparison.Ordinal);
        Assert.Contains("restore_database \"$IDENTITY_DB_NAME\"", restorePair, StringComparison.Ordinal);
        Assert.Contains("application writers remain stopped", restorePair, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("up -d --wait --wait-timeout 180", restorePair, StringComparison.Ordinal);
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
