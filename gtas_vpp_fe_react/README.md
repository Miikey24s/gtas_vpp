# GTAS VPP React Frontend

Frontend React chạy song song với ứng dụng Blazor hiện hành. Chưa route nào được xem là đã migrate cho đến khi đạt API/permission/state parity và được owner duyệt trong browser.

## Chạy local

Từ root repository, dùng Aspire để khởi động backend, Blazor và React cùng lúc:

```powershell
.\scripts\gtas.cmd run
```

Trong Aspire Dashboard, mở resource **frontend-react → GTAS React Preview**. Luồng hiện có:

1. `/login` đăng nhập bằng tài khoản đang hoạt động trong database TEST.
2. React lấy hồ sơ và quyền từ backend, sau đó chuyển tới `/app/orders`.
3. Menu tài khoản có **Đăng xuất**, luôn xóa session của tab và quay lại `/login`.

Nếu cần xem đơn demo từ workbook, nạp dữ liệu một lần trước khi chạy (lệnh idempotent — chạy lại không tạo trùng):

```powershell
.\scripts\gtas.cmd init-db -Mode MigrateAndDemo -Username "your-admin" -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_01;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
```

POC chỉ lưu JWT trong `sessionStorage` của tab local/TEST. Không dùng frontend này cho production cho đến khi backend có BFF hoặc secure HttpOnly cookie session.

Hoặc chạy riêng frontend:

```powershell
cd gtas_vpp_fe_react
npm install
$env:GTAS_API_PROXY_TARGET = "https://localhost:<backend-port>"
npm run dev
```

## Sinh API client

Mặc định client được sinh từ Swagger snapshot đã xuất trực tiếp từ backend:

```powershell
npm run api:generate
```

Khi backend đang chạy, có thể sinh thẳng từ endpoint mới nhất:

```powershell
$env:GTAS_OPENAPI_URL = "https://localhost:<backend-port>/swagger/v1/swagger.json"
npm run api:generate
```

Không sửa tay file trong `src/api/generated/`. Snapshot `openapi/gtas-vpp.openapi.json`
phải được xuất lại khi API contract thay đổi; không chứa connection string, token hoặc dữ liệu database.

## Kiểm tra

```powershell
npm run check
npm run test:e2e
```
