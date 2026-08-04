# BACKEND-REFACTOR-001 B0R — Contract, ownership và cleanup ledger

- Status: `B0R + B1a-1 COMPLETE — B1a-1b AUDITED; IMPLEMENTATION WAIT FOR LIVE CAPACITY`
- Characterization HEAD: `e6d3c5ee`; authorization slice base: `codex/ai-agent-foundation` @ `c653ac8c`
- Khảo sát ngày: `2026-08-04`
- Authority: [`BACKEND-REFACTOR-001.md`](./BACKEND-REFACTOR-001.md),
  [`ARCH-001-MODULE-MAP.md`](../architecture/ARCH-001-MODULE-MAP.md),
  `src/Backend/AGENTS.md`

## 0. Bản một ánh nhìn

| Mục | Kết luận hiện tại |
|---|---|
| Baseline | Characterization focused `83/83`; authorization/manifest focused `45/45`; backend unit `525/525`; integration mặc định `14 pass / 6 skip`; LocalDB disposable `20/20` từ SQL slice; final full backend verify PASS |
| Kiến trúc | Giữ `API → Application → Domain`; Shared là wire contract; không tạo project/microservice mới |
| Xóa an toàn đầu tiên | B1a-1 đã xóa 5 private `*LegacyAsync`, `ObjectHelpers.cs`, `PasswordHelpers.cs`; post-delete usage scan 0 |
| Chưa được xóa | `BaseServices`, `IBaseServices`, `GenericRepository`, `IGenericRepository`, `BaseGenericController` còn consumer thật |
| RBAC contract | Canonical có 3 persona; Procurement legacy alias về Manager; test đã khóa legacy ID không thuộc persona và không còn group/membership active sau reconciliation |
| HTTP contract | Manifest MVC khóa `112` endpoint theo verb + route + effective authorization; không khóa tên/controller nội bộ để vẫn cho phép refactor |
| Documentation drift | Residual `SQLController` đã được gỡ khỏi module map sau repo-wide search xác nhận không còn file/callsite |
| Localization debt | Backend trả raw English `CanCreateOrderReason`/`CanCreateAdditionalReason`; UI tiếng Việt có thể lộ English như board Order Create |
| Bước production tiếp theo | Chưa mở: khôi phục quota measurement/reforecast rồi mới chạy B1a-1b; B1a-2 vẫn characterize riêng, không trộn localization cutover |

## 1. Baseline đã kiểm chứng

| Gate | Kết quả trên HEAD khảo sát |
|---|---|
| `./scripts/gtas.cmd preflight -Scope backend` | PASS |
| `./scripts/gtas.cmd test-backend` | PASS — `525/525` sau authorization ratchet |
| Integration mặc định | PASS — `14`, skip đúng `6` opt-in LocalDB |
| `GTAS_QA_SQL_INTEGRATION=1` | PASS — `20/20`, `0` skip |
| `./scripts/gtas.cmd verify -Scope backend` | PASS — build sạch, unit/integration, EF pending-model, format, vulnerability và Gitleaks |
| EF model | `No changes have been made to the model since the last migration.` |

Số test là snapshot của HEAD khảo sát, không phải invariant lâu dài. B1 trở đi phải chạy lại gate trên
HEAD của từng slice.

## 2. Module ownership hiện tại

