using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace gtas_vpp_fe.UITests.Pages.Order
{
    public class OrderCreatePage
    {
        private readonly IPage _page;

        public OrderCreatePage(IPage page)
        {
            _page = page;
        }

        public async Task WaitForLoadedAsync()
        {
            await _page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Back to Orders", RegexOptions.IgnoreCase) }).WaitForAsync();
            await _page.GetByRole(AriaRole.Heading, new() { Name = "Product Catalog" }).WaitForAsync();
            await _page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Create Order", RegexOptions.IgnoreCase) }).WaitForAsync();
        }

        public async Task ClickBackToOrdersAsync()
        {
            await _page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Back to Orders", RegexOptions.IgnoreCase) }).ClickAsync();
        }

        public async Task<bool> IsEmptyCatalogVisibleAsync()
        {
            return await _page.GetByText("No records to display.").First.IsVisibleAsync();
        }

        public async Task ClickAddFirstProductAsync()
        {
            // RadzenButton with Icon="add" usually renders as <button ...><i class="rzi">add</i>...</button>
            await _page.Locator("button:has(i:text-is('add'))").First.ClickAsync();
        }

        public async Task FillNotesAsync(string notes)
        {
            await _page.Locator("textarea").First.FillAsync(notes);
        }

        public async Task SubmitOrderAsync()
        {
            await _page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Create Order", RegexOptions.IgnoreCase) }).ClickAsync();
        }
        
        public async Task ClickClearAllAsync()
        {
            await _page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Clear All", RegexOptions.IgnoreCase) }).ClickAsync();
        }
    }
}
