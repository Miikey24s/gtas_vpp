# Changelog checkpoint 02A - Chương 2: khảo sát và công nghệ

Ngày hoàn thiện: 11/07/2026

## Phạm vi đã xử lý

- Hoàn thiện mục 2.1 "Các hệ thống tương tự" theo bốn nhóm giải pháp: biểu mẫu/email, bảng tính dùng chung, nền tảng mua sắm tổng quát và nền tảng quản lý yêu cầu/quy trình.
- Viết lại bảng so sánh theo mức độ phù hợp với nghiệp vụ GTAS VPP; không đánh giá sản phẩm theo hướng quảng cáo hoặc khẳng định vượt quá tài liệu chính thức.
- Hoàn thiện mục 2.2 "Công nghệ sử dụng" theo cấu hình và package thực tế của source: .NET 10, ASP.NET Core Web API, Blazor Server, Radzen, SQL Server, EF Core, JWT/cookie, Mapster, Serilog, Docker Compose và Nginx.
- Vẽ lại Hình 2-1 bằng PlantUML, xuất SVG và chèn vào Word với PNG fallback để bảo đảm tương thích khi mở trên nhiều phiên bản Word/LibreOffice.
- Bổ sung tài liệu tham khảo [11]-[15] cho Google Sheets, Excel for the web, Odoo Purchase, Jira Service Management và Zoho Creator.

## Kiểm tra đã thực hiện

- PlantUML `-checkonly`: đạt.
- DOCX mở được; 8 section, 18 bảng, 6 hình inline.
- Không có Track Changes hoặc comments.
- SVG được đóng gói trong DOCX; hình có caption đúng và đọc rõ ở chiều rộng trang.
- Các hàng của bảng công nghệ được khóa không tách qua trang.
- Render toàn bộ 47 trang khổ A4 và kiểm tra trực quan; không có chữ/hình bị cắt, bảng vỡ hoặc lỗi dấu tiếng Việt.

## Hiệu chỉnh sau khi người dùng duyệt ảnh

- Khóa toàn bộ hàng của bảng so sánh hệ thống; hàng số 4 hiện nằm nguyên khối ở đầu trang kế tiếp, không còn bị tách đôi.
- Chuyển các số trích dẫn `[1]-[9]`, `[11]-[15]` và các URL tương ứng thành hyperlink thật; trong Word có thể dùng `Ctrl+Click` để mở nguồn chính thức.
- Giữ `[10]` ở dạng trích dẫn thường vì đây là mã nguồn nội bộ trong workspace, không tạo đường dẫn máy cục bộ để tránh liên kết hỏng khi gửi file cho giảng viên.
- Thu Hình 2-1 còn 4,0 inch để giữ bảng không tách, sơ đồ vẫn đọc rõ và tổng số trang vẫn là 47.

## Ghi chú phạm vi

- Nội dung mục 2.3 chưa được chỉnh sửa trong đợt 02A. Việc nội dung 2.3 dịch vị trí giữa các trang chỉ là hệ quả dàn trang sau khi bổ sung 2.1-2.2.
- Danh mục hình hiện mới cập nhật tên Hình 2-1; số trang tự động sẽ được xử lý toàn văn ở đợt 99 theo kế hoạch.
- Tổng số trang hiện tại vẫn là 47. Mục tiêu tối thiểu 60 trang sẽ được đạt bằng các sơ đồ, đặc tả, giao diện và kiểm thử có giá trị ở các đợt tiếp theo.
