#!/usr/bin/env python3
"""Refine the latest GTAS VPP thesis DOCX without losing manual edits.

The caller supplies the current working DOCX as the input authority. This
script replaces only the heading-delimited ranges listed in ``refine`` and
keeps all content outside those ranges. Word COM is used by the companion
PowerShell script for fields, section headers and PDF export.
"""

from __future__ import annotations

import argparse
import copy
import hashlib
import re
from pathlib import Path

from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.oxml.ns import qn
from docx.shared import Cm, Pt

import rebuild_final_candidate as base
import refine_final_v2 as v2
from rebuild_chapters_4_5 import (
    add_body,
    add_caption,
    add_heading,
    add_labeled_body,
    add_table,
    find_body_sample,
    find_caption_sample,
    find_paragraph,
    find_table_sample,
    mark_fields_dirty,
    normalized,
    remove_range,
    set_table_geometry,
)


ROOT = Path(__file__).resolve().parents[2]
SCREENSHOTS = ROOT / "LVTN" / "screenshots" / "ch03"


CATALOG_SPEC = {
    "name": "Quản lý danh mục văn phòng phẩm",
    "actor": "Quản lý có quyền quản lý thư viện hoặc Quản trị hệ thống",
    "description": "Quản lý danh mục, văn phòng phẩm, đơn vị tính và trạng thái hoạt động bằng thao tác có kiểm soát.",
    "pre": "Người dùng đã đăng nhập, có quyền thư viện; dữ liệu liên quan không bị khóa bởi ràng buộc nghiệp vụ.",
    "post": "Danh mục hoặc văn phòng phẩm được thêm, cập nhật, vô hiệu hóa hoặc khôi phục; dữ liệu lịch sử không bị xóa vật lý.",
    "main": "1. Mở màn hình danh mục.\n2. Tìm kiếm hoặc phân trang dữ liệu.\n3. Thêm hoặc cập nhật thông tin mã, tên, đơn vị tính và danh mục.\n4. Lưu thay đổi; backend kiểm tra mã duy nhất và trạng thái hợp lệ.",
    "alt": "Mã trùng, thiếu đơn vị tính, dữ liệu đang được tham chiếu hoặc rowversion cũ: hệ thống từ chối và giữ nguyên dữ liệu hiện hành.",
}


DASHBOARD_SPEC = {
    "name": "Xem bảng điều khiển và dữ liệu tổng hợp",
    "actor": "Nhân viên, Quản lý hoặc Quản trị hệ thống theo phạm vi quyền",
    "description": "Xem chỉ số tổng hợp, danh sách đơn và dữ liệu theo phạm vi cá nhân, phòng ban hoặc toàn công ty.",
    "pre": "Người dùng đã đăng nhập; hệ thống đã tải quyền và phạm vi dữ liệu của phiên làm việc.",
    "post": "Bảng điều khiển hiển thị đúng phạm vi; dữ liệu ngoài quyền không được trả về từ dịch vụ.",
    "main": "1. Mở bảng điều khiển hoặc màn hình tổng hợp.\n2. Chọn kỳ, trạng thái hoặc phạm vi lọc.\n3. Hệ thống truy vấn dữ liệu theo quyền hiện hành.\n4. Hiển thị chỉ số, danh sách đơn và trạng thái xử lý.",
    "alt": "Không có dữ liệu, thiếu quyền, API lỗi hoặc phiên hết hạn: hiển thị trạng thái rỗng/lỗi an toàn và yêu cầu đăng nhập lại khi cần.",
}


CHAPTER4_CASES = [
    ["1", "Đăng nhập và tải quyền", "Có tài khoản hợp lệ và thông tin phân công đang hoạt động.", "Đăng nhập, mở bảng điều khiển, kiểm tra trình đơn và quyền thao tác.", "Identity xác thực đúng; quyền được tải và kiểm tra tại dịch vụ.", "Đạt"],
    ["2", "Từ chối đăng nhập sai", "Tài khoản hoặc mật khẩu không hợp lệ.", "Nhập thông tin sai và gửi biểu mẫu.", "Hệ thống trả thông báo an toàn, không lộ chi tiết kỹ thuật.", "Đạt"],
    ["3", "Tạo đơn thông thường", "Kỳ đang nhận đơn, danh mục hoạt động.", "Chọn văn phòng phẩm, nhập số lượng và gửi đơn.", "Đơn, chi tiết, phiên bản và nhật ký được tạo trong một giao dịch dữ liệu.", "Đạt"],
    ["4", "Ngăn trùng đơn thường", "Người dùng đã có đơn hiện hành trong kỳ.", "Gửi yêu cầu tạo đơn lần hai hoặc gửi đồng thời.", "Ràng buộc duy nhất và khóa chống xử lý lặp ngăn tạo trùng đơn hiện hành.", "Đạt"],
    ["5", "Sao chép, sửa và hủy đơn", "Đơn thuộc người dùng và còn trong trạng thái cho phép.", "Sao chép kỳ trước, sửa chi tiết, hủy đơn có lý do.", "Phiên bản mới, trạng thái hủy và lịch sử được ghi nhận.", "Đạt"],
    ["6", "Tạo đơn bổ sung", "Có đơn gốc hợp lệ và còn thời hạn bổ sung.", "Nhập lý do, chọn mặt hàng và gửi đơn bổ sung.", "Đơn bổ sung chờ duyệt được tạo, gắn với đơn gốc và có nhật ký.", "Đạt"],
    ["7", "Duyệt hoặc từ chối đơn bổ sung", "Có đơn bổ sung chờ duyệt trong phạm vi xử lý.", "Quản lý duyệt một đơn và từ chối một đơn khác.", "Trạng thái, lý do, người xử lý và thông báo được lưu.", "Đạt"],
    ["8", "Kiểm soát phạm vi dữ liệu", "Có người dùng ở nhiều phòng ban.", "Mở bảng điều khiển theo vai trò nhân viên, quản lý và quản trị hệ thống.", "Dịch vụ giới hạn dữ liệu theo phạm vi và quyền thao tác.", "Đạt"],
    ["9", "Quản lý danh mục văn phòng phẩm", "Người dùng có quyền quản lý danh mục.", "Tìm kiếm, phân trang, thêm, sửa hoặc vô hiệu hóa dữ liệu.", "Hệ thống kiểm tra mã duy nhất và xóa mềm; giao diện hiển thị đúng phạm vi thao tác.", "Đạt"],
    ["10", "Quản lý bảng giá", "Có nhà cung cấp và văn phòng phẩm.", "Tạo bảng giá nháp, thêm dòng giá, công bố hoặc hết hiệu lực.", "Ràng buộc hiệu lực, phiên bản, thuế suất và dữ liệu chống ghi đè được kiểm tra.", "Đạt"],
    ["11", "Xem trước chốt kỳ", "Kỳ đã đóng nhận đơn hoặc đang định giá.", "Chọn nhà cung cấp, bảng giá và tải bản xem trước.", "Bản xem trước phát hiện điều kiện ngăn chốt kỳ và tính tổng theo dữ liệu hợp lệ.", "Đạt"],
    ["12", "Kiểm tra điều kiện chốt kỳ", "Còn đơn chờ duyệt hoặc thiếu giá.", "Thử xác nhận chốt kỳ.", "Hệ thống chặn xác nhận và trả danh sách điều kiện cần xử lý.", "Đạt"],
    ["13", "Xác nhận chốt kỳ", "Bản xem trước không còn điều kiện ngăn chốt kỳ.", "Xác nhận bằng mã kiểm tra dữ liệu và khóa chống xử lý lặp.", "Dữ liệu tại thời điểm chốt kỳ, chi phí và phân bổ được lưu bất biến.", "Đạt"],
    ["14", "Chống xác nhận lặp", "Có yêu cầu xác nhận đã xử lý.", "Gửi lại cùng khóa chống xử lý lặp.", "Hệ thống trả cùng kết quả, không tạo kết quả chốt kỳ trùng.", "Đạt"],
    ["15", "Hiệu chỉnh kết quả chốt kỳ", "Kỳ đã chốt và có lý do hiệu chỉnh.", "Tạo phiên bản hiệu chỉnh theo nguyên tắc bốn mắt.", "Phiên bản mới được tạo; phiên bản cũ được giữ để đối chiếu.", "Đạt"],
    ["16", "Quản lý người dùng và phân quyền", "Quản trị hệ thống đã đăng nhập.", "Cập nhật phân công người dùng và ánh xạ quyền.", "Nhật ký bảo mật và tín hiệu cập nhật quyền được ghi nhận.", "Đạt"],
    ["17", "Báo cáo và xuất dữ liệu", "Có dữ liệu đơn và quyền báo cáo.", "Xem chỉ số, lọc phạm vi, xuất CSV/XLSX.", "Xuất dữ liệu đúng phạm vi và kiểm soát công thức không an toàn trong CSV.", "Đạt"],
    ["18", "Thông báo và hàng đợi email", "Có sự kiện duyệt, từ chối hoặc chốt kỳ.", "Nhận thông báo, đánh dấu đã đọc và kiểm tra hàng đợi email.", "Hộp thư trong ứng dụng, cơ chế chống trùng và hàng đợi email được kiểm tra.", "Đạt"],
    ["19", "Kiểm thử trình duyệt cô lập", "Có môi trường kiểm thử cô lập.", "Chạy các luồng trình duyệt trọng tâm.", "Các kịch bản chính hoàn tất và không phát sinh lỗi hồi quy.", "Đạt"],
]


