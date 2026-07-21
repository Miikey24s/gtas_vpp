# HANDOFF-CLIPROXY — GTAS VPP

> Tài liệu chuyển ngữ cảnh khi đổi `model_provider` sang CLIProxyAPI.
> Snapshot được lập tại workspace `D:\WORK\gtas_vpp` ngày 2026-07-16
> (Asia/Ho_Chi_Minh). File này là handoff, không phải execution record thay
> thế cho `docs/execution/LEAN-05.md`.

## 0. Cách dùng handoff này

Đọc theo thứ tự:

1. `AGENTS.md` ở thư mục gốc và các `.codexrules`/Copilot instructions liên quan.
2. File này.
3. `docs/planning/06-LEAN-A-PLUS-EXECUTION-PLAN.md` (cutline duy nhất),
   `docs/planning/03-DECISIONS-REQUIRED.md` và các execution record đã có.
4. Chỉ đọc source/card cần cho package đang làm; không lặp lại audit toàn
   repository nếu evidence hiện tại không mâu thuẫn.

Không dùng file này để suy ra rằng production đã được cập nhật. Tất cả bằng
chứng production, LocalDB và test phải được phân biệt rõ như bên dưới.

## 1. Mục tiêu tổng thể

Mục tiêu là hoàn thiện GTAS VPP thành một sản phẩm luận văn tốt nghiệp A+,
phù hợp doanh nghiệp Việt Nam nhưng có giao diện riêng, dễ trình diễn và có
thể bảo trì/mở rộng. Cutline được chốt là `docs/planning/06-LEAN-A-PLUS-EXECUTION-PLAN.md`,
version `2.0-lean`, deadline bảo vệ `2026-08-15`.

Phạm vi retained phải được thực thi liên tục theo một Goal duy nhất:

`LEAN-01 → LEAN-02 → LEAN-03 → LEAN-04 → LEAN-05 → LEAN-06 → LEAN-07 → LEAN-08 → LEAN-09`.

Mục tiêu kỹ thuật/nghiệp vụ:

- App-owned ASP.NET Core Identity, RBAC server-authoritative và audit đầy đủ.
- Một công ty trong v1, bốn persona phẳng, không quay lại DB user công ty.
- Kỳ Việt Nam từ 00:00 ngày 05 đến trước 00:00 ngày 05 tháng kế tiếp
  (tương đương hết ngày 04), request thường/supplement có lineage và
  concurrency đúng.
- Cuối kỳ gom giỏ hàng toàn công ty, chọn một nhà cung cấp chính, chụp giá/VAT
  bất biến, phân bổ lại về user/phòng ban và reconcile được.
- Radzen Blazor vẫn là nền tảng; tạo personality bằng design system, IA,
  tiếng Việt, trạng thái/empty/error/loading và responsive polish; không viết
  lại sang React/Next hay ngôn ngữ khác.
- Báo cáo/KPI/Excel, inbox bền vững và email sandbox có ích trong demo.
- Build/test/migration/recovery/Word/diagrams khớp source thật. Không để luận
  văn dẫn dắt code.

## 2. Yêu cầu và quyết định người dùng đã chốt

### 2.1 Yêu cầu trao đổi

- Ban đầu yêu cầu là audit toàn repository, nghiên cứu, lập master plan và
  chưa sửa source. Sau đó người dùng chuyển sang cho phép thực thi plan bằng
  Target/Goal.
- Người dùng đổi chính sách từ “mỗi Goal một task rồi dừng” sang “một Goal
  chạy toàn bộ retained plan rồi mới dừng”; không chờ thêm tin nhắn
  `continue` giữa các package.
- Ưu tiên scope A+ core-first nhưng polished, full-time, deadline một tháng;
  local demo là bắt buộc, server chỉ khi security/deploy/restore green.
- Giữ các tool/dependency hữu ích đã cài; không tự xóa Playwright hoặc các
  runtime phục vụ test. Chromium hiện có tại
  `C:\Users\MIIKEY\AppData\Local\ms-playwright\chromium-1228\chrome-win64\chrome.exe`.
  Có thể cài thêm dependency cần thiết trong phạm vi an toàn, nhưng không tự
  thực hiện mutation production.
- AI features được nghiên cứu trong plan nhưng không được làm core phụ thuộc
  AI. `AI-001` deferred và feature flag phải off.
- Người dùng chấp nhận residual risk của các key lịch sử; không được vì áp
  lực quota mà tuyên bố key đã revoke khi chưa có evidence.

### 2.2 D-001 đến D-012 (nguồn chuẩn: `03-DECISIONS-REQUIRED.md`)

