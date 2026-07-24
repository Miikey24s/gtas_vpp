"""Render Hình 2-3 as a compact black-and-white four-role use-case diagram."""

from html import escape
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
SVG_PATH = HERE / "use-case-overview.svg"
PNG_PATH = HERE / "use-case-overview.png"

WIDTH, HEIGHT, SCALE = 1280, 1060, 2
BLACK, WHITE, LIGHT = "#000000", "#FFFFFF", "#E6E6E6"

EMPLOYEE_CASES = [
    (("Đăng nhập",), 180),
    (("Xem danh mục", "văn phòng phẩm"), 250),
    (("Tạo hoặc sao chép", "đơn thường"), 320),
    (("Chỉnh sửa hoặc hủy", "đơn của mình"), 390),
    (("Tạo và theo dõi", "đơn bổ sung"), 460),
    (("Tra cứu đơn, báo cáo cá nhân", "và nhận thông báo"), 530),
]

MANAGER_CASES = [
    (("Xem toàn bộ đơn", "trong phòng ban"), 690),
    (("Xem báo cáo", "phòng ban"), 760),
]

PROCUREMENT_CASES = [
    (("Xem toàn bộ đơn", "công ty"), 165),
    (("Tổng hợp", "đơn yêu cầu"), 227),
    (("Phê duyệt hoặc từ chối", "đơn bổ sung"), 289),
    (("Quản lý danh mục văn phòng phẩm", "và đơn vị tính"), 351),
    (("Quản lý nhà cung cấp", "và bảng giá"), 413),
    (("Chọn nhà cung cấp", "và bảng giá"), 475),
    (("Rà soát và", "chốt kỳ"), 537),
    (("Xem và xuất báo cáo", "toàn công ty"), 599),
]

ADMIN_CASES = [
    (("Thực hiện toàn bộ", "chức năng nghiệp vụ"), 740),
    (("Quản lý người dùng",), 810),
    (("Quản lý nhóm quyền và", "quyền truy cập hệ thống"), 880),
    (("Kiểm tra nhật ký", "hệ thống"), 950),
]


def svg_text(lines, cx, cy, size, bold=False, anchor="middle"):
    line_height = size * 1.16
    start = cy - line_height * (len(lines) - 1) / 2
    weight = "700" if bold else "400"
    parts = [
        f'<text x="{cx}" y="{start}" text-anchor="{anchor}" font-family="Arial" '
        f'font-size="{size}" font-weight="{weight}" fill="{BLACK}">'
    ]
    for index, line in enumerate(lines):
        parts.append(
            f'<tspan x="{cx}" dy="{0 if index == 0 else line_height}">{escape(line)}</tspan>'
        )
    parts.append("</text>")
    return "".join(parts)


