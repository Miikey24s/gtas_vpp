"""Render Hình 2-3 with a fixed, crossing-free actor/use-case layout."""

from html import escape
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
SVG_PATH = HERE / "use-case-overview.svg"
PNG_PATH = HERE / "use-case-overview.png"

WIDTH = 1400
HEIGHT = 1100
SCALE = 2

BOUNDARY = (280, 45, 1080, 1015)
ELLIPSE_CX = 850
ELLIPSE_W = 840
ELLIPSE_H = 54

SECTIONS = [
    (
        "NGHIỆP VỤ CÁ NHÂN",
        300,
        [
            ("Đăng nhập và tải quyền",),
            ("Xem danh mục văn phòng phẩm",),
            ("Tạo, sao chép, chỉnh sửa hoặc hủy đơn của mình",),
            ("Tạo và theo dõi đơn bổ sung",),
            ("Tra cứu lịch sử, báo cáo cá nhân và nhận thông báo",),
        ],
        [180, 240, 300, 360, 420],
    ),
    (
        "NGHIỆP VỤ QUẢN LÝ",
        620,
        [
            ("Xem đơn phòng ban và toàn công ty",),
            ("Phê duyệt hoặc từ chối đơn bổ sung",),
            ("Quản lý danh mục, nhà cung cấp và bảng giá",),
            ("Rà soát và chốt kỳ",),
            ("Xem và xuất báo cáo theo phạm vi được cấp",),
        ],
        [500, 560, 620, 680, 740],
    ),
    (
        "QUẢN TRỊ HỆ THỐNG",
        920,
        [
            ("Quản lý người dùng và nhóm quyền",),
            ("Quản lý quyền truy cập hệ thống",),
            ("Kiểm tra nhật ký bảo mật",),
        ],
        [850, 920, 990],
    ),
]


def svg_text(lines, cx, cy, size, bold=False):
    line_height = size * 1.15
    start = cy - line_height * (len(lines) - 1) / 2
    weight = "700" if bold else "400"
    parts = [f'<text x="{cx}" y="{start}" text-anchor="middle" font-family="Arial" font-size="{size}" font-weight="{weight}" fill="#000">']
    for index, line in enumerate(lines):
        parts.append(f'<tspan x="{cx}" dy="{0 if index == 0 else line_height}">{escape(line)}</tspan>')
    parts.append("</text>")
    return "".join(parts)


def actor_svg(x, y, label):
    return "".join(
        [
            f'<circle cx="{x}" cy="{y - 40}" r="14" fill="white" stroke="#000" stroke-width="2"/>',
            f'<line x1="{x}" y1="{y - 26}" x2="{x}" y2="{y + 24}" stroke="#000" stroke-width="2"/>',
            f'<line x1="{x - 25}" y1="{y - 7}" x2="{x + 25}" y2="{y - 7}" stroke="#000" stroke-width="2"/>',
            f'<line x1="{x}" y1="{y + 24}" x2="{x - 24}" y2="{y + 58}" stroke="#000" stroke-width="2"/>',
            f'<line x1="{x}" y1="{y + 24}" x2="{x + 24}" y2="{y + 58}" stroke="#000" stroke-width="2"/>',
            svg_text(label, x, y + 92, 25, True),
        ]
    )


def generalization_svg(child_y, route_x):
    parent_y = SECTIONS[0][1] - 40
    return "".join(
        [
            f'<polyline points="80,{child_y - 40} {route_x},{child_y - 40} {route_x},{parent_y} 60,{parent_y}" fill="none" stroke="#000" stroke-width="2"/>',
            f'<polygon points="80,{parent_y} 60,{parent_y - 12} 60,{parent_y + 12}" fill="white" stroke="#000" stroke-width="2"/>',
        ]
    )


def make_svg():
    bx, by, bw, bh = BOUNDARY
    parts = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">',
        '<rect width="100%" height="100%" fill="white"/>',
        f'<rect x="{bx}" y="{by}" width="{bw}" height="{bh}" fill="white" stroke="#000" stroke-width="2"/>',
        f'<rect x="{bx}" y="{by}" width="{bw}" height="60" fill="#E6E6E6"/>',
        svg_text(("HỆ THỐNG GTAS VPP",), bx + bw / 2, by + 39, 31, True),
    ]
    actor_labels = [("Nhân viên",), ("Quản lý",), ("Quản trị", "hệ thống")]
    for (_, actor_y, cases, case_ys), label in zip(SECTIONS, actor_labels):
        parts.append(actor_svg(95, actor_y, label))
        for lines, cy in zip(cases, case_ys):
            left = ELLIPSE_CX - ELLIPSE_W / 2
            parts.append(f'<line x1="125" y1="{actor_y - 7}" x2="{left}" y2="{cy}" stroke="#000" stroke-width="1.8"/>')
            parts.append(f'<ellipse cx="{ELLIPSE_CX}" cy="{cy}" rx="{ELLIPSE_W / 2}" ry="{ELLIPSE_H / 2}" fill="white" stroke="#000" stroke-width="1.8"/>')
            parts.append(svg_text(lines, ELLIPSE_CX, cy, 25))
    parts.append(generalization_svg(SECTIONS[1][1], 45))
    parts.append(generalization_svg(SECTIONS[2][1], 20))
    parts.append(svg_text((SECTIONS[0][0],), 850, 135, 25, True))
    parts.append(svg_text((SECTIONS[1][0],), 850, 460, 25, True))
    parts.append(svg_text((SECTIONS[2][0],), 850, 810, 25, True))
    parts.append("</svg>")
    SVG_PATH.write_text("".join(parts), encoding="utf-8")


