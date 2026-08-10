using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ButtonDensityTests : TestBase, IAuthenticatedUiTest
{
    private const string ScreenshotDirectoryEnvironmentVariable = "GTAS_BUTTON_DENSITY_SCREENSHOT_DIR";

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1366, 768)]
    public async Task SharedActions_UseCompactFlatDensityAcrossRepresentativeRoutes(int width, int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await LoginAsDefaultUserAsync();

        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0&orderView=current");
        var selector = Page.Locator(".vpp-orders-view-selector:visible");
        await selector.WaitForAsync();
        var selectorDensity = await selector.EvaluateAsync<string>("""
            root => {
                const rootRect = root.getBoundingClientRect();
                const segment = root.querySelector('button');
                const segmentRect = segment?.getBoundingClientRect();
                const style = segment ? getComputedStyle(segment) : null;
                const ok = Math.abs(rootRect.height - 34) <= 1
                    && !!segmentRect
                    && Math.abs(segmentRect.height - 28) <= 1
                    && style?.boxShadow === 'none'
                    && style?.transform === 'none';
                return `${ok}|root=${rootRect.height}|segment=${segmentRect?.height ?? 0}`
                    + `|shadow=${style?.boxShadow}|transform=${style?.transform}`;
            }
            """);
        selectorDensity.Should().StartWith("true", "segmented actions keep a 28px visual button inside the 34px framed selector");
        await CaptureIfRequestedAsync($"button-density-orders-{width}x{height}.png");

        await Page.GotoAsync($"{BaseUrl}permission?tab=0");
        var invitation = Page.GetByRole(AriaRole.Button, new() { Name = "Thêm người dùng", Exact = true });
        await invitation.WaitForAsync();
        await AssertCompactButtonAsync(invitation, maximumHeight: 32.5, expectsIcon: true);
        await CaptureIfRequestedAsync($"button-density-users-{width}x{height}.png");

        await Page.GotoAsync($"{BaseUrl}library?tab=0");
        var iconAction = Page.Locator(".vpp-admin-icon-action:visible").First;
        await iconAction.WaitForAsync();
        await AssertCompactButtonAsync(iconAction, maximumHeight: 28.5, expectsIcon: true);
        await Page.WaitForFunctionAsync("""
            () => [...document.querySelectorAll('.rz-data-grid, .rz-datatable')]
                .every(grid => !grid.classList.contains('rz-datatable-loading'))
            """);

        var bodyOverflow = await Page.EvaluateAsync<double>("document.documentElement.scrollWidth - document.documentElement.clientWidth");
        bodyOverflow.Should().BeLessThanOrEqualTo(1, "compact controls must not introduce page-level horizontal overflow");

        await CaptureIfRequestedAsync($"button-density-library-{width}x{height}.png");
    }

    private static async Task AssertCompactButtonAsync(ILocator button, double maximumHeight, bool expectsIcon)
    {
        var density = await button.EvaluateAsync<string>("""
            (element, args) => {
                const rect = element.getBoundingClientRect();
                const style = getComputedStyle(element);
                const icon = element.querySelector('.rzi, .vpp-icon');
                const iconSize = icon ? parseFloat(getComputedStyle(icon).fontSize) : 0;
                const hasExpectedIcon = !args.expectsIcon || (iconSize > 0 && iconSize <= 16.5);
                const ok = rect.height <= args.maximumHeight
                    && rect.height >= 27
                    && hasExpectedIcon
                    && style.boxShadow === 'none'
                    && style.transform === 'none';
                return `${ok}|height=${rect.height}|icon=${iconSize}`
                    + `|shadow=${style.boxShadow}|transform=${style.transform}`;
            }
            """, new { maximumHeight, expectsIcon });
        density.Should().StartWith("true", "shared text and icon actions use the compact flat button contract");
    }

    private async Task CaptureIfRequestedAsync(string fileName)
    {
        var directory = Environment.GetEnvironmentVariable(ScreenshotDirectoryEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(directory);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(directory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide,
            Scale = ScreenshotScale.Css
        });
    }
}
