# UI-ADMIN-ACCESS-001 — Quản trị danh mục và phân quyền

> Trạng thái: **COMPLETED — AA0–AA7 + POST-AUDIT DATA LIFECYCLE VERIFIED**
>
> Authority cha: [`UI-SYSTEM-001`](./UI-SYSTEM-001.md) và [`UI-DATA-SURFACE-001`](./UI-DATA-SURFACE-001.md)
>
> Phạm vi hiện hành: `/library`, `/permission`, account administration, security audit và API hardening không đổi schema. Custom role/group inheritance được owner hoãn sang plan riêng.

## 1. Mục tiêu một ánh nhìn

- Dùng **full-width Collection grid** cho bảng phẳng để dành chiều ngang cho nhiều cột.
- Chỉ dùng **ListDetail** khi hai tập dữ liệu có quan hệ cha–con cần quan sát đồng thời.
- Phân biệt đúng cấp độ thao tác: **Thêm** là action của toàn collection nên nằm ở collection header; **Xem/Sửa/Xóa hoặc Khôi phục** là action của một bản ghi nên nằm trong cột **Thao tác**. Toolbar chỉ chứa query/display controls. Thêm/Sửa mở adaptive editor dialog theo độ phức tạp; không còn inline row edit hoặc dropdown sửa trực tiếp trong cell.
- Bảng chỉ hiện cột nghiệp vụ quan trọng; audit, localization và thông tin ít dùng nằm trong **Cột hiển thị** hoặc modal chi tiết.
- Filter/sort/paging chạy trên **toàn bộ dữ liệu server**, không chỉ trang hiện tại.
- Không tạo `UniversalAdminGrid<T>` hoặc form bằng reflection. Tái dùng frame/composite; mỗi domain có typed columns, typed editor và validation riêng.

### Khung tổng quát

```text
Header-tab
└─ Main content
   ├─ Collection header: Tên + tổng bản ghi | + Thêm
   ├─ Toolbar: Tìm kiếm | Bộ lọc... | Xóa lọc | Cột
   ├─ Header cột: ... | Thao tác
   ├─ Dòng dữ liệu 1..n
   └─ Footer: tổng kết | pager | page-size

Thêm / Sửa
└─ Adaptive editor dialog
   ├─ Header cố định: tiêu đề + trạng thái
   ├─ Nội dung form có nhóm rõ ràng, native scroll khi cần
   └─ Footer cố định: Hủy | Lưu
```

## 2. Authority dữ liệu đã đối chiếu

| Domain | Entity/quan hệ chính | API/DTO hiện hành | Hệ quả UI |
|---|---|---|---|
| Loại và giá trị danh mục | `LookupCategory 1-n LookupValue`; UOM là `LookupValue` được `VppItem` tham chiếu | `/api/Library/lookup-categories`, `/lookup-values` | Không vô hiệu hóa tùy tiện value đang được item sử dụng; loại và value cần xem cạnh nhau. |
| Danh mục và mặt hàng | `VppCategory 1-n VppItem`; item bắt buộc category + UOM; `VppCode` unique | `/api/Library/vpp-categories`, `/api/catalog/items` | Mặt hàng dùng typed editor; giá/NCC chỉ hiển thị tóm tắt, chỉnh tại Pricing. |
| Nhà cung cấp và bảng giá | `Supplier 1-n PriceList`; `PriceList 1-n SupplierProductMapping` | `/api/Library/suppliers`, `/api/VPPPriceList`, `/api/VPPPrice` | Bảng giá có lifecycle Draft/Published/Expired và optimistic concurrency; action vòng đời tách khỏi form edit. |
| Phòng ban | `Department` self parent-child; `Department 1-n UserGroupMembership`; code unique active | `/api/Library/departments` | Form phải chống chọn chính nó/con cháu làm parent; không vô hiệu hóa khi còn user hoặc child active. |
| Người dùng | `AppUser 1-1 active UserGroupMembership`; membership bắt buộc group + department; Identity token và email outbox đã có | `/api/Permission/users`, `/memberships`, Account lifecycle endpoints | Thêm admin-create dạng invitation; admin không nhập, nhìn thấy hoặc gửi mật khẩu tạm. |
| Phân quyền | Runtime hiện chỉ chấp nhận đúng ba canonical group; mapping dùng `IsVisible` + `IsEnable` | `/api/Permission/groups`, `/page-components`, `/component-mapping` | Current scope giữ canonical RBAC, đổi editor thành batch modal và một access-state selector dễ hiểu. |
| Audit bảo mật | `SecurityAudit` ghi registration, activation, membership, session và permission batch | `GET /api/Permission/security-audits`, `/filter-options`; `/permission?tab=2` | Màn immutable read-only, server filter/sort/paging trên toàn nguồn; không có Thêm/Sửa/Xóa và không trả secret/token. |

EF model đã được đối chiếu với migration snapshot hiện tại và không có pending model changes tại thời điểm lập plan.

## 3. Quyết định khung và cột theo từng trang