| Module | HTTP/API owner | Application owner hiện tại | Domain/persistence owner | Hotspot cần migrate-on-touch |
|---|---|---|---|---|
| Identity & Access | `AuthController`, `AccountController`, `PermissionController`, `Api/Authorization/*` | auth bootstrap, membership/account lifecycle | `Domain/Auth/*` | `PermissionController` và service authorization còn lớn; generic repository còn consumer |
| Catalog & Pricing | `LibraryController`, `VppCatalogController`, `VPPPriceController`, `VPPPriceListController` | catalog, price list, price workflow, integrity | `Domain/Library/*` | `LibraryController` còn generic boundary; không retire trước consumer ledger |
| Requests | `VPPRequestController` | `VPPRequestService` | `Domain/VPP/VppRequest*` | service khoảng 2.5K dòng, controller khoảng 1.2K dòng; tách theo query/command/supplement sau characterization |
| Settlement | `PeriodSettlementController` | `PeriodSettlementService` + export builders | settlement entities/configuration | snapshot/hash/idempotency/four-eyes là invariant bắt buộc |
| Reports | `ReportsController` | `ReportService`, builders, insight providers | read model qua contexts | pilot tốt cho pattern module vì read-heavy và export coverage mạnh |
| Notifications | `NotificationsController`, hubs/outbox API | notification/email services | `Domain/Notifications/*` | giữ boundary hiện tại đến B7; không trộn vào request split |
| Platform | `Program`, middleware, design-time | UoW, contexts, SQL/file helpers, configuration | EF contexts/migrations | `Program.cs` và flat service folder chỉ migrate khi module registration đã rõ |

Generated migration designer/snapshot là schema history, không được phân loại là rác từ line count.

## 3. RBAC/reconciliation characterization đã khóa

Current contract:

- `CanonicalRbac.Personas` gồm `Employee`, `Manager`, `Dev` — tổng cộng 3 persona phẳng;
- `DepartmentApprover` và `ProcurementAdmin` là alias đọc hiểu về `Manager`;
- `LegacyProcurementAdminGroupId` giữ ID cũ để nhận diện dữ liệu lịch sử, nhưng không nằm trong
  `Personas` hoặc permission matrix canonical;
- QA account `Procurement` hiện được map vào `CanonicalRbac.ProcurementAdmin.GroupId`, tức Manager.

Slice B0R hiện đã khóa:

1. QA diagnostic derive số persona từ `CanonicalRbac.Personas.Count`;
2. unit test assert `LegacyProcurementAdminGroupId` không thuộc `Personas`;
3. non-vacuous unit test tạo legacy memberships, rồi chứng minh record được remap/soft-delete đúng;
4. LocalDB snapshot assert active canonical group count bằng `Personas.Count`;
5. LocalDB snapshot assert không có active legacy Procurement group hoặc membership sau seed/reseed/reset.

Các assertion này chỉ khóa reconciliation hiện tại; không tạo/xóa role, không đổi permission và không
chạy data repair thật.

## 4. Cleanup ledger

### `DELETED` — B1a-1 hoàn tất 2026-08-04

| Candidate | Evidence trước delete | Kết quả |
|---|---|---|
| `UpdateOrderLegacyAsync` | declaration duy nhất trong `VPPRequestService` | deleted; request service/controller focused pass |
| `CancelOrderLegacyAsync` | declaration duy nhất | deleted; cancellation/lifecycle focused pass |
| `ApproveAdditionalOrderLegacyAsync` | declaration duy nhất | deleted; supplement approval/four-eyes focused pass |
| `RejectAdditionalOrderLegacyAsync` | declaration duy nhất | deleted; supplement reject/audit focused pass |
| `GetCurrentPeriodInfoLegacyAsync` | declaration duy nhất | deleted; period info/policy focused pass |
| `Application/Helpers/ObjectHelpers.cs` | repo-wide usage chỉ declaration | file deleted; 6 extension handle còn 0 |
| `Application/Helpers/PasswordHelpers.cs` | empty class, usage chỉ declaration | file deleted; type handle còn 0 |

### `KEEP / CHARACTERIZE FIRST`

| Boundary | Consumer thật | Quyết định |
|---|---|---|
| `BaseServices` / `IBaseServices` | `VPPRequestService`, DI, constructor-heavy tests | B1a-2 riêng; khóa construction/UoW/DI trước khi bỏ inheritance |
| `GenericRepository<T>` / `IGenericRepository<T>` | `PermissionController`, `BaseGenericController`, Library/tests | không xóa chung; retire theo typed module consumer ledger |
| `BaseGenericController` | `LibraryController`, `VPPRequestController` | migrate-on-touch; không mass rewrite controller |
| migration designer/snapshot | EF schema history | giữ nguyên; chỉ thay qua DB-safety workflow |
| `VppColumn`/wire DTO legacy spelling | public JSON contract | giữ cho đến compatibility task có manifest |
| `prices.txt` + private seed cluster | không có runtime caller đã xác nhận, nhưng liên quan reference seed lịch sử | audit/xóa riêng ở B1b; không trộn vào dead-method slice |

