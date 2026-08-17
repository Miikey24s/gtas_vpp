# MULTI-SUPPLIER-SETTLEMENT-001 — Chốt kỳ tối đa hai nhà cung cấp

- Status: VERIFIED — OWNER REVIEW
- Date: 2026-08-18
- Authority: [ADR-015](../decisions/ADR-015-controlled-two-supplier-settlement.md)

## 0. Bản một ánh nhìn

| Mục | Quyết định |
|---|---|
| Mặc định | Quản lý vẫn chọn thủ công một nhà cung cấp và một bảng giá |
| Đề xuất | Hệ thống chỉ gợi ý tối đa hai nhà cung cấp, không tự áp dụng |
| Công thức | So sánh giá từng mặt hàng sau VAT; chưa tính vận chuyển/hợp đồng |
| Điều kiện | Đủ 100% mặt hàng và tiết kiệm ít nhất 2%; hoặc hai NCC mới ghép đủ hàng |
| Phân bổ | Một mặt hàng và toàn bộ số lượng chỉ thuộc một NCC |
| Snapshot | Giữ NCC chính; dòng dùng NCC thứ hai lưu exception có khóa đúng bảng giá |
| UI | Notice theo design system, `Dùng đề xuất`, xem lại trong dialog chốt kỳ |
| Export | Excel/PDF chỉ rõ NCC của từng mặt hàng |
| Database | Không thêm schema; dùng `SettlementItem.SupplierId/PriceListId` hiện có |

## 1. Luồng thực thi

1. Backend tải các bảng giá đang công bố và còn hiệu lực.
2. Thuật toán thử các cặp bảng giá thuộc hai NCC khác nhau, tối đa hai NCC.
3. Với mỗi mặt hàng, chọn dòng có thành tiền sau VAT thấp hơn; không chia số lượng.
4. Loại phương án thiếu hàng, có dòng mơ hồ hoặc có phí/chiết khấu/vận chuyển chưa được mô hình hóa.
5. Nếu đạt rule, trả về NCC chính, NCC phụ, bảng giá khóa theo từng dòng, tổng dự kiến và mức giảm.
6. Quản lý bấm `Dùng đề xuất`; hệ thống preview lại và chỉ cho chốt khi không còn blocker.

## 2. Verification

- Unit: tối ưu chi phí, ngưỡng dưới 2%, ghép đủ độ phủ, loại commercial terms.
- Service: apply NCC thứ hai thay đúng giá/VAT và không tách số lượng.
- Frontend: state/request clone giữ `PriceListId`, notice responsive, dialog hiển thị hai NCC.
- Export: Excel/PDF có NCC theo mặt hàng.
- Final gate: backend/frontend build, focused tests, route-real authenticated review.

## 3. Kết quả xác minh 2026-08-18

- `gtas verify -Scope all`: PASS; build sạch, backend unit `564/564`, frontend unit `502/502`, frontend UI `2/2`, integration mặc định `14 pass / 11 opt-in skip`.
- Migration LocalDB opt-in `RemoveSupplierSku`: PASS; nâng cấp, rollback và áp dụng lại không mất dòng dữ liệu.
- EF model: không còn thay đổi chưa có migration.
- Route thật đã đăng nhập: PASS tại `390×844`, `768×1024`, `1366×768`, `1920×1080`; không chiếm hoặc khởi động lại tiến trình `dotnet watch` của owner.
