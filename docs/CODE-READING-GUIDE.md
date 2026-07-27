# Sổ tay đọc code GTAS VPP

Tài liệu này để **đọc và trình bày code**, không phải để thiết kế. Mở luận văn ra, thấy một hình
giao diện, tra bảng ở mục 2 là biết ngay màn đó nằm ở file nào, gọi API nào và bị ràng buộc bởi quy
tắc nghiệp vụ nào.

- Quy tắc làm việc: `AGENTS.md`
- Kế hoạch triển khai: `docs/execution/ATLAS-001.md`
- Thiết kế 28 màn: `docs/design/atlas/` (read-only)
- Nguồn nghiệp vụ: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_v4_standard.docx`

> Trạng thái: đang xây dựng cùng với các wave W-A…W-H. Cột nào còn trống nghĩa là wave tương ứng
> chưa chạy. Cập nhật file này **cùng lần** với mỗi wave, không để dồn.

---

## 1. Ba tầng của một màn hình

Mọi màn trong hệ thống đều đi qua đúng ba tầng. Khi bị hỏi "chỗ này code ở đâu", trả lời theo thứ tự này:

```
Trình duyệt
  └─ Blazor component (.razor)        ← giao diện + trạng thái màn hình
       └─ HttpClient gọi REST API
            └─ Controller (.cs)       ← kiểm tra quyền, nhận/trả DTO
                 └─ Service (.cs)     ← luật nghiệp vụ thật sự nằm ở đây
                      └─ EF Core → SQL Server
