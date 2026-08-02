# FRONTEND-REFACTOR-001 — FR0 provisional baseline và consumer/debt ledger

> Canonical ledger của FR0. Không nhân bản các con số này sang file khác; plan chính chỉ link về đây.

## 0. Bản một ánh nhìn

| Mục | Kết quả 2026-08-03 |
|---|---|
| Trạng thái | **FR0 PASS trong scope frontend**; provisional baseline đã khóa, chưa phải golden/final visual acceptance |
| Baseline source | Branch `codex/ai-agent-foundation`, start HEAD `c6ca07bd`; hai correction UI của owner được giữ nguyên trong working tree |
| Thay đổi FR0 | Sửa specificity của canonical filter chrome; thêm route-real responsive baseline riêng; lập reading map và ledger |
| Gate | Preflight PASS; frontend unit `214/214`; Release build `0 warning / 0 error`; Product Catalog `1/1`; responsive `4/4`; User Menu `1/1` |
| Visual evidence | Đã xem trực tiếp 9 PNG runtime thật; screenshot là evidence trong `TestResults/`, không phải golden image và không commit |
| Blocker ngoài scope | `verify -Scope frontend` vẫn dừng đúng signature `model-routing-eval`: `62 pass / 1 fail` trong dirty AI-harness |
| Quyết định tiếp theo | Chưa mở FR1 cho đến khi quota probe có snapshot dùng được và blocker verify được sửa hoặc owner duyệt waiver đúng signature |

## 1. Baseline manifest

### 1.1 Correction UI của owner được bảo toàn

| File | Nội dung diff hiện hữu | SHA-256 working file | Quyền sở hữu |
|---|---|---|---|
| `src/Frontend/Blazor/wwwroot/css/vpp-polish.css` | Loại `.rz-close` khỏi upward transient animation | `DB7E810808A1DC281788C59A944BF1A4FFD8792E91E8735F7164F66585A2F1C1` | Owner dirty; FR0 không stage/commit |
| `tests/Frontend.UiTests/Tests/Order/ProductCatalogTests.cs` | Đóng page-size popup trước khi đo workspace geometry | `FA439D5BD36A09A53D571F77FC1B57A30488350B5C581CDA8B59836DA72C7C7A` | Owner dirty; FR0 không stage/commit |

Baseline này phụ thuộc đúng hai working-file hash trên. Khi owner sửa tiếp hoặc commit correction, cập nhật
hash/evidence trước slice frontend kế tiếp bị ảnh hưởng.

### 1.2 Inventory tái chạy được

| Nhóm | Snapshot FR0 | Ý nghĩa |
|---|---:|---|
| Physical Razor routes | 17 `@page` | Một page vật lý có thể chứa nhiều tab/sub-route logic |
| Logical routes | 44: 33 authenticated, 11 anonymous | Authority là `RouteCatalog`; final gate phải ledger đủ 44 key |
| `IAPIServices` | 32 source file; 26 page/component consumer | Generic transport đang lan vào feature UI |
| `GlobalClass` | 21 file có symbol; 19 production consumer sau khi bỏ declaration/composition | Identity và busy state còn trộn |
| `PermissionState` | 22 file có symbol; 20 consumer sau khi bỏ declaration/composition | Giữ contract, chỉ move ownership khi có characterization |
| `NavigateTo(...)` | 42 call trong 25 file | Route string còn phân tán |
| Direct `"/api/` literal | 34 dòng trong 8 file | Chưa tính endpoint constant và query fragment khác |
| Authored CSS | 55 file | Không tính `bin`, `obj`, Bootstrap vendor |
| `!important` | 525 occurrence trên 524 dòng sau FR0 | Debt signal; không đặt mục tiêu cơ học về 0 |
| Inline HTML `style=` | 4 dòng trong 2 file | Anchor positioning của filter/column picker; không tự động là rác |
| Component/Radzen `Style=` | 133 occurrence trên 128 dòng/43 file | Migration-on-touch về owner gần nhất |
| Authored JavaScript | 7 file, khoảng 1.658 dòng | `vpp-interactions.js` là hotspot 1.165 dòng |
| UI test discovery | 82 test case | Tăng 4 case do viewport matrix FR0 |

## 2. Reading map — Product Catalog

Đọc theo thứ tự này để nắm màn hình mà không phải mở toàn repository:

1. `Helpers/RouteCatalog.cs`: key `dashboard.catalog`, path `/dashboard?tab=2`, page/permission metadata.
2. `Helpers/UiRouteCatalog.cs`: collection + server paging + rich two-line + filter toolbar + paged footer.
3. `Components/Pages/VPPRequest/Page_VPPRequest.razor(.cs)`: physical route `/dashboard/{Per?}`, auth/page gate và compatibility redirect.
4. `Components/Pages/VPPRequest/Component_VPPRequest.razor(.cs)`: đổi logical `tab=2` sang Radzen selected index sau khi lọc quyền.
5. `Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor`: loading, toolbar, error, filtered-empty, grid và denied state.
6. `Tab_ProductCatalog.razor.cs`: local state, load categories/units/products, debounce, paging và query builder.
7. `Services/APIServices.cs` + `Helpers/Config.cs`: auth/error/JSON/total-count transport và endpoint constants.
8. `Components/DesignSystem/**` + `Tab_ProductCatalog.razor.css`: visual owner gần route.
9. `wwwroot/css/vpp-radzen-theme.css`, `vpp-datagrid.css`, `vpp-polish.css`: Radzen bridge, sort/pager và popup motion.
10. `ProductCatalogTests.cs` + `ProductCatalogResponsiveBaselineTests.cs`: interaction contract và bốn viewport bắt buộc.

Điểm dễ nhầm: `tab=2` là logical query index, không phải vị trí cố định trong Radzen tabs. User thiếu quyền
ở tab trước làm `SelectedIndex` thay đổi, nên phải giữ translation trong `Component_VPPRequest`.

## 3. Canonical consumer/debt ledger

### 3.1 Navigation

| Owner/candidate | Evidence | Class | Hướng xử lý |
|---|---|---|---|
| `RouteCatalog` | 44 logical route | `KEEP` | Runtime/navigation authority dần dùng typed path builder |
| `UiRouteCatalog` | Route → workspace/motif/state profile | `KEEP` | Giữ visual contract tách khỏi authorization |
| Sidebar permission/path arrays + header tab builder | `LeftSidebar.razor.cs` khai báo route lần hai | `MERGE/MIGRATE` | Chuyển structural route consumer ở FR2/FR3; visual polish vẫn FR8 |
| 42 `NavigateTo(...)` call | 25 file | `MIGRATE_ON_TOUCH` | Không mass rewrite; ưu tiên hotspot và deep-link có test |

### 3.2 State và session

| State | Consumer hiện tại | Class | Boundary đích |
|---|---|---|---|
| `CurrentUserState` | `AuthHelper` trực tiếp; sau đó copy sang `GlobalClass.UserInfo` | `KEEP` | UI profile projection/cache từ `/me`, không phải backend authorization authority |
| `GlobalClass` | Identity, busy counter và legacy field trong 19 consumer | `MIGRATE` | Identity → `CurrentUserState`; busy → `UiBusyState`; retire khi ledger bằng 0 |
| `PermissionState` | 20 consumer | `KEEP` | UI visibility/navigation; backend/API vẫn enforce authorization |
| `ThemeState` | `App`, `LeftSidebar` | `KEEP` | Platform UI state |
| `NotificationInboxState` | `MainLayout`, `NotificationCenter` | `KEEP/MIGRATE` | Notifications module |
| `PeriodSettlementState` | Workspace/panel settlement | `KEEP/MIGRATE` | Settlement module |

Scoped Interactive Server state chỉ sống theo circuit. State cần sống qua refresh/reconnect phải có owner
durable rõ: URL, browser storage, persistent component state hoặc backend.

### 3.3 API transport

| Candidate | Evidence | Class | Hướng xử lý |
|---|---|---|---|
| Auth header, 401/403, JSON, total count trong `APIServices` | Cross-cutting behavior đã có test | `KEEP` | Characterize trước; không rewrite transport big-bang |
| Generic `IAPIServices` surface | 32 file, 26 page/component | `MIGRATE` | Typed feature clients theo cohesive use case |
| Raw endpoint/query builder trong page | 34 direct `/api/` literal; paging/filter phân tán | `MIGRATE_ON_TOUCH` | Feature client sở hữu endpoint/query; page chỉ gọi use case |
| `IAPIServices.SetBaseUrl` | Chỉ declaration + implementation | `DELETE` | Xóa ở cleanup slice có build/DI gate |
| Direct `IHttpClientFactory` trong Account pages | Account/session concern phân tán | `MIGRATE` | `AccountApiClient` hoặc capability nhỏ, không tạo universal god transport mới |

### 3.4 Shared component ownership

| Component/candidate | Consumer | Class | Hướng xử lý |
|---|---:|---|---|
| `VppIcon` | 73 callsite / 38 file | `KEEP` | DesignSystem primitive |
| `SkeletonPage` | 13 callsite / 11 file | `KEEP` | Shared loading pattern |
| `SkeletonGrid`, `SkeletonStatCards` | Chỉ `SkeletonPage` | `MERGE_CANDIDATE` | Có thể thành internal implementation |
| `VppColumnPicker` | 11 callsite / 10 file | `KEEP/ISOLATE` | Reflection Radzen là compatibility risk, không phải rác |
| Account-only `VppInlineAlert`, `VppPasswordField`, `VppLanguageSwitch` | Chỉ Account flow | `MIGRATE` | IdentityAccess/Account ownership |
| `VppPageHeader` | Error + NotFound | `MIGRATE` | DesignSystem composite khi chạm route system |

