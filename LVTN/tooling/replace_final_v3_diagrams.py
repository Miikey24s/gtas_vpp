"""Replace selected thesis diagrams without rebuilding the Word document.

The script edits only image relationships and drawing extents that are anchored
immediately before known captions. Paragraphs, tables, styles, bookmarks,
headers, footers and fields are copied byte-for-byte from the input package.
"""

from __future__ import annotations

import argparse
import shutil
import tempfile
import zipfile
from pathlib import Path

from lxml import etree
from PIL import Image


WORD_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
REL_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
PKG_REL_NS = "http://schemas.openxmlformats.org/package/2006/relationships"
DRAWING_NS = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"
DRAWING_MAIN_NS = "http://schemas.openxmlformats.org/drawingml/2006/main"

NS = {
    "w": WORD_NS,
    "r": REL_NS,
    "pr": PKG_REL_NS,
    "wp": DRAWING_NS,
    "a": DRAWING_MAIN_NS,
}

EMU_PER_CM = 360_000
MAX_WIDTH_CM = 15.9
MAX_HEIGHT_CM = 19.0


REPLACEMENTS = {
    "Hình 2-1:": "LVTN/diagrams/ch02/architecture-overview.png",
    "Hình 2-2:": "LVTN/diagrams/ch02/functional-decomposition.png",
    "Hình 2-3:": "LVTN/diagrams/ch02/use-case-overview.png",
    "Hình 3-2:": "LVTN/diagrams/ch03/data-models/02-logical-organization-access.png",
    "Hình 3-3:": "LVTN/diagrams/ch03/data-models/03-logical-catalog-pricing.png",
    "Hình 3-4:": "LVTN/diagrams/ch03/data-models/05-logical-period-requests.png",
    "Hình 3-5:": "LVTN/diagrams/ch03/data-models/06-logical-settlement.png",
    "Hình 3-6:": "LVTN/diagrams/ch03/data-models/07-logical-identity-views-technical.png",
    "Hình 3-12:": "LVTN/diagrams/ch03/use-case-manage-catalog.png",
    "Hình 3-17:": "LVTN/diagrams/ch03/sequence-login-permissions.png",
    "Hình 3-18:": "LVTN/diagrams/ch03/sequence-create-regular-request.png",
    "Hình 3-19:": "LVTN/diagrams/ch03/sequence-create-additional-request.png",
    "Hình 3-20:": "LVTN/diagrams/ch03/sequence-approve-additional-request.png",
    "Hình 3-21:": "LVTN/diagrams/ch03/sequence-settle-period.png",
    "Hình 3-22:": "LVTN/diagrams/ch03/sequence-manage-pricing.png",
    "Hình 3-23:": "LVTN/diagrams/ch03/activity-create-regular-request.png",
    "Hình 3-24:": "LVTN/diagrams/ch03/activity-additional-request.png",
    "Hình 3-25:": "LVTN/diagrams/ch03/activity-settle-period.png",
    "Hình 3-26:": "LVTN/diagrams/ch03/activity-manage-pricing.png",
    "Hình 3-27:": "LVTN/diagrams/ch03/activity-manage-permissions.png",
}


def paragraph_text(paragraph: etree._Element) -> str:
    return "".join(paragraph.xpath(".//w:t/text()", namespaces=NS)).strip()


def find_image_paragraph(caption: etree._Element) -> etree._Element:
    sibling = caption.getprevious()
    for _ in range(5):
        if sibling is None:
            break
        if sibling.tag == f"{{{WORD_NS}}}p" and sibling.xpath(".//a:blip", namespaces=NS):
            return sibling
        sibling = sibling.getprevious()
    raise RuntimeError(f"No image found before caption: {paragraph_text(caption)}")


def drawing_size(image_path: Path) -> tuple[int, int]:
    with Image.open(image_path) as image:
        pixel_width, pixel_height = image.size
    ratio = pixel_width / pixel_height
    width_cm = min(MAX_WIDTH_CM, MAX_HEIGHT_CM * ratio)
    height_cm = width_cm / ratio
    return round(width_cm * EMU_PER_CM), round(height_cm * EMU_PER_CM)


