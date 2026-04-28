Bạn là AI Worker (Codex) trong hệ thống AI Integration cho dự án GTAS VPP.

# Bắt buộc đọc trước khi làm
- `.ai_workspace/ai_integration/architecture.md` — chuẩn AI integration
- `.ai_workspace/refactor/architecture.md` — chuẩn coding chung
- `.ai_workspace/ai_integration/context.md` — context project
- `.ai_workspace/ai_integration/tasks.json` — danh sách task (P8-A phải done)

# Nhiệm vụ: Thực thi Phase P8-B (Implementation — Tasks P8.5 → P8.8)

## P8.5 — AIDbContext + Migration

### Tạo `code-be/gtas_vpp_be.AI/Data/AIDbContext.cs`
```csharp
using gtas_vpp_be.Model.AI;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.AI.Data;

public class AIDbContext : DbContext
{
    public DbSet<AI_VPPEmbedding> VPPEmbeddings => Set<AI_VPPEmbedding>();

    public AIDbContext(DbContextOptions<AIDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AI_VPPEmbedding>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.CreatedDate).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.UpdatedDate).HasDefaultValueSql("GETDATE()");
            
            e.HasIndex(x => x.VPPId).IsUnique();
            
            e.HasOne(x => x.VPP)
             .WithMany()
             .HasForeignKey(x => x.VPPId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

### Migration
- Thêm AIDbContext registration tạm thời vào Program.cs (hoặc dùng design-time factory) để chạy migration
- `dotnet ef migrations add AddAIVPPEmbedding --context AIDbContext --project code-be/gtas_vpp_be.AI --startup-project code-be/gtas_vpp_be`
- Verify table `AI_VPPEmbedding` created

## P8.6 — SqlVPPEmbeddingStore + VectorMath

### Tạo `code-be/gtas_vpp_be.AI/Helpers/VectorMath.cs`
```csharp
namespace gtas_vpp_be.AI.Helpers;

