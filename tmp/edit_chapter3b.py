from copy import deepcopy
from pathlib import Path
from shutil import copyfile
from zipfile import ZIP_DEFLATED, ZipFile

from PIL import Image
from docx import Document
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt
from docx.table import Table
from lxml import etree


ROOT = Path(r"D:\WORK\gtas_vpp")
INPUT = ROOT / "LVTN" / "checkpoints" / "03A_chuong3_mohinhdulieu.docx"
OUTPUT = ROOT / "LVTN" / "checkpoints" / "03B_chuong3_usecase_chitiet.docx"
WORKING = ROOT / "LVTN" / "NguyenAnNam_DH52201078_working.docx"
DIAGRAM_DIR = ROOT / "LVTN" / "diagrams" / "ch03"


DIAGRAMS = [
    {
        "key": "login",
        "file": "use-case-login",
        "heading": "3.2.1.1 Chức năng đăng nhập và tải quyền",
        "caption": "Hình 3-5: Sơ đồ use case chức năng đăng nhập và tải quyền",
        "alt": "Use case đăng nhập và tải quyền",
        "rows": [
            ("Tên Use case", "Đăng nhập và tải quyền"),
            ("Actor", "Người dùng hệ thống: nhân viên, quản lý phòng ban hoặc quản trị viên."),
            ("Mô tả", "Xác thực tài khoản qua API, tạo phiên đăng nhập và tải quyền theo từng trang/component để hiển thị giao diện đúng phạm vi được cấp."),
            ("Pre-conditions", "Người dùng truy cập trang đăng nhập, chưa có phiên hợp lệ; API xác thực và cơ sở dữ liệu đang sẵn sàng."),
            ("Post-conditions", "Thành công: API trả thông tin đăng nhập và JWT có thời hạn 24 giờ; frontend tạo ticket/cookie, tải quyền rồi điều hướng đến chức năng đầu tiên được phép. Thất bại: không tạo phiên."),
            ("Luồng sự kiện chính", "Người dùng nhập tên đăng nhập, mật khẩu và môi trường nếu đang ở chế độ phát triển. Frontend kiểm tra dữ liệu rồi gọi API đăng nhập. Backend mã hóa mật khẩu, xác thực qua stored procedure, bổ sung phòng ban, tạo JWT. Frontend tạo phiên, tải quyền các trang Dashboard, Library, Permission, Report và điều hướng theo quyền."),
            ("Luồng sự kiện phụ", "Thiếu dữ liệu, môi trường không khả dụng hoặc sai thông tin đăng nhập: hiển thị lỗi. API timeout/không kết nối được: thông báo lỗi kết nối. Không có quyền trang: ẩn chức năng hoặc chuyển về tuyến truy cập hợp lệ."),
        ],
    },
    {
        "key": "regular",
        "file": "use-case-create-regular-request",
        "heading": "3.2.1.2 Chức năng tạo đơn yêu cầu thông thường",
        "caption": "Hình 3-6: Sơ đồ use case chức năng tạo đơn yêu cầu thông thường",
        "alt": "Use case tạo đơn yêu cầu thông thường",
        "rows": [
            ("Tên Use case", "Tạo đơn yêu cầu thông thường"),
            ("Actor", "Nhân viên."),
            ("Mô tả", "Cho phép nhân viên lập một đơn yêu cầu văn phòng phẩm cho kỳ hiện tại."),
            ("Pre-conditions", "Người dùng đã đăng nhập và có quyền tạo đơn; kỳ yêu cầu là kỳ hiện tại, chưa qua hạn ngày 5, chưa settlement và người dùng chưa có đơn thường trong kỳ."),
            ("Post-conditions", "Đầu đơn và chi tiết được lưu trong một giao dịch; trạng thái đơn là Submitted, đơn giá hiện hành được ghi vào từng dòng và log CREATE được tạo."),
            ("Luồng sự kiện chính", "Người dùng mở màn hình tạo đơn, chọn vật tư và nhập số lượng. Hệ thống kiểm tra kỳ, hạn ngày 5, đơn trùng, vật tư còn hoạt động, số lượng dương và không trùng mặt hàng. Backend sinh VPPCode, lưu đầu đơn, chi tiết, đơn giá hiện hành và nhật ký rồi commit."),
            ("Luồng sự kiện phụ", "Sai kỳ, quá hạn, kỳ đã settlement, đã có đơn thường, đơn rỗng, số lượng không hợp lệ, trùng vật tư hoặc vật tư/danh mục đã vô hiệu hóa: rollback và hiển thị lỗi; vi phạm chỉ mục duy nhất được trả về dạng xung đột."),
        ],
    },
    {
        "key": "edit_cancel",
        "file": "use-case-edit-cancel-request",
        "heading": "3.2.1.3 Chức năng chỉnh sửa và hủy đơn",
        "caption": "Hình 3-7: Sơ đồ use case chức năng chỉnh sửa và hủy đơn",
        "alt": "Use case chỉnh sửa và hủy đơn yêu cầu",
        "rows": [
            ("Tên Use case", "Chỉnh sửa và hủy đơn yêu cầu"),
            ("Actor", "Nhân viên."),
            ("Mô tả", "Cho phép nhân viên cập nhật hoặc hủy đơn do chính mình tạo khi trạng thái và kỳ còn cho phép thao tác."),
            ("Pre-conditions", "Người dùng đã đăng nhập; đơn tồn tại, chưa xóa mềm và thuộc người dùng hiện tại. Chỉ trạng thái Submitted hoặc Pending được cập nhật/hủy; đơn thường phải còn trong hạn."),
            ("Post-conditions", "Chỉnh sửa: giữ trạng thái hiện tại, xóa mềm chi tiết cũ và thêm chi tiết mới, tạo log UPDATE. Hủy: đầu đơn và chi tiết được xóa mềm, trạng thái chuyển Cancelled, tạo log CANCEL."),
            ("Luồng sự kiện chính", "Người dùng chọn đơn trong lịch sử. Backend kiểm tra chủ sở hữu, trạng thái và hạn kỳ. Khi sửa, hệ thống kiểm tra lại vật tư/số lượng rồi thay thế tập chi tiết trong giao dịch. Khi hủy, hệ thống chuyển trạng thái và xóa mềm đầu đơn cùng các dòng chi tiết. Cuối cùng ghi nhật ký và commit."),
            ("Luồng sự kiện phụ", "Đơn không tồn tại, thuộc người khác, đã quá hạn hoặc ở trạng thái Approved, Rejected hay Cancelled: từ chối thao tác và rollback. Chi tiết sửa không hợp lệ: không cập nhật đơn."),
        ],
    },
    {
        "key": "additional",
        "file": "use-case-create-additional-request",
        "heading": "3.2.1.4 Chức năng tạo đơn bổ sung",
        "caption": "Hình 3-8: Sơ đồ use case chức năng tạo đơn bổ sung",
        "alt": "Use case tạo đơn bổ sung",
        "rows": [
            ("Tên Use case", "Tạo đơn bổ sung"),
            ("Actor", "Nhân viên."),
            ("Mô tả", "Cho phép nhân viên tạo đơn bổ sung cho kỳ liền trước và gửi chờ phê duyệt."),
            ("Pre-conditions", "Người dùng đã đăng nhập và có quyền tạo đơn; kỳ yêu cầu là kỳ liền trước, kỳ chưa settlement, tổng số đơn bổ sung của người dùng trong kỳ nhỏ hơn 3 và không có đơn bổ sung Pending khác."),
            ("Post-conditions", "Đầu đơn, chi tiết và đơn giá hiện hành được lưu trong một giao dịch; IsAdditionalOrder = true, trạng thái Pending và log CREATE được tạo."),
            ("Luồng sự kiện chính", "Người dùng chọn tạo đơn bổ sung, nhập vật tư và số lượng. Backend kiểm tra kỳ liền trước, giới hạn 3 đơn, không có đơn Pending, kỳ chưa settlement và dữ liệu chi tiết hợp lệ; sau đó lưu đơn Pending, chi tiết và nhật ký rồi commit."),
            ("Luồng sự kiện phụ", "Không đúng kỳ liền trước, kỳ đã settlement, đã đủ 3 đơn, còn đơn Pending hoặc chi tiết không hợp lệ: rollback và thông báo nguyên nhân."),
        ],
    },
    {
        "key": "approval",
        "file": "use-case-approve-additional-request",
        "heading": "3.2.1.5 Chức năng duyệt đơn bổ sung",
        "caption": "Hình 3-9: Sơ đồ use case chức năng duyệt đơn bổ sung",
        "alt": "Use case duyệt hoặc từ chối đơn bổ sung",
        "rows": [
            ("Tên Use case", "Duyệt hoặc từ chối đơn bổ sung"),
            ("Actor", "Quản trị viên."),
            ("Mô tả", "Cho phép người có quyền REQUEST_ADMIN_APPROVAL xem các đơn bổ sung Pending và quyết định duyệt hoặc từ chối."),
            ("Pre-conditions", "Người dùng đã đăng nhập; giao diện có quyền xử lý đơn bổ sung; đơn tồn tại, chưa xóa mềm, là đơn bổ sung và đang ở trạng thái Pending."),
            ("Post-conditions", "Duyệt: trạng thái Approved, ghi ApprovedById/ApprovedAt và log APPROVE. Từ chối: trạng thái Rejected, ghi RejectedById/RejectedAt/RejectReason và log REJECT."),
            ("Luồng sự kiện chính", "Quản trị viên mở danh sách Pending, xem chi tiết và chọn duyệt hoặc từ chối. Nếu từ chối có thể nhập lý do. Backend khóa thao tác trong giao dịch, kiểm tra chuyển trạng thái, ghi người và thời điểm xử lý, tạo nhật ký rồi commit."),
            ("Luồng sự kiện phụ", "Đơn không tồn tại, không phải đơn bổ sung hoặc không còn Pending: rollback và không cập nhật. Người dùng không có component quyền: giao diện không hiển thị hoặc chặn vùng xử lý."),
        ],
    },
    {
        "key": "catalog",
        "file": "use-case-manage-catalog",
        "heading": "3.2.1.6 Chức năng quản lý danh mục mặt hàng",
        "caption": "Hình 3-10: Sơ đồ use case chức năng quản lý danh mục mặt hàng",
        "alt": "Use case quản lý danh mục mặt hàng",
        "rows": [
            ("Tên Use case", "Quản lý danh mục mặt hàng"),
            ("Actor", "Quản trị viên."),
            ("Mô tả", "Quản lý loại văn phòng phẩm, mặt hàng, đơn vị tính và nhà cung cấp dùng trong nghiệp vụ lập đơn và bảng giá."),
            ("Pre-conditions", "Người dùng đã đăng nhập, có quyền truy cập Library và component tương ứng; dữ liệu liên quan cần thiết đã tồn tại khi tạo ánh xạ."),
            ("Post-conditions", "Bản ghi được tạo, cập nhật, xóa mềm hoặc khôi phục. Dữ liệu đang IsDeleted không xuất hiện trong danh sách nghiệp vụ mặc định."),
            ("Luồng sự kiện chính", "Quản trị viên chọn tab danh mục, tải danh sách, thêm mới hoặc chỉnh sửa dữ liệu rồi lưu. Hệ thống kiểm tra trường bắt buộc và quan hệ, gọi API Library tương ứng; thao tác xóa dùng xóa mềm và có thể khôi phục khi được phép."),
            ("Luồng sự kiện phụ", "Thiếu dữ liệu, khóa không tồn tại, bản ghi trùng hoặc vi phạm quan hệ: API trả lỗi và giao diện giữ dữ liệu để người dùng sửa. Không có quyền component: tab hoặc nút thao tác bị ẩn/vô hiệu hóa."),
        ],
    },
    {
        "key": "pricing",
        "file": "use-case-manage-pricing",
        "heading": "3.2.1.7 Chức năng quản lý bảng giá",
        "caption": "Hình 3-11: Sơ đồ use case chức năng quản lý bảng giá",
        "alt": "Use case quản lý bảng giá và giá vật tư nhà cung cấp",
        "rows": [
            ("Tên Use case", "Quản lý bảng giá"),
            ("Actor", "Quản trị viên."),
            ("Mô tả", "Quản lý bảng giá và chi tiết giá theo cặp vật tư - nhà cung cấp, gồm sao chép và thiết lập giá mặc định."),
            ("Pre-conditions", "Người dùng có quyền LIBRARY_PRICE hoặc LIBRARY_PRICE_LIST; vật tư và nhà cung cấp liên quan đang hoạt động."),
            ("Post-conditions", "Bảng giá hoặc chi tiết giá được tạo/cập nhật; có thể sao chép bảng giá, đặt một bảng giá mặc định và đặt một nhà cung cấp mặc định cho mỗi vật tư trong từng bảng giá."),
            ("Luồng sự kiện chính", "Quản trị viên tạo hoặc chọn bảng giá, cập nhật thông tin, sao chép nếu cần; sau đó quản lý giá theo vật tư và nhà cung cấp. Hệ thống kiểm tra thực thể liên quan, giá không âm và các ràng buộc duy nhất trước khi commit."),
            ("Luồng sự kiện phụ", "Không tìm thấy vật tư/nhà cung cấp/bảng giá, giá âm hoặc vi phạm ràng buộc mặc định duy nhất: rollback và thông báo lỗi. Không được xóa mềm bảng giá mặc định; bảng giá có chi tiết giá chỉ xóa khi đáp ứng quy tắc dịch vụ."),
        ],
    },
    {
        "key": "settlement",
        "file": "use-case-settle-period",
        "heading": "3.2.1.8 Chức năng đóng kỳ và chụp giá",
        "caption": "Hình 3-12: Sơ đồ use case chức năng đóng kỳ và chụp giá",
        "alt": "Use case đóng kỳ và chụp giá",
        "rows": [
            ("Tên Use case", "Đóng kỳ và chụp giá"),
            ("Actor", "Quản trị viên; xử lý tự động của hệ thống."),
            ("Mô tả", "Đóng kỳ nghiệp vụ bằng bảng giá được chọn hoặc bảng giá mặc định, chụp đơn giá vào chi tiết các đơn hợp lệ."),
            ("Pre-conditions", "Năm/tháng hợp lệ; có bảng giá áp dụng; không còn đơn bổ sung Pending và bảng giá có giá cho toàn bộ vật tư thuộc các đơn Submitted/Approved trong kỳ."),
            ("Post-conditions", "CurrentSinglePrice của chi tiết được cập nhật; đầu đơn ghi SettledAt, SettledByUserId và SettledByPriceListId; mỗi đơn có log PERIOD_SETTLED."),
            ("Luồng sự kiện chính", "Quản trị viên chọn kỳ và bảng giá. Dịch vụ mở giao dịch, kiểm tra đơn Pending, lấy các đơn Submitted/Approved và kiểm tra đủ giá. Hệ thống ưu tiên giá mặc định, chụp giá vào chi tiết, đánh dấu settlement, ghi log rồi commit."),
            ("Luồng sự kiện phụ", "Không có bảng giá, còn đơn Pending, thiếu giá vật tư hoặc kỳ không hợp lệ: rollback toàn bộ giao dịch và hiển thị lỗi; không để lại dữ liệu chụp giá một phần."),
        ],
    },
    {
        "key": "permissions",
        "file": "use-case-manage-permissions",
        "heading": "3.2.1.9 Chức năng quản lý người dùng và phân quyền",
        "caption": "Hình 3-13: Sơ đồ use case chức năng quản lý người dùng và phân quyền",
        "alt": "Use case quản lý người dùng và phân quyền",
        "rows": [
            ("Tên Use case", "Quản lý người dùng và phân quyền"),
            ("Actor", "Quản trị viên."),
            ("Mô tả", "Quản lý nhóm quyền, gán người dùng vào nhóm/phòng ban và cấu hình quyền trang/component bằng IsVisible, IsEnable."),
            ("Pre-conditions", "Người dùng đã đăng nhập và có quyền truy cập trang Permission cùng component PERMISSION_USER hoặc PERMISSION_COMPONENT."),
            ("Post-conditions", "Nhóm quyền, ánh xạ người dùng - nhóm - phòng ban hoặc quyền component được cập nhật; PermissionState tải lại sẽ áp dụng cấu hình mới cho giao diện."),
            ("Luồng sự kiện chính", "Quản trị viên chọn nhóm hoặc người dùng, cập nhật nhóm cha, phòng ban hay ánh xạ quyền; hệ thống kiểm tra dữ liệu, lưu IsVisible/IsEnable và cập nhật thời điểm. Sau khi làm mới quyền, menu, tab và nút thao tác thay đổi theo cấu hình."),
            ("Luồng sự kiện phụ", "Nhóm/người dùng/ánh xạ không tồn tại, quan hệ nhóm tạo vòng lặp hoặc độ sâu từ 10 cấp trở lên: từ chối cập nhật. Không có quyền trang/component: chuyển hướng hoặc ẩn chức năng."),
        ],
    },
    {
        "key": "summary",
        "file": "use-case-dashboard-summary",
        "heading": "3.2.1.10 Chức năng xem dashboard và dữ liệu tổng hợp",
        "old_heading": "3.2.1.10 Chức năng xem dashboard và báo cáo",
        "caption": "Hình 3-14: Sơ đồ use case chức năng xem dashboard và dữ liệu tổng hợp",
        "old_caption": "Hình 3-14: Sơ đồ use case chức năng xem dashboard và báo cáo",
        "alt": "Use case xem dashboard và dữ liệu tổng hợp",
        "rows": [
            ("Tên Use case", "Xem dashboard và dữ liệu tổng hợp"),
            ("Actor", "Nhân viên, quản lý phòng ban, quản trị viên."),
            ("Mô tả", "Xem dashboard cá nhân, lịch sử đơn và dữ liệu tổng hợp theo phạm vi cá nhân, phòng ban hoặc toàn doanh nghiệp đã được cấp quyền."),
            ("Pre-conditions", "Người dùng đã đăng nhập và có ít nhất một quyền Dashboard tương ứng: lịch sử, tổng hợp phòng ban, tổng hợp toàn doanh nghiệp hoặc biểu đồ cá nhân."),
            ("Post-conditions", "Danh sách đơn, tổng số đơn/dòng/số lượng/thành tiền và biểu đồ được hiển thị theo bộ lọc cùng phạm vi quyền. Trang Report độc lập và chức năng xuất file không được xem là đã hoàn thiện trong use case này."),
            ("Luồng sự kiện chính", "Người dùng mở Dashboard, chọn tab và bộ lọc kỳ/trạng thái/phòng ban. Frontend gọi API my-orders-summary, department-orders, all-orders hoặc dashboard-charts theo phạm vi; backend lọc dữ liệu và trả tổng hợp để hiển thị bảng, KPI và biểu đồ."),
            ("Luồng sự kiện phụ", "Không có dữ liệu: hiển thị trạng thái rỗng. Không có quyền phạm vi: ẩn tab hoặc chuyển hướng. API lỗi: hiển thị thông báo và không mô tả dữ liệu chưa tải như kết quả báo cáo."),
        ],
    },
]


