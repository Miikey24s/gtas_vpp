using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class DepartmentSummaryTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task Manager_CanSeeOnlyDepartmentRequestSummary()
    {
        await LoginAsAsync(TestAccounts.Manager);
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=3&managementTab=department");

        var grid = Page.Locator("[data-testid='department-summary-data-surface']:visible").Last;
        await grid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await grid.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
        (await grid.Locator(".vpp-filter-select").CountAsync()).Should().Be(2);
        (await grid.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().BeGreaterThan(0);
        var totalOrders = Page.Locator(".vpp-history-kpis:visible").Last
            .Locator(".vpp-history-kpi-trigger strong").Nth(1);
        await totalOrders.WaitForAsync();
        (await totalOrders.InnerTextAsync()).Trim().Should().Be("2");
        (await grid.InnerTextAsync()).Should().NotContain("QA-D02", "another department must stay out of manager scope");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 768, Height = 1024 },
                     new ViewportSize { Width = 1366, Height = 768 },
                     new ViewportSize { Width = 1920, Height = 1080 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync($"{BaseUrl}dashboard?tab=3&managementTab=department");
            await Page.Locator("[data-testid='department-summary-data-surface']:visible").Last
                .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Page.WaitForTimeoutAsync(250);
            var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
            hasHorizontalOverflow.Should().BeFalse($"route must not overflow at {viewport.Width}x{viewport.Height}");

            if (viewport.Width >= 1280)
            {
                var layoutGeometry = await Page.EvaluateAsync<string>("""
                    () => {
                        const page = document.querySelector('.vpp-history-page');
                        const kpis = document.querySelector('.vpp-history-kpis');
                        const orders = document.querySelector('.vpp-history-orders-card');
                        const detail = document.querySelector('.vpp-history-drawer');
                        const heading = document.querySelector('.vpp-history-sheet-heading');
                        const header = document.querySelector('.vpp-history-drawer-header');
                        if (!page || !kpis || !orders || !detail || !heading || !header) return 'missing';

                        const pageRect = page.getBoundingClientRect();
                        const kpiRect = kpis.getBoundingClientRect();
                        const ordersRect = orders.getBoundingClientRect();
                        const detailRect = detail.getBoundingClientRect();
                        const headingRect = heading.getBoundingClientRect();
                        const headerRect = header.getBoundingClientRect();
                        const ok = Math.abs(kpiRect.top - detailRect.top) <= 1
                            && Math.abs(pageRect.bottom - ordersRect.bottom) <= 2
                            && Math.abs(pageRect.bottom - detailRect.bottom) <= 2
                            && headingRect.top >= headerRect.top - 1
                            && headingRect.bottom <= headerRect.bottom + 1;
                        return `${ok}|top=${Math.round(kpiRect.top - detailRect.top)}|bottom=${Math.round(pageRect.bottom - ordersRect.bottom)}/${Math.round(pageRect.bottom - detailRect.bottom)}|heading=${Math.round(headingRect.top - headerRect.top)}/${Math.round(headerRect.bottom - headingRect.bottom)}`;
                    }
                """);
                layoutGeometry.Should().StartWith("true", $"department summary must fill the viewport, align detail with KPI cards and keep heading text inside its header at {viewport.Width}x{viewport.Height}");
            }

            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
            {
                Directory.CreateDirectory(evidenceDirectory);
                await Page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(evidenceDirectory, $"department-summary-history-parity-{viewport.Width}x{viewport.Height}.png"),
                    FullPage = false,
                    Animations = ScreenshotAnimations.Disabled,
                    Caret = ScreenshotCaret.Hide
                });
            }
        }
    }
}
