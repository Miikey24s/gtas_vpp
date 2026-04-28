Bạn là AI Worker (Codex) trong hệ thống refactor dự án GTAS VPP.

# Bắt buộc đọc trước
- `.ai_workspace/context.md` — cấu trúc project
- `.ai_workspace/architecture.md` — chuẩn coding
- `.ai_workspace/tasks.json` — tasks P3.1 → P3.4, P4.1

# Nhiệm vụ: Phase 3 (Cross-cutting Concerns) + Phase 4 (Frontend Cleanup)

Build verify sau mỗi task: `dotnet build gtas_vpp/gtas_vpp.slnx`

---

## P3.1 — Tạo ExceptionHandlingMiddleware

**Thực hiện**:
1. Tạo `code-be/gtas_vpp_be/Middleware/ExceptionHandlingMiddleware.cs`
2. Map exceptions → HTTP status + ProblemDetails (RFC 7807):
   - `KeyNotFoundException` → 404
   - `UnauthorizedAccessException` → 403
   - `InvalidOperationException` → 400
   - `ArgumentException` → 400
   - Mọi exception khác → 500
3. Response format:
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Order not found."
}
```
4. Register trong `Program.cs` (BE): `app.UseMiddleware<ExceptionHandlingMiddleware>();` — đặt TRƯỚC `app.UseAuthentication()`
5. Log mỗi exception qua `ILogger<ExceptionHandlingMiddleware>`

---

## P3.2 — Enable Serilog trên BE

**Thực hiện**:
1. Thêm packages vào `code-be/gtas_vpp_be/gtas_vpp_be.csproj`:
   - `Serilog.AspNetCore`
   - `Serilog.Sinks.Console`
   - `Serilog.Sinks.File`
2. Trong `Program.cs` (BE), thêm Serilog configuration:
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();
builder.Host.UseSerilog();
```
3. KHÔNG thay đổi FE logging (FE đã có Serilog nhưng comment out — giữ nguyên)

---

## P3.3 — Implement WriteLog

**Thực hiện**:
1. Trong `BaseServices.cs`, method `WriteLog()` hiện rỗng
2. Inject `ILogger<BaseServices>` vào constructor
3. Implement:
```csharp
public virtual void WriteLog(Exception ex, string context, Dictionary<string, object>? properties = null)
{
    _logger.LogError(ex, "Error in {Context} {@Properties}", context, properties);
}
```
4. Cập nhật constructor BaseServices thêm `ILogger<BaseServices>` parameter
5. Cập nhật DI nếu cần (ASP.NET tự resolve ILogger<T>)

---

## P3.4 — Extract JiraSettings

**Thực hiện**:
1. Tạo `code-be/gtas_vpp_be.Service/Helpers/JiraSettings.cs`:
```csharp
public class JiraSettings
{
    public string TestJiraIssue { get; set; } = string.Empty;
    public string LiveJiraIssue { get; set; } = string.Empty;
}
```
2. Thêm section vào `appsettings.json` (BE):
```json
"JiraSettings": {
    "TestJiraIssue": "",
    "LiveJiraIssue": ""
}
```
3. Register: `builder.Services.Configure<JiraSettings>(Configuration.GetSection("JiraSettings"));`
4. Trong `BaseServices.cs`: thay `JiraIssueLive`, `JiraIssueTest`, `DeployEnv` bằng `IOptions<JiraSettings>`
5. Xóa các class `JiraIssueLive`, `JiraIssueTest`, `DeployEnv` nếu chúng chỉ chứa config

---

## P4.1 — Extract Login Endpoint

**Thực hiện**:
1. Tạo `code-fe/gtas_vpp_fe/gtas_vpp_fe/Endpoints/LoginEndpoints.cs`:
```csharp
public static class LoginEndpoints
{
    public static void MapLoginEndpoints(this WebApplication app)
    {
        // Chuyển toàn bộ app.MapGet("/perform-login", ...) từ Program.cs vào đây
    }
}
```
2. Trong FE `Program.cs`: thay 40+ dòng inline bằng `app.MapLoginEndpoints();`
3. Giữ nguyên logic login, CHỈ di chuyển code

---

# Sau khi hoàn thành
1. `dotnet build gtas_vpp/gtas_vpp.slnx` — PASS
2. `dotnet test code-be/gtas_vpp_be.Tests/` — PASS
3. Cập nhật `.ai_workspace/tasks.json`: P3.1-P3.4, P4.1 status → `"done"`
4. Ghi report vào `.ai_workspace/reports/P3_P4_final.md`

# Quy tắc
- KHÔNG thay đổi API routes, DTO contracts, DB schema
- KHÔNG thay đổi Auth/JWT/Cookie logic
- Giữ behavior y nguyên, chỉ refactor cấu trúc
