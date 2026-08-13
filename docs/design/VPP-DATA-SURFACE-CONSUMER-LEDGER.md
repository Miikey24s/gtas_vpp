# VPP Data Surface Consumer Ledger

> Snapshot: `2026-08-09` · Authority: [`UI-DATA-SURFACE-001`](../execution/UI-DATA-SURFACE-001.md) · Trạng thái: `DS0–DS4 + R1 DONE; PERIOD/PRICING/REPORT RETROFIT IMPLEMENTED`
>
> Contract update: `2026-08-13` · `CAPABILITY-SURFACE` retrofit được duyệt và lên lịch theo `FR9`; chưa được xem là đã áp dụng toàn bộ consumer.

Ledger này là bản đồ migration, không phải yêu cầu mọi bảng phải giống hệt nhau. Shared foundation chỉ sở hữu frame, toolbar, density, footer và transient cell value; route vẫn sở hữu dữ liệu, cột, API, permission và action.

## Radzen DataGrid inventory

Source hiện có **21 file / 26 DataGrid thật**. Generic type reference trong `VppColumnPicker` và custom list phân trang của Create Order không được tính là grid instance. Màn `Tab_AllOrdersSummary` cũ (2 grid) và dialog lịch sử đơn cũ (1 grid) đã về 0 consumer nên được xóa; URL legacy chỉ còn redirect về Chốt kỳ, còn lịch sử dùng workspace canonical.

| Consumer | Grid | Surface | Data source hiện tại | Density đích | Wave migration |
|---|---:|---|---|---|---|
| `Components/DesignSystem/Composites/VppOrderItemsSurface.razor` | 1 | Detail items | `Static` hoặc `ClientSnapshotPaged` khi trên 100 dòng | `RichTwoLine` | DS2 reference + bounded paging retrofit |
| `Components/Pages/Lib/Tabs/Dialog/Dialog_PriceListImport.razor` | 1 | Xem trước import bảng giá | `ClientSnapshotPaged` | `Compact` | Pricing import preview có mapping và xác nhận |
| `Components/Pages/Lib/Tabs/Tab_CategoryLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | AA2 typed collection |
| `Components/Pages/Lib/Tabs/Tab_SupplierLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | AA3 typed collection |
| `Components/Pages/Lib/Tabs/Tab_ItemLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | AA3 typed collection |
| `Components/Pages/Lib/Tabs/Tab_DepartmentLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | AA3 typed collection |
| `Components/Pages/Lib/Tabs/Tab_LookupLibrary.razor` | 2 | Master/detail admin | `ServerPaging` | `Compact` | DS4 complete |
| `Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor` | 1 | Price-list context + item-price query collection | `ServerPaging` | `Compact` | DS4 complete + context/filter retrofit |
| `Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor` | 1 | Price-list lifecycle collection | `ServerPaging` | `Compact` | DS4 complete + lifecycle action consolidation |
| `Components/Pages/Permission/Dialogs/Dialog_PermissionUiBatchEditor.razor` | 1 | Permission batch editor | `Static` | `Compact` | AA6 workspace editor |
| `Components/Pages/Permission/Tabs/Tab_SecurityAudit.razor` | 1 | Security audit collection | `ServerPaging` | `Compact` | AA7 read-only audit |
| `Components/Pages/Permission/Tabs/Tab_PagePermission.razor` | 1 | Permission group collection | `ServerPaging` | `Compact` | Full-width group table; permission detail loads in batch editor |
| `Components/Pages/Permission/Tabs/Tab_User.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | DS4 complete |
| `Components/Pages/Permission/Tabs/Tab_OrderPeriodSettings.razor` | 1 | Versioned ordering defaults history | `ClientSnapshotPaged` | `Compact` | System Admin operation + canonical history frame |
| `Components/Pages/Report.razor` | 2 | Analytics evidence tables | `Static` | `Compact` | Analytics workspace + bounded static frames complete |
| `Components/Pages/VPPRequest/Components/HistoryOrderList.razor` | 1 | Order collection | `ServerPaging` | `Compact` | DS2 reference complete |
| `Components/Pages/VPPRequest/Components/OrderPeriodManagementWorkspace.razor` | 1 | Admin order-period collection | `Static` | `Compact` | Canonical collection header + capability-driven row actions |
| `Components/Pages/VPPRequest/Components/PendingApprovalWorkspace.razor` | 1 | Canonical approval List-Detail; detail dùng shared item surface | `ServerPaging` + shared detail snapshot | `RichTwoLine` master; `Compact` detail | Collection header/filter/default selection/footer complete |
| `Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor` | 4 | Chốt kỳ theo phòng ban / người dùng / mặt hàng + yêu cầu hiệu chỉnh | `ClientSnapshotPaged` | `Compact` | DS3 unified period workspace |
| `Components/Pages/VPPRequest/OrderCreateStep3.razor` | 1 | Review selection | `ClientSnapshotVirtualized` | `RichTwoLine` | DS3 |
| `Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor` | 1 | Product collection | `ServerPaging` | `RichTwoLine` | DS2 reference complete |

## Custom list/table inventory

| Consumer | Loại | Quyết định |
|---|---|---|
| `HistoryOrderList` mobile list | Responsive mirror | Dùng cùng data/filter/state với desktop; không tạo data-source mode riêng |
| `OrderCreateStep2` orderable catalog | Custom paged data surface | Nạp snapshot được phép đặt theo batch, lọc trên toàn snapshot rồi phân trang UI mặc định 100; body vẫn scroll nội bộ và không gọi API khi đổi trang |
| `OrderCreateStep2` draft list | Static workflow list | Giữ action/quantity route-owned; chỉ nhận row rhythm/footer ở DS3 |
| `Tab_PagePermission` permission matrix | Matrix exception | Không ép cột `#`, paging hoặc data-table motif thông thường |
| `VppColumnPicker` option list | Popover control | Thuộc toolbar control, không tính là data grid |
| `NotificationCenter` list | Application feed | Ngoài data-surface migration; giữ state/accessibility contract riêng |

