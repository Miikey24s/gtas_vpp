Bạn là AI Worker (Codex) trong hệ thống refactor dự án GTAS VPP.

# Bắt buộc đọc trước
- `.ai_workspace/context.md` — cấu trúc project
- `.ai_workspace/architecture.md` — chuẩn coding phải tuân thủ
- `.ai_workspace/tasks.json` — tasks P2.1 → P2.7

# Nhiệm vụ: Phase 2 — Service Layer Refactor

Thực hiện 7 tasks lần lượt. Build verify sau mỗi task: `dotnet build gtas_vpp/gtas_vpp.slnx`. Nếu fail → fix trước khi tiếp.

---

## P2.1 — Extract IEnvironmentResolver

**Vấn đề**: `GetEnvironment()` duplicate ở cả `BaseServices.cs` lẫn `UnitOfWork.cs`. Logic giống hệt.

**Thực hiện**:
1. Tạo `code-be/gtas_vpp_be.Service/Helpers/IEnvironmentResolver.cs`:
```csharp
public interface IEnvironmentResolver
{
    string Resolve(IEnumerable<Claim>? claims, string? fallbackEnv = null);
}
```
2. Tạo `code-be/gtas_vpp_be.Service/Helpers/EnvironmentResolver.cs` — chuyển logic từ `GetEnvironment()` vào đây
3. Xóa `GetEnvironment()` khỏi `BaseServices.cs` và `UnitOfWork.cs`
4. Inject `IEnvironmentResolver` vào cả `BaseServices` và `UnitOfWork` qua constructor
5. Register DI trong `Program.cs` (BE): `builder.Services.AddSingleton<IEnvironmentResolver, EnvironmentResolver>();`
6. Giữ nguyên logic Test/Live mapping, KHÔNG thay đổi behavior

---

## P2.2 — Extract IUserNameResolver

**Vấn đề**: `WithUserNames()`, `SetUserNameIfExists()`, `IncludeUserInfoIfNeeded()` trong BaseServices dùng reflection nặng.

**Thực hiện**:
1. Tạo `code-be/gtas_vpp_be.Service/Services/IUserNameResolver.cs`:
```csharp
public interface IUserNameResolver
{
    Task<List<T>> WithUserNamesAsync<T>(List<T> entities, DbContext context) where T : class;
    Task IncludeUserInfoAsync<T>(T entity, DbContext context) where T : class;
}
```
2. Tạo `UserNameResolver.cs` — chuyển 3 methods từ BaseServices sang
3. Xóa 3 methods khỏi BaseServices
4. Inject `IUserNameResolver` vào BaseServices qua constructor
5. Cập nhật các chỗ gọi trong BaseServices (`ReadAsync`, `GetByIdAsync`, `GetByIdIncludeAsync`)
6. Register DI: `builder.Services.AddScoped<IUserNameResolver, UserNameResolver>();`

---

## P2.3 — Extract IGenericRepository<T>

**Vấn đề**: BaseServices chứa CRUD methods với string-based context switching (`nameof(Config.ContextType.VPPContext)` everywhere).

**Thực hiện**:
1. Tạo `code-be/gtas_vpp_be.Service/Services/IGenericRepository.cs`:
```csharp
public interface IGenericRepository<T> where T : class
{
    Task<T?> AddAsync(T entity);
    Task<T> UpdateAsync(T entity, Expression<Func<T, object>>[]? properties = null);
    Task<List<T>> UpdateRangeAsync(List<T> entities, Expression<Func<T, bool>> expression);
    Task<bool> DeleteAsync(object id);
    Task<List<T>> ReadAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IQueryable<T>>? include = null);
    Task<T?> GetByIdAsync(object id);
}
```
2. Tạo `GenericRepository.cs` — inject `IUnitOfWork`, dùng `_unitOfWork.VPPContext.Set<T>()` trực tiếp (bỏ switch/case)
3. Xóa CRUD methods khỏi BaseServices (AddAsync, UpdateAsync, UpdateRangeTAsync, DeleteAsync, ReadAsync, GetByIdAsync, GetByIdIncludeAsync)
4. Register DI: `builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));`
5. Cập nhật BusinessService và VPPRequestService nếu chúng gọi các methods đã xóa

---

