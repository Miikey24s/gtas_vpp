# Changelog checkpoint 03B – Use case chi tiết

## Phạm vi

- Hoàn thiện mục 3.2.1 của Chương 3.
- Cập nhật danh mục hình cho Hình 3-5 đến Hình 3-14.
- Không chỉnh sửa source code, API hoặc cơ sở dữ liệu của hệ thống.

## Nội dung đã thực hiện

- Vẽ lại 10 sơ đồ use case chi tiết bằng PlantUML, gồm: đăng nhập; tạo đơn thường; sửa/hủy đơn; tạo đơn bổ sung; duyệt đơn bổ sung; quản lý danh mục; quản lý bảng giá; đóng kỳ; quản lý người dùng và phân quyền; dashboard và dữ liệu tổng hợp.
- Lưu đồng thời `.puml`, `.svg` và `.png` tại `LVTN/diagrams/ch03/`; Word ưu tiên SVG và có PNG dự phòng.
- Chuẩn hóa toàn bộ 10 bảng đặc tả use case theo cùng bố cục, cỡ chữ và độ rộng cột.
- Hiệu chỉnh nội dung theo source: điều kiện ngày 5; một đơn thường trong kỳ; tối đa ba đơn bổ sung; trạng thái Submitted/Pending/Approved/Rejected/Cancelled; soft delete; log thao tác; kiểm tra giá và transaction khi đóng kỳ; quyền page/component và kiểm tra vòng lặp nhóm quyền.
- Đổi tên use case cuối thành “xem dashboard và dữ liệu tổng hợp”. Trang Report/xuất file không được mô tả là chức năng đã hoàn thiện.
- Giữ sơ đồ và caption cùng trang, chèn hình ở chế độ inline và tối ưu kích thước để đọc được khi in đen trắng.

## Kiểm tra

- PlantUML 1.2026.6 chạy trên Java 21.0.11; 10/10 file `.puml` qua `-checkonly` và render SVG/PNG thành công.
- DOCX render thành 55 trang; đã kiểm tra toàn bộ contact sheet và kiểm tra trực tiếp các trang thay đổi 5, 37–47.
- 17 hình inline và 1 đối tượng anchor có sẵn; 17 SVG được nhúng, không có image relationship mồ côi.
- 34 caption hình xuất hiện đúng hai lần (nội dung và danh mục hình); Hình 3-5 đến Hình 3-14 khớp nội dung mới.
- Không có Track Changes hoặc comment trong file bàn giao.
- Checkpoint và working copy có cùng SHA-256: `4832e7a071e65f8c437a7cf8c84de53c6c1d9a48c255bdae330d8f5df14f0f60`.

## Tệp bàn giao

- `LVTN/checkpoints/03B_chuong3_usecase_chitiet.docx`
- `LVTN/NguyenAnNam_DH52201078_working.docx`

