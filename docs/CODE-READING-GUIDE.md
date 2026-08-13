# Sổ tay đọc code GTAS VPP

Tài liệu này để **đọc và trình bày code**, không phải để thiết kế. Mở luận văn ra, thấy một hình
giao diện, tra bảng ở mục 2 là biết ngay màn đó nằm ở file nào, gọi API nào và bị ràng buộc bởi quy
tắc nghiệp vụ nào.

- Quy tắc làm việc: `AGENTS.md`
- Kế hoạch UI canonical: `docs/execution/UI-SYSTEM-001.md`
- Kế hoạch refactor frontend/current handoff: `docs/execution/FRONTEND-REFACTOR-001.md`
- Kế hoạch refactor backend khi chuyển phase: `docs/execution/BACKEND-REFACTOR-001.md`
- Atlas/route visual history: `docs/execution/ATLAS-001.md` (historical, không còn là current authority)
- Thiết kế 28 màn: `docs/design/atlas/` (read-only)
- Nguồn nghiệp vụ: `LVTN/NguyenAnNam_DH52201078.docx`
- Đề cương học và luyện phản biện: `docs/GTAS-VPP-DEFENSE-GUIDE.md`

> Trạng thái: đã đồng bộ với implementation Blazor/Radzen và checkpoint frontend refactor đến
> FR8A/FR8B cùng settlement mutation E2E ngày 2026-08-04. FR8C đã khóa ledger máy đọc được đủ 44 route
> và hòa giải các bảng tài liệu lịch sử; History full test đã pass `3/3` sau bounded chart retry và
> render-settle hardening; owner đã chấp thuận current runtime + final board ngày 2026-08-04;
> backend reading checkpoint đã đồng bộ qua B1a-2b1 request-service detach; các mục không có số hình là
> route/state thật nhưng chưa được luận văn gán hình riêng.

Ledger máy đọc được cho toàn bộ 44 key nằm tại
`tests/Frontend.UnitTests/Architecture/RouteAcceptanceManifest.cs`; test khóa parity với
`Helpers/RouteCatalog.cs`. Classification trong manifest là bằng chứng kỹ thuật (`Tested`, `Redirect`,
`DynamicSample`, `JustifiedEquivalent`), không thay cho owner visual acceptance.

---

## 1. Luồng chính của một màn hình

Khi bị hỏi "chỗ này code ở đâu", lần theo các lớp chính theo thứ tự này:

```
Trình duyệt
  └─ Blazor component (.razor)        ← giao diện + điều phối trạng thái màn hình
       └─ typed feature client/state   ← API endpoint + request/query mapping
            └─ HttpClient/transport
                 └─ Controller (.cs)  ← kiểm tra quyền, nhận/trả DTO
                      └─ Service (.cs)  ← luật nghiệp vụ thật sự nằm ở đây
                           └─ EF Core → SQL Server
```

Ở frontend, `Program.cs` chỉ là composition outline. Registration, pipeline và endpoint map nằm ở
`Platform/Composition/`; state dùng chung cross-feature nằm ở `Platform/State/`; download API → browser
chỉ có một owner ở `Platform/Browser/`; vô hiệu hóa session sau phản hồi 401 nằm ở `Platform/Auth/`.
Các API client/state đã refactor nằm dưới `Features/<Feature>/` để người đọc lần theo route → feature →
Shared DTO. Identity/profile/permission state hiện nằm trong `Features/IdentityAccess/State/`. `Services/`
vẫn còn transport dùng chung và residual có owner riêng như `PermissionRealtimeService`; các file đó chỉ
chuyển trong slice lifecycle phù hợp, không mass-move chỉ để đồng đều tên thư mục.

Ở backend Requests, `VPPRequestController` gọi `IVPPRequestService`; implementation
`VPPRequestService` hiện chỉ nhận `IUnitOfWork`, clock, configuration và các policy/service tùy chọn thực
sự dùng. Service không còn kế thừa `BaseServices`, nên mỗi activation không còn tạo một UnitOfWork phụ.
Legacy BaseServices/factory/resolver/Jira chain đã được xóa tại B1a-2b2; đường đọc code nghiệp vụ đơn giờ
đi thẳng qua injected `IUnitOfWork`, không còn base infrastructure trung gian.

