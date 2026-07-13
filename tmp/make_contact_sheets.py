import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

SOURCE = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(r"D:\WORK\gtas_vpp\tmp\render_05_iter3")
OUTPUT = SOURCE / "contacts"
OUTPUT.mkdir(parents=True, exist_ok=True)

pages = sorted(SOURCE.glob("page-*.png"), key=lambda p: int(p.stem.split("-")[-1]))
cols, rows = 3, 4
thumb_w, thumb_h = 360, 510
label_h = 28
margin = 18
font = ImageFont.load_default()

for sheet_index in range(0, len(pages), cols * rows):
    subset = pages[sheet_index:sheet_index + cols * rows]
    canvas = Image.new("RGB", (margin + cols * (thumb_w + margin), margin + rows * (thumb_h + label_h + margin)), "#d0d0d0")
    draw = ImageDraw.Draw(canvas)
    for pos, path in enumerate(subset):
        img = Image.open(path).convert("RGB")
        img.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
        x = margin + (pos % cols) * (thumb_w + margin) + (thumb_w - img.width) // 2
        y = margin + (pos // cols) * (thumb_h + label_h + margin)
        canvas.paste(img, (x, y))
        page_num = int(path.stem.split("-")[-1])
        draw.text((x, y + thumb_h + 5), f"Trang {page_num}", fill="black", font=font)
    out = OUTPUT / f"contact-{sheet_index // (cols * rows) + 1:02d}.jpg"
    canvas.save(out, quality=90)
    print(out)
