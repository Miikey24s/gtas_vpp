using Microsoft.Playwright;
using System.Threading.Tasks;

namespace gtas_vpp_fe.UITests.Pages.Order
{
    public class AllOrdersSummaryPage
    {
        private readonly IPage _page;

        public AllOrdersSummaryPage(IPage page)
        {
            _page = page;
        }

        public async Task NavigateToAllOrdersSummaryTabAsync()
        {
            await _page.Locator("text='All Orders Summary'").ClickAsync();
        }
        
        public async Task<int> GetGridRowCountAsync()
        {
            return await _page.Locator(".rz-datatable-data > tbody > tr").CountAsync();
        }
    }
}
