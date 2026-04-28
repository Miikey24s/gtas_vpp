using Microsoft.Playwright;
using System.Threading.Tasks;

namespace gtas_vpp_fe.UITests.Pages.Order
{
    public class ProductCatalogPage
    {
        private readonly IPage _page;

        public ProductCatalogPage(IPage page)
        {
            _page = page;
        }

        public async Task NavigateToProductCatalogTabAsync()
        {
            await _page.Locator("text='Product Catalog'").ClickAsync();
        }

        public async Task<int> GetGridRowCountAsync()
        {
            return await _page.Locator(".rz-datatable-data > tbody > tr").CountAsync();
        }
    }
}
