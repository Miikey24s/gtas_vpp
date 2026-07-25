#!/usr/bin/env python3
"""Build the GTAS VPP thesis final v3 from final v2.

This script intentionally keeps the v2 document as the input authority and
performs only the remaining local edits requested for v3. Word COM is used by
the companion PowerShell script for fields, section headers and PDF export.
"""

from __future__ import annotations

import argparse
import copy
import hashlib
import re
from pathlib import Path

from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH
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
    "actor": "Quản lý có quyền quản lý thư viện hoặc DEV",
    "description": "Quản lý danh mục, văn phòng phẩm, đơn vị tính và trạng thái hoạt động bằng thao tác có kiểm soát.",
    "pre": "Người dùng đã đăng nhập, có quyền thư viện; dữ liệu liên quan không bị khóa bởi ràng buộc nghiệp vụ.",
    "post": "Danh mục hoặc văn phòng phẩm được thêm, cập nhật, vô hiệu hóa hoặc khôi phục; dữ liệu lịch sử không bị xóa vật lý.",
    "main": "1. Mở màn hình danh mục.\n2. Tìm kiếm hoặc phân trang dữ liệu.\n3. Thêm hoặc cập nhật thông tin mã, tên, đơn vị tính và danh mục.\n4. Lưu thay đổi; backend kiểm tra mã duy nhất và trạng thái hợp lệ.",
    "alt": "Mã trùng, thiếu đơn vị tính, dữ liệu đang được tham chiếu hoặc rowversion cũ: hệ thống từ chối và giữ nguyên dữ liệu hiện hành.",
}


DASHBOARD_SPEC = {
    "name": "Xem dashboard và dữ liệu tổng hợp",
    "actor": "Nhân viên, Quản lý hoặc DEV theo phạm vi quyền",
    "description": "Xem KPI, danh sách đơn và dữ liệu tổng hợp theo phạm vi cá nhân, phòng ban hoặc toàn công ty.",
    "pre": "Người dùng đã đăng nhập; hệ thống đã tải permission và claim phạm vi dữ liệu.",
    "post": "Dashboard hiển thị đúng phạm vi; dữ liệu ngoài quyền không được trả về từ API.",
    "main": "1. Mở dashboard hoặc màn hình tổng hợp.\n2. Chọn kỳ, trạng thái hoặc phạm vi lọc.\n3. Hệ thống gọi API với claim hiện hành.\n4. Hiển thị KPI, danh sách đơn và trạng thái xử lý.",
    "alt": "Không có dữ liệu, thiếu quyền, API lỗi hoặc phiên hết hạn: hiển thị trạng thái rỗng/lỗi an toàn và yêu cầu đăng nhập lại khi cần.",
}


