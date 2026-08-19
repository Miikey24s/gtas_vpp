# Công cụ bảo trì slide bảo vệ

Các script này dùng lại khi cần kiểm tra hoặc xuất bản `presentation/SlideBaoVe.pptx`. Render, montage, layout JSON, bản ứng viên và báo cáo kiểm tra phải tạo dưới `.artifacts/` hoặc thư mục tạm; không commit các đầu ra đó.

- `inspect_deck.py`: phân tích và xuất cấu trúc deck, text frame, typography, bảng, hình ảnh, speaker notes dạng Markdown/JSON.
- `render_deck.py`: tự động render từng slide ra PNG 1080p bằng PowerPoint COM, xuất contact sheet tổng hợp và bóc tách speaker notes.
- `edit_deck.py`: tiện ích chỉnh sửa PPTX theo kịch bản (thay text hàng loạt, cập nhật notes, thay ảnh/sơ đồ).
- `check_deck.py`: kiểm tra QA tự động (cảnh báo font nhỏ, tràn chữ, thiếu tiêu đề, thiếu speaker notes).
- `render-powerpoint.ps1`: script PowerShell lõi tương tác PowerPoint COM để export PNG.
- `compare-renders.py`: so sánh hai bộ ảnh slide trước và sau chỉnh sửa.

Khi chỉnh nội dung, ưu tiên giữ PPTX native có thể sửa từng đối tượng. Bản pixel-perfect chỉ là phương án dự phòng khi máy trình chiếu làm lệch font hoặc bố cục.
