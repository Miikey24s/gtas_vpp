using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class FeatureEndpointOwnershipTests
{
    [Fact]
    public void LegacyLibraryEndpointResolver_IsRetired()
    {
        var resolverPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Frontend",
            "Blazor",
            "Helpers",
            "LibraryEndpointResolver.cs");

        Assert.False(
            File.Exists(resolverPath),
            "typed feature clients own library endpoint selection");
    }

    [Fact]
    public void Config_DoesNotOwnApiEndpointCatalogs()
    {
        var configPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Frontend",
            "Blazor",
            "Helpers",
            "Config.cs");
        var source = File.ReadAllText(configPath);

        Assert.DoesNotContain("static class VppApi", source, StringComparison.Ordinal);
        Assert.DoesNotContain("static class LibraryApi", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/api", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"api/", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "gtas_vpp.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the GTAS VPP repository root.");
    }
}
