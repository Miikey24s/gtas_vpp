# VPP Pulse — Blazor UI Renovation Living Master Plan

> **Trạng thái:** `AWAITING OWNER APPROVAL`
>
> **Phiên bản:** `1.0` — 2026-07-19
>
> **Mục tiêu:** Nâng cấp toàn bộ UI/UX GTAS VPP trực tiếp trên Blazor/Radzen hiện tại, theo từng route có review, dùng dữ liệu TEST/isolated fixture thật và giữ nguyên nghiệp vụ.
>
> **Implementation authority:** Browser runtime + source hiện tại + `RouteCatalog.cs`.
>
> **Research/reference:** Personal Design DNA, VPP Pulse/Figma, PPJ-inspired operating values và các nguồn UI/data/accessibility chính thức.

---

## 1. Vì sao file này tồn tại

Chat và Figma không đủ làm nguồn handoff lâu dài cho AI agent. File này là living master plan để:

- giữ nguyên quyết định sản phẩm xuyên nhiều phiên làm việc;
- không để agent hiểu nhầm Figma là bản pixel-perfect cần chép sang Blazor;
- theo dõi đầy đủ route/tab/state hiện có trong source;
- ghi nhận điều người dùng duyệt, từ chối và lý do;
- lan truyền bài học sang route chưa làm hoặc retrofit route đã làm;
- định nghĩa QA gate và điều kiện commit cho từng vertical slice UI.

File này phải được cập nhật trong cùng change-set khi một quyết định UI toàn cục, route status hoặc feedback mới làm thay đổi kế hoạch.

---

## 2. Quan hệ với các tài liệu khác

- `docs/planning/06-LEAN-A-PLUS-EXECUTION-PLAN.md` vẫn là execution authority của release/nghiệp vụ.
- `docs/design/VPP-PULSE-DESIGN-BRIEF.md` giữ nghiên cứu art direction và Personal Design DNA.
- `docs/design/VPP-PULSE-PRODUCT-BLUEPRINT.md` giữ IA, data storytelling và screen inventory mở rộng.
- Figma `GTAS VPP — VPP Pulse` là tài liệu tham khảo flow/visual/state, không thay thế route/source audit.
- `.github/copilot-instructions.md` và `.codexrules` giữ convention Blazor/Radzen hiện hành.
- `gtas_vpp_fe/.../Helpers/RouteCatalog.cs` là nguồn danh sách logical route/tab để triển khai và QA.

Khi có xung đột:

1. Nghiệp vụ, authorization, API và schema hiện hành thắng thiết kế.
2. Quyết định người dùng mới nhất thắng preference cũ và phải được ghi vào file này.
3. Browser runtime đã duyệt thắng Figma cũ.
4. Accessibility và khả năng hoàn thành nhiệm vụ thắng sở thích thẩm mỹ.

---

## 3. Quyết định đã chốt

### 3.1 Workflow

- Không tạo UI Lab hoặc project preview riêng.
- Không chuyển/generate nguyên Figma thành code.
- Không rewrite framework, shell hoặc toàn frontend.
- Dùng UI Blazor hiện tại làm baseline và nâng cấp trực tiếp từng route.
- `dotnet watch` là vòng lặp local mặc định trong suốt quá trình UI; không chạy một bản build tĩnh để đánh giá thay đổi hằng ngày.
- Lệnh chuẩn là `.\scripts\gtas.cmd run`; script này khởi động Aspire AppHost bằng `dotnet watch`, giữ Hot Reload và các resource mapping hiện tại.
- Release build/test chỉ là gate trước review cuối, commit route và deploy; không thay thế browser review trong vòng lặp phát triển.
- Browser review là approval gate cuối về visual và interaction.
- Figma chỉ dùng khi cần so sánh phương án, minh họa flow hoặc lưu research.
- Mỗi route được sửa, QA, review và commit như một vertical slice nhỏ.

### 3.2 Dữ liệu và môi trường

