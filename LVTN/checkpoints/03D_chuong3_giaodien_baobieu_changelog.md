# Changelog checkpoint 03D – Giao diện và báo biểu

- Nguồn: `03C_chuong3_sequence_activity.docx`.
- Kết quả: `03D_chuong3_giaodien_baobieu.docx` (68 trang vật lý).
- Bản làm việc `NguyenAnNam_DH52201078_working.docx` đã được đồng bộ và có cùng SHA-256: `8c2aa3506d36279fb06d16bb21e1ca5035785b2364563d8385b509f12fb8f965`.

## Nội dung đã hoàn thiện

- Thay 9 vị trí hình trống bằng ảnh giao diện thật, đặt inline, canh giữa, có viền xám mảnh và caption liền hình:
  - Hình 3-26: đăng nhập hệ thống.
  - Hình 3-27: dashboard đơn hàng cá nhân.
  - Hình 3-28: danh sách đơn yêu cầu.
  - Hình 3-29: tạo đơn yêu cầu văn phòng phẩm.
  - Hình 3-30: thao tác kỳ và đơn bổ sung chờ duyệt.
  - Hình 3-31: quản lý bảng giá.
  - Hình 3-32: quản lý danh mục mặt hàng.
  - Hình 3-33: phân quyền theo nhóm.
  - Hình 3-34: tổng hợp toàn doanh nghiệp theo kỳ.
- Viết lại mô tả ngắn cho từng màn hình theo chức năng thực tế trong source.
- Đổi mục 3.3.4 thành “Giao diện tổng hợp và báo biểu”. Ghi rõ `/report` hiện chỉ là khung chưa cấu hình, nút xuất bị vô hiệu hóa; dùng màn hình tổng hợp toàn doanh nghiệp đang hoạt động làm kết quả thực tế.
- Bổ sung ghi chú nguồn ảnh: ảnh đăng nhập chụp từ frontend hiện tại ngày 13/07/2026; tám ảnh còn lại lấy từ audit SERVER TEST ngày 28/05/2026; dữ liệu hiển thị là dữ liệu kiểm thử.
- Cập nhật mục lục bằng Microsoft Word, sửa lỗi `Error! Bookmark not defined.` và bật hyperlink nội bộ cho các mục.

## Trang bị tác động trong bản render cuối

- Trang vật lý 4: mục lục cập nhật và sửa liên kết lỗi.
- Trang vật lý 6: tên Hình 3-27, Hình 3-30 và Hình 3-34 trong mục lục hình.
- Trang vật lý 57–62: phần 3.3, 9 ảnh giao diện, mô tả và phần báo biểu.

## Kiểm tra

- Render bằng Microsoft Word 16 và kiểm tra trực quan đủ 68/68 trang; không có ảnh tràn lề, caption tách hình, trang trắng bất thường hoặc bảng vỡ mới.
- Kiểm tra cấu trúc: 8 section A4 dọc, lề trái 3 cm và các lề còn lại 2 cm; heading theo Heading 1–4.
- 37 ảnh inline và 1 ảnh anchor có sẵn ở trang lời cảm ơn; 65 media hợp lệ gồm 37 PNG và 28 SVG, không có media mồ côi.
- 37 mã caption hình (Hình 2-1 đến 2-3 và Hình 3-1 đến 3-34) đều xuất hiện đúng hai lần ở danh mục và nội dung.
- Không có Track Changes, comment, media thiếu hoặc lỗi ZIP.
- Lỗi LibreOffice khi render không thuộc file Word: `bootstrap.ini` của bản cài đặt có giá trị chưa được thay thế `InstallMode=<installmode>`. Không sửa thư mục `Program Files`; dùng Word để QA checkpoint.