| ID | Quyết định đã chốt |
|---|---|
| D-001 | App-owned ASP.NET Identity. `GTAS_MENU` chỉ là schema/reference local để tương thích lịch sử; không kết nối lại DB user công ty, không giữ adapter/password legacy. |
| D-002 | Single-company v1. Giữ key/migration đủ sạch để mở rộng sau, nhưng không over-engineer multi-tenant. |
| D-003 | Mỗi user chỉ có một request thường trong mỗi kỳ. Hủy giữ lịch sử; thay đổi/hủy tạo revision có liên kết. Cuối kỳ gom toàn công ty rồi phân bổ lại về request/phòng ban. Không giả vờ có inventory/receipt nếu chưa làm. |
| D-004 | Bốn persona phẳng: Employee, Department Approver, Procurement/Period Admin, System Admin. Một active group và một primary department; self-approval/four-eyes được chặn. System Admin không tự có quyền procurement. |
| D-005 | Supplement bắt buộc có request gốc và reason; một pending tại một thời điểm; `MaxApproved=1` theo user/base/period mặc định; rejected/cancelled không tiêu quota approved nhưng mọi attempt audit; `MaxAttempts=6`; có approval grace deadline. |
| D-006 | Một `PrimarySupplier` cho toàn bộ basket của công ty/kỳ theo effective settlement revision để hưởng giá sỉ. Item exception chỉ khi thiếu/không có giá, phải có permission và actor/time/reason; không fallback im lặng. Đổi NCC phải là correction revision bất biến. |
| D-007 | Đơn giá lưu net chưa VAT; VAT lấy theo price-book/contract; settlement snapshot net, rate và gross. |
| D-008 | Self-register → `PendingApproval` zero privilege → admin map employee/department/group → activate. Username/email unique; employee code unique nếu có; email confirm/reset khi cấu hình được, nếu không dùng admin fallback có audit; rate-limit và anti-enumeration bắt buộc. |
| D-009 | Rebrand độc lập thành GTAS VPP và anonymize seed/screenshot/Word/evidence; không đưa branding công ty/PII vào sản phẩm luận văn. |
| D-010 | In-app là source of truth; bắt buộc CSV/Excel và email. PDF/Teams/Zalo deferred. Email dùng outbox/retry/sandbox, không bắt buộc provider thật trong thesis. |
| D-011 | Có DigitalOcean và trước đây đã chạy `MigrateAndSeed`; credential legacy chỉ demo; user là owner. Production mutation vẫn cần explicit authority/preflight/backup. |
| D-012 | A+ core-first + polished UI, deadline `2026-08-15`, local-first; server demo chỉ khi green. Cắt P2/P3 trước, không cắt auth, data/settlement correctness, core UI, Excel, inbox/email sandbox. |

### 2.3 Goal/Target history và trạng thái khi handoff

Trong cuộc trò chuyện đã có các Goal/Target theo card (BASE-001, các package
foundation, ARCH-001, SEC-001 và các bước tiếp nối). Những package foundation
đã có execution record/commit và không được làm lại. Người dùng sau đó yêu cầu
gộp toàn bộ công việc còn lại vào một Goal liên tục vì sợ mất quota/context.

Tại thời điểm snapshot này, gọi `get_goal({})` ở root trả về:

```json
{"goal":null,"remainingTokens":null,"completionBudgetReport":null}
```

Điều này có thể do đổi provider/phiên mới hoặc Goal cũ đã bị xóa. Không tự
đoán một goal ID cũ. Agent tiếp quản phải tạo **một Goal duy nhất** (không
token budget nếu người dùng không yêu cầu) với objective tương đương:

> Execute `docs/planning/06-LEAN-A-PLUS-EXECUTION-PLAN.md` continuously from
> the current worktree: finish LEAN-05, then LEAN-06, LEAN-07, LEAN-08 and
> LEAN-09 in dependency order; preserve D-001..D-012, protected files and
> accepted security risks; do not stop between retained packages except for a
> true external/undiscoverable decision blocker.

Nếu `get_goal` trong phiên mới đã có Goal đúng objective, không tạo Goal thứ
hai; chỉ tiếp tục Goal đó.

## 3. Việc đã hoàn thành

### 3.1 Foundation và security đã đóng (không làm lại)

Các execution record tương ứng nằm trong `docs/execution/`:

- `BASE-001` — baseline/audit và preservation boundary.
- `SEC-002` — bootstrap mode an toàn, không seed account.
- `ENV-001` — bind một database/deployment, config boundary.
- `QA-001` — SQL/browser fixture isolation.
- `ARCH-001` — module boundary/architecture guard.
- `SEC-001` — incident containment, đóng với accepted residual risk.

Các commit foundation chính:

```text
5f22ad3  docs: freeze BASE-001 project baseline
4a2d0b2  fix(database): SEC-002 separate safe bootstrap modes
f8c5944  fix(environment): ENV-001 bind one database per deployment
1e25cca  test(qa): QA-001 isolate SQL and browser fixtures
3744298  refactor(architecture): enforce ARCH-001 module boundaries
3b28072  docs(security): close LEAN-01 with production evidence
```

`LEAN-01`/`SEC-001` đã có recovery release và production evidence. Có 12
demo account/membership bị khóa/inactive, secrets DB/JWT/legacy encryption
đã rotate, paired backup đã verify, health public green. **Tám historical
Google API-key alerts vẫn mở theo waiver của owner; không nói chúng đã
revoked/resolved.** Không có active app owner production được provision trong
LEAN-02/03.

### 3.2 LEAN-02, LEAN-03, LEAN-04

- `9832493` — trusted app-owned Identity, canonical flat RBAC, session
  invalidation, guarded owner bootstrap; không phụ thuộc company user DB.
- `9e50c05` — self-registration/PendingApproval, activation/recovery/password
  lifecycle, rate limit/anti-enumeration và audited fallback.
- `ee77b51` (HEAD) — GTAS VPP independent Vietnamese-first Radzen shell,
  typed navigation/error/async state, responsive/accessibility foundation.

Evidence cuối các package đã ghi trong `docs/execution/LEAN-02.md`,
`LEAN-03.md`, `LEAN-04.md`: Release build sạch; backend lần lượt 263/263,
290/290, 292/292; frontend 63/63, 75/75, 86/86; LocalDB/UI evidence theo
record; EF/Gitleaks/diff gate sạch tại thời điểm đó.

### 3.3 LEAN-05 hiện đã làm được trong working tree

