from __future__ import annotations

import hashlib
import json
import re
import zipfile
from collections import Counter
from pathlib import Path
from xml.etree import ElementTree as ET


CHECKPOINT = Path(r"D:\WORK\gtas_vpp\LVTN\checkpoints\03D_chuong3_giaodien_baobieu.docx")
WORKING = Path(r"D:\WORK\gtas_vpp\LVTN\NguyenAnNam_DH52201078_working.docx")

NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "wp": "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
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
    document_xml = archive.read("word/document.xml")
    document = ET.fromstring(document_xml)
    relationships = ET.fromstring(archive.read("word/_rels/document.xml.rels"))

    media = sorted(name for name in names if name.startswith("word/media/") and not name.endswith("/"))
    image_targets = []
    for rel in relationships.findall("pr:Relationship", NS):
        if rel.attrib.get("Type", "").endswith("/image"):
            target = rel.attrib["Target"].replace("\\", "/")
            image_targets.append("word/" + target.lstrip("/"))

    text = "".join(node.text or "" for node in document.iter(f"{{{NS['w']}}}t"))
    caption_ids = re.findall(r"Hình\s+(\d+-\d+):", text)
    caption_counts = Counter(caption_ids)
    expected_ids = ["2-1", "2-2", "2-3"] + [f"3-{index}" for index in range(1, 35)]

    tracked = {
        name: len(document.findall(f".//w:{name}", NS))
        for name in ("ins", "del", "moveFrom", "moveTo")
    }
    comment_parts = sorted(name for name in names if name.startswith("word/comments"))
    hyperlinks = document.findall(".//w:hyperlink", NS)
    toc_bookmarks = [
        node.attrib.get(f"{{{NS['w']}}}name", "")
        for node in document.findall(".//w:bookmarkStart", NS)
        if node.attrib.get(f"{{{NS['w']}}}name", "").startswith("_Toc")
    ]

    result = {
        "zip_bad_member": bad_member,
        "bytes": CHECKPOINT.stat().st_size,
        "sha256_checkpoint": sha256(CHECKPOINT),
        "sha256_working": sha256(WORKING),
        "checkpoint_matches_working": sha256(CHECKPOINT) == sha256(WORKING),
        "media_total": len(media),
        "media_png": sum(name.lower().endswith(".png") for name in media),
        "media_svg": sum(name.lower().endswith(".svg") for name in media),
        "image_relationships": len(image_targets),
        "orphan_media": sorted(set(media) - set(image_targets)),
        "missing_media_targets": sorted(set(image_targets) - set(media)),
        "inline_count": len(document.findall(".//wp:inline", NS)),
        "anchor_count": len(document.findall(".//wp:anchor", NS)),
        "tracked_changes": tracked,
        "comment_parts": comment_parts,
        "caption_occurrences": len(caption_ids),
        "caption_unique": len(caption_counts),
        "caption_wrong_multiplicity": {
            key: caption_counts.get(key, 0)
            for key in expected_ids
            if caption_counts.get(key, 0) != 2
        },
        "unexpected_caption_ids": sorted(set(caption_counts) - set(expected_ids)),
        "new_text_present": {
            value: value in text
            for value in (
                "3.3.4 Giao diện tổng hợp và báo biểu",
                "3.3.4.1 Tổng hợp toàn doanh nghiệp theo kỳ",
                "Hình 3-34: Giao diện tổng hợp toàn doanh nghiệp theo kỳ",
            )
        },
        "stale_text_present": {
            value: value in text
            for value in (
                "Error! Bookmark not defined.",
                "3.3.4 Giao diện báo cáo",
                "Hình 3-34: Giao diện báo cáo tổng hợp theo kỳ",
            )
        },
        "hyperlinks": len(hyperlinks),
        "toc_bookmarks": len(toc_bookmarks),
    }

print(json.dumps(result, ensure_ascii=False, indent=2))