Không bắt buộc module có đủ `Pages/Components/Dialogs/Api/State`; chỉ tạo folder khi có file và consumer thật.

### 3.5 CSS và JavaScript

| Candidate | Evidence | Class | Hướng xử lý |
|---|---|---|---|
| `HistoryWorkspaceShell.razor.css` | Khoảng 1.578 dòng, 301 `::deep` | `MIGRATE_ON_TOUCH` | Tách theo behavior/consumer đã chứng minh, không cắt theo số dòng |
| Global selector ownership chồng nhau | Sidebar/layout/order/data/admin grid ở nhiều file | `NEEDS_VERIFY` | Selector/computed-style ledger trước move/delete |
| `vpp-interactions.js` | 1.165 dòng: dropdown/a11y, theme/download, tabs, sidebar, observer | `MIGRATE` | Split sau lifecycle/re-init/long-session evidence |
| 6 feature/component JS module nhỏ | 16–192 dòng | `KEEP` | Chỉ module sở hữu listener/observer/resource mới cần explicit dispose/re-init |

FR0 phát hiện global native-button reset có `box-shadow: none !important`, làm canonical
`VppFilterSelect` mất inset control border. Sửa tại scoped owner bằng ngoại lệ có comment; đây là control
chrome, không phải elevation shadow. Không nới selector global và không sửa test để bỏ assertion.

### 3.6 Assets và packages

| Candidate | Evidence | Class | Gate trước xóa |
|---|---|---|---|
| 13 asset cũ khoảng 9 MB không có literal reference | Login backgrounds/logo/font/demo image cũ | `DELETE_CANDIDATE` | Publish/static manifest + runtime network log + screenshot parity |
| `OFL-Poppins.txt` | License đi cùng font Poppins | `KEEP` | Không xóa chỉ vì không có callsite |
| 43 Bootstrap sibling file không được link | Chỉ `bootstrap.min.css` được nạp | `DELETE_CANDIDATE` | Publish/static smoke |
| `bootstrap.min.css` | Có link trong `App.razor` | `NEEDS_VERIFY` | Kiểm reset/utility runtime trước removal |
| `Newtonsoft.Json` | Chỉ package declaration | `DELETE_CANDIDATE` | Direct/transitive audit + build/publish |
| Serilog package family | Chưa có runtime configuration/callsite frontend | `DELETE_CANDIDATE` | Direct/transitive audit + startup/log smoke |
| SignalR Client/Core | Realtime services đang dùng | `KEEP/NEEDS_TRANSITIVE_AUDIT` | Chỉ bỏ package trùng khi restore/build chứng minh |

## 4. Cleanup ledger

| Candidate | Class | Lý do / blocker |
|---|---|---|
| `Helpers/ObjectExtensions.cs` | `DELETE` | Hai method chỉ còn declaration |
| `IAPIServices.SetBaseUrl` | `DELETE` | Không có consumer |
| `GlobalClass.BaseUrl`, `CurrentLanguage` | `DELETE` | Không có consumer |
| Nested legacy model trong `Tab_Orders` | `DELETE` | Không có consumer ngoài declaration |
| Empty `Dispose()` ở Item/Department Library | `DELETE` | Không sở hữu resource |
| Product Catalog parameter `claims` | `DELETE` | Được truyền nhưng không đọc |
| Product Catalog nested `ProductItem`, `CategoryItem` | `MERGE` | Trùng subset Shared DTO; chọn Shared DTO hoặc projection có mapping rõ |
| `ProductCatalogPage` test page object | `DELETE` | Repo scan chỉ thấy declaration |
| `GlobalStorageModel`/Sidebar legacy storage chain | `NEEDS_VERIFY` | Cần kiểm compatibility với browser storage key cũ |
| `VppInlineAlert` và `VppInlineNotice` | `MERGE_NEEDS_VERIFY` | Visual gần nhau nhưng copy/consumer contract khác |

`DELETE` ở ledger nghĩa là usage scan hiện tại đủ rõ để đưa vào FR1 candidate, không cho phép xóa hàng loạt
ngoài một focused slice có build/test/diff review.

## 5. Product Catalog characterization còn thiếu

| Finding | Trạng thái | Gate cần thêm trước migration |
|---|---|---|
| UI permission `REQUEST_PRODUCT_CATALOG` khác API action `REQUEST_CATALOG_VIEW` | `NEEDS_VERIFY` | Role/direct-link/403 matrix |
| Retry first-load có thể no-op khi `productGrid` chưa được tạo | `NEEDS_VERIFY` | Error injection → retry runtime test |
| Route profile có empty/disabled nhưng component chỉ render filtered-empty/denied | `NEEDS_VERIFY` | State matrix và wording decision |
| Backend trả localized display fields nhưng nested wire model không đọc | `NEEDS_VERIFY` | VI/EN data contract test |
| Unit filter tải distinct values theo batch | `NEEDS_VERIFY` | Dataset lớn + total-count/request-count test |
| Debounce chỉ hủy delay, không hủy HTTP đang chạy | `NEEDS_VERIFY` | Out-of-order response characterization |
| Grid chưa opt-in `data-vpp-grid-region=true` | `NEEDS_VERIFY` | Keyboard/a11y normalizer test |
| Search/category/unit/clear/page/page-size chưa có behavior test đầy đủ | `NEEDS_VERIFY` | Request-query assertions trước typed-client migration |