LEAN-05 chưa commit, nhưng phần lớn implementation đã có:

- Period aggregate/state machine `Open → SubmissionClosed → Pricing → Settled`,
  kỳ 05→04, boundary lưu UTC và API hiển thị Vietnam wall-clock; recovery
  worker tạo/advance kỳ sau downtime.
- Request header/log có `PeriodId`, series/revision/current/supersedes,
  base/supplement lineage, reason, cancellation audit, idempotency, hash và
  rowversion.
- Regular: một user/kỳ kể cả sau cancel; update/cancel là immutable revision,
  history/timeline, stale rowversion, idempotent replay/conflict, deadline và
  settlement guard.
- Cancellation **không giải phóng slot**. Replacement đi qua update/revision
  cùng series; đây là invariant đã test.
- Supplement: phải có base cùng kỳ, reason 5–500, một pending, max approved 3,
  max attempts 6, scope/self-approval/concurrency/idempotency guard.
- Controller/DTO đã dùng envelope rowversion + idempotency cho approve/reject/
  cancel; history endpoint có scope check.
- FE đã sửa permission drift (`RequestCreate`/`RequestUpdateOwn`), hiển thị kỳ,
  deadline, quota, reason và eligibility; có reject/history dialogs,
  replacement/cancel CTA, safe error mapper và Vietnamese resources.
- Migration `20260716063247_AddPeriodRequestRevisionAndSupplementWorkflow`
  có preflight/backfill legacy, period/revision/supplement/log linkage,
  invariant checks, indexes/FK period; `Down` cố ý forward-only và throw.
- Migration agent đã tạo idempotent SQL script 78,520 bytes ở
  `%TEMP%\gtas_vpp_lean05_idempotent.sql`, chạy upgrade trên LocalDB
  `gtas_vpp_lean05_validation` với fixture legacy. Kết quả:
  `PeriodCount=2`, `BrokenLineage=0`, `RestoredCancellation=2`,
  `DuplicateCurrentRegularGroups=0`, migration history ghi nhận.

## 4. Việc còn dở / LEAN-05 chưa được đóng

Thứ tự đóng LEAN-05:

1. Chạy lại UI E2E trên disposable isolated stack với:
   `GTAS_E2E_ISOLATED=1` và
   `GTAS_E2E_MUTATION_OPT_IN=I_UNDERSTAND_THIS_MUTATES_QA_DATA`.
   Lần chạy không có isolation phải fail closed (đó là hành vi đúng); lần
   isolated trước đây mất output vì exec cell biến mất, nên chưa được claim
   PASS. Suite hiện discover 16 tests.
2. Chạy lại final Release build, BE/FE/integration, EF pending-model,
   migration script/current-tree secret scan, protected hashes, `git diff
   --check`.
3. Tạo `docs/execution/LEAN-05.md` ghi invariant, files, migration/backfill,
   rollback/forward-fix, counts và deferred scope.
4. Stage explicit LEAN-05 paths, loại bốn protected files; commit checkpoint
   ví dụ `feat(requests): add period-aware revision and supplement lifecycle`.
5. Cập nhật plan LEAN-05 `DONE`, LEAN-06 `IN_PROGRESS`, rồi tự động chuyển
   ngay sang LEAN-06.

Residual không chặn LEAN-05 nhưng phải ghi lại để harden ở LEAN-08/post-release:

- `VPPRequestService.cs` còn một số private `*LegacyAsync` dead code.
- Migration chưa có self-FK cho `BaseRequestId`, `SupersedesRequestId`,
  `SupersededByRequestId`; chưa có CHECK constraint đầy đủ cho lineage/
  supplement (service + unique indexes đang bảo vệ).
- `VPP00_Period.RowVersion` còn nullable.
- Product picker còn materialize toàn bộ `/products/lookup`; paging/search
  server thuộc CAT-001/LEAN-06.
- Copy-with-diff/WOW-001 chưa làm; chỉ làm nếu flow hiện hữu hoàn tất trong
  tối đa nửa ngày và còn buffer, mặc định deferred.
- Draft localStorage chưa có TTL/privacy hardening hoàn chỉnh; phải bảo đảm
  không leak draft giữa user, có thể harden ở LEAN-08.
- Không có execution note/commit LEAN-05 tại snapshot.

## 5. Danh sách file đã tạo/sửa trong working tree

Danh sách dưới đây là phạm vi hiện tại của LEAN-05, trước khi tạo file handoff:
39 tracked files modified và 16 untracked files mới; bốn file protected được
tách riêng ở mục 9. Các file của package đã commit trước đó nằm trong lịch sử
commit và execution record; dùng `git show --name-status <commit>` nếu cần
full historical inventory.

### 5.1 Backend model/domain/runtime

