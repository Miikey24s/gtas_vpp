"""Render Hình 2-1 in a fixed black-and-white deployment layout."""

from html import escape
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
SVG_PATH = HERE / "architecture-overview.svg"
PNG_PATH = HERE / "architecture-overview.png"

WIDTH, HEIGHT, SCALE = 1280, 700, 2
BLACK, WHITE, LIGHT = "#000000", "#FFFFFF", "#E6E6E6"


def svg_text(lines, cx, cy, size, bold=False, anchor="middle"):
    line_height = size * 1.18
    start = cy - line_height * (len(lines) - 1) / 2
    weight = "700" if bold else "400"
    parts = [
        f'<text x="{cx}" y="{start}" text-anchor="{anchor}" '
        f'font-family="Arial" font-size="{size}" font-weight="{weight}" fill="{BLACK}">'
    ]
    for index, line in enumerate(lines):
        parts.append(
            f'<tspan x="{cx}" dy="{0 if index == 0 else line_height}">{escape(line)}</tspan>'
        )
    parts.append("</text>")
    return "".join(parts)


def svg_box(parts, x, y, w, h, title, body):
    parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{WHITE}" stroke="{BLACK}" stroke-width="2"/>')
    parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="42" fill="{LIGHT}" stroke="none"/>')
    parts.append(f'<line x1="{x}" y1="{y + 42}" x2="{x + w}" y2="{y + 42}" stroke="{BLACK}" stroke-width="1.5"/>')
    parts.append(svg_text((title,), x + w / 2, y + 27, 20, True))
    parts.append(svg_text(body, x + w / 2, y + 91, 17))


def svg_polyline(parts, points, dashed=False, arrow=True):
    coords = " ".join(f"{x},{y}" for x, y in points)
    dash = ' stroke-dasharray="10 7"' if dashed else ""
    marker = ' marker-end="url(#arrow)"' if arrow else ""
    parts.append(f'<polyline points="{coords}" fill="none" stroke="{BLACK}" stroke-width="2"{dash}{marker}/>' )


def make_svg():
    parts = [
        '<?xml version="1.0" encoding="UTF-8" standalone="no"?>',
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">',
        f'<rect width="100%" height="100%" fill="{WHITE}"/>',
        '<defs><marker id="arrow" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto" markerUnits="strokeWidth"><path d="M0,0 L10,4 L0,8 Z" fill="#000000"/></marker></defs>',
    ]

    # Deployment boundary.
    parts.append(f'<rect x="185" y="35" width="1065" height="625" fill="{WHITE}" stroke="{BLACK}" stroke-width="2" stroke-dasharray="12 7"/>')
    parts.append(svg_text(("MÁY CHỦ TRIỂN KHAI / DOCKER COMPOSE",), 215, 67, 20, True, "start"))

    # External client.
    svg_box(parts, 5, 270, 165, 115, "NGƯỜI DÙNG", ("Trình duyệt", "nội bộ"))
    svg_box(parts, 235, 255, 210, 145, "NGINX", ("Reverse proxy", "HTTPS / WSS"))
    svg_box(parts, 520, 100, 280, 175, "FRONTEND", ("Blazor Server + Radzen", "Cookie authentication", "Realtime quyền và thông báo"))
    svg_box(parts, 520, 405, 280, 180, "BACKEND API", ("ASP.NET Core (.NET 10)", "JWT + action policy", "Report API + SignalR hubs"))
    svg_box(parts, 890, 100, 280, 175, "MIGRATOR", ("EF Core migration", "Seed dữ liệu nền"))
    svg_box(parts, 890, 405, 280, 180, "SQL SERVER 2022", ("GTAS_VPP_LIVE", "Dữ liệu, thông báo và log"))

    # Runtime paths are solid; initialization is dashed.
    svg_polyline(parts, [(170, 340), (235, 340)])
    svg_polyline(parts, [(445, 300), (480, 300), (480, 187), (520, 187)])
    parts.append(svg_text(("/ và /_blazor",), 476, 167, 15, anchor="end"))
    svg_polyline(parts, [(445, 355), (480, 355), (480, 495), (520, 495)])
    parts.append(svg_text(("/api",), 474, 475, 15, anchor="end"))
    svg_polyline(parts, [(660, 275), (660, 405)])
    parts.append(svg_text(("HTTP API + JWT / WSS",), 677, 341, 15, anchor="start"))
    svg_polyline(parts, [(800, 495), (890, 495)])
    parts.append(svg_text(("EF Core / SQL",), 845, 475, 15))
    svg_polyline(parts, [(1030, 275), (1030, 405)], dashed=True)
    parts.append(svg_text(("migrate + seed",), 1047, 341, 15, anchor="start"))

    parts.append(f'<line x1="340" y1="628" x2="405" y2="628" stroke="{BLACK}" stroke-width="2" marker-end="url(#arrow)"/>')
    parts.append(svg_text(("Luồng vận hành",), 420, 633, 15, anchor="start"))
    parts.append(f'<line x1="685" y1="628" x2="750" y2="628" stroke="{BLACK}" stroke-width="2" stroke-dasharray="10 7" marker-end="url(#arrow)"/>')
    parts.append(svg_text(("Luồng khởi tạo dữ liệu",), 765, 633, 15, anchor="start"))
    parts.append("</svg>")
    SVG_PATH.write_text("".join(parts), encoding="utf-8")