## 6. FR0 verification record

| Gate | Kết quả |
|---|---|
| `./scripts/gtas.cmd preflight -Scope frontend` | PASS |
| Quota probe local | HTTP `404` hai lần; không có capacity snapshot, giữ quyết định `SLICE_ONLY` |
| Initial Product Catalog run | FAIL đúng assertion chrome: global `button ... box-shadow: none !important` thắng scoped filter CSS |
| Root-cause correction | Scoped `VppFilterSelect` giữ canonical inset border bằng explicit exception; không đổi backend/API/business |
| Focused Product Catalog | PASS `1/1`; 3 PNG: surface seam, upward page-size popup, narrow shared frame |
| Responsive Product Catalog | PASS `4/4`: `390×844`, `768×1024`, `1366×768`, `1920×1080` |
| User Menu regression | PASS `1/1`; selector `.is-above` vẫn ổn ở expanded/collapsed sidebar |
| Frontend unit/architecture | PASS `214/214` |
| Release build | PASS `0 warning / 0 error` |
| UI discovery | 82 test case |
| Manual image review | PASS cho containment/popup/menu evidence; owner final visual acceptance vẫn pending |
| `./scripts/gtas.cmd verify -Scope frontend` | FAIL trước frontend gate, exact known blocker `model-routing-eval`: `62/63` agent setup checks |

Evidence runtime nằm trong ignored `TestResults/FR0-*`; không commit PNG/TRX và không dùng làm golden baseline.

## 7. FR1 entry gate

FR1 chỉ mở khi đồng thời:

1. quota/capacity đã được kiểm tra; nếu probe vẫn lỗi thì phải ghi rõ thiếu coverage và chỉ mở checkpoint
   độc lập theo chỉ đạo owner, không hạ quality gate; và
2. `model-routing-eval` pass hoặc có waiver đúng exact signature; và
3. hai correction owner dirty vẫn còn đúng hash hoặc đã được owner commit/cập nhật baseline; và
4. FR1 chỉ chọn một cleanup slice nhỏ, ưu tiên zero-consumer code/package candidate, không mass move.

Tại checkpoint 2026-08-03: điều kiện 1 đi theo owner-directed checkpoint vì probe tiếp tục `404`;
điều kiện 2 đã pass `63/63`; điều kiện 3 giữ nguyên hash; FR1A đáp ứng điều kiện 4.

Candidate khuyến nghị đầu tiên: cleanup zero-consumer thuần C# (`ObjectExtensions`, `SetBaseUrl`, legacy
member/type rõ ràng) trong một commit riêng; chưa mở typed-client/state/module migration ở cùng slice.

## 8. FR1A execution record — zero-consumer C#

Scope đã triển khai:

- xóa `Helpers/ObjectExtensions.cs`;
- bỏ `IAPIServices.SetBaseUrl` và implementation;
- bỏ `GlobalClass.BaseUrl`, `GlobalClass.CurrentLanguage`;
- bỏ ba nested model cũ trong `Tab_Orders`;
- bỏ `IDisposable` và `Dispose()` rỗng ở Item/Department Library.

| Gate | Kết quả |
|---|---|
| Repo-wide usage scan sau cleanup | PASS — không còn declaration/callsite mục tiêu |
| `dotnet format ... --verify-no-changes --include <FR1A files>` | PASS |
| `./scripts/gtas.cmd test-frontend` | PASS `214/214` |
| `dotnet build gtas_vpp.slnx -c Release --no-restore` | PASS `0 warning / 0 error` |
| My Orders focused smoke | PASS `1/1` |
| Item/Department editor smoke | BASELINE-MATCH — cùng exact timeout trên clean detached `b739288d`; không phải FR1A regression |
| Owner dirty correction hashes | UNCHANGED — `vpp-polish.css` và `ProductCatalogTests.cs` giữ đúng FR0 hash |

FR1A không đổi route, API endpoint, DTO, permission hoặc visual CSS và đạt non-regression gate. Failure
Item/Department được giữ thành fixture/permission debt cho final UI acceptance/B0R; không được che bằng
cách giảm assertion.

## 9. FR1B execution record — package-only cleanup

Đã bỏ top-level `Newtonsoft.Json` và toàn bộ Serilog package family khỏi frontend, đồng thời xóa
`using Serilog` không dùng. SignalR và Radzen giữ nguyên.

