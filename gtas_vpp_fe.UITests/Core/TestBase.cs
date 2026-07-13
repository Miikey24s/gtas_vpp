using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace gtas_vpp_fe.UITests.Core
{
    public abstract class TestBase : IAsyncLifetime
    {
        private const string DefaultDockerBaseUrl = "http://127.0.0.1:5000/";

        private DistributedApplication? _app;
        private IPlaywright? _playwright;
        private IBrowser? _browser;

        protected IPage Page { get; private set; } = null!;
        protected string BaseUrl { get; private set; } = null!;
        protected string TestUsername => GetRequiredEnvironmentVariable("GTAS_TEST_USERNAME");
        protected string TestPassword => GetRequiredEnvironmentVariable("GTAS_TEST_PASSWORD");

        public async ValueTask InitializeAsync()
        {
            BaseUrl = await ResolveBaseUrlAsync();

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = GetHeadlessMode(),
                SlowMo = GetSlowMo()
            });
            Page = await _browser.NewPageAsync();
            Page.SetDefaultTimeout(60000);
            Page.SetDefaultNavigationTimeout(120000);
        }

        public async ValueTask DisposeAsync()
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

        protected async Task LoginAsDefaultUserAsync()
        {
            var loginPage = new Pages.Auth.LoginPage(Page);
            await loginPage.GotoAsync(BaseUrl);
            await loginPage.LoginAsync(TestUsername, TestPassword);
            await loginPage.WaitForDashboardAsync();
        }

        private static string GetRequiredEnvironmentVariable(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{name}' is required for authenticated UI tests.");
            }

            return value;
        }

        private static bool GetHeadlessMode()
        {
            var value = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS");
            return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
        }

        private static float? GetSlowMo()
        {
            var value = Environment.GetEnvironmentVariable("PLAYWRIGHT_SLOWMO_MS");
            return float.TryParse(value, out var slowMo) ? slowMo : 0;
        }

        private async Task<string> ResolveBaseUrlAsync()
        {
            var configuredBaseUrl = NormalizeBaseUrl(Environment.GetEnvironmentVariable("UITEST_BASE_URL"));
            if (!string.IsNullOrWhiteSpace(configuredBaseUrl) && await IsBaseUrlReadyAsync(configuredBaseUrl))
            {
                return configuredBaseUrl;
            }

            if (await IsBaseUrlReadyAsync(DefaultDockerBaseUrl))
            {
                return DefaultDockerBaseUrl;
            }

            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyAspire_AppHost>();
            appHost.Services.AddLogging(logging =>
            {
                // UI tests must work without administrator rights. The default
                // Windows EventLog provider tries to write to ".NET Runtime"
                // and can fail before Aspire starts any resource.
                logging.ClearProviders();
                logging.AddConsole();
            });
            _app = await appHost.BuildAsync();
            await _app.StartAsync();

            var httpClient = _app.CreateHttpClient("frontend");
            var aspireBaseUrl = NormalizeBaseUrl(httpClient.BaseAddress?.ToString()) ?? DefaultDockerBaseUrl;
            await WaitForBaseUrlReadyAsync(aspireBaseUrl);
            return aspireBaseUrl;
        }

        private static string? NormalizeBaseUrl(string? baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        }

        private static async Task<bool> IsBaseUrlReadyAsync(string baseUrl)
        {
            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };

                using var response = await httpClient.GetAsync(baseUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static async Task WaitForBaseUrlReadyAsync(string baseUrl)
        {
            for (var attempt = 0; attempt < 24; attempt++)
            {
                if (await IsBaseUrlReadyAsync(baseUrl))
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            throw new InvalidOperationException($"Frontend host '{baseUrl}' did not become ready in time.");
        }
    }
}
