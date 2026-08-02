using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ProductCatalogResponsiveBaselineTests : TestBase, IAuthenticatedUiTest
{
    [Theory]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    [InlineData(1366, 768)]
    [InlineData(1920, 1080)]
    public async Task Employee_CatalogStaysContainedAcrossRequiredViewports(int width, int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=2", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        var workspace = Page.Locator(".vpp-catalog-workspace:visible").Last;
        await workspace.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });

        var grid = Page.Locator(".vpp-catalog-grid:visible").Last;
        await grid.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await grid.Locator("tbody tr").CountAsync()).Should().BeGreaterThan(0);

        var dataSurface = Page.GetByTestId("catalog-data-surface");
        (await dataSurface.GetAttributeAsync("data-vpp-data-source-mode"))
            .Should().Be("server-paging");

        await grid.Locator(".rz-paginator, .rz-pager").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible
        });
        await WaitForRenderSettleAsync();

        var geometry = await Page.EvaluateAsync<string>("""
            () => {
                const root = document.documentElement;
                const main = document.querySelector('#main-content');
                const workspace = document.querySelector('.vpp-catalog-workspace');
                const dataViewport = document.querySelector('.vpp-catalog-grid .rz-data-grid-data');
                if (!workspace || !dataViewport) return 'missing';

                const mainRect = main?.getBoundingClientRect();
                const workspaceRect = workspace.getBoundingClientRect();
                const noDocumentOverflow = root.scrollWidth <= root.clientWidth + 1;
                const mainContained = !mainRect
                    || (mainRect.left >= -1 && mainRect.right <= root.clientWidth + 1);
                const workspaceContained = workspaceRect.left >= -1
                    && workspaceRect.right <= root.clientWidth + 1;
                const hasInternalOverflow = dataViewport.scrollWidth > dataViewport.clientWidth + 1;

                return `${noDocumentOverflow}|${mainContained}|${workspaceContained}|${hasInternalOverflow}`
                    + `|document=${root.scrollWidth}/${root.clientWidth}`
                    + `|grid=${dataViewport.scrollWidth}/${dataViewport.clientWidth}`;
            }
            """);

        geometry.Should().StartWith("true|true|true|",
            "Catalog phải nằm trong viewport và để grid tự sở hữu cuộn ngang");
        if (width <= 768)
        {
            geometry.Should().StartWith("true|true|true|true|",
                "mobile/tablet phải giữ schema cột bằng cuộn ngang bên trong grid");
        }

        await CaptureAsync($"catalog-baseline-{width}x{height}.png");
    }

    private async Task CaptureAsync(string fileName)
    {
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(evidenceDirectory);
        await Page.ScreenshotAsync(new()
        {
            Path = Path.Combine(evidenceDirectory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide
        });
    }
}
