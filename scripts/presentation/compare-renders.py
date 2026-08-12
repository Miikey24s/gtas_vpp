from pathlib import Path
import json
import sys

from PIL import Image, ImageChops, ImageEnhance, ImageStat


reference_dir = Path(sys.argv[1])
candidate_dir = Path(sys.argv[2])
output_dir = Path(sys.argv[3])
output_dir.mkdir(parents=True, exist_ok=True)

reference_files = {path.name: path for path in reference_dir.glob("slide-*.png")}
candidate_files = {path.name: path for path in candidate_dir.glob("slide-*.png")}
names = sorted(reference_files.keys() & candidate_files.keys())
if not names:
    raise RuntimeError("Không tìm thấy cặp ảnh slide-*.png để so sánh.")

rows = []
for name in names:
    reference = Image.open(reference_files[name]).convert("RGB")
    candidate = Image.open(candidate_files[name]).convert("RGB")
    if reference.size != candidate.size:
        reference = reference.resize(candidate.size, Image.Resampling.LANCZOS)

    diff = ImageChops.difference(reference, candidate)
    stat = ImageStat.Stat(diff)
    mae = sum(stat.mean) / 3.0
    maximum = max(high for _, high in diff.getextrema())

    ImageEnhance.Contrast(diff).enhance(4.0).save(output_dir / name)
    rows.append({"slide": name, "mae": round(mae, 3), "max": maximum})

(output_dir / "metrics.json").write_text(
    json.dumps(rows, ensure_ascii=False, indent=2),
    encoding="utf-8",
)
print(json.dumps(rows, ensure_ascii=False))