public static class VectorMath
{
    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Vectors must have same length");
        
        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        
        var magnitude = MathF.Sqrt(magA) * MathF.Sqrt(magB);
        return magnitude == 0 ? 0 : dot / magnitude;
    }
    
    public static byte[] FloatArrayToBytes(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }
    
    public static float[] BytesToFloatArray(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
```

### Tạo `code-be/gtas_vpp_be.AI/Services/SqlVPPEmbeddingStore.cs`
Implement IVPPEmbeddingStore:
- Inject: `AIDbContext`, `IEmbeddingGenerator<string, Embedding<float>>`, `IGenericRepository<L04_VPP>`, `ILogger<SqlVPPEmbeddingStore>`
- **SearchSimilarAsync**: Load tất cả embeddings từ DB → convert byte[] → float[] → compute cosine similarity → order desc → take topK
- **UpsertEmbeddingAsync**: Check nếu VPPId đã có → update, chưa có → insert. Lưu vector as bytes.
- **RebuildAllAsync**: 
  1. Load tất cả L04_VPP (Include Category, UOM, filter !IsDeleted)
  2. Format embedding text: "{VPPName} - Loại: {CategoryName} - Đơn vị: {UOMName} - Mô tả: {Description}"
  3. Gọi IEmbeddingGenerator.GenerateAsync() cho mỗi item (hoặc batch nếu API hỗ trợ)
  4. Upsert tất cả vào DB
  5. Log: "Rebuilt {count} VPP embeddings in {elapsed}ms"
- **HasEmbeddingsAsync**: `return await _db.VPPEmbeddings.AnyAsync(ct);`

**LƯU Ý**: GenericRepository load L04_VPP KHÔNG include navigation properties. Cần inject VPPContext trực tiếp hoặc dùng AIDbContext kết hợp raw query. Xem cách phù hợp nhất với kiến trúc hiện tại.

## P8.7 — VPPSuggestionHandler + OllamaAIOrchestrator

### Tạo `code-be/gtas_vpp_be.AI/Handlers/VPPSuggestionHandler.cs`
Implement IAIUseCaseHandler:
- **UseCaseId**: `"vpp_suggestion"`
- **DisplayName**: `"Đề xuất Văn phòng phẩm"`
- **CanHandle**: Luôn return `true` (default/catch-all handler cho Phase 8)
- **HandleAsync**:
  1. Check `IVPPEmbeddingStore.HasEmbeddingsAsync()` — nếu chưa có embeddings → trả hướng dẫn rebuild
  2. Embed user query qua `IEmbeddingGenerator`
  3. Search similar VPPs (top 5) qua `IVPPEmbeddingStore`
  4. Build system prompt (tiếng Việt):
  ```
  Bạn là trợ lý AI chuyên về văn phòng phẩm (VPP) trong hệ thống GTAS VPP.
  Nhiệm vụ: Dựa vào danh sách VPP bên dưới, đề xuất sản phẩm phù hợp nhất cho yêu cầu người dùng.
  Trả lời bằng tiếng Việt, ngắn gọn, chuyên nghiệp.
  Nếu không tìm thấy VPP phù hợp, hãy nói rõ.
  
  Danh sách VPP liên quan:
  {retrieved_context}
  ```
  5. Call `IChatClient.GetResponseAsync()` with system + user messages
  6. Return `AIChatResponse.Success()` with parsed suggested items

### Tạo `code-be/gtas_vpp_be.AI/Services/OllamaAIOrchestrator.cs`
Implement IAIOrchestrator:
- Inject: `IEnumerable<IAIUseCaseHandler>`, `ILogger`, `HttpClient` (cho health check)
- **ChatAsync**:
  1. Health check: GET `{OllamaBaseUrl}/api/tags` — nếu fail → return Error("Dịch vụ AI tạm thời không khả dụng")
  2. Find handler: iterate handlers, first `CanHandle(request.Message)` → call `HandleAsync()`
  3. No handler found → return Error("Tôi không hiểu yêu cầu của bạn")
  4. Wrap trong try-catch → log exception → return Error
- **GetAvailableUseCases**: Map handlers → AIUseCaseInfo list

## P8.8 — AIController + DI + Config

### Tạo `code-be/gtas_vpp_be.AI/DependencyInjection/AIServiceExtensions.cs`
```csharp
public static class AIServiceExtensions
{
    public static IServiceCollection AddGtasAIServices(this IServiceCollection services, IConfiguration config)
    {
        var aiSettings = config.GetSection("AISettings");
        var ollamaUrl = aiSettings["OllamaBaseUrl"] ?? "http://localhost:11434";
        var chatModel = aiSettings["ChatModel"] ?? "gemma4:e4b";
        var embedModel = aiSettings["EmbeddingModel"] ?? "nomic-embed-text";
        
        // AIDbContext — dùng chung connection string
        services.AddDbContext<AIDbContext>((sp, o) =>
        {
            var constr = config.GetConnectionString("TestEnv");
            o.UseSqlServer(constr);
        });
        
        // Ollama clients (Singleton — reuse HTTP connections)
        services.AddSingleton<IChatClient>(sp =>
            new OllamaChatClient(new Uri(ollamaUrl), chatModel));
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            new OllamaEmbeddingGenerator(new Uri(ollamaUrl), embedModel));
        
        // AI Services (Scoped)
        services.AddScoped<IVPPEmbeddingStore, SqlVPPEmbeddingStore>();
        services.AddScoped<IAIUseCaseHandler, VPPSuggestionHandler>();
        services.AddScoped<IAIOrchestrator, OllamaAIOrchestrator>();
        
        return services;
    }
}
```
LƯU Ý: API của M.E.AI Ollama có thể khác — check actual constructor signatures. Trên dùng pseudo-code.

### Tạo `code-be/gtas_vpp_be/Controllers/AIController.cs`
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIOrchestrator _orchestrator;
    private readonly IVPPEmbeddingStore _embeddingStore;
    
    [HttpPost("chat")]
    public async Task<IActionResult> ChatAsync([FromBody] AIChatRequestDTO request, CancellationToken ct)
    // Map DTO → internal request → call orchestrator → map response → return
    
    [HttpPost("rebuild-embeddings")]
    public async Task<IActionResult> RebuildEmbeddingsAsync(CancellationToken ct)
    // Call _embeddingStore.RebuildAllAsync() → return OK
    
    [HttpGet("use-cases")]
    public IActionResult GetUseCases()
    // Call _orchestrator.GetAvailableUseCases()
    
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> HealthCheck(CancellationToken ct)
    // GET Ollama /api/tags → return status
}
```

### Modify `code-be/gtas_vpp_be/Program.cs`
Thêm dòng sau (trước `builder.Services.AddControllersWithViews()`):
```csharp
builder.Services.AddGtasAIServices(Configuration);
```

### Modify `code-be/gtas_vpp_be/appsettings.json`
Thêm section:
```json
"AISettings": {
  "OllamaBaseUrl": "http://localhost:11434",
  "ChatModel": "gemma4:e4b",
  "EmbeddingModel": "nomic-embed-text",
  "MaxTokens": 2048,
  "Temperature": 0.3,
  "TimeoutSeconds": 60
}
```

### Modify `code-be/gtas_vpp_be/appsettings.Development.json`
Thêm:
```json
"AISettings": {
  "OllamaBaseUrl": "http://192.168.x.x:11434"
}
```
(Dùng IP LAN thực tế của PC)

# Sau khi hoàn thành
1. `dotnet build gtas_vpp/gtas_vpp.slnx` — 0 errors
2. Swagger shows /api/ai/* endpoints
3. Cập nhật tasks.json: P8.5 → P8.8 status → done
4. Report vào `.ai_workspace/ai_integration/reports/P8B_implementation.md`

# Quy tắc
- Error handling: KHÔNG throw exception cho AI failures → graceful degradation
- Tất cả async methods phải accept CancellationToken
- Log structured format: `_logger.LogInformation("AI {Action} completed in {ElapsedMs}ms", ...)`
- Nếu M.E.AI API khác với pseudo-code → check NuGet API docs, adjust accordingly