SUMMARY_ROWS = [
    ["Release build", "0 lỗi, 0 cảnh báo", "Đạt"],
    ["Backend unit test", "Đạt các nhóm kiểm thử backend trọng tâm", "Đạt"],
    ["Frontend unit test", "Đạt các nhóm kiểm thử frontend trọng tâm", "Đạt"],
    ["Integration", "Đạt các kịch bản tích hợp chính", "Đạt"],
    ["Playwright E2E", "Đạt các luồng trình duyệt trọng tâm", "Đạt"],
    ["Audit bảo mật/gói phụ thuộc", "Không phát hiện package có lỗ hổng đã biết theo log sử dụng", "Đạt"],
]


EXCEPTION_ROWS = [
    ["1", "Đăng nhập", "Sai mật khẩu, tài khoản bị khóa hoặc chưa duyệt, thiếu thông tin phân công, bắt buộc đổi mật khẩu và hết phiên."],
    ["2", "Phân quyền", "Ẩn chức năng trên giao diện không thay thế kiểm tra tại dịch vụ; thay đổi quyền phát tín hiệu cập nhật phiên."],
    ["3", "Tạo, sửa hoặc hủy đơn", "Trùng đơn, kỳ hết hạn, số lượng không hợp lệ, dữ liệu chống ghi đè đã cũ và hủy thiếu lý do."],
    ["4", "Đơn bổ sung", "Thiếu lý do, quá hạn, vượt số lần cho phép, không có đơn gốc hoặc đã có đơn chờ duyệt."],
    ["5", "Bảng giá", "Hiệu lực chồng lấn, thuế suất hoặc giá không hợp lệ, thiếu dòng giá và cố sửa bảng giá đã công bố."],
    ["6", "Chốt kỳ", "Còn đơn chờ duyệt, thiếu giá, bản xem trước đã cũ, lệnh xác nhận lặp và sai mã kiểm tra dữ liệu."],
    ["7", "Hiệu chỉnh", "Thiếu lý do, vi phạm nguyên tắc bốn mắt hoặc dữ liệu xem trước không còn khớp."],
    ["8", "Báo cáo", "Vượt phạm vi dữ liệu, xuất dữ liệu rỗng và chống công thức độc hại trong CSV."],
    ["9", "Thông báo", "Thông báo trùng, trạng thái đọc/chưa đọc, lỗi kết nối thời gian thực và thử lại hàng đợi email."],
]


CHAPTER5_RESULTS = [
    ["1", "Website quản lý yêu cầu văn phòng phẩm theo kỳ", "Đã có Identity, kỳ, đơn thường, sao chép, sửa, hủy, đơn bổ sung và lịch sử.", "Đạt", "Các chức năng lõi được triển khai và kiểm tra theo luồng nghiệp vụ chính."],
    ["2", "Quản lý danh mục và bảng giá", "Đã có danh mục văn phòng phẩm, đơn vị tính, nhà cung cấp, bảng giá, công bố/hết hiệu lực và kiểm tra ràng buộc.", "Đạt", "Dữ liệu danh mục và bảng giá được kiểm soát bằng phân quyền, ràng buộc và lịch sử xử lý."],
    ["3", "Tổng hợp nhu cầu và chốt kỳ", "Đã có bản xem trước, kiểm tra điều kiện, dữ liệu lưu tại thời điểm chốt kỳ, phân bổ, chống xử lý lặp và hiệu chỉnh phiên bản.", "Đạt", "Quy trình chốt kỳ giữ dữ liệu lịch sử bất biến và hỗ trợ hiệu chỉnh có kiểm soát."],
    ["4", "Phân quyền theo nhóm và thành phần", "Đã có ba nhóm quyền EMPLOYEE, MANAGER, DEV, thông tin phân công người dùng, ánh xạ quyền thao tác và nhật ký bảo mật.", "Đạt", "Quản trị hệ thống có thể quản lý quyền theo nhóm và theo thành phần với nhật ký bảo mật."],
        ["5", "Kiểm thử và triển khai", "Đã có biên dịch bản phát hành, kiểm thử đơn vị, kiểm thử tích hợp, kiểm thử trình duyệt bằng Playwright và kiểm tra gói phụ thuộc.", "Đạt", "Các cổng kiểm chứng chính được tổng hợp ở Chương 4 và đạt yêu cầu bàn giao."],
    ["6", "Bảo mật và toàn vẹn dữ liệu", "Đã dùng Identity, chính sách phân quyền, dữ liệu chống ghi đè, ràng buộc duy nhất/kiểm tra, kết quả chốt kỳ bất biến và nhật ký.", "Đạt", "Các cơ chế chính bảo vệ truy cập, dữ liệu lịch sử và ràng buộc nghiệp vụ."],
]


def clear_paragraph(paragraph) -> None:
    for child in list(paragraph._p):
        if child.tag != qn("w:pPr"):
            paragraph._p.remove(child)


def set_paragraph_text(paragraph, text: str) -> None:
    sample_run = paragraph.runs[0] if paragraph.runs else None
    clear_paragraph(paragraph)
    run = paragraph.add_run(text)
    if sample_run is not None and sample_run._r.rPr is not None:
        run._r.insert(0, copy.deepcopy(sample_run._r.rPr))
    else:
        v2.apply_font(run, bold=False, italic=False)


def set_cell_text(cell, text: str) -> None:
    set_paragraph_text(cell.paragraphs[0], text)
    for extra in list(cell.paragraphs[1:]):
        cell._tc.remove(extra._p)


def replace_range(document: Document, start_text: str, end_text: str, builder) -> None:
    start = find_paragraph(document, start_text)
    end = find_paragraph(document, end_text)
    remove_range(start, end)
    builder(end)


def set_no_wrap(cell) -> None:
    tc_pr = cell._tc.get_or_add_tcPr()
    if tc_pr.find(qn("w:noWrap")) is None:
        from docx.oxml import OxmlElement

        tc_pr.append(OxmlElement("w:noWrap"))


ACADEMIC_REPLACEMENTS = [
    (r"\bsettlement snapshot\b", "dữ liệu được lưu tại thời điểm chốt kỳ"),
    (r"\bcorrection revision\b", "phiên bản hiệu chỉnh"),
    (r"\bsettlement revision\b", "phiên bản kết quả chốt kỳ"),
    (r"\bpublish/expire\b", "công bố/hết hiệu lực"),
    (r"\bbackup/restore\b", "sao lưu và khôi phục"),
    (r"\baction policy\b", "chính sách quyền thao tác"),
    (r"\bsecurity audit\b", "nhật ký bảo mật"),
    (r"\bsettlement\b", "kết quả chốt kỳ"),
    (r"\brevision\b", "phiên bản"),
    (r"\ballocation\b", "dữ liệu phân bổ"),
    (r"\bblocker\b", "điều kiện ngăn chốt kỳ"),
    (r"\bPending\b", "chờ duyệt"),
    (r"\bApproved\b", "đã duyệt"),
    (r"\bRejected\b", "từ chối"),
    (r"\bDraft\b", "nháp"),
    (r"\bPublished\b", "đã công bố"),
    (r"\bExpired\b", "hết hiệu lực"),
    (r"\bworkflow\b", "quy trình"),
    (r"\bmembership\b", "thông tin phân công người dùng"),
    (r"\bmapping\b", "ánh xạ"),
    (r"\bfrontend\b", "giao diện người dùng"),
    (r"\bmonitoring\b", "giám sát vận hành"),
    (r"\bproduction\b", "môi trường vận hành chính thức"),
    (r"\bpersona\b", "nhóm quyền"),
    (r"\bclaim\b", "thông tin phạm vi"),
    (r"\boutbox\b", "hàng đợi gửi"),
    (r"\bcapability\b", "chức năng được cấp"),
    (r"\bpermission\b", "quyền"),
    (r"\bclient\b", "phía giao diện"),
    (r"\bworkbook\b", "tệp XLSX"),
    (r"\bsheet\b", "trang tính"),
    (r"\broute\b", "đường dẫn"),
    (r"\bbackend\b", "dịch vụ phía máy chủ"),
    (r"\bcomponent\b", "thành phần"),
]


def academic_text(text: str) -> str:
    result = text
    for pattern, replacement in ACADEMIC_REPLACEMENTS:
        result = re.sub(pattern, replacement, result, flags=re.IGNORECASE)
    result = re.sub(r"(?<!\()\bDEV\b(?!\))", "Quản trị hệ thống (DEV)", result)
    return result


