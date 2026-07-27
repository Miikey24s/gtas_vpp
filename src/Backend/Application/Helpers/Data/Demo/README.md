# Demo dataset

- `demo-catalog.tsv`: 547 mặt hàng, đơn vị tính, VAT và giá.
- `demo-departments.tsv`: 52 phòng ban cùng alias mã cũ.
- `demo-users.tsv`: hồ sơ giả, bị khóa đăng nhập, dùng để bind đơn theo phòng ban.
- `demo-orders.tsv`: 2.828 dòng đơn đã gộp trùng; không chứa ghi chú/tên người từ Excel.
- `demo-source-audit.json`: fingerprint và kết quả kiểm tra nguồn.

Không sửa TSV bằng tay. Hãy chạy `scripts/data/normalize-vpp-demo-source.py`, review audit rồi chạy `MigrateAndDemo`.
