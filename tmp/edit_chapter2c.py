from copy import deepcopy
from pathlib import Path
from shutil import copyfile
from zipfile import ZIP_DEFLATED, ZipFile

from lxml import etree
from PIL import Image


ROOT = Path(r"D:\WORK\gtas_vpp")
INPUT = ROOT / "LVTN" / "checkpoints" / "02B_chuong2_nghiepvu.docx"
OUTPUT = ROOT / "LVTN" / "checkpoints" / "02C_chuong2_sodotongquat.docx"
WORKING = ROOT / "LVTN" / "NguyenAnNam_DH52201078_working.docx"
DIAGRAM_DIR = ROOT / "LVTN" / "diagrams" / "ch02"

ARCHITECTURE_SVG = DIAGRAM_DIR / "architecture-overview.svg"
ARCHITECTURE_PNG = DIAGRAM_DIR / "architecture-overview.png"
FUNCTIONAL_SVG = DIAGRAM_DIR / "functional-decomposition.svg"
FUNCTIONAL_PNG = DIAGRAM_DIR / "functional-decomposition.png"
USECASE_SVG = DIAGRAM_DIR / "use-case-overview.svg"
USECASE_PNG = DIAGRAM_DIR / "use-case-overview.png"

W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
A = "http://schemas.openxmlformats.org/drawingml/2006/main"
R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
WP = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"
PIC = "http://schemas.openxmlformats.org/drawingml/2006/picture"
REL = "http://schemas.openxmlformats.org/package/2006/relationships"
CT = "http://schemas.openxmlformats.org/package/2006/content-types"
ASVG = "http://schemas.microsoft.com/office/drawing/2016/SVG/main"
XML = "http://www.w3.org/XML/1998/namespace"

NS = {"w": W, "a": A, "r": R, "wp": WP, "pic": PIC}


def qn(namespace, tag):
    return f"{{{namespace}}}{tag}"


def paragraph_text(paragraph):
    return "".join(paragraph.xpath(".//w:t/text()", namespaces=NS)).strip()


def first_run_properties(element):
    result = element.xpath(".//w:r/w:rPr[1]", namespaces=NS)
    return deepcopy(result[0]) if result else None


def set_run_flag(r_pr, tag, enabled):
    node = r_pr.find(qn(W, tag))
    if enabled and node is None:
        etree.SubElement(r_pr, qn(W, tag))
    elif not enabled and node is not None:
        r_pr.remove(node)


def replace_paragraph_text(paragraph, text, *, bold=None, italic=None, size_half_points=None):
    p_pr = paragraph.find(qn(W, "pPr"))
    r_pr = first_run_properties(paragraph)
    for child in list(paragraph):
        if child is not p_pr:
            paragraph.remove(child)

    run = etree.SubElement(paragraph, qn(W, "r"))
    if r_pr is None:
        r_pr = etree.SubElement(run, qn(W, "rPr"))
    else:
        run.append(r_pr)

    if bold is not None:
        set_run_flag(r_pr, "b", bold)
        set_run_flag(r_pr, "bCs", bold)
    if italic is not None:
        set_run_flag(r_pr, "i", italic)
        set_run_flag(r_pr, "iCs", italic)
    if size_half_points is not None:
        for tag in ("sz", "szCs"):
            node = r_pr.find(qn(W, tag))
            if node is None:
                node = etree.SubElement(r_pr, qn(W, tag))
            node.set(qn(W, "val"), str(size_half_points))

    text_node = etree.SubElement(run, qn(W, "t"))
    if text[:1].isspace() or text[-1:].isspace():
        text_node.set(qn(XML, "space"), "preserve")
    text_node.text = text


def ensure_p_pr(paragraph):
    p_pr = paragraph.find(qn(W, "pPr"))
    if p_pr is None:
        p_pr = etree.Element(qn(W, "pPr"))
        paragraph.insert(0, p_pr)
    return p_pr


def ensure_keep_next(paragraph):
    p_pr = ensure_p_pr(paragraph)
    if p_pr.find(qn(W, "keepNext")) is None:
        p_style = p_pr.find(qn(W, "pStyle"))
        keep_next = etree.Element(qn(W, "keepNext"))
        p_pr.insert(p_pr.index(p_style) + 1 if p_style is not None else 0, keep_next)


def ensure_page_break_before(paragraph):
    p_pr = ensure_p_pr(paragraph)
    if p_pr.find(qn(W, "pageBreakBefore")) is None:
        p_pr.append(etree.Element(qn(W, "pageBreakBefore")))


