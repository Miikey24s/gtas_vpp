using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ColumnPickerContractTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task AllAdminColumnPickers_ExposeBusinessFieldsWithStableChrome()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);

        var routes = new (string Path, string[] SurfaceIds)[]
        {
            ("library?tab=0", ["lookup-categories-data-surface", "lookup-values-data-surface"]),
            ("library?tab=1", ["category-admin-data-surface"]),
            ("library?tab=2", ["item-admin-data-surface"]),
            ("library?tab=3", ["supplier-admin-data-surface"]),
            ("library?tab=5", ["department-admin-data-surface"]),
            ("library?tab=6&pricingTab=price-lists", ["price-lists-data-surface"]),
            ("library?tab=6&pricingTab=prices", ["prices-data-surface"]),
            ("permission?tab=0", ["permission-users-data-surface"]),
            ("permission?tab=1", ["permission-groups-data-surface"]),
            ("permission?tab=2", ["security-audit-data-surface"])
        };

        var visitedPickerCount = 0;
        foreach (var route in routes)
        {
            await Page.GotoAsync($"{BaseUrl}{route.Path}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            foreach (var surfaceId in route.SurfaceIds)
            {
                var surface = Page.GetByTestId(surfaceId);
                await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                var trigger = surface.Locator(".vpp-column-picker-trigger");
                await trigger.WaitForAsync(new() { State = WaitForSelectorState.Visible });

                var countText = (await trigger.Locator(".vpp-column-picker-count").InnerTextAsync()).Trim();
                var visibleCount = int.Parse(countText);
                visibleCount.Should().BeGreaterThan(0);

                await trigger.ClickAsync();
                var popover = Page.Locator(".vpp-column-picker-popover:popover-open");
                await popover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                var labels = await popover.Locator(".vpp-column-picker-label").AllInnerTextsAsync();
                labels.Count.Should().BeGreaterThanOrEqualTo(visibleCount);
                labels.Should().NotContain("#");
                labels.Should().NotContain("Thao tác");
                labels.Should().ContainSingle(label => label == "ID");
                labels.Should().NotContain(label => label.Contains("RowVersion", StringComparison.OrdinalIgnoreCase));

                var selectedBackground = await popover.Locator(".vpp-column-picker-option.is-selected").First
                    .EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor");
                selectedBackground.Should().Be("rgba(0, 0, 0, 0)");

                await Page.Keyboard.PressAsync("Escape");
                await popover.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
                visitedPickerCount++;
            }
        }

        visitedPickerCount.Should().Be(11);

        foreach (var viewport in new[]
                 {
                     (Width: 390, Height: 844),
                     (Width: 768, Height: 1024),
                     (Width: 1920, Height: 1080)
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=price-lists", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            var representativeSurface = Page.GetByTestId("price-lists-data-surface");
            var trigger = representativeSurface.Locator(".vpp-column-picker-trigger");
            await trigger.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await trigger.ScrollIntoViewIfNeededAsync();
            var triggerHeight = await trigger.EvaluateAsync<double>("element => element.getBoundingClientRect().height");
            var searchHeight = await representativeSurface.Locator(".vpp-filter-search")
                .EvaluateAsync<double>("element => element.getBoundingClientRect().height");
            var clearFiltersHeight = await representativeSurface.Locator(".vpp-clear-filters")
                .EvaluateAsync<double>("element => element.getBoundingClientRect().height");
            triggerHeight.Should().BeApproximately(32, 1);
            triggerHeight.Should().BeApproximately(searchHeight, 1);
            clearFiltersHeight.Should().BeApproximately(searchHeight, 1);
            await trigger.ClickAsync();
            var popover = Page.Locator(".vpp-column-picker-popover:popover-open");
            await popover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            var geometry = await popover.EvaluateAsync<double[]>("""
                element => {
                    const rect = element.getBoundingClientRect();
                    return [rect.left, rect.top, rect.right, rect.bottom, window.innerWidth, window.innerHeight];
                }
                """);
            geometry[0].Should().BeGreaterThanOrEqualTo(0);
            geometry[1].Should().BeGreaterThanOrEqualTo(0);
            geometry[2].Should().BeLessThanOrEqualTo(geometry[4] + 1);
            geometry[3].Should().BeLessThanOrEqualTo(geometry[5] + 1);
            await Page.Keyboard.PressAsync("Escape");
        }
    }
}
