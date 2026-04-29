using gtas_vpp_be.AI.Data;
using gtas_vpp_be.AI.Handlers;
using gtas_vpp_be.AI.Services;
using gtas_vpp_be.Service.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using System.ClientModel;

namespace gtas_vpp_be.AI.DependencyInjection;

public static class AIServiceExtensions
{
    public static IServiceCollection AddGtasAIServices(this IServiceCollection services, IConfiguration config)
    {
        var aiSettings = config.GetSection("AISettings");
        var provider = aiSettings["Provider"] ?? "Ollama";
        var chatModel = aiSettings["ChatModel"] ?? "gemma4:e4b";
        var embedModel = aiSettings["EmbeddingModel"] ?? "nomic-embed-text";
        var timeoutSeconds = int.TryParse(aiSettings["TimeoutSeconds"], out var timeout) && timeout > 0
            ? timeout
            : 60;

        // ─── Database ────────────────────────────────────────────────
        services.AddDbContext<AIDbContext>((sp, o) =>
        {
            var constr = config.GetConnectionString("TestEnv");
            if (string.IsNullOrWhiteSpace(constr))
            {
                throw new InvalidOperationException("Connection string 'TestEnv' is not configured.");
            }

            o.UseSqlServer(constr, sql => sql.MigrationsAssembly(typeof(AIDbContext).Assembly.FullName));
        });

        // ─── AI Provider Registration ────────────────────────────────
        switch (provider.ToLowerInvariant())
        {
            case "google":
                RegisterGoogleProvider(services, aiSettings, chatModel, embedModel);
                break;

            case "ollama":
            default:
                RegisterOllamaProvider(services, aiSettings, chatModel, embedModel, timeoutSeconds);
                break;
        }

        // ─── Application Services ────────────────────────────────────
        services.AddScoped<IVPPEmbeddingStore, SqlVPPEmbeddingStore>();
        services.AddScoped<IAIUseCaseHandler, VPPSuggestionHandler>();
        services.AddScoped<IAIOrchestrator, DefaultAIOrchestrator>();

        return services;
    }

    // ─── Ollama Provider ─────────────────────────────────────────────
    private static void RegisterOllamaProvider(
        IServiceCollection services,
        IConfigurationSection aiSettings,
        string chatModel,
        string embedModel,
        int timeoutSeconds)
    {
        var ollamaUrl = NormalizeBaseUrl(aiSettings["OllamaBaseUrl"] ?? "http://localhost:11434");

        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(ollamaUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        });

        services.AddSingleton<IChatClient>(sp =>
            new OllamaChatClient(new Uri(ollamaUrl), chatModel, sp.GetRequiredService<HttpClient>()));

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            new OllamaEmbeddingGenerator(new Uri(ollamaUrl), embedModel, sp.GetRequiredService<HttpClient>()));
    }

    // ─── Google Gemini Provider (OpenAI-compatible endpoint) ─────────
    private static void RegisterGoogleProvider(
        IServiceCollection services,
        IConfigurationSection aiSettings,
        string chatModel,
        string embedModel)
    {
        // Gemini exposes an OpenAI-compatible endpoint
        var geminiEndpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/");

        // Use a dummy key because the actual API Key will be injected via interceptor
        var credential = new ApiKeyCredential("DUMMY_KEY");

        services.AddSingleton(sp => 
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("GeminiClient");
            
            var clientOptions = new OpenAIClientOptions 
            { 
                Endpoint = geminiEndpoint,
                Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(httpClient)
            };
            return new OpenAIClient(credential, clientOptions);
        });

        // Register a no-op HttpClient so DefaultAIOrchestrator health check works gracefully
        services.AddSingleton(sp => 
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("GeminiClient");
            httpClient.BaseAddress = geminiEndpoint;
            return httpClient;
        });

        services.AddSingleton<IChatClient>(sp => 
            sp.GetRequiredService<OpenAIClient>().GetChatClient(chatModel).AsIChatClient());

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var rawKeys = config["AISettings:GeminiApiKeys"] ?? string.Empty;
            var firstKey = rawKeys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                               .FirstOrDefault() ?? string.Empty;
            
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("GeminiClient");
            httpClient.Timeout = TimeSpan.FromSeconds(60);

            return new gtas_vpp_be.AI.Services.GeminiEmbeddingGenerator(httpClient, embedModel, firstKey);
        });
    }

    private static string NormalizeBaseUrl(string url)
    {
        return url.EndsWith('/') ? url : $"{url}/";
    }
}
