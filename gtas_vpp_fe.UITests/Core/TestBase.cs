using Aspire.Hosting;
using Aspire.Hosting.Testing;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_test_support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Net.Http.Json;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace gtas_vpp_fe.UITests.Core;

public abstract class TestBase : IAsyncLifetime
{
    private const string DefaultDockerBaseUrl = "http://127.0.0.1:5000/";

    private DistributedApplication? _app;
    private LocalDbQaFixture? _fixture;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    protected IPage Page { get; private set; } = null!;

    protected string BaseUrl { get; private set; } = null!;

    protected string TestUsername => _fixture?.Accounts.SystemAdmin.Username
        ?? throw new InvalidOperationException("Authenticated UI tests require a harness-owned QA account.");

    protected string TestPassword => _fixture?.Accounts.SystemAdmin.Password
        ?? throw new InvalidOperationException("Authenticated UI tests require a harness-owned QA account.");

    public async ValueTask InitializeAsync()
    {
        try
        {
            var requiresAuthenticatedFixture = this is IAuthenticatedUiTest;
            var mutatesServerState = this is IMutatingUiTest;
            var isolatedOptIn = Environment.GetEnvironmentVariable(
                QaUiSafetyContract.IsolatedRunEnvironmentVariable);
            var mutationOptIn = Environment.GetEnvironmentVariable(
                QaUiSafetyContract.MutationOptInEnvironmentVariable);

            QaUiSafetyContract.EnsureRunAllowed(
                requiresAuthenticatedFixture,
                mutatesServerState,
                isolatedOptIn,
                mutationOptIn);

            BaseUrl = requiresAuthenticatedFixture
                      || string.Equals(isolatedOptIn, "1", StringComparison.Ordinal)
                ? await StartIsolatedApplicationAsync(TestContext.Current.CancellationToken)
                : await ResolveAnonymousLocalBaseUrlAsync(TestContext.Current.CancellationToken);

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = GetHeadlessMode(),
                SlowMo = GetSlowMo(),
                ExecutablePath = GetBrowserExecutablePath()
            });
            Page = await _browser.NewPageAsync();
            Page.SetDefaultTimeout(60_000);
            Page.SetDefaultNavigationTimeout(120_000);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Page is not null)
            {
                await Page.CloseAsync();
            }

            if (_browser is not null)
            {
                await _browser.CloseAsync();
                await _browser.DisposeAsync();
            }

            _playwright?.Dispose();
        }
        finally
        {
            try
            {
                if (_app is not null)
                {
                    await _app.DisposeAsync();
                }
            }
            finally
            {
                if (_fixture is not null)
                {
                    await _fixture.DisposeAsync();
                }
            }
        }
    }

    protected async Task LoginAsDefaultUserAsync()
    {
        var loginPage = new Pages.Auth.LoginPage(Page);
        await loginPage.GotoAsync(BaseUrl);
        await loginPage.LoginAsync(TestUsername, TestPassword);
        await loginPage.WaitForDashboardAsync();
    }

    private async Task<string> StartIsolatedApplicationAsync(CancellationToken cancellationToken)
    {
        await LocalDbQaFixture.RecoverStaleAsync(cancellationToken: cancellationToken);
        _fixture = await LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken);
        var args = new[]
        {
            $"--Parameters:test-database-connection-string={_fixture.ConnectionString}",
            $"--Parameters:jwt-key={_fixture.Secrets.JwtKey}",
            $"--Parameters:qa-fixture-run-id={_fixture.Options.RunId}"
        };
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MyAspire_AppHost>(args, cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            // The Windows EventLog provider can require administrator rights.
            logging.ClearProviders();
            logging.AddConsole();
        });

        _app = await appHost.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        using var backendClient = _app.CreateHttpClient("backend");
        QaUiSafetyContract.EnsureLoopbackUrl(
            backendClient.BaseAddress
            ?? throw new InvalidOperationException("Aspire backend endpoint has no address."),
            "Aspire backend endpoint");
        await WaitForConfirmedIdentityAsync(backendClient, _fixture, cancellationToken);

        using var frontendClient = _app.CreateHttpClient("frontend");
        var frontendBaseUrl = NormalizeBaseUrl(frontendClient.BaseAddress?.ToString())
            ?? throw new InvalidOperationException("Aspire frontend endpoint has no address.");
        QaUiSafetyContract.EnsureLoopbackUrl(new Uri(frontendBaseUrl), "Aspire frontend endpoint");
        await WaitForBaseUrlReadyAsync(frontendBaseUrl, cancellationToken);
        return frontendBaseUrl;
    }

    private static async Task WaitForConfirmedIdentityAsync(
        HttpClient backendClient,
        LocalDbQaFixture fixture,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 24; attempt++)
        {
            try
            {
                using var response = await backendClient.GetAsync(
                    "/internal/qa/database-identity",
                    cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var identity = await response.Content.ReadFromJsonAsync<QaFixtureIdentityResponse>(
                        cancellationToken: cancellationToken);
                    if (identity is not null
                        && string.Equals(identity.Purpose, QaFixtureIdentityContract.Purpose, StringComparison.Ordinal)
                        && string.Equals(identity.RunId, fixture.Options.RunId, StringComparison.Ordinal)
                        && string.Equals(identity.FixtureVersion, QaFixtureIdentityContract.FixtureVersion, StringComparison.Ordinal)
                        && string.Equals(identity.Environment, QaFixtureIdentityContract.HostEnvironment, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(identity.DatabaseName, fixture.Options.DatabaseName, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }
            catch (HttpRequestException) when (attempt < 23)
            {
                // The backend can still be starting its reference-data check.
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new InvalidOperationException(
            "Backend did not confirm the harness-owned TEST database identity; browser launch is blocked.");
    }

    private static async Task<string> ResolveAnonymousLocalBaseUrlAsync(
        CancellationToken cancellationToken)
    {
        var configuredBaseUrl = NormalizeBaseUrl(Environment.GetEnvironmentVariable("UITEST_BASE_URL"));
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            QaUiSafetyContract.EnsureLoopbackUrl(new Uri(configuredBaseUrl), "UITEST_BASE_URL");
            if (await IsBaseUrlReadyAsync(configuredBaseUrl, cancellationToken))
            {
                return configuredBaseUrl;
            }
        }

        if (await IsBaseUrlReadyAsync(DefaultDockerBaseUrl, cancellationToken))
        {
            return DefaultDockerBaseUrl;
        }

        throw new InvalidOperationException(
            $"No loopback frontend is ready. Set {QaUiSafetyContract.IsolatedRunEnvironmentVariable}=1 " +
            "to start a disposable Aspire/LocalDB stack.");
    }

    private static string? GetBrowserExecutablePath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("UITEST_BROWSER_EXECUTABLE");
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(configuredPath);
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                $"UITEST_BROWSER_EXECUTABLE does not exist: '{fullPath}'.");
        }

        return fullPath;
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

    private static string? NormalizeBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
    }

    private static async Task<bool> IsBaseUrlReadyAsync(
        string baseUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await httpClient.GetAsync(baseUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private static async Task WaitForBaseUrlReadyAsync(
        string baseUrl,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 24; attempt++)
        {
            if (await IsBaseUrlReadyAsync(baseUrl, cancellationToken))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new InvalidOperationException($"Frontend host '{baseUrl}' did not become ready in time.");
    }
}
