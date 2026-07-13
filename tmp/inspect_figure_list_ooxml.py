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


def attr(node, name: str):
    return None if node is None else node.get(qn(name))


def inspect(path: Path):
    with zipfile.ZipFile(path) as zf:
        root = etree.fromstring(zf.read("word/document.xml"))
    paragraphs = root.xpath(".//w:body//w:p", namespaces=NS)
    records = []
    active = False
    for index, p in enumerate(paragraphs):
        text = "".join(p.xpath(".//w:t/text()", namespaces=NS)).strip()
        if text == "MỤC LỤC CÁC HÌNH VẼ":
            active = True
        if not active:
            continue
        ppr = p.find("w:pPr", NS)
        pstyle = ppr.find("w:pStyle", NS) if ppr is not None else None
        spacing = ppr.find("w:spacing", NS) if ppr is not None else None
        jc = ppr.find("w:jc", NS) if ppr is not None else None
        tabs = []
        if ppr is not None:
            for tab in ppr.xpath("./w:tabs/w:tab", namespaces=NS):
                tabs.append({"val": attr(tab, "val"), "pos": attr(tab, "pos"), "leader": attr(tab, "leader")})
        runs = []
        for r in p.xpath(".//w:r", namespaces=NS):
            rpr = r.find("w:rPr", NS)
            rstyle = rpr.find("w:rStyle", NS) if rpr is not None else None
            rfonts = rpr.find("w:rFonts", NS) if rpr is not None else None
            sz = rpr.find("w:sz", NS) if rpr is not None else None
            runs.append({
                "text": "".join(r.xpath(".//w:t/text()", namespaces=NS)),
                "style": attr(rstyle, "val"),
                "font": attr(rfonts, "ascii"),
                "hAnsi": attr(rfonts, "hAnsi"),
                "size": attr(sz, "val"),
                "bold": rpr.find("w:b", NS) is not None if rpr is not None else False,
            })
        records.append({
            "index": index,
            "text": text,
            "style": attr(pstyle, "val"),
            "align": attr(jc, "val"),
            "spacing": None if spacing is None else {
                "before": attr(spacing, "before"),
                "after": attr(spacing, "after"),
                "line": attr(spacing, "line"),
                "lineRule": attr(spacing, "lineRule"),
            },
            "tabs": tabs,
            "hyperlinks": len(p.xpath(".//w:hyperlink", namespaces=NS)),
            "instr": " ".join(p.xpath(".//w:instrText/text()", namespaces=NS)).strip(),
            "runs": runs,
        })
        if len(records) > 1 and pstyle is not None and attr(pstyle, "val") == "Heading1":
            break
        if len(records) >= 50:
            break
    return records


sys.stdout.reconfigure(encoding="utf-8")
print(json.dumps(inspect(Path(sys.argv[1])), ensure_ascii=False, indent=2))
