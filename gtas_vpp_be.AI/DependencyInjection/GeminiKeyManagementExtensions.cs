using gtas_vpp_be.AI.KeyManagement.DelegatingHandlers;
using gtas_vpp_be.AI.KeyManagement.Interfaces;
using gtas_vpp_be.AI.KeyManagement.Services;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace gtas_vpp_be.AI.DependencyInjection;

public static class GeminiKeyManagementExtensions
{
    public static IServiceCollection AddGeminiKeyManagement(this IServiceCollection services)
    {
        // 1. Đăng ký Singleton cho Load Balancer (chỉ dùng chung 1 hàng đợi cho toàn ứng dụng)
        services.AddSingleton<IApiKeyRotationService, ApiKeyRotationService>();
        
        // 2. Đăng ký Key Manager
        services.AddSingleton<IGeminiKeyManager, GeminiKeyManager>();
        
        // 3. Đăng ký Handler (Transient vì mỗi HTTP Client instance có pipeline riêng)
        services.AddTransient<GeminiKeyRotationHandler>();

        // 4. Cấu hình HttpClient kèm Microsoft.Extensions.Http.Resilience
        var builder = services.AddHttpClient("GeminiClient");
        
        builder.AddStandardResilienceHandler(options => 
            {
                // Tùy chỉnh policy: Chỉ retry nếu gặp lỗi 429 Too Many Requests
                options.Retry.ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Result?.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
                
                // Số lần thử lại tối đa (nếu bị 429 liên tục)
                options.Retry.MaxRetryAttempts = 3;
            });

        // LƯU Ý QUAN TRỌNG: 
        // DelegatingHandler này phải ĐỨNG SAU ResilienceHandler.
        // Để mỗi lần Resilience bóp cò Retry, nó sẽ gọi lại SendAsync() của Handler này -> rút Key mới!
        builder.AddHttpMessageHandler<GeminiKeyRotationHandler>();

        return services;
    }
}
