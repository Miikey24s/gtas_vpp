# Changelog checkpoint 02C – Chương 2: sơ đồ tổng quát

## Phạm vi

- Cập nhật **Hình 2-1 Kiến trúc tổng thể**, mục **2.3.2 Sơ đồ chức năng**, **2.3.3 Sơ đồ Use case tổng quát** và bảng mô tả actor.
- Không sửa source code, API hoặc cơ sở dữ liệu.
- Áp dụng quy ước đen–trắng cho toàn bộ sơ đồ: nền trắng, chữ/viền đen, xám nhạt để phân vùng; không dùng màu làm tín hiệu duy nhất.

## Sơ đồ đã tạo

- `functional-decomposition.puml`: lưu cấu trúc ngữ nghĩa của sơ đồ chức năng để tiếp tục chỉnh nội dung.
- `functional-decomposition-layout.py`: sinh bố cục vector cố định theo đúng phong cách hình mẫu: một ô hệ thống ở trên, một trục ngang, các ô nhóm cùng một hàng, mỗi nhóm có trục dọc riêng và các mũi tên ngắn đi vào từng ô chức năng.
- `functional-decomposition.svg` và `functional-decomposition.png`: Hình 2-2 đã chèn vào Word. Sơ đồ hiện có 5 cột theo 5 nhóm chức năng thực tế; số ô con của từng cột phụ thuộc số chức năng, không bị cố định.
- `architecture-overview-layout.py`: sinh lại Hình 2-1 theo luồng trái–phải từ người dùng, Nginx, frontend/backend đến SQL Server; luồng migrator được biểu diễn bằng nét đứt và có chú giải.
- `use-case-overview-layout.py`: sinh lại Hình 2-3 theo bố cục UML hai phía. Nhân viên và Quản lý phòng ban đặt bên trái; Quản trị viên và Xử lý tự động đặt bên phải; các use case nằm trong hai cột cân đối và dùng liên kết trực tiếp thay cho các trục dây dùng chung.
- `use-case-overview.puml` và `use-case-overview.svg`: use case tổng quát với 4 actor là Nhân viên, Quản lý phòng ban, Quản trị viên và Xử lý tự động.
- Mỗi sơ đồ có PNG dự phòng trong gói Word để tương thích với trình đọc không hỗ trợ SVG.
- Cả 3 file PlantUML Chương 2, gồm `architecture-overview.puml`, đều qua kiểm tra cú pháp local.

## Nội dung và bảng actor

- Chuẩn hóa tên actor và phạm vi use case theo source hiện tại.
- Gom các xử lý tính kỳ, kiểm tra ràng buộc, chụp giá và ghi log thành một use case nội bộ ở sơ đồ tổng quát; các quan hệ chi tiết được dành cho Chương 3.
- Đổi tiêu đề cột `Tác nhân/nhóm xử lý` thành `Tác nhân`, khắc phục lỗi xuống dòng giữa từ.
- Viết lại mô tả 4 actor để phân biệt vai trò nghiệp vụ với quyền thực thi thực tế.
- Bổ sung ghi chú: quyền hiển thị được cấu hình ở frontend; một số API quản trị hiện mới yêu cầu đăng nhập và chưa cưỡng chế đầy đủ policy chi tiết ở backend.

## Bố cục

- Hình 2-1 dùng chiều rộng 6,20 inch, bố cục ngang và nằm gọn cùng caption ở trang vật lý 12.
- Hình 2-2 dùng chiều rộng 6,20 inch và nằm gọn cùng caption ở trang vật lý 27.
- Hình 2-3 dùng chiều rộng 6,20 inch, nằm trên trang riêng; bảng actor được giữ trọn ở trang kế tiếp, không tách hàng.
- Bảng actor dùng độ rộng cột cố định `1800 / 3000 / 4272 DXA`, hàng tiêu đề lặp và mọi hàng có `cantSplit`.
- Bản render cuối có 48 trang. Lần chỉnh riêng Hình 2-3 này chỉ làm thay đổi trang vật lý 28; 47 trang còn lại có ảnh render trùng hoàn toàn với bản đã duyệt trước đó.

## Kiểm tra

- Gói DOCX hợp lệ: 64 entry không lỗi, 8 section, 960 đoạn XML, 18 bảng, 6 hình inline và 1 đối tượng neo có sẵn.
- Có 3 SVG trong gói Word và 3 tham chiếu `svgBlip` tương ứng.
- Giữ nguyên 96 hyperlink trong nội dung và 14 liên kết ngoài.
- Không có Track Changes hoặc comments.
- Đã kiểm tra trực quan trang 28 ở độ phân giải gốc; không có chữ/hình bị cắt, đường nối chồng chữ hoặc caption tách khỏi hình. 47 trang còn lại khớp từng ảnh với bản render đã duyệt trước đó.
- Checkpoint và working copy có cùng SHA-256: `3d0a419823a66d68e186dcec434a61d78de1f6f752f91257981cef620cd85ede`.
