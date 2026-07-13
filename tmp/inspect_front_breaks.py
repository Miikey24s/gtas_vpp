import sys
import zipfile
from lxml import etree

NS = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
W = "{%s}" % NS["w"]

with zipfile.ZipFile(sys.argv[1]) as z:
    root = etree.fromstring(z.read("word/document.xml"))

paras = root.findall(".//w:body/w:p", NS)
for i, p in enumerate(paras):
    text = "".join(p.xpath(".//w:t/text()", namespaces=NS)).strip()
    style = p.find("w:pPr/w:pStyle", NS)
    sid = style.get(W + "val") if style is not None else None
    if not (sid in {"TOC1", "TOC2", "TOC3", "TableofFigures"} or "MỤC LỤC" in text or "TÀI LIỆU THAM KHẢO" in text or 98 <= i <= 103):
        continue
    page_break_before = p.find("w:pPr/w:pageBreakBefore", NS) is not None
    section = p.find("w:pPr/w:sectPr", NS)
    section_type = None
    if section is not None:
        t = section.find("w:type", NS)
        section_type = t.get(W + "val") if t is not None else "nextPage(default)"
    breaks = [b.get(W + "type") or "line" for b in p.findall(".//w:br", NS)]
    keep_next = p.find("w:pPr/w:keepNext", NS) is not None
    keep_lines = p.find("w:pPr/w:keepLines", NS) is not None
    print(i, sid, repr(text[:80]), "pageBreakBefore=", page_break_before, "breaks=", breaks, "sect=", section_type, "keepNext=", keep_next, "keepLines=", keep_lines)
