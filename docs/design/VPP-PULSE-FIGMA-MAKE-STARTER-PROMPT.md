# VPP Pulse — Prompt khởi động cho Figma Make

## Cách chạy

1. Tạo Figma Make project mới, hoặc vào **Settings → Clear context** nếu tái sử
   dụng file cũ.
2. Chọn **Add context → Upload from computer** và tải file
   `VPP-PULSE-FIGMA-MAKE-CONTEXT.md`.
3. Chọn Claude Opus 4.8 Build và dán đúng một prompt dưới đây.

```text
Bạn là Principal Product Designer, UX Researcher và Design Engineer. File
VPP-PULSE-FIGMA-MAKE-CONTEXT.md đính kèm là nguồn sự thật về
sản phẩm, route, persona, permission, business rule, state và mock data của GTAS
VPP; nó không phải visual style guide.

Frontend production chính của GTAS VPP là Blazor Interactive Server + Radzen.
Đây chỉ là Figma Make workspace React/Tailwind + shadcn/ui trống dùng để render
prototype thiết kế cho Blazor. Không tìm source GTAS VPP, `FIGMA-CONTEXT-ROUTER.md`,
AGENTS.md, router hay OpenAPI khác; dùng mock rõ ràng và không tuyên bố đã tích
hợp API thật hoặc đang xây frontend React production.

Trước khi dựng, hãy tự nghiên cứu enterprise SaaS/internal operations,
procurement workflow, data-dense UI, accessibility và design system hiện hành.
Nếu có web access, ưu tiên nguồn chính thức; nếu không có, dùng kiến thức sẵn có
và không bịa nguồn. Không gửi research report dài, không hỏi tôi chọn style.
Hãy tự chọn MỘT art direction tốt nhất, có bản sắc riêng và áp dụng trực tiếp.

PHASE 1 — FOUNDATION + EMPLOYEE NORTH STAR

Hãy xây ngay một prototype điều hướng được, không dừng ở plan, gồm:

1. Design system và state gallery dùng chung: token, typography, spacing,
   component, icon, focus, status, loading/empty/error/disabled/pending/conflict/
   success/offline, Light/Dark/Print và reduced motion.
2. App Shell hoàn chỉnh: permission-aware navigation cho toàn bộ workspace trong
   context, expanded/collapsed hoặc mô hình tốt hơn do bạn tự chọn, header
   utilities, VI/EN, theme, notification, account menu và Demo role switcher.
   Các workspace chưa làm sâu vẫn có destination placeholder nhất quán để kiểm
   tra IA, không dựng nội dung giả sơ sài.
3. Auth/account north star: Login, Register, Pending Approval, Forgot Password,
   Reset Password và validation/success/error representative states.
4. Employee north star dùng dataset trong context:
   - My Orders normal, empty và error/retry;
   - kỳ 07/2026, deadline, base order, exact item detail và next action;
   - Create Order: catalog/select → selected tray → review → submit success;
   - Order detail và immutable history;
   - supplement entry point với rule/quota đúng context.
5. Responsive representative screens ở 1440×900, 768×1024 và 390×844. Print
   representative cho My Orders hoặc Order Detail.

Tập trung hoàn thiện layout và interaction chính trước; functionality mock chỉ
cần đủ để owner click, đổi persona, đổi VI/EN/theme và trải nghiệm flow. Không cố
xây sâu Management, Procurement, Library, Access và Reports trong phase này.

Tự kiểm tra navigation, console, language, theme và viewport. Khi hoàn thành,
hiển thị prototype để owner review và chỉ tóm tắt ngắn: art direction đã chọn,
nghiên cứu đã áp dụng, giả định còn thiếu và lưu ý triển khai vào Blazor. Không
tự bắt đầu Phase 2 trước khi owner review Phase 1.
```
