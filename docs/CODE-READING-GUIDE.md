# Sổ tay đọc code GTAS VPP

Tài liệu này để **đọc và trình bày code**, không phải để thiết kế. Mở luận văn ra, thấy một hình
giao diện, tra bảng ở mục 2 là biết ngay màn đó nằm ở file nào, gọi API nào và bị ràng buộc bởi quy
tắc nghiệp vụ nào.

- Quy tắc làm việc: `AGENTS.md`
- Kế hoạch triển khai: `docs/execution/ATLAS-001.md`
- Thiết kế 28 màn: `docs/design/atlas/` (read-only)
- Nguồn nghiệp vụ: `LVTN/NguyenAnNam_DH52201078.docx`

> Trạng thái: đã đồng bộ với implementation ATLAS-001 và các checkpoint frontend refactor đến
> các slice FR8A/FR8B ngày 2026-08-03. Các mục không có số hình vẫn là route/state thật nhưng chưa được luận văn
> gán hình riêng.

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
chỉ có một owner ở `Platform/Browser/`. Các API client/state đã refactor nằm dưới `Features/<Feature>/`
để người đọc lần theo route → feature → Shared DTO. Identity/profile/permission state hiện nằm trong
`Features/IdentityAccess/State/`. `Services/` vẫn còn transport dùng chung và residual có owner riêng như
`PermissionRealtimeService`; các file đó chỉ chuyển trong slice lifecycle phù hợp, không mass-move chỉ để
đồng đều tên thư mục.

Điểm hay bị hỏi khi bảo vệ: **ẩn nút trên giao diện không phải là phân quyền**. Giao diện chỉ ẩn cho
gọn mắt; quyền thật được kiểm ở từng action của controller bằng `[Authorize(Policy = ...)]`. Xem luận
văn §2.3.1.1, câu cuối.

---

## 2. Bảng tra chính

`Hình` là số hình trong luận văn. `Board` là nhóm màn trong Atlas. Đường dẫn component tính từ
`src/Frontend/Blazor/Components/`.

### M0 — Nền tảng giao diện

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| — | `shell-system` | mọi route | `Layout/MainLayout.razor`, `Layout/LeftSidebar.razor`, `Layout/UserMenu.razor`, `Layout/NotificationCenter.razor` | — | — |

Các điểm cần biết sau đợt đồng bộ W-B.2 (2026-07-26):

- **Sidebar mặc định mở rộng trên desktop** và ghi nhớ lựa chọn qua
  `ProtectedLocalStorage["VPP_SidebarExpanded"]` (giống theme). Lần đầu vào app, JS
  `vppViewport.isDesktop` (trong `wwwroot/js/vpp-interactions.js`) quyết định mở hay thu gọn.
  Xem `LeftSidebar.razor.cs` → `LoadSidebarStateAsync` / `SetSidebarExpandedAsync`.
- **Role badge + breadcrumb trong header**: `LeftSidebar.razor.cs` → `RoleBadgeLabel` map
  `CurrentUserState.Current?.GroupId` sang 3 persona của `CanonicalRbac` rồi qua `Loc["RoleEmployee|RoleManager|RoleDev"]`
  (không in raw GroupName `"DEV"`); `HeaderPathSegments` dựng đường dẫn `cha › con` từ URL.
  W-B.2b đã hoàn tất: desktop dùng cùng hàng header 72px cho breadcrumb/tab chrome.
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
| 3-29 | `my-orders` | `/dashboard` | `Pages/VPPRequest/Tabs/Tab_Orders.razor`, `Components/VppOrderWorkspacePanel.razor` | `GET /api/VPPRequest/my-orders`, `my-orders-summary`, `period-info`; `GET orders/{id}/export.pdf`, `orders/{id}/export.xlsx` (tải phiếu đơn, không chứa giá) | §2.3.1.2, §3.3.1.2 |
| 3-30 | `order-create` | `/dashboard/order-create` | `Pages/VPPRequest/Page_OrderCreate.razor`, `OrderCreateStep2.razor`, `OrderCreateStep3.razor` | `GET products`, `products/lookup`, `orders/previous-items`; `POST orders` | §2.3.1.2, §2.3.1.3, §3.3.2.1 |
| 3-31 | `history` | `/dashboard` tab Lịch sử | `Pages/VPPRequest/Tabs/Tab_History.razor` (coordinator giữ state) + 5 component con presentational trong `Components/`: `HistoryScopeBar`, `HistoryKpiCards`, `HistoryTrendChart`, `HistoryOrderList`, `HistoryOrderDetailSheet` | `GET my-order-history`, `my-order-history-summary`, `orders/{id}`, `orders/{id}/history` | §3.3.2.2 |
| 3-32 | `catalog` | `/dashboard` tab Danh mục | `Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor` | `GET /api/VPPRequest/products`, `categories` | §3.3.2.3 |

**Ràng buộc quyền quan trọng:** màn nhân viên không hiển thị đơn giá, thành tiền hay tạm tính. Đây là
ràng buộc nghiệp vụ, không phải lựa chọn thẩm mỹ — đừng "thêm cột giá cho đẹp".

### M3 — Tổng hợp quản lý

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-33 | `department-summary` | `/dashboard` tab Quản lý | `Pages/VPPRequest/Tabs/Tab_DepartmentSummary.razor` | `GET /api/VPPRequest/department-orders` | §3.3.3.1 |

### M4 — Vận hành kỳ