for diagram in DIAGRAMS:
    diagram["png"] = DIAGRAM_DIR / f"{diagram['file']}.png"
    diagram["svg"] = DIAGRAM_DIR / f"{diagram['file']}.svg"
    diagram["media"] = f"{diagram['file']}.svg"
    if not diagram["png"].exists() or not diagram["svg"].exists():
        raise FileNotFoundError(diagram["file"])


def text_of(element):
    return "".join(element.xpath(".//w:t/text()"))


def set_run_font(run, size=10, bold=None, italic=None, underline=None):
    run.font.name = "Times New Roman"
    rfonts = run._element.get_or_add_rPr().get_or_add_rFonts()
    for key in ("w:ascii", "w:hAnsi", "w:eastAsia"):
        rfonts.set(qn(key), "Times New Roman")
    run.font.size = Pt(size)
    if bold is not None:
        run.bold = bold
    if italic is not None:
        run.italic = italic
    if underline is not None:
        run.underline = underline


def replace_plain(paragraph, text, *, size=None, bold=None):
    p_pr = paragraph._p.pPr
    for child in list(paragraph._p):
        if child is not p_pr:
            paragraph._p.remove(child)
    run = paragraph.add_run(text)
    if size is not None:
        set_run_font(run, size=size, bold=bold)
    return run


