from __future__ import annotations

import argparse
import re
import shutil
import tempfile
import zipfile
from pathlib import Path

from lxml import etree


W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
NS = {"w": W_NS}
W_T = f"{{{W_NS}}}t"
W_P = f"{{{W_NS}}}p"
W_R = f"{{{W_NS}}}r"
W_HYPERLINK = f"{{{W_NS}}}hyperlink"

CITATION_TOKEN = re.compile(r"\[\d+\]")
CITATION_CLUSTER = re.compile(r"\s*\[\d+\](?:\s*,\s*\[\d+\])*")


def element_text(element: etree._Element) -> str:
    return "".join(element.xpath(".//w:t/text()", namespaces=NS))


def trim_trailing_space(element: etree._Element) -> None:
    text_nodes = element.xpath(".//w:t", namespaces=NS)
    if not text_nodes:
        return
    node = text_nodes[-1]
    node.text = (node.text or "").rstrip()
    if node.get("{http://www.w3.org/XML/1998/namespace}space") == "preserve":
        node.attrib.pop("{http://www.w3.org/XML/1998/namespace}space", None)


def remove_hyperlink_clusters(paragraph: etree._Element) -> int:
    children = list(paragraph)
    removed = 0
    index = 0

    while index < len(children):
        child = children[index]
        if child.tag != W_HYPERLINK or not re.fullmatch(r"\[\d+\]", element_text(child)):
            index += 1
            continue

        cluster_start = index
        cluster_end = index
        token_count = 1

        while cluster_end + 2 < len(children):
            separator = children[cluster_end + 1]
            next_child = children[cluster_end + 2]
            if (
                separator.tag == W_R
                and re.fullmatch(r"\s*,\s*", element_text(separator))
                and next_child.tag == W_HYPERLINK
                and re.fullmatch(r"\[\d+\]", element_text(next_child))
            ):
                cluster_end += 2
                token_count += 1
                continue
            break

        if cluster_start > 0:
            trim_trailing_space(children[cluster_start - 1])

        for child_to_remove in children[cluster_start : cluster_end + 1]:
            paragraph.remove(child_to_remove)

        removed += token_count
        children = list(paragraph)
        index = cluster_start

    return removed


def remove_plain_text_citations(paragraph: etree._Element) -> int:
    removed = 0
    for text_node in paragraph.xpath(".//w:t", namespaces=NS):
        original = text_node.text or ""
        count = len(CITATION_TOKEN.findall(original))
        if not count:
            continue

        updated = CITATION_CLUSTER.sub("", original)
        text_node.text = updated
        removed += count

        if updated.startswith(" ") or updated.endswith(" "):
            text_node.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
        else:
            text_node.attrib.pop("{http://www.w3.org/XML/1998/namespace}space", None)

    return removed


def patch_document_xml(xml_bytes: bytes) -> tuple[bytes, int, int]:
    parser = etree.XMLParser(remove_blank_text=False, resolve_entities=False)
    root = etree.fromstring(xml_bytes, parser)

    in_references = False
    removed_tokens = 0
    changed_paragraphs = 0

    for paragraph in root.xpath("//w:p", namespaces=NS):
        text = element_text(paragraph).strip()
        if text == "TÀI LIỆU THAM KHẢO":
            in_references = True
            continue
        if in_references or not CITATION_TOKEN.search(text):
            continue

        removed_here = remove_hyperlink_clusters(paragraph)
        removed_here += remove_plain_text_citations(paragraph)
        if removed_here:
            removed_tokens += removed_here
            changed_paragraphs += 1

    return (
        etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes"),
        removed_tokens,
        changed_paragraphs,
    )


def patch_docx(source: Path, destination: Path) -> tuple[int, int]:
    with zipfile.ZipFile(source, "r") as input_zip:
        document_xml = input_zip.read("word/document.xml")
        patched_xml, removed_tokens, changed_paragraphs = patch_document_xml(document_xml)

        with tempfile.NamedTemporaryFile(suffix=".docx", delete=False) as temporary_file:
            temporary_path = Path(temporary_file.name)

        try:
            with zipfile.ZipFile(temporary_path, "w") as output_zip:
                for item in input_zip.infolist():
                    payload = patched_xml if item.filename == "word/document.xml" else input_zip.read(item.filename)
                    output_zip.writestr(item, payload)

            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.move(str(temporary_path), str(destination))
        finally:
            temporary_path.unlink(missing_ok=True)

    return removed_tokens, changed_paragraphs


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Remove inline numeric citation markers before the bibliography while preserving bibliography entries."
    )
    parser.add_argument("input", type=Path)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()

    removed_tokens, changed_paragraphs = patch_docx(args.input.resolve(), args.out.resolve())
    print(f"Removed {removed_tokens} inline citation markers from {changed_paragraphs} paragraphs.")
    print(f"Output: {args.out.resolve()}")


if __name__ == "__main__":
    main()
