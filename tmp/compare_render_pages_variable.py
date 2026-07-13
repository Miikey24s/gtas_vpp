from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageStat


old_dir, new_dir = map(Path, sys.argv[1:3])
old_pages = sorted(old_dir.glob("page-*.png"))
new_pages = sorted(new_dir.glob("page-*.png"))
changed = []
unchanged = []
for index in range(min(len(old_pages), len(new_pages))):
    old_image = Image.open(old_pages[index]).convert("RGB")
    new_image = Image.open(new_pages[index]).convert("RGB")
    if old_image.size != new_image.size:
        changed.append((index + 1, "size", old_image.size, new_image.size))
        continue
    diff = ImageChops.difference(old_image, new_image)
    bbox = diff.getbbox()
    if bbox is None:
        unchanged.append(index + 1)
    else:
        stat = ImageStat.Stat(diff)
        changed.append((index + 1, "pixels", bbox, round(sum(stat.mean) / 3, 4)))

print(f"old_pages={len(old_pages)} new_pages={len(new_pages)}")
print("unchanged=" + ",".join(map(str, unchanged)))
print("changed=" + ",".join(str(item[0]) for item in changed))
for item in changed:
    print(item)
