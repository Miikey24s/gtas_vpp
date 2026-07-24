from __future__ import annotations

import copy
import re
import shutil
import sys
import tempfile
import zipfile
from pathlib import Path

from lxml import etree


sys.stdout.reconfigure(encoding="utf-8")

LVTN_ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(sys.argv[1]) if len(sys.argv) > 1 else LVTN_ROOT / "NguyenAnNam_DH52201078_working.docx"
OUTPUT = Path(sys.argv[2]) if len(sys.argv) > 2 else LVTN_ROOT / "checkpoints" / "99_final.docx"

NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
}
W = "{%s}" % NS["w"]
XML = "{http://www.w3.org/XML/1998/namespace}"
FIGURE_RE = re.compile(r"^Hình\s+(\d+)-(\d+):\s*(.+)$")


def qn(local: str) -> str:
    return W + local


def paragraph_text(paragraph: etree._Element) -> str:
    return "".join(paragraph.xpath(".//w:t/text()", namespaces=NS)).strip()


def paragraph_style(paragraph: etree._Element) -> str:
    return paragraph.xpath("string(./w:pPr/w:pStyle/@w:val)", namespaces=NS)


def make_run(*, text: str | None = None, tab: bool = False, hyperlink_style: bool = True) -> etree._Element:
    run = etree.Element(qn("r"))
    run_pr = etree.SubElement(run, qn("rPr"))
    if hyperlink_style:
        style = etree.SubElement(run_pr, qn("rStyle"))
        style.set(qn("val"), "Hyperlink")
    etree.SubElement(run_pr, qn("noProof"))
    fonts = etree.SubElement(run_pr, qn("rFonts"))
    fonts.set(qn("ascii"), "Times New Roman")
    fonts.set(qn("hAnsi"), "Times New Roman")
    fonts.set(qn("eastAsia"), "Times New Roman")
    fonts.set(qn("cs"), "Times New Roman")
    if tab:
        etree.SubElement(run, qn("tab"))
    elif text is not None:
        node = etree.SubElement(run, qn("t"))
        node.text = text
    return run


def make_field_run(kind: str, *, instruction: str | None = None, result: str | None = None) -> etree._Element:
    run = make_run(hyperlink_style=True)
    if kind in {"begin", "separate", "end"}:
        fld = etree.SubElement(run, qn("fldChar"))
        fld.set(qn("fldCharType"), kind)
        if kind == "begin":
            fld.set(qn("dirty"), "true")
    elif kind == "instruction":
        instr = etree.SubElement(run, qn("instrText"))
        instr.set(XML + "space", "preserve")
        instr.text = instruction or ""
    elif kind == "result":
        text = etree.SubElement(run, qn("t"))
        text.text = result or "?"
    else:
        raise ValueError(kind)
    return run


def ensure_right_dot_tab(paragraph: etree._Element, position: int = 9000) -> None:
    p_pr = paragraph.find(qn("pPr"))
    if p_pr is None:
        p_pr = etree.Element(qn("pPr"))
        paragraph.insert(0, p_pr)
    old_tabs = p_pr.find(qn("tabs"))
    if old_tabs is not None:
        p_pr.remove(old_tabs)
    tabs = etree.Element(qn("tabs"))
    tab = etree.SubElement(tabs, qn("tab"))
    tab.set(qn("val"), "right")
    tab.set(qn("leader"), "dot")
    tab.set(qn("pos"), str(position))
    insert_at = 1 if len(p_pr) and p_pr[0].tag == qn("pStyle") else 0
    p_pr.insert(insert_at, tabs)


def clear_paragraph_content(paragraph: etree._Element) -> None:
    for child in list(paragraph):
        if child.tag != qn("pPr"):
            paragraph.remove(child)