CHAPTER4_CASES = [
    ["1", "Đăng nhập và tải quyền", "Có tài khoản hợp lệ và membership hoạt động.", "Đăng nhập, mở dashboard, kiểm tra menu và API quyền.", "Identity xác thực đúng; quyền được tải và cưỡng chế ở API.", "Đạt"],
    ["2", "Từ chối đăng nhập sai", "Tài khoản hoặc mật khẩu không hợp lệ.", "Nhập thông tin sai và gửi biểu mẫu.", "Hệ thống trả thông báo an toàn, không lộ chi tiết kỹ thuật.", "Đạt"],
    ["3", "Tạo đơn thông thường", "Kỳ đang nhận đơn, danh mục hoạt động.", "Chọn văn phòng phẩm, nhập số lượng và gửi đơn.", "Đơn, chi tiết, revision và nhật ký được tạo theo transaction.", "Đạt"],
    ["4", "Ngăn trùng đơn thường", "Người dùng đã có đơn hiện hành trong kỳ.", "Gửi yêu cầu tạo đơn lần hai hoặc request đồng thời.", "Ràng buộc unique/idempotency ngăn tạo trùng đơn hiện hành.", "Đạt"],
    ["5", "Sao chép, sửa và hủy đơn", "Đơn thuộc người dùng và còn trong trạng thái cho phép.", "Sao chép kỳ trước, sửa chi tiết, hủy đơn có lý do.", "Revision mới, trạng thái hủy và lịch sử được ghi nhận.", "Đạt"],
    ["6", "Tạo đơn bổ sung", "Có đơn gốc hợp lệ và còn cửa sổ bổ sung.", "Nhập lý do, chọn mặt hàng và gửi đơn bổ sung.", "Đơn bổ sung Pending được tạo, gắn với đơn gốc và có nhật ký.", "Đạt"],
    ["7", "Duyệt hoặc từ chối đơn bổ sung", "Có đơn bổ sung Pending trong phạm vi xử lý.", "Quản lý duyệt một đơn và từ chối một đơn khác.", "Trạng thái, lý do, người xử lý và thông báo được lưu.", "Đạt"],
    ["8", "Kiểm soát phạm vi dữ liệu", "Có người dùng ở nhiều phòng ban.", "Mở dashboard theo vai trò nhân viên/quản lý/DEV.", "API giới hạn dữ liệu theo claim và permission.", "Đạt một phần"],
    ["9", "Quản lý danh mục văn phòng phẩm", "Người dùng có quyền thư viện.", "Tìm kiếm, phân trang, thêm/sửa/vô hiệu hóa dữ liệu.", "Backend kiểm tra mã duy nhất và xóa mềm; UI còn cần chụp lại.", "Đạt một phần"],
    ["10", "Quản lý bảng giá", "Có nhà cung cấp và văn phòng phẩm.", "Tạo bảng giá draft, dòng giá, publish/expire.", "Ràng buộc hiệu lực, version, VAT và rowversion được kiểm tra.", "Đạt"],
    ["11", "Xem trước chốt kỳ", "Kỳ đã đóng nhận đơn hoặc đang định giá.", "Chọn nhà cung cấp/bảng giá và tải preview.", "Preview phát hiện blocker và tính tổng theo dữ liệu hợp lệ.", "Đạt"],
    ["12", "Blocker chốt kỳ", "Còn đơn Pending hoặc thiếu giá.", "Thử xác nhận chốt kỳ.", "Hệ thống chặn xác nhận và trả danh sách điều kiện cần xử lý.", "Đạt"],
    ["13", "Xác nhận chốt kỳ", "Preview không còn blocker.", "Xác nhận bằng hash preview và idempotency key.", "Settlement snapshot, chi phí và phân bổ được tạo bất biến.", "Đạt"],
    ["14", "Idempotency chốt kỳ", "Có yêu cầu xác nhận đã xử lý.", "Gửi lại cùng idempotency key.", "Hệ thống trả cùng kết quả, không tạo settlement trùng.", "Đạt"],
    ["15", "Hiệu chỉnh kết quả chốt kỳ", "Kỳ đã chốt và có lý do hiệu chỉnh.", "Tạo revision hiệu chỉnh với nguyên tắc bốn mắt.", "Revision mới được tạo; revision cũ được giữ để audit.", "Đạt"],
    ["16", "Quản lý người dùng và phân quyền", "DEV đã đăng nhập.", "Cập nhật membership và mapping quyền.", "Security audit và tín hiệu cập nhật quyền được ghi; UI còn cần hoàn thiện.", "Đạt một phần"],
    ["17", "Báo cáo và xuất dữ liệu", "Có dữ liệu đơn và quyền báo cáo.", "Xem KPI, lọc phạm vi, xuất CSV/XLSX.", "Xuất dữ liệu đúng phạm vi; kiểm soát CSV injection.", "Đạt một phần"],
    ["18", "Thông báo và email outbox", "Có sự kiện duyệt/từ chối hoặc chốt kỳ.", "Nhận thông báo, đánh dấu đã đọc, kiểm tra outbox.", "Inbox bền vững và dedupe/outbox được kiểm tra; email thật chưa triển khai.", "Đạt một phần"],
    ["19", "Playwright E2E cô lập", "Fixture QA cô lập.", "Chạy bộ E2E trình duyệt theo log hiện có.", "12/27 đạt, còn 15 lỗi nên chưa đạt cổng E2E.", "Chưa đạt"],
]


SUMMARY_ROWS = [
    ["Release build", "0 lỗi, 0 cảnh báo", "Đạt"],
    ["Backend unit test", "412/414 đạt, 2 chưa đạt", "Chưa đạt"],
    ["Frontend unit test", "150/151 đạt, 1 chưa đạt", "Chưa đạt"],
    ["Integration", "14 đạt, 6 chưa chạy do yêu cầu LocalDB opt-in", "Đạt một phần"],
    ["Playwright E2E", "12/27 đạt, 15 chưa đạt, khoảng 21,98 phút", "Chưa đạt"],
    ["Audit bảo mật/gói phụ thuộc", "Không phát hiện package có lỗ hổng đã biết theo log sử dụng", "Đạt"],
]


EXCEPTION_ROWS = [
    ["1", "Đăng nhập", "Sai mật khẩu, tài khoản bị khóa/chưa duyệt, thiếu membership, bắt buộc đổi mật khẩu và hết phiên."],
    ["2", "Phân quyền", "Ẩn chức năng trên UI không thay thế kiểm tra API; thay đổi quyền phát tín hiệu cập nhật phiên."],
    ["3", "Tạo/sửa/hủy đơn", "Trùng đơn, kỳ hết hạn, số lượng không hợp lệ, rowversion cũ và hủy thiếu lý do."],
    ["4", "Đơn bổ sung", "Thiếu lý do, quá hạn, vượt quota, không có đơn gốc hoặc đã có đơn Pending."],
    ["5", "Bảng giá", "Hiệu lực chồng lấn, VAT/giá không hợp lệ, thiếu dòng giá và cố sửa bảng giá đã công bố."],
    ["6", "Chốt kỳ", "Còn đơn Pending, thiếu giá, preview cũ, lệnh xác nhận lặp và sai hash đầu vào."],
    ["7", "Hiệu chỉnh", "Thiếu lý do, vi phạm nguyên tắc bốn mắt, dữ liệu preview không còn khớp."],
    ["8", "Báo cáo", "Vượt phạm vi dữ liệu, xuất dữ liệu rỗng và chống công thức độc hại trong CSV."],
    ["9", "Thông báo", "Thông báo trùng, trạng thái đọc/chưa đọc, lỗi SignalR và retry email outbox."],
]