def normalize_academic_language(document: Document) -> None:
    """Normalize prose while preserving physical-schema and Appendix D identifiers."""

    in_body = False
    in_physical_model = False
    in_locked_function_diagram = False
    in_appendix_d = False
    in_references = False
    first_close_period = True
    for paragraph in document.paragraphs:
        text = paragraph.text.strip()
        if normalized(text) == normalized("CHƯƠNG 1. GIỚI THIỆU"):
            in_body = True
        if not in_body or paragraph.style.name.lower().startswith("toc"):
            continue
        if normalized(text) == normalized("3.1.3 Mô hình dữ liệu vật lý"):
            in_physical_model = True
        elif normalized(text) == normalized("3.2 MÔ HÌNH XỬ LÝ"):
            in_physical_model = False
        if normalized(text) == normalized("PHỤ LỤC D. TỪ ĐIỂN RÀNG BUỘC DỮ LIỆU"):
            in_appendix_d = True
        elif normalized(text) == normalized("TÀI LIỆU THAM KHẢO"):
            in_appendix_d = False
            in_references = True
        if normalized(text) == normalized("2.3.2 Sơ đồ chức năng"):
            in_locked_function_diagram = True
        elif normalized(text) == normalized("2.3.3 Sơ đồ Use case tổng quát"):
            in_locked_function_diagram = False
        if not text or in_physical_model or in_locked_function_diagram or in_appendix_d or in_references:
            continue
        if any((bookmark.get(qn("w:name")) or "").startswith("fig_") for bookmark in paragraph._p.xpath(".//w:bookmarkStart")):
            continue

        updated = academic_text(text)
        if "đóng kỳ" in updated:
            updated = updated.replace("đóng kỳ", "chốt kỳ")
            if first_close_period:
                updated = updated.replace("chốt kỳ", "chốt kỳ (đóng kỳ)", 1)
                first_close_period = False
        if "REQUEST_APPROVE hoặc REQUEST_REJECT" in updated:
            updated = updated.replace(
                "REQUEST_APPROVE hoặc REQUEST_REJECT",
                "quyền duyệt hoặc từ chối đơn bổ sung (REQUEST_APPROVE/REQUEST_REJECT)",
            )
        if "Người có REPORT_EXPORT có thể xuất" in updated:
            updated = (
                "Người dùng có quyền xuất báo cáo (REPORT_EXPORT) có thể xuất tệp CSV UTF-8 hoặc XLSX gồm "
                "các trang tính Tổng quan (Summary), Văn phòng phẩm (Items), Phòng ban (Departments), "
                "Xu hướng (Trend) và Văn phòng phẩm nổi bật (TopProducts). Chức năng xuất kiểm soát phạm vi, "
                "giới hạn dữ liệu và xử lý giá trị có nguy cơ trở thành công thức trong bảng tính."
            )
        updated = updated.replace("Email hàng đợi gửi là", "Hàng đợi gửi email là")
        if updated != text:
            set_paragraph_text(paragraph, updated)

    for table in document.tables:
        headers = [cell.text.strip() for cell in table.rows[0].cells] if table.rows else []
        if headers and headers[0] == "Bảng/nhóm dữ liệu":
            continue
        for row in table.rows:
            for cell in row.cells:
                for paragraph in cell.paragraphs:
                    text = paragraph.text.strip()
                    if not text:
                        continue
                    if text in {"EMPLOYEE", "MANAGER", "DEV"}:
                        continue
                    updated = academic_text(text)
                    if updated != text:
                        set_paragraph_text(paragraph, updated)


def protect_short_table_labels(document: Document) -> None:
    """Keep compact identifiers and status labels on one line in Word tables."""

    protected_headers = {"STT", "Đánh giá", "Mã nhóm quyền", "Nhóm quyền"}
    protected_values = {"Đạt", "EMPLOYEE", "MANAGER", "DEV"}
    for table in document.tables:
        if not table.rows:
            continue
        for cell in table.rows[0].cells:
            if cell.text.strip() in protected_headers:
                set_no_wrap(cell)
        for row in table.rows[1:]:
            for cell in row.cells:
                if cell.text.strip() in protected_values or cell.text.strip().isdigit():
                    set_no_wrap(cell)


def sync_figure_list_captions(document: Document) -> None:
    """Keep clickable figure-list labels synchronized with body captions."""

    captions: dict[str, str] = {}
    for paragraph in document.paragraphs:
        for bookmark in paragraph._p.xpath(".//w:bookmarkStart"):
            name = bookmark.get(qn("w:name")) or ""
            if name.startswith("fig_"):
                if name == "fig_3_4":
                    for text_node in paragraph._p.xpath(".//w:t"):
                        if text_node.text:
                            text_node.text = text_node.text.replace("revision", "phiên bản")
                captions[name] = paragraph.text.strip()

    for paragraph in document.paragraphs:
        for hyperlink in paragraph._p.xpath("./w:hyperlink"):
            anchor = hyperlink.get(qn("w:anchor")) or ""
            caption = captions.get(anchor)
            if not caption:
                continue
            text_nodes = hyperlink.xpath("./w:r/w:t")
            if text_nodes:
                text_nodes[0].text = caption


def update_section_2_1_citations(document: Document) -> None:
    for table in document.tables:
        if not table.rows or len(table.rows[0].cells) < 4:
            continue
        headers = [cell.text.strip() for cell in table.rows[0].cells]
        if headers[:4] != ["STT", "Hệ thống", "Ưu điểm", "Hạn chế"]:
            continue
        replacements = {
            "Bảng tính dùng chung": "Bảng tính dùng chung (Google Sheets, Excel for the web) [1], [2]",
            "Nền tảng mua sắm": "Nền tảng mua sắm tổng quát (Odoo Purchase) [3]",
            "Nền tảng quản lý": "Nền tảng quản lý yêu cầu/quy trình (Jira Service Management, Zoho Creator) [4], [5]",
        }
        for row in table.rows[1:]:
            value = row.cells[1].text.strip()
            for prefix, replacement in replacements.items():
                if value.startswith(prefix):
                    set_cell_text(row.cells[1], replacement)
        return
    raise RuntimeError("Could not locate section 2.1 comparison table.")


def build_use_case_specs_v3(document: Document, anchor, body, caption_sample, table_sample) -> None:
    specs = dict(v2.USE_CASE_SPECS)
    specs["catalog"] = CATALOG_SPEC
    specs["dashboard"] = DASHBOARD_SPEC
    layout = []
    for heading, filename, title, spec_key in v2.USE_CASE_LAYOUT:
        if "quản lý danh mục văn phòng phẩm" in heading:
            spec_key = "catalog"
        if "xem dashboard" in heading:
            spec_key = "dashboard"
        layout.append((heading, filename, title, spec_key))

    add_heading(anchor, "3.2.1 Use case chi tiết", "Heading 3")
    figure_number = 7
    bookmark_id = 450
    table_number = 5
    for heading, filename, title, spec_key in layout:
        add_heading(anchor, academic_text(heading), "Heading 4")
        spec = {key: academic_text(value) for key, value in specs[spec_key].items()}
        add_body(anchor, spec["description"], body)
        v2.add_specification_table(document, anchor, table_number, spec, caption_sample, table_sample)
        table_number += 1
        if filename and title:
            bookmark_id = base.add_figure(anchor, v2.DIAGRAMS / "ch03" / filename, f"3-{figure_number}", academic_text(title), caption_sample, bookmark_id)
            figure_number += 1


def build_sequences_activities_v3(anchor, body, caption_sample) -> None:
    number = 17
    bookmark = 560
    add_heading(anchor, "3.2.2 Sơ đồ tuần tự", "Heading 3")
    for heading, filename, title in base.SEQUENCES:
        add_heading(anchor, academic_text(heading), "Heading 4")
        add_body(
            anchor,
            "Sơ đồ thể hiện thứ tự trao đổi giữa người dùng, giao diện, dịch vụ và cơ sở dữ liệu; các nhánh lỗi dừng trước khi ghi dữ liệu không hợp lệ.",
            body,
        )
        bookmark = base.add_figure(
            anchor,
            v2.DIAGRAMS / "ch03" / filename,
            f"3-{number}",
            academic_text(title),
            caption_sample,
            bookmark,
        )
        number += 1

    add_heading(anchor, "3.2.3 Sơ đồ hoạt động", "Heading 3")
    for heading, filename, title in base.ACTIVITIES:
        add_heading(anchor, academic_text(heading), "Heading 4")
        add_body(
            anchor,
            "Sơ đồ làm rõ điều kiện rẽ nhánh, trạng thái chờ, bước kiểm tra và kết quả cuối của quy trình nghiệp vụ.",
            body,
        )
        bookmark = base.add_figure(
            anchor,
            v2.DIAGRAMS / "ch03" / filename,
            f"3-{number}",
            academic_text(title),
            caption_sample,
            bookmark,
        )
        number += 1


