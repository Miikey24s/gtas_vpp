# Prompts cho GPT-5.5 — GTAS VPP Refactor Full Plan

> **Cách dùng**: Copy từng prompt vào GPT-5.5 theo thứ tự. Mỗi prompt self-contained. GPT có file access nên có thể đọc trực tiếp file đường dẫn absolute.

---

## 📋 Common Preamble (paste mỗi session mới)

```
Bạn là senior .NET architect refactor codebase enterprise. Project GTAS VPP — hệ thống đặt văn phòng phẩm hằng tháng cho ~1000 user doanh nghiệp.

Tech stack: ASP.NET Core 8, EF Core, SQL Server, Blazor Server + Radzen, Docker Compose.

Trước khi làm, hãy đọc:
1. /opt/gtas_vpp/audit-report/HANDOFF_BRIEFING.md — context 5 phút
2. /opt/gtas_vpp/audit-report/AUDIT_REPORT.md — audit chi tiết 51 finding (paste section cần khi cụ thể)

CONSTRAINTS BẮT BUỘC (không được vi phạm):
1. KHÔNG đổi thuật toán TripleDES password. Encrypt("abc*123@") PHẢI = "wiSEc6nf/dK/Vu0E738j8Q==".
2. KHÔNG drop column trong migration. Chỉ add nullable column.
3. KHÔNG đổi public API signature trừ khi giải thích lý do và confirm với user.
4. KHÔNG sửa AI module (gtas_vpp_be.AI/) trừ khi user yêu cầu rõ ràng.
5. Output minimal diff, không rewrite cả file.
6. Mỗi PR = 1 finding (atomic commit).
7. Bắt buộc viết test cho mọi business logic change.
8. Code comment: tiếng Anh (chuẩn .NET ecosystem).

REPORTING FORMAT mỗi task:
- Tóm tắt thay đổi (3-5 dòng)
- Danh sách file đã sửa (absolute path)
- Danh sách test đã thêm
- Validation command để user chạy (dotnet test, curl, etc.)
- Risk + rollback (nếu có).

Confirm bạn đã đọc HANDOFF_BRIEFING.md trước khi nhận task tiếp theo.
```

---

## 🔴 PROMPT P0 — Security + Race Condition (3-5 ngày)

### Prompt P0.1 — Move TripleDES key sang IConfiguration (F-05)

```
TASK: P0.1 · Move TripleDES key sang IConfiguration

Đọc trước:
- /opt/gtas_vpp/audit-report/AUDIT_REPORT.md section "F-05 · Hardcoded encryption key" và "P0.1"
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Helpers/PasswordHelpers.cs (file gốc)
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Controllers/AuthController.cs (caller)

GOAL: 
Move hardcoded key "ttpsolutions" trong PasswordHelpers.cs sang IConfiguration với fallback default.

CONSTRAINTS (BẮT BUỘC):
1. KHÔNG đổi thuật toán TripleDES (ECB + MD5 key + PKCS7 + Base64).
2. KHÔNG đổi format ciphertext.
3. Golden vector PHẢI giữ: Encrypt("abc*123@") == "wiSEc6nf/dK/Vu0E738j8Q==" khi key="ttpsolutions".
4. Backward compat: nếu config rỗng, dùng default "ttpsolutions" → dev/test không gãy.

DELIVERABLE:
1. Tạo class PasswordEncoderOptions { public string? Key { get; set; } } trong /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Helpers/.
2. Tạo interface IPasswordEncoder + class TripleDesPasswordEncoder implement (giữ logic encrypt/decrypt từ PasswordHelpers.Encrypt cũ).
3. Update Program.cs đăng ký:
   builder.Services.Configure<PasswordEncoderOptions>(builder.Configuration.GetSection("PasswordEncryption"));
   builder.Services.AddSingleton<IPasswordEncoder, TripleDesPasswordEncoder>();
4. Update AuthController inject IPasswordEncoder thay vì gọi static PasswordHelpers.Encrypt.
5. Update appsettings.json + appsettings.Development.json thêm section:
   "PasswordEncryption": { "Key": "ttpsolutions" }
6. Update docker-compose.yml thêm env var PasswordEncryption__Key (default "ttpsolutions").
7. Xoá hết dead code BCrypt trong PasswordHelpers.cs (HashPassword, VerifyPassword, IsBcryptHash) — F-07.

ACCEPTANCE TEST (BẮT BUỘC viết, gate merge):
- File: /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/PasswordEncoderTests.cs (tạo mới)
- Test 1: TripleDes_GoldenVector — Encrypt("abc*123@") == "wiSEc6nf/dK/Vu0E738j8Q==" với key="ttpsolutions"
- Test 2: TripleDes_CustomKey_DifferentOutput — đổi key khác phải ra ciphertext khác
- Test 3: TripleDes_NullKey_UsesDefault — Options.Key=null fallback dùng "ttpsolutions"
- Test 4: TripleDes_RoundTrip — Decrypt(Encrypt(plaintext)) == plaintext

VALIDATION (user chạy sau):
cd /opt/gtas_vpp/gtas_vpp_be
dotnet test gtas_vpp_be.Tests --filter "FullyQualifiedName~PasswordEncoder"

Báo cáo kết quả + danh sách file đã sửa.
```

### Prompt P0.2 — Race condition fix CreateOrderAsync (F-01)

