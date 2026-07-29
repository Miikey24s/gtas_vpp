# UI-ADMIN-ACCESS-001 — Quản trị danh mục và phân quyền

> Trạng thái: **PROPOSED — OWNER REVIEW**
>
> Authority cha: [`UI-SYSTEM-001`](./UI-SYSTEM-001.md) và [`UI-DATA-SURFACE-001`](./UI-DATA-SURFACE-001.md)
>
> Phạm vi: `/library`, `/permission`, API/DTO liên quan; chưa thay đổi database schema trong plan này.

## 1. Mục tiêu một ánh nhìn

- Dùng **full-width Collection grid** cho bảng phẳng để dành chiều ngang cho nhiều cột.
- Chỉ dùng **ListDetail** khi hai tập dữ liệu có quan hệ cha–con cần quan sát đồng thời.
- Mọi thao tác **Thêm/Sửa mở modal giữa màn hình**; mobile chuyển thành full-screen dialog. Không còn inline row edit hoặc dropdown sửa trực tiếp trong cell.
- Bảng chỉ hiện cột nghiệp vụ quan trọng; audit, localization và thông tin ít dùng nằm trong **Cột hiển thị** hoặc modal chi tiết.
- Filter/sort/paging chạy trên **toàn bộ dữ liệu server**, không chỉ trang hiện tại.
- Không tạo `UniversalAdminGrid<T>` hoặc form bằng reflection. Tái dùng frame/composite; mỗi domain có typed columns, typed editor và validation riêng.

### Khung tổng quát

```text
Header-tab
└─ Main content
   ├─ Toolbar: Tìm kiếm | Bộ lọc... | Xóa lọc | Cột | + Thêm
   ├─ Header cột
   ├─ Dòng dữ liệu 1..n
   └─ Footer: tổng kết | pager | page-size

Thêm / Sửa
└─ Modal giữa màn hình
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
| Người dùng | `AppUser 1-1 active UserGroupMembership`; membership bắt buộc group + department | `/api/Permission/users`, `/memberships`, Account admin endpoints | Không sửa group/department inline; mở modal phân công. Không có admin-create user API hiện hành. |
| Phân quyền | `PermissionGroup → GroupPageComponentMapping → PageComponentMapping`; canonical actions nằm trong `CanonicalRbac` | `/api/Permission/groups`, `/page-components`, `/component-mapping` | Tách quyền nghiệp vụ canonical read-only khỏi UI visibility/enable có thể cấu hình. |
| Audit bảo mật | `SecurityAudit` lưu actor, target, action, resource, outcome, reason, correlation | Chưa có route quản trị hoàn chỉnh | Nên thêm màn read-only sau core để truy vết thay đổi tài khoản/quyền. |

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
| **Người dùng** | Full-width Collection; bỏ inspector cố định để lấy lại chiều ngang | `#`, Người dùng (họ tên + login/email), Trạng thái tài khoản, Nhóm quyền, Phòng ban, Membership, Thao tác | User type, description, cập nhật, account id kỹ thuật chỉ dành DEV inspector | Không có **Thêm người dùng** trong scope hiện tại. `Phân công quyền` mở modal 640–720px gồm group, department, reason; activate/deactivate/reset-password là confirm/action riêng. |
| **Nhóm quyền và UI permission** | `VppListDetailWorkspace`: danh sách group gọn bên trái, trang/component bên phải; nút **Cấu hình** mở modal rộng | Group: `#`, Tên + code, mô tả, mô hình, cập nhật. Permission: component/action, code, loại, Visible, Enabled, lock state | Mapping id/audit không hiện mặc định | Modal 1040–1180px: selector trang ngang, matrix component, thay đổi lưu dạng draft rồi `Lưu`; không PATCH ngay từng switch. Canonical business actions read-only. |
| **Nhật ký bảo mật** *(khuyến nghị)* | Full-width Collection read-only | `#`, Thời gian, Người thao tác, Đối tượng, Hành động, Kết quả, Tài nguyên, Tóm tắt | Reason, correlation id, resource id | Không có editor; chỉ filter và xem chi tiết. Cần API read-only mới, không đổi schema. |

## 4. Contract modal chung

1. Desktop dùng centered dialog; `390×844` và vùng hẹp dùng full-screen dialog.
2. Header/footer sticky; chỉ phần body dùng native scroll. Không custom scrollbar, không làm document scroll.
3. `Create` và `Edit` tái dùng cùng typed editor theo `EditorMode`, nhưng mỗi domain có component riêng.
4. Có focus trap, focus field đầu tiên, Escape đóng khi form sạch; form bẩn phải hỏi xác nhận.
5. Save disabled khi invalid/loading; hiển thị validation cạnh field, không chỉ toast.
6. Edit gửi `RowVersion` khi DTO hỗ trợ; conflict phải báo dữ liệu đã đổi và cho reload, không ghi đè mù.
7. Soft-delete/deactivate, publish/expire, reset-password và thay đổi quyền có dialog xác nhận nêu rõ ảnh hưởng.
8. Localization nằm trong tab phụ của modal edit, không làm phình bảng chính.

