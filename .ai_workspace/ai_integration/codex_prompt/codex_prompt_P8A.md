Bạn là AI Worker (Codex) trong hệ thống AI Integration cho dự án GTAS VPP.

# Bắt buộc đọc trước khi làm
- `.ai_workspace/ai_integration/architecture.md` — chuẩn AI integration
- `.ai_workspace/refactor/architecture.md` — chuẩn coding chung
- `.ai_workspace/ai_integration/context.md` — context project
- `.ai_workspace/ai_integration/tasks.json` — danh sách task

# Nhiệm vụ: Thực thi Phase P8-A (Foundation — Tasks P8.1 → P8.4)

Thực hiện lần lượt 4 tasks. Sau mỗi task, verify `dotnet build gtas_vpp/gtas_vpp.slnx`. Nếu fail → fix trước khi tiếp.

## P8.1 — Tạo project gtas_vpp_be.AI

1. Tạo thư mục `code-be/gtas_vpp_be.AI/`
2. Tạo file `gtas_vpp_be.AI.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Configurations>Debug;Release;Debug-BE;Debug-FE</Configurations>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.AI" Version="*" />
    <PackageReference Include="Microsoft.Extensions.AI.Ollama" Version="*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.5" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\gtas_vpp_be.Service\gtas_vpp_be.Service.csproj" />
    <ProjectReference Include="..\gtas_vpp_be.Model\gtas_vpp_be.Model.csproj" />
  </ItemGroup>
</Project>
```
Lưu ý: Dùng `Version="*"` cho M.E.AI packages nếu chưa biết version chính xác. Build sẽ resolve.

3. Thêm vào `gtas_vpp/gtas_vpp.slnx`:
```xml
<Project Path="../code-be/gtas_vpp_be.AI/gtas_vpp_be.AI.csproj">
  <BuildType Solution="BE|*" Project="Debug-BE" />
  <BuildType Solution="FE|*" Project="Debug-FE" />
  <Build Solution="FE|*" Project="false" />
</Project>
```

4. Thêm vào `code-be/gtas_vpp_be/gtas_vpp_be.csproj`:
```xml
<ProjectReference Include="..\gtas_vpp_be.AI\gtas_vpp_be.AI.csproj" />
```

## P8.2 — Entity AI_VPPEmbedding

Tạo `code-be/gtas_vpp_be.Model/AI/AI_VPPEmbedding.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using gtas_vpp_be.Model.Library;

namespace gtas_vpp_be.Model.AI;

[Table("AI_VPPEmbedding")]
public class AI_VPPEmbedding
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    
    public Guid VPPId { get; set; }
    
    [ForeignKey(nameof(VPPId))]
    public L04_VPP VPP { get; set; } = null!;
    
    [Required]
    public string EmbeddingText { get; set; } = string.Empty;
    
    [Required]
    public byte[] EmbeddingVector { get; set; } = [];
    
    [Required]
    [MaxLength(100)]
    public string ModelName { get; set; } = string.Empty;
    
    public int VectorDimension { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
```

## P8.3 — Interfaces + Internal DTOs

Tạo các file trong `code-be/gtas_vpp_be.Service/AI/`:

### AI/IAIOrchestrator.cs
```csharp
namespace gtas_vpp_be.Service.AI;

public interface IAIOrchestrator
{
    Task<AIChatResponse> ChatAsync(AIChatRequest request, CancellationToken ct = default);
    IReadOnlyList<AIUseCaseInfo> GetAvailableUseCases();
}
```

### AI/IAIUseCaseHandler.cs
```csharp
namespace gtas_vpp_be.Service.AI;

public interface IAIUseCaseHandler
{
    string UseCaseId { get; }
    string DisplayName { get; }
    bool CanHandle(string userMessage);
    Task<AIChatResponse> HandleAsync(AIChatRequest request, CancellationToken ct = default);
}
```

