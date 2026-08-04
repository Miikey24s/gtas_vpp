# BACKEND-REFACTOR-001 — Backend dễ đọc, dễ trình bày và dễ bảo trì

- Status: B0R CHARACTERIZATION COMPLETE — 2 AUTH DECISIONS + UI ACCEPTANCE PENDING; B1 NOT OPEN
- Priority: P1
- Path: STANDARD — behavior-preserving modular refactor
- Owner: Nguyễn An Nam
- Planning agent: Codex
- Branch: `codex/ai-agent-foundation`
- Base commit: `46560f6020824cbea9e02bcb8bb06131f1efd501`
- Planned at: `2026-07-29T05:17:46+07:00`
- Refreshed against: `be82b1c9` at `2026-08-04`
- Related authority: `AGENTS.md`, `src/Backend/AGENTS.md`,
  `docs/architecture/ARCH-001-MODULE-MAP.md`,
  [`BACKEND-REFACTOR-001-B0R-LEDGER.md`](./BACKEND-REFACTOR-001-B0R-LEDGER.md)
- Related sequencing: `docs/execution/FRONTEND-REFACTOR-001.md`
- Supersedes: phần **R-1 backend** và quy ước comment backend trong
  `docs/execution/REFACTOR-001.md`; lịch sử R-0/R-2 của record cũ vẫn giữ nguyên
- User approval required: không còn decision kiến trúc backend; production implementation chỉ bắt đầu
  sau owner UI final acceptance. Correction A đã có evidence và không làm đổi Shared/API boundary.

<a id="plan-overview"></a>

## 0. Bản một ánh nhìn

