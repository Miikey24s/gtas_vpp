from __future__ import annotations

import argparse
import copy
import os
import re
import tempfile
import zipfile
from pathlib import Path

from docx import Document
from docx.enum.section import WD_ORIENT
from docx.enum.style import WD_STYLE_TYPE
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor
from lxml import etree


FONT = "Times New Roman"
BLACK = RGBColor(0, 0, 0)
CAPTION_RE = re.compile(r"^(Hình|Bảng)\s+(\d+-\d+):\s*(.+)$")

NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
}
W = f"{{{NS['w']}}}"


def set_rfonts(rpr, font_name: str = FONT) -> None:
    rfonts = rpr.find(qn("w:rFonts"))
    if rfonts is None:
        rfonts = OxmlElement("w:rFonts")
        rpr.insert(0, rfonts)
    for attr in ("ascii", "hAnsi", "eastAsia", "cs"):
        rfonts.set(qn(f"w:{attr}"), font_name)


def set_docx_font(font, size: float, *, bold=None, italic=None, underline=None) -> None:
    font.name = FONT
    font.size = Pt(size)
    font.color.rgb = BLACK
    if bold is not None:
        font.bold = bold
    if italic is not None:
        font.italic = italic
    if underline is not None:
        font.underline = underline
    rpr = font._element.get_or_add_rPr()
    set_rfonts(rpr)


def set_run(run, size: float, *, bold=None, italic=None, underline=None) -> None:
    set_docx_font(run.font, size, bold=bold, italic=italic, underline=underline)


def set_paragraph_spacing(paragraph, *, before=0, after=6, line=1.3) -> None:
    pf = paragraph.paragraph_format
    pf.space_before = Pt(before)
    pf.space_after = Pt(after)
    pf.line_spacing = line


def normalize_style(doc, name: str, size: float, *, bold=None, italic=None, underline=None,
                    alignment=None, before=0, after=6, line=1.0, keep=True) -> None:
    if name not in [style.name for style in doc.styles if style.type == WD_STYLE_TYPE.PARAGRAPH]:
        return
    style = doc.styles[name]
    set_docx_font(style.font, size, bold=bold, italic=italic, underline=underline)
    pf = style.paragraph_format
    pf.space_before = Pt(before)
    pf.space_after = Pt(after)
    pf.line_spacing = line
    if alignment is not None:
        pf.alignment = alignment
    pf.keep_with_next = keep


def remove_leading_tab_and_center(paragraph) -> None:
    text = paragraph.text.strip()
    if not text:
        return
    paragraph.text = text
    paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER


def add_or_replace_tbl_header(row) -> None:
    tr_pr = row._tr.get_or_add_trPr()
    for node in tr_pr.findall(qn("w:tblHeader")):
        tr_pr.remove(node)
    tbl_header = OxmlElement("w:tblHeader")
    tbl_header.set(qn("w:val"), "true")
    tr_pr.append(tbl_header)


def make_rows_flexible(table) -> None:
    for row_index, row in enumerate(table.rows):
        tr_pr = row._tr.get_or_add_trPr()
        for tag in ("w:cantSplit", "w:trHeight"):
            for node in tr_pr.findall(qn(tag)):
                tr_pr.remove(node)
        if row_index == 0:
            add_or_replace_tbl_header(row)


