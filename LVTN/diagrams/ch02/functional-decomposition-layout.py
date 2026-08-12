"""Render Hình 2-2 as a two-column A4-readable functional tree."""

from html import escape
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
SVG_PATH = HERE / "functional-decomposition.svg"
PNG_PATH = HERE / "functional-decomposition.png"

WIDTH = 1500
HEIGHT = 1500
SCALE = 2

GROUPS = [
    ("Xác thực và quản trị", ["Đăng nhập", "Tải quyền truy cập", "Quản lý nhóm quyền", "Quản lý người dùng", "Cấu hình mặc định kỳ đặt hàng"]),
    ("Xử lý đơn thông thường", ["Xác định kỳ và hạn gửi", "Tạo hoặc sao chép đơn", "Chỉnh sửa hoặc hủy đơn", "Tra cứu lịch sử đơn"]),
    ("Xử lý đơn bổ sung", ["Kiểm tra điều kiện tạo", "Tạo đơn bổ sung", "Duyệt hoặc từ chối", "Theo dõi trạng thái"]),
    ("Danh mục và bảng giá", ["Quản lý văn phòng phẩm", "Quản lý đơn vị tính", "Quản lý nhà cung cấp", "Tạo bảng giá có hiệu lực", "Đặt mặc định hoặc ngừng sử dụng"]),
    ("Vận hành và chốt kỳ", ["Quản lý lịch riêng của từng kỳ", "Khóa hoặc mở lại nhận đơn", "Xem trước và chọn dữ liệu áp dụng", "Chốt kỳ và lưu phiên bản kết quả", "Điều chỉnh đơn sau chốt"]),
    ("Theo dõi và báo cáo", ["Lọc theo phạm vi", "Xem chỉ số tổng hợp", "Xuất báo cáo", "Nhận thông báo", "Theo dõi hộp thư"]),
]

ROOT = (550, 25, 400, 95)
GROUP_W = 600
GROUP_H = 72
LEAF_W = 500
LEAF_H = 54
ROW_TOPS = [185, 620, 1055]
COL_LEFTS = [70, 830]


def split_label(text: str, max_chars: int = 29) -> tuple[str, ...]:
    if len(text) <= max_chars:
        return (text,)
    words = text.split()
    lines: list[str] = []
    current = ""
    for word in words:
        candidate = f"{current} {word}".strip()
        if current and len(candidate) > max_chars:
            lines.append(current)
            current = word
        else:
            current = candidate
    if current:
        lines.append(current)
    return tuple(lines)


def svg_text(lines, cx, cy, size, bold=False):
    line_height = size * 1.16
    start = cy - line_height * (len(lines) - 1) / 2
    weight = "700" if bold else "400"
    parts = [f'<text x="{cx}" y="{start}" text-anchor="middle" font-family="Arial" font-size="{size}" font-weight="{weight}" fill="#000">']
    for index, line in enumerate(lines):
        parts.append(f'<tspan x="{cx}" dy="{0 if index == 0 else line_height}">{escape(line)}</tspan>')
    parts.append("</text>")
    return "".join(parts)


def group_geometry(index: int):
    column = index % 2
    row = index // 2
    x = COL_LEFTS[column]
    y = ROW_TOPS[row]
    return x, y


def make_svg():
    parts = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">',
        '<rect width="100%" height="100%" fill="white"/>',
        '<defs><marker id="arrow" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto"><path d="M0,0 L10,4 L0,8 Z" fill="#000"/></marker></defs>',
    ]
    rx, ry, rw, rh = ROOT
    parts.append(f'<rect x="{rx}" y="{ry}" width="{rw}" height="{rh}" fill="white" stroke="#000" stroke-width="2"/>')
    parts.append(svg_text(("GTAS VPP",), rx + rw / 2, ry + rh / 2, 36, True))

    root_center = rx + rw / 2
    parts.append(f'<line x1="{root_center}" y1="{ry + rh}" x2="{root_center}" y2="150" stroke="#000" stroke-width="2"/>')
    parts.append('<line x1="35" y1="150" x2="1465" y2="150" stroke="#000" stroke-width="2"/>')
    parts.append('<line x1="35" y1="150" x2="35" y2="1091" stroke="#000" stroke-width="2"/>')
    parts.append('<line x1="1465" y1="150" x2="1465" y2="1091" stroke="#000" stroke-width="2"/>')

    for index, (title, leaves) in enumerate(GROUPS):
        x, y = group_geometry(index)
        cy = y + GROUP_H / 2
        if index % 2 == 0:
            parts.append(f'<line x1="35" y1="{cy}" x2="{x}" y2="{cy}" stroke="#000" stroke-width="2" marker-end="url(#arrow)"/>')
        else:
            parts.append(f'<line x1="1465" y1="{cy}" x2="{x + GROUP_W}" y2="{cy}" stroke="#000" stroke-width="2" marker-end="url(#arrow)"/>')
        parts.append(f'<rect x="{x}" y="{y}" width="{GROUP_W}" height="{GROUP_H}" fill="#E6E6E6" stroke="#000" stroke-width="2"/>')
        parts.append(svg_text(split_label(title), x + GROUP_W / 2, cy, 31, True))
        trunk_x = x + 35
        leaf_x = x + 85
        first_y = y + 95
        last_cy = first_y + (len(leaves) - 1) * 62 + LEAF_H / 2
        parts.append(f'<line x1="{trunk_x}" y1="{y + GROUP_H}" x2="{trunk_x}" y2="{last_cy}" stroke="#000" stroke-width="2"/>')
        for row, label in enumerate(leaves):
            ly = first_y + row * 62
            lcy = ly + LEAF_H / 2
            parts.append(f'<line x1="{trunk_x}" y1="{lcy}" x2="{leaf_x}" y2="{lcy}" stroke="#000" stroke-width="2" marker-end="url(#arrow)"/>')
            parts.append(f'<rect x="{leaf_x}" y="{ly}" width="{LEAF_W}" height="{LEAF_H}" fill="white" stroke="#000" stroke-width="1.6"/>')
            parts.append(svg_text(split_label(label, 34), leaf_x + LEAF_W / 2, lcy, 27))
    parts.append("</svg>")
    SVG_PATH.write_text("".join(parts), encoding="utf-8")


