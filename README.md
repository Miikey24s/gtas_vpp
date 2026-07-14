# GTAS VPP

Website quản lý văn phòng phẩm cho doanh nghiệp, gồm luồng lập và duyệt đơn yêu cầu, quản lý danh mục - bảng giá, phân quyền và tổng hợp số liệu. Repository chứa backend ASP.NET Core, frontend Blazor Server, bộ kiểm thử và tài liệu luận văn.

## Công nghệ chính

- .NET 10, ASP.NET Core Web API và Blazor Server
- Entity Framework Core, SQL Server 2022
- Radzen Blazor, JWT, Mapster và Serilog
- Docker Compose; .NET Aspire dùng để chạy backend và frontend khi phát triển
- xUnit và Playwright cho kiểm thử

## Cấu trúc repository

```text
gtas_vpp_be/             Backend, migration, model, service và shared DTO
gtas_vpp_fe/             Frontend Blazor Server
gtas_vpp_be.Tests/       Kiểm thử backend
gtas_vpp_fe.Tests/       Kiểm thử frontend
gtas_vpp_fe.UITests/     Kiểm thử giao diện với Playwright/Aspire
MyAspire.AppHost/        Điều phối môi trường phát triển
deploy/                  Runbook và script triển khai/backup/restore production
LVTN/                    Luận văn, sơ đồ, ảnh giao diện và công cụ Word
```

Shared DTO chính thức nằm tại `gtas_vpp_be/gtas_vpp_shared`; frontend tham chiếu trực tiếp project này.
Quy ước UI, localization và accessibility dành cho người và AI nằm tại
[`gtas_vpp_fe/README.md`](gtas_vpp_fe/README.md).

## Development với .NET Aspire (khuyến nghị)

Yêu cầu .NET 10 SDK và Docker Desktop. Aspire khởi tạo SQL Server Development trong
volume riêng, chờ database sẵn sàng rồi mới chạy backend và frontend. Khởi tạo ba
secret local một lần bằng PowerShell:

```powershell
function New-GtasSecret {
    $bytes = New-Object byte[] 48
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    [Convert]::ToBase64String($bytes)
}

dotnet user-secrets set "Parameters:sql-password" ("Vpp1!" + (New-GtasSecret)) --project MyAspire.AppHost/MyAspire.AppHost.csproj
dotnet user-secrets set "Parameters:jwt-key" (New-GtasSecret) --project MyAspire.AppHost/MyAspire.AppHost.csproj
dotnet user-secrets set "Parameters:password-encryption-key" (New-GtasSecret) --project MyAspire.AppHost/MyAspire.AppHost.csproj
dotnet run --project MyAspire.AppHost/MyAspire.AppHost.csproj
```

Các secret này chỉ dành cho database local. Không dùng secret production cho Development.

## Development bằng Docker Compose

Yêu cầu Docker Desktop hoặc Docker Engine có Compose.

```powershell
Copy-Item .env.example .env
# Tạo giá trị ngẫu nhiên cho ba biến đang để trống; không commit file .env.
docker compose up -d --build
```

Các dịch vụ mặc định:

- SQL Server: `localhost:1433`
- Backend: `http://localhost:8080`
- Frontend: `http://localhost:5000`

Dừng môi trường bằng `docker compose down`. Chỉ thêm `-v` khi thật sự muốn xóa volume
dữ liệu Development. Tất cả port local chỉ bind `127.0.0.1`, không mở ra mạng LAN.

## Build và kiểm thử từ source

Yêu cầu .NET 10 SDK. Cấu hình connection string và khóa cục bộ bằng biến môi trường hoặc user secrets; không ghi bí mật vào source.

```powershell
dotnet restore gtas_vpp.sln
dotnet build gtas_vpp.sln -c Release
dotnet test gtas_vpp_be.Tests/gtas_vpp_be.Tests.csproj -c Release
dotnet test gtas_vpp_fe.Tests/gtas_vpp_fe.Tests.csproj -c Release
```

UI test cần một môi trường ứng dụng đang chạy và tài khoản test truyền qua biến môi trường; không lưu tài khoản này trong Git:

```powershell
$env:GTAS_TEST_USERNAME = '<test-user>'
$env:GTAS_TEST_PASSWORD = '<test-password>'
$env:UITEST_BASE_URL = 'http://127.0.0.1:5000/'
dotnet test gtas_vpp_fe.UITests/gtas_vpp_fe.UITests.csproj -c Release
```

## Production trên DigitalOcean

Production dùng image bất biến từ GHCR, Docker Compose riêng, Nginx/Let's Encrypt,
backup SQL Server có kiểm chứng trước migration và GitHub `production` environment.
Runbook đầy đủ nằm tại [`deploy/README.md`](deploy/README.md). Không chạy
`docker-compose.yml` Development trên Droplet.

## Nhận định báo cáo bằng AI (tùy chọn)

Trang Report luôn có phân tích theo quy tắc xác định. OpenAI chỉ được gọi khi người dùng chủ động
chọn **Tạo nhận định** và cả hai biến sau đã được cấu hình ở backend:

```powershell
$env:REPORT_INSIGHTS_ENABLED = 'true'
$env:OPENAI_API_KEY = '<server-side-secret>'
```

Mặc định tính năng AI tắt, model cấu hình là `gpt-5.6-luna`. Backend chỉ gửi số liệu tổng hợp đã
được giới hạn theo quyền hiện tại, không gửi danh tính người yêu cầu hay dòng đơn gốc. Khi API tắt,
thiếu khóa, quá thời gian hoặc trả lỗi, hệ thống tự động dùng phân tích theo quy tắc. Không đưa API
key vào `appsettings*.json`, source hoặc frontend; ở production dùng secret manager/GitHub Secret.

## Luận văn và sơ đồ

- Bản đang chỉnh sửa: `LVTN/NguyenAnNam_DH52201078_working.docx`
- Bản bàn giao gần nhất: `LVTN/checkpoints/99_final.docx`
- Source sơ đồ và SVG: `LVTN/diagrams/`
- Script định dạng, render và kiểm tra Word: `LVTN/tooling/`

Xem `AGENTS.md` trước khi dùng AI sửa source hoặc luận văn để giữ đúng cấu trúc, quy ước và phạm vi file cần commit.

## Bảo mật và Git

- Không commit `.env`, thông tin đăng nhập, API key, file log, `bin/`, `obj/`, kết quả audit hay bản render tạm.
- `.env.example` chỉ chứa tên biến và giá trị mẫu an toàn.
- Quy trình deploy production nằm trong `.github/workflows/deploy.yml`; bí mật phải được cấu hình bằng GitHub Actions Secrets.
