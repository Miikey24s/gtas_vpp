"""Render Hình 3-1 in a fixed black-and-white conceptual-data layout."""

from html import escape
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
SVG_PATH = HERE / "data-conceptual.svg"
PNG_PATH = HERE / "data-conceptual.png"
WIDTH, HEIGHT, SCALE = 1280, 780, 2
BLACK, WHITE, LIGHT = "#000000", "#FFFFFF", "#E6E6E6"


def svg_text(lines, cx, cy, size=19, bold=False):
    line_height = size * 1.18
    start = cy - line_height * (len(lines) - 1) / 2
    weight = "700" if bold else "400"
    parts = [
        f'<text x="{cx}" y="{start}" text-anchor="middle" font-family="Arial" '
        f'font-size="{size}" font-weight="{weight}" fill="{BLACK}">'
    ]
    for index, line in enumerate(lines):
        parts.append(f'<tspan x="{cx}" dy="{0 if index == 0 else line_height}">{escape(line)}</tspan>')
    parts.append("</text>")
    return "".join(parts)


def svg_band(parts, x, y, w, h, title):
    parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{WHITE}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="42" fill="{LIGHT}" stroke="none"/>')
    parts.append(f'<line x1="{x}" y1="{y + 42}" x2="{x + w}" y2="{y + 42}" stroke="{BLACK}" stroke-width="1.5"/>')
    parts.append(svg_text((title,), x + w / 2, y + 27, 20, True))


def svg_box(parts, x, y, w, h, lines):
    parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="4" fill="{WHITE}" stroke="{BLACK}" stroke-width="1.8"/>')
    parts.append(svg_text(lines, x + w / 2, y + h / 2 + 1, 18, True))


def svg_path(parts, points, label=None, label_xy=None):
    coords = " ".join(f"{x},{y}" for x, y in points)
    parts.append(f'<polyline points="{coords}" fill="none" stroke="{BLACK}" stroke-width="1.8" marker-end="url(#arrow)"/>')
    if label and label_xy:
        parts.append(svg_text((label,), label_xy[0], label_xy[1], 15))


def make_svg():
    parts = [
        '<?xml version="1.0" encoding="UTF-8" standalone="no"?>',
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">',
        f'<rect width="100%" height="100%" fill="{WHITE}"/>',
        '<defs><marker id="arrow" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto" markerUnits="strokeWidth"><path d="M0,0 L10,4 L0,8 Z" fill="#000000"/></marker></defs>',
    ]

    svg_band(parts, 20, 15, 1240, 190, "TỔ CHỨC VÀ PHÂN QUYỀN")
    top_nodes = [
        (55, 95, 220, 65, ("Đơn vị tổ chức",)),
        (345, 95, 220, 65, ("Người dùng",)),
        (635, 95, 220, 65, ("Nhóm quyền",)),
        (925, 95, 275, 65, ("Trang / chức năng",)),
    ]
    for node in top_nodes:
        svg_box(parts, *node)
    svg_path(parts, [(275, 128), (345, 128)], "1 : N", (310, 115))
    svg_path(parts, [(565, 128), (635, 128)], "N : N", (600, 115))
    svg_path(parts, [(855, 128), (925, 128)], "N : N", (890, 115))

    svg_band(parts, 20, 230, 1240, 220, "ĐƠN YÊU CẦU")
    svg_box(parts, 150, 320, 240, 70, ("Đơn yêu cầu",))
    svg_box(parts, 500, 320, 240, 70, ("Chi tiết đơn",))
    svg_box(parts, 870, 320, 260, 70, ("Nhật ký thao tác",))
    svg_path(parts, [(390, 355), (500, 355)], "1 : N", (445, 342))
    svg_path(parts, [(390, 330), (390, 285), (1000, 285), (1000, 320)], "1 : N", (955, 272))

    # Organization and user references flow down without crossing each other.
    svg_path(parts, [(165, 160), (165, 285), (215, 285), (215, 320)], "tổng hợp", (120, 270))
    svg_path(parts, [(455, 160), (455, 275), (325, 275), (325, 320)], "tạo", (420, 260))

    svg_band(parts, 20, 475, 1240, 285, "DANH MỤC VÀ BẢNG GIÁ")
    svg_box(parts, 65, 560, 205, 65, ("Loại vật tư",))
    svg_box(parts, 465, 560, 205, 65, ("Vật tư", "văn phòng"))
    svg_box(parts, 465, 665, 205, 65, ("Đơn vị tính",))
    svg_box(parts, 735, 560, 205, 65, ("Giá vật tư",))
    svg_box(parts, 1005, 560, 205, 65, ("Nhà cung cấp",))
    svg_box(parts, 735, 665, 205, 65, ("Bảng giá",))

    svg_path(parts, [(270, 592), (465, 592)], "1 : N", (365, 579))
    svg_path(parts, [(568, 665), (568, 625)], "1 : N", (610, 647))
    svg_path(parts, [(670, 592), (735, 592)], "1 : N", (702, 579))
    svg_path(parts, [(1005, 592), (940, 592)], "1 : N", (972, 579))
    svg_path(parts, [(838, 665), (838, 625)], "1 : N", (880, 647))
    svg_path(parts, [(568, 560), (568, 450), (620, 450), (620, 390)], "tham chiếu", (625, 470))

    parts.append("</svg>")
    SVG_PATH.write_text("".join(parts), encoding="utf-8")


