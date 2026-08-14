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
| `DATA-FRAME` | Header/toolbar/grid/footer frame | Bảng/list có data surface | `VppDataSurfaceFrame`; Radzen body bridge giữ `scrollbar-gutter: stable` một cạnh | `ServerPaging`, `ClientSnapshotPaged`, `ClientSnapshotVirtualized`, `Static` | border, overflow, footer anchor; đổi trục cột khi scrollbar xuất hiện hoặc dùng `both-edges` tạo gutter giả hai bên |
| `COLLECTION-HEADER` | Identity + count + collection action | Add/import/export thuộc cả collection | `VppCollectionHeader` | add/secondary/disabled | CRUD trong toolbar hoặc header cột |
| `BUTTON-ACTION` | Button có text/icon theo cấp hành động | Collection, query, workflow, dialog | Radzen button bridge trong `vpp-radzen-theme.css`; token `--vpp-button-*`; component domain chỉ khi có behavior riêng | standard 32px, compact/icon-only 28px, icon 16px; primary/secondary/light/success/warning/danger | action button cao 36–44px, shadow/translate/oval focus riêng theo route; universal button wrapper chỉ đổi tên markup |
| `ADMIN-ROW-ACTIONS` | Action chính theo trạng thái + lifecycle/destructive của một dòng | Các bảng quản trị | `VppAdminIconAction`, `VppAdminActionMenu`, `VppAdminLifecycleMenu`, `VppAdminActiveToggle`, cột `vpp-admin-actions` | một action trực tiếp có nhãn ngắn; menu overflow full-text dùng cùng surface, row rhythm và hover của select dọc; active toggle chỉ khi trạng thái là quyết định trực tiếp của page | dàn 3–5 icon ngang hàng; giấu Duyệt/Từ chối/Chốt kỳ trong menu; route tự đặt kích thước/icon chrome hoặc dựng switch shell khác |
| `CAPABILITY-SURFACE` | Stable Capability Surface — giữ cấu trúc chức năng ổn định giữa các trạng thái | Action group, toolbar, grid/list và empty state cần giúp người dùng biết chức năng nào tồn tại | Route-owned typed capability projection + `VppAdminActionMenu`, `VppDataSurfaceFrame`, `VppContentState`; permission vẫn do backend/API quyết định | enabled/disabled/busy/hidden-by-permission; populated/base-empty/filtered-empty/error | ẩn action chỉ vì record chưa đủ điều kiện; thay đổi thứ tự action theo từng dòng; gỡ toolbar/header/footer khi empty; render empty như một row có hover/click |
| `FILTER-TOOLBAR` | Search, filter, clear, column picker | Tìm/lọc dữ liệu thường xuyên | `VppDataToolbar`, `VppFilterSearch`, `VppFilterSelect`, `VppClearFiltersButton`, `VppColumnPicker` | filter count/domain-specific options | popup chrome, control height, order |
| `FILTER-ADVANCED` | Bộ lọc ít dùng/nhiều điều kiện | Ngày, khoảng giá, metadata, audit/resource | typed route-owned filter panel anchored from the toolbar | compact/popover/workspace | không tạo filter icon riêng trong từng header |
| `SELECTOR-FILTER` | Thay đổi tập dữ liệu hiển thị | Category/status/department/unit filters | `VppFilterSelect<T>` | active/inactive, option count | tự tạo dropdown khác visual |
| `SELECTOR-DECISION` | Chọn mode/giải pháp nghiệp vụ | Kỳ, theo mặt hàng/phòng ban, supplier/price list | `VppSegmentedSelector<T>` cho lựa chọn ngang; `VppDecisionSelect<T>` cho dropdown decision | segmented/dropdown decision | gọi là filter nếu thực tế là decision |
| `SELECTOR-TIME-SCOPE` | Chọn thời điểm, tháng/kỳ hoặc khoảng kỳ | History, Tổng hợp phòng ban, Chốt kỳ, dialog gia hạn kỳ | `VppPeriodPickerPopover` cho tháng/kỳ; Radzen `DatePicker` được normalize bằng `vpp-radzen-theme.css` cho ngày giờ | single period, period range, date-time | native select/popup route-local; lặp lại nhãn và giá trị kỳ; control cao/viền/motion khác selector canonical |
| `SELECTOR-PAGE-SIZE` | Chọn số dòng mỗi trang | Mọi server/client paged grid | Radzen pager bridge + page-size contract trong `vpp-radzen-theme.css` | 25/50/100/200 theo profile; trigger label-only 56×32 không chevron; dropup cùng chiều rộng và vertical-option rhythm với select canonical | custom popup/oval focus riêng; để Radzen và accessibility CSS cùng vẽ chrome |
| `DATA-ROW` | Nhịp hàng và semantic cell | Mọi grid canonical | `vpp-datagrid.css` + route-owned typed columns | `Compact`, `RichTwoLine` | zebra tự bật, inline color/radius |
| `DATA-COLUMN` | Column contract | `#`, code/name, note, amount, status, action | route-owned typed `RadzenDataGridColumn` theo [data-surface ledger](VPP-DATA-SURFACE-CONSUMER-LEDGER.md) | visible/pickable/frozen/filterable/sortable | reflection/string column config |
| `DATA-FOOTER` | Summary, pager, page size, workflow action | Cuối data surface | `VppDataSummaryFooter` hoặc Radzen pager bridge; token `--vpp-data-summary-*` cho dòng tổng quan trọng | summary/paged/virtualized/action; dòng tổng quan trọng dùng amber/warning, không dùng danger | footer giả, row đè footer, route tự đổi căn lề hoặc dùng đỏ như lỗi/nguy hiểm cho số tổng bình thường |
| `CELL-VALUE` | Code/note dài và copy | Ô bị truncate | `VppCellValuePopover` | code/note/copy | popup route tự neo khác contract |
| `CONTENT-STATE` | Loading/empty/filter-empty/error/denied/disabled/success | Mọi route có state | `VppContentState`, `VppContentStateKind` | typed state + semantic role + `FillAvailable`; empty full-height căn giữa cả hai trục | text state tự dựng bằng string switch, CSS min-height riêng từng route hoặc nút refresh thủ công trong empty state |
| `STATUS-BADGE` | Trạng thái ngắn, có màu semantic | Order/admin/permission/period lifecycle state | `VppStatusBadge` + `VppStatusTone`; `VppPeriodStateBadge` là composite canonical cho vòng đời kỳ | info/success/warning/danger/neutral; `Muted` cho số `0`; `Emphasized` cho dòng tổng | badge tự map string hoặc màu theo route; đổi tone chỉ để phân biệt dòng tổng |
| `CATEGORY-CHIP` | Phân loại dữ liệu, không biểu diễn tiến trình | Loại đơn, nhóm dữ liệu, metric context ngắn | `VppCategoryChip` + `VppCategoryTone` | neutral/primary/accent; truyền tiếp `Muted`/`Emphasized` từ badge primitive | dùng màu success/danger để ám chỉ kết quả nghiệp vụ |
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
- Thứ tự canonical là `Tìm kiếm → filter theo thứ tự cột từ trái sang phải → Áp dụng (nếu route cần submit) → Xóa bộ lọc → Cột`. Search là truy vấn toàn dòng nên luôn đứng đầu; selector phạm vi/decision như kỳ, tab hoặc mode nằm ở context riêng và không chen vào thứ tự filter dữ liệu.
- Filter chỉ map tới cột nào thì phải đứng cùng thứ tự tương đối với cột đó. Nếu filter áp cho dữ liệu không có cột riêng, đặt sau các filter đã map cột nhưng trước `Xóa bộ lọc`; ghi ngoại lệ trong consumer ledger.
- Header cột mặc định chỉ sở hữu sort; admin grid không bật Radzen `FilterMode.CheckBoxList` theo mặc định.
- Điều kiện hiếm hoặc nhiều trường dùng `FILTER-ADVANCED` từ toolbar, không rải popup nhỏ ở từng header.
- `VppColumnPicker` chỉ điều chỉnh hiển thị cột, không thay thế bộ lọc.
- Filter luôn áp trên toàn bộ tập dữ liệu được cấp quyền trước `paging`/`virtualization`, sau đó mới tính tổng và phân trang.

