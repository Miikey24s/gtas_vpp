# GTAS local scripts

Chạy từ thư mục gốc repository:

```powershell
.\scripts\gtas.cmd help
```

## Lần đầu trên máy hoặc database mới

```powershell
# 1. Lưu connection string TEST và JWT vào .NET user-secrets
.\scripts\gtas.cmd configure

# 2. Tạo/migrate/seed database TEST
.\scripts\gtas.cmd init-db -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_01;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"

# 3. Tạo System Admin đầu tiên; script sẽ hỏi thông tin còn thiếu và mật khẩu ẩn
.\scripts\gtas.cmd bootstrap-admin -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_01;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True" -DepartmentCode IT -DepartmentName "Information Technology"

# 4. Tuỳ chọn: nạp catalog + đơn hàng từ workbook đã chuẩn hoá
# Nếu TEST có đúng một DEV đang hoạt động, owner được chọn tự động.
.\scripts\gtas.cmd init-db -Mode MigrateAndDemo -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_01;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"

# 5. Chạy backend và frontend bằng Aspire + Hot Reload
.\scripts\gtas.cmd run
```

## Những lần chạy sau

Mật khẩu admin phải có ít nhất 10 ký tự, gồm chữ thường, chữ hoa, số và ký tự đặc biệt. Script sẽ kiểm tra và yêu cầu nhập lại trước khi chạy migration.

`run` dùng `dotnet watch`: thay đổi hỗ trợ Hot Reload được áp dụng ngay, thay đổi lớn sẽ tự restart AppHost. Trang ứng dụng là `https://localhost:7009/Account/Login`; URL có token `?t=...` chỉ dùng để đăng nhập Aspire Dashboard. Trong Dashboard cũng có link **frontend → GTAS Login**.

Những lần sau chỉ cần `run`. `init-db` và `bootstrap-admin` có thể chạy lại an toàn: migration/seed idempotent và admin không bị tạo trùng. Mode `MigrateAndDemo` tự chọn owner khi TEST có đúng một DEV đang hoạt động; vẫn có thể truyền `-Username` để chọn tường minh. Đơn phòng ban của owner bind vào tài khoản thật, các phòng ban còn lại dùng tài khoản fixture bị khóa đăng nhập.

Các lệnh khác:

```powershell
.\scripts\gtas.cmd status                     # Kiểm tra branch và user-secrets
.\scripts\gtas.cmd preflight -Scope frontend  # Context + branch + dirty files trước task AI
.\scripts\gtas.cmd doctor -Scope frontend     # SDK/tool/context có sẵn trên máy
.\scripts\gtas.cmd agent-check                # Lint AGENTS, adapter và repo skill
.\scripts\gtas.cmd test-backend               # Backend unit test
.\scripts\gtas.cmd test-frontend              # Frontend unit test
.\scripts\gtas.cmd test                       # Build + backend/frontend unit test
.\scripts\gtas.cmd verify -Scope all          # Các gate CI không cần browser trước handoff
```

`preflight`, `doctor` và `verify` nhận `all`, `frontend`, `backend`, `tests` hoặc `thesis`; `preflight`/`doctor` còn hỗ trợ `-OutputFormat json`. Các lệnh chỉ in metadata an toàn, không đọc giá trị secret. `verify` chạy đúng gate theo scope, luôn kiểm tra agent setup, Gitleaks và whitespace; UI thay đổi vẫn phải được kiểm tra trên route Blazor thật riêng.

## Mở nhanh Razor source từ UI khi demo/bảo vệ

Frontend Debug dùng package `FindRazorSourceFile`; Visual Studio 2026 cài extension cùng tên từ Marketplace.
Khi app đang chạy Debug, nhấn `Ctrl+Shift+F` trong trình duyệt để bật Inspection Mode, rê chuột đến vùng UI
và click để Visual Studio mở file `.razor` tạo ra vùng đó. Nhấn `Esc` để thoát.

Package tự vô hiệu hóa trong Release. Không deploy Debug build vì đường dẫn source đầy đủ có thể xuất hiện
trong marker phục vụ inspector. Nếu component chỉ render component con hoặc có nhiều root element, kết quả
có thể chỉ đến component gần nhất; dùng `F12`, `Shift+F12` hoặc Copilot Agent Mode để lần tiếp code-behind,
API client, controller, service và test.

Script chỉ cho phép `init-db` và `bootstrap-admin` trên database có `TEST` hoặc `DEMO`; không lưu secret trong repository. Danh mục cấu hình đầy đủ nằm tại [`../docs/configuration/LOCAL-CONFIGURATION.md`](../docs/configuration/LOCAL-CONFIGURATION.md).