```
TASK: P0.2 · Fix race condition CreateOrderAsync với unique filtered index + catch DbUpdateException

Đọc trước:
- /opt/gtas_vpp/audit-report/AUDIT_REPORT.md section "F-01 · Race condition trong CreateOrderAsync" và "P0.2"
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Services/VPPRequestService.cs (focus method CreateOrderAsync ~ line 151-203)
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Model/VPPContext.cs (xem index khai báo)

PROBLEM:
2 request HTTP đồng thời gọi CreateOrderAsync (regular order) cùng user/period → cả 2 đều pass duplicate check (vì check ngoài transaction) → DB có 2 order regular cho 1 user/period. Vi phạm business rule.

GOAL:
Add unique filtered index level DB + catch DbUpdateException → throw ConflictException 409.

CONSTRAINTS:
1. KHÔNG drop column. Migration chỉ AddIndex.
2. Backward compat: nếu DB hiện có duplicate (data cũ), migration sẽ FAIL. Cần script cleanup trước (xem deliverable).
3. Không đổi public signature CreateOrderAsync.

DELIVERABLE:

A. Migration:
- File: /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Migrations/Migrations/{timestamp}_AddUniqueIndex_VPP01_OneRegularPerUserPeriod.cs
- SQL:
  CREATE UNIQUE INDEX UX_VPP01_OneRegularPerUserPeriod
  ON VPP01_RequestHeader (CreateUserId, Y, M)
  WHERE IsDeleted = 0 AND IsAdditionalOrder = 0;
- Update VPPContext.OnModelCreating (và VPPMigrationDbContext nếu cùng config) thêm HasIndex().HasFilter().IsUnique().

B. Pre-migration cleanup script:
- File: /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Helpers/SQL/Cleanup_DuplicateRegularOrders.sql
- Query phát hiện duplicate + mark IsDeleted=1 cho duplicate cũ hơn (giữ order ID nhỏ nhất). User chạy MANUAL trước khi apply migration.

C. Code fix:
- File: /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Services/VPPRequestService.cs
- Trong CreateOrderAsync sau SaveChanges, wrap try/catch:
  catch (DbUpdateException ex) when (IsUniqueViolation(ex))
  {
      await uow.RollbackAsync();
      throw new ConflictException("Bạn đã có đơn cho kỳ này.");
  }
- Helper IsUniqueViolation check SqlException.Number == 2601 || 2627.

D. Custom exception:
- File: /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Exceptions/ConflictException.cs (tạo mới nếu chưa có)
- Extends Exception.

E. Middleware mapping:
- File: /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Middleware/ExceptionHandlingMiddleware.cs (sửa)
- ConflictException → HTTP 409 + message.

ACCEPTANCE TEST:
- File: /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/VPPRequestTests/CreateOrderRaceConditionTests.cs (tạo mới)
- Test 1: CreateOrder_TwoConcurrentRegular_OneSucceedsOneFails — dùng Task.WhenAll 2 task, assert 1 success + 1 ConflictException
- Test 2: CreateOrder_DuplicateRegularDifferentPeriod_BothSucceed
- Test 3: CreateOrder_OneRegularOneAdditional_BothSucceed (filtered index không match additional)

Test dùng SQLite hoặc Testcontainers SQL Server (giảng giải cách dùng để user setup).

VALIDATION:
1. dotnet ef migrations add AddUniqueIndex_VPP01_OneRegularPerUserPeriod --project gtas_vpp_be.Migrations
2. dotnet ef database update
3. dotnet test --filter "FullyQualifiedName~CreateOrderRaceCondition"
4. Manual smoke test 2 tab browser submit cùng lúc — 1 tab dialog "đơn đã tồn tại".

Báo cáo + risk nếu có.
```

### Prompt P0.3 — Còn lại của P0 (JWT, VPPCode, dead code) gộp

```
TASK: P0.3 + P0.4 + P0.5 · JWT fail-fast + VPPCode unique format + remove dead BCrypt

Đọc trước:
- /opt/gtas_vpp/audit-report/AUDIT_REPORT.md section P0.3, P0.4, P0.5
- F-06, F-07, F-08

3 task atomic, làm trong 1 PR (security cleanup batch):

A. F-06 — JWT default key fail-fast:
- /opt/gtas_vpp/docker-compose.yml line ~65: 
  Đổi JWT_KEY=${JWT_KEY:-defaultKeyValueHere...} 
  THÀNH JWT_KEY=${JWT_KEY:?JWT_KEY environment variable is required and must be set.}
- Update .env.example (tạo nếu chưa có) ghi rõ JWT_KEY=<your-256bit-random-key>.
- Thêm validation trong Program.cs sau khi load config:
  if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
      throw new InvalidOperationException("JWT key must be set and >=32 chars");

B. F-07 — Remove dead BCrypt code:
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Helpers/PasswordHelpers.cs: xoá HashPassword, VerifyPassword, IsBcryptHash methods. (Có thể đã xoá ở P0.1, verify lại).
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Controllers/AuthController.cs line ~62-65: xoá comment "// fallback to bcrypt", đổi tên method TryLoginWithPassword → LoginWithTripleDesAsync để rõ ý.
- Remove BCrypt.Net-Next package reference từ gtas_vpp_be.Service.csproj (nếu có và không còn dùng).

C. F-08 — VPPCode unique format:
- File: /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Services/VPPRequestService.cs method GenerateVPPCode (~line 737-741)
- Đổi format từ $"{Y}{M:D2}{userId}{mmss}" THÀNH $"VPP-{Y}{M:D2}-{userId:D6}-{Guid.NewGuid():N}"[:24] hoặc tương tự đảm bảo unique.
  Format đề xuất: $"VPP-{Y:D4}{M:D2}-{Guid.NewGuid():N}".Substring(0, 24) — không leak userId, chắc chắn unique.
- Migration: AddUniqueIndex_VPP01_VPPCode với HasFilter("[VPPCode] IS NOT NULL").
- Test: GenerateVPPCode_1000Times_AllUnique (HashSet count == 1000).
- Cẩn thận data cũ có thể VPPCode trùng format khác — chạy script cleanup nếu cần.

ACCEPTANCE TEST:
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/VPPCodeGeneratorTests.cs (tạo mới)
- 3 test: format match regex, uniqueness 1000x, không chứa userId.

VALIDATION:
1. JWT: thử docker compose up MÀ KHÔNG set JWT_KEY → phải fail boot với clear error message.
2. dotnet test --filter "VPPCodeGenerator"
3. Manual: tạo 5 order, check VPPCode format mới + không trùng.

Báo cáo từng task A/B/C riêng biệt + commit message mẫu.
```

---

## 🟡 PROMPT P1 — Period Logic Stabilization (1 tuần)

### Prompt P1 (1 prompt cho cả phase — task nhỏ liên quan)