| Gate | Kết quả |
|---|---|
| Repo-wide runtime/config scan | PASS — không còn callsite/config Newtonsoft hoặc Serilog trong frontend/tests |
| Direct/transitive package audit | PASS — chỉ còn Radzen và SignalR top-level; không còn Serilog transitive tree |
| Restore + Release build | PASS `0 warning / 0 error` |
| Frontend unit/architecture | PASS `214/214` |
| Account startup/auth + report smoke | PASS `2/2` |
| Release publish artifact audit | PASS — không có DLL/file Newtonsoft hoặc Serilog |

## 10. FR1C execution record — dead Product Catalog parameter

Đã bỏ parameter `claims` khỏi `Tab_ProductCatalog` và callsite duy nhất vì component sử dụng
`PermissionState` làm authority, không đọc parameter này.

| Gate | Kết quả |
|---|---|
| Consumer/usage scan | PASS — parameter chỉ có một producer và không có read |
| Format verify + Release build | PASS |
| Frontend unit/architecture | PASS `214/214` |
| Product Catalog route-real | PASS `1/1` |

FR1 hoàn tất. Static asset cleanup được dời sang FR8A và test-only `ProductCatalogPage` được dời sang
FR8B để production cleanup, package cleanup và test refactor không bị trộn trong cùng wave.

## 11. FR2 transport characterization

`ApiServicesJsonTransportTests` đã được mở rộng để khóa các contract dùng lại khi feature client dần
thay raw endpoint trong page:

- bearer token lấy từ authentication state;
- fallback total count từ collection khi header thiếu;
- `204`/content rỗng trả default, không parse lỗi;
- export giữ file name, MIME, length và bytes;
- 401 session invalidation, 403 permission refresh và safe ProblemDetails giữ nguyên.

Focused transport suite PASS `11/11`.

## 12. FR2 Reports pilot

Đã tạo `Features/Reports/Api/ReportsApiClient.cs`. Client sở hữu query builder, summary/insight endpoint
và mapping PDF/XLSX/CSV; `Report.razor.cs` chỉ còn permission, UI state, localization, feedback và
derived presentation data.

| Gate | Kết quả |
|---|---|
| Reports client + architecture focused unit | PASS `7/7` |
| Frontend unit/architecture | PASS `224/224` |
| Release build | PASS `0 warning / 0 error` |
| UI discovery | 83 test case |
| Report responsive + combined Pricing/Report | PASS `2/2`, gồm 4 viewport |
| English contract + real report exports | PASS `2/2`; PDF/XLSX/CSV name, bytes và signature thật |
| Dark mode + Print CSS + axe | PASS `1/1` |

Lần chạy đầu của combined Pricing/Report fail tại pricing lifecycle menu trước khi vào Report; report
English/export vẫn pass. Thêm focused Report test để tách gate theo owner và lượt chạy lại cả focused +
combined đều pass. Không sửa assertion để che failure.

## 13. FR2 UiBusyState

Đã thay busy setter `true/false` trong `GlobalClass` bằng `Platform/State/UiBusyState` dạng lease. Mỗi
operation dùng `using var busy = BusyState.Begin()` nên tự cân bằng khi success, exception hoặc return
sớm; dispose lặp không làm counter âm.

Consumer đã migrate:

- `PermissionAwarePageBase`;
- User Administration;
- Page Permission Administration;
- global loader/subscription trong `LeftSidebar`.

`GlobalClass` hiện chỉ còn user projection tạm thời cho FR3; repo scan không còn `isBusyPage` hoặc
`BusyChanged` legacy.

| Gate | Kết quả |
|---|---|
| UiBusy focused unit + environment contract | PASS `3/3` |
| Frontend unit/architecture | PASS `225/225` |
| Release build | PASS `0 warning / 0 error` |
| Admin User + Admin Permission browser | PASS `2/2` |
| User Menu browser | PASS `2/2` |
| `./scripts/gtas.cmd verify -Scope frontend` | PASS; agent setup `63/63`, UI lightweight `2/2`, audit/leak checks pass |

`GlobalRenderFlowTests` được ghi là flaky debt: clean baseline `d5b7c19a` pass `1/1`; trên source mới
một lượt timeout lúc notification action chưa xuất hiện, lượt chẩn đoán sau notification đã xuất hiện
nhưng timeout ở full-page `/not-found` navigation. Hai failure signature khác nhau, trong khi User Menu
và các consumer busy trực tiếp đều pass; chưa coi đây là regression của `UiBusyState`, nhưng phải
hardening trước final UI acceptance.

## 14. FR3.0 identity/permission characterization

Đã thêm direct unit test trước khi thay authority hoặc gom account HTTP:

- `CurrentUserState`: concurrent load chỉ gọi `/me` một lần, cache, null retry và invalidate;
- `AuthHelper`: anonymous không gọi profile, authenticated projection từ server + access token,
  profile thiếu không overwrite legacy projection và permission null trả snapshot rỗng;
- `PermissionState`: map claim/page/component/action/group/version, route fallback, refresh signal và
  anonymous reset;
- test doubles dùng chung cho API và mutable authentication state, không thêm mocking package.