def load_font(size, bold=False):
    name = "arialbd.ttf" if bold else "arial.ttf"
    return ImageFont.truetype(str(Path(r"C:\Windows\Fonts") / name), size * SCALE)


def draw_centered(draw, lines, cx, cy, size, bold=False, anchor="middle"):
    font = load_font(size, bold)
    spacing = 4 * SCALE
    boxes = [draw.textbbox((0, 0), line, font=font) for line in lines]
    heights = [box[3] - box[1] for box in boxes]
    total = sum(heights) + spacing * (len(lines) - 1)
    y = cy * SCALE - total / 2
    for line, box, height in zip(lines, boxes, heights):
        width = box[2] - box[0]
        if anchor == "start":
            x = cx * SCALE
        elif anchor == "end":
            x = cx * SCALE - width
        else:
            x = cx * SCALE - width / 2
        draw.text((x, y), line, font=font, fill="black")
        y += height + spacing


def draw_box(draw, x, y, w, h, title, body):
    s = SCALE
    draw.rectangle((x*s, y*s, (x+w)*s, (y+h)*s), fill="white", outline="black", width=2*s)
    draw.rectangle((x*s, y*s, (x+w)*s, (y+42)*s), fill=(230, 230, 230))
    draw.line((x*s, (y+42)*s, (x+w)*s, (y+42)*s), fill="black", width=2*s)
    draw_centered(draw, (title,), x+w/2, y+25, 20, True)
    draw_centered(draw, body, x+w/2, y+91, 20)


def draw_dashed_segment(draw, start, end, width=2, dash=10, gap=7):
    x1, y1 = start[0]*SCALE, start[1]*SCALE
    x2, y2 = end[0]*SCALE, end[1]*SCALE
    length = ((x2-x1)**2 + (y2-y1)**2) ** 0.5
    if not length:
        return
    dx, dy = (x2-x1)/length, (y2-y1)/length
    pos = 0
    while pos < length:
        stop = min(pos + dash*SCALE, length)
        draw.line((x1+dx*pos, y1+dy*pos, x1+dx*stop, y1+dy*stop), fill="black", width=width*SCALE)
        pos += (dash+gap)*SCALE