def replace_diagrams(input_docx: Path, output_docx: Path, repository_root: Path) -> None:
    with zipfile.ZipFile(input_docx, "r") as source:
        entries = {name: source.read(name) for name in source.namelist()}

    document = etree.fromstring(entries["word/document.xml"])
    relationships = etree.fromstring(entries["word/_rels/document.xml.rels"])
    relationship_by_id = {
        relationship.get("Id"): relationship
        for relationship in relationships.findall(f"{{{PKG_REL_NS}}}Relationship")
    }

    body = document.find(f"{{{WORD_NS}}}body")
    if body is None:
        raise RuntimeError("The Word document has no body element.")

    caption_by_prefix: dict[str, etree._Element] = {}
    for paragraph in body.findall(f"{{{WORD_NS}}}p"):
        text = paragraph_text(paragraph)
        for prefix in REPLACEMENTS:
            if text.startswith(prefix):
                try:
                    find_image_paragraph(paragraph)
                except RuntimeError:
                    # The table of figures repeats caption text but has no image.
                    continue
                if prefix in caption_by_prefix:
                    raise RuntimeError(f"Duplicate body caption: {prefix}")
                caption_by_prefix[prefix] = paragraph

    missing = sorted(set(REPLACEMENTS) - set(caption_by_prefix))
    if missing:
        raise RuntimeError(f"Missing captions: {', '.join(missing)}")

    replaced_targets: set[str] = set()
    for prefix, relative_image in REPLACEMENTS.items():
        image_path = repository_root / relative_image
        if not image_path.is_file():
            raise FileNotFoundError(image_path)

        image_paragraph = find_image_paragraph(caption_by_prefix[prefix])
        blips = image_paragraph.xpath(".//a:blip", namespaces=NS)
        if len(blips) != 1:
            raise RuntimeError(f"Expected one image before {prefix}, found {len(blips)}")

        relationship_id = blips[0].get(f"{{{REL_NS}}}embed")
        relationship = relationship_by_id.get(relationship_id)
        if relationship is None:
            raise RuntimeError(f"Missing relationship {relationship_id} for {prefix}")

        target = relationship.get("Target")
        if target is None or not target.lower().endswith(".png"):
            raise RuntimeError(f"Expected a PNG media target for {prefix}, found {target!r}")
        package_target = str(Path("word") / Path(target.replace("/", "\\"))).replace("\\", "/")
        if package_target in replaced_targets:
            raise RuntimeError(f"Shared media target would replace multiple figures: {package_target}")
        replaced_targets.add(package_target)
        entries[package_target] = image_path.read_bytes()

        width_emu, height_emu = drawing_size(image_path)
        for extent in image_paragraph.xpath(".//wp:extent", namespaces=NS):
            extent.set("cx", str(width_emu))
            extent.set("cy", str(height_emu))
        for extent in image_paragraph.xpath(".//a:xfrm/a:ext", namespaces=NS):
            extent.set("cx", str(width_emu))
            extent.set("cy", str(height_emu))

        for doc_property in image_paragraph.xpath(".//wp:docPr", namespaces=NS):
            doc_property.set("descr", paragraph_text(caption_by_prefix[prefix]))

        print(
            f"{prefix} {image_path.name} -> "
            f"{width_emu / EMU_PER_CM:.2f} x {height_emu / EMU_PER_CM:.2f} cm"
        )

    entries["word/document.xml"] = etree.tostring(
        document, xml_declaration=True, encoding="UTF-8", standalone=True
    )
    entries["word/_rels/document.xml.rels"] = etree.tostring(
        relationships, xml_declaration=True, encoding="UTF-8", standalone=True
    )

    output_docx.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(delete=False, suffix=".docx", dir=output_docx.parent) as handle:
        temporary_path = Path(handle.name)
    try:
        with zipfile.ZipFile(temporary_path, "w", zipfile.ZIP_DEFLATED) as destination:
            for name, data in entries.items():
                destination.writestr(name, data)
        shutil.move(temporary_path, output_docx)
    finally:
        temporary_path.unlink(missing_ok=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("input_docx", type=Path)
    parser.add_argument("output_docx", type=Path)
    parser.add_argument("--repository-root", type=Path, default=Path(__file__).resolve().parents[2])
    arguments = parser.parse_args()
    replace_diagrams(
        arguments.input_docx.resolve(),
        arguments.output_docx.resolve(),
        arguments.repository_root.resolve(),
    )


if __name__ == "__main__":
    main()