def format_caption(paragraph, text):
    paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
    paragraph.paragraph_format.space_before = Pt(3)
    paragraph.paragraph_format.space_after = Pt(6)
    paragraph.paragraph_format.keep_together = True
    paragraph.paragraph_format.keep_with_next = False
    p_pr = paragraph._p.pPr
    for child in list(paragraph._p):
        if child is not p_pr:
            paragraph._p.remove(child)
    label, description = text.split(":", 1)
    label_run = paragraph.add_run(label + ":")
    set_run_font(label_run, 12, bold=True, italic=True, underline=True)
    desc_run = paragraph.add_run(description)
    set_run_font(desc_run, 12)


def add_cell_margins(cell, top=90, start=100, bottom=90, end=100):
    tc_pr = cell._tc.get_or_add_tcPr()
    tc_mar = tc_pr.first_child_found_in("w:tcMar")
    if tc_mar is None:
        tc_mar = OxmlElement("w:tcMar")
        tc_pr.append(tc_mar)
    for tag, value in (("top", top), ("start", start), ("bottom", bottom), ("end", end)):
        node = tc_mar.find(qn(f"w:{tag}"))
        if node is None:
            node = OxmlElement(f"w:{tag}")
            tc_mar.append(node)
        node.set(qn("w:w"), str(value))
        node.set(qn("w:type"), "dxa")


