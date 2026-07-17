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
.\scripts\gtas.cmd init-db -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_02;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"

# 3. Tạo System Admin đầu tiên; script sẽ hỏi thông tin còn thiếu và mật khẩu ẩn
.\scripts\gtas.cmd bootstrap-admin -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_02;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True" -DepartmentCode IT -DepartmentName "Information Technology"

# 4. Chạy backend và frontend bằng Aspire
.\scripts\gtas.cmd run
```

## Những lần chạy sau

Chỉ cần `run`. `init-db` và `bootstrap-admin` có thể chạy lại an toàn: migration/seed idempotent và admin không bị tạo trùng.

Các lệnh khác:

```powershell
.\scripts\gtas.cmd status  # Kiểm tra branch và user-secrets
.\scripts\gtas.cmd test    # Build + backend/frontend unit test
```

Script chỉ cho phép `init-db` và `bootstrap-admin` trên database có `TEST` hoặc `DEMO`; không lưu secret trong repository. Danh mục cấu hình đầy đủ nằm tại [`../docs/configuration/LOCAL-CONFIGURATION.md`](../docs/configuration/LOCAL-CONFIGURATION.md).
