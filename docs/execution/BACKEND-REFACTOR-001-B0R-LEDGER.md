# BACKEND-REFACTOR-001 B0R — Contract, ownership và cleanup ledger

- Status: `PREPARED READ-ONLY — PRODUCTION B0R/B1 CHỜ OWNER UI FINAL ACCEPTANCE`
- Branch/HEAD khảo sát: `codex/ai-agent-foundation` @ `5b060c4d`
- Khảo sát ngày: `2026-08-04`
- Authority: [`BACKEND-REFACTOR-001.md`](./BACKEND-REFACTOR-001.md),
  [`ARCH-001-MODULE-MAP.md`](../architecture/ARCH-001-MODULE-MAP.md),
  `src/Backend/AGENTS.md`

## 0. Bản một ánh nhìn

| Mục | Kết luận hiện tại |
|---|---|
| Baseline | Preflight PASS; backend unit `473/473`; integration mặc định `14 pass / 6 skip`; LocalDB disposable `20/20`; full backend verify PASS; EF không pending model |
| Kiến trúc | Giữ `API → Application → Domain`; Shared là wire contract; không tạo project/microservice mới |
| Xóa an toàn đầu tiên | 5 private `*LegacyAsync` trong `VPPRequestService`, `ObjectHelpers.cs`, `PasswordHelpers.cs` có usage chỉ là declaration |
| Chưa được xóa | `BaseServices`, `IBaseServices`, `GenericRepository`, `IGenericRepository`, `BaseGenericController` còn consumer thật |
| RBAC drift | Canonical hiện có 3 persona; Procurement legacy alias về Manager, nhưng QA fixture còn diagnostic “four personas” và chưa assert trực tiếp legacy group/membership inactive |
| Documentation drift | `ARCH-001-MODULE-MAP.md` còn residual `SQLController`, trong khi current tree không còn controller/callsite này |
| Localization debt | Backend trả raw English `CanCreateOrderReason`/`CanCreateAdditionalReason`; UI tiếng Việt có thể lộ English như board Order Create |
| Bước production đầu tiên | Sau owner UI acceptance: B0R thêm characterization/manifest, rồi B1a-1 xóa dead code nhỏ; không trộn `BaseServices` hoặc localization cutover |

## 1. Baseline đã kiểm chứng

| Gate | Kết quả trên HEAD khảo sát |
|---|---|
| `./scripts/gtas.cmd preflight -Scope backend` | PASS |
| `./scripts/gtas.cmd test-backend` | PASS — `473/473` |
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

## 3. RBAC/reconciliation characterization còn thiếu

Current contract:

- `CanonicalRbac.Personas` gồm `Employee`, `Manager`, `Dev` — tổng cộng 3 persona phẳng;
- `DepartmentApprover` và `ProcurementAdmin` là alias đọc hiểu về `Manager`;
- `LegacyProcurementAdminGroupId` giữ ID cũ để nhận diện dữ liệu lịch sử, nhưng không nằm trong
  `Personas` hoặc permission matrix canonical;
- QA account `Procurement` hiện được map vào `CanonicalRbac.ProcurementAdmin.GroupId`, tức Manager.

Gap cần khóa trong B0R:

1. sửa diagnostic `QA fixture requires the four reconciled canonical flat personas.` thành message
   dựa trên `CanonicalRbac.Personas.Count` hoặc wording không hardcode số;
2. unit test assert `LegacyProcurementAdminGroupId` không thuộc `Personas`;
3. LocalDB characterization assert active canonical group count bằng `Personas.Count`;
4. LocalDB characterization assert không có active `PermissionGroup` mang legacy Procurement ID;
5. LocalDB characterization assert không có active `UserGroupMembership` trỏ tới legacy Procurement ID.

Các assertion này chỉ khóa reconciliation hiện tại; không tạo/xóa role, không đổi permission và không
chạy data repair thật.

## 4. Cleanup ledger

### `DELETE_CANDIDATE` — B1a-1

