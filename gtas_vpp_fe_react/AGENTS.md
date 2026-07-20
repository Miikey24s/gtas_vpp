# GTAS VPP React frontend

Áp dụng cho toàn bộ `gtas_vpp_fe_react/`.

- Đây là frontend React chạy song song; không xóa hoặc sửa frontend Blazor chỉ để React hoạt động.
- Trước mọi thay đổi, đọc và cập nhật `../docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md`.
- React là modernization, không chép route/layout Blazor 1:1. Giữ business invariant, permission, audit và dữ liệu; chủ động tối ưu IA, workflow, component và API contract có kiểm soát.
- Stack chuẩn: React + TypeScript + Vite, shadcn/ui + Tailwind CSS, React Router, TanStack Query/Table, React Hook Form + Zod và i18next.
- Dùng shadcn MCP trước khi thêm component mới. Component shadcn là source thuộc repository: đọc, sửa và test trực tiếp; không bọc override CSS dài như cách dùng package UI đóng.
- Không thêm Next.js, framework SSR hoặc state library khác nếu chưa có nhu cầu sản phẩm được ghi trong living plan.
- DTO/API types phải sinh bằng `npm run api:generate` từ Swagger/OpenAPI; không copy interface từ C# bằng tay.
- Khi API hiện tại cản trở outcome, được nâng backend contract bằng change-set riêng có test; không tạo workaround frontend hoặc model giả để che contract thiếu.
- Server state dùng TanStack Query; local UI state ưu tiên React state/context trước khi thêm store.
- Mọi text hiển thị dùng i18next VI/EN. Light/Dark/Print phải dùng chung design token.
- Accessibility tối thiểu: semantic HTML, keyboard/focus, accessible name và axe không có violation critical/serious.
- Không commit `.env`, generated secret, `node_modules`, `dist`, coverage, Playwright report hoặc test artifacts.

Kiểm tra tối thiểu:

```powershell
npm run lint
npm run typecheck
npm test
npm run build
npm run test:e2e
```