def next_relationship_id(rels_root):
    ids = []
    for element in rels_root:
        rid = element.get("Id", "")
        if rid.startswith("rId") and rid[3:].isdigit():
            ids.append(int(rid[3:]))
    return f"rId{max(ids, default=0) + 1}"


def add_image_relationship(rels_root, target):
    rid = next_relationship_id(rels_root)
    relationship = etree.Element(qn(REL, "Relationship"))
    relationship.set("Id", rid)
    relationship.set(
        "Type",
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image",
    )
    relationship.set("Target", target)
    rels_root.append(relationship)
    return rid


def set_svg_fallback(document_root, fallback_rid, svg_rid, png_path, width_in, alt_text):
    blips = document_root.xpath(
        f'//a:blip[@r:embed="{fallback_rid}"]', namespaces=NS
    )
    if len(blips) != 1:
        raise RuntimeError(f"Expected one blip for {fallback_rid}, found {len(blips)}")
    blip = blips[0]

    old_ext_list = blip.find(qn(A, "extLst"))
    if old_ext_list is not None:
        blip.remove(old_ext_list)
    ext_list = etree.SubElement(blip, qn(A, "extLst"))
    svg_ext = etree.SubElement(ext_list, qn(A, "ext"))
    svg_ext.set("uri", "{96DAC541-7B7A-43D3-8B79-37D633B846F1}")
    svg_blip = etree.SubElement(
        svg_ext, qn(ASVG, "svgBlip"), nsmap={"asvg": ASVG}
    )
    svg_blip.set(qn(R, "embed"), svg_rid)

    with Image.open(png_path) as image:
        pixel_width, pixel_height = image.size
    height_in = width_in * pixel_height / pixel_width
    cx = str(round(width_in * 914400))
    cy = str(round(height_in * 914400))

    drawing = blip.xpath("ancestor::w:drawing[1]", namespaces=NS)[0]
    for extent in drawing.xpath(".//wp:extent | .//a:xfrm/a:ext", namespaces=NS):
        extent.set("cx", cx)
        extent.set("cy", cy)

    for doc_pr in drawing.xpath(".//wp:docPr", namespaces=NS):
        doc_pr.set("name", alt_text)
        doc_pr.set("descr", alt_text)
    for c_nv_pr in drawing.xpath(".//pic:cNvPr", namespaces=NS):
        c_nv_pr.set("name", alt_text)
        c_nv_pr.set("descr", alt_text)

    image_paragraph = drawing.xpath("ancestor::w:p[1]", namespaces=NS)[0]
    ensure_keep_next(image_paragraph)


def set_cell_text(cell, text, *, bold=None, align=None):
    paragraphs = cell.xpath("./w:p", namespaces=NS)
    if not paragraphs:
        paragraph = etree.SubElement(cell, qn(W, "p"))
    else:
        paragraph = paragraphs[0]
    replace_paragraph_text(paragraph, text, bold=bold)
    p_pr = ensure_p_pr(paragraph)
    if align:
        jc = p_pr.find(qn(W, "jc"))
        if jc is None:
            jc = etree.SubElement(p_pr, qn(W, "jc"))
        jc.set(qn(W, "val"), align)


def set_table_geometry(table, widths):
    total = sum(widths)
    tbl_pr = table.find(qn(W, "tblPr"))
    if tbl_pr is None:
        tbl_pr = etree.Element(qn(W, "tblPr"))
        table.insert(0, tbl_pr)
    tbl_w = tbl_pr.find(qn(W, "tblW"))
    if tbl_w is None:
        tbl_w = etree.SubElement(tbl_pr, qn(W, "tblW"))
    tbl_w.set(qn(W, "w"), str(total))
    tbl_w.set(qn(W, "type"), "dxa")
    layout = tbl_pr.find(qn(W, "tblLayout"))
    if layout is None:
        layout = etree.SubElement(tbl_pr, qn(W, "tblLayout"))
    layout.set(qn(W, "type"), "fixed")

    grid = table.find(qn(W, "tblGrid"))
    if grid is None:
        grid = etree.Element(qn(W, "tblGrid"))
        table.insert(1, grid)
    for child in list(grid):
        grid.remove(child)
    for width in widths:
        col = etree.SubElement(grid, qn(W, "gridCol"))
        col.set(qn(W, "w"), str(width))

    for row_index, row in enumerate(table.xpath("./w:tr", namespaces=NS)):
        tr_pr = row.find(qn(W, "trPr"))
        if tr_pr is None:
            tr_pr = etree.Element(qn(W, "trPr"))
            row.insert(0, tr_pr)
        if tr_pr.find(qn(W, "cantSplit")) is None:
            tr_pr.append(etree.Element(qn(W, "cantSplit")))
        if row_index == 0 and tr_pr.find(qn(W, "tblHeader")) is None:
            tr_pr.append(etree.Element(qn(W, "tblHeader")))

        cells = row.xpath("./w:tc", namespaces=NS)
        for column_index, (cell, width) in enumerate(zip(cells, widths)):
            tc_pr = cell.find(qn(W, "tcPr"))
            if tc_pr is None:
                tc_pr = etree.Element(qn(W, "tcPr"))
                cell.insert(0, tc_pr)
            tc_w = tc_pr.find(qn(W, "tcW"))
            if tc_w is None:
                tc_w = etree.SubElement(tc_pr, qn(W, "tcW"))
            tc_w.set(qn(W, "w"), str(width))
            tc_w.set(qn(W, "type"), "dxa")
            v_align = tc_pr.find(qn(W, "vAlign"))
            if v_align is None:
                v_align = etree.SubElement(tc_pr, qn(W, "vAlign"))
            v_align.set(qn(W, "val"), "center")

            for paragraph in cell.xpath("./w:p", namespaces=NS):
                p_pr = ensure_p_pr(paragraph)
                jc = p_pr.find(qn(W, "jc"))
                if jc is None:
                    jc = etree.SubElement(p_pr, qn(W, "jc"))
                jc.set(qn(W, "val"), "center" if row_index == 0 or column_index == 0 else "left")