### AI/IVPPEmbeddingStore.cs
```csharp
namespace gtas_vpp_be.Service.AI;

public interface IVPPEmbeddingStore
{
    Task<List<VPPEmbeddingSearchResult>> SearchSimilarAsync(float[] queryVector, int topK, CancellationToken ct = default);
    Task UpsertEmbeddingAsync(Guid vppId, string text, float[] vector, string modelName, CancellationToken ct = default);
    Task RebuildAllAsync(CancellationToken ct = default);
    Task<bool> HasEmbeddingsAsync(CancellationToken ct = default);
}
```

### AI/DTOs/AIChatRequest.cs
```csharp
namespace gtas_vpp_be.Service.AI;

public record AIChatMessage(string Role, string Content);

public class AIChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<AIChatMessage>? ConversationHistory { get; set; }
    public string? UseCaseHint { get; set; }
}
```

### AI/DTOs/AIChatResponse.cs
```csharp
namespace gtas_vpp_be.Service.AI;

public class AISuggestedItem
{
    public Guid VPPId { get; set; }
    public string VPPCode { get; set; } = string.Empty;
    public string VPPName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UOMName { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}

public class AIChatResponse
{
    public string Message { get; set; } = string.Empty;
    public List<AISuggestedItem>? SuggestedItems { get; set; }
    public string UseCaseId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public static AIChatResponse Error(string message) =>
        new() { IsSuccess = false, ErrorMessage = message, Message = message };

    public static AIChatResponse Success(string message, string useCaseId, List<AISuggestedItem>? items = null) =>
        new() { IsSuccess = true, Message = message, UseCaseId = useCaseId, SuggestedItems = items };
}
```

### AI/DTOs/AIUseCaseInfo.cs
```csharp
namespace gtas_vpp_be.Service.AI;

public record AIUseCaseInfo(string UseCaseId, string DisplayName, string Description);
```

### AI/DTOs/VPPEmbeddingSearchResult.cs
```csharp
namespace gtas_vpp_be.Service.AI;

public class VPPEmbeddingSearchResult
{
    public Guid VPPId { get; set; }
    public string EmbeddingText { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}
```

## P8.4 — Shared DTOs

Tạo các file trong `gtas_vpp/gtas_vpp_shared/DTOs/AI/`:

### AIChatRequestDTO.cs
```csharp
namespace gtas_vpp_shared.DTOs.AI;

public class AIChatRequestDTO
{
    public string Message { get; set; } = string.Empty;
    public List<AIChatMessageDTO>? History { get; set; }
}
```

### AIChatMessageDTO.cs
```csharp
namespace gtas_vpp_shared.DTOs.AI;

public class AIChatMessageDTO
{
    public string Role { get; set; } = string.Empty;   // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### AIChatResponseDTO.cs
```csharp
namespace gtas_vpp_shared.DTOs.AI;

public class AIChatResponseDTO
{
    public string Message { get; set; } = string.Empty;
    public List<AISuggestedItemDTO>? SuggestedItems { get; set; }
    public string UseCaseId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### AISuggestedItemDTO.cs
```csharp
namespace gtas_vpp_shared.DTOs.AI;

public class AISuggestedItemDTO
{
    public Guid VPPId { get; set; }
    public string VPPCode { get; set; } = string.Empty;
    public string VPPName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UOMName { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}
```

# Sau khi hoàn thành TẤT CẢ tasks P8.1 → P8.4
1. Chạy `dotnet build gtas_vpp/gtas_vpp.slnx` — verify 0 errors
2. Cập nhật `.ai_workspace/ai_integration/tasks.json`: đổi status → `"done"`, thêm `"completedAt"` + `"notes"`
3. Ghi tóm tắt vào `.ai_workspace/ai_integration/reports/P8A_foundation.md`

# Quy tắc
- Tuân thủ naming convention trong architecture.md
- KHÔNG viết logic AI, chỉ tạo skeleton (interfaces, entities, DTOs)
- Nullable enable, implicit usings enable
- Nếu M.E.AI package version không resolve → thử `dotnet add package Microsoft.Extensions.AI` để lấy version mới nhất, rồi cập nhật csproj
