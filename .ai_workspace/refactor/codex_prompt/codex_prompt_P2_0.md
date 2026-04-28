Bạn là AI Worker (Codex) trong hệ thống refactor dự án GTAS VPP.

# Bắt buộc đọc trước
- `.ai_workspace/context.md` — cấu trúc project
- `.ai_workspace/architecture.md` — chuẩn coding
- `.ai_workspace/tasks.json` — task P2.0

# Nhiệm vụ: P2.0 — Viết Unit Tests (Safety Net)

Viết unit tests trong `code-be/gtas_vpp_be.Tests/` để có safety net TRƯỚC khi refactor BaseServices ở các bước tiếp.

## Yêu cầu

### Test Project setup
- Project `gtas_vpp_be.Tests` đã có (xUnit + coverlet). File duy nhất hiện tại: `SmokeTests/TestRunnerSmokeTests.cs` (10 LOC)
- Thêm ProjectReference tới `gtas_vpp_be.Service` nếu chưa có
- Thêm package `Moq` (hoặc `NSubstitute`) để mock dependencies

### Tests cần viết

#### 1. `BaseServicesTests/EnvironmentResolverTests.cs`
Test method `GetEnvironment()` trong BaseServices:
- Input Claims có `Server = "Test"` → return `"TestEnv"`
- Input Claims có `Server = "Live"` → return `"LiveEnv"`
- Input Claims null/empty → return `"TestEnv"` (default)

#### 2. `VPPRequestTests/VPPRequestServiceTests.cs`
Test các business rules quan trọng trong VPPRequestService:
- `ValidateItems()`: items null → throw, items rỗng → throw, qty <= 0 → throw, duplicate VPPId → throw
- `IsDeadlinePassed()`: test ngày trước/sau deadline
- `GenerateVPPCode()`: verify format `VPP-{year}{month:D2}-{userId}-{mmss}`
- `CreateOrderAsync()`: verify header được tạo đúng status (Submitted hoặc Pending nếu isAdditional)

#### 3. `BaseServicesTests/CrudOperationsTests.cs`
Test CRUD operations dùng InMemory DbContext:
- `AddAsync<T>()` — entity được thêm vào DB
- `ReadAsync<T>()` — trả đúng danh sách theo filter
- `DeleteAsync<T>()` — entity bị xóa
- `UpdateAsync<T>()` — entity được cập nhật

### Lưu ý
- Mock `IUnitOfWorkFactory`, `IUnitOfWork`, `IHttpContextAccessor`, `IDateTimeProvider` khi cần
- Dùng EF Core InMemoryDatabase cho CRUD tests
- Mỗi test method phải có tên rõ ràng: `MethodName_Scenario_ExpectedResult`
- KHÔNG test stored procedures (phụ thuộc SQL Server thật)

## Sau khi hoàn thành
1. Chạy `dotnet test code-be/gtas_vpp_be.Tests/` — tất cả tests phải PASS
2. Chạy `dotnet build gtas_vpp/gtas_vpp.slnx` — verify build
3. Cập nhật `.ai_workspace/tasks.json`: P2.0 status → `"done"`
4. Ghi report vào `.ai_workspace/reports/P2_0_tests.md`