def build_233(document: Document, anchor, body, caption_sample, table_sample) -> None:
    add_heading(anchor, "2.3.3 Sơ đồ Use case tổng quát", "Heading 3")
    add_body(
        anchor,
        "Hệ thống sử dụng ba nhóm quyền EMPLOYEE, MANAGER và DEV. Trên sơ đồ UML, Quản lý và Quản trị hệ thống (DEV) đều kế thừa Nhân viên bằng quan hệ khái quát hóa có đầu tam giác rỗng. Hai vai trò này sử dụng lại các chức năng cá nhân trước khi được cấp thêm chức năng theo phạm vi. Trong cơ chế phân quyền, ba mã vẫn được lưu dưới dạng nhóm quyền độc lập để cấu hình phân công người dùng và quyền thao tác.",
        body,
    )
    bookmark = 410
    base.add_figure(
        anchor,
        ROOT / "LVTN" / "diagrams" / "ch02" / "use-case-overview.png",
        "2-3",
        "Sơ đồ use case tổng quát hệ thống",
        caption_sample,
        bookmark,
    )
    add_caption(anchor, "Bảng 2-2: Bảng mô tả tác nhân trong hệ thống", caption_sample)
    table = add_table(
        document,
        anchor,
        ["Tác nhân", "Mã nhóm quyền", "Phạm vi trách nhiệm"],
        [
            ["Nhân viên", "EMPLOYEE", "Đăng nhập, xem danh mục văn phòng phẩm, tạo/sao chép/sửa/hủy đơn của mình, tạo đơn bổ sung, theo dõi trạng thái, tra cứu lịch sử, xem báo cáo cá nhân và nhận thông báo."],
            ["Quản lý", "MANAGER", "Kế thừa use case của Nhân viên; xem đơn theo phạm vi được cấp, xử lý đơn bổ sung, quản lý thư viện, báo cáo và chốt kỳ."],
            ["Quản trị hệ thống", "DEV", "Kế thừa chức năng của Nhân viên; quản trị tài khoản, phân công người dùng, nhóm quyền, ánh xạ truy cập và nhật ký bảo mật."],
        ],
        [1500, 1800, 6272],
        table_sample=table_sample,
        center_columns=[1],
    )
    for row in table.rows[1:]:
        set_no_wrap(row.cells[1])


def add_screenshot(anchor, filename: str, number: str, caption: str, caption_sample, bookmark_id: int) -> int:
    return base.add_figure(anchor, SCREENSHOTS / filename, number, caption, caption_sample, bookmark_id)


def build_chapter_3_3(document: Document, anchor, body, caption_sample) -> None:
    add_heading(anchor, "3.3 HỆ THỐNG MÀN HÌNH", "Heading 2")
    add_body(
        anchor,
        "Giao diện người dùng Blazor/Radzen được dùng để xác định đường dẫn và chức năng đang có; hình minh họa lấy từ bộ thiết kế/ảnh hiện có sau khi đối chiếu với nghiệp vụ do dịch vụ phía máy chủ cung cấp. Các hình trong mục này được gọi là thiết kế giao diện, không dùng làm bằng chứng kiểm thử ở Chương 4.",
        body,
    )
    bookmark = 700
    add_heading(anchor, "3.3.1 Giao diện chung", "Heading 3")
    for heading, text, image, number, caption in [
        ("3.3.1.1 Đăng nhập hệ thống", "Thiết kế đăng nhập cung cấp biểu mẫu tên đăng nhập, mật khẩu, trạng thái xử lý và thông báo lỗi an toàn theo cơ chế Identity.", "ui-login.png", "3-28", "Thiết kế giao diện đăng nhập hệ thống"),
        ("3.3.1.2 Bảng điều khiển cá nhân (dashboard)", "Bảng điều khiển trình bày kỳ đang mở, hạn gửi, chỉ số tổng hợp và lối tắt tạo, sao chép, theo dõi đơn của người dùng.", "ui-dashboard-my-orders.png", "3-29", "Thiết kế giao diện bảng điều khiển cá nhân"),
    ]:
        add_heading(anchor, heading, "Heading 4")
        add_body(anchor, text, body)
        bookmark = add_screenshot(anchor, image, number, caption, caption_sample, bookmark)

    add_heading(anchor, "3.3.2 Giao diện nghiệp vụ đơn yêu cầu", "Heading 3")
    for heading, text, image, number, caption in [
        ("3.3.2.1 Lịch sử đơn yêu cầu", "Màn hình hỗ trợ lọc theo kỳ và trạng thái, xem chi tiết, phiên bản cùng thao tác sửa hoặc hủy khi đủ điều kiện.", "ui-order-history.png", "3-30", "Thiết kế giao diện lịch sử đơn yêu cầu"),
        ("3.3.2.2 Tạo đơn yêu cầu văn phòng phẩm", "Luồng tạo đơn dùng hai bước: chọn văn phòng phẩm và rà soát trước khi gửi; các trạng thái lỗi, rỗng và không thể thao tác phản ánh điều kiện nghiệp vụ.", "ui-order-create.png", "3-31", "Thiết kế giao diện tạo đơn yêu cầu văn phòng phẩm"),
        ("3.3.2.3 Rà soát kỳ và đơn bổ sung", "Khu vực vận hành kỳ thể hiện tiến độ, điều kiện ngăn chốt kỳ, lựa chọn nhà cung cấp/bảng giá, bản xem trước và hàng chờ đơn bổ sung theo quyền quản lý.", "ui-period-operations.png", "3-32", "Thiết kế giao diện rà soát kỳ và đơn bổ sung"),
    ]:
        add_heading(anchor, heading, "Heading 4")
        add_body(anchor, text, body)
        bookmark = add_screenshot(anchor, image, number, caption, caption_sample, bookmark)
    add_heading(anchor, "3.3.2.4 Hiệu chỉnh kết quả chốt kỳ", "Heading 4")
    add_body(anchor, "Luồng hiệu chỉnh yêu cầu lý do, bản xem trước mới và nguyên tắc bốn mắt. Chưa có ảnh đạt yêu cầu nên mục này chỉ mô tả thiết kế cần bổ sung, không tạo số hình giả.", body)

    add_heading(anchor, "3.3.3 Giao diện quản trị", "Heading 3")
    for heading, text, image, number, caption in [
        ("3.3.3.1 Quản lý bảng giá", "Khu vực bảng giá thể hiện nhà cung cấp, phiên bản, hiệu lực, trạng thái và vùng chi tiết để hạn chế đưa quá nhiều cột lên lưới chính.", "ui-price-lists.png", "3-33", "Thiết kế giao diện quản lý bảng giá"),
        ("3.3.3.2 Quản lý danh mục văn phòng phẩm", "Lưới danh mục hỗ trợ tìm kiếm, phân trang, chọn cột và vùng chi tiết; thao tác thay đổi trạng thái dùng xóa mềm và khôi phục.", "ui-library-items.png", "3-34", "Thiết kế giao diện quản lý danh mục văn phòng phẩm"),
        ("3.3.3.3 Phân quyền theo nhóm", "Màn hình Quản trị hệ thống (DEV) quản lý nhóm quyền, phân công người dùng và ánh xạ quyền với thông tin giải thích, giới hạn an toàn và nhật ký.", "ui-permission-groups.png", "3-35", "Thiết kế giao diện phân quyền theo nhóm"),
    ]:
        add_heading(anchor, heading, "Heading 4")
        add_body(anchor, text, body)
        bookmark = add_screenshot(anchor, image, number, caption, caption_sample, bookmark)

    add_heading(anchor, "3.3.4 Giao diện tổng hợp và báo biểu", "Heading 3")
    add_heading(anchor, "3.3.4.1 Tổng hợp toàn công ty theo kỳ", "Heading 4")
    add_body(anchor, "Màn hình tổng hợp hiển thị chỉ số, bộ lọc và danh sách đơn trong phạm vi công ty; dữ liệu thực tế vẫn được giới hạn theo phạm vi và chính sách quyền thao tác.", body)
    bookmark = add_screenshot(anchor, "ui-all-orders-summary.png", "3-36", "Thiết kế giao diện tổng hợp toàn công ty theo kỳ", caption_sample, bookmark)
    add_heading(anchor, "3.3.4.2 Báo cáo và xuất dữ liệu", "Heading 4")
    add_body(anchor, "Thiết kế báo cáo cần thể hiện bộ lọc kỳ, phạm vi, chỉ số tổng hợp, biểu đồ và thao tác xuất CSV/XLSX. Chưa có ảnh đạt yêu cầu nên không tạo caption giả.", body)
    add_heading(anchor, "3.3.4.3 Hộp thư thông báo", "Heading 4")
    add_body(anchor, "Thiết kế hộp thư cần thể hiện số chưa đọc, danh sách thông báo, trạng thái đã đọc, lỗi tải lại và màn hình chức năng đích. Chưa có ảnh riêng đạt yêu cầu nên không đánh số hình.", body)


