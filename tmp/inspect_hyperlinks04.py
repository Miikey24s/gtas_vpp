from pathlib import Path
import sys, zipfile
from lxml import etree

sys.stdout.reconfigure(encoding="utf-8")
docx = Path(r"D:\WORK\gtas_vpp\LVTN\checkpoints\04_chuong4_thunghiem.docx")
with zipfile.ZipFile(docx) as z:
    doc = etree.fromstring(z.read("word/document.xml"))
    rels = etree.fromstring(z.read("word/_rels/document.xml.rels"))
ns={"w":"http://schemas.openxmlformats.org/wordprocessingml/2006/main","r":"http://schemas.openxmlformats.org/officeDocument/2006/relationships","pr":"http://schemas.openxmlformats.org/package/2006/relationships"}
targets={r.get("Id"):r.get("Target") for r in rels.xpath(".//pr:Relationship",namespaces=ns)}
for i,h in enumerate(doc.xpath(".//w:hyperlink[@r:id]",namespaces=ns),1):
    rid=h.get(f"{{{ns['r']}}}id")
    txt="".join(h.xpath(".//w:t/text()",namespaces=ns))
    p=h.getparent()
    while p is not None and p.tag != f"{{{ns['w']}}}p": p=p.getparent()
    ctxt="".join(p.xpath(".//w:t/text()",namespaces=ns)) if p is not None else ""
    print(i,rid,txt,targets.get(rid),ctxt[:180],sep="\t")
