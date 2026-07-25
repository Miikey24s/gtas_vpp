# Đối chiếu hình giao diện GTAS VPP

Mốc đối chiếu: HEAD hiện tại, ngày 25/07/2026. Không tìm thấy nguồn Atlas/Figma riêng trong repository; các hình hiện có nằm tại `LVTN/screenshots/ch03` và chỉ được dùng như hình thiết kế giao diện.

| Màn hình | Đường dẫn/chức năng | Nguồn hình | Trạng thái khớp nghiệp vụ | Cần sửa/chụp lại | Vị trí dự kiến trong Word |
|---|---|---|---|---|---|
| Đăng nhập | `/Account/Login` | `ui-login.png` | Khớp luồng Identity cơ bản | Chụp lại sau khi frontend ổn định hoàn toàn | Mục 3.3.1 |
| Đơn hàng của tôi | `/dashboard?tab=0` | `ui-dashboard-my-orders.png` | Khớp tổng quan kỳ và đơn cá nhân | Chụp lại nếu shell/header thay đổi | Mục 3.3.1 |
| Lịch sử đơn | `/dashboard?tab=history` | `ui-order-history.png` | Khớp tra cứu phiên bản và trạng thái | Chụp lại sau khi ổn định bảng chi tiết dài | Mục 3.3.2 |
| Tạo đơn | `/dashboard/order-create` | `ui-order-create.png` | Khớp luồng thiết kế chọn văn phòng phẩm và rà soát | Bắt buộc chụp lại khi kiểm thử trình duyệt ổn định | Mục 3.3.2 |
| Vận hành kỳ/đơn bổ sung | `PERIOD_SETTLE`, `REQUEST_APPROVE` | `ui-period-operations.png` | Khớp một phần bản xem trước, điều kiện ngăn chốt kỳ và hàng chờ | Chụp lại để thể hiện rõ chọn nhà cung cấp/bảng giá và hiệu chỉnh | Mục 3.3.2 |
| Bảng giá | `/library` và chức năng bảng giá | `ui-price-lists.png` | Khớp một phần công bố và thời gian hiệu lực | Chụp lại khi vùng chi tiết hoàn thiện | Mục 3.3.3 |
| Danh mục văn phòng phẩm | `/library` và chức năng danh mục | `ui-library-items.png` | Khớp chức năng tra cứu/quản trị chính | Chụp lại sau khi giao diện danh mục ổn định | Mục 3.3.3 |
| Phân quyền | `/permission` | `ui-permission-groups.png` | Khớp một phần mô hình nhóm quyền | Chụp lại với đủ `EMPLOYEE`, `MANAGER`, `Quản trị hệ thống (DEV)` | Mục 3.3.3 |
| Tổng hợp toàn công ty | `RequestViewAll` trên dashboard | `ui-all-orders-summary.png` | Khớp phạm vi công ty | Chụp lại nếu KPI/bộ lọc thay đổi | Mục 3.3.4 |
| Báo cáo | `/report`, `REPORT_VIEW`, `REPORT_EXPORT` | Chưa có | Nghiệp vụ có trong dịch vụ và giao diện người dùng | Cần ảnh mới; không tạo caption giả | Mục 3.3.4/3.4 |
| Hiệu chỉnh kết quả chốt kỳ | `SETTLEMENT_CORRECT` | Chưa có | Nghiệp vụ có trong dịch vụ phía máy chủ | Cần ảnh thể hiện lý do và nguyên tắc bốn mắt | Mục 3.3.2 |
| Hộp thư thông báo | hộp thư ở vùng đầu trang | Chưa có ảnh riêng | Có danh sách, trạng thái đọc/chưa đọc và màn hình chức năng đích | Cần ảnh trạng thái tải, rỗng, lỗi và thử lại | Mục 3.3.4 |

## Kết luận

Chín hình hiện có được giữ hoặc chèn lại với caption “Thiết kế giao diện”. Ba màn hình chưa có ảnh đạt yêu cầu chỉ được mô tả bằng văn bản và không đánh số hình giả.