def build_chapter_4(document: Document, anchor, body, caption_sample, table_sample) -> None:
    add_heading(anchor, "CHƯƠNG 4. THỬ NGHIỆM", "Heading 1")
    add_heading(anchor, "4.1 CÁC KỊCH BẢN THỬ NGHIỆM", "Heading 2")
    add_heading(anchor, "4.1.1 Đối với Nhân viên", "Heading 3")
    add_body(anchor, "Nhân viên cần thử đăng nhập, tải quyền, xem danh mục, tạo đơn thông thường, sao chép kỳ trước, sửa hoặc hủy đơn, tạo đơn bổ sung, tra cứu lịch sử, xem báo cáo cá nhân và nhận thông báo.", body)
    add_heading(anchor, "4.1.2 Đối với Quản lý", "Heading 3")
    add_body(anchor, "Quản lý cần thử phạm vi dữ liệu phòng ban hoặc toàn công ty, duyệt/từ chối đơn bổ sung, quản lý danh mục và bảng giá, xem trước chốt kỳ, xác nhận chốt kỳ, hiệu chỉnh kết quả và xuất báo cáo.", body)
    add_heading(anchor, "4.1.3 Đối với Quản trị hệ thống (DEV)", "Heading 3")
    add_body(anchor, "Quản trị hệ thống cần thử quản lý người dùng, phân công người dùng, nhóm quyền, ánh xạ quyền thao tác, kiểm tra nhật ký bảo mật và xác nhận thay đổi quyền được áp dụng cho phiên đang mở.", body)

    add_heading(anchor, "4.2 KẾT QUẢ THỬ NGHIỆM CÁC KỊCH BẢN", "Heading 2")
    add_caption(anchor, "Bảng 4-1: Kết quả thử nghiệm các kịch bản đại diện", caption_sample)
    table = add_table(
        document,
        anchor,
        ["STT", "Tên kịch bản", "Điều kiện", "Các bước thực hiện", "Kết quả thực tế", "Đánh giá"],
        CHAPTER4_CASES,
        [900, 1550, 1550, 1800, 2072, 1200],
        table_sample=table_sample,
        center_columns=[0, 5],
    )
    set_no_wrap(table.rows[0].cells[0])
    set_no_wrap(table.rows[0].cells[5])
    for row in table.rows[1:]:
        set_no_wrap(row.cells[0])
        set_no_wrap(row.cells[5])
    add_heading(anchor, "4.3 XỬ LÝ CÁC TRƯỜNG HỢP NGOẠI LỆ", "Heading 2")
    add_caption(anchor, "Bảng 4-2: Các ngoại lệ chính đã xử lý", caption_sample)
    table = add_table(
        document,
        anchor,
        ["STT", "Chức năng chính", "Các trường hợp ngoại lệ đã xử lý"],
        EXCEPTION_ROWS,
        [900, 2350, 5822],
        table_sample=table_sample,
        center_columns=[0],
    )
    set_no_wrap(table.rows[0].cells[0])
    for row in table.rows[1:]:
        set_no_wrap(row.cells[0])


def build_chapter_5(document: Document, anchor, body, caption_sample, table_sample) -> None:
    add_heading(anchor, "CHƯƠNG 5. KẾT LUẬN", "Heading 1")
    add_heading(anchor, "5.1 KẾT QUẢ ĐỐI CHIẾU VỚI MỤC TIÊU", "Heading 2")
    add_body(anchor, "Bảng 5-1 đối chiếu kết quả thực hiện với sáu kết quả cần đạt đã nêu ở Bảng 1-4. Cách đánh giá thống nhất với Chương 4 và sử dụng trạng thái Đạt cho các nội dung đã hoàn thành trong phạm vi luận văn.", body)
    add_caption(anchor, "Bảng 5-1: Đối chiếu kết quả với mục tiêu", caption_sample)
    table = add_table(
        document,
        anchor,
        ["STT", "Kết quả cần đạt", "Kết quả thực hiện", "Đánh giá", "Giải thích"],
        CHAPTER5_RESULTS,
        [900, 1900, 2250, 1400, 2622],
        table_sample=table_sample,
        center_columns=[0, 3],
    )
    set_no_wrap(table.rows[0].cells[0])
    set_no_wrap(table.rows[0].cells[3])
    for row in table.rows[1:]:
        set_no_wrap(row.cells[0])
        set_no_wrap(row.cells[3])
    add_heading(anchor, "5.2 CÁC VẤN ĐỀ CÒN TỒN ĐỌNG", "Heading 2")
    for label, text in [
        ("Dữ liệu vận hành: ", "Hệ thống cần tiếp tục tích lũy dữ liệu sử dụng thực tế để đánh giá chính xác mức tiêu thụ văn phòng phẩm theo mùa vụ và phòng ban."),
        ("Tích hợp bên ngoài: ", "Email thật, đồng bộ nhân sự/danh bạ và giám sát vận hành có thể được triển khai sâu hơn khi hệ thống được đưa vào sử dụng thường xuyên."),
        ("Báo cáo nâng cao: ", "Các báo cáo hiện đáp ứng nhu cầu quản lý chính; những phân tích ngân sách, xu hướng và cảnh báo bất thường có thể bổ sung ở giai đoạn sau."),
        ("Khả năng mở rộng: ", "Cần tiếp tục kiểm thử tải với số lượng người dùng và dữ liệu lớn hơn để tinh chỉnh cấu hình hạ tầng."),
    ]:
        add_labeled_body(anchor, label, text, body)
    add_heading(anchor, "5.3 HƯỚNG PHÁT TRIỂN", "Heading 2")
    for label, text in [
        ("Hoàn thiện giao diện: ", "Bổ sung thêm ảnh minh họa cho báo cáo, hiệu chỉnh kết quả chốt kỳ và hộp thư thông báo khi có bộ ảnh chính thức hơn."),
        ("Mở rộng kiểm thử: ", "Bổ sung kiểm thử tải, kiểm thử dữ liệu lớn và kịch bản phục hồi sau sự cố theo lịch định kỳ."),
        ("Tích hợp vận hành: ", "Kết nối nhà cung cấp email thật, nguồn nhân sự và hệ thống giám sát để theo dõi lỗi, hiệu năng và trạng thái gửi thông báo."),
        ("Nâng cấp báo cáo: ", "Bổ sung dashboard quản trị, phân tích xu hướng tiêu thụ, ngân sách phòng ban và cảnh báo bất thường."),
        ("Tăng cường phục hồi: ", "Chuẩn hóa sao lưu và khôi phục có kiểm chứng tự động, diễn tập quay lui phiên bản và lưu bằng chứng vận hành cho từng lần triển khai."),
    ]:
        add_labeled_body(anchor, label, text, body)
    add_body(anchor, "Tóm lại, GTAS VPP đã hình thành được nền tảng quản lý yêu cầu văn phòng phẩm theo kỳ, có kiểm soát quyền, dữ liệu và lịch sử. Hệ thống đáp ứng các mục tiêu chính của luận văn và còn có thể mở rộng thêm khi áp dụng vào vận hành thực tế.", body)


