import argparse
import shutil
import tempfile
import zipfile
from pathlib import Path

from lxml import etree

NS = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
W = "{%s}" % NS["w"]


def ensure(parent, tag):
    node = parent.find(f"w:{tag}", NS)
    if node is None:
        node = etree.SubElement(parent, W + tag)
    return node


def set_val(parent, tag, value):
    node = ensure(parent, tag)
    node.set(W + "val", str(value))
    return node


def patch_styles(data):
    root = etree.fromstring(data)
    settings = {
        "TOC1": {"size": 32, "line": 276},  # 16 pt, 1.15 lines
        "TOC2": {"size": 28, "line": 276},  # 14 pt, 1.15 lines
        "TOC3": {"size": 26, "line": 259},  # 13 pt, 1.08 lines
    }
    for style_id, cfg in settings.items():
        style = root.find(f"w:style[@w:styleId='{style_id}']", NS)
        if style is None:
            raise RuntimeError(f"Missing style {style_id}")
        rpr = ensure(style, "rPr")
        fonts = ensure(rpr, "rFonts")
        for key in ("ascii", "hAnsi", "eastAsia", "cs"):
            fonts.set(W + key, "Times New Roman")
        set_val(rpr, "sz", cfg["size"])
        set_val(rpr, "szCs", cfg["size"])

        ppr = ensure(style, "pPr")
        spacing = ensure(ppr, "spacing")
        spacing.set(W + "before", "0")
        spacing.set(W + "after", "100")  # 5 pt after, as in the reference file
        spacing.set(W + "line", str(cfg["line"]))
        spacing.set(W + "lineRule", "auto")

    # Hyperlink keeps black/no-underline behavior, but must not force every
    # TOC level back to 13 pt. Body links still inherit 13 pt from their paragraph.
    for style_id in ("Hyperlink", "FollowedHyperlink"):
        style = root.find(f"w:style[@w:styleId='{style_id}']", NS)
        if style is None:
            continue
        rpr = style.find("w:rPr", NS)
        if rpr is None:
            continue
        for tag in ("sz", "szCs"):
            node = rpr.find(f"w:{tag}", NS)
            if node is not None:
                rpr.remove(node)
    return etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes")


def patch_document(data):
    root = etree.fromstring(data)
    settings = {"TOC1": 276, "TOC2": 276, "TOC3": 259}
    in_figure_list = False
    for paragraph in root.findall(".//w:p", NS):
        visible_text = "".join(paragraph.xpath(".//w:t/text()", namespaces=NS)).strip()
        if visible_text == "MỤC LỤC CÁC HÌNH VẼ":
            in_figure_list = True
            continue

        # Match the reference figure list: Times New Roman 13 pt, 1.15 lines,
        # 5 pt after each entry. Keep the existing dotted tab and hyperlinks.
        if in_figure_list and visible_text.startswith("Hình ") and paragraph.xpath(".//w:hyperlink", namespaces=NS):
            ppr = ensure(paragraph, "pPr")
            spacing = ensure(ppr, "spacing")
            spacing.set(W + "before", "0")
            spacing.set(W + "after", "100")
            spacing.set(W + "line", "276")
            spacing.set(W + "lineRule", "auto")
            for run in paragraph.findall(".//w:r", NS):
                rpr = run.find("w:rPr", NS)
                if rpr is None:
                    rpr = etree.Element(W + "rPr")
                    run.insert(0, rpr)
                fonts = ensure(rpr, "rFonts")
                for key in ("ascii", "hAnsi", "eastAsia", "cs"):
                    fonts.set(W + key, "Times New Roman")
                set_val(rpr, "sz", "26")
                set_val(rpr, "szCs", "26")
            continue

        pstyle = paragraph.find("w:pPr/w:pStyle", NS)
        if pstyle is None:
            continue
        style_id = pstyle.get(W + "val")
        if style_id not in settings:
            continue
        ppr = ensure(paragraph, "pPr")
        spacing = ensure(ppr, "spacing")
        if not visible_text:
            # Word leaves a field-end paragraph after the final TOC entry.
            # Keep the field structure, but collapse this invisible paragraph so
            # it cannot create a blank page before the list of figures.
            spacing.set(W + "before", "0")
            spacing.set(W + "after", "0")
            spacing.set(W + "line", "20")
            spacing.set(W + "lineRule", "exact")
            para_rpr = ensure(ppr, "rPr")
            set_val(para_rpr, "sz", "2")
            set_val(para_rpr, "szCs", "2")
        else:
            spacing.set(W + "before", "0")
            spacing.set(W + "after", "100")
            spacing.set(W + "line", str(settings[style_id]))
            spacing.set(W + "lineRule", "auto")
    return etree.tostring(root, xml_declaration=True, encoding="UTF-8", standalone="yes")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("input")
    ap.add_argument("output")
    args = ap.parse_args()
    src = Path(args.input)
    dst = Path(args.output)
    dst.parent.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory(dir=dst.parent) as tmpdir:
        tmp = Path(tmpdir) / dst.name
        with zipfile.ZipFile(src, "r") as zin, zipfile.ZipFile(tmp, "w", zipfile.ZIP_DEFLATED) as zout:
            for item in zin.infolist():
                data = zin.read(item.filename)
                if item.filename == "word/styles.xml":
                    data = patch_styles(data)
                elif item.filename == "word/document.xml":
                    data = patch_document(data)
                zout.writestr(item, data)
        shutil.copy2(tmp, dst)
    print(dst)


if __name__ == "__main__":
    main()
