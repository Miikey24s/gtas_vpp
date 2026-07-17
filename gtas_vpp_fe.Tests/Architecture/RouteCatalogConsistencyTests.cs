using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class RouteCatalogConsistencyTests
{
    [Fact]
    public void AuthenticatedRoutes_HaveUniqueKeysAndCanonicalNavigationPaths()
    {
        var duplicateKeys = RouteCatalog.Authenticated
            .GroupBy(route => route.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicateKeys);
        Assert.DoesNotContain(
            RouteCatalog.Authenticated,
            route => route.Path.Contains("/dashboard?tab=4", StringComparison.OrdinalIgnoreCase)
                || route.Path.Equals("/library?tab=4", StringComparison.OrdinalIgnoreCase));
        Assert.All(
            RouteCatalog.Authenticated.Where(route => !route.IsDynamic),
            route => Assert.StartsWith("/", route.Path));
    }
}
