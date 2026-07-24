"""Render Hình 2-3 as a compact black-and-white three-persona use-case diagram."""

from html import escape
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
SVG_PATH = HERE / "use-case-overview.svg"
PNG_PATH = HERE / "use-case-overview.png"
WIDTH, HEIGHT, SCALE = 1280, 900, 2
BLACK, WHITE, LIGHT = "#000000", "#FFFFFF", "#E6E6E6"

LEFT_CASES = [
    (("Đăng nhập và tải quyền",), 180),
    (("Xem danh mục", "văn phòng phẩm"), 260),
    (("Tạo, sao chép, chỉnh sửa", "hoặc hủy đơn của mình"), 340),
    (("Tạo và theo dõi", "đơn bổ sung"), 420),
    (("Tra cứu lịch sử, báo cáo cá nhân", "và nhận thông báo"), 500),
]
MANAGER_CASES = [
    (("Xem đơn phòng ban", "và toàn công ty"), 190),
    (("Phê duyệt hoặc từ chối", "đơn bổ sung"), 275),
    (("Quản lý danh mục, nhà cung cấp", "và bảng giá"), 360),
    (("Rà soát và chốt kỳ",), 445),
    (("Xem và xuất báo cáo", "theo phạm vi được cấp"), 530),
]
DEV_CASES = [
    (("Quản lý người dùng", "và nhóm quyền"), 660),
    (("Quản lý quyền truy cập", "hệ thống"), 740),
    (("Kiểm tra nhật ký", "bảo mật"), 820),
]


def svg_text(lines, cx, cy, size, bold=False):
    line_height = size * 1.16
    start = cy - line_height * (len(lines) - 1) / 2
    weight = "700" if bold else "400"
    parts = [f'<text x="{cx}" y="{start}" text-anchor="middle" font-family="Arial" font-size="{size}" font-weight="{weight}" fill="{BLACK}">']
    for index, line in enumerate(lines):
        parts.append(f'<tspan x="{cx}" dy="{0 if index == 0 else line_height}">{escape(line)}</tspan>')
    parts.append("</text>")
    return "".join(parts)


