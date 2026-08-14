# UI-COLUMN-CONTRACT-001 — Chuẩn hóa cột, bộ chọn cột và ô nhận diện

> Trạng thái: `PLANNED — OWNER REVIEW`
> Authority cha: [`UI-SYSTEM-001`](./UI-SYSTEM-001.md), [`UI-DATA-SURFACE-001`](./UI-DATA-SURFACE-001.md) và [`VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN`](../design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md).
> Phạm vi: frontend Blazor/Radzen; chưa đổi API, database, DTO hoặc nghiệp vụ.

## 1. Bản một ánh nhìn

### Mục tiêu

Chuẩn hóa toàn bộ data grid để người dùng luôn hiểu:

- grid có những cột nào, cột nào đang hiển thị;
- cột nào được phép ẩn/hiện và cột nào phải cố định;
- khi nào dùng một ô hai dòng `Tên + mã`, khi nào phải tách `Tên` và `Mã`;
- thứ tự `search → filter → xóa bộ lọc → cột` và thứ tự filter khớp với thứ tự cột;
- không lộ trường kỹ thuật không giúp người dùng ra quyết định.

### Phát hiện hiện tại

1. Trigger `Cột 6` đang hiển thị **6 cột đang bật**, không phải tổng số trường có thể chọn. Popup bên trong mới hiển thị dạng `6/8`, nên trigger dễ làm người dùng nghĩ chỉ có 6 cột.
2. `Mã bảng giá` đã được khai báo `Visible="false" Pickable="true"`; theo source hiện tại nó phải có trong popup, chỉ đang ẩn mặc định. Nếu route thật không thấy, đó là lỗi đăng ký/trạng thái popup cần sửa ở phase C1.
3. Cột `#` ở phần lớn grid chưa đặt rõ `Pickable="false"`, nên có thể bị đưa nhầm vào bộ chọn cột. Cột `Thao tác` đa số đã cố định đúng.
4. Một số picker đang cho phép hiện trường kỹ thuật như `UserId`, `LookupCategoryId`, `CreatedByUserId`, `RowVersion`; các trường này không phù hợp với người dùng nghiệp vụ.
5. Ô hai dòng `Tên + mã` ban đầu được duyệt cho bảng đơn hàng, nhưng hiện đã lan sang nhiều màn quản trị. Một số nơi hợp lý, một số nơi làm mã khó sort, so sánh, import và quản trị độc lập.

### Phương án khuyến nghị

| Phase | Làm gì | Kết quả | Gate |
|---|---|---|---|
| C0 — Khóa contract | Chốt phân loại cột, quy tắc hai dòng và danh sách route | Một chuẩn dùng chung trước khi sửa code | Owner duyệt file này |
| C1 — Sửa foundation | Làm rõ trigger `Cột đang hiện/tổng`; cố định `#`, `Thao tác`; loại trường kỹ thuật | Bộ chọn cột không còn gây hiểu nhầm | Unit/architecture + 2 route mẫu |
| C2 — Chuẩn hóa Library/Admin | Tách hoặc gộp `Tên/Mã` đúng ngữ cảnh; bổ sung các cột nghiệp vụ đang thiếu | Các trang quản trị đồng nhất và dễ tra cứu | Route-real toàn bộ 11 picker |
| C3 — Chuẩn hóa toàn frontend | Rà các grid không có picker, thứ tự filter–cột, tên cột và responsive | Không còn mỗi trang dùng một quy tắc riêng | Architecture inventory + browser matrix |
| C4 — Khóa bằng test/docs | Thêm contract test và cập nhật design system | Sửa sau này không làm drift trở lại | Frontend verify + owner visual review |

### Model + effort khuyến nghị

- Audit contract và final review: `gpt-5.6-sol · high`.
- Rollout cơ học sau khi contract được duyệt: `gpt-5.6-terra · medium`, sau đó `gpt-5.6-sol · high` review.
- Snapshot quota 2026-08-14 chỉ có coverage theo tuần: khoảng `10.41 account-equivalents` quan sát được; ước tính scope này tiêu thụ khoảng `2–6% Plus-equivalent`, confidence thấp–trung bình, cộng buffer 50% vẫn ở mức `ENOUGH`. Đây là khuyến nghị routing, không phải xác nhận model hiện tại đã được đổi.

