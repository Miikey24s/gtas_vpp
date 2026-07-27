using Microsoft.Playwright;
using System.Threading.Tasks;

namespace gtas_vpp_fe.UITests.Pages.Order
{
    public class OrderManagementPage
    {
        private readonly IPage _page;

        public OrderManagementPage(IPage page)
        {
            _page = page;
        }

        public async Task NavigateToAdminApprovalTabAsync()
        {
            // Click vào tab Admin Approval
            await _page.Locator("text='Admin Approval'").ClickAsync();
        }

        public async Task ClickApproveFirstOrderAsync()
        {
            await _page.Locator("button[title='Approve']").First.ClickAsync();
        }

        public async Task ClickRejectFirstOrderAsync()
        {
            await _page.Locator("button[title='Reject']").First.ClickAsync();
        }
    }
}
