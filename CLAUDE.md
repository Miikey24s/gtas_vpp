# CLAUDE.md — GTAS VPP

Quy tắc làm việc chính của repository nằm trong `AGENTS.md`. Claude Code phải tuân thủ đúng
file đó, không tạo bộ quy tắc song song.

@AGENTS.md

Khi sửa file trong một phạm vi con, đọc `AGENTS.md` gần file đó nhất. Mô hình context, skill,
execution record và handoff chung nằm tại `docs/ai/AI-AGENT-OPERATING-MODEL.md`; không sao chép
toàn bộ nội dung các nguồn này vào `CLAUDE.md`.

## Khi compact/tóm tắt hội thoại

Luôn giữ lại trong bản tóm tắt: (1) mục tiêu task đang làm và task id trong task list;
(2) danh sách file đã sửa chưa commit; (3) các quyết định owner đã chốt trong phiên
(kèm số D/R-D nếu đã ghi vào decision log); (4) lệnh build/test đang dùng và kết quả
gate gần nhất; (5) các background task/agent đang chạy kèm task id. Trạng thái dài hạn
đọc lại từ: `docs/execution/ATLAS-001.md`, `docs/execution/REFACTOR-001.md`, task list,
và auto-memory — không cần chép lại nội dung các file đó vào tóm tắt.

## Tài liệu bắt buộc đọc trước khi sửa

- UI Blazor/Radzen (`src/Frontend/Blazor/`): `.codexrules`, `.github/copilot-instructions.md`,
  `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md`.
- Chọn MCP / browser tool / QA layer: `docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md`.
- React POC: đã lưu ở tag `archive/react-poc-2026-07-27` và gỡ khỏi source hoạt động/runtime/CI-CD. `gtas_vpp_fe_react/` chỉ giữ Playwright cho tooling LVTN.

## MCP đã cấu hình cho Claude Code

| Server | Scope | Dùng khi |
|---|---|---|
| `microsoft-learn` | user | .NET, Blazor, ASP.NET Core, Aspire, tài liệu Microsoft — tra trước tiên |
| `radzen-blazor` | local (`.claude.json`) | Radzen component/property/event — bắt buộc tra trước khi sửa Radzen. Quota 50 request/15 ngày; hết quota thì dừng và xin key mới |
| `context7` | user | Tài liệu package bên thứ ba, chỉ dùng sau nguồn chính chủ |
| `playwright` | user | Browser mặc định: DOM/ARIA snapshot, interaction, screenshot, console, network |
| `chrome-devtools` | user | Performance trace, network/console sâu. Chỉ dùng profile Chrome TEST |
| `sosumi` | user | Apple HIG / developer docs làm tham khảo visual hierarchy và accessibility |

Radzen key nằm ở local scope trong `~/.claude.json`; `.mcp.json` của repo đã bị `.gitignore`.
Không commit key vào bất kỳ file nào được Git theo dõi.

Figma và GitHub dùng plugin connector sẵn có của Claude Code (cần authorize OAuth trong phiên
interactive), không thêm MCP trùng chức năng.

Không cài thêm MCP trùng vai trò (browser MCP khác, doc aggregator khác) chỉ để tăng số lượng
công cụ — xem mục "Vì sao không cài thêm browser MCP khác" trong toolchain doc.

## Lệnh build/test tối thiểu

```powershell
dotnet build gtas_vpp.sln -c Release
dotnet test tests/Backend.UnitTests/gtas_vpp_be.Tests.csproj -c Release
dotnet test tests/Frontend.UnitTests/gtas_vpp_fe.Tests.csproj -c Release
```
