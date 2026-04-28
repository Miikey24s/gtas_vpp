using Microsoft.Playwright;
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
            await _page.Locator("button.ocean-btn").ClickAsync();
        }
        
        public async Task ClickClearAllAsync()
        {
            await _page.Locator("button", new PageLocatorOptions { HasTextString = "Clear All" }).ClickAsync();
        }
    }
}
