import json
import sys
import zipfile
from collections import Counter

from lxml import etree

NS = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
W = "{%s}" % NS["w"]


def val(el, tag, attr="val"):
    node = el.find(f"w:{tag}", NS) if el is not None else None
    return node.get(W + attr) if node is not None else None


def rpr(el):
    if el is None:
        return {}
    rf = el.find("w:rFonts", NS)
    return {
        "font": rf.get(W + "ascii") if rf is not None else None,
        "font_hAnsi": rf.get(W + "hAnsi") if rf is not None else None,
        "size_half_points": val(el, "sz"),
        "sizeCs_half_points": val(el, "szCs"),
        "scale_percent": val(el, "w"),
        "char_spacing_twips": val(el, "spacing"),
        "bold": el.find("w:b", NS) is not None,
    }


def ppr(el):
    if el is None:
        return {}
    sp = el.find("w:spacing", NS)
    ind = el.find("w:ind", NS)
    tabs = []
    tabs_el = el.find("w:tabs", NS)
    if tabs_el is not None:
        for t in tabs_el.findall("w:tab", NS):
            tabs.append({"val": t.get(W + "val"), "pos": t.get(W + "pos"), "leader": t.get(W + "leader")})
    return {
        "before": sp.get(W + "before") if sp is not None else None,
        "after": sp.get(W + "after") if sp is not None else None,
        "line": sp.get(W + "line") if sp is not None else None,
        "lineRule": sp.get(W + "lineRule") if sp is not None else None,
        "left": ind.get(W + "left") if ind is not None else None,
        "hanging": ind.get(W + "hanging") if ind is not None else None,
        "tabs": tabs,
    }


def inspect(path):
    with zipfile.ZipFile(path) as z:
        styles_root = etree.fromstring(z.read("word/styles.xml"))
        doc_root = etree.fromstring(z.read("word/document.xml"))

    styles = {}
    for s in styles_root.findall("w:style", NS):
        sid = s.get(W + "styleId")
        name_el = s.find("w:name", NS)
        name = name_el.get(W + "val") if name_el is not None else sid
        based = s.find("w:basedOn", NS)
        styles[sid] = {
            "id": sid,
            "name": name,
            "based_on": based.get(W + "val") if based is not None else None,
            "rPr": rpr(s.find("w:rPr", NS)),
            "pPr": ppr(s.find("w:pPr", NS)),
        }

    toc_ids = {sid for sid, s in styles.items() if sid.lower().startswith("toc") or s["name"].lower().startswith("toc")}
    toc_styles = {sid: styles[sid] for sid in sorted(toc_ids)}
    counts = Counter()
    examples = []
    for p in doc_root.findall(".//w:p", NS):
        ps = p.find("w:pPr/w:pStyle", NS)
        sid = ps.get(W + "val") if ps is not None else None
        if sid in toc_ids:
            counts[sid] += 1
            if len(examples) < 8:
                text = "".join(p.xpath(".//w:t/text()", namespaces=NS))
                run_rprs = []
                for r in p.findall(".//w:r", NS):
                    rr = r.find("w:rPr", NS)
                    rs = rr.find("w:rStyle", NS) if rr is not None else None
                    info = rpr(rr)
                    info["run_style"] = rs.get(W + "val") if rs is not None else None
                    if any(v is not None and v is not False for v in info.values()):
                        run_rprs.append(info)
                examples.append({"style": sid, "text": text[:100], "direct_pPr": ppr(p.find("w:pPr", NS)), "run_rPr": run_rprs[:3]})

    defaults_rpr = styles_root.find("w:docDefaults/w:rPrDefault/w:rPr", NS)
    defaults_ppr = styles_root.find("w:docDefaults/w:pPrDefault/w:pPr", NS)
    important = {sid: styles[sid] for sid in styles if sid.lower() in {"normal", "hyperlink", "defaultparagraphfont"}}
    return {
        "path": path,
        "doc_defaults": {"rPr": rpr(defaults_rpr), "pPr": ppr(defaults_ppr)},
        "important_styles": important,
        "toc_styles": toc_styles,
        "toc_paragraph_counts": dict(counts),
        "examples": examples,
    }


print(json.dumps([inspect(p) for p in sys.argv[1:]], ensure_ascii=False, indent=2))
