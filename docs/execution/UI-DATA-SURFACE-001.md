# UI-DATA-SURFACE-001 — Chuẩn hóa data surface

> Trạng thái: `DS0–DS2 DONE; DS3 IMPLEMENTED — OWNER REVIEW; DS4/R1 LOCKED`
> Authority cha: [`UI-SYSTEM-001`](./UI-SYSTEM-001.md), triển khai lần lượt trong F5, F6 và F7.
> Phạm vi: frontend Blazor/Radzen; không đổi API, database, RBAC hoặc nghiệp vụ.

## 1. Bản một ánh nhìn

### Mục tiêu

Mọi màn danh sách/bảng dùng cùng một ngôn ngữ nhìn và tương tác:

```text
┌ Search ─ Filter 1 ─ Filter 2 ─ ... ─ Column picker? ─ Clear ┐  Toolbar
├ # ─ Column 1 ─ Column 2 ─ ... ─ Column n                  ┤  Header
├ 1 ─ Data / code popup / note popup                         ┤  Rows
├ …                                                          ┤
└ Summary / loading / selection                         Pager?┘  Footer
```

Khung, chiều cao theo profile, hover, focus, popup, footer và outer inset dùng chung. Route vẫn sở hữu cột, dữ liệu, paging/virtualization, permission và action nghiệp vụ.

### Phạm vi theo thứ tự

| Phase | Trạng thái | Làm gì | Sau phase có gì | Model + effort | Gate owner |
|---|---|---|---|---|---|
| DS0 — Contract | `DONE — OWNER APPROVED 2026-07-29` | Khóa motif, density, footer mode và consumer ledger | Một board nhìn là hiểu toàn hệ thống | **Sol · XHigh** — quyết định kiến trúc dài hạn | Đã duyệt motif và hai density profile |
| DS1 — Foundation | `DONE — OWNER APPROVED 2026-07-29` | Tạo shared frame/toolbar/footer/popover + token/bridge | Một chỗ chỉnh visual/interaction | **Sol · High** | Đã duyệt 2 route đại diện |
| DS2 — Reference | `DONE — OWNER APPROVED 2026-07-29` | History list, My Orders/History detail, Catalog | Nhóm M0–M2 thành mẫu canonical | **Terra · High**, **Sol · High review** | Đã duyệt và mở DS3 |
| DS3 — Workflow | `IMPLEMENTED — OWNER REVIEW` | Create Order, Department Summary, Period Review | Các workflow chính cùng motif | **Terra · High**, **Sol · High review** | Duyệt 4 route/state thật |
| DS4 — Admin | `LOCKED` | Library, Users, Permission và màn quản trị danh mục | Có column picker và paging chuẩn | **Terra · High**, **Sol · XHigh review** | Duyệt admin board |
| R1 — Refactor | `LOCKED` | Xóa adapter/CSS/state hết consumer, tách file quá tải | Code sạch hơn nhưng UI/behavior giữ nguyên | **Sol · XHigh plan/review**, **Terra · High migration** | Before/after visual + behavior parity |

### Bốn quyết định cần owner duyệt

