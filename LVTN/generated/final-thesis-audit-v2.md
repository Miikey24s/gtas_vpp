# Báo cáo kiểm tra luận văn GTAS VPP — bản v2

Ngày kiểm tra: 25/07/2026

Đầu vào: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_candidate.docx`

Mốc source nghiệp vụ: `5f24fda286799d4b9869c0d381b8042fbc2c2120`

HEAD khi dựng tài liệu: `9ef63785870aa15c5edb28973f72d032233c92bd`

Phần source ứng dụng không thay đổi so với mốc evidence; vì vậy số liệu Chương 4 và Chương 5 được tái sử dụng từ log đã có, không chạy lại test chỉ để tạo số liệu mới.

## 1. Phần đã sửa

- Đánh lại 18 tài liệu tham khảo theo thứ tự xuất hiện; sửa citation trong nội dung và bảng công nghệ theo đúng ngữ nghĩa.
- Tạo 37 liên kết citation nội bộ tới bookmark tài liệu tham khảo và 18 liên kết URL ngoài có thể bấm.
- Mục 2.2 bỏ tên schema cũ `VPP03_Log`; mục 2.3.1.5 dùng đúng bốn trạng thái `Open`, `SubmissionClosed`, `Pricing`, `Settled` kèm thuật ngữ nghiệp vụ tiếng Việt.
- Mục 2.3.3 dùng ký pháp UML generalization tam giác rỗng từ Quản lý tới Nhân viên; DEV được mô tả là vai trò quản trị kỹ thuật. Mã `EMPLOYEE`, `MANAGER`, `DEV` không bị ngắt dòng.
- Mục 3.1.3 được rút gọn thành bốn nhóm bảng nghiệp vụ; thông tin chi tiết về index, check constraint, rowversion, delete behavior, Identity, bản dịch, view và keyless entity được giữ trong phần chính hoặc Phụ lục D. Tên bảng/identifier quan trọng không còn bị xé chữ trong cột hẹp.
- Mục 3.2.1 bổ sung chín bảng đặc tả use case: đăng nhập; tạo đơn thường; sửa/hủy; tạo đơn bổ sung; duyệt/từ chối; bảng giá; xem trước/chốt kỳ; hiệu chỉnh; người dùng/phân quyền.
- Chương 4 sửa thời lượng thành “khoảng 21,98 phút”, ghi rõ integration `14 đạt, 6 chưa chạy (LocalDB opt-in)` và giữ nguyên trạng thái chưa đạt của các cổng còn lỗi.
- Cập nhật TOC, mục lục hình, PAGEREF và số trang bằng Microsoft Word COM sau khi chốt nội dung.

## 2. Phần được bảo toàn

- Trang bìa không đổi; so sánh raster trang 1 với PDF đầu vào cho kết quả `0` pixel khác biệt.
- Chương 1 giữ nguyên; các trang 8–11 của đầu vào và v2 giống nhau ở mức raster.
- Mục 2.1 giữ nguyên nội dung/ngữ nghĩa, chỉ cập nhật số citation và đích liên kết.
- Hình và nội dung mục 2.3.2 được giữ nguyên; hình giao diện hiện có ở mục 3.3 tiếp tục dùng caption “Thiết kế giao diện”.
- Không ghi đè bản đầu vào; không sửa source, database hoặc migration để khớp luận văn.

## 3. Mapping tài liệu tham khảo đã kiểm tra

| Số | Nội dung được trích dẫn | Nguồn chính thức |
|---|---|---|
| [1] | Google Sheets chia sẻ và cộng tác | Google Workspace Learning Center |
| [2] | Excel for the web chia sẻ và cộng tác | Microsoft Support |
| [3] | Quản lý mua hàng | Odoo Purchase Documentation |
| [4] | Tiếp nhận yêu cầu dịch vụ | Atlassian Jira Service Management |
| [5] | Workflow | Zoho Creator Help |
| [6] | Nền tảng web/API và authorization | Microsoft ASP.NET Core |
| [7] | Tài khoản và xác thực | Microsoft ASP.NET Core Identity |
| [8] | Frontend Interactive Server | Microsoft Blazor |
| [9] | ORM, model và migration | Microsoft EF Core |
| [10] | Hệ quản trị dữ liệu | Microsoft SQL Server |
| [11] | Component giao diện | Radzen Blazor Components |
| [12] | Ánh xạ entity/DTO | Mapster |
| [13] | Structured logging | Serilog |
| [14] | Đóng gói và điều phối container | Docker Compose |
| [15] | Reverse proxy | NGINX proxy module |
| [16] | Điều phối local | .NET Aspire |
| [17] | Unit/integration test | xUnit.net v3 |
| [18] | E2E trình duyệt | Playwright .NET |

Mỗi nguồn có ít nhất một citation đúng ngữ nghĩa; không còn nguồn OpenAI/AI hoặc nguồn không được sử dụng.

## 4. Kết quả build và kiểm thử được sử dụng

| Cổng kiểm tra | Kết quả |
|---|---|
| Release build | Đạt, 0 lỗi, 0 cảnh báo |
| Backend unit test | 412/414 đạt, 2 không đạt |
| Frontend unit test | 150/151 đạt, 1 không đạt |
| Integration | 14 đạt, 6 chưa chạy do yêu cầu LocalDB opt-in |
| EF pending model changes | Không có model change chưa scaffold |
| NuGet vulnerability audit | Không phát hiện package có lỗ hổng đã biết |
| Playwright E2E cô lập | 12/27 đạt, 15 không đạt, khoảng 21,98 phút |

Chương 4, Bảng 5.1 và báo cáo này dùng cùng bộ số liệu. Các lỗi còn lại vẫn được kết luận “Chưa đạt”. Log gốc nằm tại `LVTN/generated/test-logs/2026-07-25/`.

## 5. Kiểm tra DOCX/PDF

- DOCX ZIP hợp lệ; 7 section; khổ A4 và lề đúng mẫu; không có page border.
- PDF Word có 73 trang; toàn bộ 73 trang đã render và kiểm tra bằng contact sheet cùng các trang chi tiết.
- 68 mục TOC, 39 mục danh mục hình, 39 caption/bookmark hình và 39 PAGEREF hình hợp lệ.
- 162 hyperlink: 144 liên kết nội bộ và 18 liên kết ngoài; không có anchor hoặc relationship bị gãy.
- 39 hình luận văn là In Line with Text. Đối tượng nổi duy nhất là shape trang bìa được bảo toàn từ đầu vào, không phải hình nội dung.
- Không có comment, tracked changes, field lỗi, trang trắng bất thường, bảng tràn lề, caption lạc trang hoặc mã test/persona bị tách.
- Audit định dạng còn ghi nhận `Hình 2-2` gồm ba run XML; đây là cấu trúc sẵn có trong mục 2.3.2 bị khóa, hiển thị và caption vẫn đúng nên không sửa.
- Renderer DOCX dựa trên LibreOffice không chuyển đổi được trong môi trường Windows này; PDF dùng để QA là bản xuất trực tiếp từ Microsoft Word, đúng yêu cầu bàn giao.

## 6. Hình giao diện còn thiếu

Ma trận cập nhật nằm tại `LVTN/generated/missing-ui-screens.md`. Ưu tiên chụp lại: tạo đơn; vận hành/chốt kỳ; phân quyền; báo cáo; hiệu chỉnh kết quả chốt kỳ; hộp thư thông báo. Không có caption hoặc số hình giả cho màn hình chưa có ảnh.

## 7. Output và checksum

- DOCX: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_v2.docx`

  SHA-256: `89A7008270E7A2D2D795A22F75552C10B0851C37517E8A02EC6508DA733F1D0C`
- PDF: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_v2.pdf`

  SHA-256: `5AACA7F22C0351304CABEFD0D40BBA5D29EEDF05B014F2142A5BD7400AEB83F6`

## 8. Điểm cần GVHD xác nhận

- Cách trình bày kết quả “Đạt về chức năng” nhưng “Chưa đạt cổng kiểm thử” tại Bảng 5.1.
- Việc giữ các hình giao diện hiện có dưới nhãn “Thiết kế giao diện” cho tới khi frontend và E2E ổn định.
- Mức chi tiết của data dictionary chuyển xuống Phụ lục D thay vì giữ toàn bộ trong mục 3.1.3.
