from pathlib import Path
import sys
from docx import Document
from docx.oxml.ns import qn

sys.stdout.reconfigure(encoding="utf-8")
doc = Document(Path(r"D:\WORK\gtas_vpp\LVTN\checkpoints\04_chuong4_thunghiem.docx"))
body = doc.element.body
for i, node in enumerate(body.iterchildren()):
    text = "".join((t.text or "") for t in node.iter(qn("w:t"))).strip().replace("\n", " ")
    has_sect = node.find("./w:pPr/w:sectPr", namespaces={"w":"http://schemas.openxmlformats.org/wordprocessingml/2006/main"}) is not None
    if i >= 705:
        print(i, node.tag.rsplit("}",1)[-1], "SECT" if has_sect else "", text[:150])