| File | Trạng thái | Mục đích |
|---|---|---|
| `gtas_vpp_be/gtas_vpp_be.Model/VPP/VPP00_Period.cs` | new | Aggregate kỳ, UTC boundary, Vietnam timezone, state/audit/rowversion. |
| `gtas_vpp_be/gtas_vpp_be.Model/VPP/VppPeriodState.cs` | new | Enum/state machine period. |
| `gtas_vpp_be/gtas_vpp_be.Model/VPP/VPP01_RequestHeader.cs` | modified | Period, series/revision/lineage, supplement/cancel/idempotency/rowversion. |
| `gtas_vpp_be/gtas_vpp_be.Model/VPP/VPP03_Log.cs` | modified | Typed actor/company/action/revision/correlation/reason audit. |
| `gtas_vpp_be/gtas_vpp_be.Model/VPPMigrationDbContext.cs` | modified | EF mapping, lengths/checks/FK/indexes và filtered uniqueness. |
| `gtas_vpp_be/gtas_vpp_be.Service/Domain/PeriodCalculator.cs` | modified | Tính kỳ 05→04, local/UTC boundary đa nền tảng. |
| `gtas_vpp_be/gtas_vpp_be.Service/Domain/VppRequestPolicy.cs` | new | Config deadline/grace/quota/attempt limits và validation. |
| `gtas_vpp_be/gtas_vpp_be.Service/Helpers/Context/VPPContext.cs` | modified | Runtime EF mapping đồng bộ migration context. |
| `gtas_vpp_be/gtas_vpp_be.Service/Services/IVppPeriodService.cs` | new | Contract ensure/get/advance/transition idempotent. |
| `gtas_vpp_be/gtas_vpp_be.Service/Services/VppPeriodService.cs` | new | Tạo kỳ cạnh tranh an toàn, transition/recovery guard. |
| `gtas_vpp_be/gtas_vpp_be.Service/Services/VppPeriodRecoveryWorker.cs` | new | Hosted recovery/advance worker mỗi 300 giây. |
| `gtas_vpp_be/gtas_vpp_be.Service/Services/VPPRequestService.cs` | modified | Core create/update/cancel/history/supplement/approval, scope, concurrency, idempotency, period flags. |
| `gtas_vpp_be/gtas_vpp_be/Controllers/VPPRequestController.cs` | modified | History endpoint; cancel envelope; approve/reject rowversion/idempotency/scope. |
| `gtas_vpp_be/gtas_vpp_be/Program.cs` | modified | DI policy, period service, recovery worker. |
| `gtas_vpp_be/gtas_vpp_be/appsettings.json` | modified | Explicit `VPP` config: day 5, grace 2, quota 3, attempts 6, recovery 300s. |

### 5.2 EF migration

| File | Trạng thái | Mục đích |
|---|---|---|
| `gtas_vpp_be/gtas_vpp_be.Migrations/Migrations/20260716063247_AddPeriodRequestRevisionAndSupplementWorkflow.cs` | new | Forward-only schema/data migration, preflight, legacy backfill, invariant checks, indexes/FK. |
| `gtas_vpp_be/gtas_vpp_be.Migrations/Migrations/20260716063247_AddPeriodRequestRevisionAndSupplementWorkflow.Designer.cs` | new | EF migration metadata. |
| `gtas_vpp_be/gtas_vpp_be.Migrations/Migrations/VPPMigrationDbContextModelSnapshot.cs` | modified | Snapshot model mới. |

### 5.3 Shared DTO/wire contracts

| File | Trạng thái | Mục đích |
|---|---|---|
| `gtas_vpp_be/gtas_vpp_shared/DTOs/Req/VPP/ApproveOrderReqDTO.cs` | new | Rowversion/idempotency cho approve. |
| `gtas_vpp_be/gtas_vpp_shared/DTOs/Req/VPP/VPP_CancelOrderReqDTO.cs` | new | Rowversion/reason/idempotency cho cancel. |
| `gtas_vpp_be/gtas_vpp_shared/DTOs/Req/VPP/RejectOrderReqDTO.cs` | modified | Rowversion/idempotency reject. |
| `gtas_vpp_be/gtas_vpp_shared/DTOs/Req/VPP/VPP01_CreateReqDTO.cs` | modified | Base request, supplement reason, idempotency. |
| `gtas_vpp_be/gtas_vpp_shared/DTOs/Req/VPP/VPP01_UpdateReqDTO.cs` | modified | Supplement reason, rowversion, idempotency. |
| `gtas_vpp_be/gtas_vpp_shared/DTOs/Res/VPP/VPP01_RequestHeaderResDTO.cs` | modified | Period/revision/lineage/supplement/audit/rowversion/CanReplace. |
| `gtas_vpp_be/gtas_vpp_shared/DTOs/Res/VPP/VPP_PeriodInfoResDTO.cs` | modified | State/boundary/base/quota/attempt/eligibility reasons. |
| `gtas_vpp_be/gtas_vpp_shared/DTOs/Res/VPP/VPP_RequestHistoryResDTO.cs` | new | Revisions và typed timeline. |

### 5.4 Frontend