| Gate | Kết quả |
|---|---|
| CurrentUser + AuthHelper + Permission focused | PASS `10/10` |
| Frontend unit/architecture | PASS `235/235` |
| Production source | Không đổi trong slice characterization này |

Nợ được ghi nhận, chưa sửa trong test-only slice: permission `EnsureLoadedAsync` có thể chạy hai refresh
nối tiếp khi concurrent; `PermissionState` subscribe refresh signal nhưng chưa có dispose contract;
legacy `GlobalClass.UserInfo` chỉ được retire sau khi sidebar/consumer chuyển sang `CurrentUserState`.

## 15. FR3 account transport foundation

Đã tách `Platform/Api/ApiProblemReader` khỏi private parser trong `APIServices` và tạo
`Features/IdentityAccess/Api/AccountApiClient`.

Boundary:

- public register/confirm/resend/recovery/reset dùng typed `HttpClient`, không gắn bearer và không gọi
  session invalidation khi backend trả `401` hợp lệ;
- change password tiếp tục đi qua authenticated `IAPIServices`;
- client sở hữu endpoint/query/serialization; localization và navigation vẫn thuộc page;
- chưa tạo interface/base client/DI extension một-consumer.

| Gate | Kết quả |
|---|---|
| ApiProblem + AccountApiClient + legacy transport focused | PASS `22/22` |
| Frontend unit/architecture | PASS `246/246` |
| Release build | PASS `0 warning / 0 error` |

Contract test khóa root `code`, nested `extensions`, safe detail, malformed fallback, bốn public POST,
confirm-email token encoding, public `401` không dùng authenticated transport và change-password dùng
đúng authenticated endpoint. Chưa có page migration trong foundation commit.

## 16. FR3.1 public account migration — recovery/resend

Đã migrate hai public flow đơn giản nhất:

- `ForgotPassword` → `AccountApiClient.RequestPasswordRecoveryAsync`;
- `ResendConfirmation` → `AccountApiClient.ResendConfirmationAsync`.

Hai page không còn tự tạo named `HttpClient`, dựng endpoint, đọc JSON response hoặc parse error body.
Submit handler dùng chính request parameter thay vì bỏ qua `_submittedModel`; page vẫn sở hữu loading,
localized success/error copy và form state.

| Gate | Kết quả |
|---|---|
| Format verify từng page | PASS |
| Frontend unit/architecture | PASS `246/246` sau mỗi checkpoint |
| Anonymous account routes, 7 route × 3 viewport | PASS `1/1` sau mỗi checkpoint |

Public `ApiRequestException` được map qua `AccountLifecycleUiMapper`; network failure vẫn dùng
`RecoveryConnectionFailed`. Markup, route và navigation không đổi.

## 17. FR3.1 public account migration — complete

Ba public flow còn lại đã migrate:

- Register → `RegisterAsync`;
- Reset Password → `ResetPasswordAsync`;
- Confirm Email → `ConfirmEmailAsync`, giữ interactive-only guard để không gửi side effect hai lần.

`Config` đã bỏ năm public account endpoint constant zero-consumer; endpoint authority nằm trong
`AccountApiClient`. Login vẫn là public client riêng ở checkpoint kế tiếp; ChangePassword vẫn dùng
authenticated transport.

| Gate | Kết quả |
|---|---|
| Frontend unit/architecture | PASS `246/246` |
| Registration mutation lifecycle | PASS `1/1` trên isolated LocalDB với explicit mutation opt-in |
| ConfirmEmail prerender + account route focused | PASS `35/35` |
| Anonymous account routes | PASS `1/1`, 7 route × 3 viewport sau public migration hoàn tất |

Một validation-layout run fail 2 px tại registration desktop (`page=771`, viewport `768`, card `747`),
trong khi anonymous route matrix pass. Đây là visual/harness debt thuộc final UI acceptance, không nằm
trên HTTP/page-state code đã migrate; không nới geometry assertion trong refactor.

## 18. FR3 login migration

Đã tạo `AuthenticationApiClient` cho public `POST /api/Auth/login` và migrate `LoginPage` khỏi
`IHttpClientFactory`. Client sở hữu endpoint, JSON và status-aware exception; page vẫn sở hữu localized
feedback, `LoginTicketCache`, remember-me, returnUrl và full-page `/perform-login` navigation.

| Gate | Kết quả |
|---|---|
| Authentication client + login policy/cache focused | PASS `14/14` |
| Frontend unit/architecture | PASS `251/251` |
| Release build | PASS `0 warning / 0 error` |
| Invalid login browser | PASS `1/1`, 3 viewport |
| Valid login browser | PASS `1/1` khi chạy riêng trên isolated fixture |

Lượt chạy gộp invalid + valid có valid-login timeout sau nhiều invalid attempt trên cùng fixture. Vì
valid-login pass trên fixture mới và invalid-login cũng pass độc lập trong lượt gộp, đây được ghi là
test-interaction/throttling debt; không nới timeout hoặc sửa assertion để che hiện tượng.

