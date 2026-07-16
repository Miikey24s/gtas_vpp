using System.Text.RegularExpressions;
using gtas_vpp_be.Model.View;
using Xunit;

namespace gtas_vpp_be.Tests.Architecture;

public sealed class CredentialExposureSqlTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void UsersView_DoesNotExposePasswordVerifier()
    {
        var viewsSql = ReadSql("01_Views.sql");
        var usersView = ExtractBatch(viewsSql, "CREATE OR ALTER VIEW dbo.v_Users");

        Assert.DoesNotContain("PasswordChar", usersView, StringComparison.OrdinalIgnoreCase);
        Assert.Null(typeof(v_Users).GetProperty("PasswordChar"));
    }

    [Fact]
    public void PermissionLookup_DoesNotMaterializePasswordVerifier()
    {
        var proceduresSql = ReadSql("02_StoredProcedures.sql");
        var procedure = ExtractBatch(
            proceduresSql,
            "CREATE OR ALTER PROCEDURE [dbo].[sp_Authen_GetPermissionSinglePage]");

        Assert.DoesNotContain("PasswordChar", procedure, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegacyLoginProcedure_IsAbsentFromSeedAndDroppedForUpgrades()
    {
        var proceduresSql = ReadSql("02_StoredProcedures.sql");
        var retirementSql = ReadSql("04_RetireLegacyAuth.sql");

        Assert.DoesNotContain(
            "CREATE OR ALTER PROCEDURE [dbo].[sp_Authen_Login]",
            proceduresSql,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordChar", proceduresSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "DROP PROCEDURE IF EXISTS dbo.sp_Authen_Login",
            retirementSql,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadSql(string fileName) =>
        File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "gtas_vpp_be",
            "gtas_vpp_be.Service",
            "Helpers",
            "SQL",
            fileName));

    private static string ExtractBatch(string sql, string marker)
    {
        var markerIndex = sql.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        Assert.True(markerIndex >= 0, $"Could not find SQL marker '{marker}'.");

        var batchEnd = Regex.Match(
            sql[(markerIndex + marker.Length)..],
            @"^\s*GO\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        Assert.True(batchEnd.Success, $"Could not find GO terminator after '{marker}'.");
        return sql.Substring(markerIndex, marker.Length + batchEnd.Index);
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