Không gộp chúng thành một component string-configured. Dùng typed component/contract riêng, cùng token và popup bridge.

## 3.1. `DATA-SURFACE-ORDER`

Mọi data surface phải kể cùng một câu chuyện đọc, nhưng route vẫn giữ ngoại lệ nghiệp vụ có bằng chứng:

1. `#` hoặc định danh chính: kỳ, mã, tên, người dùng, phòng ban, mặt hàng.
2. Phân loại và trạng thái: loại đơn, trạng thái đơn/kỳ, nhóm quyền, danh mục, đơn vị.
3. Số liệu: số đơn, số dòng, số lượng, đơn giá, thành tiền, VAT, tổng cộng.
4. Thời gian và nội dung bổ sung: ngày gửi/tạo/cập nhật, mô tả, ghi chú.
5. `Thao tác` luôn cuối và frozen bên phải khi grid cần row action.

Quy tắc tên cột:

- Dùng cùng một nhãn cho cùng một khái niệm: `Mặt hàng`, `Danh mục`, `Đơn vị`, `Nhà cung cấp`, `Phòng ban`, `Trạng thái`, `Số lượng`, `Đơn giá`, `Thành tiền`, `VAT`, `Tổng cộng`, `Thao tác`.
- Header ưu tiên 1–3 từ và bỏ từ đã rõ từ context: `Hạn duyệt bổ sung` thay cho `Hạn duyệt đơn bổ sung`; `Cập nhật` thay cho `Thay đổi gần nhất`. Không viết acronym kỹ thuật như `NCC`, `MĐ`, `SL` hoặc nhãn tiếng Anh nội bộ trong UI tiếng Việt.
- Không đổi nghĩa chỉ để tránh ellipsis. Nếu tên ngắn hợp lệ vẫn bị cắt ở desktop, chỉnh track/min-width/priority cột; chỉ cột phụ mới ẩn vào `VppColumnPicker` trên viewport hẹp.
- Cột ẩn/pickable và danh sách `Cột` giữ đúng thứ tự khai báo của grid; metadata registry dùng tên người dùng hiểu, không dùng `Class Code`, `VPP Category`, `Create User` hoặc identifier kỹ thuật tương tự.
- Default sort: dữ liệu vận hành và audit mới → cũ; danh mục theo tên/mã tự nhiên; thứ tự cấu hình/lookup theo trường nghiệp vụ. Không đảo sort chỉ để đổi bố cục cột.

