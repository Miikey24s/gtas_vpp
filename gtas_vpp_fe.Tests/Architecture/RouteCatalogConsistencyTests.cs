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
        Assert.Contains(
            RouteCatalog.Authenticated,
            route => route.Key == "account.change-password" && route.Path == "/Account/ChangePassword");
        Assert.Equal(
            ["pending", "review", "demand", "supply"],
            RouteCatalog.Authenticated
                .Where(route => route.Key.StartsWith("dashboard.period.", StringComparison.Ordinal))
                .Select(route => route.Path.Split("periodTab=").Last())
                .OrderBy(value => Array.IndexOf(["pending", "review", "demand", "supply"], value))
                .ToArray());
    }

    [Fact]
    public void AnonymousRoutes_IncludeTheAtlasAccountLifecycle()
    {
        var paths = RouteCatalog.Anonymous.Select(route => route.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("/Account/Login", paths);
        Assert.Contains("/Account/ForgotPassword", paths);
        Assert.Contains("/Account/ResetPassword", paths);
        Assert.Contains("/Account/Register", paths);
        Assert.Contains("/logoutprocess", paths);
    }
}