def set_table_widths(table, widths):
    table.autofit = False
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_pr = table._tbl.tblPr
    tbl_w = tbl_pr.find(qn("w:tblW"))
    if tbl_w is None:
        tbl_w = OxmlElement("w:tblW")
        tbl_pr.append(tbl_w)
    tbl_w.set(qn("w:w"), str(sum(widths)))
    tbl_w.set(qn("w:type"), "dxa")

    tbl_ind = tbl_pr.find(qn("w:tblInd"))
    if tbl_ind is None:
        tbl_ind = OxmlElement("w:tblInd")
        tbl_pr.append(tbl_ind)
    tbl_ind.set(qn("w:w"), "0")
    tbl_ind.set(qn("w:type"), "dxa")

    grid = table._tbl.tblGrid
    for child in list(grid):
        grid.remove(child)
    for width in widths:
        col = OxmlElement("w:gridCol")
        col.set(qn("w:w"), str(width))
        grid.append(col)

    for row in table.rows:
        cant_split = row._tr.get_or_add_trPr().find(qn("w:cantSplit"))
        if cant_split is None:
            row._tr.get_or_add_trPr().append(OxmlElement("w:cantSplit"))
        for idx, cell in enumerate(row.cells):
            cell.width = Inches(widths[idx] / 1440)
            tc_w = cell._tc.get_or_add_tcPr().find(qn("w:tcW"))
            if tc_w is None:
                tc_w = OxmlElement("w:tcW")
                cell._tc.get_or_add_tcPr().append(tc_w)
            tc_w.set(qn("w:w"), str(widths[idx]))
            tc_w.set(qn("w:type"), "dxa")
            add_cell_margins(cell)


