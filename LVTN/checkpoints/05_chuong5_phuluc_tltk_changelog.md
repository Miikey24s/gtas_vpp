# Changelog checkpoint 05 – Chương 5, phụ lục và tài liệu tham khảo

- Nguồn: `04_chuong4_thunghiem.docx`.
- Kết quả: `05_chuong5_phuluc_tltk.docx` (72 trang vật lý).
- Bản làm việc `NguyenAnNam_DH52201078_working.docx` đã được đồng bộ và có cùng SHA-256: `ef51ac159578de29d566f0e7742bdef20869941dbcac6a93d367d9769450e24a`.

## Nội dung đã hoàn thiện

- Viết lại Chương 5 theo hướng đối chiếu trực tiếp kết quả với mục tiêu đề tài, phân biệt rõ “Đạt”, “Đạt một phần” và “Đạt ở mức đề tài”.
- Thay bảng đánh giá cũ bằng Bảng 5-1 gồm 7 mục tiêu/tiêu chí và bằng chứng tương ứng.
- Ghi trung thực các phần chưa hoàn thiện: trang `/report` và xuất file, giới hạn của bằng chứng UI, kiểm thử SQL Server/tải, cảnh báo phụ thuộc, tích hợp nhân sự–thông báo và quy trình duyệt mở rộng.
- Viết lại hướng phát triển theo các tồn đọng thực tế, tránh mô tả chức năng chưa triển khai như kết quả đã hoàn thành.
- Mở rộng Phụ lục A và B thành hướng dẫn thao tác ngắn cho nhân viên và quản trị viên; bổ sung các điều kiện nghiệp vụ quan trọng về kỳ, đơn bổ sung, phê duyệt và đóng kỳ.
- Chuẩn hóa 15 tài liệu tham khảo, bổ sung ngày truy cập 13/07/2026 và URL chính thức khi phù hợp.
- Tạo bookmark `ref_01` đến `ref_15`; chuyển 30 dấu trích dẫn trong nội dung/bảng thành liên kết nội bộ đến đúng tài liệu tham khảo.
- Tạo 14 liên kết ngoài cho URL tài liệu tham khảo. Dấu trích dẫn và URL dùng chữ đen, gạch chân để vẫn nhận biết được khi in đen trắng.
- Cập nhật mục lục và số trang sau khi hoàn thiện nội dung.

## Trang bị tác động trong bản render cuối

- Trang vật lý 3–4: mục lục và số trang các phần cuối.
- Trang vật lý 67–68: Chương 5.
- Trang vật lý 69–70: Phụ lục A và B.
- Trang vật lý 71–72: tài liệu tham khảo.

## Kiểm tra

- Render bằng Microsoft Word 16 và kiểm tra trực quan đủ 72/72 trang ở ảnh trang và contact sheet; không phát hiện chữ/hình bị cắt, bảng tràn lề, caption tách bất thường hoặc trang gần như trống do lỗi dàn trang.
- Tài liệu hợp lệ theo cấu trúc ZIP; bản checkpoint và bản working có cùng SHA-256.
- Giữ nguyên 8 section A4 dọc, lề trái 3 cm và các lề còn lại 2 cm.
- Có 23 bảng; Bảng 5-1 là bảng thứ 23, thay thế bảng đánh giá Chương 5 cũ. Bảng rộng 9000 DXA, tổng `tblGrid` và tổng `tcW` của từng hàng đều bằng 9000 DXA.
- Có đủ 15 bookmark tài liệu tham khảo, ID/tên không trùng và cặp bookmark start/end hợp lệ.
- Có đúng 30 liên kết trích dẫn nội bộ; mọi anchor đều tồn tại. Có 14 liên kết URL ngoài, không có quan hệ hyperlink hỏng hoặc mồ côi.
- Giữ nguyên 65 media gồm 37 PNG và 28 SVG; 37 ảnh inline và 1 ảnh anchor, không có media thiếu hoặc mồ côi.
- 37 mã caption hình vẫn xuất hiện đúng hai lần; caption Bảng 5-1 tồn tại.
- Không có Track Changes, comment hoặc `Error! Bookmark not defined.`.
- LibreOffice trên máy vẫn hỏng `bootstrap.ini`; không sửa thư mục cài đặt, dùng Word để cập nhật field/xuất PDF và Poppler để kiểm tra ảnh trang.

