# GTAS VPP

Lệnh local ngắn gọn: `.\scripts\gtas.cmd help`. Danh mục connection string, TEST/LIVE, migration mode, admin bootstrap, JWT, SMTP và OpenAI nằm tại [`docs/configuration/LOCAL-CONFIGURATION.md`](docs/configuration/LOCAL-CONFIGURATION.md).

Website quản lý văn phòng phẩm cho doanh nghiệp, gồm luồng lập và duyệt đơn yêu cầu, quản lý danh mục - bảng giá, phân quyền và tổng hợp số liệu. Repository chứa backend ASP.NET Core, frontend Blazor Server, bộ kiểm thử và tài liệu luận văn.

## Công nghệ chính

- .NET 10, ASP.NET Core Web API và Blazor Server
- Entity Framework Core, SQL Server 2022
- Radzen Blazor, JWT, Mapster và Serilog
- Docker Compose; .NET Aspire dùng để chạy backend và frontend khi phát triển
- xUnit và Playwright cho kiểm thử

## Cấu trúc repository

```text
src/Backend/             API, application service, domain model và migration
src/Frontend/Blazor/     Frontend Blazor Server/Radzen đang chạy chính thức
src/Shared/              DTO và contract dùng chung giữa backend/frontend
src/Hosting/             Aspire AppHost và service defaults
tests/                   Unit, integration, UI test và test support
deploy/                  Dockerfile, runbook và script vận hành production
deploy/nginx/            Cấu hình Nginx được kiểm tra và đóng gói cùng deployment
docs/                    Kiến trúc, thiết kế, execution plan và tài liệu lưu trữ
LVTN/                    Luận văn, sơ đồ, ảnh giao diện và công cụ Word
scripts/browser/         Playwright Node dùng chung cho render Atlas và ảnh luận văn
```

Shared DTO chính thức nằm tại `src/Shared`; frontend tham chiếu trực tiếp project này.
Quy ước UI, localization và accessibility dành cho người và AI nằm tại
[`src/Frontend/README.md`](src/Frontend/README.md).

## Làm việc với AI agent

- Đọc root `AGENTS.md` và `AGENTS.md` gần file đang sửa nhất.
- Mô hình context, skill, plan, MCP và handoff nằm tại [`docs/ai/AI-AGENT-OPERATING-MODEL.md`](docs/ai/AI-AGENT-OPERATING-MODEL.md).
- Trước task phức tạp, chạy `./scripts/gtas.cmd preflight -Scope <all|frontend|backend|tests|thesis>`.
- Repo skills bao phủ UI Blazor/Radzen, database safety và thesis DOCX; kế hoạch dài dùng template [`docs/planning/05-EXECUTION-TEMPLATE.md`](docs/planning/05-EXECUTION-TEMPLATE.md).
- Chạy `./scripts/gtas.cmd agent-check` để lint instruction/skill và xem [`docs/ai/AI-AGENT-EVALS.md`](docs/ai/AI-AGENT-EVALS.md) khi đánh giá một custom mới.
- Không commit model preference, MCP credential, browser profile hoặc secret theo máy.

## Development với .NET Aspire (khuyến nghị)

Yêu cầu .NET 10 SDK và SQL Server local. Repository không chứa connection string sử dụng
được; Aspire nhận binding `TestEnv` qua secret `test-database-connection-string` và database
phát triển phải dùng tên `GTAS_VPP_TEST`, không dùng `GTAS_VPP_LIVE`. Aspire chỉ điều phối
backend/frontend nên không yêu cầu Docker. Khởi tạo ba secret local một lần bằng PowerShell:

```powershell
function New-GtasSecret {
    $bytes = New-Object byte[] 48
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    [Convert]::ToBase64String($bytes)
}

$appHost = 'src/Hosting/AppHost/MyAspire.AppHost.csproj'
$testDatabase = 'Server=(localdb)\MSSQLLocalDB;Database=GTAS_VPP_TEST;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True'

dotnet user-secrets set "Parameters:test-database-connection-string" $testDatabase --project $appHost
dotnet user-secrets set "Parameters:jwt-key" (New-GtasSecret) --project $appHost
dotnet run --project src/Hosting/AppHost/MyAspire.AppHost.csproj
```