def set_cell_text(cell, text, *, label=False):
    cell.text = ""
    paragraph = cell.paragraphs[0]
    paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER if label else WD_ALIGN_PARAGRAPH.JUSTIFY
    paragraph.paragraph_format.space_before = Pt(0)
    paragraph.paragraph_format.space_after = Pt(0)
    paragraph.paragraph_format.line_spacing = 1.15
    paragraph.paragraph_format.keep_together = True
    run = paragraph.add_run(text)
    set_run_font(run, 10, bold=label)
    cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER


def next_table_after(paragraph, doc):
    node = paragraph._p.getnext()
    while node is not None:
        if node.tag == qn("w:tbl"):
            return Table(node, doc)
        node = node.getnext()
    raise RuntimeError(f"No table after caption: {paragraph.text}")


doc = Document(INPUT)
new_picture_rids = {}

# Update the one use-case name whose former wording overstated the unfinished Report page.
summary = DIAGRAMS[-1]
for paragraph in doc.paragraphs:
    if paragraph.text.strip() == summary["old_heading"]:
        replace_plain(paragraph, summary["heading"])

for diagram in DIAGRAMS:
    old_caption = diagram.get("old_caption", diagram["caption"])
    caption_matches = [p for p in doc.paragraphs if p.text.strip() in {old_caption, diagram["caption"]}]
    if not caption_matches:
        raise RuntimeError(f"Cannot locate caption: {old_caption}")

    # The first match can be the list-of-figures entry; the body caption is followed by a use-case table.
    body_caption = None
    for candidate in caption_matches:
        try:
            table = next_table_after(candidate, doc)
            if len(table.rows) == 7 and len(table.columns) == 2:
                body_caption = candidate
                break
        except RuntimeError:
            continue
    if body_caption is None:
        raise RuntimeError(f"Cannot locate body caption/table: {diagram['caption']}")

    # Replace every object/empty paragraph between the diagram label and caption with one inline figure.
    label_node = body_caption._p.getprevious()
    while label_node is not None:
        if label_node.tag == qn("w:p") and text_of(label_node).strip() == "- Sơ đồ use-case":
            break
        label_node = label_node.getprevious()
    if label_node is None:
        raise RuntimeError(f"Cannot locate diagram label before {diagram['caption']}")

    node = label_node.getnext()
    while node is not None and node is not body_caption._p:
        next_node = node.getnext()
        node.getparent().remove(node)
        node = next_node

    picture_paragraph = doc.add_paragraph()
    picture_paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
    picture_paragraph.paragraph_format.space_before = Pt(3)
    picture_paragraph.paragraph_format.space_after = Pt(0)
    picture_paragraph.paragraph_format.keep_with_next = True
    picture_paragraph.paragraph_format.keep_together = True

    with Image.open(diagram["png"]) as image:
        ratio = image.width / image.height
    max_width = 6.15
    max_height = 5.00
    if ratio >= max_width / max_height:
        width = max_width
        height = width / ratio
    else:
        height = max_height
        width = height * ratio

    run = picture_paragraph.add_run()
    run.add_picture(str(diagram["png"]), width=Inches(width), height=Inches(height))
    drawing = picture_paragraph._p.xpath(".//w:drawing")[0]
    blip = drawing.xpath(".//a:blip")[0]
    rid = blip.get(qn("r:embed"))
    new_picture_rids[diagram["key"]] = rid
    for doc_pr in drawing.xpath(".//wp:docPr"):
        doc_pr.set("name", diagram["alt"])
        doc_pr.set("descr", diagram["alt"])
    for c_nv_pr in drawing.xpath(".//pic:cNvPr"):
        c_nv_pr.set("name", diagram["alt"])
        c_nv_pr.set("descr", diagram["alt"])

    body_caption._p.addprevious(picture_paragraph._p)
    format_caption(body_caption, diagram["caption"])

    # Keep heading, diagram label, figure and caption together as one visual unit.
    heading = next((p for p in doc.paragraphs if p.text.strip() == diagram["heading"]), None)
    if heading is None:
        raise RuntimeError(f"Cannot locate heading: {diagram['heading']}")
    heading.paragraph_format.keep_with_next = True
    from docx.text.paragraph import Paragraph
    label_paragraph = Paragraph(label_node, body_caption._parent)
    label_paragraph.paragraph_format.keep_with_next = True
    label_paragraph.paragraph_format.space_after = Pt(0)

    table = next_table_after(body_caption, doc)
    if len(table.rows) != 7 or len(table.columns) != 2:
        raise RuntimeError(f"Unexpected use-case table geometry: {diagram['caption']}")
    for row, values in zip(table.rows, diagram["rows"]):
        set_cell_text(row.cells[0], values[0], label=True)
        set_cell_text(row.cells[1], values[1])
    set_table_widths(table, [1944, 6912])

