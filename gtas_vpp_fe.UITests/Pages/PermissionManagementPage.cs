using Microsoft.Playwright;
using System.Threading.Tasks;

namespace gtas_vpp_fe.UITests.Pages
{
    public class PermissionManagementPage
    {
        private readonly IPage _page;
        private readonly ILocator _sidebarReportLink;

        public PermissionManagementPage(IPage page)
        {
            _page = page;
            _sidebarReportLink = _page.Locator("a[href='/report']");
        }

        public async Task GotoAsync(string baseUrl)
        {
            await _page.GotoAsync($"{baseUrl}permission?tab=1");
            await _page.Locator(".permission-group-grid").WaitForAsync();
        }

        public async Task SetComponentVisibilityAsync(
            string groupName,
            string pageTabName,
            string componentCode,
            bool isVisible)
        {
            await ExpandGroupAsync(groupName, pageTabName);
            await SelectGroupPageTabAsync(pageTabName);

            var componentRow = await GetComponentRowAsync(componentCode);
            var visibleSwitch = componentRow.Locator(".rz-switch").Nth(1);
            var switchInput = visibleSwitch.Locator("input[type='checkbox']").First;

            await visibleSwitch.WaitForAsync();
            await switchInput.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached
            });
            if (await switchInput.IsCheckedAsync() == isVisible)
            {
                return;
            }

            await visibleSwitch.ClickAsync();
            await _page.GetByText("Permission updated", new PageGetByTextOptions { Exact = false }).Last.WaitForAsync();
        }

        public async Task WaitForReportMenuVisibleAsync()
        {
            await _sidebarReportLink.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 60000
            });
        }

        public async Task WaitForReportMenuHiddenAsync()
        {
            await _sidebarReportLink.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 60000
            });
        }

        private async Task ExpandGroupAsync(string groupName, string pageTabName)
        {
            var pageTab = _page.GetByRole(AriaRole.Tab, new() { Name = pageTabName, Exact = true });
            if (await pageTab.CountAsync() > 0 && await pageTab.First.IsVisibleAsync())
            {
                return;
            }

            var groupRow = await GetGroupRowAsync(groupName);
            var toggler = groupRow.Locator(".rz-row-toggler").First;

            await toggler.WaitForAsync();
            await toggler.ClickAsync();
            await pageTab.First.WaitForAsync();
        }

        private async Task SelectGroupPageTabAsync(string pageTabName)
        {
            var pageTab = _page.GetByRole(AriaRole.Tab, new() { Name = pageTabName, Exact = true }).First;
            await pageTab.WaitForAsync();
            await pageTab.ClickAsync();
        }

        private async Task<ILocator> GetGroupRowAsync(string groupName)
        {
            var groupCell = _page
                .Locator(".permission-group-grid")
                .GetByText(groupName, new LocatorGetByTextOptions { Exact = true })
                .First;

            try
            {
                await groupCell.WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
            }
            catch (TimeoutException exception)
            {
                var pageText = await _page.Locator("body").InnerTextAsync();
                var compactPageText = pageText.Length > 1500 ? pageText[..1500] + "..." : pageText;
                throw new InvalidOperationException(
                    $"Group '{groupName}' was not rendered. Page text: {compactPageText}",
                    exception);
            }
            return groupCell.Locator("xpath=ancestor::tr[contains(@class,'rz-data-row')]").First;
        }

        private async Task<ILocator> GetComponentRowAsync(string componentCode)
        {
            var componentCell = _page
                .Locator(".rz-expanded-row-content")
                .First
                .GetByText(componentCode, new LocatorGetByTextOptions { Exact = true })
                .First;

            await componentCell.WaitForAsync();
            return componentCell.Locator("xpath=ancestor::tr[contains(@class,'rz-data-row')]").First;
        }
    }
}
