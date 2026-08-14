# UI-COLUMN-CONTRACT-001 — Chuẩn hóa cột, bộ chọn cột và ô nhận diện

> Trạng thái: `C0–C6 IMPLEMENTED — OWNER VISUAL REVIEW`
> Authority cha: [`UI-SYSTEM-001`](./UI-SYSTEM-001.md), [`UI-DATA-SURFACE-001`](./UI-DATA-SURFACE-001.md) và [`VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN`](../design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md).
> Phạm vi: frontend Blazor/Radzen, shared DTO và các projection API phục vụ cột quản trị; không đổi schema database hoặc nghiệp vụ ghi dữ liệu.

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
| C0 — Khóa contract | Chốt phân loại cột, quy tắc hai dòng và danh sách route | Một chuẩn dùng chung trước khi sửa code | `DONE` |
| C1 — Sửa foundation | Làm rõ trigger `Cột đang hiện/tổng`; cố định `#`, `Thao tác`; loại trường kỹ thuật | Bộ chọn cột không còn gây hiểu nhầm | `DONE` — unit/architecture pass |
| C2 — Chuẩn hóa Library/Admin | Tách hoặc gộp `Tên/Mã` đúng ngữ cảnh; bổ sung các cột nghiệp vụ đang thiếu | Các trang quản trị đồng nhất và dễ tra cứu | `DONE` — 11/11 picker route-real pass |
| C3 — Chuẩn hóa toàn frontend | Rà các grid không có picker, thứ tự filter–cột, tên cột và responsive | Không còn mỗi trang dùng một quy tắc riêng | `DONE` — 21 file/26 grid audited |
| C4 — Khóa bằng test/docs | Thêm contract test và cập nhật design system | Sửa sau này không làm drift trở lại | `IMPLEMENTED` — chờ owner nhìn route thật |
| C5 — ID và nhãn ngày | Thêm GUID của chính bản ghi vào picker nhưng ẩn mặc định; chuẩn hóa nhãn ngày tạo/cập nhật | Quản trị viên có thể tra cứu ID khi cần mà grid mặc định vẫn gọn | `IMPLEMENTED` — chờ owner duyệt cột nào nên bật mặc định |
| C6 — Cột quyết định và hoạt động | Bổ sung số bản ghi liên quan, lần nhập/đăng nhập gần nhất và resource ID từ dữ liệu hiện có | Grid mặc định ưu tiên thông tin giúp quản trị viên ra quyết định; metadata ít dùng vẫn nằm trong picker | `IMPLEMENTED` — backend/frontend tests pass |

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
| ID của bản ghi | `Id`; riêng giá mặt hàng dùng `PriceMappingId` | Chỉ có ở màn quản trị, tên `ID`, luôn ẩn mặc định nhưng được phép bật từ picker |
| Kỹ thuật quan hệ/đồng thời | Foreign-key ID, numeric `UserId`, `RowVersion`, raw concurrency token | Không xuất hiện trong picker; audit route chỉ ngoại lệ cho correlation/resource identifier có ý nghĩa điều tra |

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
- Mỗi picker quản trị có đúng một `ID` của chính bản ghi, `Visible="false" Pickable="true"`;
- `RowVersion`, foreign-key ID, numeric `UserId` và concurrency token không được có trong picker ngoài allow-list audit;
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
- Owner visual review vẫn là gate cuối trước khi đổi trạng thái thành accepted.

## 7. Kết quả triển khai — 2026-08-14

- `VppColumnPicker` hiển thị `đang hiện/tổng`, ví dụ `6/8`; trigger và accessibility label dùng cùng một số liệu.
- Dòng đã chọn trong popup không còn phủ nền xanh; trạng thái được thể hiện bằng checkbox, hover vẫn dùng transient surface chung.
- Toàn bộ 11 picker loại `#`, `Thao tác`, khóa ngoại và concurrency token khỏi danh sách lựa chọn.
- Category, Department, Item, Price list, Item price và Permission group đã tách `Tên`/`Mã`; User, Supplier và Security audit giữ hai dòng đúng vai trò metadata.
- Bổ sung các cột nghiệp vụ/audit hợp lệ: mã, VAT mặc định, số nhà cung cấp, mô tả, người/ngày tạo và cập nhật nơi DTO hỗ trợ.
- Owner feedback ngày 2026-08-14: `Tạo lúc`/`Cập nhật lúc` đổi thành `Ngày tạo`/`Ngày cập nhật`; cả 11 picker quản trị có `ID` (GUID) nhưng ẩn mặc định. Đây là ID của chính bản ghi, không mở lại các khóa ngoại hoặc concurrency token đã loại bỏ.
- Owner feedback tiếp theo ngày 2026-08-14: bật mặc định các số liệu quyết định gồm `Số giá trị`, `Số mặt hàng`, `Số nhà cung cấp`, `Số người dùng`, `Số quyền` và `Ngày cập nhật` của bảng giá; giữ `Số bảng giá`, `Lần nhập gần nhất`, `Lần đăng nhập gần nhất`, `Ngày cập nhật` của giá mặt hàng và `ID tài nguyên` ở trạng thái ẩn/pickable.
- Nhà cung cấp hiển thị một cột `Địa chỉ` đã ghép; `Thành phố`, `Phường/Xã`, `Địa chỉ 2`, `Địa chỉ 3` vẫn có thể bật riêng từ picker. Người dùng hiển thị mã nhân viên ở dòng nhận diện phụ nhưng vẫn giữ cột `Mã nhân viên` riêng trong picker.
- Dữ liệu mới lấy từ bảng và quan hệ hiện có; không tạo migration: lookup value, mặt hàng, ánh xạ giá, bảng giá/lần nhập, membership, lần đăng nhập và permission mapping.
- Quét 21 file chứa 26 `RadzenDataGrid`: không còn hard-coded English title; các ô hai dòng còn lại thuộc allow-list giao dịch, identity hỗ trợ hoặc audit.

