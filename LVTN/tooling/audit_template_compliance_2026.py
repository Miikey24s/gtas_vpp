from __future__ import annotations

import argparse
import json
import posixpath
import re
import sys
import zipfile
from collections import Counter
from pathlib import Path

from lxml import etree


sys.stdout.reconfigure(encoding="utf-8")
NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    "pr": "http://schemas.openxmlformats.org/package/2006/relationships",
    "wp": "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
}
W = f"{{{NS['w']}}}"
R = f"{{{NS['r']}}}"
CAPTION_RE = re.compile(r"^(Hình|Bảng)\s+(\d+-\d+):\s*(.+)$")


def text_of(node) -> str:
    return "".join(t.text or "" for t in node.findall(".//w:t", NS)).strip()


def style_of(p) -> str:
    style = p.find("w:pPr/w:pStyle", NS)
    return style.get(W + "val", "") if style is not None else ""


def resolve_word_target(target: str) -> str:
    target = target.replace("\\", "/")
    if target.startswith("/"):
        return target.lstrip("/")
    return posixpath.normpath(posixpath.join("word", target))


def run_props(run) -> dict:
    rpr = run.find("w:rPr", NS)
    def val(tag, attr="val"):
        node = rpr.find(f"w:{tag}", NS) if rpr is not None else None
        return node.get(W + attr) if node is not None else None
    return {
        "size": val("sz"),
        "color": val("color"),
        "underline": val("u"),
        "bold": val("b"),
        "italic": val("i"),
        "has_bold": rpr is not None and rpr.find("w:b", NS) is not None,
        "has_italic": rpr is not None and rpr.find("w:i", NS) is not None,
    }


parser = argparse.ArgumentParser()
parser.add_argument("docx", type=Path)
args = parser.parse_args()

