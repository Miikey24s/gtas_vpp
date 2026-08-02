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

1. quota probe có sanitized snapshot dùng được và reforecast kết luận `ENOUGH` cho slice; và
2. `model-routing-eval` được sửa, hoặc owner duyệt waiver tạm đúng exact signature `62 pass / 1 fail`; và
3. hai correction owner dirty vẫn còn đúng hash hoặc đã được owner commit/cập nhật baseline; và
4. FR1 chỉ chọn một cleanup slice nhỏ, ưu tiên zero-consumer code/package candidate, không mass move.

Candidate khuyến nghị đầu tiên: cleanup zero-consumer thuần C# (`ObjectExtensions`, `SetBaseUrl`, legacy
member/type rõ ràng) trong một commit riêng; chưa mở typed-client/state/module migration ở cùng slice.