Điểm hay bị hỏi khi bảo vệ: **ẩn nút trên giao diện không phải là phân quyền**. Giao diện chỉ ẩn cho
gọn mắt; quyền thật được kiểm ở từng action của controller bằng `[Authorize(Policy = ...)]`. Xem luận
văn §2.3.1.1, câu cuối.

---

## 2. Bảng tra chính

`Hình` là số hình trong luận văn. `Board` là nhóm màn trong Atlas. Đường dẫn component tính từ
`src/Frontend/Blazor/Components/`.

> Bảng này phục vụ đọc code theo nghiệp vụ, không phải ledger exhaustive. Coverage và canonical path của
> mọi logical route/query variant chỉ xem ở
> `tests/Frontend.UnitTests/Architecture/RouteAcceptanceManifest.cs`.

### M0 — Nền tảng giao diện

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| — | `shell-system` | mọi route | `Layout/MainLayout.razor`, `Layout/LeftSidebar.razor`, `Layout/UserMenu.razor`, `Layout/NotificationCenter.razor` | — | — |

Các điểm cần biết sau đợt đồng bộ W-B.2 (2026-07-26):

- **Sidebar mặc định mở rộng trên desktop** và ghi nhớ lựa chọn qua
  `ProtectedLocalStorage["VPP_SidebarExpanded"]` (giống theme). Lần đầu vào app, JS
  `vppViewport.isDesktop` (trong `wwwroot/js/vpp-interactions.js`) quyết định mở hay thu gọn.
  Xem `LeftSidebar.razor.cs` → `LoadSidebarStateAsync` / `SetSidebarExpandedAsync`.
- **Role/group context**: role hiện hành nằm ở dòng phụ của sidebar do `LeftSidebar.razor.cs`/`UserMenu.razor`
  sở hữu; header không lặp role badge hoặc breadcrumb. Không suy luận role từ text UI — backend policy và
  `CanonicalRbac` mới là authorization authority.
- **Trạng thái dùng chung**: `DesignSystem/Primitives/VppContentState.razor` nhận
  `VppContentStateKind` typed cho loading/empty/filter-empty/error/denied/disabled/success/warning.
  Primitive tự gắn `role`, `aria-live` và `aria-busy` theo semantics; các adapter string cũ đã được xóa
  sau khi consumer về 0. `NotificationCenter.razor` đưa focus vào panel và trả focus về trigger khi đóng.
- **Icon điều hướng** đặt tên ngữ nghĩa trong `DesignSystem/Primitives/VppIcons.cs`; sibling tĩnh phải khác glyph
  (quyết định D13 — Atlas render động nên được phép trùng, Blazor thì không).
- **Token quan trọng** trong `wwwroot/css/vpp-tokens.css`: nút chuẩn `--vpp-control-height: 36px`,
  compact `30px`; màu semantic ở light mode dùng tông sâu đạt AA (khối `:root:not(.rz-theme-dark)`),
  dark mode giữ tông sáng; link dùng `--vpp-text-link` / `--vpp-text-link-hover` theo theme.

### M1 — Tài khoản và phiên làm việc

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-28 | `login` | `/Account/Login` | `Pages/Authen/LoginPage.razor` | `AuthController` | §2.3.1.1, §3.3.1.1 |
| — | `forgot-password` | `/Account/ForgotPassword` | `Pages/Authen/ForgotPassword.razor` | `AccountController` | — |
| — | `reset-password` | `/Account/ResetPassword` | `Pages/Authen/ResetPassword.razor` | `AccountController` | — |
| — | `change-password` | `/Account/ChangePassword` | `Pages/Authen/ChangePassword.razor` | `AccountController` | — |
| — | `logout` | `/logoutprocess` | `Pages/Authen/Logout.razor` | `AuthController` | — |
| — | `register` | `/Account/Register` | `Pages/Authen/Register.razor` | `AccountController` | — |

