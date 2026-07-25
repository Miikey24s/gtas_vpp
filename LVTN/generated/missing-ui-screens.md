# Đối chiếu hình giao diện GTAS VPP

Mốc đối chiếu: ngày 25/07/2026. Nội dung nghiệp vụ trong luận văn và backend hiện tại là nguồn xác định chức năng; Atlas được dùng để chuẩn hóa bố cục và hình minh họa thiết kế. Frontend cũ chỉ được tham khảo để xác định đường dẫn hoặc khả năng đã có, không được xem là bằng chứng nghiệp vụ mới nhất.

| Màn hình | Nguồn hình trong luận văn | Trạng thái | Ghi chú |
|---|---|---|---|
| Đăng nhập | Atlas `01-login.png` | Đã có | Thể hiện đăng nhập và tải quyền. |
| Bảng điều khiển cá nhân | Atlas `02-my-orders.png` | Đã có | Tổng quan kỳ và đơn của nhân viên. |
| Tạo đơn thông thường | Atlas `03-order-create.png` | Đã có | Chọn vật phẩm, nhập số lượng và rà soát trước khi gửi. |
| Lịch sử đơn | Atlas `04-history.png` | Đã có | Theo dõi trạng thái và các phiên bản của đơn. |
| Danh mục văn phòng phẩm | Atlas `05-catalog.png` | Đã có | Tra cứu vật phẩm đang được phép đặt. |
| Tổng hợp phòng ban | Atlas `06-department-summary.png` | Đã có | Phạm vi quản lý theo phòng ban. |
| Duyệt đơn bổ sung | Atlas `07-supplement-approval.png` | Đã có | Duyệt hoặc từ chối kèm lý do. |
| Rà soát kỳ | Atlas `08-period-review.png` | Đã có | Kiểm tra trạng thái kỳ và điều kiện xử lý. |
| Phân bổ nguồn cung | Atlas `09-supply-allocation.png` | Đã có | Thể hiện lựa chọn nhà cung cấp và dữ liệu phân bổ. |
| Xem trước, xác nhận và hiệu chỉnh kết quả chốt kỳ | Atlas `10-settlement-flow.png` | Đã có | Dùng cùng không gian nghiệp vụ; hiệu chỉnh tạo phiên bản mới, không ghi đè lịch sử. |
| Quản lý vật phẩm | Atlas `11-items.png` | Đã có | Quản lý danh mục, vật phẩm và đơn vị tính. |
| Quản lý bảng giá | Atlas `12-price-lists.png` | Đã có | Thể hiện bản nháp, công bố và hết hiệu lực. |
| Quản lý người dùng | Atlas `13-users.png` | Đã có | Quản trị tài khoản và phân công người dùng. |
| Quản lý quyền | Atlas `14-permissions.png` | Đã có | Ba vai trò: Nhân viên, Quản lý và Quản trị hệ thống. |
| Báo cáo | Atlas `15-reports.png` | Đã có | Minh họa tổng hợp và xuất báo cáo theo phạm vi quyền. |
| Hộp thư thông báo và trạng thái hệ thống | Atlas `16-system-states.png` | Đã có | Bao gồm đã đọc/chưa đọc, tải dữ liệu, rỗng, lỗi và thử lại. |

## Màn hình còn cần chụp runtime

Các hình trên là **thiết kế giao diện**, không phải bằng chứng kiểm thử. Khi frontend mới được triển khai đầy đủ theo Atlas, nên chụp lại các luồng trọng tâm trên hệ thống chạy thật: tạo đơn, duyệt đơn bổ sung, chốt kỳ, hiệu chỉnh kết quả chốt kỳ, phân quyền, báo cáo và hộp thư thông báo. Cho đến lúc đó, luận văn không gắn nhãn các hình Atlas là kết quả thử nghiệm.
