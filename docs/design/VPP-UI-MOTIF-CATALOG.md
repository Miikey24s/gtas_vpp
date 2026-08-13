# GTAS VPP UI motif catalog

> Trạng thái: `ACTIVE — canonical visual and composition authority`
>
> Visual contract: OpenAI/Codex-inspired minimal system. Atlas M0–M2 chỉ là reference read-only; route Blazor thật là authority cuối.

## 1. Cách dùng cho AI agent

Motif là một mẫu UI lặp lại có cùng mục đích, geometry và behavior. Trước khi viết markup mới, agent phải:

1. Tìm route trong `gtas_vpp_fe.Helpers.RouteCatalog` và `UiRouteCatalog`.
2. Chọn đúng `WorkspacePattern`, `DataSourceMode`, `Density`, `Toolbar`, `Footer` và `ResponsiveStrategy` của route.
3. Tìm motif canonical trong bảng bên dưới.
4. Tái sử dụng component hiện có; nếu chưa có, tạo một vertical slice với tối thiểu hai consumer thật hoặc giữ ở route nếu behavior chưa đủ giống.
5. Ghi consumer, trạng thái và evidence vào consumer ledger cùng change-set.

Không tạo `UniversalPage<T>`, `UniversalGrid<T>`, selector cấu hình bằng string, reflection-driven form/column engine hoặc adapter chỉ để đổi tên markup.

## 2. Motif canonical

