using gtas_vpp_fe.Helpers;
using System.Text.RegularExpressions;
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
            ["periods", "pending", "review", "demand", "supply", "settle"],
            RouteCatalog.Authenticated
                .Where(route => route.Key.StartsWith("dashboard.period.", StringComparison.Ordinal))
                .Select(route => route.Path.Split("periodTab=").Last())
                .OrderBy(value => Array.IndexOf(["periods", "pending", "review", "demand", "supply", "settle"], value))
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
        Assert.Contains("/Account/ResendConfirmation", paths);
        Assert.Contains(
            RouteCatalog.Anonymous,
            route => route.Key == "account.confirm-email"
                && route.Path.StartsWith("/Account/ConfirmEmail?", StringComparison.Ordinal));
        Assert.Contains("/logoutprocess", paths);
    }

    [Fact]
    public void NavigationMetadata_CoversEveryLogicalQueryVariant()
    {
        var expectedQueryParams = new HashSet<string>(StringComparer.Ordinal)
        {
            "tab",
            "managementTab",
            "periodTab",
            "periodYear",
            "periodMonth",
            "pricingTab",
            "orderView",
            "orderId",
            "isAdditional",
            "copyFrom",
            "mode",
            "priceListId",
            "required"
        };

        Assert.True(
            expectedQueryParams.SetEquals(RouteCatalog.NavigationQueryParams),
            $"Navigation query metadata drifted. Actual: {string.Join(", ", RouteCatalog.NavigationQueryParams)}");

        Assert.Equal(
            ["current", "supplement"],
            RouteCatalog.Authenticated
                .Where(route => route.Key.StartsWith("dashboard.my-orders.", StringComparison.Ordinal))
                .Select(route => route.Path.Split("orderView=").Last())
                .ToArray());
        Assert.Contains(
            RouteCatalog.Authenticated,
            route => route.Key == "dashboard.order-create.additional"
                && route.Path == "/dashboard/order-create?isAdditional=true");
        Assert.Contains(
            RouteCatalog.Authenticated,
            route => route.Key == "dashboard.order-create.copy-previous"
                && route.Path == "/dashboard/order-create?copyFrom=previous");
        Assert.Contains(
            RouteCatalog.Authenticated,
            route => route.Key == "dashboard.order-create.recreate"
                && route.Path.Contains("mode=recreate", StringComparison.Ordinal));
        Assert.Contains(
            RouteCatalog.Authenticated,
            route => route.Key == "library.pricing.prices.selected-list"
                && route.Path.Contains("priceListId={SAMPLE_PRICE_LIST_ID}", StringComparison.Ordinal));
    }

    [Fact]
    public void Catalog_CoversEveryRazorPageDirective()
    {
        var repositoryRoot = FindRepositoryRoot();
        var pagesRoot = Path.Combine(repositoryRoot, "src", "Frontend", "Blazor", "Components", "Pages");
        var catalogPaths = RouteCatalog.Authenticated
            .Concat(RouteCatalog.Anonymous)
            .Select(route => route.Path.Split('?')[0].TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingPaths = Directory
            .EnumerateFiles(pagesRoot, "*.razor", SearchOption.AllDirectories)
            .SelectMany(File.ReadLines)
            .Select(line => Regex.Match(line, "^@page\\s+\"(?<path>[^\"]+)\""))
            .Where(match => match.Success)
            .Select(match => NormalizePageDirective(match.Groups["path"].Value))
            .Where(path => !catalogPaths.Contains(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            missingPaths.Length == 0,
            $"RouteCatalog is missing Razor pages: {string.Join(", ", missingPaths)}");
    }

    private static string NormalizePageDirective(string path)
    {
        var templateIndex = path.IndexOf("/{", StringComparison.Ordinal);
        return (templateIndex >= 0 ? path[..templateIndex] : path).TrimEnd('/');
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