| File | Trạng thái | Mục đích |
|---|---|---|
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/OrderCreateContext.cs` | modified | Wizard state cho base/reason/rowversion. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/OrderCreateStep2.razor` | modified | Reason riêng, validation 5–500. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/OrderCreateStep3.razor` | modified | Hiển thị reason lúc xác nhận. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Page_OrderCreate.razor` | modified | Accessible steps, period/quota/deadline/base panel, submit guard. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Page_OrderCreate.razor.cs` | modified | Eligibility, permission, replacement, payload/idempotency, safe errors. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/BaseOrderTab.cs` | modified | Safe `UiErrorMapper`. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/Tab_AdminApproval.razor` | modified | Per-row loading và approval UI. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/Tab_AdminApproval.razor.cs` | modified | Approval/reject concurrency/idempotency và reason dialog. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/Tab_History.razor` | modified | Action xem lifecycle. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/Tab_History.razor.cs` | modified | History dialog/timeline loader. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/Tab_Orders.razor` | modified | Eligibility/quota/period, replace/history/cancel CTA. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Tabs/Tab_Orders.razor.cs` | modified | Guards, confirmation, rowversion/idempotency và safe errors. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Components/Dialog_RejectSupplement.razor` | new | Reject reason dialog 5–500. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Components/Dialog_RequestHistory.razor` | new | Loading/error/retry/timeline/revision dialog. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Resources/Components.App.resx` | modified | Vietnamese strings kỳ/quota/revision/reason/action. |
| `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Resources/Components.App.en.resx` | modified | English equivalents. |

### 5.5 Tests

| File | Trạng thái | Mục đích |
|---|---|---|
| `gtas_vpp_be.Tests/Architecture/SharedContractSerializationTests.cs` | modified | Expected public JSON shape. |
| `gtas_vpp_be.Tests/Architecture/SharedWireContractManifestTests.cs` | modified | Shared wire manifest hash (`453B8FADAA51AA680BF722AF867265C7BB34453F1BDEC54511C64F632BD5C634`). |
| `gtas_vpp_be.Tests/Domain/VppPeriodPolicyTests.cs` | new | Defaults, limits, 05→05 boundary, grace, legal transitions. |
| `gtas_vpp_be.Tests/Domain/VppPeriodServiceTests.cs` | new | Ensure idempotency, recovery và transition. |
| `gtas_vpp_be.Tests/VPPRequestTests/CreateOrderAfterSettlementTests.cs` | modified | Settled chặn regular/supplement. |
| `gtas_vpp_be.Tests/VPPRequestTests/CreateOrderRaceConditionTests.cs` | modified | Period/current revision race/index semantics. |
| `gtas_vpp_be.Tests/VPPRequestTests/VPPRequestServiceTests.cs` | modified | Fixtures/typed audit/period assertions. |
| `gtas_vpp_be.Tests/VPPRequestTests/VPPRequestLifecycleTests.cs` | new | Revision/history, cancel/replace, slot, stale rowversion, idempotency, scope/quota/self-decision. |
| `gtas_vpp_fe.Tests/Services/ApiServicesJsonTransportTests.cs` | modified | Approval concurrency envelope serialization. |

### 5.6 Documentation đã có và cần đọc

`docs/execution/BASE-001.md`, `ENV-001.md`, `QA-001.md`, `ARCH-001.md`,
`SEC-001.md`, `SEC-002.md`, `LEAN-02.md`, `LEAN-03.md`, `LEAN-04.md` là các
execution records đã có. `docs/execution/LEAN-05.md` chưa tồn tại và phải
được tạo khi đóng package. Không sửa Word trong handoff này.

## 6. Lỗi/vấn đề và giới hạn còn tồn tại

### Chưa được claim là pass

- Isolated UI E2E chưa có output cuối. Không có auth/runtime target cố định để
  chạy production; chỉ dùng disposable QA stack. Plain run phải bị safety
  contract chặn.
- Secret scan/Gitleaks và full migration script gate của final LEAN-05 cần
  chạy lại trên current tree trước commit.
- Chưa có `LEAN-05.md`, chưa stage, chưa commit.

### Risk kỹ thuật cần giữ trong handoff

- Migration `Down` forward-only; rollback DB phải là verified backup/restore
  hoặc forward correction, không dùng source revert để giả vờ undo dữ liệu.
- Self-FK/CHECK lineage và nullable Period rowversion là hardening follow-up,
  không tự ý mở rộng scope LEAN-05 nếu không có blocker evidence.
- `VPPRequestService` dead helpers và draft localStorage TTL là debt có thể
  xử lý ở LEAN-08.
- Không còn Docker CLI trên máy; LocalDB là evidence hiện có. Không tuyên bố
  cross-platform/Testcontainers coverage.
- Không có app owner production active; không mutate DigitalOcean/database.
- Tám Google-key alerts lịch sử còn mở theo waiver; AI off.
- Git có cảnh báo line-ending LF→CRLF khi diff; `git diff --check` hiện exit 0,
  nhưng phải chạy lại sau stage.

## 7. Build, test, migration và lệnh đã chạy

### 7.1 Verification mới nhất trên snapshot này

Các lệnh sau đã chạy serial, không sửa source:

```powershell
dotnet build gtas_vpp.sln -c Release --no-restore
# PASS — 0 Warning(s), 0 Error(s), 14.46s

dotnet test gtas_vpp_be.Tests\gtas_vpp_be.Tests.csproj -c Release --no-build --no-restore
# PASS — 321 passed, 0 failed, 0 skipped

dotnet test gtas_vpp_fe.Tests\gtas_vpp_fe.Tests.csproj -c Release --no-build --no-restore
# PASS — 87 passed, 0 failed, 0 skipped

dotnet test gtas_vpp_be.IntegrationTests\gtas_vpp_be.IntegrationTests.csproj -c Release --no-build --no-restore
# PASS — 14 passed, 3 intentional LocalDB opt-in skips, 17 total

dotnet ef migrations has-pending-model-changes `
  --project gtas_vpp_be\gtas_vpp_be.Migrations\gtas_vpp_be.Migrations.csproj `
  --startup-project gtas_vpp_be\gtas_vpp_be\gtas_vpp_be.csproj `
  --context VPPMigrationDbContext --configuration Release --no-build
# PASS — No changes have been made to the model since the last migration.

dotnet test gtas_vpp_fe.UITests\gtas_vpp_fe.UITests.csproj `
  -c Release --no-build --no-restore --list-tests
# PASS discovery — 16 tests discovered; execution result is not yet available.