for required in (
    INPUT,
    ARCHITECTURE_SVG,
    ARCHITECTURE_PNG,
    FUNCTIONAL_SVG,
    FUNCTIONAL_PNG,
    USECASE_SVG,
    USECASE_PNG,
):
    if not required.exists():
        raise FileNotFoundError(required)

with ZipFile(INPUT, "r") as source:
    document_root = etree.fromstring(source.read("word/document.xml"))
    rels_root = etree.fromstring(source.read("word/_rels/document.xml.rels"))
    content_types = etree.fromstring(source.read("[Content_Types].xml"))

    functional_svg_rid = add_image_relationship(
        rels_root, "media/functional-decomposition.svg"
    )
    usecase_svg_rid = add_image_relationship(rels_root, "media/use-case-overview.svg")

    if not content_types.xpath(
        './ct:Default[@Extension="svg"]', namespaces={"ct": CT}
    ):
        default = etree.SubElement(content_types, qn(CT, "Default"))
        default.set("Extension", "svg")
        default.set("ContentType", "image/svg+xml")

    set_svg_fallback(
        document_root,
        "rId13",
        "rId45",
        ARCHITECTURE_PNG,
        6.20,
        "Kiến trúc triển khai tổng thể hệ thống GTAS VPP",
    )
    set_svg_fallback(
        document_root,
        "rId14",
        functional_svg_rid,
        FUNCTIONAL_PNG,
        6.20,
        "Sơ đồ phân rã chức năng hệ thống GTAS VPP",
    )
    set_svg_fallback(
        document_root,
        "rId15",
        usecase_svg_rid,
        USECASE_PNG,
        6.20,
        "Sơ đồ use case tổng quát hệ thống GTAS VPP",
    )

    body_paragraphs = document_root.xpath("//w:body/w:p", namespaces=NS)
    by_text = {paragraph_text(p): p for p in body_paragraphs if paragraph_text(p)}
    replace_paragraph_text(
        by_text["Hình 2-2: Sơ đồ chức năng hệ thống"],
        "Hình 2-2: Sơ đồ phân rã chức năng hệ thống",
    )
    replace_paragraph_text(
        by_text["Hình 2-3: Sơ đồ use case tổng quát"],
        "Hình 2-3: Sơ đồ use case tổng quát hệ thống",
    )
    ensure_page_break_before(by_text["2.3.3 Sơ đồ Use case tổng quát"])
    ensure_keep_next(by_text["2.3.2 Sơ đồ chức năng"])
    ensure_keep_next(by_text["2.3.3 Sơ đồ Use case tổng quát"])

    actor_tables = []
    for table in document_root.xpath("//w:tbl", namespaces=NS):
        first_cell_text = "".join(
            table.xpath("./w:tr[1]/w:tc[1]//w:t/text()", namespaces=NS)
        )
        if first_cell_text.startswith("Tác nhân"):
            actor_tables.append(table)
    if len(actor_tables) != 1:
        raise RuntimeError(f"Expected one actor table, found {len(actor_tables)}")
    actor_table = actor_tables[0]
    rows = actor_table.xpath("./w:tr", namespaces=NS)
    values = [
        (
            "Tác nhân",
            "Mô tả",
            "Use case chính",
        ),
        (
            "Nhân viên",
            "Người dùng tạo và theo dõi yêu cầu văn phòng phẩm của mình trong phạm vi đơn vị công tác.",
            "Đăng nhập, xem danh mục, tạo/sao chép/sửa/hủy đơn, xem lịch sử, tạo đơn bổ sung và theo dõi trạng thái.",
        ),
        (
            "Quản lý phòng ban",
            "Vai trò nghiệp vụ theo dõi nhu cầu văn phòng phẩm theo phạm vi phòng ban được cấu hình.",
            "Xem tổng hợp phòng ban, lọc theo kỳ/trạng thái và đối chiếu dữ liệu.",
        ),
        (
            "Quản trị viên",
            "Vai trò được cấu hình quyền quản trị dữ liệu nền, phân quyền và xử lý nghiệp vụ cuối kỳ.",
            "Quản lý danh mục, nhà cung cấp, bảng giá; duyệt đơn bổ sung; đóng kỳ; phân quyền; xem tổng hợp toàn hệ thống.",
        ),
        (
            "Xử lý tự động",
            "Nhóm xử lý nội bộ, không phải người dùng trực tiếp; thực hiện kiểm tra và ghi nhận nghiệp vụ.",
            "Tính kỳ, kiểm tra hạn chốt/trùng đơn/ràng buộc, chụp giá và ghi log.",
        ),
    ]
    if len(rows) != len(values):
        raise RuntimeError(f"Expected {len(values)} actor rows, found {len(rows)}")
    for row_index, (row, row_values) in enumerate(zip(rows, values)):
        cells = row.xpath("./w:tc", namespaces=NS)
        for column_index, (cell, value) in enumerate(zip(cells, row_values)):
            set_cell_text(
                cell,
                value,
                bold=(row_index == 0),
                align="center" if row_index == 0 or column_index == 0 else "left",
            )
    set_table_geometry(actor_table, [1800, 3000, 4272])

    body = document_root.find(qn(W, "body"))
    actor_index = list(body).index(actor_table)
    page_break_paragraph = etree.Element(qn(W, "p"))
    page_break_properties = etree.SubElement(page_break_paragraph, qn(W, "pPr"))
    etree.SubElement(page_break_properties, qn(W, "pageBreakBefore"))
    page_break_spacing = etree.SubElement(page_break_properties, qn(W, "spacing"))
    page_break_spacing.set(qn(W, "before"), "0")
    page_break_spacing.set(qn(W, "after"), "0")
    body.insert(actor_index, page_break_paragraph)
    actor_index += 1
    following = body[actor_index + 1]
    if following.tag != qn(W, "p") or paragraph_text(following):
        following = etree.Element(qn(W, "p"))
        body.insert(actor_index + 1, following)
    replace_paragraph_text(
        following,
        "Lưu ý: Các tác nhân trên phản ánh vai trò nghiệp vụ và quyền hiển thị được cấu hình ở frontend. Một số API quản trị hiện mới yêu cầu người dùng đã đăng nhập, chưa cưỡng chế đầy đủ policy chi tiết ở backend.",
        italic=True,
        size_half_points=20,
    )

    document_xml = etree.tostring(
        document_root, xml_declaration=True, encoding="UTF-8", standalone=True
    )
    rels_xml = etree.tostring(
        rels_root, xml_declaration=True, encoding="UTF-8", standalone=True
    )
    content_types_xml = etree.tostring(
        content_types, xml_declaration=True, encoding="UTF-8", standalone=True
    )

    with ZipFile(OUTPUT, "w", ZIP_DEFLATED) as target:
        names = set(source.namelist())
        for item in source.infolist():
            data = source.read(item.filename)
            if item.filename == "word/document.xml":
                data = document_xml
            elif item.filename == "word/_rels/document.xml.rels":
                data = rels_xml
            elif item.filename == "[Content_Types].xml":
                data = content_types_xml
            elif item.filename == "word/media/image2.png":
                data = FUNCTIONAL_PNG.read_bytes()
            elif item.filename == "word/media/image3.png":
                data = USECASE_PNG.read_bytes()
            elif item.filename == "word/media/image1.png":
                data = ARCHITECTURE_PNG.read_bytes()
            elif item.filename == "word/media/architecture-overview.svg":
                data = ARCHITECTURE_SVG.read_bytes()
            target.writestr(item, data)

        additions = {
            "word/media/functional-decomposition.svg": FUNCTIONAL_SVG.read_bytes(),
            "word/media/use-case-overview.svg": USECASE_SVG.read_bytes(),
        }
        for name, data in additions.items():
            if name not in names:
                target.writestr(name, data)

copyfile(OUTPUT, WORKING)
print(f"checkpoint={OUTPUT}")
print(f"working={WORKING}")
