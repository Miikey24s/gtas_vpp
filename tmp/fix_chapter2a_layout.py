from pathlib import Path
from shutil import copy2
from zipfile import ZIP_DEFLATED, ZipFile

from lxml import etree


ROOT = Path(r"D:\WORK\gtas_vpp")
CHECKPOINT = ROOT / "LVTN" / "checkpoints" / "02A_chuong2_congnghe.docx"
WORKING = ROOT / "LVTN" / "NguyenAnNam_DH52201078_working.docx"
OUTPUT = ROOT / "tmp" / "02A_chuong2_congnghe_fixed.docx"

NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "a": "http://schemas.openxmlformats.org/drawingml/2006/main",
    "wp": "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
}


def paragraph_text(paragraph):
    return "".join(paragraph.xpath(".//w:t/text()", namespaces=NS))


def ensure_child(parent, qname):
    child = parent.find(qname)
    if child is None:
        child = etree.Element(qname)
        parent.insert(0, child)
    return child


with ZipFile(CHECKPOINT, "r") as source:
    document = etree.fromstring(source.read("word/document.xml"))

    # Update the visible caption. The original caption text is split across two runs.
    captions = [
        p
        for p in document.xpath("//w:p", namespaces=NS)
        if paragraph_text(p) == "Hình 2-1: Kiến trúc tổng thể hệ thống GTAS VPP"
    ]
    if len(captions) != 1:
        raise RuntimeError(f"Expected one visible Figure 2-1 caption, found {len(captions)}")
    caption_runs = captions[0].xpath("./w:r", namespaces=NS)
    caption_runs[0].xpath("./w:t", namespaces=NS)[0].text = "Hình 2-1"
    caption_runs[1].xpath("./w:t", namespaces=NS)[0].text = (
        ": Kiến trúc triển khai tổng thể hệ thống GTAS VPP"
    )
    for run in caption_runs[2:]:
        for text in run.xpath("./w:t", namespaces=NS):
            text.text = ""

    # Reduce the architecture figure from 5.25 in to 4.70 in so it can use the
    # remaining space in section 2.2 without making the labels too small.
    drawings = document.xpath(
        '//wp:docPr[@title="Hình 2-1"]/ancestor::w:drawing[1]',
        namespaces=NS,
    )
    if len(drawings) != 1:
        raise RuntimeError(f"Expected one Figure 2-1 drawing, found {len(drawings)}")
    width_in = 4.70
    height_in = width_in * 888 / 866
    cx = str(round(width_in * 914400))
    cy = str(round(height_in * 914400))
    for extent in drawings[0].xpath(".//wp:extent | .//a:xfrm/a:ext", namespaces=NS):
        extent.set("cx", cx)
        extent.set("cy", cy)

    # Keep every technology-table row intact. This prevents short fragments such
    # as a citation number from being orphaned at the top of the following page.
    tables = document.xpath("//w:tbl", namespaces=NS)
    if len(tables) < 3:
        raise RuntimeError(f"Expected at least three tables, found {len(tables)}")
    for row in tables[2].xpath("./w:tr", namespaces=NS):
        tr_pr = row.find(f"{{{NS['w']}}}trPr")
        if tr_pr is None:
            tr_pr = etree.Element(f"{{{NS['w']}}}trPr")
            row.insert(0, tr_pr)
        if tr_pr.find(f"{{{NS['w']}}}cantSplit") is None:
            tr_pr.append(etree.Element(f"{{{NS['w']}}}cantSplit"))

    patched_xml = etree.tostring(
        document,
        xml_declaration=True,
        encoding="UTF-8",
        standalone="yes",
    )
    with ZipFile(OUTPUT, "w", ZIP_DEFLATED) as target:
        for item in source.infolist():
            data = patched_xml if item.filename == "word/document.xml" else source.read(item.filename)
            target.writestr(item, data)

copy2(OUTPUT, CHECKPOINT)
copy2(OUTPUT, WORKING)
print(CHECKPOINT)
print(WORKING)
