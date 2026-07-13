"""Render Hình 2-2 with the fixed column/trunk layout approved by the user."""

from html import escape
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
SVG_PATH = HERE / "functional-decomposition.svg"
PNG_PATH = HERE / "functional-decomposition.png"

WIDTH = 1535
HEIGHT = 760
SCALE = 2

GROUPS = [
    (
        ("Xác thực và", "phân quyền"),
        [
            ("Đăng nhập",),
            ("Tải quyền trang/", "component"),
            ("Cập nhật quyền", "tức thời"),
            ("Quản lý", "nhóm quyền"),
            ("Quản lý", "người dùng"),
        ],
    ),
    (
        ("Đơn yêu cầu", "thường"),
        [
            ("Xác định kỳ và", "hạn chốt"),
            ("Tạo/sao chép", "đơn thường"),
            ("Sửa hoặc hủy", "đơn yêu cầu"),
            ("Xem lịch sử và", "trạng thái đơn"),
        ],
    ),
    (
        ("Đơn bổ sung",),
        [
            ("Kiểm tra điều kiện", "đơn bổ sung"),
            ("Tạo đơn bổ sung",),
            ("Duyệt hoặc từ chối", "đơn bổ sung"),
            ("Theo dõi", "trạng thái đơn"),
        ],
    ),
    (
        ("Danh mục và", "bảng giá"),
        [
            ("Quản lý danh mục", "và vật tư"),
            ("Quản lý", "đơn vị tính"),
            ("Quản lý", "nhà cung cấp"),
            ("Quản lý bảng giá",),
            ("Ánh xạ giá theo", "nhà cung cấp"),
        ],
    ),
    (
        ("Tổng hợp và", "cuối kỳ"),
        [
            ("Tổng hợp cá nhân",),
            ("Tổng hợp", "phòng ban"),
            ("Tổng hợp", "toàn hệ thống"),
            ("Đóng kỳ và", "chụp đơn giá"),
            ("Ghi log thao tác",),
        ],
    ),
    (
        ("Báo cáo và", "thông báo"),
        [
            ("Báo cáo theo", "phạm vi quyền"),
            ("Lọc, KPI", "và biểu đồ"),
            ("Xuất dữ liệu CSV",),
            ("Nhận thông báo", "tức thời"),
            ("Quản lý hộp thư", "thông báo"),
        ],
    ),
]

ROOT_X, ROOT_Y, ROOT_W, ROOT_H = (WIDTH - 320) // 2, 20, 320, 100
BUS_Y = 160
GROUP_Y, GROUP_W, GROUP_H = 190, 235, 70
LEAF_Y, LEAF_STEP, LEAF_W, LEAF_H = 300, 90, 195, 70
COL_STEP = 255


def svg_text(lines, cx, cy, size, bold=False):
    line_height = size * 1.18
    start = cy - line_height * (len(lines) - 1) / 2
    weight = "700" if bold else "400"
    parts = [
        f'<text x="{cx}" y="{start}" text-anchor="middle" '
        f'font-family="Arial" font-size="{size}" font-weight="{weight}" '
        f'fill="#000000">'
    ]
    for index, line in enumerate(lines):
        dy = 0 if index == 0 else line_height
        parts.append(
            f'<tspan x="{cx}" dy="{dy}">{escape(line)}</tspan>'
        )
    parts.append("</text>")
    return "".join(parts)


def make_svg():
    parts = [
        '<?xml version="1.0" encoding="UTF-8" standalone="no"?>',
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" '
        f'viewBox="0 0 {WIDTH} {HEIGHT}">',
        '<rect width="100%" height="100%" fill="#FFFFFF"/>',
        '<defs><marker id="arrow" markerWidth="10" markerHeight="8" refX="9" refY="4" '
        'orient="auto" markerUnits="strokeWidth"><path d="M0,0 L10,4 L0,8 Z" fill="#000000"/></marker></defs>',
    ]

    centers = [10 + i * COL_STEP + 122.5 for i in range(len(GROUPS))]
    root_cx = ROOT_X + ROOT_W / 2
    parts.append(
        f'<line x1="{root_cx}" y1="{ROOT_Y + ROOT_H}" x2="{root_cx}" y2="{BUS_Y}" stroke="#000" stroke-width="1.6"/>'
    )
    parts.append(
        f'<line x1="{centers[0]}" y1="{BUS_Y}" x2="{centers[-1]}" y2="{BUS_Y}" stroke="#000" stroke-width="1.6"/>'
    )

    parts.append(
        f'<rect x="{ROOT_X}" y="{ROOT_Y}" width="{ROOT_W}" height="{ROOT_H}" fill="#FFF" stroke="#000" stroke-width="1.6"/>'
    )
    parts.append(svg_text(("HỆ THỐNG GTAS VPP",), root_cx, ROOT_Y + 34, 25, True))
    parts.append(svg_text(("Quản lý yêu cầu", "văn phòng phẩm"), root_cx, ROOT_Y + 70, 22, False))

    for index, (group_lines, leaves) in enumerate(GROUPS):
        base = 10 + index * COL_STEP
        group_x = base + 5
        group_cx = group_x + GROUP_W / 2
        trunk_x = base + 22
        leaf_x = base + 45

        parts.append(
            f'<line x1="{group_cx}" y1="{BUS_Y}" x2="{group_cx}" y2="{GROUP_Y}" '
            'stroke="#000" stroke-width="1.6" marker-end="url(#arrow)"/>'
        )
        parts.append(
            f'<rect x="{group_x}" y="{GROUP_Y}" width="{GROUP_W}" height="{GROUP_H}" '
            'fill="#FFF" stroke="#000" stroke-width="1.6"/>'
        )
        parts.append(svg_text(group_lines, group_cx, GROUP_Y + GROUP_H / 2, 22, True))

        last_center = LEAF_Y + (len(leaves) - 1) * LEAF_STEP + LEAF_H / 2
        parts.append(
            f'<line x1="{trunk_x}" y1="{GROUP_Y + GROUP_H}" x2="{trunk_x}" y2="{last_center}" stroke="#000" stroke-width="1.6"/>'
        )

        for row, lines in enumerate(leaves):
            y = LEAF_Y + row * LEAF_STEP
            cy = y + LEAF_H / 2
            parts.append(
                f'<line x1="{trunk_x}" y1="{cy}" x2="{leaf_x}" y2="{cy}" '
                'stroke="#000" stroke-width="1.6" marker-end="url(#arrow)"/>'
            )
            parts.append(
                f'<rect x="{leaf_x}" y="{y}" width="{LEAF_W}" height="{LEAF_H}" '
                'fill="#FFF" stroke="#000" stroke-width="1.4"/>'
            )
            parts.append(svg_text(lines, leaf_x + LEAF_W / 2, cy, 20, False))

    parts.append("</svg>")
    SVG_PATH.write_text("".join(parts), encoding="utf-8")