`VPPRequestService` hiện không đọc member kế thừa `_unitOfWork`, `Claims`, `JiraIssue` hoặc `WriteLog`,
nhưng constructor base vẫn tạo một UoW phụ. Đây là lý do B1a-2 có giá trị, đồng thời là lý do không xóa
thẳng khi chưa có DI/transaction characterization.

### Generic repository/controller consumer ledger canonical

| Member/boundary | Production consumer hiện tại | Quyết định |
|---|---|---|
| `AddAsync` | generic create của `LibraryController` | giữ đến B3 typed cutover |
| `UpdateAsync` | generic PUT/PATCH của Library; permission mapping update | không xóa chung; tách theo consumer |
| `ReadAsync` | Library; `VPPRequestController` đọc `VppCategory`; `PermissionController` | còn consumer thật |
| `GetByIdAsync` | Library và Permission | còn consumer thật |
| `UpdateRangeAsync` | không có production caller | candidate thu hẹp interface ở B3, không trộn B1a-1 |
| `DeleteAsync` | chỉ nằm sau protected generic delete; không có production callsite trực tiếp | characterize generic status/consumer trước khi retire |
| `BaseGenericController` | `LibraryController`, `VPPRequestController` | giữ; migrate-on-touch theo module |

Frontend hiện vẫn dùng generic Library cho lookup/category/supplier/department CRUD và PATCH/DELETE
`supplier-product-mappings`. Typed catalog item đã dùng đủ create/update/status/delete; Price List dùng
create/update/archive/hard-delete/default/publish/expire/clone. Các endpoint pricing không có frontend
caller vẫn được xem là public/hidden contract cho đến consumer audit ở B3, không được xóa từ search FE.

### B1a-1 execution record — COMPLETE 2026-08-04

Execution base: `693cb58c`. Repo-wide search loại `bin/obj` cho thấy mỗi handle dưới đây chỉ còn
declaration; hai helper không có reflection/fully-qualified caller và SDK dùng default compile items nên
không sửa csproj.

| Xóa trên execution base | Boundary lịch sử trước delete |
|---|---|
| `UpdateOrderLegacyAsync` | `VPPRequestService.cs:902-954` |
| `CancelOrderLegacyAsync` | `VPPRequestService.cs:1269-1270` |
| `ApproveAdditionalOrderLegacyAsync` | `VPPRequestService.cs:1862-1897` |
| `RejectAdditionalOrderLegacyAsync` | `VPPRequestService.cs:1912-1947` |
| `GetCurrentPeriodInfoLegacyAsync` | `VPPRequestService.cs:2046-2085` |
| `ObjectHelpers.cs` | xóa toàn file; 6 extension method đều declaration-only |
| `PasswordHelpers.cs` | xóa toàn file rỗng |

B1a-1 không mở rộng sang `TransitionStatus`, `GetCurrentAndPreviousPeriod`, `IsDeadlinePassed`,
`OrderStateMachine` hoặc reflection test. Audit B1a-1b bên dưới xác nhận closure chính xác; tách commit
giữ rollback/review rõ và tránh xóa public type opportunistic.

Focused gate trước full verify:

```powershell
$b1aFocusedFilter = "FullyQualifiedName~VPPRequestLifecycleTests|" +
    "FullyQualifiedName~VPPRequestServiceTests|" +
    "FullyQualifiedName~CreateOrderRaceConditionTests|" +
    "FullyQualifiedName~VPPRequestControllerTests|" +
    "FullyQualifiedName~PeriodCalculatorTests"
dotnet test tests/Backend.UnitTests/gtas_vpp_be.Tests.csproj -c Release --filter $b1aFocusedFilter
./scripts/gtas.cmd verify -Scope backend
```