git diff --check
# exit 0; only LF→CRLF advisory warnings from Git.
```

### 7.2 Migration evidence đã có từ agent trước

- Generated idempotent SQL: 78,520 bytes,
  `%TEMP%\gtas_vpp_lean05_idempotent.sql`.
- Disposable LocalDB validation database:
  `gtas_vpp_lean05_validation`.
- Upgrade from earlier migration với 6 legacy rows + 3 logs succeeded.
- `PeriodCount=2`, `BrokenLineage=0`, `RestoredCancellation=2`,
  `DuplicateCurrentRegularGroups=0`, migration history present.
- Guarded downgrade đã throw `NotSupportedException` và database vẫn intact.
- Đây là local/disposable evidence, không phải production apply.

### 7.3 E2E command cần chạy để đóng LEAN-05

Chỉ dùng disposable QA target và explicit opt-in:

```powershell
$env:GTAS_E2E_ISOLATED='1'
$env:GTAS_E2E_MUTATION_OPT_IN='I_UNDERSTAND_THIS_MUTATES_QA_DATA'
dotnet test gtas_vpp_fe.UITests\gtas_vpp_fe.UITests.csproj `
  -c Release --no-build --no-restore
Remove-Item Env:GTAS_E2E_ISOLATED -ErrorAction SilentlyContinue
Remove-Item Env:GTAS_E2E_MUTATION_OPT_IN -ErrorAction SilentlyContinue
```

Không chạy lệnh trên với production URL/DB; nếu fixture không isolated thì
dừng và sửa safety harness, không tắt guard.

## 8. Git status, diff stat và log

### 8.1 Snapshot trước và sau khi tạo handoff

```text
Branch: codex/sec-001-secret-rotation
HEAD: ee77b51a6274773c5f914193e9a6fcafd5c6a3e4
Upstream: chưa cấu hình
Staged changes: không có
Working tree trước handoff: 39 tracked modified + 16 untracked (LEAN-05)
Working tree sau handoff: 39 tracked modified + 17 untracked
  (16 LEAN-05 files + HANDOFF-CLIPROXY.md)
```

`git diff --stat` (tracked files only):

```text
39 files changed, 2010 insertions(+), 272 deletions(-)
```

`git status --short` sau khi tạo file có thêm đúng dòng `?? HANDOFF-CLIPROXY.md`;
không có staged change. Các dòng còn lại được liệt kê đầy đủ ở mục 5 và mục
9.1. `git diff --stat` không tính 17 untracked files, nên con số 39/+2010/-272
ở trên không phải tổng kích thước handoff/LEAN-05.

### 8.2 `git log -10 --oneline --decorate` tại snapshot

```text
ee77b51 (HEAD -> codex/sec-001-secret-rotation) feat(ui): establish Vietnamese Radzen shell and safe async states
9e50c05 feat(auth): add pending registration and recovery lifecycle
9832493 feat(auth): establish trusted app-owned identity and canonical rbac
3b28072 (origin/Nam) docs(security): close LEAN-01 with production evidence
c1e88df chore(security): retire one-time containment tooling
8b2e7e7 fix(deploy): require pinned runtime for database recovery
5c259f3 fix(deploy): recover missing database without rename fallback
a6cc483 ops(security): add quiesced demo containment and paired restore
fd41b2a fix(deploy): arm app recovery before SQL rotation
af289d8 fix(deploy): recover apps after database key roll-forward
```

Historical full hashes/timestamps, nếu cần:

```text
ee77b51a6274773c5f914193e9a6fcafd5c6a3e4  2026-07-16  feat(ui): establish Vietnamese Radzen shell and safe async states
9e50c0544cd1aa2d1b87de0e65887f0ac1f56176  2026-07-16  feat(auth): add pending registration and recovery lifecycle
983249329153a9e966344f1fdbf613e24ec98fd2  2026-07-16  feat(auth): establish trusted app-owned identity and canonical rbac
3b280728499e130621176f00739d3b8b2d8f7267  2026-07-15  docs(security): close LEAN-01 with production evidence
c1e88dfa0129ba0d8c680e04c170bbde6df0a1ff  2026-07-15  chore(security): retire one-time containment tooling
8b2e7e7e2763e137750eef7fef2bb55b1c002656  2026-07-15  fix(deploy): require pinned runtime for database recovery
5c259f3330e12412d7e6911d71ba262cc7b10c7a  2026-07-15  fix(deploy): recover missing database without rename fallback
a6cc483859f5ccb3dca5e84efa11e11dbfd36de8  2026-07-15  ops(security): add quiesced demo containment and paired restore
fd41b2a1716402bafa36ca1b73fbb7a97259e533  2026-07-15  fix(deploy): arm app recovery before SQL rotation
af289d840138989f0f73f64afabd659d2a8b04ae  2026-07-15  fix(deploy): recover apps after database key roll-forward
```

## 9. Tuyệt đối không làm lại hoặc thay đổi

### 9.1 Protected files — không sửa, restore, move hoặc stage

Các hash đã xác nhận ở snapshot:

```text
E9E91C5A9A67F736E9CEAEC0DA1282DC68AF44E3D36039A465E214F0756AE17D  LVTN/NguyenAnNam_DH52201078_working.docx
E7FAD87A1CF792468E5378FA8ED3CFF0DFA5CB459F62DF72A9CFE3977F0F6B36  gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/App.razor
622C4C976ABE287A7CB6ED78DC984D58AF04CE67B03FCAD9C351AF80BB248319  gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-login.css
81E49BD90CF3C823076CA3F8B0A3B5192038719E3D4605C4EC0EA176CDF28ACB  gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-responsive.css
```

Nếu có overlap thật sự, dừng và hỏi owner; không đoán rằng file dirty là
phần của package.