| ID | Motif | Dùng khi | Component/authority | Variant typed | Route không được tự làm lại |
|---|---|---|---|---|---|
| `SHELL-NAV` | Shell, sidebar, header-tab, seam, page inset | Mọi authenticated page | `MainLayout`, `LeftSidebar`, `vpp-layout.css`, `vpp-sidebar.css`, `vpp-tabs.css`, page-inset tokens | expanded/collapsed, desktop/tablet/mobile | seam, active indicator, hover rhythm, outer inset |
| `HEADER-TAB-GROUP` | Điều hướng cha–con cùng primary header | Nhóm route Điều hành kỳ, Bảng giá | `VppHeaderTabGroup`, `VppHeaderSubTab`, `LeftSidebar`, shared tab indicator | expanded desktop, local mobile fallback, permission-aware default | dùng selector decision/filter thay navigation; active line ở cả cha và con |
| `ACCOUNT` | Account/auth form shell | Login, register, password, email flow | `VppAccountWorkspace` | artwork, compact, scrollable | brand/language/header layout |
| `COLLECTION` | Danh sách một tập dữ liệu | Library, users, audit, catalog, kỳ đặt hàng | `VppCollectionWorkspace` + `VppDataSurfaceFrame` | compact/rich, paged/static | frame, toolbar, footer geometry |
| `LIST-DETAIL` | Danh sách + inspector/detail | History, permission, lookup | `VppListDetailWorkspace` | ratio + overlay detail | split seam, pane height, detail placement |
| `SPLIT-EDITOR` | Hai vùng chọn/chỉnh sửa | Lookup, Create Order | `VppSplitEditorWorkspace` | resizable/fixed, stacked tablet | pane scroll ownership, seam |
| `OPERATION` | Workflow/operation screen | Chốt kỳ, approval, order create | `VppOperationWorkspace` + `VppWorkflowStepper` | compact steps, action footer | step geometry, active/completed/pending states |
| `ANALYTICS` | KPI/chart/list/detail data story | History, department summary, report | `VppAnalyticsWorkspace` | chart/list/detail arrangement | KPI rhythm, chart empty state, detail alignment |
| `DATA-FRAME` | Header/toolbar/grid/footer frame | Bảng/list có data surface | `VppDataSurfaceFrame` | `ServerPaging`, `ClientSnapshotPaged`, `ClientSnapshotVirtualized`, `Static` | border, overflow, footer anchor |
| `COLLECTION-HEADER` | Identity + count + collection action | Add/import/export thuộc cả collection | `VppCollectionHeader` | add/secondary/disabled | CRUD trong toolbar hoặc header cột |
| `BUTTON-ACTION` | Button có text/icon theo cấp hành động | Collection, query, workflow, dialog | Radzen button bridge trong `vpp-radzen-theme.css`; token `--vpp-button-*`; component domain chỉ khi có behavior riêng | standard 32px, compact/icon-only 28px, icon 16px; primary/secondary/light/success/warning/danger | action button cao 36–44px, shadow/translate/oval focus riêng theo route; universal button wrapper chỉ đổi tên markup |
| `ADMIN-ROW-ACTIONS` | Action chính theo trạng thái + lifecycle/destructive của một dòng | Các bảng quản trị | `VppAdminIconAction`, `VppAdminActionMenu`, `VppAdminLifecycleMenu`, `VppAdminActiveToggle`, cột `vpp-admin-actions` | một action trực tiếp có nhãn ngắn; menu overflow full-text dùng cùng surface, row rhythm và hover của select dọc; active toggle chỉ khi trạng thái là quyết định trực tiếp của page | dàn 3–5 icon ngang hàng; giấu Duyệt/Từ chối/Chốt kỳ trong menu; route tự đặt kích thước/icon chrome hoặc dựng switch shell khác |
| `CAPABILITY-SURFACE` | Stable Capability Surface — giữ cấu trúc chức năng ổn định giữa các trạng thái | Action group, toolbar, grid/list và empty state cần giúp người dùng biết chức năng nào tồn tại | Route-owned typed capability projection + `VppAdminActionMenu`, `VppDataSurfaceFrame`, `VppContentState`; permission vẫn do backend/API quyết định | enabled/disabled/busy/hidden-by-permission; populated/base-empty/filtered-empty/error | ẩn action chỉ vì record chưa đủ điều kiện; thay đổi thứ tự action theo từng dòng; gỡ toolbar/header/footer khi empty; render empty như một row có hover/click |
| `FILTER-TOOLBAR` | Search, filter, clear, column picker | Tìm/lọc dữ liệu thường xuyên | `VppDataToolbar`, `VppFilterSearch`, `VppFilterSelect`, `VppClearFiltersButton`, `VppColumnPicker` | filter count/domain-specific options | popup chrome, control height, order |
| `FILTER-ADVANCED` | Bộ lọc ít dùng/nhiều điều kiện | Ngày, khoảng giá, metadata, audit/resource | typed route-owned filter panel anchored from the toolbar | compact/popover/workspace | không tạo filter icon riêng trong từng header |
| `SELECTOR-FILTER` | Thay đổi tập dữ liệu hiển thị | Category/status/department/unit filters | `VppFilterSelect<T>` | active/inactive, option count | tự tạo dropdown khác visual |
| `SELECTOR-DECISION` | Chọn mode/giải pháp nghiệp vụ | Kỳ, theo mặt hàng/phòng ban, supplier/price list | `VppSegmentedSelector<T>` cho lựa chọn ngang; `VppDecisionSelect<T>` cho dropdown decision | segmented/dropdown decision | gọi là filter nếu thực tế là decision |
| `SELECTOR-PAGE-SIZE` | Chọn số dòng mỗi trang | Mọi server/client paged grid | Radzen pager bridge + page-size contract trong `vpp-radzen-theme.css` | 25/50/100/200 theo profile; trigger label-only 56×32 không chevron; dropup cùng chiều rộng và vertical-option rhythm với select canonical | custom popup/oval focus riêng; để Radzen và accessibility CSS cùng vẽ chrome |
| `DATA-ROW` | Nhịp hàng và semantic cell | Mọi grid canonical | `vpp-datagrid.css` + route-owned typed columns | `Compact`, `RichTwoLine` | zebra tự bật, inline color/radius |
| `DATA-COLUMN` | Column contract | `#`, code/name, note, amount, status, action | route-owned typed `RadzenDataGridColumn` theo [data-surface ledger](VPP-DATA-SURFACE-CONSUMER-LEDGER.md) | visible/pickable/frozen/filterable/sortable | reflection/string column config |
| `DATA-FOOTER` | Summary, pager, page size, workflow action | Cuối data surface | `VppDataSummaryFooter` hoặc Radzen pager bridge | summary/paged/virtualized/action | footer giả, row đè footer hoặc route tự đổi căn lề |
| `CELL-VALUE` | Code/note dài và copy | Ô bị truncate | `VppCellValuePopover` | code/note/copy | popup route tự neo khác contract |
| `CONTENT-STATE` | Loading/empty/filter-empty/error/denied/disabled/success | Mọi route có state | `VppContentState`, `VppContentStateKind` | typed state + semantic role + `FillAvailable`; empty full-height căn giữa cả hai trục | text state tự dựng bằng string switch, CSS min-height riêng từng route hoặc nút refresh thủ công trong empty state |
| `STATUS-BADGE` | Trạng thái ngắn, có màu semantic | Order/admin/permission/period lifecycle state | `VppStatusBadge` + `VppStatusTone`; `VppPeriodStateBadge` là composite canonical cho vòng đời kỳ | info/success/warning/danger/neutral; period compact/full | badge tự map string hoặc màu theo route |
| `CATEGORY-CHIP` | Phân loại dữ liệu, không biểu diễn tiến trình | Loại đơn, nhóm dữ liệu, metric context ngắn | `VppCategoryChip` + `VppCategoryTone` | neutral/primary/accent | dùng màu success/danger để ám chỉ kết quả nghiệp vụ |
| `METRIC-CARD` | Một chỉ số định lượng | Analytics/report summary | `VppMetricCard` | neutral/accent/success/warning | raw `kpi-card`/shine markup |
| `DIALOG-EDITOR` | Thêm/sửa form | Admin CRUD | `VppAdaptiveDialogShell` + typed dialog contract | compact/wide/fullscreen, sticky footer | inline row edit hoặc dialog tự vẽ shell |
| `DIALOG-ACTIONS` | Hủy/lưu/submit/destructive action | Dialog/editor | `VppDialogActions` trong `VppAdaptiveDialogShell` | primary/secondary/danger, busy/disabled, leading slot | footer spacing hoặc cặp button riêng từng dialog |
| `TRANSIENT` | Popup, popover, user menu, filter, notification | Surface tạm thời | `vpp-transient-surface`, `vpp-polish.css`, Radzen bridge | above/down/center, reduced-motion | `transform` làm đổi anchor geometry |
| `FEEDBACK` | Inline notice/toast/reconnect | Thông báo hệ thống/nghiệp vụ | `VppInlineNotice`, `IToastService`, reconnect contract | neutral/info/success/warning/danger | inject `NotificationService`, `RadzenAlert` hoặc raw exception text theo route |
| `FILE-EXPORT` | Tải PDF/Excel/CSV từ API | Order, history, approval, report, settlement | `VppFileExportActions`; `IBrowserFileDownloadService`; `ExportFileContract`; backend builder typed + `SimpleWorkbookBuilder` | per-format busy, API stream + MIME, PDF, workbook, CSV | nút export, `DotNetStreamReference`/JS pipeline, tên file/MIME hoặc SpreadsheetML packager lặp theo route |
| `SKELETON` | Loading placeholder | Chờ data | `SkeletonPage`, `SkeletonGrid`, `vpp-loading.css` | page/grid/row; `FillAvailable` phân bố row placeholder xuống hết thân grid | legacy `.shimmer-*` mới hoặc skeleton chỉ phủ nửa data surface |