```
TASK: P1 · Stabilize period logic — Single Source of Truth

Đọc trước:
- /opt/gtas_vpp/audit-report/AUDIT_REPORT.md section §7.3 (P1) + F-02 + F-09 + F-23 + F-33
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Services/VPPRequestService.cs (GetCurrentAndPreviousPeriod ~ line 688-698)
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/Tab_Orders.razor.cs (line 51-62)
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Page_OrderCreate.razor.cs (line 353-360)
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_shared/DTOs/Res/VPP/VPP01_RequestHeaderResDTO.cs (line 30-34)

PROBLEM:
Period logic ("kỳ hiện tại là tháng nào?") tính ở 4 chỗ với clock khác nhau → drift. User submit order với Y/M khác BE expect.

BUSINESS RULE (user đã confirm):
- Kỳ tháng N chạy từ `00:00:00` ngày `05/N` đến `23:59:59` ngày `04/(N+1)`
- Từ `00:00:00` ngày `05/N`: kỳ hiện tại = tháng N, kỳ trước = tháng N-1
- Trước thời điểm đó: kỳ hiện tại = tháng N-1, kỳ trước = tháng N-2
- Edge case năm rollover (tháng 1 → tháng 12 năm trước) phải work.

GOAL:
1. 1 nguồn sự thật: PeriodCalculator class ở BE (pure function, testable).
2. FE chỉ consume GET /period-info, không tự tính.
3. Validate Y/M ở BE: chỉ accept current period (regular) hoặc previous period (additional).
4. DTO chuyển computed property sang plain property (BE materialize).

DELIVERABLE:

A. PeriodCalculator class:
- File: /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Domain/PeriodCalculator.cs (tạo folder Domain trong Service tạm thời, sẽ move sang Domain project ở P2)
- Pure class, không I/O, không DateTime.Now bên trong:
  public sealed class PeriodCalculator {
      private readonly int _deadlineDay;
      public PeriodCalculator(int deadlineDay = 5) => _deadlineDay = deadlineDay;
      public Period Current(DateTime now);
      public Period Previous(DateTime now);
      public bool IsDeadlinePassed(DateTime now, Period p);
  }
  public readonly record struct Period(int Year, int Month);
- Inject vào DI: builder.Services.AddSingleton(sp => new PeriodCalculator(sp.GetRequiredService<IConfiguration>().GetValue("VPPDeadlineDay", 5)));

B. VPPRequestService refactor:
- Inject PeriodCalculator + IDateTimeProvider.
- Bỏ method GetCurrentAndPreviousPeriod private.
- Tất cả nơi cần period: gọi _periodCalc.Current(_clock.Now).

C. Validate Y/M (F-09):
- Trong CreateOrderAsync, sau khi tính period authoritative:
  if (req.IsAdditional) {
      var prev = _periodCalc.Previous(_clock.Now);
      if (req.Y != prev.Year || req.M != prev.Month)
          throw new BusinessException($"Additional order chỉ cho kỳ {prev.Year}/{prev.Month:D2}.");
  } else {
      var cur = _periodCalc.Current(_clock.Now);
      if (req.Y != cur.Year || req.M != cur.Month)
          throw new BusinessException($"Regular order chỉ cho kỳ hiện tại {cur.Year}/{cur.Month:D2}.");
  }

D. DTO refactor (F-33):
- File: /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_shared/DTOs/Res/VPP/VPP01_RequestHeaderResDTO.cs
- Đổi IsDeadlinePassed, CanEdit, CanCancel từ computed → plain { get; set; }
- BE set giá trị trong projection (Mapster config hoặc Select() explicit).

E. FE bỏ logic period (F-02, F-23):
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Page_OrderCreate.razor.cs:
  - Bỏ logic tính period local (line 353-360)
  - Inject IAPIServices, OnInitializedAsync load PeriodInfo từ GET /api/VPPRequest/period-info
  - Submit: dùng PeriodInfo.CurrentPeriodYear / Month / PreviousPeriodYear / Month
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/Tab_Orders.razor.cs:
  - Bỏ getter CurrentOrderPeriodDate tự tính
  - Dùng PeriodInfo (đã có sẵn) thay vì DateTime.Now

ACCEPTANCE TEST:
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/Domain/PeriodCalculatorTests.cs (tạo mới)
- Theory test với InlineData:
  [InlineData("2026-04-04 23:59:59", 2026, 3, 2026, 2)]  // chưa qua deadline → kỳ là tháng 3
  [InlineData("2026-04-05 00:00:00", 2026, 4, 2026, 3)]  // đúng deadline → kỳ là tháng 4
  [InlineData("2026-04-05 00:00:01", 2026, 4, 2026, 3)]
  [InlineData("2026-05-04 23:59:59", 2026, 4, 2026, 3)]  // cuối kỳ tháng 4
  [InlineData("2026-01-04 12:00:00", 2025, 12, 2025, 11)] // năm rollover
  [InlineData("2026-01-05 00:00:00", 2026, 1, 2025, 12)]  // năm rollover deadline
- Test CreateOrder_WrongYM_Throws: submit Y=2099 → BusinessException.
- Test CreateOrder_AdditionalWithCurrentPeriod_Throws: type=additional nhưng Y/M=current period → throw.

VALIDATION:
1. dotnet test --filter "PeriodCalculator"
2. Manual: tạo order vào 23:59:59 ngày 4 (set clock VM hoặc mock IDateTimeProvider) → period = N-1. 00:00:01 ngày 5 → period = N.
3. FE smoke: submit order trên 2 browser khác timezone → cả 2 ra cùng period (vì BE quyết định).

Báo cáo + lưu ý migration nếu DTO change breaks FE binding.
```

---

## 🔴 PROMPT P3 — Performance (1.5 tuần) — LÀM TRƯỚC P2 do scale 1000 user

### Prompt P3 (gộp 5 task — chỉ liên quan EF query)

```
TASK: P3 · DbContext consolidation + Query tuning cho scale 1000 concurrent user

Đọc trước:
- /opt/gtas_vpp/audit-report/AUDIT_REPORT.md section §7.5 (P3) + F-04 + F-12 + F-13 + F-17 + F-18
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Program.cs (line 55-78)
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Controllers/LibraryController.cs (focus GetTableDataWithFilteringAsync line 85-194)
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Controllers/VPPRequestController.cs (focus GetDashboardCharts line 264-318)
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Mappings/MapsterConfig.cs (line 63-66)
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Services/VPPRequestService.cs (focus ApplyRequesterNamesAsync line 700-725)

PROBLEM:
Với 1000 concurrent user, hệ thống sẽ OOM trong vài giờ vì:
- F-12: Mỗi request LibraryController load 1000 row vào RAM rồi filter
- F-13: Dashboard load all orders user vào RAM rồi GroupBy
- F-17: List endpoint mapster load luôn Items collection (n+1 query)
- F-18: ApplyRequesterNamesAsync thêm 1 round-trip riêng cho FullName
- F-04: Dual DbContext (VPPContext + VPPMigrationDbContext) → config drift

GOAL:
Mọi query → IQueryable translate sang SQL, không in-memory materialize trừ khi cần.

DELIVERABLE (5 task atomic, có thể split 5 PR riêng):

A. P3.1 — Consolidate DbContext (F-04):
- Xoá /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Model/VPPMigrationDbContext.cs.
- Trong Program.cs: bỏ AddDbContext<VPPMigrationDbContext>, chỉ giữ AddDbContext<VPPContext>(o => o.UseSqlServer(connStr, sql => sql.MigrationsAssembly("gtas_vpp_be.Migrations"))).
- Update Migration project chỉ reference VPPContext.
- Verify migration history không broken: dotnet ef migrations list.

B. P3.2 — LibraryController bỏ in-memory filter (F-12):
- Method GetTableDataWithFilteringAsync<TModel, TDto>:
  TỪ:
    var allData = await ReadEntitiesAsync<TModel>(true, take: 1000);
    var query = allData.AsQueryable();
    query = query.Where(filter);
    var totalCount = query.Count();
  THÀNH:
    var query = _unitOfWork.VPPContext.Set<TModel>().AsNoTracking().Where(x => !x.IsDeleted);
    if (!string.IsNullOrWhiteSpace(filterExpression))
        query = query.Where(filterExpression);  // System.Linq.Dynamic.Core sẽ translate sang SQL nếu IQueryable
    var totalCount = await query.CountAsync();
    var data = await query.OrderBy(orderBy).Skip(skip).Take(top).ProjectToType<TDto>().ToListAsync();

C. P3.3 — Dashboard GroupBy ở DB (F-13):
- Method GetDashboardCharts:
  TỪ:
    var orders = await query.ToListAsync(); // materialize ALL
    var monthly = orders.GroupBy(...).Select(...).ToList();
  THÀNH:
    var monthly = await query
        .Where(x => x.SubmittedDate.HasValue)
        .GroupBy(x => new { x.SubmittedDate.Value.Year, x.SubmittedDate.Value.Month })
        .Select(g => new MonthlyChartItem {
            Year = g.Key.Year, Month = g.Key.Month,
            TotalOrders = g.Count(),
            TotalQty = g.Sum(x => x.TotalQty)  // nếu đã có column TotalQty từ P6
        })
        .ToListAsync();
- Tương tự cho statusDistribution chart.

D. P3.4 — Mapster projection explicit (F-17):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Mappings/MapsterConfig.cs:
  - Bỏ Map(dest => dest.Items, src => src.VPP02_RequestDetails) khỏi list config.
  - Tạo 2 config riêng:
    - VPP01_RequestHeaderListResDTO (không có Items, có TotalLines + TotalQty)
    - VPP01_RequestHeaderDetailResDTO (đầy đủ Items)
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Services/VPPRequestService.cs:
  - GetMyOrdersSummaryPagedAsync, GetAllOrdersPagedAsync... return List<...ListResDTO>
  - GetOrderByIdAsync (single) return ...DetailResDTO

E. P3.5 — JOIN v_Users bỏ ApplyRequesterNames (F-18):
- Trong projection ListResDTO:
  RequesterName = _ctx.Set<v_Users>()
      .Where(u => u.UserID == x.CreateUserId)
      .Select(u => u.FullName)
      .FirstOrDefault()
- Bỏ method ApplyRequesterNamesAsync khỏi VPPRequestService.

ACCEPTANCE TEST:
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/Performance/QueryShapeTests.cs (tạo mới)
- Dùng MockQueryable hoặc thực tế EF + SQLite:
  - Test 1: GetMyOrdersPaged_ReturnsCorrectCount_WhenDbHas5000Orders (verify không OOM, < 1s)
  - Test 2: GetDashboard_GroupByExecutedInDB — capture SQL via interceptor, assert có "GROUP BY"
  - Test 3: ListResDTO_DoesNotIncludeItems — count items column null/empty
- Snapshot test SQL output trước/sau (so sánh để verify SQL được tối ưu).

VALIDATION:
1. dotnet test --filter "QueryShape"
2. Manual: set log level Debug cho Microsoft.EntityFrameworkCore.Database.Command → tail log khi gọi /dashboard-charts → verify SQL có "GROUP BY", không có hàng nghìn row trả về.
3. Load test sơ bộ với k6 (1 endpoint, 50 concurrent user, 30s) → monitor memory.

Báo cáo + danh sách method/endpoint nào còn materialize, defer cho P3.6 nếu có.
```

