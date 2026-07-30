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
        await Page.Locator("[data-testid='item-admin-data-surface']").WaitForAsync();
        (await Page.Locator(".vpp-record-inspector").CountAsync()).Should().Be(0,
            "item administration now uses the full-width collection pattern and adaptive dialogs");
        var visibleItemColumns = await Page.Locator("[data-testid='item-admin-data-surface'] .vpp-admin-grid thead th:visible").CountAsync();
        visibleItemColumns.Should().BeLessThanOrEqualTo(8, "the default table should stay scannable while extra fields remain in the column picker or editor");

        await GotoMainRouteAsync("library?tab=6&pricingTab=price-lists");
        await Page.Locator("[data-testid='price-lists-data-surface']").WaitForAsync();
        // Positive signal first: the price-list grid finished loading with rows or the empty state.
        await Page.WaitForFunctionAsync("""
            () => {
                const grid = document.querySelector('.vpp-price-list-workspace .vpp-admin-grid');
                return !!grid
                    && !grid.classList.contains('rz-datatable-loading')
                    && !grid.querySelector('.rz-datatable-loading-content')
                    && !!grid.querySelector('.rz-data-row, .rz-datatable-emptymessage');
            }
            """);
        // Deliberate quiet window for the negative assertion below: prove no error toast appears.
        await Page.WaitForTimeoutAsync(500);
        (await Page.Locator(".rz-notification:visible").CountAsync()).Should().Be(0,
            "the price-list controller must activate successfully and return a normal empty or populated grid");

        await GotoMainRouteAsync("permission?tab=0");
        var userSurface = Page.Locator("[data-testid='permission-users-data-surface']");
        await userSurface.WaitForAsync();
        (await Page.Locator(".vpp-record-inspector").CountAsync()).Should().Be(0,
            "user administration uses the full-width collection pattern instead of a fixed inspector");
        (await Page.Locator(".permission-user-grid tbody .vpp-admin-inline-select").CountAsync()).Should().BeGreaterThan(0,
            "membership group and department are edited directly in their table columns");
        var invitationButton = Page.GetByRole(AriaRole.Button, new() { Name = "Thêm người dùng", Exact = true });
        await invitationButton.WaitForAsync();
        (await invitationButton.GetAttributeAsync("title")).Should().NotBeNullOrWhiteSpace(
            "the invitation action explains why it is unavailable when email delivery is disabled");

        await GotoMainRouteAsync("permission?tab=1");
        await Page.Locator("[data-testid='permission-groups-data-surface']").WaitForAsync();
        await Page.GetByRole(AriaRole.Button, new()
        {
            Name = "Quyền API (tham chiếu)",
            Exact = true
        }).ClickAsync();
        await Page.Locator(".vpp-permission-matrix").WaitForAsync();
        (await Page.Locator(".vpp-permission-matrix tbody tr").CountAsync()).Should().Be(18);
        (await Page.Locator(".vpp-permission-matrix thead th").CountAsync()).Should().Be(4);

        await GotoMainRouteAsync("dashboard?tab=5&periodTab=review");
        await Page.Locator(".vpp-period-settlement-page").WaitForAsync();
        (await Page.Locator(".vpp-workflow-stepper:visible").CountAsync()).Should().Be(0);
        (await Page.Locator(".vpp-period-settlement-page .vpp-segmented-selector").CountAsync()).Should().Be(2);
        await Page.Locator("[data-testid='period-settlement-data-surface']").WaitForAsync();

        await GotoMainRouteAsync("report");
        await Page.Locator("h1.vpp-report-heading").WaitForAsync();
        await Page.Locator(".vpp-report-filters").WaitForAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Xuất CSV", Exact = true }).WaitForAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Xuất Excel", Exact = true }).WaitForAsync();

        await Page.SetViewportSizeAsync(768, 1024);
        foreach (var route in new[] { "permission?tab=1", "report" })
        {
            await GotoMainRouteAsync(route);
            var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
            hasHorizontalOverflow.Should().BeFalse($"{route} should fit the tablet viewport");
        }

        await Page.SetViewportSizeAsync(390, 844);
        foreach (var route in new[] { "library?tab=2", "permission?tab=0", "permission?tab=1", "dashboard?tab=5&periodTab=review", "report" })
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
        await WaitForRenderSettleAsync();
    }
}
