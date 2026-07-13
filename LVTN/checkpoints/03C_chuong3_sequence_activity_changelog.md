# Changelog checkpoint 03C – Sequence và activity diagram

## Phạm vi

- Hoàn thiện mục 3.2.2 và 3.2.3 của Chương 3.
- Thay 11 vị trí hình trống bằng 6 sequence diagram và 5 activity diagram.
- Không sửa source code, API hoặc cơ sở dữ liệu của hệ thống.

## Nội dung đã thực hiện

- Bổ sung 6 sequence diagram: đăng nhập và tải quyền; tạo đơn thường; tạo đơn bổ sung; duyệt/từ chối đơn bổ sung; đóng kỳ và chụp giá; quản lý bảng giá.
- Bổ sung 5 activity diagram: tạo đơn thường; xử lý đơn bổ sung; đóng kỳ; quản lý bảng giá; phân quyền.
- Các sequence diagram dùng đúng các lớp và thành phần có trong source như `LoginPage`, `PermissionState`, `VPPRequestController`, `VPPRequestService`, `PeriodSettlementService`, `VPPPriceService` và `VPPContext`.
- Thể hiện rõ validation, transaction, commit/rollback, state machine và log nghiệp vụ thay vì chỉ mô tả luồng thành công.
- Bổ sung một đoạn giải thích ngắn trước mỗi hình để nêu điểm kiểm tra và phạm vi của sơ đồ.
- Giữ nội dung quản lý bảng giá trung thực với source: không mô tả bảng giá bị khóa sau settlement khi source chưa có quy tắc này.
- Sơ đồ dùng nền trắng, nét đen, chữ Arial và không phụ thuộc màu sắc; Word ưu tiên SVG và giữ PNG dự phòng.

## Kiểm tra

- PlantUML 1.2026.6 trên Java 21.0.11: 11/11 file `.puml` qua kiểm tra cú pháp và render SVG/PNG thành công.
- DOCX render thành 65 trang vật lý; đã kiểm tra toàn bộ contact sheet và kiểm tra trực tiếp các trang thay đổi 47–57 ở kích thước gốc.
- 28 hình inline và 1 đối tượng anchor có sẵn; 28 SVG được nhúng, không có image relationship mồ côi.
- 34 caption hình xuất hiện đúng hai lần; Hình 3-15 đến Hình 3-25 khớp nội dung mới.
- Không còn placeholder “Sơ đồ tuần tự” hoặc “Sơ đồ hoạt động” trong phần nội dung.
- Không có Track Changes hoặc comment.
- Checkpoint và working copy có cùng SHA-256: `55851599e28e0cd1650e4f15aa222cb5d58813402cdeef3a7e0496432e34cc73`.

## Tệp bàn giao

- `LVTN/checkpoints/03C_chuong3_sequence_activity.docx`
- `LVTN/NguyenAnNam_DH52201078_working.docx`

Mục lục và danh mục hình sẽ được cập nhật số trang đồng bộ ở đợt 99 sau khi toàn bộ nội dung đã ổn định.