| Trang | Khung đề xuất | Cột mặc định | Cột tùy chọn trong `Cột` | Modal Thêm/Sửa |
|---|---|---|---|---|
| **Loại danh mục → Giá trị** | `VppListDetailWorkspace` khoảng 34/66; trái là loại, phải là giá trị của loại đang chọn | Trái: `#`, Tên + mã, Module, Trạng thái. Phải: `#`, Mã, Giá trị, Thứ tự, Trạng thái, Thao tác | Mô tả, Extra 1–3, ngôn ngữ gốc, cập nhật bởi/lúc | Modal nhỏ 520–600px cho loại; modal vừa 640px cho giá trị. Parent category bị khóa khi mở từ danh sách phải. |
| **Danh mục mặt hàng** | Full-width `VppCollectionWorkspace` | `#`, Danh mục (tên + mã), Trạng thái, Mô tả ngắn, Cập nhật, Thao tác | Ngôn ngữ/fallback, audit; `Số mặt hàng` chỉ mở khi API có projection count | Modal nhỏ 560–640px: mã, tên, mô tả; localization là tab phụ khi edit. |
| **Mặt hàng** | Full-width Collection | `#`, Mặt hàng (tên + mã), Danh mục, Đơn vị, Trạng thái, NCC mặc định, Giá mặc định, Thao tác | Số NCC, VAT mặc định, mô tả, ngôn ngữ/fallback, audit | Modal rộng 760–880px: mã, tên, danh mục active, UOM active, mô tả. Không chỉnh giá trong modal này. |
| **Nhà cung cấp** | Full-width Collection | `#`, Nhà cung cấp (tên đầy đủ + tên ngắn), Thành phố/Phường, Trạng thái, Địa chỉ ngắn, Thao tác | Địa chỉ 2–3, mô tả, localization, audit; count bảng giá/mặt hàng chỉ khi API projection có thật | Modal vừa 680–760px: tên, tên ngắn, Address 1–3, ward, city, mô tả. |
| **Bảng giá** | Full-width Collection; chọn bảng giá rồi chuyển selector ngang sang **Giá mặt hàng** | `#`, Bảng giá (tên + mã), NCC, Phiên bản, Trạng thái, Hiệu lực, Số mặt hàng, Mặc định, Thao tác | Tiền tệ, VAT policy, hợp đồng, discount/rebate/fee/shipping, publish/expire/audit | Modal rộng 880–1040px, chia `Thông tin chung / Hiệu lực / Điều khoản thương mại`; publish, expire, set-default và clone là action riêng có confirm. |
| **Giá mặt hàng** | Collection theo bảng giá và NCC đã chọn; không split màn hình | `#`, Mặt hàng (tên + mã), Danh mục, Đơn vị, Supplier SKU, Đơn giá, VAT, MOQ, Lead time, Mặc định, Thao tác | Net price nếu sau này tách khác giá, mô tả, trạng thái, audit | Modal vừa 720–820px; price list + supplier khóa theo context, chọn item active chưa có trong bảng giá, một ô đơn giá, VAT, MOQ, lead, SKU, default, mô tả. |
| **Phòng ban** | Full-width Collection; hierarchy thể hiện bằng cột parent, không dùng tree làm bảng chính | `#`, Phòng ban (tên + mã), Phòng ban cha, Trạng thái, Mô tả, Thao tác | Số phòng con/user active khi API có projection, localization, audit | Modal vừa 640–720px: mã, tên, parent, mô tả; selector parent loại chính nó và toàn bộ descendants. |
| **Người dùng** | Full-width Collection; bỏ inspector cố định để lấy lại chiều ngang | `#`, Người dùng (họ tên + login/email), Trạng thái tài khoản, Nhóm quyền, Phòng ban, Trạng thái lời mời/membership, Thao tác | Employee code, email confirmed, must-change-password, user type, cập nhật; account id chỉ dành DEV inspector | Có **Thêm người dùng**. Modal 760–840px: username, email, họ tên, employee code, group, department, lý do, gửi lời mời. Admin không nhập password; invite one-time để user tự đặt password. |
| **Nhóm quyền và permission** | `VppListDetailWorkspace`: canonical group gọn bên trái, quyền theo trang bên phải; nút **Cấu hình** mở workspace dialog | Group: `#`, Tên + code, mô tả, số user. Permission: tên, code, loại, trạng thái truy cập, trạng thái khóa | Audit và mapping id kỹ thuật không hiện mặc định | Permission editor dùng workspace dialog `90vw × 85vh`, selector trang ngang và một access-state control; lưu batch một lần. Ba canonical group immutable; không có Thêm role trong current scope. |
| **Nhật ký bảo mật** | Full-width Collection read-only | `#`, Thời gian, Người thao tác, Đối tượng, Hành động, Kết quả, Tài nguyên, Tóm tắt | Reason, correlation id, resource id | Không có editor; chỉ filter và xem chi tiết. API read-only đã triển khai, không đổi schema. |

## 4. Contract adaptive editor dialog

Không dùng một kích thước modal cho mọi form. **Số cột của bảng không quyết định kích thước form**; chỉ số field thật sự cần nhập và độ phức tạp của editor mới quyết định.

| Loại | Khi dùng | Kích thước desktop | Ví dụ |
|---|---|---|---|
| **Compact dialog** | 1–6 field, một nhóm dữ liệu | 520–640px, height theo content | Danh mục, loại danh mục |
| **Standard editor** | 7–14 field hoặc cần hai cột form | 720–880px, max `80vh` | Mặt hàng, NCC, phòng ban, thêm user, tạo role |
| **Workspace dialog** | Có matrix, bảng con, nhiều section hoặc cần so sánh | `90vw`, max 1280–1360px, `85vh` | Bảng giá, permission editor |

Khoảng trống không bị lấp bằng field giả hoặc kéo input quá rộng. Dialog dùng `max-width` theo nội dung, nhóm field thành section; một field dài như mô tả chiếm full row, field ngắn ghép hai cột. Workspace dialog dành phần còn lại cho matrix/table thay vì tạo mảng trắng.

1. `390×844` và vùng hẹp chuyển full-screen dialog.
2. Header/footer sticky; chỉ body dùng native scroll. Không custom scrollbar, không làm document scroll.
3. `Create` và `Edit` tái dùng cùng typed editor theo `EditorMode`, nhưng mỗi domain có component riêng.
4. Có focus trap, focus field đầu tiên, Escape đóng khi form sạch; form bẩn phải hỏi xác nhận.
5. Save disabled khi invalid/loading; validation nằm cạnh field, không chỉ toast.
6. Edit gửi `RowVersion` khi DTO hỗ trợ; conflict báo dữ liệu đã đổi và cho reload, không ghi đè mù.
7. Soft-delete/deactivate, publish/expire, reset-password và thay đổi quyền có confirm nêu rõ ảnh hưởng.
8. Localization nằm trong tab phụ của editor, không làm phình bảng chính.

