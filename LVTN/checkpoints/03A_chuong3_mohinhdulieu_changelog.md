# Changelog checkpoint 03A – Mô hình dữ liệu Chương 3

## Phạm vi

- Chỉ hoàn thiện mục 3.1 của Chương 3 và các phần liên quan trực tiếp: danh mục hình, caption và thứ tự số hình trong Chương 3.
- Không thay đổi source code, API hoặc cơ sở dữ liệu.
- Các sơ đồ use case, sequence, activity và ảnh giao diện ở mục 3.2–3.4 vẫn để dành cho các đợt 03B–03D.

## Nội dung đã thực hiện

- Tách mục 3.1 thành ba mức:
  - 3.1.1 Mô hình dữ liệu ý niệm.
  - 3.1.2 Mô hình dữ liệu luận lý.
  - 3.1.3 Mô hình dữ liệu vật lý.
- Tạo mới bốn sơ đồ đen–trắng, phù hợp in A4:
  - `data-conceptual.puml/.svg/.png`.
  - `erd-authorization.puml/.svg/.png`.
  - `erd-catalog-pricing.puml/.svg/.png`.
  - `erd-request-log.puml/.svg/.png`.
- Chia ERD luận lý thành ba nhóm để chữ đủ lớn: tổ chức–phân quyền, danh mục–bảng giá và đơn yêu cầu–nhật ký.
- Bổ sung ba bảng mô tả vật lý, bao quát 17 bảng trong migration và hai view keyless được ánh xạ ở runtime.
- Làm rõ các trường chỉ là tham chiếu nghiệp vụ nhưng không có khóa ngoại vật lý, gồm `UserId`, mã công ty/phòng ban, `ParentGroupId` và `ParentId`.
- Bổ sung các ràng buộc quan trọng: duy nhất bảng giá mặc định, nhà cung cấp mặc định theo mặt hàng/bảng giá, một đơn thường theo người dùng/kỳ và mã `VPPCode` duy nhất.
- Ghi rõ trạng thái đơn thực tế: Submitted = 1, Cancelled = 4, Pending = 6, Approved = 7, Rejected = 8.
- Đánh lại số hình Chương 3 thành Hình 3-1 đến Hình 3-34; cập nhật danh mục hình tương ứng.
- Loại bỏ hình ERD cũ dạng ảnh raster khỏi gói DOCX.

## Kiểm tra

- File checkpoint và working copy có cùng SHA-256: `530d6c4c36bb8abccfa71625fafeddf67c6e0cde0cb0c359d672f35299295005`.
- DOCX mở và giải nén hợp lệ; 53 trang sau khi render.
- 8 section, khổ A4; lề trái 3 cm, các lề còn lại 2 cm.
- 9 hình inline và 1 hình anchor có sẵn ở phần đầu tài liệu; không phát sinh hình trôi trong nội dung Chương 3.
- 7 SVG được liên kết và đóng gói trong DOCX; có PNG fallback.
- Không có Track Changes, comment hoặc media ERD cũ còn sót.
- Hình 3-1 đến Hình 3-34 không thiếu số, không trùng số; mỗi số xuất hiện một lần trong danh mục và một lần ở caption.
- Đã kiểm tra trực quan toàn bộ 53 trang; không có chữ, hình hoặc bảng bị cắt/tràn trang.

## Tệp bàn giao

- `03A_chuong3_mohinhdulieu.docx`
- `03A_chuong3_mohinhdulieu_changelog.md`

