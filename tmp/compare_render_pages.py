from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageStat


if len(sys.argv) != 3:
    raise SystemExit("Usage: compare_render_pages.py OLD_DIR NEW_DIR")

old_dir = Path(sys.argv[1])
new_dir = Path(sys.argv[2])
old_pages = sorted(old_dir.glob("page-*.png"))
new_pages = sorted(new_dir.glob("page-*.png"))
if len(old_pages) != len(new_pages):
    raise SystemExit(f"Page-count mismatch: {len(old_pages)} != {len(new_pages)}")

changed = []
for old_path, new_path in zip(old_pages, new_pages):
    old_image = Image.open(old_path).convert("RGB")
    new_image = Image.open(new_path).convert("RGB")
    if old_image.size != new_image.size:
        changed.append((old_path.name, "size", old_image.size, new_image.size))
        continue
    diff = ImageChops.difference(old_image, new_image)
    bbox = diff.getbbox()
    if bbox is None:
        continue
    stat = ImageStat.Stat(diff)
    mean = sum(stat.mean) / len(stat.mean)
    changed.append((old_path.name, "pixels", bbox, round(mean, 4)))

print(f"old_pages={len(old_pages)}")
print(f"new_pages={len(new_pages)}")
print(f"changed_pages={len(changed)}")
for item in changed:
    print(item)