## DS1 representative proof

| Behavior | Consumer | Foundation dùng | Không thay đổi |
|---|---|---|---|
| Server paging | History order list | `VppDataSurfaceFrame`, `VppDataToolbar`, opt-in `vpp-data-grid`, `VppCellValuePopover` | API `skip/top`, pager, filter state, row selection |
| Client snapshot + bounded paging | Order-items detail | `VppDataSurfaceFrame`, `VppDataToolbar`, Radzen pager, opt-in `vpp-data-grid`, `VppCellValuePopover` | Input collection, filter orchestration và export/action |

## Consumer ledger rule

- Thêm/xóa file có DataGrid phải cập nhật ledger cùng change-set.
- Chỉ xóa CSS/adapter legacy khi consumer tương ứng không còn trong cột migration.
- `OrderCreateStep2` vẫn là legacy server-window trong DS1; chỉ DS3 được đổi sang snapshot client đã duyệt trong plan.
- Khi một consumer được chạm trong refactor, phải audit thêm `CAPABILITY-SURFACE`: action order, hidden/disabled reason, loading footprint, base-empty, filtered-empty, error, footer/pager và empty-row hover.

## Stable Capability Surface retrofit queue

| Nhóm | Consumer ưu tiên | Việc cần khóa | Trạng thái |
|---|---|---|---|
| Reference | `OrderPeriodManagementWorkspace` | Primary action ổn định; menu đủ action theo thứ tự cố định; unavailable disabled; menu toàn disabled vẫn mở có chủ đích | `REFERENCE IMPLEMENTED` |
| Admin collections | Category, Supplier, Item, Department, Lookup, Price List, Price, User, Permission | Cùng action set cho cùng entity; permission hidden; lifecycle unavailable disabled; base-empty giữ toolbar/cột/footer | `PLANNED FR9-A` |
| Requests read/write | My Orders, History, Product Catalog, Department Summary, Order Create | Giữ command footprint; archive/read-only dùng disabled đúng nghĩa; empty/filtered-empty không giả row tương tác | `PLANNED FR9-B` |
| Operations | Pending Approval, Settlement, Period Settings | Workflow action theo capability; busy giữ geometry; denied không lộ chức năng; error retry giữ frame | `PLANNED FR9-C` |
| Analytics/feeds | Report, Security Audit, Notifications | Static/analytics empty state đúng shell; không pager giả; action/filter không nhảy theo data | `PLANNED FR9-D` |

Mỗi nhóm chỉ chuyển `DONE` sau architecture test, focused route test và browser review tại viewport phù hợp. Không chạy mass-rewrite; áp dụng theo module boundary để tránh trộn refactor cấu trúc với thay đổi nghiệp vụ.

## DS2 reference group

- `HistoryOrderList`: canonical search/select/clear, typed server-paged frame và compact density; không còn route-owned filter menu/CSS/state.
- `VppOrderItemsSurface`: canonical filters, rich two-line rows và paging 100 khi tập dữ liệu vượt ngưỡng; History detail + My Orders dùng cùng component.
- `Tab_ProductCatalog`: canonical toolbar trong typed server-paged frame, rich two-line bridge và giữ API/paging/sort hiện hữu.

## DS3 workflow group

- `OrderCreateStep2`: client snapshot theo batch, filter toàn bộ snapshot và pager mặc định 100; canonical toolbar/code popup, scroll/đổi trang không gọi lại API.
- `Tab_DepartmentSummary`: tái sử dụng `HistoryOrderList` và toàn bộ History workspace; route chỉ truyền cột người đặt riêng, API và permission phòng ban.
- `PeriodSettlementPanel`: hợp nhất rà soát, supplier preview và confirm/correct trong một typed frame; legacy period URL vẫn mở cùng workspace nhưng không còn stepper bốn bước.

## DS4 admin group

- Categories, Items, Suppliers, Departments, Lookup, Price, Price List và Users dùng route-typed server-paged frame, compact density, canonical toolbar, column picker và native grid scroll/pager. Theo quyết định owner 2026-07-30, toolbar là nguồn lọc chính; header chỉ sort và `FilterMode.CheckBoxList` không phải admin default. Giá mặt hàng tách `Bảng giá` thành context cố định; toolbar chỉ giữ query filter `Danh mục + trạng thái ánh xạ`. `Component_ShareGrid<T>` reflection legacy đã hết consumer và được xóa.
- Library shell truyền chiều cao viewport xuống active panel; Lookup chia đôi desktop và xếp dọc tablet. List/detail admin cũng xếp dọc ở tablet để không cắt pane hoặc tạo horizontal document overflow.
- Pricing tabs là navigation nội bộ trong flow, không còn sticky-offset đè lên toolbar. Danh sách bảng giá giữ ba action trực tiếp tối đa; publish/expire/default/clone/active/hard-delete nằm trong menu lifecycle dùng chung.
- Permission group/component grids opt-in cùng header/row/footer bridge; permission action matrix tiếp tục là ngoại lệ đúng nghiệp vụ.
- Report dùng `VppAnalyticsWorkspace`; hai bảng evidence là `Static + Compact`, mỗi bảng có collection header và static summary footer nhưng không giả server paging/pager.
