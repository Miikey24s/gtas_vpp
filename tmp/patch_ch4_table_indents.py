from __future__ import annotations

import shutil
import zipfile
from pathlib import Path

from lxml import etree


DOCX = Path(r"D:\WORK\gtas_vpp\LVTN\checkpoints\04_chuong4_thunghiem.docx")
WORKING = Path(r"D:\WORK\gtas_vpp\LVTN\NguyenAnNam_DH52201078_working.docx")
TEMP = DOCX.with_suffix(".indent_patch.tmp.docx")

W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
NS = {"w": W}

with zipfile.ZipFile(DOCX, "r") as source:
    document_xml = source.read("word/document.xml")
    root = etree.fromstring(document_xml)
    tables = root.xpath("//w:body/w:tbl", namespaces=NS)
    if len(tables) != 23:
        raise RuntimeError(f"Expected 23 tables, found {len(tables)}")

    for one_based_index in range(18, 23):
        table = tables[one_based_index - 1]
        tbl_pr = table.find(f"{{{W}}}tblPr")
        if tbl_pr is None:
            raise RuntimeError(f"Table {one_based_index} has no tblPr")
        for existing in list(tbl_pr.findall(f"{{{W}}}tblInd")):
            tbl_pr.remove(existing)
        indent = etree.Element(f"{{{W}}}tblInd")
        indent.set(f"{{{W}}}w", "120")
        indent.set(f"{{{W}}}type", "dxa")
        tbl_w = tbl_pr.find(f"{{{W}}}tblW")
        insert_at = tbl_pr.index(tbl_w) + 1 if tbl_w is not None else 0
        tbl_pr.insert(insert_at, indent)

    patched = etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone=True)

    with zipfile.ZipFile(TEMP, "w") as target:
        for info in source.infolist():
            data = patched if info.filename == "word/document.xml" else source.read(info.filename)
            target.writestr(info, data)

TEMP.replace(DOCX)
shutil.copy2(DOCX, WORKING)
print(DOCX)