## 2. Contract cột được đề xuất

### 2.1 Bốn nhóm cột

| Nhóm | Ví dụ | Quy tắc picker |
|---|---|---|
| Cố định cấu trúc | `#`, checkbox chọn dòng, `Thao tác` | Luôn `Pickable="false"`; không tính vào số cột người dùng có thể chọn |
| Nghiệp vụ chính | Tên, mã, trạng thái, nhà cung cấp, phòng ban, số tiền | Hiện mặc định; được phép ẩn nếu không phá thao tác chính |
| Nghiệp vụ phụ | Mô tả, địa chỉ phụ, người/ngày cập nhật | Ẩn mặc định nhưng có trong picker với tên thân thiện |
| Kỹ thuật | GUID nội bộ, foreign-key ID, `RowVersion`, raw concurrency token | Không xuất hiện trong picker; audit route chỉ ngoại lệ cho correlation/resource identifier có ý nghĩa điều tra |

Trigger picker đề xuất hiển thị `Cột 6/8`. Popup tiếp tục ghi rõ `Đang hiện 6/8`, có tìm kiếm và reset. Con số không bao gồm `#`, checkbox chọn dòng hoặc `Thao tác`.

### 2.2 Quy tắc `Tên + mã`

| Ngữ cảnh | Cách hiển thị | Lý do |
|---|---|---|
| Giao dịch/đọc nhanh: đơn hàng, lịch sử, chốt kỳ, drawer chi tiết | Một ô hai dòng: tên chính, mã phụ | Giữ bảng gọn; người dùng chủ yếu quét theo tên |
| Quản trị dữ liệu: danh mục, mặt hàng, phòng ban, bảng giá, giá mặt hàng, lookup | Tách `Tên` và `Mã` thành hai cột | Mã là khóa tra cứu/import/sort và có thể cần copy riêng |
| Metadata hỗ trợ: tên NCC + tên viết tắt, người dùng + tên đăng nhập | Chỉ gộp hai dòng khi dòng phụ không có filter/sort/thao tác riêng | Tránh tạo thêm cột không cần thiết |
| Audit/security | Nhãn thân thiện ở dòng chính, mã/raw action ở dòng phụ | Hữu ích để điều tra nhưng không lấn át nội dung chính |

Density đi kèm:

- màn quản trị đã tách `Tên/Mã`: dùng `Compact`;
- màn giao dịch có ô hai dòng: dùng `RichTwoLine`;
- không dùng ô hai dòng chỉ để lấp khoảng trống.

### 2.3 Thứ tự chuẩn

1. `#` hoặc chọn dòng.
2. Nhận diện chính: `Tên`, sau đó `Mã` nếu tách cột.
3. Phân loại/quan hệ: loại, danh mục, nhà cung cấp, phòng ban.
4. Trạng thái.
5. Giá trị định lượng: số lượng, đơn giá, VAT, thành tiền.
6. Mô tả/audit.
7. `Thao tác` cố định bên phải.

Toolbar phải theo đúng thứ tự các cột có thể lọc. Column picker là công cụ hiển thị, không thay vai trò của filter.

## 3. Kiểm kê 11 column picker hiện có

