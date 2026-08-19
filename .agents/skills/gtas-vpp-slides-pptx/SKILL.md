---
name: gtas-vpp-slides-pptx
description: Inspect, edit, create, QA, and verify PowerPoint presentation decks (.pptx) safely. Use for slide structure analysis, text editing, layout adjustments, image replacements, speaker notes, export to high-res PNG and contact sheets, visual inspection, and defense/presentation readiness checks.
---

# GTAS VPP PowerPoint Presentation (.pptx)

## Load authority & References

1. Đọc `presentation/README.md` và `scripts/presentation/README.md`.
2. Đối với slide bảo vệ luận văn, tham khảo:
   - [Cấu trúc chuẩn slide bảo vệ LVTN](./references/defense-slide-structure.md)
   - [Cookbook code mẫu tự động hóa python-pptx](./references/python-pptx-cookbook.md)
   - Kế hoạch `docs/execution/LVTN-PRESENTATION-001.md` và luận văn chuẩn `LVTN/NguyenAnNam_DH52201078.docx`.
3. Sử dụng bộ công cụ Python + PowerPoint COM trong `scripts/presentation/` để phân tích, chỉnh sửa, render và kiểm tra chất lượng.

---

## Toolchain & Runbooks

### 1. Inspect slide deck structure & content
To inspect text, shapes, typography, tables, images, and speaker notes:
```powershell
# Xem tóm tắt toàn bộ deck
python scripts/presentation/inspect_deck.py presentation/SlideBaoVe.pptx --format summary

# Xem chi tiết cấu trúc dạng Markdown (hoặc JSON)
python scripts/presentation/inspect_deck.py presentation/SlideBaoVe.pptx --format markdown

# Xem chi tiết riêng 1 slide
python scripts/presentation/inspect_deck.py presentation/SlideBaoVe.pptx --slide 3
```

### 2. Render slides & Contact Sheet for Visual QA
To export pixel-perfect 1920x1080 PNGs and a combined contact sheet for visual inspection:
```powershell
python scripts/presentation/render_deck.py presentation/SlideBaoVe.pptx --output-dir .artifacts/slide_render
```
- Output bao gồm:
  - `slide-01.png`, `slide-02.png`, ...: Từng slide ở độ phân giải cao 1080p.
  - `SlideBaoVe_contact_sheet.png`: Bảng tổng hợp toàn bộ slide để review tổng thể bố cục, màu sắc, font chữ bằng `view_file`.
  - `SlideBaoVe_notes.md`: Toàn bộ speaker notes phục vụ thuyết trình.

### 3. Automated QA & Health Check
To check for small fonts, text overflows, missing titles, or missing speaker notes:
```powershell
python scripts/presentation/check_deck.py presentation/SlideBaoVe.pptx --min-font-size 12 --max-bullets 8
```

### 4. Programmatic & Surgical PPTX Edits
To edit text, speaker notes, replace images, or script custom slides:
```powershell
# Thay thế văn bản hàng loạt hoặc theo slide
python scripts/presentation/edit_deck.py presentation/SlideBaoVe.pptx --output .artifacts/SlideBaoVe_updated.pptx --replace-text "CŨ" "MỚI"

# Cập nhật speaker notes cho slide
python scripts/presentation/edit_deck.py presentation/SlideBaoVe.pptx --output .artifacts/SlideBaoVe_updated.pptx --set-notes 3 "Nội dung thuyết trình mới cho slide 3"

# Thay thế hình ảnh/sơ đồ trong slide
python scripts/presentation/edit_deck.py presentation/SlideBaoVe.pptx --output .artifacts/SlideBaoVe_updated.pptx --replace-image 2 "Picture 1" "path/to/new_image.png"
```

Hoặc viết script Python với `python-pptx` để chỉnh sửa chuyên sâu về layout, shape, màu sắc và typography.

---

## Nguyên tắc thiết kế slide thuyết trình & bảo vệ luận văn

1. **Một thông điệp chính mỗi slide (One idea per slide)**:
   - Mỗi slide chỉ giải quyết một luận điểm rõ ràng (tiêu đề + takeaway).
   - Tránh nhồi nhét quá 6-8 gạch đầu dòng trên cùng một slide.

2. **Hệ thống phân cấp thị giác (Visual Hierarchy)**:
   - Tiêu đề slide: Font lớn (24pt – 36pt), in đậm hoặc tương phản cao.
   - Nội dung chính: 14pt – 20pt. Tránh dùng font < 12pt trên slide trình chiếu hội trường.
   - Font chữ chuẩn: Sử dụng font hiện đại, rõ nét (Inter, Space Grotesk, Roboto, Segoe UI, Arial).

3. **Hình ảnh & Sơ đồ kỹ thuật**:
   - Sử dụng sơ đồ sắc nét (SVG/PNG xuất độ phân giải cao), không bị méo tỉ lệ khung hình (aspect ratio).
   - Đặt chú thích ngắn gọn, làm nổi bật điểm mấu chốt.

4. **Speaker Notes đầy đủ**:
   - Mỗi slide phải có speaker notes bao gồm:
     - **Mở đầu / Dẫn dắt**: Câu nối từ slide trước.
     - **Ý chính cần nói**: 2-3 điểm nhấn trọng tâm (không đọc nguyên văn từng chữ trên slide).
     - **Thời lượng dự kiến**: Phù hợp với tổng thời gian bảo vệ (10–15 phút).
     - **Chuyển ý**: Câu dẫn sang slide tiếp theo.

5. **Quy trình nghiệm thu & Đính kèm hình ảnh (Verification & Visual Delivery Loop)**:
   - Luôn render ra PNG và tạo Contact Sheet sau khi chỉnh sửa (`python scripts/presentation/render_deck.py`).
   - Tự động copy ảnh render sang thư mục artifact và tạo artifact Markdown đính kèm ảnh (`![caption](/path/to/slide.png)`) để người dùng xem trực quan ngay lập tức sau mỗi lượt xử lý.
   - Dùng `view_file` để kiểm tra trực quan visual layout, căn lề, tránh tràn chữ ra ngoài khung trước khi bàn giao.
