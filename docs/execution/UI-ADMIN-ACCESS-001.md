# UI-ADMIN-ACCESS-001 — Quản trị danh mục và phân quyền

> Trạng thái: **APPROVED CURRENT SCOPE — IMPLEMENTING AA0/AA1**
>
> Authority cha: [`UI-SYSTEM-001`](./UI-SYSTEM-001.md) và [`UI-DATA-SURFACE-001`](./UI-DATA-SURFACE-001.md)
>
> Phạm vi hiện hành: `/library`, `/permission`, account administration, security audit và API hardening không đổi schema. Custom role/group inheritance được owner hoãn sang plan riêng.

## 1. Mục tiêu một ánh nhìn

- Dùng **full-width Collection grid** cho bảng phẳng để dành chiều ngang cho nhiều cột.
- Chỉ dùng **ListDetail** khi hai tập dữ liệu có quan hệ cha–con cần quan sát đồng thời.
- Mọi thao tác **Thêm/Sửa mở adaptive editor dialog** theo độ phức tạp; form nhỏ dùng modal giữa màn hình, form lớn dùng workspace dialog gần full-screen. Không còn inline row edit hoặc dropdown sửa trực tiếp trong cell.
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
| Audit bảo mật | `SecurityAudit` đã ghi một số sự kiện registration, activation, membership và session | Chưa có route query quản trị hoàn chỉnh | Bổ sung màn immutable read-only và ghi mọi thay đổi user/role/permission vào audit. |

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
| **Nhật ký bảo mật** *(khuyến nghị)* | Full-width Collection read-only | `#`, Thời gian, Người thao tác, Đối tượng, Hành động, Kết quả, Tài nguyên, Tóm tắt | Reason, correlation id, resource id | Không có editor; chỉ filter và xem chi tiết. Cần API read-only mới, không đổi schema. |

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
| **AA1 — Shared admin foundation** | **FOUNDATION SLICE COMPLETE — FULL-WIDTH MIGRATION PENDING AA2** | **Sol · High** | Chuẩn hóa modal shell adaptive, toolbar/column policy giữ nguyên frame hiện có; action menu để sau khi đủ consumer | Hai lookup editor thật dùng shell; full-width Collection legacy sẽ migrate ở AA2/AA3 | 203 unit/architecture pass; isolated route-real desktop/mobile pass; owner visual review |
| **AA2 — Lookup & categories** | **IN PROGRESS — CATEGORY SLICE COMPLETE** | **Terra · High implement; Sol · High review** | Loại→Giá trị ListDetail; Danh mục Collection; typed modal; dependency-aware deactivate | Danh mục đã full-width + popup typed; Lookup/dependency impact còn lại | Category route-real pass; Lookup dependency state pending |
| **AA3 — Items, suppliers, departments** | `PENDING AA2` | **Terra · High implement; Sol · High review** | Migrate ba Collection grid, typed editor, hierarchy validation, bỏ reflection inline edit | Ba màn quản trị phẳng đồng bộ, tận dụng full width | API validation, 390–1920, no document scroll |
| **AA4 — Pricing** | `PENDING AA3` | **Sol · XHigh** | Bảng giá + Giá mặt hàng; lifecycle actions; optimistic concurrency; modal thương mại | Luồng Draft → edit item → publish/expire rõ ràng, không nhầm form với action | Pricing invariants, concurrency, currency/date, mutation E2E |
| **AA5 — Admin user invitation** | `PENDING AA1` | **Sol · XHigh** | Full-width user grid; add-user invitation editor; membership/status actions; bỏ inline dropdown | Admin tạo user mà không biết password; resend/revoke/activate state rõ | Identity token, uniqueness, transaction, audit, email-disabled state, self-lockout |
| **AA6 — Canonical permission editor** | `PENDING AA1/AA5` | **Sol · XHigh implement; Sol · Max review** | Group list-detail, workspace batch modal, unified access-state selector, canonical action/UI separation, realtime refresh | Owner xem và lưu UI permission một lần; action matrix vẫn read-only | Batch atomicity, protected DEV recovery, session invalidation, audit |
| **AA7 — Security audit & hardening** | `PENDING AA5/AA6` | **Sol · XHigh** | Audit read-only, full visual/motion/accessibility/VI-EN review, refactor duplicate code | Toàn bộ quản trị + phân quyền đồng bộ và có truy vết | Full frontend/backend/integration verify, authenticated browser matrix |
| **AA-RBAC — Custom role inheritance** | **DEFERRED BY OWNER** | **Sol · Max** | Plan/database task riêng khi owner mở lại | Không ảnh hưởng current UI execution | Chưa được phép tạo migration/code |