### Bằng chứng

- Frontend unit hiện tại: `446/446` pass; trong đó contract C6 khóa các cột tổng hợp/hoạt động đã duyệt và trạng thái visible/hidden của metadata.
- Backend unit hiện tại: `550/550` pass; có test projection cho số mặt hàng/bảng giá của NCC, số NCC của mặt hàng, lần nhập gần nhất, ngày cập nhật giá, lần đăng nhập và số quyền.
- `ColumnPickerContractTests.AllAdminColumnPickers_ExposeBusinessFieldsWithStableChrome`: pass; mở đủ `11/11` picker, xác nhận mỗi picker có đúng một `ID` ẩn mặc định và spot-check `390×844`, `768×1024`, `1366×768`, `1920×1080`.
- `PricingAndReportMotifTests.PricingAndReports_KeepCanonicalContractsAcrossResponsiveViewports`: final pass; ảnh thật xác nhận bảng giá dùng cuộn ngang nội bộ ở laptop thay vì ép ngắn các tiêu đề `Trạng thái`, `Mặc định`, `Số mặt hàng`.
- `PricingAndReportMotifTests.PriceListColumnPicker_ShowsBusinessColumnsAndHidesTechnicalFields`: pass.
- `LibraryGridScrollTests.LookupColumnPicker_TogglesAndResetsWithVisibleTotalCount`: pass.
- Test rộng `Class_Definitions_Use_Compact_Master_Detail_Layout` còn fail ở assertion document scroll `290px` trước khi đi tới picker. Đây là layout issue độc lập, không được che bằng cách hạ assertion trong slice này.
- Test rộng `PermissionAdministration_UsesFullWidthGroupTableAndAdaptiveBatchEditor` đi qua UI mới nhưng dừng ở fixture count cũ `18`, trong khi TEST hiện có `19` dòng quyền API; không sửa assertion theo dữ liệu tạm trong slice này.
- `./scripts/gtas.cmd verify -Scope frontend`: agent setup `63/63`, Release build `0 warning/error`, frontend unit `445/445`, anonymous UI smoke `2/2` và NuGet audit pass; gate cuối dừng tại lỗi charset có sẵn ở migration backend `20260810233259_AddPriceListImportBatches.cs`, ngoài frontend scope.

## 8. Cột đang ẩn để owner duyệt — 2026-08-14

Tất cả cột dưới đây vẫn có trong picker nhưng không làm rộng grid mặc định. `ID` luôn giữ ẩn mặc định; các cột còn lại chỉ bật mặc định sau khi owner duyệt theo nhu cầu tra cứu thực tế.

| Màn quản trị | Cột đang ẩn mặc định |
|---|---|
| Loại danh mục | `ID`, `Mô-đun`, `Mô tả`, `Người tạo`, `Ngày tạo`, `Người cập nhật`, `Ngày cập nhật` |
| Giá trị danh mục | `ID`, `Mô tả`, `Người tạo`, `Ngày tạo`, `Người cập nhật`, `Ngày cập nhật` |
| Danh mục | `ID`, `Ngày tạo`, `Ngày cập nhật` |
| Mặt hàng | `ID`, `VAT mặc định`, `Mô tả`, `Ngày tạo`, `Ngày cập nhật` |
| Nhà cung cấp | `ID`, `Số bảng giá`, `Thành phố`, `Phường/Xã`, `Địa chỉ 2`, `Địa chỉ 3`, `Mô tả`, `Ngày tạo`, `Ngày cập nhật` |
| Phòng ban | `ID`, `Ngày tạo`, `Ngày cập nhật` |
| Bảng giá | `ID`, `Lần nhập gần nhất`, `Mô tả`, `Người tạo`, `Ngày tạo`, `Người cập nhật` |
| Giá mặt hàng | `ID` (ID ánh xạ giá), `Mô tả`, `Ngày cập nhật` |
| Người dùng | `ID`, `Mã nhân viên`, `Lần đăng nhập gần nhất`, `Mô tả`, `Người tạo`, `Ngày tạo`, `Người cập nhật`, `Ngày cập nhật` |
| Nhóm quyền | `ID`, `Người tạo`, `Ngày tạo`, `Người cập nhật`, `Ngày cập nhật` |
| Nhật ký bảo mật | `ID`, `ID tài nguyên`, `Lý do`, `Mã tương quan` |
