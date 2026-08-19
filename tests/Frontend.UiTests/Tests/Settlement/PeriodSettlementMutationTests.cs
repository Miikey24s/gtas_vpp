using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
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
    public async Task Settlement_ExportsCompletePdfAndWorkbookAfterConfirmation()
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
        var previewDialog = Page.GetByTestId("settlement-preview-dialog");
        await previewDialog.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await previewDialog.GetByRole(AriaRole.Button, new() { Name = "Chốt kỳ", Exact = true }).ClickAsync();
        _ = await WaitForRevisionCountAsync(procurementApi, targetYear, targetMonth, 1);
        await Page.GetByText("Đã chốt kỳ", new() { Exact = true }).WaitForAsync();

        var pdf = await DownloadSettlementAsync("PDF", ".pdf");
        var workbook = await DownloadSettlementAsync("Excel", ".xlsx");

        using var archive = ZipFile.OpenRead(workbook.Path);
        var workbookXml = ReadArchiveEntry(archive, "xl/workbook.xml");
        workbookXml.Should().Contain("Tổng quan");
        workbookXml.Should().Contain("Mặt hàng");
        workbookXml.Should().Contain("Phân bổ");

        var worksheetText = string.Join(
            "\n",
            archive.Entries
                .Where(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal))
                .Select(entry => ReadArchiveEntry(archive, entry.FullName)));
        worksheetText.Should().Contain("Thành tiền trước VAT");
        worksheetText.Should().Contain("Tổng cộng");
        worksheetText.Should().Contain("Mã đơn");
        worksheetText.Should().Contain("Người đặt");

        pdf.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(15));
        workbook.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(15));
    }

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
        var previewDialog = Page.GetByTestId("settlement-preview-dialog");
        await previewDialog.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await previewDialog.GetByRole(AriaRole.Button, new() { Name = "Chốt kỳ", Exact = true }).ClickAsync();

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
                new() { Name = "Chốt lại kỳ", Exact = true })
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

        // Toast của bước chốt trước không thuộc dialog điều chỉnh và không được che bằng chứng visual.
        var settlementToast = Page.Locator(".rz-notification:visible");
        if (await settlementToast.CountAsync() > 0)
        {
            var closeToastButton = settlementToast.Last
                .GetByRole(AriaRole.Button)
                .Last;
            if (await closeToastButton.CountAsync() > 0)
            {
                await closeToastButton.ClickAsync();
            }
        }

        var surface = Page.GetByTestId("period-settlement-data-surface");
        await surface.GetByRole(AriaRole.Button, new() { Name = "Xem đơn", Exact = true }).First.ClickAsync();
        await Page.GetByRole(
                AriaRole.Button,
                new() { Name = "Điều chỉnh sau chốt", Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });

        await Page.GetByRole(
                AriaRole.Button,
                new() { Name = "Điều chỉnh sau chốt", Exact = true })
            .ClickAsync();
        var correctionDialog = Page.GetByTestId("post-settlement-order-correction-dialog");
        await correctionDialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // Luồng điều chỉnh chỉ thao tác trên các mặt hàng đã có trong đơn, không thêm mặt hàng mới.
        await Assertions.Expect(correctionDialog.Locator(".vpp-order-correction-items"))
            .ToBeVisibleAsync();
        await Assertions.Expect(correctionDialog.Locator(".vpp-order-correction-item"))
            .ToHaveCountAsync(1);
        var correctionItemDisplay = await correctionDialog.Locator(".vpp-order-correction-item")
            .EvaluateAsync<string>("node => getComputedStyle(node).display");
        correctionItemDisplay.Should().Be("grid", "scoped CSS của dialog phải được nạp trước khi kiểm tra hình ảnh");
        await Assertions.Expect(correctionDialog.GetByText("Đổi số lượng: 0", new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(correctionDialog.GetByText("Bỏ khỏi đơn: 0", new() { Exact = true }))
            .ToBeVisibleAsync();

        var quantityInput = correctionDialog.Locator(".vpp-order-correction-item input").First;
        await quantityInput.FillAsync("3");
        await Assertions.Expect(correctionDialog.GetByText("Đổi số lượng: 1", new() { Exact = true }))
            .ToBeVisibleAsync();

        await correctionDialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Bỏ khỏi đơn", Exact = true })
            .ClickAsync();
        await Assertions.Expect(correctionDialog.GetByText("Sẽ bỏ", new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(correctionDialog.GetByText("Bỏ khỏi đơn: 1", new() { Exact = true }))
            .ToBeVisibleAsync();
        var undoButton = correctionDialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Hoàn tác", Exact = true });
        await Assertions.Expect(undoButton).ToBeEnabledAsync();
        var undoOpacity = await undoButton.EvaluateAsync<double>(
            "node => Number.parseFloat(getComputedStyle(node).opacity)");
        undoOpacity.Should().BeGreaterThanOrEqualTo(
            0.9,
            "hàng bị bỏ có thể làm mờ dữ liệu nhưng không được làm mờ thao tác Hoàn tác");
        var removeAllItemsWarning = correctionDialog.GetByText(
            "Phải giữ ít nhất một mặt hàng. Nếu không còn nhu cầu, chọn Hủy toàn bộ đơn.",
            new() { Exact = true });
        await Assertions.Expect(removeAllItemsWarning).ToBeVisibleAsync();
        await Assertions.Expect(correctionDialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Gửi yêu cầu", Exact = true }))
            .ToBeDisabledAsync();
        await removeAllItemsWarning.EvaluateAsync(
            "node => node.scrollIntoView({ block: 'center', inline: 'nearest' })");
        await CaptureAsync("post-settlement-order-item-removal-1366x768.png");

        await correctionDialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Hoàn tác", Exact = true })
            .ClickAsync();
        await Assertions.Expect(quantityInput).ToHaveValueAsync("4");
        await Assertions.Expect(correctionDialog.GetByText("Đổi số lượng: 0", new() { Exact = true }))
            .ToBeVisibleAsync();

        await correctionDialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Thay đổi", Exact = true })
            .ClickAsync();
        await Page.GetByRole(
                AriaRole.Option,
                new() { Name = "Hủy toàn bộ đơn", Exact = true })
            .ClickAsync();
        await Assertions.Expect(correctionDialog.GetByText(
                "Đơn sẽ được hủy sau khi một quản lý khác duyệt. Bản chốt hiện tại vẫn được giữ đến khi kỳ được chốt lại.",
                new() { Exact = true }))
            .ToBeVisibleAsync();
        await CaptureAsync("post-settlement-order-correction-1366x768.png");
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

    private async Task<(string Path, TimeSpan Elapsed)> DownloadSettlementAsync(
        string buttonName,
        string expectedExtension)
    {
        var button = Page.GetByRole(AriaRole.Button, new() { Name = buttonName, Exact = true });
        await button.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await Assertions.Expect(button).ToBeEnabledAsync(new() { Timeout = 60_000 });

        var stopwatch = Stopwatch.StartNew();
        var download = await Page.RunAndWaitForDownloadAsync(() => button.ClickAsync());
        stopwatch.Stop();

        download.SuggestedFilename.Should().StartWith("GTAS-VPP-Chot-ky-");
        download.SuggestedFilename.Should().EndWith(expectedExtension);
        (await download.FailureAsync()).Should().BeNull();

        var path = await download.PathAsync();
        path.Should().NotBeNullOrWhiteSpace();
        var bytes = await File.ReadAllBytesAsync(path!, TestContext.Current.CancellationToken);
        bytes.Length.Should().BeGreaterThan(32);
        if (expectedExtension == ".pdf")
        {
            Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
        }
        else
        {
            bytes.Take(2).Should().Equal((byte)'P', (byte)'K');
        }

        return (path!, stopwatch.Elapsed);
    }

    private static string ReadArchiveEntry(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path);
        entry.Should().NotBeNull();
        using var reader = new StreamReader(entry!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
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