def svg_actor(parts, x, top, label, note=None):
    parts.append(f'<circle cx="{x}" cy="{top + 14}" r="13" fill="{WHITE}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<line x1="{x}" y1="{top + 27}" x2="{x}" y2="{top + 72}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<line x1="{x - 25}" y1="{top + 45}" x2="{x + 25}" y2="{top + 45}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<line x1="{x}" y1="{top + 72}" x2="{x - 22}" y2="{top + 97}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<line x1="{x}" y1="{top + 72}" x2="{x + 22}" y2="{top + 97}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(svg_text(label, x, top + 121, 17, True))
    if note:
        parts.append(svg_text((note,), x, top + 151, 13))


def svg_ellipse(parts, cx, cy, lines, w=380, h=62):
    parts.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{w / 2}" ry="{h / 2}" fill="{WHITE}" stroke="{BLACK}" stroke-width="1.8"/>')
    parts.append(svg_text(lines, cx, cy + 1, 16))


def make_svg():
    parts = ['<?xml version="1.0" encoding="UTF-8" standalone="no"?>', f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">', f'<rect width="100%" height="100%" fill="{WHITE}"/>']
    svg_actor(parts, 70, 270, ("Nhân viên",))
    svg_actor(parts, 1210, 270, ("Quản lý",), "Kế thừa Nhân viên")
    svg_actor(parts, 1210, 690, ("DEV",))
    parts.append(f'<rect x="155" y="45" width="970" height="820" fill="{WHITE}" stroke="{BLACK}" stroke-width="2.2"/>')
    parts.append(f'<rect x="155" y="45" width="970" height="52" fill="{LIGHT}"/>')
    parts.append(svg_text(("HỆ THỐNG GTAS VPP",), 640, 78, 22, True))
    parts.append(svg_text(("NGHIỆP VỤ CÁ NHÂN",), 390, 125, 15, True))
    parts.append(svg_text(("NGHIỆP VỤ QUẢN LÝ",), 875, 125, 15, True))
    parts.append(svg_text(("QUẢN TRỊ KỸ THUẬT",), 875, 610, 15, True))
    for lines, cy in LEFT_CASES:
        parts.append(f'<line x1="95" y1="315" x2="200" y2="{cy}" stroke="{BLACK}" stroke-width="1.5"/>')
        svg_ellipse(parts, 390, cy, lines)
    for lines, cy in MANAGER_CASES:
        parts.append(f'<line x1="1185" y1="315" x2="1065" y2="{cy}" stroke="{BLACK}" stroke-width="1.5"/>')
        svg_ellipse(parts, 875, cy, lines)
    for lines, cy in DEV_CASES:
        parts.append(f'<line x1="1185" y1="735" x2="1065" y2="{cy}" stroke="{BLACK}" stroke-width="1.5"/>')
        svg_ellipse(parts, 875, cy, lines)
    parts.append("</svg>")
    SVG_PATH.write_text("".join(parts), encoding="utf-8")


def load_font(size, bold=False):
    return ImageFont.truetype(str(Path(r"C:\Windows\Fonts") / ("arialbd.ttf" if bold else "arial.ttf")), size * SCALE)


def draw_centered(draw, lines, cx, cy, size, bold=False):
    font = load_font(size, bold)
    spacing = 3 * SCALE
    boxes = [draw.textbbox((0, 0), line, font=font) for line in lines]
    heights = [box[3] - box[1] for box in boxes]
    y = cy * SCALE - (sum(heights) + spacing * (len(lines) - 1)) / 2
    for line, box, height in zip(lines, boxes, heights):
        draw.text((cx * SCALE - (box[2] - box[0]) / 2, y), line, font=font, fill="black")
        y += height + spacing


def draw_actor(draw, x, top, label, note=None):
    s = SCALE
    draw.ellipse(((x - 13) * s, (top + 1) * s, (x + 13) * s, (top + 27) * s), fill="white", outline="black", width=2 * s)
    draw.line((x * s, (top + 27) * s, x * s, (top + 72) * s), fill="black", width=2 * s)
    draw.line(((x - 25) * s, (top + 45) * s, (x + 25) * s, (top + 45) * s), fill="black", width=2 * s)
    draw.line((x * s, (top + 72) * s, (x - 22) * s, (top + 97) * s), fill="black", width=2 * s)
    draw.line((x * s, (top + 72) * s, (x + 22) * s, (top + 97) * s), fill="black", width=2 * s)
    draw_centered(draw, label, x, top + 117, 17, True)
    if note:
        draw_centered(draw, (note,), x, top + 147, 13)


def draw_ellipse(draw, cx, cy, lines, w=380, h=62):
    s = SCALE
    draw.ellipse(((cx - w / 2) * s, (cy - h / 2) * s, (cx + w / 2) * s, (cy + h / 2) * s), fill="white", outline="black", width=2 * s)
    draw_centered(draw, lines, cx, cy, 16)


def make_png():
    image = Image.new("RGB", (WIDTH * SCALE, HEIGHT * SCALE), "white")
    draw = ImageDraw.Draw(image)
    s = SCALE
    draw_actor(draw, 70, 270, ("Nhân viên",))
    draw_actor(draw, 1210, 270, ("Quản lý",), "Kế thừa Nhân viên")
    draw_actor(draw, 1210, 690, ("DEV",))
    draw.rectangle((155 * s, 45 * s, 1125 * s, 865 * s), fill="white", outline="black", width=2 * s)
    draw.rectangle((155 * s, 45 * s, 1125 * s, 97 * s), fill=(230, 230, 230))
    draw_centered(draw, ("HỆ THỐNG GTAS VPP",), 640, 78, 22, True)
    draw_centered(draw, ("NGHIỆP VỤ CÁ NHÂN",), 390, 125, 15, True)
    draw_centered(draw, ("NGHIỆP VỤ QUẢN LÝ",), 875, 125, 15, True)
    draw_centered(draw, ("QUẢN TRỊ KỸ THUẬT",), 875, 610, 15, True)
    for lines, cy in LEFT_CASES:
        draw.line((95 * s, 315 * s, 200 * s, cy * s), fill="black", width=2 * s)
        draw_ellipse(draw, 390, cy, lines)
    for lines, cy in MANAGER_CASES:
        draw.line((1185 * s, 315 * s, 1065 * s, cy * s), fill="black", width=2 * s)
        draw_ellipse(draw, 875, cy, lines)
    for lines, cy in DEV_CASES:
        draw.line((1185 * s, 735 * s, 1065 * s, cy * s), fill="black", width=2 * s)
        draw_ellipse(draw, 875, cy, lines)
    image.save(PNG_PATH, dpi=(180, 180))


if __name__ == "__main__":
    make_svg()
    make_png()
    print(SVG_PATH)
    print(PNG_PATH)