def svg_actor(parts, x, top, label, note=None):
    head_y = top + 14
    parts.append(
        f'<circle cx="{x}" cy="{head_y}" r="13" fill="{WHITE}" '
        f'stroke="{BLACK}" stroke-width="2"/>'
    )
    parts.append(f'<line x1="{x}" y1="{top + 27}" x2="{x}" y2="{top + 72}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<line x1="{x - 25}" y1="{top + 45}" x2="{x + 25}" y2="{top + 45}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<line x1="{x}" y1="{top + 72}" x2="{x - 22}" y2="{top + 97}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<line x1="{x}" y1="{top + 72}" x2="{x + 22}" y2="{top + 97}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(svg_text(label, x, top + 121, 17, True))
    if note:
        note_y = top + 151 + max(0, len(label) - 1) * 16
        parts.append(svg_text((note,), x, note_y, 13, False))


def svg_ellipse(parts, cx, cy, lines, w=350, h=56):
    parts.append(
        f'<ellipse cx="{cx}" cy="{cy}" rx="{w / 2}" ry="{h / 2}" '
        f'fill="{WHITE}" stroke="{BLACK}" stroke-width="1.8"/>'
    )
    parts.append(svg_text(lines, cx, cy + 1, 16))


def svg_line(parts, start, end, width=1.5):
    parts.append(
        f'<line x1="{start[0]}" y1="{start[1]}" x2="{end[0]}" y2="{end[1]}" '
        f'stroke="{BLACK}" stroke-width="{width}"/>'
    )


def make_svg():
    parts = [
        '<?xml version="1.0" encoding="UTF-8" standalone="no"?>',
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" '
        f'viewBox="0 0 {WIDTH} {HEIGHT}">',
        f'<rect width="100%" height="100%" fill="{WHITE}"/>',
    ]

    svg_actor(parts, 70, 275, ("Nhân viên",))
    svg_actor(parts, 70, 680, ("Quản lý", "phòng ban"), "Kế thừa Nhân viên")
    svg_actor(parts, 1185, 300, ("Chuyên viên", "quản lý", "văn phòng phẩm"), "Kế thừa Nhân viên")
    svg_actor(parts, 1185, 760, ("Quản trị", "hệ thống"))

    parts.append(f'<rect x="170" y="50" width="940" height="950" fill="{WHITE}" stroke="{BLACK}" stroke-width="2.2"/>')
    parts.append(f'<rect x="170" y="50" width="940" height="52" fill="{LIGHT}" stroke="none"/>')
    parts.append(f'<line x1="170" y1="102" x2="1110" y2="102" stroke="{BLACK}" stroke-width="1.5"/>')
    parts.append(svg_text(("HỆ THỐNG GTAS VPP",), 640, 84, 22, True))
    parts.append(f'<line x1="640" y1="118" x2="640" y2="978" stroke="#777777" stroke-width="1.2" stroke-dasharray="7 7"/>')

    parts.append(svg_text(("NGHIỆP VỤ NHÂN VIÊN",), 420, 130, 15, True))
    parts.append(svg_text(("QUẢN LÝ PHÒNG BAN",), 420, 625, 15, True))
    parts.append(svg_text(("CHUYÊN VIÊN QUẢN LÝ VĂN PHÒNG PHẨM",), 860, 130, 14, True))
    parts.append(svg_text(("QUẢN TRỊ HỆ THỐNG",), 860, 690, 15, True))

    for lines, cy in EMPLOYEE_CASES:
        svg_line(parts, (95, 320), (245, cy))
        svg_ellipse(parts, 420, cy, lines)

    for lines, cy in MANAGER_CASES:
        svg_line(parts, (95, 725), (245, cy))
        svg_ellipse(parts, 420, cy, lines)

    for lines, cy in PROCUREMENT_CASES:
        svg_line(parts, (1160, 345), (1035, cy))
        svg_ellipse(parts, 860, cy, lines)

    for lines, cy in ADMIN_CASES:
        svg_line(parts, (1160, 805), (1035, cy))
        svg_ellipse(parts, 860, cy, lines)

    parts.append("</svg>")
    SVG_PATH.write_text("".join(parts), encoding="utf-8")


def load_font(size, bold=False):
    name = "arialbd.ttf" if bold else "arial.ttf"
    return ImageFont.truetype(str(Path(r"C:\Windows\Fonts") / name), size * SCALE)


def draw_centered(draw, lines, cx, cy, size, bold=False):
    font = load_font(size, bold)
    spacing = 3 * SCALE
    boxes = [draw.textbbox((0, 0), line, font=font) for line in lines]
    heights = [box[3] - box[1] for box in boxes]
    total = sum(heights) + spacing * (len(lines) - 1)
    y = cy * SCALE - total / 2
    for line, box, height in zip(lines, boxes, heights):
        width = box[2] - box[0]
        draw.text((cx * SCALE - width / 2, y), line, font=font, fill="black")
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
        note_y = top + 147 + max(0, len(label) - 1) * 16
        draw_centered(draw, (note,), x, note_y, 13)


def draw_ellipse(draw, cx, cy, lines, w=350, h=56):
    s = SCALE
    draw.ellipse(((cx - w / 2) * s, (cy - h / 2) * s, (cx + w / 2) * s, (cy + h / 2) * s), fill="white", outline="black", width=2 * s)
    draw_centered(draw, lines, cx, cy, 16)


def make_png():
    image = Image.new("RGB", (WIDTH * SCALE, HEIGHT * SCALE), "white")
    draw = ImageDraw.Draw(image)
    s = SCALE

    draw_actor(draw, 70, 275, ("Nhân viên",))
    draw_actor(draw, 70, 680, ("Quản lý", "phòng ban"), "Kế thừa Nhân viên")
    draw_actor(draw, 1185, 300, ("Chuyên viên", "quản lý", "văn phòng phẩm"), "Kế thừa Nhân viên")
    draw_actor(draw, 1185, 760, ("Quản trị", "hệ thống"))

    draw.rectangle((170 * s, 50 * s, 1110 * s, 1000 * s), fill="white", outline="black", width=2 * s)
    draw.rectangle((170 * s, 50 * s, 1110 * s, 102 * s), fill=(230, 230, 230))
    draw.line((170 * s, 102 * s, 1110 * s, 102 * s), fill="black", width=2 * s)
    draw_centered(draw, ("HỆ THỐNG GTAS VPP",), 640, 80, 22, True)

    for y in range(118, 978, 14):
        draw.line((640 * s, y * s, 640 * s, min(y + 7, 978) * s), fill=(119, 119, 119), width=s)

    draw_centered(draw, ("NGHIỆP VỤ NHÂN VIÊN",), 420, 126, 15, True)
    draw_centered(draw, ("QUẢN LÝ PHÒNG BAN",), 420, 621, 15, True)
    draw_centered(draw, ("CHUYÊN VIÊN QUẢN LÝ VĂN PHÒNG PHẨM",), 860, 126, 14, True)
    draw_centered(draw, ("QUẢN TRỊ HỆ THỐNG",), 860, 686, 15, True)

    for lines, cy in EMPLOYEE_CASES:
        draw.line((95 * s, 320 * s, 245 * s, cy * s), fill="black", width=2 * s)
        draw_ellipse(draw, 420, cy, lines)

    for lines, cy in MANAGER_CASES:
        draw.line((95 * s, 725 * s, 245 * s, cy * s), fill="black", width=2 * s)
        draw_ellipse(draw, 420, cy, lines)

    for lines, cy in PROCUREMENT_CASES:
        draw.line((1160 * s, 345 * s, 1035 * s, cy * s), fill="black", width=2 * s)
        draw_ellipse(draw, 860, cy, lines)

    for lines, cy in ADMIN_CASES:
        draw.line((1160 * s, 805 * s, 1035 * s, cy * s), fill="black", width=2 * s)
        draw_ellipse(draw, 860, cy, lines)

    image.save(PNG_PATH, dpi=(180, 180))


if __name__ == "__main__":
    make_svg()
    make_png()
    print(SVG_PATH)
    print(PNG_PATH)
