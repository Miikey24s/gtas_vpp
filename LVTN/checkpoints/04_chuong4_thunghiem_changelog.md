# Changelog checkpoint 04 – Chương 4: Thử nghiệm

- Nguồn: `03D_chuong3_giaodien_baobieu.docx`.
- Kết quả: `04_chuong4_thunghiem.docx` (70 trang vật lý).
- Bản làm việc `NguyenAnNam_DH52201078_working.docx` đã được đồng bộ và có cùng SHA-256: `fc7b48fca71146d1dbfbc919d20ac89070faeafdc49b1d2573d7d692b1a1d7a3`.

## Nội dung đã hoàn thiện

- Viết lại Chương 4 theo ba phần: kịch bản thử nghiệm, kết quả thử nghiệm, xử lý ngoại lệ và tồn đọng kỹ thuật.
- Bổ sung 13 test case đại diện, mỗi test case có mã, tiền điều kiện, bước thực hiện, kết quả mong đợi, kết quả thực tế và trạng thái.
- Tạo 5 bảng mới:
  - Bảng 4-1: phạm vi và bằng chứng kiểm thử.
  - Bảng 4-2: xác thực, kỳ và đơn yêu cầu.
  - Bảng 4-3: trạng thái, bảng giá và đóng kỳ.
  - Bảng 4-4: frontend và audit giao diện.
  - Bảng 4-5: tổng hợp kết quả kiểm thử và build.
- Ghi nhận kết quả chạy ngày 13/07/2026 trên .NET SDK 10.0.301:
  - Backend: 132/132 test đạt, 0 lỗi, 0 bỏ qua, khoảng 4 giây.
  - Frontend helper: 26/26 test đạt, 0 lỗi, 0 bỏ qua, khoảng 585 ms.
  - Solution chính build thành công, 0 warning và 0 error.
  - Project UI test build thành công, 0 warning và 0 error; không chạy E2E vì cần môi trường FE/BE và dữ liệu test đã cấu hình.
- Ghi rõ audit UI 54/54 là snapshot ngày 28/05/2026 gồm 18 route × 3 viewport, không phải kết quả chạy lại hiện tại.
- Ghi hai cảnh báo restore NU1903 của `Microsoft.OpenApi 2.4.1` và `SQLitePCLRaw.lib.e_sqlite3 2.1.11` là tồn đọng kỹ thuật.
- Tham chiếu project AI bị thiếu không còn tái hiện trong build hiện tại, nên không ghi như lỗi còn tồn tại.
- Cập nhật mục lục và hyperlink cho các mục 4.1.1, 4.1.2, 4.3.1 và 4.3.2.

## Trang bị tác động trong bản render cuối

- Trang vật lý 4: mục lục Chương 4 và số trang các phần sau.
- Trang vật lý 63–66: toàn bộ nội dung Chương 4.

## Kiểm tra

- Render bằng Microsoft Word 16 và kiểm tra trực quan đủ 70/70 trang; không có bảng tràn lề, header bị mất, hàng bị cắt, trang trắng hoặc khoảng trắng bất thường mới.
- Năm bảng Chương 4 có chiều rộng 9000 DXA, `tblInd` 120 DXA, tổng `tblGrid` và tổng `tcW` từng hàng đều bằng 9000 DXA.
- Cấu trúc tài liệu giữ nguyên 8 section A4 dọc, lề trái 3 cm và các lề còn lại 2 cm.
- 37 ảnh inline và 1 ảnh anchor; 65 media hợp lệ gồm 37 PNG và 28 SVG, không có media mồ côi.
- 37 mã caption hình vẫn xuất hiện đúng hai lần; 5 caption bảng Chương 4 và TC-01 đến TC-13 đủ, đúng thứ tự.
- Không có Track Changes, comment, media thiếu, lỗi ZIP hoặc `Error! Bookmark not defined.`.
- LibreOffice cài trên máy vẫn hỏng `bootstrap.ini`; dùng Word để xuất PDF và Poppler để kiểm tra PNG.