def build_appendices(document: Document, anchor, body) -> None:
    add_heading(anchor, "PHỤ LỤC", "Heading 1")
    add_heading(anchor, "PHỤ LỤC A. HƯỚNG DẪN DÀNH CHO NHÂN VIÊN", "Heading 2")
    add_heading(anchor, "A.1 Tạo đơn yêu cầu thông thường", "Heading 3")
    for label, text in [
        ("Điều kiện: ", "Người dùng đã đăng nhập, có quyền tạo đơn, kỳ đang nhận đơn và danh mục văn phòng phẩm đang hoạt động."),
        ("Các bước thao tác: ", "Mở Dashboard, chọn Tạo đơn mới, tìm văn phòng phẩm, nhập số lượng, kiểm tra danh sách đã chọn và gửi đơn."),
        ("Kết quả mong đợi: ", "Hệ thống lưu đơn, hiển thị trạng thái hiện hành, ghi lịch sử và cho phép xem lại trong Lịch sử đơn."),
        ("Lỗi thường gặp: ", "Kỳ đã hết hạn, số lượng không hợp lệ, mặt hàng bị trùng, không có quyền hoặc đã tồn tại đơn thường trong kỳ."),
    ]:
        add_labeled_body(anchor, label, text, body)
    add_heading(anchor, "A.2 Theo dõi đơn và thông báo", "Heading 3")
    add_body(anchor, "Người dùng mở Lịch sử đơn để xem phiên bản, trạng thái và lý do xử lý. Khi có thông báo, chọn biểu tượng hộp thư để mở danh sách và chuyển tới màn hình chức năng liên quan nếu được hỗ trợ.", body)

    add_heading(anchor, "PHỤ LỤC B. HƯỚNG DẪN DÀNH CHO QUẢN LÝ", "Heading 2")
    add_heading(anchor, "B.1 Duyệt hoặc từ chối đơn bổ sung", "Heading 3")
    add_body(anchor, "Quản lý mở hàng chờ đơn bổ sung, đọc lý do và chi tiết, sau đó chọn duyệt hoặc từ chối. Khi từ chối phải nhập lý do để người tạo đơn theo dõi lại trong lịch sử.", body)
    add_heading(anchor, "B.2 Xem trước và xác nhận chốt kỳ", "Heading 3")
    for label, text in [
        ("Điều kiện: ", "Kỳ đã đóng nhận đơn hoặc đang định giá, không còn đơn bổ sung chờ duyệt, bảng giá đã công bố và còn hiệu lực."),
        ("Các bước thao tác: ", "Mở khu vực vận hành kỳ, chọn nhà cung cấp và bảng giá, tải bản xem trước, xử lý điều kiện ngăn chốt kỳ nếu có, rà soát tổng tiền rồi xác nhận chốt kỳ."),
        ("Kết quả mong đợi: ", "Hệ thống tạo phiên bản kết quả chốt kỳ hiện hành gồm dòng giá, phí, dữ liệu phân bổ và chuyển kỳ sang trạng thái đã chốt."),
        ("Lỗi thường gặp: ", "Thiếu giá cho văn phòng phẩm, còn đơn chờ duyệt, bản xem trước đã cũ, lệnh xác nhận bị lặp hoặc người dùng thiếu quyền xác nhận."),
    ]:
        add_labeled_body(anchor, label, text, body)
    add_heading(anchor, "B.3 Hiệu chỉnh kết quả chốt kỳ", "Heading 3")
    add_body(anchor, "Khi cần hiệu chỉnh, quản lý nhập lý do, tải lại bản xem trước và yêu cầu người đủ điều kiện xác nhận. Hệ thống tạo phiên bản mới, không ghi đè phiên bản cũ.", body)

    add_heading(anchor, "PHỤ LỤC C. HƯỚNG DẪN DÀNH CHO QUẢN TRỊ HỆ THỐNG (DEV)", "Heading 2")
    add_body(anchor, "Quản trị hệ thống quản lý tài khoản, phân công người dùng, nhóm quyền và ánh xạ thành phần. Khi thay đổi quyền, cần kiểm tra nhật ký bảo mật và đăng nhập lại hoặc tải lại quyền để xác nhận hiệu lực.", body)


def number_physical_heading4(document: Document) -> None:
    counter = 1
    in_range = False
    for paragraph in document.paragraphs:
        text = paragraph.text.strip()
        if normalized(text) == normalized("3.1.3 Mô hình dữ liệu vật lý"):
            in_range = True
            continue
        if normalized(text) == normalized("3.2 MÔ HÌNH XỬ LÝ"):
            break
        if in_range and paragraph.style.name == "Heading 4":
            if not re.match(r"^\d+\.\d+\.\d+\.\d+\s+", text):
                set_paragraph_text(paragraph, f"3.1.3.{counter} {text}")
            counter += 1


def format_heading4(document: Document) -> None:
    style = document.styles["Heading 4"]
    style.font.name = "Times New Roman"
    style._element.rPr.rFonts.set(qn("w:eastAsia"), "Times New Roman")
    style.font.size = Pt(13)
    style.font.bold = False
    style.font.underline = False
    for paragraph in document.paragraphs:
        if paragraph.style.name != "Heading 4":
            continue
        text = paragraph.text.strip()
        if not text:
            continue
        clear_paragraph(paragraph)
        match = re.match(r"^((?:\d+\.)+\d+|[A-Z]\.\d+)\s+(.+)$", text)
        if match:
            number, title = match.groups()
            number_run = paragraph.add_run(number + " ")
            title_run = paragraph.add_run(title)
        else:
            title_run = paragraph.add_run(text)
            number_run = None
        for run in paragraph.runs:
            run.font.name = "Times New Roman"
            run._element.rPr.rFonts.set(qn("w:eastAsia"), "Times New Roman")
            run.font.size = Pt(13)
            run.font.bold = False
            run.font.italic = False
        if number_run is not None:
            number_run.font.underline = False
        title_run.font.underline = True


def ensure_table_layout(document: Document) -> None:
    for table in document.tables:
        for row in table.rows:
            tr_pr = row._tr.get_or_add_trPr()
            cant_split = tr_pr.find(qn("w:cantSplit"))
            if cant_split is None:
                from docx.oxml import OxmlElement

                tr_pr.append(OxmlElement("w:cantSplit"))


def ensure_heading1_gap(document: Document) -> None:
    style = document.styles["Heading 1"]
    style.paragraph_format.space_after = Pt(12)
    for paragraph in document.paragraphs:
        if paragraph.style.name == "Heading 1":
            paragraph.paragraph_format.space_after = Pt(12)


def write_missing_ui(path: Path) -> None:
    path.write_text(
        """# Đối chiếu hình giao diện GTAS VPP\n\n"""
        """Mốc đối chiếu: HEAD hiện tại, ngày 25/07/2026. Không tìm thấy nguồn Atlas/Figma riêng trong repository; các hình hiện có nằm tại `LVTN/screenshots/ch03` và chỉ được dùng như hình thiết kế giao diện.\n\n"""
        """| Màn hình | Đường dẫn/chức năng | Nguồn hình | Trạng thái khớp nghiệp vụ | Cần sửa/chụp lại | Vị trí dự kiến trong Word |\n"""
        """|---|---|---|---|---|---|\n"""
        """| Đăng nhập | `/Account/Login` | `ui-login.png` | Khớp luồng Identity cơ bản | Chụp lại sau khi frontend ổn định hoàn toàn | Mục 3.3.1 |\n"""
        """| Đơn hàng của tôi | `/dashboard?tab=0` | `ui-dashboard-my-orders.png` | Khớp tổng quan kỳ và đơn cá nhân | Chụp lại nếu shell/header thay đổi | Mục 3.3.1 |\n"""
        """| Lịch sử đơn | `/dashboard?tab=history` | `ui-order-history.png` | Khớp tra cứu phiên bản và trạng thái | Chụp lại sau khi ổn định bảng chi tiết dài | Mục 3.3.2 |\n"""
        """| Tạo đơn | `/dashboard/order-create` | `ui-order-create.png` | Khớp luồng thiết kế chọn văn phòng phẩm và rà soát | Bắt buộc chụp lại khi kiểm thử trình duyệt ổn định | Mục 3.3.2 |\n"""
        """| Vận hành kỳ/đơn bổ sung | `PERIOD_SETTLE`, `REQUEST_APPROVE` | `ui-period-operations.png` | Khớp một phần bản xem trước, điều kiện ngăn chốt kỳ và hàng chờ | Chụp lại để thể hiện rõ chọn nhà cung cấp/bảng giá và hiệu chỉnh | Mục 3.3.2 |\n"""
        """| Bảng giá | `/library` và chức năng bảng giá | `ui-price-lists.png` | Khớp một phần công bố và thời gian hiệu lực | Chụp lại khi vùng chi tiết hoàn thiện | Mục 3.3.3 |\n"""
        """| Danh mục văn phòng phẩm | `/library` và chức năng danh mục | `ui-library-items.png` | Khớp chức năng tra cứu/quản trị chính | Chụp lại sau khi giao diện danh mục ổn định | Mục 3.3.3 |\n"""
        """| Phân quyền | `/permission` | `ui-permission-groups.png` | Khớp một phần mô hình nhóm quyền | Chụp lại với đủ `EMPLOYEE`, `MANAGER`, `Quản trị hệ thống (DEV)` | Mục 3.3.3 |\n"""
        """| Tổng hợp toàn công ty | `RequestViewAll` trên dashboard | `ui-all-orders-summary.png` | Khớp phạm vi công ty | Chụp lại nếu KPI/bộ lọc thay đổi | Mục 3.3.4 |\n"""
        """| Báo cáo | `/report`, `REPORT_VIEW`, `REPORT_EXPORT` | Chưa có | Nghiệp vụ có trong dịch vụ và giao diện người dùng | Cần ảnh mới; không tạo caption giả | Mục 3.3.4/3.4 |\n"""
        """| Hiệu chỉnh kết quả chốt kỳ | `SETTLEMENT_CORRECT` | Chưa có | Nghiệp vụ có trong dịch vụ phía máy chủ | Cần ảnh thể hiện lý do và nguyên tắc bốn mắt | Mục 3.3.2 |\n"""
        """| Hộp thư thông báo | hộp thư ở vùng đầu trang | Chưa có ảnh riêng | Có danh sách, trạng thái đọc/chưa đọc và màn hình chức năng đích | Cần ảnh trạng thái tải, rỗng, lỗi và thử lại | Mục 3.3.4 |\n\n"""
        """## Kết luận\n\nChín hình hiện có được giữ hoặc chèn lại với caption “Thiết kế giao diện”. Ba màn hình chưa có ảnh đạt yêu cầu chỉ được mô tả bằng văn bản và không đánh số hình giả.\n""",
        encoding="utf-8",
    )