def load_font(size: int, bold=False):
    filename = "arialbd.ttf" if bold else "arial.ttf"
    return ImageFont.truetype(str(Path(r"C:\Windows\Fonts") / filename), size * SCALE)


def draw_centered(draw, lines, cx, cy, size, bold=False):
    font = load_font(size, bold)
    spacing = 4 * SCALE
    boxes = [draw.textbbox((0, 0), line, font=font) for line in lines]
    heights = [box[3] - box[1] for box in boxes]
    total = sum(heights) + spacing * (len(lines) - 1)
    y = cy * SCALE - total / 2
    for line, box, height in zip(lines, boxes, heights):
        width = box[2] - box[0]
        draw.text((cx * SCALE - width / 2, y), line, font=font, fill="black")
        y += height + spacing


def line(draw, start, end, width=2):
    draw.line((start[0] * SCALE, start[1] * SCALE, end[0] * SCALE, end[1] * SCALE), fill="black", width=width * SCALE)


def arrow(draw, start, end):
    line(draw, start, end)
    direction = 1 if end[0] >= start[0] else -1
    x, y = end[0] * SCALE, end[1] * SCALE
    size = 9 * SCALE
    draw.polygon([(x, y), (x - direction * size, y - size / 2), (x - direction * size, y + size / 2)], fill="black")


def make_png():
    image = Image.new("RGB", (WIDTH * SCALE, HEIGHT * SCALE), "white")
    draw = ImageDraw.Draw(image)
    rx, ry, rw, rh = ROOT
    draw.rectangle((rx * SCALE, ry * SCALE, (rx + rw) * SCALE, (ry + rh) * SCALE), outline="black", width=2 * SCALE)
    draw_centered(draw, ("GTAS VPP",), rx + rw / 2, ry + rh / 2, 36, True)
    root_center = rx + rw / 2
    line(draw, (root_center, ry + rh), (root_center, 150))
    line(draw, (35, 150), (1465, 150))
    line(draw, (35, 150), (35, 1091))
    line(draw, (1465, 150), (1465, 1091))

    for index, (title, leaves) in enumerate(GROUPS):
        x, y = group_geometry(index)
        cy = y + GROUP_H / 2
        if index % 2 == 0:
            arrow(draw, (35, cy), (x, cy))
        else:
            arrow(draw, (1465, cy), (x + GROUP_W, cy))
        draw.rectangle((x * SCALE, y * SCALE, (x + GROUP_W) * SCALE, (y + GROUP_H) * SCALE), fill=(230, 230, 230), outline="black", width=2 * SCALE)
        draw_centered(draw, split_label(title), x + GROUP_W / 2, cy, 31, True)
        trunk_x = x + 35
        leaf_x = x + 85
        first_y = y + 95
        last_cy = first_y + (len(leaves) - 1) * 62 + LEAF_H / 2
        line(draw, (trunk_x, y + GROUP_H), (trunk_x, last_cy))
        for row, label in enumerate(leaves):
            ly = first_y + row * 62
            lcy = ly + LEAF_H / 2
            arrow(draw, (trunk_x, lcy), (leaf_x, lcy))
            draw.rectangle((leaf_x * SCALE, ly * SCALE, (leaf_x + LEAF_W) * SCALE, (ly + LEAF_H) * SCALE), fill="white", outline="black", width=2 * SCALE)
            draw_centered(draw, split_label(label, 34), leaf_x + LEAF_W / 2, lcy, 27)
    image.save(PNG_PATH, dpi=(180, 180))


if __name__ == "__main__":
    make_svg()
    make_png()
    print(SVG_PATH)
    print(PNG_PATH)