---

## 🟠 PROMPT P2 — Clean Architecture (3-4 tuần) — split nhiều prompt

P2 là phase lớn nhất, nên SPLIT thành 4 sub-prompt để control risk. Làm theo thứ tự sau.

### Prompt P2.A — Tạo Domain + Application project skeleton

```
TASK: P2.A · Setup Clean Architecture skeleton — Domain + Application projects

Đọc trước:
- /opt/gtas_vpp/audit-report/AUDIT_REPORT.md section §5 (Target Architecture) + §7.4 (P2)

GOAL:
Tạo 2 project mới + thiết lập dependency direction Clean Arch:
- gtas_vpp_be.Domain (pure, no I/O dependency)
- gtas_vpp_be.Application (use cases, contracts)

Dependency rule: Web → Application → Domain. Infrastructure → Application contracts.

CONSTRAINTS:
1. CHƯA xoá VPPRequestService cũ. P2.A chỉ tạo skeleton + move pure class.
2. CHƯA refactor controller. P2.B sẽ làm.

DELIVERABLE:

A. Tạo projects:
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Domain/gtas_vpp_be.Domain.csproj
  - TargetFramework: net8.0
  - Không có package reference nào (pure)
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Application/gtas_vpp_be.Application.csproj
  - TargetFramework: net8.0
  - Reference: gtas_vpp_be.Domain, gtas_vpp_shared
  - Package: Microsoft.Extensions.Logging.Abstractions

B. Add vào solution:
cd /opt/gtas_vpp/gtas_vpp_be
dotnet sln add gtas_vpp_be.Domain/gtas_vpp_be.Domain.csproj
dotnet sln add gtas_vpp_be.Application/gtas_vpp_be.Application.csproj

C. Move pure class từ Service vào Domain:
- PeriodCalculator + Period record → gtas_vpp_be.Domain/Orders/PeriodCalculator.cs (đã tạo ở P1, nay move)
- Tạo gtas_vpp_be.Domain/Orders/OrderInvariants.cs (static class):
    - ValidateItems(IEnumerable<VPP02_ItemReqDTO> items) — extract từ VPPRequestService:633-650
- Tạo gtas_vpp_be.Domain/Orders/VPPCodeGenerator.cs (static class):
    - Generate(int year, int month) → string — logic từ P0.5
- Tạo gtas_vpp_be.Domain/Orders/OrderStateMachine.cs:
    - enum OrderAction { Cancel, Update, Approve, Reject }
    - static int Transition(int fromStatus, OrderAction action) — throws nếu invalid

D. Update Service project reference Domain:
- gtas_vpp_be.Service.csproj add reference gtas_vpp_be.Domain
- VPPRequestService inject IPeriodCalculator etc. (chưa thay logic, chỉ inject)

E. Update Web project reference Application:
- gtas_vpp_be.csproj add reference gtas_vpp_be.Application
- (chưa dùng, sẽ làm P2.B)

ACCEPTANCE:
- dotnet build cả solution PASS
- dotnet test PASS (regression không vỡ)
- Verify project dependency direction:
  - Domain không reference gì khác
  - Application reference Domain
  - Service vẫn build với Domain reference

VALIDATION:
cd /opt/gtas_vpp/gtas_vpp_be
dotnet build
dotnet test
dotnet list gtas_vpp_be.Domain/gtas_vpp_be.Domain.csproj reference  # phải rỗng
dotnet list gtas_vpp_be.Application/gtas_vpp_be.Application.csproj reference  # chỉ có Domain + shared

Báo cáo + tree project mới.
```

### Prompt P2.B — Tách VPPRequestService thành 3 handler

