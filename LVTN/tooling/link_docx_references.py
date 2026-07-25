#!/usr/bin/env python3
"""Add clickable citation and reference links without changing visible text."""

from __future__ import annotations

import argparse
import copy
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
}
W = f"{{{NS['w']}}}"
R = f"{{{NS['r']}}}"
PR = f"{{{NS['pr']}}}"
XML_SPACE = "{http://www.w3.org/XML/1998/namespace}space"
CITATION_RE = re.compile(r"\[(1[0-8]|[1-9])\]")
URL_RE = re.compile(r"https://\S+")


def paragraph_text(paragraph: etree._Element) -> str:
    return "".join(paragraph.xpath(".//w:t/text()", namespaces=NS)).strip()


def cloned_run(original: etree._Element, text: str) -> etree._Element:
    run = etree.Element(W + "r")
    run_properties = original.find(W + "rPr")
    if run_properties is not None:
        run.append(copy.deepcopy(run_properties))
    node = etree.SubElement(run, W + "t")
    if text.startswith(" ") or text.endswith(" "):
        node.set(XML_SPACE, "preserve")
    node.text = text
    return run


def wrap_text_matches(
    paragraph: etree._Element,
    pattern: re.Pattern[str],
    hyperlink_factory,
) -> int:
    added = 0
    for text_node in list(paragraph.xpath(".//w:t", namespaces=NS)):
        run = text_node.getparent()
        if run.tag != W + "r" or run.getparent().tag == W + "hyperlink":
            continue
        value = text_node.text or ""
        matches = list(pattern.finditer(value))
        if not matches:
            continue
        parent = run.getparent()
        position = parent.index(run)
        cursor = 0
        replacements: list[etree._Element] = []
        for match in matches:
            if match.start() > cursor:
                replacements.append(cloned_run(run, value[cursor:match.start()]))
            hyperlink = hyperlink_factory(run, match)
            replacements.append(hyperlink)
            cursor = match.end()
            added += 1
        if cursor < len(value):
            replacements.append(cloned_run(run, value[cursor:]))
        parent.remove(run)
        for offset, replacement in enumerate(replacements):
            parent.insert(position + offset, replacement)
    return added


def patch_document(document_data: bytes, rels_root: etree._Element) -> tuple[bytes, int, int]:
    parser = etree.XMLParser(remove_blank_text=False)
    root = etree.fromstring(document_data, parser)
    paragraphs = root.xpath(".//w:body//w:p", namespaces=NS)
    reference_headings = [p for p in paragraphs if paragraph_text(p) == "TÀI LIỆU THAM KHẢO"]
    if not reference_headings:
        raise RuntimeError("Reference heading was not found.")
    reference_heading = reference_headings[-1]
    reference_index = paragraphs.index(reference_heading)

    citation_links = 0
    for paragraph in paragraphs[:reference_index]:
        citation_links += wrap_text_matches(
            paragraph,
            CITATION_RE,
            lambda original, match: make_internal_hyperlink(
                original,
                match.group(0),
                f"ref_{int(match.group(1)):02d}",
            ),
        )

    existing_ids = {
        rel.get("Id", "") for rel in rels_root.findall(PR + "Relationship")
    }
    next_number = 1

    def next_relationship_id() -> str:
        nonlocal next_number
        while f"rIdRef{next_number}" in existing_ids:
            next_number += 1
        value = f"rIdRef{next_number}"
        existing_ids.add(value)
        next_number += 1
        return value

    external_links = 0
    for paragraph in paragraphs[reference_index + 1:]:
        text = paragraph_text(paragraph)
        if not re.match(r"^\[\d+\]", text):
            continue

        def external_factory(original, match):
            nonlocal external_links
            raw_url = match.group(0)
            url = raw_url.rstrip(".,;")
            trailing = raw_url[len(url):]
            relationship_id = next_relationship_id()
            relationship = etree.SubElement(rels_root, PR + "Relationship")
            relationship.set("Id", relationship_id)
            relationship.set(
                "Type",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink",
            )
            relationship.set("Target", url)
            relationship.set("TargetMode", "External")
            hyperlink = etree.Element(W + "hyperlink")
            hyperlink.set(R + "id", relationship_id)
            hyperlink.set(W + "history", "1")
            hyperlink.append(cloned_run(original, url))
            if trailing:
                # The supplied references currently place punctuation after the
                # URL, but keep this branch for repeatable future use.
                hyperlink.tail = trailing
            external_links += 1
            return hyperlink

        wrap_text_matches(paragraph, URL_RE, external_factory)

    return (
        etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes"),
        citation_links,
        external_links,
    )


def make_internal_hyperlink(original: etree._Element, text: str, anchor: str) -> etree._Element:
    hyperlink = etree.Element(W + "hyperlink")
    hyperlink.set(W + "anchor", anchor)
    hyperlink.set(W + "history", "1")
    hyperlink.append(cloned_run(original, text))
    return hyperlink


def link_document(source: Path, output: Path) -> tuple[int, int]:
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(source, "r") as archive:
        rels_data = archive.read("word/_rels/document.xml.rels")
        parser = etree.XMLParser(remove_blank_text=False)
        rels_root = etree.fromstring(rels_data, parser)
        document_data, citations, references = patch_document(
            archive.read("word/document.xml"), rels_root
        )
        with tempfile.NamedTemporaryFile(delete=False, suffix=".docx", dir=output.parent) as stream:
            temp_path = Path(stream.name)
        try:
            with zipfile.ZipFile(temp_path, "w") as target:
                for item in archive.infolist():
                    data = archive.read(item.filename)
                    if item.filename == "word/document.xml":
                        data = document_data
                    elif item.filename == "word/_rels/document.xml.rels":
                        data = etree.tostring(
                            rels_root,
                            xml_declaration=True,
                            encoding="UTF-8",
                            standalone="yes",
                        )
                    target.writestr(item, data)
            with zipfile.ZipFile(temp_path, "r") as check:
                bad_member = check.testzip()
                if bad_member:
                    raise RuntimeError(f"Corrupt DOCX member: {bad_member}")
            shutil.move(str(temp_path), str(output))
        finally:
            temp_path.unlink(missing_ok=True)
    return citations, references


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    citation_count, reference_count = link_document(args.source, args.output)
    print(f"CITATION_LINKS={citation_count}")
    print(f"EXTERNAL_REFERENCE_LINKS={reference_count}")
    print(args.output.resolve())


if __name__ == "__main__":
    main()
