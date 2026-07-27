using Microsoft.Playwright;
using System.Threading.Tasks;

namespace gtas_vpp_fe.UITests.Pages.Order
{
    public class DepartmentSummaryPage
    {
        private readonly IPage _page;

        public DepartmentSummaryPage(IPage page)
        {
            _page = page;
        }

        public async Task NavigateToDepartmentSummaryTabAsync()
        {
            await _page.Locator("text='Department Summary'").ClickAsync();
        }

        public async Task<int> GetGridRowCountAsync()
        {
            return await _page.Locator(".rz-datatable-data > tbody > tr").CountAsync();
        }
    }
}
