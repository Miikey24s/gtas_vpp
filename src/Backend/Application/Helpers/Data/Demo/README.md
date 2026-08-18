# Demo dataset

- `demo-catalog.tsv`: 547 mặt hàng, đơn vị tính, VAT và giá.
- `demo-departments.tsv`: 52 phòng ban cùng alias mã cũ.
- `demo-users.tsv`: hồ sơ giả dùng để bind đơn theo phòng ban. Account và membership hoạt động như dữ liệu TEST bình thường nên quản trị viên có thể tắt rồi bật lại quyền truy cập. Các hồ sơ này không có mật khẩu; muốn đăng nhập vẫn phải đi qua luồng thiết lập mật khẩu an toàn.
- `demo-orders.tsv`: 2.828 dòng đơn đã gộp trùng; không chứa ghi chú/tên người từ Excel.
- `demo-source-audit.json`: fingerprint và kết quả kiểm tra nguồn.

Không sửa TSV bằng tay. Hãy chạy `scripts/data/normalize-vpp-demo-source.py`, review audit rồi chạy `MigrateAndDemo`.
Thông tin provenance chỉ nằm trong audit/seed history; các trường người dùng nhìn thấy được seed như dữ liệu nghiệp vụ bình thường, không chứa nhãn `demo fixture`.