with zipfile.ZipFile(args.docx) as archive:
    names = set(archive.namelist())
    bad_member = archive.testzip()
    root = etree.fromstring(archive.read("word/document.xml"))
    rels = etree.fromstring(archive.read("word/_rels/document.xml.rels"))
    styles = etree.fromstring(archive.read("word/styles.xml"))

    body_paragraphs = root.findall("./w:body/w:p", NS)
    chapter_start = next(
        i for i, p in enumerate(body_paragraphs)
        if style_of(p) == "Heading1" and text_of(p).upper().startswith("CHƯƠNG 1.")
    )
    figure_list_title = next(
        i for i, p in enumerate(body_paragraphs) if text_of(p) == "MỤC LỤC CÁC HÌNH VẼ"
    )

    bookmarks = {
        node.get(W + "name", "")
        for node in root.findall(".//w:bookmarkStart", NS)
        if node.get(W + "name")
    }
    hyperlinks = root.findall(".//w:hyperlink", NS)
    internal = [node for node in hyperlinks if node.get(W + "anchor")]
    external = [node for node in hyperlinks if node.get(R + "id")]
    missing_anchors = sorted({
        node.get(W + "anchor") for node in internal
        if node.get(W + "anchor") not in bookmarks
    })

    figure_list = [
        p for p in body_paragraphs[figure_list_title + 1:chapter_start]
        if any(h.get(W + "anchor", "").startswith("fig_") for h in p.findall(".//w:hyperlink", NS))
    ]
    toc = [
        p for p in body_paragraphs
        if style_of(p) in {"TOC1", "TOC2", "TOC3"} and text_of(p)
    ]
    captions = [
        p for p in body_paragraphs[chapter_start + 1:]
        if CAPTION_RE.match(text_of(p))
    ]

    caption_format_errors = []
    for p in captions:
        runs = [r for r in p.findall("w:r", NS) if text_of(r)]
        if len(runs) != 2:
            caption_format_errors.append((text_of(p), "run_count", len(runs)))
            continue
        number, description = map(run_props, runs)
        if not (
            number["size"] in {None, "26"} and number["color"] in {None, "000000"}
            and number["has_bold"]
            and number["has_italic"]
            and number["underline"] == "single"
            and description["size"] in {None, "26"}
            and description["color"] in {None, "000000"}
            and description["underline"] in {None, "none"}
        ):
            caption_format_errors.append((text_of(p), number, description))

    link_display_errors = []
    for h in hyperlinks:
        for r in h.findall(".//w:r", NS):
            if not text_of(r):
                continue
            props = run_props(r)
            if (
                props["color"] not in {None, "000000"}
                or props["underline"] not in {None, "none"}
            ):
                link_display_errors.append((text_of(h), props))

    tracked = sum(len(root.findall(f".//w:{tag}", NS)) for tag in ("ins", "del", "moveFrom", "moveTo"))
    floating = len(root.findall(".//wp:anchor", NS))
    inline = len(root.findall(".//wp:inline", NS))
    page_borders = len(root.findall(".//w:sectPr/w:pgBorders", NS))

    geometry = []
    for sect in root.findall(".//w:sectPr", NS):
        sz = sect.find("w:pgSz", NS)
        mar = sect.find("w:pgMar", NS)
        geometry.append({
            "w": sz.get(W + "w") if sz is not None else None,
            "h": sz.get(W + "h") if sz is not None else None,
            "top": mar.get(W + "top") if mar is not None else None,
            "right": mar.get(W + "right") if mar is not None else None,
            "bottom": mar.get(W + "bottom") if mar is not None else None,
            "left": mar.get(W + "left") if mar is not None else None,
        })
    geometry_errors = [g for g in geometry if g != {
        "w": "11906", "h": "16838", "top": "1134", "right": "1134",
        "bottom": "1134", "left": "1701",
    }]

    hyperlink_style = styles.find("w:style[@w:styleId='Hyperlink']", NS)
    hyperlink_style_props = {}
    if hyperlink_style is not None:
        fake_run = etree.Element(W + "r")
        rpr = hyperlink_style.find("w:rPr", NS)
        if rpr is not None:
            fake_run.append(etree.fromstring(etree.tostring(rpr)))
        hyperlink_style_props = run_props(fake_run)

    floating_contexts = []
    for anchor in root.findall(".//wp:anchor", NS):
        p = anchor
        while p is not None and p.tag != W + "p":
            p = p.getparent()
        if p is None:
            floating_contexts.append({"body_index": None, "text": "", "previous": "", "next": ""})
            continue
        try:
            index = body_paragraphs.index(p)
        except ValueError:
            index = None
        floating_contexts.append({
            "body_index": index,
            "text": text_of(p),
            "previous": text_of(body_paragraphs[index - 1]) if index and index > 0 else "",
            "next": text_of(body_paragraphs[index + 1]) if index is not None and index + 1 < len(body_paragraphs) else "",
        })

    rel_by_id = {rel.get("Id"): rel for rel in rels.findall("pr:Relationship", NS)}
    broken_external = [h.get(R + "id") for h in external if h.get(R + "id") not in rel_by_id]
    missing_targets = []
    for rel in rel_by_id.values():
        if rel.get("TargetMode") == "External":
            continue
        target = resolve_word_target(rel.get("Target", ""))
        if target not in names:
            missing_targets.append((rel.get("Id"), target))

    body_sizes = Counter()
    heading_sizes = Counter()
    for p in body_paragraphs[chapter_start:]:
        style = style_of(p)
        for r in p.findall(".//w:r", NS):
            if not text_of(r):
                continue
            size = run_props(r)["size"]
            if style.startswith("Heading"):
                heading_sizes[(style, size)] += len(text_of(r))
            else:
                body_sizes[size] += len(text_of(r))

    table_sizes = Counter()
    for r in root.findall(".//w:tbl//w:r", NS):
        if text_of(r):
            table_sizes[run_props(r)["size"]] += len(text_of(r))

    result = {
        "document": str(args.docx),
        "zip_error": bad_member,
        "sections": len(geometry),
        "geometry_errors": geometry_errors,
        "page_borders": page_borders,
        "toc_entries": len(toc),
        "figure_list_entries": len(figure_list),
        "body_captions": len(captions),
        "hyperlinks": len(hyperlinks),
        "internal_hyperlinks": len(internal),
        "external_hyperlinks": len(external),
        "missing_internal_anchors": missing_anchors,
        "broken_external_links": broken_external,
        "missing_package_targets": missing_targets,
        "link_display_errors": link_display_errors,
        "hyperlink_style": hyperlink_style_props,
        "caption_format_errors": caption_format_errors,
        "tracked_change_nodes": tracked,
        "inline_images": inline,
        "floating_images": floating,
        "floating_image_paragraphs": floating_contexts,
        "body_font_sizes_by_chars": {str(k): v for k, v in body_sizes.most_common()},
        "heading_font_sizes_by_chars": {str(k): v for k, v in heading_sizes.most_common()},
        "table_font_sizes_by_chars": {str(k): v for k, v in table_sizes.most_common()},
    }

print(json.dumps(result, ensure_ascii=False, indent=2))