Kết quả thực thi:

- post-delete repo-wide search: 0 handle cho 5 method legacy, 2 helper type và 6 extension method;
- focused lifecycle/request/controller/period gate: `119/119`;
- full backend verify: build `0 warning/error`, unit `525/525`, integration mặc định `14 pass/6 skip`,
  EF zero-delta, format, vulnerability và Gitleaks PASS;
- `TransitionStatus`, `GetCurrentAndPreviousPeriod`, `IsDeadlinePassed` và closure liên quan được giữ cho
  B1a-1b audit; không mở rộng scope hoặc sửa reflection test trong commit này.

Rollback boundary: một commit chỉ xóa đúng 5 method + 2 file; không DTO/route/policy/schema/migration.

### B1a-1b execution card — AUDITED / NOT OPEN

Audit read-only trên HEAD `dd171412` xác nhận behavior-preserving boundary nhỏ nhất:

| Scope nếu được mở | Evidence consumer |
|---|---|
| Xóa private `IsDeadlinePassed`, `TransitionStatus`, `GetCurrentAndPreviousPeriod` khỏi `VPPRequestService` | không còn production caller sau B1a-1 |
| Xóa `Application/Domain/OrderStateMachine.cs` (`OrderStateMachine`, `OrderAction`) | chỉ còn `TransitionStatus` và test riêng |
| Xóa `OrderStateMachineTests.cs` | chỉ test production type đã hết caller |
| Xóa 2 test `IsDeadlinePassed_*` và reflection helper `InvokeIsDeadlinePassed` trong `VPPRequestServiceTests` | reflection consumer duy nhất của private wrapper |

Không mở rộng sang public `PeriodCalculator.IsDeadlinePassed`: sau wrapper cleanup method này chỉ còn
domain test, nhưng phải audit cùng các convenience API công khai khác thay vì xóa opportunistic. Active
workflow đã dùng trực tiếp current/previous, persisted period boundary và status guard của từng use case.

Gate đề xuất: `VPPRequestServiceTests`, `VPPRequestLifecycleTests`, `CreateOrderRaceConditionTests`,
`PeriodCalculatorTests`, `VppPeriodPolicyTests`, sau đó full backend verify. Rủi ro còn lại là binary
consumer ngoài repo của public `OrderStateMachine`/`OrderAction`; repository không publish Application
như package và không có evidence consumer này.

Capacity gate: live quota probe tiếp tục `404`; measurement script không có trong `.ai-harness/bin`;
cache aggregate 2026-07-29 (`11/16` coverage) đã stale. Vì vậy card này chỉ ở trạng thái audit, chưa được
phép implementation hoặc gán forecast chính xác.

## 5. Contract và documentation drift

| Drift | Phân loại | Hướng xử lý |
|---|---|---|
| Historical `SQLController`/Newtonsoft residual | resolved stale docs | đã gỡ khỏi module map; HTTP surface hiện hành được khóa bằng manifest MVC |
| QA fixture error text hardcode “four personas” | resolved stale diagnostic | message đã derive từ canonical persona count, không đổi seed behavior |
| Raw English period action reason trên Shared DTO | presentation coupling on wire | B0R ghi manifest; compatibility slice sau đó thêm reason code typed và FE localization, giữ message cũ trong deprecation window |
| Một số controller/service vẫn flat và rất lớn | readability/ownership debt | tách theo use case ở B2–B7, không theo số dòng máy móc |

Không nên chỉ dịch trực tiếp raw backend message sang tiếng Việt: API có thể còn consumer tiếng Anh và
string không phải contract ổn định để UI branch. Phương án bền vững là reason code typed + resource phía
frontend, triển khai additive trước rồi mới retire message khi consumer ledger bằng 0.

### Behavior characterization đã thêm

- Reports: cả 5 endpoint khóa missing-identity `401`, scope-denied/invalid-scope `403`, mapping
  `own/department/all`, insight rate-limit, summary/insight composition và CSV/XLSX/PDF file contract.
