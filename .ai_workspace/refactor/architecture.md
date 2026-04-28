# GTAS VPP â€” Architecture Standards

> File nĂ y lĂ  chuáº©n báº¯t buá»™c cho má»i Worker khi refactor. Äá»c Ká»¸ trÆ°á»›c khi sá»­a code.

## Tech Stack

| Layer | Tech |
|-------|------|
| Backend API | .NET 10 Web API |
| Frontend | Blazor Server + Radzen Blazor 10.2 |
| ORM | EF Core 10 (Code-First) |
| Mapping | Mapster 10.0.7 |
| Auth BE | JWT Bearer |
| Auth FE | Cookie + Claims-based policies |
| DB | SQL Server (v_Users = SQL View) |
| Test | xUnit |
| Aspire | MyAspire.AppHost (orchestration) |

## Clean Architecture â€” Ăp dá»¥ng cho Refactor

```
Presentation (Controllers, Blazor Pages)
    â†“ depends on
Application (Service interfaces, DTOs, Business logic)
    â†“ depends on
Domain (Entities, Enums, Value Objects)
    â†“ depends on
Infrastructure (EF Core DbContext, SP execution, External APIs)
```

**Dependency rule**: Lá»›p trong KHĂ”NG BAO GIá»œ reference lá»›p ngoĂ i.

## Project References (giá»¯ nguyĂªn)

```
gtas_vpp_be â†’ gtas_vpp_be.Service â†’ gtas_vpp_be.Model
gtas_vpp_be â†’ gtas_vpp_shared
gtas_vpp_fe â†’ gtas_vpp_shared (ONLY â€” khĂ´ng reference BE)
```

## Quy táº¯c SOLID

### SRP â€” Single Responsibility
- 1 class = 1 nhiá»‡m vá»¥. Max ~200 LOC
- **BaseServices.cs (473 LOC)** pháº£i tĂ¡ch thĂ nh: Repository, SP Executor, UserName Resolver

### OCP â€” Open/Closed
- Má»Ÿ rá»™ng behavior qua interface/abstract, khĂ´ng sá»­a class gá»‘c

### LSP â€” Liskov Substitution
- Subclass pháº£i thay tháº¿ Ä‘Æ°á»£c parent mĂ  khĂ´ng phĂ¡ logic

### ISP â€” Interface Segregation
- Interface nhá», chuyĂªn biá»‡t. KhĂ´ng 1 interface Ă´m háº¿t CRUD + SP + Query

### DIP â€” Dependency Inversion
- Inject interface, khĂ´ng new trá»±c tiáº¿p
- **Cáº¥m** dĂ¹ng `[Inject]` attribute cá»§a Blazor trong Service layer

## Naming Convention

| Loáº¡i | Pattern | VĂ­ dá»¥ |
|------|---------|-------|
| Service | `I{Feature}Service` / `{Feature}Service` | `IVPPRequestService` |
| Repository | `I{Entity}Repository` | `IGenericRepository<T>` |
| DTO Request | `{Entity}_{Action}ReqDTO` | `VPP01_CreateReqDTO` |
| DTO Response | `{Entity}ResDTO` | `VPP01_RequestHeaderResDTO` |
| Controller | `{Feature}Controller` | `VPPRequestController` |
| **Cáº¥m typo** | `Business` âœ… | `Bussiness` âŒ |

## Error Handling

- Service layer: throw typed exceptions (`KeyNotFoundException`, `UnauthorizedAccessException`, `InvalidOperationException`)
- Controller: **KHĂ”NG try-catch** â€” delegate cho ExceptionHandlingMiddleware
- Middleware tráº£ `ProblemDetails` (RFC 7807)

## Transaction

- DĂ¹ng `DbContext.Database.BeginTransactionAsync()` 
- **KHĂ”NG dĂ¹ng** `TransactionScope` (quĂ¡ phá»©c táº¡p, dá»… leak)
- Transaction chá»‰ má»Ÿ á»Ÿ Service layer

## Logging

- Inject `ILogger<T>`, KHĂ”NG dĂ¹ng static Serilog
- Format: `_logger.LogError(ex, "Failed to {Action} {Entity} {Id}", action, entity, id)`

## Multi-Environment (giá»¯ nguyĂªn logic)

- Claims `"Server"` â†’ resolve `"TestEnv"` / `"LiveEnv"`
- TĂ¡ch thĂ nh `IEnvironmentResolver` â€” 1 nÆ¡i duy nháº¥t

## Stored Procedures

- Stored procedure scripts stay under `gtas_vpp_be.Service/Helpers/SQL/` and `gtas_vpp_shared/SQL/`.
- Keep the current `FromSqlRaw("exec {0} @SpType={1}, @Param={2}")` pattern behind `IStoredProcedureExecutor`.

## AI Integration (Phase 8)

### Framework
- `Microsoft.Extensions.AI` (M.E.AI) - KHONG Semantic Kernel
- Abstractions: `IChatClient`, `IEmbeddingGenerator<string, Embedding<float>>`
- Provider: Ollama chay tren PC qua LAN

### Architecture
- Interface: `gtas_vpp_be.Service/AI/`
- Implementation: `gtas_vpp_be.AI/`
- DI: `AddGtasAIServices()` extension method
- Frontend: floating `AIChatBox` trong `MainLayout`

### Models (Ollama)
| Purpose | Model | VRAM |
|---------|-------|------|
| Text generation | `gemma4:e4b` | ~5-6GB |
| Embedding | `nomic-embed-text` | ~550MB |

### Quy tac
- AI service KHONG access `VPPContext` truc tiep; dung `AIDbContext` rieng va service/repository contract.
- Response co fallback khi Ollama unavailable.
- Embedding cache invalidate khi VPP catalog thay doi.
- FE chi reference `gtas_vpp_shared`, khong reference BE project.

