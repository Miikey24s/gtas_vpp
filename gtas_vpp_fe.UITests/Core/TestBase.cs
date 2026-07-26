using gtas_vpp_test_support;
using Microsoft.Playwright;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace gtas_vpp_fe.UITests.Core;

public abstract class TestBase : IAsyncLifetime
{
    private const string DefaultDockerBaseUrl = "http://127.0.0.1:5000/";

    private IsolatedE2EStack? _ownedStack;
    private IBrowserContext? _browserContext;
    private QaTestAccounts? _accounts;

    protected IPage Page { get; private set; } = null!;

    protected string BaseUrl { get; private set; } = null!;

    protected string TestUsername => TestAccounts.SystemAdmin.Username;

    protected string TestPassword => TestAccounts.SystemAdmin.Password;

    protected QaTestAccounts TestAccounts => _accounts
        ?? throw new InvalidOperationException("Authenticated UI tests require a harness-owned QA fixture.");

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

            // Deliberately kept per-test (defense-in-depth): the env contract must hold
            // for every test even though the app fixtures are shared.
            QaUiSafetyContract.EnsureRunAllowed(
                requiresAuthenticatedFixture,
                mutatesServerState,
                isolatedOptIn,
                mutationOptIn);

            var cancellationToken = TestContext.Current.CancellationToken;
            if (mutatesServerState)
            {
                // REFACTOR-001-R0 Phase A: mutating tests keep the proven per-test stack.
                // A rerun against an already-mutated database does not reproduce the first
                // run (cancelled seed orders, pre-existing usernames), so each mutating
                // test still gets its own fresh LocalDB + app.
                _ownedStack = await IsolatedE2EStack.StartAsync(cancellationToken);
                BaseUrl = _ownedStack.BaseUrl;
                _accounts = _ownedStack.Fixture.Accounts;
            }
            else if (requiresAuthenticatedFixture
                     || string.Equals(isolatedOptIn, "1", StringComparison.Ordinal))
            {
                var sharedApp = await TestContext.Current.GetFixture<SharedE2EAppFixture>()
                    ?? throw new InvalidOperationException(
                        $"{GetType().Name} needs the shared E2E app: annotate the test class " +
                        "with [Collection(ReadOnlyE2ECollection.Name)] (audit it as read-only " +
                        "first) or mark it as IMutatingUiTest.");
                BaseUrl = await sharedApp.GetOrStartAsync(cancellationToken);
                _accounts = sharedApp.Accounts;
            }
            else
            {
                BaseUrl = await ResolveAnonymousLocalBaseUrlAsync(cancellationToken);
            }

            var browserFixture = await TestContext.Current.GetFixture<PlaywrightBrowserFixture>()
                ?? throw new InvalidOperationException(
                    "PlaywrightBrowserFixture is not registered as an assembly fixture.");
            var browser = await browserFixture.GetBrowserAsync();