Model routing dựa trên hướng dẫn GPT-5.6 hiện hành: Sol cho kiến trúc, pricing và security-critical review; Terra cho các migration route lặp lại có contract đã khóa. Không thay model theo từng file.

## 8. Owner execution authority — 2026-07-30

- **DEFERRED:** custom role và group inheritance; không code/migration trong current execution.
- **APPROVED:** admin user invitation, security audit read-only, API hardening, batch permission và count projection.
- **APPROVED:** UI dùng một access-state selector cho canonical UI mapping; backend action matrix vẫn read-only.
- **APPROVED:** chỉ `Loại danh mục → Giá trị` và `Nhóm quyền → Permission` dùng ListDetail; các trang còn lại full-width Collection.
- **EXECUTION STARTED:** AA0 → AA1; sau mỗi wave có route representative và owner review artifact trước khi mở wave kế tiếp.

## 9. Execution record — AA0/AA1 foundation slice — 2026-07-30

- AA0 đã khóa contract theo repository và Radzen `11.1.4`: ba mức `Compact / Standard / Workspace`, dùng `CssClass`, `WrapperCssClass` và `ContentCssClass`; không tạo form generic bằng reflection và không đổi schema/RBAC.
- AA1 foundation đã thêm `VppAdminDialogProfiles` và `VppAdaptiveDialogShell`. Body của editor có native scroll riêng, footer sticky; compact form co theo nội dung trên mobile, standard/workspace mới chuyển full-screen.
- Hai editor lookup thật đã migrate: `Dialog_AddLookupCategory` và `Dialog_AddLookupValue`; footer dùng `TryCloseAsync`, options không còn hard-code width tại caller.
- Evidence: frontend Release build pass; `203` frontend unit/architecture tests pass; isolated Playwright `LookupEditor_Uses_AdaptiveDialogShell_OnDesktopAndMobile` pass ở desktop `1366×768` và mobile `390×844`. Screenshot visual review đã kiểm tra bằng mắt; evidence thô nằm trong thư mục tạm ignored và không đưa vào commit.
- AA1 chưa được suy ra là hoàn tất toàn wave: migration full-width Collection của các admin grid legacy (`Component_ShareGrid`) vẫn để AA2/AA3, đúng dependency và decision record hiện hành.

## 10. Execution record — AA2 category slice — 2026-07-30

- `/library?tab=1` không còn dùng `Component_ShareGrid` legacy. Route đã chuyển sang `Tab_CategoryLibrary` typed, full-width `VppCollectionWorkspace`, server paging/filter/sort trên toàn nguồn và column picker hiện hành.
- Thêm/Sửa dùng `Dialog_CategoryEditor` compact adaptive; không còn inline row edit cho Danh mục. Permission hiện hành vẫn khóa Add/Edit/Status khi component không được enable.
- Lookup ListDetail đã có Edit popup typed cho cả `LookupCategory` và `LookupValue`; Add/Edit dùng cùng `VppAdminDialogProfiles`, không mở inline cell editor. Dependency impact trước khi deactivate vẫn là phần backend kế tiếp, chưa giả vờ hoàn tất.
- Consumer ledger được cập nhật thành `16 file / 21 DataGrid`; pattern test của Danh mục đổi từ ListDetail sang Collection.
- Evidence: Release build `0 warning`; `203` unit/architecture tests pass; isolated Playwright category surface + typed dialog pass. Screenshot `1366×768` đã được kiểm bằng mắt, artifact thô giữ trong `tmp/` ignored.
- AA2 còn Lookup dependency-aware deactivate và mutation CRUD an toàn; không được suy ra là toàn wave đã hoàn tất.
