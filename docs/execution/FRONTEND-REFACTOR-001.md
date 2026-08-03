# FRONTEND-REFACTOR-001 — Frontend dễ đọc, dễ trình bày và dễ bảo trì

- Status: `IN PROGRESS — FR0–FR6 COMPLETE; FR7 STRUCTURE COMPLETE, CORRECTION UX/E2E PENDING; FR8A PROVEN CLEANUP STARTED`
- Priority: P1
- Path: `STANDARD — behavior-preserving feature-first refactor`
- Owner: Nguyễn An Nam
- Planning agent: Codex
- Branch: `codex/ai-agent-foundation`
- Base commit: `296027daf69d81cee43b09e1dddf9af4950a028e`
- Planned at: `2026-08-02T23:42:18+07:00`
- Frontend authority: `src/Frontend/Blazor/`
- Related authority: `AGENTS.md`, `src/Frontend/Blazor/AGENTS.md`,
  `docs/architecture/ARCH-001-MODULE-MAP.md`, `docs/execution/UI-SYSTEM-001.md`,
  `docs/design/VPP-UI-MOTIF-CATALOG.md`, `Helpers/RouteCatalog.cs` và
  `Helpers/UiRouteCatalog.cs`
- Supersedes: phần **R-2 frontend** trong `docs/execution/REFACTOR-001.md`; lịch sử R-0 và các
  decision đã hoàn thành vẫn được giữ nguyên
- Does not supersede: visual, interaction, motif và route-real QA authority của `UI-SYSTEM-001`
- User approval required: owner đã duyệt **hướng bắt đầu refactor trước lượt duyệt UI cuối**;
  owner review plan này được khuyến nghị trước FR1, nhưng không còn câu hỏi blocking cho FR0 read-only

<a id="plan-overview"></a>

## 0. Bản một ánh nhìn