def load_font(size, bold=False):
    name = "arialbd.ttf" if bold else "arial.ttf"
    return ImageFont.truetype(str(Path(r"C:\Windows\Fonts") / name), size * SCALE)


def draw_centered(draw, lines, cx, cy, size=19, bold=False):
    font = load_font(size, bold)
    spacing = 4 * SCALE
    boxes = [draw.textbbox((0, 0), line, font=font) for line in lines]
    heights = [b[3] - b[1] for b in boxes]
    total = sum(heights) + spacing * (len(lines) - 1)
    y = cy * SCALE - total / 2
    for line, box, height in zip(lines, boxes, heights):
        width = box[2] - box[0]
        draw.text((cx * SCALE - width / 2, y), line, font=font, fill="black")
        y += height + spacing


def draw_band(draw, x, y, w, h, title):
    s = SCALE
    draw.rectangle((x*s, y*s, (x+w)*s, (y+h)*s), fill="white", outline="black", width=2*s)
    draw.rectangle((x*s, y*s, (x+w)*s, (y+42)*s), fill=(230, 230, 230))
    draw.line((x*s, (y+42)*s, (x+w)*s, (y+42)*s), fill="black", width=2*s)
    draw_centered(draw, (title,), x+w/2, y+25, 20, True)


def draw_box(draw, x, y, w, h, lines):
    s = SCALE
    draw.rectangle((x*s, y*s, (x+w)*s, (y+h)*s), fill="white", outline="black", width=2*s)
    draw_centered(draw, lines, x+w/2, y+h/2, 18, True)


def draw_path(draw, points, label=None, label_xy=None):
    s = SCALE
    coords = [v*s for p in points for v in p]
    draw.line(coords, fill="black", width=2*s, joint="curve")
    (x1,y1),(x2,y2)=points[-2],points[-1]
    dx,dy=(x2-x1)*s,(y2-y1)*s
    length=max((dx*dx+dy*dy)**0.5,1)
    ux,uy=dx/length,dy/length
    px,py=-uy,ux
    tip=(x2*s,y2*s);size=9*s;base=(tip[0]-ux*size,tip[1]-uy*size)
    draw.polygon([tip,(base[0]+px*size*.45,base[1]+py*size*.45),(base[0]-px*size*.45,base[1]-py*size*.45)],fill="black")
    if label and label_xy:
        draw_centered(draw,(label,),label_xy[0],label_xy[1],15)


def make_png():
    image=Image.new("RGB",(WIDTH*SCALE,HEIGHT*SCALE),"white")
    draw=ImageDraw.Draw(image)
    draw_band(draw,20,15,1240,190,"TỔ CHỨC VÀ PHÂN QUYỀN")
    for node in [(55,95,220,65,("Đơn vị tổ chức",)),(345,95,220,65,("Người dùng",)),(635,95,220,65,("Nhóm quyền",)),(925,95,275,65,("Trang / chức năng",))]:draw_box(draw,*node)
    draw_path(draw,[(275,128),(345,128)],"1 : N",(310,115));draw_path(draw,[(565,128),(635,128)],"N : N",(600,115));draw_path(draw,[(855,128),(925,128)],"N : N",(890,115))
    draw_band(draw,20,230,1240,220,"ĐƠN YÊU CẦU")
    draw_box(draw,150,320,240,70,("Đơn yêu cầu",));draw_box(draw,500,320,240,70,("Chi tiết đơn",));draw_box(draw,870,320,260,70,("Nhật ký thao tác",))
    draw_path(draw,[(390,355),(500,355)],"1 : N",(445,342));draw_path(draw,[(390,330),(390,285),(1000,285),(1000,320)],"1 : N",(955,272))
    draw_path(draw,[(165,160),(165,285),(215,285),(215,320)],"tổng hợp",(120,270));draw_path(draw,[(455,160),(455,275),(325,275),(325,320)],"tạo",(420,260))
    draw_band(draw,20,475,1240,285,"DANH MỤC VÀ BẢNG GIÁ")
    for node in [(65,560,205,65,("Loại vật tư",)),(465,560,205,65,("Vật tư","văn phòng")),(465,665,205,65,("Đơn vị tính",)),(735,560,205,65,("Giá vật tư",)),(1005,560,205,65,("Nhà cung cấp",)),(735,665,205,65,("Bảng giá",))]:draw_box(draw,*node)
    draw_path(draw,[(270,592),(465,592)],"1 : N",(365,579));draw_path(draw,[(568,665),(568,625)],"1 : N",(610,647));draw_path(draw,[(670,592),(735,592)],"1 : N",(702,579));draw_path(draw,[(1005,592),(940,592)],"1 : N",(972,579));draw_path(draw,[(838,665),(838,625)],"1 : N",(880,647));draw_path(draw,[(568,560),(568,450),(620,450),(620,390)],"tham chiếu",(625,470))
    image.save(PNG_PATH,dpi=(180,180))


if __name__=="__main__":
    make_svg();make_png();print(SVG_PATH);print(PNG_PATH)
