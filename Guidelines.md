# GTAS VPP — Figma Context Router

Repository này chứa backend, frontend Blazor/Radzen chính, hồ sơ React POC đã lưu trữ,
deployment, luận văn và tài liệu thiết kế. Phạm vi Figma hiện tại là nghiên cứu
và dựng prototype cho frontend chính `src/Frontend/Blazor/`; không phải tiếp tục migration
React.

Trước khi thiết kế, đọc:

1. `AGENTS.md`
2. `.codexrules` và `.github/copilot-instructions.md`
3. `docs/design/VPP-PULSE-FIGMA-MAKE-BUILD-BRIEF.md`
4. `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md`
5. `src/Frontend/Blazor/Helpers/RouteCatalog.cs`

Sau đó khảo sát Razor/CSS/resource/test thật trong
`src/Frontend/Blazor/` và contract dùng chung trong
`src/Shared/`. Khi thật sự cần đối chiếu React POC cũ, đọc tag
`archive/react-poc-2026-07-27`; `gtas_vpp_fe_react/` hiện chỉ chứa Playwright cho tooling LVTN.

Figma agent được toàn quyền tự chọn art direction, bố cục, typography,
component, motion và cách kể chuyện dữ liệu. UI hiện tại, Personal Design DNA
và sản phẩm tham khảo chỉ cung cấp evidence, không phải mẫu phải sao chép.

Các giới hạn bắt buộc: giữ đúng nghiệp vụ/API/phân quyền/audit; không phơi secret
hoặc dữ liệu production; không sửa backend, database, deployment hay tạo lại React phụ;
không tự chuyển production frontend sang React; không push trực tiếp, merge hoặc
deploy từ Figma. Code layer React do Figma tạo chỉ là design evidence để owner
duyệt rồi Codex triển khai lại vào Blazor/Radzen. Nếu Figma bắt buộc tạo thay đổi
trong repository, bắt đầu từ latest `origin/Nam` trên branch `figma/*` riêng.