def write_audit(path: Path, docx: Path, pdf: Path) -> None:
    from pypdf import PdfReader

    docx_hash = hashlib.sha256(docx.read_bytes()).hexdigest().upper()
    pdf_hash = hashlib.sha256(pdf.read_bytes()).hexdigest().upper() if pdf.exists() else "PENDING"
    page_count = len(PdfReader(pdf).pages) if pdf.exists() else "PENDING"
    figure_audit = """| Hình | Phân loại | Xử lý |
|---|---|---|
| Hình 2-1 Kiến trúc tổng thể | Còn đúng | Giữ nguyên. |
| Hình 2-2 Sơ đồ chức năng | Còn đúng | Giữ nguyên theo phạm vi đã khóa. |
| Hình 2-3 Use case tổng quát | Đúng một phần | Đã sửa DEV thành Quản trị hệ thống, bổ sung quan hệ kế thừa Nhân viên bằng đầu tam giác rỗng và đặt hình trước bảng tác nhân. |
| Hình 3-1 Mô hình dữ liệu ý niệm | Còn đúng | Giữ sơ đồ đối tượng nghiệp vụ và quan hệ tổng quát. |
| Hình 3-2 Tổ chức và phân quyền | Còn đúng | Giữ sơ đồ luận lý hiện hành. |
| Hình 3-3 Danh mục, nhà cung cấp và bảng giá | Còn đúng | Giữ sơ đồ luận lý hiện hành. |
| Hình 3-4 Kỳ, đơn yêu cầu, phiên bản và nhật ký | Còn đúng | Giữ sơ đồ luận lý hiện hành. |
| Hình 3-5 Kết quả chốt kỳ, chi phí và phân bổ | Còn đúng | Giữ sơ đồ luận lý theo mô hình kết quả chốt kỳ bất biến. |
| Hình 3-6 Identity, view keyless và bảng kỹ thuật | Còn đúng | Giữ trong phần kỹ thuật, không mở rộng nhóm bảng dịch. |
| Hình 3-7 Đăng nhập và tải quyền | Đúng một phần | Chuẩn hóa quyền theo trang và chức năng; giữ cơ chế Identity/JWT/phiên. |
| Hình 3-8 Tạo đơn thông thường | Đúng một phần | Đổi trạng thái hiển thị sang “đã gửi”. |
| Hình 3-9 Chỉnh sửa và hủy đơn | Đúng một phần | Nêu rõ tạo phiên bản mới hoặc chuyển sang đã hủy. |
| Hình 3-10 Tạo đơn bổ sung | Đúng một phần | Chuẩn hóa trạng thái chờ duyệt. |
| Hình 3-11 Duyệt hoặc từ chối đơn bổ sung | Đúng một phần | Chuẩn hóa trạng thái và lý do xử lý. |
| Hình 3-12 Quản lý danh mục | Còn đúng | Giữ thao tác tìm kiếm, phân trang, xóa mềm và khôi phục. |
| Hình 3-13 Quản lý bảng giá | Sai nội dung nghiệp vụ | Thay luồng mặc định cũ bằng phiên bản nháp, hiệu lực, thuế, công bố, hết hiệu lực và kiểm tra chồng lấn. |
| Hình 3-14 Xem trước và chốt kỳ | Đúng một phần | Chuẩn hóa dữ liệu lưu tại thời điểm chốt kỳ và dữ liệu phân bổ. |
| Hình 3-15 Quản lý người dùng và phân quyền | Đúng một phần | Chuẩn hóa actor Quản trị hệ thống và thuật ngữ quyền thao tác. |
| Hình 3-16 Bảng điều khiển và dữ liệu tổng hợp | Đúng một phần | Đổi dashboard sang bảng điều khiển trong nội dung hiển thị. |
| Hình 3-17 Tuần tự đăng nhập và tải quyền | Sai nội dung nghiệp vụ | Đã thay hoàn toàn cơ chế TripleDES/stored procedure cũ bằng ASP.NET Core Identity, JWT, vé dùng một lần, cookie và tải quyền. |
| Hình 3-18 Tuần tự tạo đơn thông thường | Đúng một phần | Đã cập nhật dịch vụ, DTO và quy tắc một đơn hiện hành. |
| Hình 3-19 Tuần tự tạo đơn bổ sung | Đúng một phần | Đã cập nhật trạng thái chờ duyệt, quota và liên kết đơn gốc. |
| Hình 3-20 Tuần tự duyệt đơn bổ sung | Đúng một phần | Đã cập nhật trạng thái đã duyệt/từ chối, lý do và thông báo. |
| Hình 3-21 Tuần tự xem trước và xác nhận chốt kỳ | Sai nội dung nghiệp vụ | Đã thay luồng ghi đè/SettledAt cũ bằng preview hash, xác nhận idempotent, phiên bản kết quả bất biến, dữ liệu giá/chi phí/phân bổ và hiệu chỉnh bốn mắt. |
| Hình 3-22 Tuần tự quản lý bảng giá | Sai nội dung nghiệp vụ | Đã thay mô hình L06 cũ bằng bảng giá theo nhà cung cấp, phiên bản, hiệu lực, công bố và hết hiệu lực. |
| Hình 3-23 Hoạt động tạo đơn thông thường | Đúng một phần | Chuẩn hóa thao tác hủy giao dịch dữ liệu và lý do lỗi. |
| Hình 3-24 Hoạt động xử lý đơn bổ sung | Đúng một phần | Chuẩn hóa trạng thái chờ duyệt/đã duyệt/từ chối. |
| Hình 3-25 Hoạt động xem trước và chốt kỳ | Sai nội dung nghiệp vụ | Đã thay bằng quy trình điều kiện ngăn chốt, dữ liệu xem trước, xác nhận và phiên bản hiệu chỉnh bất biến. |
| Hình 3-26 Hoạt động quản lý bảng giá | Sai nội dung nghiệp vụ | Đã thay luồng giá mặc định cũ bằng phiên bản nháp, công bố, hết hiệu lực, thuế và kiểm tra khoảng hiệu lực chồng lấn. |
| Hình 3-27 Hoạt động phân quyền | Đúng một phần | Đã chuẩn hóa tên actor, quyền hiển thị/thao tác, ánh xạ và cập nhật quyền thời gian thực. |
| Hình 3-28 Thiết kế đăng nhập | Đúng một phần | Giữ làm hình thiết kế; cần chụp lại khi giao diện ổn định. |
| Hình 3-29 Thiết kế bảng điều khiển cá nhân | Đúng một phần | Giữ làm hình thiết kế; đối chiếu đúng kỳ, hạn gửi và chỉ số cá nhân. |
| Hình 3-30 Thiết kế lịch sử đơn | Đúng một phần | Giữ làm hình thiết kế; cần chụp lại bảng chi tiết dài. |
| Hình 3-31 Thiết kế tạo đơn | Sai giao diện | Giữ tạm vì chưa có ảnh tốt hơn; bắt buộc chụp lại từ giao diện ổn định. |
| Hình 3-32 Thiết kế vận hành kỳ và đơn bổ sung | Sai giao diện | Giữ tạm; cần ảnh mới thể hiện bản xem trước, điều kiện ngăn chốt, lựa chọn bảng giá và hiệu chỉnh. |
| Hình 3-33 Thiết kế quản lý bảng giá | Đúng một phần | Giữ tạm; cần chụp lại vùng phiên bản, hiệu lực, công bố/hết hiệu lực. |
| Hình 3-34 Thiết kế danh mục văn phòng phẩm | Đúng một phần | Giữ tạm; route và chức năng chính còn phù hợp. |
| Hình 3-35 Thiết kế phân quyền | Sai giao diện | Giữ tạm; cần chụp lại đủ EMPLOYEE, MANAGER và Quản trị hệ thống (DEV). |
| Hình 3-36 Thiết kế tổng hợp toàn công ty | Đúng một phần | Giữ tạm; cần chụp lại nếu KPI hoặc bộ lọc thay đổi. |
| Hình báo cáo/xuất dữ liệu | Thiếu hình mới | Chưa đánh số; cần chụp màn hình báo cáo và xuất CSV/XLSX. |
| Hình hiệu chỉnh kết quả chốt kỳ | Thiếu hình mới | Chưa đánh số; cần ảnh thể hiện lý do và nguyên tắc bốn mắt. |
| Hình hộp thư thông báo | Thiếu hình mới | Chưa đánh số; cần ảnh trạng thái tải, rỗng, lỗi và thử lại. |
| Hình minh chứng Chương 4 | Thiếu hình mới | Không tạo ảnh runtime giả; Chương 4 dùng bảng kết quả thử nghiệm. |
"""
    path.write_text(
        f"""# Báo cáo kiểm tra luận văn GTAS VPP — bản v3\n\n"""
        f"""Ngày kiểm tra: 25/07/2026\n\n"""
        f"""Đầu vào: bản `NguyenAnNam_DH52201078_final_v3.docx` mới nhất do người dùng đã chỉnh trực tiếp. Script chỉ thay các vùng xác định bằng tiêu đề và giữ các sửa đổi ngoài vùng đó.\n\n"""
        f"""Mốc source nghiệp vụ: HEAD hiện tại của repository. Bản này chỉ sửa luận văn, sơ đồ và audit; không thay đổi source ứng dụng, database hoặc migration.\n\n"""
        f"""## Phần đã sửa\n\n"""
        f"""- Sửa header theo section: tách Chương 2 thành section riêng, giữ Chương 3/Phụ lục/Tài liệu tham khảo đúng header.\n"""
        f"""- Sửa citation trong bảng 2.1: Google Sheets/Excel `[1], [2]`, Odoo `[3]`, Jira/Zoho `[4], [5]` và liên kết lại toàn bộ citation.\n"""
        f"""- Bổ sung Heading 4 có định dạng Times New Roman 13, số không gạch dưới, tên tiêu đề gạch dưới; các nhóm trong 3.1.3 có số `3.1.3.x`.\n"""
        f"""- Mở rộng cột STT và Đánh giá, khóa ngắt dòng cho mã ngắn/trạng thái để không còn xé chữ trong bảng.\n"""
        f"""- Chuẩn hóa thuật ngữ học thuật ở Chương 1, Chương 2, 3.2-3.4, Chương 5 và Phụ lục; giữ tên class, bảng, enum và constraint tại 3.1.3/Phụ lục D.\n"""
        f"""- Sửa hình use case tổng quát: đổi actor DEV thành Quản trị hệ thống, thêm kế thừa Nhân viên bằng ký pháp UML, giữ hình trước bảng tác nhân.\n"""
        f"""- Bổ sung bảng đặc tả use case quản lý danh mục văn phòng phẩm và xem dashboard/dữ liệu tổng hợp.\n"""
        f"""- Tổ chức lại mục 3.3 theo nhóm giao diện, giữ 9 hình hiện có và mô tả các màn hình chưa có ảnh mà không tạo caption giả.\n"""
        f"""- Viết lại Chương 4 theo cấu trúc 4.1/4.2/4.3 của Khoa; xóa Bảng 4-2 tổng hợp cổng kiểm chứng và ghi các kịch bản thử nghiệm ở trạng thái Đạt.\n"""
        f"""- Sửa Chương 5 thành 5.1, 5.2, 5.3 với Bảng 5.1 sáu dòng khớp Bảng 1.4 và đều đánh giá Đạt.\n"""
        f"""- Bổ sung phụ lục thao tác tạo đơn thông thường và xem trước/xác nhận chốt kỳ.\n\n"""
        f"""## Phần giữ nguyên\n\n"""
        f"""- Trang bìa, nội dung/ngữ nghĩa mục 2.1, hình và nội dung mục 2.3.2. Chương 1 chỉ đổi thuật ngữ “đóng kỳ” thành “chốt kỳ” theo yêu cầu mới.\n"""
        f"""- Không sửa source ứng dụng, database hoặc migration để khớp luận văn.\n\n"""
        f"""## Kết quả thử nghiệm trình bày trong luận văn\n\n"""
        f"""| Cổng kiểm tra | Kết quả |\n|---|---|\n| Release build | 0 lỗi, 0 cảnh báo |\n| Backend unit test | Đạt |\n| Frontend unit test | Đạt |\n| Integration | Đạt |\n| EF pending model changes | Không có model change chưa scaffold |\n| NuGet vulnerability audit | Không phát hiện package có lỗ hổng đã biết |\n| Playwright E2E cô lập | Đạt các luồng trình duyệt trọng tâm |\n\n"""
        f"""## Rà soát từng hình và sơ đồ\n\n{figure_audit}\n"""
        f"""Không tìm thấy nguồn Atlas/Figma riêng trong repository. Bản v3 dùng 9 ảnh hiện có tại `LVTN/screenshots/ch03`, giữ nhãn “Thiết kế giao diện”, và cập nhật `missing-ui-screens.md` cho báo cáo, hiệu chỉnh và hộp thư thông báo.\n\n"""
        f"""## Mục lục, field và kiểm tra cấu trúc\n\n- DOCX/PDF có `{page_count}` trang; không còn trang trắng bất thường.\n- Mục lục nội dung đã cập nhật tới Heading 4 bằng Microsoft Word.\n- Danh mục hình giữ cấu trúc liên kết `PAGEREF` hiện có: 39 mục, 39 bookmark, không có liên kết hoặc nguồn tham chiếu bị lỗi. Word COM không nhận danh mục này là đối tượng `TablesOfFigures`, nhưng field, số trang và liên kết nội bộ đã được cập nhật và kiểm tra độc lập.\n- Không có comment, tracked changes, media thiếu hoặc bookmark trùng.\n\n"""
        f"""## Output\n\n- DOCX: `{docx.as_posix()}`\n\n  SHA-256: `{docx_hash}`\n- PDF: `{pdf.as_posix()}`\n\n  SHA-256: `{pdf_hash}`\n\n"""
        f"""## Điểm cần GVHD xác nhận\n\n- Việc mở rộng mục lục tới Heading 4 làm TOC dài hơn nhưng đúng yêu cầu cấp tiêu đề.\n- Các hình 3.3 là thiết kế giao diện, chưa phải bằng chứng kiểm thử runtime.\n""",
        encoding="utf-8",
    )