def add_figure_bookmark(paragraph: etree._Element, name: str, bookmark_id: int) -> None:
    start = etree.Element(qn("bookmarkStart"))
    start.set(qn("id"), str(bookmark_id))
    start.set(qn("name"), name)
    end = etree.Element(qn("bookmarkEnd"))
    end.set(qn("id"), str(bookmark_id))
    insert_at = 1 if len(paragraph) and paragraph[0].tag == qn("pPr") else 0
    paragraph.insert(insert_at, start)
    paragraph.append(end)


def set_page_break_before(paragraph: etree._Element, enabled: bool) -> None:
    p_pr = paragraph.find(qn("pPr"))
    if p_pr is None:
        p_pr = etree.Element(qn("pPr"))
        paragraph.insert(0, p_pr)
    existing = p_pr.find(qn("pageBreakBefore"))
    if enabled and existing is None:
        p_pr.append(etree.Element(qn("pageBreakBefore")))
    elif not enabled and existing is not None:
        p_pr.remove(existing)


def rebuild_figure_list_entry(
    paragraph: etree._Element,
    caption: str,
    bookmark_name: str,
    *,
    page_break_before: bool,
) -> None:
    clear_paragraph_content(paragraph)
    ensure_right_dot_tab(paragraph)
    set_page_break_before(paragraph, page_break_before)

    hyperlink = etree.SubElement(paragraph, qn("hyperlink"))
    hyperlink.set(qn("anchor"), bookmark_name)
    hyperlink.set(qn("history"), "1")
    hyperlink.append(make_run(text=caption))
    hyperlink.append(make_run(tab=True))
    hyperlink.append(make_field_run("begin"))
    hyperlink.append(make_field_run("instruction", instruction=f" PAGEREF {bookmark_name} \\h "))
    hyperlink.append(make_field_run("separate"))
    hyperlink.append(make_field_run("result", result="?"))
    hyperlink.append(make_field_run("end"))


def remove_old_figure_bookmarks(root: etree._Element) -> None:
    ids = {
        node.get(qn("id"))
        for node in root.xpath(".//w:bookmarkStart[starts-with(@w:name, 'fig_')]", namespaces=NS)
    }
    for node in root.xpath(".//w:bookmarkStart[starts-with(@w:name, 'fig_')]", namespaces=NS):
        node.getparent().remove(node)
    for node in root.xpath(".//w:bookmarkEnd", namespaces=NS):
        if node.get(qn("id")) in ids:
            node.getparent().remove(node)


