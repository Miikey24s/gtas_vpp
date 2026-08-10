using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using gtas_vpp_shared.Constants;
using System.Globalization;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Vòng recovery nhỏ, an toàn khi restart, cho kỳ đã vượt biên lúc ứng dụng offline.
/// Worker không sở hữu DbContext; mỗi tick lấy scope mới để request lỗi không làm
/// hỏng lần lặp tiếp theo.
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
                    await service.AdvanceDuePeriodsAsync(stoppingToken);
                    await service.TopUpOpenHorizonAsync(
                        CanonicalRbac.DefaultMemberCompanyCode.ToString(
                            CultureInfo.InvariantCulture),
                        cancellationToken: stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    // Sự cố database tạm thời không được dừng host; tick tiếp theo
                    // retry bằng scope sạch.
                    _logger.LogError(
                        exception,
                        "VPP period recovery tick failed; will retry on the next interval.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host tắt bình thường.
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
