"""Render Hình 2-3 as a conventional black-and-white use-case diagram."""

from html import escape
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
SVG_PATH = HERE / "use-case-overview.svg"
PNG_PATH = HERE / "use-case-overview.png"

WIDTH, HEIGHT, SCALE = 1280, 1060, 2
BLACK, WHITE, LIGHT = "#000000", "#FFFFFF", "#E6E6E6"

EMPLOYEE_CASES = [
    (("Đăng nhập và tải quyền",), 190),
    (("Xem danh mục", "văn phòng phẩm"), 285),
    (("Tạo, sao chép, sửa", "hoặc hủy đơn"), 380),
    (("Tạo đơn bổ sung",), 475),
    (("Xem lịch sử và", "trạng thái đơn"), 570),
    (("Báo cáo cá nhân và", "hộp thư thông báo"), 665),
]

ADMIN_CASES = [
    (("Quản lý danh mục,", "vật tư và nhà cung cấp"), 190),
    (("Quản lý bảng giá",), 285),
    (("Duyệt hoặc từ chối", "đơn bổ sung"), 380),
    (("Đóng kỳ và", "chụp đơn giá"), 475),
    (("Quản lý người dùng", "và phân quyền"), 570),
    (("Báo cáo toàn hệ thống", "và xuất CSV"), 665),
]


def svg_text(lines, cx, cy, size, bold=False, anchor="middle"):
    line_height = size * 1.18
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


