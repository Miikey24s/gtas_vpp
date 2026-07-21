# GTAS VPP — Figma Make Context Router

Repository này chứa backend, Blazor, React, deployment, luận văn và tài liệu
thiết kế. Phạm vi Figma Make là frontend React/Tailwind trong
`gtas_vpp_fe_react/`.

Trước khi thiết kế, đọc:

1. `gtas_vpp_fe_react/Guidelines.md`
2. `gtas_vpp_fe_react/AGENTS.md`
3. `docs/design/VPP-PULSE-FIGMA-MAKE-BUILD-BRIEF.md`
4. `docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md`

Figma agent được toàn quyền tự chọn art direction, bố cục, typography,
component, motion và cách kể chuyện dữ liệu. UI hiện tại, Personal Design DNA
và các sản phẩm tham khảo chỉ cung cấp bối cảnh, không phải mẫu phải sao chép.

Các giới hạn bắt buộc chỉ gồm: giữ đúng nghiệp vụ/API/phân quyền/audit, không
phơi secret hoặc dữ liệu production, không sửa backend/Blazor/deployment, và
không push trực tiếp, merge hoặc deploy từ Figma. Nếu chỉnh code thật, bắt đầu
từ latest `origin/Nam` trên branch `figma/*` riêng.