- Request reads: detail/history/PDF/XLSX khóa cùng-company resource scope; detail khóa ba nhánh
  owner/department/all; pending supplement khóa decision permission và claim-derived company/department.
- Supplement decisions: approve/reject khóa missing `UserID`, cross-company `403` và service-level
  department/company scope cho cả hai quyết định.
- Error/JSON: Shared wire manifest/serialization giữ shape; ProblemDetails khóa exact property set và
  mapping 400/403/404/409/422/500, gồm argument, business và EF concurrency exception.

Controller unit test gọi action trực tiếp nên không chạy authorization middleware. HTTP manifest khóa
outer policy metadata; runtime unauthenticated-policy integration qua host được hoãn đến auth-host/B6,
không dựng `WebApplicationFactory` chỉ cho B1a-1.

### Hai authorization decision đã triển khai

| ID | Status | Contract đã chốt | Ratchet evidence |
|---|---|---|---|
| B0R-D1 | `OWNER APPROVED A / IMPLEMENTED 2026-08-04` | `order-filter-values?scope=pending` và pending grid cùng chấp nhận `REQUEST_APPROVE OR REQUEST_REJECT`; quyền `REQUEST_VIEW_ALL` vẫn chỉ quyết định data scope | approve-only/reject-only đều `200`; thiếu cả hai quyền trả `403`; service nhận đúng company/department và `canViewAllDepartments=false` |
| B0R-D2 | `OWNER APPROVED A / IMPLEMENTED 2026-08-04` | history dùng class-level authenticated + `CanViewOrderAsync`, đồng nhất với detail/PDF/XLSX | manifest đổi thành `AUTHENTICATED`; department/company scope không cần `VIEW_OWN`; same-company thiếu resource permission và other-company vẫn `403` |

Security backlog cho B4: action history hiện tải full revisions trước resource authorization; cần tách seed/header
authorization trước query đầy đủ và khóa invariant mọi revision trong cùng series có cùng resource scope. B0R
không âm thầm đổi semantics `404`/`403` hoặc schema/index.

## 6. B0R deliverables và tiến độ

- [x] MVC manifest cho `112` endpoint: route + HTTP verb + effective authorization policy.
- [x] Representative status/error/JSON/export characterization cho Reports, request resource scope,
  supplement decisions, Shared wire shape và ProblemDetails; generic Catalog/Pricing controller parity
  được hoãn đến trước B3 vì không liên quan B1a-1.
- [x] RBAC legacy reconciliation assertions ở mục 3.
- [x] Generic endpoint/repository consumer ledger ở mục 4.
- [x] Module map bỏ residual `SQLController`; reading guide chi tiết còn đồng bộ ở wave tài liệu.
- [x] Final full backend verify PASS: agent setup `63/63`, build sạch, unit `525/525`, default
  integration `14/6 skip`, EF zero-delta, format, vulnerability và Gitleaks; LocalDB `20/20` giữ từ
  HTTP/RBAC slice vì behavior slice chỉ thêm unit/docs.
- [x] Owner chốt B0R-D1/B0R-D2 phương án A và UI final acceptance; authorization ratchet pass, B1a-1 mở.
- [x] B1a-1 xóa đúng 5 legacy method + 2 helper file; focused `119/119` và full backend verify PASS.

## 7. Boundary an toàn

- B0R chỉ đổi hai authorization behavior đã được owner duyệt; không đổi Shared wire shape, database hoặc
  nghiệp vụ đặt hàng. B1a-1 đã hoàn tất như behavior-preserving dead-code slice riêng.
- Ngoài B0R-D1/D2 đã được owner duyệt, không đổi Shared wire shape, route, status code, database schema
  hoặc migration trong B0R.
- Không stage/overwrite các dirty file ngoài scope được liệt kê trong continuation record.
- Nếu B1 phát hiện schema/data change thật, dừng backend refactor record và chuyển sang DB-safety
  execution record có recovery/backup/forward-correction phù hợp.