def normalize_docx(source: Path, output: Path) -> None:
    doc = Document(source)

    normal = doc.styles["Normal"]
    set_docx_font(normal.font, 13)
    normal_pf = normal.paragraph_format
    normal_pf.space_before = Pt(0)
    normal_pf.space_after = Pt(6)
    normal_pf.line_spacing = 1.3

    normalize_style(
        doc, "Heading 1", 24, bold=True, italic=False, underline=False,
        alignment=WD_ALIGN_PARAGRAPH.RIGHT, before=0, after=18, line=1.0,
    )
    normalize_style(
        doc, "Heading 2", 15, bold=True, italic=False, underline=False,
        before=12, after=6, line=1.0,
    )
    normalize_style(
        doc, "Heading 3", 14, bold=True, italic=False, underline=False,
        before=6, after=6, line=1.0,
    )
    normalize_style(
        doc, "Heading 4", 13, bold=False, italic=False, underline=True,
        before=6, after=6, line=1.0,
    )
    # Match the preferred reference TOC hierarchy: level 1 is visibly larger,
    # while level 3 remains at the thesis body size.
    for toc_style, size, line in (
        ("TOC 1", 16, 1.15),
        ("TOC 2", 14, 1.15),
        ("TOC 3", 13, 1.08),
    ):
        normalize_style(
            doc, toc_style, size, bold=False, italic=False, underline=False,
            before=0, after=5, line=line, keep=False,
        )
    for hyperlink_style in ("Hyperlink", "FollowedHyperlink"):
        try:
            style = doc.styles[hyperlink_style]
        except KeyError:
            style = doc.styles.add_style(hyperlink_style, WD_STYLE_TYPE.CHARACTER)
        set_docx_font(
            style.font, 13, bold=False, italic=False, underline=False,
        )
        # Let paragraph styles control TOC 1/2/3 sizes. Body hyperlinks still
        # inherit 13 pt from Normal and retain black/no-underline display.
        style.font.size = None
        rpr = style.element.get_or_add_rPr()
        for tag in ("w:sz", "w:szCs"):
            node = rpr.find(qn(tag))
            if node is not None:
                rpr.remove(node)

    chapter_start = next(
        index for index, paragraph in enumerate(doc.paragraphs)
        if paragraph.style.style_id == "Heading1"
        and paragraph.text.strip().upper().startswith("CHƯƠNG 1.")
    )

    # Front matter roles from the official sample.
    for index, paragraph in enumerate(doc.paragraphs[:chapter_start]):
        text = paragraph.text.strip()
        if not text:
            if paragraph.style.style_id in {"TOC1", "TOC2", "TOC3"}:
                paragraph.paragraph_format.space_before = Pt(0)
                paragraph.paragraph_format.space_after = Pt(0)
                paragraph.paragraph_format.line_spacing = Pt(1)
                paragraph.paragraph_format.line_spacing_rule = WD_LINE_SPACING.EXACTLY
                for run in paragraph.runs:
                    set_run(run, 1)
            continue
        if text == "LUẬN VĂN TỐT NGHIỆP":
            remove_leading_tab_and_center(paragraph)
            set_paragraph_spacing(paragraph, before=0, after=8, line=1.0)
            for run in paragraph.runs:
                set_run(run, 16, bold=True, italic=False, underline=False)
        elif text == "Tên đề tài:":
            paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
            set_paragraph_spacing(paragraph, before=0, after=8, line=1.0)
            for run in paragraph.runs:
                set_run(run, 14, bold=False, italic=True, underline=True)
        elif text == "XÂY DỰNG WEBSITE QUẢN LÝ VĂN PHÒNG PHẨM PHONG PHÚ":
            paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
            set_paragraph_spacing(paragraph, before=0, after=8, line=1.0)
            for run in paragraph.runs:
                set_run(run, 23, bold=True, italic=False, underline=False)
        elif text == "LỜI CẢM ƠN":
            paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
            set_paragraph_spacing(paragraph, before=0, after=12, line=1.0)
            for run in paragraph.runs:
                set_run(run, 18, bold=True, italic=False, underline=False)
        elif text in {"MỤC LỤC", "MỤC LỤC CÁC HÌNH VẼ"}:
            remove_leading_tab_and_center(paragraph)
            set_paragraph_spacing(paragraph, before=0, after=12, line=1.0)
            for run in paragraph.runs:
                set_run(run, 18, bold=True, italic=False, underline=False)
        elif paragraph.style.style_id in {"TOC1", "TOC2", "TOC3"}:
            toc_line = 1.08 if paragraph.style.style_id == "TOC3" else 1.15
            set_paragraph_spacing(paragraph, before=0, after=5, line=toc_line)
        elif text.startswith("Hình ") and "\t" in paragraph.text:
            # Figure-list items follow the retained sample: 13 pt, 1.15 lines,
            # and 5 pt after each entry. Hyperlinks remain intact.
            set_paragraph_spacing(paragraph, before=0, after=5, line=1.15)
        elif 29 <= index <= 34:
            paragraph.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
            set_paragraph_spacing(paragraph, before=0, after=6, line=1.3)
            for run in paragraph.runs:
                set_run(run, 13)
        else:
            # Keep the cover's intentional blank-line geometry, but remove font drift.
            for run in paragraph.runs:
                if run.text.strip():
                    set_run(run, 13)

    for index, paragraph in enumerate(doc.paragraphs[chapter_start:], chapter_start):
        text = paragraph.text.strip()
        has_drawing = bool(paragraph._p.xpath(".//w:drawing | .//w:pict"))
        style_id = paragraph.style.style_id
        page_break_before = paragraph.paragraph_format.page_break_before

        if style_id == "Heading1":
            if text.upper() in {"PHỤ LỤC", "TÀI LIỆU THAM KHẢO"}:
                paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
                set_paragraph_spacing(paragraph, before=0, after=18, line=1.0)
                for run in paragraph.runs:
                    set_run(run, 18, bold=True, italic=False, underline=False)
            else:
                paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
                set_paragraph_spacing(paragraph, before=0, after=18, line=1.0)
                for run in paragraph.runs:
                    set_run(run, 24, bold=True, italic=False, underline=False)
            paragraph.paragraph_format.keep_with_next = True
        elif style_id == "Heading2":
            set_paragraph_spacing(paragraph, before=12, after=6, line=1.0)
            paragraph.paragraph_format.keep_with_next = True
            for run in paragraph.runs:
                set_run(run, 15, bold=True, italic=False, underline=False)
        elif style_id == "Heading3":
            set_paragraph_spacing(paragraph, before=6, after=6, line=1.0)
            paragraph.paragraph_format.keep_with_next = True
            for run in paragraph.runs:
                set_run(run, 14, bold=True, italic=False, underline=False)
        elif style_id == "Heading4":
            set_paragraph_spacing(paragraph, before=6, after=6, line=1.0)
            paragraph.paragraph_format.keep_with_next = True
            for run in paragraph.runs:
                set_run(run, 13, bold=False, italic=False, underline=True)
        elif CAPTION_RE.match(text):
            paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
            set_paragraph_spacing(paragraph, before=0, after=6, line=1.0)
            paragraph.paragraph_format.keep_with_next = False
            paragraph.paragraph_format.keep_together = True
        elif has_drawing and not text:
            paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
            set_paragraph_spacing(paragraph, before=0, after=6, line=1.0)
            paragraph.paragraph_format.keep_together = True
        elif text:
            set_paragraph_spacing(paragraph, before=0, after=6, line=1.3)
            for run in paragraph.runs:
                set_run(run, 13)

        # Preserve intentionally introduced pagination boundaries.
        paragraph.paragraph_format.page_break_before = page_break_before

    for table in doc.tables:
        make_rows_flexible(table)
        for row in table.rows:
            for cell in row.cells:
                for paragraph in cell.paragraphs:
                    set_paragraph_spacing(paragraph, before=0, after=0, line=1.0)
                    for run in paragraph.runs:
                        if run.text:
                            set_run(run, 12)

    for section_index, section in enumerate(doc.sections):
        section.page_width = Cm(21)
        section.page_height = Cm(29.7)
        section.orientation = WD_ORIENT.PORTRAIT
        section.top_margin = Cm(2)
        section.right_margin = Cm(2)
        section.bottom_margin = Cm(2)
        section.left_margin = Cm(3)

        for header in (section.header, section.first_page_header, section.even_page_header):
            for paragraph in header.paragraphs:
                if not paragraph.text.strip():
                    continue
                for run in paragraph.runs:
                    if run.text:
                        run.text = run.text.upper()
                    set_run(run, 10, bold=False, italic=True, underline=False)
        for footer in (section.footer, section.first_page_footer, section.even_page_footer):
            for paragraph in footer.paragraphs:
                if not paragraph.text.strip():
                    continue
                for run in paragraph.runs:
                    if run.text:
                        run.text = run.text.upper()
                    set_run(run, 10, bold=False, italic=True, underline=False)

    output.parent.mkdir(parents=True, exist_ok=True)
    doc.save(output)