def draw_polyline(draw, points, dashed=False):
    scaled = [(x*SCALE, y*SCALE) for x, y in points]
    for start, end in zip(points, points[1:]):
        if dashed:
            draw_dashed_segment(draw, start, end)
        else:
            draw.line((start[0]*SCALE, start[1]*SCALE, end[0]*SCALE, end[1]*SCALE), fill="black", width=2*SCALE)
    (x1, y1), (x2, y2) = scaled[-2], scaled[-1]
    dx, dy = x2-x1, y2-y1
    length = max((dx*dx+dy*dy) ** 0.5, 1)
    ux, uy = dx/length, dy/length
    px, py = -uy, ux
    size = 9*SCALE
    base_x, base_y = x2-ux*size, y2-uy*size
    draw.polygon([(x2, y2), (base_x+px*size*.45, base_y+py*size*.45), (base_x-px*size*.45, base_y-py*size*.45)], fill="black")


def make_png():
    image = Image.new("RGB", (WIDTH*SCALE, HEIGHT*SCALE), "white")
    draw = ImageDraw.Draw(image)
    # Dashed deployment boundary.
    for x in range(185, 1250, 20):
        draw.line((x*SCALE, 35*SCALE, min(x+12, 1250)*SCALE, 35*SCALE), fill="black", width=2*SCALE)
        draw.line((x*SCALE, 660*SCALE, min(x+12, 1250)*SCALE, 660*SCALE), fill="black", width=2*SCALE)
    for y in range(35, 660, 20):
        draw.line((185*SCALE, y*SCALE, 185*SCALE, min(y+12, 660)*SCALE), fill="black", width=2*SCALE)
        draw.line((1250*SCALE, y*SCALE, 1250*SCALE, min(y+12, 660)*SCALE), fill="black", width=2*SCALE)
    draw_centered(draw, ("MÁY CHỦ TRIỂN KHAI / DOCKER COMPOSE",), 215, 62, 25, True, "start")

    draw_box(draw, 5, 270, 165, 115, "NGƯỜI DÙNG", ("Trình duyệt", "nội bộ"))
    draw_box(draw, 235, 255, 210, 145, "NGINX", ("Reverse proxy", "HTTPS / WSS"))
    draw_box(draw, 520, 100, 280, 175, "FRONTEND", ("Blazor Server + Radzen", "Xác thực bằng cookie", "Quyền và thông báo", "tức thời"))
    draw_box(draw, 520, 405, 280, 180, "BACKEND API", ("ASP.NET Core Web API", ".NET 10 + JWT", "Báo cáo và SignalR"))
    draw_box(draw, 890, 100, 280, 175, "MIGRATOR", ("EF Core migration", "Seed dữ liệu nền"))
    draw_box(draw, 890, 405, 280, 180, "SQL SERVER 2022", ("GTAS_VPP_LIVE", "Dữ liệu nghiệp vụ và log"))

    for points, dashed in [
        ([(170,340),(235,340)], False),
        ([(445,300),(480,300),(480,187),(520,187)], False),
        ([(445,355),(480,355),(480,495),(520,495)], False),
        ([(660,275),(660,405)], False),
        ([(800,495),(890,495)], False),
        ([(1030,275),(1030,405)], True),
    ]:
        draw_polyline(draw, points, dashed)
    draw_centered(draw, ("/ và /_blazor",), 476, 162, 20, anchor="end")
    draw_centered(draw, ("/api",), 474, 470, 20, anchor="end")
    draw_centered(draw, ("HTTP API + JWT / WSS",), 677, 336, 18, anchor="start")
    draw_centered(draw, ("migrate + seed",), 1047, 336, 20, anchor="start")

    draw_polyline(draw, [(340,628),(405,628)])
    draw_centered(draw, ("Luồng vận hành",), 420, 628, 20, anchor="start")
    draw_polyline(draw, [(685,628),(750,628)], True)
    draw_centered(draw, ("Luồng khởi tạo dữ liệu",), 765, 628, 20, anchor="start")
    image.save(PNG_PATH, dpi=(180, 180))


if __name__ == "__main__":
    make_svg()
    make_png()
    print(SVG_PATH)
    print(PNG_PATH)