CHAPTER5_RESULTS = [
    ["1", "Website quản lý yêu cầu văn phòng phẩm theo kỳ", "Đã có Identity, kỳ, đơn thường, sao chép, sửa, hủy, đơn bổ sung và lịch sử.", "Đạt một phần", "Chức năng lõi có trong backend và phần giao diện, nhưng E2E trình duyệt chưa đạt toàn bộ."],
    ["2", "Quản lý danh mục và bảng giá", "Đã có danh mục văn phòng phẩm, đơn vị tính, nhà cung cấp, bảng giá, publish/expire và kiểm tra ràng buộc.", "Đạt một phần", "Backend và test trọng tâm có bằng chứng; giao diện danh mục/bảng giá còn cần hoàn thiện hình và E2E."],
    ["3", "Tổng hợp nhu cầu và chốt kỳ", "Đã có preview, blocker, settlement snapshot, phân bổ, idempotency và hiệu chỉnh revision.", "Đạt một phần", "Luồng nghiệp vụ có kiểm thử backend; UI vận hành/chốt kỳ và ảnh minh họa chưa đủ hoàn chỉnh."],
    ["4", "Phân quyền theo nhóm và thành phần", "Đã có persona EMPLOYEE, MANAGER, DEV, membership, mapping component/action và audit bảo mật.", "Đạt một phần", "Cơ chế quyền đã có nhưng màn hình phân quyền và E2E còn tồn đọng."],
    ["5", "Kiểm thử và triển khai", "Đã có build, unit, integration mặc định, Playwright E2E và audit gói phụ thuộc.", "Chưa đạt", "Backend, frontend và E2E vẫn còn test chưa đạt; integration SQL đặc thù chưa chạy trong mặc định."],
    ["6", "Bảo mật và toàn vẹn dữ liệu", "Đã dùng Identity, policy authorization, rowversion, unique/check constraint, settlement bất biến và audit.", "Đạt một phần", "Các cơ chế chính đã có; cần thêm bằng chứng vận hành cho backup/restore, tải và giám sát production."],
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
        add_heading(anchor, heading, "Heading 4")
        spec = specs[spec_key]
        add_body(anchor, spec["description"], body)
        v2.add_specification_table(document, anchor, table_number, spec, caption_sample, table_sample)
        table_number += 1
        if filename and title:
            bookmark_id = base.add_figure(anchor, v2.DIAGRAMS / "ch03" / filename, f"3-{figure_number}", title, caption_sample, bookmark_id)
            figure_number += 1


def add_screenshot(anchor, filename: str, number: str, caption: str, caption_sample, bookmark_id: int) -> int:
    return base.add_figure(anchor, SCREENSHOTS / filename, number, caption, caption_sample, bookmark_id)


def build_chapter_3_3(document: Document, anchor, body, caption_sample) -> None:
    add_heading(anchor, "3.3 HỆ THỐNG MÀN HÌNH", "Heading 2")
    add_body(
        anchor,
        "Frontend Blazor/Radzen được dùng để xác định route và capability đang có; hình minh họa lấy từ bộ thiết kế/ảnh hiện có sau khi đối chiếu với nghiệp vụ backend. Các hình trong mục này được gọi là thiết kế giao diện, không dùng làm bằng chứng kiểm thử ở Chương 4.",
        body,
    )
    bookmark = 700
    add_heading(anchor, "3.3.1 Giao diện chung", "Heading 3")
    for heading, text, image, number, caption in [
        ("3.3.1.1 Đăng nhập hệ thống", "Thiết kế đăng nhập cung cấp biểu mẫu tên đăng nhập, mật khẩu, trạng thái xử lý và thông báo lỗi an toàn theo cơ chế Identity.", "ui-login.png", "3-28", "Thiết kế giao diện đăng nhập hệ thống"),
        ("3.3.1.2 Dashboard đơn hàng cá nhân", "Dashboard trình bày kỳ đang mở, hạn gửi, KPI và lối tắt tạo, sao chép, theo dõi đơn của người dùng.", "ui-dashboard-my-orders.png", "3-29", "Thiết kế giao diện dashboard đơn hàng cá nhân"),
    ]:
        add_heading(anchor, heading, "Heading 4")
        add_body(anchor, text, body)
        bookmark = add_screenshot(anchor, image, number, caption, caption_sample, bookmark)

    add_heading(anchor, "3.3.2 Giao diện nghiệp vụ đơn yêu cầu", "Heading 3")
    for heading, text, image, number, caption in [
        ("3.3.2.1 Lịch sử đơn yêu cầu", "Màn hình hỗ trợ lọc theo kỳ và trạng thái, xem chi tiết, revision cùng thao tác sửa hoặc hủy khi đủ điều kiện.", "ui-order-history.png", "3-30", "Thiết kế giao diện lịch sử đơn yêu cầu"),
        ("3.3.2.2 Tạo đơn yêu cầu văn phòng phẩm", "Luồng tạo đơn dùng hai bước: chọn văn phòng phẩm và rà soát trước khi gửi; các trạng thái lỗi, rỗng và disabled phản ánh điều kiện nghiệp vụ.", "ui-order-create.png", "3-31", "Thiết kế giao diện tạo đơn yêu cầu văn phòng phẩm"),
        ("3.3.2.3 Rà soát kỳ và đơn bổ sung", "Khu vực vận hành kỳ thể hiện tiến độ, blocker, lựa chọn nhà cung cấp/bảng giá, preview và hàng chờ đơn bổ sung theo quyền quản lý.", "ui-period-operations.png", "3-32", "Thiết kế giao diện rà soát kỳ và đơn bổ sung"),
    ]:
        add_heading(anchor, heading, "Heading 4")
        add_body(anchor, text, body)
        bookmark = add_screenshot(anchor, image, number, caption, caption_sample, bookmark)
    add_heading(anchor, "3.3.2.4 Hiệu chỉnh kết quả chốt kỳ", "Heading 4")
    add_body(anchor, "Luồng hiệu chỉnh yêu cầu lý do, bản xem trước mới và nguyên tắc bốn mắt. Chưa có ảnh đạt yêu cầu nên mục này chỉ mô tả thiết kế cần bổ sung, không tạo số hình giả.", body)

    add_heading(anchor, "3.3.3 Giao diện quản trị", "Heading 3")
    for heading, text, image, number, caption in [
        ("3.3.3.1 Quản lý bảng giá", "Workspace bảng giá thể hiện nhà cung cấp, phiên bản, hiệu lực, trạng thái và panel chi tiết để hạn chế đưa quá nhiều cột lên lưới chính.", "ui-price-lists.png", "3-33", "Thiết kế giao diện quản lý bảng giá"),
        ("3.3.3.2 Quản lý danh mục văn phòng phẩm", "Lưới danh mục hỗ trợ tìm kiếm, phân trang, chọn cột và panel chi tiết; thao tác thay đổi trạng thái dùng xóa mềm và khôi phục.", "ui-library-items.png", "3-34", "Thiết kế giao diện quản lý danh mục văn phòng phẩm"),
        ("3.3.3.3 Phân quyền theo nhóm", "Màn hình DEV quản lý persona, membership và mapping quyền với thông tin giải thích, giới hạn an toàn và audit.", "ui-permission-groups.png", "3-35", "Thiết kế giao diện phân quyền theo nhóm"),
    ]:
        add_heading(anchor, heading, "Heading 4")
        add_body(anchor, text, body)
        bookmark = add_screenshot(anchor, image, number, caption, caption_sample, bookmark)

    add_heading(anchor, "3.3.4 Giao diện tổng hợp và báo biểu", "Heading 3")
    add_heading(anchor, "3.3.4.1 Tổng hợp toàn công ty theo kỳ", "Heading 4")
    add_body(anchor, "Màn hình tổng hợp hiển thị KPI, bộ lọc và danh sách đơn trong phạm vi công ty; dữ liệu thực tế vẫn do backend giới hạn theo claim và action policy.", body)
    bookmark = add_screenshot(anchor, "ui-all-orders-summary.png", "3-36", "Thiết kế giao diện tổng hợp toàn công ty theo kỳ", caption_sample, bookmark)
    add_heading(anchor, "3.3.4.2 Báo cáo và xuất dữ liệu", "Heading 4")
    add_body(anchor, "Thiết kế báo cáo cần thể hiện bộ lọc kỳ, phạm vi, KPI, biểu đồ và thao tác xuất CSV/XLSX. Chưa có ảnh đạt yêu cầu nên không tạo caption giả.", body)
    add_heading(anchor, "3.3.4.3 Hộp thư thông báo", "Heading 4")
    add_body(anchor, "Thiết kế hộp thư cần thể hiện số chưa đọc, danh sách thông báo, trạng thái đã đọc, lỗi tải lại và route đích. Chưa có ảnh riêng đạt yêu cầu nên không đánh số hình.", body)


def build_chapter_4(document: Document, anchor, body, caption_sample, table_sample) -> None:
    add_heading(anchor, "CHƯƠNG 4. THỬ NGHIỆM", "Heading 1")
    add_body(
        anchor,
        "Các kịch bản thử nghiệm được ghi nhận tại commit 5f24fda, ngày 25/07/2026, trên môi trường Windows, .NET 10, SQL Server/LocalDB và trình duyệt Playwright cô lập. Từ mốc đó đến bản luận văn này, source ứng dụng không thay đổi; vì vậy số liệu kiểm thử được dùng lại từ log hiện có và các lỗi còn lại được giữ nguyên trạng thái.",
        body,
    )
    add_heading(anchor, "4.1 CÁC KỊCH BẢN THỬ NGHIỆM", "Heading 2")
    add_heading(anchor, "4.1.1 Đối với Nhân viên", "Heading 3")
    add_body(anchor, "Nhân viên cần thử đăng nhập, tải quyền, xem danh mục, tạo đơn thông thường, sao chép kỳ trước, sửa hoặc hủy đơn, tạo đơn bổ sung, tra cứu lịch sử, xem báo cáo cá nhân và nhận thông báo.", body)
    add_heading(anchor, "4.1.2 Đối với Quản lý", "Heading 3")
    add_body(anchor, "Quản lý cần thử phạm vi dữ liệu phòng ban hoặc toàn công ty, duyệt/từ chối đơn bổ sung, quản lý danh mục và bảng giá, xem trước chốt kỳ, xác nhận chốt kỳ, hiệu chỉnh kết quả và xuất báo cáo.", body)
    add_heading(anchor, "4.1.3 Đối với DEV", "Heading 3")
    add_body(anchor, "DEV cần thử quản lý người dùng, membership, nhóm quyền, mapping thành phần/action, kiểm tra security audit và xác nhận thay đổi quyền được áp dụng cho phiên đang mở.", body)

    add_heading(anchor, "4.2 KẾT QUẢ THỬ NGHIỆM CÁC KỊCH BẢN", "Heading 2")
    add_caption(anchor, "Bảng 4-1: Kết quả thử nghiệm các kịch bản đại diện", caption_sample)
    table = add_table(
        document,
        anchor,
        ["STT", "Tên test case", "Điều kiện", "Các bước thực hiện", "Kết quả thực tế", "Đánh giá"],
        CHAPTER4_CASES,
        [650, 1700, 1800, 2050, 2200, 672],
        table_sample=table_sample,
        center_columns=[0, 5],
    )
    for row in table.rows[1:]:
        set_no_wrap(row.cells[0])
        set_no_wrap(row.cells[5])
    add_body(anchor, "Bảng tổng hợp dưới đây chỉ trình bày kết quả ở mức cổng kiểm chứng; chi tiết lệnh chạy, selector và lỗi implementation được giữ trong audit/log thay vì đưa vào phần chính.", body)
    add_caption(anchor, "Bảng 4-2: Tổng hợp cổng kiểm chứng", caption_sample)
    add_table(document, anchor, ["Hạng mục", "Kết quả", "Đánh giá"], SUMMARY_ROWS, [2900, 4300, 1872], table_sample=table_sample, center_columns=[2])

    add_heading(anchor, "4.3 XỬ LÝ CÁC TRƯỜNG HỢP NGOẠI LỆ", "Heading 2")
    add_caption(anchor, "Bảng 4-3: Các ngoại lệ chính đã xử lý", caption_sample)
    table = add_table(
        document,
        anchor,
        ["STT", "Chức năng chính", "Các trường hợp ngoại lệ đã xử lý"],
        EXCEPTION_ROWS,
        [650, 2500, 5922],
        table_sample=table_sample,
        center_columns=[0],
    )
    for row in table.rows[1:]:
        set_no_wrap(row.cells[0])


def build_chapter_5(document: Document, anchor, body, caption_sample, table_sample) -> None:
    add_heading(anchor, "CHƯƠNG 5. KẾT LUẬN", "Heading 1")
    add_heading(anchor, "5.1 KẾT QUẢ ĐỐI CHIẾU VỚI MỤC TIÊU", "Heading 2")
    add_body(anchor, "Bảng 5-1 đối chiếu kết quả thực hiện với sáu kết quả cần đạt đã nêu ở Bảng 1-4. Cách đánh giá thống nhất với Chương 4: chỉ sử dụng Đạt, Đạt một phần hoặc Chưa đạt.", body)
    add_caption(anchor, "Bảng 5-1: Đối chiếu kết quả với mục tiêu", caption_sample)
    add_table(
        document,
        anchor,
        ["STT", "Kết quả cần đạt", "Kết quả thực hiện", "Đánh giá", "Giải thích"],
        CHAPTER5_RESULTS,
        [650, 2100, 2450, 1050, 2822],
        table_sample=table_sample,
        center_columns=[0, 3],
    )
    add_heading(anchor, "5.2 CÁC VẤN ĐỀ CÒN TỒN ĐỌNG", "Heading 2")
    for label, text in [
        ("Frontend và E2E: ", "Một số route và workflow trình duyệt chưa ổn định, đặc biệt tạo đơn, phân quyền, báo cáo, thông báo và layout bảng dài."),
        ("Kiểm thử tải: ", "Chưa có bằng chứng kiểm thử tải đầy đủ cho nhiều người dùng đồng thời, dữ liệu lớn và thời gian chạy dài."),
        ("Email, HR và monitoring: ", "Email thật, đồng bộ nhân sự/danh bạ và giám sát vận hành production chưa triển khai chính thức."),
        ("Báo cáo doanh nghiệp: ", "Báo cáo hiện đáp ứng KPI và xuất dữ liệu cơ bản, chưa hoàn thiện phân tích quản trị chuyên sâu."),
        ("Backup/restore: ", "Đã có script và kiểm tra cục bộ, nhưng chưa đủ bằng chứng vận hành định kỳ trên môi trường production thật."),
    ]:
        add_labeled_body(anchor, label, text, body)
    add_heading(anchor, "5.3 HƯỚNG PHÁT TRIỂN", "Heading 2")
    for label, text in [
        ("Hoàn thiện giao diện: ", "Ổn định toàn bộ route chính, bổ sung ảnh thiết kế còn thiếu và đóng các lỗi Playwright E2E."),
        ("Mở rộng kiểm thử: ", "Bổ sung kiểm thử tải, kiểm thử dữ liệu lớn và kịch bản phục hồi sau sự cố theo lịch định kỳ."),
        ("Tích hợp vận hành: ", "Kết nối nhà cung cấp email thật, nguồn nhân sự và hệ thống giám sát để theo dõi lỗi, hiệu năng và trạng thái gửi thông báo."),
        ("Nâng cấp báo cáo: ", "Bổ sung dashboard quản trị, phân tích xu hướng tiêu thụ, ngân sách phòng ban và cảnh báo bất thường."),
        ("Tăng cường phục hồi: ", "Chuẩn hóa backup/restore có kiểm chứng tự động, diễn tập rollback và lưu bằng chứng vận hành cho từng lần triển khai."),
    ]:
        add_labeled_body(anchor, label, text, body)
    add_body(anchor, "Tóm lại, GTAS VPP đã hình thành được nền tảng quản lý yêu cầu văn phòng phẩm theo kỳ, có kiểm soát quyền, dữ liệu và lịch sử. Hệ thống vẫn cần hoàn thiện giao diện, kiểm thử và vận hành trước khi xem là sẵn sàng cho sử dụng chính thức.", body)


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
    add_body(anchor, "Người dùng mở Lịch sử đơn để xem revision, trạng thái và lý do xử lý. Khi có thông báo, chọn biểu tượng hộp thư để mở danh sách và chuyển tới chức năng liên quan nếu route được hỗ trợ.", body)

    add_heading(anchor, "PHỤ LỤC B. HƯỚNG DẪN DÀNH CHO QUẢN LÝ", "Heading 2")
    add_heading(anchor, "B.1 Duyệt hoặc từ chối đơn bổ sung", "Heading 3")
    add_body(anchor, "Quản lý mở hàng chờ đơn bổ sung, đọc lý do và chi tiết, sau đó chọn duyệt hoặc từ chối. Khi từ chối phải nhập lý do để người tạo đơn theo dõi lại trong lịch sử.", body)
    add_heading(anchor, "B.2 Xem trước và xác nhận chốt kỳ", "Heading 3")
    for label, text in [
        ("Điều kiện: ", "Kỳ đã đóng nhận đơn hoặc đang định giá, không còn đơn bổ sung Pending, bảng giá đã công bố và còn hiệu lực."),
        ("Các bước thao tác: ", "Mở khu vực vận hành kỳ, chọn nhà cung cấp và bảng giá, tải bản xem trước, xử lý blocker nếu có, rà soát tổng tiền rồi xác nhận chốt kỳ."),
        ("Kết quả mong đợi: ", "Hệ thống tạo settlement revision hiện hành gồm dòng giá, phí, phân bổ và chuyển kỳ sang trạng thái đã chốt."),
        ("Lỗi thường gặp: ", "Thiếu giá cho văn phòng phẩm, còn đơn Pending, preview đã cũ, sai idempotency key hoặc người dùng thiếu quyền xác nhận."),
    ]:
        add_labeled_body(anchor, label, text, body)
    add_heading(anchor, "B.3 Hiệu chỉnh kết quả chốt kỳ", "Heading 3")
    add_body(anchor, "Khi cần hiệu chỉnh, quản lý nhập lý do, tải lại preview và yêu cầu người đủ điều kiện xác nhận. Hệ thống tạo revision mới, không ghi đè revision cũ.", body)

    add_heading(anchor, "PHỤ LỤC C. HƯỚNG DẪN DÀNH CHO DEV", "Heading 2")
    add_body(anchor, "DEV quản lý tài khoản, membership, nhóm quyền và mapping thành phần. Khi thay đổi quyền, cần kiểm tra security audit và đăng nhập lại hoặc tải lại quyền để xác nhận hiệu lực.", body)


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


def write_missing_ui(path: Path) -> None:
    path.write_text(
        """# Đối chiếu hình giao diện GTAS VPP\n\n"""
        """Mốc đối chiếu: commit `5f24fda286799d4b9869c0d381b8042fbc2c2120`, ngày 25/07/2026. Không tìm thấy nguồn Atlas/Figma riêng trong repository; các hình hiện có nằm tại `LVTN/screenshots/ch03` và chỉ được dùng như hình thiết kế giao diện.\n\n"""
        """| Màn hình | Route/capability | Nguồn hình | Trạng thái khớp nghiệp vụ | Cần sửa/chụp lại | Vị trí dự kiến trong Word |\n"""
        """|---|---|---|---|---|---|\n"""
        """| Đăng nhập | `/Account/Login` | `ui-login.png` | Khớp luồng Identity cơ bản | Chụp lại sau khi frontend ổn định hoàn toàn | Mục 3.3.1 |\n"""
        """| Đơn hàng của tôi | `/dashboard?tab=0` | `ui-dashboard-my-orders.png` | Khớp tổng quan kỳ và đơn cá nhân | Chụp lại nếu shell/header thay đổi | Mục 3.3.1 |\n"""
        """| Lịch sử đơn | `/dashboard?tab=history` | `ui-order-history.png` | Khớp tra cứu revision và trạng thái | Chụp lại sau khi ổn định bảng chi tiết dài | Mục 3.3.2 |\n"""
        """| Tạo đơn | `/dashboard/order-create` | `ui-order-create.png` | Khớp luồng thiết kế chọn văn phòng phẩm và rà soát | Bắt buộc chụp lại khi E2E tạo đơn ổn định | Mục 3.3.2 |\n"""
        """| Vận hành kỳ/đơn bổ sung | `PERIOD_SETTLE`, `REQUEST_APPROVE` | `ui-period-operations.png` | Khớp một phần preview, blocker và hàng chờ | Chụp lại để thể hiện rõ chọn NCC/bảng giá và hiệu chỉnh | Mục 3.3.2 |\n"""
        """| Bảng giá | `/library` và capability bảng giá | `ui-price-lists.png` | Khớp một phần publish/effectivity | Chụp lại khi panel chi tiết hoàn thiện | Mục 3.3.3 |\n"""
        """| Danh mục văn phòng phẩm | `/library` và capability danh mục | `ui-library-items.png` | Khớp chức năng tra cứu/quản trị chính | Chụp lại sau khi UI danh mục ổn định | Mục 3.3.3 |\n"""
        """| Phân quyền | `/permission` | `ui-permission-groups.png` | Khớp một phần mô hình persona | Chụp lại với đủ `EMPLOYEE`, `MANAGER`, `DEV` | Mục 3.3.3 |\n"""
        """| Tổng hợp toàn công ty | `RequestViewAll` trên dashboard | `ui-all-orders-summary.png` | Khớp phạm vi công ty | Chụp lại nếu KPI/bộ lọc thay đổi | Mục 3.3.4 |\n"""
        """| Báo cáo | `/report`, `REPORT_VIEW`, `REPORT_EXPORT` | Chưa có | Nghiệp vụ có trong backend/frontend | Cần ảnh mới; không tạo caption giả | Mục 3.3.4/3.4 |\n"""
        """| Hiệu chỉnh kết quả chốt kỳ | `SETTLEMENT_CORRECT` | Chưa có | Nghiệp vụ có trong backend | Cần ảnh thể hiện lý do và nguyên tắc bốn mắt | Mục 3.3.2 |\n"""
        """| Hộp thư thông báo | header notification/inbox | Chưa có ảnh riêng | Có inbox, trạng thái đọc/chưa đọc và route đích | Cần ảnh loading/rỗng/lỗi/retry | Mục 3.3.4 |\n\n"""
        """## Kết luận\n\nChín hình hiện có được giữ hoặc chèn lại với caption “Thiết kế giao diện”. Ba màn hình chưa có ảnh đạt yêu cầu chỉ được mô tả bằng văn bản và không đánh số hình giả.\n""",
        encoding="utf-8",
    )


def write_audit(path: Path, docx: Path, pdf: Path) -> None:
    docx_hash = hashlib.sha256(docx.read_bytes()).hexdigest().upper()
    pdf_hash = hashlib.sha256(pdf.read_bytes()).hexdigest().upper() if pdf.exists() else "PENDING"
    path.write_text(
        f"""# Báo cáo kiểm tra luận văn GTAS VPP — bản v3\n\n"""
        f"""Ngày kiểm tra: 25/07/2026\n\n"""
        f"""Đầu vào: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_v2.docx`\n\n"""
        f"""Mốc source nghiệp vụ: `5f24fda286799d4b9869c0d381b8042fbc2c2120`. HEAD hiện tại chỉ thay đổi luận văn/tooling so với mốc này trong phạm vi app, nên Chương 4 dùng lại log kiểm thử hiện có.\n\n"""
        f"""## Phần đã sửa\n\n"""
        f"""- Sửa header theo section: tách Chương 2 thành section riêng, giữ Chương 3/Phụ lục/Tài liệu tham khảo đúng header.\n"""
        f"""- Sửa citation trong bảng 2.1: Google Sheets/Excel `[1], [2]`, Odoo `[3]`, Jira/Zoho `[4], [5]` và liên kết lại toàn bộ citation.\n"""
        f"""- Bổ sung Heading 4 có định dạng Times New Roman 13, số không gạch dưới, tên tiêu đề gạch dưới; các nhóm trong 3.1.3 có số `3.1.3.x`.\n"""
        f"""- Bổ sung bảng đặc tả use case quản lý danh mục văn phòng phẩm và xem dashboard/dữ liệu tổng hợp.\n"""
        f"""- Tổ chức lại mục 3.3 theo nhóm giao diện, giữ 9 hình hiện có và mô tả các màn hình chưa có ảnh mà không tạo caption giả.\n"""
        f"""- Viết lại Chương 4 theo cấu trúc 4.1/4.2/4.3 của Khoa; xóa mục tồn đọng kỹ thuật khỏi Chương 4.\n"""
        f"""- Sửa Chương 5 thành 5.1, 5.2, 5.3 với Bảng 5.1 sáu dòng khớp Bảng 1.4 và thống nhất với Chương 4.\n"""
        f"""- Bổ sung phụ lục thao tác tạo đơn thông thường và xem trước/xác nhận chốt kỳ.\n\n"""
        f"""## Phần giữ nguyên\n\n"""
        f"""- Trang bìa, Chương 1, nội dung/ngữ nghĩa mục 2.1, hình và nội dung mục 2.3.2.\n"""
        f"""- Không sửa source ứng dụng, database hoặc migration để khớp luận văn.\n\n"""
        f"""## Kết quả build/test sử dụng\n\n"""
        f"""| Cổng kiểm tra | Kết quả |\n|---|---|\n| Release build | 0 lỗi, 0 cảnh báo |\n| Backend unit test | 412/414 đạt, 2 chưa đạt |\n| Frontend unit test | 150/151 đạt, 1 chưa đạt |\n| Integration | 14 đạt, 6 chưa chạy do yêu cầu LocalDB opt-in |\n| EF pending model changes | Không có model change chưa scaffold |\n| NuGet vulnerability audit | Không phát hiện package có lỗ hổng đã biết |\n| Playwright E2E cô lập | 12/27 đạt, 15 không đạt, khoảng 21,98 phút |\n\n"""
        f"""## Hình Atlas/giao diện\n\nKhông tìm thấy nguồn Atlas/Figma riêng trong repository. Bản v3 dùng 9 ảnh hiện có tại `LVTN/screenshots/ch03`, giữ nhãn “Thiết kế giao diện”, và cập nhật `missing-ui-screens.md` cho báo cáo, hiệu chỉnh và hộp thư thông báo.\n\n"""
        f"""## Output\n\n- DOCX: `{docx.as_posix()}`\n\n  SHA-256: `{docx_hash}`\n- PDF: `{pdf.as_posix()}`\n\n  SHA-256: `{pdf_hash}`\n\n"""
        f"""## Điểm cần GVHD xác nhận\n\n- Việc mở rộng mục lục tới Heading 4 làm TOC dài hơn nhưng đúng yêu cầu cấp tiêu đề.\n- Các hình 3.3 là thiết kế giao diện, chưa phải bằng chứng kiểm thử runtime.\n- Chương 4 giữ trạng thái test chưa đạt thay vì đổi sang đạt theo sự tồn tại của code.\n""",
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
        "3.2.1 Use case chi tiết",
        "3.2.2 Sơ đồ tuần tự",
        lambda anchor: build_use_case_specs_v3(document, anchor, body, caption_sample, table_sample),
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
    number_physical_heading4(document)
    format_heading4(document)
    mark_fields_dirty(document)

    locked_after = v2.protected_digests(document)
    changed = [name for name in locked_before if locked_before[name] != locked_after[name]]
    allowed = {"section_2_1"}
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
