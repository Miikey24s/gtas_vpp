# VPP Data Surface Consumer Ledger

> Snapshot: `2026-07-29` · Authority: [`UI-DATA-SURFACE-001`](../execution/UI-DATA-SURFACE-001.md) · Trạng thái: `DS0–DS1 DONE; DS2 IMPLEMENTED — OWNER REVIEW`

Ledger này là bản đồ migration, không phải yêu cầu mọi bảng phải giống hệt nhau. Shared foundation chỉ sở hữu frame, toolbar, density, footer và transient cell value; route vẫn sở hữu dữ liệu, cột, API, permission và action.

## Radzen DataGrid inventory

Source hiện có **19 file / 25 DataGrid thật**. Generic type reference trong `VppColumnPicker` không được tính là grid instance.

| Consumer | Grid | Surface | Data source hiện tại | Density đích | Wave migration |
|---|---:|---|---|---|---|
| `Components/DesignSystem/Composites/VppOrderItemsSurface.razor` | 1 | Detail items | `ClientSnapshotVirtualized` | `RichTwoLine` | DS2 reference complete |
| `Components/Pages/Lib/Component_ShareGrid.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | DS4 |
| `Components/Pages/Lib/Tabs/Tab_LookupLibrary.razor` | 2 | Master/detail admin | `ServerPaging` | `Compact` | DS4 |
| `Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | DS4 |
| `Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | DS4 |
| `Components/Pages/Permission/Tabs/Tab_PagePermission.razor` | 2 | Permission collection | `ServerPaging` | `Compact` | DS4 |
| `Components/Pages/Permission/Tabs/Tab_User.razor` | 1 | Admin collection | `ServerPaging` | `Compact` | DS4 |
| `Components/Pages/Report.razor` | 2 | Static report | `Static` | `Compact` | DS4 / exception review |
| `Components/Pages/VPPRequest/Components/Dialog_RequestHistory.razor` | 1 | Dialog history | `Static` | `Compact` | Deferred dialog exception; không thuộc reference route DS2 |
| `Components/Pages/VPPRequest/Components/HistoryOrderList.razor` | 1 | Order collection | `ServerPaging` | `Compact` | DS2 reference complete |
| `Components/Pages/VPPRequest/Components/PendingApprovalWorkspace.razor` | 2 | Approval + detail | `ServerPaging` + `Static` | `Compact` | DS3 |
| `Components/Pages/VPPRequest/Components/PeriodDemandPanel.razor` | 1 | Demand collection | `Static` + client pager | `RichTwoLine` | DS3 |
| `Components/Pages/VPPRequest/Components/PeriodReviewPanel.razor` | 2 | Review + detail | `ServerPaging` + `Static` | `Compact` | DS3 |
| `Components/Pages/VPPRequest/Components/PeriodSupplyAllocationPanel.razor` | 1 | Allocation comparison | `ClientSnapshotVirtualized` | `RichTwoLine` | DS3 |
| `Components/Pages/VPPRequest/OrderCreateStep2.razor` | 1 | Orderable catalog | legacy server-window virtualization → `ClientSnapshotVirtualized` | `RichTwoLine` | DS3 |
| `Components/Pages/VPPRequest/OrderCreateStep3.razor` | 1 | Review selection | `ClientSnapshotVirtualized` | `RichTwoLine` | DS3 |
| `Components/Pages/VPPRequest/Tabs/Tab_AllOrdersSummary.razor` | 2 | Orders + detail | `ServerPaging` + `Static` | `Compact` | DS3 |
| `Components/Pages/VPPRequest/Tabs/Tab_DepartmentSummary.razor` | 1 | Department orders | `ServerPaging` | `Compact` | DS3 |
| `Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor` | 1 | Product collection | `ServerPaging` | `RichTwoLine` | DS2 reference complete |

## Custom list/table inventory

| Consumer | Loại | Quyết định |
|---|---|---|
| `HistoryOrderList` mobile list | Responsive mirror | Dùng cùng data/filter/state với desktop; không tạo data-source mode riêng |
| `OrderCreateStep2` draft list | Static workflow list | Giữ action/quantity route-owned; chỉ nhận row rhythm/footer ở DS3 |
| `Tab_PagePermission` permission matrix | Matrix exception | Không ép cột `#`, paging hoặc data-table motif thông thường |
| `PeriodDemandPanel` nested detail table | Static nested detail | Giữ progressive disclosure; không biến thành grid server độc lập nếu chưa cần |
| `VppColumnPicker` option list | Popover control | Thuộc toolbar control, không tính là data grid |
| `NotificationCenter` list | Application feed | Ngoài data-surface migration; giữ state/accessibility contract riêng |

## DS1 representative proof

| Behavior | Consumer | Foundation dùng | Không thay đổi |
|---|---|---|---|
| Server paging | History order list | `VppDataSurfaceFrame`, `VppDataToolbar`, opt-in `vpp-data-grid`, `VppCellValuePopover` | API `skip/top`, pager, filter state, row selection |
| Client snapshot + virtualized DOM | Order-items detail | `VppDataSurfaceFrame`, `VppDataToolbar`, `VppDataSummaryFooter`, opt-in `vpp-data-grid`, `VppCellValuePopover` | Input collection, filter orchestration, export/action, virtualization |

## Consumer ledger rule

- Thêm/xóa file có DataGrid phải cập nhật ledger cùng change-set.
- Chỉ xóa CSS/adapter legacy khi consumer tương ứng không còn trong cột migration.
- `OrderCreateStep2` vẫn là legacy server-window trong DS1; chỉ DS3 được đổi sang snapshot client đã duyệt trong plan.

## DS2 reference group

- `HistoryOrderList`: canonical search/select/clear, typed server-paged frame và compact density; không còn route-owned filter menu/CSS/state.
- `VppOrderItemsSurface`: canonical filters, typed client-snapshot virtualized frame, rich two-line rows và virtual footer dùng chung cho History detail + My Orders.
- `Tab_ProductCatalog`: canonical toolbar trong typed server-paged frame, rich two-line bridge và giữ API/paging/sort hiện hữu.
