using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ProductCatalogTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task Employee_CanBrowseRequestProductCatalog()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=2", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await Page.Locator(".vpp-catalog-workspace").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });

        var grid = Page.Locator(".vpp-catalog-grid:visible").Last;
        await grid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await grid.Locator("tbody tr").CountAsync()).Should().BeGreaterThan(0);
        (await Page.Locator(".vpp-catalog-card-header").CountAsync()).Should().Be(0);
        (await Page.Locator("[data-testid='catalog-data-surface']").GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
        (await grid.Locator("thead th").First.InnerTextAsync()).Trim().Should().Be("#");
        (await grid.Locator("tbody tr").First.Locator("td").First.InnerTextAsync()).Trim().Should().Be("1");
        var gridCornerRadius = await grid.EvaluateAsync<string>("""
            element => {
                const style = getComputedStyle(element);
                return `${style.borderTopLeftRadius}|${style.borderTopRightRadius}`;
            }
        """);
        gridCornerRadius.Should().Be("0px|0px", "the grid header joins the toolbar inside one shared data surface");
        await grid.Locator(".rz-paginator, .rz-pager").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await grid.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().BeGreaterThan(0);
        var catalogGeometry = await Page.EvaluateAsync<string>("""
            () => {
                const workspace = document.querySelector('.vpp-catalog-workspace');
                const grid = document.querySelector('.vpp-catalog-grid');
                const pager = grid?.querySelector('.rz-paginator, .rz-pager');
                if (!workspace || !grid || !pager) return 'missing';
                const workspaceRect = workspace.getBoundingClientRect();
                const pagerRect = pager.getBoundingClientRect();
                const ok = Math.abs(workspaceRect.bottom - pagerRect.bottom) <= 2
                    && document.documentElement.scrollHeight <= document.documentElement.clientHeight + 1;
                return `${ok}|bottom=${Math.round(workspaceRect.bottom - pagerRect.bottom)}|document=${document.documentElement.scrollHeight}/${document.documentElement.clientHeight}`;
            }
        """);
        catalogGeometry.Should().StartWith("true", "Catalog pager must stay at the workspace bottom without document scrolling");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "catalog-flat-paged-1920x1080.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }

    }
}