- Primary review dùng DTO/API thật với `TEST_01` hoặc isolated LocalDB fixture.
- Không tạo model mock riêng khác `gtas_vpp_shared`.
- Edge case khó dựng được tạo bằng typed fixture/seed cùng DTO và validation contract thật.
- Không chạy mutation UI trên shared/prod database.
- Mutating E2E chỉ chạy khi có:

```powershell
$env:GTAS_E2E_ISOLATED = '1'
$env:GTAS_E2E_MUTATION_OPT_IN = 'I_UNDERSTAND_THIS_MUTATES_QA_DATA'
```

### 3.3 Rendering

- Giữ global `InteractiveServer` tại `App.razor`.
- Không thêm descendant `@rendermode`.
- Prerender-sensitive content dùng `RendererInfo.IsInteractive` và stable skeleton/state shell.
- Reconnect, error boundary và navigation phải giữ context, không tạo màn hình trắng.

### 3.4 Branding

- Product brand là GTAS VPP độc lập.
- PPJ chỉ là nguồn cảm hứng về vận hành/chất liệu; không dùng logo, tên, dữ liệu hoặc ảnh công ty khi chưa có quyền.
- Screenshot, seed, report và luận văn dùng dữ liệu ẩn danh hoặc deterministic QA data.

---

## 4. Art direction

### 4.1 Visual thesis

**VPP Pulse — Industrial Editorial Operations**

Một enterprise application chính xác và bình tĩnh, có typography biên tập đủ cá tính để tạo dấu ấn, nhưng form/table/permission vẫn rõ, ổn định và audit được.

### 4.2 Personal Design DNA được áp dụng

Nguồn local: `E:\WIN-MEDIA\Downloads\personal-design-dna-6449d951585344bfb5c342708c7a4c80.json`.

Các tín hiệu có độ tin cậy đáng kể:

- editorial typography có cá tính;
- oversized heading/con số ở vùng cần nhấn;
- density thiên về thoáng;
- lưới truyền thống hơn bento thử nghiệm;
- góc cạnh, radius nhỏ;
- border-first, shadow tối giản;
- màu lạnh, tương đối đơn sắc và ít gradient;
- motion tối giản;
- hình ảnh/motif trừu tượng hơn ảnh literal;
- accessibility và nghiệp vụ luôn cao hơn sở thích cá nhân.

### 4.3 PPJ-inspired operating DNA

Chỉ chuyển thành behavior, không chuyển thành branding:

| Giá trị | Biểu hiện UI |
|---|---|
| Flawless execution | Validation rõ, mutation có xác nhận, không mất context |
| Quality | Số liệu có đơn vị, trạng thái có text/icon, audit trail đọc được |
| On time | Deadline/SLA/age được ưu tiên trong dashboard và queue |
| Innovation | Data story, drill-down và micro-detail hữu ích thay vì trang trí |
| Compliance | Permission boundary, safe error, confirmation và evidence |
| Sustainability | Giảm thao tác lặp, hỗ trợ digital/print hợp lý, không tạo visual thừa |

### 4.4 Controlled wow

Được phép nổi bật ở:

- login/first impression;
- page heading và period headline;
- KPI/takeaway quan trọng;
- report narrative và reconciliation;
- empty/success state có giá trị hướng dẫn.

Phải tiết chế ở:

- DataGrid;
- CRUD form;
- permission matrix;
- settlement mutation;
- dialog xác nhận và error state.

Không dùng mặc định:

- glassmorphism;
- large radius/pill cho container;
- heavy shadow;
- gradient glow;
- animation spring/parallax;
- bento layout chỉ để tạo cảm giác hiện đại;
- chart không trả lời câu hỏi nghiệp vụ.

---

## 5. Foundations giữ làm source of truth

### Typography

- Poppins: display/page/section heading.
- Inter: body, form, grid, navigation.
- JetBrains Mono: request code, correlation, trace và technical identifier.
- Không dùng ALL CAPS dài cho nội dung chính.
- Oversized typography chỉ dùng cho takeaway/KPI, không dùng trong dense admin UI.