            // One fresh BrowserContext per test: cookies, localStorage and the Blazor
            // circuit stay exactly as isolated as the old one-browser-per-test model.
            _browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "vi-VN",
                TimezoneId = "Asia/Ho_Chi_Minh"
            });
            Page = await _browserContext.NewPageAsync();
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

            if (_browserContext is not null)
            {
                await _browserContext.DisposeAsync();
            }
        }
        finally
        {
            // Shared fixtures (browser, read-only app) are owned by xUnit; only the
            // per-test mutating stack belongs to this instance.
            if (_ownedStack is not null)
            {
                await _ownedStack.DisposeAsync();
            }
        }
    }

    protected async Task LoginAsDefaultUserAsync()
        => await LoginAsAsync(TestAccounts.SystemAdmin);

    protected async Task LoginAsAsync(QaTestAccount account)
    {
        await Page.GotoAsync(
            $"{BaseUrl}set-language?culture=vi&returnUrl=%2FAccount%2FLogin",
            new PageGotoOptions
            {
                Timeout = 120_000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
        var loginPage = new Pages.Auth.LoginPage(Page);
        await loginPage.GotoAsync(BaseUrl);
        await loginPage.LoginAsync(account.Username, account.Password);
        await loginPage.WaitForDashboardAsync();
    }

    protected async Task SwitchUserAsync(QaTestAccount account)
    {
        await Page.GotoAsync($"{BaseUrl}perform-logout", new PageGotoOptions
        {
            Timeout = 120_000,
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        var loginUrl = new System.Text.RegularExpressions.Regex(
            ".*/Account/Login.*",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var timeoutAt = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < timeoutAt && !loginUrl.IsMatch(Page.Url))
        {
            await Task.Delay(200);
        }

        if (!loginUrl.IsMatch(Page.Url))
        {
            throw new TimeoutException($"Timed out waiting for logout redirect. Last URL: {Page.Url}");
        }

        await Page.Locator(".vpp-login-form").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await LoginAsAsync(account);
    }

    protected Task<ILocator> GetInteractiveButtonAsync(
        ILocator scope,
        string accessibleName,
        bool exact = true)
        => GetInteractiveButtonAsync(
            scope.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
            {
                Name = accessibleName,
                Exact = exact
            }),
            accessibleName);

    protected Task<ILocator> GetInteractiveButtonAsync(
        ILocator scope,
        System.Text.RegularExpressions.Regex accessibleName)
        => GetInteractiveButtonAsync(
            scope.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
            {
                NameRegex = accessibleName
            }),
            accessibleName.ToString());

    private static async Task<ILocator> GetInteractiveButtonAsync(
        ILocator candidates,
        string description)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        var lastCount = 0;
        do
        {
            lastCount = await candidates.CountAsync();
            for (var index = lastCount - 1; index >= 0; index--)
            {
                var candidate = candidates.Nth(index);
                if (!await candidate.IsVisibleAsync())
                {
                    continue;
                }

                // Blazor only stamps "_bl_" ElementReference attributes on elements captured
                // via @ref (e.g. Radzen component roots). Atlas quiet row actions are plain
                // HTML buttons with @onclick, so accept a marker on the element OR any
                // ancestor: both only appear after the live interactive circuit has rendered
                // the subtree, which is the prerender race this heuristic guards against.
                var hasBlazorBinding = await candidate.EvaluateAsync<bool>(
                    """
                    element => {
                        for (let node = element; node; node = node.parentElement) {
                            if (Array.from(node.attributes).some(attribute => attribute.name.startsWith('_bl_'))) {
                                return true;
                            }
                        }
                        return false;
                    }
                    """);
                if (hasBlazorBinding)
                {
                    return candidate;
                }
            }

            await Task.Delay(100);
        } while (DateTime.UtcNow < deadline);

        throw new InvalidOperationException(
            $"No visible Blazor-interactive button matched '{description}' within 30 seconds. Candidate count: {lastCount}.");
    }

    private static async Task<string> ResolveAnonymousLocalBaseUrlAsync(
        CancellationToken cancellationToken)
    {
        var configuredBaseUrl = IsolatedE2EStack.NormalizeBaseUrl(
            Environment.GetEnvironmentVariable("UITEST_BASE_URL"));
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            QaUiSafetyContract.EnsureLoopbackUrl(new Uri(configuredBaseUrl), "UITEST_BASE_URL");
            if (await IsolatedE2EStack.IsBaseUrlReadyAsync(configuredBaseUrl, cancellationToken))
            {
                return configuredBaseUrl;
            }
        }

        if (await IsolatedE2EStack.IsBaseUrlReadyAsync(DefaultDockerBaseUrl, cancellationToken))
        {
            return DefaultDockerBaseUrl;
        }

        throw new InvalidOperationException(
            $"No loopback frontend is ready. Set {QaUiSafetyContract.IsolatedRunEnvironmentVariable}=1 " +
            "to start a disposable Aspire/LocalDB stack.");
    }
}
