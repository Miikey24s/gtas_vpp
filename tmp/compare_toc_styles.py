import json
import sys
from pathlib import Path

from docx import Document
from docx.oxml.ns import qn


def attr(el, name):
    if el is None:
        return None
    return el.get(qn(name))


def child(el, name):
    if el is None:
        return None
    return el.find(qn(name))


def rpr_info(rpr):
    if rpr is None:
        return {}
    rf = child(rpr, "w:rFonts")
    sz = child(rpr, "w:sz")
    szcs = child(rpr, "w:szCs")
    spacing = child(rpr, "w:spacing")
    scale = child(rpr, "w:w")
    position = child(rpr, "w:position")
    kern = child(rpr, "w:kern")
    color = child(rpr, "w:color")
    underline = child(rpr, "w:u")
    return {
        "ascii": attr(rf, "w:ascii"),
        "hAnsi": attr(rf, "w:hAnsi"),
        "eastAsia": attr(rf, "w:eastAsia"),
        "cs": attr(rf, "w:cs"),
        "sz_half_points": attr(sz, "w:val"),
        "szCs_half_points": attr(szcs, "w:val"),
        "char_spacing_twips": attr(spacing, "w:val"),
        "char_scale_percent": attr(scale, "w:val"),
        "position_half_points": attr(position, "w:val"),
        "kern_half_points": attr(kern, "w:val"),
        "color": attr(color, "w:val"),
        "underline": attr(underline, "w:val"),
        "bold": child(rpr, "w:b") is not None,
        "italic": child(rpr, "w:i") is not None,
    }


def ppr_info(ppr):
    if ppr is None:
        return {}
    spacing = child(ppr, "w:spacing")
    ind = child(ppr, "w:ind")
    jc = child(ppr, "w:jc")
    return {
        "before_twips": attr(spacing, "w:before"),
        "after_twips": attr(spacing, "w:after"),
        "line": attr(spacing, "w:line"),
        "lineRule": attr(spacing, "w:lineRule"),
        "left_twips": attr(ind, "w:left"),
        "right_twips": attr(ind, "w:right"),
        "firstLine_twips": attr(ind, "w:firstLine"),
        "hanging_twips": attr(ind, "w:hanging"),
        "alignment": attr(jc, "w:val"),
    }


def style_info(doc, style_name):
    try:
        s = doc.styles[style_name]
    except KeyError:
        return None
    return {
        "name": s.name,
        "style_id": s.style_id,
        "based_on": s.base_style.name if s.base_style is not None else None,
        "python_font_name": s.font.name,
        "python_font_size_pt": s.font.size.pt if s.font.size is not None else None,
        "rPr": rpr_info(s.element.rPr),
        "pPr": ppr_info(s.element.pPr),
    }


def run_info(run):
    return {
        "text": run.text,
        "style": run.style.name if run.style is not None else None,
        "font_name": run.font.name,
        "font_size_pt": run.font.size.pt if run.font.size is not None else None,
        "rPr": rpr_info(run._element.rPr),
    }


def hyperlink_runs(paragraph):
    out = []
    for h in paragraph._p.findall(qn("w:hyperlink")):
        for r in h.findall(qn("w:r")):
            text = "".join((t.text or "") for t in r.findall(qn("w:t")))
            out.append({"text": text, "rPr": rpr_info(r.find(qn("w:rPr")))})
    return out


def inspect(path):
    doc = Document(path)
    style_names = ["Normal", "Hyperlink", "TOC Heading", "TOC 1", "TOC 2", "TOC 3"]
    toc_paras = []
    in_toc = False
    for i, p in enumerate(doc.paragraphs):
        text = p.text.strip()
        if text.upper() in {"MỤC LỤC", "TABLE OF CONTENTS"}:
            in_toc = True
        elif in_toc and text.upper().startswith("DANH MỤC HÌNH"):
            break
        if in_toc and text and len(toc_paras) < 18:
            toc_paras.append({
                "index": i,
                "text": text,
                "style": p.style.name,
                "pPr": ppr_info(p._p.pPr),
                "runs": [run_info(r) for r in p.runs],
                "hyperlink_runs": hyperlink_runs(p),
            })
    return {
        "path": str(Path(path)),
        "styles": {name: style_info(doc, name) for name in style_names},
        "toc_paragraphs": toc_paras,
    }


def main():
    print(json.dumps([inspect(p) for p in sys.argv[1:]], ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