Toàn bộ account route dùng trực tiếp pattern `DesignSystem/Patterns/VppAccountWorkspace.razor`; adapter `VppAccountShell` đã được retire sau khi consumer về 0.

Hai primitive chỉ phục vụ account flow (`VppLanguageSwitch`, `VppPasswordField`) thuộc
`Features/IdentityAccess/Components/`, không nằm trong shared bucket.

State identity cũng có owner riêng trong `Features/IdentityAccess/State/`: `CurrentUserState` nạp/cache
profile `/me`, `PermissionState` giữ snapshot quyền và route fallback, còn `PermissionRefreshSignal` là
signal nội bộ khi transport gặp 403. Ba state này là scoped theo circuit; `AuthHelper` chỉ phối hợp xác thực,
không biến state UI thành authority phân quyền backend.

### M2 — Vòng đời đơn của nhân viên

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-29 | `my-orders` | `/dashboard?tab=0` | `Pages/VPPRequest/Tabs/Tab_Orders.razor`, `Pages/VPPRequest/Components/VppOrderWorkspacePanel.razor` | `GET /api/VPPRequest/my-orders`, `my-orders-summary`, `period-info`; `GET orders/{id}/export.pdf`, `orders/{id}/export.xlsx` (tải phiếu đơn, không chứa giá) | §2.3.1.2, §3.3.1.2 |
| 3-30 | `order-create` | `/dashboard/order-create` | `Pages/VPPRequest/Page_OrderCreate.razor`, `Pages/VPPRequest/OrderCreateStep2.razor`, `Pages/VPPRequest/OrderCreateStep3.razor` | `GET products`, `products/lookup`, `orders/previous-items`; `POST orders` | §2.3.1.2, §2.3.1.3, §3.3.2.1 |
| 3-31 | `history` | `/dashboard?tab=1` | `Pages/VPPRequest/Tabs/Tab_History.razor` (coordinator giữ state) + `Pages/VPPRequest/Components/HistoryWorkspaceShell.razor` và các component con `HistoryScopeBar`, `HistoryKpiCards`, `HistoryTrendChart`, `HistoryOrderList`, `HistoryOrderDetailSheet` | `GET my-order-history`, `my-order-history-summary`, `orders/{id}`, `orders/{id}/history` | §3.3.2.2 |
| 3-32 | `catalog` | `/dashboard?tab=2` | `Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor` | `GET /api/VPPRequest/products`, `categories` | §3.3.2.3 |

**Ràng buộc quyền quan trọng:** màn nhân viên không hiển thị đơn giá, thành tiền hay tạm tính. Đây là
ràng buộc nghiệp vụ, không phải lựa chọn thẩm mỹ — đừng "thêm cột giá cho đẹp".

### M3 — Tổng hợp quản lý

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-33 | `department-summary` | `/dashboard?tab=3&managementTab=department` | `Pages/VPPRequest/Tabs/Tab_DepartmentSummary.razor` + `Pages/VPPRequest/Components/HistoryWorkspaceShell.razor`/shared History components | `GET /api/VPPRequest/department-orders` | §3.3.3.1 |

### M4 — Vận hành kỳ

