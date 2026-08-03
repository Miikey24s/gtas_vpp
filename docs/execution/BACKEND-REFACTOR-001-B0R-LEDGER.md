# BACKEND-REFACTOR-001 B0R — Contract, ownership và cleanup ledger

- Status: `CHARACTERIZATION IN PROGRESS — HTTP/RBAC + FULL VERIFY PASS; PRODUCTION B1 CHỜ OWNER UI FINAL ACCEPTANCE`
- Slice base: `codex/ai-agent-foundation` @ `69397af9`
- Khảo sát ngày: `2026-08-04`
- Authority: [`BACKEND-REFACTOR-001.md`](./BACKEND-REFACTOR-001.md),
  [`ARCH-001-MODULE-MAP.md`](../architecture/ARCH-001-MODULE-MAP.md),
  `src/Backend/AGENTS.md`

## 0. Bản một ánh nhìn

| Mục | Kết luận hiện tại |
|---|---|
| Baseline | Focused HTTP/RBAC `23/23`; backend unit `476/476`; integration mặc định `14 pass / 6 skip`; LocalDB disposable `20/20`; full backend verify PASS |
| Kiến trúc | Giữ `API → Application → Domain`; Shared là wire contract; không tạo project/microservice mới |
| Xóa an toàn đầu tiên | 5 private `*LegacyAsync` trong `VPPRequestService`, `ObjectHelpers.cs`, `PasswordHelpers.cs` có usage chỉ là declaration |
| Chưa được xóa | `BaseServices`, `IBaseServices`, `GenericRepository`, `IGenericRepository`, `BaseGenericController` còn consumer thật |
| RBAC contract | Canonical có 3 persona; Procurement legacy alias về Manager; test đã khóa legacy ID không thuộc persona và không còn group/membership active sau reconciliation |
| HTTP contract | Manifest MVC khóa `112` endpoint theo verb + route + effective authorization; không khóa tên/controller nội bộ để vẫn cho phép refactor |
| Documentation drift | Residual `SQLController` đã được gỡ khỏi module map sau repo-wide search xác nhận không còn file/callsite |
| Localization debt | Backend trả raw English `CanCreateOrderReason`/`CanCreateAdditionalReason`; UI tiếng Việt có thể lộ English như board Order Create |
| Bước production đầu tiên | Sau owner UI acceptance và B0R full PASS: B1a-1 xóa dead code nhỏ; không trộn `BaseServices` hoặc localization cutover |

## 1. Baseline đã kiểm chứng

| Gate | Kết quả trên HEAD khảo sát |
|---|---|
| `./scripts/gtas.cmd preflight -Scope backend` | PASS |
| Backend unit trong full verify | PASS — `476/476` |
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
| Historical `SQLController`/Newtonsoft residual | resolved stale docs | đã gỡ khỏi module map; HTTP surface hiện hành được khóa bằng manifest MVC |
| QA fixture error text hardcode “four personas” | resolved stale diagnostic | message đã derive từ canonical persona count, không đổi seed behavior |
| Raw English period action reason trên Shared DTO | presentation coupling on wire | B0R ghi manifest; compatibility slice sau đó thêm reason code typed và FE localization, giữ message cũ trong deprecation window |
| Một số controller/service vẫn flat và rất lớn | readability/ownership debt | tách theo use case ở B2–B7, không theo số dòng máy móc |

Không nên chỉ dịch trực tiếp raw backend message sang tiếng Việt: API có thể còn consumer tiếng Anh và
string không phải contract ổn định để UI branch. Phương án bền vững là reason code typed + resource phía
frontend, triển khai additive trước rồi mới retire message khi consumer ledger bằng 0.

## 6. B0R deliverables và tiến độ

- [x] MVC manifest cho `112` endpoint: route + HTTP verb + effective authorization policy.
- [ ] Representative status/error/JSON/export characterization còn phải đối chiếu coverage hiện hữu.
- [x] RBAC legacy reconciliation assertions ở mục 3.
- [x] Generic endpoint/repository consumer ledger ở mục 4.
- [x] Module map bỏ residual `SQLController`; reading guide chi tiết còn đồng bộ ở wave tài liệu.
- [x] Full backend verify PASS; focused `23/23`, unit `476/476`, default integration `14/6 skip`, LocalDB `20/20`, EF zero-delta.
- [ ] Chỉ sau B0R PASS và owner UI acceptance mới mở B1a-1.

## 7. Boundary an toàn

- Không move/xóa hoặc đổi runtime behavior production backend trước owner UI final acceptance;
  test, execution record và comment-only cleanup được phép chuẩn bị an toàn.
- Không đổi Shared wire shape, route, policy, status code, database schema hoặc migration trong B0R.
- Không stage/overwrite các dirty file ngoài scope được liệt kê trong continuation record.
- Nếu B1 phát hiện schema/data change thật, dừng backend refactor record và chuyển sang DB-safety
  execution record có recovery/backup/forward-correction phù hợp.
