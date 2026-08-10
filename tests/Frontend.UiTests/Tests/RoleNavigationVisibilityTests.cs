using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_test_support;
using Microsoft.Playwright;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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
        await AssertOwnHistoryCountAsync(TestAccounts.Employee, 8);
        await AssertDepartmentApiForbiddenAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=3&managementTab=department");
        await AssertShellReadyAsync();
        (await Page.GetByTestId("department-summary-data-surface").CountAsync()).Should().Be(0);
        await Page.GetByTestId("current-order-panel").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=periods");
        await AssertShellReadyAsync();
        (await Page.GetByTestId("order-period-management-table").CountAsync())
            .Should().Be(0);
        await Page.GetByTestId("current-order-panel").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await CaptureIfRequestedAsync("employee.png");
        await AssertDeepLinkRedirectsToDashboardAsync("library?tab=2");

        await SwitchUserAsync(TestAccounts.Manager);
        await AssertShellReadyAsync();
        await AssertHasLinksAsync("/library?tab=2", "/dashboard?tab=3", "/dashboard?tab=5", "/report");
        await AssertHasNoLinksAsync("/permission");
        await AssertOwnHistoryCountAsync(TestAccounts.Manager, 8);
        await AssertDepartmentHistoryCountAsync(TestAccounts.Manager, 26);
        await CaptureIfRequestedAsync("manager.png");
        await AssertDeepLinkRedirectsToDashboardAsync("permission?tab=0");

        await SwitchUserAsync(TestAccounts.SystemAdmin);
        await AssertShellReadyAsync();
        await AssertHasLinksAsync("/library?tab=2", "/permission?tab=0", "/permission?tab=1", "/dashboard?tab=5");
        await AssertOwnHistoryCountAsync(TestAccounts.SystemAdmin, 8);
        await AssertDepartmentHistoryCountAsync(TestAccounts.SystemAdmin, 26);
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

    private async Task AssertOwnHistoryCountAsync(QaTestAccount account, int expectedCount)
    {
        using var client = await CreateAuthorizedApiClientAsync(account);
        var orders = await client.GetFromJsonAsync<List<VppRequestResDTO>>(
                         "/api/VPPRequest/my-order-history?skip=0&top=100",
                         TestContext.Current.CancellationToken)
                     ?? [];
        orders.Should().HaveCount(expectedCount);
    }

    private async Task AssertDepartmentApiForbiddenAsync(QaTestAccount account)
    {
        using var client = await CreateAuthorizedApiClientAsync(account);
        using var ordersResponse = await client.GetAsync(
            "/api/VPPRequest/department-orders?skip=0&top=100",
            TestContext.Current.CancellationToken);
        using var summaryResponse = await client.GetAsync(
            "/api/VPPRequest/department-order-history-summary",
            TestContext.Current.CancellationToken);

        ordersResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task AssertDepartmentHistoryCountAsync(QaTestAccount account, int expectedCount)
    {
        using var client = await CreateAuthorizedApiClientAsync(account);
        using var response = await client.GetAsync(
            "/api/VPPRequest/department-orders?skip=0&top=100",
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<VppRequestResDTO>>(
                         TestContext.Current.CancellationToken)
                     ?? [];
        orders.Should().HaveCount(expectedCount);
        orders.Should().OnlyContain(order => order.DepartmentCode == TestAccounts.Manager.DepartmentCode);
    }

    private async Task<HttpClient> CreateAuthorizedApiClientAsync(QaTestAccount account)
    {
        BackendBaseUrl.Should().NotBeNullOrWhiteSpace();
        var client = new HttpClient { BaseAddress = new Uri(BackendBaseUrl!) };
        try
        {
            using var loginResponse = await client.PostAsJsonAsync(
                "/api/Auth/login",
                new AuthenticationLoginRequest(account.Username, account.Password),
                TestContext.Current.CancellationToken);
            loginResponse.EnsureSuccessStatusCode();
            var login = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResultDTO>(
                TestContext.Current.CancellationToken);
            login?.AccessToken.Should().NotBeNullOrWhiteSpace();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                login!.AccessToken);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
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
