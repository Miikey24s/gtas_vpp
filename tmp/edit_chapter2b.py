from copy import deepcopy
from pathlib import Path
from shutil import copy2
from zipfile import ZIP_DEFLATED, ZipFile

from lxml import etree


ROOT = Path(r"D:\WORK\gtas_vpp")
SOURCE = ROOT / "LVTN" / "checkpoints" / "02A_chuong2_congnghe.docx"
CHECKPOINT = ROOT / "LVTN" / "checkpoints" / "02B_chuong2_nghiepvu.docx"
WORKING = ROOT / "LVTN" / "NguyenAnNam_DH52201078_working.docx"
OUTPUT = ROOT / "tmp" / "02B_chuong2_nghiepvu_patched.docx"

W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
NS = {"w": W_NS}

PERMISSION_NOTE = (
    "Lưu ý về phạm vi phân quyền: danh sách trang và component được tải khi đăng nhập "
    "để điều khiển khả năng hiển thị, tương tác trên frontend. Ở phiên bản hiện tại, "
    "các controller nghiệp vụ liên quan chủ yếu mới áp dụng [Authorize] chung, chưa gắn "
    "policy riêng cho từng thao tác; vì vậy các vai trò nêu dưới đây phản ánh cấu hình "
    "giao diện, chưa phải ràng buộc quyền đã được cưỡng chế đầy đủ tại API."
)

