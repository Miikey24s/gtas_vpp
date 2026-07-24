# Đối chiếu hình giao diện GTAS VPP

Mốc đối chiếu: commit `5f24fda286799d4b9869c0d381b8042fbc2c2120`, ngày 25/07/2026. Hình hiện có là bằng chứng thiết kế hoặc ảnh chụp giao diện, không được dùng làm bằng chứng kiểm thử.

| Màn hình | Route/capability | Nguồn hình | Trạng thái khớp nghiệp vụ | Cần sửa/chụp lại | Vị trí dự kiến trong Word |
|---|---|---|---|---|---|
| Đăng nhập | `/Account/Login` | `ui-login.png` | Khớp luồng Identity cơ bản | Chụp lại sau khi sửa lỗi accessibility `document-title` | Mục 3.3 |
| Đơn hàng của tôi | `/dashboard?tab=0` | `ui-dashboard-my-orders.png` | Khớp tổng quan kỳ và đơn cá nhân | Chụp lại khi shell/header được chốt | Mục 3.3 |
| Lịch sử đơn | `/dashboard?tab=history` | `ui-order-history.png` | Khớp tra cứu revision và trạng thái | Chụp lại sau khi ổn định bảng chi tiết dài | Mục 3.3 |
| Tạo đơn | `/dashboard/order-create` | `ui-order-create.png` | Khớp luồng chọn văn phòng phẩm và rà soát | Bắt buộc chụp lại; E2E hiện không tìm thấy thành phần nhập số lượng | Mục 3.3 |
| Tổng hợp toàn công ty | `RequestViewAll` trên dashboard | `ui-all-orders-summary.png` | Khớp phạm vi công ty | Chụp lại nếu bố cục KPI hoặc bộ lọc thay đổi | Mục 3.3 |
| Vận hành kỳ | `PERIOD_SETTLE` | `ui-period-operations.png` | Khớp một phần quản lý kỳ | Chụp lại để thể hiện rõ chọn nhà cung cấp, bảng giá, preview, blocker và hiệu chỉnh | Mục 3.3 |
| Danh mục văn phòng phẩm | `/library` và capability danh mục | `ui-library-items.png` | Khớp chức năng tra cứu/quản trị chính | Chụp lại sau khi sửa catalog và sticky header | Mục 3.3 |
| Bảng giá | `/library` và capability bảng giá | `ui-price-lists.png` | Khớp một phần publish/effectivity | Chụp lại sau khi sửa khoảng cách sticky và xác nhận panel chi tiết | Mục 3.3 |
| Phân quyền | `/permission` | `ui-permission-groups.png` | Hình cũ chưa chắc khớp mô hình ba persona | Bắt buộc chụp lại với `EMPLOYEE`, `MANAGER`, `DEV` | Mục 3.3 |
| Báo cáo | `/report`, `REPORT_VIEW`, `REPORT_EXPORT` | Chưa có | Nghiệp vụ có trong backend/frontend | Cần ảnh mới; trước mắt chỉ mô tả, không tạo caption giả | Bổ sung sau Mục 3.4 |
| Hiệu chỉnh kết quả chốt kỳ | `SETTLEMENT_CORRECT` | Chưa có | Nghiệp vụ có trong backend | Cần thiết kế/chụp mới thể hiện lý do và nguyên tắc four-eyes | Bổ sung Mục 3.3 |
| Hộp thư thông báo | capability thông báo trên header | Chưa có ảnh riêng | Nghiệp vụ có inbox, đọc/chưa đọc và route đích | Cần ảnh mới cho trạng thái loading, lỗi, rỗng và retry | Bổ sung Mục 3.3 |

## Kết luận

Chín hình hiện có được giữ với caption “Thiết kế giao diện” để tránh khẳng định quá mức về trạng thái hiện thực. Ba màn hình chưa có ảnh đạt yêu cầu chỉ được mô tả bằng văn bản và không được đánh số hình giả.