## 4.1 Hiển thị, cho phép và trạng thái truy cập

Hai giá trị hiện hành không trùng nhau:

| Dữ liệu | Ý nghĩa |
|---|---|
| `IsVisible=false, IsEnable=false` | Ẩn khỏi UI; user không nhìn thấy component/menu. |
| `IsVisible=true, IsEnable=false` | Nhìn thấy nhưng chỉ đọc/disabled; dùng khi cần biết tính năng tồn tại nhưng không được thao tác. |
| `IsVisible=true, IsEnable=true` | Nhìn thấy và được tương tác, nhưng backend policy vẫn là security boundary cuối. |
| `IsVisible=false, IsEnable=true` | Trạng thái vô lý; backend hiện đã từ chối. |

Không gộp hai ý nghĩa ở backend. Trên UI, thay hai switch dễ nhầm bằng **một selector `Trạng thái truy cập`**:

- `Ẩn` — local deny cho UI component.
- `Chỉ xem` — hiện nhưng disabled/read-only.
- `Cho phép thao tác` — hiện và interactive.

Với **backend action permission**, current scope giữ canonical matrix read-only; không cho runtime thay đổi action grants. Khi triển khai phải audit toàn bộ consumer vì frontend hiện chưa áp dụng `IsEnable` đồng đều ở mọi route/component.

## 4.2 Custom role và kế thừa group — DEFERRED

Owner hoãn phần này để hoàn thiện UI hiện tại trước. Nội dung dưới đây chỉ giữ làm decision record, **không nằm trong execution AA0–AA7 và không được tạo migration trong current scope**.

Đây là thay đổi RBAC mức **trung bình–cao nhưng kiểm soát được** nếu giữ các giới hạn sau:

1. Chỉ **single inheritance**: mỗi custom role có đúng một parent. Không làm multiple inheritance vì conflict khó giải thích và khó audit.
2. Ba canonical role `EMPLOYEE / MANAGER / DEV` là system root, immutable. **V1 chỉ cho custom role kế thừa trực tiếp một system root**; custom → custom để phase sau nếu thật sự cần.
3. Dù V1 chỉ có một tầng, backend vẫn chặn self-parent, cycle, parent không phải system root hoặc parent bị vô hiệu hóa. Nếu mở nested custom role sau này, depth tối đa 3.
4. Effective permission tính từ root → child; override gần user nhất thắng.
5. `Kế thừa` không tạo grant mới; `Deny` phải thắng `Allow` kế thừa để tạo role ít quyền hơn parent.
6. Custom role chỉ được **giữ hoặc giảm** quyền trong ceiling của parent; không được cấp action/UI component mà parent không có.
7. DEV access-administration và recovery path được bảo vệ; không cho custom role hoặc thao tác runtime làm mất system-admin cuối cùng.
8. User vẫn chỉ có một active membership/group; chưa mở multi-role assignment trong scope này.
9. Mọi thay đổi role/parent/override tăng permission version, refresh role và descendants, revoke session khi cần và ghi `SecurityAudit`.

Thiết kế persistence đề xuất là migration additive: bổ sung metadata system/custom/status, access override rõ ràng và `RowVersion`; backfill ba root canonical; thêm self-FK/index, check không tự làm parent và check `IsEnable => IsVisible`. Không dùng `ParentGroupId` trần mà thiếu hierarchy validation.

## 4.3 Thêm người dùng bởi quản trị viên

Flow đề xuất:

1. Admin nhập username, email, họ tên, employee code, group, department và lý do.
2. Backend kiểm tra unique username/email/employee code và role/department active.
3. Tạo account `InvitationPending`, tạo membership trong cùng transaction và ghi audit.
4. Sinh invitation one-time từ Identity token, gửi qua email adapter/outbox hiện có.
5. User mở link, xác nhận email và tự đặt mật khẩu; admin không biết mật khẩu.
6. Thành công chuyển account sang `Active`, tăng session/permission version và ghi audit.
7. UI có `Gửi lại lời mời`, `Thu hồi lời mời`, `Vô hiệu hóa`; không hiển thị token hoặc password.

Nếu email local bị tắt, account vẫn ở `InvitationPending`; chỉ DEV/non-production mới được copy invite link qua action có audit. Production không hiện token trong toast/log.

## 4.4 Nhật ký bảo mật read-only

Đây là lịch sử bất biến của thao tác nhạy cảm: ai làm, tác động lên ai, thay đổi gì, kết quả, lý do, thời gian và correlation id. **Read-only** nghĩa là UI chỉ được tìm kiếm, lọc, xem chi tiết và export theo permission; không có Thêm/Sửa/Xóa. Mục đích là điều tra sự cố, chứng minh thay đổi quyền và biết ai đã tạo/vô hiệu hóa tài khoản.

## 5. Contract bảng và nhiều cột