```
TASK: P2.B · Split VPPRequestService thành OrderQueryHandler + OrderCommandHandler + OrderApprovalHandler

Đọc trước:
- /opt/gtas_vpp/audit-report/AUDIT_REPORT.md section F-10 + F-11 + P2.2
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Services/VPPRequestService.cs (770 dòng)
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Controllers/VPPRequestController.cs (359 dòng)

GOAL:
Tách god class 770 dòng thành 3 handler theo CQS (Command Query Separation).

CONSTRAINTS:
1. Public API endpoint KHÔNG đổi (route + response shape).
2. P2.A đã tạo Application project — handler nằm trong gtas_vpp_be.Application/Orders/.
3. KHÔNG xoá VPPRequestService ngay — giữ làm wrapper gọi handler để controller chưa cần refactor.
4. Method duplicate (F-11) → gộp thành 1 generic helper trong handler.

DELIVERABLE:

A. Tạo 3 handler:
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Application/Orders/Queries/OrderQueryHandler.cs:
  - GetMyOrdersPagedAsync (gộp My + MyOrdersSummary)
  - GetAllOrdersPagedAsync (gộp All + Department, parametrize departmentCode? for filter)
  - GetPendingOrdersPagedAsync
  - GetOrderByIdAsync
  - GetPreviousOrderItemsAsync
  - GetPeriodInfoAsync
  - GetDashboardChartsAsync (move từ controller, theo P3.3)
  - GetSummaryAsync, GetSummaryByDepartmentAsync
  - Helper private GetFilteredOrdersPagedAsync<TFilter>(Expression<...>, paging)

- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Application/Orders/Commands/OrderCommandHandler.cs:
  - CreateOrderAsync
  - UpdateOrderAsync
  - CancelOrderAsync
  - Private helper BuildLogPayload

- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Application/Orders/Commands/OrderApprovalHandler.cs:
  - ApproveAdditionalOrderAsync
  - RejectAdditionalOrderAsync

B. Wire DI:
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Program.cs:
  builder.Services.AddScoped<OrderQueryHandler>();
  builder.Services.AddScoped<OrderCommandHandler>();
  builder.Services.AddScoped<OrderApprovalHandler>();

C. VPPRequestService chuyển sang wrapper (deprecated):
- File giữ nguyên path, nhưng method body chỉ delegate xuống handler tương ứng:
  public Task<...> CreateOrderAsync(...) => _commandHandler.CreateOrderAsync(...);
- Đánh dấu [Obsolete("Use OrderCommandHandler directly")] trên class.

D. Controller refactor (incremental — 1 controller):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Controllers/VPPRequestController.cs:
  - Inject 3 handler thay vì IVPPRequestService.
  - Đổi mọi call _vppRequestService.XYZ() → _queryHandler.XYZ() hoặc _commandHandler hoặc _approvalHandler.
  - Xoá GetDashboardCharts inline logic, gọi _queryHandler.GetDashboardChartsAsync.

ACCEPTANCE TEST:
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/Application/OrderCommandHandlerTests.cs
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/Application/OrderQueryHandlerTests.cs
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/Application/OrderApprovalHandlerTests.cs
- Mỗi handler ≥ 5 test case (happy + 4 failure):
  - CreateOrder: happy, duplicate, deadline_passed, no_items, wrong_period
  - Approve: happy, not_pending, not_admin
  - Query: by_period, by_status, by_department

VALIDATION:
1. dotnet test --filter "Handler" → all green
2. Manual: gọi mỗi endpoint VPPRequestController trên Swagger, verify response shape giống trước.
3. Performance: latency endpoint không tăng > 10%.

Báo cáo + line count comparison (before/after VPPRequestService.cs).
```

### Prompt P2.C — Bỏ Generic Repository nested tx + BaseServices + Config static

```
TASK: P2.C · Remove anti-pattern infrastructure code

Đọc trước:
- F-03 + F-14 + F-15 + F-20 + F-36

GOAL:
- Xoá IGenericRepository<T> + nested transaction
- Xoá BaseServices + IBaseServices
- Xoá Config.Initialize static, dùng IOptions<JwtSettings>
- Xoá BaseGenericController service locator

DELIVERABLE:

A. Xoá GenericRepository (F-03, F-15):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Services/GenericRepository.cs → delete file
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Services/IGenericRepository.cs → delete
- Caller hiện tại (PermissionController, LibraryController via BaseGenericController):
  - Refactor để inject IUnitOfWork hoặc DbContext trực tiếp.
  - Mọi Add/Update/Delete: _uow.VPPContext.Set<T>().Add(x); await _uow.SaveChangesAsync();
- Bỏ Program.cs đăng ký AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>)).

B. Xoá BaseServices (F-14, F-36):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Services/BaseServices.cs → delete file + IBaseServices
- Mọi service kế thừa BaseServices: bỏ inheritance, inject thẳng IUnitOfWork + ILogger<T> + IOptions<JiraSettings>.
- Bỏ Program.cs đăng ký AddScoped<IBaseServices, BaseServices>.

C. Config static → IOptions (F-20):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Helpers/Config.cs:
  - Xoá phương thức Initialize.
  - Có thể giữ class chỉ chứa enum (EnvironmentType, EFBaseMethod) nếu được dùng nơi khác.
- Tạo /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Service/Helpers/JwtSettings.cs:
  public class JwtSettings { public string Key {get;set;}=""; public string Issuer {get;set;}=""; public string Audience {get;set;}=""; public int ExpirationMinutes {get;set;}=60; }
- Program.cs: builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
- AuthController + JWT middleware: inject IOptions<JwtSettings> _jwt.

D. BaseGenericController (F-15):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be/Controllers/BaseGenericController.cs (nếu có) → delete
- Controller cụ thể inject service/handler thay vì service locator.

ACCEPTANCE TEST:
- Regression test mọi endpoint trước/sau response giống nhau.
- Test 1: JwtSettings_LoadedFromConfig_NotStatic — verify DI inject ok.
- Test 2: Repository_AnyOperation_NoNestedTransaction — wrap test trong transaction outer, gọi handler, verify SaveChanges không throw.

VALIDATION:
1. dotnet build PASS (sau khi xoá file, mọi reference được clean up).
2. dotnet test PASS.
3. Manual: smoke test 5 endpoint chính.

Báo cáo + danh sách file đã xoá.
```

### Prompt P2.D — Soft-delete decouple từ Cancelled

```
TASK: P2.D · Soft-delete chỉ dành cho hard-delete admin, không gộp với Cancelled

Đọc trước: F-21 + §7.4 P2.7

PROBLEM:
Hiện tại CancelOrderAsync set IsDeleted=true → order bị filter khỏi UI. User không thấy order đã huỷ.

GOAL:
- IsDeleted=true CHỈ khi admin thực sự delete.
- Cancel: chỉ đổi Status=Cancelled.
- UI hiện order cancelled trong tab History.

DELIVERABLE:

A. Service:
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Application/Orders/Commands/OrderCommandHandler.cs CancelOrderAsync:
  - Bỏ dòng header.IsDeleted = true.
  - Giữ header.Status = (int)VPPStatus.Cancelled.

B. Query filter:
- Tất cả query "đang hoạt động" (Tab_Orders current): WHERE Status NOT IN (4, 8)  (Cancelled, Rejected)
- Query history: WHERE IsDeleted = 0  (vẫn dùng)
- Query "đã huỷ" mới (nếu cần): WHERE Status = 4

C. Migration:
- Không cần migration schema. Chỉ data fix: optional script set IsDeleted=false cho order Status=Cancelled hiện tại để user thấy lại.

ACCEPTANCE TEST:
- Test 1: CancelOrder_DoesNotSetIsDeleted
- Test 2: GetMyOrders_IncludesCancelled
- Test 3: HardDelete_SetsIsDeleted (admin endpoint nếu có)

VALIDATION:
1. dotnet test
2. Manual: tạo order, cancel, refresh Tab Orders → thấy badge "Cancelled" thay vì biến mất.

Báo cáo.
```

