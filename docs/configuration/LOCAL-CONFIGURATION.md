# Cấu hình và chạy local

Ưu tiên dùng script quản lý thay vì nhớ từng biến:

```powershell
.\scripts\gtas.cmd help
.\scripts\gtas.cmd configure
.\scripts\gtas.cmd init-db -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_02;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
.\scripts\gtas.cmd bootstrap-admin -ConnectionString "..." -DepartmentCode IT -DepartmentName "Information Technology"
.\scripts\gtas.cmd init-db -Mode MigrateAndDemo -Username "your-admin" -ConnectionString "..."
.\scripts\gtas.cmd run
```

`configure` lưu connection string và JWT key vào .NET user-secrets của Aspire trên máy hiện tại. File trong repository không chứa secret. `init-db` và `bootstrap-admin` chỉ chấp nhận tên database có `TEST` hoặc `DEMO`.

## Danh mục cấu hình kỹ thuật

| Nội dung | Key | Ghi chú ngắn |
|---|---|---|
| Connection string TEST | `ConnectionStrings__TestEnv` | Local/QA; database phải có `TEST` hoặc `DEMO`. |
| Connection string LIVE | `ConnectionStrings__LiveEnv` | Chỉ production; không cấu hình đồng thời TEST và LIVE. |
| Chọn TEST/LIVE | `DatabaseSettings__DefaultEnvironment` | `TestEnv` hoặc `LiveEnv`. |
| Migration mode | `DatabaseInitialization__Mode` | Một trong bốn mode ở dưới. |
| Chạy migration rồi thoát | `DatabaseInitialization__RunOnly=true` | Dùng cho CI/CD và script bootstrap. |
| Owner dữ liệu Demo | `DatabaseInitialization__DemoOwnerUsername` | Username active nhận đơn của phòng ban chính; script tự truyền khi dùng `-Mode MigrateAndDemo -Username ...`. |
| JWT key | `JwtSettings__Key` | Secret server-side, nên là chuỗi ngẫu nhiên dài. |
| Admin bootstrap | `AuthBootstrap__*` | One-shot, chỉ chạy cùng `RunOnly` + reference seed. |
| SMTP password | `EmailNotifications__Password` | Chỉ cần khi bật gửi email. |
| AI provider priority | `ReportInsights__ProviderPriority__0..n` | Thứ tự thử: `groq`, `gemini`, `ollama`, `openai`. Provider lỗi/quota sẽ chuyển provider kế tiếp. |
| Groq API key | `GROQ_API_KEY` | Free online provider; chỉ cần khi muốn dùng Groq. |
| Gemini API key | `GEMINI_API_KEY` hoặc `GOOGLE_API_KEY` | Free online provider; chỉ gửi aggregate/synthetic data. |
| OpenAI API key | `OPENAI_API_KEY` | Provider trả phí tùy chọn; tương thích cấu hình cũ. |
| Ollama local | `ReportInsights:Providers:Ollama:Enabled=true` | Không cần key; mặc định `false`, model mặc định `qwen3:8b`. |

## Bốn migration mode

- `None`: không migrate, không seed.
- `Migrate`: chỉ áp dụng EF migrations.
- `MigrateAndReference`: migrate và seed permission/reference idempotent; lựa chọn mặc định nên dùng.
- `MigrateAndDemo`: thêm catalog, phòng ban, bảng giá và đơn hàng đã chuẩn hoá từ workbook; chỉ cho database TEST/DEMO, cần `AllowDemoData=true` và một owner active. Tài khoản phụ là hồ sơ giả, không có mật khẩu và bị khóa đăng nhập.

## Bootstrap System Admin local

`bootstrap-admin` thực hiện theo một transaction:

1. Áp dụng migration và reference seed.
2. Kiểm tra department theo code; nếu chưa có thì tạo department này, chỉ trong `TestEnv` có tên database chứa `TEST`/`DEMO`.
3. Tạo tài khoản ASP.NET Core Identity, membership System Admin và bootstrap ledger.

Script dùng `OperationKey` ổn định theo database + username. Chạy lần 2, 3, 4 với cùng thông tin là idempotent; không tạo thêm admin. Mật khẩu được nhập ẩn và chỉ tồn tại trong environment của process đang chạy.

Không đặt connection string, JWT key, SMTP password, mật khẩu admin hoặc OpenAI key trong `appsettings.Development.json`. File đó chỉ giữ default không nhạy cảm; local dùng user-secrets, production dùng GitHub/DigitalOcean secrets.

## Bật AI report local

Tính năng vẫn tắt mặc định. Đặt secret cho backend project (không phải AppHost và không commit):

```powershell
$backend = 'src/Backend/Api/gtas_vpp_be.csproj'
dotnet user-secrets set 'ReportInsights:Enabled' 'true' --project $backend
dotnet user-secrets set 'GROQ_API_KEY' '<groq-key>' --project $backend
dotnet user-secrets set 'ReportInsights:ProviderPriority:0' 'groq' --project $backend
```

Hoặc chạy local hoàn toàn bằng Ollama:

```powershell
ollama pull qwen3:8b
dotnet user-secrets set 'ReportInsights:Enabled' 'true' --project $backend
dotnet user-secrets set 'ReportInsights:Providers:Ollama:Enabled' 'true' --project $backend
dotnet user-secrets set 'ReportInsights:ProviderPriority:0' 'ollama' --project $backend
```

Backend luôn gửi aggregate DTO đã kiểm tra; provider không được truy vấn database. Nếu hết quota, timeout, trả JSON lỗi hoặc không có key, hệ thống dùng rule-based insight.