| Mục | Tóm tắt dễ hiểu | Chi tiết |
|---|---|---|
| Kết quả cần đạt | Frontend nhìn và chạy như hiện tại, nhưng một sinh viên năm 4 có thể lần từ route → page → state → API client → Shared DTO, hiểu file nào sở hữu việc gì và trình bày được luồng chính | [Objective](#plan-detail-objective) |
| Quyết định thời điểm | Refactor được bắt đầu **trước** final visual acceptance. UI hiện tại là baseline tạm; nếu owner sửa UI sau đó thì thực hiện correction riêng rồi refactor tiếp phần bị ảnh hưởng | [Timing contract](#plan-detail-timing) |
| Phạm vi | Blazor/Radzen frontend, frontend tests và tài liệu đọc code; không đổi API/DTO/database/RBAC/nghiệp vụ, không khôi phục React | [Scope](#plan-detail-scope) |
| Phương án | Giữ một project Blazor, giữ design system hiện có; tổ chức dần theo feature `IdentityAccess`, `CatalogPricing`, `Requests`, `Settlement`, `Reports`, `Notifications`, cộng `Platform` dùng chung | [Target structure](#plan-detail-target-structure) |
| Các bước chính | FR0 baseline tạm → FR1 cleanup dễ thấy → FR2 platform + Reports pilot → FR3 Account/System → FR4 Catalog/Pricing → FR5 Identity/Notifications → FR6 Requests read → FR7 Requests write/Settlement → FR8 shell/CSS/JS/tests/docs/final | [Waves](#plan-detail-waves) |
| Comment/naming | Identifier English dễ hiểu; comment tiếng Việt ngắn chỉ giải thích **vì sao/ràng buộc**; bỏ comment kể lại code, mã wave/ticket và lịch sử AI khi file được chạm | [Readability contract](#plan-detail-readability) |
| Model/quota routing | Architecture/hotspot/final review: `gpt-5.6-sol`; lát rõ và lặp lại: `gpt-5.6-terra`. Quota probe local tiếp tục trả `404`, nên execution phải đi theo checkpoint nhỏ và không được hạ chất lượng để vừa quota | [Routing](#plan-detail-routing) |
| Baseline hiện tại | Release build sạch; frontend unit/architecture `370/370`; 83 UI test được phát hiện. VPPRequest pages không còn generic transport/API endpoint; Requests/Settlement có owner rõ cho query, command, export, draft, editor, submission, approval và settlement mapping | [Evidence](#plan-detail-evidence) |
| Rủi ro chính | Refactor chồng lên correction UI chưa commit; move/rename làm test path-based vỡ; feature client thành lớp wrapper vô nghĩa; CSS/JS global thay đổi visual âm thầm | [Risks](#plan-detail-risks) |
| Việc làm ngay | FR8A/FR8B cleanup độc lập đã hoàn tất. Correction state đã được harden để không gửi snapshot cũ và giữ retry idempotent; FR7 còn chờ owner chốt UX `Xem trước lại` rồi mới triển khai mutation E2E đóng wave | [Continuation](#plan-detail-continuation) |

**Thuật ngữ:**

- `behavior-preserving refactor`: đổi cấu trúc bên trong nhưng hành vi quan sát được không đổi;
- `provisional baseline`: baseline tạm vì owner chưa duyệt visual cuối;
- `feature-first`: file được nhóm theo chức năng/nghiệp vụ thay vì gom mọi helper/service vào một
  thư mục phẳng;
- `migration-on-touch`: chỉ move/rename code cũ khi module đó đang được xử lý và có test bảo vệ;
- `consumer ledger`: bảng ghi file/component/CSS/API nào còn dùng implementation cũ trước khi xóa.

<a id="plan-detail-objective"></a>

## 1. Objective và definition of done

### Objective

Refactor frontend để owner có thể:

1. nhìn tên folder/file là đoán đúng module và trách nhiệm chính;
2. lần một luồng từ URL → route component → UI state → feature API client → DTO trong tối đa vài
   bước có tài liệu dẫn đường;
3. đọc identifier English phổ thông, nhất quán với thuật ngữ GTAS VPP;
4. hiểu các ràng buộc khó nhờ comment tiếng Việt ngắn và test name có nghĩa;
5. trình bày được kiến trúc Blazor/Radzen, data flow, state, permission và API integration khi bảo vệ;
6. tiếp tục vibe-coding mà AI khó nhét endpoint, state hoặc CSS vào sai owner;
7. sửa UI sau này mà không phải quay lại một component god-class hoặc stylesheet không rõ quyền sở hữu.

### Definition of done

- Blazor/Radzen, global `InteractiveServer`, route, query parameter, permission, API/JSON, locale,
  theme và nghiệp vụ giữ nguyên trừ task thay đổi riêng được owner duyệt.
- Mỗi feature có boundary rõ: route/page orchestration, component trình bày, feature API client,
  feature state/model và test.
- `IAPIServices` generic không còn được inject trực tiếp vào Razor/page component; transport thấp chỉ
  được gọi qua typed feature client hoặc platform service có ownership rõ.
- Raw API URL/query construction không còn rải trong Razor. Endpoint và request builder thuộc feature
  client; route chỉ truyền input có nghĩa.
- `CurrentUserState` là UI profile projection/cache canonical từ `/me`; trạng thái đăng nhập vẫn do
  `AuthenticationStateProvider`/cookie/claims xác lập và backend/API vẫn là authorization authority.
  Busy state tách riêng; `GlobalClass` được retire sau khi consumer ledger về 0.
- Sidebar/header/navigation dùng route metadata canonical thay vì duy trì một danh sách permission/path
  thứ hai cạnh `RouteCatalog`.
- Hotspot được tách theo **lý do thay đổi** và khả năng test, không theo luật số dòng máy móc.
- CSS global chỉ giữ foundation/Radzen bridge/shell/cross-cutting thật sự dùng chung; feature/component
  style chuyển dần về owner gần nhất khi selector ledger chứng minh an toàn.
- JavaScript global được chia theo responsibility có lifecycle/dispose rõ hoặc được giữ với waiver có
  bằng chứng nếu việc tách không tạo giá trị.
- Dead code/package/static asset chỉ bị xóa sau usage + runtime/resource verification.
- `docs/architecture/ARCH-001-MODULE-MAP.md` và `docs/CODE-READING-GUIDE.md` phản ánh source cuối;
  tên test đọc như executable documentation.
- Các gate tại [mục 11](#plan-detail-verification) pass; owner final visual acceptance hoàn tất trước
  khi tạo golden baseline và trước khi chốt ảnh slide/luận văn cuối.

### Non-goals

- Không rewrite React, microfrontend, WebAssembly hoặc framework UI khác.
- Không tạo project/class library mới chỉ để giống textbook Clean Architecture.
- Không thay design system/motif đã hoàn thành trong `UI-SYSTEM-001` nếu không có correction UI riêng.
- Không tạo `UniversalPage<T>`, `UniversalGrid<T>`, generic CRUD engine, reflection-driven columns hoặc
  endpoint/query config bằng string.
- Không tự thêm Flux/Redux/MediatR/AutoMapper hoặc state framework mới nếu chưa có vấn đề đo được.
- Không mass-move, mass-rename, mass-format hoặc đổi namespace toàn frontend trong một commit.
- Không đặt mục tiêu “mọi file dưới N dòng”, “0 `!important`” hoặc “100% CSS isolation” một cách máy móc.
- Không đổi backend, Shared wire contract, database, RBAC, LVTN hoặc deployment trong record này.

<a id="plan-detail-timing"></a>

## 2. Timing contract — owner decision 2026-08-02

Owner xác nhận không cần chờ final UI acceptance mới refactor. Quyết định áp dụng như sau:

1. UI hiện tại sau `UI-SYSTEM-001` là **provisional baseline**, không phải visual golden.
2. Refactor chỉ cam kết giữ hành vi/visual của baseline tại thời điểm mở slice.
3. Nếu owner yêu cầu sửa UI trong lúc refactor:
   - correction UI là một change-set có mục tiêu riêng;
   - không che correction trong commit move/rename/cleanup;
   - sau correction, cập nhật baseline và refactor tiếp phần bị ảnh hưởng nếu cần.
4. Owner chấp nhận chi phí rework hợp lý để đổi lấy việc source dễ đọc sớm hơn.
5. Golden screenshot, ảnh luận văn cuối và slide cuối chỉ chốt sau final visual acceptance.
6. Hai file đang dirty `wwwroot/css/vpp-polish.css` và
   `tests/Frontend.UiTests/Tests/Order/ProductCatalogTests.cs` thuộc correction hiện hữu; FR0 phải
   đọc/giữ nguyên diff này, không overwrite hoặc gọi baseline đã khóa trước khi focused QA pass.

Nguyên tắc “hai chiếc mũ” vẫn giữ: một commit là refactor hoặc UI behavior change, không đồng thời cả
hai nếu diff không thể review độc lập.

<a id="plan-detail-evidence"></a>

## 3. Evidence baseline — 2026-08-02

Canonical FR0 inventory, reading map, consumer/debt ledger và execution evidence nằm tại
[`FRONTEND-REFACTOR-001-FR0-LEDGER.md`](./FRONTEND-REFACTOR-001-FR0-LEDGER.md). Plan này chỉ giữ
contract và sequencing; không nhân bản ledger đang thay đổi theo source.

### Repository và test gates

| Evidence | Kết quả hiện tại |
|---|---|
| Authored frontend inventory | 273 file `.cs/.razor/.css/.js`, khoảng 40.073 dòng khi bỏ `bin`, `obj` và vendor Bootstrap |
| C# | 101 file, 14.090 dòng |
| Razor | 110 file, 9.577 dòng |
| CSS | 55 file, 14.748 dòng |
| JavaScript | 7 file, 1.658 dòng |
| Logical route typed | 44 route: 33 authenticated + 11 anonymous |
| `./scripts/gtas.cmd test-frontend` | PASS — `246/246` |
| `dotnet build gtas_vpp.slnx -c Release --no-restore` | PASS — `0 warning / 0 error` |
| UI test discovery | 83 test case |
| `./scripts/gtas.cmd verify -Scope frontend` | PASS tại FR2; agent setup `63/63`, frontend unit `225/225`, UI lightweight `2/2`, vulnerability/leak audit pass |
| Owner visual status | `UI-SYSTEM-001` đã implement F0–F7; owner final visual review vẫn pending |

Số file/dòng/test là snapshot hiện tại, không phải invariant lâu dài.

### Hotspot đã đo

| File | Dòng xấp xỉ | Vấn đề đọc hiểu chính |
|---|---:|---|
| `wwwroot/css/vpp-admin.css` | 1.919 | Nhiều workspace/admin/history/dialog rule trong một global owner |
| `wwwroot/css/vpp-layout.css` | 1.687 | Shell, route viewport, report và responsive behavior cùng file |
| `HistoryWorkspaceShell.razor.css` | 1.578 | Một scoped file vẫn sở hữu quá nhiều child concern và nhiều `::deep` |
| `wwwroot/js/vpp-interactions.js` | 1.165 | A11y normalization, dropdown, theme/language, download, navigation indicator và mutation observer cùng module |
| `Page_OrderCreate.razor.cs` | 821 | Route/query mode, draft, autosave, load, validation, submit và navigation cùng coordinator |
| `HistoryOrderWorkspaceTabBase.cs` | 812 | Query, scope/filter, chart, list/detail, export và JS lifecycle cùng base |
| `PeriodSettlementPanel.razor.cs` | 764 | Load/filter/aggregate/quote/settle/correct/export cùng component |
| `Tab_User.razor.cs` | 727 | Lookup, filter, activation, membership, permission và notification cùng page |
| `LeftSidebar.razor.cs` | 636 | Auth, current user, permission, theme, language, storage, sidebar và header navigation cùng class |

Đây là tín hiệu để tìm seam; số dòng không tự động yêu cầu split.

### Debt/duplication signals

- `IAPIServices` xuất hiện trong 32 source file, trong đó 26 file component/page; có 41 raw API string
  literal và query `skip/top/orderby/filter` được dựng phân tán.
- `CurrentUserState` đã tồn tại nhưng `AuthHelper` vẫn sao chép identity sang `GlobalClass`;
  `GlobalClass` đồng thời giữ busy counter, `BaseUrl` và `CurrentLanguage`.
- `RouteCatalog` đã chứa path/title/page/permission nhưng `LeftSidebar` vẫn khai báo lại permission/path
  và tự build header tab.
- Có 67 companion file mang prefix `Page_`, `Tab_`, `Component_`, `Dialog_` cạnh naming mới;
  21 public property dùng camel/lowercase và khoảng 30 file còn block-scoped namespace.
- Snapshot FR0 sau scoped control correction có 525 `!important` occurrence, 71 hex literal và 137
  inline style occurrence (`133` component/Radzen `Style=`, `4` HTML `style=`). Mục tiêu là giảm khi chạm đúng owner, không
  ép về 0 bằng big-bang.
- Có khoảng 250 dòng dài hơn 200 ký tự; một số Library tab nén field, `try/catch`, API call và mutation
  vào một dòng, làm source khó đọc dù behavior đơn giản.
- Architecture/browser tests rất mạnh nhưng cũng có hotspot path/source-text coupled:
  `SharedUiFoundationTests` 1.306 dòng, `HistoryTests` 1.295,
  `ShellNavigationRegressionTests` 1.132 và `LibraryGridScrollTests` 850.

### Cleanup candidates đã có usage evidence ban đầu

- `Helpers/ObjectExtensions.cs`: chỉ còn declaration.
- `IAPIServices.SetBaseUrl`: chỉ còn interface + implementation; `HttpClient.BaseAddress` đã được DI cấu hình.
- `GlobalClass.BaseUrl`, `GlobalClass.CurrentLanguage`: không có consumer.
- `DropdownModel`, `LeftSidebar.dropdownDataModels_Company`, `selected_Company`, `State` và
  `GlobalStorageModel` chain: không tham gia render hiện tại; cần storage compatibility audit trước xóa.
- Hai no-op `Dispose()` và một số nested model cũ cần usage scan theo type/member trước khi delete.
- Frontend package `Newtonsoft.Json` và Serilog package family không có runtime configuration/callsite
  hiện hành; cần MSBuild/package/transitive audit trước removal.
- 13 static asset khoảng 9 MB không có literal reference trong source/docs/tests: background/login/logo
  cũ, `normalview`, `mordernview`, ba font cũ và favicon PNG. Phải kiểm network/static manifest trước xóa.
- `VppColumnPicker` dùng reflection vào non-public Radzen API: **KEEP/ISOLATE**, không coi là rác;
  ghi rõ lý do và khóa focused test khi nâng Radzen.

<a id="plan-detail-readability"></a>

## 4. Readability contract

### Naming

- Identifier, file và namespace mới dùng English.
- Ưu tiên từ đầy đủ, đúng nghiệp vụ: `LoadPendingOrdersAsync`, `SelectedDepartmentId`,
  `CanApproveRequest`, `SettlementPreview`.
- Boolean bắt đầu bằng `Is`, `Has`, `Can` hoặc `Should` khi phù hợp.
- Async I/O method có suffix `Async`; event handler cũng dùng tên mô tả hành động, không dùng
  `ButtonOnClick_*`, `ProcessData`, `HandleThing`, `mgr`, `tmp`, `data2`.
- Dùng cùng glossary xuyên DTO, feature client, page, component, test và reading guide:
  `Request`, `Supplement`, `Period`, `Settlement`, `Revision`, `PriceList`, `Department`, `Permission`.
- New internal code dùng casing .NET hiện hành (`VppRequest`, `UserId`, `FileName`). Không mass-rename
  public Shared DTO/JSON/route/query/permission code hoặc type legacy nếu có compatibility impact.
- Prefix `Page_`, `Tab_`, `Component_`, `Dialog_` được migrate-on-touch sang tên có nghĩa như
  `OrderEditorSession`, `UserAdministration`, `PermissionBatchDialog`; không rename cả tree cùng lúc.

### Comment tiếng Việt, không biến source thành bài giảng

Comment chỉ giải thích điều code không thể tự nói rõ:

- luật nghiệp vụ;
- permission/security boundary;
- prerender/interactive lifecycle hoặc Radzen workaround khó đoán;
- concurrency/idempotency/cancellation/dispose;
- compatibility/recovery bắt buộc.

Ví dụ phù hợp:

```csharp
// Giữ dữ liệu cũ khi refresh lỗi để người dùng không mất ngữ cảnh đang xem.
```

Không dùng:

```csharp
// Gọi API lấy danh sách đơn hàng.
// F6B / P1 / Atlas round 4.
```

- Comment ngắn dùng `//` trên dòng riêng; XML documentation chỉ cho public/shared boundary thật sự cần
  giải thích.
- Mã wave/ticket, lịch sử thử-sai, “AI generated” và reference học thuật dài chuyển về execution
  record/Git/reading guide khi file được chạm.
- Không mặc định gắn `§` luận văn trong source; mapping nằm ở `docs/CODE-READING-GUIDE.md`.

### Component, class và method

- Route/page chịu trách nhiệm đọc route/query, permission gate, điều phối load/mutation và compose UI.
- Presentational component nhận typed `[Parameter]`, phát `EventCallback<T>` và không tự ghép raw API URL.
- Feature dialog có thể sở hữu save workflow nếu đó là trách nhiệm công khai, nhưng chỉ gọi typed feature
  client; không dùng generic transport/string endpoint trực tiếp.
- Page state tạm giữ private trong component; state dùng xuyên route/circuit mới đưa vào scoped service.
- Không ghi đè parameter; dùng local state hoặc `Value`/`ValueChanged` contract.
- Tách class khi có seam theo use case/lifecycle/test; không tạo private method một dòng hoặc hàng chục
  component nhỏ chỉ để giảm LOC.
- Public member của component giảm về private/protected khi Razor và test không cần public surface.
- API call dài có `CancellationToken`; unsubscribe event và dispose timer/resource/`IJSObjectReference`
  khi component rời DOM, có xử lý `JSDisconnectedException` phù hợp. Không gọi JS interop để cleanup
  DOM từ `Dispose/DisposeAsync`; DOM cleanup thuộc module client-side/`MutationObserver` khi cần.
- Không tối ưu `ShouldRender` hoặc tạo hàng nghìn component nhỏ nếu chưa có measurement.
- Test name theo `Action_Scenario_ExpectedBehavior`; test observable behavior, không khóa private method.

### Changed-file ratchet

Mỗi file được chạm phải:

- dùng format/casing nhất quán;
- không tăng raw API literal, `Style=`/`style=`, hex hoặc `!important` không giải thích;
- xóa comment/ticket noise trong đúng responsibility đang sửa;
- giữ hoặc giảm public surface;
- thêm localization resource thay vì hard-code VI/EN mới;
- không mass-format file ngoài scope.

<a id="plan-detail-scope"></a>

## 5. Scope và compatibility boundaries

### In scope

- `src/Frontend/Blazor/` production source và package/resource cleanup liên quan.
- `tests/Frontend.UnitTests`, `tests/Frontend.UiTests` và page-object/helper cần để khóa behavior.
- `docs/architecture/ARCH-001-MODULE-MAP.md`, `docs/CODE-READING-GUIDE.md`, route/motif/debt ledger.
- Architecture ratchet cho API/state/navigation/CSS/asset ownership.

### Must remain stable

- `@page` route, query parameter, deep link và first-accessible navigation behavior.
- Permission/page/component code, hidden/disabled/action capability và direct API authorization.
- HTTP endpoint, verb, request/response JSON, status/error/session behavior và export bytes/name/MIME.
- Blazor global `InteractiveServer`, prerender/interactive handoff và circuit/reconnect behavior.
- Radzen widget semantics, paging/virtualization, dialog, popup, keyboard/focus và accessibility.
- VI/EN, Light/Dark/Print, responsive geometry và content state của baseline đã chụp.
- Shared DTO wire shape và backend/database behavior.

### Stop conditions

Wave dừng và tách task/approval riêng nếu:

- cần đổi API/DTO/RBAC/database/nghiệp vụ hoặc backend source;
- owner correction UI làm thay đổi acceptance đang dùng cho cùng slice;
- route/permission/deep-link consumer bên ngoài chưa kiểm chứng;
- Radzen change cần MCP nhưng quota/key unavailable;
- package/asset có runtime network consumer hoặc external link chưa xác định;
- browser diff không giải thích được bằng refactor dự kiến;
- mutation test cần dữ liệu không disposable hoặc chưa có opt-in an toàn.

<a id="plan-detail-target-structure"></a>

## 6. Target structure

Giữ **một project Blazor**. Design system và layout toàn cục tiếp tục là authority dùng chung; source
nghiệp vụ chuyển dần về feature để người đọc thấy UI, API và state của cùng chức năng gần nhau.

```text
src/Frontend/Blazor/
├─ Components/
│  ├─ DesignSystem/            # token consumer, primitive, composite, pattern; không API/nghiệp vụ
│  └─ Layout/                  # app shell và global chrome
├─ Features/
│  ├─ IdentityAccess/
│  │  ├─ Account/
│  │  ├─ Administration/
│  │  ├─ Api/
│  │  └─ State/
│  ├─ CatalogPricing/
│  │  ├─ Pages/
│  │  ├─ Components/
│  │  ├─ Dialogs/
│  │  └─ Api/
│  ├─ Requests/
│  │  ├─ Pages/
│  │  ├─ Components/
│  │  ├─ Api/
│  │  └─ State/
│  ├─ Settlement/
│  │  ├─ Components/
│  │  ├─ Api/
│  │  └─ State/
│  ├─ Reports/
│  │  ├─ Pages/
│  │  └─ Api/
│  └─ Notifications/
│     ├─ Components/
│     ├─ Api/
│     └─ State/
└─ Platform/
   ├─ Api/                     # auth header, JSON, errors, paging headers, file stream
   ├─ Auth/
   ├─ Browser/
   ├─ Localization/
   ├─ Routing/
   └─ State/                   # busy/theme hoặc cross-feature state thật sự
```

Đây là **target end-state**, không phải lệnh mass-move. Không bắt buộc mỗi feature có đủ mọi folder
`Pages/Components/Dialogs/Api/State`; chỉ tạo folder khi có file/consumer thật. Mỗi module đi qua:

1. characterization/browser baseline;
2. tách responsibility trong path hiện tại;
3. tạo feature/platform boundary có consumer thật;
4. move file và namespace trong một slice nhỏ;
5. cập nhật architecture/path tests;
6. xóa adapter/path cũ khi ledger bằng 0.

Không tạo README cho mọi folder. Authority bền vững nằm ở module map và code-reading guide; source
giữ tên rõ và API nhỏ.

## 7. Ownership contracts

### API

- FR2 dùng pilot để chọn **các HTTP capability nhỏ** thay vì đóng cứng một universal `ApiTransport`.
  Typed `HttpClient`/delegating handler có thể sở hữu authorization và cross-cutting HTTP concern;
  ProblemDetails reader, file stream/download và session invalidation được tách nếu lifecycle khác nhau.
- Feature client như `ReportsApiClient`, `CatalogApiClient`, `RequestsApiClient`,
  `SettlementApiClient`, `IdentityAccessApiClient` sở hữu endpoint và typed request/query builder.
- Feature client trả DTO hoặc frontend-only result có nghĩa như `PagedResult<T>`; không trả raw
  `HttpResponseMessage` cho page, trừ streaming contract có lifecycle rõ.
- Không tạo một client cho từng endpoint; group theo cohesive module/use case.
- `APIServices` là transitional adapter. Migrate consumer theo module, rồi rename/retire khi ledger 0.
- `Config` giữ app route/cookie/config thật sự; endpoint constant chuyển dần về owning feature client.

### State

- `CurrentUserState`: projection/cache UI canonical của profile `/me`; không thay
  `AuthenticationStateProvider`, claims/cookie hoặc backend authorization.
- `PermissionState`, `ThemeState`, `NotificationInboxState`: giữ nếu responsibility rõ và scoped đúng circuit.
- `UiBusyState`: busy lease/counter có API an toàn, thay boolean setter dễ lệch counter.
- Component-local state không được đẩy vào global service chỉ để “dễ dùng”.
- Query/filter cần bookmark hoặc deep link nằm ở route/query; dialog/open state nằm local.
- Scoped state của Interactive Server chỉ sống theo circuit, không phải durable storage. State cần sống
  qua refresh/reconnect phải nằm ở URL, browser storage, persistent component state hoặc backend.
- Event state service phải unsubscribe/dispose đúng lifecycle và gọi `InvokeAsync` khi cập nhật renderer từ nền.

### Navigation

- `RouteCatalog` sở hữu route key/path/title/page/permission và typed path builder.
- `UiRouteCatalog` chỉ sở hữu visual/composition metadata.
- Sidebar, header tabs, landing redirect và route tests đọc cùng catalog; không giữ mảng permission/path thứ hai.
- Dynamic path/query builder phải encode input; không nối navigation string ở nhiều component.

### Component và design system

- `Components/DesignSystem` tuyệt đối không gọi API, không biết permission code hoặc DTO orchestration.
- Shared abstraction cần ít nhất hai consumer cùng behavior; giống visual nhưng khác workflow thì chia sẻ
  primitive/composite thấp hơn.
- `Components/Shared` được phân loại `MOVE_TO_DESIGN_SYSTEM`, `MOVE_TO_FEATURE`, `MERGE` hoặc `KEEP`;
  không xóa/move theo tên folder.
- `VppColumnPicker` reflection workaround được cô lập và test; không lan reflection sang component khác.

### CSS và JavaScript

- Global CSS chỉ giữ token, base, Radzen bridge, shell và cross-cutting owner đã ghi trong
  `VPP-UI-CSS-OWNERSHIP.md`.
- Feature/component CSS ưu tiên `.razor.css`; `::deep` chỉ dùng khi CSS isolation cần chạm DOM do
  component con render ra, chủ yếu là Radzen, và phải có comment why khi lý do không hiển nhiên.
- Tách CSS theo consumer/behavior, không cắt một file 1.500 dòng thành nhiều file tùy ý.
- Global JS được phân trách nhiệm: accessibility normalization, transient positioning, theme/culture,
  navigation indicator, download/storage. Có thể giữ một bootstrap mỏng nếu runtime cần.
- Chỉ module có listener/observer/resource mới cần init/dispose/re-init contract; module stateless không
  bị ép thêm lifecycle ceremony. Blazor dispose `IJSObjectReference`, còn DOM cleanup không gọi JS
  interop từ `Dispose`. `LongSessionStabilityTests` là gate trước khi retire observer/listener cũ.

## 8. Cleanup classification

| Candidate | Class | Hành động dự kiến | Gate trước khi làm |
|---|---|---|---|
| `ObjectExtensions.cs` | `DELETE_CANDIDATE` | Xóa ở FR1 | Repo-wide method usage = declaration; build + 214 test |
| `IAPIServices.SetBaseUrl` | `DELETE_CANDIDATE` | Xóa ở FR1 | DI base address test + API transport tests |
| `GlobalClass.BaseUrl/CurrentLanguage` | `DELETE_CANDIDATE` | Xóa ở FR1 | Usage 0 + GlobalClass tests |
| `GlobalClass` identity + busy | `MIGRATE_ON_TOUCH` | Current user → `CurrentUserState`; busy → `UiBusyState`; retire khi ledger 0 | Account/permission/library/shell tests |
| `GlobalStorageModel` + Sidebar `State`/company fields | `NEEDS_AUDIT` | Xác nhận local-storage key không còn behavior rồi xóa | Storage/browser scan + shell route smoke |
| Nested/no-op model/dispose trong Library/Orders | `DELETE_CANDIDATE` | Xóa theo type/member usage | Focused unit/build/browser route |
| `Newtonsoft.Json` + Serilog package family | `DELETE_CANDIDATE/NEEDS_AUDIT` | Bỏ package/using không dùng | `dotnet list package --include-transitive`, build, deploy/health smoke |
| 13 zero-reference static asset | `DELETE_CANDIDATE` | Xóa một asset slice | Source/docs scan, browser network, App static manifest, screenshot parity |
| Prefix naming cũ | `MIGRATE_ON_TOUCH` | Rename theo feature, không mass rename | Build + route/path architecture + focused UI tests |
| 41 raw API literal + `Config` endpoint cluster | `DELETE_COMPLETE` | Typed feature client đã sở hữu endpoint; resolver và global endpoint catalog được xóa ở FR8A | Full usage scan + architecture ratchet + frontend tests/build |
| `Components/Shared` overlap | `DELETE_COMPLETE` | Generic UI đã về đúng DesignSystem layer; hai component account về `Features/IdentityAccess/Components`; project-root `_Imports.razor` phủ toàn cây Razor và `Components/Shared` không còn source owner | Consumer ledger + architecture ratchet + frontend tests/build |
| `VppColumnPicker` non-public Radzen reflection | `KEEP/ISOLATED` | Đã chuyển vào DesignSystem composite; architecture test khóa reflection chỉ tồn tại tại owner này | Radzen version + column picker browser test |
| Global CSS/JS hotspot | `MIGRATE_ON_TOUCH` | Tách theo responsibility gần cuối | DOM/computed-style/interaction/long-session parity |
| `bin`, `obj`, TestResults, screenshot/trace thô | `LOCAL_CLEANUP` | Giữ ignored, không commit; xóa local khi cần và không có process owner | Process/lock check + path validation |
| UI-SYSTEM canonical component/CSS có consumer | `KEEP` | Không làm lại design system trong plan này | Motif/route catalog authority |

<a id="plan-detail-routing"></a>

## 9. Model, effort và quota routing

### Current official guidance

- OpenAI current model guide ngày lập plan: `gpt-5.6-sol` cho frontier capability;
  `gpt-5.6-terra` cho cân bằng chất lượng/chi phí.
- Architecture, ambiguous lifecycle, shared CSS/JS và final review cần `sol` high/xhigh.
- Mechanical cleanup, clear file moves và repetitive feature-client migration dùng `terra` medium/high,
  với `sol` review tại checkpoint lớn.
- Đây là **khuyến nghị routing**, không có nghĩa root task đã tự đổi model.

### Quota snapshot

- Local sanitized probe `Get-CLIProxyQuotaSnapshot.ps1` được chạy hai lần ngày 2026-08-02 và đều
  trả HTTP `404`; không có live coverage/capacity đáng tin cậy.
- Không có consumption history tương ứng cho frontend refactor khoảng 40K authored LOC + browser QA.
- Forecast toàn plan: khoảng `65–165% Plus-equivalent`, confidence thấp; giữ safety envelope đến
  khoảng `250%` trước khi tuyên bố đủ capacity cho full plan.
- Kết luận hiện tại: không có bằng chứng để tuyên bố `ENOUGH` cho toàn plan. Owner đã yêu cầu tiếp tục
  xuyên suốt, vì vậy chỉ mở từng checkpoint độc lập, giữ nguyên quality gate và reforecast tại boundary.
- Không tự hạ model/effort để vừa quota. Nếu upper bound sau reforecast vượt capacity có buffer thì `WAIT`.

<a id="plan-detail-waves"></a>

## 10. Execution waves

| Wave | Outcome | Scope chính | Model + effort khuyến nghị | Cost forecast | Gate mở wave sau |
|---|---|---|---|---:|---|
| **FR0 — Provisional baseline & ledger** | Khóa đúng điểm xuất phát dù UI chưa final | Đọc diff `vpp-polish.css` + ProductCatalog test; route/component/API/state/CSS/JS/asset/package ledger; capture Product Catalog + archetype đại diện; source/path manifest; baseline reading map | `gpt-5.6-terra` high, review `gpt-5.6-sol` high | 2–5% | Preflight, build, `214/214`, focused ProductCatalog route-real ở 4 viewport; screenshot chỉ là evidence, không golden |
| **FR1 — Proven cleanup & immediate readability** | Source bớt rác mà chưa đổi architecture | **FR1A:** zero-consumer C#; **FR1B:** package-only; **FR1C:** dead parameter nhỏ. Asset chuyển FR8A; test-only cleanup chuyển FR8B. Không trộn move/rename hoặc Product Catalog formatting vào các lát này | `gpt-5.6-terra` medium/high | 3–8% | Usage/resource evidence; build + unit + route smoke tương xứng; package/security audit |
| **FR2 — Platform contracts + Reports pilot** | Chốt pattern API/state/routing trên feature read-only nhỏ | Characterize `APIServices`; pilot typed HttpClient/small HTTP capabilities + `PagedResult<T>`; typed `ReportsApiClient`; tạo `UiBusyState`; route catalog consumer API; move Reports theo target structure | `gpt-5.6-sol` high design/review, `terra` high implement | 5–12% | Report JSON/export/name/MIME parity; 401/403; Report responsive/Dark/Print; DI smoke |
| **FR3 — Account, system & structural shell state** | Anonymous/session flow dùng typed client; shell identity/busy/navigation source không còn lặp nhưng visual chưa đổi | Account client; retire direct `IHttpClientFactory` trong pages; Login/LoginPage ownership audit; change/recovery/confirm/register/logout/error/not-found; migrate LeftSidebar current-user/busy/route-source wiring, giữ CSS/JS visual cho FR8 | `gpt-5.6-sol` high, `terra` high implement | 6–14% | Account unit; shell identity/navigation focused test; AccountShell, LoginFeedback, Login, Logout, GlobalRender, Accessibility; VI/EN + keyboard + 4 viewport |
| **FR4 — Catalog & Pricing** | Admin data feature có structure lặp lại, dễ lần và không generic transport trong Razor | Lookup, Category, Item, Supplier, Department, PriceList, Price; typed clients/query objects; dialog ownership; server grid state; localization; rename-on-touch | `gpt-5.6-terra` high, review `sol` high | 10–24% | DataSurfaceFoundation, LibraryGridScroll, Pricing/Report motif; Lookup mutation; row thật phải render; permission parity |
| **FR5 — Identity access & Notifications** | User/group/permission/audit/realtime dễ giải thích và không god-page | User administration use cases; membership/activation/capability client; permission mapping; security audit; notification client/state; migrate remaining feature `GlobalClass` consumers và chỉ retire adapter khi ledger 0 | `gpt-5.6-sol` xhigh plan/review, `terra` high implement | 10–25% | Admin user/permission/audit tests; permission mutation; direct 401/403; session invalidation; realtime disposal |
| **FR6 — Requests read paths** | My Orders, History, Catalog và Department Summary có query/state/component ownership rõ | Typed request query client; remove raw endpoints; split History query/filter/detail/export responsibility; My Orders workspace; product catalog; department summary; route/path rename-on-touch | `gpt-5.6-sol` xhigh plan/review, `terra` high implement | 12–30% | MyOrders, History, ProductCatalog, DepartmentSummary, selector/deep-link; paging/virtualization/bounded DOM; console/network |
| **FR7 — Requests write & Settlement** | Core thesis workflow tách theo use case nhưng behavior/mutation không đổi | Order editor session, draft store/autosave, submission coordinator, step components; supplement approval; settlement query/preview/confirm/correct/export; cancellation/dispose | `gpt-5.6-sol` xhigh, `terra` high implement | 15–38% | OrderCreate, OrderManagement, DS3, pending workspace, ExportDownload; API/DB observable outcome; idempotency/draft/recreate/correction parity |
| **FR8 — Global hardening & final acceptance** | Xóa owner cạnh tranh còn lại, hoàn tất test/docs và owner duyệt UI cuối qua ba checkpoint tách biệt | FR8A shell/shared/CSS/JS → FR8B test-only cleanup → FR8C route/docs/final acceptance | `gpt-5.6-sol` xhigh | 10–25% | Mỗi checkpoint có commit/gate riêng; golden chỉ sau FR8C owner approval |

Một implementer chính giữ context. Reviewer/subagent chỉ audit/verify độc lập; agent cùng sửa source phải
dùng worktree riêng và không chạm cùng module.

### FR8 checkpoints bắt buộc

1. **FR8A — Global UI/CSS/JS:** migrate visual shell, shared/design-system owner, CSS/JS và asset/package
   final audit; chạy broad runtime, long-session, axe, Dark/Print rồi **freeze production structure**.
2. **FR8B — Test-only refactor:** production source không đổi; tách test hotspot/page-object/assertion
   helper, giữ discovered test name/count và assertion intent.
3. **FR8C — Route/docs/final acceptance:** cập nhật module map, code-reading guide và ledger đủ 44 route
   key; mỗi key được đánh dấu `TESTED`, `REDIRECT`, `DYNAMIC_SAMPLE` hoặc `JUSTIFIED_EQUIVALENT` kèm
   evidence. Owner rà final board/route thật rồi mới tạo golden baseline.

## 10.1 Module decomposition guide

Tên bên dưới là responsibility guide, không phải yêu cầu tạo đủ class ngay lập tức.

### Platform

- HTTP capability set: typed `HttpClient`/auth handler, response/problem reader, file stream và
  session-invalid handling theo lifecycle; không bắt buộc một universal transport interface.
- `UiBusyState`: disposable/lease-based busy ownership.
- `AppRouteCatalog` hoặc API typed trên `RouteCatalog`: path builder + permission/title metadata.
- Browser service/module: theme, storage, download, viewport; không để page gọi nhiều global JS string.
- `Program.cs` đọc như outline: add platform → add features → configure auth/localization → map app.

### IdentityAccess

- `AccountApiClient`: register/confirm/recovery/reset/change và admin lifecycle endpoint phù hợp.
- `CurrentUserState`: `/me` và display identity.
- `IdentityAccessApiClient`: user/group/membership/page mapping/security audit.
- Page/dialog chia theo use case: invitation, activation, membership, password link, permission batch.
- Không chuyền raw group/department/permission string ở nhiều page nếu typed DTO/option đã tồn tại.

### CatalogPricing

- Client/query theo aggregate: Catalog, Lookup, Pricing; không một client cho từng endpoint.
- Server paging/filter request là frontend-only typed record hoặc method parameter có nghĩa.
- Route/tab giữ column/action/permission; data client không sở hữu Radzen component.
- Dialog editor giữ input/validation/save contract; hard-delete/activation rule vẫn do backend quyết định.

### Requests

- `RequestsQueryClient`: My Orders, History, Product Catalog, Department Summary, filter values.
- `RequestsCommandClient`: create/update/cancel/recreate/supplement approve/reject.
- `RequestsExportClient`: endpoint order PDF/XLSX trên download pipeline dùng chung.
- `OrderEditorSession`: selected items, step và validation UI state; route-derived mode vẫn thuộc page.
- `OrderDraftStore`: local draft serialization/storage/recovery; page giữ timer, dirty flag và notification.
- `OrderSubmissionCoordinator`: dispatch create/update/recreate và trả outcome typed; không sở hữu toast/navigation.
- `PendingApprovalFilterBuilder` + decision request factory: filter escaping và approve/reject retry payload.
- History split theo query/filter/list selection/detail/export; không tạo base class lớn mới.

### Settlement

- `SettlementApiClient`: status, demand, preview, confirm, correct, export.
- `SettlementRequestFactory`: preview/confirm/correct Shared DTO mapping và exception deep clone.
- Page/panel giữ selection/filter/view mode; view-model builder giữ derived row/summary thuần.
- Confirmation/correction là explicit method/use case; không giấu trong generic `SaveAsync`.

### Reports và Notifications

- Reports là pilot read-heavy để chứng minh feature structure/API client trước module mutation lớn.
- Notification inbox/realtime tách transport, state và presentation; dispose subscription khi circuit/page kết thúc.

<a id="plan-detail-verification"></a>

## 11. Verification ladder

### Gate mọi wave

```powershell
./scripts/gtas.cmd preflight -Scope frontend
./scripts/gtas.cmd test-frontend
dotnet build gtas_vpp.slnx -c Release --no-restore
git diff --check
```

Chạy thêm `dotnet format ... --verify-no-changes` theo exact changed files khi slice đổi C# format hoặc
naming; không format toàn repository trong refactor logic.

### Gate route thật

- Viewport: `390×844`, `768×1024`, `1366×768`, `1920×1080`.
- VI/EN và Light/Dark đại diện; Print cho report/detail/export route phù hợp.
- Loading, normal, empty, filter-empty, error, denied, disabled và success theo route profile.
- Keyboard/focus, accessible name, axe critical/serious, overflow, console và failed network.
- Paging/virtualization request count, bounded DOM, popup/dialog position và enhanced-navigation lifecycle.
- Screenshot phải được xem bằng mắt; geometry/source assertion không thay visual review.

### Authenticated read-only browser safety

Mọi authenticated browser test cần isolated QA fixture. Luôn restore environment sau command:

```powershell
$previousIsolated = $env:GTAS_E2E_ISOLATED
try {
    $env:GTAS_E2E_ISOLATED = '1'
    dotnet test tests/Frontend.UiTests/gtas_vpp_fe.UITests.csproj `
        -c Release --no-restore --filter 'FullyQualifiedName~ProductCatalogTests'
}
finally {
    if ($null -eq $previousIsolated) {
        Remove-Item Env:GTAS_E2E_ISOLATED -ErrorAction SilentlyContinue
    } else {
        $env:GTAS_E2E_ISOLATED = $previousIsolated
    }
}
```

### Mutation safety

Mutation browser tests cần thêm explicit opt-in và phải restore cả hai biến:

```powershell
$previousIsolated = $env:GTAS_E2E_ISOLATED
$previousMutationOptIn = $env:GTAS_E2E_MUTATION_OPT_IN
try {
    $env:GTAS_E2E_ISOLATED = '1'
    $env:GTAS_E2E_MUTATION_OPT_IN = 'I_UNDERSTAND_THIS_MUTATES_QA_DATA'
    dotnet test tests/Frontend.UiTests/gtas_vpp_fe.UITests.csproj -c Release --no-restore
}
finally {
    if ($null -eq $previousMutationOptIn) {
        Remove-Item Env:GTAS_E2E_MUTATION_OPT_IN -ErrorAction SilentlyContinue
    } else {
        $env:GTAS_E2E_MUTATION_OPT_IN = $previousMutationOptIn
    }

    if ($null -eq $previousIsolated) {
        Remove-Item Env:GTAS_E2E_ISOLATED -ErrorAction SilentlyContinue
    } else {
        $env:GTAS_E2E_ISOLATED = $previousIsolated
    }
}
```

Không dùng profile/cookie/database production hoặc tự điều khiển `dotnet watch` của owner.

### Gate theo loại thay đổi

| Change type | Bắt buộc thêm |
|---|---|
| File/folder/namespace rename | Route/path architecture tests; `_Imports`; resource key; `--list-tests` parity; focused route |
| API transport/feature client | Request URL/query/header/status/error characterization; 401/403; session invalidation |
| State/event/lifecycle | Unit test + enhanced navigation/reconnect + dispose/listener leak check |
| Account/permission | Direct API authorization, role/action matrix, cookie/session and mutation outcome |
| Paging/virtualization | Filter-before-paging, total count, reset offset, bounded DOM và request count |
| CSS move/delete | Consumer/selector ledger; computed style; 4 viewport; Light/Dark; visual inspection |
| JS split/delete | Init/dispose/re-init khi module sở hữu resource; keyboard; popup/navigation motion; long-session stability |
| Package removal | Direct/transitive package audit, Release build, publish/static asset/health smoke |
| Static asset removal | Repo scan + runtime network log + static manifest + screenshot parity |
| Test refactor | Production source unchanged; discovered test names/count and assertion intent preserved |
| Final wave | Full isolated UI suite; 44-key route ledger; dynamic/account flows; `verify -Scope frontend`; owner final visual acceptance, then golden baseline |

### Current verification state

`model-routing-eval` đã được sửa bằng thay đổi wording-compatible và `verify -Scope frontend` pass.
FR1A runtime gate được đóng theo non-regression: My Orders pass; hai smoke Item/Department editor fail
cùng exact timeout trên clean baseline `b739288d`, nên đây là baseline fixture/permission debt, không phải
regression của cleanup. Debt vẫn phải được xử lý trước final UI acceptance.

<a id="plan-detail-risks"></a>

## 12. Risks và rollback

| Risk | Mức | Mitigation | Rollback |
|---|---|---|---|
| Refactor đè correction UI dirty | High | FR0 đọc exact diff, stage exact paths, không mass checkout/reset | Revert riêng refactor commit; giữ owner diff |
| Feature-first thành mass move | High | Migration-on-touch, một module/commit, move sau characterization | Revert move/namespace slice, giữ tests |
| Typed client chỉ là wrapper vô nghĩa | Medium | Group theo cohesive use case, page không raw endpoint, API nhỏ | Hạ abstraction hoặc merge client trong module |
| Route/deep-link đổi vô ý | High | RouteCatalog manifest + browser direct navigation | Revert path builder/rename slice |
| Global state bị chia sai làm stale UI | High | Current user/busy/permission characterization + event/dispose tests | Giữ transitional adapter, rollback consumer slice |
| CSS move làm visual drift | High | Consumer ledger + computed style + runtime visual at 4 viewport | Revert CSS slice, không thêm `!important` che lỗi |
| JS split tích lũy observer/listener | High | Explicit lifecycle + LongSessionStabilityTests | Revert module split, giữ bootstrap cũ |
| Package/asset tưởng rác nhưng có runtime consumer | Medium | Publish/network/static manifest before delete | Re-add exact package/asset in isolated commit |
| Test source/path assertion cản rename | Medium | Đổi test từ path detail sang stable contract khi phù hợp; giữ observable assertion | Revert rename; không sửa test để bỏ behavior |
| Comment quá nhiều hoặc “lộ AI” | Medium | Why-only ratchet; history nằm docs/Git | Xóa comment noise trong same slice |
| User-owned dirty file bị stage nhầm | High | `git add <exact-path>`, staged diff review; không `git add .` | Unstage exact path; không reset user work |

Rollback mặc định là revert một vertical slice nhỏ. Không có database restore khi frontend change đã
chứng minh không mutation schema/data; mutation QA phải tự reset fixture theo harness contract.

## 13. Thứ tự portfolio hiện hành

Owner đã thay thế yêu cầu “chốt UI hoàn toàn rồi mới refactor frontend”. Thứ tự dễ hiểu hiện tại:

1. **Checkpoint correction UI đang dở** — hiểu và giữ hai dirty file hiện hữu; chưa cần final acceptance.
2. **Frontend refactor FR0→FR8** — từng lát giữ baseline tạm; correction UI mới vẫn được phép xen ở
   boundary rõ rồi refactor tiếp.
3. **Owner final UI acceptance** — rà toàn bộ route, sửa correction cuối và chỉ lúc này mới chốt golden.
4. **Backend refactor B0→B8** theo `BACKEND-REFACTOR-001`.
5. **Luận văn + slide finalization** — cập nhật source map, test evidence, screenshot và sơ đồ cuối.

Storyboard slide, dàn ý chương trình bày và glossary có thể làm sớm. Không chốt screenshot, test count,
file path hoặc sơ đồ kiến trúc cuối trước khi frontend/backend refactor ổn định.

## 14. Owner decisions

| ID | Trạng thái | Quyết định |
|---|---|---|
| FE-D1 | `APPROVED 2026-08-02` | Bắt đầu refactor trước final visual acceptance; chấp nhận refactor tiếp sau correction UI |
| FE-D2 | `APPROVED 2026-08-03` | Giữ một Blazor project, feature-first theo module của `ARCH-001`; không rewrite framework/new project |
| FE-D3 | `APPROVED 2026-08-03` | `CurrentUserState` canonical, tách `UiBusyState`, typed feature clients; migrate-on-touch, không big-bang |
| FE-D4 | `APPROVED 2026-08-03` | English identifiers + Vietnamese why-only comments; bỏ ticket/wave/history khỏi source khi chạm |
| FE-D5 | `APPROVED 2026-08-03` | Final golden/slide screenshots chỉ sau owner final UI acceptance |

FE-D2..D5 là authority cho implementation hiện tại; thay đổi material cần quay lại owner decision.

<a id="plan-detail-continuation"></a>

## 15. Continuation note

- Current status: **FR0–FR6 hoàn tất; FR7 structural ownership đã triển khai; correction UX + mutation E2E còn pending. FR8A đang tiếp tục cleanup độc lập có usage evidence; `Components/Shared` đã về 0 source owner**.
  Provisional baseline chưa phải
  golden hoặc owner final visual acceptance.
- FR0 start point: `codex/ai-agent-foundation` @ `c6ca07bd`.
- Pre-existing dirty files ngoài plan docs: AI-harness, LVTN DOCX, `vpp-polish.css`,
  `ProductCatalogTests.cs` và hai text extraction artifact; không stage/overwrite.
- FR0 evidence: preflight PASS; Release build `0 warning/error`; frontend unit/architecture
  `214/214`; 82 UI test discovered; Product Catalog `1/1`, responsive matrix `4/4`, User Menu `1/1`;
  9 PNG runtime đã xem trực tiếp; `verify -Scope frontend` PASS với agent setup `63/63`.
- FR1A evidence: usage scan không còn match; format verify PASS; Release build PASS; unit `214/214`;
  My Orders smoke PASS. Item/Department editor có cùng exact timeout trên clean baseline `b739288d`,
  nên FR1A đạt non-regression; fixture/permission debt được giữ cho UI acceptance/B0R.
- FR1B evidence: direct/transitive package audit không còn Newtonsoft/Serilog; restore/build/unit pass;
  anonymous account + authenticated report smoke `2/2`; publish không chứa DLL Newtonsoft/Serilog.
- FR1C evidence: bỏ parameter `claims` không đọc khỏi Product Catalog; format/build/unit pass và focused
  Product Catalog route-real `1/1`.
- FR2 transport evidence: `ApiServicesJsonTransportTests` khóa bearer, total-count fallback, no-content,
  file metadata/bytes, 401, 403 và ProblemDetails; focused `11/11`.
- FR2 Reports evidence: endpoint/query/export suffix thuộc `ReportsApiClient`; page không còn raw
  `api/reports`/`BuildEndpoint`/`IAPIServices`; frontend unit `224/224`, UI discovery 83, report responsive
  + combined Pricing/Report `2/2`, English + real PDF/XLSX/CSV export `2/2`, Dark/Print/axe `1/1`.
- FR2 UiBusy evidence: toàn bộ legacy busy setter/event đã về 0 consumer; lease nested/double-dispose
  unit pass; full frontend `225/225`; admin user/permission `2/2`; User Menu `2/2`; Release build và
  `verify -Scope frontend` PASS. `GlobalRenderFlowTests` còn flaky ở fixture navigation/render timing:
  clean baseline pass, current runs fail tại hai điểm khác nhau; không sửa assertion để che nợ này.
- FR3.0 state evidence: thêm test trực tiếp cho `CurrentUserState`, `AuthHelper`, `PermissionState` và
  reusable test doubles; focused `10/10`, full frontend `235/235`. Contract đã khóa single-flight/cache,
  null retry, invalidate, server-profile projection, anonymous reset, refresh signal và route fallback.
- FR3 account foundation: `ApiProblemReader` được dùng chung bởi transitional `APIServices` và typed
  `AccountApiClient`; public POST/GET exact route/body/token encoding, public 401 isolation và authenticated
  change-password delegation pass `22/22`; full frontend `246/246`, build sạch.
- FR3.1 migration: ForgotPassword và ResendConfirmation không còn `IHttpClientFactory`, dùng parameter
  submit có nghĩa và typed error mapping; mỗi checkpoint full unit `246/246`, account route-real `1/1`.
- FR3.1 public complete: Register mutation `1/1`; Reset/Confirm prerender+route contract pass; final
  anonymous 7-route × 3-viewport `1/1`, full unit `246/246`. Năm endpoint constant global zero-consumer
  đã xóa.
- FR3 login migration: `AuthenticationApiClient` sở hữu public `POST /api/Auth/login`; `LoginPage`
  không còn `IHttpClientFactory` nhưng vẫn giữ `LoginTicketCache`, returnUrl, remember-me và
  `/perform-login`. Contract 400/401/429 + malformed JSON pass; full frontend `251/251`, Release build
  sạch. Invalid-login browser pass ở 3 viewport; valid-login pass khi chạy riêng trên isolated fixture.
  Lượt chạy chung bị nhiễu sau nhiều invalid attempt và timeout navigation, nên không dùng làm regression
  verdict cho client.
- FR3 change-password migration: page dùng authenticated method của `AccountApiClient`, endpoint constant
  global đã xóa; architecture gate cấm account pages sở hữu `IHttpClientFactory`/`IAPIServices`. Focused
  account client/route `31/31`, full frontend `259/259`, Release build sạch; success vẫn force-load qua
  `/perform-logout` để hủy cookie/session sau khi đổi mật khẩu.
- FR3 shell identity migration: `LeftSidebar`/`UserMenu` đọc trực tiếp `CurrentUserState` thay vì bản sao
  `GlobalClass.UserInfo`, đồng thời subscribe/unsubscribe state event theo lifecycle. Focused state/shell
  architecture `66/66`, Release build sạch và User Menu route-real `2/2`; tên, nhóm vai trò và phòng ban
  giữ nguyên.
- FR3 shell navigation: `ShellNavigationCatalog` nối 4 section/17 leaf với canonical `RouteCatalog`;
  sidebar/header không còn lặp literal URL, permission array, label hoặc icon. Default theo quyền,
  order-create alias, period legacy aliases và pricing `tab=4` được khóa bằng test. Full frontend
  `262/262`, Release build sạch; sidebar controls `1/1`, nested header `1/1` và desktop header `1/1`
  khi chạy isolated riêng. Lượt gộp ba test có animation/hover timing nhiễu nên không dùng làm verdict.
- FR3 shell cleanup: xóa company dropdown và `CostingSetting` storage state zero-consumer khỏi sidebar,
  đồng thời xóa hai model chỉ phục vụ code chết và các namespace import tương ứng. Release build sạch;
  shell/prerender focused `39/39`.
- FR4 Lookup: `LookupApiClient` sở hữu query/filter/paging fallback/dependency impact/CRUD cho category
  và value; grid/dialog không còn generic transport hoặc `GlobalClass`, dùng `CurrentUserState` cho audit
  user. Full frontend `266/266`, Release build sạch, Lookup master-detail route-real `1/1`. Mutation E2E
  không thấy nút Add nhưng đối chứng commit trước FR4 fail cùng locator, nên được ghi fixture/permission
  debt chứ không gán regression cho client mới.
- FR4 Category: `CatalogApiClient` bắt đầu sở hữu category paging/filter/CRUD/status; grid/dialog dùng
  `CurrentUserState`, không còn generic transport/`GlobalClass`. Full frontend `268/268`, Release build
  sạch. Hai browser test Category dừng tại Add action vì `CanModify` false trước khi gọi client; cùng họ
  fixture/permission debt đã được chứng minh ở Lookup và không thuộc transport slice.
- FR4 Supplier: client tiếp tục sở hữu supplier paging/filter/dependency-impact/CRUD/status; grid/dialog
  dùng `CurrentUserState` và không còn `IAPIServices`/`GlobalClass`. Full frontend `269/269`, Release build
  sạch; dependency-impact và endpoint được khóa bằng contract test.
- FR4 Department: client sở hữu active-parent list, paging/filter/dependency-impact/CRUD/status. Grid và
  dialog được format lại từ one-line code thành các block dễ đọc, vẫn giữ parent validation, hard-delete
  và toast flow; full frontend `270/270`, Release build sạch.
- FR4 Item: client sở hữu reference data category/UOM, item paging/search/filter/sort, typed create/update,
  status endpoint và hard-delete. Grid/dialog one-line được format thành block dễ đọc; full frontend
  `271/271`, Release build sạch và architecture test khóa không quay lại generic transport.
- FR4 Price List: `PricingApiClient` sở hữu supplier lookup, server query và toàn lifecycle create/update/
  publish/expire/default/clone/deactivate/hard-delete. Page không còn generic transport hoặc endpoint
  builder; full frontend `273/273`, Release build sạch.
- FR4 Item Price: client sở hữu price-list/supplier reference data, rows/category options/filter query và
  create/update/deactivate/default/hard-delete. Full frontend `274/274`, Release build sạch; pricing
  bounded-grid route-real `1/1`. Motif test có dữ liệu nhưng lifecycle action bị ẩn vì `CanModify=false`,
  cùng permission-fixture debt đã biết.
- FR4 state retirement: production consumer `GlobalClass.UserInfo` về 0; xóa class/DI/import và bốn
  injection thừa. `AuthHelper` chỉ xác nhận cookie rồi nạp canonical `CurrentUserState`, không còn profile
  projection thứ hai. Full frontend `274/274`, Release build sạch; shell identity route-real `1/1`.
- FR5 Permission/Security Audit: `PermissionAdministrationApiClient` sở hữu group query, UI permission
  mapping và security-audit query/filter. Hai tab không còn generic transport hoặc tự ghép endpoint;
  parameter claims thừa đã xóa. Full frontend `276/276`; Permission editor và Security Audit route-real
  cùng pass `2/2` trên isolated host.
- FR5 User Administration: `UserAdministrationApiClient` sở hữu lookup, user query, invitation,
  activation, password-link và membership commands. Tab không còn generic transport, endpoint builder
  hay claims/PagePermission parameter thừa; sáu Config endpoint zero-consumer đã xóa. Full frontend
  `279/279`; user administration desktop/mobile route-real pass `1/1` isolated.
- FR5 permission lifecycle: `PermissionState` coalesce refresh signal thay vì chờ semaphore lồng nhau,
  nên `/me/permissions` trả 403 không còn deadlock. State unsubscribe signal khi dispose; focused
  lifecycle `5/5`, full frontend `281/281`.
- FR5 Notifications: `NotificationApiClient` sở hữu inbox/read endpoints,
  `NotificationRealtimeClient` sở hữu SignalR connection/subscription, còn `NotificationInboxState`
  chỉ điều phối UI state. Focused `7/7`, full frontend `288/288`; notification panel route-real pass
  `1/1` isolated. FR5 hoàn tất.
- FR6 Product Catalog: `RequestsQueryClient` bắt đầu sở hữu request-side catalog query, category/unit
  reference data và filter escaping. Page dùng Shared `VppItemResDTO`/`VppCategoryResDTO`, không còn
  model lồng hoặc generic transport. Full frontend `291/291`; catalog route-real pass `1/1` isolated.
- FR6 Orders/History: query client sở hữu period info, My Orders, order detail/history, history summary,
  pending list và distinct filter endpoint. `BaseOrderTab` không còn URL/generic transport; history scope
  là enum typed. Full frontend `294/294`; My Orders + History `2/2` và Department Summary `1/1`
  route-real isolated. FR6 hoàn tất.
- FR7 Requests command transport: `RequestsCommandClient` sở hữu create/update/recreate/cancel/restore
  và approve/reject đơn bổ sung; order page, My Orders và pending approval không còn generic transport
  cho các mutation này. Order editor dùng Shared `VppItemResDTO`, query client sở hữu catalog snapshot,
  period/order/previous-items; `OrderCreateStep2` khai báo lifecycle dispose để hủy search debounce.
  Focused `35/35`, full frontend `296/296`, solution Release build `0 warning/error`. Isolated mutation
  E2E pass `4/5`; test workflow approve/reject timeout ở dialog lịch sử cuối cùng và clean baseline
  `b568d8fc` fail cùng locator/stack trace, nên được ghi là nợ E2E có sẵn chứ không phải regression của
  command transport.
- FR7 Settlement transport: `SettlementApiClient` sở hữu status, demand, full order snapshot, preview,
  confirm, correction và PDF/XLSX export; period workspace dùng `RequestsQueryClient` cho period info và
  panel dùng `CatalogApiClient` cho department directory. Panel/workspace không còn generic transport,
  raw settlement endpoint hoặc endpoint constant cạnh tranh trong `Config`. Characterization khóa đổi kỳ,
  preview sai kỳ, idempotency retry, complete-confirmation context và request/response/export contracts.
  Focused `42/42`, full frontend `303/303`, solution Release build `0 warning/error`; route-real DS3
  Settlement pass `1/1`. Atlas aggregate test timeout ở nút Report `Xuất CSV`; clean baseline `b7ec8433`
  fail cùng locator/stack trace, nên không phải regression của Settlement slice.
- FR7 Order Draft Store: JSON payload, localStorage get/set/remove và dọn draft khác kỳ chuyển sang
  `Features/Requests/Drafts/OrderDraftStore`; page tiếp tục sở hữu timer 8 giây, dirty flag, toast và UI
  dispatcher. Key, PascalCase JSON shape, edit/recreate/copy semantics và create-only removal giữ nguyên;
  payload legacy `Items=null` được normalize về danh sách rỗng như behavior trước refactor. Focused `35/35`,
  full frontend `308/308`; isolated Order Create lifecycle mutation pass `1/1`.
- FR7 submission request factory: create/update/recreate DTO mapping chuyển sang factory thuần, khóa period,
  base request, row version, idempotency, item order/quantity và trim reason theo đúng từng mode. Hai nguồn
  additional hiện hữu (`IsAdditional` route state và `Editor.IsAdditional` session state) được giữ tách biệt
  để không đổi payload khi query-only navigation làm chúng lệch. Focused `30/30`, full frontend `312/312`,
  solution Release build `0 warning/error`; Order Create lifecycle mutation pass `1/1` sau parity patch.
- FR7 Settlement projection: department option, item option/filter, department filter/group/totals và status
  precedence chuyển sang `SettlementWorkspaceProjection` thuần; component chỉ còn orchestration và ánh xạ
  localization/status tone. Focused Settlement/architecture `34/34`, full frontend `315/315`, solution
  Release build `0 warning/error`; route-real DS3 Settlement pass `1/1` trên isolated fixture. Test chọn kỳ
  dùng selector CSS có mục đích vì dialog có hai button cùng accessible name `Hủy`.
- FR7 Order Editor Session: `OrderCreateContext` data bag và step index `0/1` được thay bằng
  `OrderEditorSession` + enum typed. Session sở hữu step, selection, quantity/note mutation và validation
  thuần; page tiếp tục sở hữu route mode, auth/permission, period authority, draft timer/storage, command,
  toast và navigation. Hai nguồn additional legacy vẫn tách biệt; recreate vẫn khởi tạo danh sách rỗng.
  Focused session/architecture `6/6`, full frontend `320/320`, solution Release build `0 warning/error`;
  isolated edit-cancel-restore và recreate-from-blank lifecycle pass `2/2`.
- FR7 Order Submission Coordinator: ba operation typed create/update/recreate điều phối factory + command
  client và trả outcome cho page; period refresh, hai nguồn additional, idempotency key, draft cleanup,
  localization, toast/log và navigation vẫn ở đúng owner. Coordinator không nuốt transport exception và
  tiếp tục chấp nhận response nullable như behavior cũ. Focused submission/architecture `11/11`, full
  frontend `324/324`, solution Release build `0 warning/error`; isolated create supplement,
  edit-cancel-restore và recreate-from-blank pass `3/3`.
- FR7 Settlement request factory: preview/confirm/correction Shared DTO mapping chuyển sang factory thuần;
  Year/Month vẫn từ component, snapshot identity từ preview, idempotency key truyền nguyên trạng và exception
  được deep-clone theo đúng thứ tự với reason trim. Panel tiếp tục sở hữu reason validation, dialog, toast và
  state transition. Focused Settlement/architecture `35/35`, full frontend `328/328`, solution Release build
  `0 warning/error`; route-real DS3 Settlement pass `1/1`. Fixture chưa có E2E mutation chuyên biệt cho
  confirm/correct nên phần đó chỉ được khóa bằng factory/API/state tests hiện tại.
- FR7 pending approval helpers: exact 4-field search/department filter và escaping chuyển sang
  `PendingApprovalFilterBuilder`; approve/reject DTO + retry key theo `(OrderId, Action)` chuyển sang factory
  instance do component sở hữu. Dialog, processing state, toast và reload vẫn ở tab. Focused `8/8`, full
  frontend `334/334`, solution Release build `0 warning/error`; pending list-detail route pass `1/1`.
  Mutation flow tạo/duyệt/từ chối đã đi qua nhưng test `1/2` timeout ở dialog lịch sử cuối cùng; clean baseline
  `b568d8fc` fail cùng locator/stack trace nên đây vẫn là nợ E2E có sẵn.
- Backend-review backlog, không sửa trong FR7: pending query hiện luôn gửi default `orderby`, khiến controller
  đi qua nhánh filter/sort in-memory; rà lại khi review `BACKEND-REFACTOR-001` thay vì đổi behavior ở frontend.
- FR7 feature ownership cleanup: `OrderDraftStoragePolicy` chuyển khỏi `Helpers` vào
  `Features/Requests/Drafts`; `PeriodSettlementState` chuyển khỏi route component vào
  `Features/Settlement/State`, DI dùng typed namespace ngắn. Focused state/draft/architecture `39/39`, full
  frontend `334/334`, solution Release build `0 warning/error`; DS3 Settlement route pass `1/1`.
- FR7 closure decision: sau confirm/correct, state xóa key và giữ preview; `CanSubmitCurrentPreview` vì vậy
  khóa action cho đến khi có preview mới. Tự động rotate key hoặc re-preview sẽ đổi workflow hiện tại, nên
  không sửa âm thầm trong behavior-preserving refactor; cần owner chốt `re-preview bắt buộc` hay
  `tự refresh sau thành công`.
- FR7 correction audit + behavior-preserving hardening: backend tự tính lại `InputHash`; correction kế tiếp
  cần current revision target và idempotency key mới, nhưng việc bắt người dùng chủ động xem preview là UX/
  human-review gate. Phương án A `Xem trước lại` vẫn được khuyến nghị và chưa triển khai khi chưa có owner
  approval. Trong phần không đổi workflow, `CompleteConfirmation` được đổi thành
  `RequireFreshPreviewForNextSubmission`, `CanConfirm` thành `CanSubmitCurrentPreview`, `canCorrect` thành
  `hasCorrectionTarget`; submit bị khóa khi preview mới đang tải. Sau mutation, status phải refresh thành công
  trước khi key bị xóa để retry cùng command vẫn giữ idempotent nếu refresh lỗi. Focused state/architecture
  `5/5`, full frontend `370/370`, solution Release build `0 warning/error`; DS3 Settlement route-real pass
  `5/5` gồm unified workflow và bốn viewport.
- FR7 order export owner: hai route không còn lặp `Config.VppApi.Orders`; `RequestsExportClient` sở hữu
  endpoint/suffix còn `IBrowserFileDownloadService` tiếp tục stream đúng pipeline. Aggregate architecture gate
  quét toàn bộ `Components/Pages/VPPRequest` cấm generic transport, direct download service và raw API/config
  endpoint. Focused `11/11`, full frontend `338/338`, solution Release build `0 warning/error`; isolated Order
  Exports tải file thật pass `1/1`.
- Final-acceptance harness preflight: `AtlasFullRuntimeTests` ban đầu chỉ fail do test chưa allowlist request
  `/_blazor/initializers` bị `ERR_ABORTED` khi enhanced navigation hủy bootstrap cũ; các browser test cùng
  contract đã coi đây là abort hợp lệ. Sau khi thêm đúng path + đúng failure type, toàn bộ Atlas runtime pass
  `2/2`: 28 màn × 4 viewport, representative Dark/Print/axe. Đây là evidence kỹ thuật mới, chưa thay owner
  visual acceptance và chưa tạo golden baseline.
- FR8A endpoint authority cleanup: `LibraryEndpointResolver` và test chỉ còn tự tham chiếu sau khi mọi
  consumer đã chuyển sang typed feature client. Xóa resolver cùng `Config.VppApi`, `Config.LibraryApi` và
  bốn API base constant zero-consumer; thêm architecture ratchet cấm `Config` sở hữu endpoint catalog.
  Full usage scan không còn production consumer; focused `2/2`, full frontend `338/338`, solution Release
  build `0 warning/error`.
- FR8A package cleanup: bỏ direct package `Microsoft.AspNetCore.SignalR.Client.Core` vì
  `Microsoft.AspNetCore.SignalR.Client 10.0.9` đã phụ thuộc đúng `Core 10.0.9`. Restore/package graph xác nhận
  Core chuyển thành transitive; full frontend `338/338`, solution Release build sạch, publish output vẫn có
  cả hai SignalR assembly và isolated user-menu/notification realtime smoke pass `1/1`.
- FR8A design-system ownership: chuyển `VppIcon`, semantic `VppIcons`, `VppBrandMark` và toàn bộ skeleton
  loading Razor/scoped CSS khỏi `Components/Shared` vào `Components/DesignSystem/Primitives`; không giữ alias
  namespace/path cũ. Cập nhật hai C# consumer và architecture ratchet cho path canonical. Focused skeleton/
  shared foundation `48/48`, full frontend `346/346`, solution Release build `0 warning/error`; isolated
  brand/icon/user-menu/notification `2/2` và Product Catalog 4 viewport `4/4`.
- FR8A column-picker ownership: chuyển `VppColumnPicker` khỏi `Components/Shared` vào canonical
  `Components/DesignSystem/Composites`. Reflection vào non-public Radzen `SetVisible/ChangeState` được giữ
  nguyên nhưng architecture test khóa workaround chỉ tồn tại trong component owner này. Focused shared
  foundation `37/37`, full frontend `347/347`, solution Release build sạch; User Administration route pass `1/1`.
- FR8A generic shared UI: chuyển `VppPageHeader` vào DesignSystem composite và `VppInlineAlert` +
  `VppAlertTone` vào DesignSystem primitives. Public parameter/markup giữ nguyên, path Shared cũ bị architecture
  ratchet cấm quay lại. Focused shared/account `64/64`, full frontend `350/350`, solution Release build sạch;
  Account shell + authenticated NotFound return flow pass `2/2`.
- FR8A account ownership: chuyển `VppLanguageSwitch` và `VppPasswordField` vào
  `Features/IdentityAccess/Components`, đưa `_Imports.razor` lên project root để namespace áp dụng nhất quán
  cho cả feature và component, đồng thời khóa `Components/Shared` không còn source file. Focused architecture
  `68/68`, full frontend `354/354`, solution Release build sạch. Account shell smoke pass `2/3`; test form
  đăng ký còn vượt viewport đúng `2px` (`page=771`, `viewport=768`, `card=747`) và worktree sạch tại
  `59b6a102` tái hiện cùng số liệu, nên đây là geometry debt có sẵn chứ không phải regression của slice.
- FR8A notification-state ownership: chuyển `NotificationInboxState` khỏi thư mục service phẳng vào
  `Features/Notifications/State`, đồng bộ namespace consumer/test và architecture ratchet cấm quay lại path
  cũ. Feature Notifications giờ có ba owner tách biệt `Api`, `Realtime`, `State`; focused `45/45`, full frontend
  `354/354`, solution Release build sạch và isolated user-menu/notification panel pass `2/2`.
- FR8A theme-state ownership: chuyển `ThemeState` khỏi thư mục service phẳng vào `Platform/State`, thêm unit
  contract khóa default/dark/idempotent notification và architecture ratchet khóa owner mới. Focused `7/7`,
  full frontend `356/356`, solution Release build sạch; user-menu/theme interaction pass `1/1`. Aggregate theme
  route audit còn timeout ở `.vpp-permission-matrix`; worktree sạch tại `ac04b5b4` fail đúng locator này nên
  được giữ là permission-fixture debt có sẵn, không phải regression của slice.
- FR8A composition root: rút `Program.cs` từ 233 xuống 16 dòng vật lý và chuyển chi tiết vào ba owner
  `FrontendRuntimeSettings`, `FrontendServiceCollectionExtensions`, `FrontendApplicationExtensions`. Lifetime,
  HTTP timeout/handler, cookie, Data Protection, localization, middleware order và endpoint map đều được giữ
  tường minh, không dùng assembly scanning. Focused composition/config `46/46`, full frontend `362/362`,
  solution Release build sạch, exact-file format gate pass và isolated account/logout/not-found pass `3/3`.
- FR8A formatting hardening: chuẩn hóa initializer indentation ở `OrderSubmissionRequestFactory`,
  `SettlementRequestFactory` và `SettlementWorkspaceProjectionTests`; behavior-focused factory/projection tests
  pass `11/11`. Sau checkpoint này, full `verify -Scope frontend` PASS: agent setup `63/63`, Release build sạch,
  frontend unit `362/362`, UI smoke `2/2`, vulnerability audit và redacted Gitleaks scan đều pass.
- FR8A static-asset cleanup: xóa có kiểm chứng 11 legacy asset và 42 Bootstrap sibling zero-consumer; giữ
  `Artboard*.jpg` cho khả năng dùng trong thesis/slide, asset login hiện hành, font có owner và chỉ
  `bootstrap.min.css` + source map trong app-local distribution. Architecture asset `2/2`, full frontend
  `364/364`, solution Release build sạch; publish tree không còn candidate app-local, Atlas runtime `2/2`,
  account shell + authenticated user menu `2/2`. Bootstrap min CSS vẫn tải qua link canonical, không có 404.
- FR8A browser-download ownership: chuyển `BrowserFileDownloadService` và interface khỏi `Services` phẳng vào
  `Platform/Browser`; Reports, Requests và Settlement chỉ consume interface, không tạo stream/JS pipeline cạnh
  tranh. Architecture/export client focused `18/18`, full frontend `364/364`, solution Release build sạch và
  real-file export E2E `2/2`.
- FR8A column-picker CSS ownership: chuyển toàn bộ selector và responsive rule
  `.vpp-column-picker-*` khỏi global `wwwroot/css/vpp-admin.css` về companion scoped CSS 320 dòng của
  `DesignSystem/Composites/VppColumnPicker`; chỉ dùng `::deep` tại boundary icon con do `VppIcon` render.
  Architecture ratchet xác nhận global selector count bằng 0; focused shared foundation `41/41`, full frontend
  `364/364`, solution Release build `0 warning/error`. Hai browser flow trực tiếp mở và thao tác picker pass
  `2/2` (Library search/toggle/reset/geometry và Permission options/motion); ảnh runtime 1920x1080 đã được xem
  trực tiếp, popover/checkbox/scroll/footer không vỡ. Lượt chạy rộng hai class đạt `6/14`; tám test còn lại
  timeout ở CTA/action ngoài picker nên được ghi nhận riêng và không dùng làm verdict cho CSS ownership slice.
- FR8A active-toggle CSS ownership: chuyển block `.vpp-admin-active-switch` và Radzen switch variables khỏi
  global `vpp-admin.css` về companion scoped CSS của `DesignSystem/Composites/VppAdminActiveToggle`; giữ
  `::deep` vì switch là child component và ghi rõ lý do bằng comment tiếng Việt ngắn. Architecture focused
  `1/1`, full frontend `364/364`, solution Release build `0 warning/error`; Lookup màu track/thumb và
  User Administration responsive route-real pass `2/2` trên fixture cô lập.
- FR8A retired Atlas CSS cleanup: xóa 387 dòng thuộc 23 selector `.vpp-atlas-*` (67 occurrence) khỏi
  global `vpp-admin.css` sau tracked scan xác nhận production consumer bằng 0. History/Department Summary và
  Pending Approval đã dùng canonical `HistoryWorkspaceShell`/`PendingApprovalWorkspace`; architecture ratchet
  cấm selector legacy quay lại. Full frontend `366/366`, solution Release build `0 warning/error`; Department
  Summary, Pending Approval và all-28 runtime pass `3/3`. Dark/Print/axe ban đầu timeout tại user-menu action
  ngoài CSS slice, sau đó pass `1/1` trên fixture mới. Nhóm `vpp-order-view-*` được tách sang slice riêng vì
  có cả consumer thật và assertion legacy, không xóa gộp cùng Atlas.
- FR8A retired order-items CSS cleanup: audit tách rõ selector đang dùng `vpp-order-view-panel/meta/identity`
  với 7 nhóm grid-frame/filter/search/select/clear/footer/department-surface không còn production consumer.
  Xóa 103 dòng legacy khỏi `vpp-kpi.css`/`vpp-admin.css`; toolbar và filter hiện thuộc scoped owner
  `VppOrderItemsSurface.razor.css`. Architecture ratchet cấm đúng nhóm đã retire nhưng không cấm ba selector
  còn dùng. Full frontend `366/366`, solution Release build `0 warning/error`; My Orders responsive, History
  interaction/scroll parity và Department Summary responsive route-real pass `3/3` trên fixture cô lập.
- FR8A retired My Orders action/export CSS: xóa 89 dòng thuộc bốn nhóm
  `story-commands/story-actions/export-actions/export-button` sau repo scan xác nhận production consumer bằng
  0. Action hiện thuộc `vpp-data-card-actions`; PDF/XLSX thuộc `VppFileExportActions` với scoped responsive CSS.
  Architecture ratchet khóa owner thay thế và cấm selector cũ quay lại. Full frontend `370/370`, solution
  Release build `0 warning/error`; My Orders 4 viewport và real-file Order Exports pass `2/2` trên fixture cô lập.
- FR8B test-only cleanup: xóa `tests/Frontend.UiTests/Pages/Order/ProductCatalogPage.cs` sau repo-wide scan
  xác nhận chỉ còn declaration, không có consumer. Không sửa production source, test hiện hữu hoặc dirty
  `ProductCatalogTests.cs`; focused Product Catalog route/responsive tests, full frontend `364/364` và
  Release build giữ nguyên. Đây là deletion độc lập để giảm page-object rác, không đổi discovered test name
  hay assertion intent.
- FR8B Order page-object cleanup: xóa bốn helper `DepartmentSummaryPage`, `HistoryPage`, `OrderCreatePage`
  và `OrderManagementPage` (136 dòng vật lý) sau repo-wide scan xác nhận chỉ còn declaration/constructor. Build và
  UI test project/solution Release pass `0 warning/error`, discovery giữ `83` test và full frontend giữ
  `365/365`; `LoginPage`/`PermissionManagementPage` có consumer thật nên tiếp tục giữ. QA-001 được cập nhật
  để ghi rõ reference `OrderCreatePage.FillNotesAsync` chỉ là historical residual đã retire.
- FR8A IdentityAccess state ownership: chuyển `CurrentUserState`, `PermissionState` và
  `PermissionRefreshSignal` khỏi flat `Services/` vào `Features/IdentityAccess/State/`; chuyển hai state test
  tương ứng về `tests/Frontend.UnitTests/Features/IdentityAccess/State/`, cập nhật consumer usings và giữ
  `PermissionRealtimeService`/transport ở owner cũ. Architecture ownership gate mới, full frontend `365/365`,
  solution Release build `0 warning/error`; User Menu + Permission desktop/mobile + User Administration
  route-real pass `5/5` trên fixture cô lập. DI registration giữ nguyên; không đổi cache, refresh,
  event-dispose, claims hay authorization behavior.
- FR8A session-invalidation ownership: chuyển `AuthSessionInvalidationCoordinator` khỏi `Services/` phẳng
  vào `Platform/Auth`; generic `APIServices` vẫn chỉ gọi interface khi backend trả 401. DI tiếp tục `Scoped`
  để one-shot guard sống theo circuit. Unit test mới khóa reason rỗng, trim + URL-escape, `forceLoad` và gọi
  lặp không điều hướng lần hai; architecture ratchet cấm owner cũ quay lại. Full frontend `370/370`, solution
  Release build `0 warning/error` và logout route-real pass `1/1` trên fixture cô lập.
- FR8C code-reading sync: cập nhật `docs/CODE-READING-GUIDE.md` và `docs/architecture/ARCH-001-MODULE-MAP.md`
  theo feature/platform ownership hiện tại (`Program` composition, `Platform/Auth`, `Platform/State`,
  `Platform/Browser`, `Notifications/State`, account components), sửa reference `CurrentUserState` và bỏ
  test-count cũ. Đây là handoff đọc code cho thesis/slide; không thay đổi runtime.
- Quota: sanitized probe tiếp tục trả `404`; capacity chưa xác nhận. Thực thi theo checkpoint nhỏ theo
  chỉ đạo owner, không hạ model/effort hoặc bỏ gate để vừa quota.
- Next exact action: owner chốt correction workflow. Khuyến nghị `re-preview bắt buộc`: sau mỗi revision thành
  công, thay nút correction bị khóa bằng notice + CTA `Xem trước lại`; chỉ bật correction khi preview sinh key
  mới. Sau đó bổ sung mutation E2E hai user cho confirm/correct/four-eyes và chạy gate đóng FR7. FR8A/FR8B
  cleanup độc lập đã hết candidate có zero-consumer evidence; không tách `PermissionRealtimeService`,
  `vpp-interactions.js`, Step2 hoặc draft timer khi chưa có lifecycle/final-acceptance gate.
- Do not redo: UI-SYSTEM F0–F7, data-surface DS0–DS4/R1, source inventory, current best-practice
  research và unit/build baseline.
- Do not touch in FR0/FR1: backend, Shared DTO wire shape, database/migrations, React archive,
  owner `dotnet watch`, raw screenshot/trace outside ignored evidence folder.

## 16. Research sources

- [OpenAI — current GPT-5.6 model guidance](https://developers.openai.com/api/docs/guides/latest-model.md)
- [Microsoft — ASP.NET Core Razor components, naming and code-behind](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/?view=aspnetcore-10.0)
- [Microsoft — Blazor event handling and `EventCallback`](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling?view=aspnetcore-10.0)
- [Microsoft — Avoid overwriting component parameters](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/overwriting-parameters?view=aspnetcore-10.0)
- [Microsoft — Blazor call Web API](https://learn.microsoft.com/en-us/aspnet/core/blazor/call-web-api?view=aspnetcore-10.0)
- [Microsoft — .NET dependency injection guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines)
- [Microsoft — Blazor state management](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/?view=aspnetcore-10.0)
- [Microsoft — Blazor authentication state](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/authentication-state?view=aspnetcore-10.0)
- [Microsoft — Razor component lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0)
- [Microsoft — Razor component disposal](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/component-disposal?view=aspnetcore-10.0)
- [Microsoft — Blazor JavaScript interoperability and DOM cleanup](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0)
- [Microsoft — Blazor rendering performance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/rendering?view=aspnetcore-10.0)
- [Microsoft — Blazor CSS isolation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation?view=aspnetcore-10.0)
- [Microsoft — C# identifier naming conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)
- [Microsoft — Common C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Microsoft — .NET unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [W3C WAI — Page structure](https://www.w3.org/WAI/tutorials/page-structure/)
- [W3C WAI — Form labels](https://www.w3.org/WAI/tutorials/forms/labels/)
- [WCAG 2.2 — Focus visible](https://www.w3.org/WAI/WCAG22/Understanding/focus-visible)
- [WCAG 2.2 — Status messages](https://www.w3.org/WAI/WCAG22/Understanding/status-messages)
- [Martin Fowler — Refactoring](https://martinfowler.com/books/refactoring.html)
- [Martin Fowler — Workflows of refactoring](https://martinfowler.com/articles/workflowsOfRefactoring/fallback.html)