---

## 🎨 PROMPT P4 — FE Refactor (1 tuần)

### Prompt P4 — Gộp StatusDisplay + BaseOrderTab + dedup permission

```
TASK: P4 · FE refactor — Shared StatusDisplay + BaseOrderTab + dedup permission

Đọc trước:
- /opt/gtas_vpp/audit-report/AUDIT_REPORT.md section §7.6 (P4) + F-16 + F-24
- 6 file Tab_*.razor.cs trong /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/

GOAL:
- StatusDisplay helper shared cho FE+BE
- BaseOrderTab generic base component cho 6 tab
- Dedup Permissions constant

DELIVERABLE:

A. StatusDisplay (F-16):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_shared/UI/StatusDisplay.cs (tạo mới)
  public static class StatusDisplay {
      public static string GetText(int status) => status switch {
          1 => "Submitted", 4 => "Cancelled", 6 => "Pending", 7 => "Approved", 8 => "Rejected", _ => "-"
      };
      public static string GetCssClass(int status) => status switch {
          1 => "vpp-badge-submitted", 4 => "vpp-badge-cancelled", 6 => "vpp-badge-pending",
          7 => "vpp-badge-approved", 8 => "vpp-badge-rejected", _ => "vpp-badge-default"
      };
      public static BadgeStyle GetBadgeStyle(int status)? // optional, nếu vẫn cần Radzen BadgeStyle
  }
- Xoá GetStatusText + GetStatusBadgeStyle trong 5 Tab_*.razor.cs.
- Xoá switch trong VPP01_RequestHeaderResDTO.StatusText, VPPRequestController.GetDashboardCharts.
- Tất cả gọi StatusDisplay.GetText(order.Status) + StatusDisplay.GetCssClass(order.Status).

B. BaseOrderTab<TFilter> (F-24):
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/BaseOrderTab.cs (abstract class kế thừa ComponentBase)
- Generic <TFilter> param.
- Property state: IsLoading, IsGridLoading, CurrentSkip, PageSize, FilterDebounceTimer, YearOptions/MonthOptions/StatusOptions
- Abstract method: protected abstract Task<...> FetchDataAsync(int skip, int top, TFilter filter);
- Virtual method: OnFilterChanged, OnLoadData (giữ logic chung).
- 6 Tab cụ thể chỉ extend BaseOrderTab<MyFilter> + override FetchDataAsync + định nghĩa columns trong .razor.

C. Permissions dedup (Maintainability):
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Helpers/Config.cs:
  - Xoá class Page_ComponentCode.ComponentCode.RequestOrder.
  - Mọi nơi dùng nó: import using gtas_vpp_shared.Constants; rồi gọi Permissions.RequestOrder.

ACCEPTANCE TEST:
- Test 1: StatusDisplayTests trong gtas_vpp_be.Tests — verify text + cssClass cho 5 status + default.
- FE: smoke test 6 tab render đúng, filter ok, badge màu đúng.

VALIDATION:
1. dotnet test --filter "StatusDisplay"
2. dotnet build cả FE solution
3. Manual: load 6 tab, đổi filter, verify đúng kết quả.

Báo cáo + line count reduction (trước/sau 6 Tab_*.razor.cs).
```

---

## ✨ PROMPT P5 — UX/UI Redesign (2-3 tuần) — chia 4 sub-prompt

### Prompt P5.A — Token system + sidebar consolidation

```
TASK: P5.A · Sidebar dùng token + xoá inline !important + status badge palette

Đọc trước:
- /opt/gtas_vpp/audit-report/AUDIT_REPORT.md section §6 (Target UX/UI) + F-40, F-41, F-45, F-30
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-tokens.css
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Layout/MainLayout.razor.css

DELIVERABLE:

A. Sidebar palette unify (F-40):
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Layout/MainLayout.razor.css:
  - Bỏ background-image gradient navy/tím.
  - Thay bằng background: var(--vpp-bg-elevated);
- Active menu item background: var(--vpp-primary-500); opacity 0.15.

B. Status badge palette mới (F-45):
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-tokens.css thêm semantic token:
  --vpp-info: #0ea5e9; --vpp-info-muted: #0ea5e91a;
  --vpp-warning: #f59e0b; --vpp-warning-muted: #f59e0b1a;
  --vpp-success: #10b981; --vpp-success-muted: #10b9811a;
  --vpp-danger: #ef4444; --vpp-danger-muted: #ef44441a;
- Tạo /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-status-badge.css với 5 class .vpp-badge-submitted/pending/approved/cancelled/rejected.

C. KPI minimal flat (F-30):
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-kpi.css:
  - Xoá kpi-blue, kpi-purple, kpi-emerald, kpi-amber, kpi-card-shine.
  - Giữ 1 class .vpp-kpi-card với background --vpp-bg-elevated, border 1px --vpp-border-default.
  - Accent: small icon top-right, opacity 0.4.

D. Bỏ inline !important (F-41):
- Pass tất cả file .razor trong /opt/gtas_vpp/gtas_vpp_fe/.../Components/Pages/VPPRequest/.
- Move inline Style="..." có !important vào .css file tương ứng.
- Tăng specificity bằng selector `.vpp-foo .rz-bar` thay vì `!important`.

ACCEPTANCE:
- Visual review: sidebar không còn gradient navy/tím, KPI cùng style, badge 5 màu phân biệt.
- grep "!important" /opt/gtas_vpp/gtas_vpp_fe/.../*.razor → 0 match.

VALIDATION:
1. dotnet build FE PASS
2. Mở Tab Orders, verify visual đúng design
3. Test contrast với DevTools axe extension

Báo cáo + screenshot before/after.
```

### Prompt P5.B — Self-host font + a11y + date helper

