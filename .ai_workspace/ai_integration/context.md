# AI Integration — Context (Phase 8)

> Tóm tắt cấu trúc project hiện tại liên quan đến AI Integration.
> Đọc thêm `.ai_workspace/refactor/context.md` để biết full context.

## 1. Solution Structure

```
gtas_vpp/gtas_vpp.slnx
├── code-be/gtas_vpp_be/              — Web API (.NET 10)
├── code-be/gtas_vpp_be.Service/      — Application/Service layer
├── code-be/gtas_vpp_be.Model/        — Domain entities
├── code-be/gtas_vpp_be.Migrations/   — EF Core migrations
├── code-be/gtas_vpp_be.Tests/        — xUnit tests
├── code-be/gtas_vpp_be.AI/           — [NEW] AI Infrastructure
├── code-fe/gtas_vpp_fe/              — Blazor Server FE
├── gtas_vpp/gtas_vpp_shared/         — Shared DTOs/Enums
├── gtas_vpp/MyAspire.AppHost/        — Aspire orchestration
└── gtas_vpp/MyAspire.ServiceDefaults/
```

## 2. Entities liên quan (cần embed)

### L04_VPP (Văn phòng phẩm)
```csharp
// code-be/gtas_vpp_be.Model/Library/L04_VPP.cs
[Table("L04_VPP")]
public class L04_VPP : BaseModel   // BaseModel có: Id, Description, CreateUserId/Date, UpdateUserId/Date, IsDeleted
{
    public string? VPPCode { get; set; }
    public string? VPPName { get; set; }
    public Guid UOMId { get; set; }
    public L02_ClassDetail UOM { get; set; }           // Unit of Measure
    public Guid VPPCategoryId { get; set; }
    public L03_VPPCategory? VPPCategory { get; set; }
    public virtual ICollection<L06_VPPSupplierMapping>? L06_VPPSupplierMappings { get; set; }
}
```

### L03_VPPCategory (Danh mục VPP)
```csharp
// code-be/gtas_vpp_be.Model/Library/L03_VPPCategory.cs
[Table("L03_VPPCategory")]
public class L03_VPPCategory : BaseModel
{
    public string? VPPCategoryCode { get; set; }
    public string? VPPCategoryName { get; set; }
    public virtual ICollection<L04_VPP>? VPPs { get; set; }
}
```

### L02_ClassDetail (Đơn vị tính — UOM)
```csharp
// code-be/gtas_vpp_be.Model/Library/L02_ClassDetail.cs
[Table("L02_ClassDetail")]
public class L02_ClassDetail : BaseModel
{
    public string? ClassDetailCode { get; set; }
    public string? ClassDetailName { get; set; }  // VD: "Cây", "Hộp", "Ram", "Cuộn"
    public Guid ClassId { get; set; }
    public L01_Class Class { get; set; }
    public virtual ICollection<L04_VPP>? VPPs_UOM { get; set; }
}
```

## 3. Embedding Text Format

Mỗi VPP item sẽ được embed với text format:
```
"{VPPName} - Loại: {VPPCategoryName} - Đơn vị: {UOMName} - Mô tả: {Description}"
```

Ví dụ:
```
"Bút bi Thiên Long TL-027 - Loại: Bút viết - Đơn vị: Cây - Mô tả: Bút bi cao cấp mực xanh"
"Giấy A4 Double A 80gsm - Loại: Giấy in - Đơn vị: Ram - Mô tả: Giấy in trắng chất lượng cao"
```

## 4. Key Infrastructure (đã có)

### DbContext — VPPContext
```csharp
// code-be/gtas_vpp_be.Service/Helpers/Context/VPPContext.cs
// Chứa: L04_VPPs, L03_VPPCategories, L02_ClassesDetail, etc.
// FK relationships configured in OnModelCreating
```

### DI Registration Pattern (Program.cs)
```csharp
// code-be/gtas_vpp_be/Program.cs
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();
builder.Services.AddScoped<IVPPRequestService, VPPRequestService>();
// ... pattern: Interface → Implementation, AddScoped
```

### MainLayout (FE)
```razor
@* code-fe/gtas_vpp_fe/gtas_vpp_fe/Components/Layout/MainLayout.razor *@
@attribute [Authorize]
@inherits LayoutComponentBase

<RadzenDialog />
<RadzenNotification />

<RadzenLayout Style="grid-template-columns: auto 1fr auto; ...">
    <LeftSidebar />
    <RadzenBody>
        @Body
    </RadzenBody>
</RadzenLayout>
@* → Thêm <AIChatBox /> ở đây (sau RadzenLayout, trước closing tag) *@
```

### FE API Call Pattern
```csharp
// code-fe/gtas_vpp_fe/gtas_vpp_fe/Services/APIServices.cs
// Pattern: HttpClient với JWT token trong Cookie
// BaseURL config trong appsettings.json
```

## 5. Connection Strings

```json
// code-be/gtas_vpp_be/appsettings.Development.json
{
  "ConnectionStrings": {
    "TestEnv": "Server=...;Database=...;..."
  }
}
```

## 6. Build & Test Commands

```bash
dotnet build gtas_vpp/gtas_vpp.slnx          # Build toàn bộ
dotnet test code-be/gtas_vpp_be.Tests/        # Run tests
dotnet ef migrations add X --project ...       # Add migration
```