REPLACEMENTS = {
    '- Nếu tài khoản hoặc mật khẩu không đúng, hệ thống trả về thông báo "Tên đăng nhập hoặc mật khẩu không chính xác" và yêu cầu nhập lại.':
        '- Nếu xác thực không thành công, Backend trả về phản hồi từ chối đăng nhập với thông báo lỗi phù hợp; frontend hiển thị thông báo và yêu cầu người dùng nhập lại.',
    '- Nếu xác thực thành công, Backend tạo token JWT có thời hạn 24 giờ, chứa các thông tin quan trọng của người dùng gồm: mã người dùng (UserID), tên đăng nhập, tên đầy đủ, mã nhóm quyền (GroupId), mã phòng ban (DepartmentCode) và vai trò quản trị (IsAdmin).':
        '- Nếu xác thực thành công, Backend tạo JWT có thời hạn 24 giờ. Token chứa UserID, UserLogin, họ tên (Name), GroupId, IsAdmin; bổ sung DepartmentCode và môi trường Server khi có dữ liệu tương ứng.',
    '- Kỳ yêu cầu phải chưa được thực hiện thao tác đóng kỳ. Nếu đã đóng kỳ, hệ thống thông báo "Kỳ này đã được đóng, không thể tạo đơn mới".':
        '- Backend chặn tạo đơn khi đã tồn tại ít nhất một đầu đơn không bị xóa trong cùng kỳ có trường SettledAt. Kiểm tra này được thực hiện sau các ràng buộc về kỳ và trùng đơn.',
    '- Hệ thống tạo đầu đơn với mã đơn tự động (VPP-YYYYMM-GUID), trạng thái "Đã gửi" (Submitted), thông tin phòng ban và thời điểm gửi đơn.':
        '- Hệ thống tạo đầu đơn với mã tự động dạng VPP-YYYYMM-GUID, trạng thái "Đã gửi" (Submitted), DepartmentCode và thời điểm gửi. MemberCompanyCode được đọc từ claim, nhưng AuthController hiện chưa thêm claim này vào JWT nên giá trị có thể rỗng; đây là tồn đọng kỹ thuật cần xử lý.',
    '- Hệ thống tạo chi tiết đơn cho từng vật tư, đồng thời tra cứu và ghi nhận đơn giá hiện tại từ bảng giá mặc định đang có hiệu lực.':
        '- Hệ thống tạo chi tiết cho từng vật tư và tra cứu đơn giá trong bảng giá mặc định. Nếu chưa có bảng giá mặc định hoặc vật tư chưa có ánh xạ giá, phiên bản hiện tại vẫn lưu chi tiết với đơn giá 0 thay vì từ chối tạo đơn.',
    '4. Quản trị viên phê duyệt hoặc từ chối':
        '4. Người xử lý phê duyệt hoặc từ chối',
    '4. Quản trị viên phê duyệt hoặc từ chối:':
        '4. Người xử lý phê duyệt hoặc từ chối:',
    '- Quản trị viên truy cập tab "Duyệt đơn bổ sung" để xem danh sách các đơn đang chờ duyệt.':
        '- Người dùng được cấu hình hiển thị và kích hoạt tab "Duyệt đơn bổ sung" xem danh sách các đơn Pending. API hiện chỉ yêu cầu đăng nhập, chưa kiểm tra policy duyệt riêng ở backend.',
    '- Phê duyệt: Hệ thống chuyển trạng thái từ "Chờ duyệt" sang "Đã duyệt" (Approved), ghi nhận mã người duyệt và thời điểm duyệt.':
        '- Phê duyệt: state machine chỉ cho phép chuyển từ Pending sang Approved; hệ thống ghi ApprovedById, ApprovedAt và xóa lý do từ chối cũ nếu có.',
    '- Từ chối: Quản trị viên có thể nhập lý do từ chối. Hệ thống chuyển trạng thái sang "Bị từ chối" (Rejected), ghi nhận lý do, mã người từ chối và thời điểm từ chối.':
        '- Từ chối: state machine chỉ cho phép chuyển từ Pending sang Rejected; hệ thống ghi lý do, RejectedById và RejectedAt.',
    '4. Ghi lịch sử thao tác và khóa kỳ':
        '4. Ghi lịch sử và đánh dấu các đơn đã chốt',
    '- Quản trị viên truy cập vùng chức năng Library, tab "Bảng giá" để quản lý các bảng giá hiện có.':
        '- Người dùng được cấp component tương ứng trên giao diện Library truy cập tab "Bảng giá" để quản lý các bảng giá hiện có.',
    '- Quản trị viên truy cập tab "Đóng kỳ", chọn kỳ cần đóng và bảng giá áp dụng.':
        '- Người dùng được hiển thị tab "Đóng kỳ" chọn năm, tháng và bảng giá áp dụng. Backend hiện chỉ kiểm tra người dùng đã đăng nhập, chưa giới hạn kỳ được chọn theo ngày hiện tại hoặc policy riêng.',
    '- Hệ thống ghi nhận đơn giá cố định vào từng dòng chi tiết đơn.':
        '- Hệ thống chỉ lấy các đơn không bị xóa có trạng thái Submitted hoặc Approved, sau đó ghi đơn giá của bảng giá được chọn vào từng dòng chi tiết.',
    '- Cập nhật thông tin đóng kỳ: thời điểm đóng, mã người thực hiện và mã bảng giá áp dụng.':
        '- Trên từng đầu đơn được xử lý, hệ thống cập nhật SettledAt, SettledByUserId, SettledByPriceListId và thời điểm cập nhật trong cùng transaction.',
    '- Ghi lại lịch sử với tiêu đề "PERIOD_SETTLED". Sau khi đóng, tất cả yêu cầu tạo đơn mới cho kỳ đã đóng sẽ bị chặn.':
        '- Mỗi đơn được xử lý có log "PERIOD_SETTLED". Backend chưa tạo bản ghi kỳ độc lập; trạng thái đóng được suy ra từ các đầu đơn có SettledAt. Do đó, nếu kỳ không có đơn Submitted/Approved thì thao tác không tạo dấu vết đóng kỳ; việc tạo đơn mới chỉ bị chặn khi đã có ít nhất một đầu đơn cùng kỳ mang SettledAt.',
    '- Quản trị viên đăng nhập và truy cập vùng chức năng Library.':
        '- Người dùng được cấu hình quyền giao diện đăng nhập và truy cập vùng chức năng Library.',
    '- Hệ thống hỗ trợ chế độ "Hiển thị mục đã xóa" để quản trị viên có thể xem và quản lý các mục đã bị vô hiệu hóa.':
        '- Giao diện dùng PATCH trường IsDeleted để vô hiệu hóa hoặc kích hoạt lại và hỗ trợ "Hiển thị mục đã xóa". Endpoint DELETE generic là xóa vật lý nên không được đồng nhất với thao tác vô hiệu hóa.',
    '- Quản trị viên truy cập vùng chức năng Library, tab "Nhà cung cấp".':
        '- Người dùng được cấp component tương ứng truy cập Library, tab "Nhà cung cấp".',
    '- Quản trị viên có thể vô hiệu hóa nhà cung cấp không còn hợp tác. Nhà cung cấp bị vô hiệu hóa sẽ không hiển thị khi thiết lập giá.':
        '- Nhà cung cấp không còn hợp tác được đánh dấu IsDeleted qua PATCH; bản ghi này bị loại khỏi danh sách thiết lập giá và bị VPPPriceService từ chối khi tạo hoặc cập nhật ánh xạ giá.',
    '- Quản trị viên truy cập vùng chức năng Library, tab "Phân loại".':
        '- Người dùng được cấp component tương ứng truy cập Library, tab "Phân loại".',
    '- Quản trị viên có thể vô hiệu hóa các giá trị không còn sử dụng và kích hoạt lại khi cần.':
        '- Nhóm phân loại hoặc giá trị chi tiết có thể được vô hiệu hóa và kích hoạt lại bằng PATCH IsDeleted; đây là thao tác mềm, không phải DELETE vật lý.',
    '- Quản trị viên truy cập vùng chức năng Library, tab "Phòng ban".':
        '- Người dùng được cấp component tương ứng truy cập Library, tab "Phòng ban".',
    '- Quản trị viên có thể vô hiệu hóa phòng ban không còn hoạt động và kích hoạt lại khi cần.':
        '- Phòng ban không còn hoạt động được vô hiệu hóa hoặc kích hoạt lại bằng PATCH IsDeleted. Backend chưa kiểm tra policy riêng cho thao tác này.',
    '- Quản trị viên truy cập vùng chức năng Library, tab "Giá" (Pricing).':
        '- Người dùng được cấp component tương ứng truy cập Library, tab "Giá" (Pricing).',
    '- Quản trị viên có thể xóa ánh xạ giá khi không còn sử dụng.':
        '- Khi không còn sử dụng, ánh xạ giá được xóa mềm: IsDeleted chuyển thành true và IsDefault được đặt lại false.',
    '- Người dùng truy cập trang Dashboard, chọn tab "Tổng hợp phòng ban" hoặc "Tổng hợp toàn doanh nghiệp" tùy theo quyền được cấp.':
        '- Người dùng truy cập Dashboard; frontend hiển thị tab tổng hợp cá nhân, phòng ban hoặc toàn doanh nghiệp theo cấu hình page/component đã tải khi đăng nhập.',
    '- Tổng hợp phòng ban: Người dùng có quyền quản lý phòng ban xem tổng hợp toàn bộ đơn yêu cầu trong phòng ban mình, lọc theo năm/tháng/trạng thái/mã phòng ban.':
        '- Tổng hợp phòng ban: frontend mặc định gửi DepartmentCode của người dùng, nhưng API hiện vẫn chấp nhận departmentCode từ query và chưa cưỡng chế giá trị này phải trùng claim. Vì vậy phạm vi phòng ban mới được kiểm soát ở giao diện.',
    '- Tổng hợp toàn doanh nghiệp: Quản trị viên xem tổng hợp toàn bộ đơn yêu cầu trên toàn doanh nghiệp, bao gồm tổng thành tiền, lọc theo phòng ban/năm/tháng/trạng thái.':
        '- Tổng hợp toàn doanh nghiệp: endpoint all-orders trả dữ liệu toàn hệ thống khi không truyền bộ lọc phòng ban và chỉ yêu cầu đăng nhập. Đây là chức năng dự kiến dành cho quản trị viên nhưng chưa có policy backend tương ứng.',
    '- Trang Dashboard hiển thị biểu đồ tổng quan: số lượng đơn và số lượng vật tư theo từng tháng, phân bố trạng thái đơn và tổng số đơn.':
        '- Dashboard hiển thị biểu đồ theo dữ liệu của người dùng hiện tại: số đơn, tổng số lượng và số dòng theo tháng, phân bố trạng thái và tổng số đơn.',
    '- Quản trị viên truy cập vùng chức năng Permission (Phân quyền), tab "Phân quyền trang".':
        '- Người dùng được cấp component tương ứng truy cập Permission, tab "Phân quyền trang".',
    '- Quản trị viên có thể xóa nhóm quyền không còn sử dụng.':
        '- Endpoint xóa nhóm quyền hiện gọi DeleteAsync và xóa vật lý; chưa có kiểm tra policy riêng hoặc cảnh báo tham chiếu ở controller, vì vậy chỉ nên thực hiện sau khi kiểm tra quan hệ dữ liệu.',
    '- Sau khi chọn một nhóm quyền, hệ thống hiển thị ma trận phân quyền gồm tất cả các trang và component của hệ thống như Dashboard, Library, Permission, Report và các tab/component con.':
        '- Sau khi chọn một nhóm quyền, hệ thống nhóm các page-component mapping đã tồn tại của nhóm thành ma trận theo trang; nếu nhóm chưa có mapping thì API trả danh sách rỗng.',
    '- Quản trị viên truy cập vùng chức năng Permission, tab "Người dùng".':
        '- Người dùng được cấp component tương ứng truy cập Permission, tab "Người dùng".',
    '- Người dùng nhấn vào một đơn để xem chi tiết, bao gồm: danh sách vật tư, số lượng, đơn giá, ghi chú, thông tin người tạo, thời điểm tạo/cập nhật và lịch sử trạng thái.':
        '- Người dùng nhấn vào đơn để xem đầu đơn, danh sách vật tư, số lượng, đơn giá, ghi chú và thời điểm tạo/cập nhật. DTO chi tiết hiện chưa trả danh sách VPP03_Log nên chưa có dòng thời gian lịch sử trạng thái; endpoint lấy chi tiết theo id cũng chưa kiểm tra đơn có thuộc người dùng hiện tại hay không.',
}


