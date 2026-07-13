from pathlib import Path
from shutil import copy2
from zipfile import ZIP_DEFLATED, ZipFile

from lxml import etree


ROOT = Path(r"D:\WORK\gtas_vpp")
CHECKPOINT = ROOT / "LVTN" / "checkpoints" / "02A_chuong2_congnghe.docx"
WORKING = ROOT / "LVTN" / "NguyenAnNam_DH52201078_working.docx"
OUTPUT = ROOT / "tmp" / "02A_chuong2_congnghe_resized.docx"

NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "a": "http://schemas.openxmlformats.org/drawingml/2006/main",
    "wp": "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
}

with ZipFile(CHECKPOINT, "r") as source:
    document = etree.fromstring(source.read("word/document.xml"))
    drawings = document.xpath(
        '//wp:docPr[@title="Hình 2-1"]/ancestor::w:drawing[1]', namespaces=NS
    )
    if len(drawings) != 1:
        raise RuntimeError(f"Expected one Figure 2-1 drawing, found {len(drawings)}")

    width_in = 4.00
    height_in = width_in * 888 / 866
    cx = str(round(width_in * 914400))
    cy = str(round(height_in * 914400))
    for extent in drawings[0].xpath(".//wp:extent | .//a:xfrm/a:ext", namespaces=NS):
        extent.set("cx", cx)
        extent.set("cy", cy)

    xml = etree.tostring(
        document, xml_declaration=True, encoding="UTF-8", standalone="yes"
    )
    with ZipFile(OUTPUT, "w", ZIP_DEFLATED) as target:
        for item in source.infolist():
            data = xml if item.filename == "word/document.xml" else source.read(item.filename)
            target.writestr(item, data)

copy2(OUTPUT, CHECKPOINT)
copy2(OUTPUT, WORKING)
print(CHECKPOINT)
