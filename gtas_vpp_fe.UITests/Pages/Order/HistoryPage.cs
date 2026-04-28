using Microsoft.Playwright;
using System.Threading.Tasks;

namespace gtas_vpp_fe.UITests.Pages.Order
{
    public class HistoryPage
    {
        private readonly IPage _page;

        public HistoryPage(IPage page)
        {
            _page = page;
        }

        public async Task NavigateToHistoryTabAsync()
        {
            await _page.Locator("text='History'").ClickAsync();
        }

        public async Task<int> GetGridRowCountAsync()
        {
            // Count rows in the main data grid body
            return await _page.Locator(".rz-datatable-data > tbody > tr").CountAsync();
        }
    }
}
