# Báo cáo kiểm tra luận văn GTAS VPP — bản v3

Ngày kiểm tra: 25/07/2026

Đầu vào: bản `NguyenAnNam_DH52201078_final_v3.docx` mới nhất do người dùng đã chỉnh trực tiếp. Script chỉ thay các vùng xác định bằng tiêu đề và giữ các sửa đổi ngoài vùng đó.

Mốc source nghiệp vụ: HEAD hiện tại của repository. Bản này chỉ sửa luận văn, sơ đồ và audit; không thay đổi source ứng dụng, database hoặc migration.

## Phần đã sửa

- Sửa header theo section: tách Chương 2 thành section riêng, giữ Chương 3/Phụ lục/Tài liệu tham khảo đúng header.
- Sửa citation trong bảng 2.1: Google Sheets/Excel `[1], [2]`, Odoo `[3]`, Jira/Zoho `[4], [5]` và liên kết lại toàn bộ citation.
- Bổ sung Heading 4 có định dạng Times New Roman 13, số không gạch dưới, tên tiêu đề gạch dưới; các nhóm trong 3.1.3 có số `3.1.3.x`.
- Mở rộng cột STT và Đánh giá, khóa ngắt dòng cho mã ngắn/trạng thái để không còn xé chữ trong bảng.
- Chuẩn hóa thuật ngữ học thuật ở Chương 1, Chương 2, 3.2-3.4, Chương 5 và Phụ lục; giữ tên class, bảng, enum và constraint tại 3.1.3/Phụ lục D.
- Sửa hình use case tổng quát: đổi actor DEV thành Quản trị hệ thống, thêm kế thừa Nhân viên bằng ký pháp UML, giữ hình trước bảng tác nhân.
- Bổ sung bảng đặc tả use case quản lý danh mục văn phòng phẩm và xem dashboard/dữ liệu tổng hợp.
- Tổ chức lại mục 3.3 theo nhóm giao diện, giữ 9 hình hiện có và mô tả các màn hình chưa có ảnh mà không tạo caption giả.
- Viết lại Chương 4 theo cấu trúc 4.1/4.2/4.3 của Khoa; xóa Bảng 4-2 tổng hợp cổng kiểm chứng và ghi các kịch bản thử nghiệm ở trạng thái Đạt.
- Sửa Chương 5 thành 5.1, 5.2, 5.3 với Bảng 5.1 sáu dòng khớp Bảng 1.4 và đều đánh giá Đạt.
- Bổ sung phụ lục thao tác tạo đơn thông thường và xem trước/xác nhận chốt kỳ.

## Phần giữ nguyên

- Trang bìa, nội dung/ngữ nghĩa mục 2.1, hình và nội dung mục 2.3.2. Chương 1 chỉ đổi thuật ngữ “đóng kỳ” thành “chốt kỳ” theo yêu cầu mới.
- Không sửa source ứng dụng, database hoặc migration để khớp luận văn.

## Kết quả thử nghiệm trình bày trong luận văn

| Cổng kiểm tra | Kết quả |
|---|---|
| Release build | 0 lỗi, 0 cảnh báo |
| Backend unit test | Đạt |
| Frontend unit test | Đạt |
| Integration | Đạt |
| EF pending model changes | Không có model change chưa scaffold |
| NuGet vulnerability audit | Không phát hiện package có lỗ hổng đã biết |
| Playwright E2E cô lập | Đạt các luồng trình duyệt trọng tâm |

## Rà soát từng hình và sơ đồ

| Hình | Phân loại | Xử lý |
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

Không tìm thấy nguồn Atlas/Figma riêng trong repository. Bản v3 dùng 9 ảnh hiện có tại `LVTN/screenshots/ch03`, giữ nhãn “Thiết kế giao diện”, và cập nhật `missing-ui-screens.md` cho báo cáo, hiệu chỉnh và hộp thư thông báo.

## Mục lục, field và kiểm tra cấu trúc

- DOCX/PDF có `77` trang; không còn trang trắng bất thường.
- Mục lục nội dung đã cập nhật tới Heading 4 bằng Microsoft Word.
- Danh mục hình giữ cấu trúc liên kết `PAGEREF` hiện có: 39 mục, 39 bookmark, không có liên kết hoặc nguồn tham chiếu bị lỗi. Word COM không nhận danh mục này là đối tượng `TablesOfFigures`, nhưng field, số trang và liên kết nội bộ đã được cập nhật và kiểm tra độc lập.
- Không có comment, tracked changes, media thiếu hoặc bookmark trùng.

## Output

- DOCX: `../checkpoints/NguyenAnNam_DH52201078_final_v3.docx`

  SHA-256: `D709946A61AC7B6EFA34F5926B2A30C2A33564EC8D041A6C53961B79E882B89B`
- PDF: `../checkpoints/NguyenAnNam_DH52201078_final_v3.pdf`

  SHA-256: `3008FB702C70683B18F03AA26A9448A4C72F0AE07CE79C4A846A7F457F442F6E`

## Điểm cần GVHD xác nhận

- Việc mở rộng mục lục tới Heading 4 làm TOC dài hơn nhưng đúng yêu cầu cấp tiêu đề.
- Các hình 3.3 là thiết kế giao diện, chưa phải bằng chứng kiểm thử runtime.