def ensure_child(parent, tag: str, first: bool = False):
    child = parent.find(tag, NS)
    if child is None:
        child = etree.Element(W + tag.split(":", 1)[1])
        if first:
            parent.insert(0, child)
        else:
            parent.append(child)
    return child


def set_ooxml_toggle(rpr, local: str, value: bool) -> None:
    node = rpr.find(f"w:{local}", NS)
    if value:
        if node is None:
            node = etree.SubElement(rpr, W + local)
        node.set(W + "val", "1")
    elif node is not None:
        rpr.remove(node)


def set_ooxml_run(r, size_half_points: int, *, bold=None, italic=None, underline=None) -> None:
    rpr = r.find("w:rPr", NS)
    if rpr is None:
        rpr = etree.Element(W + "rPr")
        r.insert(0, rpr)
    rfonts = ensure_child(rpr, "w:rFonts", first=True)
    for attr in ("ascii", "hAnsi", "eastAsia", "cs"):
        rfonts.set(W + attr, FONT)
    for name in ("sz", "szCs"):
        node = ensure_child(rpr, f"w:{name}")
        node.set(W + "val", str(size_half_points))
    color = ensure_child(rpr, "w:color")
    color.set(W + "val", "000000")
    if bold is not None:
        set_ooxml_toggle(rpr, "b", bold)
        set_ooxml_toggle(rpr, "bCs", bold)
    if italic is not None:
        set_ooxml_toggle(rpr, "i", italic)
        set_ooxml_toggle(rpr, "iCs", italic)
    if underline is not None:
        node = rpr.find("w:u", NS)
        if node is None:
            node = etree.SubElement(rpr, W + "u")
        node.set(W + "val", "single" if underline else "none")