def patch_document(document_xml: bytes) -> tuple[bytes, dict[str, object]]:
    parser = etree.XMLParser(remove_blank_text=False)
    root = etree.fromstring(document_xml, parser)
    body_paragraphs = root.xpath("./w:body/w:p", namespaces=NS)

    figure_list_title_index = next(
        index for index, paragraph in enumerate(body_paragraphs)
        if paragraph_text(paragraph) == "MỤC LỤC CÁC HÌNH VẼ"
    )
    chapter_one_index = next(
        index for index, paragraph in enumerate(body_paragraphs)
        if paragraph_style(paragraph) == "Heading1" and paragraph_text(paragraph).upper().startswith("CHƯƠNG 1.")
    )

    list_paragraphs = []
    for paragraph in body_paragraphs[figure_list_title_index + 1:chapter_one_index]:
        if FIGURE_RE.match(paragraph_text(paragraph)):
            list_paragraphs.append(paragraph)

    real_captions: list[tuple[str, etree._Element, str]] = []
    seen_ids: set[str] = set()
    for paragraph in body_paragraphs[chapter_one_index + 1:]:
        text = paragraph_text(paragraph)
        match = FIGURE_RE.match(text)
        if not match:
            continue
        figure_id = f"{match.group(1)}-{match.group(2)}"
        if figure_id in seen_ids:
            raise ValueError(f"Trùng caption hình trong nội dung: {figure_id}")
        seen_ids.add(figure_id)
        real_captions.append((figure_id, paragraph, text))

    if not list_paragraphs:
        raise ValueError("Không tìm thấy mục hình mẫu để giữ định dạng hiện hành")
    if len(list_paragraphs) < len(real_captions):
        insertion_point = list_paragraphs[-1]
        for _ in range(len(real_captions) - len(list_paragraphs)):
            clone = copy.deepcopy(list_paragraphs[-1])
            insertion_point.addnext(clone)
            insertion_point = clone
            list_paragraphs.append(clone)
    elif len(list_paragraphs) > len(real_captions):
        for paragraph in list_paragraphs[len(real_captions):]:
            paragraph.getparent().remove(paragraph)
        list_paragraphs = list_paragraphs[:len(real_captions)]

    remove_old_figure_bookmarks(root)
    existing_ids = [
        int(value)
        for value in root.xpath(".//w:bookmarkStart/@w:id", namespaces=NS)
        if value.isdigit()
    ]
    next_id = max(existing_ids, default=0) + 1

    bookmark_names = []
    corrected_entries = []
    for entry_index, (list_paragraph, (figure_id, caption_paragraph, caption_text)) in enumerate(
        zip(list_paragraphs, real_captions)
    ):
        bookmark_name = "fig_" + figure_id.replace("-", "_")
        bookmark_names.append(bookmark_name)
        old_list_text = paragraph_text(list_paragraph)
        if old_list_text != caption_text:
            corrected_entries.append({"from": old_list_text, "to": caption_text})
        add_figure_bookmark(caption_paragraph, bookmark_name, next_id)
        next_id += 1
        rebuild_figure_list_entry(
            list_paragraph,
            caption_text,
            bookmark_name,
            page_break_before=entry_index == 19,
        )

    section_borders = len(root.xpath(".//w:sectPr/w:pgBorders", namespaces=NS))
    if section_borders:
        raise ValueError("Tài liệu nguồn có page border; dừng để không làm trái yêu cầu của người dùng")

    output_xml = etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes")
    return output_xml, {
        "figure_count": len(real_captions),
        "bookmark_names": bookmark_names,
        "corrected_entries": corrected_entries,
        "page_border_count": section_borders,
    }


def patch_settings(settings_xml: bytes) -> bytes:
    parser = etree.XMLParser(remove_blank_text=False)
    root = etree.fromstring(settings_xml, parser)
    update = root.find(qn("updateFields"))
    if update is None:
        update = etree.Element(qn("updateFields"))
        root.append(update)
    update.set(qn("val"), "true")
    return etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes")


def build() -> dict[str, object]:
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(SOURCE, "r") as source_archive:
        document_xml, report = patch_document(source_archive.read("word/document.xml"))
        settings_xml = patch_settings(source_archive.read("word/settings.xml"))
        with tempfile.NamedTemporaryFile(delete=False, suffix=".docx", dir=OUTPUT.parent) as temp_stream:
            temp_path = Path(temp_stream.name)
        try:
            with zipfile.ZipFile(temp_path, "w") as output_archive:
                for item in source_archive.infolist():
                    data = source_archive.read(item.filename)
                    if item.filename == "word/document.xml":
                        data = document_xml
                    elif item.filename == "word/settings.xml":
                        data = settings_xml
                    output_archive.writestr(item, data)
            with zipfile.ZipFile(temp_path, "r") as test_archive:
                bad = test_archive.testzip()
                if bad:
                    raise ValueError(f"DOCX ZIP lỗi tại: {bad}")
            shutil.move(str(temp_path), str(OUTPUT))
        finally:
            temp_path.unlink(missing_ok=True)
    report["output"] = str(OUTPUT)
    report["bytes"] = OUTPUT.stat().st_size
    return report


if __name__ == "__main__":
    report = build()
    print(f"OUTPUT={report['output']}")
    print(f"BYTES={report['bytes']}")
    print(f"FIGURES={report['figure_count']}")
    print(f"BOOKMARKS={len(report['bookmark_names'])}")
    print(f"CORRECTED_LIST_ENTRIES={len(report['corrected_entries'])}")
    for item in report["corrected_entries"]:
        print(f"- {item['from']} -> {item['to']}")
    print(f"PAGE_BORDERS={report['page_border_count']}")