Quy tắc thứ tự action:

- Cụm ngoài dòng: `Xem/Chi tiết → xuất/chia sẻ → chỉnh sửa → workflow chính → destructive`; action workflow chính có thể được nhấn màu nhưng vẫn giữ vị trí ổn định giữa các trạng thái.
- Cột dòng: action chính trước, overflow `...` sau. Trong overflow: `Xem → Sửa → nghiệp vụ không phá hủy → khôi phục/vô hiệu hóa → xóa vĩnh viễn`; destructive luôn cuối.
- Dialog footer: leading utility ở trái; bên phải luôn `Hủy → Xác nhận/Lưu/Duyệt/Chốt`. Nếu chỉ có một action thì đặt tại vị trí xác nhận, không đảo trái–phải theo route.
- Workflow nhiều bước: `Quay lại` tách ở trái; nhóm phải theo `Lưu nháp → tiện ích/ghi chú → Tiếp tục/Gửi`. Approval đặt `Từ chối → Duyệt`; drawer đặt `Xem toàn màn hình → PDF → Excel → chỉnh sửa → Đóng`.
- Collection header: utility như lịch sử/xuất file đứng trước action tạo/import; component `VppFileExportActions` giữ thứ tự `PDF → Excel → CSV`. Không đổi thứ tự theo loading, quyền hoặc dữ liệu rỗng.
- Tab/selector đi từ phạm vi lớn → nhóm dữ liệu → chi tiết và mặc định vào lựa chọn đầu tiên hợp lệ. Loading/empty/error giữ nguyên thứ tự toolbar, cột, action và footer để capability surface không nhảy.

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
- Nhóm badge dùng để so sánh số lượng theo loại hoặc trạng thái giữ đủ các mục canonical và hiển thị `0` thay vì biến mất. Badge có giá trị `0` dùng `Muted="true"` để giảm nhấn nhưng giữ nguyên vị trí, kích thước và tone nhận diện; badge ở dòng tổng dùng thêm `Emphasized="true"` để chuyển sang nền đặc trong chính tone semantic, không đổi sang một màu nghiệp vụ khác. Chỉ thu gọn còn một badge khi người dùng chủ động lọc đúng chiều dữ liệu đó.
- Base-empty vẫn giữ collection header, toolbar, column header, body và footer/pager theo cùng geometry với populated state. Collection action hợp lệ như `Thêm` hoặc `Import` vẫn hoạt động; filter/pager không có dữ liệu có thể disabled nhưng không bị tháo khỏi layout.
- Filtered-empty giữ filter và `Xóa bộ lọc` hoạt động. Body chỉ hiển thị một `VppContentState` căn giữa cả hai trục; không tạo fake data row, không hover/cursor/action-row affordance. Footer hiển thị tổng `0` và pager disabled nếu surface có paging.
- Error state giữ cùng frame và có `Thử lại`; denied state không được để lộ action/cột bị chặn bởi permission. Menu toàn disabled chỉ được mở khi nó giải thích rõ capability của cùng nhóm; không render menu rỗng.

Quy tắc ổn định trục DataGrid:

- Vùng cuộn canonical của DataGrid dùng scrollbar native và `scrollbar-gutter: stable` ở cạnh cuối để header, body, footer và grid kế bên không nhảy chiều rộng khi vertical overflow xuất hiện hoặc biến mất.
- Không dùng `stable both-edges`, custom scrollbar width/color hoặc pseudo-element scrollbar; các cách đó tạo dải trống giả, lệch tâm hoặc conflict theo browser/theme.
- Nhóm badge so sánh cùng schema dùng track cố định theo vị trí semantic. Độ dài số (`1`, `51`, `100`) không được làm thay đổi tâm giữa body và dòng tổng.

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
