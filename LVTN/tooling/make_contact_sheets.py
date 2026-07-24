from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw


def build(render_dir: Path, batch_size: int = 20) -> list[Path]:
    pages = sorted(render_dir.glob("page-*.png"))
    outputs: list[Path] = []
    for start in range(0, len(pages), batch_size):
        batch = pages[start : start + batch_size]
        sheet = Image.new("RGB", (1400, 1600), (220, 220, 220))
        for offset, page in enumerate(batch):
            image = Image.open(page).convert("RGB")
            image.thumbnail((260, 368))
            cell = Image.new("RGB", (280, 400), "white")
            cell.paste(image, ((280 - image.width) // 2, 10))
            ImageDraw.Draw(cell).text((8, 380), f"Trang {start + offset + 1}", fill="black")
            sheet.paste(cell, ((offset % 5) * 280, (offset // 5) * 400))
        output = render_dir / f"contact-{start + 1:03d}-{start + len(batch):03d}.jpg"
        sheet.save(output, quality=88)
        outputs.append(output)
    return outputs


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("render_dir", type=Path)
    args = parser.parse_args()
    for output in build(args.render_dir):
        print(output.resolve())


if __name__ == "__main__":
    main()