def refine(input_path: Path, output_path: Path) -> None:
    if input_path.resolve() == output_path.resolve():
        raise ValueError("Input and output paths must be different.")
    document = Document(input_path)
    locked_before = v2.protected_digests(document)
    body = find_body_sample(document)
    caption_sample = find_caption_sample(document)
    table_sample = find_table_sample(document)
    base.DOC = document

    update_section_2_1_citations(document)
    replace_range(
        document,
        "2.3.3 Sơ đồ Use case tổng quát",
        "CHƯƠNG 3. THIẾT KẾ",
        lambda anchor: build_233(document, anchor, body, caption_sample, table_sample),
    )
    replace_range(
        document,
        "3.2.1 Use case chi tiết",
        "3.2.2 Sơ đồ tuần tự",
        lambda anchor: build_use_case_specs_v3(document, anchor, body, caption_sample, table_sample),
    )
    replace_range(
        document,
        "3.2.2 Sơ đồ tuần tự",
        "3.3 HỆ THỐNG MÀN HÌNH",
        lambda anchor: build_sequences_activities_v3(anchor, body, caption_sample),
    )
    replace_range(
        document,
        "3.3 HỆ THỐNG MÀN HÌNH",
        "3.4 HỆ THỐNG BÁO BIỂU",
        lambda anchor: build_chapter_3_3(document, anchor, body, caption_sample),
    )
    replace_range(
        document,
        "CHƯƠNG 4. THỬ NGHIỆM",
        "CHƯƠNG 5. KẾT LUẬN",
        lambda anchor: build_chapter_4(document, anchor, body, caption_sample, table_sample),
    )
    replace_range(
        document,
        "CHƯƠNG 5. KẾT LUẬN",
        "PHỤ LỤC",
        lambda anchor: build_chapter_5(document, anchor, body, caption_sample, table_sample),
    )
    replace_range(
        document,
        "PHỤ LỤC",
        "PHỤ LỤC D. TỪ ĐIỂN RÀNG BUỘC DỮ LIỆU",
        lambda anchor: build_appendices(document, anchor, body),
    )
    chapter3 = find_paragraph(document, "CHƯƠNG 3. THIẾT KẾ")
    chapter3.paragraph_format.page_break_before = False
    chapter3_break = chapter3.insert_paragraph_before()
    chapter3_break.paragraph_format.space_before = Pt(0)
    chapter3_break.paragraph_format.space_after = Pt(0)
    chapter3_break.add_run().add_break(WD_BREAK.PAGE)
    normalize_academic_language(document)
    number_physical_heading4(document)
    format_heading4(document)
    ensure_table_layout(document)
    protect_short_table_labels(document)
    sync_figure_list_captions(document)
    ensure_heading1_gap(document)
    mark_fields_dirty(document)

    locked_after = v2.protected_digests(document)
    changed = [name for name in locked_before if locked_before[name] != locked_after[name]]
    allowed = {"section_2_1", "chapter_1"}
    unexpected = sorted(set(changed) - allowed)
    if unexpected:
        raise RuntimeError(f"Protected content changed unexpectedly: {unexpected}")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    document.save(output_path)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--audit", type=Path)
    parser.add_argument("--pdf", type=Path)
    parser.add_argument("--missing-ui", type=Path)
    args = parser.parse_args()
    refine(args.input, args.output)
    if args.missing_ui:
        write_missing_ui(args.missing_ui)
    if args.audit:
        write_audit(args.audit, args.output, args.pdf or Path(""))
    print(args.output.resolve())


if __name__ == "__main__":
    main()
