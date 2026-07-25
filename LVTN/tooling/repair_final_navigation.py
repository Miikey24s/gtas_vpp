#!/usr/bin/env python3
"""Repair reference bookmarks, comments, and field-update settings in a DOCX."""

from __future__ import annotations

import argparse
import re
import shutil
import tempfile
import zipfile
from pathlib import Path

from lxml import etree


NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    "pr": "http://schemas.openxmlformats.org/package/2006/relationships",
    "ct": "http://schemas.openxmlformats.org/package/2006/content-types",
}
W = "{%s}" % NS["w"]


def qn(local: str) -> str:
    return W + local


def text_of(paragraph) -> str:
    return "".join(paragraph.xpath(".//w:t/text()", namespaces=NS)).strip()


def add_bookmark(paragraph, name: str, bookmark_id: int) -> None:
    start = etree.Element(qn("bookmarkStart"))
    start.set(qn("id"), str(bookmark_id))
    start.set(qn("name"), name)
    end = etree.Element(qn("bookmarkEnd"))
    end.set(qn("id"), str(bookmark_id))
    insertion = 1 if paragraph.find(qn("pPr")) is not None else 0
    paragraph.insert(insertion, start)
    paragraph.append(end)


def patch_document(data: bytes) -> bytes:
    parser = etree.XMLParser(remove_blank_text=False)
    root = etree.fromstring(data, parser)
    body_paragraphs = root.xpath("./w:body/w:p", namespaces=NS)

    old_ids = {
        node.get(qn("id"))
        for node in root.xpath(".//w:bookmarkStart[starts-with(@w:name, 'ref_')]", namespaces=NS)
    }
    for node in root.xpath(".//w:bookmarkStart[starts-with(@w:name, 'ref_')]", namespaces=NS):
        node.getparent().remove(node)
    for node in root.xpath(".//w:bookmarkEnd", namespaces=NS):
        if node.get(qn("id")) in old_ids:
            node.getparent().remove(node)

    reference_heading = next(
        p for p in body_paragraphs if text_of(p) == "TÀI LIỆU THAM KHẢO"
    )
    references = [
        p for p in body_paragraphs[body_paragraphs.index(reference_heading) + 1:]
        if re.match(r"^\[\d+\]", text_of(p))
    ]
    reference_numbers = [int(re.match(r"^\[(\d+)\]", text_of(p)).group(1)) for p in references]
    expected_numbers = list(range(1, len(references) + 1))
    if not references or reference_numbers != expected_numbers:
        raise RuntimeError(
            f"References must be contiguous from 1; found {reference_numbers}"
        )

    existing_ids = [
        int(value) for value in root.xpath(".//w:bookmarkStart/@w:id", namespaces=NS)
        if value.isdigit()
    ]
    next_id = max(existing_ids, default=0) + 1
    for number, paragraph in enumerate(references, 1):
        add_bookmark(paragraph, f"ref_{number:02d}", next_id)
        next_id += 1

    for tag in ("commentRangeStart", "commentRangeEnd", "commentReference"):
        for node in root.xpath(f".//w:{tag}", namespaces=NS):
            node.getparent().remove(node)

    return etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes")


def patch_settings(data: bytes) -> bytes:
    parser = etree.XMLParser(remove_blank_text=False)
    root = etree.fromstring(data, parser)
    update = root.find(qn("updateFields"))
    if update is None:
        update = etree.SubElement(root, qn("updateFields"))
    update.set(qn("val"), "false")
    return etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes")


def patch_relationships(data: bytes) -> bytes:
    parser = etree.XMLParser(remove_blank_text=False)
    root = etree.fromstring(data, parser)
    for node in list(root):
        if "comments" in node.get("Type", "").lower():
            root.remove(node)
    return etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes")


def patch_content_types(data: bytes) -> bytes:
    parser = etree.XMLParser(remove_blank_text=False)
    root = etree.fromstring(data, parser)
    for node in list(root):
        if "comments" in node.get("PartName", "").lower():
            root.remove(node)
    return etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes")


def repair(source: Path, output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(source, "r") as archive:
        with tempfile.NamedTemporaryFile(delete=False, suffix=".docx", dir=output.parent) as stream:
            temp_path = Path(stream.name)
        try:
            with zipfile.ZipFile(temp_path, "w") as target:
                for item in archive.infolist():
                    if item.filename.startswith("word/comments"):
                        continue
                    data = archive.read(item.filename)
                    if item.filename == "word/document.xml":
                        data = patch_document(data)
                    elif item.filename == "word/settings.xml":
                        data = patch_settings(data)
                    elif item.filename == "word/_rels/document.xml.rels":
                        data = patch_relationships(data)
                    elif item.filename == "[Content_Types].xml":
                        data = patch_content_types(data)
                    target.writestr(item, data)
            with zipfile.ZipFile(temp_path, "r") as check:
                bad = check.testzip()
                if bad:
                    raise RuntimeError(f"Corrupt DOCX member: {bad}")
            shutil.move(str(temp_path), str(output))
        finally:
            temp_path.unlink(missing_ok=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    repair(args.source, args.output)
    print(args.output.resolve())


if __name__ == "__main__":
    main()
