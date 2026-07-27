using Xunit;

namespace gtas_vpp_be.Tests.Architecture;

public sealed class SecretScanConfigurationTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ProjectScopedMcpConfiguration_IsAbsentAndIgnored()
    {
        var trackedMcpPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Frontend",
            "Blazor",
            ".mcp.json");
        var gitIgnore = File.ReadAllLines(Path.Combine(RepositoryRoot, ".gitignore"))
            .Select(line => line.Trim())
            .ToHashSet(StringComparer.Ordinal);

        Assert.False(
            File.Exists(trackedMcpPath),
            "Project-scoped MCP configuration must not contain a tracked provider key.");
        Assert.Contains(".mcp.json", gitIgnore);
    }

    [Fact]
    public void DockerContext_ExcludesLocalSecretAndScannerArtifacts()
    {
        var dockerIgnore = File.ReadAllLines(Path.Combine(RepositoryRoot, ".dockerignore"))
            .Select(line => line.Trim())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("**/.vscode", dockerIgnore);
        Assert.Contains("**/.mcp.json", dockerIgnore);
        Assert.Contains("/tools", dockerIgnore);
    }

    [Fact]
    public void ContinuousIntegration_UsesPinnedCurrentTreeGateOnly()
    {
        var workflow = ReadRepositoryFile(".github", "workflows", "ci.yml");
        var scanner = ReadRepositoryFile("scripts", "security", "Invoke-Gitleaks.ps1");

        Assert.Contains("secret-scan:", workflow, StringComparison.Ordinal);
        Assert.Contains("Invoke-Gitleaks.ps1 -Mode Current", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("-Mode History", workflow, StringComparison.Ordinal);

        Assert.Contains("$gitleaksVersion = \"8.30.1\"", scanner, StringComparison.Ordinal);
        Assert.Contains("Get-FileHash", scanner, StringComparison.Ordinal);
        Assert.Contains("--redact=100", scanner, StringComparison.Ordinal);
        Assert.Contains("--ignore-gitleaks-allow", scanner, StringComparison.Ordinal);
        Assert.Contains(
            "if ($Mode -eq \"History\" -and -not $PostRevocation)",
            scanner,
            StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([RepositoryRoot, .. pathSegments]));

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory);
             current is not null;
             current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "gtas_vpp.slnx")))
            {
                return current.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"Cannot find gtas_vpp.slnx above {AppContext.BaseDirectory}.");
    }
}