Luồng bốn bước: `Rà soát kỳ → Gom nhu cầu → Chọn nguồn cung → Chốt kỳ` (quyết định D4).

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-34 | `supplement-approval` | `/dashboard?tab=5&periodTab=pending` | `Pages/VPPRequest/Tabs/Tab_AdminApproval.razor` (coordinator), `Pages/VPPRequest/Components/PendingApprovalWorkspace.razor`, `Pages/VPPRequest/Components/Dialog_RejectSupplement.razor` | `GET additional-orders/pending`; `POST additional-orders/{id}/approve`, `/reject` | §2.3.1.4, §3.3.3.2 |
| 3-35 | `period-review` | `/dashboard?tab=5&periodTab=review` | `Pages/VPPRequest/Components/PeriodOperationsWorkspace.razor`, `Pages/VPPRequest/Components/PeriodSettlementPanel.razor` | `GET all-orders`; `GET /api/PeriodSettlement/{y}/{m}`; `POST preview/confirm` | §2.3.1.5, §3.3.3.3 |
| — | `period-demand` | `/dashboard?tab=5&periodTab=demand` | Legacy URL chuyển vào `Pages/VPPRequest/Components/PeriodSettlementPanel.razor`; dữ liệu gom dùng view `SettlementByDepartment` / `SettlementByItem` | `GET all-orders`; `GET period-demand` | §3.3.3.4 |
| 3-36 | `supply-allocation` | `/dashboard?tab=5&periodTab=supply` | Legacy URL chuyển vào supplier decision/dialog của `Pages/VPPRequest/Components/PeriodSettlementPanel.razor` | `POST /api/PeriodSettlement/preview` | §2.3.1.8, §3.3.3.4 |
| 3-37 | `settlement-flow` | `/dashboard?tab=5&periodTab=settle` | `Pages/VPPRequest/Components/PeriodSettlementPanel.razor` | `POST preview`, `confirm`, `{id}/correct`; `GET current/{y}/{m}`, `revisions/{y}/{m}` | §2.3.1.8, §2.3.1.9, §3.3.3.5 |

### M5A + M5B — Thư viện dữ liệu

Tất cả nằm ở `/library?tab=N`. Mỗi nghiệp vụ dùng tab typed riêng và cùng composition
`VppCollectionWorkspace`/`VppSplitEditorWorkspace` + `VppDataSurfaceFrame`; generic reflection grid và
record inspector cũ đã được xóa để tránh chồng CRUD, CSS và permission contract.

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| — | `classes` | `/library?tab=0` | `Pages/Lib/Tabs/Tab_LookupLibrary.razor` | `LibraryController /{tableCode}` | §2.3.1.6 |
| — | `categories` | `/library?tab=1` | `Pages/Lib/Tabs/Tab_CategoryLibrary.razor`, `Dialog_CategoryEditor.razor` | `LibraryController /{tableCode}` | §2.3.1.6 |
| 3-38 | `items` | `/library?tab=2` | `Pages/Lib/Tabs/Tab_ItemLibrary.razor`, `Dialog_ItemEditor.razor` | `VppCatalogController /items` | §2.3.1.6, §3.3.4.1 |
| — | `suppliers` | `/library?tab=3` | `Pages/Lib/Tabs/Tab_SupplierLibrary.razor`, `Dialog_SupplierEditor.razor` | `LibraryController /{tableCode}` | §2.3.1.7 |
| — | `departments` | `/library?tab=5` | `Pages/Lib/Tabs/Tab_DepartmentLibrary.razor`, `Dialog_DepartmentEditor.razor` | `LibraryController /{tableCode}` | — |
| 3-39 | `price-lists` | `/library?tab=6&pricingTab=price-lists` | `Pages/Lib/Tabs/Tab_PriceListLibrary.razor`, `Dialog_PriceListEditor.razor` | `VPPPriceListController` + `publish`, `expire`, `clone`, `compare` | §2.3.1.7, §3.3.4.2 |
| — | `prices` | `/library?tab=6&pricingTab=prices` | `Pages/Lib/Tabs/Tab_PriceLibrary.razor`, `Dialog_PriceEditor.razor` | `VPPPriceController` + `resolve`, `item-prices` | §2.3.1.7 |

**Hai loại "trạng thái" khác nhau, rất hay nhầm:**

- Hầu hết bảng: trạng thái chỉ là xóa mềm — `Hoạt động` (`IsDeleted = false`) hoặc `Ngừng áp dụng`
  (`IsDeleted = true`).
- Riêng bảng giá có vòng đời thật: `Draft → Published → Expired` (§2.3.1.7).

