using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class AtlasWave1Tests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task AdministrativeAndPeriodWorkspaces_AreInteractiveAndResponsive()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1920, 1080);

        await GotoMainRouteAsync("library?tab=2");
        await Page.Locator(".vpp-atlas-admin-workspace").WaitForAsync();
        await Page.Locator(".vpp-record-inspector").WaitForAsync();
        var visibleItemColumns = await Page.Locator(".library-share-grid thead th:visible").CountAsync();
        visibleItemColumns.Should().BeLessThanOrEqualTo(8, "technical and translation fields belong in the inspector by default");

        await GotoMainRouteAsync("library?tab=6&pricingTab=price-lists");
        await Page.Locator(".vpp-atlas-admin-workspace").WaitForAsync();
        await Page.WaitForTimeoutAsync(500);
        (await Page.Locator(".rz-notification:visible").CountAsync()).Should().Be(0,
            "the price-list controller must activate successfully and return a normal empty or populated grid");

        await GotoMainRouteAsync("permission?tab=0");
        await Page.Locator(".vpp-atlas-user-workspace").WaitForAsync();
        await Page.Locator(".vpp-record-inspector").WaitForAsync();

        await GotoMainRouteAsync("permission?tab=1");
        await Page.GetByText("Ba vai trò chuẩn", new() { Exact = false }).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });

        await GotoMainRouteAsync("dashboard?tab=5&periodTab=review");
        await Page.Locator(".vpp-period-workspace").WaitForAsync();
        (await Page.Locator(".vpp-period-flow > span").CountAsync()).Should().Be(4);
        await Page.Locator(".vpp-settle-container").WaitForAsync();

        await GotoMainRouteAsync("report");
        await Page.Locator("h1.vpp-report-heading").WaitForAsync();

        await Page.SetViewportSizeAsync(390, 844);
        foreach (var route in new[] { "library?tab=2", "permission?tab=0", "dashboard?tab=5&periodTab=review", "report" })
        {
            await GotoMainRouteAsync(route);
            var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
            hasHorizontalOverflow.Should().BeFalse($"{route} should fit the mobile viewport");
        }
    }

    private async Task GotoMainRouteAsync(string route)
    {
        await Page.GotoAsync($"{BaseUrl}{route}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator("#main-content").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await Page.WaitForTimeoutAsync(350);
    }
}
