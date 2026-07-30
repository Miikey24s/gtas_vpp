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
| `HEADER-TAB-GROUP` | Điều hướng cha–con cùng primary header | Nhóm route Quản lý kỳ, Bảng giá | `VppHeaderTabGroup`, `VppHeaderSubTab`, `LeftSidebar`, shared tab indicator | expanded desktop, local mobile fallback, permission-aware default | dùng selector decision/filter thay navigation; active line ở cả cha và con |
| `ACCOUNT` | Account/auth form shell | Login, register, password, email flow | `VppAccountWorkspace` | artwork, compact, scrollable | brand/language/header layout |
| `COLLECTION` | Danh sách một tập dữ liệu | Library, users, audit, catalog | `VppCollectionWorkspace` + `VppDataSurfaceFrame` | compact/rich, paged/static | frame, toolbar, footer geometry |
| `LIST-DETAIL` | Danh sách + inspector/detail | History, permission, lookup | `VppListDetailWorkspace` | ratio + overlay detail | split seam, pane height, detail placement |
| `SPLIT-EDITOR` | Hai vùng chọn/chỉnh sửa | Lookup, Create Order | `VppSplitEditorWorkspace` | resizable/fixed, stacked tablet | pane scroll ownership, seam |
| `OPERATION` | Workflow/operation screen | Period management, approval, order create | `VppOperationWorkspace` + `VppWorkflowStepper` | compact steps, action footer | step geometry, active/completed/pending states |
| `ANALYTICS` | KPI/chart/list/detail data story | History, department summary, report | `VppAnalyticsWorkspace` | chart/list/detail arrangement | KPI rhythm, chart empty state, detail alignment |
| `DATA-FRAME` | Header/toolbar/grid/footer frame | Bảng/list có data surface | `VppDataSurfaceFrame` | `ServerPaging`, `ClientSnapshotPaged`, `ClientSnapshotVirtualized`, `Static` | border, overflow, footer anchor |
| `COLLECTION-HEADER` | Identity + count + collection action | Add/import/export thuộc cả collection | `VppCollectionHeader` | add/secondary/disabled | CRUD trong toolbar hoặc header cột |
| `FILTER-TOOLBAR` | Search, filter, clear, column picker | Tìm/lọc dữ liệu thường xuyên | `VppDataToolbar`, `VppFilterSearch`, `VppFilterSelect`, `VppClearFiltersButton`, `VppColumnPicker` | filter count/domain-specific options | popup chrome, control height, order |
| `FILTER-ADVANCED` | Bộ lọc ít dùng/nhiều điều kiện | Ngày, khoảng giá, metadata, audit/resource | typed route-owned filter panel anchored from the toolbar | compact/popover/workspace | không tạo filter icon riêng trong từng header |
| `SELECTOR-FILTER` | Thay đổi tập dữ liệu hiển thị | Category/status/department/unit filters | `VppFilterSelect<T>` | active/inactive, option count | tự tạo dropdown khác visual |
| `SELECTOR-DECISION` | Chọn mode/giải pháp nghiệp vụ | Kỳ, theo mặt hàng/phòng ban, supplier/price list | `VppSegmentedSelector<T>` hoặc typed Radzen select khi cần nhiều option | segmented/dropdown decision | gọi là filter nếu thực tế là decision |
| `SELECTOR-PAGE-SIZE` | Chọn số dòng mỗi trang | Mọi server/client paged grid | Radzen pager bridge + page-size contract trong `vpp-datagrid.css` | 25/50/100/200 theo profile | custom popup/oval focus riêng |
| `DATA-ROW` | Nhịp hàng và semantic cell | Mọi grid canonical | `vpp-datagrid.css` + route-owned typed columns | `Compact`, `RichTwoLine` | zebra tự bật, inline color/radius |
| `DATA-COLUMN` | Column contract | `#`, code/name, note, amount, status, action | route-owned typed `RadzenDataGridColumn` theo [data-surface ledger](VPP-DATA-SURFACE-CONSUMER-LEDGER.md) | visible/pickable/frozen/filterable/sortable | reflection/string column config |
| `DATA-FOOTER` | Summary, pager, page size, workflow action | Cuối data surface | `VppDataSummaryFooter` hoặc route action footer | summary/paged/virtualized/action | footer giả hoặc row đè footer |
| `CELL-VALUE` | Code/note dài và copy | Ô bị truncate | `VppCellValuePopover` | code/note/copy | popup route tự neo khác contract |
| `CONTENT-STATE` | Loading/empty/filter-empty/error/denied/disabled/success | Mọi route có state | `VppContentState`, `VppContentStateKind` | typed state + semantic role | text state tự dựng bằng string switch |
| `STATUS-BADGE` | Trạng thái ngắn, có màu semantic | Order/admin/permission state | `VppStatusBadge` + `VppStatusTone` | info/success/warning/danger/neutral | badge tự map string hoặc màu theo route |
| `METRIC-CARD` | Một chỉ số định lượng | Analytics/report summary | `VppMetricCard` | neutral/accent/success/warning | raw `kpi-card`/shine markup |
| `DIALOG-EDITOR` | Thêm/sửa form | Admin CRUD | `VppAdaptiveDialogShell` + typed dialog contract | compact/wide/fullscreen, sticky footer | inline row edit hoặc dialog tự vẽ shell |
| `DIALOG-ACTIONS` | Hủy/lưu/submit/destructive action | Dialog/editor | `vpp-adaptive-dialog-actions` contract + Radzen buttons | primary/secondary/danger | footer spacing riêng từng dialog |
| `TRANSIENT` | Popup, popover, user menu, filter, notification | Surface tạm thời | `vpp-transient-surface`, `vpp-polish.css`, Radzen bridge | above/down/center, reduced-motion | `transform` làm đổi anchor geometry |
| `FEEDBACK` | Inline notice/toast/reconnect | Thông báo hệ thống/nghiệp vụ | `VppInlineNotice`, toast/reconnect contract | neutral/info/success/warning/danger | `RadzenAlert` hoặc raw exception text tự dựng theo route |
| `SKELETON` | Loading placeholder | Chờ data | `SkeletonPage`, `SkeletonGrid`, `vpp-loading.css` | page/grid/row | legacy `.shimmer-*` mới |

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
