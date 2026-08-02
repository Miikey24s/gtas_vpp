using gtas_vpp_fe.Components.Layout;
using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class ShellNavigationCatalogTests
{
    [Fact]
    public void Catalog_HasFourSectionsAndSeventeenUniqueLeafItems()
    {
        var items = ShellNavigationCatalog.Sections.SelectMany(section => section.Items).ToArray();

        Assert.Equal(4, ShellNavigationCatalog.Sections.Count);
        Assert.Equal(17, items.Length);
        Assert.Equal(17, items.Select(item => item.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(17, items.Select(item => item.RouteKey).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void EveryLeafTargetsCanonicalStaticAuthenticatedRoute()
    {
        foreach (var item in ShellNavigationCatalog.Sections.SelectMany(section => section.Items))
        {
            var route = RouteCatalog.GetRequired(item.RouteKey);

            Assert.True(route.IsAuthenticated, item.RouteKey);
            Assert.False(route.IsDynamic, item.RouteKey);
            Assert.StartsWith("/", item.Path);
        }
    }

    [Fact]
    public void DefaultsAndCompatibilityAliasesPreserveCurrentShellBehavior()
    {
        Assert.Equal(
            [
                "dashboard.my-orders",
                "dashboard.history",
                "dashboard.catalog",
                "dashboard.management.department",
                "dashboard.period.review",
                "dashboard.period.pending-approval"
            ],
            ShellNavigationCatalog.Dashboard.DefaultRouteKeys);
        Assert.Equal(
            "library.pricing.price-lists",
            ShellNavigationCatalog.Library.DefaultRouteKeys[4]);
        Assert.Contains("dashboard/order-create", ShellNavigationCatalog.MyOrders.ActiveAliases!);
        Assert.NotNull(ShellNavigationCatalog.PeriodReview.ActiveAliases);
        Assert.Equal(
            ["periodTab=demand", "periodTab=supply", "periodTab=settle"],
            ShellNavigationCatalog.PeriodReview.ActiveAliases!);
        Assert.Contains("tab=4", ShellNavigationCatalog.Prices.ActiveAliases!);
    }
}