Luồng bốn bước: `Rà soát kỳ → Gom nhu cầu → Chọn nguồn cung → Chốt kỳ` (quyết định D4).

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-34 | `supplement-approval` | `/dashboard?tab=5&periodTab=pending` | `Tabs/Tab_AdminApproval.razor` (coordinator), `Components/PendingApprovalWorkspace.razor`, `Dialog_RejectSupplement.razor` | `GET additional-orders/pending`; `POST additional-orders/{id}/approve`, `/reject` | §2.3.1.4, §3.3.3.2 |
| 3-35 | `period-review` | `/dashboard?tab=5&periodTab=review` | `Components/PeriodOperationsWorkspace.razor`, `PeriodSettlementPanel.razor` | `GET all-orders`; `GET /api/PeriodSettlement/{y}/{m}`; `POST preview/confirm` | §2.3.1.5, §3.3.3.3 |
| — | `period-demand` | `/dashboard?tab=5&periodTab=demand` | Legacy URL chuyển vào `PeriodSettlementPanel.razor`; dữ liệu gom được thể hiện qua selector `Theo đơn / Theo phòng ban` | `GET all-orders`; `GET period-demand` | §3.3.3.4 |
| 3-36 | `supply-allocation` | `/dashboard?tab=5&periodTab=supply` | Legacy URL chuyển vào supplier decision/dialog của `PeriodSettlementPanel.razor` | `POST /api/PeriodSettlement/preview` | §2.3.1.8, §3.3.3.4 |
| 3-37 | `settlement-flow` | `/dashboard?tab=5&periodTab=review` | `Components/PeriodSettlementPanel.razor` | `POST preview`, `confirm`, `{id}/correct`; `GET current/{y}/{m}`, `revisions/{y}/{m}` | §2.3.1.8, §2.3.1.9, §3.3.3.5 |

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

`Tab_User` hỗ trợ lời mời passwordless, gán nhóm quyền/phòng ban, kích hoạt, gửi link đặt lại mật khẩu
và vô hiệu hóa membership qua dialog typed. UI chỉ nhận DTO quản trị an toàn; `SessionVersion`, password
hash, security stamp và token không được render hoặc đưa vào form.

`Tab_PagePermission` có hai lớp cố ý tách biệt:

- **Ma trận action 18×3:** đọc trực tiếp `CanonicalRbac.Actions/Personas/HasAction`, không có nút lưu.
- **Ánh xạ UI:** dữ liệu từ `GET groups/{id}/page-components`; chỉ component UI có `CanConfigure`
  mới được bật/tắt. `GroupCode` lấy từ DTO backend, tuyệt đối không suy ra từ tên vai trò đã dịch.

### M7 — Báo cáo

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-42 | `reports` | `/report` | `Pages/Report.razor` | `GET /api/Reports/summary`, `insights`, `export` (CSV), `export.xlsx` | §3.3.5.1, §3.4 |

**Chỉ có CSV và XLSX. Không có PDF** (quyết định D3).

`Report.razor` dùng cùng `scope/year/month` cho summary và hai export. Search phòng ban chỉ lọc
client-side `DepartmentBreakdown`; bảng chỉ hiển thị field DTO thật. Trend bind `TotalAmount`. Khi
`SettlementId` có giá trị, số liệu và bằng chứng hiển thị là snapshot lúc chốt kỳ, không tính lại. Trend
chỉ render khi có ít nhất 2 điểm (smooth từ 3 điểm); donut bỏ giá trị 0. Thiếu dữ liệu dùng empty state,
không cố render SVG suy biến.

### M8 — Trạng thái hệ thống

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-43 | `system-states` | mọi route | `DesignSystem/Primitives/VppContentState.razor`, `Layout/NotificationCenter.razor`, `Layout/ReconnectModal.razor`, `DesignSystem/Primitives/SkeletonGrid.razor` | `NotificationsController` | §3.3.5.2 |

`Helpers/RouteCatalog.cs` là danh sách route/state dùng cho shell và test contract; bốn state vận hành kỳ
`pending/review/demand/supply` cùng các route account anonymous đều được khai báo rõ. Với DataGrid đã
audit, component gắn `data-vpp-grid-region="true"`; `wwwroot/js/vpp-interactions.js` chuẩn hóa role của
wrapper/table, vùng cuộn keyboard-focus và `aria-disabled` do Radzen 11.1.4 sinh ra.

**Evidence hiện tại:** Release build sạch; frontend unit/architecture `366/366`; 28 screen × 4 viewport
runtime pass, representative Dark/Print/axe pass, Atlas export/account/user-menu smoke và real-file
download gates pass. Backend gate và owner visual approval vẫn là checkpoint riêng; ảnh runtime chỉ khóa
vào thesis/slide sau khi owner chấp thuận UI cuối.
E2E tải thật report CSV/XLSX và order PDF/XLSX; mutation cô lập pass permission toggle, vòng đời đơn
thường và duyệt/từ chối đơn bổ sung.

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
Nhân viên tạo đơn bổ sung (bắt buộc nêu lý do, gắn đơn gốc)
  → chỉ được tạo trong cửa sổ nghiệp vụ cho phép
  → không vượt giới hạn số lần, không có sẵn một đơn đang chờ xử lý
  → Quản lý mở hàng chờ, xem chi tiết, rồi duyệt hoặc từ chối
  → từ chối bắt buộc nhập lý do
  → lưu quyết định + lý do + người xử lý + thời điểm vào đơn và nhật ký; người tạo nhận thông báo
```

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