def svg_actor(parts, x, top, label):
    head_y = top + 14
    parts.append(
        f'<circle cx="{x}" cy="{head_y}" r="13" fill="{WHITE}" '
        f'stroke="{BLACK}" stroke-width="2"/>'
    )
    parts.append(f'<line x1="{x}" y1="{top + 27}" x2="{x}" y2="{top + 72}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<line x1="{x - 25}" y1="{top + 45}" x2="{x + 25}" y2="{top + 45}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<line x1="{x}" y1="{top + 72}" x2="{x - 22}" y2="{top + 97}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<line x1="{x}" y1="{top + 72}" x2="{x + 22}" y2="{top + 97}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(svg_text(label, x, top + 121, 18, True))


def svg_ellipse(parts, cx, cy, lines, w=300, h=62):
    parts.append(
        f'<ellipse cx="{cx}" cy="{cy}" rx="{w / 2}" ry="{h / 2}" '
        f'fill="{WHITE}" stroke="{BLACK}" stroke-width="2"/>'
    )
    parts.append(svg_text(lines, cx, cy + 1, 18))


def svg_line(parts, start, end, width=1.8):
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

    # Actors stay outside the system boundary, as in a conventional UML diagram.
    svg_actor(parts, 70, 300, ("Nhân viên",))
    svg_actor(parts, 70, 750, ("Quản lý", "phòng ban"))
    svg_actor(parts, 1210, 300, ("Quản trị viên",))
    svg_actor(parts, 1210, 850, ("Xử lý tự động",))

    # System boundary and restrained grayscale title band.
    parts.append(f'<rect x="170" y="60" width="940" height="940" fill="{WHITE}" stroke="{BLACK}" stroke-width="2.2"/>')
    parts.append(f'<rect x="170" y="60" width="940" height="50" fill="{LIGHT}" stroke="none"/>')
    parts.append(f'<line x1="170" y1="110" x2="1110" y2="110" stroke="{BLACK}" stroke-width="1.5"/>')
    parts.append(svg_text(("HỆ THỐNG GTAS VPP",), 640, 92, 22, True))
    parts.append(f'<line x1="640" y1="125" x2="640" y2="970" stroke="#777777" stroke-width="1.2" stroke-dasharray="7 7"/>')
    parts.append(svg_text(("NGHIỆP VỤ NGƯỜI DÙNG",), 420, 140, 16, True))
    parts.append(svg_text(("QUẢN TRỊ HỆ THỐNG",), 860, 140, 16, True))

    # Direct fan-out associations avoid the non-standard shared rails used before.
    employee_start = (95, 345)
    for lines, cy in EMPLOYEE_CASES:
        svg_line(parts, employee_start, (270, cy))
        svg_ellipse(parts, 420, cy, lines)

    admin_start = (1185, 345)
    for lines, cy in ADMIN_CASES:
        svg_line(parts, admin_start, (1010, cy))
        svg_ellipse(parts, 860, cy, lines)

    parts.append(svg_text(("THEO DÕI PHÒNG BAN",), 420, 760, 16, True))
    svg_line(parts, (95, 795), (270, 815))
    svg_ellipse(parts, 420, 815, ("Báo cáo phòng ban", "được cấp quyền"))

    parts.append(svg_text(("XỬ LÝ NỘI BỘ",), 860, 875, 16, True))
    svg_line(parts, (1185, 895), (1010, 930))
    svg_ellipse(parts, 860, 930, ("Tính kỳ, kiểm tra ràng buộc,", "chụp giá, ghi log và phát thông báo"), 370, 70)

    parts.append("</svg>")
    SVG_PATH.write_text("".join(parts), encoding="utf-8")


def load_font(size, bold=False):
    name = "arialbd.ttf" if bold else "arial.ttf"
    return ImageFont.truetype(str(Path(r"C:\Windows\Fonts") / name), size * SCALE)


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


def draw_actor(draw, x, top, label):
    s = SCALE
    draw.ellipse(((x - 13) * s, (top + 1) * s, (x + 13) * s, (top + 27) * s), fill="white", outline="black", width=2 * s)
    draw.line((x * s, (top + 27) * s, x * s, (top + 72) * s), fill="black", width=2 * s)
    draw.line(((x - 25) * s, (top + 45) * s, (x + 25) * s, (top + 45) * s), fill="black", width=2 * s)
    draw.line((x * s, (top + 72) * s, (x - 22) * s, (top + 97) * s), fill="black", width=2 * s)
    draw.line((x * s, (top + 72) * s, (x + 22) * s, (top + 97) * s), fill="black", width=2 * s)
    draw_centered(draw, label, x, top + 117, 18, True)


def draw_ellipse(draw, cx, cy, lines, w=300, h=62):
    s = SCALE
    draw.ellipse(((cx - w / 2) * s, (cy - h / 2) * s, (cx + w / 2) * s, (cy + h / 2) * s), fill="white", outline="black", width=2 * s)
    draw_centered(draw, lines, cx, cy, 18)


def draw_dashed_vertical(draw, x, y1, y2):
    for y in range(y1, y2, 14):
        draw.line((x * SCALE, y * SCALE, x * SCALE, min(y + 7, y2) * SCALE), fill=(119, 119, 119), width=1 * SCALE)


def make_png():
    image = Image.new("RGB", (WIDTH * SCALE, HEIGHT * SCALE), "white")
    draw = ImageDraw.Draw(image)
    s = SCALE

    draw_actor(draw, 70, 300, ("Nhân viên",))
    draw_actor(draw, 70, 750, ("Quản lý", "phòng ban"))
    draw_actor(draw, 1210, 300, ("Quản trị viên",))
    draw_actor(draw, 1210, 850, ("Xử lý tự động",))

    draw.rectangle((170 * s, 60 * s, 1110 * s, 1000 * s), fill="white", outline="black", width=2 * s)
    draw.rectangle((170 * s, 60 * s, 1110 * s, 110 * s), fill=(230, 230, 230))
    draw.line((170 * s, 110 * s, 1110 * s, 110 * s), fill="black", width=2 * s)
    draw_centered(draw, ("HỆ THỐNG GTAS VPP",), 640, 88, 22, True)
    draw_dashed_vertical(draw, 640, 125, 970)
    draw_centered(draw, ("NGHIỆP VỤ NGƯỜI DÙNG",), 420, 136, 16, True)
    draw_centered(draw, ("QUẢN TRỊ HỆ THỐNG",), 860, 136, 16, True)

    for lines, cy in EMPLOYEE_CASES:
        draw.line((95 * s, 345 * s, 270 * s, cy * s), fill="black", width=2 * s)
        draw_ellipse(draw, 420, cy, lines)

    for lines, cy in ADMIN_CASES:
        draw.line((1185 * s, 345 * s, 1010 * s, cy * s), fill="black", width=2 * s)
        draw_ellipse(draw, 860, cy, lines)

    draw_centered(draw, ("THEO DÕI PHÒNG BAN",), 420, 756, 16, True)
    draw.line((95 * s, 795 * s, 270 * s, 815 * s), fill="black", width=2 * s)
    draw_ellipse(draw, 420, 815, ("Báo cáo phòng ban", "được cấp quyền"))

    draw_centered(draw, ("XỬ LÝ NỘI BỘ",), 860, 871, 16, True)
    draw.line((1185 * s, 895 * s, 1010 * s, 930 * s), fill="black", width=2 * s)
    draw_ellipse(draw, 860, 930, ("Tính kỳ, kiểm tra ràng buộc,", "chụp giá, ghi log và phát thông báo"), 370, 70)

    image.save(PNG_PATH, dpi=(180, 180))


if __name__ == "__main__":
    make_svg()
    make_png()
    print(SVG_PATH)
    print(PNG_PATH)