```
TASK: P5.B · Self-host fonts + Accessibility WCAG AA + Date format helper

Đọc trước: F-31, F-28, F-29

A. Self-host Inter + JetBrains Mono (F-31):
- Tải Inter (Regular 400, Medium 500, SemiBold 600, Bold 700) + JetBrains Mono (Regular, Medium).
- Đặt /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/fonts/.
- Thêm @font-face vào vpp-tokens.css (top of file).

B. A11y pass (F-28):
- Tất cả RadzenButton Icon="..." trong .razor:
  - Thêm Title="..." + Aria-label="...".
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-a11y.css:
  - Thêm :focus-visible outline 2px solid var(--vpp-primary-500).
  - Thêm @media (prefers-reduced-motion: reduce) { * { animation: none; transition: none; } }
- Audit contrast ratio: text trên gradient < 4.5:1 → đổi gradient hoặc text color.
- Icon decorative thêm aria-hidden="true".

C. DateFormatter helper (F-29):
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Helpers/DateFormatter.cs:
  public static class DateFormatter {
      public const string ShortDate = "dd/MM/yyyy";
      public const string LongDate = "HH:mm dd/MM/yyyy";
      public const string MonthYear = "MM/yyyy";
      public static string Format(DateTime? d, string fmt) => d?.ToString(fmt, CultureInfo.GetCultureInfo("vi-VN")) ?? "-";
  }
- Replace mọi .ToString("dd/MM/yyyy") trong 6 Tab + Page_OrderCreate → DateFormatter.Format(d, DateFormatter.ShortDate).

ACCEPTANCE:
- Run axe DevTools / Pa11y trên Tab Orders + Wizard.
- 0 violation level "serious" hoặc "critical".

VALIDATION:
1. Visual: font Inter render đúng trên Windows/Linux (test 2 OS).
2. Keyboard: Tab through Wizard, mỗi focus state visible.
3. Screen reader: NVDA hoặc VoiceOver đọc đúng aria-label cho icon button.

Báo cáo a11y score before/after.
```

### Prompt P5.C — Wizard IA re-balance + Empty state + Loading state

```
TASK: P5.C · Re-layout Wizard 3 step + Empty state CTA + Loading state thống nhất

Đọc trước: F-43, F-26, F-27

A. Wizard re-layout (F-43):
- /opt/gtas_vpp/gtas_vpp_fe/.../OrderCreateStep1.razor: 
  Thêm vào Step 1:
  - Period info (call API /period-info, hiển thị rõ kỳ chạy từ `00:00` ngày `05/M` đến `23:59` ngày `04/(M+1)`, ví dụ: "Đặt cho kỳ {Y}/{M}, hết hạn lúc 23:59 ngày 04 tháng sau")
  - Type indicator (New / Additional / Copy)
  - Nếu IsAdditional → bắt buộc nhập Reason (required validator).
- /opt/gtas_vpp/gtas_vpp_fe/.../OrderCreateStep2.razor:
  - Bỏ "Order Context" block (đã ở Step 1).
  - Chỉ giữ Catalog (60%) | Cart (40%) split.
- Step 3 giữ nguyên.

B. Empty state Component (F-26):
- Tạo /opt/gtas_vpp/gtas_vpp_fe/.../Components/Shared/VppEmptyState.razor:
  <div class="vpp-empty">
    <Icon name="@Icon" />
    <h3>@Title</h3>
    <p>@Description</p>
    @if (PrimaryAction != null) { <RadzenButton Click="@PrimaryAction">@PrimaryLabel</RadzenButton> }
    @if (SecondaryAction != null) { <RadzenLink Click="@SecondaryAction">@SecondaryLabel</RadzenLink> }
  </div>
- Apply ở 6 Tab thay vì <div class="vpp-empty-state">.

C. Loading state thống nhất (F-27):
- /opt/gtas_vpp/gtas_vpp_fe/.../wwwroot/css/vpp-loading.css (tạo mới)
- 4 mode rõ ràng:
  - .vpp-skeleton-page (full page placeholder)
  - .vpp-skeleton-grid (DataGrid loading row)
  - .vpp-progress-inline (cho AI search)
  - .vpp-button-spinner (cho Submit button)
- Pass tất cả Razor, chuẩn hoá:
  - Page first load: <SkeletonPage/>
  - DataGrid paginate: IsGridLoading=true (Radzen built-in)
  - AI search: <RadzenProgressBar/>
  - Form submit: <RadzenButton IsBusy="@IsSaving" BusyText="Đang lưu...">

ACCEPTANCE:
- Wizard 3 step cân (~ 200-300 dòng mỗi razor)
- 6 Tab có EmptyState với CTA
- 0 instance `glb.isBusyPage = true` (legacy block UI)

VALIDATION:
- Manual flow tạo order regular / additional / copy.
- Verify Step 1 hiển thị reason field khi additional.

Báo cáo + UX gif demo nếu có.
```

### Prompt P5.D — Mobile responsive + i18n

```
TASK: P5.D · Mobile responsive 4 breakpoint + i18n hardcode cleanup

Đọc trước: F-44, F-25, §6.3 + §6.5

A. Mobile responsive (F-44):
- /opt/gtas_vpp/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-responsive.css:
  Restructure với 4 breakpoint:
  
  /* 0-640px mobile */
  @media (max-width: 640px) {
      .vpp-kpi-grid { grid-template-columns: 1fr; }
      .vpp-sidebar { transform: translateX(-100%); }
      .vpp-sidebar.open { transform: translateX(0); }
      .vpp-datagrid { display: none; }
      .vpp-mobile-cards { display: block; }  /* card list thay grid */
  }
  /* 641-1024px tablet */
  @media (min-width: 641px) and (max-width: 1024px) {
      .vpp-kpi-grid { grid-template-columns: 1fr 1fr; }
      .vpp-sidebar { width: 60px; }  /* icon-only */
      .vpp-datagrid { overflow-x: auto; }
  }
  /* 1025-1440px laptop */
  /* 1441+ wide */

- Tạo component <VppMobileCard Items="@orders" /> cho DataGrid mobile fallback.

B. i18n cleanup (F-25):
- Grep tất cả hardcoded English string trong /opt/gtas_vpp/gtas_vpp_fe/.../Components/Pages/:
  rg '>"[A-Z][a-z]+\s+[a-z]+' --type razor
- Migrate vào .resx:
  - /opt/gtas_vpp/gtas_vpp_fe/.../Resources/Resources.vi-VN.resx
  - /opt/gtas_vpp/gtas_vpp_fe/.../Resources/Resources.en-US.resx
- Replace literal → @Loc["key"].

ACCEPTANCE:
- Mobile (Chrome DevTools iPhone SE 360x640): sidebar collapse, KPI 1 cột, DataGrid → card.
- Tablet: sidebar icon-only, KPI 2 cột.
- i18n: chuyển locale vi-VN ↔ en-US, mọi string đổi đúng.

VALIDATION:
1. Manual responsive test 4 viewport size.
2. Locale switcher trên header → verify text đổi.

Báo cáo + screenshot 4 viewport.
```

---

## 📋 PROMPT P6 — Audit Trail + State Machine (1 tuần)

### Prompt P6 (gộp)

