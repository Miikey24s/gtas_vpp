# GTAS VPP React Auxiliary Frontend

Frontend React là proof-of-concept phụ đang tạm dừng; frontend chính và public
target hiện tại là Blazor/Radzen trong `../gtas_vpp_fe/`.
Trạng thái từng route, quality gate và owner review được quản lý tại
`docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md`.

Không tiếp tục migration/cutover hoặc dùng project này làm Figma target nếu owner
chưa mở lại phạm vi React rõ ràng.

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

Khi chạy qua Aspire hoặc production, React dùng secure HttpOnly cookie session và antiforgery header; access token không được lưu trong browser storage. `VITE_AUTH_MODE=token` chỉ dành cho isolated legacy/Bearer compatibility tests và không được dùng cho production.

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

## Production image

Image production build Vite bằng Node LTS, sau đó chỉ chép static asset sang
unprivileged Nginx. Container phục vụ SPA deep-link, cache immutable cho asset đã
fingerprint và health probe tại `/healthz`:

```powershell
docker build -t gtas-vpp-react-frontend:local .
docker run --rm -p 5100:8080 gtas-vpp-react-frontend:local
```

Image này chỉ phục vụ proof-of-concept/rollback experiment. Public Nginx mặc định
route tới Blazor; chỉ chuyển sang React bằng thao tác có chủ đích theo
`deploy/README.md`.