Reference bootstrap không tạo tài khoản hoặc credential mẫu. Tài khoản ứng dụng dùng
ASP.NET Core Identity và password hash một chiều; legacy stored-procedure/TripleDES login
không còn là đường đăng nhập. Không dùng connection string hoặc secret Production cho
Development.

## Development bằng Docker Compose

Đây là phương án tùy chọn, tách biệt với Aspire dùng SQL Server local. Chỉ phương án này
mới yêu cầu Docker Desktop hoặc Docker Engine có Compose.

```powershell
Copy-Item .env.example .env
# Tạo DB_SA_PASSWORD và JWT_KEY ngẫu nhiên, riêng cho Development.
# Không commit file .env.
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
dotnet restore gtas_vpp.slnx
dotnet build gtas_vpp.slnx -c Release
dotnet test tests/Backend.UnitTests/gtas_vpp_be.Tests.csproj -c Release
dotnet test tests/Frontend.UnitTests/gtas_vpp_fe.Tests.csproj -c Release
```

Lệnh rút gọn cho agent và lập trình viên:

```powershell
./scripts/gtas.cmd test
./scripts/gtas.cmd verify -Scope all
```

UI test mặc định tự dựng stack TEST cô lập. Không trỏ test vào ứng dụng hoặc database đang dùng thủ công:

```powershell
$env:GTAS_E2E_ISOLATED = '1'
dotnet test tests/Frontend.UiTests/gtas_vpp_fe.UITests.csproj -c Release
```

Test có thay đổi dữ liệu còn yêu cầu
`GTAS_E2E_MUTATION_OPT_IN=I_UNDERSTAND_THIS_MUTATES_QA_DATA`; xem
[`docs/testing/QA-001-ISOLATED-TESTING.md`](docs/testing/QA-001-ISOLATED-TESTING.md).

## Production trên DigitalOcean

Production dùng image bất biến từ GHCR, Docker Compose riêng, Nginx/Let's Encrypt,
backup SQL Server có kiểm chứng trước migration và GitHub `production` environment.
Runbook đầy đủ nằm tại [`deploy/README.md`](deploy/README.md). Không chạy
`docker-compose.yml` Development trên Droplet.

## Nhận định báo cáo bằng AI (tùy chọn)

Trang Report luôn có phân tích theo quy tắc xác định. AI chỉ được gọi khi người dùng chủ động
chọn **Tạo nhận định**, feature được bật và provider có secret server-side. Khung hiện hỗ trợ
Groq, Gemini, Ollama local và OpenAI tương thích cũ:

```powershell
$env:REPORT_INSIGHTS_ENABLED = 'true'
$env:GROQ_API_KEY = '<server-side-secret>'
```

Thứ tự provider mặc định là `groq`, `gemini`, `ollama`, `openai`; có thể đổi bằng
`ReportInsights__ProviderPriority__0..n`. Mặc định tính năng AI tắt. Backend chỉ gửi số liệu tổng hợp
đã được giới hạn theo quyền hiện tại, không gửi danh tính người yêu cầu hay dòng đơn gốc. Khi API
tắt, thiếu khóa, hết quota, quá thời gian hoặc trả JSON không hợp lệ, hệ thống tự động dùng phân tích
theo quy tắc. Không đưa API key vào `appsettings*.json`, source hoặc frontend; ở production dùng
secret manager/GitHub Secret.

## Luận văn và sơ đồ

- Nguồn luận văn chuẩn: `LVTN/NguyenAnNam_DH52201078.docx`
- Hướng dẫn cấu trúc và quy trình review: `LVTN/README.md`
- Source sơ đồ và SVG: `LVTN/diagrams/`
- Script định dạng, render và kiểm tra Word: `LVTN/tooling/`

Xem `AGENTS.md` trước khi dùng AI sửa source hoặc luận văn để giữ đúng cấu trúc, quy ước và phạm vi file cần commit.

## Bảo mật và Git

- Không commit `.env`, thông tin đăng nhập, API key, file log, `bin/`, `obj/`, kết quả audit hay bản render tạm.
- `.env.example` chỉ chứa tên biến và giá trị mẫu an toàn.
- Quy trình deploy production nằm trong `.github/workflows/deploy.yml`; bí mật phải được cấu hình bằng GitHub Actions Secrets.