| Candidate | Evidence hiện tại | Gate trước delete |
|---|---|---|
| `UpdateOrderLegacyAsync` | declaration duy nhất trong `VPPRequestService` | request service/controller focused + full backend |
| `CancelOrderLegacyAsync` | declaration duy nhất | cancellation/lifecycle focused + full backend |
| `ApproveAdditionalOrderLegacyAsync` | declaration duy nhất | supplement approval/four-eyes focused + full backend |
| `RejectAdditionalOrderLegacyAsync` | declaration duy nhất | supplement reject/audit focused + full backend |
| `GetCurrentPeriodInfoLegacyAsync` | declaration duy nhất | period info/policy focused + full backend |
| `Application/Helpers/ObjectHelpers.cs` | repo-wide usage chỉ declaration | build, unit, integration, format |
| `Application/Helpers/PasswordHelpers.cs` | empty class, usage chỉ declaration | build, unit, integration, format |

### `KEEP / CHARACTERIZE FIRST`

| Boundary | Consumer thật | Quyết định |
|---|---|---|
| `BaseServices` / `IBaseServices` | `VPPRequestService`, DI, constructor-heavy tests | B1a-2 riêng; khóa construction/UoW/DI trước khi bỏ inheritance |
| `GenericRepository<T>` / `IGenericRepository<T>` | `PermissionController`, `BaseGenericController`, Library/tests | không xóa chung; retire theo typed module consumer ledger |
| `BaseGenericController` | `LibraryController`, `VPPRequestController` | migrate-on-touch; không mass rewrite controller |
| migration designer/snapshot | EF schema history | giữ nguyên; chỉ thay qua DB-safety workflow |
| `VppColumn`/wire DTO legacy spelling | public JSON contract | giữ cho đến compatibility task có manifest |

`VPPRequestService` hiện không đọc member kế thừa `_unitOfWork`, `Claims`, `JiraIssue` hoặc `WriteLog`,
nhưng constructor base vẫn tạo một UoW phụ. Đây là lý do B1a-2 có giá trị, đồng thời là lý do không xóa
thẳng khi chưa có DI/transaction characterization.

## 5. Contract và documentation drift

| Drift | Phân loại | Hướng xử lý |
|---|---|---|
| `ARCH-001-MODULE-MAP.md` còn nhắc `SQLController`/Newtonsoft nhưng current tree không còn file/callsite | stale docs | B0R cập nhật module map sau repo-wide search |
| QA fixture error text hardcode “four personas” | stale diagnostic | sửa cùng RBAC characterization, không đổi seed behavior |
| Raw English period action reason trên Shared DTO | presentation coupling on wire | B0R ghi manifest; compatibility slice sau đó thêm reason code typed và FE localization, giữ message cũ trong deprecation window |
| Một số controller/service vẫn flat và rất lớn | readability/ownership debt | tách theo use case ở B2–B7, không theo số dòng máy móc |

Không nên chỉ dịch trực tiếp raw backend message sang tiếng Việt: API có thể còn consumer tiếng Anh và
string không phải contract ổn định để UI branch. Phương án bền vững là reason code typed + resource phía
frontend, triển khai additive trước rồi mới retire message khi consumer ledger bằng 0.

## 6. B0R deliverables sau owner UI acceptance

1. route + HTTP verb + authorization policy manifest cho mọi controller;
2. representative status/error/JSON/export characterization;
3. RBAC legacy reconciliation assertions ở mục 3;
4. generic endpoint/repository consumer ledger;
5. module/file reading map cập nhật và xóa documentation drift;
6. chạy unit `473+`, integration default + LocalDB `20/20`, EF zero-delta và full backend verify;
7. chỉ sau B0R PASS mới mở B1a-1.

## 7. Boundary an toàn

- Không sửa production backend trước owner UI final acceptance.
- Không đổi Shared wire shape, route, policy, status code, database schema hoặc migration trong B0R.
- Không stage/overwrite các dirty file ngoài scope được liệt kê trong continuation record.
- Nếu B1 phát hiện schema/data change thật, dừng backend refactor record và chuyển sang DB-safety
  execution record có recovery/backup/forward-correction phù hợp.