def font(size, bold=False):
    filename = "arialbd.ttf" if bold else "arial.ttf"
    return ImageFont.truetype(str(Path(r"C:\Windows\Fonts") / filename), size * SCALE)


def draw_centered(draw, lines, cx, cy, size, bold=False):
    f = font(size, bold)
    spacing = 3 * SCALE
    boxes = [draw.textbbox((0, 0), line, font=f) for line in lines]
    heights = [b[3] - b[1] for b in boxes]
    total = sum(heights) + spacing * (len(lines) - 1)
    y = cy * SCALE - total / 2
    for line, box, height in zip(lines, boxes, heights):
        width = box[2] - box[0]
        draw.text((cx * SCALE - width / 2, y), line, font=f, fill="black")
        y += height + spacing


def draw_actor(draw, x, y, label):
    s = SCALE
    draw.ellipse(((x - 14) * s, (y - 54) * s, (x + 14) * s, (y - 26) * s), fill="white", outline="black", width=2 * s)
    draw.line((x * s, (y - 26) * s, x * s, (y + 24) * s), fill="black", width=2 * s)
    draw.line(((x - 25) * s, (y - 7) * s, (x + 25) * s, (y - 7) * s), fill="black", width=2 * s)
    draw.line((x * s, (y + 24) * s, (x - 24) * s, (y + 58) * s), fill="black", width=2 * s)
    draw.line((x * s, (y + 24) * s, (x + 24) * s, (y + 58) * s), fill="black", width=2 * s)
    draw_centered(draw, label, x, y + 92, 25, True)


def draw_generalization(draw, child_y, route_x):
    s = SCALE
    parent_y = SECTIONS[0][1] - 40
    draw.line((80 * s, (child_y - 40) * s, route_x * s, (child_y - 40) * s, route_x * s, parent_y * s, 60 * s, parent_y * s), fill="black", width=2 * s, joint="curve")
    draw.polygon(((80 * s, parent_y * s), (60 * s, (parent_y - 12) * s), (60 * s, (parent_y + 12) * s)), fill="white", outline="black")


def make_png():
    image = Image.new("RGB", (WIDTH * SCALE, HEIGHT * SCALE), "white")
    draw = ImageDraw.Draw(image)
    bx, by, bw, bh = BOUNDARY
    draw.rectangle((bx * SCALE, by * SCALE, (bx + bw) * SCALE, (by + bh) * SCALE), fill="white", outline="black", width=2 * SCALE)
    draw.rectangle((bx * SCALE, by * SCALE, (bx + bw) * SCALE, (by + 60) * SCALE), fill=(230, 230, 230))
    draw_centered(draw, ("HỆ THỐNG GTAS VPP",), bx + bw / 2, by + 39, 31, True)
    actor_labels = [("Nhân viên",), ("Quản lý",), ("Quản trị", "hệ thống")]
    for (_, actor_y, cases, case_ys), label in zip(SECTIONS, actor_labels):
        draw_actor(draw, 95, actor_y, label)
        for lines, cy in zip(cases, case_ys):
            left = ELLIPSE_CX - ELLIPSE_W / 2
            draw.line((125 * SCALE, (actor_y - 7) * SCALE, left * SCALE, cy * SCALE), fill="black", width=2 * SCALE)
            draw.ellipse(((ELLIPSE_CX - ELLIPSE_W / 2) * SCALE, (cy - ELLIPSE_H / 2) * SCALE, (ELLIPSE_CX + ELLIPSE_W / 2) * SCALE, (cy + ELLIPSE_H / 2) * SCALE), fill="white", outline="black", width=2 * SCALE)
            draw_centered(draw, lines, ELLIPSE_CX, cy, 25)
    draw_generalization(draw, SECTIONS[1][1], 45)
    draw_generalization(draw, SECTIONS[2][1], 20)
    draw_centered(draw, (SECTIONS[0][0],), 850, 135, 25, True)
    draw_centered(draw, (SECTIONS[1][0],), 850, 460, 25, True)
    draw_centered(draw, (SECTIONS[2][0],), 850, 810, 25, True)
    image.save(PNG_PATH, dpi=(180, 180))


if __name__ == "__main__":
    make_svg()
    make_png()
    print(SVG_PATH)
    print(PNG_PATH)