### M6 — Người dùng và phân quyền

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-40 | `users` | `/permission` | `Pages/Permission/Tabs/Tab_User.razor` | `GET users` (search + `accountStatus`), `groups`; `POST admin/activate`, `admin/reset-password`; `PUT memberships`; `POST memberships/deactivate` | §3.3.4.3 |
| 3-41 | `permissions` | `/permission` | `Pages/Permission/Tabs/Tab_PagePermission.razor` | `GET groups`, `groups/{id}/page-components`; `PUT groups/{id}` | §3.3.4.4 |

`Tab_User` hỗ trợ lời mời passwordless, gán nhóm quyền/phòng ban bằng dropdown inline
(`ApplyInlineMembershipAsync`), kích hoạt, gửi link đặt lại mật khẩu và vô hiệu hóa membership. Dialog typed
chỉ dùng cho lời mời; UI chỉ nhận DTO quản trị an toàn; `SessionVersion`, password
hash, security stamp và token không được render hoặc đưa vào form.

`Tab_PagePermission` có hai lớp cố ý tách biệt:

- **Ma trận action 18×3:** đọc trực tiếp `CanonicalRbac.Actions/Personas/HasAction`, không có nút lưu.
- **Ánh xạ UI:** dữ liệu từ `GET groups/{id}/page-components`; chỉ component UI có `CanConfigure`
  mới được bật/tắt. `GroupCode` lấy từ DTO backend, tuyệt đối không suy ra từ tên vai trò đã dịch.

### M7 — Báo cáo

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-42 | `reports` | `/report` | `Pages/Report.razor` | `GET /api/Reports/summary`, `insights`, `export.pdf`, `export.xlsx`, `export.csv` | §3.3.5.1, §3.4 |

**Report hỗ trợ PDF, XLSX và CSV**; format được map qua `VppFileExportFormat`/`ReportsApiClient`, không
suy luận từ extension rải trong page.

Luồng backend hiện tại:

```text
GET /api/reports/*
  → Api/Features/Reports/ReportsController
  → ReportQueryContext (scope lấy từ claims đã xác thực)
  → ReportService
      → dữ liệu đơn đang hoạt động từ VPPContext
      → CurrentSettlementReportReader chỉ đọc snapshot hiện hành khi đã chốt kỳ
      → ReportCsvBuilder / ReportPdfBuilder / ReportWorkbookBuilder
```

`AddReportsModule` là owner đăng ký DI cho query, settlement reader và insight providers. Reader settlement
chỉ là compatibility boundary của Reports; nó không được xác nhận/chỉnh sửa/chuyển trạng thái kỳ.

Các primitive tạo file dùng chung nằm ở `Application/Platform/Files`:
`ExportFileContract`, `SimpleWorkbookBuilder`, `VppPdfFontRegistry` và `VppPdfTheme`. Builder theo nghiệp vụ
vẫn ở module gần consumer (`Order*`, `Report*`, `Settlement*`); không tạo một generic export service mới.

Adapter HTTP dùng chung nằm ở `Api/Platform/Middleware`; factory chỉ phục vụ lệnh EF design-time nằm ở
`Api/Platform/DatabaseInitialization`. Namespace công khai được giữ nguyên để pipeline và test không đổi.

`Report.razor` dùng cùng `scope/year/month` cho summary và ba export. Search phòng ban chỉ lọc
client-side `DepartmentBreakdown`; bảng chỉ hiển thị field DTO thật. Trend bind `TotalAmount`. Khi
`SettlementId` có giá trị, số liệu và bằng chứng hiển thị là snapshot lúc chốt kỳ, không tính lại. Trend
chỉ render khi có ít nhất 2 điểm (smooth từ 3 điểm); donut bỏ giá trị 0. Thiếu dữ liệu dùng empty state,
không cố render SVG suy biến.

### M8 — Trạng thái hệ thống

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-43 | `system-states` | mọi route | `DesignSystem/Primitives/VppContentState.razor`, `Layout/NotificationCenter.razor`, `Layout/ReconnectModal.razor`, `DesignSystem/Primitives/SkeletonGrid.razor` | `NotificationsController` | §3.3.5.2 |

