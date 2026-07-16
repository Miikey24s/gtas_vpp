using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using gtas_vpp_shared.Constants;
using System.Globalization;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Small, restart-safe recovery loop for periods whose boundary was crossed
/// while the application was offline.  It owns no DbContext; every tick gets
/// a fresh scope so a failed request cannot poison the next iteration.
/// </summary>
public sealed class VppPeriodRecoveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VppPeriodRecoveryWorker> _logger;
    private readonly TimeSpan _interval;

    public VppPeriodRecoveryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<VppPeriodRecoveryWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _interval = ResolveInterval(configuration);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider
                        .GetRequiredService<IVppPeriodService>();
                    await service.EnsureCurrentAsync(
                        CanonicalRbac.DefaultMemberCompanyCode.ToString(
                            CultureInfo.InvariantCulture),
                        stoppingToken);
                    await service.AdvanceDuePeriodsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    // A transient database outage must not terminate the host;
                    // the next tick retries with a clean scope.
                    _logger.LogError(
                        exception,
                        "VPP period recovery tick failed; will retry on the next interval.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
    }

    private static TimeSpan ResolveInterval(IConfiguration configuration)
    {
        var configuredSeconds = configuration.GetSection("VPP")
            .GetValue<int?>("PeriodRecoveryIntervalSeconds")
            ?? configuration.GetValue("VPPPeriodRecoveryIntervalSeconds", 300);
        var seconds = Math.Clamp(configuredSeconds, 5, 86_400);
        return TimeSpan.FromSeconds(seconds);
    }
}