- Toolbar desktop chỉ chứa điều khiển truy vấn/hiển thị: `Tìm kiếm → filter nghiệp vụ → Xóa bộ lọc → Cột`; không chứa CRUD.
- Collection header chứa identity/tổng bản ghi và nút **Thêm** khi route cho phép tạo mới. Cell cột **Thao tác** chứa toàn bộ **Xem/Sửa/Xóa-Khôi phục** và action vòng đời liên quan; header cột giữ text thuần, không đặt action button trong cột dữ liệu khác.
- Tất cả filter/sort/distinct query chạy server-side trên toàn DB; paging không giới hạn dữ liệu bộ lọc vào page hiện tại.
- Các grid lớn (`Mặt hàng`, `Giá mặt hàng`) mặc định 100 dòng; options `50 / 100 / 200`. Grid quản trị còn lại mặc định 50; options `25 / 50 / 100`.
- Footer luôn có top border, tổng số bản ghi, pager và page-size; không để row cuối đè lên footer.
- Không hiển thị mặc định `Guid`, `RowVersion`, numeric user id hoặc tên property kỹ thuật. Audit dùng nhãn thân thiện và column picker.
- Sort chỉ xuất hiện trên cột có ý nghĩa; icon sort ẩn khi idle, hiện rõ khi hover/active; cùng contract toàn dự án.
- Hàng giữ cùng height token; cell có name + code dùng hai dòng cố định, không làm row nhảy khi virtualization/paging.
- Trạng thái dùng badge; action dùng menu `...` khi có hơn ba hành động để tránh cột thao tác quá rộng.

## 6. Gap backend phải xử lý cùng UI

| Gap | Phương án |
|---|---|
| Generic Library API đang nhận DTO rộng và một số entity chưa có unique/typed validation đầy đủ | Tạo typed request + validation theo từng domain trước khi khóa editor; không dựa chỉ vào validation UI. |
| Lookup/category/supplier/department chưa có dependency impact rõ trước deactivate | Lookup/category/supplier/department đã có read-only dependency-impact endpoint; API còn enforce guard khi PATCH `IsDeleted=true`, không chỉ dựa vào UI. |
| Count như số item, số bảng giá, số user/child chưa có projection ổn định | Chỉ thêm cột sau khi API trả count trực tiếp; không tải navigation collection nặng chỉ để đếm. |
| Permission hiện PATCH từng mapping ngay lập tức | Thêm batch command transaction; validate toàn draft trước khi ghi, audit before/after và chỉ đóng modal khi toàn batch thành công. |
| User chưa có admin-create endpoint | Thêm typed admin invitation command; dùng Identity token + email/outbox; không dùng password tạm. |
| `IsVisible`/`IsEnable` là hai switch dễ nhầm | UI dùng một access-state selector; backend vẫn phân biệt presentation state và action grant. |
| `SecurityAudit` chưa có UI query và permission mapping mới chỉ log Serilog | Thêm endpoint read-only; mọi user/role/permission mutation ghi audit database, không ghi secret/token. |

### Giải thích API hardening / batch / count projection

- **API hardening:** backend dùng typed request, validation, dependency check và invariant; không tin dữ liệu chỉ vì UI đã validate.
- **Batch permission:** toàn bộ thay đổi trong permission modal được kiểm tra và commit trong một transaction; tránh 10 switch thành công nhưng switch thứ 11 lỗi làm quyền ở trạng thái dở dang.
- **Count projection:** API trả trực tiếp `ItemCount`, `UserCount`, `ChildCount` bằng query tổng hợp; frontend không tải toàn bộ navigation collection chỉ để đếm.
- Current scope không đổi schema. Custom role inheritance nếu được mở lại sau này phải có plan database riêng với preflight, disposable LocalDB, fresh/upgrade test và recovery record.

## 7. Kế hoạch thực thi canonical

| Wave | Trạng thái | Model + effort | Thực hiện | Sau wave owner có gì để duyệt | Gate |
|---|---|---|---|---|---|
| **AA0 — Baseline & contract** | **COMPLETED — CONTRACT LOCKED** | **Sol · High** | Chụp route-real hiện tại, inventory DTO/API/permission, khóa column/dialog contract và API gaps | Contract ba mức dialog, cột và API gap đã ghi; không đổi nghiệp vụ | Source/schema/permission ledger khớp; no hidden API invention |
| **AA1 — Shared admin foundation** | **COMPLETED — ADAPTIVE DIALOG FOUNDATION** | **Sol · High** | Chuẩn hóa modal shell adaptive, toolbar/column policy giữ nguyên frame hiện có; action menu để sau khi đủ consumer | Compact/Standard/Workspace dialog đã có consumer thật | 203 unit/architecture pass; isolated route-real desktop/mobile pass; owner visual review |
| **AA2 — Lookup & categories** | **COMPLETED — LISTDETAIL + COLLECTION + IMPACT GUARD** | **Terra · High implement; Sol · High review** | Loại→Giá trị ListDetail; Danh mục Collection; typed modal; dependency-aware deactivate | Danh mục full-width + popup typed; Lookup Add/Edit + impact guard đã có | Category/lookup route-real và backend impact tests pass; full matrix gom ở AA7 |
| **AA3 — Items, suppliers, departments** | **COMPLETED — UI + API INTEGRITY GUARD** | **Terra · High implement; Sol · High review** | Migrate ba Collection grid, typed editor, hierarchy/dependency validation, bỏ reflection inline edit | Item/Supplier/Department typed full-width; deactivate an toàn; cây phòng ban không tạo vòng lặp | 4 backend integrity tests, 203 frontend tests, Release build 0 warning, Supplier/Department isolated browser pass; full visual matrix gom ở AA7 |
| **AA4 — Pricing** | **COMPLETED — FULL-WIDTH + ADAPTIVE EDITORS** | **Sol · XHigh** | Bảng giá + Giá mặt hàng; lifecycle actions; optimistic concurrency; modal thương mại | Hai Collection đồng bộ filter; Bảng giá không còn inspector chật; editor giá/bảng giá dùng adaptive shell | 19 pricing backend tests, 203 frontend tests, Release build 0 warning, 2 isolated pricing browser tests; mutation E2E vẫn cần opt-in |
| **AA5 — Admin user invitation** | **COMPLETED — PASSWORDLESS + FULL-WIDTH USER ADMIN** | **Sol · XHigh** | Full-width user grid; add-user invitation editor; membership/status actions; bỏ inline dropdown | Admin tạo user mà không biết password; email-disabled state giải thích rõ | 26 focused backend tests, 203 frontend tests, Release build 0 warning, isolated desktop/mobile + editor pass |
| **AA6 — Canonical permission editor** | **COMPLETED — BATCH + LIVE SESSION VERIFIED** | **Sol · XHigh implement; Sol · Max review** | Group list-detail, workspace batch modal, unified access-state selector, canonical action/UI separation, realtime refresh | Owner xem và lưu UI permission một lần; action matrix vẫn read-only | 9 backend permission tests, 203 frontend tests, desktop/mobile visual pass ở hai lượt isolated riêng, mutation + restore pass |
| **AA7 — Security audit & hardening** | **COMPLETED — VERIFIED** | **Sol · XHigh** | Audit read-only, full visual/motion/accessibility/VI-EN review, refactor duplicate code | Toàn bộ quản trị + phân quyền đồng bộ, truy cập được từ header/sidebar và có truy vết | Solution 0 warning; frontend 204/204; backend 445/445; LocalDB 20/20; 28×4 runtime + dark/print/axe + admin route-real pass |
| **AA-RBAC — Custom role inheritance** | **DEFERRED BY OWNER** | **Sol · Max** | Plan/database task riêng khi owner mở lại | Không ảnh hưởng current UI execution | Chưa được phép tạo migration/code |