`Helpers/RouteCatalog.cs` là danh sách route/state dùng cho shell và test contract; năm state vận hành kỳ
`pending/review/demand/supply/settle` cùng các route account anonymous đều được khai báo rõ. Với DataGrid đã
audit, component gắn `data-vpp-grid-region="true"`; `wwwroot/js/vpp-interactions.js` chuẩn hóa role của
wrapper/table, vùng cuộn keyboard-focus và `aria-disabled` do Radzen 11.1.4 sinh ra.

Backend Notifications được chia theo trách nhiệm:

- `Application/Services/Notifications`: inbox, publish, permission recipients, email outbox và SMTP contract;
- `Api/Notifications`: SignalR hub/realtime adapter và `AddNotificationsModule`/`MapNotificationsModule`;
- `Api/Features/Notifications/NotificationsController`: HTTP transport.

Các module Request, Settlement và Identity vẫn gọi chung `IAppNotificationService`; controller nghiệp vụ
không biết SignalR hay cách lưu outbox.

**Evidence index hiện tại:** `RouteAcceptanceManifest` giữ 44 logical route/query key; frontend unit/architecture
`373/373` và checkpoint verify gần nhất pass build/unit/UI smoke. E2E tải thật cover report PDF/XLSX/CSV và
order PDF/XLSX; settlement mutation cô lập chứng minh revision 1 bất biến, cùng người bị four-eyes từ chối
và người thứ hai tạo correction revision 2. History full test pass `3/3` sau bounded chart retry và double-rAF
render settle khi đóng transient detail popover. Owner visual approval đã hoàn tất; backend refactor và việc
khóa ảnh runtime vào thesis/slide vẫn là hai checkpoint riêng.

### Feature owner quick map

Để lần theo code khi trình bày, route/page chỉ là coordinator; API, state và mutation owner nằm ở feature:

| Flow | API/state owner chính |
|---|---|
| My Orders / History / Catalog | `Features/Requests/Api/RequestsQueryClient`, `Features/Requests/{Editor,Drafts,Submission}`, `Pages/VPPRequest/Components/HistoryWorkspaceShell` |
| Order create/edit | `Features/Requests/Drafts/OrderDraftStore`, `Features/Requests/Api/RequestsCommandClient`, `Features/Requests/Submission/OrderSubmissionCoordinator` |
| Period / Settlement | `Features/Settlement/Api/SettlementApiClient`, `Features/Settlement/State/PeriodSettlementState`, `Features/Settlement/Submission/SettlementRequestFactory` |
| Library / Pricing | typed clients dưới `Features/CatalogPricing/Api/` và state/query gần từng tab |
| Users / Permissions | `Features/IdentityAccess/Api/` và `Features/IdentityAccess/State/` |
| Reports / download | `Features/Reports/Api/ReportsApiClient` và `Platform/Browser/` |

---

## 3. Năm luồng hay bị hỏi khi bảo vệ

### 3.1 Tạo đơn thông thường — §2.3.1.2

```
Nhân viên mở /dashboard/order-create
  → hệ thống xác định kỳ đang nhận đơn (period-info) và kiểm tra hạn gửi
  → chọn mặt hàng, nhập số lượng + ghi chú (hoặc sao chép từ kỳ trước)
  → kiểm tra: ít nhất 1 dòng, số lượng dương, không trùng mặt hàng, mặt hàng còn hoạt động
  → POST orders: tạo đơn + chi tiết + phiên bản hiện hành + nhật ký trong MỘT transaction
```

Câu hỏi bẫy: *"Hai người bấm gửi cùng lúc thì sao?"* — một ràng buộc duy nhất ở tầng database chặn
việc tạo hai đơn thường trùng trong cùng kỳ. Không dựa vào kiểm tra ở tầng ứng dụng.

### 3.2 Đơn bổ sung — §2.3.1.4