1. Dùng composition `frame → toolbar → route-owned grid → footer`, không tạo `UniversalGrid<T>` hoặc cây kế thừa markup.
2. Không ép mọi hàng cao bằng nhau tuyệt đối; dùng hai profile `Compact` và `RichTwoLine` ở [mục 3.2](#32-nhịp-chiều-cao-khuyến-nghị).
3. Footer virtualization luôn có thông tin hữu ích, nhưng không có pager giả; xem [mục 3.3](#33-footer-theo-data-behavior).
4. Cleanup nhỏ làm liên tục trong slice; refactor rộng chỉ mở ở R1 sau khi reference UI được duyệt.

## 2. Hiện trạng và nguyên nhân trùng code

- Repository hiện có 18 file chứa 24 `RadzenDataGrid` thật sau khi Tổng hợp phòng ban tái sử dụng grid của Lịch sử; ledger canonical nằm tại [`VPP-DATA-SURFACE-CONSUMER-LEDGER`](../design/VPP-DATA-SURFACE-CONSUMER-LEDGER.md).
- `VppFilterSearch` hiện được dùng trực tiếp ở Product Catalog, Create Order, Permission Users và trong `VppOrderItemsSurface`.
- `HistoryOrderList` vẫn tự dựng `vpp-history-search`, `vpp-history-select`, menu state và một block CSS route riêng.
- Đây là kết quả của migration theo vertical slice ở F4: shared filters được chứng minh trên consumer mới/chạm tới, còn History list giữ implementation cũ để tránh big-bang cùng lúc với server paging, popup state và responsive mobile list.
- Cách làm cũ an toàn trong ngắn hạn nhưng đã tạo visual drift. DS2 sẽ migrate History list sang canonical component, rồi chỉ xóa CSS/state legacy khi consumer ledger về 0.

## 3. Contract data surface đề xuất

### 3.1 Cấu trúc

| Vùng | Contract chung | Phần route được tùy biến |
|---|---|---|
| Toolbar | Search trước; filter theo thứ tự nghiệp vụ; column picker nếu cần; clear ở cuối | Placeholder, option, số filter, debounce, permission |
| Header | Nền/chữ/weight/alignment/token chung; cột `#` là mặc định cho danh sách cần dò vị trí | Tên, độ rộng, sort, cột hiển thị |
| Row | Hover/selected/focus chung; padding và vertical alignment theo density | Cell template, action, badge, business state |
| Cell popup | Một component cho code/note bị rút gọn, copy và viewport anchoring | Label, value, icon, có/không cho copy |
| Footer | Một nhịp cao và semantic chung | Summary, pager, loading, selection, aggregate |

Không bắt buộc cột `#` cho tree, permission matrix hoặc bề mặt có thứ tự riêng không mang ý nghĩa cho người dùng.

### 3.2 Nhịp chiều cao khuyến nghị

Không nên áp literal `toolbar = header = mọi data row = footer` cho toàn hệ thống:

- Dòng một tầng có thể giữ `40px`.
- Dòng có `tên + code` cần `52px`; ép xuống `40px` sẽ chật/cắt chữ, nâng mọi bảng lên `52px` lại lãng phí diện tích.

Contract đề xuất:

| Token/profile | Chiều cao | Dùng cho |
|---|---:|---|
| Control | `32px` | Search, select, clear, column picker trigger |
| Toolbar shell | `42px` | Chứa control với inset trên/dưới cân nhau |
| Header | `40px` | Header bảng desktop |
| `Compact` row | `40px` | Một dòng text/badge/số |
| `RichTwoLine` row | `52px` | Tên + code hoặc nội dung hai tầng |
| Footer | `42px` | Summary và pager |

Như vậy các đường biên vẫn thẳng, đều và có nhịp; chỉ phần nội dung thật sự cần hai dòng mới cao hơn.

### 3.3 Footer theo data behavior

| Mode | Nội dung nên có | Không nên có |
|---|---|---|
| Server paging | `Hiển thị a–b trên tổng n` + pager; page size nếu nghiệp vụ cần | Continuous scroll trong cùng grid |
| Virtualized/continuous | `Đang hiển thị x trên tổng n`; loading/end-of-list; aggregate hoặc selected count nếu hữu ích | Pager giả hoặc vùng trắng trang trí |
| Static/small list | `Hiển thị x mục`; aggregate ngắn nếu có giá trị | Footer rỗng chỉ để đủ chiều cao |
| Total chưa biết | `Đã tải x mục` + trạng thái đang tải | Khẳng định tổng giả |

Footer virtualization không bị “trống” nếu nó trả lời được ít nhất một câu: đang thấy bao nhiêu, tổng bao nhiêu, đã chọn bao nhiêu, tổng số lượng bao nhiêu, hoặc còn đang tải không.

### 3.4 Responsive

- Desktop/tablet rộng giữ toolbar một dòng khi đủ chỗ.
- Tablet hẹp/mobile: search chiếm toàn hàng; filter chia grid; clear chiếm toàn hàng khi cần.
- Bảng nhiều cột có thể chuyển sang mobile card/list hoặc horizontal region có chủ đích; không ép desktop row-height lên mobile card.
- Popup/popover phải dùng cùng transient motion, không đổi geometry khi mở và không vượt viewport.

### 3.5 Data loading và virtualization

Virtualization chỉ nên giảm số DOM row, không được biến mỗi đoạn cuộn thành một loading state nhìn thấy được.

| Mode | Dùng khi | Contract |
|---|---|---|
| `ClientSnapshotVirtualized` | Dataset hữu hạn, DTO nhẹ, cần chọn/duyệt liên tục; ví dụ catalog Create Order hiện khoảng 547 dòng | Tải snapshot một lần khi vào màn hoặc đổi filter; DOM vẫn chỉ render viewport + overscan; cuộn không gọi API và không hiện loader |
| `ServerPaging` | Danh sách quản trị/lịch sử có thể tăng dài, người dùng tra cứu theo trang | API `skip/top`, pager rõ; không continuous scroll |
| `ServerVirtualizedPrefetch` | Dataset rất lớn nhưng nghiệp vụ bắt buộc cuộn liên tục | Cache cửa sổ hiện tại + trước/sau, prefetch nền, fixed row height; không dùng blocking loader cho mỗi lần cuộn |

Khuyến nghị cho Create Order hiện tại là `ClientSnapshotVirtualized`: 547 `ProductOption` chỉ gồm id và vài chuỗi ngắn, nên giữ snapshot trong memory nhẹ hơn nhiều so với render 547 component row. Radzen vẫn chỉ mount số row nhìn thấy; hiệu năng cuộn không phụ thuộc network. API hiện giới hạn 100 dòng/request, vì vậy frontend sẽ tải theo batch và chỉ hiện một initial/filter loading state. Ngưỡng chuyển mode phải được đo bằng payload, latency và route-real benchmark; không ghi một con số tạm thành invariant toàn repository.

Không dùng lại contract hiện tại `LoadData` theo từng cửa sổ + `IsLoading=true` mỗi lần scroll cho dataset hữu hạn, vì nó làm network latency và loading overlay xuất hiện trực tiếp trong thao tác cuộn.

## 4. Kiến trúc component

```text
VppDataSurfaceFrame
├─ Toolbar slot → VppDataToolbar
│  ├─ VppFilterSearch
│  ├─ VppFilterSelect<T>
│  ├─ optional ColumnPicker
│  └─ VppClearFiltersButton
├─ Data slot → RadzenDataGrid hoặc virtualized route component
└─ Footer slot
   ├─ Radzen paginator đã được bridge style, hoặc
   └─ VppDataSummaryFooter cho virtualized/static
```

Shared foundation dự kiến:

- `VppDataSurfaceFrame`: chỉ sở hữu geometry/border/overflow/slot, không biết DTO hoặc endpoint.
- `VppDataToolbar`: layout toolbar và responsive; filter vẫn là typed child component.
- `VppDataSummaryFooter`: typed mode cho summary/loading/selection/aggregate; không tự paging dữ liệu.
- `VppCellValuePopover`: code/note/copy với anchor + viewport + accessibility chung.
- `VppDataDensity`: `Compact`, `RichTwoLine`; map sang semantic token.
- Radzen bridge: header/row/paginator dùng token chung theo class opt-in, không override mọi DataGrid chưa audit.

Không tạo:

- `UniversalGrid<T>` nhận endpoint/property bằng string;
- reflection để sinh cột/action;
- base class sở hữu UI state của nhiều nghiệp vụ;
- wrapper cho toàn bộ API Radzen.

## 5. Kế hoạch chi tiết

### DS0 — Contract và visual board

- Lập consumer ledger cho 18 file / 24 DataGrid và các custom list/table; số lượng giảm vì History và Tổng hợp phòng ban dùng chung một grid có slot cột.
- Phân loại từng surface: `Paged`, `Virtualized`, `Static`, `Matrix/Tree`, `Dialog`.
- Chụp route thật đại diện: History list, My Orders detail, Product Catalog, Users.
- Tạo board chú thích trực tiếp toolbar/header/row/footer, hai density và outer inset ở sidebar expanded/collapsed.
- Khóa acceptance criteria trước khi sửa shared CSS.

Gate: **DONE** — owner duyệt DS0–DS1 ngày 2026-07-29; ledger có architecture gate chống quên consumer mới.

### DS1 — Shared foundation

- Thêm semantic tokens cho toolbar/header/row/footer và density.
- Tạo `VppDataSurfaceFrame`, `VppDataToolbar`, `VppDataSummaryFooter`, `VppCellValuePopover`.
- Giữ `VppFilterSearch`, `VppFilterSelect<T>`, `VppClearFiltersButton` làm canonical filter controls.
- Thêm architecture tests: không duplicate focus ring; shared transient surface; opt-in Radzen bridge; không reflection/endpoint string.
- Khóa ba data-source mode và performance gate: request count trong lúc scroll, bounded DOM, fixed row height, overscan và loading visibility.
- Chứng minh trên hai consumer khác behavior: một paged và một virtualized.

Gate: build/unit + route-real desktop/mobile + owner visual review.

#### DS1 implementation record — 2026-07-29

- Shared foundation: `VppDataSurfaceFrame`, `VppDataToolbar`, `VppDataSummaryFooter`, `VppCellValuePopover`; typed contract: `VppDataSourceMode`, `VppDataDensity`, `VppDataFooterMode`, `VppCellValueKind`.
- Semantic token khóa nhịp `32 / 42 / 40 / 40|52 / 42`; Radzen bridge chỉ áp dụng khi consumer opt-in class `vpp-data-grid`.
- Paged proof: `HistoryOrderList` giữ nguyên `LoadData`, `skip/top`, pager, selection và filter state; thêm typed frame/toolbar/cell popover.
- Virtualized proof: `VppOrderItemsSurface` giữ input snapshot route-owned, `AllowVirtualization`, overscan và không có `LoadData`; thêm typed frame/footer/cell popover.
- Compatibility alias `vpp-history-popover-*` còn trong shared cell popover để JS/CSS/test hiện hữu không vỡ; chỉ DS2 được xóa khi ledger consumer bằng 0.
- Performance gate route-real: virtualized scroll không phát request `/api/VPPRequest/*`, DOM mounted không quá 40 row, không pager giả và browser/OS scrollbar giữ nguyên.
- Wave Review Board ignored: `tmp/ds1-review/ds1-history-paged-{1920x1080,1366x768,768x1024,390x844}.png`, `ds1-history-cell-popover-*`, `ds1-order-items-virtualized-*`, dark representative `1366x768`.
- Evidence: frontend Release build `0 warning / 0 error`; `./scripts/gtas.cmd test-frontend` pass `194/194`; focused isolated Playwright DS1 + History/F4 pass `5/5`, sau đó DS1 four-viewport/dark rerun pass `1/1`.
- `verify -Scope frontend` dừng tại `model-routing-eval` thuộc change-set AI-harness có sẵn ngoài scope; 62/63 agent-setup checks pass và DS1 không sửa/stage các file đó.
- Create Order vẫn giữ legacy server-window virtualization trong DS1; migration snapshot client thuộc DS3 và chưa được thực hiện.

### DS2 — Reference M0–M2

1. Migrate `HistoryOrderList` sang shared search/select/clear; xóa `OpenFilterMenu` và legacy CSS chỉ khi không còn dùng.
2. Đưa History list và `VppOrderItemsSurface` vào shared frame/footer contract nhưng giữ server paging so với virtualization.
3. Chuẩn hóa Product Catalog theo cùng toolbar/header/footer/density.
4. Rà My Orders ở sidebar expanded/collapsed và 4 viewport.

Gate: History list/detail + My Orders + Catalog giống motif, nhưng không đổi API/action.

#### DS2 implementation record — 2026-07-29

- History list đã thay toàn bộ search/select tự dựng bằng `VppFilterSearch`, hai `VppFilterSelect<T>` và `VppClearFiltersButton`; toolbar nằm trong slot của `VppDataSurfaceFrame` và vẫn giữ server paging, debounce, selection cùng API hiện hữu.
- Đã xóa `OpenFilterMenu`, `FilterMenuToggled`, bốn menu key, route CSS/JS legacy của select sau khi consumer search về 0. `VppFilterSelect` có callback `Opening` để đóng KPI/code/note transient surface trước khi browser mở popover canonical.
- History detail và My Orders tiếp tục dùng cùng `VppOrderItemsSurface`; loại bỏ state menu lọc không còn cần, giữ client-snapshot virtualization, row density, footer và action nghiệp vụ.
- Product Catalog đã dùng typed `VppDataSurfaceFrame` với `ServerPaging`, `RichTwoLine`, canonical toolbar và opt-in Radzen bridge; API, sort, page size và bốn cột không đổi.
- Responsive nhìn bằng mắt: desktop `1920/1366`, tablet `768` và mobile `390`; search chiếm hàng riêng ở tablet hẹp/mobile, filter popup neo đúng trigger và không có document-level horizontal overflow. Evidence ignored nằm ở `tmp/ds2-review/ds2-*`.
- Owner review fix: popup select bỏ min-width cứng `12rem`, chỉ rộng hơn trigger khi nội dung cần; search dùng border thật do wrapper sở hữu và đổi active ngay, không còn frame transition làm viền bị đứt khi vừa gõ.
- Evidence: frontend Release build `0 warning / 0 error`; `./scripts/gtas.cmd test-frontend` pass `195/195`; isolated Playwright DS2/History/My Orders/Catalog pass `9/9`, responsive rerun pass `1/1`.
- `gtas-vpp-ui-system` checklist data-surface chưa ghi trong change-set này vì skill đang có thay đổi AI-harness ngoài scope; chỉ cập nhật sau khi change-set đó được hợp nhất hoặc owner cho phép xử lý chung.

### DS3 — Workflow chính

- Create Order catalog bên trái: chuyển sang client snapshot + virtualized DOM, shared toolbar, fixed rich-row density, code/note popup và virtual footer; scroll không phát request/loading mới.
- Department Summary: shared toolbar, paged footer và detail-on-demand giữ nguyên.
- Period Review/Rà soát: filter/column/summary theo cùng motif; không nhập business action vào shared frame.
- Kiểm tra empty, filtered-empty, loading, error và long-data.

Gate: board 4 route thật, console/network sạch và không document-level scroll ngoài contract.

#### DS3 implementation record — 2026-07-29

- Owner mở DS3 sau khi review xong DS2; DS2 được ghi nhận `DONE — OWNER APPROVED` và DS4/R1 vẫn khóa.
- Create Order catalog dùng `ClientSnapshotVirtualized`: tải toàn snapshot theo batch `100`, filter client, Radzen chỉ mount viewport + overscan; cuộn không phát thêm request `/api/VPPRequest/products` và không hiện loading theo cửa sổ. Grid được khóa viewport hữu hạn để virtualization thực sự hoạt động, không chỉ bật cờ.
- Create Order dùng shared toolbar/filter/clear, `RichTwoLine`, virtual footer và `VppCellValuePopover` cho code; draft/action/note/submit vẫn do route sở hữu.
- Department Summary dùng shared toolbar + typed server-paged frame, compact density và pager hiện hữu; filter responsive theo độ rộng khung master nên không bị cắt khi master/detail chia đôi. API, permission, row selection và detail-on-demand không đổi.
- Period Review dùng shared year/month select và typed server-paged frame; reload, readiness, chốt kỳ và các action nghiệp vụ không được đưa vào shared component. Empty-period fixture vẫn hiển thị content state thay vì grid rỗng.
- Owner-review correction: bỏ geometry guard JS theo từng row vì tạo state/motion phụ thuộc timing của virtualization. Create Order chuyển riêng surface chọn hàng sang Blazor `Virtualize` với header `div` độc lập nằm trong cùng native scroll viewport; không còn phụ thuộc table painting order của Radzen khi spacer row thay đổi.
- Regression gate cuộn qua nhiều vị trí lẻ theo chiều cao row, kiểm header luôn sở hữu paint/hit area, DOM row vẫn bounded và không còn module/class guard động; browser screenshot là bằng chứng visual cuối.
- Visual evidence ignored: `tmp/ds3-review/f4-review-order-create-1920x1080.png`, `ds3-order-create-code-popover-1920x1080.png`, `ds3-department-summary-1366x768.png`, `ds3-period-review-1366x768.png`.
- Evidence hiện tại: Release build `0 warning / 0 error`; frontend unit/architecture `196/196` pass; isolated Playwright Create Order + Department Summary + Period Review `3/3` pass. `verify -Scope frontend` chỉ dừng tại `model-routing-eval` của change-set AI-harness có sẵn ngoài DS3 (`62/63` setup checks pass); owner visual review còn là gate đóng DS3.

#### Department Summary parity record — 2026-07-29

- Theo owner review, Tổng hợp phòng ban không còn giữ một master/detail implementation riêng: route dùng chung `HistoryOrderWorkspaceTabBase`, `HistoryWorkspaceShell`, scope kỳ, KPI, biểu đồ, toolbar, pager, popup, responsive mobile list và `HistoryOrderDetailSheet` với Lịch sử đơn.
- `HistoryOrderList` nhận slot typed cho cột và mobile row; Department chỉ truyền 8 cột `# → Kỳ → Mã đơn → Người đặt → Loại đơn → Trạng thái → Ngày gửi → Ghi chú`. CSS có track riêng cho schema 8 cột nhưng dùng chung density, row/footer và interaction.
- Backend thêm endpoint history-like theo phòng ban cho summary + server paging; server luôn lấy `DepartmentCode` và `MemberCompanyCode` từ claim, không tin scope do client gửi. API cũ được giữ cho consumer chưa migrate.
- Consumer ledger giảm từ `19 file / 25 grid` còn `18 file / 24 grid` vì hai route dùng cùng một grid thật, không sao chép markup.
- Evidence: solution Release build `0 warning / 0 error`; frontend unit/architecture `196/196`; backend unit `429/429`; isolated Department Summary Playwright `1/1`, không horizontal overflow tại `390×844`, `768×1024`, `1366×768`, `1920×1080`; ảnh đã được kiểm bằng mắt tại `tmp/department-history-parity/` (ignored). `verify -Scope frontend` vẫn dừng ở `model-routing-eval` của nhóm AI-harness dirty ngoài scope (`62/63`).
- Owner-review correction: dashboard shell phải áp cùng contract `vpp-history-shell` cho cả History và Department Summary để route chạm đáy viewport. Trên desktop, detail bắt đầu từ hàng KPI thay vì hàng scope toolbar và cùng kết thúc ở đáy với danh sách; local `text-box` trim bị gỡ khỏi nội dung History để không xén dấu/đỉnh chữ tiếng Việt. Frontend `196/196` và hai focused isolated Playwright route pass; ảnh 1366/1920 đã được kiểm bằng mắt tại `tmp/history-department-layout-fix/` và `%TEMP%/gtas-vpp-history-visual/` (đều không commit).

### DS4 — Library, admin và permission

- Chuẩn hóa page quản trị danh mục, Users và Permission theo shared toolbar/frame.
- Column picker là optional toolbar slot; profile cột vẫn thuộc route.
- Grid nhiều cột dùng server paging/detail-on-demand; không nhồi mọi cột vào viewport.
- Permission matrix/tree được ghi ngoại lệ nếu motif table thường không phù hợp.

Gate: owner duyệt admin board và keyboard/accessibility trace.

### R1 — Behavior-preserving UI refactor

Tên chính xác cho việc “đổi code nhưng UI/tương tác không đổi” là **behavior-preserving refactor**; trong đợt này có thể gọi ngắn là **structural UI refactor**.

- Xóa adapter, selector, state và CSS chỉ khi consumer ledger bằng 0.
- Tách file route/composite quá tải theo responsibility.
- Merge token/selector trùng; không rename diện rộng nếu không tạo giá trị đo được.
- So sánh trước/sau bằng DOM contract, screenshot, interaction, console/network và test route thật.
- Không kết hợp refactor rộng với redesign đang còn review.

## 6. Cách cleanup trong lúc còn thiết kế

Khuyến nghị hybrid:

- **Làm ngay trong mỗi slice:** xóa code vừa bị thay thế, merge CSS trùng cục bộ, cập nhật test và consumer ledger.
- **Để R1:** đổi cấu trúc folder lớn, tách nhiều component, rename xuyên route, xóa toàn bộ legacy và tối ưu architecture toàn frontend.

Không nên để toàn bộ cleanup tới cuối vì debt sẽ tiếp tục nhân lên; cũng không nên refactor rộng ngay bây giờ vì UI contract còn đổi. Mốc nhắc R1 là sau DS2 owner approval, rồi thực thi sau DS4 hoặc khi một cụm legacy đã hết consumer.

## 7. Nguyên tắc code AI-first đề xuất

Áp dụng theo tài liệu OpenAI chính thức:

1. Mỗi task ghi rõ `goal · context · constraints · done when`; task lớn dùng một goal và execution plan sống.
2. `AGENTS.md` chỉ giữ layout, command, convention, constraint và definition of done bền vững; chi tiết dài nằm ở architecture/plan.
3. Abstraction theo tầng `token → primitive → composite → pattern → route`; chỉ trích xuất sau ít nhất hai consumer thật.
4. Typed enum/record/slot thay string config, reflection và endpoint động.
5. Một implementer chính; đổi model ở checkpoint lớn; worktree tách biệt khi có nhiều agent cùng ghi.
6. Build/unit không chứng minh UI: phải có route-real DOM geometry, screenshot nhìn bằng mắt, interaction, console/network và accessibility.
7. Workflow lặp lại ổn định mới cập nhật `gtas-vpp-ui-system` skill; invariant cơ học mới đưa vào script/test/hook.
8. Commit theo vertical slice; diff review và consumer ledger là gate trước khi xóa legacy.

Sau khi DS2 chứng minh workflow trên ít nhất hai route, mới cập nhật skill UI bằng checklist data-surface. Chưa cần tạo plugin riêng vì capability vẫn chỉ thuộc GTAS VPP.

Nguồn:

- [Codex best practices](https://learn.chatgpt.com/guides/best-practices)
- [AGENTS.md guidance](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
- [Long-running work and Goal mode](https://learn.chatgpt.com/docs/long-running-work)
- [Codex ExecPlans](https://developers.openai.com/cookbook/articles/codex_exec_plans)
- [GPT-5.6 model guidance](https://developers.openai.com/api/docs/guides/latest-model.md)

## 8. Validation bắt buộc

- Frontend build Release, unit/architecture tests và `git diff --check`.
- Route-real: `390×844`, `768×1024`, `1366×768`, `1920×1080`.
- Sidebar expanded/collapsed; VI/EN; Light/Dark đại diện.
- Keyboard/focus, hover/selected, popup viewport, paging/virtualization, empty/error/loading.
- Không document-level overflow ngoài route contract; native scrollbar giữ nguyên.
- Screenshot/board được người kiểm tra nhìn bằng mắt; geometry test chỉ chống regression.
- Full `verify -Scope frontend` ở checkpoint; blocker ngoài scope phải tách rõ.

## 9. Rủi ro và cách khóa

| Rủi ro | Cách khóa |
|---|---|
| Big-bang migration làm vỡ nhiều route | Vertical slice + consumer ledger + commit riêng |
| Shared component thành generic quá mức | Chỉ sở hữu chrome/geometry; route giữ data/action/API |
| Ép row height làm cắt text | Hai density profile + long-text fixture |
| Popup lệch/nhảy | Một transient/anchor contract + frame sampling + visual review |
| CSS global làm ảnh hưởng grid chưa audit | Opt-in class + bridge test |
| Refactor đổi behavior âm thầm | Before/after route-real parity và owner gate |

## 10. Definition of done

- Các route trong scope dùng canonical toolbar/filter/footer/density theo ledger.
- Không còn duplicate History filter implementation.
- Outer inset giữ đúng ở sidebar expanded/collapsed.
- Footer đúng data behavior; không pager giả hoặc footer rỗng trang trí.
- Legacy adapter/CSS chỉ còn khi ledger chứng minh vẫn có consumer.
- F5/F6 visual board được owner duyệt; R1 giữ UI/interaction parity; F7 full gate pass.