Model routing dựa trên hướng dẫn GPT-5.6 hiện hành: Sol cho kiến trúc, pricing và security-critical review; Terra cho các migration route lặp lại có contract đã khóa. Không thay model theo từng file.

## 8. Owner execution authority — 2026-07-30

- **DEFERRED:** custom role và group inheritance; không code/migration trong current execution.
- **APPROVED:** admin user invitation, security audit read-only, API hardening, batch permission và count projection.
- **APPROVED:** UI dùng một access-state selector cho canonical UI mapping; backend action matrix vẫn read-only.
- **APPROVED:** chỉ `Loại danh mục → Giá trị` và `Nhóm quyền → Permission` dùng ListDetail; các trang còn lại full-width Collection.
- **EXECUTION COMPLETED:** AA0 → AA7 đã có code, test, route-real visual evidence và commit theo wave; custom role/group inheritance vẫn để plan riêng.

## 9. Execution record — AA0/AA1 foundation slice — 2026-07-30

- AA0 đã khóa contract theo repository và Radzen `11.1.4`: ba mức `Compact / Standard / Workspace`, dùng `CssClass`, `WrapperCssClass` và `ContentCssClass`; không tạo form generic bằng reflection và không đổi schema/RBAC.
- AA1 foundation đã thêm `VppAdminDialogProfiles` và `VppAdaptiveDialogShell`. Body của editor có native scroll riêng, footer sticky; compact form co theo nội dung trên mobile, standard/workspace mới chuyển full-screen.
- Hai editor lookup thật đã migrate: `Dialog_AddLookupCategory` và `Dialog_AddLookupValue`; footer dùng `TryCloseAsync`, options không còn hard-code width tại caller.
- Evidence: frontend Release build pass; `203` frontend unit/architecture tests pass; isolated Playwright `LookupEditor_Uses_AdaptiveDialogShell_OnDesktopAndMobile` pass ở desktop `1366×768` và mobile `390×844`. Screenshot visual review đã kiểm tra bằng mắt; evidence thô nằm trong thư mục tạm ignored và không đưa vào commit.
- AA1 chưa được suy ra là hoàn tất toàn wave: migration full-width Collection của các admin grid legacy (`Component_ShareGrid`) vẫn để AA2/AA3, đúng dependency và decision record hiện hành.

## 10. Execution record — AA2 category slice — 2026-07-30

- `/library?tab=1` không còn dùng `Component_ShareGrid` legacy. Route đã chuyển sang `Tab_CategoryLibrary` typed, full-width `VppCollectionWorkspace`, server paging/filter/sort trên toàn nguồn và column picker hiện hành.
- Thêm/Sửa dùng `Dialog_CategoryEditor` compact adaptive; không còn inline row edit cho Danh mục. Permission hiện hành vẫn khóa Add/Edit/Status khi component không được enable.
- Lookup ListDetail đã có Edit popup typed cho cả `LookupCategory` và `LookupValue`; Add/Edit dùng cùng `VppAdminDialogProfiles`, không mở inline cell editor.
- Library API đã thêm `GET /api/Library/{tableCode}/{id}/dependency-impact` cho `lookup-categories`, `lookup-values` và `vpp-categories`; UI chặn deactivate khi còn bản ghi active tham chiếu. Không đổi schema/migration.
- Consumer ledger được cập nhật thành `16 file / 21 DataGrid`; pattern test của Danh mục đổi từ ListDetail sang Collection.
- Evidence: Release build `0 warning`; `203` unit/architecture tests pass; isolated Playwright category surface + typed dialog pass. Screenshot `1366×768` đã được kiểm bằng mắt, artifact thô giữ trong `tmp/` ignored.
- AA2 còn Lookup dependency-aware deactivate và mutation CRUD an toàn; không được suy ra là toàn wave đã hoàn tất.
- AA2 còn full lookup route visual review và mutation CRUD isolated; supplier/department dependency impact được giữ ở AA3.

## 11. Execution record — AA3 supplier slice — 2026-07-30

- `/library?tab=3` đã chuyển từ `Component_ShareGrid` legacy sang `Tab_SupplierLibrary` typed full-width Collection; Add/Edit dùng `Dialog_SupplierEditor` standard adaptive.
- Bảng giữ server-side filter/sort/paging, column picker, status toggle và cột nghiệp vụ; không còn inline editor/reflection trên route này.
- Items và Departments chưa được suy ra hoàn tất; dependency impact Supplier sẽ khóa cùng domain mutation contract ở AA3.

## 12. Execution record — AA3 item slice — 2026-07-30

