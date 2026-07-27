# GTAS VPP — Figma Context Router

Repository này chứa backend, frontend Blazor/Radzen chính, bản React POC đóng băng,
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
`src/Shared/`. Chỉ đọc `gtas_vpp_fe_react/` như một thử nghiệm
phụ khi cần đối chiếu ý tưởng đã từng làm; không dùng nó làm source authority.

Figma agent được toàn quyền tự chọn art direction, bố cục, typography,
component, motion và cách kể chuyện dữ liệu. UI hiện tại, Personal Design DNA
và sản phẩm tham khảo chỉ cung cấp evidence, không phải mẫu phải sao chép.

Các giới hạn bắt buộc: giữ đúng nghiệp vụ/API/phân quyền/audit; không phơi secret
hoặc dữ liệu production; không sửa backend, database, deployment hay React phụ;
không tự chuyển production frontend sang React; không push trực tiếp, merge hoặc
deploy từ Figma. Code layer React do Figma tạo chỉ là design evidence để owner
duyệt rồi Codex triển khai lại vào Blazor/Radzen. Nếu Figma bắt buộc tạo thay đổi
trong repository, bắt đầu từ latest `origin/Nam` trên branch `figma/*` riêng.
