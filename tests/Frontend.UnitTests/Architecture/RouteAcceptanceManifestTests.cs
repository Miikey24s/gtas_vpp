using gtas_vpp_fe.Helpers;
using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class RouteAcceptanceManifestTests
{
    [Fact]
    public void Manifest_CoversEveryRouteCatalogKeyExactlyOnce()
    {
        var routeEntries = RouteCatalog.All;
        var manifestEntries = RouteAcceptanceManifest.Entries;

        Assert.Equal(44, routeEntries.Count);
        Assert.Equal(routeEntries.Count, manifestEntries.Count);

        var duplicateKeys = manifestEntries
            .GroupBy(entry => entry.RouteKey, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicateKeys);
        Assert.Equal(
            routeEntries.Select(route => route.Key).ToHashSet(StringComparer.OrdinalIgnoreCase),
            manifestEntries.Select(entry => entry.RouteKey).ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Manifest_PreservesCanonicalPathsAndEvidenceMetadata()
    {
        var repositoryRoot = FindRepositoryRoot();
        var routeKeys = RouteCatalog.All
            .Select(route => route.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in RouteAcceptanceManifest.Entries)
        {
            var route = RouteCatalog.GetRequired(entry.RouteKey);
            var evidencePath = Path.Combine(
                repositoryRoot,
                entry.EvidencePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.Equal(route.Path, entry.Path);
            Assert.False(string.IsNullOrWhiteSpace(entry.Evidence), entry.RouteKey);
            Assert.False(string.IsNullOrWhiteSpace(entry.EvidencePath), entry.RouteKey);
            Assert.True(File.Exists(evidencePath), $"Evidence path does not exist for {entry.RouteKey}: {entry.EvidencePath}");
            Assert.False(string.IsNullOrWhiteSpace(entry.TestHandle), entry.RouteKey);

            if (entry.Classification == RouteAcceptanceClassification.DynamicSample)
            {
                Assert.True(route.IsDynamic, $"DynamicSample route is not marked dynamic: {entry.RouteKey}");
            }

            if (entry.Classification == RouteAcceptanceClassification.JustifiedEquivalent)
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.RepresentativeRouteKey), entry.RouteKey);
                Assert.Contains(entry.RepresentativeRouteKey!, routeKeys);
                Assert.NotEqual(entry.RouteKey, entry.RepresentativeRouteKey, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Manifest_TestHandlesExistInTheirEvidenceSources()
    {
        var repositoryRoot = FindRepositoryRoot();

        foreach (var entry in RouteAcceptanceManifest.Entries)
        {
            var evidencePath = Path.Combine(
                repositoryRoot,
                entry.EvidencePath.Replace('/', Path.DirectorySeparatorChar));
            var source = File.ReadAllText(evidencePath);
            var separatorIndex = entry.TestHandle.IndexOf('.', StringComparison.Ordinal);

            Assert.True(separatorIndex > 0, $"Test handle must use Type.Method form: {entry.RouteKey}");
            var typeName = entry.TestHandle[..separatorIndex];
            var methodName = entry.TestHandle[(separatorIndex + 1)..];

            Assert.Contains($"class {typeName}", source, StringComparison.Ordinal);
            Assert.True(
                Regex.IsMatch(source, $@"\b{Regex.Escape(methodName)}\s*\(", RegexOptions.CultureInvariant),
                $"Test handle is not declared in its evidence source for {entry.RouteKey}: {entry.TestHandle}");
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
