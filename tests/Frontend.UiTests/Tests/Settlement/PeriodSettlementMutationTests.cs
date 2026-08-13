using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_test_support;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Settlement;

public sealed class PeriodSettlementMutationTests : TestBase, IMutatingUiTest
{
    private const string ScreenshotDirectoryEnvironmentVariable = "GTAS_SETTLEMENT_MUTATION_SCREENSHOT_DIR";
    private const string AdjustmentReason = "Chọn nhầm bảng giá và cần lưu lại kết quả đúng";

    [Fact]
    public async Task Settlement_AdjustmentFromUi_KeepsTheOldVersionAndCreatesVersionTwo()
    {
        BackendBaseUrl.Should().NotBeNullOrWhiteSpace();
        using var procurementApi = await CreateAuthorizedApiClientAsync(TestAccounts.Procurement);
        var period = await procurementApi.GetFromJsonAsync<VppPeriodInfoResDTO>(
            "/api/VPPRequest/period-info",
            TestContext.Current.CancellationToken);
        period.Should().NotBeNull();

        var targetYear = period!.PreviousPeriodYear;
        var targetMonth = period.PreviousPeriodMonth;
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsAsync(TestAccounts.Procurement);
        await OpenSettlementAsync(targetYear, targetMonth);

        var settleButton = await WaitForEnabledActionAsync("Chốt kỳ");
        await settleButton.ClickAsync();
        var confirmDialog = Page.Locator(".rz-dialog:visible").Last;
        await confirmDialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await confirmDialog.GetByRole(AriaRole.Button, new() { Name = "Có", Exact = true }).ClickAsync();
        _ = await WaitForRevisionCountAsync(procurementApi, targetYear, targetMonth, 1);

        await SwitchUserAsync(TestAccounts.SystemAdmin);
        using var systemAdminApi = await CreateAuthorizedApiClientAsync(TestAccounts.SystemAdmin);
        await OpenSettlementAsync(targetYear, targetMonth);
        var adjustButton = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Điều chỉnh bản chốt", Exact = true });
        await adjustButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await adjustButton.ClickAsync();
        var adjustmentDialog = Page.GetByTestId("settlement-correction-dialog");
        await adjustmentDialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await adjustmentDialog.Locator("textarea").FillAsync(AdjustmentReason);
        await adjustmentDialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Lưu bản chốt mới", Exact = true })
            .ClickAsync();
        await adjustmentDialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 60_000 });
        await Page.GetByText("Hệ thống đã lưu thành bản chốt 2. Bản trước vẫn có trong lịch sử.", new() { Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });

        var revisions = await GetRevisionsAsync(systemAdminApi, targetYear, targetMonth);
        revisions.Should().HaveCount(2);
        revisions.Single(item => item.RevisionNumber == 1).IsCurrentRevision.Should().BeFalse();
        var current = revisions.Single(item => item.RevisionNumber == 2);
        current.IsCurrentRevision.Should().BeTrue();
        current.IsCorrection.Should().BeTrue();
        current.CorrectionReason.Should().Be(AdjustmentReason);
        var managedPeriods = await systemAdminApi.GetFromJsonAsync<List<VppManagedPeriodResDTO>>(
            "/api/order-periods",
            TestContext.Current.CancellationToken);
        managedPeriods.Should().ContainSingle(periodItem =>
            periodItem.Year == targetYear
            && periodItem.Month == targetMonth
            && periodItem.State == "Settled");
        await Page.GetByText("Bản chốt 2", new() { Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await CaptureAsync("settlement-version-2-1366x768.png");
    }

    [Fact]
    public async Task Settlement_SettledView_UsesFriendlyVersionAndAdjustmentActionsWithoutReopen()
    {
        BackendBaseUrl.Should().NotBeNullOrWhiteSpace();
        using var procurementApi = await CreateAuthorizedApiClientAsync(TestAccounts.Procurement);
        var period = await procurementApi.GetFromJsonAsync<VppPeriodInfoResDTO>(
            "/api/VPPRequest/period-info",
            TestContext.Current.CancellationToken);
        period.Should().NotBeNull();

        var targetYear = period!.PreviousPeriodYear;
        var targetMonth = period.PreviousPeriodMonth;

        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsAsync(TestAccounts.Procurement);
        await OpenSettlementAsync(targetYear, targetMonth);

        var settleButton = await WaitForEnabledActionAsync("Chốt kỳ");
        await settleButton.ClickAsync();
        var confirmDialog = Page.Locator(".rz-dialog:visible").Last;
        await confirmDialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await confirmDialog.GetByRole(AriaRole.Button, new() { Name = "Có", Exact = true }).ClickAsync();

        var revisionsAfterConfirm = await WaitForRevisionCountAsync(procurementApi, targetYear, targetMonth, 1);
        await Page.GetByText("Đã chốt kỳ", new() { Exact = true }).WaitForAsync();

        var firstRevision = revisionsAfterConfirm.Single();
        firstRevision.RevisionNumber.Should().Be(1);
        firstRevision.IsCurrentRevision.Should().BeTrue();
        firstRevision.IsCorrection.Should().BeFalse();
        firstRevision.ConfirmedByUserId.Should().Be(TestAccounts.Procurement.UserId);

        await Page.GetByText("Bản chốt 1", new() { Exact = true }).WaitForAsync();
        await Page.GetByRole(
                AriaRole.Button,
                new() { Name = "Điều chỉnh bản chốt", Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await CaptureAsync("settlement-version-1-1366x768.png");
        (await Page.GetByRole(
                AriaRole.Button,
                new() { Name = "Mở lại để chốt lại", Exact = true })
            .CountAsync()).Should().Be(0);
        await Page.GetByRole(
                AriaRole.Button,
                new() { Name = "Lịch sử chốt", Exact = true })
            .ClickAsync();
        await Page.GetByTestId("settlement-version-history-dialog").WaitForAsync();
        await Page.GetByTestId("settlement-version-history-dialog")
            .GetByRole(AriaRole.Button, new() { Name = "Đóng", Exact = true })
            .ClickAsync();

        var surface = Page.GetByTestId("period-settlement-data-surface");
        await surface.GetByRole(AriaRole.Button, new() { Name = "Xem đơn", Exact = true }).First.ClickAsync();
        await Page.GetByRole(
                AriaRole.Button,
                new() { Name = "Điều chỉnh sau chốt", Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    }

    private async Task OpenSettlementAsync(int year, int month)
    {
        await Page.GotoAsync(
            $"{BaseUrl}dashboard?tab=5&periodTab=settle",
            new() { WaitUntil = WaitUntilState.Load });
        await WaitForSettlementSurfaceAsync(year, month);
    }

    private async Task WaitForSettlementSurfaceAsync(int year, int month)
    {
        var surface = Page.Locator("[data-testid='period-settlement-data-surface']:visible");
        await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await surface.Locator(".vpp-skeleton-page").WaitForAsync(new()
        {
            State = WaitForSelectorState.Hidden,
            Timeout = 60_000
        });
        await surface.GetByText($"Kỳ {month:00}/{year}", new() { Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    }

    private async Task<ILocator> WaitForEnabledActionAsync(string accessibleName)
    {
        var action = await GetInteractiveButtonAsync(
            Page.GetByTestId("period-settlement-data-surface")
                .Locator(".vpp-collection-header-actions:visible"),
            accessibleName);
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (await action.IsEnabledAsync())
            {
                return action;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        var condition = await Page.Locator(".vpp-settlement-blocker-bar:visible")
            .AllInnerTextsAsync();
        throw new InvalidOperationException(
            $"Settlement action '{accessibleName}' remained disabled. " +
            $"Visible blockers: {string.Join(" | ", condition)}");
    }

    private async Task<HttpClient> CreateAuthorizedApiClientAsync(QaTestAccount account)
    {
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

    private async Task CaptureAsync(string fileName)
    {
        var configured = Environment.GetEnvironmentVariable(ScreenshotDirectoryEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return;
        }

        var directory = Path.GetFullPath(configured);
        Directory.CreateDirectory(directory);
        await Page.ScreenshotAsync(new()
        {
            Path = Path.Combine(directory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide,
            Scale = ScreenshotScale.Css
        });
    }

    private static async Task<List<SettlementRevisionResDTO>> WaitForRevisionCountAsync(
        HttpClient client,
        int year,
        int month,
        int expectedCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        List<SettlementRevisionResDTO> revisions = [];
        while (DateTime.UtcNow < deadline)
        {
            revisions = await GetRevisionsAsync(client, year, month);
            if (revisions.Count == expectedCount)
            {
                return revisions;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        throw new InvalidOperationException(
            $"Expected {expectedCount} settlement revisions for {month:00}/{year}, " +
            $"but observed {revisions.Count}.");
    }

    private static async Task<List<SettlementRevisionResDTO>> GetRevisionsAsync(
        HttpClient client,
        int year,
        int month)
    {
        var revisions = await client.GetFromJsonAsync<List<SettlementRevisionResDTO>>(
            $"/api/periodsettlement/revisions/{year}/{month}",
            TestContext.Current.CancellationToken);
        return revisions ?? [];
    }
}
