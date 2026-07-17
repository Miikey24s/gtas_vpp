using Xunit;

namespace gtas_vpp_be.Tests.Architecture;

public sealed class TrustedAccessMigrationSafetyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Migration_ContainsFailClosedUpgradePreflightAndDataPreservingDownGuard()
    {
        var migration = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "gtas_vpp_be",
            "gtas_vpp_be.Migrations",
            "Migrations",
            "20260715160616_AddTrustedAccessIdentity.cs"));

        Assert.Contains("AUTH_IDENTITY_ID_RANGE_COLLISION", migration, StringComparison.Ordinal);
        Assert.Contains("AUTH_ACTIVE_LEGACY_MEMBERSHIP", migration, StringComparison.Ordinal);
        Assert.Contains("AUTH_UNKNOWN_ACTIVE_GROUP", migration, StringComparison.Ordinal);
        Assert.Contains("UX_P04_UserGroup_OneActivePerUser", migration, StringComparison.Ordinal);
        Assert.Contains("CK_P04_UserGroup_ActivePrimaryDepartment", migration, StringComparison.Ordinal);

        var downStart = migration.IndexOf(
            "protected override void Down",
            StringComparison.Ordinal);
        Assert.True(downStart >= 0);
        Assert.Contains(
            "AUTH_DESTRUCTIVE_DOWN_BLOCKED",
            migration[downStart..],
            StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory);
             current != null;
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