```
TASK: P6 · Audit trail columns + OrderStateMachine + Price snapshot

Đọc trước:
- /opt/gtas_vpp/audit-report/AUDIT_REPORT.md §7.8 (P6) + F-19, F-22

A. Migration add 5 column (F-19):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Migrations/Migrations/{timestamp}_AddApprovalColumns_VPP01.cs:
  AddColumn<int>("ApprovedById", "VPP01_RequestHeader", nullable: true);
  AddColumn<DateTime>("ApprovedAt", "VPP01_RequestHeader", nullable: true);
  AddColumn<int>("RejectedById", "VPP01_RequestHeader", nullable: true);
  AddColumn<DateTime>("RejectedAt", "VPP01_RequestHeader", nullable: true);
  AddColumn<string>("RejectReason", "VPP01_RequestHeader", maxLength: 500, nullable: true);
- Update VPP01_RequestHeader entity + VPPContext config.

B. Update Approval/Reject handler:
- OrderApprovalHandler.ApproveAdditionalOrderAsync set ApprovedById/At.
- RejectAdditionalOrderAsync set RejectedById/At + RejectReason.
- Bỏ ghi reason vào VPP03_Log JSON (giữ Log thường thôi).

C. OrderStateMachine (đã có skeleton từ P2.A):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Domain/Orders/OrderStateMachine.cs:
  Hoàn thiện logic transition.
- Update handler dùng OrderStateMachine.Transition() thay vì hardcode switch case.

D. CurrentSinglePrice snapshot (F-22):
- OrderCommandHandler.CreateOrderAsync:
  Khi build VPP02_RequestDetail entity, lookup L06_VPPSupplierMapping:
    var price = await _uow.VPPContext.Set<L06_VPPSupplierMapping>()
        .Where(x => x.VPPId == item.VPPId && !x.IsDeleted)
        .Select(x => x.Price)
        .FirstOrDefaultAsync();
    detail.CurrentSinglePrice = price ?? 0;
- N+1 risk: batch lookup all VPPId 1 query bằng GROUP BY hoặc DISTINCT.

ACCEPTANCE TEST:
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/Domain/OrderStateMachineTests.cs:
  Mỗi (status, action) → expected new status hoặc throw.
  ~15 test case.
- Approve_SetsApprovedByAndAt_Test.
- CreateOrder_SnapshotsPriceFromL06_Test.

VALIDATION:
1. dotnet ef database update → migration apply.
2. dotnet test PASS.
3. Manual: approve 1 additional order, verify VPP01_RequestHeader có ApprovedById/At, không null.

Báo cáo + SQL query mẫu để extract report "duyệt trong tháng".
```

---

## 🧪 PROMPT P7 — Test + CI (1 tuần)

### Prompt P7 (gộp toàn phase)

```
TASK: P7 · Test coverage expansion + GitHub Actions CI + load test setup

Đọc trước: §7.9 (P7) + F-32

A. Domain layer tests (pure, fast):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/Domain/:
  - PeriodCalculatorTests.cs — 7 test (đã có từ P1, verify pass)
  - OrderInvariantsTests.cs — test ValidateItems (null, empty, valid, duplicate VPPId, qty<=0)
  - VPPCodeGeneratorTests.cs — uniqueness, format
  - OrderStateMachineTests.cs — 15 test transition

B. Application layer tests (in-memory DbContext):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/Application/:
  - OrderCommandHandlerTests.cs (đã có từ P2.B)
  - OrderQueryHandlerTests.cs
  - OrderApprovalHandlerTests.cs
- Setup helper: TestDbContextFactory với SQLite in-memory.

C. Integration test (race condition):
- /opt/gtas_vpp/gtas_vpp_be/gtas_vpp_be.Tests/Integration/RaceConditionTests.cs:
  - Spawn 2 task gọi CreateOrderAsync cùng user/period đồng thời.
  - Verify 1 success, 1 fail ConflictException.
  - Có thể cần Testcontainers SQL Server (vì SQLite không support filtered unique index giống).

D. Playwright smoke test (FE):
- Tạo /opt/gtas_vpp/gtas_vpp_be.Tests.E2E/ (project mới hoặc folder).
- Test cases:
  1. Login với password "abc*123@" → redirect dashboard.
  2. Create regular order → submit → see in Tab Orders.
  3. Filter Tab History theo year/month → đúng kết quả.

E. GitHub Actions CI:
- /opt/gtas_vpp/.github/workflows/ci.yml (tạo mới):
  name: CI
  on: [push, pull_request]
  jobs:
    build-test:
      runs-on: ubuntu-latest
      steps:
        - uses: actions/checkout@v4
        - uses: actions/setup-dotnet@v4
          with: { dotnet-version: '8.0.x' }
        - run: dotnet restore gtas_vpp_be/gtas_vpp_be.sln
        - run: dotnet build gtas_vpp_be/gtas_vpp_be.sln --no-restore --configuration Release
        - run: dotnet test gtas_vpp_be/gtas_vpp_be.Tests --no-build --configuration Release --logger trx
        - uses: actions/upload-artifact@v4
          if: always()
          with: { name: test-results, path: '**/TestResults/*.trx' }
- Update README.md badge build status.

F. Branch protection (manual setup, document only):
- /opt/gtas_vpp/docs/CONTRIBUTING.md (tạo mới):
  - Hướng dẫn user setup branch protection trên GitHub: main protected, require PR + CI pass.

G. Load test setup (P7.6):
- Tạo /opt/gtas_vpp/load-test/k6/dashboard.js:
  import http from 'k6/http';
  export const options = { vus: 1000, duration: '2m' };
  export default function () {
      http.get('http://localhost:5000/api/VPPRequest/dashboard-charts', {
          headers: { Authorization: `Bearer ${__ENV.TOKEN}` }
      });
  }
- Tạo /opt/gtas_vpp/load-test/k6/create-order.js tương tự.
- README load-test/README.md hướng dẫn run.

ACCEPTANCE:
- Test coverage report (Coverlet) cho gtas_vpp_be.Service + gtas_vpp_be.Application ≥ 60%.
- GitHub Actions: push PR test → CI run → green.
- Load test: P95 latency < 500ms với 1000 VU.

VALIDATION:
1. dotnet test --collect:"XPlat Code Coverage" → report.
2. Push 1 PR thử → CI tab GitHub show check.
3. k6 run load-test/k6/dashboard.js → metrics output.

Báo cáo + coverage % + load test summary.
```

---

## 📝 Tips chung khi work với GPT-5.5

1. **Mỗi phase = 1 conversation mới** (tránh context dài quá → quên constraint).
2. **Trước khi gen code**: yêu cầu GPT confirm đã đọc HANDOFF_BRIEFING + section liên quan.
3. **Sau khi gen code**: TỰ chạy validation commands trong prompt, KHÔNG trust blindly.
4. **Nếu test fail**: gửi log lại GPT + yêu cầu fix iterative, không rewrite từ đầu.
5. **Git commit theo task**: 1 task atomic = 1 commit; PR riêng cho từng phase con (P2.A, P2.B...).
6. **Backup trước mỗi phase**: `git checkout -b refactor/p0-security` rồi work trên branch.
7. **Rollback**: nếu phase nào sai, `git reset --hard` về branch cũ.

## 🎯 Tracking progress

User có thể tạo `/opt/gtas_vpp/audit-report/PROGRESS.md` ghi:
- [ ] P0.1 — TripleDES key config (xong ngày YYYY-MM-DD, PR #X)
- [ ] P0.2 — Race condition fix
- [ ] P0.3 — JWT + VPPCode + Dead code
- [ ] P1 — Period logic
- [ ] P3.A — DbContext consolidate
- ...

— *Hết — happy refactoring với GPT-5.5!*
