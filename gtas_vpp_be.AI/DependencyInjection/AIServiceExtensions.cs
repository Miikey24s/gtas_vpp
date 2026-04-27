using gtas_vpp_be.AI.Data;
using gtas_vpp_be.AI.Handlers;
using gtas_vpp_be.AI.Services;
using gtas_vpp_be.Service.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace gtas_vpp_be.AI.DependencyInjection;

public static class AIServiceExtensions
{
    public static IServiceCollection AddGtasAIServices(this IServiceCollection services, IConfiguration config)
    {
        var aiSettings = config.GetSection("AISettings");
        var ollamaUrl = NormalizeBaseUrl(aiSettings["OllamaBaseUrl"] ?? "http://localhost:11434");
        var chatModel = aiSettings["ChatModel"] ?? "gemma4:e4b";
        var embedModel = aiSettings["EmbeddingModel"] ?? "nomic-embed-text";
        var timeoutSeconds = int.TryParse(aiSettings["TimeoutSeconds"], out var timeout) && timeout > 0
            ? timeout
            : 60;

        services.AddDbContext<AIDbContext>((sp, o) =>
        {
            var constr = config.GetConnectionString("TestEnv");
            if (string.IsNullOrWhiteSpace(constr))
            {
                throw new InvalidOperationException("Connection string 'TestEnv' is not configured.");
            }

            o.UseSqlServer(constr, sql => sql.MigrationsAssembly(typeof(AIDbContext).Assembly.FullName));
        });

        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(ollamaUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        });

        services.AddSingleton<IChatClient>(sp =>
            new OllamaChatClient(new Uri(ollamaUrl), chatModel, sp.GetRequiredService<HttpClient>()));

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            new OllamaEmbeddingGenerator(new Uri(ollamaUrl), embedModel, sp.GetRequiredService<HttpClient>()));

        services.AddScoped<IVPPEmbeddingStore, SqlVPPEmbeddingStore>();
        services.AddScoped<IAIUseCaseHandler, VPPSuggestionHandler>();
        services.AddScoped<IAIOrchestrator, OllamaAIOrchestrator>();

        return services;
    }

    private static string NormalizeBaseUrl(string url)
    {
        return url.EndsWith('/') ? url : $"{url}/";
    }
}
