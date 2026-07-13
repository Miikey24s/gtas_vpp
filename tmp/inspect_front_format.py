from __future__ import annotations

import sys
from pathlib import Path

from docx import Document


sys.stdout.reconfigure(encoding="utf-8")
doc = Document(Path(sys.argv[1]))


def pt(value):
    return round(value.pt, 2) if value is not None else None


for index, paragraph in enumerate(doc.paragraphs[:140]):
    text = paragraph.text.strip()
    if not text:
        continue
    run_data = []
    for run in paragraph.runs:
        if not run.text.strip():
            continue
        run_data.append(
            f"{run.text!r}:font={run.font.name},size={pt(run.font.size)},"
            f"b={run.bold},i={run.italic},u={run.underline}"
        )
    pf = paragraph.paragraph_format
    line = pf.line_spacing if isinstance(pf.line_spacing, float) else pt(pf.line_spacing)
    print(
        f"{index:03d} style={paragraph.style.style_id} align={paragraph.alignment} "
        f"before={pt(pf.space_before)} after={pt(pf.space_after)} line={line} "
        f"text={text!r}"
    )
    for record in run_data:
        print("    " + record)
