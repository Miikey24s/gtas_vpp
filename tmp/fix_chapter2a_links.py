from copy import deepcopy
from pathlib import Path
from shutil import copy2
from zipfile import ZIP_DEFLATED, ZipFile
import re

from lxml import etree


ROOT = Path(r"D:\WORK\gtas_vpp")
CHECKPOINT = ROOT / "LVTN" / "checkpoints" / "02A_chuong2_congnghe.docx"
WORKING = ROOT / "LVTN" / "NguyenAnNam_DH52201078_working.docx"
OUTPUT = ROOT / "tmp" / "02A_chuong2_congnghe_links_fixed.docx"

W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
R_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
REL_NS = "http://schemas.openxmlformats.org/package/2006/relationships"
NS = {"w": W_NS, "r": R_NS}

URLS = {
    "[1]": "https://learn.microsoft.com/aspnet/core",
    "[2]": "https://learn.microsoft.com/ef/core",
    "[3]": "https://learn.microsoft.com/sql/sql-server",
    "[4]": "https://learn.microsoft.com/aspnet/core/blazor",
    "[5]": "https://blazor.radzen.com",
    "[6]": "https://github.com/MapsterMapper/Mapster",
    "[7]": "https://serilog.net",
    "[8]": "https://docs.docker.com/compose",
    "[9]": "https://playwright.dev/dotnet",
    "[11]": "https://support.google.com/a/users/answer/13309904",
    "[12]": "https://support.microsoft.com/en-US/Excel/basic-tasks-in-excel-for-the-web",
    "[13]": "https://www.odoo.com/documentation/18.0/applications/inventory_and_mrp/purchase.html",
    "[14]": "https://www.atlassian.com/software/jira/service-management/product-guide/getting-started/service-request-management",
    "[15]": "https://www.zoho.com/creator/approval-workflow/",
}

TOKEN_RE = re.compile(
    r"\[(?:[1-9]|11|12|13|14|15)\]|https://[^\s]+"
)


def next_relationship_id(root):
    used = set()
    for rel in root:
        rid = rel.get("Id", "")
        if rid.startswith("rId") and rid[3:].isdigit():
            used.add(int(rid[3:]))
    value = max(used, default=0) + 1
    while value in used:
        value += 1
    return f"rId{value}"


def relationship_for(rels, target, cache):
    if target in cache:
        return cache[target]
    rid = next_relationship_id(rels)
    rel = etree.Element(f"{{{REL_NS}}}Relationship")
    rel.set("Id", rid)
    rel.set(
        "Type",
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink",
    )
    rel.set("Target", target)
    rel.set("TargetMode", "External")
    rels.append(rel)
    cache[target] = rid
    return rid


def clone_text_run(source_run, text):
    run = etree.Element(f"{{{W_NS}}}r")
    rpr = source_run.find(f"{{{W_NS}}}rPr")
    if rpr is not None:
        run.append(deepcopy(rpr))
    text_node = etree.SubElement(run, f"{{{W_NS}}}t")
    if text.startswith(" ") or text.endswith(" "):
        text_node.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    text_node.text = text
    return run


def add_hyperlinks(document, rels):
    cache = {
        rel.get("Target"): rel.get("Id")
        for rel in rels
        if rel.get("Type", "").endswith("/hyperlink")
        and rel.get("TargetMode") == "External"
    }
    changed = 0
    runs = list(document.xpath("//w:r[not(ancestor::w:hyperlink)]", namespaces=NS))
    for run in runs:
        text_nodes = run.xpath("./w:t", namespaces=NS)
        if not text_nodes:
            continue
        text = "".join(node.text or "" for node in text_nodes)
        matches = list(TOKEN_RE.finditer(text))
        if not matches:
            continue

        parent = run.getparent()
        insertion_index = parent.index(run)
        cursor = 0
        replacements = []
        for match in matches:
            if match.start() > cursor:
                replacements.append(clone_text_run(run, text[cursor : match.start()]))
            token = match.group(0)
            target = URLS.get(token, token.rstrip(".,;"))
            # If punctuation followed a URL, leave it outside the hyperlink.
            linked_text = token
            trailing = ""
            if token.startswith("https://"):
                linked_text = token.rstrip(".,;")
                trailing = token[len(linked_text) :]
            hyperlink = etree.Element(f"{{{W_NS}}}hyperlink")
            hyperlink.set(f"{{{R_NS}}}id", relationship_for(rels, target, cache))
            hyperlink.set(f"{{{W_NS}}}history", "1")
            hyperlink.append(clone_text_run(run, linked_text))
            replacements.append(hyperlink)
            if trailing:
                replacements.append(clone_text_run(run, trailing))
            cursor = match.end()
        if cursor < len(text):
            replacements.append(clone_text_run(run, text[cursor:]))

        parent.remove(run)
        for offset, replacement in enumerate(replacements):
            parent.insert(insertion_index + offset, replacement)
        changed += len(matches)
    return changed


def lock_comparison_table_rows(document):
    tables = document.xpath("//w:tbl", namespaces=NS)
    if len(tables) < 2:
        raise RuntimeError(f"Expected at least two tables, found {len(tables)}")
    rows = tables[1].xpath("./w:tr", namespaces=NS)
    for row in rows:
        tr_pr = row.find(f"{{{W_NS}}}trPr")
        if tr_pr is None:
            tr_pr = etree.Element(f"{{{W_NS}}}trPr")
            row.insert(0, tr_pr)
        if tr_pr.find(f"{{{W_NS}}}cantSplit") is None:
            tr_pr.append(etree.Element(f"{{{W_NS}}}cantSplit"))
    return len(rows)


with ZipFile(CHECKPOINT, "r") as source:
    document = etree.fromstring(source.read("word/document.xml"))
    rels = etree.fromstring(source.read("word/_rels/document.xml.rels"))

    linked_tokens = add_hyperlinks(document, rels)
    locked_rows = lock_comparison_table_rows(document)
    if linked_tokens == 0:
        raise RuntimeError("No citation or URL tokens were converted to hyperlinks")

    document_xml = etree.tostring(
        document, xml_declaration=True, encoding="UTF-8", standalone="yes"
    )
    rels_xml = etree.tostring(
        rels, xml_declaration=True, encoding="UTF-8", standalone="yes"
    )

    with ZipFile(OUTPUT, "w", ZIP_DEFLATED) as target:
        for item in source.infolist():
            if item.filename == "word/document.xml":
                data = document_xml
            elif item.filename == "word/_rels/document.xml.rels":
                data = rels_xml
            else:
                data = source.read(item.filename)
            target.writestr(item, data)

copy2(OUTPUT, CHECKPOINT)
copy2(OUTPUT, WORKING)
print(f"linked_tokens={linked_tokens}")
print(f"locked_rows={locked_rows}")
print(CHECKPOINT)