## P2.4 — Extract IStoredProcedureExecutor

**Vấn đề**: `SP()` và `Query()` trong BaseServices nên tách riêng.

**Thực hiện**:
1. Tạo `code-be/gtas_vpp_be.Service/Services/IStoredProcedureExecutor.cs`:
```csharp
public interface IStoredProcedureExecutor
{
    Task<sp_ResDTO> ExecuteSPAsync(string spName, string spType, object param, int? timeout = 300);
    Task<sp_ResDTO> ExecuteQueryAsync(string query, int? timeout = 300);
}
```
2. Tạo `StoredProcedureExecutor.cs` — inject `IUnitOfWork`, chuyển logic từ BaseServices.SP() và BaseServices.Query()
3. Xóa SP() và Query() khỏi BaseServices (và IBaseServices interface)
4. Register DI: `builder.Services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();`
5. Cập nhật BusinessService: thay base.SP() → inject IStoredProcedureExecutor

---

## P2.5 — Refactor UnitOfWork

**Vấn đề**: Dùng `TransactionScope` thay vì `DbContext.Database.BeginTransactionAsync()`. Quá phức tạp.

**Thực hiện**:
1. Trong `UnitOfWork.cs`:
   - Xóa `using System.Transactions;`
   - Thay `TransactionScope` bằng `IDbContextTransaction` từ `DbContext.Database`
   - `BeginTransactionAsync()`: gọi `VPPContext.Database.BeginTransactionAsync()`
   - `CommitAsync()`: gọi `transaction.CommitAsync()` sau `SaveChangesAsync()`
   - `Rollback()`: gọi `transaction.RollbackAsync()`
2. Inject `IEnvironmentResolver` (từ P2.1) thay vì tự duplicate GetEnvironment()
3. Xóa `GetEnvironment()` method và Claims property khỏi UnitOfWork (đã extract ở P2.1)

---

## P2.6 — Remove BusinessService wrapper

**Vấn đề**: `BusinessService` chỉ là wrapper gọi lại BaseServices methods. Không cần thiết.

**Thực hiện**:
1. Trong mỗi controller đang inject `IBusinessService`:
   - Thay bằng inject `IGenericRepository<T>` (cho CRUD) + `IStoredProcedureExecutor` (cho SP/Query)
   - Ví dụ: `_businessService.BaseService<L04_VPP>(EF_BASEMETHOD.EF_GetTAsync, ...)` → `_repository.ReadAsync<L04_VPP>(...)`
   - `_businessService.SP(...)` → `_spExecutor.ExecuteSPAsync(...)`
2. Xóa file `BusinessService.cs` 
3. Xóa DI registration trong Program.cs (BE)
4. Cập nhật `BaseGenericController.cs` nếu nó inject IBusinessService

**LƯU Ý**: Controllers affected: LibraryController, PermissionController, SQLController, VPPRequestController, BaseGenericController. Kiểm tra kỹ từng controller.

---

## P2.7 — DRY VPPRequestService

**Vấn đề**: `GetMyOrdersAsync()` và `GetMyOrdersSummaryAsync()` gần giống hệt nhau.

**Thực hiện**:
1. Tạo 1 private method chung: `GetFilteredOrdersAsync(int userId, ...)`
2. Cả `GetMyOrdersAsync` và `GetMyOrdersSummaryAsync` gọi method chung này
3. Nếu 2 methods hoàn toàn giống → xóa 1 cái, giữ 1 cái
4. Cập nhật interface `IVPPRequestService` nếu cần

---

# Sau khi hoàn thành
1. `dotnet build gtas_vpp/gtas_vpp.slnx` — PASS
2. `dotnet test code-be/gtas_vpp_be.Tests/` — chạy existing tests (có thể cần update tests theo refactor)
3. Cập nhật `.ai_workspace/tasks.json`: P2.1-P2.7 status → `"done"`
4. Ghi report vào `.ai_workspace/reports/P2_refactor.md`

# Quy tắc
- Tuân thủ `.ai_workspace/architecture.md`
- KHÔNG thay đổi API contracts (DTO, endpoint routes)
- KHÔNG thay đổi DB schema/migrations
- Nếu test cũ fail do refactor → cập nhật test cho khớp code mới, KHÔNG xóa test
