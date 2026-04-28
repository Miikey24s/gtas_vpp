using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace gtas_vpp_fe.UITests.Core
{
    public abstract class TestBase : IAsyncLifetime
    {
        private DistributedApplication? _app;
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        
        protected IPage Page { get; private set; } = null!;
        protected string BaseUrl { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyAspire_AppHost>();
            _app = await appHost.BuildAsync();
            await _app.StartAsync();

            var httpClient = _app.CreateHttpClient("frontend");
            BaseUrl = httpClient.BaseAddress!.ToString();

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 500
            });
            Page = await _browser.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            if (Page != null)
            {
                await Page.CloseAsync();
            }
            if (_browser != null)
            {
                await _browser.CloseAsync();
                await _browser.DisposeAsync();
            }
            _playwright?.Dispose();
            
            if (_app != null)
            {
                await _app.DisposeAsync();
            }
        }
    }
}
