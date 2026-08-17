# Frontend Blazor/Radzen instructions

Áp dụng cho toàn bộ `src/Frontend/Blazor/`.

## Context bắt buộc

- Đọc root `AGENTS.md`, `.github/copilot-instructions.md` và living plan UI.
- Đọc `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md` và cập nhật cùng change-set khi thay đổi design contract, route status hoặc owner feedback.
- Đọc `docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md` trước khi chọn MCP hoặc QA layer.
- Dùng repo skill `.agents/skills/gtas-vpp-ui-system/` cho thay đổi UI, Atlas, token, responsive hoặc visual QA.

## Kiến trúc UI

- OpenAI/Codex-inspired minimal system là visual contract chính: neutral system surface, typography gọn, spacing/radius nhất quán, icon outline đơn sắc, một primary action rõ và motion tiết chế. M0–M2 Atlas chỉ còn là reference bố cục/nghiệp vụ đã chuẩn hóa; Atlas vẫn read-only và browser Blazor thật là visual authority cuối.
- Dùng composition: token → primitive → composite → workspace pattern → route. Không tạo cây kế thừa markup hoặc `UniversalPage<T>`.
- Chỉ trích xuất abstraction khi ít nhất hai route thật có cùng layout/behavior. Route giữ API, permission và nghiệp vụ; pattern chỉ giữ layout/state composition.
- Razor/HTML sở hữu shell, navigation, card, toolbar, action bar, state và responsive layout. Radzen giữ DataGrid, Dialog, DropDown, DatePicker, Numeric, validation và component phức tạp có giá trị.
- Không bọc toàn bộ Radzen. Chuẩn hóa bằng token, Radzen bridge và frame/composite có mục đích rõ.

## CSS và component

- Màu, spacing, typography, radius, shadow, control height và motion lấy từ `wwwroot/css/vpp-tokens.css`.
- Mapping Radzen chỉ nằm trong `wwwroot/css/vpp-radzen-theme.css`; Radzen base phải được tải trước project overrides.
- Ưu tiên `.razor.css` cho layout component/route. Không thêm inline `style`, hex color, pixel spacing hoặc `!important` mới nếu token/bridge giải quyết được.
- Parameter variant dùng enum/record thay string tự do khi tập giá trị hữu hạn.
- Tách component khi markup hoặc code-behind trộn nhiều responsibility; không áp một giới hạn dòng máy móc nhưng tránh file 500–800 dòng tiếp tục phình.
- Chuỗi UI dùng `@Loc[]`; code identifier tiếng Anh. Comment tiếng Việt theo kiểu `quick-scan`: ghi ngắn
  trách nhiệm component/state, bước orchestration, kết quả/tác động và lifecycle/Radzen rule khó đoán;
  không mô tả lại markup, event hoặc API call hiển nhiên.

## Workflow

1. Chạy `./scripts/gtas.cmd preflight -Scope frontend` từ repo root.
2. Đọc route, DTO/API, permission, fixture và implementation tương tự.
3. Tra Microsoft Learn cho .NET/Blazor/Aspire; tra Radzen MCP theo đúng component, model, field, binding/event và behavior. Hết quota/key thì dừng phần phụ thuộc Radzen.
4. Với route bảo vệ, dùng tài khoản/fixture TEST, giữ browser context hợp lý và không lưu credential/cookie/storage vào Git.
5. Làm một vertical slice, chạy frontend unit/architecture test và route-real Playwright. Không dùng `networkidle` làm điều kiện duy nhất với Blazor Server.
6. Kiểm tra `390×844`, `768×1024`, `1366×768`, `1920×1080`; VI/EN, Light/Dark và Print khi liên quan.
7. Thu console/network failure; kiểm tra keyboard/focus, dialog return, validation, loading, empty, filtered-empty, error, denied, disabled và success.
8. Route động chỉ dùng dữ liệu đại diện; tránh crawl lặp vô hạn. Chrome DevTools chỉ dùng khi cần trace/performance sâu.
9. Không đổi auth, database schema hoặc backend behavior chỉ để sửa visual. Chỉ khóa baseline sau owner approval.

## Lệnh tối thiểu

```powershell
./scripts/gtas.cmd test-frontend
dotnet build gtas_vpp.slnx -c Release
```

UI test authenticated cần `GTAS_E2E_ISOLATED=1`; test mutation còn cần `GTAS_E2E_MUTATION_OPT_IN=I_UNDERSTAND_THIS_MUTATES_QA_DATA`.
