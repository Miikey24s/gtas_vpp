using Aspire.Hosting;
using Aspire.Hosting.Testing;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_test_support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace gtas_vpp_fe.UITests.Core;

/// <summary>
/// One disposable LocalDB + Aspire (backend + frontend) stack. This is the exact
/// startup chain the old per-test <c>TestBase.StartIsolatedApplicationAsync</c> ran:
/// stale-manifest recovery, harness-owned LocalDB instance, Aspire app host, backend
/// database-identity handshake, and frontend readiness. Ports stay dynamic (Aspire
/// assigns them) and only loopback URLs are accepted.
/// </summary>
internal sealed class IsolatedE2EStack : IAsyncDisposable
{
    // Same total readiness budget as the previous 24 x 2s polls, with a finer step so
    // an already-ready endpoint is confirmed in ~500ms instead of up to 2s.
    private const int ReadinessPollAttempts = 96;
    private static readonly TimeSpan ReadinessPollStep = TimeSpan.FromMilliseconds(500);

    private IsolatedE2EStack(
        LocalDbQaFixture fixture,
        DistributedApplication app,
        string baseUrl)
    {
        Fixture = fixture;
        App = app;
        BaseUrl = baseUrl;
    }

    public LocalDbQaFixture Fixture { get; }

    public DistributedApplication App { get; }

    public string BaseUrl { get; }

    public static async Task<IsolatedE2EStack> StartAsync(CancellationToken cancellationToken)
    {
        await LocalDbQaFixture.RecoverStaleAsync(cancellationToken: cancellationToken);
        var fixture = await LocalDbQaFixture.CreateAsync(cancellationToken: cancellationToken);
        DistributedApplication? app = null;
        try
        {
            var args = new[]
            {
                $"--Parameters:test-database-connection-string={fixture.ConnectionString}",
                $"--Parameters:jwt-key={fixture.Secrets.JwtKey}",
                $"--Parameters:qa-fixture-run-id={fixture.Options.RunId}"
            };
            var appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.MyAspire_AppHost>(args, cancellationToken);
            appHost.Services.AddLogging(logging =>
            {
                // The Windows EventLog provider can require administrator rights.
                logging.ClearProviders();
                logging.AddConsole();
            });

            app = await appHost.BuildAsync(cancellationToken);
            await app.StartAsync(cancellationToken);

            using var backendClient = app.CreateHttpClient("backend");
            QaUiSafetyContract.EnsureLoopbackUrl(
                backendClient.BaseAddress
                ?? throw new InvalidOperationException("Aspire backend endpoint has no address."),
                "Aspire backend endpoint");
            await WaitForConfirmedIdentityAsync(backendClient, fixture, cancellationToken);

            using var frontendClient = app.CreateHttpClient("frontend");
            var frontendBaseUrl = NormalizeBaseUrl(frontendClient.BaseAddress?.ToString())
                ?? throw new InvalidOperationException("Aspire frontend endpoint has no address.");
            QaUiSafetyContract.EnsureLoopbackUrl(new Uri(frontendBaseUrl), "Aspire frontend endpoint");
            await WaitForBaseUrlReadyAsync(frontendBaseUrl, cancellationToken);
            return new IsolatedE2EStack(fixture, app, frontendBaseUrl);
        }
        catch
        {
            try
            {
                if (app is not null)
                {
                    await app.DisposeAsync();
                }
            }
            finally
            {
                await fixture.DisposeAsync();
            }

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await App.DisposeAsync();
        }
        finally
        {
            await Fixture.DisposeAsync();
        }
    }

    internal static string? NormalizeBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
    }

    internal static async Task<bool> IsBaseUrlReadyAsync(
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

    private static async Task WaitForConfirmedIdentityAsync(
        HttpClient backendClient,
        LocalDbQaFixture fixture,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < ReadinessPollAttempts; attempt++)
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
            catch (HttpRequestException) when (attempt < ReadinessPollAttempts - 1)
            {
                // The backend can still be starting its reference-data check.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < ReadinessPollAttempts - 1)
            {
                // Backend nhận kết nối nhưng chưa trả lời kịp (migrate/seed dưới tải)
                // — HttpClient tự hủy request; thử lại thay vì ném ra khỏi InitializeAsync.
            }

            await Task.Delay(ReadinessPollStep, cancellationToken);
        }

        throw new InvalidOperationException(
            "Backend did not confirm the harness-owned TEST database identity; browser launch is blocked.");
    }

    private static async Task WaitForBaseUrlReadyAsync(
        string baseUrl,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < ReadinessPollAttempts; attempt++)
        {
            if (await IsBaseUrlReadyAsync(baseUrl, cancellationToken))
            {
                return;
            }

            await Task.Delay(ReadinessPollStep, cancellationToken);
        }

        throw new InvalidOperationException($"Frontend host '{baseUrl}' did not become ready in time.");
    }
}
