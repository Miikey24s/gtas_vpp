using gtas_vpp_test_support;

namespace gtas_vpp_fe.UITests.Core;

/// <summary>
/// Collection fixture that shares one isolated app + QA database across the
/// read-only/anonymous collection (REFACTOR-001-R0 S2/S3). It starts lazily so a
/// filtered run of anonymous tests without <c>GTAS_E2E_ISOLATED=1</c> never builds
/// the stack, and stale-manifest recovery runs once per fixture start instead of
/// once per test. Mutating tests never touch this fixture: they keep their own
/// per-test stack so every mutation lands on a fresh database.
/// </summary>
public sealed class SharedE2EAppFixture : IAsyncLifetime
{
    private readonly SemaphoreSlim _startGate = new(1, 1);
    private IsolatedE2EStack? _stack;
    private Exception? _startupFailure;

    public QaTestAccounts Accounts => _stack?.Fixture.Accounts
        ?? throw new InvalidOperationException(
            "The shared E2E app has not been started; call GetOrStartAsync first.");

    public string BackendBaseUrl => _stack?.BackendBaseUrl
        ?? throw new InvalidOperationException(
            "The shared E2E app has not been started; call GetOrStartAsync first.");

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Starts the shared stack on first use and returns its frontend base URL.
    /// Subsequent calls only run a fast health probe so a crashed app fails the
    /// remaining tests quickly with a clear message instead of per-test timeouts.
    /// </summary>
    public async Task<string> GetOrStartAsync(CancellationToken cancellationToken)
    {
        await _startGate.WaitAsync(cancellationToken);
        try
        {
            if (_startupFailure is not null)
            {
                throw new InvalidOperationException(
                    "The shared E2E stack already failed to start in this collection; failing fast.",
                    _startupFailure);
            }

            if (_stack is not null)
            {
                if (!await IsolatedE2EStack.IsBaseUrlReadyAsync(_stack.BaseUrl, cancellationToken))
                {
                    throw new InvalidOperationException(
                        $"Shared E2E frontend '{_stack.BaseUrl}' stopped responding mid-collection.");
                }

                return _stack.BaseUrl;
            }

            try
            {
                _stack = await IsolatedE2EStack.StartAsync(cancellationToken);
            }
            catch (Exception startException)
            {
                _startupFailure = startException;
                throw;
            }

            return _stack.BaseUrl;
        }
        finally
        {
            _startGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_stack is not null)
            {
                await _stack.DisposeAsync();
            }
        }
        finally
        {
            _startGate.Dispose();
        }
    }
}
