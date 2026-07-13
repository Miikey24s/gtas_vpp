from __future__ import annotations

import sys
from pathlib import Path

from docx import Document


sys.stdout.reconfigure(encoding="utf-8")
doc = Document(Path(sys.argv[1]))

for paragraph in doc.paragraphs:
    if paragraph.text.startswith("Hình 2-1:") or paragraph.text.startswith("Bảng 4-1:"):
        print(paragraph.text)
        print("paragraph style", paragraph.style.style_id, paragraph.style.name)
        for run in paragraph.runs:
            if not run.text:
                continue
            style = run.style
            print(
                repr(run.text),
                "run_style", style.style_id, style.name,
                "direct", run.font.name, run.font.size, run.bold, run.italic, run.underline,
                "style_font", style.font.name, style.font.size, style.font.bold,
                style.font.italic, style.font.underline,
            )

for name in ("Hyperlink", "FollowedHyperlink", "CaptionFigureNumber", "CaptionFigureDescription"):
    try:
        style = doc.styles[name]
    except KeyError:
        continue
    print("STYLE", name, style.style_id, style.font.name, style.font.size,
          style.font.bold, style.font.italic, style.font.underline,
          style.font.color.rgb if style.font.color and style.font.color.rgb else None)