| Route/component | Hiện trạng chính | Điều chỉnh đề xuất |
|---|---|---|
| Category | Tên + mã đang chung ô; `#` có thể pick | Tách `Tên danh mục`/`Mã danh mục`; khóa `#`; thêm cập nhật gần nhất dạng tùy chọn |
| Department | Tên + mã đang chung ô; ít cột phụ | Tách `Tên phòng ban`/`Mã phòng ban`; khóa `#`; cân nhắc mô tả/ngày cập nhật |
| Item | Tên + mã đang chung ô; thiếu một số thông tin quản trị | Tách `Tên mặt hàng`/`Mã mặt hàng`; bổ sung VAT mặc định, số NCC, mô tả, ngày cập nhật nếu DTO hỗ trợ |
| Lookup category | Có ID và user ID kỹ thuật trong picker | Giữ mã/tên riêng; bỏ ID kỹ thuật; dùng tên người tạo/cập nhật thay numeric ID |
| Lookup value | Có ID/FK/user ID kỹ thuật trong picker | Bỏ ID/FK kỹ thuật; chỉ thêm `ExtraField` khi có nhãn nghiệp vụ cụ thể, không lộ tên generic |
| Price list | Tên + mã chung ô; `Mã bảng giá` đang ẩn/pickable; trigger `Cột 6` gây hiểu nhầm | Tách `Tên bảng giá`/`Mã bảng giá`; giữ mô tả/ngày cập nhật tùy chọn; không đưa version, hiệu lực, tiền tệ, hợp đồng, phí thương mại đã hoãn vào UI |
| Item price | Mặt hàng + mã chung ô; có SKU/giá/VAT/MOQ/ngày giao | Tách tên/mã mặt hàng trong màn quản trị; khóa `#`; `NetPrice` là quyết định nghiệp vụ riêng, không tự thêm chỉ vì DTO có field |
| Supplier | Tên + tên viết tắt và thành phố + phường đang gộp; phường còn có cột tùy chọn riêng | Giữ tên + tên viết tắt nếu không cần filter riêng; loại nội dung phường bị lặp; địa chỉ/audit là cột tùy chọn |
| Permission group | Tên nhóm + mã nhóm chung ô | Tách khi mã nhóm là khóa quản trị; khóa `#`; không lộ permission ID kỹ thuật |
| Security audit | Nhiều ô hai dòng có chủ đích; có correlation ID | Giữ hai dòng; correlation/resource ID là ngoại lệ hợp lệ; kiểm tra label thân thiện và visibility mặc định |
| User | Tên + login, email + mã nhân viên; picker có `UserId`, `RowVersion` | Bỏ `UserId`, `RowVersion`; giữ tên + login; tách mã nhân viên thành cột tùy chọn nếu cần lọc/export; thêm ngày tạo/cập nhật nếu hữu ích |

## 4. Kiểm kê các grid không có picker

C3 không tự thêm picker vào mọi bảng. Mỗi grid được phân loại:

- `NoPicker`: bảng ít cột, chi tiết đơn, list lựa chọn hoặc workflow cần bố cục cố định;
- `OptionalPicker`: bảng quản trị/tra cứu có nhiều hơn khoảng 6 cột nghiệp vụ hoặc có cột phụ hữu ích;
- `RequiredPicker`: bảng có nhiều cột, responsive phải ẩn bớt hoặc người dùng cần tùy biến theo công việc.

Mỗi route phải có một record gồm: cột nguồn, label, thứ tự, visible mặc định, pickable, sortable, filterable, responsive priority và lý do. Không suy ra cột UI bằng cách đưa toàn bộ property DTO lên màn hình.

## 5. Kiểm thử và bằng chứng

### Architecture tests

- mọi cột `#`, checkbox chọn dòng và `Thao tác` phải `Pickable="false"`;
- `RowVersion`, raw GUID/FK ID và concurrency token không được có trong picker ngoài allow-list audit;
- trigger và popup dùng cùng `visible/total`;
- filter theo cùng thứ tự với cột tương ứng;
- route admin đã xác định mã là khóa quản trị phải có cột mã riêng;
- label picker phải là tiếng Việt thân thiện, không fallback tên property kỹ thuật.

### Route-real QA

- mở cả 11 picker trên route đã đăng nhập, xác nhận đủ cột và reset đúng;
- kiểm tra desktop 1366px, tablet 768px và mobile 390px cho route đại diện;
- kiểm tra light/dark, tiếng Việt/tiếng Anh, focus keyboard và popup không vượt viewport;
- kiểm tra ẩn/hiện cột không làm header, row, footer hoặc frozen action lệch nhau;
- chụp evidence picker của `Bảng giá`, `Mặt hàng`, `Người dùng`, `Security audit` trước khi owner duyệt rollout toàn bộ.

## 6. Rủi ro và approval gate

- Tách `Tên/Mã` có thể làm bảng rộng hơn; phải kết hợp default visibility và responsive priority, không tăng horizontal scroll mù quáng.
- Không thêm field chỉ vì DTO có sẵn; field phải hỗ trợ tra cứu, quyết định hoặc audit thực tế.
- `NetPrice`, trường thương mại/hợp đồng và lookup `ExtraField` cần quyết định nghiệp vụ riêng trước khi đưa lên UI.
- C0 chỉ là tài liệu. Chỉ bắt đầu C1–C4 sau khi owner duyệt contract này.