## 19. FR3 authenticated password-change migration

`ChangePassword` đã chuyển từ direct `IAPIServices` sang authenticated method của `AccountApiClient`.
Page chỉ còn form/loading/localized message và full-page `/perform-logout` sau thành công; endpoint
`/api/account/password/change` chỉ còn một owner trong feature client.

| Gate | Kết quả |
|---|---|
| Account client + lifecycle route focused | PASS `31/31` |
| Frontend unit/architecture | PASS `259/259` |
| Release build | PASS `0 warning / 0 error` |

Architecture test mới khóa toàn bộ account page code-behind không được inject lại
`IHttpClientFactory` hoặc `IAPIServices`. Logout/cookie/session revocation flow không đổi trong slice này.

## 20. FR3 shell identity migration

`LeftSidebar` và `UserMenu` đã đọc hồ sơ hiển thị trực tiếp từ `CurrentUserState.Current`. Bản projection
`GlobalClass.UserInfo` không còn là source của tên, login, email, vai trò hoặc phòng ban trong shell.
Sidebar subscribe/unsubscribe `CurrentUserState.Changed` cùng lifecycle hiện có để refresh đúng khi hồ sơ
bị invalidate/reload.

| Gate | Kết quả |
|---|---|
| State + shell architecture focused | PASS `66/66` |
| Release build | PASS `0 warning / 0 error` |
| User Menu route-real | PASS `2/2` trên isolated fixture |

`GlobalClass` chưa xóa ở checkpoint này vì một số Library mutation/editor còn dùng user id cũ; các
consumer đó sẽ chuyển sang `CurrentUserState` trong FR4 trước khi xóa projection và DI registration.

## 21. FR3 shell navigation catalog

Đã thêm `ShellNavigationCatalog` gồm 4 section và 17 leaf item. Mỗi item chỉ giữ metadata shell
(label/icon/group/default/compatibility alias) và resolve path/page/permission từ `RouteCatalog`.
`LeftSidebar`/primary header đã dùng catalog này; ba route array, literal path và ba hàm permission theo
section cũ đã bị xóa.

| Gate | Kết quả |
|---|---|
| Route + shell catalog focused | PASS `7/7` |
| Shell/permission architecture focused | PASS `61/61` |
| Frontend unit/architecture | PASS `262/262` |
| Release build | PASS `0 warning / 0 error` |
| Sidebar controls | PASS `1/1` |
| Nested Period/Pricing header | PASS `1/1` khi chạy isolated riêng |
| Desktop inactive/hover header | PASS `1/1` khi chạy isolated riêng |

Lượt chạy gộp ba browser test có hai timing failure ở animation/hover; từng test fail đều pass trên
fixture mới. Không đổi CSS, timeout hay assertion. `RouteCatalog` vẫn ở path authority hiện tại vì skill
repository đang có thay đổi của owner và còn trỏ tới `Helpers/RouteCatalog.cs`.

## 22. FR3 shell zero-consumer cleanup

Đã xóa `dropdownDataModels_Company`, `selected_Company`, `State`, `LoadStateAsync` và read storage key
`CostingSetting` khỏi `LeftSidebar`. `DropdownModel`/`GlobalStorageModel` không còn consumer nên được xóa;
global Razor import và bốn C# using thừa cũng được dọn.

| Gate | Kết quả |
|---|---|
| Release build | PASS `0 warning / 0 error` |
| Shell + prerender focused | PASS `39/39` |

Không xóa sidebar preference/theme/language storage vì chúng vẫn có runtime consumer và test bảo vệ.

## 23. FR4 Lookup feature client

Đã tạo `Features/CatalogPricing/Api/LookupApiClient` làm owner của hai endpoint Lookup. Client dựng
filter/search/status/order/paging, tự về trang đầu khi page hiện tại rỗng sau mutation, và gom dependency
impact, soft-delete, create/update/hard-delete. `Tab_LookupLibrary` chỉ còn state grid/selection/toast;
hai dialog dùng typed client và `CurrentUserState`.

| Gate | Kết quả |
|---|---|
| Lookup client contract | PASS `3/3` |
| Lookup + catalog architecture focused | PASS `29/29` |
| Frontend unit/architecture | PASS `266/266` |
| Release build | PASS `0 warning / 0 error` |
| Lookup master-detail route-real | PASS `1/1` |
| Lookup mutation | BASELINE DEBT — current và pre-FR4 `e597e484` cùng timeout tại Add action |

Worktree đối chứng tạm đã được xóa sau khi xác nhận non-regression. Không nới permission assertion hay
ép hiển thị nút; fixture/permission seed sẽ được xử lý ở UI final acceptance/backend B0R.

## 24. FR4 Category feature client

`CatalogApiClient` đã được tạo với category server query và mutation API. `Tab_CategoryLibrary` không
còn tự dựng filter/query/endpoint; `Dialog_CategoryEditor` và grid dùng typed methods, còn audit user
lấy từ `CurrentUserState`.