```
Nhân viên tạo đơn bổ sung (bắt buộc nêu lý do; không bắt buộc có đơn thường)
  → chỉ được tạo trong cửa sổ nghiệp vụ cho phép
  → quota/lượt thử/one-Pending tính chung theo người dùng + kỳ
  → nếu có đơn thường hợp lệ, backend lưu BaseRequestId để truy vết; nếu không thì để null
  → Quản lý mở hàng chờ, xem chi tiết, rồi duyệt hoặc từ chối
  → từ chối bắt buộc nhập lý do
  → lưu quyết định + lý do + người xử lý + thời điểm vào đơn và nhật ký; người tạo nhận thông báo
```

Hai từ dễ nhầm:

- `standalone supplement`: đơn bổ sung độc lập, không có `BaseRequestId`;
- `lineage`: quan hệ truy vết nguồn gốc. Ở đây base chỉ giúp biết đơn bổ sung liên
  quan đơn thường nào khi quan hệ đó tồn tại, không còn là điều kiện được phép tạo.

Luật hiện hành nằm trong ADR-014. Database có hai filtered unique index để chặn
đồng thời: tối đa một `Pending` theo user/kỳ và không trùng
`SupplementAttemptNumber` theo user/kỳ.

Quyền cần: `REQUEST_APPROVE` / `REQUEST_REJECT` (định nghĩa ở
`src/Shared/Constants/Permissions.cs`, gán vai trò ở `CanonicalRbac.cs`). Người dùng
**không tự duyệt đơn của chính mình**.

### 3.3 Chốt kỳ — §2.3.1.8

```
Quản lý chọn nhà cung cấp chính → chọn bảng giá còn hiệu lực
  → POST preview: xem độ phủ giá, đơn bổ sung chờ xử lý, mặt hàng thiếu giá, ngoại lệ, tổng tiền
  → POST confirm: server tính LẠI hash đầu vào để chặn dữ liệu cũ
  → tạo phiên bản kết quả chốt kỳ BẤT BIẾN (dòng giá + phí + phân bổ)
  → kỳ chuyển sang Settled
```

Hai cơ chế đáng nói:

- **`InputHash`** — nếu dữ liệu đã đổi kể từ lúc xem trước, xác nhận sẽ bị chặn thay vì chốt nhầm.
- **Idempotency key** — bấm xác nhận hai lần trả về cùng một kết quả, không tạo hai bản chốt.

### 3.4 Hiệu chỉnh kết quả chốt kỳ — §2.3.1.9

```
Yêu cầu nhập lý do
  → NGUYÊN TẮC BỐN MẮT: người xác nhận hiệu chỉnh không được là người tạo phiên bản đang hiện hành
  → tạo phiên bản MỚI, đánh dấu phiên bản cũ không còn hiện hành
  → snapshot cũ được giữ nguyên để đối chiếu và audit
```

Không bao giờ ghi đè hay xóa bản chốt cũ.

### 3.5 Phân quyền — §3.3.4.4

Đúng **ba vai trò** (quyết định D1, xem `src/Shared/Constants/CanonicalRbac.cs`):

| Mã | Hiển thị | Phạm vi |
|---|---|---|
| `EMPLOYEE` | Nhân viên | Đơn của chính mình |
| `MANAGER` | Quản lý | Kế thừa Nhân viên + quản lý dữ liệu, duyệt đơn bổ sung, vận hành kỳ |
| `DEV` | Quản trị hệ thống | Toàn quyền, phục vụ phát triển và kiểm thử; vẫn phải tuân điều kiện nghiệp vụ |

Mỗi tài khoản chỉ có **một** phân công quyền đang hiệu lực.

---

## 4. Từ điển thuật ngữ

Code viết tiếng Anh, luận văn viết tiếng Việt. Bảng này nối hai bên.

