using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class UiMotifCatalogTests
{
    [Fact]
    public void EveryLogicalRouteHasExactlyOneTypedUiProfile()
    {
        var routeKeys = RouteCatalog.Authenticated
            .Concat(RouteCatalog.Anonymous)
            .Select(route => route.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var profileKeys = UiRouteCatalog.Profiles.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(routeKeys, profileKeys);
        Assert.Equal(routeKeys.Count, UiRouteCatalog.Profiles.Count);
    }

    [Fact]
    public void ProfilesDeclareCompositionMotifsAndStateCoverage()
    {
        Assert.All(UiRouteCatalog.Profiles.Values, profile =>
        {
            Assert.False(string.IsNullOrWhiteSpace(profile.RouteKey));
            Assert.NotEmpty(profile.Motifs);
            Assert.NotEmpty(profile.States);
            Assert.Contains(profile.Motifs, motif => motif is "SHELL-NAV" or "ACCOUNT" or "COLLECTION" or "LIST-DETAIL" or "SPLIT-EDITOR" or "OPERATION" or "ANALYTICS" or "CONTENT-STATE");
        });
    }

    [Fact]
    public void MotifAuthorityIsLinkedFromTheUiSkillAndExecutionDocs()
    {
        var root = FindRepositoryRoot();
        var skill = File.ReadAllText(Path.Combine(root, ".agents", "skills", "gtas-vpp-ui-system", "SKILL.md"));
        var execution = File.ReadAllText(Path.Combine(root, "docs", "execution", "UI-SYSTEM-001.md"));
        var catalogPath = Path.Combine(root, "docs", "design", "VPP-UI-MOTIF-CATALOG.md");

        Assert.True(File.Exists(catalogPath));
        Assert.Contains("VPP-UI-MOTIF-CATALOG.md", skill, StringComparison.Ordinal);
        Assert.Contains("VPP-UI-MOTIF-CATALOG.md", execution, StringComparison.Ordinal);
    }

    [Fact]
    public void RetiredUiAdaptersAndLegacySelectorsCannotReturn()
    {
        var root = FindRepositoryRoot();
        var frontend = Path.Combine(root, "src", "Frontend", "Blazor");
        var retiredFiles = new[]
        {
            Path.Combine(frontend, "wwwroot", "app.css"),
            Path.Combine(frontend, "wwwroot", "css", "vpp-status-badge.css"),
            Path.Combine(frontend, "Components", "Shared", "KpiCard.razor"),
            Path.Combine(frontend, "Components", "Shared", "KpiVariant.cs"),
            Path.Combine(frontend, "Components", "Shared", "StatusBadge.razor"),
            Path.Combine(frontend, "Components", "Shared", "VppAccountShell.razor"),
            Path.Combine(frontend, "Components", "Shared", "VppMobileCard.razor"),
            Path.Combine(frontend, "Helpers", "StatusDisplayRadzen.cs")
        };

        Assert.All(retiredFiles, path => Assert.False(File.Exists(path), $"Retired UI file returned: {path}"));

        var authoredSource = string.Join("\n", Directory
            .EnumerateFiles(frontend, "*.*", SearchOption.AllDirectories)
            .Where(path => new[] { ".razor", ".cs", ".css", ".js" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText));

        foreach (var retiredIdentifier in new[]
                 {
                     "librariestab",
                     "VppAccountShell",
                     "VppMobileCard",
                     "KpiVariant",
                     "StatusDisplayRadzen",
                     "GetCssClass(",
                     "<RadzenBadge",
                     "<RadzenAlert",
                     "BadgeStyle.",
                     "class=\"kpi-card",
                     "class=\"vpp-badge",
                     "class=\"shimmer"
                 })
        {
            Assert.DoesNotContain(retiredIdentifier, authoredSource, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
