# Quy ước sơ đồ luận văn GTAS VPP

## Khả năng in đen trắng

- Tất cả sơ đồ dùng nền trắng, chữ và viền đen; chỉ dùng xám nhạt khi cần phân vùng.
- Không dùng màu làm tín hiệu duy nhất để phân biệt thành phần hoặc trạng thái.
- Luồng xử lý khi hệ thống đang chạy dùng nét liền; luồng khởi tạo, phụ thuộc hoặc xử lý nền dùng nét đứt và có nhãn.
- Nét quan hệ phải đủ tương phản khi in đen trắng; tránh đường quá mảnh hoặc xám nhạt.

## Bố cục và chữ

- Font Arial, hỗ trợ đầy đủ dấu tiếng Việt.
- Sơ đồ phải đọc được ở chiều rộng trang Word; ưu tiên rút gọn nhãn hoặc tách hình trước khi giảm cỡ chữ.
- Hạn chế đường nối giao nhau. Các thành phần cùng vai trò hoặc cùng tầng được căn theo hàng/cột rõ ràng.
- SVG là định dạng chính để chèn vào Word; PNG độ phân giải cao là ảnh dự phòng.
- Hình và caption luôn ở chế độ inline, canh giữa và giữ liền nhau.

## Nguồn có thể chỉnh sửa

- Mỗi sơ đồ kỹ thuật giữ file `.puml` để lưu cấu trúc và thuật ngữ nghiệp vụ.
- Với hình cần bố cục cố định như bản mẫu, file `*-layout.py` sinh SVG/PNG theo tọa độ để kết quả không thay đổi giữa các lần render.
- Mọi file `.puml` phải qua `PlantUML -checkonly`; mọi SVG/PNG phải được kiểm tra trực quan trước khi chèn vào Word.
