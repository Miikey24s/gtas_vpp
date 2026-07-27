# Thesis browser tooling

Thư mục này chỉ là **dependency host** (nơi cài dependency) cho Playwright mà
`LVTN/tooling/capture_atlas_thesis_screens.cjs` đang nạp từ đường dẫn
`gtas_vpp_fe_react/node_modules/playwright`.

Frontend production duy nhất là Blazor/Radzen tại `../src/Frontend/Blazor/`.
React POC trước đây đã được loại khỏi source hoạt động, AppHost, Docker và CI/CD;
có thể phục hồi từ tag `archive/react-poc-2026-07-27` khi thật sự cần tra cứu.

## Cài công cụ

```powershell
npm ci
npx playwright install chromium
```

Không chạy `npm run dev` hoặc deploy từ thư mục này vì nó không còn chứa ứng dụng.
