# Changelog checkpoint 99 – Đồng bộ luận văn với hệ thống ngày 13/07/2026

- Kết quả: `99_final.docx` gồm 77 trang vật lý; bản working đã được đồng bộ byte-for-byte.
- SHA-256 của bản final và working: `38085040886de12b41d3f640995c472dc7f35d0c895617b468f3881f7cbd784d`.
- Bản gốc `NguyenAnNam_DH52201078.docx` được giữ nguyên.
- Giữ đúng quyết định của người dùng: **không thêm page border cho trang bìa**.

## Nội dung đồng bộ theo source

- Cập nhật kiến trúc, sơ đồ chức năng, use case tổng quát, mô hình dữ liệu và các luồng phân quyền để phản ánh action policy ở backend, snapshot page/component ở frontend và thu hồi quyền gần thời gian thực qua SignalR.
- Mô hình dữ liệu vật lý hiện có 18 bảng, bổ sung `N01_Notification` cho hộp thư bền vững.
- Cập nhật phần báo cáo: KPI, biểu đồ theo phạm vi, top vật tư, xuất CSV và nhận định tùy chọn; không còn mô tả trang Report là giao diện chờ.
- Cập nhật phần thông báo: lưu cơ sở dữ liệu, số chưa đọc, đánh dấu đã đọc và phát theo thời gian thực.
- Ghi rõ nhận định OpenAI mặc định tắt, chỉ nhận số liệu tổng hợp, có rate limit và fallback theo quy tắc; không tham gia phê duyệt hoặc thay đổi dữ liệu nghiệp vụ.
- Bảng đặc tả tách rõ quyền hiển thị page/component và quyền action tại API cho duyệt/từ chối, danh mục, bảng giá, đóng kỳ và phân quyền.
- Chương 4 ghi nhận đúng mốc hiện tại: 147 backend test, 29 frontend test, 12 UI test được discovery, Release build 0 lỗi/0 cảnh báo và audit NuGet không phát hiện package dễ tổn thương. Snapshot UI 54/54 ngày 28/05/2026 được ghi riêng, không xem là lần chạy hiện tại.

## Sơ đồ và khả năng in

- Cập nhật 8 hình trọng yếu trong Word từ source `.puml`/`.svg`: kiến trúc, phân rã chức năng, use case tổng quát, mô hình ý niệm, ERD đơn–log–notification, use case phân quyền, sequence đăng nhập/quyền và activity phân quyền.
- Toàn bộ sơ đồ giữ phong cách đen trắng, nền trắng, nét rõ; sơ đồ phân rã chức năng dùng bố cục cây nhiều cột theo mẫu người dùng đã chốt.
- Tài liệu có 37 hình kỹ thuật/giao diện, 46 caption nội dung (37 hình và 9 bảng), 23 bảng Word.

## Định dạng, liên kết và điều hướng

- Khổ A4 dọc; lề trái 3 cm, các lề còn lại 2 cm ở 8 section.
- Font mặc định Times New Roman 13 pt; bảng 12 pt; heading và caption giữ đúng vai trò đã chuẩn hóa theo mẫu 2026.
- Mục lục gồm 62 liên kết nội bộ, phân cấp 16/14/13 pt; dòng `PHỤ LỤC` có chấm dẫn và số trang.
- Danh mục hình gồm 37 liên kết nội bộ, Times New Roman 13 pt, giãn dòng 1,15 và có số trang.
- Tổng cộng 149 hyperlink: 132 liên kết nội bộ và 17 URL ngoài; không có bookmark thiếu, target hỏng hoặc lỗi hiển thị liên kết.
- Tài liệu tham khảo có 18 mục; các nguồn OpenAI chính thức được bổ sung cho Responses API, Structured Outputs và hướng dẫn production.
- Không giữ Track Changes; test case TC-15/TC-16 không bị tách đôi giữa hai trang.

## Kiểm tra cuối

- DOCX hợp lệ, không có phần tử ZIP lỗi, page border, link hỏng hoặc caption sai mẫu.
- Render bằng Microsoft Word 16 và kiểm tra trực quan 77 trang ở kích thước đầy đủ cùng 7 contact sheet; không có trang trắng ngoài ý muốn, chữ/hình bị cắt hay bảng tràn lề.
- 37 hình nội dung ở chế độ inline; một hình neo duy nhất là khung trang trí của trang `LỜI CẢM ƠN`, không phải hình kỹ thuật.
