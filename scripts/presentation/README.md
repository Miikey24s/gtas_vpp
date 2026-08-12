# Công cụ bảo trì slide bảo vệ

Các script này dùng lại khi cần kiểm tra hoặc xuất bản `presentation/SlideBaoVe.pptx`. Render, montage, layout JSON, bản ứng viên và báo cáo kiểm tra phải tạo dưới `.artifacts/` hoặc thư mục tạm; không commit các đầu ra đó.

- `inspect-deck.mjs`: xuất ảnh và layout để kiểm tra từng slide.
- `render-powerpoint.ps1`: render bằng Microsoft PowerPoint trên Windows.
- `compare-renders.py`: so sánh hai bộ ảnh slide.
- `build-pixel-perfect-backup.mjs`: tạo bản dự phòng dạng ảnh toàn slide và giữ speaker notes.

Khi chỉnh nội dung, ưu tiên giữ PPTX native có thể sửa từng đối tượng. Bản pixel-perfect chỉ là phương án dự phòng khi máy trình chiếu làm lệch font hoặc bố cục.
