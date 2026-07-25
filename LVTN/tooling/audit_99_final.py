from __future__ import annotations

import hashlib
import json
import posixpath
import re
import sys
import zipfile
from collections import Counter
from pathlib import Path
from xml.etree import ElementTree as ET


sys.stdout.reconfigure(encoding="utf-8")

LVTN_ROOT = Path(__file__).resolve().parents[1]
FINAL = Path(sys.argv[1]) if len(sys.argv) > 1 else LVTN_ROOT / "checkpoints" / "99_final.docx"
WORKING = Path(sys.argv[2]) if len(sys.argv) > 2 else LVTN_ROOT / "NguyenAnNam_DH52201078_working.docx"

NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "wp": "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    "pr": "http://schemas.openxmlformats.org/package/2006/relationships",
}
W = f"{{{NS['w']}}}"
R = f"{{{NS['r']}}}"
FIGURE_RE = re.compile(r"^Hình\s+(\d+-\d+):\s*(.+)$")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def paragraph_text(node: ET.Element) -> str:
    return "".join(child.text or "" for child in node.iter(W + "t")).strip()


def paragraph_style(node: ET.Element) -> str:
    style = node.find("w:pPr/w:pStyle", NS)
    return style.attrib.get(W + "val", "") if style is not None else ""


def resolve_word_target(target: str) -> str:
    target = target.replace("\\", "/")
    if target.startswith("/"):
        return target.lstrip("/")
    return posixpath.normpath(posixpath.join("word", target))


def int_attr(node: ET.Element | None, name: str) -> int | None:
    if node is None:
        return None
    value = node.attrib.get(W + name)
    return int(value) if value is not None else None