### Color

- Light mặc định: nền `#F7F8F9`, surface trắng, ink navy.
- Primary Sky Blue; Accent Teal.
- Semantic colors luôn đi cùng label/icon/pattern, không chỉ dùng màu.
- Dark và Print đổi semantic token, không override rải rác theo route.

### Shape/elevation

- Radius: 4px detail, 6px interactive, 8px container/dialog.
- Border là cách phân lớp chính.
- Shadow chỉ cho floating UI, dialog, popover hoặc selected focus hierarchy.

### Density

- Auth/empty/success: spacious.
- Dashboard: balanced.
- Grid/form/admin: compact-balanced, không thu nhỏ target/focus.

### Motion

- 150–200 ms cho state transition nhỏ.
- Không dùng motion để che loading chậm.
- Tôn trọng `prefers-reduced-motion`.

---

## 6. Current baseline: giữ gì và sửa gì

### Giữ

- Login split-screen và illustration hiện tại.
- Global shell, language/theme switch và Radzen foundation.
- Sky/Teal palette, Poppins/Inter và token namespace `--vpp-*`.
- Permission-aware navigation, API contract và business workflows.
- Server-side paging/filter/sort cho dataset lớn.

### Sửa dần

- Page hierarchy còn phẳng hoặc title/KPI quá nhỏ.
- Một số route có quá nhiều vùng trắng nhưng next action không rõ.
- Tab lồng nhau và collapsed icon navigation khó định hướng.
- Action primary/secondary/destructive chưa nhất quán.
- Mixed VI/EN trong content/error/data presentation.
- Empty state thiếu nguyên nhân, điều kiện và next action.
- Dense table thiếu inspector/drawer ở trường hợp nhiều cột.
- Dashboard/report chưa luôn đi từ takeaway tới evidence và action.
- Figma thiếu parity với route thật như Departments, Classes, Categories và Prices.

Không coi screenshot hiện tại hoặc Figma hiện tại là final; chúng chỉ là baseline và research evidence.

---

## 7. Data storytelling contract

### 7.1 Story sequence

```text
Takeaway
→ KPI có context
→ visual trả lời một câu hỏi
→ evidence/annotation
→ exact drill-down table
→ next action
```

### 7.2 Format selection

| Câu hỏi | Format mặc định |
|---|---|
| Giá trị hiện tại | KPI + label + comparison/context |
| Danh sách cần thao tác | Table/mobile card |
| Trend theo kỳ | Line hoặc column |
| So sánh phòng ban/sản phẩm | Sorted horizontal bar |
| Tỷ trọng trạng thái | Segmented/100% bar + counts |
| Exception/blocker | Ranked action list |
| Timeline nghiệp vụ | Timeline + actor/time/reason |
| Supplier/price comparison | Ranked exact table + bullet bar |
| Workflow state | Stepper/state rail |
| Permission/configuration | Matrix/table/switch |

### 7.3 Guardrails

- Không dùng donut/pie mặc định.
- Không dual axis mặc định.
- Không giấu insight quan trọng chỉ trong tooltip.
- Chart phải có title rõ, unit, source/filter context và accessible table/drill-down.
- AI narrative chỉ xuất hiện sau deterministic KPI/chart và phải có evidence/fallback label.

---

## 8. Implementation waves

| Wave | Phạm vi | Gate trước khi sang wave tiếp |
|---|---|---|
| W0 | Shared foundations/components | Token/action/state/grid contract được browser-QA |
| W1 | Shell + Auth/System | Anonymous/account/reconnect/error flow ổn định |
| W2 | Employee workspace | Create/edit/cancel/history core journey pass |
| W3 | Management | Department/all-company scope và approval rõ |
| W4 | Procurement/Period | Preview/exception/settlement/reconciliation đúng |
| W5 | Library | Toàn bộ master-data route dùng pattern chung |
| W6 | Access Control | Account/RBAC boundary và audit rõ |
| W7 | Reports/AI states | Data story, export, print, fallback/evidence rõ |
| W8 | Cross-route hardening | VI/EN, Dark/Print, responsive, a11y, perf, final regression |

