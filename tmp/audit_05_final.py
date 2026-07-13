from __future__ import annotations

import hashlib
import json
import posixpath
import re
import zipfile
from collections import Counter
from pathlib import Path
from urllib.parse import urlparse
from xml.etree import ElementTree as ET


CHECKPOINT = Path(r"D:\WORK\gtas_vpp\LVTN\checkpoints\05_chuong5_phuluc_tltk.docx")
WORKING = Path(r"D:\WORK\gtas_vpp\LVTN\NguyenAnNam_DH52201078_working.docx")

NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "wp": "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    "pr": "http://schemas.openxmlformats.org/package/2006/relationships",
}
W = f"{{{NS['w']}}}"
R = f"{{{NS['r']}}}"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def int_attr(node: ET.Element | None, local_name: str) -> int | None:
    if node is None:
        return None
    value = node.attrib.get(W + local_name)
    return int(value) if value is not None else None


def rel_target_from_document(target: str) -> str:
    return posixpath.normpath(posixpath.join("word", target.replace("\\", "/")))


with zipfile.ZipFile(CHECKPOINT) as archive:
    bad_member = archive.testzip()
    names = set(archive.namelist())
    document = ET.fromstring(archive.read("word/document.xml"))
    rels = ET.fromstring(archive.read("word/_rels/document.xml.rels"))

    rel_by_id = {
        rel.attrib["Id"]: rel
        for rel in rels.findall("pr:Relationship", NS)
    }
    hyperlinks = document.findall(".//w:hyperlink", NS)
    bookmark_starts = document.findall(".//w:bookmarkStart", NS)
    bookmark_ends = document.findall(".//w:bookmarkEnd", NS)
    bookmark_names = [node.attrib.get(W + "name", "") for node in bookmark_starts]
    bookmark_ids = [node.attrib.get(W + "id", "") for node in bookmark_starts]
    bookmark_end_ids = [node.attrib.get(W + "id", "") for node in bookmark_ends]
    bookmark_name_set = set(bookmark_names)

    internal_links = [node for node in hyperlinks if node.attrib.get(W + "anchor")]
    citation_internal_links = [
        node
        for node in internal_links
        if re.fullmatch(r"ref_\d{2}", node.attrib.get(W + "anchor", ""))
    ]
    unresolved_anchors = sorted({
        node.attrib.get(W + "anchor", "")
        for node in internal_links
        if node.attrib.get(W + "anchor", "") not in bookmark_name_set
    })

    external_links = [node for node in hyperlinks if node.attrib.get(R + "id")]
    broken_relationship_ids = sorted({
        node.attrib.get(R + "id", "")
        for node in external_links
        if node.attrib.get(R + "id", "") not in rel_by_id
    })
    used_rel_ids = {
        node.attrib.get(R + "id", "")
        for node in document.iter()
        if node.attrib.get(R + "id")
    }
    hyperlink_rels = [
        rel for rel in rel_by_id.values()
        if rel.attrib.get("Type", "").endswith("/hyperlink")
    ]
    orphan_hyperlink_rels = sorted(
        rel.attrib["Id"] for rel in hyperlink_rels if rel.attrib["Id"] not in used_rel_ids
    )
    external_targets = [
        rel_by_id[node.attrib[R + "id"]].attrib.get("Target", "")
        for node in external_links
        if node.attrib.get(R + "id", "") in rel_by_id
    ]
    invalid_external_targets = sorted({
        target
        for target in external_targets
        if urlparse(target).scheme not in {"http", "https", "mailto"}
    })

    media = sorted(name for name in names if name.startswith("word/media/") and not name.endswith("/"))
    image_targets = [
        rel_target_from_document(rel.attrib["Target"])
        for rel in rel_by_id.values()
        if rel.attrib.get("Type", "").endswith("/image")
    ]

    text = "".join(node.text or "" for node in document.iter(W + "t"))
    figure_ids = re.findall(r"Hình\s+(\d+-\d+):", text)
    figure_counts = Counter(figure_ids)
    expected_figure_ids = ["2-1", "2-2", "2-3"] + [f"3-{index}" for index in range(1, 35)]
    table_caption_ids = re.findall(r"Bảng\s+(\d+-\d+):", text)

    tracked = {
        name: len(document.findall(f".//w:{name}", NS))
        for name in ("ins", "del", "moveFrom", "moveTo")
    }
    comment_parts = sorted(name for name in names if name.startswith("word/comments"))

    tables = document.findall(".//w:body/w:tbl", NS)
    table_5_1_geometry = None
    for index, table in enumerate(tables, start=1):
        table_text = "".join(node.text or "" for node in table.iter(W + "t"))
        if "Mục tiêu/tiêu chí" not in table_text or "Kết quả và bằng chứng" not in table_text:
            continue
        tbl_pr = table.find("w:tblPr", NS)
        grid = [int_attr(node, "w") for node in table.findall("w:tblGrid/w:gridCol", NS)]
        row_sums = []
        for row in table.findall("w:tr", NS):
            widths = [int_attr(cell.find("w:tcPr/w:tcW", NS), "w") for cell in row.findall("w:tc", NS)]
            row_sums.append(sum(value for value in widths if value is not None))
        table_5_1_geometry = {
            "table_index": index,
            "tbl_width": int_attr(tbl_pr.find("w:tblW", NS), "w"),
            "indent": int_attr(tbl_pr.find("w:tblInd", NS), "w"),
            "grid_sum": sum(value for value in grid if value is not None),
            "row_sums": sorted(set(row_sums)),
            "rows": len(table.findall("w:tr", NS)),
        }
        break

    sections = document.findall(".//w:sectPr", NS)
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
        })

    required_text = (
        "Đạt một phần",
        "Đạt ở mức đề tài",
        "/report",
        "Microsoft.OpenApi 2.4.1",
        "SQLitePCLRaw.lib.e_sqlite3 2.1.11",
        "132/132",
        "26/26",
        "54/54",
        "snapshot ngày 28/05/2026",
        "REQUEST_ADMIN_APPROVAL",
        "PERIOD_SETTLE",
    )

    result = {
        "zip_bad_member": bad_member,
        "bytes": CHECKPOINT.stat().st_size,
        "sha256_checkpoint": sha256(CHECKPOINT),
        "sha256_working": sha256(WORKING),
        "checkpoint_matches_working": sha256(CHECKPOINT) == sha256(WORKING),
        "section_count": len(sections),
        "section_geometry": section_geometry,
        "table_count": len(tables),
        "table_5_1_geometry": table_5_1_geometry,
        "reference_bookmarks": sorted(name for name in bookmark_names if re.fullmatch(r"ref_\d{2}", name)),
        "bookmark_start_names_unique": len(bookmark_names) == len(set(bookmark_names)),
        "bookmark_start_ids_unique": len(bookmark_ids) == len(set(bookmark_ids)),
        "bookmark_start_end_ids_match": Counter(bookmark_ids) == Counter(bookmark_end_ids),
        "hyperlinks_total": len(hyperlinks),
        "internal_hyperlinks": len(internal_links),
        "citation_internal_links": len(citation_internal_links),
        "citation_anchor_counts": Counter(node.attrib.get(W + "anchor", "") for node in citation_internal_links),
        "unresolved_internal_anchors": unresolved_anchors,
        "external_hyperlinks": len(external_links),
        "external_hyperlink_relationships": len(hyperlink_rels),
        "external_targets_unique": len(set(external_targets)),
        "invalid_external_targets": invalid_external_targets,
        "broken_hyperlink_relationship_ids": broken_relationship_ids,
        "orphan_hyperlink_relationship_ids": orphan_hyperlink_rels,
        "media_total": len(media),
        "media_png": sum(name.lower().endswith(".png") for name in media),
        "media_svg": sum(name.lower().endswith(".svg") for name in media),
        "image_relationships": len(image_targets),
        "orphan_media": sorted(set(media) - set(image_targets)),
        "missing_media_targets": sorted(set(image_targets) - set(media)),
        "inline_count": len(document.findall(".//wp:inline", NS)),
        "anchor_count": len(document.findall(".//wp:anchor", NS)),
        "figure_caption_occurrences": len(figure_ids),
        "figure_caption_unique": len(figure_counts),
        "figure_caption_wrong_multiplicity": {
            key: figure_counts.get(key, 0)
            for key in expected_figure_ids
            if figure_counts.get(key, 0) != 2
        },
        "unexpected_figure_caption_ids": sorted(set(figure_counts) - set(expected_figure_ids)),
        "table_caption_ids": table_caption_ids,
        "table_5_1_caption_present": "5-1" in table_caption_ids,
        "tracked_changes": tracked,
        "comment_parts": comment_parts,
        "required_text_present": {value: value in text for value in required_text},
        "stale_or_error_text_present": {
            value: value in text
            for value in (
                "Error! Bookmark not defined.",
                "132 backend test và 26 frontend test đã pass ngày 11/07/2026",
                "Trang báo cáo đã hoàn thiện",
            )
        },
    }

print(json.dumps(result, ensure_ascii=False, indent=2))
