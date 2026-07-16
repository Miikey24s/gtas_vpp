using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

public sealed class HistoryTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task Employee_CanOpenSeededRequestHistoryFromHistoryTab()
    {
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=1");

        var historyGrid = Page.Locator(".vpp-data-card.vpp-datagrid");
        await historyGrid.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        var rows = historyGrid.Locator("tbody tr");
        (await rows.CountAsync()).Should().BeGreaterThan(0);

        await rows.First.Locator("button[title='Xem lịch sử phiên bản']").ClickAsync();
        var dialog = Page.Locator(".rz-dialog:visible").Last;
        await dialog.GetByText("Vòng đời đơn yêu cầu", new() { Exact = false }).WaitForAsync();
        await dialog.GetByText("1 phiên bản", new() { Exact = false }).WaitForAsync();
        await dialog.GetByText("Dòng thời gian", new() { Exact = true }).WaitForAsync();
        await dialog.GetByText("Các phiên bản của đơn", new() { Exact = true }).WaitForAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Đóng", Exact = true }).ClickAsync();
    }
}