Thứ tự ưu tiên trong cùng plan:

1. Login + Shell.
2. My Orders.
3. Create/Edit Request.
4. Department/All Orders.
5. Period Operations/Settlement.
6. Report.
7. Library.
8. Permission.
9. Secondary account/system routes.
10. Cross-route Dark/Print/mobile polish.

Full scope không bị cắt; thứ tự chỉ bảo vệ các route có giá trị demo/luận văn cao nhất trước.

---

## 9. Route ledger

Status hợp lệ:

- `PENDING`
- `BASELINE_CAPTURED`
- `IN_IMPLEMENTATION`
- `OWNER_REVIEW`
- `CHANGES_REQUESTED`
- `APPROVED`
- `VERIFIED`
- `RETROFIT_REQUIRED`

### W0 — Shared foundations

| Target | Status | Scope |
|---|---|---|
| Tokens/themes/print | PENDING | Semantic token, Light/Dark/Print, contrast |
| Page header/action hierarchy | PENDING | `VppPageHeader`, action placement, breadcrumbs |
| Status/state primitives | PENDING | Badge, loading, empty, error, success, 403 |
| DataGrid/admin pattern | PENDING | Toolbar, paging, column picker, inspector, mobile card |
| Dialog/form/notification | PENDING | Validation, focus, confirmation, retry, durable feedback |

### W1 — Shell + Auth/System

| Route/state | Status | Notes |
|---|---|---|
| `/` redirect | PENDING | First accessible route, no blank flash |
| `/Account/Login` | PENDING | Keep illustration; safe localized feedback |
| `/Account/Register` | PENDING | Validation + pending approval |
| `/Account/ConfirmEmail` | PENDING | Success/expired/invalid |
| `/Account/ForgotPassword` | PENDING | Anti-enumeration |
| `/Account/ResetPassword` | PENDING | Policy/expired/replay |
| `/Account/ChangePassword` | PENDING | Current/new/confirm |
| `/loginprocess` | PENDING | Progress/fallback only |
| `/logoutprocess` | PENDING | Safe clear + redirect login |
| `/Error` | PENDING | Safe message + correlation + retry |
| `/not-found` | PENDING | Return to valid workspace |
| Shell/notification/reconnect | PENDING | Context preservation, action inbox |

### W2 — Employee

| Logical route | Status | Notes |
|---|---|---|
| `dashboard.my-orders` | PENDING | Period story, quota, next action |
| `dashboard.history` | PENDING | Timeline/revision/detail |
| `dashboard.catalog` | PENDING | Browse/search/read-only detail |
| `dashboard.order-create.new` | PENDING | Select → review → submit |
| `dashboard.order-create.edit` | PENDING | Update + stale/permission guard |
| Copy previous | PENDING | Diff and source context |
| Additional request | PENDING | Reason/quota/current attempt |

### W3 — Management

| Logical route/state | Status | Notes |
|---|---|---|
| `dashboard.management.department` | PENDING | Department scope + queue |
| `dashboard.management.all` | PENDING | Company scope + drill-down |
| Supplement approval | PENDING | Base/diff/reason/quota/evidence |
| Reject dialog | PENDING | Required reason + consequence |

### W4 — Procurement/Period

| Logical route/state | Status | Notes |
|---|---|---|
| `dashboard.period-operations` | PENDING | Permission-dependent sub-tabs |
| Period review | PENDING | KPI + server table + blockers |
| Additional approval queue | PENDING | Age/SLA/action priority |
| Supplier/price comparison | PENDING | Exact ranked comparison |
| Settlement preview | PENDING | Exceptions/evidence/hash |
| Confirm settlement | PENDING | Immutable snapshot warning |
| Settled/revision view | PENDING | Reconciliation + audit timeline |

### W5 — Library