def font(path, size):
    return ImageFont.truetype(str(path), size * SCALE)


def draw_centered(draw, lines, cx, cy, font_obj, fill="black"):
    spacing = 4 * SCALE
    boxes = [draw.textbbox((0, 0), line, font=font_obj) for line in lines]
    heights = [box[3] - box[1] for box in boxes]
    total = sum(heights) + spacing * (len(lines) - 1)
    y = cy * SCALE - total / 2
    for line, box, height in zip(lines, boxes, heights):
        width = box[2] - box[0]
        draw.text((cx * SCALE - width / 2, y), line, font=font_obj, fill=fill)
        y += height + spacing


def arrow(draw, start, end, width=2):
    x1, y1 = (int(v * SCALE) for v in start)
    x2, y2 = (int(v * SCALE) for v in end)
    draw.line((x1, y1, x2, y2), fill="black", width=width * SCALE)
    size = 8 * SCALE
    draw.polygon([(x2, y2), (x2 - size, y2 - size // 2), (x2 - size, y2 + size // 2)], fill="black")


def make_png():
    image = Image.new("RGB", (WIDTH * SCALE, HEIGHT * SCALE), "white")
    draw = ImageDraw.Draw(image)
    regular = font(Path(r"C:\Windows\Fonts\arial.ttf"), 20)
    group_font = font(Path(r"C:\Windows\Fonts\arialbd.ttf"), 22)
    root_font = font(Path(r"C:\Windows\Fonts\arialbd.ttf"), 25)
    root_sub_font = font(Path(r"C:\Windows\Fonts\arial.ttf"), 22)

    def line(points, width=2):
        draw.line(tuple(int(v * SCALE) for point in points for v in point), fill="black", width=width * SCALE)

    centers = [10 + i * COL_STEP + 122.5 for i in range(len(GROUPS))]
    root_cx = ROOT_X + ROOT_W / 2
    line(((root_cx, ROOT_Y + ROOT_H), (root_cx, BUS_Y)))
    line(((centers[0], BUS_Y), (centers[-1], BUS_Y)))
    draw.rectangle(tuple(int(v * SCALE) for v in (ROOT_X, ROOT_Y, ROOT_X + ROOT_W, ROOT_Y + ROOT_H)), outline="black", width=2 * SCALE)
    draw_centered(draw, ("HỆ THỐNG GTAS VPP",), root_cx, ROOT_Y + 30, root_font)
    draw_centered(draw, ("Quản lý yêu cầu", "văn phòng phẩm"), root_cx, ROOT_Y + 70, root_sub_font)

    for index, (group_lines, leaves) in enumerate(GROUPS):
        base = 10 + index * COL_STEP
        group_x = base + 5
        group_cx = group_x + GROUP_W / 2
        trunk_x = base + 22
        leaf_x = base + 45
        arrow(draw, (group_cx, BUS_Y), (group_cx, GROUP_Y))
        draw.rectangle(tuple(int(v * SCALE) for v in (group_x, GROUP_Y, group_x + GROUP_W, GROUP_Y + GROUP_H)), outline="black", width=2 * SCALE)
        draw_centered(draw, group_lines, group_cx, GROUP_Y + GROUP_H / 2, group_font)
        last_center = LEAF_Y + (len(leaves) - 1) * LEAF_STEP + LEAF_H / 2
        line(((trunk_x, GROUP_Y + GROUP_H), (trunk_x, last_center)))
        for row, lines in enumerate(leaves):
            y = LEAF_Y + row * LEAF_STEP
            cy = y + LEAF_H / 2
            arrow(draw, (trunk_x, cy), (leaf_x, cy))
            draw.rectangle(tuple(int(v * SCALE) for v in (leaf_x, y, leaf_x + LEAF_W, y + LEAF_H)), outline="black", width=2 * SCALE)
            draw_centered(draw, lines, leaf_x + LEAF_W / 2, cy, regular)

    image.save(PNG_PATH, dpi=(180, 180))


if __name__ == "__main__":
    make_svg()
    make_png()
    print(SVG_PATH)
    print(PNG_PATH)