```

Điểm hay bị hỏi khi bảo vệ: **ẩn nút trên giao diện không phải là phân quyền**. Giao diện chỉ ẩn cho
gọn mắt; quyền thật được kiểm ở từng action của controller bằng `[Authorize(Policy = ...)]`. Xem luận
văn §2.3.1.1, câu cuối.

---

## 2. Bảng tra chính

`Hình` là số hình trong luận văn. `Board` là nhóm màn trong Atlas. Đường dẫn component tính từ
`gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/`.

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
  `glb.UserInfo.GroupId` sang 3 persona của `CanonicalRbac` rồi qua `Loc["RoleEmployee|RoleManager|RoleDev"]`
  (không in raw GroupName `"DEV"`); `HeaderPathSegments` dựng đường dẫn `cha › con` từ URL.
  Desktop tạm ẩn header tới W-B.2b (xem quyết định D14 trong ATLAS-001).
- **Trạng thái dùng chung**: `Shared/VppStatePanel.razor` có 4 state `empty | loading | error | denied`.
  `denied` là trạng thái trung tính (icon primary, không viền đỏ) — thiếu quyền KHÔNG phải sự cố hệ thống.
  Nút thử lại của `error` tự động là primary; mọi màn thiếu quyền có lối thoát `Loc["BackToAllowedPage"]`.
- **Icon điều hướng** đặt tên ngữ nghĩa trong `Shared/VppIcons.cs`; sibling tĩnh phải khác glyph
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

Toàn bộ sáu màn dùng chung khung `Shared/VppAccountShell.razor`.

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
| 3-34 | `supplement-approval` | `/dashboard` tab Vận hành kỳ | `Pages/VPPRequest/Tabs/Tab_AdminApproval.razor`, `Components/Dialog_RejectSupplement.razor` | `GET additional-orders/pending`; `POST additional-orders/{id}/approve`, `/reject` | §2.3.1.4, §3.3.3.2 |
| 3-35 | `period-review` | `/dashboard` tab Vận hành kỳ | `Pages/VPPRequest/Components/PeriodReviewPanel.razor` | `GET /api/PeriodSettlement/{y}/{m}` | §2.3.1.5, §3.3.3.3 |
| — | `period-demand` | *(W-D)* | *(W-D)* — hấp thụ `Tab_AllOrdersSummary.razor` làm chế độ `Theo đơn` | `GET all-orders`; `GET period-demand` *(D7, W-D)* | §3.3.3.4 |
| 3-36 | `supply-allocation` | *(W-D)* | *(W-D)* — tách khỏi `PeriodSettlementPanel.razor` | `POST /api/PeriodSettlement/preview` | §2.3.1.8, §3.3.3.4 |
| 3-37 | `settlement-flow` | `/dashboard` tab Vận hành kỳ | `Pages/VPPRequest/Components/PeriodSettlementPanel.razor` | `POST preview`, `confirm`, `{id}/correct`; `GET current/{y}/{m}`, `revisions/{y}/{m}` | §2.3.1.8, §2.3.1.9, §3.3.3.5 |

### M5A + M5B — Thư viện dữ liệu

Tất cả nằm ở `/library?tab=N`, dùng chung `Pages/Lib/Component_ShareGrid.razor` +
`Component_RecordInspector.razor`.

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| — | `classes` | `/library?tab=0` | `Pages/Lib/Tabs/Tab_LookupLibrary.razor` | `LibraryController /{tableCode}` | §2.3.1.6 |
| — | `categories` | `/library?tab=1` | `Component_ShareGrid` (`VppCategoryResDTO`) | `LibraryController /{tableCode}` | §2.3.1.6 |
| 3-38 | `items` | `/library?tab=2` | `Component_ShareGrid` (`VppItemResDTO`) | `VppCatalogController /items` | §2.3.1.6, §3.3.4.1 |
| — | `suppliers` | `/library?tab=3` | `Component_ShareGrid` (`SupplierResDTO`) | `LibraryController /{tableCode}` | §2.3.1.7 |
| — | `departments` | `/library?tab=5` | `Component_ShareGrid` | `LibraryController /{tableCode}` | — |
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

`Tab_User` không tạo/sửa danh tính tài khoản: vòng đời thật là người dùng tự đăng ký → quản trị chọn
nhóm quyền/phòng ban → kích hoạt. `Component_RecordInspector` chỉ hiển thị dữ liệu nghiệp vụ và audit;
`SessionVersion`, password hash, security stamp và token bị loại trước khi reflection phân nhóm tab.

### M7 — Báo cáo

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-42 | `reports` | `/report` | `Pages/Report.razor` | `GET /api/Reports/summary`, `insights`, `export` (CSV), `export.xlsx` | §3.3.5.1, §3.4 |

**Chỉ có CSV và XLSX. Không có PDF** (quyết định D3).

### M8 — Trạng thái hệ thống

| Hình | Atlas | Route | Component | API | Mục luận văn |
|---|---|---|---|---|---|
| 3-43 | `system-states` | mọi route | `Layout/NotificationCenter.razor`, `Layout/ReconnectModal.razor`, `Shared/VppStatePanel.razor`, `Shared/VppEmptyState.razor`, `Shared/SkeletonGrid.razor` | `NotificationsController` | §3.3.5.2 |

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
`gtas_vpp_be/gtas_vpp_shared/Constants/Permissions.cs`, gán vai trò ở `CanonicalRbac.cs`). Người dùng
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

Đúng **ba vai trò** (quyết định D1, xem `gtas_vpp_shared/Constants/CanonicalRbac.cs`):

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
| `Period` | Kỳ đặt hàng | Bốn trạng thái: `Open`, `SubmissionClosed`, `Pricing`, `Settled` — xem `gtas_vpp_be/gtas_vpp_be.Model/VPP/VppPeriodState.cs` |
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

Các quy tắc trên được khóa bằng architecture test trong `gtas_vpp_fe.Tests/Architecture/`.

---

## 6. Quy ước comment

- Code, tên biến, tên hàm, tên file: **tiếng Anh 100%**.
- Comment tiếng Việt **chỉ** ở chỗ luật nghiệp vụ không đoán được từ code, kèm số mục luận văn:

```csharp
// Quy tắc bốn mắt: người xác nhận hiệu chỉnh không được là người tạo
// phiên bản đang hiện hành. Xem luận văn §2.3.1.9.
if (currentRevision.CreatedBy == actingUserId)
```

- Không comment những dòng tự hiển nhiên. Comment giải thích **vì sao**, không mô tả lại **cái gì**.