| Logical route | Status | Notes |
|---|---|---|
| `library.classes` | PENDING | Two-pane lookup hierarchy |
| `library.categories` | PENDING | Server grid + inspector |
| `library.items` | PENDING | Product fields/filter/active state |
| `library.suppliers` | PENDING | Contact/active/audit |
| `library.departments` | PENDING | Missing dedicated Figma screen; membership impact |
| `library.pricing.price-lists` | PENDING | Lifecycle/detail drawer |
| `library.pricing.prices` | PENDING | Product/list/supplier/version filter |

### W6 — Access Control

| Logical route/state | Status | Notes |
|---|---|---|
| `permission.user` | PENDING | Activation/mapping/session |
| `permission.component` | PENDING | Flat groups + page/component matrix |
| User inspector | PENDING | Identity/membership/audit |
| Reset/revoke actions | PENDING | Confirm + durable feedback |

### W7 — Reports/AI states

| Logical route/state | Status | Notes |
|---|---|---|
| `report` overview | PENDING | Takeaway + KPI + exact table |
| Trend/status story | PENDING | One question per visual |
| Department/product story | PENDING | Ranked bars + drill-down |
| Settlement reconciliation | PENDING | Variance/evidence |
| Export/Print | PENDING | Loading/complete/limit/error/A4 |
| AI insight states | PENDING | Disabled by default; evidence/fallback/governance |

### W8 — Global hardening

| Target | Status | Notes |
|---|---|---|
| Light/Dark consistency | PENDING | Semantic tokens, no route override drift |
| Print mode | PENDING | Monochrome, page-break, hide nav/actions |
| VI/EN | PENDING | Resource strings; user data remains original |
| Mobile/tablet | PENDING | 390/768 without desktop shrink-only pattern |
| Accessibility | PENDING | Keyboard/focus/labels/axe |
| Performance | PENDING | Paging/LoadData/render count |
| Final visual regression | PENDING | Approved baselines + browser inspection |

---

## 10. Route execution loop

Mỗi route đi qua đúng thứ tự:

1. Kiểm tra `git status` và thay đổi người dùng.
2. Đọc route source, DTO/API, permission, test và pattern tương tự.
3. Chụp baseline bằng data/role phù hợp.
4. Ghi route brief:
   - persona;
   - user question/task;
   - data fields;
   - primary/secondary/destructive actions;
   - permission boundary;
   - loading/empty/error/success states;
   - responsive/print requirement.
5. Đánh dấu ledger `BASELINE_CAPTURED` rồi `IN_IMPLEMENTATION`.
6. Sửa trực tiếp route/shared component tối thiểu cần thiết.
7. Chạy targeted build/test và browser QA.
8. Đánh dấu `OWNER_REVIEW` và trình người dùng:
   - before/after;
   - các quyết định visual/data/action;
   - state đã kiểm tra;
   - rủi ro/retrofit có thể phát sinh.
9. Nếu người dùng duyệt:
   - `APPROVED` → full relevant QA → `VERIFIED` → commit.
10. Nếu người dùng từ chối:
    - `CHANGES_REQUESTED`;
    - ghi feedback/learning log;
    - phân loại local/global;
    - cập nhật plan và retrofit queue trước khi sửa lại.

Không xử lý hàng loạt nhiều route rồi mới xin duyệt nếu thay đổi visual lớn chưa có precedent được duyệt.

---

## 11. Feedback learning protocol

### Phân loại feedback

| Loại | Ví dụ | Hành động |
|---|---|---|
| Local route | Report cần KPI khác Library | Chỉ sửa route/feature |
| Shared component | Button quá cao ở mọi trang | Sửa primitive và audit consumers |
| Global visual rule | User thích border hơn card shadow | Cập nhật art direction/tokens |
| Content rule | Label khó hiểu/mixed language | Cập nhật localization/content pattern |
| Data rule | Thiếu field quyết định | Audit API/DTO trước; không tự đổi nghiệp vụ |
| Interaction rule | Drawer làm mất context | Cập nhật pattern và retrofit nơi tương tự |
| Accessibility rule | Focus/keyboard kém | Sửa ngay mọi consumer bị ảnh hưởng |

