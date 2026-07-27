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

# 4. Tuỳ chọn: nạp catalog + đơn hàng Demo từ workbook đã chuẩn hoá
.\scripts\gtas.cmd init-db -Mode MigrateAndDemo -Username "your-admin" -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_01;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"

# 5. Chạy backend và frontend bằng Aspire + Hot Reload
.\scripts\gtas.cmd run
```

## Những lần chạy sau

Mật khẩu admin phải có ít nhất 10 ký tự, gồm chữ thường, chữ hoa, số và ký tự đặc biệt. Script sẽ kiểm tra và yêu cầu nhập lại trước khi chạy migration.

`run` dùng `dotnet watch`: thay đổi hỗ trợ Hot Reload được áp dụng ngay, thay đổi lớn sẽ tự restart AppHost. Trang ứng dụng là `https://localhost:7009/Account/Login`; URL có token `?t=...` chỉ dùng để đăng nhập Aspire Dashboard. Trong Dashboard cũng có link **frontend → GTAS Login**.

Những lần sau chỉ cần `run`. `init-db` và `bootstrap-admin` có thể chạy lại an toàn: migration/seed idempotent và admin không bị tạo trùng. Mode `MigrateAndDemo` cần một username đang hoạt động; đơn phòng ban của username đó sẽ bind vào tài khoản thật, các phòng ban còn lại dùng tài khoản demo bị khóa đăng nhập.

Các lệnh khác:

```powershell
.\scripts\gtas.cmd status  # Kiểm tra branch và user-secrets
.\scripts\gtas.cmd test    # Build + backend/frontend unit test
```

Script chỉ cho phép `init-db` và `bootstrap-admin` trên database có `TEST` hoặc `DEMO`; không lưu secret trong repository. Danh mục cấu hình đầy đủ nằm tại [`../docs/configuration/LOCAL-CONFIGURATION.md`](../docs/configuration/LOCAL-CONFIGURATION.md).
