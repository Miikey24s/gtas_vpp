# BACKEND-REFACTOR-001 — Checklist kiểm tra chức năng mới

Mục đích: owner kiểm tra một lượt các chức năng mới trước khi mở tiếp B3–B5. Đây là kiểm tra hành vi người dùng,
không phải checklist kỹ thuật nội bộ.

## Cách ghi kết quả

Ghi một trong bốn nhãn vào cột **Kết quả**:

- `PASS`: đúng mong đợi;
- `BUG`: thao tác hoặc dữ liệu sai;
- `UI`: nghiệp vụ đúng nhưng cách hiển thị/điều khiển chưa ổn;
- `BUSINESS`: cần chốt lại quy tắc nghiệp vụ.

Nếu có lỗi, ghi ngắn trang, kỳ/đơn/bảng giá đã dùng và ảnh chụp nếu lỗi liên quan giao diện.

## A. Kỳ đặt hàng và đơn hàng

| ID | Thao tác kiểm tra | Kết quả mong đợi | Kết quả | Ghi chú |
|---|---|---|---|---|
| P01 | Mở trang **Các kỳ đặt hàng** | Kỳ mới xếp trước kỳ cũ; trạng thái, ngày mở, ngày đóng và số đơn đúng |  |  |
| P02 | Nhân viên chọn một trong nhiều kỳ đang mở và tạo đơn | Đơn được lưu đúng kỳ đã chọn, không tự rơi về kỳ mặc định khác |  |  |
| P03 | Sửa lịch hoặc gia hạn một kỳ đang mở | Lịch mới được lưu; dữ liệu của kỳ khác không đổi |  |  |
| P04 | Thử ngày cuối tháng, tháng 2 và năm nhuận | Ngày hợp lệ theo lịch thật; không sinh ngày không tồn tại |  |  |
| P05 | Kỳ đến ngày đóng | Không nhận thêm đơn thường; đơn đã gửi vẫn xem được |  |  |
| P06 | Tạo/gửi đơn bổ sung sau ngày đóng nhưng còn trong hạn | Đơn bổ sung đi đúng luồng duyệt, không mở lại đơn thường |  |  |
| P07 | Thử đơn bổ sung sau khi hết hạn | Hệ thống từ chối rõ ràng, không lưu nửa chừng |  |  |
| P08 | Quản lý sửa/hủy đơn khi kỳ đã chốt theo đúng luồng cho phép | Có lý do, thông báo và lịch sử; nhân viên không có quyền tương tự |  |  |

## B. Chốt kỳ và hiệu chỉnh sau chốt

| ID | Thao tác kiểm tra | Kết quả mong đợi | Kết quả | Ghi chú |
|---|---|---|---|---|
| S01 | Mở trang **Chốt kỳ** và chọn kỳ | Dữ liệu tải đúng kỳ; tổng theo phòng ban/người đặt/mặt hàng khớp nhau |  |  |
| S02 | Chọn nhà cung cấp/bảng giá hoặc dùng gợi ý | Đơn giá, VAT và tổng giá trị nhất quán; gợi ý dùng tối đa 2 NCC, mỗi mặt hàng thuộc trọn một NCC, chỉ tính giá + VAT và không tự áp dụng |  |  |
| S03 | Chốt kỳ sớm khi vẫn trong thời gian cho phép | Có cảnh báo dễ hiểu; kết quả chốt tạo bản đầu tiên và không làm sai hạn đơn bổ sung tính từ ngày đóng |  |  |
| S04 | Xem bản đã chốt | Số liệu là ảnh chụp tại lúc chốt; đổi bảng giá hiện tại không làm đổi bản cũ |  |  |
| S05 | Tạo yêu cầu hiệu chỉnh sau chốt | Bắt buộc có lý do; người tạo không thể tự xác nhận bước thứ hai |  |  |
| S06 | Quản lý thứ hai duyệt hiệu chỉnh | Tạo bản tiếp theo; bản cũ vẫn xem được; lịch sử hiển thị bằng câu chữ thân thiện |  |  |
| S07 | Từ chối hoặc hủy yêu cầu hiệu chỉnh | Bản đang áp dụng không đổi; lý do và trạng thái được lưu rõ |  |  |
| S08 | Xuất PDF/Excel ở bản đang áp dụng và bản cũ | File tải được, đúng kỳ/bản, tổng tiền khớp màn hình |  |  |

## C. Bảng giá và import

| ID | Thao tác kiểm tra | Kết quả mong đợi | Kết quả | Ghi chú |
|---|---|---|---|---|
| R01 | Tạo bảng giá mới | Bảng giá có hiệu lực ngay; phương thức mặc định là thủ công, không có bước nháp/công bố thừa |  |  |
| R02 | Tải file mẫu từ một bảng giá | File có sẵn mã hệ thống, tên mặt hàng, đơn vị và giá hiện tại để chỉ cần sửa giá |  |  |
| R03 | Cập nhật giá bằng Excel, để trống một vài ô giá | Preview hiển thị đúng thay đổi; ô trống giữ giá cũ, không biến thành 0 |  |  |
| R04 | File có mã lạ, sai đơn vị, dòng trùng hoặc giá không hợp lệ | Dòng lỗi được chỉ rõ và bị chặn; hệ thống không tự tạo mặt hàng mới hoặc import nửa chừng |  |  |
| R05 | File có tên cột khác mẫu | Có bước mapping/preview rõ; mapping thủ công vẫn dùng được, AI nếu bật chỉ hỗ trợ và không tự ghi dữ liệu |  |  |
| R06 | Xác nhận file hợp lệ | Số mặt hàng và giá đúng preview; lịch sử ghi nguồn Excel, người thực hiện và thời điểm |  |  |
| R07 | Đặt bảng giá mặc định, vô hiệu hóa và thử xóa | Action đúng trạng thái; dữ liệu đang được dùng không bị xóa sai |  |  |
| R08 | Dùng bảng giá vừa import ở trang Chốt kỳ | Danh sách chọn và tổng tiền dùng đúng dữ liệu mới |  |  |

## D. Hồi quy nhanh

| ID | Thao tác kiểm tra | Kết quả mong đợi | Kết quả | Ghi chú |
|---|---|---|---|---|
| G01 | Đăng nhập bằng nhân viên, quản lý và quản trị hệ thống | Mỗi vai trò thấy đúng trang và action được cấp |  |  |
| G02 | Tạo, sửa, gửi và xem lịch sử một đơn thường | Luồng cũ vẫn hoạt động, chi tiết và số lượng đúng |  |  |
| G03 | Mở báo cáo theo phạm vi cá nhân/phòng ban/toàn hệ thống | Scope không rò dữ liệu; tổng báo cáo khớp dữ liệu chốt |  |  |
| G04 | Xuất báo cáo PDF/XLSX/CSV | File tải được và nội dung tương ứng màn hình |  |  |
| G05 | Mở thông báo sau các thao tác kỳ/đơn/hiệu chỉnh | Thông báo đúng người, đúng nội dung, không nhân đôi bất thường |  |  |

## Cách mở khóa refactor tiếp theo

- `R01–R08` đạt: mở B3 Catalog & Pricing.
- `P01–P08` đạt: mở phần Period trước, sau đó Requests trong B4.
- `S01–S08` đạt: mở B5 Settlement & post-settlement correction.
- Một nhóm có `BUG` vẫn không chặn nhóm độc lập đã `PASS`; sửa bug bằng commit riêng rồi mới refactor nhóm đó.