## 5. Contract bảng và nhiều cột

- Toolbar desktop: `Tìm kiếm → filter nghiệp vụ → Xóa bộ lọc → Cột → + Thêm`.
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
| Lookup/category/supplier/department chưa có dependency impact rõ trước deactivate | Bổ sung preflight/impact response hoặc endpoint kiểm tra; dialog confirm phải hiển thị dependency thật. |
| Count như số item, số bảng giá, số user/child chưa có projection ổn định | Chỉ thêm cột sau khi API trả count trực tiếp; không tải navigation collection nặng chỉ để đếm. |
| Permission hiện PATCH từng mapping ngay lập tức | Thêm batch endpoint transaction hoặc command có kết quả từng item; modal chỉ đóng khi toàn batch thành công. |
| User không có admin-create endpoint | Giữ registration → admin activation. Nếu owner muốn “Thêm người dùng”, mở plan bảo mật/backend riêng; không giả nút UI. |
| Canonical RBAC chỉ có các persona được source khóa | Không thêm custom role builder. Chỉ sửa metadata được phép và UI component mapping; thay đổi role model là task RBAC riêng cần owner duyệt. |
| `SecurityAudit` chưa có UI query | Thêm endpoint read-only, phân trang và filter; không cho sửa/xóa audit. |

## 7. Kế hoạch thực thi canonical

| Wave | Model + effort | Thực hiện | Sau wave owner có gì để duyệt | Gate |
|---|---|---|---|---|
| **AA0 — Baseline & contract** | **Sol · High** | Chụp route-real hiện tại, inventory DTO/API/permission, khóa column/dialog contract và API gaps | Review board trước/sau cho `/library` và `/permission`; không đổi nghiệp vụ | Source/schema/permission ledger khớp; no hidden API invention |
| **AA1 — Shared admin foundation** | **Sol · High** | Chuẩn hóa Collection frame, toolbar order, column policy, modal shell, action menu, footer/pager | Một màn sandbox/admin representative chứng minh bảng rộng + modal + mobile | Architecture tests; keyboard/dialog geometry; Light/Dark |
| **AA2 — Lookup & categories** | **Terra · High implement; Sol · High review** | Loại→Giá trị ListDetail; Danh mục Collection; typed modal; dependency-aware deactivate | Hai màn đầu hoàn chỉnh, có add/edit popup và column picker | CRUD route-real; filter full DB; dependency state |
| **AA3 — Items, suppliers, departments** | **Terra · High implement; Sol · High review** | Migrate ba Collection grid, typed editor, hierarchy validation, bỏ reflection inline edit | Ba màn quản trị phẳng đồng bộ, tận dụng full width | API validation, 390–1920, no document scroll |
| **AA4 — Pricing** | **Sol · XHigh** | Bảng giá + Giá mặt hàng; lifecycle actions; optimistic concurrency; modal thương mại | Luồng Draft → edit item → publish/expire rõ ràng, không nhầm form với action | Pricing invariants, concurrency, currency/date, mutation E2E |
| **AA5 — Users** | **Sol · XHigh** | Full-width user grid; membership modal; activate/deactivate/reset confirmation; bỏ inline dropdown | Trang người dùng dễ quét, mutation an toàn và có lý do | RBAC/non-production guard, self-lockout prevention, session refresh |
| **AA6 — Permissions** | **Sol · XHigh implement; Sol · Max review** | Group list-detail, wide batch permission modal, canonical/read-only separation, realtime refresh | Owner duyệt một group và toàn matrix theo page mà không sửa từng cell trực tiếp | Batch atomicity, protected DEV navigation, mutation opt-in, audit |
| **AA7 — Audit & hardening** | **Sol · XHigh** | Security audit read-only, full visual/motion/accessibility/VI-EN review, refactor duplicate code | Toàn bộ quản trị + phân quyền đồng bộ và có truy vết | Full frontend verify, authenticated browser matrix, diff/debt review |

Model routing dựa trên hướng dẫn GPT-5.6 hiện hành: Sol cho kiến trúc, pricing và security-critical review; Terra cho các migration route lặp lại có contract đã khóa. Không thay model theo từng file.

## 8. Owner approval gate

Trước khi code, owner cần duyệt các điểm sau:

1. **Không có nút Thêm người dùng**: giữ flow đăng ký → quản trị viên kích hoạt/phân công.
2. **Không có custom role builder**: canonical role/action giữ read-only; chỉ cấu hình UI visibility/enable trong modal batch.
3. Có thêm tab **Nhật ký bảo mật** read-only hay để sau AA6.
4. Chấp nhận bổ sung API hardening/batch/count projection cần thiết nhưng **không đổi schema** trong scope này; schema change nếu phát sinh phải tách approval riêng.
5. Duyệt khung: chỉ `Loại→Giá trị` và `Nhóm quyền→Permission` dùng ListDetail; các trang còn lại full-width Collection.
