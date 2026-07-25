from __future__ import annotations

import argparse
import os
import shutil
import tempfile
import zipfile
from datetime import datetime
from pathlib import Path

from lxml import etree


W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
NS = {"w": W_NS}


def qn(local_name: str) -> str:
    return f"{{{W_NS}}}{local_name}"


def paragraph_text(paragraph: etree._Element) -> str:
    return "".join(paragraph.xpath(".//w:t/text()", namespaces=NS)).strip()


def child(parent: etree._Element, local_name: str) -> etree._Element:
    element = parent.find(qn(local_name))
    if element is None:
        element = etree.SubElement(parent, qn(local_name))
    return element


def patch_document_xml(data: bytes) -> tuple[bytes, list[tuple[str, str]]]:
    root = etree.fromstring(data)
    results: list[tuple[str, str]] = []

    for paragraph in root.xpath(
        "//w:body/w:p[w:pPr/w:pStyle[@w:val='Heading1']]", namespaces=NS
    ):
        text = paragraph_text(paragraph)
        if not (
            text.startswith("Chương 1.")
            or text.startswith("Chương 2.")
            or text.startswith("CHƯƠNG 3.")
            or text.startswith("CHƯƠNG 4.")
            or text.startswith("CHƯƠNG 5.")
        ):
            continue

        properties = paragraph.find(qn("pPr"))
        if properties is None:
            raise RuntimeError(f"Heading has no paragraph properties: {text}")

        # 840 twips = 42 pt. This matches the roughly three-line visual gap
        # shown in the faculty template without inserting empty paragraphs.
        spacing = child(properties, "spacing")
        spacing.set(qn("after"), "840")

        # The faculty sample places chapter titles on the right side of the
        # text area. Write the alignment explicitly so an old paragraph-level
        # center setting cannot override the corrected Heading 1 style.
        alignment = child(properties, "jc")
        alignment.set(qn("val"), "right")
        results.append((text, "right"))

    if len(results) != 5:
        raise RuntimeError(f"Expected 5 chapter headings, found {len(results)}")

    return (
        etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes"),
        results,
    )


def patch_docx(document: Path) -> tuple[Path, list[tuple[str, str]]]:
    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    backup = document.with_name(f"{document.stem}.pre-chapter-spacing-{timestamp}.docx")
    shutil.copy2(document, backup)

    handle, temporary_name = tempfile.mkstemp(suffix=".docx", dir=document.parent)
    os.close(handle)
    temporary = Path(temporary_name)
    results: list[tuple[str, str]] = []

    try:
        with zipfile.ZipFile(document, "r") as source, zipfile.ZipFile(temporary, "w") as target:
            for entry in source.infolist():
                data = source.read(entry.filename)
                if entry.filename == "word/document.xml":
                    data, results = patch_document_xml(data)
                target.writestr(entry, data)

        with zipfile.ZipFile(temporary, "r") as verification:
            bad_entry = verification.testzip()
            if bad_entry is not None:
                raise RuntimeError(f"Corrupt DOCX entry after patch: {bad_entry}")

        os.replace(temporary, document)
    finally:
        if temporary.exists():
            temporary.unlink()

    return backup, results


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("document", type=Path)
    args = parser.parse_args()

    document = args.document.resolve()
    backup, results = patch_docx(document)
    for title, alignment in results:
        print(f"CHAPTER={title}|ALIGNMENT={alignment}|SPACE_AFTER_PT=42")
    print(f"BACKUP={backup}")
    print(f"DOCX={document}")


if __name__ == "__main__":
    main()
