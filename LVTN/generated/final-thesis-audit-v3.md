# Báo cáo kiểm tra luận văn GTAS VPP — bản v3

Ngày kiểm tra: 25/07/2026

Đầu vào: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_v2.docx`

Mốc source nghiệp vụ: `5f24fda286799d4b9869c0d381b8042fbc2c2120`. HEAD hiện tại chỉ thay đổi luận văn/tooling so với mốc này trong phạm vi app, nên Chương 4 dùng lại log kiểm thử hiện có.

## Phần đã sửa

- Sửa header theo section: tách Chương 2 thành section riêng, giữ Chương 3/Phụ lục/Tài liệu tham khảo đúng header.
- Sửa citation trong bảng 2.1: Google Sheets/Excel `[1], [2]`, Odoo `[3]`, Jira/Zoho `[4], [5]` và liên kết lại toàn bộ citation.
- Bổ sung Heading 4 có định dạng Times New Roman 13, số không gạch dưới, tên tiêu đề gạch dưới; các nhóm trong 3.1.3 có số `3.1.3.x`.
- Bổ sung bảng đặc tả use case quản lý danh mục văn phòng phẩm và xem dashboard/dữ liệu tổng hợp.
- Tổ chức lại mục 3.3 theo nhóm giao diện, giữ 9 hình hiện có và mô tả các màn hình chưa có ảnh mà không tạo caption giả.
- Viết lại Chương 4 theo cấu trúc 4.1/4.2/4.3 của Khoa; xóa mục tồn đọng kỹ thuật khỏi Chương 4.
- Sửa Chương 5 thành 5.1, 5.2, 5.3 với Bảng 5.1 sáu dòng khớp Bảng 1.4 và thống nhất với Chương 4.
- Bổ sung phụ lục thao tác tạo đơn thông thường và xem trước/xác nhận chốt kỳ.

## Phần giữ nguyên

- Trang bìa, Chương 1, nội dung/ngữ nghĩa mục 2.1, hình và nội dung mục 2.3.2.
- Không sửa source ứng dụng, database hoặc migration để khớp luận văn.

## Kết quả build/test sử dụng

| Cổng kiểm tra | Kết quả |
|---|---|
| Release build | 0 lỗi, 0 cảnh báo |
| Backend unit test | 412/414 đạt, 2 chưa đạt |
| Frontend unit test | 150/151 đạt, 1 chưa đạt |
| Integration | 14 đạt, 6 chưa chạy do yêu cầu LocalDB opt-in |
| EF pending model changes | Không có model change chưa scaffold |
| NuGet vulnerability audit | Không phát hiện package có lỗ hổng đã biết |
| Playwright E2E cô lập | 12/27 đạt, 15 không đạt, khoảng 21,98 phút |

## Hình Atlas/giao diện

Không tìm thấy nguồn Atlas/Figma riêng trong repository. Bản v3 dùng 9 ảnh hiện có tại `LVTN/screenshots/ch03`, giữ nhãn “Thiết kế giao diện”, và cập nhật `missing-ui-screens.md` cho báo cáo, hiệu chỉnh và hộp thư thông báo.

## Kiểm tra DOCX/PDF

- Word COM cập nhật field, mục lục và xuất PDF thành công; PDF cuối có 78 trang.
- Mục lục nội dung dùng field `TOC \o "1-4" \h \z \u`, có 49 dòng Heading 4; dòng `PHỤ LỤC` có tab và dấu chấm dẫn theo form.
- Danh mục hình hiển thị 39 dòng hình trong front matter; XML có 153 `PAGEREF` và 205 hyperlink. Word object model trả `TOF_COUNT=0` vì danh mục hình đang được duy trì bằng field/link trong tài liệu, nhưng render PDF và XML đều cho thấy danh mục hình vẫn hiển thị đúng.
- Header đã kiểm tra trên PDF: Chương 2 không hiện header Chương 3; Phụ lục D giữ header `PHỤ LỤC` cho tới hết phụ lục; Tài liệu tham khảo bắt đầu ở trang mới và trang tiếp theo có header `TÀI LIỆU THAM KHẢO`.
- Heading 4 được kiểm tra bằng XML: số thứ tự không gạch dưới, tên tiêu đề gạch dưới; các nhóm 3.1.3 có số `3.1.3.1` đến `3.1.3.5`.
- Render toàn bộ PDF thành 78 ảnh tại `LVTN/generated/render-final-v3`; đã kiểm tra contact sheet, không thấy trang trắng bất thường, caption lạc trang, bảng tràn lề hoặc ảnh quá nhỏ ở các trang bị tác động.
- So sánh raster bìa v3 với v2 cho kết quả `0` pixel khác biệt; DOCX không có comment hoặc tracked changes.

## Output

- DOCX: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_v3.docx`

  SHA-256: `CA2D891CF807A0E909F29E8C08F93D2AAC92B0D2AD696B81D1BB7DE84E961E81`
- PDF: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_v3.pdf`

  SHA-256: `C5D93F230CD50C9311823DFA7E67D4D9CFD65DD4982844A6B116682E30FA23B0`

## Điểm cần GVHD xác nhận

- Việc mở rộng mục lục tới Heading 4 làm TOC dài hơn nhưng đúng yêu cầu cấp tiêu đề.
- Các hình 3.3 là thiết kế giao diện, chưa phải bằng chứng kiểm thử runtime.
- Chương 4 giữ trạng thái test chưa đạt thay vì đổi sang đạt theo sự tồn tại của code.
