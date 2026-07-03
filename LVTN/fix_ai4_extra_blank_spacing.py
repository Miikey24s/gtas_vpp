# -*- coding: utf-8 -*-
from __future__ import annotations

import re
import shutil
import zipfile
from pathlib import Path
from tempfile import NamedTemporaryFile

from lxml import etree


DOCX = Path(r"D:\WORK\gtas_vpp\LVTN\NguyenAnNam_DH52201078_AI_4.docx")
W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
NS = {"w": W_NS}
THANKS_TITLE = "LỜI CẢM ƠN"
NUMBERED_RE = re.compile(r"^(\d+\.|\[\d+\])\s+")


def qn(tag: str) -> str:
    return f"{{{W_NS}}}{tag}"


def para_text(p) -> str:
    return "".join(p.xpath(".//w:t/text()", namespaces=NS)).strip()


def ensure_ppr(p):
    ppr = p.find("w:pPr", NS)
    if ppr is None:
        ppr = etree.Element(qn("pPr"))
        p.insert(0, ppr)
    return ppr


def set_spacing(p, *, before: str, after: str, line: str = "312") -> None:
    ppr = ensure_ppr(p)
    spacing = ppr.find("w:spacing", NS)
    if spacing is None:
        spacing = etree.Element(qn("spacing"))
        ppr.append(spacing)
    spacing.set(qn("before"), before)
    spacing.set(qn("after"), after)
    spacing.set(qn("line"), line)
    spacing.set(qn("lineRule"), "auto")


def is_protected_empty(p) -> bool:
    protected = [
        "./w:pPr/w:sectPr",
        ".//w:br",
        ".//w:drawing",
        ".//w:pict",
        ".//w:object",
        ".//w:fldChar",
        ".//w:instrText",
        ".//w:bookmarkStart",
        ".//w:bookmarkEnd",
        ".//w:hyperlink",
    ]
    return any(p.xpath(expr, namespaces=NS) for expr in protected)


def is_list_like(p, text: str) -> bool:
    styles = p.xpath("./w:pPr/w:pStyle/@w:val", namespaces=NS)
    if styles and styles[0] == "ListParagraph":
        return True
    return bool(NUMBERED_RE.match(text))


def main() -> None:
    tmp = NamedTemporaryFile(delete=False, suffix=".docx")
    tmp.close()
    tmp_path = Path(tmp.name)

    removed_empty = 0
    tightened_lists = 0

    with zipfile.ZipFile(DOCX, "r") as zin, zipfile.ZipFile(tmp_path, "w", zipfile.ZIP_DEFLATED) as zout:
        root = etree.fromstring(zin.read("word/document.xml"))
        body = root.find("w:body", NS)
        paras = body.findall("w:p", NS)

        try:
            start_idx = next(i for i, p in enumerate(paras) if para_text(p) == THANKS_TITLE)
        except StopIteration as exc:
            raise RuntimeError("Cannot find LỜI CẢM ƠN") from exc

        # Do not touch the cover page: paragraphs before LỜI CẢM ƠN remain intact.
        for p in list(paras[start_idx:]):
            text = para_text(p)
            if not text and not is_protected_empty(p):
                body.remove(p)
                removed_empty += 1
                continue
            if text and is_list_like(p, text):
                set_spacing(p, before="0", after="0", line="312")
                tightened_lists += 1

        new_xml = etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone=True)
        for item in zin.infolist():
            data = zin.read(item.filename)
            if item.filename == "word/document.xml":
                data = new_xml
            zout.writestr(item, data)

    shutil.move(str(tmp_path), DOCX)
    print(f"removed_empty_paragraphs={removed_empty}")
    print(f"tightened_list_paragraphs={tightened_lists}")
    print(DOCX)


if __name__ == "__main__":
    main()
