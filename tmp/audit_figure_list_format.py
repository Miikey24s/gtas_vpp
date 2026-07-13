from __future__ import annotations

import json
import sys
import zipfile
from pathlib import Path

from lxml import etree


W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
NS = {"w": W}


def qn(local: str) -> str:
    return f"{{{W}}}{local}"


def value(node, name: str = "val"):
    return None if node is None else node.get(qn(name))


path = Path(sys.argv[1])
with zipfile.ZipFile(path) as zf:
    bad_member = zf.testzip()
    root = etree.fromstring(zf.read("word/document.xml"))

entries = []
active = False
for p in root.xpath(".//w:body//w:p", namespaces=NS):
    text = "".join(p.xpath(".//w:t/text()", namespaces=NS)).strip()
    if text == "MỤC LỤC CÁC HÌNH VẼ":
        active = True
        continue
    if not active:
        continue
    if text.startswith("Hình ") and p.xpath(".//w:hyperlink", namespaces=NS):
        spacing = p.find("w:pPr/w:spacing", NS)
        sizes = {
            value(r.find("w:rPr/w:sz", NS))
            for r in p.xpath(".//w:r", namespaces=NS)
            if "".join(r.xpath(".//w:t/text()", namespaces=NS)).strip()
        }
        fonts = {
            value(r.find("w:rPr/w:rFonts", NS), "ascii")
            for r in p.xpath(".//w:r", namespaces=NS)
            if "".join(r.xpath(".//w:t/text()", namespaces=NS)).strip()
        }
        entries.append({
            "text": text,
            "after": value(spacing, "after"),
            "line": value(spacing, "line"),
            "lineRule": value(spacing, "lineRule"),
            "sizes": sorted(x for x in sizes if x is not None),
            "fonts": sorted(x for x in fonts if x is not None),
            "links": len(p.xpath(".//w:hyperlink", namespaces=NS)),
        })

out = {
    "path": str(path),
    "zip_bad_member": bad_member,
    "count": len(entries),
    "bad_format_count": sum(
        e["after"] != "100" or e["line"] != "276" or e["lineRule"] != "auto"
        or e["sizes"] != ["26"] or e["fonts"] != ["Times New Roman"]
        for e in entries
    ),
    "link_count": sum(e["links"] for e in entries),
    "first": entries[0] if entries else None,
    "last": entries[-1] if entries else None,
}
sys.stdout.reconfigure(encoding="utf-8")
print(json.dumps(out, ensure_ascii=False, indent=2))
