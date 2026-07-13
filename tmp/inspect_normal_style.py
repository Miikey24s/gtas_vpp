from __future__ import annotations

import json
import sys
import zipfile
from pathlib import Path

from lxml import etree


W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
NS = {"w": W}


def val(node, name="val"):
    return None if node is None else node.get(f"{{{W}}}{name}")


with zipfile.ZipFile(Path(sys.argv[1])) as zf:
    root = etree.fromstring(zf.read("word/styles.xml"))

out = {}
for sid in ("Normal", "Hyperlink"):
    nodes = root.xpath(f'.//w:style[@w:styleId="{sid}"]', namespaces=NS)
    if not nodes:
        continue
    style = nodes[0]
    spacing = style.find("w:pPr/w:spacing", NS)
    size = style.find("w:rPr/w:sz", NS)
    out[sid] = {
        "size": val(size),
        "before": val(spacing, "before"),
        "after": val(spacing, "after"),
        "line": val(spacing, "line"),
        "lineRule": val(spacing, "lineRule"),
    }
defaults = root.find("w:docDefaults", NS)
spacing = defaults.find("w:pPrDefault/w:pPr/w:spacing", NS) if defaults is not None else None
size = defaults.find("w:rPrDefault/w:rPr/w:sz", NS) if defaults is not None else None
out["docDefaults"] = {
    "size": val(size),
    "before": val(spacing, "before"),
    "after": val(spacing, "after"),
    "line": val(spacing, "line"),
    "lineRule": val(spacing, "lineRule"),
}
sys.stdout.reconfigure(encoding="utf-8")
print(json.dumps(out, ensure_ascii=False, indent=2))
