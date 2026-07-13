from pathlib import Path
from shutil import copyfile
from tempfile import NamedTemporaryFile
from zipfile import ZIP_DEFLATED, ZipFile

from lxml import etree


ROOT = Path(r"D:\WORK\gtas_vpp\LVTN")
CHECKPOINT = ROOT / "checkpoints" / "02B_chuong2_nghiepvu.docx"
WORKING = ROOT / "NguyenAnNam_DH52201078_working.docx"

W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
NS = {"w": W}


with ZipFile(CHECKPOINT, "r") as source:
    document_xml = source.read("word/document.xml")
    root = etree.fromstring(document_xml)

    changed = []
    for index, paragraph in enumerate(root.xpath("//w:body/w:p", namespaces=NS)):
        text = "".join(paragraph.xpath(".//w:t/text()", namespaces=NS)).strip()
        is_process_heading = text.startswith("2.3.1.")
        is_step_lead_in = text == "Y\u00eau c\u1ea7u t\u1eebng b\u01b0\u1edbc:"
        if not (is_process_heading or is_step_lead_in):
            continue

        p_pr = paragraph.find(f"{{{W}}}pPr")
        if p_pr is None:
            p_pr = etree.Element(f"{{{W}}}pPr")
            paragraph.insert(0, p_pr)
        if p_pr.find(f"{{{W}}}keepNext") is None:
            keep_next = etree.Element(f"{{{W}}}keepNext")
            p_style = p_pr.find(f"{{{W}}}pStyle")
            if p_style is None:
                p_pr.insert(0, keep_next)
            else:
                p_pr.insert(p_pr.index(p_style) + 1, keep_next)
        changed.append((index, text))

    if len(changed) != 30:
        raise RuntimeError(f"Expected 30 target paragraphs, found {len(changed)}")

    patched_document_xml = etree.tostring(
        root, xml_declaration=True, encoding="UTF-8", standalone=True
    )

    with NamedTemporaryFile(delete=False, suffix=".docx", dir=CHECKPOINT.parent) as tmp:
        temp_path = Path(tmp.name)

    with ZipFile(temp_path, "w", ZIP_DEFLATED) as target:
        for item in source.infolist():
            payload = (
                patched_document_xml
                if item.filename == "word/document.xml"
                else source.read(item.filename)
            )
            target.writestr(item, payload)

temp_path.replace(CHECKPOINT)
copyfile(CHECKPOINT, WORKING)

print(f"keep_with_next={len(changed)}")
for item in changed:
    print(item)
