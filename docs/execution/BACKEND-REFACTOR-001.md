# BACKEND-REFACTOR-001 — Backend dễ đọc, dễ trình bày và dễ bảo trì

- Status: B0R + B1 COMPLETE — REFRESHED FOR STAGED OLD/NEW REFACTOR — AWAITING OWNER APPROVAL
- Priority: P1
- Path: STANDARD — behavior-preserving modular refactor
- Owner: Nguyễn An Nam
- Planning agent: Codex
- Branch: `Nam`
- Base commit: `46560f6020824cbea9e02bcb8bb06131f1efd501`
- Planned at: `2026-07-29T05:17:46+07:00`
- Refreshed against: `8b651bb3` at `2026-08-13`
- Related authority: `AGENTS.md`, `src/Backend/AGENTS.md`,
  `docs/architecture/ARCH-001-MODULE-MAP.md`,
  [`BACKEND-REFACTOR-001-B0R-LEDGER.md`](./BACKEND-REFACTOR-001-B0R-LEDGER.md)
- Related sequencing: `docs/execution/FRONTEND-REFACTOR-001.md`
- Supersedes: phần **R-1 backend** và quy ước comment backend trong
  `docs/execution/REFACTOR-001.md`; lịch sử R-0/R-2 của record cũ vẫn giữ nguyên
- User approval: B0R-D1/B0R-D2 và toàn bộ B1a/B1b-A/B đã hoàn tất theo quyết định trước. Ngày
  2026-08-13 owner chọn hướng mới: refactor vùng cũ độc lập trước, giữ nguyên vùng chức năng mới để
  kiểm tra thực tế, sau đó mới refactor vùng mới. Execution sequencing mới trong record này đang chờ
  owner duyệt trước khi sửa production code.

<a id="plan-overview"></a>

## 0. Bản một ánh nhìn

