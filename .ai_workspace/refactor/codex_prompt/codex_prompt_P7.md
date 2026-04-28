Bạn là AI Worker (Codex) trong hệ thống refactor dự án GTAS VPP.

# Bắt buộc đọc trước
- `.ai_workspace/reports/final_review_v3.md` — review lần 3

# Nhiệm vụ: Phase 7 — Final Polish (2 tasks)

---

## P7.1 — Fix Blazor Memory Leak

**Vấn đề**: `Component_Library.razor.cs` subscribe `NavigationManager.LocationChanged += OnLocationChanged` nhưng KHÔNG implement `IDisposable` → memory leak trên Blazor Server.

**Thực hiện**:
1. Mở `code-fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Lib/Component_Library.razor.cs`
2. Implement `IDisposable`:
```csharp
public partial class Component_Library : ComponentBase, IDisposable
{
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
```
3. Quét TẤT CẢ file .razor.cs khác trong `Components/` — tìm bất kỳ chỗ nào subscribe event (`+=`) mà thiếu `IDisposable` + unsubscribe (`-=`). Fix luôn nếu có.

---

## P7.2 — Remove Hardcoded JWT Key

**Vấn đề**: `Config.cs` có fallback hardcoded JWT key `GTAS_VPP_BE_DEV_ONLY_KEY_CHANGE_IN_PRODUCTION_2026`. Rủi ro nếu dev quên đổi.

**Thực hiện**:
1. Mở `code-be/gtas_vpp_be.Service/Helpers/Config.cs`
2. Xóa giá trị default/fallback cho JWT key
3. Thay bằng kiểm tra startup: nếu key null hoặc rỗng → throw `InvalidOperationException("JWT Key must be configured via environment variable or user secrets")`
4. Trong `appsettings.Development.json`: giữ key dev (cho local dev)
5. Trong `appsettings.json` (production): đặt giá trị rỗng hoặc placeholder để ép cấu hình qua env var

---

# Sau khi hoàn thành
1. `dotnet build gtas_vpp/gtas_vpp.slnx` — PASS, 0 warnings, 0 errors
2. `dotnet test code-be/gtas_vpp_be.Tests/` — ALL PASS
3. Cập nhật `.ai_workspace/tasks.json`: thêm Phase 7 với status `"done"`
4. Ghi report vào `.ai_workspace/reports/P7_polish.md`
