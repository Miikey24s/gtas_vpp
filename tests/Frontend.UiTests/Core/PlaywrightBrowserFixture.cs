using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

[assembly: AssemblyFixture(typeof(PlaywrightBrowserFixture))]

namespace gtas_vpp_fe.UITests.Core;

/// <summary>
/// Assembly fixture that owns the single Chromium process for the whole suite
/// (REFACTOR-001-R0 S1). Tests isolate themselves with one fresh
/// <see cref="IBrowserContext"/> per test instead of one browser per test.
/// The browser is launched lazily so non-browser tests never pay for it.
/// </summary>
public sealed class PlaywrightBrowserFixture : IAsyncLifetime
{
    private readonly SemaphoreSlim _launchGate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Returns the shared browser, relaunching it if Chromium died mid-suite so a
    /// single crash does not cascade into failures for every remaining test.
    /// </summary>
    public async Task<IBrowser> GetBrowserAsync()
    {
        await _launchGate.WaitAsync();
        try
        {
            if (_browser is { IsConnected: true })
            {
                return _browser;
            }

            if (_browser is not null)
            {
                await _browser.DisposeAsync();
                _browser = null;
            }

            _playwright ??= await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = GetHeadlessMode(),
                SlowMo = GetSlowMo(),
                ExecutablePath = GetBrowserExecutablePath()
            });
            return _browser;
        }
        finally
        {
            _launchGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_browser is not null)
            {
                await _browser.CloseAsync();
                await _browser.DisposeAsync();
            }

            _playwright?.Dispose();
        }
        finally
        {
            _launchGate.Dispose();
        }
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
}