| Mục | Tóm tắt dễ hiểu | Chi tiết |
|---|---|---|
| Kết quả cần đạt | Backend vẫn chạy y như hiện tại nhưng người mới có thể lần từ API → use case → database nhanh, tên dễ hiểu, class có trách nhiệm rõ, không còn rác đã chứng minh | [Objective](#plan-detail-objective) |
| Phạm vi | `src/Backend`, backend tests và tài liệu đọc code; không redesign UI, không đổi API/JSON/quyền/nghiệp vụ/schema | [Scope](#plan-detail-scope) |
| Phương án | Giữ modular monolith và 4 project hiện tại; tổ chức dần theo module `IdentityAccess`, `CatalogPricing`, `Requests`, `Settlement`, `Reports`, `Notifications`, `Platform`; không big-bang rewrite | [Target structure](#plan-detail-target-structure) |
| Các bước chính | Đã xong B0R/B1 → khóa ranh giới cũ/mới/giao nhau → refactor **Reports cũ, chỉ đọc** → owner kiểm tra chức năng mới → sửa bug nghiệp vụ riêng → refactor Catalog/Pricing → Period/Requests → Settlement/Correction → Identity/Platform/Persistence | [Waves](#plan-detail-waves) |
| Comment/naming | Identifier English, ưu tiên từ đầy đủ và từ vựng nghiệp vụ; comment tiếng Việt ngắn chỉ giải thích **vì sao/ràng buộc**, không mặc định gắn số mục luận văn vào source | [Readability contract](#plan-detail-readability) |
| Model/quota routing | Snapshot 13/08 còn `1243%` weekly aggregate, 13/14 account khả dụng nhưng thiếu coverage 5 giờ. Dùng `gpt-5.6-terra` high cho lát refactor rõ contract, `gpt-5.6-sol` high/xhigh cho boundary/review; kết luận hiện tại `SLICE_ONLY`, đủ mở một checkpoint sau khi duyệt, chưa cam kết chạy liền toàn plan | [Routing](#plan-detail-routing) |
| Kiểm tra | Mỗi checkpoint khóa route/permission/JSON trước, chạy focused test trong vòng lặp và full backend gate trước commit. Chức năng mới chỉ chuyển từ `FROZEN` sang `ACCEPTED` sau checklist thực tế của owner và regression test tương ứng | [Verification](#plan-detail-verification) |
| Rủi ro chính | Gọi code là “cũ” nhưng vẫn dùng chung period/settlement/pricing mới; refactor vô tình hợp thức hóa bug chưa nghiệm thu; `VPPContext`, Shared DTO, seed và `Program.cs` gây ảnh hưởng xuyên module | [Risks](#plan-detail-risks) |
| Bước tiếp theo | B2F–B2C Reports đang được thực thi liên tục theo owner approval; sau khi gate xanh sẽ rà vùng cũ độc lập tiếp theo. Không chạm Period, Requests, Pricing import/AI, Settlement/Correction hoặc schema trước vòng kiểm tra chức năng mới | [Continuation](#plan-detail-continuation) |

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
| Backend unit trên B0R characterization | PASS — 520/520 tại baseline; 525/525 sau authorization ratchet |
| Backend integration default | PASS — 14 pass, 6 skip có điều kiện; kết quả này chưa đủ chứng minh SQL Server behavior |
| Backend integration với `GTAS_QA_SQL_INTEGRATION=1` | PASS — 20 pass, 0 skip trên harness-owned disposable LocalDB; fixture đã derive số role từ `CanonicalRbac.Personas` |
| EF pending-model check | PASS — không có thay đổi model sau migration gần nhất |
| `dotnet format whitespace ... --verify-no-changes --include src/Backend` | PASS qua solution-wide `dotnet format --verify-no-changes` trong full backend verify |
| `dotnet format analyzers ... --severity warn --verify-no-changes --include src/Backend` | PASS qua solution-wide `dotnet format --verify-no-changes` trong full backend verify |
| Current project graph | Khớp `ARCH-001-MODULE-MAP.md` |
| `./scripts/ai/Test-AgentSetup.ps1` | PASS — 63/63; blocker `model-routing-eval` cũ đã được sửa |
| `./scripts/gtas.cmd verify -Scope backend` | PASS — build 0 warning/error, agent setup 63/63, unit 525/525, integration portable 14 pass/6 conditional skip, EF zero delta, vulnerability/leak audit pass |

Số test chỉ là snapshot ngày refresh, không phải invariant lâu dài.

### Hotspots đã đo

| File | Dòng xấp xỉ | Vấn đề đọc hiểu chính |
|---|---:|---|
| `Application/Services/Requests/VPPRequestService.cs` | 2.557 | Query, create/update/cancel, supplement approval, period logic, mapping và helper nằm chung |
| `Application/Services/Seeding/DemoWorkbookSeeder.cs` | 1.475 | Parse, validate, reconcile và write demo data trong một file |
| `Application/Services/Settlement/PeriodSettlementService.cs` | 1.311 | Preview, confirm/correct, revision query, legacy settlement và snapshot logic cùng class |
| `Api/Controllers/VPPRequestController.cs` | 1.194 | Transport, filter/query, export, notification và dashboard query bị trộn; còn direct EF cho dashboard |
| `Api/Controllers/LibraryController.cs` | 1.168 | Typed query tồn tại song song generic CRUD/PATCH và direct `VPPContext` access |
| `Api/Controllers/PermissionController.cs` | 1.090 | Permission/user query, mapping và transaction qua `IUnitOfWork.VPPContext` trong controller |
| `Api/Authorization/AccountLifecycleService.cs` | 1.028 | Account lifecycle và security branching là hotspot Identity/Access mới |
| `Application/Services/Seeding/SeedData.cs` | 960 | Reference seed, demo seed, SQL script, prices-file parsing và validation |
| `Application/Services/AuthBootstrap/AuthBootstrapProvisioner.cs` | 847 | Bootstrap, mapping và reconciliation cần owner module rõ |
| `Api/Authorization/MembershipAdministrationService.cs` | 845 | Membership mutation và authorization logic cần characterization security |
| `Api/Program.cs` | 816 | Logging, config validation, DI, auth, middleware, migration/seed runner và deployment guards |

Số dòng chỉ là **tín hiệu hotspot**, không phải tiêu chí tự động buộc tách class.

### Cleanup evidence đã xác nhận

- B1a-1 đã xóa năm private method `UpdateOrderLegacyAsync`, `CancelOrderLegacyAsync`,
  `ApproveAdditionalOrderLegacyAsync`, `RejectAdditionalOrderLegacyAsync`,
  `GetCurrentPeriodInfoLegacyAsync` sau khi repo-wide search xác nhận declaration-only.
- `ObjectHelpers` và `PasswordHelpers` đã bị xóa; repo-wide search sau delete không còn type/method handle.
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
- `BaseServices`/`IBaseServices` đã được characterize, detach và xóa cùng factory/resolver/Jira chain tại
  B1a-2; `IUnitOfWork`, dynamic context factory, username resolver và HTTP context accessor vẫn được giữ.
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

### Ranh giới thực thi sau khi đã thêm chức năng mới

Không chia máy móc theo ngày tạo file. Một file chỉ được xem là **vùng cũ an toàn** khi dependency scan và
test chứng minh nó không ghi hoặc điều khiển lifecycle mới.

| Nhóm | Trạng thái hiện tại | Phạm vi | Quy tắc trước owner acceptance |
|---|---|---|---|
| **Ổn định cũ** | `READY_AFTER_FREEZE` | Reports query/export/insight hiện hành; module Notifications chỉ audit, chưa move | Được refactor nếu giữ nguyên route, scope, DTO, bytes export và không extract/move logic Settlement mới |
| **Chức năng mới** | `FROZEN` | rolling order periods; sửa/gia hạn/đóng/mở kỳ; post-close adjustment window; settlement reopen guard; hiệu chỉnh đơn sau chốt; import bảng giá; AI gợi ý mapping cột | Chỉ sửa bug owner phát hiện hoặc test bảo vệ; không rename/move/split/generalize trước acceptance |
| **Vùng giao nhau** | `FROZEN` | `VPPRequestService`, `VPPRequestController`, `VppPeriodService`, `PeriodSettlementService`, `ReportService` phần đọc `Settlement`, `LibraryController`, `VPPContext`, `Program.cs`, Shared VPP/Library DTO và seed/demo | Không refactor trong wave cũ. Nếu B2 Reports cần chạm, giữ đoạn integration tại chỗ và coi đó là compatibility adapter |
| **Nền tảng rủi ro cao** | `DEFERRED` | `PermissionController`, generic repository/controller, auth/bootstrap, hai DbContext, migration/model snapshot, database initialization | Làm sau các module nghiệp vụ; thay đổi DB phải chuyển sang task dùng skill DB safety |

`FROZEN` không có nghĩa code mới đã đúng. Nó có nghĩa hành vi đang chờ owner kiểm tra nên refactor không
được dùng để đổi cấu trúc hoặc “làm đẹp” phần đó. Bug fix và refactor luôn là hai change-set riêng.

#### Acceptance gate để mở khóa vùng mới

Owner kiểm tra theo luồng thật; agent ghi lại từng kết quả `PASS`, `BUG`, `COPY/UI ONLY` hoặc
`BUSINESS DECISION`. Tối thiểu gồm:

1. **Kỳ đặt hàng:** tự mở nhiều kỳ, chọn kỳ đặt, sửa/gia hạn lịch, đóng/mở lại đúng quyền và đúng mốc thời gian.
2. **Chốt kỳ:** chọn NCC/bảng giá, tổng tiền trước VAT/VAT/tổng giá trị, xem theo phòng ban/người đặt/mặt hàng,
   chốt sớm và thời gian hiệu chỉnh sau đóng kỳ.
3. **Sau chốt:** xem bản đã lưu, tạo hiệu chỉnh, quy tắc hai quản lý, không cho sửa trực tiếp dữ liệu đã chốt.
4. **Bảng giá/import:** tạo bảng giá có hiệu lực, preview file, mapping cột thủ công/AI, confirm import,
   duplicate/error handling và lịch sử import.
5. **Regression:** đơn thường, đơn bổ sung, phân quyền và báo cáo/export vẫn hoạt động với kỳ đã chốt.

Sau khi owner xác nhận, lỗi được sửa trong task feature riêng và focused tests xanh, module tương ứng mới
chuyển sang `ACCEPTED_FOR_REFACTOR`. Không yêu cầu mọi chức năng mới phải được mở khóa cùng lúc.

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
| Năm private `*LegacyAsync` trong `VPPRequestService` | DELETED 2026-08-04 | B1a-1 đã xóa đúng scope | Focused `119/119`; backend unit `525/525`; full backend verify PASS |
| `ObjectHelpers.cs`, `PasswordHelpers.cs` | DELETED 2026-08-04 | B1a-1 đã xóa hai file zero-consumer | Repo-wide search 0 handle; build/integration/format/audit PASS |
| `TransitionStatus`, `GetCurrentAndPreviousPeriod`, service `IsDeadlinePassed`, `OrderStateMachine` closure | DELETED 2026-08-04 | B1a-1b đã xóa 3 helper, state-machine file/test và 2 reflection test; giữ public `PeriodCalculator.IsDeadlinePassed` để audit riêng | Search 0; focused `91/91`; backend `503/503`; full verify + independent review PASS |
| `Api/gtas_vpp_be.http` | REPLACED 2026-08-04 | Health + authenticated read-only request smoke, chỉ placeholder, không mutation | GET-only/no-secret scan; manifest `1/1`; API build PASS |
| `Api/readme.md` | DELETED 2026-08-04 | Root `scripts/gtas.cmd`/backend instructions đã là command authority; stale local readme đã xóa | Documentation link check + backend verify PASS |
| `Api/.gitignore`, `Api/.gitattributes` | DELETED 2026-08-04 | Root metadata cover đầy đủ; nested duplicates đã xóa | 195 ignore rule covered; `check-ignore`/attrs PASS |
| `Api/logs`, `bin`, `obj`, `*.csproj.user` | LOCAL_CLEANUP | B1b xóa local ignored artifact sau khi xác minh lock; không đổi production log behavior trong cleanup slice | Xác minh process owner/lock trước cleanup |
| Relative Serilog path `logs/log-.txt` | PLATFORM_CHANGE | B7 đưa sang configuration có Development/migrator/production default rõ; không gộp với dead-code cleanup | Deployment configuration tests, container-volume/log smoke, rollback config |
| Ghost csproj include `.github/copilot-instructions.md` | DELETED 2026-08-04 | Đã bỏ item target không tồn tại | XML parse, API build và full backend verify PASS |
| Dead default-price seed cluster + `prices.txt` | DELETE_CANDIDATE/NEEDS_AUDIT | Xóa private cluster sau characterization; chỉ xóa file/content include khi resource ledger xác nhận 0 runtime consumer | Seed/reference tests + build output inspection |
| EF `.Designer.cs`, snapshot, applied migrations | KEEP | Không coi là rác | DB safety skill + explicit migration task nếu cần |
| Poppins fonts, demo TSV, SQL scripts | KEEP/NEEDS_AUDIT | Giữ vì có csproj/runtime consumer; chỉ thay khi có replacement verified | Build output/resource/seed tests |
| `GenericRepository`, `BaseGenericController`, generic Library writes | MIGRATE_ON_TOUCH | Không xóa big-bang; typed path + consumer cutover từng module | Route/consumer/authorization/parity tests |
| `VPPRequestService : BaseServices` và extra UoW activation | DETACHED 2026-08-04 | B1a-2b1 đã bỏ inheritance/base constructor và sáu dependency; request service chỉ giữ dependency thực sự dùng | Focused `72/72`; backend `506/506`; full verify + independent review PASS |
| `BaseServices`/`IBaseServices` và factory/resolver/Jira chain | DELETED 2026-08-04 | B1a-2b2 đã xóa declaration, registration, config và helper/safety tests. Giữ `IDynamicDbContextFactory`, `IUnitOfWork`, `IUserNameResolver`, `AddHttpContextAccessor` | Consumer scan 0; focused `41/41`; backend `503/503`; full verify + review PASS |
| Hai DbContext có mapping trùng | NEEDS_AUDIT | Document trước; không merge ở B1 | EF no-pending-model + LocalDB parity |

<a id="plan-detail-routing"></a>

## 7. Model, effort và quota routing

### Refresh 2026-08-13

- Probe local force-refresh lúc `2026-08-13T00:17:00Z`: `14` account có weekly coverage, `13` khả dụng,
  aggregate còn `1243% = 12.43 account-equivalents`; coverage chỉ là weekly, thiếu hoặc chưa hoàn chỉnh cửa
  sổ 5 giờ. Đây là snapshot volatile, không phải repository invariant.
- Capacity hiện đủ để lập plan và mở **một checkpoint độc lập** sau owner approval. Vì thiếu short-window
  coverage và lịch sử chi phí dao động, không cam kết chạy liên tục B2→B8. Kết luận là `SLICE_ONLY`;
  force-refresh và đo lại trước checkpoint kế tiếp.
- Theo OpenAI Docs hiện hành, `gpt-5.6-sol` phù hợp công việc frontier/architecture còn
  `gpt-5.6-terra` cân bằng chất lượng và chi phí; reasoning `high`/`xhigh` chỉ dùng khi độ khó và gate đo được
  cần. Plan này dùng Terra high cho implementation rõ contract, Sol high review B2 và Sol xhigh cho
  Requests/Settlement/security hoặc final cross-module review.

### Lịch sử routing trước refresh

- Owner đã cho phép bật local `codex-quota-scheduler`; config chỉ đổi flag plugin, có backup hash và
  hot-reload thành công nên không restart gateway. Bốn stable quota scripts được refresh từ bản bundled;
  sau đó measurement wrapper được sửa named-argument splatting và parser/smoke đều PASS.
- Snapshot/coverage/account state là dữ liệu volatile local, không phải repository invariant. Probe hiện
  trả schema v2; short-window coverage vẫn thiếu nên không tuyên bố đủ cho toàn B1a-2→B8.
- Official model resolver hiện chọn `gpt-5.6-sol`. Theo guidance chính thức, không dùng model mạnh nhất
  cho mọi lát: Sol dành cho architecture, security/business ambiguity và final review; Terra dành cho
  implementation cơ học, rõ contract và high-volume.
- B1a-1b checkpoint dùng `gpt-5.6-terra` high implement và `gpt-5.6-sol` xhigh review; aggregate pool
  delta đo được `12%`. Số này gồm toàn checkpoint và có thể chịu concurrent-pool noise, không phải billing
  hay attribution chính xác cho một model.
- B1a-2a test-only checkpoint đo `38%` aggregate, vượt forecast 4–15% và upper bound 30% sau buffer 100%.
  Forecast miss này có thể gồm reasoning/review/full-verify và concurrent-pool noise, nhưng đủ để bác bỏ
  estimate cũ thay vì rationalize sau sự kiện.
- B1a-2b1 detach checkpoint đo `34%` aggregate, nằm trong forecast rộng 25–65%; full verify và review đều
  pass. Sau b1 có ba mẫu aggregate `12%`, `38%`, `34%`; short-window coverage vẫn thiếu và history còn
  ít. Kết luận cho B1a-2b2 là `REFORECAST / SLICE_ONLY` hoặc `WAIT` theo live budget gate; không tự hạ
  model/effort và không tuyên bố `ENOUGH` cho full plan.
- B1a-2b2 orphan cleanup đo `21%` aggregate, thấp hơn forecast 25–55%; full verify và review đều pass.
  Sau B1a có bốn mẫu `12%`, `38%`, `34%`, `21%`; vẫn phải force-refresh và reforecast trước B1b-A vì
  short-window coverage thiếu và capacity đã giảm đáng kể.
- B1b-A metadata checkpoint dùng `gpt-5.6-terra` medium và đo `34%` aggregate, cao hơn estimate 8–25%
  nhưng trong buffer 50%; full backend verify pass. B1b-B tiếp tục phải force-refresh riêng, không dùng
  metadata size để suy luận chi phí giả tạo.
- B1b-B `.http` checkpoint đo `4%` aggregate; route/secret/build gates pass. Post-gate forced refresh vẫn
  chỉ có weekly coverage và không đủ safe buffered bound cho B2 Reports pilot. Kết luận hiện tại là
  `WAIT`, không tự hạ model/effort hoặc mở một slice không nằm trong safe plan.
- B2R read-only audit chạy trên `cb5fe163`: snapshot force-refresh tại `2026-08-04T03:11:49Z` còn
  `61%` weekly aggregate, chỉ `2/13` account khả dụng và thiếu cửa sổ 5 giờ cho toàn bộ account. Đây là
  execution snapshot có thể thay đổi, không phải repository invariant; coverage hiện tại không đủ để
  tuyên bố một B2 production checkpoint có safe buffer.

`Khuyến nghị routing` không có nghĩa model của root task đã tự đổi.

<a id="plan-detail-waves"></a>

## 8. Execution waves

| Wave | Outcome | Scope chính | Model + effort khuyến nghị | Cost forecast | Review/handoff gate |
|---|---|---|---|---:|---|
| **B0R — Refresh contract & reading baseline** | Khóa HEAD hiện tại trước khi move/split; không làm lại role-count fix đã hoàn tất | Refresh inventory/analyzer/test gates; assert trực tiếp legacy procurement role inactive + membership/mapping reconciliation; manifest route+verb+policy và representative status/error/serialization/export; DI/resource smoke; generic endpoint consumer ledger; backend module/file map trong code-reading guide | `gpt-5.6-terra` high, review `gpt-5.6-sol` high | 2–5% | All current unit/integration gates xanh; all opt-in LocalDB tests pass, 0 skip/fail; EF zero delta; full verify xanh |
| **B1a-1 — COMPLETE 2026-08-04** | Production slice đầu tiên dễ review/rollback | Đã xóa 5 request legacy methods + `ObjectHelpers` + `PasswordHelpers`; không đổi API, DTO, schema, DI hay nghiệp vụ | `gpt-5.6-terra` medium/high, review `sol` high | 1–3% heuristic | Usage proof 0; focused `119/119`; backend `525/525`; full verify PASS |
| **B1a-1b — COMPLETE 2026-08-04** | Dọn helper closure do B1a-1 để lại mà không kéo public API khác theo | Đã xóa 3 private helper; `OrderStateMachine`/`OrderAction` + test; 2 reflection test + helper. Giữ `PeriodCalculator.IsDeadlinePassed` | `gpt-5.6-terra` high, review `gpt-5.6-sol` xhigh | Actual aggregate checkpoint `12%` | Search 0; focused `91/91`; backend `503/503`; full verify + independent review PASS |
| **B1a-2a — COMPLETE 2026-08-04** | Khóa đúng construction/UoW/DI trước khi xóa inheritance | Đã thêm 3 construction tests chứng minh `_scopedUow` là active path, base factory tạo extra lazy UoW và request DI contract resolve strict scopes | `gpt-5.6-terra` high, review `sol` xhigh | Actual aggregate checkpoint `38%`; prior forecast rejected | Class `3/3`; focused `231/231`; backend `506/506`; full verify + review PASS |
| **B1a-2b1 — COMPLETE 2026-08-04** | Detach request service và loại extra UnitOfWork activation | Đã bỏ inheritance/base constructor và sáu dependency; cập nhật tám constructor test sites; giữ legacy registrations cho rollback độc lập | `gpt-5.6-terra` high, review `sol` xhigh | Actual aggregate checkpoint `34%` | Focused `72/72`; backend `506/506`; full verify + review PASS |
| **B1a-2b2 — COMPLETE 2026-08-04** | Xóa legacy code/config sau khi request service không còn consumer | Đã xóa BaseServices/factory/resolver/Jira chain; cập nhật Program/appsettings/helper/routing tests; giữ framework runtime dependencies | `gpt-5.6-terra` high, review `sol` xhigh | Actual aggregate checkpoint `21%` | Consumer search 0; focused `41/41`; backend `503/503`; full verify + review PASS |
| **B1b-A — COMPLETE 2026-08-04** | Repo metadata không còn duplicate/ghost/stale command file | Đã xóa nested Git metadata, stale API readme và ghost csproj include | `gpt-5.6-terra` medium | Actual aggregate checkpoint `34%` | Ignore/attr/XML/API build; backend `503/503`; full verify PASS |
| **B1b-B — COMPLETE 2026-08-04** | `.http` dùng route hiện hành, không credential/mutation | Health + authenticated read-only examples với bearer/order placeholders; không đụng local artifacts | `gpt-5.6-terra` medium | Actual aggregate checkpoint `4%` | GET-only/no-secret scan; manifest `1/1`; API build PASS |
| **B2 — Reports cũ, chỉ đọc** (`COMPLETE 2026-08-13`) | Chốt pattern module ở vùng ít mutation nhất mà không thay đổi nghiệp vụ mới | B2F khóa validation/current revision/scope; B2A có `ReportQueryContext`, settlement reader và CSV builder; B2B có `Api/Features/Reports` + `AddReportsModule`; B2C đã handoff vào code-reading guide | `gpt-5.6-terra` high implement, review `gpt-5.6-sol` high | Chưa có đo aggregate riêng đáng tin cậy | Focused 51/51; backend 543/543; frontend client 6/6; disposable SQL 24/24; EF zero delta; formatter/analyzer scoped PASS. Full verify chỉ còn blocker encoding migration import ngoài B2 |
| **UAT-N — Owner kiểm tra chức năng mới** | Xác nhận hành vi thật trước khi refactor module mới | Period, chốt kỳ, correction sau chốt, price-list import và AI mapping; phân loại `PASS/BUG/UI/BUSINESS DECISION` | Owner chạy luồng thật; agent dùng `gpt-5.6-sol` high khi phân tích bug khó | Không tính như production refactor | Checklist acceptance hoàn tất theo từng module; bug fix commit riêng và regression test xanh |
| **B3 — Catalog & Pricing** (`FROZEN` đến khi pricing accepted) | Typed read/write/import paths rõ, thu nhỏ `LibraryController` | Catalog query, price-list lifecycle, price resolver, import parser/workflow, AI mapping boundary; retire generic writes từng consumer | `gpt-5.6-sol` high design/review, `terra` high implement | Reforecast sau UAT | Consumer ledger 0; price/import/AI fallback tests; LocalDB parity; permission parity |
| **B4 — Period & Requests** (`FROZEN` đến khi period/request accepted) | Tách lifecycle kỳ khỏi god request service mà giữ nguyên toàn bộ luồng đơn | Period query/schedule/lifecycle trước; sau đó request query/history, create-update-cancel, supplement workflow, demand và notification boundary | `gpt-5.6-sol` xhigh plan/review, `terra` high implement | Reforecast sau UAT | Schedule/time boundary, idempotency/concurrency/revision/history, route/auth/JSON và focused LocalDB |
| **B5 — Settlement & post-settlement correction** (`FROZEN` đến khi settlement accepted) | Preview/confirm/revision/correction rõ theo use case | `PeriodSettlementService`, snapshot pricing/evidence, correction hai quản lý, notification và revision query; không mở lại nghiệp vụ đã bị owner loại | `gpt-5.6-sol` xhigh, `terra` high implement | Reforecast sau UAT | Snapshot/hash/idempotency/four-eyes/VAT/revision tests; SQL Server integration; no schema delta |
| **B6 — Identity & Access** | Security logic có ownership rõ, controller không query context | `PermissionController`, `AccountLifecycleService`, `MembershipAdministrationService`, `AuthBootstrapProvisioner`; giữ `Api/Authorization` là owner hiện hữu cho đến khi move thật sự cải thiện navigation, không mass-move để khớp cây mẫu | `gpt-5.6-sol` xhigh, `terra` high implement | 10–25% | Role/action/scope matrix; direct API 401/403; session invalidation; bootstrap/reconciliation; no secret/log regression |
| **B7 — Platform, composition & operations** | `Program.cs`/hosting/logging dễ đọc mà không đổi deployment behavior | Mỗi B2–B6 đã sở hữu `Add<Module>()`; B7 chỉ compose, tách DB init runner, cấu hình log path với Development/migrator/production contract, worker/notification/platform cleanup | `gpt-5.6-sol` high/xhigh | 5–12% | Composition-root smoke; deployment/log-volume contract; worker tests; all module registrations resolve |
| **B8 — Persistence mapping & final review** | Giảm mapping drift hoặc ghi nhận waiver có bằng chứng; hoàn tất guide/adapters | DB-safety checkpoint riêng cho shared `IEntityTypeConfiguration<T>` nếu zero-delta; xóa adapter hết consumer; namespace/folder cleanup; final code-reading review | `gpt-5.6-sol` xhigh | 5–12% | All current opt-in LocalDB tests pass, 0 skip/fail; fresh migrate/reseed/reset; EF no delta; full backend verify; route/wire/dependency manifest |

Chỉ đổi model ở ranh giới wave/checkpoint; một implementer chính giữ context, reviewer/subagent chỉ
audit hoặc verify độc lập.

### B2 Reports execution card — refreshed 2026-08-13

#### Bằng chứng hiện tại

- `ReportsController` đã là controller transport tương đối mỏng với đúng `5` GET endpoint. MVC manifest
  đã khóa route/verb/effective authorization; controller tests đã khóa missing identity `401`,
  invalid/denied scope `403`, claim-derived user/department/company, insight rate limit và ba file result.
- `ExceptionHandlingMiddlewareTests` đã khóa ProblemDetails `400` cho `ArgumentException`,
  `ArgumentOutOfRangeException` và `InvalidOperationException`; frontend `ReportsApiClientTests` đã khóa
  query encoding cùng mapping `summary`, `insights`, CSV, XLSX và PDF. Vì vậy B2 **không** thêm lại các
  test route/policy/status này chỉ để tăng số lượng test.
- Audit validation trên baseline: backend focused `41/41`, frontend Reports client `6/6`, không fail/skip.
- Hotspot thật là `ReportService` (`521` dòng): bốn method lặp sáu primitive parameter, tên `Code` không
  nói rõ nghĩa, scope/filter setup bị lặp, và current-settlement query/allocation mapping bị lặp giữa
  summary với workbook. CSV projection/building cũng nằm chung với query orchestration.
- `ReportWorkbookBuilder`, `ReportPdfBuilder` và provider stack `ReportInsights/` đã có trách nhiệm riêng;
  B2 không redesign AI provider hoặc đổi dependency/model. `SimpleWorkbookBuilder` và
  `ExportFileContract` có consumer ngoài Reports nên thuộc shared file platform, không chuyển vào module.
- `ReportService` hiện đọc settlement snapshot để tính số liệu đã chốt. Đây là **vùng giao nhau**: B2 được
  phép thêm characterization và tạo typed internal reader nếu chứng minh zero behavior delta, nhưng không
  đổi allocation, pricing, revision selection hay settlement semantics trước owner acceptance.

#### B2F — freeze map và characterization gap

1. Lập dependency map cho request rows, settlement snapshot, exports và insights; đánh dấu điểm giao nhau.
2. Thêm direct service tests cho validation matrix: invalid scope, thiếu company, thiếu department khi
   scope department, year/month ngoài khoảng.
3. Khóa current-revision selection khi đồng thời có revision cũ và current revision.
4. Khóa workbook allocation theo `own`/`department` để việc tái sử dụng settlement reader không làm rò
   dữ liệu khác scope.
5. Giữ `MaxExportRows = 50_000`; chỉ thêm test row-limit nếu fixture bounded không làm suite chậm hoặc
   tốn bộ nhớ bất hợp lý. Nếu chưa có test phù hợp, guard này là explicit review gate và không được move
   hoặc đổi trong B2A.

Gate: focused `ReportServiceTests`, `ReportsControllerTests`, `ExceptionHandlingMiddlewareTests`,
`BackendHttpContractManifestTests` và frontend `ReportsApiClientTests`; không sửa production ở checkpoint
test/docs-only này. Không move hoặc sửa production code tại B2F.

#### B2A — readability seam an toàn

1. Đổi internal parameter `Code` thành `departmentCode`; repo search hiện không có named-argument consumer.
2. Tạo immutable `ReportQueryContext` (scope, server-derived user/department/company, year, month) để bốn
   operation không chuyền sáu primitive rời. Comment tiếng Việt tối đa một câu chỉ nhấn mạnh
   department/company lấy từ authenticated claims, không tin client input.
3. Extract typed current-settlement reader/snapshot chỉ khi characterization chứng minh zero delta. Reader
   thuộc Reports và chỉ đọc; không trở thành settlement workflow mới.
4. Extract CSV projection/encoding thành pure builder nếu diff sau bước 3 vẫn nhỏ và test độc lập rõ.
   `IReportService` có thể giữ vai trò facade để controller/API không đổi.

Gate: output DTO/JSON không đổi; totals và allocation scope parity; CSV BOM/formula safety; XLSX/PDF
signature/metadata; no pending EF model change.

#### B2B — module ownership và composition

- Migration-on-touch các file Reports vào `Api/Features/Reports` và `Application/Reports` sau khi B2A xanh;
  giữ insights dưới `Application/Reports/Insights`.
- Thêm `AddReportsModule` sở hữu `IReportService` và report-insight registrations; `Program.cs` chỉ gọi
  module extension. Không tạo extension một dòng cho từng class.
- Không split `ReportsController` chỉ vì `171` dòng; chỉ tách khi một action có reason-to-change riêng và
  route manifest chứng minh parity.

#### B2C — verify và handoff

- Chạy focused gates, `./scripts/gtas.cmd verify -Scope backend`, EF pending-model, diff review và
  `git diff --check` trên HEAD của wave.
- Cập nhật `docs/CODE-READING-GUIDE.md` bằng luồng Report: route → scope authorization → query context →
  summary/settlement snapshot → export/insight.
- Mỗi checkpoint commit riêng và reforecast quota trước checkpoint kế; không mở B3 cùng change-set.

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
- Hai authorization contract đã được owner duyệt và triển khai: pending grid/filter dùng cùng
  `REQUEST_APPROVE OR REQUEST_REJECT`; history dùng class authentication + `CanViewOrderAsync` giống
  detail/PDF/XLSX. Ratchet test khóa reject-only pending filter và department/company history scope.
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
| Refactor vùng “cũ” nhưng chạm code mới dùng chung | High | Freeze map + dependency scan; vùng giao nhau giữ compatibility adapter | Revert checkpoint B2; giữ characterization tests |
| Biến bug chưa nghiệm thu thành hành vi được refactor hóa | High | Owner acceptance trước B3–B5; bug fix và refactor tách commit | Revert refactor, sửa bug trên baseline trước |
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

## 12. Thứ tự thực thi hiện hành

### Current sequencing — refreshed 2026-08-13

Đây là sequencing được khuyến nghị sau khi codebase đã có thêm rolling periods, correction sau chốt và
price-list import/AI mapping:

1. **B2F Reports freeze/characterization:** test/docs-only, xác định chính xác đoạn Reports giao với
   Settlement mới; không sửa production.
2. **B2A–B2C Reports:** refactor vùng read-only độc lập, commit nhỏ; không đụng rule của kỳ, pricing,
   request mutation hoặc settlement correction.
3. **UAT-N owner acceptance:** owner kiểm tra lần lượt Period → Settlement/Correction → Price import/AI.
   Có thể làm song song về thời gian với B2, nhưng mọi bug được sửa ở task/commit feature riêng.
4. **B3 Catalog/Pricing:** chỉ mở khi checklist pricing/import đạt acceptance; refactor cả import và AI
   mapping như boundary tùy chọn, không đưa AI vào core business rule.
5. **B4 Period & Requests:** refactor period lifecycle trước, sau đó tách request query/command/supplement;
   vì hai vùng đang phụ thuộc trực tiếp nên không tách request cũ trước period acceptance.
6. **B5 Settlement/Correction:** làm sau B3+B4 để dùng boundary pricing/period ổn định; giữ immutable
   settlement revision và four-eyes correction.
7. **B6–B8:** Identity/Access → Platform/composition → Persistence/final review. Không đưa `Program.cs`,
   Shared DTO, generic repository/controller hoặc hai DbContext vào wave sớm.

Phương án này là best practice phù hợp nhất với repository hiện tại, không phải quy tắc tuyệt đối cho mọi
project: nó kết hợp separation of concerns, feature ownership, characterization test và branch-by-abstraction
nhẹ bằng compatibility adapter. Điểm quan trọng là **không refactor code chưa được nghiệm thu**, nhưng vẫn
tận dụng thời gian để xử lý module cũ thực sự độc lập.

<a id="plan-detail-decisions"></a>

## 13. Decisions đã chốt

| ID | Status | Decision | Authority |
|---|---|---|---|
| BR-D1 | APPROVED/REPO AUTHORITY | Giữ modular monolith/4 project; không rewrite Clean Architecture hoặc microservices | `src/Backend/AGENTS.md`, `ARCH-001` |
| BR-D2 | APPROVED/REPO AUTHORITY | Identifier English; comment tiếng Việt why-only; mapping luận văn nằm trong reading guide | Root/backend `AGENTS.md` |
| BR-D3 | SUPERSEDED 2026-08-02 | Dùng sequencing hiện hành ở mục 12 và `FRONTEND-REFACTOR-001` | Owner sequencing update |
| B0R-D1 | APPROVED/IMPLEMENTED 2026-08-04 | Pending filter dùng `APPROVE OR REJECT` như pending grid | Owner phương án A + B0R ledger mục 5 |
| B0R-D2 | APPROVED/IMPLEMENTED 2026-08-04 | History dùng authenticated + resource scope như detail/PDF/XLSX | Owner phương án A + B0R ledger mục 5 |
| B2-D1 | SUPERSEDED 2026-08-13 | Capacity gate cũ đã được refresh; sequencing mới dùng B2F/B2A và freeze vùng mới | Snapshot mới + owner đổi thứ tự |
| BR-D4 | APPROVED/IN FORCE 2026-08-13 | Refactor vùng cũ độc lập trước; chức năng mới `FROZEN` đến khi owner acceptance; vùng giao nhau không refactor sớm | Owner: “làm full plan... phần nào làm trước được thì cứ làm trước” |
| BR-D5 | APPROVED/IN FORCE 2026-08-13 | Bug fix và refactor là change-set riêng; module mới mở khóa từng phần, không cần chờ nghiệm thu toàn hệ thống | Owner yêu cầu chạy liên tục, không chờ từng turn |

B0R/B1 giữ nguyên bằng chứng hoàn tất. BR-D4/BR-D5 đã được owner duyệt; agent tiếp tục checkpoint an toàn
không cần xin duyệt lại từng turn và chỉ dừng ở behavior/API/database boundary thật sự.

<a id="plan-detail-continuation"></a>

## 14. Continuation note

- Current HEAD: branch `Nam` @ `8b651bb3`; preflight backend PASS và worktree sạch trước plan edit.
- B0R/B1 cleanup đã hoàn tất trước đó. Từ sau mốc 04/08, backend đã thêm standalone supplements,
  rolling periods, settlement reopen/correction window, post-settlement correction, price-list import và AI
  column mapping; vì vậy các execution card B3–B5 cũ không còn được chạy nguyên trạng.
- Current hotspot snapshot: `VPPRequestService` khoảng 2570 dòng, `PeriodSettlementService` 1366,
  `VppPeriodService` 999, `LibraryController` 917, `PermissionController` 981 và `ReportService` 482.
  Số dòng chỉ dùng để định hướng, không phải tiêu chí tự động tách class.
- Current execution: B2F characterization commit `d2fd8598`; B2A–B2C Reports hoàn tất. Evidence:
  focused `51/51`, backend unit `543/543`, frontend Reports client `6/6`, disposable SQL `24/24`, EF zero
  delta và scoped whitespace/analyzer PASS. Full verify dừng sau các gate xanh vì migration
  `20260810233259_AddPriceListImportBatches.cs` có lỗi `CHARSET`; file thuộc vùng pricing/import đang
  `FROZEN`, không sửa trong B2. Tiếp theo rà Notifications và pure platform/file seams có test evidence.
- Independent legacy slice sau B2: Notifications đã chuyển persistence/inbox/email-outbox service về
  Application; API chỉ giữ HTTP controller, SignalR adapter và module composition. Namespace/interface giữ
  nguyên nên Request/Settlement/Identity consumers không đổi. Focused registration/notification/
  architecture/config gates `53/53` PASS; chờ full backend unit + scoped format trước commit.
- Do not redo: B0R route/auth/ProblemDetails characterization và B1 dead-code/base-service cleanup đã có.

## 15. Research sources

- [OpenAI — Model guidance for GPT-5.6](https://developers.openai.com/api/docs/guides/latest-model)
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
