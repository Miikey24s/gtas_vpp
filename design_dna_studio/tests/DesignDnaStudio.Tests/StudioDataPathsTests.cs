using DesignDnaStudio.Web.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace DesignDnaStudio.Tests;

public sealed class StudioDataPathsTests
{
    [Fact]
    public void RepositoryLocalDataRoot_IsRejected()
    {
        var studioRoot = FindStudioRoot();
        var contentRoot = Path.Combine(studioRoot, "src", "DesignDnaStudio.Web");
        var configuration = Configuration(Path.Combine(studioRoot, "local-research-data"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            StudioDataPaths.Resolve(configuration, contentRoot));

        Assert.Contains("outside the repository", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UncDataRoot_IsRejectedOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var configuration = Configuration(@"\\server\share\DesignDNAStudio");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            StudioDataPaths.Resolve(configuration));

        Assert.Contains("network", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IConfiguration Configuration(string dataRoot) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Studio:DataRoot"] = dataRoot
            })
            .Build();

    private static string FindStudioRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DesignDnaStudio.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the DesignDNA Studio source root.");
    }
}
