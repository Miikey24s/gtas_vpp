Bạn là AI Worker (Codex) trong hệ thống refactor dự án GTAS VPP.

# Bắt buộc đọc trước
- `.ai_workspace/context.md` — cấu trúc project
- `.ai_workspace/architecture.md` — chuẩn coding
- `.ai_workspace/reports/post_refactor_review.md` — kết quả review từ Gemini

# Nhiệm vụ: Phase 5 — Security Fixes + Cleanup

Build verify sau mỗi task: `dotnet build gtas_vpp/gtas_vpp.slnx`

---

## P5.1 — Fix SQL Injection trong StoredProcedureExecutor (CRITICAL)

**Vấn đề**: `ExecuteQueryAsync(string query)` gọi `.FromSqlRaw(query)` với raw string. Nếu query chứa user input → SQL injection.

**Thực hiện**:
1. Trong `StoredProcedureExecutor.cs`, sửa `ExecuteQueryAsync`:
   - Thay `FromSqlRaw(query)` → `FromSqlRaw(query, parameters)` với overload nhận `params object[] parameters`
   - Hoặc: đổi signature thành `ExecuteQueryAsync(FormattableString query)` rồi dùng `FromSqlInterpolated(query)`
2. Tìm tất cả chỗ gọi `ExecuteQueryAsync` trong project, đảm bảo chúng truyền parameterized query
3. Nếu có chỗ nào build query bằng string concatenation → refactor thành parameterized

---

## P5.2 — Fix CORS Policy (CRITICAL)

**Vấn đề**: Backend dùng `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()` — không an toàn cho production.

**Thực hiện**:
1. Trong `code-be/gtas_vpp_be/appsettings.json`, thêm:
```json
"CorsSettings": {
    "AllowedOrigins": ["https://localhost:5001"]
}
```
2. Trong `code-be/gtas_vpp_be/appsettings.Development.json`, thêm:
```json
"CorsSettings": {
    "AllowedOrigins": ["https://localhost:5001", "https://localhost:5002"]
}
```
3. Trong `Program.cs` (BE), sửa CORS policy:
```csharp
var allowedOrigins = Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```
4. Đổi `app.UseCors("AllowAll")` → `app.UseCors("AllowFrontend")`

---

## P5.3 — Fix Cookie Security

**Vấn đề**: Cookie thiếu `SecurePolicy.Always`, `SameSiteMode.None` không hoạt động đúng trên HTTPS.

**Thực hiện**:
1. Trong `code-fe/gtas_vpp_fe/gtas_vpp_fe/Program.cs`, tìm `.AddCookie(options =>` block
2. Thêm: `options.Cookie.SecurePolicy = CookieSecurePolicy.Always;`
3. Đảm bảo `options.Cookie.HttpOnly = true;` (nếu chưa có)

---

## P5.4 — Xóa commented code trong FE

**Vấn đề**: Nhiều file .razor.cs chứa khối code comment cũ (đặc biệt là `.ContinueWith(x => x.Result...)` anti-patterns).

**Thực hiện**:
1. Quét tất cả file trong `code-fe/gtas_vpp_fe/gtas_vpp_fe/Components/`
2. Xóa các khối code bị comment ra (nhiều dòng `//` liên tiếp chứa code cũ)
3. GIỮ LẠI comments hữu ích (giải thích logic, TODO, v.v.)
4. Các file cần kiểm tra đặc biệt:
   - `Tab_User.razor.cs`
   - `Tab_PagePermission.razor.cs`
   - `Component_Library.razor.cs`
   - Mọi file .razor.cs khác trong thư mục Components

---

## P5.5 — Viết tests cho Controller + Middleware

**Thực hiện**:
1. Tạo `code-be/gtas_vpp_be.Tests/MiddlewareTests/ExceptionHandlingMiddlewareTests.cs`:
   - Test KeyNotFoundException → 404 ProblemDetails
   - Test UnauthorizedAccessException → 403 ProblemDetails
   - Test InvalidOperationException → 400 ProblemDetails
   - Test generic Exception → 500 ProblemDetails

2. Tạo `code-be/gtas_vpp_be.Tests/ControllerTests/VPPRequestControllerTests.cs`:
   - Test GetMyOrders — valid user → Ok
   - Test GetMyOrders — null UserId claim → Unauthorized
   - Test CreateOrder — valid → Ok
   - Test GetOrderById — not found → NotFound (qua middleware)
   - Mock `IVPPRequestService`, `IGenericRepository`, `IStoredProcedureExecutor`

3. Tạo `code-be/gtas_vpp_be.Tests/ControllerTests/AuthControllerTests.cs`:
   - Test Login — valid credentials → Ok + token
   - Test Login — invalid → Unauthorized
   - Mock dependencies

---

# Sau khi hoàn thành
1. `dotnet build gtas_vpp/gtas_vpp.slnx` — PASS
2. `dotnet test code-be/gtas_vpp_be.Tests/` — ALL PASS
3. Cập nhật `.ai_workspace/tasks.json`: thêm Phase 5 tasks với status `"done"`
4. Ghi report vào `.ai_workspace/reports/P5_security_cleanup.md`

# Quy tắc
- P5.1 và P5.2 là CRITICAL security fixes — ưu tiên cao nhất
- KHÔNG thay đổi API routes, DTO contracts, DB schema
- KHÔNG xóa code đang hoạt động, chỉ xóa commented-out code
