using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class RoleNavigationVisibilityTests : TestBase, IAuthenticatedUiTest
{
    private const string ScreenshotDirectoryEnvironmentVariable = "GTAS_ROLE_NAV_SCREENSHOT_DIR";

    [Fact]
    public async Task CanonicalPersonas_SeeOnlyTheirAuthorizedWorkspaces()
    {
        await Page.SetViewportSizeAsync(1366, 768);

        await LoginAsAsync(TestAccounts.Employee);
        await AssertShellReadyAsync();
        await AssertHasLinksAsync("/dashboard?tab=0", "/dashboard?tab=1", "/dashboard?tab=2", "/report");
        await AssertHasNoLinksAsync("/library", "/permission", "/dashboard?tab=3", "/dashboard?tab=5");
        await CaptureIfRequestedAsync("employee.png");
        await AssertDeepLinkRedirectsToDashboardAsync("library?tab=2");

        await SwitchUserAsync(TestAccounts.Manager);
        await AssertShellReadyAsync();
        await AssertHasLinksAsync("/library?tab=2", "/dashboard?tab=3", "/dashboard?tab=5", "/report");
        await AssertHasNoLinksAsync("/permission");
        await CaptureIfRequestedAsync("manager.png");
        await AssertDeepLinkRedirectsToDashboardAsync("permission?tab=0");

        await SwitchUserAsync(TestAccounts.SystemAdmin);
        await AssertShellReadyAsync();
        await AssertHasLinksAsync("/library?tab=2", "/permission?tab=0", "/permission?tab=1", "/dashboard?tab=5");
        await CaptureIfRequestedAsync("dev.png");
    }

    private async Task AssertShellReadyAsync()
    {
        await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached
        });
        await WaitForRenderSettleAsync();
    }

    private async Task AssertHasLinksAsync(params string[] hrefPrefixes)
    {
        foreach (var hrefPrefix in hrefPrefixes)
        {
            var count = await Page.Locator($".vpp-sidebar-nav a[href^='{hrefPrefix}']").CountAsync();
            count.Should().BeGreaterThan(0, $"the current persona should see navigation starting with {hrefPrefix}");
        }
    }

    private async Task AssertHasNoLinksAsync(params string[] hrefPrefixes)
    {
        foreach (var hrefPrefix in hrefPrefixes)
        {
            var count = await Page.Locator($".vpp-sidebar-nav a[href^='{hrefPrefix}']").CountAsync();
            count.Should().Be(0, $"the current persona must not see navigation starting with {hrefPrefix}");
        }
    }

    private async Task AssertDeepLinkRedirectsToDashboardAsync(string route)
    {
        await Page.GotoAsync($"{BaseUrl}{route}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.WaitForURLAsync(
            new System.Text.RegularExpressions.Regex(
                ".*/dashboard.*",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            new PageWaitForURLOptions { Timeout = 30_000 });
        (await Page.Locator(".vpp-admin-data-surface").CountAsync()).Should().Be(0);
    }

    private async Task CaptureIfRequestedAsync(string fileName)
    {
        var configuredDirectory = Environment.GetEnvironmentVariable(ScreenshotDirectoryEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return;
        }

        var directory = Path.GetFullPath(configuredDirectory);
        Directory.CreateDirectory(directory);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(directory, fileName),
            FullPage = false
        });
    }
}