def paragraph_text(paragraph):
    return "".join(paragraph.xpath(".//w:t/text()", namespaces=NS))


def set_paragraph_text(paragraph, text):
    runs = paragraph.xpath("./w:r", namespaces=NS)
    if not runs:
        run = etree.SubElement(paragraph, f"{{{W_NS}}}r")
    else:
        run = runs[0]
        for child in list(run):
            if child.tag != f"{{{W_NS}}}rPr":
                run.remove(child)
        for extra in runs[1:]:
            paragraph.remove(extra)
    for hyperlink in paragraph.xpath("./w:hyperlink", namespaces=NS):
        paragraph.remove(hyperlink)
    text_node = etree.SubElement(run, f"{{{W_NS}}}t")
    if text.startswith(" ") or text.endswith(" "):
        text_node.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    text_node.text = text


with ZipFile(SOURCE, "r") as source:
    document = etree.fromstring(source.read("word/document.xml"))
    paragraphs = document.xpath("//w:body/w:p", namespaces=NS)
    by_text = {}
    for paragraph in paragraphs:
        by_text.setdefault(paragraph_text(paragraph), []).append(paragraph)

    missing = []
    duplicate = []
    for old_text, new_text in REPLACEMENTS.items():
        matches = by_text.get(old_text, [])
        if not matches:
            missing.append(old_text)
            continue
        if len(matches) != 1:
            duplicate.append((old_text, len(matches)))
            continue
        set_paragraph_text(matches[0], new_text)

    if missing or duplicate:
        raise RuntimeError(f"Replacement audit failed: missing={missing}, duplicate={duplicate}")

    heading = by_text["2.3.1 Các quy trình, nghiệp vụ"][0]
    template = by_text["Quy trình đăng nhập và tải quyền gồm các bước sau:"][0]
    note = deepcopy(template)
    set_paragraph_text(note, PERMISSION_NOTE)
    heading.addnext(note)

    xml = etree.tostring(
        document, xml_declaration=True, encoding="UTF-8", standalone="yes"
    )
    with ZipFile(OUTPUT, "w", ZIP_DEFLATED) as target:
        for item in source.infolist():
            data = xml if item.filename == "word/document.xml" else source.read(item.filename)
            target.writestr(item, data)

copy2(OUTPUT, CHECKPOINT)
copy2(OUTPUT, WORKING)
print(f"replacements={len(REPLACEMENTS)}")
print(f"checkpoint={CHECKPOINT}")