- `/library?tab=2` đã chuyển sang `Tab_ItemLibrary` typed Collection; filter category/UOM, server paging/sort, column picker và status endpoint vẫn giữ đúng API catalog hiện hành.
- Add/Edit dùng `Dialog_ItemEditor` standard với typed `VppItemCreateRequest`/`VppItemUpdateRequest`; không dùng reflection hoặc inline editor.
- Department migration, supplier dependency impact và full AA3 visual matrix còn pending.

## 13. Execution record — AA3 department slice — 2026-07-30

- `/library?tab=5` đã chuyển sang `Tab_DepartmentLibrary` typed full-width Collection; parent hiển thị thành cột nghiệp vụ và editor loại chính bản ghi khỏi parent options.
- Add/Edit dùng `Dialog_DepartmentEditor`; route không còn inline/reflection editor.
- `LibraryIntegrityService` đã tách rule integrity khỏi controller: Supplier đếm mapping + price list đang dùng; Department đếm child active + membership active.
- API `PATCH .../{id}` từ chối deactivate khi còn reference active; Department create/update/patch từ chối self-parent, parent inactive/missing và parent là descendant.
- Evidence: 4 backend integrity tests pass; frontend Release build và 203 unit/architecture tests pass; isolated Playwright Supplier + Department editor pass ở `1366×768`.
- Full visual matrix `390/768/1366/1920`, accessibility và motion review vẫn để AA7; không suy ra từ focused route test.

## 14. Execution record — AA4 pricing — 2026-07-30

- Bảng giá đã đổi từ ListDetail + inspector sang full-width `VppCollectionWorkspace`, đúng decision chỉ Lookup và Permission còn dùng ListDetail.
- Toolbar Bảng giá dùng search + status selector + clear-filter canonical; Giá mặt hàng dùng cùng `VppFilterSelect` cho bảng giá/NCC và `VppFilterSearch`, bỏ reload thủ công.
- `Dialog_PriceListEditor` dùng Workspace adaptive; `Dialog_PriceEditor` dùng Standard adaptive. Publish/expire/set-default/clone vẫn là action lifecycle riêng, không trộn vào form.
- Lỗi overlay loading do reload lần hai đã được phát hiện bằng screenshot và loại bỏ; ảnh runtime sau sửa đã được kiểm bằng mắt ở `1366×420`.
- Evidence: frontend Release build `0 warning`; 203 unit/architecture tests; 19 backend PriceList/VPPPrice tests; isolated Playwright pricing geometry + price-list adaptive editor pass. Mutation publish/expire không chạy vì chưa bật mutation opt-in.

## 15. Execution record — AA5 admin user invitation — 2026-07-30

- Backend bổ sung invitation passwordless và gửi liên kết thiết lập/đặt lại mật khẩu một lần. Admin không nhập, nhận hoặc nhìn thấy mật khẩu; acceptance xác nhận email và xóa `MustChangePassword`.
- `/permission?tab=0` đã chuyển từ ListDetail + inspector sang full-width `VppCollectionWorkspace`. Grid bỏ toàn bộ dropdown sửa inline; nhóm quyền/phòng ban và activation mở typed adaptive editor.
- Toolbar có search, trạng thái, nhóm quyền, phòng ban, clear-filter, Add User và column picker. Hai action cuối được gom thành một cụm để wrap có trật tự ở desktop hẹp và mobile.
- Email disabled là trạng thái an toàn có chủ đích: Add User và gửi reset link bị khóa, tooltip lấy từ capability API giải thích nguyên nhân; không tự bật SMTP hoặc sửa secret/config.
- Sửa regression route thật do Radzen grid prerender không tự gọi `LoadData`: khôi phục reload đúng một lần trong `OnAfterRenderAsync`, không dùng reload lặp hoặc timer.
- Evidence: 26 focused backend lifecycle/controller tests; frontend Release build `0 warning`; 203/203 frontend unit/architecture tests; isolated Playwright desktop `1366×768`, mobile `390×844` và compact membership editor pass. Screenshot đã được kiểm bằng mắt; artifact thô nằm trong `tmp/` ignored.
- Custom role/group inheritance vẫn `DEFERRED`; AA5 không tạo migration hoặc thay canonical RBAC.

## 16. Execution record — AA6 canonical permission editor — 2026-07-30

- `/permission?tab=1` mặc định là ListDetail: ba canonical group ở trái, quyền UI theo trang ở phải; API action matrix tách sang selector `Quyền API (tham chiếu)` và hoàn toàn read-only.
- Workspace `Dialog_PermissionUiBatchEditor` thay hai switch rời bằng một `Trạng thái truy cập`: `Ẩn / Chỉ xem / Cho phép thao tác`. Action grant, mapping bắt buộc và mapping ngoài role ceiling chỉ hiển thị trạng thái khóa.
- Backend có `PATCH /api/Permission/component-mappings/batch`: validate toàn batch trước mutation, chặn action grant và recovery navigation của DEV, dùng transaction relational, ghi một `PERMISSION_UI_BATCH_UPDATED` audit và notify session sau commit.
- `GET /api/Permission/groups` trả `UserCount` bằng projection; group grid không hiện pager thừa khi chỉ có ba canonical role. Dialog grid sở hữu native scroll và không tràn xuống vùng lý do/footer.
- Consumer ledger tăng thành `20 file / 25 DataGrid`; browser/page object cũ đã chuyển từ inline switch sang batch editor.
- Evidence: frontend/backend Release build `0 warning`; frontend `203/203`; backend canonical permission `9/9`; Playwright desktop và mobile pass ở hai lượt isolated riêng; mutation hide/show Report permission và restore `1/1`. Một lượt gộp từng gặp negotiation SignalR tạm thời giữa fixture, desktop rerun độc lập pass. Ảnh desktop, workspace editor, API matrix và mobile đã được kiểm bằng mắt trong `tmp/aa6-ui-evidence/` (ignored).
- Custom role/group inheritance vẫn `DEFERRED`; AA6 không đổi schema hoặc canonical RBAC.