### 9.2 Product/security/architecture non-negotiables

- Không quay lại `GTAS_MENU` company user DB, stored-procedure login,
  TripleDES/reversible password hoặc generic SQL executor.
- Không redo LEAN-00/LEAN-01/LEAN-02/LEAN-03/LEAN-04; đọc evidence và tiếp
  tục LEAN-05.
- Không claim tám Google keys đã revoke, không reactivate 12 demo identities,
  không restore compromised credential.
- Không mutate production/DigitalOcean/owner account/database/email provider
  nếu thiếu explicit authority, maintenance window, preflight và paired
  backup. No production apply trong LEAN-05.
- Không rewrite frontend framework, microservices, Kafka/Redis, full
  PO/inventory/accounting, chatbot/AI mutation.
- Shared DTO chỉ ở `gtas_vpp_be/gtas_vpp_shared`; không tạo bản sao ở FE.
- Không stage `bin/`, `obj/`, `TestResults/`, secret, token, password,
  connection string cá nhân, real PII, temp render/cache.
- Không làm Word drive implementation; source/schema/UI freeze trước khi sửa
  working DOCX. Không overwrite `LVTN/NguyenAnNam_DH52201078.docx`, không
  thêm page border.
- Migration rollback là backup/restore hoặc forward correction; không dùng
  `git reset --hard`, `git checkout --` trên dirty user files.
- UI E2E phải fail closed và chỉ mutate disposable isolated QA data.

### 9.3 Scope cuts đã chốt

`REPORT-004` PDF, WOW-002/003/004, AI-001, real notification provider,
mass-format/analyzer sweep, broad generic endpoint retirement, full-system
load/SLA, telemetry platform và server deploy mặc định đều deferred/reduced
theo plan. NOTIF-003 được thay bằng Mailpit/local sandbox. Không tự khôi phục
cut chỉ vì còn quota.

## 10. Các bước tiếp theo theo đúng thứ tự

### Bước 0 — nhận phiên

1. Đọc file này, `AGENTS.md`, `.codexrules`, plan và decisions.
2. Chạy `get_goal`. Nếu null, tạo một Goal duy nhất theo mục 2.3; không tạo
   Goal cho từng card.
3. `git status --short`, hash bốn protected files, kiểm tra không có process
   test/app cũ đang mutate DB. Không stage gì ở bước này.

### Bước 1 — đóng LEAN-05

1. Recover/rerun isolated UI E2E và lưu exit/count/evidence.
2. Chạy final build/test/integration/EF/migration/secret/diff gates.
3. Tạo `docs/execution/LEAN-05.md`.
4. Stage explicit 51 file LEAN-05 (35 tracked + 16 new), tuyệt đối exclude
   Word/App.razor/login CSS/responsive CSS và handoff nếu không muốn gộp.
5. Commit checkpoint, verify `git status` và protected hashes.
6. Cập nhật execution plan: LEAN-05 DONE, LEAN-06 IN_PROGRESS.

### Bước 2 — LEAN-06 Procurement và immutable settlement

Không làm một commit khổng lồ; dùng các checkpoint nội bộ:

1. **CAT-001:** typed catalog/unit/supplier writes, validation/soft-delete,
   allowlisted server paging/filter/sort/search tiếng Việt; prove 1,000 items
   (optional 10k probe), không full materialization/hard-delete bypass.
2. **PRICE-001:** versioned/effective supplier-linked price books, deterministic
   `PriceAsOfUtc` resolver, net+VAT, missing/ambiguous/expired block, preflight
   và backfill.
3. **PRICE-002:** typed draft/publish/expire, quote comparison cho whole
   basket với coverage/subtotal/discount/rebate/fee/shipping/VAT/MOQ/lead/
   validity; calculation version phải giống settlement.
4. **SET-001:** read-only whole-company preview từ regular submitted + approved
   supplement có provenance; rank/chọn one primary supplier; exception có
   reason; preview/input hash, VND rounding, blockers và allocation
   reconciliation.
5. **SET-002/UI-006:** transaction ngắn, idempotent close vào immutable
   header/item/charge/allocation snapshots; đúng một supplier/revision; tổng
   và allocation reconcile; correction là revision mới có reason/permission/
   four-eyes, không overwrite. Advance period `Pricing → Settled`.
6. Mỗi sub-checkpoint có auth direct API + UI, migration/forward rollback,
   tests và execution note delta; chỉ sau đó chuyển LEAN-07.

### Bước 3 — LEAN-07 Product proof

REPORT-001 semantic KPI/glossary/reconcile → REPORT-002 role dashboard/
drill-down → REPORT-003 một workbook Excel polish (metadata, summary, items,
department allocations, formula-injection safe). Sau prerequisite, NOTIF-001
inbox durable/idempotent/deep-link/SignalR recovery → NOTIF-002 Vietnamese
email outbox/template/retry/preferences bằng Mailpit/local sandbox. Không thêm
AI mới.

### Bước 4 — LEAN-08 Release candidate

Reduced formatting/CI, targeted probes (catalog 1k, one report query,
settlement preview), scrubbed logs/correlation/health, complete SQL/API/
security/concurrency tests, 4–5 isolated E2E journeys × 3 viewports,
axe/keyboard/console/network, secret/vulnerability/migration artifact,
paired backup/restore. Local clean release mandatory; DigitalOcean chỉ khi
preflight/authority green. Nếu local-only, ghi `Path: ALTERNATIVE`, không
claim production.

### Bước 5 — LEAN-09 Thesis/defense/handoff