### Decision and learning log

| Date | Route/component | Decision/feedback | Why | Local/Global | Plan change | Retrofit targets | Status |
|---|---|---|---|---|---|---|---|
| 2026-07-19 | Workflow | Không dùng UI Lab; code trực tiếp từng route | UI thật đã tồn tại và đẹp hơn Figma prototype | Global | Living plan này | N/A | Recorded |
| 2026-07-19 | Design authority | Browser runtime thắng Figma | Tránh design/code drift và route coverage thiếu | Global | Figma chuyển thành reference | Toàn bộ route | Recorded |

### Retrofit queue

| Priority | Source feedback | Target route/component | Required change | Status |
|---|---|---|---|---|
| — | — | — | — | Empty |

Retrofit không mặc định làm ngay giữa route hiện tại nếu không ảnh hưởng correctness/accessibility. Agent phải ghi queue và đề xuất thời điểm xử lý để tránh scope explosion.

---

## 12. QA gates

### Per-route browser matrix

- Desktop: `1920×1080`.
- Tablet: `768×1024`.
- Mobile: `390×844`.
- Light và Dark cho route authenticated.
- Print cho report/detail/evidence route phù hợp.
- VI mặc định; EN stress cho label dài và layout-sensitive state.

### State/data matrix

- prerender/loading;
- empty;
- normal;
- long text/large values;
- paged data;
- validation/conflict;
- unauthorized/disabled;
- retryable error;
- success/durable feedback;
- reconnect/session expiry khi liên quan.

### Technical gates

- Không horizontal page overflow.
- Keyboard/focus order hợp lý.
- Không visible control thiếu accessible name.
- Axe không critical/serious hoặc exception được ghi rõ.
- Không unexpected console error hoặc failed API.
- Không raw exception/JSON trong user notification.
- Authorization đúng cả UI và direct API.
- Mutation không double-submit và retry idempotent khi cần.
- DataGrid lớn dùng server paging/`LoadData`; virtualization chỉ sau benchmark.

### Commands tối thiểu theo scope

```powershell
dotnet build gtas_vpp.sln -c Release
dotnet test gtas_vpp_fe.Tests/gtas_vpp_fe.Tests.csproj -c Release
```

Thay đổi shared/backend contract phải chạy thêm test liên quan và full solution gate theo `AGENTS.md`.

---

## 13. Git/commit protocol

- Một commit logic cho một route hoặc shared primitive được duyệt.
- Không trộn route chưa duyệt với route đã hoàn tất.
- Không stage file người dùng ngoài scope.
- Screenshot/trace tự động lưu vào ignored temp/evidence folder, không commit mặc định.
- Trước commit:

```powershell
git diff --check
git status --short
```

- Không push/merge/deploy nếu người dùng chưa yêu cầu.

---

## 14. Owner approval gate cho version 1.0

Các quyết định chờ người dùng duyệt:

- [ ] Browser runtime là nguồn visual cuối; Figma chỉ hỗ trợ.
- [ ] Giữ login illustration và visual baseline hiện tại, chỉ polish có kiểm soát.
- [ ] Controlled wow: mạnh ở heading/data story, tiết chế ở table/form/permission.
- [ ] Desktop-first theo từng route nhưng mọi route phải không vỡ tablet/mobile.
- [ ] Thứ tự thực thi W0 → W1 → W2 → W3 → W4 → W7 → W5 → W6 → W8 được chấp nhận.

Chưa bắt đầu implementation trước khi các quyết định trên được duyệt hoặc chỉnh lại trong file này.

---

## 15. Research references