## 17. Execution record — AA7 security audit & final hardening — 2026-07-30

- Bổ sung `GET /api/Permission/security-audits` và `/filter-options` dưới policy `PERMISSION_MANAGE`: search/action/outcome/date filter, allowlisted sort, server paging và `X-Total-Count` đều chạy trên toàn query. Response typed không chứa password, token hoặc browser/session secret.
- `/permission?tab=2` dùng full-width `VppCollectionWorkspace`, filter/search/column picker canonical và adaptive read-only detail dialog; không có mutation action. Route được đăng ký trong `RouteCatalog`, header-tab và sidebar; action permission được route/navigation xử lý đúng bằng `PermissionState.HasPermission`.
- User invitation/membership/permission editor đã được hoàn tất resource VI/EN; admin cell hai dòng dùng chung `vpp-admin-two-line-cell`, CSS permission legacy không còn consumer đã được xóa. Chốt kỳ trở lại canonical `VppOperationWorkspace` mà không đổi behavior.
- Browser evidence đã kiểm bằng mắt tại `1366×768` và `390×844`: audit desktop, filter popover, detail dialog, mobile và English; data load hoàn tất, không document overflow. Ma trận 28 màn × 4 viewport, representative dark/print/axe, Security Audit `3/3`, User `1/1`, Permission `2/2`, WorkspacePattern và AtlasWave1 đều pass trên Aspire/LocalDB cô lập.
- Build/test: solution Release `0 warning / 0 error`; frontend `204/204`; backend `445/445`; integration mặc định `14 pass / 6 opt-in skip`; disposable LocalDB `20/20`, `0 skip`, không còn instance `GTASVPP_QA_*`; EF báo không có pending model changes.
- Wire-contract manifest được cập nhật có chủ đích cho `SecurityAuditResDTO`. Baseline LocalDB cũ hardcode bốn role legacy đã được thay bằng `CanonicalRbac.Personas` hiện hành; không đổi schema/migration. Custom role/group inheritance vẫn `DEFERRED`.

## 18. Post-plan smart cleanup — 2026-07-30

- Xóa `Component_ShareGrid<T>`, `Component_RecordInspector<T>`, `Component_Loading` và `DialogProvider` sau source scan xác nhận zero-consumer. Radzen provider thật vẫn thuộc `MainLayout`/`LoginLayout` qua `RadzenComponents`.
- `Component_Library` chỉ còn điều phối tab, permission và URL; preload, reflection CRUD, dropdown cache và toast/API helper legacy được gỡ để mỗi tab typed sở hữu data flow duy nhất.
- Xóa CSS chỉ phục vụ generic list/detail inspector; giữ nguyên selector đang dùng cho Pricing, Permission và các collection typed. Test architecture đổi từ bảo vệ legacy sang khóa không cho legacy quay lại.
- Đây là cleanup behavior-preserving, không đổi API, database, permission, route hay visual contract; refactor sâu file CSS lớn tiếp tục `DEFERRED` đến khi UI hoàn thiện.

## 19. Post-audit Library data lifecycle và column ownership — 2026-07-30

- DB/API không thiếu dữ liệu. Isolated contract xác nhận `X-Total-Count > 0` cho Category, Item, Supplier, Department và Price List; browser xác nhận thêm Lookup Value và Price rows.
- Root cause grid trắng là Radzen `LoadData` không chạy lại khi tab đã prerender trước interactive circuit. Sáu collection tab dùng chung `VppServerGridComponentBase<T>` để reload một lần sau interactive handoff; Price chỉ reload khi đã có price-list/supplier context. Lookup auto-select category đầu tiên để detail pane có data ngay.
- Column ownership được trả về route typed đúng mục 3: Category/Item/Supplier/Department full-width, Lookup list-detail và Pricing hai collection. Cột phụ/audit vẫn truy cập qua `Cột`, không ép tất cả vào viewport; toolbar/frame/pager/hover/focus tiếp tục do design system sở hữu.
- Test cũ cho phép empty state đã được thay bằng API count + rendered row gate. Tablet assertion stale từng đòi Price List/User là list-detail được sửa thành collection workspace contract hiện hành; không còn test bảo vệ kiến trúc đã bị loại bỏ.

### 19.1 Ma trận route → API → data state → component → CSS owner

| Route | API/query authority | Data state đã khóa | Component/workspace owner | CSS owner hiện hành |
|---|---|---|---|---|
| `/library?tab=0` | `/api/Library/lookup-categories`; `/api/Library/lookup-values` theo category | Hai grid `ServerPaging`; category đầu được chọn một lần sau load; category và value đều phải có DOM row thật trong fixture | `Tab_LookupLibrary` + `VppListDetailWorkspace` | Geometry: `VppListDetailWorkspace.razor.css`; frame/toolbar: scoped design-system CSS; cell/grid: `vpp-admin.css` |
| `/library?tab=1` | `/api/Library/vpp-categories` | `X-Total-Count > 0`; first interactive reload; server filter/sort/page | `Tab_CategoryLibrary` + `VppCollectionWorkspace` | `VppCollectionWorkspace.razor.css`, `VppDataSurfaceFrame.razor.css`, `VppDataToolbar.razor.css`, `vpp-admin.css` |
| `/library?tab=2` | `/api/catalog/items`; category/UOM lookup cho toolbar | `X-Total-Count > 0`; first interactive reload; toolbar filter không phụ thuộc page hiện tại | `Tab_ItemLibrary` + `VppCollectionWorkspace` | Shared Collection/Frame/Toolbar CSS; `vpp-admin.css` chỉ sở hữu typed column rhythm/cell state |
| `/library?tab=3` | `/api/Library/suppliers` | `X-Total-Count > 0`; first interactive reload; dependency impact trước deactivate | `Tab_SupplierLibrary` + `VppCollectionWorkspace` | Shared Collection/Frame/Toolbar CSS + `vpp-admin.css` |
| `/library?tab=5` | `/api/Library/departments`; cùng endpoint active để resolve parent | `X-Total-Count > 0`; first interactive reload; parent label từ snapshot active | `Tab_DepartmentLibrary` + `VppCollectionWorkspace` | Shared Collection/Frame/Toolbar CSS + `vpp-admin.css` |
| `/library?tab=6&pricingTab=price-lists` | `/api/vpppricelist`; `/api/Library/suppliers` | `X-Total-Count > 0`; first interactive reload; lifecycle Draft/Published/Expired giữ nguyên | `Tab_PriceListLibrary` + `VppCollectionWorkspace` | Shared Collection/Frame/Toolbar CSS + pricing selectors trong `vpp-admin.css` |
| `/library?tab=6&pricingTab=prices` | `/api/vppprice/item-prices` theo `priceListId + supplierId`; lookup `/api/vpppricelist` và suppliers | Chỉ reload khi đủ hai context; DOM row thật bắt buộc; price/SKU/VAT/MOQ/lead thuộc route typed | `Tab_PriceLibrary` + `VppCollectionWorkspace` | Shared Collection/Frame/Toolbar CSS + pricing selectors trong `vpp-admin.css` |

