# GTAS VPP — AI Integration Architecture

> File này là chuẩn bắt buộc cho mọi Worker khi implement AI features. Đọc KỸ trước khi code.
> Kế thừa toàn bộ chuẩn từ `../refactor/architecture.md`.

## Quyết định đã chốt

| Item | Quyết định |
|------|-----------|
| Framework AI | `Microsoft.Extensions.AI` (M.E.AI) — KHÔNG dùng Semantic Kernel |
| LLM Provider | Ollama (chạy trên PC qua LAN, KHÔNG localhost) |
| Chat Model | `gemma4:e4b` (4B effective, Q4 ~5-6GB VRAM) |
| Embedding Model | `nomic-embed-text` (~550MB VRAM) |
| Embedding Storage | **SQL Server** (persist, bền vững) |
| Frontend | Blazor chatbox (floating, trong MainLayout) |
| Scope | Extensible — Phase 1: đề xuất VPP, sau mở rộng |

## Network Topology

```
┌─────────────────────┐         LAN          ┌─────────────────────┐
│     LAPTOP          │ ◄──────────────────► │       PC            │
│  .NET 10 BE + FE    │   HTTP :11434        │  Ollama Server      │
│  SQL Server         │                      │  RTX 2070S 8GB      │
│  Port :5xxx         │                      │  gemma4:e4b         │
└─────────────────────┘                      │  nomic-embed-text   │
                                             └─────────────────────┘
```

> [!IMPORTANT]
> Ollama chạy trên máy PC (có GPU), còn project chạy trên laptop.
> Config `OllamaBaseUrl` phải dùng **IP LAN** của PC (VD: `http://192.168.1.100:11434`).
> Trên PC cần chạy: `OLLAMA_HOST=0.0.0.0 ollama serve` để cho phép kết nối từ LAN.

## Clean Architecture — AI Layer

```
Presentation (AIController, AIChatBox.razor)
    ↓ depends on
Application (IAIOrchestrator, IAIUseCaseHandler, IVPPEmbeddingStore, DTOs)
    ↓ defined in gtas_vpp_be.Service/AI/
Domain (AI_VPPEmbedding entity)
    ↓ defined in gtas_vpp_be.Model/AI/
Infrastructure (OllamaAIOrchestrator, SqlVPPEmbeddingStore, AIDbContext)
    ↓ defined in gtas_vpp_be.AI/ [NEW PROJECT]
```

## Project References (bổ sung)

```
gtas_vpp_be.AI  → gtas_vpp_be.Service (for interfaces)
                → gtas_vpp_be.Model   (for entities)
gtas_vpp_be     → gtas_vpp_be.AI      (for DI registration)
gtas_vpp_fe     → gtas_vpp_shared     (ONLY — không thay đổi)
```

## DI Lifecycle Rules

| Service | Lifetime | Lý do |
|---------|----------|-------|
| `IChatClient` | Singleton | Reuse HTTP connection to Ollama |
| `IEmbeddingGenerator` | Singleton | Reuse HTTP connection to Ollama |
| `IAIOrchestrator` | Scoped | Per-request, inject UoW/DbContext |
| `IVPPEmbeddingStore` | Scoped | Dùng AIDbContext (scoped) |
| `IAIUseCaseHandler` | Scoped | Per-request, user context |

## Extensible Architecture — Strategy Pattern

```csharp
// Thêm use case mới = 3 bước:
// 1. Tạo class implement IAIUseCaseHandler
// 2. Register vào DI (AddScoped)
// 3. Orchestrator tự detect qua IEnumerable<IAIUseCaseHandler>
```

### Planned Use Cases

| Phase | UseCaseId | Handler | Status |
|-------|-----------|---------|--------|
| 8 | `vpp_suggestion` | `VPPSuggestionHandler` | 🔜 |
| 9 | `auto_order` | `AutoOrderHandler` | ⏳ |
| 10 | `consumption_analysis` | `ConsumptionAnalysisHandler` | ⏳ |

## Embedding Persistence

- Table: `AI_VPPEmbedding` trong SQL Server
- Vector format: `float[]` → `byte[]` qua `Buffer.BlockCopy` → `VARBINARY(MAX)`
- Search: Load tất cả vectors in-memory → cosine similarity (dataset < 1000 OK)
- Invalidation: Gọi rebuild khi VPP catalog CRUD thay đổi

## Naming Convention (bổ sung)

| Loại | Pattern | Ví dụ |
|------|---------|-------|
| AI Interface | `IAI{Feature}` | `IAIOrchestrator` |
| AI Handler | `{Feature}Handler` | `VPPSuggestionHandler` |
| AI Entity | `AI_{Entity}` | `AI_VPPEmbedding` |
| AI DTO | `AIChat{Action}DTO` | `AIChatRequestDTO` |
| AI Controller | `AIController` | `AIController` |

## Error Handling (AI-specific)

- Ollama unavailable → trả `AIChatResponse.IsSuccess = false` + message "Dịch vụ AI tạm thời không khả dụng"
- Embedding generation fail → log error + trả fallback response
- Model timeout (>30s) → cancel token + trả timeout message
- **KHÔNG throw exception** cho AI failures — graceful degradation

## Config Structure

```json
{
  "AISettings": {
    "OllamaBaseUrl": "http://192.168.x.x:11434",
    "ChatModel": "gemma4:e4b",
    "EmbeddingModel": "nomic-embed-text",
    "MaxTokens": 2048,
    "Temperature": 0.3,
    "TimeoutSeconds": 60,
    "MaxConversationHistory": 10
  }
}
```

## Quy tắc AI Service

1. AI service KHÔNG trực tiếp access VPPContext → dùng IGenericRepository hoặc AIDbContext riêng
2. Response phải có fallback khi Ollama unavailable
3. Embedding cache invalidate khi VPP catalog thay đổi (CRUD event)
4. System prompt phải hỗ trợ tiếng Việt
5. Tất cả AI calls phải có CancellationToken
6. Log structured: `_logger.LogInformation("AI {UseCaseId} processed in {ElapsedMs}ms", ...)`
