# Python-pptx Automation Cookbook

Tổng hợp các mẫu code Python để tạo, sửa và định dạng slide PowerPoint tự động bằng `python-pptx`.

---

## 1. Mở và Lưu Presentation

```python
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN

prs = Presentation("template.pptx")
# prs.slide_width = Inches(13.333) # 16:9 widescreen
# prs.slide_height = Inches(7.5)
```

---

## 2. Thêm Slide Mới với Layout

```python
# Chọn layout (0: Title, 1: Title & Content, 5: Title Only, 6: Blank, ...)
blank_layout = prs.slide_layouts[6]
slide = prs.slides.add_slide(blank_layout)
```

---

## 3. Tạo Tiêu đề và Thẻ Nội dung (Card / Box)

```python
from pptx.enum.shapes import MSO_SHAPE

# 1. Thêm hình nền hộp / Card
shape = slide.shapes.add_shape(
    MSO_SHAPE.ROUNDED_RECTANGLE,
    left=Inches(1.0),
    top=Inches(2.0),
    width=Inches(5.0),
    height=Inches(3.5)
)
shape.fill.solid()
shape.fill.fore_color.rgb = RGBColor(245, 247, 250) # Màu nền nhẹ
shape.line.color.rgb = RGBColor(218, 224, 233)      # Viền mỏng

# 2. Thêm Text bên trong Card
tf = shape.text_frame
tf.word_wrap = True
tf.margin_left = Inches(0.2)
tf.margin_top = Inches(0.2)

p_title = tf.paragraphs[0]
p_title.text = "Tính năng Nổi bật"
p_title.font.name = "Inter"
p_title.font.size = Pt(16)
p_title.font.bold = True
p_title.font.color.rgb = RGBColor(18, 24, 38)

p_body = tf.add_paragraph()
p_body.text = "• Đặt hàng theo kỳ độc lập và kiểm soát tự động."
p_body.font.name = "Inter"
p_body.font.size = Pt(13)
p_body.font.color.rgb = RGBColor(71, 84, 103)
```

---

## 4. Chèn và Căn chỉnh Hình ảnh / Sơ đồ

```python
img_path = "assets/architecture_diagram.png"
pic = slide.shapes.add_picture(
    img_path,
    left=Inches(6.5),
    top=Inches(2.0),
    width=Inches(5.8) # Tự động giữ nguyên tỉ lệ (aspect ratio)
)
```

---

## 5. Thêm Bảng Dữ liệu (Table)

```python
rows = 3
cols = 3
table_shape = slide.shapes.add_table(rows, cols, Inches(1.0), Inches(2.0), Inches(8.0), Inches(2.0))
table = table_shape.table

# Đặt độ rộng cột
table.columns[0].width = Inches(3.0)
table.columns[1].width = Inches(2.5)
table.columns[2].width = Inches(2.5)

# Điền dữ liệu
headers = ["Thành phần", "Công nghệ", "Ghi chú"]
for c_idx, text in enumerate(headers):
    cell = table.cell(0, c_idx)
    cell.text = text
    cell.fill.solid()
    cell.fill.fore_color.rgb = RGBColor(30, 41, 59)
    for p in cell.text_frame.paragraphs:
        p.font.name = "Inter"
        p.font.size = Pt(12)
        p.font.bold = True
        p.font.color.rgb = RGBColor(255, 255, 255)
```

---

## 6. Đặt Speaker Notes

```python
if not slide.has_notes_slide:
    slide.notes_slide

notes_frame = slide.notes_slide.notes_text_frame
notes_frame.text = (
    "Mở đầu: Trình bày kiến trúc phân tầng của GTAS VPP.\n"
    "Ý chính: Tách biệt rõ ràng Blazor Server, Web API và Entity Framework Core.\n"
    "Chuyển: Tiếp theo là thiết kế cơ sở dữ liệu."
)
```
