# VPP Data Surface Consumer Ledger

> Snapshot: `2026-07-29` · Authority: [`UI-DATA-SURFACE-001`](../execution/UI-DATA-SURFACE-001.md) · Trạng thái: `DS0–DS4 IMPLEMENTED; R1 IN PROGRESS`

Ledger này là bản đồ migration, không phải yêu cầu mọi bảng phải giống hệt nhau. Shared foundation chỉ sở hữu frame, toolbar, density, footer và transient cell value; route vẫn sở hữu dữ liệu, cột, API, permission và action.

## Radzen DataGrid inventory

Source hiện có **20 file / 25 DataGrid thật**. Generic type reference trong `VppColumnPicker` và custom list phân trang của Create Order không được tính là grid instance.

| Consumer | Grid | Surface | Data source hiện tại | Density đích | Wave migration |
|---|---:|---|---|---|---|
| `Components/DesignSystem/Composites/VppOrderItemsSurface.razor` | 1 | Detail items | `Static` hoặc `ClientSnapshotPaged` khi trên 100 dòng | `RichTwoLine` | DS2 reference + bounded paging retrofit |
| `Components/Pages/Lib/Component_ShareGrid.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | DS4 complete |
| `Components/Pages/Lib/Tabs/Tab_CategoryLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | AA2 typed collection |
| `Components/Pages/Lib/Tabs/Tab_SupplierLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | AA3 typed collection |
| `Components/Pages/Lib/Tabs/Tab_ItemLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | AA3 typed collection |
| `Components/Pages/Lib/Tabs/Tab_DepartmentLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | AA3 typed collection |
| `Components/Pages/Lib/Tabs/Tab_LookupLibrary.razor` | 2 | Master/detail admin | `ServerPaging` | `Compact` | DS4 complete |
| `Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | DS4 complete |
| `Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | DS4 complete |
| `Components/Pages/Permission/Dialogs/Dialog_PermissionUiBatchEditor.razor` | 1 | Permission batch editor | `Static` | `Compact` | AA6 workspace editor |
| `Components/Pages/Permission/Tabs/Tab_PagePermission.razor` | 2 | Permission collection | `ServerPaging` | `Compact` | DS4 complete; matrix exception retained |
| `Components/Pages/Permission/Tabs/Tab_User.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | DS4 complete |
| `Components/Pages/Report.razor` | 2 | Static report | `Static` | `Compact` | DS4 static exception complete |
| `Components/Pages/VPPRequest/Components/Dialog_RequestHistory.razor` | 1 | Dialog history | `Static` | `Compact` | Deferred dialog exception; không thuộc reference route DS2 |
| `Components/Pages/VPPRequest/Components/HistoryOrderList.razor` | 1 | Order collection | `ServerPaging` | `Compact` | DS2 reference complete |
| `Components/Pages/VPPRequest/Components/PendingApprovalWorkspace.razor` | 1 | Approval list; detail dùng shared item surface | `ServerPaging` + shared detail snapshot | `Compact` | Split list-detail retrofit |
| `Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor` | 2 | Chốt kỳ theo đơn / theo phòng ban | `ServerPaging` + `ClientSnapshotPaged` | `Compact` | DS3 unified period workspace |
| `Components/Pages/VPPRequest/OrderCreateStep3.razor` | 1 | Review selection | `ClientSnapshotVirtualized` | `RichTwoLine` | DS3 |
| `Components/Pages/VPPRequest/Tabs/Tab_AllOrdersSummary.razor` | 2 | Orders + detail | `ServerPaging` + `Static` | `Compact` | DS3 |
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

## DS2 reference group

- `HistoryOrderList`: canonical search/select/clear, typed server-paged frame và compact density; không còn route-owned filter menu/CSS/state.
- `VppOrderItemsSurface`: canonical filters, rich two-line rows và paging 100 khi tập dữ liệu vượt ngưỡng; History detail + My Orders dùng cùng component.
- `Tab_ProductCatalog`: canonical toolbar trong typed server-paged frame, rich two-line bridge và giữ API/paging/sort hiện hữu.

## DS3 workflow group

- `OrderCreateStep2`: client snapshot theo batch, filter toàn bộ snapshot và pager mặc định 100; canonical toolbar/code popup, scroll/đổi trang không gọi lại API.
- `Tab_DepartmentSummary`: tái sử dụng `HistoryOrderList` và toàn bộ History workspace; route chỉ truyền cột người đặt riêng, API và permission phòng ban.
- `PeriodSettlementPanel`: hợp nhất rà soát, supplier preview và confirm/correct trong một typed frame; legacy period URL vẫn mở cùng workspace nhưng không còn stepper bốn bước.

## DS4 admin group

- `Component_ShareGrid`, Lookup, Price, Price List và Users dùng typed server-paged frame, compact density, canonical toolbar, column picker và native grid scroll/pager.
- Library shell truyền chiều cao viewport xuống active panel; Lookup chia đôi desktop và xếp dọc tablet. List/detail admin cũng xếp dọc ở tablet để không cắt pane hoặc tạo horizontal document overflow.
- Pricing tabs là navigation nội bộ trong flow, không còn sticky-offset đè lên toolbar.
- Permission group/component grids opt-in cùng header/row/footer bridge; permission action matrix tiếp tục là ngoại lệ đúng nghiệp vụ.
- Report giữ hai grid static: dùng compact visual contract nhưng không giả server paging, pager hoặc footer.
