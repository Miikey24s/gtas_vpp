# VPP Pulse — Prompt Figma cho frontend Blazor chính

## Trước khi gửi prompt

1. Import toàn repository `Miikey24s/gtas_vpp` từ GitHub, branch nguồn `Nam`.
2. Không chọn `gtas_vpp_fe_react` làm frontend target. Nếu Figma hỏi working
   directory có thể chạy bằng Node, dùng code layer/workspace cô lập của Figma;
   repository vẫn chỉ là nguồn context.
3. Không cho Figma sửa trực tiếp production code trong vòng thiết kế. Nếu công cụ
   bắt buộc tạo branch, dùng `figma/blazor-design-phase1`.
4. Chọn Claude Opus 4.8 Build và chỉ dán prompt tiếng Việt dưới đây.

```text
Bạn là Principal Product Designer, UX Researcher và Design Engineer đang thiết kế
GTAS VPP từ codebase đã import trên GitHub.

PRE-FLIGHT VÀ NGUỒN AUTHORITY

1. Xác nhận repository là Miikey24s/gtas_vpp và source branch là Nam.
2. Frontend production chính là Blazor Interactive Server + Radzen tại:
   gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/.
   gtas_vpp_fe_react/ chỉ là proof-of-concept phụ đang tạm dừng. Không lấy React
   phụ làm target, không đề xuất cutover framework và không sửa nó.
3. Đọc đầy đủ theo thứ tự:
   - Guidelines.md
   - AGENTS.md
   - .codexrules
   - .github/copilot-instructions.md
   - docs/design/VPP-PULSE-FIGMA-MAKE-BUILD-BRIEF.md
   - docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md
   - docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md
   - gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Helpers/RouteCatalog.cs
   - App.razor, Routes, MainLayout, LeftSidebar, HeaderControls, account shell,
     auth routes, Dashboard/My Orders, Create Order, History và CSS/resource/test
     liên quan trong frontend Blazor;
   - shared DTO, API/permission/error contract liên quan trong backend.
4. Khảo sát source thật trước khi kết luận thiếu route, field, state hoặc
   permission. Không dùng một screenshot cũ hay React POC làm route authority.

MỤC TIÊU THIẾT KẾ

Tự nghiên cứu enterprise SaaS/internal operations, procurement workflow,
data-dense UI, accessibility và design system hiện hành. Tự chọn MỘT art
direction tốt nhất, có bản sắc, rõ ràng và phù hợp hệ thống nội bộ; không hỏi
owner chọn style trước và không sao chép Apple, ChatGPT, Notion, Linear, Figma,
Radzen hay UI hiện tại.

Bạn được thay đổi IA cục bộ, layout, typography, color, spacing, geometry,
component, navigation, table/chart, data storytelling, animation và
micro-interaction. Tuy nhiên thiết kế phải khả thi để Codex triển khai lại bằng
Razor/HTML/CSS/SVG và Radzen cho component phức tạp; không dựa vào hiệu ứng chỉ
có ở React khiến Blazor không thể đạt parity hợp lý.

RANH GIỚI BẮT BUỘC

- Giữ đúng API, permission, authentication, audit, immutable history,
  supplement/period/settlement rule và dữ liệu VI/EN hiện hành.
- Không tự tạo DTO, field, status, permission hoặc business rule mới.
- Không sửa backend, database, deployment, React phụ, generated API files,
  secret hoặc production data.
- Không chuyển production frontend sang React. Nếu Figma cần React để render code
  layer, coi nó là prototype thiết kế cô lập; không phải code sẽ merge nguyên vẹn.
- Mọi visible UI-owned text, validation, tooltip, notification, state và
  accessible name phải có VI/EN.
- Light, Dark, Print, keyboard/focus, reduced motion và responsive là capability
  thật, không phải vài screenshot minh họa.

PHASE 1 — FOUNDATION + EMPLOYEE NORTH STAR

Không làm cả hệ thống trong một generation. Hãy dựng một prototype điều hướng
được và đủ chi tiết để owner review cho:

1. Design system: token, typography, spacing, geometry, color, focus, motion,
   state và component primitives dùng chung.
2. App Shell: permission-aware navigation, expanded/collapsed, header utilities,
   VI/EN, Light/Dark/Print, notification, account menu, desktop/tablet/mobile và
   hover/active/focus/pressed/disabled/loading.
3. Auth/account: Login, Register, Pending Approval nếu contract có, Forgot
   Password, Reset Password và validation/success/error đại diện.
4. Employee journey:
   - My Orders: loading, normal, empty, error/retry;
   - kỳ hiện tại, deadline, base order, chi tiết mặt hàng và một next action rõ;
   - Create Order: catalog/select → selected tray → review → submit success;
   - Order Detail và immutable History;
   - supplement entry point đúng giới hạn/rule trong source.
5. Review ở 1440×900, 768×1024 và 390×844; Print cho My Orders hoặc Order Detail.

DATA STORYTELLING VÀ QA

- Mỗi màn hình đi theo task → takeaway → evidence → action → states.
- Không lặp kỳ, deadline, trạng thái, metric hoặc CTA nếu lần lặp không thêm nghĩa.
- Chart chỉ dùng khi trả lời tốt hơn con số/bảng; luôn có title, unit, time scope,
  accessible fallback và detail/table khi cần.
- Kiểm tra VI/EN, Light/Dark/Print, keyboard/focus, reduced motion, overflow,
  long data, empty/error/forbidden và contrast.
- Không commit, push, PR, merge hoặc deploy trong Phase 1.
- Khi xong, hiển thị prototype để owner review và tóm tắt ngắn: art direction,
  source/contract đã dùng, state đã cover, giả định còn thiếu và điểm Codex cần
  triển khai vào Blazor. Không tự bắt đầu Phase 2.
```
