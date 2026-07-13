from __future__ import annotations

import hashlib
import json
import re
import zipfile
from collections import Counter
from pathlib import Path
from xml.etree import ElementTree as ET


CHECKPOINT = Path(r"D:\WORK\gtas_vpp\LVTN\checkpoints\04_chuong4_thunghiem.docx")
WORKING = Path(r"D:\WORK\gtas_vpp\LVTN\NguyenAnNam_DH52201078_working.docx")

NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "wp": "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
    "pr": "http://schemas.openxmlformats.org/package/2006/relationships",
}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


with zipfile.ZipFile(CHECKPOINT) as archive:
    bad_member = archive.testzip()
    names = set(archive.namelist())
    document = ET.fromstring(archive.read("word/document.xml"))
    rels = ET.fromstring(archive.read("word/_rels/document.xml.rels"))

    media = sorted(name for name in names if name.startswith("word/media/") and not name.endswith("/"))
    image_targets = []
    for rel in rels.findall("pr:Relationship", NS):
        if rel.attrib.get("Type", "").endswith("/image"):
            image_targets.append("word/" + rel.attrib["Target"].replace("\\", "/").lstrip("/"))

    text = "".join(node.text or "" for node in document.iter(f"{{{NS['w']}}}t"))
    figure_ids = re.findall(r"Hình\s+(\d+-\d+):", text)
    figure_counts = Counter(figure_ids)
    expected_figure_ids = ["2-1", "2-2", "2-3"] + [f"3-{index}" for index in range(1, 35)]
    table_caption_ids = re.findall(r"Bảng\s+4-(\d):", text)
    test_case_ids = re.findall(r"TC-(\d{2})", text)

    tracked = {
        name: len(document.findall(f".//w:{name}", NS))
        for name in ("ins", "del", "moveFrom", "moveTo")
    }
    comment_parts = sorted(name for name in names if name.startswith("word/comments"))

    tables = document.findall(".//w:body/w:tbl", NS)
    new_table_geometry = []
    for one_based_index in range(18, 23):
        table = tables[one_based_index - 1]
        tbl_pr = table.find("w:tblPr", NS)
        tbl_w = tbl_pr.find("w:tblW", NS)
        tbl_ind = tbl_pr.find("w:tblInd", NS)
        grid = [int(node.attrib[f"{{{NS['w']}}}w"]) for node in table.findall("w:tblGrid/w:gridCol", NS)]
        row_sums = []
        for row in table.findall("w:tr", NS):
            widths = [
                int(cell.find("w:tcPr/w:tcW", NS).attrib[f"{{{NS['w']}}}w"])
                for cell in row.findall("w:tc", NS)
            ]
            row_sums.append(sum(widths))
        new_table_geometry.append(
            {
                "table": one_based_index,
                "tbl_width": int(tbl_w.attrib[f"{{{NS['w']}}}w"]),
                "indent": int(tbl_ind.attrib[f"{{{NS['w']}}}w"]),
                "grid_sum": sum(grid),
                "row_sums": sorted(set(row_sums)),
            }
        )

    checkpoint_hash = sha256(CHECKPOINT)
    working_hash = sha256(WORKING)
    result = {
        "zip_bad_member": bad_member,
        "bytes": CHECKPOINT.stat().st_size,
        "sha256_checkpoint": checkpoint_hash,
        "sha256_working": working_hash,
        "checkpoint_matches_working": checkpoint_hash == working_hash,
        "media_total": len(media),
        "media_png": sum(name.lower().endswith(".png") for name in media),
        "media_svg": sum(name.lower().endswith(".svg") for name in media),
        "image_relationships": len(image_targets),
        "orphan_media": sorted(set(media) - set(image_targets)),
        "missing_media_targets": sorted(set(image_targets) - set(media)),
        "inline_count": len(document.findall(".//wp:inline", NS)),
        "anchor_count": len(document.findall(".//wp:anchor", NS)),
        "table_count": len(tables),
        "chapter4_table_geometry": new_table_geometry,
        "tracked_changes": tracked,
        "comment_parts": comment_parts,
        "figure_caption_occurrences": len(figure_ids),
        "figure_caption_unique": len(figure_counts),
        "figure_caption_wrong_multiplicity": {
            key: figure_counts.get(key, 0)
            for key in expected_figure_ids
            if figure_counts.get(key, 0) != 2
        },
        "chapter4_table_captions": table_caption_ids,
        "test_case_ids": test_case_ids,
        "test_case_sequence_ok": test_case_ids == [f"{index:02d}" for index in range(1, 14)],
        "required_text_present": {
            value: value in text
            for value in (
                "132",
                "26",
                "54/54",
                "Microsoft.OpenApi 2.4.1",
                "SQLitePCLRaw.lib.e_sqlite3 2.1.11",
                "snapshot ngày 28/05/2026",
                "chạy ngày 13/07/2026",
            )
        },
        "stale_or_error_text_present": {
            value: value in text
            for value in (
                "Error! Bookmark not defined.",
                "4.3 XỬ LÝ CÁC TRƯỜNG HỢP NGOẠI LỆ",
                "132 backend test và 26 frontend test đã pass ngày 11/07/2026",
            )
        },
        "hyperlinks": len(document.findall(".//w:hyperlink", NS)),
        "toc_bookmarks": len(
            [
                node
                for node in document.findall(".//w:bookmarkStart", NS)
                if node.attrib.get(f"{{{NS['w']}}}name", "").startswith("_Toc")
            ]
        ),
    }

print(json.dumps(result, ensure_ascii=False, indent=2))