## 3. Quy tắc selector

Các selector có thể cùng visual foundation nhưng khác semantic:

- `FILTER`: chỉ thay đổi tập dữ liệu đang xem.
- `DECISION`: thay đổi mode/phương án nghiệp vụ.
- `PAGE-SIZE`: chỉ thay đổi số dòng hiển thị.
- `HEADER-TAB`: điều hướng route; không dùng `VppSegmentedSelector` thay thế.

Quy tắc vị trí lọc trong data grid:

- `FILTER-TOOLBAR` là lớp lọc hiển thị chính và phải nằm trước vùng dữ liệu.
- Header cột mặc định chỉ sở hữu sort; admin grid không bật Radzen `FilterMode.CheckBoxList` theo mặc định.
- Điều kiện hiếm hoặc nhiều trường dùng `FILTER-ADVANCED` từ toolbar, không rải popup nhỏ ở từng header.
- `VppColumnPicker` chỉ điều chỉnh hiển thị cột, không thay thế bộ lọc.
- Filter luôn áp trên toàn bộ tập dữ liệu được cấp quyền trước `paging`/`virtualization`, sau đó mới tính tổng và phân trang.

Không gộp chúng thành một component string-configured. Dùng typed component/contract riêng, cùng token và popup bridge.

Quy tắc `HEADER-TAB-GROUP`:

- Khi người dùng đi vào tab cha mà chưa chỉ rõ child, luôn chọn child đầu tiên đang hiển thị mà người dùng có quyền, theo đúng thứ tự trên header/sidebar.
- Query child hợp lệ và bookmark cũ vẫn được giữ. Query rỗng hoặc không hợp lệ fallback về child đầu tiên; không để nhóm không có active tab và không mặc định vào child thứ hai.
- Khi nhóm đang active, nhãn cha chỉ là context không lặp navigation; underline và `aria-current` thuộc đúng child hiện tại.

Quy tắc `CONTENT-STATE`:

- State thay thế toàn bộ nội dung của page, workspace, pane hoặc data frame bật `FillAvailable="true"`; state phải kéo đến đáy vùng còn lại và giữ geometry trước/sau khi có dữ liệu.
- State nằm trong dialog, form, chart/card, wizard step hoặc notice cục bộ giữ compact và không bật `FillAvailable`.
- `VppDataSurfaceFrame` tự truyền contract full-height cho content state trong body; route không viết lại `min-height` để giả lập một vùng dữ liệu.
- Raw `EmptyTemplate` của widget chỉ được giữ khi widget sở hữu table geometry; chuỗi wrapper từ body đến empty row phải truyền được `height: 100%`.
- Empty/success/filtered-empty không có nút `Làm mới`; dữ liệu tự tải theo route, realtime hoặc mutation. Error state vẫn được dùng `Thử lại` như recovery có chủ đích, nhưng không gọi hoặc trình bày nó như refresh dữ liệu thường xuyên.

Quy tắc `CAPABILITY-SURFACE`:

- Với cùng một loại entity và cùng quyền truy cập, action chính, nhóm overflow và thứ tự action phải ổn định giữa các dòng/trạng thái. Action chưa đủ điều kiện nghiệp vụ vẫn hiện nhưng ở trạng thái disabled; không gắn click handler và có lý do ngắn khi nguyên nhân không hiển nhiên.
- Chỉ ẩn action khi người dùng không có quyền, chức năng không thuộc workflow/entity đó, hoặc chức năng chưa được phát hành. Frontend không dùng disabled để thay thế kiểm tra quyền ở backend.
- Khi loading hoặc mutation đang chạy, giữ footprint của toolbar/action/footer và chuyển control liên quan sang busy/disabled để tránh layout nhảy. Responsive được gom action vào overflow hoặc ẩn cột ưu tiên thấp theo profile, nhưng không làm action biến mất khác nhau giữa các record chỉ vì trạng thái.
- Base-empty vẫn giữ collection header, toolbar, column header, body và footer/pager theo cùng geometry với populated state. Collection action hợp lệ như `Thêm` hoặc `Import` vẫn hoạt động; filter/pager không có dữ liệu có thể disabled nhưng không bị tháo khỏi layout.
- Filtered-empty giữ filter và `Xóa bộ lọc` hoạt động. Body chỉ hiển thị một `VppContentState` căn giữa cả hai trục; không tạo fake data row, không hover/cursor/action-row affordance. Footer hiển thị tổng `0` và pager disabled nếu surface có paging.
- Error state giữ cùng frame và có `Thử lại`; denied state không được để lộ action/cột bị chặn bởi permission. Menu toàn disabled chỉ được mở khi nó giải thích rõ capability của cùng nhóm; không render menu rỗng.

Quy tắc scroll ownership của workspace:

- Shell authenticated luôn giữ `100dvh`; `.vpp-layout-body` là page scroller duy nhất khi trang cần dài hơn viewport. Không mở scroll trên `body`/document.
- Mọi `Vpp*Workspace` khai báo `VppWorkspaceScrollMode`: `Internal` cho list/detail hoặc grid cần một viewport cố định, `Page` cho workflow/trang dài, `Adaptive` cho workspace chỉ khóa nội bộ khi desktop đủ rộng và cao.
- `Adaptive` chuyển sang page flow khi viewport dưới `1440px` hoặc cao dưới `800px`; grid/pane vẫn tự sở hữu horizontal/internal scroll theo schema, không kéo ngang toàn document.
- Master-detail nhiều cột chỉ hiển thị song song khi chiều rộng còn lại thực sự đủ. History dùng persistent detail từ `1440px`; laptop dùng list toàn chiều ngang và detail drawer.
- Responsive không được giảm font để “nhét” dữ liệu. Thu chrome/sidebar/row rhythm trước, sau đó ẩn cột ưu tiên thấp, stack pane hoặc dùng drawer/horizontal grid scroll.

Quy tắc `FILE-EXPORT`:

- Route chỉ truyền `VppFileExportFormat`; không truyền suffix endpoint dạng string từ markup.
- `VppFileExportActions` sở hữu label, icon, disabled và busy state theo từng định dạng.
- `IBrowserFileDownloadService` stream response kèm MIME thật sang browser, xác nhận số byte và dùng tên từ `Content-Disposition`.
- Backend dùng `ExportFileContract` cho tên file/MIME; Excel phải có header style, độ rộng cột, freeze header, auto-filter và number format phù hợp.
- PDF ưu tiên bản in/tóm tắt dễ đọc; Excel giữ dữ liệu chi tiết; CSV chỉ là định dạng phụ cho báo cáo hoặc tích hợp dữ liệu.

Footer có paging dùng một thứ tự cố định trên desktop: summary ở trái; cụm điều hướng trang, page-size và nhãn page-size ở phải. Mọi `RadzenDataGrid` có paging dùng `PagerHorizontalAlign="HorizontalAlign.Right"`; standalone `RadzenPager` dùng `HorizontalAlign="HorizontalAlign.Right"`. Responsive dưới `768px` dùng grid mobile native của Radzen, không để route tự căn giữa hoặc căn trái riêng.

## 4. Quy tắc action

| Cấp | Ví dụ | Vị trí |
|---|---|---|
| Collection | Thêm, import, export | Collection header hoặc action bar của workspace |
| Query/display | Tìm, lọc, xóa lọc, cột | Data toolbar |
| Row | Xem, sửa, xóa, khôi phục | Cột Thao tác của dòng |
| Workflow | Tiếp tục, gửi duyệt, chốt kỳ | Workflow/action footer của route |
| Destructive | Hủy, xóa, vô hiệu hóa | Row/workflow action, có confirm khi cần |

## 5. Route metadata contract

Mỗi logical route phải có profile trong `src/Frontend/Blazor/Helpers/UiRouteCatalog.cs`:

- `WorkspacePattern`;
- `DataSourceMode`;
- `Density`;
- `ToolbarMotif`;
- `FooterMotif`;
- `ResponsiveStrategy`;
- `StateCoverage`;
- `Notes` cho ngoại lệ nghiệp vụ.

`RouteCatalog` và `UiRouteCatalog` phải có cùng tập key. Route mới không được merge nếu chưa có profile và architecture test tương ứng.

## 6. Legacy retirement

Chỉ được xóa khi cả bốn điều kiện cùng đạt:

1. source/markup/JS/CSS scan còn `0 consumer`;
2. replacement canonical đã có consumer thật;
3. build + architecture test + focused route test pass;
4. execution/consumer ledger ghi `DELETE` và rollback rõ.

Các file/selector đã retire không được giữ alias “cho chắc” nếu alias tạo thêm implementation hoặc cascade cạnh tranh. Compatibility chỉ được giữ khi có dependency ngoài source đã chứng minh.

## 7. Review board bắt buộc

Mỗi motif mới hoặc migration lớn phải có board runtime nhỏ, không commit raw trace:

- desktop `1366×768` hoặc `1920×1080`;
- tablet `768×1024`;
- mobile `390×844` khi route responsive;
- Light/Dark nếu motif dùng semantic theme;
- frame bắt đầu/mid/settled khi có motion;
- keyboard/focus và state representative.

Screenshot Atlas không được dùng để tuyên bố Blazor đã pass.