# Update the static list-of-figures entry for Hình 3-14 without changing the final caption styling.
for paragraph in doc.paragraphs:
    if paragraph is body_caption:
        continue
    if paragraph.text.strip() == summary["old_caption"]:
        replace_plain(paragraph, summary["caption"], size=12)

doc.save(OUTPUT)


# Attach each SVG as the preferred Office image while keeping its PNG fallback.
A = "http://schemas.openxmlformats.org/drawingml/2006/main"
R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
REL = "http://schemas.openxmlformats.org/package/2006/relationships"
CT = "http://schemas.openxmlformats.org/package/2006/content-types"
ASVG = "http://schemas.microsoft.com/office/drawing/2016/SVG/main"


def ns(namespace, tag):
    return f"{{{namespace}}}{tag}"


def next_rid(rels_root):
    values = []
    for rel in rels_root:
        rid = rel.get("Id", "")
        if rid.startswith("rId") and rid[3:].isdigit():
            values.append(int(rid[3:]))
    return f"rId{max(values, default=0) + 1}"


with ZipFile(OUTPUT, "r") as source:
    document_root = etree.fromstring(source.read("word/document.xml"))
    rels_root = etree.fromstring(source.read("word/_rels/document.xml.rels"))
    content_types = etree.fromstring(source.read("[Content_Types].xml"))

    if not content_types.xpath('./ct:Default[@Extension="svg"]', namespaces={"ct": CT}):
        default = etree.SubElement(content_types, ns(CT, "Default"))
        default.set("Extension", "svg")
        default.set("ContentType", "image/svg+xml")

    for diagram in DIAGRAMS:
        svg_rid = next_rid(rels_root)
        rel = etree.Element(ns(REL, "Relationship"))
        rel.set("Id", svg_rid)
        rel.set("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image")
        rel.set("Target", f"media/{diagram['media']}")
        rels_root.append(rel)

        blips = document_root.xpath(
            f'//a:blip[@r:embed="{new_picture_rids[diagram["key"]]}"]',
            namespaces={"a": A, "r": R},
        )
        if len(blips) != 1:
            raise RuntimeError(f"Expected one PNG blip for {diagram['key']}, got {len(blips)}")
        blip = blips[0]
        ext_list = blip.find(ns(A, "extLst"))
        if ext_list is None:
            ext_list = etree.SubElement(blip, ns(A, "extLst"))
        ext = etree.SubElement(ext_list, ns(A, "ext"))
        ext.set("uri", "{96DAC541-7B7A-43D3-8B79-37D633B846F1}")
        svg_blip = etree.SubElement(ext, ns(ASVG, "svgBlip"), nsmap={"asvg": ASVG})
        svg_blip.set(ns(R, "embed"), svg_rid)

    # Remove image relationships/media made orphaned by replacing the two old use-case drawings.
    referenced_rids = set(document_root.xpath("//@r:embed | //@r:id | //@r:link", namespaces={"r": R}))
    obsolete_targets = set()
    image_rel_type = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image"
    for rel in list(rels_root):
        if rel.get("Type") == image_rel_type and rel.get("Id") not in referenced_rids:
            target = rel.get("Target", "")
            if target.startswith("media/"):
                obsolete_targets.add(f"word/{target}")
            rels_root.remove(rel)

    document_xml = etree.tostring(document_root, xml_declaration=True, encoding="UTF-8", standalone=True)
    rels_xml = etree.tostring(rels_root, xml_declaration=True, encoding="UTF-8", standalone=True)
    content_types_xml = etree.tostring(content_types, xml_declaration=True, encoding="UTF-8", standalone=True)

    temp = OUTPUT.with_suffix(".tmp.docx")
    with ZipFile(temp, "w", ZIP_DEFLATED) as target:
        for item in source.infolist():
            if item.filename in obsolete_targets:
                continue
            data = source.read(item.filename)
            if item.filename == "word/document.xml":
                data = document_xml
            elif item.filename == "word/_rels/document.xml.rels":
                data = rels_xml
            elif item.filename == "[Content_Types].xml":
                data = content_types_xml
            target.writestr(item, data)

        existing = set(source.namelist())
        for diagram in DIAGRAMS:
            name = f"word/media/{diagram['media']}"
            if name not in existing:
                target.writestr(name, diagram["svg"].read_bytes())

temp.replace(OUTPUT)
working_temp = WORKING.with_suffix(".tmp.docx")
copyfile(OUTPUT, working_temp)
working_temp.replace(WORKING)
print(f"checkpoint={OUTPUT}")
print(f"working={WORKING}")
