# Changelog checkpoint 02B – Chương 2: nghiệp vụ

## Phạm vi

- Chỉ cập nhật mục **2.3.1 Các quy trình, nghiệp vụ** trong Chương 2.
- Đối chiếu 15 quy trình với controller, service, state machine, permission và mô hình dữ liệu trong source GTAS VPP.
- Không sửa source code, API hoặc cơ sở dữ liệu.

## Nội dung đã hiệu chỉnh

- Hiệu chỉnh 35 đoạn mô tả để phản ánh đúng luồng thực tế của hệ thống.
- Làm rõ quy tắc kỳ yêu cầu: trước ngày 5 dùng kỳ trước, từ ngày 5 dùng kỳ hiện tại.
- Làm rõ đơn thường, đơn bổ sung, giới hạn tối đa 3 đơn bổ sung, trạng thái `Submitted`, `Pending`, `Approved`, `Rejected`, `Cancelled` và các chuyển trạng thái hợp lệ.
- Bổ sung các ràng buộc tạo/sửa đơn: một đơn thường mỗi người dùng trong một kỳ, số lượng lớn hơn 0, không trùng vật tư, vật tư/danh mục còn hiệu lực và kiểm tra đóng kỳ.
- Làm rõ hành vi giá mặc định: chi tiết đơn thường có thể lưu đơn giá 0 khi chưa có ánh xạ giá; giá được chụp lại khi đóng kỳ.
- Mô tả chính xác thao tác đóng kỳ: xử lý các đơn `Submitted`/`Approved`, cập nhật giá và dấu vết `SettledAt`; hiện chưa có bản ghi kỳ độc lập nên kỳ không có đơn phù hợp sẽ không tạo dấu vết đóng kỳ.
- Phân biệt vô hiệu hóa bằng `PATCH IsDeleted` với endpoint `DELETE` xóa vật lý ở các danh mục dùng chung.
- Làm rõ phạm vi dữ liệu của màn hình tổng hợp, lịch sử đơn và các endpoint chi tiết.
- Ghi nhận trung thực các khoảng trống hiện tại: một số API mới kiểm tra đăng nhập (`[Authorize]`) nhưng chưa có policy nghiệp vụ riêng; JWT đăng nhập chưa đưa `MemberCompanyCode` vào claim; DTO chi tiết chưa trả timeline log.
- Giữ nguyên nội dung Chương 2.3.2–2.3.3 để xử lý ở checkpoint 02C.

## Bố cục và liên kết

- Áp dụng `Keep with next` cho 15 tiêu đề quy trình và 15 dòng “Yêu cầu từng bước” để tránh tiêu đề bị treo cuối trang.
- Bản render có 48 trang; Chương 3 bắt đầu ở trang vật lý 29.
- Giữ nguyên 96 nút hyperlink và 14 liên kết ngoài từ checkpoint 02A. Tài liệu nội bộ `[10]` không gắn URL vì đó là source workspace, không phải nguồn web.
- Không có Track Changes hoặc comments trong bản bàn giao.

## Kiểm tra

- DOCX mở và kiểm tra ZIP package thành công: 8 section, 672 paragraph, 18 table, 6 inline shape.
- Khổ A4; lề trái 3 cm, các lề còn lại 2 cm.
- Render và kiểm tra trực quan đủ 48 trang; không phát hiện chữ/hình bị cắt, trang trắng bất thường hoặc tiêu đề quy trình đứng một mình cuối trang.
- PlantUML `architecture-overview.puml` qua kiểm tra cú pháp local.
- Checkpoint và working copy có cùng SHA-256: `4d97a7dd6867dfb694a3ba9b81885dd5d09cfcf7a7893e8c2245d13bbaa734ff`.

## Ghi nhận cho checkpoint 02C

- Bảng actor ở phần use case tổng quát còn xuống dòng xấu tại tiêu đề cột “Tác nhân/nhóm xử lý”.
- Các sơ đồ chức năng và use case tổng quát hiện có sẽ được vẽ lại bằng PlantUML/SVG và đồng bộ actor trong checkpoint 02C.