| Trong code | Trong luận văn | Ghi chú |
|---|---|---|
| `Period` | Kỳ đặt hàng | Bốn trạng thái: `Open`, `SubmissionClosed`, `Pricing`, `Settled` — xem `src/Backend/Domain/VPP/VppPeriodState.cs` |
| `Request` / `Order` | Đơn yêu cầu | `VppRequest` là tên thực thể |
| `Additional` / `Supplement` | Đơn bổ sung | Cần lý do và được duyệt riêng |
| `Revision` | Phiên bản | Sửa đơn tạo phiên bản mới, không ghi đè |
| `Settlement` | Chốt kỳ | Kết quả bất biến tại thời điểm chốt |
| `Correction` | Hiệu chỉnh | Tạo phiên bản chốt kỳ mới, cần bốn mắt |
| `Preview` | Xem trước | Chưa ghi gì vào database |
| `RowVersion` | Cơ chế chống ghi đè | Phát hiện hai người sửa cùng lúc |
| `IsDeleted` | Xóa mềm | Không xóa vật lý, giữ lịch sử đơn và báo cáo |
| `PriceBook` / `PriceList` | Bảng giá | `Draft / Published / Expired` |
| `Coverage` | Độ phủ giá | Bao nhiêu mặt hàng có giá từ nguồn chính |
| `Exception` | Ngoại lệ nguồn cung | Mặt hàng phải lấy nhà cung cấp khác, bắt buộc nhập lý do |
| `Blocker` | Điều kiện ngăn chốt | Phải xử lý xong mới chốt kỳ được |
| `Snapshot` | Dữ liệu lưu tại thời điểm | Không đổi theo danh mục về sau |
| `Claims` | Quyền của phiên đăng nhập | Nạp sau khi đăng nhập |
| `Scope` | Phạm vi dữ liệu | Cá nhân / phòng ban / toàn công ty |

---

## 5. Vì sao chỗ này phân trang, chỗ kia cuộn?

Câu này rất dễ bị hỏi. Quy tắc chọn theo **ý định của người dùng**, không theo số lượng dòng:

| Kiểu | Dùng khi | Ví dụ |
|---|---|---|
| **Server paging** | Người dùng đang *tra cứu*, chọn một bản ghi rồi mở chi tiết | Danh mục, danh sách lịch sử, hàng chờ duyệt, thư viện, người dùng, báo cáo |
| **Cuộn + ảo hóa** | Người dùng đang *thao tác liên tục* trên nhiều dòng và cần giữ trạng thái đang làm | Mặt hàng trong đơn, chọn mặt hàng khi tạo đơn, chi tiết đơn, tổng nhu cầu, đối chiếu nguồn cung, ma trận quyền |

Hai quy tắc kèm theo:

1. Một bảng **không** vừa có phân trang vừa cuộn vô hạn. Màn master–detail được phép phân trang ở
   danh sách và ảo hóa riêng trong vùng chi tiết.
2. Tìm kiếm/lọc/sắp xếp chạy trên **toàn bộ** tập dữ liệu được cấp quyền, trước `Count` và
   `Skip/Take`. Lọc phải tìm được bản ghi nằm ngoài trang 1 — đây là lỗi phân trang phổ biến mà hệ
   thống này cố tình tránh.

Các quy tắc trên được khóa bằng architecture test trong `tests/Frontend.UnitTests/Architecture/`.

---

## 6. Quy ước comment

- Code, tên biến, tên hàm, tên file: **tiếng Anh 100%**.
- Comment tiếng Việt **chỉ** ở chỗ cần giải thích lý do nghiệp vụ, security boundary, concurrency,
  compatibility hoặc recovery mà tên code chưa thể hiện đủ:

```csharp
// Không cho người tạo tự xác nhận hiệu chỉnh để giữ nguyên tắc bốn mắt.
if (currentRevision.CreatedBy == actingUserId)
```

- Không mặc định gắn số mục luận văn vào source. Mapping giữa code và luận văn được giữ trong sổ tay
  này để code production không mang dấu vết học thuật dễ stale hoặc quá lộ liễu.
- Không comment những dòng tự hiển nhiên. Comment giải thích **vì sao**, không mô tả lại **cái gì**.
- Giải thích dài đưa vào guide/ADR; XML documentation chỉ dùng cho public surface hoặc domain rule
  thật sự khó hiểu, không tạo boilerplate cho mọi getter/setter.