| Mục | Tóm tắt dễ hiểu | Chi tiết |
|---|---|---|
| Kết quả cần đạt | Backend vẫn chạy y như hiện tại nhưng người mới có thể lần từ API → use case → database nhanh, tên dễ hiểu, class có trách nhiệm rõ, không còn rác đã chứng minh | [Objective](#plan-detail-objective) |
| Phạm vi | `src/Backend`, backend tests và tài liệu đọc code; không redesign UI, không đổi API/JSON/quyền/nghiệp vụ/schema | [Scope](#plan-detail-scope) |
| Phương án | Giữ modular monolith và 4 project hiện tại; tổ chức dần theo module `IdentityAccess`, `CatalogPricing`, `Requests`, `Settlement`, `Reports`, `Notifications`, `Platform`; không big-bang rewrite | [Target structure](#plan-detail-target-structure) |
| Các bước chính | B0R refresh/khóa contract → B1a-1 dead code nhỏ → B1a-2 bỏ `BaseServices` có characterization → B1b metadata/local cleanup → B2 Reports → B3 Catalog/Pricing → B4 Requests → B5 Settlement → B6 Identity/Access → B7 Platform → B8 persistence/final review | [Waves](#plan-detail-waves) |
| Comment/naming | Identifier English, ưu tiên từ đầy đủ và từ vựng nghiệp vụ; comment tiếng Việt ngắn chỉ giải thích **vì sao/ràng buộc**, không mặc định gắn số mục luận văn vào source | [Readability contract](#plan-detail-readability) |
| Model/quota routing | Official resolver hiện chọn `gpt-5.6-sol` cho kiến trúc/review khó và `gpt-5.6-terra` cho lát cơ học rõ. Quota probe ngày 03/08 trả 404 nên capacity chưa được xác minh; chỉ mở B0R độc lập rồi probe/đo lại trước B1 | [Routing](#plan-detail-routing) |
| Kiểm tra | Backend unit 520/520; integration mặc định 14 pass/6 skip; disposable LocalDB 20/20; EF không có pending model; agent setup 63/63. Số test là snapshot, không phải invariant | [Verification](#plan-detail-verification) |
| Rủi ro chính | Mass move/rename làm diff khó review; generic endpoint có hidden consumer; hai DbContext dễ gây model drift; migration/generated file bị hiểu nhầm là rác | [Risks](#plan-detail-risks) |
| Bước tiếp theo | Owner chốt UI visual board và B0R-D1/B0R-D2 → reforecast → mới vào B1a-1; raw English period reason giữ ở compatibility backlog | [Continuation](#plan-detail-continuation) |

**Thuật ngữ:** `behavior-preserving` = đổi cấu trúc bên trong nhưng hành vi quan sát được không đổi;
`characterization test` = test khóa hành vi hiện có trước khi refactor; `migration-on-touch` = chỉ di
chuyển code cũ khi module đó đang được xử lý và có test bảo vệ.

<a id="plan-detail-objective"></a>

## 1. Objective và definition of done

### Objective

Refactor backend để một sinh viên năm 4 có thể:

1. biết chức năng nằm ở module nào mà không phải tìm trong một thư mục `Services` phẳng;
2. lần một luồng nghiệp vụ từ route → controller → use case/service → persistence trong tối đa vài
   bước có tài liệu dẫn đường;
3. đọc tên class/method/variable bằng English phổ thông, đúng thuật ngữ GTAS VPP;
4. hiểu các invariant khó nhờ comment tiếng Việt ngắn và test tên rõ;
5. trình bày được kiến trúc, luồng request, phân quyền, transaction và database khi bảo vệ luận văn;
6. tiếp tục vibe-coding mà AI khó đặt logic sai layer hoặc tạo đường ghi nghiệp vụ thứ hai.

### Definition of done

- Dependency production vẫn đúng `API → Application → Domain`; `Migrations → Domain`; Shared contract
  vẫn là wire-contract duy nhất.
- API route, HTTP verb, authorization policy, status-code contract, JSON shape và serialization của
  public DTO không đổi, trừ một task compatibility riêng được owner duyệt.
- Không có controller nghiệp vụ truy cập `VPPContext` trực tiếp; controller chỉ làm transport,
  authorization/binding và gọi typed use case/service.
- Các hotspot lớn được tách theo **lý do thay đổi**, không theo luật số dòng máy móc. File lớn còn lại
  phải có lý do rõ trong module ledger.
- Không còn dead code đã chứng minh, stale sample, nested Git metadata trùng hoặc runtime artifact
  phát sinh trong `src`.
- `docs/CODE-READING-GUIDE.md` có backend module map, glossary English–Vietnamese và ít nhất năm
  luồng hay bị giảng viên hỏi.
- Tất cả required gates tại [mục 10](#plan-detail-verification) pass; nếu model/schema phát sinh delta
  ngoài dự kiến thì wave phải dừng.

### Non-goals

- Không rewrite sang microservices, CQRS toàn hệ thống, event bus hoặc một template Clean Architecture
  mới chỉ để “đẹp kiến trúc”.
- Không tự thêm MediatR, AutoMapper replacement, generic repository/controller mới hoặc project
  `Infrastructure` nếu chưa có vấn đề đo được và owner chưa duyệt.
- Không đổi nghiệp vụ, permission, API, DTO wire name, database schema, migration history, stored
  procedure hoặc dữ liệu thật trong plan này.
- Không mass-format, mass-namespace-rename hoặc move toàn bộ repository trong một commit.
- Không refactor frontend/UI trong record này.

## 2. Evidence baseline — historical 2026-07-29, refreshed 2026-08-04

### Repository và build gates

| Evidence | Kết quả hiện tại |
|---|---|
| Backend source inventory | 186 file C# tracked; 163 file non-generated, khoảng 31.923 dòng. `Migrations` có 47 file/38.021 dòng chủ yếu là schema history/generated và không được coi là rác |
| Backend unit trên B0R characterization | PASS — 520/520 |
| Backend integration default | PASS — 14 pass, 6 skip có điều kiện; kết quả này chưa đủ chứng minh SQL Server behavior |
| Backend integration với `GTAS_QA_SQL_INTEGRATION=1` | PASS — 20 pass, 0 skip trên harness-owned disposable LocalDB; fixture đã derive số role từ `CanonicalRbac.Personas` |
| EF pending-model check | PASS — không có thay đổi model sau migration gần nhất |
| `dotnet format whitespace ... --verify-no-changes --include src/Backend` | PASS qua solution-wide `dotnet format --verify-no-changes` trong full backend verify |
| `dotnet format analyzers ... --severity warn --verify-no-changes --include src/Backend` | PASS qua solution-wide `dotnet format --verify-no-changes` trong full backend verify |
| Current project graph | Khớp `ARCH-001-MODULE-MAP.md` |
| `./scripts/ai/Test-AgentSetup.ps1` | PASS — 63/63; blocker `model-routing-eval` cũ đã được sửa |
| `./scripts/gtas.cmd verify -Scope backend` | PASS — build 0 warning/error, agent setup 63/63, unit 520/520, integration portable 14 pass/6 conditional skip, EF zero delta, vulnerability/leak audit pass |

Số test chỉ là snapshot ngày refresh, không phải invariant lâu dài.

### Hotspots đã đo

| File | Dòng xấp xỉ | Vấn đề đọc hiểu chính |
|---|---:|---|
| `Application/Services/VPPRequestService.cs` | 2.557 | Query, create/update/cancel, supplement approval, period logic, mapping và helper nằm chung |
| `Application/Services/DemoWorkbookSeeder.cs` | 1.475 | Parse, validate, reconcile và write demo data trong một file |
| `Application/Services/PeriodSettlementService.cs` | 1.311 | Preview, confirm/correct, revision query, legacy settlement và snapshot logic cùng class |
| `Api/Controllers/VPPRequestController.cs` | 1.194 | Transport, filter/query, export, notification và dashboard query bị trộn; còn direct EF cho dashboard |
| `Api/Controllers/LibraryController.cs` | 1.168 | Typed query tồn tại song song generic CRUD/PATCH và direct `VPPContext` access |
| `Api/Controllers/PermissionController.cs` | 1.090 | Permission/user query, mapping và transaction qua `IUnitOfWork.VPPContext` trong controller |
| `Api/Authorization/AccountLifecycleService.cs` | 1.028 | Account lifecycle và security branching là hotspot Identity/Access mới |
| `Application/Services/SeedData.cs` | 960 | Reference seed, demo seed, SQL script, prices-file parsing và validation |
| `Application/Services/AuthBootstrap/AuthBootstrapProvisioner.cs` | 847 | Bootstrap, mapping và reconciliation cần owner module rõ |
| `Api/Authorization/MembershipAdministrationService.cs` | 845 | Membership mutation và authorization logic cần characterization security |
| `Api/Program.cs` | 816 | Logging, config validation, DI, auth, middleware, migration/seed runner và deployment guards |

Số dòng chỉ là **tín hiệu hotspot**, không phải tiêu chí tự động buộc tách class.

### Cleanup evidence đã xác nhận

- Năm private method `UpdateOrderLegacyAsync`, `CancelOrderLegacyAsync`,
  `ApproveAdditionalOrderLegacyAsync`, `RejectAdditionalOrderLegacyAsync`,
  `GetCurrentPeriodInfoLegacyAsync` chỉ xuất hiện tại chính declaration.
- `ObjectHelpers` và `PasswordHelpers` vẫn không có consumer nào ngoài file định nghĩa.
- `Api/gtas_vpp_be.http` còn route mẫu `weatherforecast` và `api/sql/Test/test` không tồn tại.
- `Api/readme.md` còn lệnh EF theo layout project cũ.
- `Api/.gitignore` và `Api/.gitattributes` lặp gần như toàn bộ root metadata.
- `Api/gtas_vpp_be.csproj` còn include `.github/copilot-instructions.md` tương đối nhưng file đó không
  tồn tại trong project path.
- `Api/logs`, `bin`, `obj`, `*.csproj.user` là ignored artifact; log sink hiện ghi tương đối vào
  `logs/log-.txt`, nên chạy local có thể làm `src/Backend/Api/logs` phình ra.
- `SeedDefaultPricesFromFile` và private helper cluster trong `SeedData.cs` không có caller; `prices.txt`
  hiện chỉ còn csproj content include và historical docs, cần resource/seed audit trước khi remove.
- Runtime `VPPContext` và `VPPMigrationDbContext` có phần lớn mapping bị lặp; audit phát hiện tên
  constraint period đã drift giữa hai context. Đây là lý do phải thêm model/config parity gate, không
  phải lý do hợp nhất context ngay lập tức.
- `BaseServices`/`IBaseServices` vẫn có consumer thật qua `VPPRequestService`, DI và tests; không còn là
  dead-code deletion chung với B1a-1. Cần characterization riêng trước khi bỏ inheritance/extra UoW.
- Poppins/OFL, demo TSV/JSON, SQL scripts và export builders có resource/runtime consumer; mặc định
  `KEEP` cho đến khi ledger chứng minh replacement và output parity.

<a id="plan-detail-readability"></a>

## 3. Readability contract

### Naming

- Identifier, file và namespace mới dùng English.
- Ưu tiên readability hơn brevity: `departmentOrders`, `currentSettlementRevision`,
  `ApproveAdditionalOrderAsync`; tránh `tmp`, `obj`, `data2`, `mgr`, `proc` nếu có từ rõ hơn.
- Dùng đúng glossary nghiệp vụ của dự án: `Request`, `Supplement`, `Period`, `Settlement`,
  `Revision`, `PriceList`, `MemberCompany`, `Department`.
- New internal identifiers dùng casing .NET hiện hành (`VppRequest`, `UserId`, `FileName`). Không
  mass-rename `VPP*`, `UserID`, typo/casing trong public DTO, JSON, DB object hoặc migration chỉ vì style.
- Một tên phải trả lời được “đây là gì/đang làm gì”; tránh suffix chung chung như `Manager`, `Helper`,
  `Utils`, `Processor` nếu trách nhiệm cụ thể có thể được gọi tên.

### Comment tiếng Việt, nhưng không “lộ liễu”

Comment chỉ dùng khi code không tự giải thích được **lý do**:

- invariant nghiệp vụ;
- security/authorization boundary;
- concurrency/idempotency/transaction decision;
- compatibility workaround bắt buộc;
- recovery hoặc dữ liệu lịch sử không được xóa.

Ví dụ phù hợp:

```csharp
// Không cho người tạo tự xác nhận hiệu chỉnh để giữ nguyên tắc bốn mắt.
```

Không dùng comment để kể lại code:

```csharp
// Lấy dữ liệu từ database.
// Kiểm tra trạng thái pending.
```

Không mặc định ghi `§2.3...` trong source. Mapping tới luận văn nằm trong
`docs/CODE-READING-GUIDE.md`; chỉ giữ reference trong code khi một rule thật sự không thể hiểu đúng
nếu thiếu tài liệu ngoài source. Không để comment kiểu “AI generated”, lịch sử thử-sai hoặc TODO không
có owner/task.

### Class/method

- Một class có một nhóm trách nhiệm cohesive và một lý do thay đổi chính.
- Tách khi class đang trộn query + mutation + export + notification + database initialization, không
  tách chỉ vì vượt N dòng.
- Controller mỏng: validate transport, lấy current-user context, gọi typed service/use case, map HTTP
  result. Không chứa EF query hoặc business branching dài.
- Method dài được tách theo bước nghiệp vụ có tên; không tạo private method một dòng chỉ để giảm LOC.
- Interface chỉ tạo ở boundary cần DI/test double hoặc có nhiều implementation thật; không tạo
  `IThingService` máy móc cho mọi class.
- Test name theo `Method_Scenario_ExpectedBehavior`; test là executable documentation, không test
  private method hoặc cấu trúc implementation không quan trọng.

### Tooling ratchet

- Giữ built-in .NET/Roslyn analyzers; chưa thêm analyzer package thứ ba ở B0R/B1.
- B0R lập report cho high-signal rule như unused private member, naming và complexity; ban đầu dùng
  suggestion/warning hoặc changed-file ratchet.
- Chỉ nâng rule thành error sau khi baseline sạch và false-positive thấp.
- Mechanical format phải là commit riêng; không trộn với move/split/business-sensitive refactor.

<a id="plan-detail-scope"></a>

## 4. Scope và compatibility boundaries

### In scope

- `src/Backend/Api`, `Application`, `Domain` và runtime-cleanup liên quan.
- Backend unit/integration/architecture tests cần để khóa hành vi.
- `docs/CODE-READING-GUIDE.md`, module map/ADR nếu architecture boundary thực sự đổi.
- `Migrations` chỉ được audit và bảo vệ; chỉnh file migration/model chỉ khi một task database riêng
  được owner duyệt và chạy skill `gtas-vpp-db-safety`.

### Must remain stable

- Route + HTTP verb + authorization policy.
- Request/response DTO JSON property set, casing, nullability và computed serialized field.
- Status codes, error envelope và authentication/session behavior.
- Transaction, concurrency, idempotency, period/supplement/settlement invariants.
- EF model snapshot và pending-model result đối với wave không chạm DB.
- Stored procedure/view name, SQL seed behavior và deployment mode.

### Stop conditions

Wave dừng và tách task/approval riêng nếu xuất hiện một trong các điểm sau:

- cần đổi public DTO/API hoặc frontend consumer;
- EF báo pending model change;
- cần drop/rename table, column, index, stored procedure hoặc migration đã áp dụng;
- refactor phát hiện behavior hiện tại sai và cần sửa nghiệp vụ;
- hidden external consumer của generic endpoint hoặc SQL gateway chưa kiểm chứng được.

<a id="plan-detail-target-structure"></a>

## 5. Target structure

Giữ bốn project hiện tại; tổ chức dần theo feature/module bên trong project, không tạo thêm project
chỉ để giống sơ đồ sách.

```text
src/Backend/
├─ Api/
│  ├─ Features/
│  │  ├─ IdentityAccess/
│  │  ├─ CatalogPricing/
│  │  ├─ Requests/
│  │  ├─ Settlement/
│  │  ├─ Reports/
│  │  └─ Notifications/
│  └─ Platform/
│     ├─ Composition/
│     ├─ Middleware/
│     ├─ DatabaseInitialization/
│     └─ Configuration/
├─ Application/
│  ├─ IdentityAccess/
│  ├─ CatalogPricing/
│  │  └─ Localization/
│  ├─ Requests/
│  ├─ Settlement/
│  ├─ Reports/
│  │  └─ ReportInsights/
│  ├─ Notifications/
│  └─ Platform/
│     ├─ Configuration/
│     ├─ Persistence/
│     ├─ Seeding/
│     ├─ Files/
│     └─ Time/
├─ Domain/
│  ├─ Common/
│  ├─ IdentityAccess/
│  ├─ CatalogPricing/
│  ├─ Requests/
│  ├─ Settlement/
│  ├─ Notifications/
│  ├─ Projections/
│  └─ Persistence/
│     └─ Configurations/
└─ Migrations/
   └─ Migrations/        # Lịch sử EF, không dọn theo tiêu chí LOC/tên xấu.
```

Đây là target end-state, không phải một mass move. Mỗi module đi qua trình tự:

1. characterization tests;
2. tách trách nhiệm trong path hiện tại;
3. move file bằng IDE/tooling với compile/test;
4. đổi namespace nội bộ khi thật sự giúp navigation;
5. xóa adapter cũ chỉ khi consumer ledger bằng 0.

### Không áp dụng textbook máy móc

- Không bắt buộc mỗi endpoint một handler/class.
- Không tạo `Commands/Queries/Handlers` ba tầng nếu module nhỏ chỉ cần một typed service rõ.
- Không tạo generic repository mới. Generic path hiện hữu được giữ tạm cho compatibility và retire
  theo module sau consumer audit.
- Chưa hợp nhất `VPPContext` và `VPPMigrationDbContext`. B0R phải ghi rõ vai trò; chỉ extract mapping
  chung nếu EF pending-model check chứng minh zero delta.

## 6. Cleanup classification

| Candidate | Class | Hành động dự kiến | Gate trước khi làm |
|---|---|---|---|
| Năm private `*LegacyAsync` trong `VPPRequestService` | DELETE_CANDIDATE | Xóa trong B1a-1 | Usage count = 1; focused tests + all current backend tests |
| `ObjectHelpers.cs`, `PasswordHelpers.cs` | DELETE_CANDIDATE | Xóa trong B1a-1 | Repo-wide usage chỉ declaration; build/test |
| `Api/gtas_vpp_be.http` | REPLACE | Viết lại thành request smoke hiện hành, không chứa credential | Route inventory/test |
| `Api/readme.md` | MERGE/DELETE | Đưa lệnh đúng vào root/backend guide rồi xóa file stale | Documentation link check |
| `Api/.gitignore`, `Api/.gitattributes` | DELETE_CANDIDATE | Hợp nhất vào root metadata | `git check-ignore`, attr comparison |
| `Api/logs`, `bin`, `obj`, `*.csproj.user` | LOCAL_CLEANUP | B1b xóa local ignored artifact sau khi xác minh lock; không đổi production log behavior trong cleanup slice | Xác minh process owner/lock trước cleanup |
| Relative Serilog path `logs/log-.txt` | PLATFORM_CHANGE | B7 đưa sang configuration có Development/migrator/production default rõ; không gộp với dead-code cleanup | Deployment configuration tests, container-volume/log smoke, rollback config |
| Ghost csproj include `.github/copilot-instructions.md` | DELETE_CANDIDATE | Bỏ item không tồn tại | MSBuild item inspection + build |
| Dead default-price seed cluster + `prices.txt` | DELETE_CANDIDATE/NEEDS_AUDIT | Xóa private cluster sau characterization; chỉ xóa file/content include khi resource ledger xác nhận 0 runtime consumer | Seed/reference tests + build output inspection |
| EF `.Designer.cs`, snapshot, applied migrations | KEEP | Không coi là rác | DB safety skill + explicit migration task nếu cần |
| Poppins fonts, demo TSV, SQL scripts | KEEP/NEEDS_AUDIT | Giữ vì có csproj/runtime consumer; chỉ thay khi có replacement verified | Build output/resource/seed tests |
| `GenericRepository`, `BaseGenericController`, generic Library writes | MIGRATE_ON_TOUCH | Không xóa big-bang; typed path + consumer cutover từng module | Route/consumer/authorization/parity tests |
| `BaseServices`/`IBaseServices` và related UoW/config chain | MIGRATE_ON_TOUCH/NEEDS_CHARACTERIZATION | B0R lập consumer ledger; B1a-2 chỉ bỏ inheritance/extra UoW sau khi khóa construction/transaction behavior. Xóa từng related type riêng nếu usage thật bằng 0 | Focused service/config/transaction tests + DI smoke |
| Hai DbContext có mapping trùng | NEEDS_AUDIT | Document trước; không merge ở B1 | EF no-pending-model + LocalDB parity |

<a id="plan-detail-routing"></a>

## 7. Model, effort và quota routing

### Refresh 2026-08-04

- Local quota probe được retry lại ngày 04/08 nhưng endpoint scheduler vẫn trả HTTP 404; coverage hiện
  tại không khả dụng. Sanitized cache gần nhất được capture lúc `2026-07-29 05:05 +07`, đã stale và
  không được dùng để tuyên bố capacity hiện tại.
- Official model resolver hiện chọn `gpt-5.6-sol`. Theo guidance chính thức, không dùng model mạnh nhất
  cho mọi lát: Sol dành cho architecture, security/business ambiguity và final review; Terra dành cho
  implementation cơ học, rõ contract và high-volume.
- Cost range trong bảng wave là planning heuristic rộng, confidence thấp; chưa có matching consumption
  history mới nên không được coi là quota commitment.
- B0R đã hoàn tất. Với live coverage hiện không xác minh được, kết luận cho B1→B8 là `WAIT`, không tuyên
  bố `ENOUGH`. Sau owner gate chỉ B1a-1 đủ điều kiện `SLICE_ONLY`: scope độc lập, low-single-digit
  heuristic với buffer 2× và confidence thấp; giữ `gpt-5.6-terra` high + review `gpt-5.6-sol` high,
  rồi đo aggregate delta trước B1a-2.

`Khuyến nghị routing` không có nghĩa model của root task đã tự đổi.

<a id="plan-detail-waves"></a>

## 8. Execution waves

| Wave | Outcome | Scope chính | Model + effort khuyến nghị | Cost forecast | Review/handoff gate |
|---|---|---|---|---:|---|
| **B0R — Refresh contract & reading baseline** | Khóa HEAD hiện tại trước khi move/split; không làm lại role-count fix đã hoàn tất | Refresh inventory/analyzer/test gates; assert trực tiếp legacy procurement role inactive + membership/mapping reconciliation; manifest route+verb+policy và representative status/error/serialization/export; DI/resource smoke; generic endpoint consumer ledger; backend module/file map trong code-reading guide | `gpt-5.6-terra` high, review `gpt-5.6-sol` high | 2–5% | All current unit/integration gates xanh; all opt-in LocalDB tests pass, 0 skip/fail; EF zero delta; full verify xanh |
| **B1a-1 — Proven dead code nhỏ** | Production slice đầu tiên dễ review/rollback | Xóa 5 request legacy methods + `ObjectHelpers` + `PasswordHelpers`; không đổi API, DTO, schema, DI hay nghiệp vụ | `gpt-5.6-terra` medium/high, review `sol` high | 1–3% | Usage proof; focused request/controller tests + all current backend tests |
| **B1a-2 — Retire BaseServices boundary** | Bỏ inheritance và extra UnitOfWork chỉ sau khi behavior đã khóa | Characterize `VPPRequestService` construction/UoW behavior; bỏ `BaseServices`/`IBaseServices` và cập nhật DI/tests nếu consumer ledger về 0 | `gpt-5.6-terra` high, review `sol` high | 2–5% | Constructor/transaction/DI smoke; focused lifecycle/race tests + all current backend tests |
| **B1b — Metadata & local cleanup** | Source tree sạch, tài liệu local đúng | Stale `.http`/readme, ghost csproj include, nested Git metadata, ignored artifact cleanup | `gpt-5.6-terra` medium | 1–3% | `git check-ignore`/attrs/build/docs links; không đổi logging/deploy behavior |
| **B2 — Reports pilot** | Chốt pattern module trên seam read-heavy đã có service/export coverage tốt | Bổ sung `ReportsController` route/policy/direct 401/403/status-error manifest; sau đó thin controller và gom report query/builders/insights theo module. Giữ `SimpleWorkbookBuilder`/`ExportFileContract` ở Platform/Files vì Order và Settlement cùng dùng | `gpt-5.6-terra` high, review `sol` high | 4–10% | Route/JSON/status/content parity; direct 401/403; CSV/XLSX/PDF filename/MIME/signature; no DB model delta |
| **B3 — Catalog & Pricing** | Typed read/write paths rõ, thu nhỏ `LibraryController` | Catalog query, price-list lifecycle, price resolver; retire generic writes từng consumer | `gpt-5.6-sol` high cho design, `terra` high implement | 8–18% | Consumer ledger 0 trước delete; LocalDB price/catalog tests; permission parity |
| **B4 — Requests** | Luồng đơn dễ trình bày và không còn god service/controller | Query/history, create-update-cancel, supplement workflow, demand, export/notification boundary | `gpt-5.6-sol` xhigh plan/review, `terra` high implement | 15–35% | Idempotency/concurrency/revision/history tests; route/auth/JSON parity; focused LocalDB |
| **B5 — Settlement** | Preview/confirm/correct/revision tách theo use case | `PeriodSettlementService`; snapshot pricing/evidence; correction/four-eyes; query services | `gpt-5.6-sol` xhigh, `terra` high implement | 10–25% | Snapshot/hash/idempotency/four-eyes tests; SQL Server integration; no schema delta |
| **B6 — Identity & Access** | Security logic có ownership rõ, controller không query context | `PermissionController`, `AccountLifecycleService`, `MembershipAdministrationService`, `AuthBootstrapProvisioner`; giữ `Api/Authorization` là owner hiện hữu cho đến khi move thật sự cải thiện navigation, không mass-move để khớp cây mẫu | `gpt-5.6-sol` xhigh, `terra` high implement | 10–25% | Role/action/scope matrix; direct API 401/403; session invalidation; bootstrap/reconciliation; no secret/log regression |
| **B7 — Platform, composition & operations** | `Program.cs`/hosting/logging dễ đọc mà không đổi deployment behavior | Mỗi B2–B6 đã sở hữu `Add<Module>()`; B7 chỉ compose, tách DB init runner, cấu hình log path với Development/migrator/production contract, worker/notification/platform cleanup | `gpt-5.6-sol` high/xhigh | 5–12% | Composition-root smoke; deployment/log-volume contract; worker tests; all module registrations resolve |
| **B8 — Persistence mapping & final review** | Giảm mapping drift hoặc ghi nhận waiver có bằng chứng; hoàn tất guide/adapters | DB-safety checkpoint riêng cho shared `IEntityTypeConfiguration<T>` nếu zero-delta; xóa adapter hết consumer; namespace/folder cleanup; final code-reading review | `gpt-5.6-sol` xhigh | 5–12% | All current opt-in LocalDB tests pass, 0 skip/fail; fresh migrate/reseed/reset; EF no delta; full backend verify; route/wire/dependency manifest |

Chỉ đổi model ở ranh giới wave/checkpoint; một implementer chính giữ context, reviewer/subagent chỉ
audit hoặc verify độc lập.

## 9. Module decomposition guide

Các tên dưới đây là hướng trách nhiệm, không phải yêu cầu tạo đủ mọi file ngay lập tức.

### Requests

- `RequestQueryService`: my/department/all/history queries và paging.
- `RequestCommandService`: create/update/cancel + transaction/revision.
- `SupplementWorkflowService`: pending/approve/reject + decision idempotency.
- `RequestDemandService`: period demand/aggregation.
- Export builders giữ riêng; notification publish qua interface/boundary, không nằm trong controller.
- `VPPRequestController` có thể giữ route compatibility nhưng chỉ delegate; nếu vẫn lớn, tách
  controller theo route group với explicit route giữ nguyên.
- Trước B4, characterization phải khóa representative success/error: conflict, invalid rowversion,
  duplicate idempotency key, unauthorized scope và current error envelope.

### Settlement

- `SettlementPreviewService`: build preview, blockers, coverage và input hash.
- `SettlementRevisionService`: confirm/correct/persist immutable snapshot.
- `SettlementQueryService`: current/list revisions/status.
- `LegacyPeriodSettlementService` chỉ tồn tại nếu còn caller thật; nếu không có, xóa qua usage audit.

### Catalog/Pricing

- Typed query service cho list/search/grid.
- Typed command/workflow cho catalog mutation và price-list lifecycle.
- Generic read adapter có thể giữ tạm; generic write path chỉ retire khi frontend/other consumer ledger
  bằng 0 và hard-delete bypass được khóa.
- Trước B3, generic Library characterization phải khóa success/error/status và authorization của
  representative GET/POST/PATCH/DELETE path; route metadata một mình là chưa đủ.

### Identity/Access

- HTTP auth/cookie/JWT adapter ở `Api/Platform`.
- Account lifecycle, membership và permission use case có boundary typed.
- `CurrentUserContext` là security boundary; không chuyền raw client scope/department thay cho
  server-derived scope.

### Platform

- `Program.cs` cuối cùng nên đọc như một outline: build config → add modules → build app → configure
  pipeline → optional DB init → map endpoints → run.
- DI registration chuyển theo từng module wave (`AddReportsModule`, `AddCatalogPricingModule`, ...),
  không dồn thành cross-module diff ở B7 và không tạo hàng chục extension một dòng.
- Runtime `VPPContext` và migration `VPPMigrationDbContext` được tài liệu hóa vai trò; mapping shared
  chỉ extract nếu zero model delta được chứng minh.

<a id="plan-detail-verification"></a>

## 10. Verification ladder

### Gate mọi wave

```powershell
./scripts/gtas.cmd preflight -Scope backend
./scripts/ai/Test-AgentSetup.ps1
./scripts/gtas.cmd test-backend
dotnet format whitespace gtas_vpp.slnx --verify-no-changes --no-restore --include src/Backend tests/Backend.UnitTests
dotnet format analyzers gtas_vpp.slnx --verify-no-changes --no-restore --include src/Backend tests/Backend.UnitTests --severity warn
git diff --check
```

### Disposable SQL Server gate

Bắt buộc cho B0R baseline, mọi wave chạm EF query/context/seed/SQL/resource và final handoff; không
bắt buộc cho pure metadata/docs slice như B1b nếu diff chứng minh không đụng runtime resource.

```powershell
$previous = $env:GTAS_QA_SQL_INTEGRATION
try {
    $env:GTAS_QA_SQL_INTEGRATION = '1'
    dotnet test tests/Backend.IntegrationTests/gtas_vpp_be.IntegrationTests.csproj -c Release --no-restore
}
finally {
    if ($null -eq $previous) {
        Remove-Item Env:GTAS_QA_SQL_INTEGRATION -ErrorAction SilentlyContinue
    } else {
        $env:GTAS_QA_SQL_INTEGRATION = $previous
    }
}
```

Expected gate: **toàn bộ test LocalDB hiện hành pass, 0 skip, 0 fail**. Snapshot refresh ngày 04/08 là
20/20; con số này không phải invariant nếu suite được bổ sung test đúng.

### Gate theo loại thay đổi

| Change type | Bắt buộc thêm |
|---|---|
| Controller/file move | Route + HTTP verb + authorization policy manifest; API contract tests |
| Shared DTO/mapping touch ngoài dự kiến | STOP; serialization manifest + owner-approved compatibility task |
| EF query/persistence/seed/resource refactor | Focused unit + toàn bộ opt-in disposable LocalDB pass, 0 skip/fail |
| DbContext/model/config touch | EF pending-model check; nếu có delta thì STOP và chuyển DB task |
| Authorization/account | Direct endpoint 401/403, action/scope matrix, session invalidation, audit/log privacy |
| Request/settlement mutation | Transaction, concurrency, rowversion, idempotency, replay, history/snapshot invariants |
| Log/config/platform | Development + migrator + production configuration contract tests; secret scan |
| Final wave | Toàn bộ opt-in LocalDB pass, 0 skip/fail + `./scripts/gtas.cmd verify -Scope backend` + full diff review |

### Characterization status của B0R

- MVC manifest đã khóa `112` controller endpoint theo route/verb/effective authorization trước khi split/move.
- Generic repository/controller consumer ledger đã canonical; generic Catalog/Pricing parity chi tiết
  được hoãn đến trước B3 vì B1a-1 không chạm boundary này.
- RBAC unit characterization đã tạo dữ liệu legacy thật để chứng minh remap/soft-delete; LocalDB
  snapshot đã assert legacy Procurement group và membership đều inactive sau seed/reseed/reset.
- Reports đã khóa đủ 5 action guard, scope permission, insight rate limit, CSV/XLSX/PDF contract;
  request resource scope/supplement decisions và ProblemDetails mapping cũng đã có representative tests.
- Hai behavior authorization chờ owner: pending filter hiện chỉ nhận `REQUEST_APPROVE` dù grid nhận
  approve/reject; history còn outer `REQUEST_VIEW_OWN` khác detail/PDF/XLSX resource scope.
- Analyzer CLI warn-level đang sạch nhưng `.editorconfig` mới chủ yếu khóa whitespace; unused private
  member/naming/complexity chưa thành ratchet.

### Full verify policy

Blocker lịch sử `62 pass/1 fail: model-routing-eval` đã được sửa; refresh hiện tại có agent setup
63/63. Không còn waiver cố định theo signature cũ. Trước khi gọi bất kỳ production wave nào PASS,
`./scripts/gtas.cmd verify -Scope backend` phải được chạy trên HEAD của wave và xanh hoàn toàn; nếu fail
ngoài scope thì ghi exact signature, tách owner task và dừng wave thay vì dùng waiver lịch sử.

<a id="plan-detail-risks"></a>

## 11. Risks và rollback

| Risk | Mức | Mitigation | Rollback |
|---|---|---|---|
| Big-bang move làm mất lịch sử và khó review | High | Migration-on-touch, một module/commit, move sau characterization | Revert riêng move/split commit |
| Route/authorization thay đổi vô ý khi split controller | High | Route+verb+policy manifest trước B2 | Revert controller slice; giữ tests |
| Generic write có hidden consumer | High | Consumer search + frontend client ledger + deprecation/parity window | Giữ legacy adapter; không delete |
| EF mapping drift giữa hai DbContext | High | No merge mặc định; pending-model + LocalDB parity | Revert mapping extraction; không migration |
| Comment quá nhiều làm code “giống bài giảng” | Medium | Comment why-only; thesis mapping ở guide | Xóa comment noise trong same slice |
| Analyzer bật quá mạnh tạo noisy diff | Medium | Report → warning → error theo ratchet | Hạ severity/revert config commit |
| Refactor lẫn bug/feature | High | “Two hats”: refactor và behavior change là task/commit riêng | Revert refactor; mở bug task riêng |
| User-owned dirty files bị stage nhầm | High | Stage exact path; review staged diff; không `git add .` | Unstage scoped paths, không reset user work |

Không cần database restore **chỉ khi** đã chứng minh zero schema/data mutation. Code/config/resource
rollback vẫn áp dụng cho `prices.txt`, SQL/resource packaging, DbContext mapping và log path; các slice
này phải fresh migrate + reseed/reset disposable LocalDB, kiểm output resources và revert riêng
implementation nếu parity fail. Nếu một wave cần migration thật, record này dừng tại boundary đó;
recovery chuyển sang DB execution record có backup/restore/forward-correction rõ.

## 12. Thứ tự ba công việc

### Current sequencing — refreshed 2026-08-04

Frontend structural refactor đã đi qua FR0–FR6, FR7 structural ownership và FR8A/FR8B cleanup. Thứ tự
portfolio hiện hành là:

1. owner đã chốt correction UX A: sau confirm/correct phải bấm `Xem trước lại`;
2. mutation E2E hai user cho confirm/correct/four-eyes và post-success fresh-preview gate đã pass;
3. FR8C route/docs + technical runtime board đã hoàn tất; owner còn phải duyệt visual board và chốt
   golden/screenshot cuối;
4. B0R test/doc characterization đã hoàn tất; sau owner chốt UI + hai authorization decision,
   probe/đo lại capacity rồi mới thực thi B1→B8 theo từng slice nhỏ;
5. đồng bộ code-reading guide/luận văn và hoàn thiện slide; xử lý raw English period reason ở boundary
   backend/localization riêng, không trộn vào B0R characterization.

B0R chỉ thêm test/docs/comment-only và không move/xóa production backend trước UI final acceptance.
Nếu UI correction làm đổi Shared/API contract ngoài dự kiến, backend plan phải refresh dependency
boundary thêm một lần trước B1.

<a id="plan-detail-decisions"></a>

## 13. Decisions đã chốt

| ID | Status | Decision | Authority |
|---|---|---|---|
| BR-D1 | APPROVED/REPO AUTHORITY | Giữ modular monolith/4 project; không rewrite Clean Architecture hoặc microservices | `src/Backend/AGENTS.md`, `ARCH-001` |
| BR-D2 | APPROVED/REPO AUTHORITY | Identifier English; comment tiếng Việt why-only; mapping luận văn nằm trong reading guide | Root/backend `AGENTS.md` |
| BR-D3 | SUPERSEDED 2026-08-02 | Dùng sequencing hiện hành ở mục 12 và `FRONTEND-REFACTOR-001` | Owner sequencing update |
| B0R-D1 | PENDING OWNER | Pending filter dùng `APPROVE OR REJECT` như pending grid | B0R ledger mục 5 |
| B0R-D2 | PENDING OWNER | History dùng authenticated + resource scope như detail/PDF/XLSX | B0R ledger mục 5 |

Không còn backend architecture decision pending; còn hai authorization behavior decision ở B0R-D1/D2.
UI final acceptance vẫn là dependency gate của portfolio.

<a id="plan-detail-continuation"></a>

## 14. Continuation note

- Current status: frontend correction A, mutation E2E và technical runtime board đã pass. B0R test/doc
  characterization đã hoàn tất; production B1 chờ UI acceptance và B0R-D1/D2.
- Backend slice base: `codex/ai-agent-foundation` @ `7ef42ac6`.
- Pre-existing dirty files outside this task: `.agents/skills/gtas-vpp-ui-system/*`, `AGENTS.md`,
  `LVTN/NguyenAnNam_DH52201078.docx`, `docs/ai/*`, `docs/planning/05-EXECUTION-TEMPLATE.md`,
  `scripts/ai/Test-AgentSetup.ps1`, `src/Frontend/Blazor/wwwroot/css/vpp-polish.css`,
  `tests/Frontend.UiTests/Tests/Order/ProductCatalogTests.cs` và hai text extraction untracked.
- Last completed evidence: focused behavior `83/83`, backend unit `520/520`; integration default 14 pass/6
  skip, disposable LocalDB 20/20 từ HTTP/RBAC slice và final full backend verify PASS. Mỗi production
  wave vẫn phải rerun gate trên HEAD của chính wave trước khi gọi PASS.
- Next exact backend action: owner chốt UI visual board + B0R-D1/B0R-D2; B1a-1 execution card đã khóa
  exact deletion scope trong B0R ledger. Sau gate, probe/đo lại rồi mới triển khai. Raw English
  `CanCreateOrderReason` là localization backlog cần phân loại ở B0R, không tự sửa trong frontend.
- Do not redo: role-count hardcode fix, model-routing-eval fix, source inventory refresh và dead-code
  usage scan; chỉ refresh lại nếu HEAD/backend dependency đã đổi trước B0R.
- Do not touch: UI-SYSTEM-001 source, frontend, Shared wire shape, migration history hoặc user-owned
  dirty files trong B0R/B1.

## 15. Research sources

- [OpenAI — Upgrading to GPT-5.6 Sol](https://developers.openai.com/api/docs/guides/upgrading-to-gpt-5p6-sol.md)
- [OpenAI — Prompt guidance for GPT-5.6](https://developers.openai.com/api/docs/guides/prompt-guidance-gpt-5p6.md)
- [Microsoft — Common web application architectures](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [Microsoft — Architectural principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
- [Microsoft — Develop ASP.NET Core MVC apps, controllers and feature organization](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/develop-asp-net-core-mvc-apps)
- [Microsoft — C# identifier naming conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)
- [Microsoft — Common C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Microsoft — .NET code analysis](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)
- [Microsoft — `dotnet format`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)
- [Microsoft — Unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Microsoft — EF Core testing strategy](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy)
- [Martin Fowler — Refactoring](https://www.refactoring.com/)
- [Martin Fowler — YAGNI](https://martinfowler.com/bliki/Yagni.html)