Traceability/ADR/PlantUML+SVG/evidence → source freeze → chỉ sửa working DOCX
đúng source → update fields/links/pages/captions/references → render và inspect
toàn bộ → tạo `LVTN/checkpoints/99_final.docx` (chỉ final checkpoint tracked)
→ hai lần demo sạch với fallback offline → release notes/tag/handoff. Luồng
demo: register → approve → request → supplement → basket → supplier →
allocation → report/Excel/email → revoke permission.

## 11. Prompt hoàn chỉnh cho Codex agent mới

Dán nguyên prompt sau vào agent tiếp quản (hoặc dùng làm objective/context
chính sau khi tạo Goal):

```text
Bạn đang tiếp quản repository D:\WORK\gtas_vpp sau khi model_provider đổi sang
CLIProxyAPI. Hãy đọc HANDOFF-CLIPROXY.md ở thư mục gốc, AGENTS.md,
docs/planning/06-LEAN-A-PLUS-EXECUTION-PLAN.md và các decision/execution record
được chỉ rõ trước khi hành động.

Mục tiêu duy nhất: thực thi liên tục retained LEAN-A-PLUS plan từ trạng thái
hiện tại. LEAN-00/01/02/03/04 đã hoàn tất; hãy đóng LEAN-05, sau đó tự động
thực thi LEAN-06, LEAN-07, LEAN-08 và LEAN-09 theo dependency, không dừng
giữa package và không tạo Goal cho từng card. Chỉ dừng khi LEAN-01..LEAN-09
đều DONE hoặc có external/undiscoverable blocker thật sự sau khi đã làm hết
safe work. Nếu get_goal() trả null, tạo đúng một Goal với objective này,
không đặt token budget nếu không được yêu cầu.

Trước khi sửa:
- Kiểm tra git status/branch và SHA-256 bốn protected files:
  LVTN/NguyenAnNam_DH52201078_working.docx
  gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/App.razor
  gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-login.css
  gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-responsive.css
- Không sửa/restore/move/stage bốn file đó. Không dùng git reset --hard hoặc
  git checkout --.
- Giữ app-owned Identity, single-company, bốn flat personas, Radzen Blazor,
  Shared DTO ở backend shared project và toàn bộ D-001..D-012.
- Không reconnect company user DB/GTAS_MENU login; không claim tám historical
  Google-key alerts đã revoked; không reactivate demo identities; không mutate
  production/DigitalOcean/provider/email thật.
- Giữ Playwright/tooling hữu ích; E2E chỉ chạy trên disposable isolated QA
  data với safety opt-in. Không commit secret/PII/bin/obj/TestResults/temp.

Việc đầu tiên là hoàn tất LEAN-05:
1. Chạy isolated UI E2E bằng GTAS_E2E_ISOLATED=1 và
   GTAS_E2E_MUTATION_OPT_IN=I_UNDERSTAND_THIS_MUTATES_QA_DATA; lần chạy không
   isolation phải fail closed. Lưu evidence, không chạy production.
2. Chạy Release solution build, BE/FE/integration, EF pending-model,
   migration script, current-tree secret scan, diff check và protected hashes.
3. Tạo docs/execution/LEAN-05.md, stage đúng file LEAN-05, exclude protected
   files, commit checkpoint. Chỉ sau đó chuyển plan LEAN-05 DONE/LEAN-06
   IN_PROGRESS.

LEAN-06 phải làm theo thứ tự CAT-001 → PRICE-001 → PRICE-002 → SET-001 →
SET-002/UI-006: typed catalog với server paging/search cho ít nhất 1,000
items; effective net/VAT price books và deterministic resolver; whole-company
quote/preview; một primary supplier có exception reason; immutable
price/discount/fee/VAT/item/allocation snapshots; idempotent close, correction
revision và Pricing→Settled. Không làm full PO/inventory/accounting.

LEAN-07: role-scoped reconciled reports/KPI, một workbook Excel an toàn,
durable in-app inbox và Vietnamese email outbox/retry bằng Mailpit sandbox;
AI vẫn off, PDF/Teams/Zalo/real provider deferred.

LEAN-08: reduced release quality/recovery gates, targeted performance,
structured scrubbed logs, isolated core E2E/a11y/viewport checks, migration/
backup/restore evidence. Local release bắt buộc; server chỉ khi owner
authority/preflight green.

LEAN-09: traceability/ADR/PlantUML+SVG, source-freeze rồi cập nhật working
DOCX bằng documents render-and-verify workflow, tạo checkpoints/99_final.docx,
hai demo rehearsal và handoff/tag. Không overwrite original Word, không page
border, luận văn phải khớp source thật.

Mỗi package phải có execution record, acceptance evidence, rollback/forward
recovery note, exact tests/counts và git scope. Dùng tối đa hai subagent bounded
song song khi hữu ích; không lặp full audit. Giao tiếp ngắn gọn mỗi khi có
tiến triển nhưng tiếp tục tự động, không chờ người dùng nói “tiếp tục”.
```

## 12. Definition of handoff done

- File này tồn tại ở root và là file duy nhất được tạo trong turn handoff.
- Source/database/config ngoài file này không bị sửa bởi turn handoff.
- Agent mới có thể biết chính xác current branch, uncommitted LEAN-05,
  decisions, protected boundaries, evidence, residual risks và ordered next
  steps mà không cần đọc lại cuộc trò chuyện.
- Mọi claim “DONE” trong tương lai phải có execution record và command output;
  đặc biệt không claim UI E2E/production/key revocation khi chưa có evidence.
