from __future__ import annotations

import sys
import zipfile
from collections import Counter
from pathlib import Path

from lxml import etree


sys.stdout.reconfigure(encoding="utf-8")
DOCX = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(
    r"D:\WORK\gtas_vpp\LVTN\checkpoints\05_chuong5_phuluc_tltk.docx"
)

NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    "wp": "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
    "pr": "http://schemas.openxmlformats.org/package/2006/relationships",
}
W = "{%s}" % NS["w"]
R = "{%s}" % NS["r"]


def node_text(node):
    return "".join(node.xpath(".//w:t/text()", namespaces=NS)).strip()


with zipfile.ZipFile(DOCX) as archive:
    root = etree.fromstring(archive.read("word/document.xml"))
    rels = etree.fromstring(archive.read("word/_rels/document.xml.rels"))
    settings = etree.fromstring(archive.read("word/settings.xml"))

    body_paragraphs = root.xpath("./w:body/w:p", namespaces=NS)
    print("=== FRONT MATTER PARAGRAPHS ===")
    for index, paragraph in enumerate(body_paragraphs[:130]):
        text = node_text(paragraph)
        style = paragraph.xpath("string(./w:pPr/w:pStyle/@w:val)", namespaces=NS)
        instr = " | ".join(" ".join(value.split()) for value in paragraph.xpath(".//w:instrText/text()", namespaces=NS))
        hyperlinks = paragraph.xpath(".//w:hyperlink", namespaces=NS)
        anchors = [h.get(W + "anchor") for h in hyperlinks if h.get(W + "anchor")]
        rel_ids = [h.get(R + "id") for h in hyperlinks if h.get(R + "id")]
        if text or instr or hyperlinks:
            print(f"P{index:03d}\t{style!r}\t{text!r}\tFIELD={instr!r}\tanchors={anchors}\trels={rel_ids}")

    print("\n=== FIELD INVENTORY ===")
    instr_values = [" ".join(value.split()) for value in root.xpath(".//w:instrText/text()", namespaces=NS)]
    for value, count in Counter(instr_values).most_common():
        print(count, repr(value))

    print("\n=== BOOKMARK / LINK INVENTORY ===")
    bookmark_starts = root.xpath(".//w:bookmarkStart", namespaces=NS)
    bookmark_names = [node.get(W + "name") for node in bookmark_starts]
    hyperlinks = root.xpath(".//w:hyperlink", namespaces=NS)
    internal = [node.get(W + "anchor") for node in hyperlinks if node.get(W + "anchor")]
    external = [node.get(R + "id") for node in hyperlinks if node.get(R + "id")]
    print("bookmarks", len(bookmark_names), "unique", len(set(bookmark_names)))
    print("bookmark_prefixes", Counter((name or "").split("_")[0] for name in bookmark_names).most_common(20))
    print("links", len(hyperlinks), "internal", len(internal), "external", len(external))
    print("internal_prefixes", Counter((name or "").split("_")[0] for name in internal).most_common(20))
    print("unresolved", sorted(set(internal) - set(bookmark_names)))

    print("\n=== HEADINGS / CAPTIONS ===")
    for index, paragraph in enumerate(body_paragraphs):
        style = paragraph.xpath("string(./w:pPr/w:pStyle/@w:val)", namespaces=NS)
        text = node_text(paragraph)
        if style in {"1", "2", "3", "Heading1", "Heading2", "Heading3", "Caption"} or text.startswith(("Hình ", "Bảng ")):
            bookmarks = [node.get(W + "name") for node in paragraph.xpath(".//w:bookmarkStart", namespaces=NS)]
            anchors = [node.get(W + "anchor") for node in paragraph.xpath(".//w:hyperlink", namespaces=NS) if node.get(W + "anchor")]
            print(f"P{index:03d}\t{style!r}\t{text[:120]!r}\tbookmarks={bookmarks}\tanchors={anchors}")

    print("\n=== SECTION / BORDER / PAGE NUMBER ===")
    sections = root.xpath(".//w:sectPr", namespaces=NS)
    for index, section in enumerate(sections, 1):
        print(
            index,
            "type", section.xpath("string(./w:type/@w:val)", namespaces=NS) or "nextPage",
            "titlePg", bool(section.xpath("./w:titlePg", namespaces=NS)),
            "borders", len(section.xpath("./w:pgBorders", namespaces=NS)),
            "pgNumStart", section.xpath("string(./w:pgNumType/@w:start)", namespaces=NS),
            "headerRefs", [(n.get(W + "type"), n.get(R + "id")) for n in section.xpath("./w:headerReference", namespaces=NS)],
            "footerRefs", [(n.get(W + "type"), n.get(R + "id")) for n in section.xpath("./w:footerReference", namespaces=NS)],
        )

    print("updateFields", [node.get(W + "val") for node in settings.xpath(".//w:updateFields", namespaces=NS)])
    print("tracked", {name: len(root.xpath(f".//w:{name}", namespaces=NS)) for name in ("ins", "del", "moveFrom", "moveTo")})
    print("comments", [name for name in archive.namelist() if name.startswith("word/comments")])