with zipfile.ZipFile(FINAL) as archive:
    bad_member = archive.testzip()
    names = set(archive.namelist())
    root = ET.fromstring(archive.read("word/document.xml"))
    rels = ET.fromstring(archive.read("word/_rels/document.xml.rels"))
    settings = ET.fromstring(archive.read("word/settings.xml"))

    body_paragraphs = root.findall("./w:body/w:p", NS)
    all_text = "".join(paragraph_text(paragraph) for paragraph in body_paragraphs)

    bookmark_starts = root.findall(".//w:bookmarkStart", NS)
    bookmark_ends = root.findall(".//w:bookmarkEnd", NS)
    bookmark_names = [node.attrib.get(W + "name", "") for node in bookmark_starts]
    bookmark_ids = [node.attrib.get(W + "id", "") for node in bookmark_starts]
    bookmark_end_ids = [node.attrib.get(W + "id", "") for node in bookmark_ends]
    bookmark_name_set = set(bookmark_names)

    hyperlinks = root.findall(".//w:hyperlink", NS)
    internal_links = [node for node in hyperlinks if node.attrib.get(W + "anchor")]
    external_links = [node for node in hyperlinks if node.attrib.get(R + "id")]
    internal_anchors = [node.attrib.get(W + "anchor", "") for node in internal_links]

    rel_by_id = {
        rel.attrib["Id"]: rel
        for rel in rels.findall("pr:Relationship", NS)
    }
    used_rel_ids = {
        node.attrib.get(R + "id", "")
        for node in root.iter()
        if node.attrib.get(R + "id")
    }
    hyperlink_rels = [
        rel for rel in rel_by_id.values()
        if rel.attrib.get("Type", "").endswith("/hyperlink")
    ]
    broken_external_link_ids = sorted({
        node.attrib.get(R + "id", "")
        for node in external_links
        if node.attrib.get(R + "id", "") not in rel_by_id
    })
    orphan_hyperlink_rel_ids = sorted(
        rel.attrib["Id"] for rel in hyperlink_rels if rel.attrib["Id"] not in used_rel_ids
    )

    missing_internal_relationship_targets = []
    for rel in rel_by_id.values():
        if rel.attrib.get("TargetMode") == "External":
            continue
        target = resolve_word_target(rel.attrib.get("Target", ""))
        if target not in names:
            missing_internal_relationship_targets.append((rel.attrib["Id"], target))

    title_index = next(
        index for index, paragraph in enumerate(body_paragraphs)
        if paragraph_text(paragraph) == "MỤC LỤC CÁC HÌNH VẼ"
    )
    chapter_one_index = next(
        index for index, paragraph in enumerate(body_paragraphs)
        if paragraph_style(paragraph) == "Heading1"
        and paragraph_text(paragraph).upper().startswith("CHƯƠNG 1.")
    )
    figure_list_paragraphs = [
        paragraph
        for paragraph in body_paragraphs[title_index + 1:chapter_one_index]
        if paragraph.find("w:hyperlink", NS) is not None
        and paragraph.find("w:hyperlink", NS).attrib.get(W + "anchor", "").startswith("fig_")
    ]

    figure_list_records = []
    for paragraph in figure_list_paragraphs:
        hyperlink = paragraph.find("w:hyperlink", NS)
        anchor = hyperlink.attrib.get(W + "anchor", "")
        visible_texts = [node.text or "" for node in hyperlink.findall(".//w:t", NS)]
        caption_text = visible_texts[0] if visible_texts else ""
        page_text = visible_texts[-1] if len(visible_texts) > 1 else ""
        instructions = [" ".join((node.text or "").split()) for node in hyperlink.findall(".//w:instrText", NS)]
        page_break = paragraph.find("w:pPr/w:pageBreakBefore", NS) is not None
        figure_list_records.append({
            "anchor": anchor,
            "caption": caption_text,
            "page": page_text,
            "instructions": instructions,
            "page_break_before": page_break,
        })

    body_caption_records = []
    for paragraph in body_paragraphs[chapter_one_index + 1:]:
        text = paragraph_text(paragraph)
        match = FIGURE_RE.match(text)
        if not match:
            continue
        bookmarks = [
            node.attrib.get(W + "name", "")
            for node in paragraph.findall(".//w:bookmarkStart", NS)
            if node.attrib.get(W + "name", "").startswith("fig_")
        ]
        body_caption_records.append({
            "id": match.group(1),
            "text": text,
            "bookmarks": bookmarks,
        })

    expected_caption_by_anchor = {
        "fig_" + record["id"].replace("-", "_"): record["text"]
        for record in body_caption_records
    }
    list_caption_mismatches = [
        record
        for record in figure_list_records
        if expected_caption_by_anchor.get(record["anchor"]) != record["caption"]
    ]
    invalid_figure_pages = [
        record for record in figure_list_records if not re.fullmatch(r"\d+", record["page"])
    ]
    invalid_figure_pageref_fields = [
        record
        for record in figure_list_records
        if record["instructions"] != [f"PAGEREF {record['anchor']} \\h"]
    ]

    toc_paragraphs = [
        paragraph
        for paragraph in body_paragraphs
        if paragraph_style(paragraph) in {"TOC1", "TOC2", "TOC3"}
        and paragraph_text(paragraph)
    ]
    toc_anchors = [
        hyperlink.attrib.get(W + "anchor", "")
        for paragraph in toc_paragraphs
        for hyperlink in paragraph.findall(".//w:hyperlink", NS)
        if hyperlink.attrib.get(W + "anchor")
    ]
    toc_field_instructions = [
        " ".join((node.text or "").split())
        for paragraph in toc_paragraphs
        for node in paragraph.findall(".//w:instrText", NS)
        if (node.text or "").strip().startswith("TOC ")
    ]

    sections = root.findall(".//w:sectPr", NS)
    section_geometry = []
    for section in sections:
        page_size = section.find("w:pgSz", NS)
        margins = section.find("w:pgMar", NS)
        section_geometry.append({
            "width": int_attr(page_size, "w"),
            "height": int_attr(page_size, "h"),
            "orientation": page_size.attrib.get(W + "orient", "portrait") if page_size is not None else None,
            "top": int_attr(margins, "top"),
            "right": int_attr(margins, "right"),
            "bottom": int_attr(margins, "bottom"),
            "left": int_attr(margins, "left"),
            "page_borders": len(section.findall("w:pgBorders", NS)),
            "title_page": section.find("w:titlePg", NS) is not None,
            "header_refs": len(section.findall("w:headerReference", NS)),
            "footer_refs": len(section.findall("w:footerReference", NS)),
        })

    media = sorted(name for name in names if name.startswith("word/media/") and not name.endswith("/"))
    image_targets = [
        resolve_word_target(rel.attrib["Target"])
        for rel in rel_by_id.values()
        if rel.attrib.get("Type", "").endswith("/image")
    ]

    figure_occurrences = re.findall(r"Hình\s+(\d+-\d+):", all_text)
    figure_counts = Counter(figure_occurrences)
    expected_figure_ids = ["2-1", "2-2", "2-3"] + [f"3-{index}" for index in range(1, 44)]

    result = {
        "zip_bad_member": bad_member,
        "bytes": FINAL.stat().st_size,
        "sha256_final": sha256(FINAL),
        "sha256_working": sha256(WORKING) if WORKING.exists() else None,
        "final_matches_working": WORKING.exists() and sha256(FINAL) == sha256(WORKING),
        "section_count": len(sections),
        "section_geometry": section_geometry,
        "page_border_count": sum(item["page_borders"] for item in section_geometry),
        "table_count": len(root.findall(".//w:body/w:tbl", NS)),
        "toc_field_instructions": toc_field_instructions,
        "toc_entry_count": len(toc_paragraphs),
        "toc_link_count": len(toc_anchors),
        "toc_unresolved_anchors": sorted(set(toc_anchors) - bookmark_name_set),
        "figure_list_entry_count": len(figure_list_records),
        "figure_bookmark_count": len([name for name in bookmark_names if name.startswith("fig_")]),
        "figure_list_link_count": len([anchor for anchor in internal_anchors if anchor.startswith("fig_")]),
        "figure_list_caption_mismatches": list_caption_mismatches,
        "figure_list_invalid_page_results": invalid_figure_pages,
        "figure_list_invalid_pageref_fields": invalid_figure_pageref_fields,
        "figure_list_page_break_entries": [
            index + 1 for index, record in enumerate(figure_list_records) if record["page_break_before"]
        ],
        "body_caption_count": len(body_caption_records),
        "body_caption_missing_or_extra_bookmarks": [
            record for record in body_caption_records
            if record["bookmarks"] != ["fig_" + record["id"].replace("-", "_")]
        ],
        "reference_bookmark_count": len([name for name in bookmark_names if re.fullmatch(r"ref_\d{2}", name)]),
        "reference_internal_link_count": len([anchor for anchor in internal_anchors if re.fullmatch(r"ref_\d{2}", anchor)]),
        "hyperlinks_total": len(hyperlinks),
        "internal_hyperlinks": len(internal_links),
        "external_hyperlinks": len(external_links),
        "unresolved_internal_anchors": sorted(set(internal_anchors) - bookmark_name_set),
        "broken_external_link_ids": broken_external_link_ids,
        "orphan_hyperlink_relationship_ids": orphan_hyperlink_rel_ids,
        "missing_internal_relationship_targets": missing_internal_relationship_targets,
        "bookmark_names_unique": len(bookmark_names) == len(set(bookmark_names)),
        "bookmark_ids_unique": len(bookmark_ids) == len(set(bookmark_ids)),
        "bookmark_start_end_ids_match": Counter(bookmark_ids) == Counter(bookmark_end_ids),
        "media_total": len(media),
        "media_png": sum(name.lower().endswith(".png") for name in media),
        "media_svg": sum(name.lower().endswith(".svg") for name in media),
        "image_relationships": len(image_targets),
        "orphan_media": sorted(set(media) - set(image_targets)),
        "missing_media_targets": sorted(set(image_targets) - set(media)),
        "inline_count": len(root.findall(".//wp:inline", NS)),
        "anchor_count": len(root.findall(".//wp:anchor", NS)),
        "figure_caption_occurrences": len(figure_occurrences),
        "figure_caption_unique": len(figure_counts),
        "figure_caption_wrong_multiplicity": {
            key: figure_counts.get(key, 0)
            for key in expected_figure_ids
            if figure_counts.get(key, 0) != 2
        },
        "unexpected_figure_caption_ids": sorted(set(figure_counts) - set(expected_figure_ids)),
        "tracked_changes": {
            name: len(root.findall(f".//w:{name}", NS))
            for name in ("ins", "del", "moveFrom", "moveTo")
        },
        "comment_parts": sorted(name for name in names if name.startswith("word/comments")),
        "update_fields_on_open": [
            node.attrib.get(W + "val", "") for node in settings.findall(".//w:updateFields", NS)
        ],
        "error_text_present": {
            value: value in all_text
            for value in (
                "Error! Bookmark not defined.",
                "Error! Reference source not found.",
                "Error! No table of figures entries found.",
            )
        },
        "corrected_figure_names_present": {
            value: value in all_text
            for value in (
                "Hình 2-2: Sơ đồ chức năng",
                "Hình 2-3: Sơ đồ use case tổng quát hệ thống",
            )
        },
    }

print(json.dumps(result, ensure_ascii=False, indent=2))
