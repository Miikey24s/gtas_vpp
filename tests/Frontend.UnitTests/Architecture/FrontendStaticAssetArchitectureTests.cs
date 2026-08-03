using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class FrontendStaticAssetArchitectureTests
{
    [Fact]
    public void LegacyProductionAssets_AreNotServed()
    {
        var root = GetFrontendRoot();
        var legacyAssets = new[]
        {
            "wwwroot/favicon.png",
            "wwwroot/logoppj-new.png",
            "wwwroot/fonts/RobotoFlex.woff2",
            "wwwroot/fonts/SourceSans3VF-Italic.ttf.woff2",
            "wwwroot/fonts/SourceSans3VF-Upright.ttf.woff2",
            "wwwroot/images/Han_Transport.gif",
            "wwwroot/images/login-bg-crop.jpeg",
            "wwwroot/images/login-bg.jpeg",
            "wwwroot/images/logoppj800.png",
            "wwwroot/images/mordernview.png",
            "wwwroot/images/normalview.png"
        };

        foreach (var relativePath in legacyAssets)
        {
            Assert.False(
                File.Exists(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar))),
                $"Legacy asset must not be served: {relativePath}");
        }
    }

    [Fact]
    public void BootstrapDistribution_KeepsOnlyTheLinkedStylesheetAndItsMap()
    {
        var root = GetFrontendRoot();
        var bootstrapRoot = Path.Combine(root, "wwwroot", "lib", "bootstrap", "dist");
        var remainingFiles = Directory
            .EnumerateFiles(bootstrapRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(bootstrapRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["css/bootstrap.min.css", "css/bootstrap.min.css.map"],
            remainingFiles);

        var app = File.ReadAllText(Path.Combine(root, "Components", "App.razor"));
        Assert.Contains("lib/bootstrap/dist/css/bootstrap.min.css", app, StringComparison.Ordinal);
        Assert.DoesNotContain("lib/bootstrap/dist/js", app, StringComparison.Ordinal);
    }

    private static string GetFrontendRoot()
        => Path.Combine(FindRepositoryRoot(), "src", "Frontend", "Blazor");

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
