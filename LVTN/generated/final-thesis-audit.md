# Báo cáo kiểm tra luận văn GTAS VPP

Ngày kiểm tra: 25/07/2026

Mốc source: `5f24fda286799d4b9869c0d381b8042fbc2c2120`

Môi trường: Windows, .NET 10, cấu hình Release.

## 1. Phạm vi đã cập nhật

- Viết lại mục 2.3.1 theo Identity, đơn thông thường, đơn bổ sung, kỳ, danh mục, bảng giá, chốt kỳ, hiệu chỉnh, báo cáo và thông báo hiện hành.
- Đồng bộ mục 2.3.3 theo ba persona `EMPLOYEE`, `MANAGER`, `DEV`; không mặc định DEV trực tiếp thực hiện mọi nghiệp vụ.
- Dựng lại mục 3.1 từ Entity, DbContext, Fluent API, migration và model snapshot; loại DTO khỏi ERD và chỉ nhắc ngắn nhóm bảng bản dịch.
- Đồng bộ mục 3.2, 3.3, 3.4; hình giao diện được ghi là “Thiết kế giao diện”, không dùng làm bằng chứng kiểm thử.
- Viết lại Chương 4, Chương 5, Phụ lục và tài liệu tham khảo theo kết quả kiểm chứng tại HEAD.
- Bổ sung nguồn Serilog vào nội dung để toàn bộ 15 tài liệu tham khảo đều có vị trí trích dẫn.

## 2. Phần được bảo toàn

- Không ghi đè file working hoặc bản gốc.
- Trang bìa, toàn bộ Chương 1, mục 2.1, 2.2 và 2.3.2 giữ nguyên nội dung/ngữ nghĩa; script kiểm tra digest trước và sau khi thay nội dung.
- Tài liệu còn đúng 8 section; header, footer, bookmark, field code và section/page break được bảo toàn.
- Không có comment hoặc tracked changes.

## 3. Sơ đồ và giao diện

- Mục 3.1 sử dụng sáu hình dữ liệu: ý niệm; tổ chức và phân quyền; danh mục, nhà cung cấp và bảng giá; kỳ, đơn, revision và nhật ký; settlement, chi phí và phân bổ; Identity/view/keyless/bảng kỹ thuật.
- Mục 3.2 sử dụng các sơ đồ use case, tuần tự và hoạt động đã đối chiếu với source hiện hành.
- Có 39 hình được chèn In Line with Text, 39 bookmark hình và 39 liên kết trong danh mục hình.
- Ma trận hình còn thiếu hoặc cần chụp lại nằm tại `LVTN/generated/missing-ui-screens.md`.

## 4. Kết quả build và kiểm thử

| Cổng kiểm tra | Kết quả |
|---|---|
| Release build | Đạt, 0 lỗi, 0 cảnh báo |
| Backend unit test | 412/414 đạt, 2 không đạt |
| Frontend unit test | 150/151 đạt, 1 không đạt do thiếu 17 khóa localization |
| Integration mặc định | 14 đạt, 6 bỏ qua theo opt-in LocalDB |
| EF pending model changes | Không có model change chưa scaffold |
| NuGet vulnerability audit | Không phát hiện package có lỗ hổng đã biết |
| Playwright E2E cô lập | 12/27 đạt, 15 không đạt, tổng thời gian 21,9828 phút |

Nhóm E2E chưa đạt gồm luồng tạo/bổ sung đơn, đăng ký tài khoản, hộp thông báo, phân quyền, catalog, bảng dữ liệu dài, nhận diện thương hiệu, bố cục sticky/header và accessibility `document-title`. Luận văn ghi đúng trạng thái “Chưa đạt”, không dùng số liệu cũ hoặc Atlas thay cho runtime evidence.

Log lệnh nằm tại `LVTN/generated/test-logs/2026-07-25/`.

## 5. Kiểm tra Word và PDF

- DOCX mở và kiểm tra ZIP bình thường.
- 63 liên kết mục lục, 39 liên kết danh mục hình, 132 liên kết nội bộ; không có anchor bị thiếu.
- Không có lỗi `Bookmark not defined`, `Reference source not found` hoặc mục lục hình rỗng.
- Audit định dạng chỉ còn cảnh báo kỹ thuật ở caption khóa `Hình 2-2: Sơ đồ chức năng` có ba run; không sửa vì mục 2.3.2 thuộc phạm vi khóa và kết quả render không lỗi.
- PDF cuối có 94 trang. Toàn bộ 94 trang đã render; không thấy trang trắng, hình/bảng tràn lề, chữ chồng nhau hoặc caption lạc trang bất hợp lý.

## 6. Output

- DOCX: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_candidate.docx`

  SHA-256: `4F84E71B6F0A350BA169FB8B720C7158473D6413ABA645A34104676B63F10441`
- PDF: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_candidate.pdf`

  SHA-256: `50C0DF7649B0C3977BEBC95A96B091261182AD472E157330BDE858B7843380F0`

## 7. Điểm còn cần xử lý

- Cần sửa ba unit test và 15 E2E trước khi kết luận sản phẩm sẵn sàng bàn giao.
- Cần chụp lại các màn hình được đánh dấu trong `missing-ui-screens.md` sau khi giao diện ổn định.
- Trạng thái “Đạt/Chưa đạt” và cách trình bày kết quả kiểm thử vẫn nên được giảng viên hướng dẫn xác nhận trước bản nộp chính thức.