### 19.2 Phân loại code/CSS chồng chéo

| Thành phần | Kết luận | Hành động |
|---|---|---|
| `Component_ShareGrid`, `Component_RecordInspector` và generic admin inspector CSS | `DELETE` đã hoàn tất ở smart cleanup trước; zero consumer | Không phục hồi; architecture/source scan bảo vệ |
| `.vpp-admin-filterbar`, `.vpp-library-search`, selector toolbar trực tiếp cũ | `DELETE` trong post-audit; zero consumer sau khi mọi route dùng `VppDataToolbar` | Đã xóa khỏi `vpp-admin.css` |
| `.vpp-admin-grid` và typed column classes | `KEEP` — vẫn là shared cell/header/pager contract của Library và Permission | Không xóa mù; route chỉ truyền width/profile qua CSS variables |
| `VppCollectionWorkspace` / `VppListDetailWorkspace` scoped CSS | `KEEP` — authority geometry hiện hành | Test route + tablet khóa đúng pattern |
| `vpp-layout.css` | `KEEP` — shell/header/sidebar/inset, không sở hữu cột Library | Không thêm override route-specific mới vào đây |

## 20. CRUD placement và action-column hardening — 2026-07-30

- Toolbar của toàn bộ Library/Permission chỉ còn tìm kiếm, bộ lọc, xóa lọc và chọn cột; không còn nút CRUD.
- Owner visual review đã thay quyết định ban đầu: `VppActionColumnHeader` bị xóa. Route có quyền tạo mới dùng `VppCollectionHeader`, đặt **Thêm** ở collection header; **Xem/Sửa/Xóa-Khôi phục** và action vòng đời vẫn nằm trong cell cột **Thao tác**.
- Cột **Thao tác** không tham gia sort/filter/reorder/column picker và được ghim bên phải để vẫn nhìn thấy ở tablet hoặc bảng nhiều cột. Trạng thái active/inactive là badge chỉ đọc, mutation chuyển về action rõ nghĩa.
- Evidence hiện hành: frontend Release build `0 warning / 0 error`; frontend unit/architecture `203/203`; isolated admin route matrix, Lookup geometry, Category/Lookup editor đều pass. Ảnh desktop/tablet đã được kiểm bằng mắt; artifact thô nằm trong `tmp/lookup-header-scroll-evidence-final/` và không commit.

## 21. Lookup bounded surface correction — 2026-07-30

- Hai pane Lookup có collection header riêng, footer/pager luôn hiện kể cả một trang; tiêu đề cột **Thao tác** không còn chứa nút mutation.
- Splitter desktop dùng đủ chiều cao workspace; grid value giữ footer ở đáy và scroll dọc trong `.rz-data-grid-data`, không kéo dài document hoặc cắt hàng cuối.
- Library grids tắt `AllowAlternatingRows`; row dùng một nền neutral, chỉ hover/selected/status tạo khác biệt thị giác. Pane master thu gọn width sau khi bỏ nút Thêm để không phát sinh horizontal overflow ở `1366×768`.

## 22. Lookup/Class lifecycle correction — 2026-07-31

- Dialog Lookup Value bỏ hoàn toàn `ExtraField1–3`; metadata/column picker cũng không còn đưa ba field legacy ra UI. DTO và cột database vẫn được giữ để đọc/ghi tương thích dữ liệu cũ.
- Dialog tạo loại/giá trị dùng label cố định và placeholder `Ví dụ: ...` lấy từ bản ghi database đang hiển thị; edit dialog vẫn hiển thị giá trị thật, không dùng placeholder thay dữ liệu.
- Cột **Thao tác** tách ba hành động: sửa, switch `Hoạt động` (`ON = active`, `OFF = IsDeleted`) và `delete_forever`. Nút xóa vĩnh viễn bị khóa khi bản ghi còn active.
- DELETE Lookup/Class đi qua `LibraryIntegrityService`: bắt buộc đã soft-delete, chặn mọi reference kể cả reference inactive, xóa translation sở hữu và entity trong một transaction; FK vẫn là guard cuối khi có race.
- Evidence: frontend Release build `0 warning/error`; backend integrity `7/7`; frontend architecture `28/28`; isolated responsive dialog `1/1`; mutation tạo → vô hiệu hóa → xóa vĩnh viễn `1/1`. Ảnh desktop/mobile và action column đã kiểm bằng mắt trong `tmp/lookup-hard-delete-ui/` (ignored).