| Gate | Kết quả |
|---|---|
| Category client contract | PASS `2/2` |
| Catalog/architecture focused | PASS `29/29` |
| Frontend unit/architecture | PASS `268/268` |
| Release build | PASS `0 warning / 0 error` |
| Category grid/editor browser | BASELINE PERMISSION DEBT — Add action không visible trước API call |

`CanModify` và `PagePermissionResDTO` không đổi trong slice; failure xảy ra trước khi bất kỳ method của
`CatalogApiClient` được gọi. Không ép enable action hoặc sửa assertion để che fixture seed thiếu quyền.

## 25. FR4 Supplier feature client

`CatalogApiClient` đã mở rộng cho Supplier: server query, dependency impact, create/update, status và
hard-delete. `Tab_SupplierLibrary`/`Dialog_SupplierEditor` chỉ điều phối UI và đọc audit user từ
`CurrentUserState`.

| Gate | Kết quả |
|---|---|
| Catalog client focused | PASS `3/3` |
| Catalog/architecture focused | PASS `30/30` |
| Frontend unit/architecture | PASS `269/269` |
| Release build | PASS `0 warning / 0 error` |

Không chạy lại editor Add browser ở checkpoint này vì cùng permission gate đã fail trước API call ở
Lookup/Category; route-real matrix sẽ được chạy sau khi fixture seed được sửa tại final acceptance/B0R.

## 26. FR4 Department feature client + readability cleanup

`CatalogApiClient` đã mở rộng cho Department, gồm active list dùng cho parent dropdown, server paging,
dependency impact, create/update/status/hard-delete. Hai file Department dạng one-line trước đây được
format lại thành các method/markup block có tên và control flow rõ, không đổi rule parent/self-parent.

| Gate | Kết quả |
|---|---|
| Catalog client focused | PASS `4/4` |
| Catalog/architecture focused | PASS `31/31` |
| Frontend unit/architecture | PASS `270/270` |
| Release build | PASS `0 warning / 0 error` |

Department editor route-real Add vẫn thuộc permission-fixture debt đã biết, nên checkpoint này dùng
contract/build/unit làm behavior gate và không sửa permission chỉ để test mở dialog.

## 27. FR4 Item feature client + readability cleanup

`CatalogApiClient` đã mở rộng cho Item: category/UOM reference data, server query với search/category/UOM,
typed `VppItemCreateRequest`/`VppItemUpdateRequest`, status endpoint và hard-delete. Grid và dialog được
format lại từ one-line code thành các method/markup block dễ lần theo.

| Gate | Kết quả |
|---|---|
| Catalog client focused | PASS `5/5` |
| Catalog/architecture focused | PASS `32/32` |
| Frontend unit/architecture | PASS `271/271` |
| Release build | PASS `0 warning / 0 error` |

Item editor browser Add thuộc permission-fixture debt đã biết và đã tồn tại trước slice; architecture
test mới khóa page/dialog chỉ gọi typed client, không tự dựng endpoint hoặc inject `IAPIServices`.

## 28. FR4 Price List lifecycle client

Đã tạo `PricingApiClient` và migrate `Tab_PriceListLibrary`. Client sở hữu supplier lookup, filter/search/
paging/sort và toàn lifecycle Price List: create, update, deactivate/restore, hard-delete, set-default,
publish, expire và clone. Page giữ dialog/context menu/toast/navigation.

| Gate | Kết quả |
|---|---|
| Pricing client contract | PASS `2/2` |
| Pricing/architecture focused | PASS `29/29` |
| Frontend unit/architecture | PASS `273/273` |
| Release build | PASS `0 warning / 0 error` |

`claims` parameter zero-consumer của Price List đã xóa; Item Price tạm giữ đến slice kế tiếp. Lifecycle
request type và exact endpoint được khóa bằng unit test trước khi migrate page.

## 29. FR4 Item Price feature client

`PricingApiClient` đã mở rộng cho Item Price: reference data Price List/Supplier, server rows query,
distinct category batching, create/update, deactivate/restore, set-default và hard-delete. Hai pricing
page không còn `IAPIServices`, `Config.LibraryApi` hoặc endpoint builder; `claims` parameter cũng về 0.

| Gate | Kết quả |
|---|---|
| Pricing client contract | PASS `3/3` |
| Pricing/architecture focused | PASS `30/30` |
| Frontend unit/architecture | PASS `274/274` |
| Release build | PASS `0 warning / 0 error` |
| Pricing bounded grids route-real | PASS `1/1` |
| Pricing lifecycle motif | PERMISSION DEBT — rows có dữ liệu nhưng action bị ẩn khi `CanModify=false` |

Failure motif xảy ra sau khi grid có row và trước mutation call; transport/query đã được route-real xác
nhận. Không ép hiện lifecycle menu khi fixture không cấp quyền sửa.
