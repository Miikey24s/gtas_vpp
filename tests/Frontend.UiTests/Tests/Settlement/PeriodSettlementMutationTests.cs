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
    private const string SameUserCorrectionReason = "Kiểm tra quy tắc bốn mắt cùng người";
    private const string ManagerCorrectionReason = "Hiệu chỉnh kỳ sau khi người thứ hai rà soát";

    [Fact]
    public async Task Settlement_ConfirmThenCorrection_RequiresAnotherAuthorizedUserAndPreservesRevisions()
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
        await Page.GetByRole(AriaRole.Button, new() { Name = "Tạo phiên bản hiệu chỉnh", Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var firstRevision = revisionsAfterConfirm.Single();
        firstRevision.RevisionNumber.Should().Be(1);
        firstRevision.IsCurrentRevision.Should().BeTrue();
        firstRevision.IsCorrection.Should().BeFalse();
        firstRevision.ConfirmedByUserId.Should().Be(TestAccounts.Procurement.UserId);

        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load });
        await WaitForSettlementSurfaceAsync(targetYear, targetMonth);
        await SubmitCorrectionAsync(SameUserCorrectionReason);
        await Page.GetByText(
                "Four-eyes control requires another procurement user to confirm the correction.",
                new() { Exact = false })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });

        var revisionsAfterRejectedCorrection = await GetRevisionsAsync(procurementApi, targetYear, targetMonth);
        revisionsAfterRejectedCorrection.Should().ContainSingle(
            "cùng người xác nhận không được tạo thêm phiên bản chốt kỳ");

        await SwitchUserAsync(TestAccounts.Manager);
        await OpenSettlementAsync(targetYear, targetMonth);
        await SubmitCorrectionAsync(ManagerCorrectionReason);
        await Page.GetByText("Đã tạo phiên bản hiệu chỉnh", new() { Exact = false })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });

        using var managerApi = await CreateAuthorizedApiClientAsync(TestAccounts.Manager);
        var revisions = await WaitForRevisionCountAsync(managerApi, targetYear, targetMonth, 2);
        var ordered = revisions.OrderBy(revision => revision.RevisionNumber).ToArray();
        var original = ordered[0];
        var correction = ordered[1];

        original.RevisionNumber.Should().Be(1);
        original.IsCurrentRevision.Should().BeFalse();
        original.IsCorrection.Should().BeFalse();
        original.ConfirmedByUserId.Should().Be(TestAccounts.Procurement.UserId);

        correction.RevisionNumber.Should().Be(2);
        correction.IsCurrentRevision.Should().BeTrue();
        correction.IsCorrection.Should().BeTrue();
        correction.ConfirmedByUserId.Should().Be(TestAccounts.Manager.UserId);
        correction.SupersedesSettlementId.Should().Be(original.Id);
        correction.CorrectionReason.Should().Be(ManagerCorrectionReason);
        revisions.Count(revision => revision.IsCurrentRevision).Should().Be(1);
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
            Page.Locator(".vpp-settlement-decision-action:visible"),
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

    private async Task SubmitCorrectionAsync(string reason)
    {
        var correctionButton = await WaitForEnabledActionAsync("Tạo phiên bản hiệu chỉnh");
        await correctionButton.ClickAsync();

        var dialog = Page.Locator("[data-testid='settlement-correction-dialog']:visible");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await dialog.Locator("textarea").FillAsync(reason);
        await dialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Tạo phiên bản hiệu chỉnh", Exact = true })
            .ClickAsync();
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 60_000 });
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