- Personal Design DNA export — local, 2026-07-18.
- [PPJ International — Vision and Mission](https://www.ppj-international.com/vision-mission.html)
- [PPJ International — Sustainability](https://www.ppj-international.com/sustainability.html)
- [Atlassian Design — Foundations](https://atlassian.design/foundations)
- [Atlassian Design — Design Tokens](https://atlassian.design/tokens/design-tokens)
- [Carbon Design System — Dashboards](https://carbondesignsystem.com/data-visualization/dashboards/)
- [Tableau Blueprint — Visual Best Practices](https://help.tableau.com/current/blueprint/en-us/bp_visual_best_practices.htm)
- [Microsoft Learn — Accessible Power BI Reports](https://learn.microsoft.com/en-us/power-bi/create-reports/desktop-accessibility-creating-reports)
- [W3C — WCAG 2.2](https://www.w3.org/TR/WCAG22/)
- [Radzen Blazor DataGrid](https://blazor.radzen.com/datagrid?theme=default&wcag=true)
- [Radzen DataGrid Performance](https://blazor.radzen.com/datagrid-performance)
- [MDN — prefers-reduced-motion](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/%40media/prefers-reduced-motion)

---

## 16. Continuation protocol cho AI agent

Khi tiếp tục UI renovation trong thread/session mới:

1. Đọc `AGENTS.md` và mọi `AGENTS.md` gần scope.
2. Đọc toàn bộ file này.
3. Đọc `.codexrules`, `.github/copilot-instructions.md` và route source.
4. Kiểm tra `git status`, branch và diff chưa commit.
5. Đọc ledger, feedback log và retrofit queue mới nhất.
6. Chọn đúng route `PENDING`/`CHANGES_REQUESTED` theo thứ tự đã duyệt.
7. Không suy luận rằng Figma đã cover đủ route.
8. Không tạo UI Lab, project preview hay architecture render mode khác.
9. Không thay đổi API/DB/nghiệp vụ chỉ để đạt visual.
10. Cập nhật file này trước khi báo route hoàn tất.

### 10.1 Local development loop bằng dotnet-watch

Mỗi phiên làm UI bắt đầu như sau:

```powershell
cd D:\WORK\gtas_vpp
.\scripts\gtas.cmd status
.\scripts\gtas.cmd run
```

Sau khi Aspire khởi động:

1. Mở GTAS frontend từ resource/dashboard URL, không nhầm Aspire dashboard login URL với login của GTAS.
2. Chọn database TEST đã cấu hình và role phù hợp.
3. Mở route đang làm ở browser.
4. Sửa `.razor`/`.razor.cs`/CSS trong branch hiện tại; để `dotnet watch` tự rebuild/Hot Reload.
5. Kiểm tra browser console, network, reconnect và state sau mỗi thay đổi lớn.
6. Nếu watch báo lỗi Razor generator/hint name hoặc state incremental bất thường, restart watch rồi chạy clean Release build; không kết luận lỗi sản phẩm chỉ từ watch state.

Trong vòng lặp watch không chạy migration/mutation tùy tiện. Dữ liệu phải đến từ TEST/isolated fixture đã chuẩn bị trước; các test mutation vẫn cần explicit opt-in theo Section 3.2.

### 10.2 Bước đầu tiên của renovation

**W0.0 — Baseline capture, không sửa code:**

- Chạy `.\scripts\gtas.cmd run`.
- Kiểm tra shell, login, theme/language switch và resource frontend.
- Chụp baseline route ưu tiên ở `1920×1080`, sau đó spot-check `768×1024` và `390×844`.
- Ghi đúng dữ liệu/role/permission/state đã dùng vào route ledger.
- Đánh dấu issue theo ba nhóm: correctness, usability, visual polish.
- Chỉ sau khi baseline được lưu mới bắt đầu W0 shared foundation hoặc route đầu tiên.

Baseline là bằng chứng so sánh; không được sửa screenshot để khớp thiết kế, không được xóa baseline vì route mới trông khác.

Prompt tiếp tục ngắn:

```text
Tiếp tục VPP Pulse Blazor UI renovation theo
docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md.
Đọc ledger/learning/retrofit hiện tại, kiểm tra git status và làm đúng route tiếp theo.
Browser runtime là visual authority; không dùng Figma làm pixel source và không tạo UI Lab.
```