def new_caption_run(text: str, *, number: bool):
    r = etree.Element(W + "r")
    t = etree.SubElement(r, W + "t")
    t.text = text
    set_ooxml_run(
        r, 26,
        bold=True if number else False,
        italic=True if number else False,
        underline=True if number else False,
    )
    return r


def paragraph_text(p) -> str:
    return "".join(t.text or "" for t in p.findall(".//w:t", NS)).strip()


def paragraph_style(p) -> str:
    node = p.find("w:pPr/w:pStyle", NS)
    return node.get(W + "val", "") if node is not None else ""


def replace_caption_runs(p, match: re.Match) -> None:
    ppr = p.find("w:pPr", NS)
    starts = [copy.deepcopy(node) for node in p.findall("w:bookmarkStart", NS)]
    ends = [copy.deepcopy(node) for node in p.findall("w:bookmarkEnd", NS)]
    for child in list(p):
        if child is not ppr:
            p.remove(child)
    for node in starts:
        p.append(node)
    p.append(new_caption_run(f"{match.group(1)} {match.group(2)}", number=True))
    p.append(new_caption_run(f": {match.group(3)}", number=False))
    for node in ends:
        p.append(node)


def rewrite_package(path: Path) -> None:
    with zipfile.ZipFile(path, "r") as zin:
        members = {name: zin.read(name) for name in zin.namelist()}

    parser = etree.XMLParser(remove_blank_text=False)
    root = etree.fromstring(members["word/document.xml"], parser)
    body_paragraphs = root.findall("./w:body/w:p", NS)
    chapter_start = next(
        index for index, paragraph in enumerate(body_paragraphs)
        if paragraph_style(paragraph) == "Heading1"
        and paragraph_text(paragraph).upper().startswith("CHƯƠNG 1.")
    )

    for index, paragraph in enumerate(body_paragraphs):
        text = paragraph_text(paragraph)
        style_id = paragraph_style(paragraph)
        if text.startswith("Hình ") and paragraph.findall(".//w:hyperlink", NS):
            ppr = paragraph.find("w:pPr", NS)
            if ppr is None:
                ppr = etree.Element(W + "pPr")
                paragraph.insert(0, ppr)
            spacing = ensure_child(ppr, "w:spacing")
            spacing.set(W + "before", "0")
            spacing.set(W + "after", "100")
            spacing.set(W + "line", "276")
            spacing.set(W + "lineRule", "auto")
            for run in paragraph.findall(".//w:r", NS):
                set_ooxml_run(run, 26, underline=False)
        if index >= chapter_start:
            match = CAPTION_RE.match(text)
            if match:
                replace_caption_runs(paragraph, match)
            elif style_id == "Heading1":
                size = 36 if text.upper() in {"PHỤ LỤC", "TÀI LIỆU THAM KHẢO"} else 48
                for run in paragraph.findall(".//w:r", NS):
                    set_ooxml_run(run, size, bold=True, italic=False, underline=False)
            elif style_id == "Heading2":
                for run in paragraph.findall(".//w:r", NS):
                    set_ooxml_run(run, 30, bold=True, italic=False, underline=False)
            elif style_id == "Heading3":
                for run in paragraph.findall(".//w:r", NS):
                    set_ooxml_run(run, 28, bold=True, italic=False, underline=False)
            elif style_id == "Heading4":
                for run in paragraph.findall(".//w:r", NS):
                    set_ooxml_run(run, 26, bold=False, italic=False, underline=True)
            elif text:
                for run in paragraph.findall(".//w:r", NS):
                    set_ooxml_run(run, 26)

    # Tables use one consistent 12 pt role to prevent row-only overflow pages;
    # body prose and captions remain 13 pt.
    for paragraph in root.findall(".//w:tbl//w:p", NS):
        for run in paragraph.findall(".//w:r", NS):
            set_ooxml_run(run, 24)

    # Hyperlinks remain clickable, but their display follows the black-and-white sample.
    for hyperlink in root.findall(".//w:hyperlink", NS):
        for run in hyperlink.findall(".//w:r", NS):
            set_ooxml_run(run, 26, underline=False)

    for sect_pr in root.findall(".//w:sectPr", NS):
        for borders in sect_pr.findall("w:pgBorders", NS):
            sect_pr.remove(borders)

    settings = etree.fromstring(members["word/settings.xml"], parser)
    update_fields = settings.find("w:updateFields", NS)
    if update_fields is None:
        update_fields = etree.SubElement(settings, W + "updateFields")
    update_fields.set(W + "val", "true")

    members["word/document.xml"] = etree.tostring(
        root, xml_declaration=True, encoding="UTF-8", standalone="yes"
    )
    members["word/settings.xml"] = etree.tostring(
        settings, xml_declaration=True, encoding="UTF-8", standalone="yes"
    )

    styles = etree.fromstring(members["word/styles.xml"], parser)
    for style_id in ("Hyperlink", "FollowedHyperlink"):
        style = styles.find(f"w:style[@w:styleId='{style_id}']", NS)
        if style is None:
            style = etree.SubElement(styles, W + "style")
            style.set(W + "type", "character")
            style.set(W + "styleId", style_id)
            name = etree.SubElement(style, W + "name")
            name.set(W + "val", style_id)
            based_on = etree.SubElement(style, W + "basedOn")
            based_on.set(W + "val", "DefaultParagraphFont")
            etree.SubElement(style, W + "unhideWhenUsed")
        rpr = style.find("w:rPr", NS)
        if rpr is None:
            rpr = etree.SubElement(style, W + "rPr")
        rfonts = ensure_child(rpr, "w:rFonts", first=True)
        for attr in ("ascii", "hAnsi", "eastAsia", "cs"):
            rfonts.set(W + attr, FONT)
        # Let TOC paragraph styles control their own 16/14/13 pt hierarchy;
        # figure-list runs carry an explicit 13 pt size above.
        for name in ("sz", "szCs"):
            node = rpr.find(f"w:{name}", NS)
            if node is not None:
                rpr.remove(node)
        color = ensure_child(rpr, "w:color")
        color.set(W + "val", "000000")
        underline = ensure_child(rpr, "w:u")
        underline.set(W + "val", "none")
    members["word/styles.xml"] = etree.tostring(
        styles, xml_declaration=True, encoding="UTF-8", standalone="yes"
    )

    for name in list(members):
        if not re.fullmatch(r"word/(header|footer)\d+\.xml", name):
            continue
        part = etree.fromstring(members[name], parser)
        for t in part.findall(".//w:t", NS):
            if t.text:
                t.text = t.text.upper()
        for run in part.findall(".//w:r", NS):
            set_ooxml_run(run, 20, bold=False, italic=True, underline=False)
        members[name] = etree.tostring(
            part, xml_declaration=True, encoding="UTF-8", standalone="yes"
        )

    fd, temp_name = tempfile.mkstemp(suffix=".docx", dir=str(path.parent))
    os.close(fd)
    temp_path = Path(temp_name)
    try:
        with zipfile.ZipFile(temp_path, "w", zipfile.ZIP_DEFLATED) as zout:
            for name, data in members.items():
                zout.writestr(name, data)
        os.replace(temp_path, path)
    finally:
        temp_path.unlink(missing_ok=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    normalize_docx(args.source, args.output)
    rewrite_package(args.output)
    print(args.output)


if __name__ == "__main__":
    main()
