# Browser tooling

Thư mục này là **dependency host** (nơi cài dependency) Playwright dùng chung cho
script render Design Atlas và chụp ảnh luận văn. Đây không phải application frontend.

Frontend production duy nhất là Blazor/Radzen tại `../../src/Frontend/Blazor/`.
React POC trước đây đã được loại khỏi source hoạt động, AppHost, Docker và CI/CD;
có thể phục hồi từ tag `archive/react-poc-2026-07-27` khi thật sự cần tra cứu.

## Cài công cụ

```powershell
Set-Location scripts/browser
npm ci
npx playwright install chromium
```

Không chạy `npm run dev` hoặc deploy từ thư mục này vì nó không còn chứa ứng dụng.
