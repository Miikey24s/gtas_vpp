using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace gtas_vpp_fe.UITests.Pages;

public sealed class PermissionManagementPage
{
    private readonly IPage page;
    private readonly ILocator sidebarReportLink;

    public PermissionManagementPage(IPage page)
    {
        this.page = page;
        sidebarReportLink = page.Locator("a[href='/report']");
    }

    public async Task GotoAsync(string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}permission?tab=1");
        await page.Locator("[data-testid='permission-groups-data-surface']").WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.permission-group-grid .permission-group-identity').length > 0");
    }

    public async Task SetComponentVisibilityAsync(
        string groupCode,
        string pageTabName,
        string componentCode,
        bool isVisible)
    {
        await OpenGroupEditorAsync(groupCode);

        var editor = page.Locator("[data-testid='permission-ui-batch-editor']");
        await editor.WaitForAsync();
        await SelectEditorPageAsync(editor, pageTabName);

        var componentRow = await GetEditorComponentRowAsync(editor, componentCode);
        var accessTrigger = componentRow.Locator(".vpp-filter-select-trigger").First;
        await accessTrigger.WaitForAsync();
        var desiredLabel = isVisible ? "Cho phép thao tác" : "Ẩn";
        if (string.Equals(await accessTrigger.GetAttributeAsync("title"), desiredLabel, StringComparison.Ordinal))
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Hủy", Exact = true }).Last.ClickAsync();
            await editor.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            return;
        }

        await accessTrigger.ClickAsync();
        var option = page.GetByRole(AriaRole.Option, new() { Name = desiredLabel, Exact = true }).Last;
        await option.WaitForAsync();
        await option.ClickAsync();

        var saveButton = page.GetByRole(AriaRole.Button, new() { Name = "Lưu thay đổi", Exact = true }).Last;
        await saveButton.ClickAsync();
        await editor.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
        await page.GetByText("Đã cập nhật phân quyền", new PageGetByTextOptions { Exact = false }).Last.WaitForAsync();
    }

    public async Task WaitForReportMenuVisibleAsync()
    {
        await sidebarReportLink.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
    }

    public async Task WaitForReportMenuHiddenAsync()
    {
        await sidebarReportLink.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden,
            Timeout = 60_000
        });
    }

    private async Task OpenGroupEditorAsync(string groupCode)
    {
        var groupCell = page
            .Locator(".permission-group-grid .permission-group-identity small")
            .GetByText(groupCode, new LocatorGetByTextOptions { Exact = true })
            .First;

        await groupCell.WaitForAsync();
        var groupRow = groupCell.Locator("xpath=ancestor::tr[contains(@class,'rz-data-row')]").First;
        var configureButton = groupRow.GetByRole(AriaRole.Button, new()
        {
            Name = "Cấu hình quyền UI",
            Exact = true
        });
        await configureButton.WaitForAsync();
        await configureButton.ClickAsync();
    }

    private static async Task SelectEditorPageAsync(ILocator editor, string pageTabName)
    {
        var normalizedName = Regex.Replace(pageTabName, @"\s*\([^)]*\)\s*$", string.Empty);
        var pageButton = editor.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex(Regex.Escape(normalizedName), RegexOptions.IgnoreCase)
        }).First;
        await pageButton.WaitForAsync();
        await pageButton.ClickAsync();
    }

    private static async Task<ILocator> GetEditorComponentRowAsync(ILocator editor, string componentCode)
    {
        var componentCell = editor.GetByText(componentCode, new LocatorGetByTextOptions { Exact = true }).First;
        await componentCell.WaitForAsync();
        return componentCell.Locator("xpath=ancestor::tr[contains(@class,'rz-data-row')]").First;
    }
}
