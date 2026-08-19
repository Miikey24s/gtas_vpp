# VPP Pulse — Blazor UI Renovation Living Master Plan

> **Trạng thái:** `ACTIVE — BLAZOR/RADZEN AUTHORITY; REACT POC ARCHIVED`
>
> **Phiên bản:** `3.04` — 2026-08-10
>
> **Mục tiêu:** Làm nguồn thực thi ưu tiên cho frontend Blazor/Radzen. React POC cũ được bảo toàn bằng archive tag, không còn nằm trong source hoạt động.
>
> **Implementation authority:** Blazor/Radzen là implementation authority hiện tại. Chỉ khôi phục hoặc tạo lại React khi owner có quyết định mới.
>
> **Research/reference:** OpenAI/ChatGPT UI guidelines là art-direction research chính; VPP Pulse/Figma và Atlas giữ context bố cục/nghiệp vụ; Microsoft/Radzen/W3C quyết định implementation và accessibility.

---

## 0. Bản một ánh nhìn — UI system scalable

> **Trạng thái:** `F0–F7 + FRONTEND REFACTOR COMPLETE; OWNER FINAL VISUAL ACCEPTANCE APPROVED 2026-08-04; GOLDEN DEFERRED`.

| Câu hỏi | Quyết định ngắn gọn | Xem chi tiết |
|---|---|---|
| Muốn đạt gì? | Một UI Blazor/Radzen dễ đọc, dễ tùy biến và đủ ổn định để AI agent mở rộng mà không tạo thêm component “vạn năng”. | [Mục tiêu và phạm vi](../execution/UI-SYSTEM-001.md#1-mục-tiêu-và-phạm-vi) |
| Xây theo kiểu nào? | Hybrid: Razor/HTML sở hữu layout; Radzen sở hữu widget phức tạp; tái sử dụng theo `token → primitive → composite → pattern → route`. | [Kiến trúc đích](../execution/UI-SYSTEM-001.md#3-kiến-trúc-đích) |
| Làm theo thứ tự nào? | F0 khóa baseline → F1 token/bridge → F2 primitive/state → F3 composite → F4 pattern → F5 M0–M2 → F6 M3–M8 → F7 hardening. | [Các wave F0–F7](../execution/UI-SYSTEM-001.md#5-kế-hoạch-thực-thi-f0f7) |
| Dùng model nào? | Bảng F0–F7 giữ routing lịch sử riêng của `UI-SYSTEM-001`; plan mới phải dùng authority/quota guidance hiện hành trong execution record của chính nó. | [Frontend refactor routing](../execution/FRONTEND-REFACTOR-001.md#plan-detail-routing) |
| Sau mỗi wave có gì? | F0–F4 hình thành khung; F5 hoàn chỉnh nhóm màn M0–M2; F6 đưa M3–M8 lên khung; F7 harden và đóng `UI-SYSTEM-001`. | [Bảng wave canonical](../execution/UI-SYSTEM-001.md#5-kế-hoạch-thực-thi-f0f7) |
| Duyệt trực quan thế nào? | Mỗi wave có một `Wave Review Board` nhìn trong một màn hình; dùng screenshot runtime, contact sheet, state board hoặc diagram đúng bản chất wave, kèm trace ngắn khi interaction quan trọng. | [Visual review contract](../execution/UI-SYSTEM-001.md#51-visual-review-contract) |
| Bước code hiện tại? | F0–F7, DS0–DS4, R1 và `FRONTEND-REFACTOR-001` đã hoàn tất; current runtime + final board đã được owner chấp thuận. Backend refactor là phase đang mở. | [Frontend refactor continuation](../execution/FRONTEND-REFACTOR-001.md#plan-detail-continuation) |
| Kiểm tra bằng gì? | Build/test chỉ là gate code; nghiệm thu cuối trên route Blazor thật ở 4 viewport, VI/EN, Light/Dark, state, console/network và accessibility. | [Validation](../execution/UI-SYSTEM-001.md#6-validation-và-definition-of-done) |
| Rủi ro chính? | CSS override chồng chéo, abstraction quá sớm, component reflection, refactor big-bang và làm lệch nghiệp vụ. Tất cả đều có gate/migration nhỏ để hoàn tác được. | [Rủi ro và recovery](../execution/UI-SYSTEM-001.md#8-rủi-ro-và-recovery) |
| Cần owner duyệt gì? | Final review board đã được chấp thuận ngày 2026-08-04. Golden/contact sheet và ảnh luận văn/slide sẽ khóa sau khi visual tái tạo được từ clean HEAD. | [Owner acceptance record](../execution/UI-SYSTEM-001.md#9-owner-approval-gate) |

Execution record chi tiết: [`UI-SYSTEM-001`](../execution/UI-SYSTEM-001.md).

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
- `docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md` định nghĩa MCP, browser QA, accessibility, visual regression và performance workflow cho AI agent.
- `docs/design/VPP-UI-CSS-OWNERSHIP.md` định nghĩa authority của token, Radzen bridge, shared, feature, legacy và vendor CSS.
- `docs/execution/UI-SYSTEM-001.md` là execution record chi tiết cho đợt chuẩn hóa UI system scalable sau Atlas.
- `docs/execution/FRONTEND-REFACTOR-001.md` là execution authority cho module ownership, API/state,
  naming/comment, cleanup và folder/file sau khi UI system đã hoàn tất implementation.
- Figma `GTAS VPP — VPP Pulse` là tài liệu tham khảo flow/visual/state, không thay thế route/source audit.
- `src/Frontend/Blazor/AGENTS.md`, UI repo skill và `.github/instructions/frontend.instructions.md` giữ convention Blazor/Radzen hiện hành.
- `src/Frontend/Blazor/Helpers/RouteCatalog.cs` là nguồn danh sách logical route/tab để triển khai và QA.

Khi có xung đột:

1. Nghiệp vụ, authorization, API và schema hiện hành thắng thiết kế.
2. Quyết định người dùng mới nhất thắng preference cũ và phải được ghi vào file này.
3. Browser runtime đã duyệt thắng Figma cũ.
4. Accessibility và khả năng hoàn thành nhiệm vụ thắng sở thích thẩm mỹ.

---

## 3. Quyết định đã chốt

### 3.0 Quyết định chuyển ưu tiên — 2026-07-21; cập nhật lưu trữ — 2026-07-27

- Owner tạm ngưng React vì sắp tới đợt deadline và tiếp tục hoàn thiện frontend Blazor/Radzen hiện hành.
- Quyết định “không xóa React POC” ngày 2026-07-21 đã được owner thay thế ngày 2026-07-27 bằng yêu cầu dọn sâu repository.
- Source React POC được bảo toàn ở tag `archive/react-poc-2026-07-27` và bản ZIP phục hồi ngoài repository; dependency Playwright dùng chung đã chuyển vào `scripts/browser/`.
- Mọi UI work tiếp theo phải sửa trực tiếp `src/Frontend/Blazor`, dùng API/DTO và database TEST hoặc isolated fixture thật.
- Thứ tự hiện tại: Atlas `ATLAS-001` hoàn tất → F0/F1 đã tích hợp → F2 qua implementation + independent review/fix → F3 owner-approved → F4 implemented/tested, chờ owner review trước F5.
- Khi deadline qua hoặc owner yêu cầu quay lại React, kế hoạch React được mở lại bằng một quyết định riêng; không tự động cutover.

**Bằng chứng kích hoạt lại Blazor/Radzen — 2026-07-21:**

- Radzen MCP đã được kiểm tra và còn hoạt động; quy tắc hard-stop khi hết quota/key lỗi vẫn giữ nguyên.
- Sau rollback round 6: `dotnet build gtas_vpp.sln -c Release --no-restore` pass `0 warning / 0 error`; frontend tests `143/143` và backend tests `410/410` pass.
- Blazor isolated browser harness đã bổ sung runtime dependency `Aspire.Hosting.JavaScript` do AppHost vẫn giữ React resource ở trạng thái paused; `DashboardMyOrdersVisualTests` pass trên Aspire/LocalDB cô lập ở `390×844`, `768×1024`, `1366×768`, `1920×1080`, gồm kiểm tra VI/EN, overflow, 4 evidence và single-source CTA/archive.
- Ảnh evidence mới nhất của isolated fixture là trạng thái empty; trạng thái submitted/non-empty với demo data TEST vẫn phải được owner mở trên `dotnet-watch` để duyệt trực quan.
- W1 vẫn giữ `OWNER_REVIEW` cho đến khi owner duyệt trực quan trên phiên TEST đã đăng nhập; build/test không thay thế browser approval.

### 3.1 Workflow

- Không tạo UI Lab hoặc project preview riêng.
- Không chuyển/generate nguyên Figma thành code.
- Không rewrite framework hoặc toàn frontend; tiếp tục giữ global Blazor `InteractiveServer`.
- Shared shell, navigation và tab được phép chuyển từ Radzen sang native Razor/HTML/SVG khi cần DOM ổn định, CSS isolation và visual control. Radzen chỉ giữ ở component phức tạp tạo giá trị rõ như DataGrid, Dialog, DatePicker và form CRUD.
- Dùng UI Blazor hiện tại làm baseline nghiệp vụ, nhưng không bắt buộc kế thừa markup hoặc visual Radzen đã bị owner từ chối.
- `dotnet watch` là vòng lặp local mặc định trong suốt quá trình UI; không chạy một bản build tĩnh để đánh giá thay đổi hằng ngày.
- Lệnh chuẩn là `.\scripts\gtas.cmd run`; script này khởi động Aspire AppHost bằng `dotnet watch`, giữ Hot Reload và các resource mapping hiện tại.
- Release build/test chỉ là gate trước review cuối, commit route và deploy; không thay thế browser review trong vòng lặp phát triển.
- Browser review là approval gate cuối về visual và interaction.
- Figma chỉ dùng khi cần so sánh phương án, minh họa flow hoặc lưu research.
- Khi Figma có Import GitHub/Code on Canvas, import toàn repository để đọc đúng Blazor, shared DTO, route catalog và living plan; không chọn `scripts/browser` làm frontend target.
- Figma không chạy/ship Blazor thay Codex. Nếu cần React code layer để dựng preview, output đó chỉ là prototype thiết kế cô lập; sau owner review, implementation thật vẫn được viết và QA trong `src/Frontend/Blazor`.
- Toolchain phải đi theo vai trò: OpenAI Developer UI guidance cho hierarchy/clarity/spacing/feedback, Microsoft Learn/Radzen cho framework/component, Playwright cho route/DOM/ARIA, Chrome DevTools cho debug/performance, axe cho accessibility và Figma cho design context khi cần.
- Nếu Radzen MCP hết quota hoặc key không hoạt động, dừng toàn bộ công việc và chờ owner cung cấp key mới.
- Mỗi route được sửa, QA, review và commit như một vertical slice nhỏ.

### 3.1.1 AI-first scalable UI architecture — OWNER-DIRECTED (2026-07-28)

**Decision `D-AI-UI-01`:** M0–M2 Atlas đã được owner chuẩn hóa là style contract cho các wave tiếp theo. Frontend tiếp tục là Blazor + Radzen theo mô hình hybrid; mục tiêu là một khung có thể scale và tùy biến mà agent mới đọc được theo progressive disclosure (chỉ nạp chi tiết khi cần), không phải một “universal component framework”.

| Lớp | Sở hữu | Ví dụ |
|---|---|---|
| Design token | Giá trị visual dùng chung | màu, spacing, type scale, radius, shadow, control height, motion |
| Primitive | Một hành vi/visual nhỏ, ổn định | button, badge, field, surface, content state, icon action |
| Composite | Cụm UI có mục đích rõ | filter bar, action bar, KPI strip, entity header, order-detail surface |
| Workspace pattern | Bố cục và state composition có thể tái dùng | Account, Collection, ListDetail, SplitEditor, Operation, Analytics |
| Route | Nghiệp vụ và tích hợp | API, permission, orchestration, route copy và ngoại lệ đã được duyệt |

Quy tắc bắt buộc:

1. Dùng composition thay vì kế thừa markup. Không tạo `UniversalPage<T>`, `UniversalGrid<T>` hoặc engine cấu hình bằng string.
2. Chỉ trích xuất abstraction sau khi ít nhất hai route thật có cùng layout và behavior; route vẫn sở hữu API, permission và business state.
3. Razor/HTML sở hữu shell, navigation, surface, toolbar, action bar, responsive layout và content state. Radzen giữ DataGrid, Dialog, DropDown, DatePicker, Numeric, validation và widget phức tạp tạo giá trị rõ.
4. Không bọc toàn bộ Radzen. Chuẩn hóa qua `vpp-tokens.css`, `vpp-radzen-theme.css`, primitive/composite có mục đích và CSS isolation của component/route.
5. Radzen base CSS phải tải trước project overrides. Không tăng thêm inline style, màu hex, pixel spacing hoặc `!important` nếu token/bridge giải quyết được.
6. Một vertical slice phải hoàn tất route thật, state, responsive, accessibility, VI/EN, Light/Dark và browser evidence phù hợp trước khi nhân rộng pattern.

Rollout khung dùng chung:

| Wave | Kết quả |
|---|---|
| F0 | Inventory component/CSS/inline style và xác nhận CSS load order |
| F1 | Chuẩn hóa token semantic và Radzen bridge |
| F2 | Primitive + content-state foundation |
| F3 | Composite dùng chung theo hai consumer thật |
| F4 | Sáu workspace pattern có contract nhỏ, typed và tùy biến bằng slot |
| F5 | Migrate M0–M2 làm reference implementation; xóa duplication đã được thay thế |
| F6 | Mở M3–M8 theo vertical slice và route ledger |
| F7 | Browser hardening, accessibility, performance và owner-approved visual baseline |

Definition of done của architecture này: agent mới xác định đúng authority bằng `AGENTS.md` gần nhất, dùng repo skill `.agents/skills/gtas-vpp-ui-system/`, không tạo abstraction trước nhu cầu, và chứng minh UI trên Blazor runtime thay vì chỉ dựa vào build/screenshot.

### 3.1.2 Admin filtering contract — OWNER-APPROVED (2026-07-30)

Phương án A được owner duyệt: admin và permission dùng toolbar-first filtering. `VppDataToolbar` là nguồn lọc chính; header cột chỉ dùng sort; điều kiện hiếm/nhiều trường mở từ `FILTER-ADVANCED`; `VppColumnPicker` chỉ điều chỉnh cột. Radzen `FilterMode.CheckBoxList` không còn là motif mặc định của admin grid. Migration đi theo slice `Class/Lookup → Library collections → Pricing → Permission`; backend distinct endpoints chỉ được retire sau khi consumer ledger xác nhận không còn route dùng.

### 3.1.3 Frontend React — ARCHIVED/DEFERRED (cập nhật 2026-07-27)

- React POC từng nằm tại `gtas_vpp_fe_react`; source lịch sử hiện được lưu ở tag `archive/react-poc-2026-07-27`.
- Các nội dung bên dưới là hồ sơ kỹ thuật và bằng chứng đã làm, không phải phạm vi triển khai của giai đoạn deadline hiện tại.
- Không thực hiện thêm thay đổi, QA hoặc migration route React cho đến khi owner mở lại phạm vi này.
- Không đổi tên, ghi đè hoặc xóa frontend Blazor. Blazor vẫn là bản luận văn/runtime authority cho đến khi React đạt route parity và owner duyệt cutover rõ ràng.
- Stack nền: React + TypeScript + Vite, shadcn/ui + Tailwind CSS, React Router, TanStack Query/Table, React Hook Form + Zod, i18next, Lucide và Recharts.
- Không dùng Next.js cho giai đoạn này: GTAS là application nội bộ, backend ASP.NET Core/JWT đã tách riêng và không cần SEO/React Server Components hoặc thêm một Node production server.
- TypeScript contract phải sinh từ Swagger/OpenAPI của backend; không tự chép DTO C# bằng tay và không tạo contract nghiệp vụ song song.
- React POC không còn trong source hoạt động, AppHost, production runtime hoặc CI/CD. Dependency host cùng tên chỉ giữ Playwright cho tooling LVTN cũ.
- Proof-of-concept đầu tiên là `Login → App shell → My Orders` với API/TEST thật, đủ VI/EN, Light/Dark/Print, responsive, loading/empty/error/success, permissions và accessibility.
- Chỉ bắt đầu migrate route tiếp theo khi proof-of-concept được chứng minh tốt hơn Blazor bằng runtime review và test, không dựa vào mock screenshot.

**React foundation evidence — 2026-07-20:**

- React POC lịch sử đã được scaffold bằng React 19 + TypeScript 6 + Vite 8; bằng chứng nằm trong archive tag, không phải project đang chạy.
- Foundation đã có React Router lazy routes, TanStack Query provider, i18next VI/EN, Light/Dark/Print tokens, shadcn/ui source components, responsive shell và error/not-found boundary.
- OpenAPI client dùng `@hey-api/openapi-ts`; URL Swagger lấy từ `GTAS_OPENAPI_URL`, còn runtime `/api` dùng Aspire service discovery/proxy hoặc `.env.local` khi chạy Vite độc lập.
- `src/Hosting/AppHost` chỉ điều phối backend và Blazor/Radzen. React POC đã được gỡ khỏi AppHost, Docker Compose production và CI/CD ngày 2026-07-27.
- shadcn MCP đã được cài vào Codex user profile; cần restart Codex hoặc mở task mới để tool xuất hiện trong phiên.
- Vertical slice `Login → App shell → My Orders → Logout` đã dùng generated client từ Swagger, auth/permission bootstrap thật, API query thật và không tạo DTO nghiệp vụ song song.
- QA tự động đã pass: Prettier, Oxlint, TypeScript, Vitest, Vite production build, Playwright Chromium ở `390`, `768`, `1366` px, axe không có violation critical/serious, không horizontal overflow, `npm audit` không có vulnerability, AppHost Release build `0 warning / 0 error` và backend `397/397` test pass.
- Playwright chỉ dùng contract fixture để phủ loading/auth/layout/a11y một cách cô lập. Owner vẫn phải chạy Aspire với database TEST và đăng nhập tài khoản thật trước khi duyệt visual/runtime hoặc cân nhắc route tiếp theo.

**React proof-of-concept ledger:**

| Route/state | Status | Contract và phạm vi |
|---|---|---|
| `/login` | OWNER_REVIEW — TEST runtime pending | `/api/Auth/login`; inline validation, safe credential error, VI/EN, password visibility, return URL nội bộ |
| Protected auth bootstrap | OWNER_REVIEW — TEST runtime pending | `/api/Auth/me` + `/api/Auth/me/permissions`; hết hạn/401 xóa session và quay về login |
| `/app/orders` | OWNER_REVIEW — TEST runtime pending | App shell + `/api/VPPRequest/period-info` + `/api/VPPRequest/my-orders`; permission `REQUEST_VIEW_OWN` |
| Logout | OWNER_REVIEW — TEST runtime pending | `/api/Auth/logout`; luôn dọn client session và quay về `/login` kể cả backend tạm unavailable |

Quyết định auth cho POC:

- Backend hiện tại chỉ cung cấp JWT qua username/password; chưa có BFF/cookie contract cho React.
- POC dùng bearer token trong `sessionStorage` để giữ phiên khi reload tab, không dùng `localStorage` và không ghi token vào log/error/telemetry.
- Đây là giới hạn chỉ dành cho local/TEST runtime review. Production cutover bị chặn cho đến khi có BFF hoặc secure HttpOnly cookie session, vì browser storage vẫn có rủi ro khi ứng dụng bị XSS.
- React không tự giải mã JWT để quyết định quyền; user/permission state luôn lấy từ endpoint backend và API vẫn là authorization authority.
- Swagger snapshot được xuất trực tiếp từ backend với `DatabaseInitialization=None`; response annotations được bổ sung chỉ để sinh typed client, không đổi runtime behavior.

### 3.1.4 Frontend readability refactor timing — OWNER-APPROVED (2026-08-02)

- Owner cho phép bắt đầu `FRONTEND-REFACTOR-001` trước final visual acceptance và chấp nhận chỉnh/refactor
  tiếp nếu correction UI xuất hiện sau đó.
- UI runtime hiện tại là provisional baseline; `UI-SYSTEM-001`, motif catalog và browser Blazor thật
  tiếp tục quyết định visual/interaction.
- Refactor source không được trộn với redesign trong cùng slice. Correction UI mới phải có evidence
  riêng rồi structural follow-up cập nhật phần bị ảnh hưởng.
- Owner đã chấp thuận current runtime + final board ngày 2026-08-04. Golden baseline, ảnh luận văn cuối
  và screenshot slide cuối được hoãn đến clean reproducible HEAD và phase chốt luận văn/slide; raw
  English reason là localization backlog riêng.

### 3.2 Dữ liệu và môi trường

- Primary review dùng DTO/API thật với `TEST_01` hoặc isolated LocalDB fixture.
- `MigrateAndDemo` dùng projection TSV đã kiểm tra từ workbook đăng ký VPP: 547 mặt hàng, 52 phòng ban, 161 đơn/2.828 dòng được map vào rolling 12-month window.
- Owner active nhận dữ liệu phòng ban chính; các phòng ban còn lại dùng identity giả bị `Disabled`, không password và e-mail `.demo.local`. Ghi chú nguồn có tên cá nhân không được đưa vào runtime dataset.
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

### 3.5 RBAC authority hiện tại — ba vai trò canonical

> Quyết định bốn vai trò ngày 2026-07-24 là lịch sử và đã bị `D1` trong `docs/execution/ATLAS-001.md` thay thế trước khi code. Authority hiện tại là source `src/Shared/Constants/CanonicalRbac.cs` và luận văn đã được đồng bộ.

Ba persona canonical là `EMPLOYEE / Nhân viên`, `MANAGER / Quản lý` và `DEV / Phát triển`. UI chỉ hiển thị action theo permission/API hiện hành; refactor UI không đổi role, seed, authorization, domain guard hoặc production boundary. Khi tài liệu lịch sử phía dưới còn nhắc `DEPARTMENT_APPROVER`, `PROCUREMENT_ADMIN` hoặc `SYSTEM_ADMIN`, coi đó là snapshot đã superseded, không phải lệnh triển khai hiện tại.

**Owner correction — 2026-08-05:** `EMPLOYEE` chỉ nhìn thấy workspace đơn cá nhân, danh mục mặt hàng trong Dashboard và báo cáo phạm vi cá nhân; không hiển thị `/library` hoặc `/permission`. `MANAGER` sở hữu Library và các màn quản lý/vận hành nhưng không sở hữu Permission. `DEV` giữ toàn bộ workspace và quản trị hệ thống. Backend action `LIBRARY_VIEW` của Employee chỉ phục vụ dữ liệu tham chiếu cần cho luồng đặt hàng, không được suy diễn thành quyền nhìn thấy workspace quản trị Library. Sidebar phải fail-closed khi thiếu mapping `SIDEBAR`, không tự mở menu bằng fallback.

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

- Centered account shell đã được owner duyệt: grid background, shared brand lockup, shared field/button/link rhythm và VI/EN switch.
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
- Account pages đã dùng chung account shell, brand lockup, spacing, link/button và feedback treatment; W0.2 đã được owner duyệt và khóa trước khi chuyển W1.
- Icon đang có dấu hiệu trộn `RadzenIcon` với Material Symbols; cần một wrapper/icon map duy nhất.
- Empty state dashboard đang lặp CTA, status và vùng trắng quá lớn; không ép mọi route phải vừa một viewport bằng cách làm chữ hoặc target quá nhỏ.
- History cần phân biệt rõ `không có lịch sử`, `không có kết quả theo bộ lọc` và `kỳ trước không có đơn`.
- Error toast không được hiển thị raw error code như `OperationInvalid`; phải đi qua mapper, localized safe message và trace/correlation id khi cần.
- Grid quản trị cần route-specific column profile, không dùng reflection để mặc định phơi toàn bộ field.

Không coi screenshot hiện tại hoặc Figma hiện tại là final; chúng chỉ là baseline và research evidence.

### 6.1 Screenshot review — W0.0 evidence từ owner

Các ảnh owner gửi ngày 2026-07-19 được ghi nhận là evidence của runtime hiện tại; chưa thay thế cho lần baseline capture trực tiếp bằng `dotnet watch`.

| Nhóm | Quan sát | Phân loại | Quyết định/đề xuất |
|---|---|---|---|
| Account shell | Login/Register dùng hero khác recovery; form đăng ký cuộn, copy dài và validation mixed VI/EN | Visual + usability | Owner review round 2 chốt bỏ hero trên toàn account flow; mọi route dùng centered grid shell, copy ngắn, VI/EN switch, reserved validation slots và không internal-scroll ở desktop. |
| Brand asset | Có nhu cầu thêm logo/hình | Product/brand | Không dùng AI để tạo logo hoặc icon chức năng. Dùng logo/vector mark chuẩn và icon system hiện hữu; AI art chỉ là asset phụ cho hero/empty state, phải review contrast, licensing và render thật trước khi nhận. |
| Icon | Một số icon nhìn sai hoặc không cùng nét | Visual/accessibility | Audit font/icon loading và các call-site; hợp nhất về `VppIcon`/semantic icon map, không sửa từng màn hình bằng ký tự Unicode rời. |
| Dashboard empty | Lặp CTA ở toolbar và empty state; status copy dài; vùng trắng lớn; kỳ trước trống gây scroll | Information architecture | Một primary action ở toolbar, empty state chỉ giữ context + next action khi cần. Gộp status thành summary ngắn; khi không có dữ liệu kỳ trước dùng compact row hoặc ẩn section, không dựng panel lớn. |
| Viewport/scroll | Một số màn hình phải cuộn dù nội dung ngắn | Usability | Mục tiêu là loại bỏ page scroll vô ích ở shell/dashboard/form ngắn. Bảng, form dài và detail nhiều trường vẫn dùng internal scroll, paging hoặc drawer có chiều cao rõ ràng. |
| History | Copy “Không có lịch sử...” không nói rõ đây là empty dataset hay filter miss | Content/data | Tạo state enum riêng và message VI/EN theo ngữ cảnh; không dùng một câu cho mọi trường hợp. |
| Error feedback | Toast hiển thị `OperationInvalid` | Correctness | Evidence cho thấy một caller có thể bypass `UiErrorMapper`; cần truy ra call-site, map thành safe localized message (`RequestInvalid`/business message), giữ raw code chỉ trong log và hiển thị trace id cho lỗi không mong đợi. |
| Library/admin grid | Nhiều cột, khó đọc nhưng vẫn cần CRUD/filter | Information architecture | Giữ server-side `LoadData`, filter/sort/paging; tạo column profile theo route/DTO: mặc định chỉ code, name, status và field quyết định; cột phụ qua column picker/filter drawer; detail/edit mở drawer hoặc full page tùy độ dài. Departments là route parity bắt buộc. |

### 6.2 Quy tắc hiển thị không lặp (data storytelling)

Mỗi vùng chỉ trả lời một câu hỏi: **đang ở đâu → điều gì cần biết → nên làm gì tiếp theo**. Không lặp cùng một deadline/trạng thái ở toolbar, KPI và empty panel nếu không có thêm ngữ cảnh. Với dashboard, ưu tiên `takeaway → evidence → action`; với grid, ưu tiên `scope/filter → essential columns → row action → detail on demand`. Quy tắc này áp dụng cho cả VI và EN, Light/Dark/Print.

### 6.3 Hợp đồng thuật ngữ nghiệp vụ

- entity `VPP item/product` hiển thị cho người dùng là **mặt hàng** / **item** khi nói về một dòng hàng cụ thể;
- dùng `Mã mặt hàng`, `Tên mặt hàng`, `Tổng mặt hàng` cho dữ liệu giao dịch; dùng **Danh mục văn phòng phẩm** cho tên module/chức năng tra cứu và quản trị catalog;
- `dòng hàng` chỉ là thuật ngữ kỹ thuật nội bộ cho row/line, không dùng trong UI;
- `vật tư` chỉ dùng khi mô tả phạm vi nghiệp vụ rộng trong tài liệu, không trộn với `mặt hàng` trên cùng luồng UI/report/export;
- resource VI/EN, insight tự động, export header và UI test phải cùng tuân theo contract này.

### 6.4 Bài học toàn cục từ owner review

- Các route cùng flow phải dùng cùng nhịp dọc: brand → title → field đầu → field cuối → primary action → secondary action; không chỉnh từng trang bằng khoảng cách rời.
- Hình/illustration chỉ được giữ khi giúp nhận diện hoặc nhiệm vụ. Asset đẹp nhưng không còn phục vụ flow phải bỏ thay vì cố bảo vệ thiết kế cũ.
- Header, tab, sidebar, hamburger và utility control phải dùng cùng interaction grammar: full hitbox, tint nhẹ, active mạnh hơn hover, không dịch chuyển layout.
- Owner ưu tiên thấy nội dung chính gọn trong viewport, nhưng không được ép mọi bảng/form dài thành chữ nhỏ hoặc target khó bấm; dùng hierarchy, paging, drawer và scroll region có chủ đích.
- KPI/evidence phải “kiếm được chỗ”: quota đặt ngay tại CTA, terminology theo nghiệp vụ, không lặp kỳ/trạng thái/deadline chỉ để lấp layout.
- Copy VI/EN phải viết theo ngữ cảnh và cùng nghĩa; không ghép resource máy móc hoặc để technical term lọt lên UI.
- Agent phải cập nhật living plan ngay khi feedback tạo thành quy tắc shared/global, thay vì chỉ sửa screenshot đang được nhắc tới.

### 6.5 [HISTORICAL/COMPLETED] Thứ tự triển khai đề xuất trước khi sửa code

> Đây là proposal trước khi UI-SYSTEM F0–F7 được triển khai. Giữ lại để giải thích quyết định lịch sử;
> không dùng các nhãn W0–W4 bên dưới làm backlog hiện tại. Current handoff nằm ở
> `docs/execution/FRONTEND-REFACTOR-001.md` và manifest 44 logical route.

1. **W0.1 Shared foundation:** audit icon loading, notification/error pipeline, typography/spacing/link/button tokens và height/overflow contract.
2. **W0.2 Account shell:** Login, Forgot/Reset/Change Password, Register, logout menu; chốt brand lockup và các state lỗi/thành công.
3. **W1 Dashboard:** My Orders empty/non-empty, status summary, previous-period behavior và above-the-fold layout.
4. **W2 History:** filter, loading, empty-by-filter, no-history, error, retry và pagination copy.
5. **W3 Library/admin:** Departments trước, sau đó Classes/Categories/Items/Suppliers/Price Lists; áp dụng column profiles và CRUD drawer/form states.
6. **W4 còn lại:** period operations, permissions, reports và các route detail; mỗi route phải kế thừa primitive đã chốt, không tạo style riêng.

Mỗi wave phải chạy browser review ở `1920×1080`, spot-check `768×1024`/`390×844`, VI/EN, Light/Dark/Print, loading/empty/error/success/disabled và kiểm tra console/network trước khi chuyển wave.

### 6.6 OpenAI/Codex-inspired shell và page contract — current from 2026-07-29

Contract này giữ toàn bộ geometry/nghiệp vụ owner đã duyệt từ các vòng trước, nhưng art direction hiện hành là OpenAI/Codex minimal system. Nó áp dụng cho **header, sidebar và toàn bộ page**, không chỉ route đang được nhắc tới.

**Shell geometry và hierarchy**

- Mọi mockup authenticated phải render trọn shell; không duyệt page crop bỏ sidebar/header vì sẽ che mất lỗi trục, inset và surface.
- Sidebar collapsed width và primary header height dùng cùng semantic token. Logo/brand và primary-tab label được so theo cùng outer visual axis; không tuyên bố cân chỉ vì từng phần tử tự center trong hitbox riêng.
- Primary header full-bleed từ mép sidebar đến mép phải viewport; navigation chrome và sidebar dùng cùng surface token, divider/hairline có một owner và không tạo bốn cạnh trắng hoặc line kép.
- Mọi page authenticated dùng cùng `page outer inset` theo từng trục: khoảng cách từ content đến đáy header, mép sau sidebar, mép phải và mép dưới phải nhất quán giữa các màn hình. Mặc định inline-start bằng inline-end (`symmetric content inset`); block-start/block-end có thể khác nhau nhưng mỗi cạnh phải giữ đúng một nhịp trên mọi page. Full-bleed navigation, print và overlay chỉ được ngoại lệ khi contract ghi rõ.
- Browser gate phải kiểm tra cả token đã resolve, computed padding của shell và mép workspace nhìn thấy ở expanded/collapsed; không được chỉ đo container kỹ thuật. Tab con, breadcrumb hoặc wizard header nằm bên trong content là hierarchy hợp lệ, không phải outer inset cần xóa.
- Root icon giữ cùng cột ở expanded/collapsed. Child và grandchild dùng depth token đệ quy: interaction surface vẫn full-row, chỉ rail/icon/text lùi cấp; không thêm cấp sâu hơn grandchild. Nếu hierarchy tiếp tục sâu, chuyển sang split/list trong page.
- Header tab và sidebar dùng cùng interaction primitive: row/hitbox, typography, neutral hover, project-blue active indicator, focus-visible và press feedback. `Orientation-aware navigation rhythm`: sidebar dọc dùng hover surface gần sát hai mép ngang và gap theo trục dọc; header ngang dùng surface gần sát hai mép dọc và cùng gap theo trục ngang.
- Header tab dạng link phải giữ cùng màu chữ ở cả `:link` và `:visited`; lịch sử điều hướng của trình duyệt không được làm ẩn tab cấp cao khi tab cha đang mở thành nhóm tab con.
- Nhóm route có đúng một cấp con dùng `HEADER-TAB-GROUP`: khi vào nhóm, parent trở thành context label không tương tác và không có underline; parent chỉ là link tới child mặc định khi nhóm chưa mở. Dấu `›` biểu thị hierarchy và chỉ child hiện tại sở hữu shared active indicator. Hai divider dùng full-height của tab group và màu `border-default` bình thường để phân định rõ nhưng không tạo accent cạnh tranh. Desktop đặt cụm trong primary header; mobile render cùng typed navigation ở đầu panel vì primary header desktop bị ẩn. Owner duyệt áp dụng đầu tiên cho `Quản lý kỳ → Chốt kỳ / Duyệt đơn bổ sung` và `Bảng giá → Danh sách bảng giá / Giá mặt hàng` ngày 2026-07-30; `VppSegmentedSelector` không được dùng thay header navigation.
- Header sidebar expanded đặt hai icon action cùng rail với chevron menu: `Expand all / Collapse all` điều khiển toàn bộ group có quyền hiển thị, còn collapse sidebar chỉ đổi độ rộng shell. Cả hai dùng hitbox, hover/focus và motion token chung; không điều khiển expander bằng DOM/JS.
- Dòng phụ dưới tên người dùng trong sidebar hiển thị **group/role hiện hành** (`Nhân viên`, `Quản lý`, `Quản trị hệ thống` hoặc tên group tùy biến); shared header không lặp group/role. Popup tài khoản vẫn giữ phòng ban để hai loại ngữ cảnh không bị đánh tráo.

**Motion và performance**

- Indicator là một shared object đi trực tiếp source → destination: header/tab chạy trái–phải, sidebar chạy trên–dưới; không chạy tuần tự qua item trung gian, không bung từ tâm và không treo khi ancestor bị collapse/clip. Contract này chỉ áp dụng cho navigation/selector có một active target, không áp dụng cho divider hoặc viền bảng tĩnh.
- Sidebar expand/collapse, expander và indicator dùng cùng motion token khoảng `180–220ms`, easing vào nhanh/dừng mềm; movement phải theo hướng gây ra thay đổi và có `prefers-reduced-motion` fallback.
- Menu lọc, popup, popover và transient panel do project sở hữu dùng một enter-motion token chung với account menu; hướng mở bám vị trí trên/dưới trigger, còn modal giữa màn hình giữ tâm cố định.
- Không dùng Canvas glyph offset, fixed optical offset theo từng chuỗi, DOM probe tạo node hoặc global `MutationObserver` quét DataGrid/virtualized rows. Runtime geometry chỉ đo route thật khi cần QA; interaction production ưu tiên component event/state trực tiếp.
- Refresh/direct-load có thể dùng reveal rất nhẹ, nhưng shell phải hiện ngay, không layout shift, không lặp ở internal navigation và không che loading/error thật.

**Typography, surface và content**

- Control/sidebar/header label dùng system-font stack, body token khoảng `14/20`, weight Regular/Medium; heading/page data dùng semantic scale ít bậc để tạo hierarchy và không ép ALL CAPS.
- Sidebar/header là navigation chrome bình tĩnh; content canvas tách lớp nhẹ; card/table surface sáng rõ, border-first, radius nhất quán, shadow chỉ dành cho transient/elevated surface. Không copy brand/pixel ChatGPT vào nghiệp vụ GTAS.
- Mỗi page đi theo `scope/context → takeaway/evidence → action → detail on demand`; bỏ page title/helper/status/deadline lặp lại nếu tab, card hoặc meta đã trả lời cùng câu hỏi.
- Khi primary header-tab hoặc sidebar grandchild đã xác định page, content bắt đầu trực tiếp bằng filter/KPI/workspace; không lặp page title và helper text ở đầu trang. Action nghiệp vụ được đặt trong toolbar/card sở hữu dữ liệu thay vì giữ một page-heading rỗng.
- Không đặt nút `Làm mới/Refresh` thường trực ở page heading. Dữ liệu cập nhật theo thao tác/filter hoặc navigation; retry chỉ xuất hiện trong error state có thể khôi phục, còn reload toàn document đã thuộc browser.
- Trong bảng đơn hàng, tên mặt hàng và mã mặt hàng nằm chung một cột: tên là dòng chính, mã là metadata dòng phụ để giữ bảng gọn và dễ quét. Chỉ màn hình quản trị dữ liệu thư viện mới tách `Mã mặt hàng` thành cột riêng khi mã là đối tượng thao tác chính.
- Nhãn hướng tới người dùng phải viết đầy đủ `Đơn vị`; không dùng viết tắt nội bộ `ĐVT` trên màn hình, mockup, export hoặc bản in.
- My Orders và History dùng chung một `order-detail surface` từ filter đến footer: cùng search, popup danh mục/đơn vị, clear-filter state, code/note popover, sáu cột, name+code hierarchy, alignment, empty/loading/error, virtualization và count footer. Chỉ header/context và chiều rộng thay đổi; action lấy từ capability backend.
- Header History bám phiếu PDF canonical: kicker GTAS VPP, mã + trạng thái, một hàng `Kỳ · Loại đơn · Người đặt · Phòng ban · Gửi lúc`, ghi chú và PDF/Excel. Không hiển thị action `Xem lịch sử phiên bản` trong phiếu chi tiết.
- Đơn hiện tại hoặc đơn bổ sung chỉ hiện sửa/hủy khi `CanEdit/CanCancel` cho phép. Đơn kỳ trước và History là read-only đối với mutation: không hiện sửa/hủy; read-only navigation như xem lịch sử chỉ giữ khi nghiệp vụ cho phép.
- Page ngắn ưu tiên một viewport. Dữ liệu dài dùng vùng scroll có biên rõ, sticky header, server paging hoặc virtualization theo capacity; detail tối đa khoảng 500 dòng chỉ cuộn trong panel, không kéo dài toàn page.

**Interaction và state**

- Hover/active/press surface phải phủ đầy visible hitbox, không bị line hoặc inner Radzen link/ripple cắt đôi. Idle text/icon giữ tương phản bình thường; active không giả hover.
- Chỉ một transient surface mở tại một thời điểm; outside click/Escape đóng; popup tự flip theo viewport và không làm layout đổi kích thước.
- Loading lần đầu dùng geometry-matched skeleton; refresh sau đó giữ last-known content. Empty, filter-empty, error/retry, disabled và success dùng chung primitive, localized VI/EN và không hiển thị raw technical code.
- Browser route thật với Radzen DOM, screenshot, bounding boxes, console/network và interaction là verification authority. Fixture/source assertion chỉ là lớp regression bổ sung.

### 6.7 [HISTORICAL/COMPLETED] Mockup-first approval gate và full page inventory

> Atlas/mockup inventory là evidence và design history. Browser Blazor thật vẫn là visual authority cuối;
> bảng 44 logical route/query key hiện được quản lý riêng trong `RouteAcceptanceManifest`.

Không mở rộng redesign production sang route mới trước khi owner duyệt mockup. Mockup là prototype/evidence ngoài production source; không tạo UI Lab trong repository và không sửa React paused.

**Bộ mockup đề xuất — 28 canonical screens, mỗi screen có shell hoàn chỉnh:**

| Board | Logical page/state được cover |
|---|---|
| M0 — Nền tảng giao diện | tokens, type scale, surface, shell, trạng thái tương tác và quy tắc loading/empty/error dùng chung |
| M1 — Tài khoản và phiên | Login, Forgot Password, Reset Password, Change Password, Logout, Register; Login/Logout bám thiết kế Blazor hiện tại và chỉ polish nhẹ khi cần |
| M2 — Vòng đời đơn cá nhân | My Orders, Order Create/Edit, History, Product Catalog |
| M3 — Tổng hợp quản lý | Department Summary và các biến thể tổng hợp theo phạm vi được cấp |
| M4 — Phê duyệt và chốt kỳ | Supplement/Pending Approval là một màn hình canonical theo nhiều trạng thái; Period Review, Demand Aggregation, Supply Allocation và Settlement preview/confirm/result |
| M5A — Danh mục và tổ chức | Class Definitions, Categories, Items, Departments; dùng chung list/detail/CRUD archetype nhưng mỗi logical tab có một mockup dữ liệu thật |
| M5B — Nhà cung cấp và giá | Suppliers, Price Lists, Prices; tách riêng để review đúng vòng đời công bố/hết hiệu lực và quan hệ nguồn cung |
| M6 — Người dùng và phân quyền | Users/Membership, Groups & Permissions matrix, last-admin/guarded change states |
| M7 — Báo cáo | Own/department/company scope, filter, exact table, export/print states |
| M8 — Trạng thái vận hành đại diện | notification inbox, reconnect, unauthorized, error, not-found, initial loading và empty/filter-empty; đây là bảng QA tập trung, không phải module nghiệp vụ độc lập |

Vòng duyệt đầu tạo đúng **28 canonical desktop screens ở `1920×1080`** cho artifact chụp ảnh, ưu tiên Light mode và trạng thái populated/primary. Chế độ owner review không được khóa vào canvas này: screen phải dùng đúng viewport trình duyệt, giữ shell trong `100dvh`, không sinh body scrollbar và chỉ cho vùng content/table có chủ đích cuộn nội bộ. Loading, empty, filter-empty và error không nhân thành một ảnh riêng cho từng route; chúng kế thừa shared state templates do agent tự thiết kế trong M0/M8, sau đó chỉ render representative state ở các archetype quan trọng. My Orders/History/Library vẫn có long-data representative; permission-dependent controls có role annotation. Sau khi motif desktop được duyệt, route quan trọng mới mở rộng `768×1024`, `390×844`, Dark mode và EN spot-check để tránh sửa cùng một lỗi trên quá nhiều biến thể sớm.

**Design Atlas — cơ chế duyệt và sửa hàng loạt:**

- mockup được dựng thành một prototype HTML/CSS cô lập ngoài production source, không tạo UI Lab trong repository và không sửa React paused;
- một bộ `design tokens + shared primitives + deterministic fixtures + screen manifest` sinh cả 28 screen; header, sidebar, typography, table, form, card và state không được copy CSS riêng theo page;
- Atlas có một trang overview gồm 28 thumbnail, filter theo board/role/state và mở từng screen ở chế độ tự khớp viewport; chế độ capture cố định `1920×1080` chỉ dành cho Playwright xuất contact sheet/PNG, không dùng để đánh giá density bằng browser zoom;
- các global controls cho phép đổi `sidebar expanded/collapsed`, role, VI/EN, Light/Dark và viewport trên cùng prototype. Feedback toàn cục được sửa ở token/primitive một lần rồi render lại toàn bộ 28 screen bằng Playwright;
- feedback riêng page mới đi vào screen composition. Nếu ba page cùng yêu cầu một sửa đổi, thay đổi đó phải được nâng thành shared primitive thay vì vá ba CSS riêng;
- mỗi lần render tạo một review manifest gồm screen id, revision, token version và trạng thái `DRAFT/CHANGES_REQUESTED/APPROVED`; ảnh đã approved trở thành visual baseline cho implementation Blazor route thật;
- owner duyệt theo ba tầng để giảm thao tác: **M0 motif toàn cục → archetype/board → ngoại lệ page**, không cần mở và chỉnh thủ công từng route cho các thay đổi chung.

**Ma trận archetype dùng chung — khóa trước khi tùy biến nghiệp vụ:**

| Archetype | Primitive sở hữu bố cục | Screen áp dụng | Phần được phép tùy biến |
|---|---|---|---|
| Shared shell | Sidebar cha–con–cháu, primary header tabs, account menu, role annotation, responsive drawer | M0 và toàn bộ screen authenticated M2–M8 | Navigation tree theo quyền, tab đang chọn, role; không tạo shell riêng theo page |
| Collection workspace | KPI tùy chọn → filter toolbar → data card/table → pager/state | Product Catalog chỉ đọc, Department Summary, All Orders, Reports và các danh sách không cần detail cố định | KPI, schema cột, export/action theo capability, server paging |
| Master–detail workspace | Filter toolbar → list/table trái → persistent inspector phải; compact chuyển detail thành overlay | Supplement/Pending Approval, 7 Library screen, Users; History dùng biến thể grid riêng nhưng tái sử dụng detail primitive | Width list/detail, field summary, action slot, read-only/mutation capability |
| Order-detail surface | Order meta → filter → bảng 6 cột `# / Mặt hàng / Danh mục / Đơn vị / Số lượng / Ghi chú` → shared state | My Orders regular/supplement của kỳ đang chọn và History detail | Width, meta, filter availability và action slot; History không có mutation action |
| Workflow/wizard | Step navigation → working table/form → summary rail → validation/confirm | Order Create/Edit và Settlement confirm | Số bước phải lấy từ nghiệp vụ/source; action/validation theo trạng thái, không tự thêm bước |
| Period operation | Period hero → readiness/KPI → exception list/chart → guarded action | Period Review, Pending Approvals, Settlement | Quy tắc chặn/cảnh báo, quyền chốt/duyệt, audit và confirmation |
| Canonical account flow | Centered account shell → form/progress/message → primary action/links | Login, forgot/reset/change password, logout, register non-production | Field, validation, copy và môi trường; Login/Logout bám UI hiện hành |
| Shared content state | Geometry-matched loading, refreshing, empty, filter-empty, error/retry, disabled/success | M8 đại diện và mọi consumer ở các archetype trên | Copy theo ngữ cảnh và CTA theo capability; geometry/motion/icon treatment dùng chung |

Quy tắc triển khai Atlas và Blazor: screen chỉ compose archetype + fixture + capability; không copy lại shell/filter/table/inspector/state. Nếu một khác biệt chỉ do role, trạng thái hoặc quyền thì truyền cấu hình/slot; chỉ tạo page-specific markup khi cấu trúc nghiệp vụ thực sự khác. Atlas đã bắt đầu dùng chung `collectionWorkspace`, `masterDetailWorkspace` và `orderItemsRegion` để khóa contract này trước khi code route thật.

**Quy tắc điều hướng dữ liệu — paging so với scroll/virtualization:**

- dùng `scroll + virtualization` khi người dùng tương tác liên tục trên nhiều dòng trong cùng một ngữ cảnh và cần giữ selection/edit state: chọn mặt hàng khi tạo/sửa đơn, mặt hàng đã chọn, item detail của My Orders/History và item review trước khi duyệt;
- dùng `server paging` khi mục tiêu chính là xem/tra cứu danh sách hoặc chọn một record để mở detail: Catalog, History order list, approval queue, management summary, Library, Users và Reports;
- không hiển thị pager bên trong order-item surface; grid có body bounded, sticky header, internal scroll và virtualization. Tổng số nằm gần title/summary, không tạo footer lặp;
- bảng paging có summary `Hiển thị X–Y trên Z`, pager ngay sau content; nếu chỉ có một trang thì ẩn page controls. Không phối hợp pager hiển thị với continuous scroll trong cùng một grid;
- Library/Users dù có hành động sửa vẫn dùng paging vì thao tác diễn ra trên từng record/detail, không cần giữ multi-row working state như order composition.
- ranh giới cuối bảng phải chỉ có một separator 1px: với scroll/virtualization, row cuối không tự vẽ border và footer summary sở hữu separator; với paging, row cuối vẫn có underline để kết thúc vùng dữ liệu, khoảng fill sạch hấp thụ chiều cao còn lại và pager sở hữu separator riêng ở đáy. Không để row border, scrollbar end-cap và footer border chồng thành dải dày hoặc che nội dung khi cuộn hết.

| Atlas contract | Paging | Scroll/virtualization |
|---|---|---|
| Collection read-only/admin | Catalog, Library, Users, Reports | Không |
| Master–detail | History order list, Department Summary order list, Supplement Approval queue | Vùng chi tiết mặt hàng của record đang chọn |
| Period operation | Period Review blockers, Period Demand department list, Settlement source summary | Period Demand item totals, Supply Allocation item comparison |
| Continuous working state | Không | My Orders items, Order Create catalog/selected items, Permission matrix |

**Contract filter toàn bộ dữ liệu — bắt buộc cho paging và virtualization:**

- mọi search/filter/sort chạy ở server trên full authorized query; thứ tự xử lý chuẩn là `authorization scope → search/filter → sort → CountAsync → Skip/Take → projection`;
- frontend Radzen truyền `search`, filter expression, `orderby`, `skip` và `top` qua `LoadData`; API trả `Data + TotalCount` của tập dữ liệu sau lọc nhưng trước paging. Không được tải một page rồi mới dùng LINQ/JavaScript để lọc tại client;
- khi filter/search thay đổi, grid quay về offset/page đầu nhưng giữ toàn bộ filter state; input search debounce khoảng 250–350ms để tránh gọi API mỗi phím;
- dữ liệu cho checkbox-list filter/distinct values cũng lấy từ full authorized + currently filtered dataset, không chỉ từ các row của page đang hiển thị;
- scroll + virtualization dùng cùng query contract: `skip/top` đại diện vùng row đang nhìn thấy, còn search/filter vẫn áp trước vùng tải. Selection của Order Create được lưu theo item ID trong working context, không phụ thuộc row còn nằm trong viewport;
- automated integration/UI test phải có fixture trong đó kết quả tìm kiếm nằm ngoài page 1, xác nhận filter tìm thấy record, `TotalCount` đúng, grid reset offset và clear filter phục hồi dữ liệu;
- implementation authority là `RadzenDataGrid` 11.1.4 trên Blazor `InteractiveServer`. Design Atlas chỉ khóa visual hierarchy, data-navigation mode và state contract; không mô phỏng hay cam kết DOM/API giống Radzen. Sau approval, mỗi archetype được map sang shared Blazor component rồi QA bằng authenticated route thật, console/network và DOM geometry.

**Approval sequence:** `Atlas overview 28 screens → M0 nền tảng → M2 vòng đời đơn → M3/M4 quản lý và chốt kỳ → M5A/M5B dữ liệu nền và giá → M6 phân quyền → M7 báo cáo → M1/M8 hardening`. Owner có thể sửa một global token/primitive, một board hoặc một page exception. Chỉ board `APPROVED` mới được chuyển thành implementation vertical slice và visual baseline.

**Design Atlas draft evidence — generated 2026-07-23:**

- artifact root: `C:\Users\MIIKEY\.codex\visualizations\2026\07\23\019f8d67-1d84-76f0-b7a3-7ccc09bd475f\gtas-vpp-design-atlas`;
- interactive review: `http://127.0.0.1:4178` khi `serve-atlas.cjs` đang chạy;
- trang overview dùng review wall dạng caro: 3 cột phủ toàn bộ chiều ngang ở desktop, gap bằng 0, tile phẳng không bo góc/shadow và chỉ phân tách bằng hairline 1px; viewport CSS `1280px` (tương đương zoom trình duyệt 150% trên màn 1920px) vẫn giữ 3 cột để owner quan sát đồng thời nhiều màn hình;
- output gồm 28 Light screens `1920×1080`, 9 board contact sheets, 1 overview và 4 Dark representatives;
- automated capture gate xác nhận mỗi screen đúng `1920×1080`, shell authenticated giữ sidebar `286px` + header `72px`, account card căn giữa, đúng một active primary tab và console error count bằng 0;
- automated review gate chạy toàn bộ 28 screen tại `1904×914`, `1536×864` và `1366×768` (84 tổ hợp): screen bằng đúng viewport, body không cuộn ngang/dọc và content canvas không tràn ngang; nếu nội dung dài thì chỉ scroll region nội bộ được phép cuộn;
- mọi copy tiếng Việt trong mockup phải diễn đạt theo ngữ cảnh nghiệp vụ; không dịch từng từ máy móc. Tên role/action/API/môi trường kỹ thuật chỉ giữ tiếng Anh khi cần đối chiếu audit/source và phải có nhãn tiếng Việt xung quanh;
- toàn bộ screen hiện ở trạng thái `DRAFT`; chưa phải production baseline và chưa cho phép implement route cho đến khi owner duyệt M0/board tương ứng.

**Hai motif confirmations đã được owner chốt ngày 2026-07-23:**

1. Typography: system-font stack trung tính cho shell/navigation/form/table; Poppins chỉ giữ ở headline/KPI editorial cần tạo dấu ấn. Quyết định lịch sử này hiện được mở rộng bởi `D-OPENAI-UI-01`: hierarchy gọn, ít decoration và không mô phỏng pixel sản phẩm bên ngoài.
2. Phạm vi vòng ảnh đầu: 28 desktop Light screens; Dark chỉ render M0 + ba archetype đại diện (data workspace, form, admin matrix), rồi mới sinh full Dark/responsive sau khi Light motif được duyệt.

**Agent-ready refactor sau mockup approval, trước route implementation:**

- chuẩn hóa CSS load order/layers: tokens → base → Radzen normalization → shared primitives → route CSS; giảm selector conflict, legacy rule và `!important`;
- tạo shared primitives cho shell navigation, page context, filter toolbar, content state, master/detail, dense grid, popover và permission boundary;
- đưa route/state/role fixture vào metadata dùng chung cho mockup, Playwright và route ledger;
- chuyển interaction khỏi global DOM observer sang explicit component events/state; performance soak là gate cho shared JS;
- giữ public API nhỏ, naming rõ và docs gần code để AI agent không phải suy luận lại geometry/permission contract ở mỗi route.

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

## 8. Implementation waves — HISTORICAL PROPOSAL

`M0–M8` mockup approval ở Section 6.7 là proposal lịch sử. Existing change-set đang chờ review được giữ nguyên;
không dùng bảng W0–W8 bên dưới làm current execution queue. Current frontend refactor status nằm ở
`FRONTEND-REFACTOR-001`; manifest 44 key chỉ là technical coverage, không thay owner visual acceptance.

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

## 9. Route ledger — HISTORICAL VISUAL/WAVE SNAPSHOT

> Các bảng trong mục này ghi lại Atlas/visual wave và quyết định theo từng thời điểm, không phải canonical
> status hiện tại. Không gộp 28 screen visual với 44 logical route/query key: coverage kỹ thuật hiện tại xem
> [`RouteAcceptanceManifest`](../../tests/Frontend.UnitTests/Architecture/RouteAcceptanceManifest.cs), còn
> visual approval vẫn ở mục 14 và runtime browser.

### Atlas Blazor wave 1 — 2026-07-26

Owner cho phép triển khai liên tục trên branch `codex/atlas-blazor-wave1`, không merge/deploy. Bộ 16 ảnh trong
`LVTN/screenshots/ch03/atlas/` là định hướng bố cục; backend/API/schema/test hiện hành vẫn quyết định nghiệp vụ,
quyền và dữ liệu. Frontend hiện tại được giữ khi đã tốt hơn hoặc chính xác hơn ảnh Atlas.

| Atlas | Route/state thật | Trạng thái | Phạm vi wave 1 |
|---|---|---|---|
| 01 Login | `/Account/Login` | ISOLATED_QA_PASS — OWNER_REVIEW | Account shell + validation + capture runtime sạch |
| 02 Đơn hàng của tôi | `/dashboard?tab=0` | ISOLATED_QA_PASS — OWNER_REVIEW | Period story, KPI, order workspace và PDF/XLSX tải thật |
| 02B Chọn kỳ đặt hàng | Employee `/dashboard?tab=0&regularPeriodId={id}&supplementPeriodId={id}`; admin `/dashboard?tab=5&periodTab=periods` | IMPLEMENTED — VALIDATION IN PROGRESS | Scheduler chỉ duy trì một kỳ chuẩn; quản lý có thể thêm kỳ rời rạc với lịch độc lập. Hai loại đơn giữ kỳ chọn độc lập: đơn thường mặc định kỳ đang mở, đơn bổ sung mặc định kỳ trước gần nhất kể cả khi đã hết hạn/chốt để còn xem dữ liệu; ví dụ kỳ thường 08/2026 thì bổ sung chọn 07/2026. Quyền tạo bổ sung vẫn do trạng thái và hạn kỳ quyết định. `periodId` chỉ còn tương thích link cũ. Card hạn gửi luôn theo đúng loại đơn và kỳ đang xem. Mọi mutation dùng `PERIOD_SETTLE`; sau chốt dùng request + settlement revisions, notification deep link và four-eyes. Thiết kế rolling 3 kỳ trong `MULTI-PERIOD-ORDERING-001.md` chỉ còn là lịch sử; authority hiện hành nằm tại `ORDERING-PRICING-REVISION-20260818.md`. |
| 03 Tạo/sửa đơn | `/dashboard/order-create` | ISOLATED_QA_PASS — OWNER_REVIEW | Quy trình hai bước; mutation edit/history/cancel pass |
| 04 Lịch sử | `/dashboard?tab=1` | ISOLATED_QA_PASS — OWNER_REVIEW | Summary/chart/list/detail; chart suy biến dùng empty state thay SVG `NaN` |
| 05 Danh mục mặt hàng | `/dashboard?tab=2` | ISOLATED_QA_PASS — OWNER_REVIEW | Toolbar/state theo Atlas, không lộ giá cho nhân viên |
| 06 Tổng hợp phòng ban | `/dashboard?tab=3&managementTab=department` | ISOLATED_QA_PASS — OWNER_REVIEW | Dùng chung History workspace; chỉ khác 8 cột danh sách và scope dữ liệu phòng ban |
| 07 Duyệt đơn bổ sung | `/dashboard?tab=5&periodTab=pending` | ISOLATED_QA_PASS — OWNER_REVIEW | Canonical List-Detail: collection header/count, search + phòng ban trên toàn hàng chờ, tự chọn đơn đầu, full code + status/detail facts, pager master và workflow footer bám đáy; mutation approve/reject giữ nguyên |
| 08 Rà soát kỳ | `/dashboard?tab=5&periodTab=review` | ISOLATED_QA_PASS — OWNER_REVIEW | Trạng thái, blocker chốt kỳ và bằng chứng nguồn |
| 09 Gom nhu cầu | `/dashboard?tab=5&periodTab=demand` | ISOLATED_QA_PASS — OWNER_REVIEW | Hai chế độ Theo đơn/Theo mặt hàng, dữ liệu thật từ period-demand |
| 10 Chọn nguồn cung | `/dashboard?tab=5&periodTab=supply` | ISOLATED_QA_PASS — OWNER_REVIEW | Nhà cung cấp trước bảng giá; preview và đối chiếu giá thật |
| 11 Chốt kỳ | `/dashboard?tab=5&periodTab=settle` | ISOLATED_QA_PASS — OWNER_REVIEW | Route riêng xác nhận đúng preview từ Chọn nguồn cung; snapshot bất biến, confirm/correct theo capability |
| 12 Mặt hàng quản trị | `/library?tab=2` | ISOLATED_QA_PASS — OWNER_REVIEW | Collection + inspector; CRUD/status thật, DataGrid a11y opt-in |
| 13 Danh sách bảng giá | `/library?tab=6&pricingTab=price-lists` | SOURCE_COMPLETE — QA_PENDING | Collection server-paged; bảng giá dùng ngay sau khi tạo, trạng thái người dùng chỉ còn Hoạt động/Vô hiệu hóa; menu giữ Sửa/Mặc định/Sao chép/Vô hiệu hóa/Xóa vĩnh viễn, không còn nháp/công bố/hết hiệu lực |
| 13B Giá mặt hàng | `/library?tab=6&pricingTab=prices` | ISOLATED_QA_PASS — OWNER_REVIEW | Bảng giá là context; NCC suy ra; search/danh mục/trạng thái ánh xạ lọc full authorized dataset |
| 14 Người dùng | `/permission?tab=0` | ISOLATED_QA_PASS — OWNER_REVIEW | Search/filter backend; activation/reset/deactivate thật; inspector không lộ secret |
| 15 Nhóm và quyền | `/permission?tab=1` | VERIFIED — OWNER_REVIEW | Ma trận 18×3 read-only + UI mapping; mutation toggle pass theo `GroupCode=DEV` |
| 16 Báo cáo | `/report` | ISOLATED_QA_PASS — OWNER_REVIEW | Analytics workspace; KPI/chart, static evidence frame có footer, insight detail và PDF/XLSX/CSV thật |
| 17 Trạng thái hệ thống | shared state primitives | ISOLATED_QA_PASS — OWNER_REVIEW | Inbox/reconnect/denied/error/empty/loading có semantics/focus thống nhất |
| Đổi mật khẩu tự nguyện | `/Account/ChangePassword` | APPROVED — RUNTIME EVIDENCE | Entry point trong `UserMenu`; account shell bảo vệ bằng `[Authorize]` |

Wave 1 không tạo endpoint, role, trường dữ liệu hoặc hành động giả để khớp ảnh Atlas. Ba persona hiện hành là
`EMPLOYEE`, `MANAGER`, `DEV`; trên giao diện `DEV` được diễn giải là **Quản trị hệ thống (DEV)**.

Evidence đóng ATLAS-001 ngày 2026-07-27: Release solution build sạch; backend `422/422`, frontend
architecture/unit `180/180`. `AtlasFullRuntimeTests` pass 28 screen × 4 viewport; representative
Dark/Print/axe, shell responsive và 17 capture runtime pass. Export UI tải thật report CSV/XLSX và order
PDF/XLSX. Mutation opt-in pass permission toggle, vòng đời đơn thường và duyệt/từ chối đơn bổ sung.
Capture giữ ngoài repository tại `TestResults/atlas-w-h-final3/`; đây là evidence, chưa phải visual golden
baseline vì owner chưa duyệt từng route.

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
| Tokens/themes/print | IN_IMPLEMENTATION | Viewport/overflow contract verified; Light/Dark/Print tokens remain |
| Page header/action hierarchy | PENDING | `VppPageHeader`, action placement, breadcrumbs |
| Status/state primitives | IN_IMPLEMENTATION | `VppIcon` centralized; legacy empty state delegates to `VppEmptyState` |
| DataGrid/admin pattern | PENDING | Toolbar, paging, column picker, inspector, mobile card |
| Dialog/form/notification | IN_IMPLEMENTATION | Raw exception/error codes removed from user feedback; dialog/form consistency remains |

**W0.1 verified evidence — 2026-07-19:**

- self-hosted Material Symbols render only through `VppIcon`; semantic names live in `VppIcons`;
- all user-facing frontend exception paths use `UiErrorMapper`; raw `ex.Message` remains diagnostic-only;
- `OperationInvalid` maps to localized `RequestInvalid`, while safe detail/trace rules remain enforced;
- app shell uses `100dvh` fallback and `overflow-y: auto`, so short pages no longer show a forced scrollbar;
- notification trigger dùng chung header-control primitive với theme switch: cùng kích thước, border/surface/shadow, hover/active/focus; unread badge giữ semantic riêng;
- Release solution build: `0 warning / 0 error`; frontend unit/architecture tests: `140/140`;
- anonymous login browser QA passed at `390×844`, `768×1024`, `1920×1080`;
- authenticated shell QA passed on isolated Aspire/LocalDB across core routes and three viewports.

### W1 — Shell + Auth/System

| Route/state | Status | Notes |
|---|---|---|
| `/` redirect | VERIFIED | Typed loading state trong lúc resolve first-accessible route, không blank flash |
| `/Account/Login` | APPROVED | Centered shell; inline credential feedback; VI/EN switch |
| `/Account/Register` | APPROVED | No employee-code field; localized stable validation; no desktop scroll |
| `/Account/ConfirmEmail` | APPROVED | Success/expired/invalid; compact shell |
| `/Account/ForgotPassword` | APPROVED | Anti-enumeration; compact recovery shell |
| `/Account/ResetPassword` | APPROVED | Policy/expired/replay; invalid link hides form |
| `/Account/ChangePassword` | APPROVED | Current/new/confirm; forced-change context |
| `/loginprocess` | VERIFIED | Shared account progress shell + fallback về login; không còn route trắng |
| `/logoutprocess` | APPROVED | Safe clear + redirect login; branded progress shell |
| `/Error` | SOURCE_VERIFIED | Typed error state + correlation + retry + dashboard fallback |
| `/not-found` | VERIFIED | Typed empty state; browser flow quay về valid workspace pass |
| Shell/notification/reconnect | VERIFIED_F5 | Notification focus-after-render không làm chết circuit; reconnect giữ contract Blazor; full motion/a11y matrix tiếp tục ở F7 |

**W0.2 round-2 evidence — 2026-07-19:**

- toàn bộ account route dùng cùng centered grid shell; hero artwork không còn render;
- account card dùng shared vector request-document mark và có VI/EN switch giữ nguyên route hiện tại;
- cùng brand lockup, title scale, password field, inline alert, back-link và action treatment được dùng xuyên account flow;
- Register bỏ `EmployeeCode` khỏi UI nhưng giữ DTO/API field optional để không tạo schema/API breaking change;
- form validation dùng localized Radzen validators, mirror Identity password policy và reserve message slots để không layout shift;
- credential failure hiển thị trong form thay vì toast; button copy rút gọn thành Login/Register/Send tương ứng ngôn ngữ;
- account API error code đi qua `AccountLifecycleUiMapper`; raw backend message không render ra UI;
- shared shell dùng semantic sidebar icon map; notification center bỏ legacy `rzi` markup để tránh missing glyph;
- account browser QA pass ở `390×844`, `1366×768`, `1920×1080`, gồm VI/EN, invalid reset link, icon centralization và horizontal overflow;
- isolated account-menu QA và authenticated shell responsive QA pass; accessibility/logout regression pass;
- frontend unit/architecture tests: `139/139`; Release solution build: `0 warning / 0 error`.

**W0.2 round-3 owner feedback — 2026-07-19:**

- thay request-document outline mark bằng solid geometric `V` mark để nhận diện rõ ở kích thước nhỏ;
- password wrapper sở hữu duy nhất một underline; eye action căn theo tâm dọc để không tạo đoạn line lệch/đứt;
- account links dùng lưới ba cột cân bằng, đặt dấu phân cách đúng trên centerline của primary action;
- xóa CSS scoped legacy của Login từng ghi đè shared account shell và gây lệch link dù CSS global đã đúng;
- browser geometry test khóa chung left/right edge giữa username, password và primary action.

**W0.2 round-4 owner feedback — 2026-07-19:**

- rút gọn confirm-password copy thành `Nhập lại mật khẩu` / `Confirm password` trên Register, Reset và Change Password;
- mọi account underline dùng nét nền `1px`; password wrapper sở hữu nét duy nhất, Radzen control/input bên trong bắt buộc `0px`;
- Login bỏ dấu phân cách dạng text và dùng secondary action row hai cột bằng nhau, có divider nhẹ để VI/EN luôn cân đối;
- browser geometry test đo trực tiếp độ dày border, tâm action row và chiều rộng hai secondary action.

**W0.2 approval — 2026-07-19:**

- Forgot Password dùng tiêu đề một dòng `Khôi phục mật khẩu` / `Recover password`, không tách accent;
- khoảng cách từ account title xuống field đầu tăng nhẹ và áp dụng đồng bộ toàn bộ account route;
- owner duyệt W0.2 và cho phép chuyển sang W1 Dashboard.

**W0.2 post-approval rhythm refinement — 2026-07-19:**

- Login/Register/Forgot Password dùng chung token cho topbar, title-to-form, field, submit và secondary-action spacing;
- brand lockup và VI/EN switch khóa cùng chiều cao/tâm dọc; primary action giữ cùng chiều cao `48px`;
- lỗi credential của Login dùng chính validation slot dưới password, bỏ error row riêng từng làm nút Login thấp hơn hai route còn lại;
- browser geometry QA khóa parity ở `390×844`, `768×1024`, `1366×768`, `1920×1080`; frontend unit/architecture `140/140` pass.

### W2 — Employee

| Logical route | Status | Notes |
|---|---|---|
| `dashboard.my-orders` | VERIFIED_F5 | Shared data story/order-detail, native scroll, canonical filter/transient motion |
| `dashboard.history` | VERIFIED_F5 | Summary → trend → paged list + bounded virtualized detail; persistent/overlay responsive behavior |
| `dashboard.catalog` | VERIFIED_F5 | Read-only server-paged collection, canonical toolbar/footer and full-height workspace |
| `dashboard.order-create.new` | VERIFIED_F5 | Select → review → submit; shared stepper/data surface and editable quantity |
| `dashboard.order-create.edit` | VERIFIED_F5 | Update lifecycle and current order detail composite regression pass |
| Copy previous | PENDING | Diff and source context |
| Additional request | PENDING | Reason/quota/current attempt |

**W2 History approved design contract — owner review 2026-07-23:**

- default scope is all periods; the route does not auto-filter to the current period, while wide desktop auto-selects the first visible order for the persistent detail panel;
- after the compact scope control, wide desktop becomes a true two-column workspace: the left column owns KPIs → period chart → exact order list, while the persistent detail column starts beside the KPIs and spans the full story height;
- do not repeat the `Lịch sử đơn` tab label inside the content and remove helper copy such as `Phạm vi` when the segmented control is already self-explanatory;
- the main order list never owns horizontal page scrolling and fills the remaining desktop viewport height; low result counts leave a calm empty table region instead of collapsing the workspace, while page capacity is measured from the real available card height and excess orders use server-side paging;
- desktop/tablet columns remain single-line with ellipsis and responsive column reduction; mobile uses cards instead of forcing a wide grid;
- wide desktop keeps list and detail as two persistent, top-aligned columns; compact screens keep the detail hidden until selection and then use an overlay to preserve usable width;
- only the bounded 500-item detail region may scroll vertically; product code and name share one single-line cell and long text uses ellipsis instead of wrapping;
- all card edges, headings, controls, chart plot, grid header and numeric axes share the same outer alignment and system-font scale as the authenticated sidebar/header;
- implementation uses current revisions only, server-side summary/list queries, lazy detail loading and bounded DOM virtualization; route-real QA must cover low-row auto-height and a 500-item order.
- quantity trend charts keep a linear, data-driven value axis with no fixed maximum; the formatter uses locale-aware `N0`, while a future order-of-magnitude spread may switch to compact `K/M` labels or a logarithmic axis only after owner review. Current history quantity DTOs remain `int`, so “vô hạn” is a product-scale concern requiring a `long`/database range decision, not a chart-only CSS fix.

**W2 History detail-filter overflow follow-up — owner feedback 2026-07-23:**

- the detail toolbar keeps a stable proportional split instead of expanding from content (`search : category ≈ 1.65 : 1` on desktop and `1.35 : 1` on mobile); long selected values may use single-line ellipsis as the final constraint while retaining the complete value in the control tooltip;
- the category popup owns full-text disclosure: it uses a viewport-safe width, wraps long options and introduces vertical scrolling only when the option list exceeds the available viewport height;
- project-owned select menus close on pointer interaction anywhere outside the select, including the chart, table, detail panel, header or sidebar; interaction inside the trigger/menu remains uninterrupted;
- route-real QA now selects the longest available fixture category and asserts label/icon separation plus no toolbar overflow.

**W2 History table-axis follow-up — owner feedback 2026-07-23:**

- outer grid tracks are not sufficient when Radzen header and cell wrappers have different alignment classes; each history column now carries a shared semantic alignment class for header and data content;
- route-real QA compares actual text/badge visual anchors (left edge, center or right edge), not only `th`/`td` bounding boxes, so a visually shifted row cannot pass by sharing the same outer track.
- desktop history retains the complete nine-column contract from the approved mockup, including `Ngày gửi` and `Ghi chú`; responsive fractional tracks prioritize complete header labels, while long row notes alone may ellipsize with their full value retained in the cell title.

**Shared visual rules — owner authority, updated 2026-07-23:**

- consistency wins over local decoration: a new screen must reuse the existing project tokens, system-font stack, control sizing, state colors and interaction rhythm; if a design breaks a shared rule, report the conflict before implementing it;
- primary content axis aligns to the first primary-header tab label; the first control row is vertically centered against the first sidebar navigation row, with route-real DOM geometry deciding any ambiguous case;
- compact controls use an OpenAI-like grouped surface: one quiet container, flat items, selected item uses the project primary blue, no permanent blue border, and focus uses a neutral high-contrast ring instead of a browser-blue outline;
- icon-only actions embedded beside text use a borderless 20px visual box with a 14–15px lightweight symbol, align to the text centre and reveal only a restrained hover/focus surface; do not stack a visible button border around an icon whose glyph already contains overlapping squares or another enclosing shape;
- selects, search fields, date fields and filter controls share the same compact hit area, radius, typography and hover/focus behavior; dense route filters use a project-owned menu so the popup does not fall back to browser-blue native styling, while date entry may remain native;
- filter triggers use the neutral surface for `Tất cả`/`All` and project blue only when a specific value is active; search controls follow the same rule when their input is non-empty; clear-filter remains an enabled interactive control in both states, returns every related trigger/search to default, and uses blue only when there is something to clear;
- system font and hierarchy follow the sidebar/header scale; labels stay compact, values carry emphasis, text wraps only when required, and ellipsis is preferred for dense single-line data;
- responsive data tables do not hard-code pixel/percentage widths for every column; use content-priority fractional tracks (`minmax(0, fr)`), give identity/code fields a larger share based on real data length, and hide lower-priority columns before allowing horizontal scroll;
- shared table alignment is semantic and data-driven: identifiers/text and notes align left, lifecycle/status badges align center, quantities/counts align right, and date/time fields align center; header and cell wrappers must share the same alignment class and padding;
- a master list and its persistent detail table reuse the same header surface, typography, separator color, semantic column alignment and disclosure behavior; detail-only fields must not be silently omitted when the DTO already exposes them, and user-facing Vietnamese labels prefer complete terms such as `Đơn vị` over internal abbreviations such as `ĐVT`;
- visual centring is a global acceptance rule, not a route-local polish step: compact badges, chips, legend toggles and inline icon/text controls use an explicit border-box height, `inline-flex`, `align-items/justify-content:center`, a normalized line box and stable border width so selected/hover states never shift neighboring content; categorical table headers, cells and badges must share the same measured centre axis, while mixed-size inline metadata aligns by the approved visual centre or baseline for that component;
- all Vietnamese date/time display uses the centralized `DateFormatter` with `vi-VN`: `dd/MM/yyyy` for dates, `HH:mm dd/MM/yyyy` for date-time values, and `MM/yyyy` for period labels; do not introduce route-local date format strings;
- content cards use restrained borders/shadows and no Liquid Glass treatment; motion is short and functional (`150–200ms`) and must respect `prefers-reduced-motion`;
- desktop master-detail keeps the detail surface persistent and removes an unnecessary close affordance; mobile may use an overlay with an explicit close action;
- populated, refreshing, filter-empty and no-data states preserve the exact same desktop grid tracks: the detail column is never removed from DOM, chart/SVG and list cards keep their rendered bounds, and refresh feedback overlays last-known content instead of swapping regions to differently sized skeletons; skeleton replacement is reserved for the first load when no prior content exists;
- semantic colors remain stable across routes: primary blue for regular/action identity, amber warning for additional orders, and status colors for lifecycle state only.

**W1 My Orders round-1 evidence — 2026-07-19:**

- thay toolbar + bốn KPI rời rạc bằng một story header: kỳ hiện tại, deadline, trạng thái và hạn mức bổ sung có một nguồn hiển thị;
- KPI chỉ tính dữ liệu kỳ hiện tại và rút còn ba câu hỏi: số đơn, số mặt hàng, tổng số lượng;
- CTA chỉ xuất hiện ở story header; empty state không lặp lại Tạo đơn/Sao chép kỳ trước;
- đơn thường, đơn bổ sung hiện tại và kỳ trước được phân tầng; kỳ trước thu gọn thành archive row để giữ current story above-the-fold;
- khi period API không xác định được trạng thái, UI hiển thị `Không xác định` thay vì suy diễn là đã đóng kỳ;
- browser QA pass ở `390×844`, `768×1024`, `1366×768`, `1920×1080`, gồm VI/EN, overflow, legacy icon, duplicate CTA và above-the-fold contract;
- isolated lifecycle E2E có dữ liệu pass: sửa đơn → cập nhật → lịch sử → hủy; frontend unit/architecture tests `140/140`; Release solution build `0 warning / 0 error`.

**W1 My Orders round-2 owner direction — 2026-07-19:**

- giữ header ở vai trò product/global utility shell; chỉ khóa lại nhịp, hitbox và ranh giới trước account;
- sidebar collapsed phải còn wayfinding rõ, active parent phản ánh route con; metadata ngày không được giả dạng navigation item;
- primary tab dùng full-height hitbox, hover phủ trọn container và active underline; secondary tab vẫn thấp hơn một bậc phân cấp;
- thay story header + ba KPI ngang cấp bằng period command center theo thứ tự `kết luận → bằng chứng → hành động`;
- trạng thái kỳ và trạng thái đơn phải tách nghĩa; deadline rail là controlled wow có giá trị vận hành;
- bỏ thông tin demo kỹ thuật khỏi bảng người dùng, giảm ưu tiên mã hàng và tránh lặp kỳ/trạng thái/CTA.

**W1 My Orders round-2 implementation evidence — 2026-07-19:**

- primary/secondary tabs có full-height hitbox, hover/focus rõ và `AriaLabel` cho tablist;
- collapsed sidebar dùng rounded-square active surface, phản ánh section theo URL và không render date metadata như một icon navigation;
- header giữ product/global hierarchy, thêm ranh giới nhẹ trước account menu;
- period command center hợp nhất trạng thái kỳ, kết luận theo dữ liệu, deadline rail, item/quantity evidence, quota và action panel;
- bảng hạ mã hàng thành metadata phụ, chỉ render cột ghi chú khi có business note; demo seeder không còn ghi provenance vào description;
- `dotnet build gtas_vpp.sln -c Release --no-restore` pass `0 warning / 0 error`;
- frontend tests `142/142`, backend tests `397/397` pass; browser automation độc lập bị chuyển tới logout vì không dùng phiên đăng nhập của owner, nên visual runtime chờ owner review trên phiên `dotnet-watch` hiện có.

**W1 shell round-3 owner feedback — 2026-07-19:**

- owner duyệt hover/interaction của tab và muốn sidebar cùng nút hamburger dùng chung mô-típ;
- interaction token được chuẩn hóa thành full hitbox, primary tint nhẹ, icon/text tăng nhấn và focus ring rõ;
- không dùng translate/scale làm item dịch chuyển; active state vẫn mạnh hơn hover và giữ indicator theo trục điều hướng.

**W1 shell round-3 implementation evidence — 2026-07-19:**

- sidebar expanded/collapsed và hamburger cùng dùng `7%` primary tint khi hover, radius `md` và transition không dịch chuyển;
- hover tăng nhấn icon bằng primary color; active/current section dùng tint mạnh hơn và giữ left indicator;
- keyboard focus có outline nội bộ nhất quán với tab;
- frontend tests `142/142` pass, CSS brace validation và `git diff --check` pass.

**W1 My Orders round-4 owner feedback — 2026-07-19:**

- bỏ lặp kỳ giữa eyebrow và takeaway title; copy VI/EN phải đúng ngữ cảnh thay vì ghép máy móc;
- chuẩn hóa toàn bộ user-facing terminology sang `mặt hàng` / `item`;
- quota bổ sung chuyển vào chính CTA để người dùng thấy khả năng thực hiện ngay tại action;
- evidence strip thay quota trùng lặp bằng nhiều dữ liệu có ích hơn: lần gửi gần nhất, mặt hàng có số lượng cao nhất và dữ liệu kỳ trước.

**W1 My Orders round-4 implementation evidence — 2026-07-19:**

- period chỉ còn một nguồn hiển thị trong eyebrow; takeaway đổi thành `Đơn của bạn đã được gửi` / `Bạn chưa gửi đơn`;
- `Đang nhận đơn` đổi thành `Kỳ đang mở`, deadline copy rút gọn và thời điểm gửi dùng đúng nhãn;
- quota được đưa vào CTA dạng `Tạo đơn bổ sung · 3/3`; action panel mô tả bước tiếp theo thay vì lặp trạng thái đơn;
- evidence strip gồm tổng mặt hàng, tổng số lượng, lần gửi gần nhất, mặt hàng có số lượng cao nhất và số đơn kỳ trước;
- resource VI/EN, insight tự động, CSV export, Design DNA preview và UI test đã thống nhất `mặt hàng` / `item`; terminology audit không còn `sản phẩm`, `vật tư`, `dòng hàng` trong runtime surfaces;
- Release solution build pass `0 warning / 0 error`; frontend tests `143/143`, backend tests `397/397`; Design DNA Engine/Web/Tests build pass.

**W1 shell + My Orders round-5 owner feedback — 2026-07-20:**

- notification icon vẫn dùng màu secondary trong khi theme control dùng primary, làm header utility group chưa thật sự đồng bộ;
- account popover chỉ hiển thị tên và mã phòng ban nên chưa kể rõ `tôi là ai → đang ở ngữ cảnh tổ chức nào → hành động phiên làm việc`;
- sau khi bỏ lặp kỳ, `07/2026` trở thành metadata quá nhỏ và chìm; cần tăng visual hierarchy nhưng không đưa kỳ trở lại title hoặc lặp thêm nơi khác.
- description `Theo dõi xử lý hoặc tạo đơn bổ sung...`, block `Hành động tiếp theo` và CTA bổ sung đang nói cùng một ý; action copy không phải data quan trọng nên không được chiếm ba vùng.

**W1 round-5 implementation direction — 2026-07-20:**

- notification và theme icon dùng cùng primary color/hover treatment; regression test phải so cả icon color, không chỉ border/surface geometry;
- user popover theo thứ tự `identity → username → department name + code → logout`, không thêm field không hỗ trợ quyết định;
- kỳ hiện tại dùng editorial time anchor: nhãn nhỏ, giá trị lớn/đậm ở đầu story; vẫn chỉ có một nguồn hiển thị period;
- bỏ block `Hành động tiếp theo`; khi đã gửi đơn, description chỉ mô tả khả năng theo dõi, còn nhu cầu bổ sung chỉ xuất hiện một lần tại CTA có quota;
- áp dụng nguyên tắc dashboard chính chủ: context rõ, thông tin quan trọng lớn hơn, màu nhấn tiết chế và layout dẫn mắt từ trên-trái xuống evidence/action.

**W1 shell + My Orders round-5 implementation evidence — 2026-07-20:**

- notification bell và theme toggle dùng cùng surface, border, hover và màu primary trong Light/Dark; browser regression so cả icon đang hiển thị sau khi transition hoàn tất;
- account popover dùng thứ tự `họ tên → tên đăng nhập → tên phòng ban + mã → đăng xuất`, bỏ dữ liệu không giúp quyết định và giữ logout neutral mặc định;
- kỳ hiện tại chỉ xuất hiện một lần dưới dạng time anchor `07/2026`, lớn và đậm hơn nhãn nhưng không lặp lại trong takeaway title;
- bỏ hoàn toàn `Hành động tiếp theo` và câu mô tả bổ sung trùng nghĩa; description chỉ còn lịch sử xử lý, CTA là nơi duy nhất hiển thị `Tạo đơn bổ sung · còn/tổng`;
- Release solution build pass `0 warning / 0 error`; frontend tests `143/143`; isolated browser QA pass cho My Orders ở `390×844`, `768×1024`, `1366×768`, `1920×1080` và account/header Light/Dark.

**W1 My Orders round-6 owner feedback — 2026-07-20:**

- bỏ hẳn câu `Theo dõi chi tiết đơn và lịch sử xử lý.` ở trạng thái đã gửi vì title, bảng chi tiết và tab lịch sử đã đủ truyền đạt;
- command center đang bị kéo quá ngang: CTA chiếm một cột riêng nhưng phần lớn cột là khoảng trống, deadline rail dài hơn giá trị thông tin;
- năm evidence chưa cân bằng và `0 đơn kỳ trước` bị lặp với archive row ngay bên dưới;
- count của section nằm quá xa title; order header trình bày như form hai hàng nên tốn chiều cao và làm table bị tách khỏi identity/action của đơn.

**W1 round-6 implementation direction — 2026-07-20:**

- dùng command center một cột: identity/title và status/action cùng header, deadline thành rail compact có giới hạn chiều dài, evidence thành grid bốn cột cân bằng;
- trạng thái đã gửi không render description; chỉ trạng thái empty/closed/unknown mới dùng explanatory copy khi thực sự cần;
- bỏ previous-cycle count khỏi evidence, giữ archive row làm nguồn duy nhất cho kỳ trước;
- đặt section count sát title; nén order metadata thành hai field ngang `mã đơn` và `thời điểm gửi`, giữ action cùng header và DataGrid làm evidence ngay bên dưới.

**Design reference extraction cho round 6:**

- **Apple:** simplicity không đồng nghĩa với xóa sạch; mỗi phần tử phải có mục đích, copy ngắn, hierarchy rõ và UI phải tránh cản trở tác vụ chính;
- **Notion:** tạo nhịp bằng spacing chuẩn; các block liên quan đứng sát nhau hơn, chuyển section mới mới dùng khoảng thở lớn hơn;
- **Figma UI3:** ưu tiên nội dung thay vì chrome, đặt control quan trọng gần đúng context, giữ action sẵn dùng nhưng không cấp một panel riêng khi không cần;
- **Linear:** giao diện bình tĩnh, nhất quán và dễ quét; navigation/chrome lùi lại để nội dung vận hành nổi lên;
- GTAS VPP áp dụng các nguyên tắc trên theo ngữ cảnh enterprise: sắc nét, ít decoration, action rõ, density vừa và không hy sinh VI/EN, accessibility hoặc Radzen behavior.

**W1 My Orders round-6 implementation evidence — 2026-07-20:**

- submitted state bỏ description hoàn toàn; title rút thành `Đơn đã gửi` / `Order submitted`, section rút thành `Chi tiết đơn` / `Order details`;
- command center bỏ dedicated action column: status và CTA nằm cạnh context ở header; deadline rail giới hạn chiều dài và bốn evidence chia grid cân bằng;
- previous-cycle count không còn ở evidence; archive row là nguồn duy nhất mô tả kỳ trước;
- count nằm sát section title; order header chuyển từ label/value form hai dòng thành hai metadata field ngang, action giữ cùng hàng;
- Radzen docs đã được đối chiếu cho contextual Button/DataGrid composition; Release build pass `0 warning / 0 error`, frontend `143/143`, isolated browser tests `2/2` pass gồm bốn viewport và submitted lifecycle state.

**W1 My Orders round-7 owner feedback — 2026-07-20 — historical, superseded by D-OPENAI-UI-01:**

- round 6 vẫn mang cảm giác dashboard template và còn quá nhiều lớp trang trí;
- owner yêu cầu route này chỉ học Apple, không pha Notion/Figma/Linear: nhẹ, đơn giản và tối ưu;
- gradient, left accent, progress rail, pill background, outlined CTA, evidence kéo hết chiều ngang và card lồng nhau đều làm nội dung nặng hơn giá trị thực.

**W1 round-7 Apple-led direction — 2026-07-20 — historical evidence only:**

- `Simplicity`: mỗi thành phần phải có mục đích; bỏ progress visualization khi deadline text đã đủ trả lời;
- `Hierarchy`: dùng typography và spacing để phân cấp; không dùng nhiều màu, gradient hoặc border cạnh tranh;
- `Agency`: primary action ở ngay context, dùng một filled accent button; secondary/status controls giữ monochrome;
- `Lists and tables`: text ngắn, header rõ, row dễ quét; table nền phẳng với hairline separator thay vì zebra/card decoration nặng;
- `Color`: chỉ giữ accent cho primary action và status dot; không dùng cùng màu để trang trí text, icon và background đồng thời;
- Apple HIG là bộ lọc thẩm mỹ chính, không phải mẫu để sao chép pixel; W3C/Deque, Radzen/Microsoft và nguồn data visualization chính chủ vẫn được dùng cho accessibility, component behavior và cách trình bày dữ liệu mà HIG không đặc tả đủ cho dashboard web;
- các nguồn Notion/Figma/Linear ở round 6 chỉ còn là lịch sử nghiên cứu, không còn là art direction chủ động của `dashboard.my-orders`.

**W1 round-7 MCP/reference decision — 2026-07-20 — no longer active for GTAS:**

- Apple cung cấp MCP chính thức qua Xcode nhưng yêu cầu macOS/Xcode, không phù hợp workstation Windows hiện tại;
- chọn Sosumi Apple Docs MCP vì truy xuất on-demand Apple Developer Documentation, HIG và WWDC, không cần API key và không cài index cũ vào repository;
- đã thêm global endpoint `https://sosumi.ai/mcp` và xác minh handshake MCP `2025-06-18`, server `sosumi.ai` version `1.0.0`;
- Sosumi là dịch vụ mã nguồn mở không chính thức; mỗi quyết định quan trọng phải giữ liên kết Apple gốc, browser runtime và QA GTAS vẫn là authority cuối.

**W1 My Orders round-7 implementation evidence — 2026-07-20:**

- overview chuyển thành content section phẳng: bỏ gradient, left accent, progress rail và border-card; kỳ, takeaway, deadline và evidence dùng typography/spacing làm hierarchy;
- status dùng dot + text; CTA bổ sung là filled primary action duy nhất, không shadow; bốn evidence co theo nội dung thay vì giãn toàn viewport;
- section count đổi thành metadata trong ngoặc, archive rỗng bỏ số `0` lặp, order status bỏ pill background/border và technical fixture note không còn xuất hiện trên UI;
- order detail giữ một grouped surface nhẹ, header nén thành code + thời điểm gửi, table dùng hairline/horizontal rows và không zebra decoration;
- Release solution build pass `0 warning / 0 error`; frontend tests `143/143`; isolated My Orders responsive + regular lifecycle `2/2` pass; submitted screenshot đã chờ row dữ liệu render trước khi capture;
- route vẫn ở `OWNER_REVIEW`; chưa tạo golden baseline và chưa commit W1 cho đến khi owner duyệt runtime.

**W1 My Orders round-8 owner feedback — 2026-07-21:**

- khôi phục composition command center đã được owner thích: surface xanh nhẹ, left accent, period/status/action rõ, deadline rail và evidence ngang;
- đặt `07/2026` cùng dòng ngay sau `Kỳ đặt hàng hiện tại`, không tách thành time anchor hai dòng;
- nghiệp vụ chỉ cho tối đa một đơn bổ sung được duyệt; CTA không hiển thị quota dạng `3/3` hoặc `1/1`, chỉ ghi `Tạo đơn bổ sung`;
- art direction toàn cục chuyển sang vuông/góc cạnh: card, button, input, tab, menu, dialog, notification và panel dùng corner radius bằng `0`; chỉ giữ hình tròn cho avatar, status dot và biểu tượng có semantics hình tròn.

**W1 round-8 implementation direction — 2026-07-21:**

- phục hồi hierarchy và bố cục theo screenshot owner cung cấp nhưng không phục hồi copy trùng lặp đã loại bỏ ở các round trước;
- giới hạn supplement phải được enforcement ở backend policy/config/service và test, không chỉ ẩn quota trên UI;
- radius thay đổi qua shared design tokens/Radzen variables trước, sau đó audit các literal radius còn lại theo từng route để tránh CSS override phân mảnh;
- W1 quay về `CHANGES_REQUESTED`; sau implementation phải chạy Release build, unit tests và isolated responsive/browser QA trước khi owner review lại.

**W1 My Orders round-8 implementation evidence — 2026-07-21:**

- command center đã khôi phục surface xanh nhẹ, left accent, status, deadline progress, năm evidence và contextual action column; không khôi phục copy submitted bị owner đánh giá là lặp;
- `Kỳ đặt hàng hiện tại 07/2026` nằm cùng một centerline trên desktop; responsive tự wrap ở viewport hẹp;
- CTA supplement dùng copy `Tạo đơn bổ sung` / `Create supplement`, không còn quota fraction; backend default/config/legacy fallback đều enforcement `MaxApprovedSupplements=1` (2026-07-26 đã nâng lên `3` theo luận văn §1.2.3 — xem quyết định trong `docs/execution/ATLAS-001.md`), test và tài liệu quyết định đã đồng bộ;
- shared radius tokens, Radzen/Bootstrap compatibility variables và shell controls chuyển sang góc vuông; avatar, status dot và icon có semantics hình tròn được giữ lại;
- Release solution build pass `0 warning / 0 error`; frontend tests `144/144`, backend tests `410/410`; isolated My Orders + header/user-menu browser QA pass `2/2` và không overflow trên bốn viewport;
- route trở lại `OWNER_REVIEW`; evidence local mới nằm ngoài repository tại `%TEMP%\\gtas-vpp-w1-round8`.

**W1 My Orders Apple order-workspace direction — 2026-07-22:**

- owner yêu cầu thử lại My Orders với Apple Store Order Status/Order Details làm nguồn tham khảo chính, nhưng nghiệp vụ/API hiện hành vẫn thắng visual reference;
- data story phải làm rõ theo thứ tự: kỳ đặt hiện tại → đơn chính đã đặt → đơn bổ sung → đơn kỳ trước → action theo từng order;
- `xóa` trong ngôn ngữ người dùng được triển khai bằng nghiệp vụ **hủy đơn** hiện có, không hard-delete; edit/cancel tiếp tục bị khóa bởi `CanEdit`/`CanCancel` từ backend theo status, deadline, current revision và settlement;
- đơn kỳ trước là read-only ở UI và chỉ còn action xem lịch sử, kể cả khi dữ liệu response bất thường; không tạo edit/cancel cho archive;
- My Orders chưa có endpoint/module export PDF hoặc Excel riêng. Lượt này chỉ hiển thị hai action disabled có nhãn `Sắp có` / `Coming soon`, không tạo nút giả hoặc thay đổi backend ngoài scope;
- visual chuyển từ command-center nhiều KPI sang order workspace nhẹ: current-cycle header, ba summary cells có ý nghĩa, section order chính/bổ sung/kỳ trước và detail table dùng RadzenDataGrid hiện có;
- route ở `IN_IMPLEMENTATION`; sau thay đổi phải chạy frontend tests, Release build và browser QA responsive trước khi trả lại `OWNER_REVIEW`.

**W1 My Orders Apple order-workspace implementation evidence — 2026-07-22:**

- current cycle trở thành time anchor duy nhất; status/deadline nằm cùng context và ba summary cell tách rõ đơn chính, đơn bổ sung và archive kỳ trước;
- section đơn chính và bổ sung luôn hiện, kể cả empty; action sửa/lịch sử/hủy có text label, còn archive cưỡng chế `isArchive` để không render edit/cancel;
- PDF và Excel hiển thị bằng native disabled button + `VppIcon`, có `Sắp có`/`Coming soon`; chưa gọi API hoặc tạo export giả;
- bỏ progress rail và bốn KPI template; detail table tiếp tục dùng RadzenDataGrid compact/horizontal separators theo pattern đã tra Radzen MCP;
- `dotnet build gtas_vpp.sln -c Release --no-restore` pass `0 warning / 0 error`; frontend unit/architecture tests `148/148` pass;
- isolated responsive My Orders visual test pass ở `390×844`, `768×1024`, `1366×768`, `1920×1080`; isolated regular lifecycle create/edit/history/cancel test pass và screenshot submitted nằm ngoài repository tại `%TEMP%\\gtas-vpp-myorders-apple-final`;
- route chuyển về `OWNER_REVIEW`; chưa tạo golden baseline hoặc commit cho đến khi owner duyệt runtime.

**W1 My Orders hierarchy and interaction refinement — owner feedback 2026-07-22:**

- hierarchy hiện tại chưa phân bậc đủ rõ giữa kỳ hiện tại, section title, order identity và metadata; chuẩn hóa lại title scale thay vì chỉ tăng độ đậm;
- workspace trên màn hình rộng phải có `max-width` và căn giữa; ba summary item chuyển thành card riêng có border/surface nhẹ để không rời rạc;
- embedded item grid giữ row không-clickable theo nghiệp vụ, nhưng phải có separator, zebra/hover trung tính và cột số lượng căn phải để quét số nhanh;
- status và order commands tách thành hai nhóm; action tăng spacing/min-height thay vì đưa vào overflow menu vì mỗi order chỉ có tối đa ba lệnh quan trọng;
- current/supplement/previous empty state giữ icon nhẹ với surface rõ hơn; text phụ và export disabled tăng tương phản nhưng vẫn truyền đạt đúng trạng thái unavailable;
- không thay đổi `CanEdit`/`CanCancel`, archive read-only, cancel semantics hoặc export roadmap; route trở lại `IN_IMPLEMENTATION` cho đến khi Release build, tests và isolated responsive QA pass.

**W1 My Orders hierarchy and interaction implementation evidence — 2026-07-22:**

- workspace giới hạn `1440px` và căn giữa content region; period title giảm dominance, section title/order kind/metadata dùng scale riêng;
- ba summary item trở thành card có border/surface nhẹ và responsive stack; secondary copy cùng disabled export tăng contrast nhưng vẫn phân biệt unavailable;
- item-name column nhận phần rộng còn lại, quantity chuyển `TextAlign.Right`; embedded grid có separator, subtle zebra và neutral hover với cursor mặc định để không giả vờ row-click;
- order status/read-only tách khỏi command group; edit/history/cancel giữ visible, tăng gap và min-height `38px`, riêng coarse pointer đạt `44px`;
- current/supplement/previous empty presentation đều giữ icon context; supplement icon có colored surface thay cho glyph rời;
- `dotnet build gtas_vpp.sln -c Release --no-restore` pass `0 warning / 0 error`; frontend unit/architecture tests `149/149` pass;
- isolated responsive My Orders test pass ở `390×844`, `768×1024`, `1366×768`, `1920×1080`; isolated regular lifecycle có data create/edit/history/cancel pass; evidence nằm ngoài repository tại `%TEMP%\\gtas-vpp-myorders-hierarchy`;
- route trở lại `OWNER_REVIEW`; chưa commit cho đến khi owner duyệt visual runtime.

**W1 My Orders single-viewport tabbed workspace — owner direction 2026-07-23:**

- owner chốt thay ba section dọc bằng một vùng nội dung dùng chung: compressed period header + three summary cards + internal order tabs + one internally scrollable table panel;
- implementation tiếp tục ở Blazor/Radzen authority; chữ `React` trong prompt là context sai với project boundary hiện hành và không biến `scripts/browser` thành application;
- query `tab=0` tiếp tục sở hữu primary Dashboard tab. Internal selection dùng query riêng `orderView=current|supplement|previous` để reload/share không phá route cấp trang;
- regular current/previous order là duy nhất theo `UX_Requests_OneRegularPerUserPeriod`; supplement không tuyệt đối duy nhất vì backend cho phép nhiều attempt rejected/cancelled trước quota approved, nên normal state vẫn một order nhưng UI phải có compact selector fallback nếu API trả nhiều attempt;
- tách order table/meta/actions/empty state thành component dùng chung; ba summary card là selector duy nhất với radio-group semantics, RadzenDataGrid bỏ paging 10 dòng để dùng fixed-height virtualization;
- desktop khoảng `900px` phải giữ page không cuộn khi dữ liệu ngắn; với tối đa khoảng `500` item chỉ grid body cuộn và header dính; mobile cho summary stack và table horizontal scroll;
- không đổi API/DTO, `CanEdit`/`CanCancel`, archive read-only, cancel semantics, create supplement hoặc export roadmap; route ở `IN_IMPLEMENTATION` đến khi build/tests/browser QA đủ empty, populated, URL reload và long-list fixture.

**W1 My Orders single-viewport tabbed implementation evidence — 2026-07-23:**

- ba loại order render qua `VppOrderWorkspacePanel`; summary card và `RadzenTabs` đồng bộ `orderView=current|supplement|previous`, reload giữ đúng view và supplement có selector fallback khi nhiều attempt;
- `RadzenDataGrid` dùng fixed-height body, sticky column header, `AllowVirtualization=true`, overscan `10`; empty state giữ cùng table frame và archive cưỡng chế read-only;
- workspace nới giới hạn từ `1440px` lên `1760px`: vẫn centered/bounded nhưng không tạo gutter lớn làm content trông tách khỏi sidebar ở màn 1920px; shell geometry test xác nhận logo, icon và avatar không lệch ở expanded/collapsed;
- fixture opt-in `GTAS_E2E_LONG_ORDER_LINES=500` chứng minh đủ 500 dòng, DOM row count vẫn bounded, chỉ grid body cuộn và document không phát sinh vertical scroll;
- frontend unit/architecture `149/149` pass; isolated browser QA pass cho responsive/URL/empty, shell responsive + navigation geometry, regular edit-history-cancel và supplement create-approve-reject;
- route chuyển `OWNER_REVIEW`; PDF/Excel tiếp tục disabled `Sắp có` vì chưa có module export thật và chưa commit cho đến khi owner duyệt runtime.

**W1 My Orders shell/column refinement — owner feedback 2026-07-23:**

- số thứ tự, số lượng và ĐVT phải có fixed width; `Số lượng` dùng right axis, `ĐVT` dùng center axis nhất quán cho cả header/cell, còn `Tên mặt hàng` nhận phần rộng còn lại;
- light-mode shell giữ sidebar/top header trên semantic elevated surface, content canvas dùng semantic base surface, card và table tiếp tục elevated để tạo layer rõ thay vì trắng-trên-trắng;
- bỏ trạng thái `Kỳ đang mở`, chỉ giữ deadline; bỏ order-type heading lặp trong grid meta vì summary card và tab đã định danh view;
- giữ nguyên URL-synced tabs, internal scroll + sticky header + virtualization 500 dòng, action edit/history/cancel, create supplement và primary `?tab=0` route; route trở lại `IN_IMPLEMENTATION` đến khi browser geometry/long-list regression pass.

**W1 My Orders shell/column refinement implementation evidence — 2026-07-23:**

- DataGrid khóa `#=56px`, `Số lượng=120px/right`, `ĐVT=96px/center`; item-name column không đặt width và hấp thụ phần còn lại; header title nhận full-width flex contract để cùng trục text với cell;
- light navigation chrome dùng `--vpp-bg-elevated` qua shared/Radzen sidebar token; `.vpp-layout-body` dùng `--vpp-bg-base`, story/summary/order-grid tiếp tục dùng elevated surface;
- period-open badge và order-type label trong panel meta đã bỏ; empty view không render meta header, chỉ còn một empty state trong table frame;
- browser bounding-box QA xác nhận quantity right edge và UOM center lệch không quá `1.5px`; fixed widths đúng tolerance, item-name column co giãn trên `600px` tại desktop;
- frontend unit/architecture `149/149`, shell responsive, My Orders responsive/URL/empty, regular edit-history-cancel và fixture 500-line internal-scroll/virtualization đều pass; Release solution build `0 warning / 0 error`;
- route trở lại `OWNER_REVIEW`; không commit trước visual approval theo gate hiện hành.

**W1 My Orders viewport/performance follow-up — owner feedback 2026-07-23:**

- bỏ internal `RadzenTabs` vì ba summary card đã cùng thực hiện một nhiệm vụ chọn current/supplement/previous; card dùng `radiogroup`/`radio`, mang line-count badge và tiếp tục đồng bộ `orderView`;
- khóa height/min-height/overflow theo chuỗi RadzenBody → content → primary tab panel → order workspace; story/summary không co, selected panel nhận phần còn lại và chỉ grid body cuộn;
- empty current/supplement được phép hiện CTA theo đúng backend capability, previous read-only không có CTA;
- DataGrid body dùng `scrollbar-gutter: stable both-edges` để header/cell giữ cùng trục khi scrollbar xuất hiện;
- giới hạn global navigation `MutationObserver` chỉ xử lý subtree chứa tab/sidebar host, không quét lại indicator cho row churn của virtualized DataGrid;
- frontend unit/architecture `149/149`, Release solution build `0 warning / 0 error`; isolated My Orders QA pass responsive/URL/empty, fixture `500` dòng giữ DOM row bounded và document không cuộn, soak `60s` không tăng row/indicator hoặc phát sinh console error;
- route trở lại `OWNER_REVIEW`; chưa commit theo visual approval gate hiện hành.

**W1 My Orders full-bleed header regression — owner feedback 2026-07-23:**

- owner phát hiện primary header bị inset ở cả bốn cạnh và không nối liền sidebar sau lượt khóa viewport;
- nguyên nhân gồm `.vpp-orders-shell { overflow: hidden; }` clip phần nav đã bù âm `--vpp-layout-body-inset` và page-level `scrollbar-gutter: stable` giữ lại một dải trống bên phải;
- outer shell cho phép overflow visible để header vượt inset; RadzenBody dùng gutter auto còn panel/workspace/grid tiếp tục sở hữu stable/internal scrolling riêng nên không làm trang cuộn lại;
- bổ sung browser geometry contract: cạnh trái header trùng cạnh phải sidebar và cạnh phải header trùng viewport;
- browser QA pass tại `1366×768` và `1920×1080`, screenshot xác nhận không còn bốn cạnh inset; frontend `149/149`, Release solution build `0 warning / 0 error`;
- route trở lại `OWNER_REVIEW`; chưa commit lượt follow-up này.

**W1 sidebar/header seamless chrome follow-up — owner feedback 2026-07-23:**

- owner duyệt full-bleed header nhưng phát hiện sidebar `border-right` vẫn chạy xuyên qua hàng logo/tab, tạo đường cắt giữa logo và `Đơn hàng của tôi`;
- bỏ border toàn chiều cao và Radzen sidebar edge shadow; thay bằng divider nền 1px chỉ bắt đầu từ `--vpp-header-height` trở xuống;
- logo và primary tabs trở thành một surface liền mạch, trong khi ranh giới sidebar/content ở vùng menu vẫn được giữ;
- do Radzen theme được nạp sau authored CSS, seam reset được khóa trên selector runtime `.rz-layout.vpp-layout > .rz-sidebar.vpp-sidebar` với `!important`;
- browser computed-style xác nhận `border-right-width: 0px`, `box-shadow: none`; screenshot/regression pass tại `1366×768` và `1920×1080`; frontend `149/149` pass; route trở lại `OWNER_REVIEW` và chưa commit lượt follow-up.

**Shared refresh reveal — owner direction 2026-07-23 — historical, motion values superseded by D-OPENAI-UI-01:**

- refresh/direct document load dùng một motion language chung cho logo/sidebar, primary header và content; nền shell được paint ngay để tránh white flash;
- sidebar content fade + dịch ngang `6px`, primary header fade + dịch dọc `-6px`, panel fade + nâng `8px`/scale `0.997`; cùng easing `cubic-bezier(0.32, 0.72, 0, 1)` và stagger tối đa `85ms`;
- marker được gắn trong `<head>` trước first paint, tự gỡ sau `680ms`, không chạy lại khi Blazor internal navigation và bỏ ngay khi BFCache restore;
- `prefers-reduced-motion` tắt hoàn toàn entry animation;
- browser refresh regression xác nhận root marker tồn tại tại `DOMContentLoaded`, sidebar đang chạy đúng keyframe và marker tự gỡ trong `2s`; frontend `150/150`, Release solution build `0 warning / 0 error`;
- shared shell trở lại `OWNER_REVIEW`; chưa commit lượt follow-up.

**Unified toolbar baseline refinement — owner feedback 2026-07-23:**

- owner xác nhận vertical seam đã hết nhưng continuous white chrome làm logo trông nổi rời và tổng thể thiếu điểm neo;
- chọn unified-toolbar pattern: giữ logo/tab cùng white surface, không khôi phục vertical seam, kéo một hairline ngang xuyên suốt dưới cả sidebar header và primary tabs;
- divider dọc vẫn chỉ bắt đầu dưới header, tạo T-junction rõ nhưng nhẹ; dùng semantic border token, không hard-code màu;
- browser screenshot tại `1366×768`/`1920×1080` xác nhận baseline liền từ logo qua tabs và T-junction sạch; My Orders responsive/refresh regression pass, frontend `150/150`;
- shared shell trở lại `OWNER_REVIEW`; chưa commit lượt follow-up.

**Collapsed navigation optical offset — owner feedback 2026-07-23:**

- owner nhận thấy cụm bốn root icon bám quá sát unified-toolbar baseline và trông bị kéo lên;
- giảm header-to-nav overlap từ `10px` xuống semantic `--vpp-space-1` (`4px`), dịch toàn cụm nav/active surface/shared indicator xuống `6px` mà không đổi khoảng cách icon-icon;
- browser geometry xác nhận rounded first root control đã clear baseline, screenshot `1366×768`/`1920×1080` cho thấy cụm icon cân hơn; My Orders regression và frontend `150/150` pass;
- shared shell trở lại `OWNER_REVIEW`; chưa commit lượt follow-up.

**Collapsed hover-surface rhythm — owner feedback 2026-07-23:**

- owner yêu cầu khoảng hairline → hover surface icon 1 bằng khoảng hover surface icon 1 → icon 2, tính theo thị giác chứ không chỉ tâm glyph;
- bỏ hoàn toàn header/nav overlap còn lại và thêm `--vpp-sidebar-row-half-gap` (`2px`) trước nav: wrapper vốn có top margin `2px`, nên line-to-first gap = inter-row gap = `4px`;
- browser regression đo trực tiếp bounding box của header và hai root hover surfaces; hai gap đạt tolerance `1.5px`, screenshot `1366×768`/`1920×1080` xác nhận active/hover surface không bám hairline;
- shared shell trở lại `OWNER_REVIEW`; chưa commit lượt follow-up.

**W1 nested sidebar surface follow-up — owner feedback 2026-07-23:**

- owner phát hiện sidebar container đã trắng nhưng child/grandchild rows vẫn dùng nền xám cũ;
- nguyên nhân là Radzen PanelMenu dùng token nền riêng cho level 1/2/3, trong khi lượt trước chỉ đổi navigation chrome và active tokens;
- override cả ba idle background token về transparent, thêm third-level active token và reset explicit idle wrapper; giữ nguyên hover/active tint, hierarchy indent, shared indicator và motion;
- route tạm trở lại `IN_IMPLEMENTATION` đến khi architecture + expanded nested-menu browser QA pass.

**W1 nested sidebar surface implementation evidence — 2026-07-23:**

- `.vpp-sidebar .rz-panel-menu` đặt default background token của level 1/2/3 về `transparent`; wrapper idle explicit reset transparent, third-level active vẫn dùng shared active tint;
- shell palette không hard-code literal: content dùng `--vpp-bg-base`, navigation chrome/card dùng `--vpp-bg-elevated` thông qua `--vpp-navigation-chrome-bg` và Radzen sidebar token;
- architecture `149/149` pass; isolated expanded-sidebar regression xác nhận toàn bộ inactive child/grandchild wrapper có computed background `rgba(0, 0, 0, 0)` và geometry/indicator/motion vẫn pass;
- route trở lại `OWNER_REVIEW`; giữ gate chưa commit trước visual approval.

**W1 shared reconnect direction — 2026-07-22 — historical, visual direction superseded by D-OPENAI-UI-01:**

- owner yêu cầu thiết kế lại reconnect theo Apple HIG nhưng không thay đổi global `InteractiveServer` hoặc cơ chế circuit reconnect hiện hành;
- trạng thái tự khôi phục dùng spinner nhỏ, copy ngắn và không có action; chỉ khi `failed`, `paused` hoặc `resume-failed` mới hiển thị nút hành động;
- bỏ ripple trang trí và backdrop blur nặng; alert giữ title cụ thể, informative text ngắn, một primary action và touch target tối thiểu `44px`;
- giữ đầy đủ state contract chính thức của Blazor: `show`, `retrying`, `failed`, `rejected`, `paused`, `resume-failed`, `hide`; `rejected` tiếp tục reload vì circuit cũ không còn khả dụng;
- bổ sung VI/EN, dark mode, focus-visible, forced-colors và reduced-motion; route/state ở `IN_IMPLEMENTATION` cho đến khi build, frontend tests và isolated browser QA hoàn tất.

**W1 shared reconnect implementation evidence — 2026-07-22:**

- markup tách rõ automatic reconnect, retry countdown, connection failed, paused và resume-failed; failed/paused có đúng một primary action, rejected vẫn reload theo contract Blazor;
- alert dùng compact surface, neutral scrim, spinner/icon theo state, copy VI/EN ngắn và không còn ripple; dialog focus ring mặc định được bỏ nhưng button giữ `focus-visible` rõ;
- accessibility gồm `aria-live`, `aria-busy`, action focus khi cần, touch target `44px`, reduced-motion, forced-colors và responsive width;
- `dotnet build gtas_vpp.sln -c Release --no-restore` pass `0 warning / 0 error`; frontend unit/architecture tests `149/149` pass;
- isolated `ReconnectModalVisualTests` pass đủ năm state ở `390×844` và `1366×768`, không overflow và không render action ở automatic retry; evidence nằm ngoài repository tại `%TEMP%\\gtas-vpp-reconnect-apple`;
- shared state chuyển về `OWNER_REVIEW`; chưa commit cho đến khi owner duyệt runtime cùng W1 My Orders.

### W3 — Management

| Logical route/state | Status | Notes |
|---|---|---|
| `dashboard.management.department` | PENDING | Department scope + queue |
| `dashboard.management.all` | PENDING | Company scope + drill-down |
| Supplement approval | SOURCE_COMPLETE_RUNTIME_PENDING | Queue/detail/action đã theo Atlas; quota x/3 cần DTO riêng |
| Reject dialog | SOURCE_COMPLETE_RUNTIME_PENDING | Lý do bắt buộc và consequence giữ nguyên qua component split |

### W4 — Procurement/Period

| Logical route/state | Status | Notes |
|---|---|---|
| `dashboard.period-operations` | SOURCE_COMPLETE_RUNTIME_PENDING | Permission-dependent sub-tabs; host đã chẻ R-2 |
| Period review | SOURCE_COMPLETE_RUNTIME_PENDING | Hero + readiness blockers từ preview thật |
| Additional approval queue | SOURCE_COMPLETE_RUNTIME_PENDING | Sort chờ lâu nhất, detail/action tách component |
| Supplier/price comparison | SOURCE_COMPLETE_RUNTIME_PENDING | Supplier-first; đơn giá thật từ item-prices + giá ngoại lệ từ preview |
| Settlement preview | IMPLEMENTED — QA PASS | Supplier/price-list decision, exceptions và input hash dùng API thật; post-mutation explicit re-preview còn chờ owner decision |
| Confirm settlement | IMPLEMENTED — QA PASS | Immutable snapshot + idempotency/four-eyes backend contract; mutation E2E hai user đã pass ngày 2026-08-04 |
| Settled/revision view | IMPLEMENTED — CORRECTION UX PENDING | Status/export/current correction target đã có; cần notice + `Xem trước lại` trước correction kế tiếp |

### W5 — Library

| Logical route | Status | Notes |
|---|---|---|
| `library.classes` | SOURCE_COMPLETE_RUNTIME_PENDING | Two-pane hierarchy, soft-delete only |
| `library.categories` | SOURCE_COMPLETE_RUNTIME_PENDING | Server grid + inspector actions |
| `library.items` | SOURCE_COMPLETE_RUNTIME_PENDING | Product fields/filter/active state |
| `library.suppliers` | SOURCE_COMPLETE_RUNTIME_PENDING | Address visible + contact/active/audit |
| `library.departments` | SOURCE_COMPLETE_RUNTIME_PENDING | Server grid + inspector actions |
| `library.pricing.price-lists` | SOURCE_COMPLETE_RUNTIME_PENDING | Lifecycle + inspector, localized status filter |
| `library.pricing.prices` | SOURCE_COMPLETE_RUNTIME_PENDING | Product/list/supplier/version filter |

### W6 — Access Control

| Logical route/state | Status | Notes |
|---|---|---|
| `permission.user` | ISOLATED_QA_PASS | Activation/mapping/session theo backend contract |
| `permission.component` | ISOLATED_QA_PASS — OWNER_REVIEW | Canonical group ListDetail + batch UI editor; API matrix read-only; mutation batch + live-session restore pass |
| `permission.security-audit` | VERIFIED | Read-only server-paged audit, filter/options API, adaptive detail, VI/EN, desktop/mobile và header/sidebar navigation pass |
| `permission.order-period-settings` | SOURCE_COMPLETE — ROUTE_QA_PENDING | System Admin tạo settings version mới bằng Radzen form; lịch sử version dùng DataGrid; kỳ đã tạo không bị sửa ngược |
| User inspector | VERIFIED | Identity/membership/audit; loại secret/session stamp |
| Reset/revoke actions | SOURCE_COMPLETE | Confirm + durable feedback; owner review còn mở |

### W7 — Reports/AI states

| Logical route/state | Status | Notes |
|---|---|---|
| `report` overview | VERIFIED | KPI + exact table theo DTO thật |
| Trend/status story | VERIFIED | Trend `TotalAmount`; chart suy biến dùng empty state |
| Department/product story | VERIFIED | DataGrid breakdown + top item từ response thật |
| Settlement reconciliation | VERIFIED | Snapshot/evidence khi `SettlementId` tồn tại |
| Export/Print | VERIFIED | CSV/XLSX download E2E + A4 print hardening |
| AI insight states | OWNER_REVIEW | Evidence/fallback/governance giữ theo capability hiện hành |

### W8 — Global hardening

| Target | Status | Notes |
|---|---|---|
| Light/Dark consistency | ISOLATED_QA_PASS | Representative route gate + semantic contrast fixes |
| Print mode | VERIFIED | Hide shell/action/toast/reconnect; flatten shadow/overflow |
| VI/EN | SOURCE_VERIFIED | Resource contract + account language regression; user data giữ nguyên |
| Mobile/tablet | VERIFIED | 28 screen tại 390/768, không page overflow |
| Accessibility | VERIFIED | Focus/labels/live regions + representative axe pass |
| Performance | REGRESSION_PASS | 112 route/viewport render không console/network failure; paging/virtualization giữ nguyên |
| Final visual regression | OWNER_APPROVED — GOLDEN_DEFERRED | 17 PNG settled làm review evidence; tạo golden sau khi clean HEAD tái tạo đúng visual đã duyệt |

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

> **Append-only historical log:** các decision/feedback row bên dưới giữ nguyên để giải thích vì sao UI
> đã đổi. Nếu một row cũ mâu thuẫn với current contract, ưu tiên mục 6.6, mục 14 và
> `FRONTEND-REFACTOR-001`; riêng role/group chỉ hiển thị ở sidebar, không áp dụng lại role badge trong header.

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
| 2026-08-18 | Global DataGrid header + Giá mặt hàng | Header cột giữ một dòng và một chiều cao ổn định; nhãn dài phải được rút gọn, cấp đủ width hoặc chuyển thành cột phụ trong column picker trước khi dùng ellipsis. Bảng giá và file import chỉ dùng `Mã mặt hàng` thống nhất của hệ thống | Header xuống dòng làm nhịp bảng không đều và khó quét. Một mã mặt hàng chung giúp file mẫu, import, export và tra cứu không phải duy trì ánh xạ mã phụ | Global `DATA-COLUMN`; local Pricing + database cleanup | Gỡ global wrap override, khóa header `nowrap` theo token; giữ horizontal scroll/column picker. Xóa trường mã phụ khỏi UI, shared contract, import/export, backend và schema bằng migration riêng | Mọi Radzen DataGrid; Pricing import/export; CatalogPricing; settlement snapshot; database migration; tests | IMPLEMENTED — QA PASS; backend `555/555` (trừ manifest HTTP lệch sẵn ngoài scope), frontend `502/502`, migration LocalDB fresh/down/up/re-apply `1/1`, pricing route-real 1366/390 `1/1` |
| 2026-08-14 | Global DataGrid gutter + Chốt kỳ badge tracks | Mọi data-surface DataGrid chừa sẵn một gutter scrollbar native ở cạnh cuối để có/không có overflow không làm schema cột nhảy. Hai nhóm badge `Tổng đơn` và `Trạng thái` dùng hai track cố định; badge body và footer cùng tâm/chiều rộng dù số là `1`, `51` hay `100` | Owner nhận thấy grid có scrollbar làm layout dịch ngang và badge tổng co theo số dài nên lệch quang học với badge body | Global `DATA-FRAME`; local consumer `STATUS-BADGE`/`CATEGORY-CHIP` Chốt kỳ | Radzen data-surface bridge dùng `scrollbar-gutter: stable`, không `both-edges`; settlement group đổi flex content-width sang 2-column grid, selected-filter vẫn thu về 1 track | Mọi canonical DataGrid; ba grid Chốt kỳ có geometry assertion body/footer | IMPLEMENTED — QA PASS; frontend `443/443`, Release build sạch, settlement route-real `4/4`, F4 Catalog/Users/OrderCreate `1/1`; History full-flow dừng trước scrollbar gate do baseline filter height `32/32/32/28` ngoài scope |
| 2026-08-14 | Chốt kỳ — nhấn dòng tổng và trạng thái đếm bằng 0 | Dòng `Tổng cộng` dùng nền, chữ và đường nhấn amber/cam rõ hơn dữ liệu thường nhưng không mang nghĩa lỗi. Các badge đếm trong `Tổng đơn` và `Trạng thái` luôn giữ đủ mục; mọi giá trị `0` (`Đơn thường`, `Đơn bổ sung`, `Đã gửi`, `Đã duyệt`) chuyển sang muted. Badge ở dòng tổng dùng variant emphasized dạng filled, giữ đúng tone xám/cam/xanh dương/xanh lá của từng loại nhưng khác rõ badge nền nhạt ở body | Owner cần dòng tổng nổi bật như báo cáo, badge tổng khác badge dòng thường và số không không cạnh tranh thị giác với số có dữ liệu | Global token + primitive `DATA-FOOTER`, `STATUS-BADGE`, `CATEGORY-CHIP`; consumer Chốt kỳ | Thêm token `--vpp-data-summary-*`, `--vpp-content-muted-opacity` và typed `Muted`/`Emphasized`; không suy luận từ text, consumer truyền theo dữ liệu số | Ba grid Chốt kỳ; architecture + route-real light/dark/responsive | IMPLEMENTED — QA PASS; frontend `443/443`, settlement route-real `4/4`; filtered-empty xác nhận cả bốn badge `0` vẫn giữ geometry và muted, visual light/dark xác nhận badge tổng filled khác rõ badge body |
| 2026-08-14 | Chốt kỳ — schema ba grid, filter và dòng tổng | Ba grid dùng tên ngắn, cùng nghĩa: `Dòng mặt hàng`, `Số lượng`, `Thành tiền`, `VAT`, `Tổng cộng`; Theo người đặt tách `Người dùng` và `Phòng ban`; Theo phòng ban thêm `Đặt nhiều nhất`. Filter nhóm theo đúng thứ tự cột `Phòng ban → Loại đơn → Trạng thái`. Khi lọc loại đơn/trạng thái, cột badge tương ứng thu về đúng giá trị đã chọn. Badge Tổng đơn/body/footer cùng tâm tiêu đề; toàn bộ chữ dòng tổng dùng info-strong và ô thao tác footer không frozen/separator | Owner review phát hiện badge lệch tâm, frozen seam đè dải tổng, tên cột dài bị ellipsis, người dùng trộn phòng ban trong dòng phụ và filter không cùng thứ tự schema | Local `DATA-COLUMN` + `FILTER-TOOLBAR` + `DATA-FOOTER` + `STATUS-BADGE` | Mở rộng projection frontend bằng top item và status counts từ snapshot đơn/demand đã có; không thêm API/database. Cập nhật resource VI/EN, focused projection/architecture tests và route-real settlement geometry | `SettlementWorkspaceProjection`, `PeriodSettlementPanel`, resource VI/EN, projection + architecture + route-real tests | IMPLEMENTED — QA PASS; Release build sạch; focused unit/architecture `15/15`; route-real `4/4` tại 390/768/1366/1920; geometry xác nhận badge cùng tâm và footer action không sticky/separator |
| 2026-08-14 | Global selector option state + Chốt kỳ decision cards | Option đang chọn trong mọi dropdown dọc giữ nền trung tính, dùng chữ medium và dấu check; màu info chỉ xuất hiện khi hover/focus. Hàng quyết định Chốt kỳ chia bốn card NCC, bảng giá, PDF, Excel có chiều ngang bằng nhau | Owner phát hiện option đầu tiên bị tô xanh dù chỉ đang selected, tạo cảm giác hover/active giả; hai card quyết định rộng hơn card xuất làm hàng trên mất cân bằng | Global `SELECTOR-FILTER`, `SELECTOR-DECISION`, `SELECTOR-PAGE-SIZE`; local settlement decision grid | Chuẩn hóa selected state trong hai composite project-owned và Radzen dropdown bridge; giữ page-size compact không thêm check; desktop dùng 4 cột bằng nhau, tablet 2 cột, mobile 1 cột | `VppDecisionSelect`, `VppFilterSelect`, Radzen dropdown/page-size, `PeriodSettlementPanel`, architecture + route-real tests | IMPLEMENTED — QA PASS; frontend `438/438`; route-real settlement `4/4` tại 390/768/1366/1920; geometry xác nhận bốn card lệch tối đa 1px và selected option có nền trong suốt |
| 2026-08-14 | Chốt kỳ — preview thống nhất và bốn card quyết định/xuất file | Dùng một CTA duy nhất: `Chốt kỳ` khi chưa chốt và `Chốt lại kỳ` khi đã có bản chốt. CTA mở dialog workspace cho phép kiểm tra/đổi NCC, bảng giá, xem đơn, tổng trước VAT/VAT/tổng giá trị và yêu cầu sửa đơn. Lần chốt lại hiển thị thay đổi so với bản gần nhất. Dòng trên gồm đúng bốn card đồng nhịp: NCC, bảng giá, PDF, Excel; hai card xuất chỉ khả dụng sau khi đã chốt và không còn nằm trong header grid | Owner muốn toàn bộ quyết định được kiểm tra tại một nơi trước khi sinh bản mới; select lồng trong card và export trong header làm hierarchy rời rạc. Backend cũ tự sinh bản chốt ngay khi duyệt sửa đơn nên mất bước xác nhận cuối | Local `SELECTOR-DECISION` + `DIALOG-EDITOR` + `COLLECTION-ACTION`; backend settlement workflow | Duyệt sửa/hủy đơn chỉ cập nhật đơn và đánh dấu chờ chốt lại; pending correction chặn submit. Chỉ CTA trong dialog mới tạo bản chốt N+1 và liên kết các thay đổi đã duyệt. Bỏ dialog hiệu chỉnh cũ và action header trùng | `PeriodSettlementPanel`, `Dialog_SettlementPreview`, `VppDecisionSelect`, correction/settlement services, focused unit/architecture/route-real tests | IMPLEMENTED — QA PASS; Release build sạch; frontend `438/438`; backend focused `12/12`; route-real dialog/4-card responsive `4/4` tại 390/768/1366/1920 và ảnh light/dark đã review |
| 2026-08-14 | Chốt kỳ + selector thời gian/kỳ | Bỏ hai KPI tiền ở đầu trang vì dòng `Tổng cộng` đã hiển thị số liệu theo cột; thay bằng thanh `Phương án chốt` compact chỉ gồm nhà cung cấp và bảng giá. Picker `Tùy chọn` dùng `VppDecisionSelect`, bỏ dòng kỳ lặp, thêm icon/heading gọn; DatePicker Radzen dùng cùng control height, focus, popup, radius và shadow. Dòng tổng bỏ toàn bộ separator/pseudo-element của cột thao tác frozen | Owner nhận thấy KPI tiền trùng dòng tổng, hai select NCC/bảng giá còn giống card rời, popup kỳ lặp thông tin và frozen action seam đè lên dòng tổng | Global `SELECTOR-TIME-SCOPE`; local `SELECTOR-DECISION` + `DATA-FOOTER` | Nâng shared period picker cho History/Department/Settlement; normalize toàn bộ DatePicker hiện có qua Radzen bridge; route Chốt kỳ chỉ giữ decision bar, không nhân thêm KPI/card | `VppPeriodPickerPopover`, `Dialog_OrderPeriodAction`, `PeriodSettlementPanel`, architecture + route-real tests | IMPLEMENTED — QA PASS; frontend build, architecture `185/185`, settlement route-real `4/4`, picker/history/period-action focused gate và visual 390/768/1920 |
| 2026-08-14 | Bảng giá theo nhà cung cấp — thuật ngữ điều khoản | Dialog thêm/sửa bảng giá dùng nhãn nghiệp vụ tiếng Việt rõ nghĩa: `Khoản giảm thêm`, `Phụ phí`, `Phí vận chuyển`; bỏ `Rebate` khỏi UI tiếng Việt. Nhà cung cấp là thông tin bắt buộc cho bảng giá tạo/sửa mới; tiền tệ hiển thị VND cố định theo khả năng hiện hành | Owner phát hiện nhãn tiếng Anh và cần hiểu trường nào thực sự tham gia tính tiền chốt kỳ | `DIALOG-EDITOR` + localization; catalog-pricing validation | Toàn bộ nhãn đi qua VI/EN resource; form giải thích điều khoản áp dụng cho toàn bảng giá; backend từ chối bảng giá mới không có nhà cung cấp, không migration hoặc sửa dữ liệu cũ | `Dialog_PriceListEditor`, `PriceListService`, resources và focused tests | IMPLEMENTED — QA PASS; backend `9/9`, frontend architecture `1/1`, route-real dialog `1/1` |
| 2026-08-14 | Bảng giá — ẩn thông tin chưa cần nhập | Form thêm/sửa chỉ còn mã, tên, nhà cung cấp, cờ mặc định và mô tả. Ẩn `Mã hợp đồng tham chiếu`, chiết khấu, khoản giảm thêm, phụ phí, phí vận chuyển và đơn vị tiền tệ khỏi UI; bảng giá mới tiếp tục mặc định `VND` | Hiển thị disabled vẫn tạo nhiễu và gợi ý chức năng đã sẵn sàng; xóa backend/schema lại làm mất khả năng bật lại và có nguy cơ ảnh hưởng dữ liệu cũ | `DIALOG-EDITOR`; UI-only compatibility | Không bind/render các trường hoãn lại; không reset giá trị khi sửa; `PriceListUpdateReqDTO.CurrencyCode` giữ default `VND`; giữ DTO/API/database và resource để tái đánh giá sau. Dialog dùng profile Standard | `Dialog_PriceListEditor`, shared DTO, dialog profile, architecture + route-real test | IMPLEMENTED — QA PASS; architecture `25/25`, route-real `1/1`, visual 1366×768 reviewed |
| 2026-08-14 | Bảng giá — select NCC và thuật ngữ nhân bản | Form dùng `VppDecisionSelect` cho nhà cung cấp, popup/hover/check theo design system và không còn search box Radzen lệch motif. Action `Sao chép thành bảng giá mới` đổi thành `Nhân bản bảng giá`; toast và tên bản sao dùng cùng thuật ngữ | Owner phát hiện dropdown editor còn dùng visual Radzen thô và copy action dài/khó hiểu | Global `SELECTOR-DECISION`; local `DIALOG-EDITOR` + localization | `PriceList.Id` vẫn là GUID nội bộ; `PriceListCode` tiếp tục là mã nghiệp vụ tối đa 50 ký tự, dùng cho tìm kiếm/import/đối chiếu. Nhân bản tạo GUID mới và sao chép toàn bộ dòng giá từ nguồn; khi không có NCC hoạt động, trigger giữ nguyên vị trí nhưng disabled | `Dialog_PriceListEditor`, `Tab_PriceListLibrary`, VI/EN resources, clone service evidence, architecture + route-real test | IMPLEMENTED — QA PASS; architecture `25/25`, Release build `0 warning/error`, route-real `1/1`, visual 1366×768 reviewed |
| 2026-08-19 | My Orders — giữ thao tác tạo và sao chép trong empty state | Khi chưa có đơn thường, `Tạo đơn kỳ này` là primary và `Sao chép đơn kỳ trước` là secondary. Hai thao tác cùng hiện; thao tác chưa khả dụng bị mờ và có lý do, không biến mất | Logic cũ chỉ chọn một CTA và luôn ưu tiên tạo đơn, khiến sao chép dù backend cho phép vẫn không bao giờ được render | M2 My Orders + global `CAPABILITY-SURFACE` | Shared order-items surface hỗ trợ hai empty actions; quyền vẫn quyết định visibility, capability kỳ quyết định enabled/disabled | `VppOrderItemsSurface`, `VppOrderWorkspacePanel`, `Tab_Orders`, VI/EN resources, architecture test | IMPLEMENTED — QA PENDING |
| 2026-08-15 | Bảng giá — nhãn chọn nhà cung cấp một dòng | Form thêm/sửa bảng giá chỉ hiển thị một câu `Chọn nhà cung cấp`, không xếp thêm eyebrow `Nhà cung cấp áp dụng` phía trên | Hai lớp nhãn lặp cùng ý làm control cao và tạo cảm giác có hai trường thông tin | Local `DIALOG-EDITOR` trên global `SELECTOR-DECISION` | Giữ placeholder làm nhãn hiển thị duy nhất; giữ aria-label mô tả cho trợ năng và toàn bộ logic chọn/validation hiện hành | `Dialog_PriceListEditor`, architecture + route-real test | IMPLEMENTED — QA PASS; architecture `1/1`, Release build `0 warning/error`, route-real dialog `1/1` |
| 2026-08-14 | Chốt kỳ — tổng đơn, nhãn dữ liệu và header action | Gộp `Đơn thường`/`Đơn bổ sung` vào một cột `Tổng đơn` bằng hai category badge đúng tone system; đổi `Tổng mặt hàng` thành `Dòng mặt hàng` vì dữ liệu là tổng line, không phải distinct item. Chuyển `Chốt kỳ` vào collection header; đổi copy thành `Bản chốt N`, `Lịch sử chốt`, `Điều chỉnh bản chốt` và giữ chỉnh đơn ở drawer riêng | Owner review phát hiện schema dài, nhãn `Tổng mặt hàng` gây hiểu nhầm và action của cùng một kỳ bị chia giữa hai vùng | Local `DATA-COLUMN` + `COLLECTION-ACTION` + `STATUS` | Giữ projection/backend hiện tại; đổi schema/copy/layout, dùng VppCategoryChip Neutral/Accent; bỏ footer action rời và thu grid min-width | Ba grid phòng ban/người đặt + header Chốt kỳ; mutation/workspace/architecture tests | IMPLEMENTED — QA PASS; workspace route-real `4/4`, mutation `2/2`, DS3 responsive `6/6`, visual light 1920/mobile 390/version 1–2 reviewed |
| 2026-08-14 | Chốt kỳ — phân cấp thị giác dòng tổng | Dòng tổng bỏ border trên riêng để không tạo đường đôi với separator của item cuối; đổi sang nền info rất nhạt, nhãn info-strong và giữ số semibold | Owner review phát hiện seam bị chồng và dòng tổng chưa đủ khác dữ liệu thường | Local `DATA-FOOTER` + `DATA-COLUMN` | Chỉ đổi route-local CSS, giữ nguyên height, sticky, schema, total semantics và dark/light token | Ba grid Chốt kỳ; route-real geometry + visual | IMPLEMENTED — QA PASS; route-real `4/4`, light 1920 và dark 1366 đã review |
| 2026-08-13 | Chốt kỳ — dòng tổng hợp theo cột | Thêm một dòng `Tổng cộng` trong footer cột DataGrid, nằm ngay trên pager và thẳng theo schema hiện tại. Tổng tính trên toàn bộ kết quả sau lọc, trước paging; đổi trang không làm số tổng thay đổi. Phòng ban/Người đặt cộng các metric đếm, số lượng và tiền. Mặt hàng dùng số đơn không trùng, cộng số lượng/tiền; đơn giá và tỷ lệ VAT hiển thị `–` vì không có ý nghĩa cộng | Owner muốn cách đọc giống báo cáo/Excel nhưng layout Chốt kỳ có horizontal scroll và cột thao tác frozen; thanh tổng rời hoặc KPI bổ sung sẽ mất liên kết với cột và dễ lệch ở laptop | Local `DATA-FOOTER` + `DATA-COLUMN`, dùng capability Radzen hiện hữu | Dùng typed aggregate projection + `RadzenDataGridColumn.FooterTemplate`; không gọi thêm API, không cộng rải trong Razor. Footer giữ nền trung tính, số căn phải/tabular, không card/radius/shadow; pager được trả về token canonical, hàng tổng 40px ghim sát phía trên | Ba grid chính trong `PeriodSettlementPanel`; unit projection và route-real 390/768/1366/1920 | IMPLEMENTED — QA PASS; unit `7/7`, route-real `4/4`, visual 1920 reviewed |
| 2026-08-13 | Các kỳ đặt hàng + grid compact | Mỗi dòng luôn có hai thao tác chính trực tiếp: `Xem` và `Chốt kỳ`; `Chốt kỳ` disabled khi chưa đủ điều kiện. Menu `...` chỉ còn `Gia hạn kỳ`; bỏ `Sửa lịch`, `Mở lại nhận đơn` và `Xóa kỳ` khỏi collection. Chỉ kỳ `Đang mở` được gia hạn; các trạng thái khác giữ trigger `...` ở vị trí cũ nhưng disabled. Dialog chỉ cho kéo dài ngày đóng và hạn duyệt bổ sung, bắt buộc lý do. DataGrid có ít bản ghi giữ nhịp compact; empty state vẫn lấp đầy và căn giữa | Các kỳ trong luồng thực tế được hệ thống mở đồng thời, nên thao tác sửa toàn bộ lịch không còn phù hợp. Owner muốn một hành động vận hành rõ nghĩa, không trùng lặp hoặc cho phép sửa ngược lịch đang chạy | Global `DATA-ROW` + local `ADMIN-ROW-ACTIONS`, `CAPABILITY-SURFACE` | Giữ `Gia hạn kỳ` là capability phụ duy nhất; dọn nhánh dialog edit/reopen/delete và bỏ request settings thừa khỏi critical path. API update schedule phía backend/client vẫn giữ tương thích nhưng không publish tại collection | Mọi DataGrid canonical có ít dữ liệu; `OrderPeriodManagementWorkspace`, period route-real tests | IMPLEMENTED — QA PASS; build 0 warning/error; architecture `12/12`; route-real responsive/dialog `4/4` tại 390/768/1366/1920 |
| 2026-08-13 | Global — Stable Capability Surface implementation | Thêm typed surface state và canonical empty component; action/menu truyền lý do disabled; cùng entity giữ action group ổn định, action/cột thiếu quyền bị ẩn. Các kỳ đặt hàng giữ DataGrid khi empty; Library admin dùng canonical empty template; My Orders, Approval và Settlement giữ action có quyền nhưng disabled theo business state | Contract đã được owner duyệt; cần enforcement thực tế để các chức năng mới không quay lại kiểu ẩn action hoặc tháo cả grid khi empty | Global `CAPABILITY-SURFACE`, `ADMIN-ROW-ACTIONS`, `DATA-FRAME`, `CONTENT-STATE` | Retrofit shared components, Library/Pricing/Permission, Period/My Orders/Approval/Settlement, Security Audit; thêm architecture assertions và token empty-height | Toàn bộ frontend data/action surface hiện hành | DONE — build sạch; frontend `434/434`; route-real content state `2/2`, period `10/10`, data surface `5/5`; visual catalog/history/my-orders 1920 reviewed |
| 2026-08-13 | Global — Stable Capability Surface | Với cùng entity và cùng quyền, giữ nguyên action chính, thứ tự menu và data-surface shell giữa các trạng thái. Action chưa đủ điều kiện nghiệp vụ vẫn hiện disabled; chỉ hidden khi thiếu quyền, không thuộc workflow hoặc chưa phát hành. Grid/list base-empty và filtered-empty giữ header, toolbar, column header, body, footer/pager; empty căn giữa body và không có fake-row hover/click | Owner muốn người dùng biết đầy đủ chức năng tồn tại, không phải đoán action bị thiếu; đồng thời empty state không được làm grid/list co lại hoặc đổi bố cục | Global `CAPABILITY-SURFACE` phối hợp `ADMIN-ROW-ACTIONS`, `CONTENT-STATE`, `DATA-FRAME` | Thêm canonical motif và FR9 migrate-on-touch; audit permission/business-state/action-order cùng base-empty/filtered-empty/error cho từng consumer; không mass-rewrite trước lượt owner kiểm tra chức năng mới | Toàn bộ action group, admin grid, operational list, analytics/static surface; `OrderPeriodManagementWorkspace` là reference đầu tiên | APPROVED — FR9 PLANNED; reference Period implemented, toàn project chưa retrofit |
| 2026-08-13 | Các kỳ đặt hàng — action cố định theo capability | **SUPERSEDED bởi record action trực tiếp ở trên.** Bản đầu dùng `Xem chi tiết` làm action chính và đặt `Chốt kỳ` trong menu; deep-link đúng tháng/năm và dialog chi tiết cũ đã được loại bỏ | Owner review tiếp xác nhận `Xem` và `Chốt kỳ` đều là thao tác chính, nên `Chốt kỳ` không nên nằm trong overflow | Local `ADMIN-ROW-ACTIONS`, dùng shared menu capability | Giữ deep-link và stable disabled behavior; chuyển `Chốt kỳ` ra ngoài, rút nhãn `Xem`, overflow chỉ còn thao tác phụ | `OrderPeriodManagementWorkspace`, shared `VppAdminActionMenu`, period route-real tests | SUPERSEDED — xem record mới 2026-08-13 |
| 2026-08-13 | Sidebar thu gọn — popup tài khoản và divider shell | Divider dọc chỉ phân cách nền sidebar/content; khi popup tài khoản mở, toàn bộ popup phải nằm trên divider, không xuất hiện đường dọc cắt nội dung | Owner phát hiện pseudo-element divider của shell dùng lớp cao hơn sidebar nên vẫn phủ lên account popup dù popup đã có z-index transient đúng | Global `SHELL-NAV` + `TRANSIENT` | Hạ divider xuống dưới lớp sidebar; khóa architecture và route-real z-index để popup collapsed không bị đường shell xuyên qua | Mọi transient surface neo trong sidebar mở/thu gọn | IMPLEMENTED — QA PASS; architecture `49/49`, isolated route-real `1/1`, ảnh collapsed 1366 đã kiểm bằng mắt |
| 2026-08-10 | Các kỳ đặt hàng — DatePicker alignment và bỏ đóng sớm | Trigger lịch trong mọi `RadzenDatePicker` phải nằm đúng tâm dọc của input, không nhảy ở hover/focus và phải mở popup ngay lần click đầu. Route kỳ không hiển thị `Đóng nhận đơn sớm`; kỳ tự đóng theo lịch, quản lý chỉ sửa lịch, gia hạn hoặc mở lại khi capability cho phép | Button density bridge đang xóa `translateY(-50%)` của icon nội tại DatePicker; selector hover/focus có specificity cao hơn làm trigger tụt xuống khỏi con trỏ và click thất bại. Action đóng sớm lặp trách nhiệm scheduler và làm menu vận hành khó hiểu | Global Radzen DatePicker bridge; local `ADMIN-ROW-ACTIONS` và period lifecycle copy | Loại trigger DatePicker khỏi reset transform của action button, giữ neo giữa ở hover/active/focus, căn button-box/glyph bằng flex center; gỡ close action/dialog khỏi projection UI; giữ API tương thích nội bộ | Mọi DatePicker; `OrderPeriodManagementWorkspace`, `Dialog_OrderPeriodAction`, route/browser tests và execution record | IMPLEMENTED — QA PASS; frontend `426/426`, architecture `2/2`, route-real hover/click `4/4` tại 390/768/1366/1920; popup-open visual reviewed |
| 2026-08-10 | Chốt kỳ + data states — performance, filter và no-refresh | Trang Chốt kỳ chỉ đưa dữ liệu của view đang mở vào critical path; view Mặt hàng tải lười khi người dùng chọn và cache trong vòng đời component. Grid mặc định dùng snapshot tóm tắt, không tải `Items` từng đơn; chi tiết chỉ tải khi bấm Xem. Trạng thái/revision/correction sau chốt là dữ liệu phụ, tải sau và không giữ skeleton. Các kỳ đặt hàng có toolbar tìm kỳ/thay đổi gần nhất + trạng thái + năm; skeleton `FillAvailable` phủ đều toàn thân grid; empty/full state căn giữa hai trục. Empty state và notification không có nút `Làm mới`; chỉ error state giữ `Thử lại` | Owner vẫn thấy grid chậm dù status/preview đã hiện. Trace code cho thấy request snapshot từng gửi `orderby`, ép backend tải toàn bộ rồi sort in-memory; đồng thời route chờ demand của tab chưa mở và correction/revision phía dưới. DTO đầy đủ còn materialize toàn bộ item detail dù grid chỉ dùng số tổng hợp | Global `FILTER-TOOLBAR`, `CONTENT-STATE`, `SKELETON`; local settlement progressive loading + Requests summary query | Tách header/grid/deferred loading; `EnsureCurrentViewDataAsync` chỉ tải demand cho Items, cache orders/demand/directory; cache projection đã lọc để không rebuild theo từng cell. Thêm `summaryOnly=true` với projection header + aggregate tại SQL, giữ endpoint cũ cho consumer cần full detail. Correction query theo đúng `PeriodId`. Hạ performance gate từ 12s xuống 6s. Giữ filter, full skeleton/content-state và no-refresh retrofit của wave trước | Period settlement/management; `VPPRequest/all-orders` summary branch; shared loading/content state; all frontend empty/manual refresh consumers | IMPLEMENTED — focused QA PASS; solution build 0 warning/error; frontend `28/28`; backend summary controller `1/1`; isolated initial grid `2342–2920ms` (<6s gate). Broader DS3 test vẫn lộ assertion loaded-surface full-height cũ tại 1366, tách khỏi critical-path performance fix |
| 2026-08-10 | Các kỳ đặt hàng — action consistency và khóa kỳ | Action chính dùng nhãn ổn định theo hành động: kỳ đã chốt vẫn hiện `Chốt kỳ` nhưng disabled, không đổi thành `Đã chốt`. Mọi dòng luôn có menu `...` với `Xem chi tiết`, kể cả Đang chốt/Đã chốt. Copy vận hành đổi `Khóa nhận đơn / Mở lại nhận đơn` thành `Khóa kỳ / Mở khóa kỳ`; mô tả vẫn nói rõ khóa kỳ chỉ ngừng nhận đơn, chưa phải chốt | Owner phát hiện action settled đổi từ động từ sang trạng thái, còn các dòng không có capability phụ bị mất luôn đường xem chi tiết; thuật ngữ khóa/mở khóa kỳ ngắn và đối xứng hơn trên trang quản trị | Local period action projection theo global `ADMIN-ROW-ACTIONS`; status resource VI/EN | Luôn thêm DetailsAction trước capability actions; gom `SubmissionClosed/Pricing/Settled` về CTA `Chốt kỳ`, disabled với Settled; đổi panel/toast/resource/help copy sang Khóa kỳ/Mở khóa kỳ; test khóa menu ở mọi row và settled CTA disabled | `OrderPeriodManagementWorkspace`, `Dialog_OrderPeriodDetails`, period state localization, route-real tests, multi-period execution record | IMPLEMENTED — QA PASS; frontend `425/425`, route-real `5/5` tại 390/768/1366/1920, visual 1366 reviewed |
| 2026-08-10 | Chốt kỳ — deferred PO/NCC boundary | **REVISED bởi record “bỏ reopen” bên dưới.** Phase hiện tại chưa làm module PO/NCC và không hỏi quản lý tự xác nhận `chưa có PO`. Kỳ đã chốt không mở lại; điều chỉnh hợp lệ tạo bản chốt kế tiếp và giữ bản trước. Cờ tác động mua sắm backend chỉ là điểm nối dự phòng, không có UI/manual action; khi có module PO thật, sự kiện phát hành mới tự bật guard | Owner xác nhận hoàn thiện kỳ đặt hàng trước, chưa triển khai PO; checkbox hiện tại khiến người dùng phải xác nhận một dữ liệu mà hệ thống chưa quản lý và dễ hiểu nhầm chốt kỳ là đã gửi NCC | Local settlement copy + backend future integration boundary | Gỡ toàn bộ dialog/endpoint reopen và copy PO; giữ lịch sử bản chốt, kiểm tra quyền/người thực hiện và ghi rõ PO integration là phase sau, không tạo module hoặc thao tác thủ công giả | Settlement version history, mutation test, multi-period execution record; future PO event integration | REVISED — xem record 2026-08-10 “bỏ reopen” |
| 2026-08-10 | Các kỳ đặt hàng — automation-first collection | Đổi nhóm `Điều hành kỳ` thành `Quản lý kỳ đặt hàng`, child đầu là `Các kỳ đặt hàng`; route chỉ hiển thị danh sách và thao tác trên kỳ đã tồn tại. Bỏ notice cấu hình, nút xem cấu hình, mở đủ số kỳ và mở kỳ rời rạc vì hệ thống tự tạo kỳ; toàn bộ policy nằm ở `Quản trị hệ thống → Cấu hình đặt hàng`. Collection, loading và empty phải lấp đầy vùng khả dụng như Danh mục mặt hàng; empty căn giữa; mọi grid canonical dùng cùng màu header token | Owner xác nhận quản lý không chủ động tạo kỳ và phát hiện period/loading co theo nội dung, empty lệch tâm, header admin grid dùng màu nền khác data-grid canonical | Global `COLLECTION`, `CONTENT-STATE`, `SKELETON`, data-grid header; local navigation/copy | Chuyển period route từ page-mode sang collection adaptive full-height; thêm fill contract cho skeleton; bỏ route-local automation controls/manual panel; chuẩn hóa resource và header grid bridge | Period navigation/workspace; shared SkeletonPage/VppContentState; mọi `.vpp-data-grid` và `.vpp-admin-grid` | IMPLEMENTED — QA PASS; frontend unit `425/425`, focused route/navigation/loading `13/13`, axe pass, build 0 warning/error, visual `390/1920` + loading `1920` |
| 2026-08-10 | Popup menu/page-size — parity với filter canonical | Menu `⋯` và page-size popup phải bắt đầu motion sau khi đã neo đúng trigger; dùng cùng radius, shadow, row rhythm, hover/selected và transient easing với `VppFilterSelect`. Menu chỉ có một surface; page-size cách trigger 4px, không overlap và phải mở/đóng lặp lại ổn định | ContextMenu còn giữ chrome của cả container ngoài lẫn `.rz-menu` bên trong nên xuất hiện viền/halo kép. Dropdown portal từng nhận animation cả khi `rz-close` và CSS che trạng thái đóng, làm lệch vòng đời nội bộ của Radzen sau nhiều lần toggle | Global `TRANSIENT`, `ADMIN-ROW-ACTIONS`, `SELECTOR-PAGE-SIZE` | Reset toàn bộ border/background/shadow của menu con để outer surface là owner duy nhất; chỉ chạy motion page-size khi `rz-open`, không ép ẩn portal đang đóng; tăng khoảng retry neo portal và khóa bằng test non-overlap, single-surface, stable geometry, motion direction cùng 4 chu kỳ mở/đóng ở mỗi viewport | `VppAdminActionMenu`, Radzen pager dropdown và mọi consumer DataGrid | IMPLEMENTED — QA PASS; route-real `4/4` tại 390/768/1366/1920, tổng `16` chu kỳ toggle lặp lại; build 0 warning/error; screenshot được kiểm bằng mắt |
| 2026-08-10 | Chốt kỳ — financial workspace và action không trùng | **REVISED bởi record “bỏ reopen” bên dưới.** Header giữ 4 card: chọn NCC, chọn bảng giá, trước VAT + VAT, tổng giá trị. Ba view dùng số tiền từ bảng giá/bản chốt thật: Phòng ban/Người đặt có `Tạm tính · VAT · Thành tiền (gồm VAT)`; Mặt hàng thêm `Đơn giá · Thuế VAT`; bỏ độ phủ và trạng thái tổng hợp phòng ban. Drawer nhóm có option `Tất cả đơn trong nhóm` khi nhóm có nhiều đơn. Sau chốt chỉ còn `Điều chỉnh sau chốt` để lưu bản mới | UI cũ có hai action trùng nghĩa, dùng `TotalAmount` cũ của đơn cho bảng tổng hợp và hiển thị coverage như một KPI/cột vận hành, dễ làm người dùng hiểu nhầm số tiền và hành động | Local settlement workspace trên global `KPI`, `FILTER-SELECT`, `DATA-GRID`, `DETAIL-DRAWER`; backend preview/versioned financial projection | Mở rộng quote line và allocation DTO; preview phân bổ net/VAT/commercial theo request, snapshot trả item/allocation; FE group theo allocation và cache map giá theo VppId. Decision select dùng selected check trung tính. Page-size không còn phụ thuộc cờ JS để được nhìn thấy, click/keyboard bridge chỉ bổ sung hướng motion | Settlement API/service/shared DTO, `PeriodSettlementPanel`, detail sheet, projection, `VppFilterSelect`, pager bridge/tests | REVISED — financial QA giữ nguyên, action theo record mới |
| 2026-08-10 | Chốt kỳ — bỏ reopen, bản chốt thân thiện và cửa sổ chỉnh đơn | Kỳ đã chốt không quay ngược trạng thái. Header hiển thị `Bản N`, action `Điều chỉnh sau chốt` và `Xem các bản đã lưu`; thay đổi hợp lệ tạo bản kế tiếp, bản trước giữ nguyên. Quản trị hệ thống cấu hình số ngày chỉnh đơn sau ngày đóng, mặc định 10; trong hạn Quản lý được cập nhật/hủy đơn có lý do, sau hạn chỉ chọn NCC/bảng giá và chốt. Chốt sớm được phép khi kỳ đã đóng và không còn đơn bổ sung chờ duyệt; hết hạn duyệt bổ sung thì không duyệt thêm nhưng vẫn cho từ chối đơn còn treo để tránh khóa cứng kỳ | Owner bỏ reopen vì làm vòng đời khó hiểu; copy revision/four-eyes kỹ thuật không phù hợp người vận hành. Mốc chỉnh đơn phải tính từ ngày đóng, tách khỏi thời điểm chốt và hạn duyệt bổ sung 5 ngày | Global `DIALOG-EDITOR`, `CATEGORY-CHIP`, `ADMIN-FORM`; local settlement version history/order adjustment; additive DB compatibility | Gỡ endpoint/client/dialog reopen; giữ cột cũ tương thích DB nhưng không còn runtime/UI. Thêm `PostCloseAdjustmentDays`, deadline/capability trên period, manager adjustment endpoint tạo request revision, early settlement từ SubmissionClosed, full/per-order correction tạo settlement revision kế tiếp. Pending supplement luôn chặn chốt; sau deadline approve bị chặn nhưng reject vẫn mở. Copy dùng `bản chốt`, `điều chỉnh`, `đang dùng`, không lộ thuật ngữ kỹ thuật | Settings/domain/migration, VPP request adjustment, settlement service/API, Chốt kỳ/history dialogs, VI/EN resources, unit/architecture/mutation tests | AUTOMATED QA PASS — OWNER REVIEW; build `0 warning/error`, backend `516/516`, frontend `427/427`, LocalDB `22/22`, focused route-real `4/4` (2 responsive + 2 mutation), EF/format/gitleaks pass |
| 2026-08-10 | Kỳ đặt hàng — toggle menu, ngày giờ và empty audit copy | Nút `⋯` phải đóng bền vững khi click lần hai; ngày giờ dùng `HH:mm dd/MM/yyyy`; audit thiếu dữ liệu chỉ hiện một dấu `–` | Radzen có thể đóng context-menu trước khi Blazor kiểm tra DOM, khiến handler hiểu nhầm là menu đã đóng và mở lại; route còn hardcode format ngày ngược helper chung và render hai dòng fallback thừa | Global `ADMIN-ROW-ACTIONS` interaction; local period presentation | Ghi nhận trạng thái menu từ `pointerdown`, test chờ 350ms để bắt reopen trễ; dùng `DateFormatter.LongDate`; bỏ `Chưa có ghi chú` và dòng timestamp rỗng | `VppAdminActionMenu`, Order period grid/detail, period Playwright gate | IMPLEMENTED — QA PASS; frontend `423/423`, focused browser `1/1`, axe `1/1`, visual `390/1366` |
| 2026-08-09 | Global badge and category-chip semantics | Mọi `VppStatusBadge` dùng một contract màu: Info = đang hoạt động/đã gửi/sắp mở; Success = hoàn thành/đã duyệt/đã chốt; Warning = chờ xử lý/đang chốt; Danger = hủy/từ chối/lỗi; Neutral = nháp/đã đóng/hết hiệu lực. Loại đơn và metric context chuyển sang `VppCategoryChip`; `Đơn thường` neutral, `Đơn bổ sung` accent | Owner phát hiện badge thuộc nhiều loại thông tin đang map màu không đồng bộ giữa Requests, Period, Library và Permission; route-local switch làm cùng ý nghĩa có màu khác nhau | Global `STATUS-BADGE` + `CATEGORY-CHIP` design-system contract | Thêm `VppStatusToneContract`, `VppCategoryChip`; retrofit order/period, active-inactive library, price-list lifecycle, account/audit/access, permission reference và order-create category metadata; architecture test khóa consumer canonical | Toàn bộ `VppStatusBadge` hiện hành và future status/category consumers | IMPLEMENTED — QA PASS; frontend `417/417`, History + Library route-real `2/2`, final History responsive `1/1` |
| 2026-08-09 | History / Department Summary — order status vs period lifecycle | Badge `Trạng thái` chỉ biểu diễn vòng đời đơn (`Đã gửi`, `Chờ duyệt`, `Đã duyệt`, `Từ chối`, `Đã hủy`). Vòng đời kỳ lấy từ `VppPeriod.State` và có cột `Trạng thái kỳ` riêng ngay sau `Kỳ`; không ghép badge và không đặt dòng phụ trong ô kỳ. Danh sách History bỏ hai cột tổng hợp `Mặt hàng` và `Số lượng`; số liệu chi tiết vẫn nằm trong drawer. `VppPeriodStateBadge` theo contract global: Open info, SubmissionClosed neutral, Pricing warning, Settled success | Copy `Đã gửi (đã khóa kỳ)` trộn hai aggregate, secondary text trong cột kỳ quá chật, còn text thuần ở cột riêng chưa đủ rõ khi quét bảng; database đã lưu hai trạng thái độc lập và drawer đã có thống kê mặt hàng/số lượng | Requests wire contract + `STATUS-BADGE` design-system motif + M3 History presentation; không đổi schema | Thêm `periodState` vào `VppRequestResDTO`, materialize từ period aggregate; thêm composite badge canonical compact/full và retrofit History, Department Summary, detail/mobile cùng Quản lý kỳ; filter/export tiếp tục dùng order status | `VppStatusContract`, `VppPeriodStateBadge`, request history/detail API, period management, VI/EN resources và contract/browser tests | IMPLEMENTED — QA PASS; semantic classes and responsive desktop/mobile geometry verified |
| 2026-08-09 | Shared DataGrid empty-state hover | Empty/filtered-empty nằm trong table để giữ geometry nhưng không được nhận hover background, cursor hoặc action-row affordance | Owner phát hiện Radzen render EmptyTemplate thành một `tr`, khiến selector hover data row tô xanh toàn vùng empty dù không có record để tương tác | Global data-grid/content-state interaction | Loại row chứa `.rz-datatable-empty`, `.vpp-content-state` hoặc `.vpp-order-grid-empty` khỏi hover selector; giữ nền trung tính và cursor mặc định; khóa bằng route-real computed-paint test | `VppOrderItemsSurface`, `.vpp-datagrid`, `.vpp-order-grid`, `.vpp-data-grid` và future Radzen EmptyTemplate | IMPLEMENTED — QA PASS; frontend `384/384`, computed paint before/after hover `1/1` |
| 2026-08-09 | Kỳ đặt hàng — tách cấu hình hệ thống khỏi vận hành kỳ | **REVISED bởi record 2026-08-10.** Chuyển số kỳ mặc định, ngày tự mở, ngày đóng mặc định, giờ/múi giờ, hạn duyệt bổ sung và số ngày chỉnh đơn sang `Quản trị hệ thống → Cấu hình đặt hàng`. Mặc định duyệt bổ sung 5 ngày, chỉnh đơn 10 ngày kể từ ngày đóng; quản trị hệ thống vẫn đổi được theo bản cấu hình và tháng hiệu lực. Trang Kỳ đặt hàng chỉ vận hành từng kỳ và dẫn sang Chốt kỳ | Route cũ trộn chính sách toàn công ty với thao tác hằng ngày của MANAGER; `Đóng kỳ` dễ bị hiểu nhầm là chốt kết quả | Global separation of duties + `ADMIN-ROW-ACTIONS`, `STATUS-BADGE`, `TRANSIENT`; backend state/version history | Permission `PERIOD_SETTINGS_MANAGE` chỉ cho SystemAdmin/DEV; route quản trị cấu hình có lịch sử; period route chỉ đọc cấu hình đang có hiệu lực; map `Open / SubmissionClosed / Pricing / Settled` thành `Đang mở / Đã khóa / Đang chốt / Đã chốt`; kỳ đã chốt không quay lại trạng thái cũ | System Admin navigation/settings, OrderPeriods API/UI, settlement deep link/version history, RBAC/migration/tests | REVISED — xem record 2026-08-10 “bỏ reopen” |
| 2026-08-09 | Quản lý kỳ — bỏ hero và summary text lặp page identity | Nội dung route bắt đầu từ action bar chỉ chứa `Mở thêm kỳ / Cấu hình mặc định / Mở đủ số kỳ`; bỏ hero và hai dòng summary `kỳ đang nhận đơn / quy tắc ngày 29–31` phía trên danh sách | Header-sub-tab và chính nhóm `Đang nhận đơn` trong danh sách đã cung cấp đủ scope; hai lớp text tóm tắt chỉ lặp thông tin và chiếm chiều cao hữu dụng | Local route composition, theo global `Header-tab owns page identity` | Xóa slot Header, markup/CSS hero, bỏ summary copy và chuyển browser readiness/scroll target sang action bar/data surface | `/dashboard?tab=5&periodTab=periods` | IMPLEMENTED — QA PASS; frontend `384/384`, browser/axe `5/5` tại 4 viewport |
| 2026-08-09 | Runtime correction — period page scroll + header child default | `VppWorkspaceScrollMode.Page` phải mở toàn chuỗi `.vpp-layout-body → .vpp-content → Radzen panel`, kể cả desktop `1920px`; wheel phải cuộn được tới section cuối. Mọi header/sidebar group khi vào route cha, query rỗng hoặc query sai phải chọn child đầu tiên có quyền; nhóm Quản lý kỳ mặc định là `Quản lý kỳ`, không phải `Chốt kỳ` | Owner runtime screenshot chứng minh section `Các kỳ đã tạo` vẫn bị cắt dù test cũ tại `1280px` dùng programmatic scroll đã pass. Navigation còn ưu tiên `PeriodReview` ở ba fallback khác nhau | Global scroll ownership + `HEADER-TAB-GROUP` | Mở height của `.vpp-content` theo page mode; đổi default order/path/fallback tại navigation catalog, header shell, dashboard tab và `Tab_AdminApproval`; nâng test sang mouse wheel và bottom visibility tại 4 viewport | Page-mode workspaces; Quản lý kỳ; Bảng giá và mọi nested header/sidebar group kế thừa first-accessible-child rule | IMPLEMENTED — QA PASS; frontend `384/384`; wheel/bottom + header default `5/5` tại `390`, `768`, `1366`, `1920` |
| 2026-08-09 | Kỳ đặt hàng — admin collection và tên điều hướng | Nhóm cha đổi từ `Quản lý kỳ` thành `Điều hành kỳ`; child mặc định đổi từ `Quản lý kỳ` thành `Kỳ đặt hàng`. Route quản lý kỳ chuyển từ ListDetail sang danh mục DataGrid: collection header giữ `Mở kỳ / Cấu hình mặc định / Mở đủ số kỳ`, từng dòng có xem, sửa lịch, gia hạn, trạng thái nhận đơn một chiều và xóa theo capability | Owner thấy parent/child trùng tên và bố cục danh sách–chi tiết không đồng bộ motif trang quản trị. DTO đã có đủ capability nên không cần thay API/database | Global navigation copy + local `COLLECTION`, `COLLECTION-HEADER`, `ADMIN-ROW-ACTIONS`, `STATUS-BADGE` | Tách profile route `dashboard.period.periods` thành Collection; giữ đóng nhận đơn là chuyển trạng thái một chiều có form lý do, toggle chỉ biểu diễn trạng thái và bị khóa sau khi đóng; collection header wrap thành hai hàng trên mobile | `OrderPeriodManagementWorkspace`, `UiRouteCatalog`, `VppCollectionHeader`, shell resources, navigation/browser/architecture tests | IMPLEMENTED — QA PASS; build 0 warning/error, frontend `418/418`, route-real + axe `5/5` tại `390`, `768`, `1366`, `1920` |
| 2026-08-09 | Kỳ đặt hàng — gỡ hàng chờ sửa/hủy sau chốt khỏi danh mục | Route `Kỳ đặt hàng` chỉ quản lý collection và lịch kỳ, không hiển thị form/lưới `Sửa/hủy đơn sau chốt`. Hàng chờ xác nhận chuyển về `Chốt kỳ` và chỉ render khi kỳ đang xem thực sự có yêu cầu Pending | Owner phát hiện card rỗng vẫn nằm dưới danh mục kỳ dù đây là workflow xử lý đơn đã chốt, làm màn cấu hình dài và sai ngữ cảnh | Local route ownership: Collection vs Settlement workflow | Gỡ API/state/markup/CSS correction khỏi `OrderPeriodManagementWorkspace`; Chốt kỳ tải correction theo năm/tháng, giữ four-eyes confirm/reject và không render empty placeholder | `OrderPeriodManagementWorkspace`, `PeriodSettlementPanel`, responsive/architecture tests | IMPLEMENTED — KỲ ĐẶT HÀNG QA PASS (`418/418`, browser/axe `5/5`); Chốt kỳ render smoke đạt nhưng focused test còn fail geometry data-surface cũ `354.78px > 2px` |
| 2026-08-09 | History/Department default scope, selector motion và thứ tự kỳ | `Lịch sử đơn` cùng `Tổng hợp phòng ban` mặc định vào `Tất cả kỳ`; deep-link đơn vẫn tự chuyển sang đúng kỳ. Segmented selector không chạy transition ở lần đo indicator đầu tiên khi tab vừa được dựng lại, nhưng giữ animation khi user đổi lựa chọn. Danh mục `Kỳ đặt hàng` sắp mới nhất → cũ nhất | Owner thấy hai màn lịch sử nên mở toàn bộ dữ liệu, selector ngang nháy khi chuyển tab và grid kỳ đang đi ngược thứ tự quản trị thường dùng | Shared History workspace + global `VppSegmentedSelector` + local period collection ordering | Đổi shared scope khởi tạo sang All; thêm initial-sync motion guard trong JS/CSS design system; sort `Periods` và `OpenPeriods` giảm dần theo năm/tháng; cập nhật browser/architecture gates theo responsive drawer và grid 8 cột hiện hành | History, Department Summary, mọi segmented-selector consumer, `OrderPeriodManagementWorkspace` | IMPLEMENTED — QA PASS; build 0 warning/error, frontend `418/418`, History `1/1`, Department `1/1`, Period grid `5/5` |
| 2026-08-09 | Frontend admin action hierarchy | Mỗi page tự xác định action trực tiếp theo mục tiêu, state và permission; không dàn mọi action ngang hàng. Kỳ đặt hàng ưu tiên collection action `Mở đủ số kỳ`; row action theo state là `Khóa nhận đơn / Chốt kỳ / Sửa lịch / Xem`, còn `Mở kỳ rời rạc` là secondary. Duyệt giữ `Duyệt/Từ chối`; Chốt kỳ giữ `Chốt kỳ/Điều chỉnh`; danh mục giữ `Sửa`; bảng giá giữ `Sửa giá/Xem`; người dùng giữ `Duyệt` hoặc access toggle; trang read-only giữ `Xem`. Lifecycle, gửi link và destructive vào menu `⋯` full-text; destructive nằm cuối và có danger tone | Owner thấy icon khó hiểu nhưng nhãn đầy đủ làm grid dài, đồng thời yêu cầu phân action quan trọng chính xác theo từng page | Global `ADMIN-ROW-ACTIONS`, route-owned business priority | Thêm `VppAdminActionMenu`, `VppAdminLifecycleMenu`, hỗ trợ label ngắn trong `VppAdminIconAction`; retrofit Period, Lookup/Category/Item/Supplier/Department, Price List/Price và User; giữ nguyên Approval/Settlement/Audit nơi hierarchy đã đúng | Admin collections, operation decisions và future row-action consumers | IMPLEMENTED — QA PASS; build 0 warning/error, architecture `51/51`, focused browser `8/8`, visual review `390/768/1366/1920` |
| 2026-08-09 | Neo vị trí menu action quản trị | Menu overflow mở tách khỏi nút `⋯`, ưu tiên phía dưới, tự lật lên khi thiếu chỗ và clamp trong viewport | Menu theo tọa độ click của Radzen bung ngược sang trái, che action chính và trigger | Global `ADMIN-ROW-ACTIONS` + `TRANSIENT` | `VppAdminActionMenu` dùng geometry của anchor và module JS dùng chung; E2E khóa invariant không giao nhau và không vượt viewport | Tất cả consumer của `VppAdminActionMenu` | IMPLEMENTED — QA PASS; build 0 warning/error, browser `4/4` tại `390`, `768`, `1366`, `1920` |
| 2026-08-09 | Kỳ đặt hàng — menu, detail và trạng thái dễ hiểu | **REVISED bởi record tách cấu hình/vận hành ở trên.** Overflow menu dùng cùng surface/hover/rhythm với select dọc; `Xem chi tiết` mở standard dialog; management phân biệt `Đang mở / Đã khóa / Đang chốt / Đã chốt`. Action là `Khóa nhận đơn`, không dùng `Đóng kỳ`; dialog dùng copy vận hành `Ngày mở / Ngày đóng / Hạn duyệt đơn bổ sung / Số đơn`, giải thích ngắn hạn duyệt dành cho đơn bổ sung đang chờ và không hiển thị audit kỹ thuật `Thay đổi gần nhất` | Owner thấy menu lệch design system, detail bung dưới grid làm đứt luồng đọc, copy trạng thái kỹ thuật khó hiểu và audit revision không cần thiết trong dialog xem nhanh | `ADMIN-ROW-ACTIONS`, `DIALOG-EDITOR`, `STATUS-BADGE`, `TRANSIENT`; backend state machine giữ chi tiết thật | Style shared `VppAdminActionMenu`; thêm `Dialog_OrderPeriodDetails`; dùng compact state projection không làm mất `SubmissionClosed/Pricing`; chuẩn hóa nhãn ngày trên grid/form/dialog; giữ audit tại collection quản trị nhưng bỏ khỏi dialog | Period management + toàn bộ consumer overflow menu | IMPLEMENTED — QA PASS; current checkpoint frontend `425/425`, period route-real `6/6`, visual review `390/768/1366/1920` |
| 2026-08-09 | Pager, overflow toggle và nhịp cột Kỳ | Page-size selector dùng trigger label-only 56×32 và dropup cùng chiều rộng; chỉ Radzen bridge sở hữu chrome focus/open. Nút `⋯` click lần hai đóng menu hiện tại. Cột `Kỳ` dùng typography cell bình thường, không tự in đậm khác các collection khác | Owner thấy page-size trigger/dropup rộng và có dấu hiệu hai lớp style, menu overflow không toggle, riêng cột Kỳ có emphasis không có lý do nghiệp vụ | Global `SELECTOR-PAGE-SIZE`, `ADMIN-ROW-ACTIONS`; local `DATA-COLUMN` | Portal dropdown được đánh dấu `vpp-page-size-panel` từ trigger pager; gỡ rule pager trùng khỏi `vpp-a11y.css`; action-menu module nhận diện cùng anchor đang mở; bỏ `<strong>` route-local | Mọi paged grid và consumer `VppAdminActionMenu`; Kỳ đặt hàng | IMPLEMENTED — QA PASS; frontend `423/423`, period browser/axe `5/5`, visual review `390/768/1366/1920` |
| 2026-08-09 | Frontend-wide responsive + scroll ownership | Shell vẫn cố định `100dvh`, nhưng page/workspace/grid phải khai báo đúng nơi sở hữu scroll bằng `VppWorkspaceScrollMode` (`Page`, `Internal`, `Adaptive`). Adaptive mở page flow dưới `1440px` hoặc cao dưới `800px`; laptop thu chrome/sidebar/row rhythm, không giảm font. History chỉ ghim master-detail từ `1440px`, còn laptop dùng list toàn chiều ngang + drawer | Root cause là nhiều lớp cùng ép `height: 100%` và `overflow: hidden`, khiến wizard/trang dài bị cắt; đồng thời 1366px vẫn bị xem như desktop rộng nên bảng 9 cột bị ép vào nửa màn hình | Global shell/workspace/data density | Thêm typed scroll mode vào năm workspace pattern, mở bounded Radzen panels theo mode, sửa wizard Order Create và page dài, chuẩn hóa compact tokens, đổi History breakpoint và khóa bằng architecture + route-real browser test | Shared patterns; shell; admin/library/permission/report; My Orders/History; Order Create; Period management/settlement | IMPLEMENTED — QA PASS; frontend `384/384`; focused scroll `1/1`; shell/workspace/catalog regression `7/7`, gồm 24 tổ hợp 6 route × 4 viewport (`390`, `768`, `1366`, `1920`) |
| 2026-08-09 | Frontend-wide content-state height audit | Mọi empty/error/denied/success state thay thế page, workspace, pane hoặc data frame dùng typed `FillAvailable`; state inline trong dialog/form/chart/wizard vẫn compact. Shared data frame và Radzen empty row chịu trách nhiệm truyền chiều cao đến đáy | Sau khi sửa My Orders, audit cho thấy History, Catalog, Report, Permission, Approval, Settlement và system routes vẫn dựa vào CSS cục bộ hoặc min-height nội dung nên có thể lặp lỗi “chỉ chiếm đoạn trên” | Global M8 content-state geometry | Bổ sung modifier tại `VppContentState`, retrofit toàn bộ workspace-level consumers, khóa phân biệt full/inline bằng architecture test và browser representative routes | Shared primitive; system routes; Report; Permission; History; Catalog; Approval; Period management; Settlement; order detail | IMPLEMENTED — QA PASS; frontend `383/383`, browser Catalog + History `1/1` tại `1920×1080` |
| 2026-08-08 | Post-settlement order correction | **APPROVED và được làm rõ bởi record 2026-08-10.** Đơn thuộc kỳ đã chốt không bị ghi đè. Chỉ người có quyền quản lý kỳ được gửi yêu cầu `Điều chỉnh sau chốt` hoặc hủy đơn; nhân viên không có action. Khi một quản lý khác xác nhận, hệ thống tạo bản đơn mới và bản chốt mới, đồng thời gửi thông báo cho nhân viên | Sửa/xóa trực tiếp làm sai bản chốt, báo cáo và lịch sử. Hủy sau chốt phải lưu thay đổi để vẫn đối chiếu được dữ liệu trước đó | Requests/Ordering + settlement adjustment + notifications | Dùng permission `PERIOD_SETTLE`; yêu cầu có trạng thái chờ/xác nhận/từ chối. UI dùng câu chữ thân thiện, không hiển thị thuật ngữ kỹ thuật về revision/four-eyes | `VPPRequestService`, request history, `PeriodSettlementService`, notification, My Orders/History, settlement workspace, SQL/API/browser tests | AUTOMATED QA PASS — OWNER REVIEW |
| 2026-08-09 | Multi-period ordering — ListDetail completion and sparse-horizon correction | Mặc định duy trì chuỗi current + 2 future; period rời rạc là ngoại lệ bổ sung, không được tính thay tháng còn thiếu trong chuỗi. Màn admin lấy danh sách kỳ làm chính; settings và mở kỳ riêng chỉ hiện khi yêu cầu | Owner yêu cầu làm hết plan kỳ sau khi scroll/header đã ổn định; audit runtime phát hiện UI cũ vẫn là hai form lớn + bảng phẳng và generator đếm mọi Open period nên một tháng rời rạc xa có thể làm thủng chuỗi mặc định | Requests/Ordering period management + shared action token/a11y | Generator quét từ anchor và chỉ dừng khi chuỗi mặc định đủ; duplicate bị chặn. Operation/ListDetail tự chọn kỳ Open đầu, hiện edit/extend/close/delete theo capability; notification dùng dashboard deep link hiện hành. Primary action token đạt contrast và disclosure control dùng ARIA hợp lệ | `VppPeriodService`, period admin workspace, employee selector, correction service/controller, notification links, responsive/navigation/content-state regression | AUTOMATED_QA_PASS — OWNER_REVIEW; backend 508, frontend 384, LocalDB 22, browser 14 + employee 1, axe pass |
| 2026-08-09 | My Orders period selector + global typography correction | Kỳ đặt hàng chỉ xuất hiện một lần dưới dạng typed decision selector; bỏ cặp giá trị tĩnh + native select lặp nhau. My Orders và Order Create dùng chung `VppDecisionSelect<TValue>`. Root typography trở về chuẩn `1rem = 16px` để token 12/13/14px resolve đúng như tài liệu | Owner phát hiện dropdown rơi về browser-native và toàn app nhỏ hơn design scale. Nguyên nhân là `html: 14px` làm mọi token `rem` bị co thêm 12.5% | Global typography; M2 My Orders + Order Create | Giữ body/table mặc định 14px, label/caption 12–13px, heading theo semantic scale; không tăng rời từng route. Selector kỳ là `SELECTOR-DECISION`, selected value là nguồn kỳ duy nhất, popup selected dùng nền nhẹ + check | `vpp-tokens.css`, `VppDecisionSelect`, `Tab_Orders`, `Page_OrderCreate`, route catalog, architecture + isolated browser tests | IMPLEMENTED — QA PASS; 4 viewport responsive, VI/EN và open-popup evidence |
| 2026-08-09 | Full-height order content states | Empty, filtered-empty và error state trong order-items surface phải lấp đầy toàn bộ vùng dữ liệu còn lại đến footer/đáy panel; không dừng ở min-height nội dung của Radzen EmptyTemplate | Owner phát hiện state chỉ chiếm đoạn trên dù workspace đã khóa full viewport | M2 My Orders; shared order-items consumers như History/detail | Giữ column header làm context; kéo chuỗi `table → tbody → empty row → state` theo chiều cao data region. Content state ngoài grid dùng cùng fill contract | `VppOrderItemsSurface`, My Orders responsive browser geometry | IMPLEMENTED — QA PASS |
| 2026-08-05 | Chốt kỳ — góc nhìn Người dùng | Tách người đặt thành chế độ xem riêng `Phòng ban / Người dùng / Mặt hàng`; bảng Phòng ban và Mặt hàng không ghép thêm tên người đặt. Góc nhìn Người dùng tổng hợp theo tài khoản và vẫn giữ ngữ cảnh phòng ban, số đơn, số mặt hàng, số lượng, thành tiền và trạng thái | Danh sách nhiều tên trong một ô làm bảng Mặt hàng khó đọc và trộn hai chiều phân tích khác nhau | M4 settlement presentation, không đổi API/nghiệp vụ | Dùng snapshot đơn hiện có để group theo `CreatedByUserId`; giữ cùng filter loại đơn/trạng thái/phòng ban; bỏ resource/cột requester ghép cũ | `PeriodSettlementPanel`, settlement projection, unit/route-real tests | IMPLEMENTED — VERIFIED |
| 2026-08-05 | My Orders + Order Create supplement policy | Cho phép tạo đơn bổ sung khi chưa có đơn thường; base request chỉ là lineage tùy chọn. Quota approved, attempt và one-Pending tính chung theo user/kỳ | Owner chọn phương án A để bỏ bước tạo đơn thường giả; backend/schema hiện đã hỗ trợ base nullable nhưng policy/UI/index cũ còn buộc base | M2 business capability, không đổi visual motif | Bỏ prerequisite copy/guard; giữ wizard reason; thêm route-real mutation cho persona không có regular; đồng bộ ADR, settlement và migration | `Tab_Orders`, `Page_OrderCreate`, request service, settlement, thesis/defense docs | IMPLEMENTED — VERIFIED |
| 2026-08-05 | Chốt kỳ — hiển thị người đặt và tạm gỡ business-data translation | Bảng Phòng ban hiển thị danh sách người đặt trong từng phòng; bảng Mặt hàng hiển thị người đặt lấy từ breakdown đơn. Dữ liệu danh mục tạm dùng trực tiếp tên/diễn giải canonical, không còn API hay bảng translation; đổi ngôn ngữ UI bằng `.resx` vẫn giữ nguyên | Owner cần biết ai tạo nhu cầu ngay trong workspace chốt kỳ và muốn giảm nhiễu schema khi translation chưa được sử dụng | M4 settlement + backend library simplification | Reuse `HistoryRequester`; giữ grid hiện tại, không tạo motif mới; migration chỉ chạy khi 7 bảng translation trống và metadata ngôn ngữ vẫn là `vi` | `PeriodSettlementPanel`, settlement projection, catalog/pricing/library DTO/API/model, EF migration | IMPLEMENTED — VERIFIED |
| 2026-08-01 | Pricing + Reports canonical retrofit | Giá mặt hàng dùng bảng giá làm context, không lặp selector NCC trong toolbar; Danh sách bảng giá gom lifecycle hiếm vào menu; Báo cáo dùng Analytics workspace với static evidence footer thật | Ba màn đã đúng nghiệp vụ nhưng composition cũ khiến context/filter/action trộn nhau, Report chưa thể hiện rõ pattern và legacy CSS còn khả năng chồng | Pricing + Reports, dùng shared system contract | Tách context khỏi query filter; giới hạn direct actions; compose Report theo typed slots; xóa selector/CSS/action-column zero-consumer và khóa route-real 4 viewport | `Tab_PriceListLibrary`, `Tab_PriceLibrary`, `Report`, consumer ledger và architecture/Playwright gates | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | Canonical DataGrid sorting | Mọi grid có sort dùng single-column ba trạng thái `tăng → giảm → mặc định`, quay về trang đầu khi sort và không hiện chỉ số multi-sort. Header nghỉ giữ sạch nhưng reserve icon track; hover/focus mới hiện affordance trung tính, trạng thái active hiện mũi tên xanh mà không làm xê dịch label | Trước đó Catalog, quản trị và period grids trộn single/multi-sort; icon Radzen chưa sort nằm sát chữ nên trông như nội dung bị chồng, còn việc ẩn hoàn toàn làm mất discoverability | Global data-grid interaction | Chuẩn hóa parameter Radzen trên mọi sortable grid; đưa visual state vào `vpp-datagrid.css`; thêm architecture scan toàn repo và route-real Catalog gate cho hover/asc/desc/reset + stable geometry | Catalog, Orders, Period, Report, Library, Users và Permissions; detail/workflow grid không có nhu cầu vẫn giữ `AllowSorting=false` | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | Pager page-size focus + popup direction | Page-size trigger đóng/mở bằng chuột giữ chrome trung tính, không có viền xanh oval; keyboard focus vẫn có inset ring. Option hover/selected giống filter canonical và phải nằm trọn trong popup. Popup sát đáy được Radzen lật lên trên thì motion cũng chạy hướng lên, không dùng animation của sidebar | Global accessibility focus cộng với state open của bridge tạo hai lớp ring; Radzen portal còn để list rộng hơn panel nên nền hover/selected tràn sang phải | Global pager dropdown + transient motion | Tách pointer-open khỏi keyboard focus; chuẩn hóa `box-sizing`, width và overflow của panel/list/item; đánh dấu portal `above` theo geometry và khóa containment bằng Playwright | Mọi Radzen pager/dropdown portal; Catalog là consumer đại diện | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | Catalog shared-frame responsive correction | Catalog phải dùng đầy đủ data-surface contract chung: khung sở hữu border/radius, grid giữ min-width theo schema và dùng native horizontal scroll khi vùng nội dung hẹp hoặc browser zoom cao; không ép cột Mặt hàng thành vài ký tự | Lượt trước chỉ sửa đường nối toolbar/header nên test pass nhưng bỏ sót lỗi thị giác ở viewport hẹp: CSS route `overflow-x: hidden` làm Radzen co cột và trông như header/data bị đè | Shared data-surface behavior + Catalog schema | Đưa overflow/min-width behavior vào Radzen bridge opt-in; Catalog chỉ khai báo min-width tổng và width/min-width từng cột; thêm route-real gate ở `960×768` | Catalog trước; các data-grid opt-in khác kế thừa behavior nhưng giữ schema riêng | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | My Orders view selector labels | Ba lựa chọn chỉ hiển thị `Đơn kỳ hiện tại / Đơn bổ sung / Kỳ trước`, bỏ toàn bộ badge số | Số lượng đã có trong nội dung và footer liên quan; badge làm selector nặng và gây nhiễu khi mục đích chính chỉ là đổi view | My Orders route | Bỏ `Badge` khỏi `OrderViewOptions` và xóa các computed count chỉ phục vụ badge; giữ capability badge/ordinal của component dùng chung cho workflow thực sự cần đánh số | `dashboard?tab=0`; không lan sang step selector | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | Chốt kỳ — unified data workspace | Dùng khung Catalog, bỏ KPI/readiness card và workflow bốn bước; hai selector `Kỳ này / Tùy chọn` + `Phòng ban / Mặt hàng`, mặc định Phòng ban. Dải quyết định gồm hai select canonical theo thứ tự `Nhà cung cấp → Bảng giá`, tổng tiền và CTA chốt kỳ; không còn footer/xem trước. Header bảng luôn có badge `Chưa chốt kỳ / Đã chốt kỳ`; PDF/Excel chỉ xuất hiện sau khi API trả settlement hiện hành. Mặt hàng dùng snapshot demand + pager `100`, phòng ban dùng snapshot group | Owner muốn nhãn góc nhìn ngắn, bỏ tiền tố “Theo”, đặt Phòng ban trước Mặt hàng và phân biệt rõ dữ liệu live với snapshot đã chốt | Period settlement route | Tái dùng period-demand + preview/confirm/correct API; khóa export bằng `IsSettled + SettlementId`; khóa popup page-size không tràn option khỏi panel; route-real desktop/tablet/mobile và human screenshot review | `PeriodSettlementPanel`, `VppStatusBadge`, `VppFilterSelect`, Radzen dropdown bridge, settlement state | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | Catalog toolbar → grid seam correction | Không dùng route card bọc một `VppDataSurfaceFrame` embedded rồi ép grid vuông; một frame duy nhất phải sở hữu border/radius/clip, grid bên trong vuông và header bắt đầu đúng sau toolbar | Lượt sửa radius đầu làm runtime trông như header bị lớp toolbar/card đè lên vì surface ownership bị chia đôi | Catalog + shared data-surface rule | Bỏ `section.vpp-catalog-card`, bật `Bordered=true` cho frame canonical và khóa geometry `toolbar.bottom = thead.top`, không overlap | Catalog; dùng làm reference cho consumer data-surface mới | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | My Orders view selector | Chỉ giữ selector ngang và contextual action; bỏ dòng tóm tắt `Đơn đã gửi · n mặt hàng · tổng số lượng n` và bỏ card/wrapper bao ngoài selector | Nhãn selector và bảng đơn đã cung cấp đủ context; summary + outer card tạo thêm một tầng visual không có giá trị | My Orders route | Xóa summary markup/computed copy; switchbar trở thành layout container trong suốt, selector tự sở hữu surface | `dashboard?tab=0` | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | Canonical select + pager page-size dropdown | `VppFilterSelect` là visual contract cho select toàn dự án; page-size dùng cùng chiều cao, radius, hover/focus, popup và selected indicator. Vì page-size luôn có giá trị, trigger đóng giữ nền trung tính; xanh chỉ dùng khi mở/focus và cho option hiện tại trong popup | Owner phát hiện page-size select vừa lệch chrome Radzen, vừa bị nền xanh vĩnh viễn nếu coi mọi giá trị là active | Global select interaction | Ánh xạ pager trigger và Radzen dropdown portal vào cùng token/chrome; khóa neutral/hover bằng browser computed-style; giữ form-specific override có chủ đích | Mọi data pager và Radzen dropdown popup; Catalog/History/Department/Period/Admin là consumer đại diện | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | Shared data-surface grid seam | Khung data surface sở hữu bo góc ngoài; `RadzenDataGrid` nằm dưới toolbar phải vuông, không tự bo hai góc trên tại đường nối toolbar → header bảng | Owner phát hiện Catalog xuất hiện hai góc cong thừa ngay trên hàng tên cột, làm bề mặt ghép trông như hai card chồng nhau | Shared data-surface bridge | Scope `--rz-grid-border-radius: 0` cho `.vpp-data-grid` bên trong `VppDataSurfaceFrame`; thêm architecture + computed-style browser gate | Catalog và mọi grid đã opt-in vào shared data surface | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | Global art direction + execution mode | Retire `T001`; OpenAI/Codex minimal system thay Apple làm visual research chính. Dùng system colors/font, restrained type scale, token spacing/radius, icon outline, một CTA chính và motion có mục đích; không sao chép pixel ChatGPT | Owner muốn làm toàn bộ UI một lượt chuẩn và cho rằng OpenAI phù hợp hơn với cách GPT-5.6/Codex mở rộng codebase | Global | Hoàn tất F5–F7 + DS4/R1; khôi phục full QA; thêm OpenAI visual/motion contract và final runtime board | Toàn bộ M0–M8, shell, transient surfaces, animations, docs/toolchain | IMPLEMENTED — OWNER APPROVED 2026-08-04 |
| 2026-07-29 | DS2 Catalog + primary header flattening | Catalog bỏ block giới thiệu lặp, dùng đúng motif `filter → header có # → rows → footer/pager`; server paging luôn hiện trong viewport. Primary header không còn tab con/breadcrumb; `Quản lý` được thay trực tiếp bằng `Tổng hợp phòng ban` | Owner review phát hiện Catalog thiếu footer/cột số thứ tự dù API đã paging và nested header-tab tạo hierarchy thừa | Catalog route + global authenticated header | Sửa grid thành flex data/pager, thêm page-aware row number; xóa breadcrumb model/markup/CSS và dùng label primary trực tiếp | Catalog, dashboard/order-create, period, pricing và mọi header consumer | IMPLEMENTED — OWNER REVIEW |
| 2026-07-29 | DS3 History + Department Summary viewport correction | Cả hai route dùng cùng shell full-height; chi tiết đơn bắt đầu ngang mép trên KPI và cùng chạm đáy viewport với danh sách. Không dùng `text-box` trim cho nội dung phiếu/badge tiếng Việt vì có thể xén dấu hoặc đỉnh glyph | Owner review phát hiện Department Summary bị đứt chuỗi height, detail bắt đầu cao hơn KPI và phần trên chữ bị cắt | Shared History workspace + dashboard shell routing | Áp `vpp-history-shell` cho History và Department Summary; đổi detail sang grid row KPI → đáy; bỏ local glyph trim và khóa bằng route geometry | History, Department Summary, future HistoryWorkspaceShell consumers | IMPLEMENTED — OWNER REVIEW |
| 2026-07-28 | F4 owner review round 6 — collapsed user popup | Sidebar mở rộng giữ account popup trong sidebar; sidebar thu gọn phải mở popup sang bên phải rail và bám đáy, không dùng panel rộng phủ lên icon/navigation | Owner phát hiện popup user ở rail thu gọn không còn giống behavior trước và che cả sidebar lẫn main content | Global shell interaction | Tách anchor theo shell state; thêm browser geometry gate cho expanded motion và collapsed no-overlap/bottom anchor | Account menu ở mọi authenticated route, desktop collapsed shell | IMPLEMENTED — OWNER_REVIEW |
| 2026-07-28 | F4 owner review round 5 — Catalog, Users, Create Order, order detail | Xóa toàn bộ custom scrollbar và dùng native browser/OS; internal scroll vẫn giữ để không cuộn document. Catalog/Users/Create Order dùng shared page header, filter và transient motion như danh sách đơn; Users bỏ tải lại và thêm column picker; Account giữ nguyên. Search/header bảng chi tiết đơn phải cùng visual system với danh sách | Owner duyệt theo màn đại diện và phát hiện sự không nhất quán ở scroll, filter chrome, page header, column visibility và workflow cấp cháu | Global interaction + representative F4 consumers | Thêm shared filter composites; bounded Catalog/Users; nâng nested Create Order header/stepper; migration order-detail toolbar; khóa route-real no-document-scroll/loading-settled/row-number gate | F5–F6 route migration, mọi data-heavy filter toolbar và nested workflow mới | IMPLEMENTED — OWNER_REVIEW |
| 2026-07-28 | F4 symmetric content inset correction | Không chấp nhận kết luận “đều” chỉ từ `.vpp-content`; phải đo token đã resolve, computed shell padding và visible workspace ở sidebar mở/thu. Legacy `.rz-body !important` không được ghi đè VPP shell; flex panel phải fill đúng chiều rộng | Owner vẫn nhìn thấy lệch sau lượt đầu; browser chứng minh History hụt phải 30px và shell thực tế dùng padding legacy thay vì token mới | Global shell + F4 geometry gate | Scope rule legacy khỏi `.vpp-layout-body`; khóa History flex width; bỏ outer padding ẩn; nâng gate expanded/collapsed và ancestor diagnostics | Mọi authenticated route và F5–F6 consumer mới | IMPLEMENTED — OWNER_REVIEW |
| 2026-07-28 | F4 shell geometry + global interaction motion | Divider dọc thuộc shell grid để luôn thẳng khi sidebar mở/thu; active indicator dùng một object chạy theo hướng; header/sidebar dùng gap + cross-axis inset chung; transient surface dùng một enter-motion contract. `Main content` được gọi ngắn là `symmetric content inset` khi nói khoảng cách trái–phải | Owner yêu cầu shell thẳng, line/hover có cùng motif theo hướng, popup lọc mở như user menu và outer inset dễ gọi tên/kiểm tra giữa mọi màn hình | Global shell + design-system interaction | Thêm navigation/transient token; mở native header cho shared indicator runtime; route-real seam/inset/motion gates; không áp line động cho divider/table border | Mọi authenticated route, project-owned filter menu/popover/popup, F5–F6 migration | IMPLEMENTED — OWNER_REVIEW |
| 2026-07-28 | F4 workspace patterns + page outer inset | Chuẩn hóa sáu pattern `Account / Collection / ListDetail / SplitEditor / Operation / Analytics` bằng typed slot; mỗi cạnh page dùng một token riêng và phải nhất quán giữa route | Owner muốn UI scale/tùy biến tốt cho AI agent, các màn hình có khoảng cách với header/sidebar rõ ràng nhưng không tạo component vạn năng | Global UI architecture | Thêm folder `DesignSystem/Patterns`, README chọn pattern, architecture gate hai consumer và route-real geometry gate | M0–M8 migration ở F5–F6 | IMPLEMENTED — OWNER_REVIEW |
| 2026-07-28 | Shared header + compact Radzen tabs | Bỏ group/role badge khỏi header; group chỉ hiện ở sidebar identity. Tab không active giữ text primary khi hover/focus; bề mặt hover bo `--vpp-radius-md`, inset đều khỏi hai cạnh header, còn active giữ gạch xanh sát đáy | Owner phát hiện header lặp group, chữ tab biến mất trên nền sáng và hover hình chữ nhật cao toàn header không cùng ngôn ngữ bo góc của design system | Global shell | Gỡ `vpp-header-role-badge`; khóa màu semantic; dùng pseudo-surface bo/inset để không làm lệch active underline; thêm architecture + browser regression | Mọi authenticated route, primary header tabs desktop và compact Radzen tabs mobile/tablet | IMPLEMENTED — OWNER_REVIEW |
| 2026-07-28 | F3 shared order detail | My Orders và History phải giống nhau từ ô tìm kiếm đến footer, gồm popup lọc và code/note interaction; chỉ header và độ rộng khác. Header History chuyển sang bố cục phiếu PDF, giữ PDF/Excel/ghi chú và bỏ lịch sử phiên bản | Owner review phát hiện F3 ban đầu mới dùng chung grid/footer nên abstraction chưa bao phủ toàn behavior thật | Shared composite + local route header | Mở rộng `VppOrderItemsSurface` thành filter-to-footer contract; giữ filter state/API/capability ở route; đổi History sheet header theo `OrderPdfBuilder` | My Orders current/supplement/previous, History detail, future approval/management detail | IMPLEMENTED — OWNER_REVIEW |
| 2026-07-27 | W-H global hardening | Shared states có live-region/focus semantics; Radzen 11.1.4 DataGrid chỉ normalize accessibility trên vùng opt-in; chart thiếu chuỗi hợp lệ chuyển sang empty state thay vì render SVG `NaN`; print ẩn toàn bộ chrome/action/transient UI | Route-real axe phát hiện nested grid/rowgroup, chart `NaN` và focus notification chưa bền; sửa tại shared boundary giảm drift giữa route | Global M8 | Đóng W-H; thêm 28×4 runtime matrix, representative Dark/Print/axe và capture gate | State primitives, report/history chart, opted-in grids, notification center, print stylesheet | VERIFIED — 112 runtime combinations + axe/print/dark pass |
| 2026-07-27 | Report + order export | E2E phải tải file thật và kiểm tra extension/payload, không dừng ở việc nút xuất hiện | W-G và D10 trước đó mới có render gate; browser download mới chứng minh đủ FE → authenticated API → byte stream → file | Cross-route | Thêm `ExportDownloadTests`; report PDF/CSV/XLSX và order PDF/XLSX | Report, My Orders, History/order detail export consumers | VERIFIED 2026-08-01 — 5 downloads pass on isolated fixture; PDF/XLSX/CSV signature được đọc thật |
| 2026-07-30 | Permission UI mapping | E2E/page object định danh nhóm bằng `GroupCode=DEV`; mọi thay đổi UI mapping đi qua workspace batch editor và một selector access-state, không còn switch inline | Identity bảo mật phải tách khỏi display name; batch tránh quyền ở trạng thái cập nhật dở dang | Local M6 + test contract | Cập nhật page object, atomic API test và mutation opt-in có restore | Permission group ListDetail, batch editor, current-session navigation | ISOLATED_QA_PASS — hide/show Report permission updates current session and restores seed state |
| 2026-07-30 | Admin CRUD placement | Toolbar quản trị chỉ còn search/filter/clear/column picker; **Thêm** nằm ở collection header, còn Xem/Sửa/Xóa-Khôi phục và action vòng đời nằm trong cell Thao tác. Header cột Thao tác là text thuần | Owner review xác nhận nút Thêm trong header cột gây chật và sai cấp độ: create thuộc collection, các action còn lại thuộc từng row | Local M6 + Library | Thêm `VppCollectionHeader` + `VppDataSurfaceFrame.Header`; xóa `VppActionColumnHeader`; Lookup luôn hiện footer, scroll nội bộ và bỏ zebra rows | `/library`, `/permission` | ISOLATED_QA_PASS — admin matrix + Lookup geometry desktop/tablet |
| 2026-07-30 | Equal selector + period picker + order deep-link + fixed Class split | Mọi option trong selector ngang dùng cùng độ rộng theo label dài nhất. My Orders/History hiện full code và History action mở đúng order trong workspace. History/Chốt kỳ dùng một period-picker nổi canonical. Lookup Class dùng split cố định, Code/Tên riêng cột; bỏ legacy code popover đã hết consumer | Owner phát hiện selector lệch nhịp theo độ dài text, date editor cũ rời rạc, mã bị rút gọn và splitter Class tạo layout không ổn định | Shared composites + M2/M4/M6 | Nâng `VppSegmentedSelector`; thêm `VppPeriodPickerPopover`; deep-link `orderId`; fixed `VppSplitEditorWorkspace`; xóa JS/CSS popover mã đơn zero-consumer; route-real human visual gate | My Orders, History, Department Summary, Chốt kỳ, Lookup Class | IMPLEMENTED — OWNER REVIEW |
| 2026-07-31 | Cancelled order recovery | Đơn thường và đơn bổ sung đã hủy có hai command riêng: `Hoàn tác hủy` clone nguyên snapshot và `Tạo lại đơn` mở wizard rỗng; cả hai tạo revision mới trong cùng `RequestSeriesId`, giữ bản hủy bất biến. Đơn bổ sung giữ nguyên sequence/attempt và quay về `Pending`, không tiêu thụ thêm quota | Owner làm rõ “tạo đơn mới” sau hủy phải vừa dễ hiểu ở UI vừa không phá unique current-regular invariant, supplement attempt policy hoặc lịch sử audit | M2 lifecycle + shared API contract | Retire `CanReplace/REPLACE`; thêm typed `CanRestore/CanRecreate`, API `restore/recreate`, audit `RESTORE/RECREATE`, VI/EN và mutation-isolated lifecycle QA. Release build `0 warning`; backend `458/458`; frontend `205/205`; regular restore/recreate route-real pass; mobile 390px kiểm bằng mắt không còn cắt action | My Orders, Order Create, revision history | IMPLEMENTED — VERIFIED |
| 2026-07-31 | My Orders action row + toast close | Dòng action của đơn hiện tại/bổ sung dùng cùng thứ tự `Lịch sử → PDF → Excel → Sửa/Hủy/Hoàn tác/Tạo lại → primary CTA`; nút đóng toast là icon phẳng, không border/radius hoặc line focus riêng, hover/focus chỉ đổi màu và nền nhẹ | Owner phát hiện action cùng cấp bị tách rời và focus chrome cũ tạo oval/line nặng quanh nút đóng | Shared order workspace + global FEEDBACK | Sắp xếp lại `VppOrderWorkspacePanel`, để primary action cuối; xóa toàn bộ chrome bao quanh dấu X trong `vpp-toast.css`, giữ nền focus không làm xê dịch. Architecture gate + computed-style visual QA | My Orders current/supplement/previous, History/export consumers, mọi Radzen toast | IMPLEMENTED — FOCUSED QA PASS |
| 2026-07-31 | Account email onboarding completion | Register mô tả đúng luồng `đăng ký → xác nhận email → admin duyệt`; thêm route/API gửi lại xác nhận chống dò tài khoản, audit read-only và Mailpit sandbox opt-in trong Aspire. Email tài khoản hiện có bao phủ xác nhận, reset password, lời mời và kích hoạt; production không tự bật SMTP | Owner yêu cầu hoàn thiện đăng ký/mail nhưng app thường không được phụ thuộc Docker hoặc secret bên ngoài | Account lifecycle + ACCOUNT/FEEDBACK | Thêm typed resend contract, rate limit `account-confirm`, VI/EN account page, route catalog, outbox/audit tests; AppHost pin Mailpit `v1.30.0`, chỉ bật qua `EmailSandbox__Enabled=true` | Register, ConfirmEmail, ResendConfirmation, Forgot/Reset Password, admin invite/activate | IMPLEMENTED — BUILD/UNIT/UI SMOKE PASS; MAILPIT RUNTIME OPT-IN |
| 2026-07-30 | Security audit administration | Thêm route `permission.security-audit` read-only, truy cập từ header/sidebar; query/filter/sort/paging chạy server-side và chi tiết mở adaptive dialog. Không có Thêm/Sửa/Xóa, không trả secret/token | Owner duyệt Nhật ký bảo mật read-only; audit phải đủ dùng điều tra nhưng không biến thành CRUD log | Local M6 + final hardening | Typed DTO/API, permission route, VI/EN, desktop/mobile/human visual, 28×4 + dark/print/axe | Permission navigation, SecurityAudit data surface, audit detail | VERIFIED — solution 0 warning; FE 204, BE 445, LocalDB 20; route-real audit 3/3 |
| 2026-07-27 | W-G `/report` | Báo cáo có hai export thật CSV/XLSX, toolbar filter/action thống nhất, trend theo `TotalAmount`, bảng phòng ban chỉ dùng `Code/OrderCount/TotalQuantity/TotalAmount`, và settlement evidence qua resx VI/EN. Không dựng series/cột/KPI không có trong DTO | Backend đã có CSV UTF-8 và workbook 5 sheet; DTO không trả breakdown thường/bổ sung, tên phòng ban, số mặt hàng theo phòng hay so kỳ trước. Dữ liệu kỳ đã chốt phải đọc snapshot, không tính lại để làm đẹp UI | Local M7 | W-G source hoàn tất; isolated render/overflow 3 viewport pass. Ghi retrofit Atlas cho toàn bộ field giả | M7 Reports; Atlas reports | VERIFIED — frontend 180/180, report CSV/XLSX download E2E pass |
| 2026-07-27 | W-F.2 `permission.component` | Ma trận action × 3 vai trò render trực tiếp từ `CanonicalRbac` và chỉ đọc; UI mapping động hiện có nằm ở lớp thứ hai. Grid nhóm dùng DTO permission có `GroupCode`, không suy diễn security identity từ tên hiển thị. Không có nút lưu matrix | Action permissions là canonical backend authority; chỉ page/component visibility có endpoint mutation. Tên hiển thị có thể dịch, `GroupCode` mới là identity ổn định | Local M6 | W-F source hoàn tất; isolated matrix/render/overflow pass. Ghi retrofit Atlas bỏ `Lưu ma trận quyền` | M6 Permissions; Atlas permissions card | VERIFIED — frontend 180/180, mutation hide/show permission pass |
| 2026-07-27 | W-F.1 `permission.user` | Đồng bộ màn Người dùng theo Atlas ở lớp copy/filter nhưng giữ vòng đời tài khoản thật của backend: search `UserName/FullName/Email`, filter `accountStatus`, ba trạng thái canonical, activation/reset/deactivate có audit. `SessionVersion` được coi là security-stamp equivalent nên bị loại khỏi inspector; `RowVersion` vẫn hiển thị ở tab Kỹ thuật | Backend đã có contract lọc và action thật; Atlas không được mở admin-create/edit khi API không tồn tại, và secret/session invalidation evidence không được lộ qua reflection inspector | Local + shared inspector safety | W-F.1 hoàn tất source; isolated route render/overflow pass. Thêm resx VI/EN và architecture regression | M6 Users; mọi DTO tương lai đi qua `Component_RecordInspector` | ISOLATED_QA_PASS — frontend 171/171, UI 1/1; mutation/owner review pending |
| 2026-07-26 | ATLAS-001 — kế hoạch triển khai toàn bộ Atlas | Mở kế hoạch `docs/execution/ATLAS-001.md`: đối chiếu đủ 28 màn Atlas với route Blazor, phân loại gap A/B/C/D và chia 8 wave W-A…W-H. Thứ tự thẩm quyền chốt là luận văn → Atlas → backend/API → frontend hiện tại. Atlas mới hơn frontend nên Blazor phải kéo lên theo Atlas, nhưng Atlas không được vẽ trường/hành động không có thật trong DTO | Owner yêu cầu một kế hoạch đầy đủ trước khi thực thi, và cần Atlas với frontend đồng bộ mà không phải khớp từng pixel | Global | Thêm execution doc; route ledger cập nhật sau mỗi wave | Toàn bộ 28 màn | PLANNED |
| 2026-07-26 | Canonical RBAC | **D1** — giữ ba vai trò `EMPLOYEE / MANAGER / DEV`. Quyết định bốn vai trò ngày 2026-07-24 chuyển sang Superseded | Luận văn §3.3.4.4 và `src/Shared/Constants/CanonicalRbac.cs` đều là ba vai trò; bản bốn vai trò chưa từng được code | Global | Không đụng backend authorization | Permission page, Atlas, sidebar/header role badge | OWNER_CONFIRMED |
| 2026-07-26 | `Tab_AllOrdersSummary` → Period Management | **SUPERSEDED 2026-07-30** — nội dung cần thiết đã được hấp thụ vào workspace Chốt kỳ theo mặt hàng/phòng ban; component/tab độc lập và test cũ được retire để không giữ hai UI authority. URL `managementTab=all` chỉ còn redirect tương thích | Owner yêu cầu xóa hẳn code UI cũ sau khi motif mới đã đủ consumer, tránh cascade và test contract chồng chéo | Global | Motif consolidation | Period management | IMPLEMENTED — VERIFYING |
| 2026-07-26 | `/report` export | **SUPERSEDED 2026-08-01** — Report giữ CSV/XLSX và bổ sung PDF tóm tắt; PDF ưu tiên bản đọc/in, Excel giữ dữ liệu chi tiết nhiều sheet | Owner yêu cầu hoàn thiện đồng bộ PDF/Excel toàn hệ thống; pipeline QuestPDF và export action typed hiện đã canonical, nên không còn lý do kỹ thuật duy trì ngoại lệ Report không có PDF | Global | FILE-EXPORT completion | Report page, API, browser download | IMPLEMENTED — PDF/XLSX/CSV tải thật pass isolated fixture |
| 2026-07-26 | Luồng vận hành kỳ | **D4** — tách thành bốn bước rõ `Rà soát kỳ → Gom nhu cầu → Chọn nguồn cung → Chốt kỳ`. `supply-allocation` tách khỏi `PeriodSettlementPanel` | Atlas đã vẽ bốn bước; `supply-allocation` đã là Hình 3-36 trong luận văn nhưng chưa có màn thật | Global | W-D — phần code mới nhiều nhất của kế hoạch | Period operations M4 | OWNER_CONFIRMED |
| 2026-07-26 | Design Atlas source | **D5** — đưa Atlas vào repository tại `docs/design/atlas/` dưới dạng design reference đóng băng, read-only. Không commit `output/` | Bản gốc nằm trong cache Codex và có thể bị dọn bất cứ lúc nào, kéo theo mất nguồn của 16 hình luận văn | Global | W-A | `AGENTS.md`, `.gitignore`, `LVTN/tooling/capture_atlas_thesis_screens.cjs` | Implemented |
| 2026-07-26 | Quy ước đọc-hiểu code | **D6** — code/tên biến/tên hàm tiếng Anh 100%; thêm `docs/CODE-READING-GUIDE.md` tiếng Việt và comment tiếng Việt ngắn tại điểm luật nghiệp vụ khó đoán, kèm số mục luận văn | Owner vibe-coding nhưng phải đọc hiểu và trình bày code khi bảo vệ | Global | Cập nhật cùng lần với mỗi wave | Toàn bộ source mới | IN_IMPLEMENTATION |
| 2026-07-26 | `GET /api/VPPRequest/period-demand` | **D7** — bổ sung một endpoint chỉ đọc trả `AggregatedVppResDTO`, gom theo mặt hàng từ phiên bản đơn hợp lệ hiện hành của kỳ, policy `Permissions.PeriodSettle` | DTO `AggregatedVppResDTO` đã tồn tại nhưng mồ côi, không service nào trả về. Luận văn §3.3.3.4 đã mô tả bước gom nhu cầu nên đây là hiện thực hóa, không phải nghiệp vụ mới | Global | W-D | `VPPRequestController`, period operations | OWNER_CONFIRMED — chờ W-D |
| 2026-07-26 | Xuất theo đơn (My Orders / order detail) | **D10** — owner duyệt làm thật tính năng xuất PDF + Excel theo đơn, thay cho phương án gỡ nút của W-A.7. Backend thêm endpoint export cấp đơn; XLSX theo pattern `ReportWorkbookBuilder` (ZipArchive + SpreadsheetML, không thêm dependency); PDF dùng QuestPDF + Poppins embedded để giữ tiếng Việt trên Linux. Atlas khôi phục nút export | Owner trả lời "Xuất PDF + Xuất Excel làm luôn đi nha" sau khi được báo nút không có backend | Global | Task W-C.0 trong ATLAS-001 | `VPPRequestController`, My Orders, order detail sheet, Atlas M2 | VERIFIED — PDF/XLSX download E2E pass |
| 2026-07-26 | Luận văn | **D9** — ATLAS-001 không sửa bất kỳ file `.docx` nào. Từ 27/07/2026, nguồn chuẩn là `LVTN/NguyenAnNam_DH52201078.docx`; không còn bản `working` hoặc checkpoint Word trong Git | Owner tạm hoãn sửa nội dung luận văn trong wave Atlas | Global | W-H bỏ mục sửa câu chữ; chỉ xuất ảnh runtime ra thư mục | Sai lệch §3.3.5.1 ghi ở mục 11 của ATLAS-001 | OWNER_CONFIRMED |
| 2026-07-24 | Four-role permission model | Giữ bốn persona: `EMPLOYEE / Nhân viên`, `DEPARTMENT_APPROVER / Quản lý phòng ban`, `PROCUREMENT_ADMIN / Chuyên viên quản lý văn phòng phẩm`, `SYSTEM_ADMIN / Quản trị hệ thống`. Không gộp hai vai trò quản lý nghiệp vụ: phòng ban chỉ xem toàn bộ đơn và báo cáo trong phạm vi phòng; chuyên viên văn phòng phẩm xem toàn công ty, phê duyệt/từ chối đơn bổ sung, tổng hợp yêu cầu, quản lý catalog/đơn vị/nhà cung cấp/bảng giá, chọn nguồn cung và chốt kỳ. Quản trị hệ thống có full action/UI chỉ trong Development/TEST/isolated QA; `DEV` là tên gọi nội bộ, không phải role thứ năm hoặc copy hiển thị | Owner xem lại separation of duties và chốt thuật ngữ thân thiện theo nghiệp vụ thực tế | Canonical RBAC, seed/environment guard, permission UI, Atlas, thesis use cases and all role annotations | Giữ một active membership/user; backend authorization kiểm tra từng action, không username bypass. Reconcile change-set ba-role đang làm dở trước khi tiếp tục code để không mất membership hoặc mở quyền ngoài ý muốn | Backend auth/seed/tests, Permission page, sidebar/header role badge, Atlas 28 screens, thesis | **Superseded 2026-07-26 bởi D1 trong `docs/execution/ATLAS-001.md`** — chưa từng được code; luận văn §3.3.4.4 và `CanonicalRbac.cs` đều dùng ba vai trò `EMPLOYEE / MANAGER / DEV` |
| 2026-07-24 | M2 employee vertical slice implementation | Bắt đầu triển khai bốn màn Blazor/Radzen đã có mockup Atlas: `Đơn hàng của tôi`, `Tạo/sửa đơn`, `Lịch sử đơn`, `Danh mục mặt hàng`. Copy tiếng Việt phải theo ngữ cảnh của nhân viên thường, không hiển thị persona/debug text. My Orders và chi tiết History dùng chung order-detail motif; danh mục chỉ để tra cứu nên không có giá/nhà cung cấp hoặc action thêm vào đơn; tạo đơn là focused workflow hai bước, có ghi chú đơn và ghi chú từng mặt hàng, không hiển thị đơn giá cho nhân viên. Chức năng roadmap chưa có backend vẫn là control bấm được nhưng chỉ phát notification trung thực bằng hệ thống toast hiện có | Owner cho phép code trước dù Atlas còn có thể tiếp tục chỉnh và muốn một vertical slice thật để duyệt trên route Blazor | M2 employee pages + shared order/table/filter/notification primitives | Giữ backend capability `CanEdit/CanCancel` làm authority; data lookup dài dùng server filter + scroll/virtualization, danh sách lịch sử/danh mục xem dữ liệu dùng server paging; mọi placeholder action phải thông báo “chưa khả dụng”, không giả lập kết quả | My Orders, Order Create/Edit, History, Product Catalog; later reports/export consumers | IN_IMPLEMENTATION |
| 2026-07-24 | Shared Admin Entity Workspace | Các màn CRUD quản trị không nhét toàn bộ cột database lên bảng cùng lúc. M5 và Người dùng dùng chung workspace: bảng chính có mã/tên/thao tác sticky khi kéo ngang, preset cột tương tác thật `Mặc định · Nghiệp vụ · Audit · Tất cả`, panel phải chuyển được giữa `Thông tin chung · Quan hệ · Bản dịch · Audit · Kỹ thuật`, chọn hàng cập nhật inspector, thêm/sửa dùng full-height drawer theo field-set riêng từng entity và vô hiệu hóa phải khôi phục được. Drawer nhóm trường thành `Thông tin chung · Quan hệ nghiệp vụ · Kỹ thuật chỉ đọc`, có vùng nội dung cuộn và action bám đáy. ID, FK, RowVersion và audit được xem ở tab kỹ thuật; secret như PasswordHash/token/security stamp không bao giờ render hoặc chỉnh trực tiếp | Owner duyệt phương án xem đủ dữ liệu admin theo hai lớp, sau đó từ chối bản chỉ có khung tĩnh và yêu cầu hoàn thiện interaction thật trên toàn bộ tab quản trị/phân quyền | Shared admin collection/master-detail primitives, M5 library screens, M6 users | Refactor future Blazor `Component_ShareGrid` and `Component_RecordInspector` into configured primitives; keep permissions as a guarded matrix rather than generic CRUD. Atlas regression phải chạy đủ 8 entity: đổi preset, đổi inspector tab, mở create/edit drawer, chọn hàng, soft-delete/restore và secret-exposure guard | Classes, categories, items, suppliers, price lists, prices, departments, users; permissions remains specialized | Implemented interactively in Atlas — OWNER_REVIEW |
| 2026-07-24 | Period procurement consolidation workflow | Atlas gộp `Tổng hợp toàn công ty` vào workspace `Vận hành kỳ` và thay hai màn hàng chờ trùng nghiệp vụ bằng một màn `Duyệt đơn bổ sung` duy nhất. Luồng vận hành kỳ gồm bốn bước liên kết: `Rà soát kỳ → Gom nhu cầu → Chọn nguồn cung → Chốt snapshot`. `PROCUREMENT_ADMIN` phê duyệt/từ chối đơn bổ sung trên phạm vi toàn công ty, theo dõi blocker, gom revision hợp lệ, chọn nhà cung cấp trước rồi chỉ xem bảng giá đang hiệu lực của nhà cung cấp đó; `DEPARTMENT_APPROVER` chỉ xem toàn bộ đơn và báo cáo trong phạm vi phòng ban. Nếu nguồn chính thiếu mặt hàng, được chọn nhà cung cấp/bảng giá ngoại lệ theo từng mặt hàng nhưng bắt buộc nhập lý do và lưu audit. Chỉ tạo snapshot bất biến sau khi hết hạn nhận đơn và mọi blocker đã được xử lý | Owner chốt Chuyên viên quản lý văn phòng phẩm là người quyết định đơn bổ sung và yêu cầu hoàn thiện Atlas/luận văn trước khi chuyển sang Blazor | M3 management, M4 period operations, canonical RBAC and settlement workflow | Backend action guard phải kiểm tra `PROCUREMENT_ADMIN`; UI manager không hiển thị action duyệt/từ chối. Dùng supplier-first selector, coverage preview, item-level exception form và immutable settlement preview. Giữ đúng 28 screen bằng cách thay màn trùng bằng hai bước nghiệp vụ còn thiếu thay vì tăng số màn | Period review, period demand, supply allocation, settlement, supplement approval, reports | OWNER-CONFIRMED — CODE_PAUSED |
| 2026-07-24 | Non-production System Admin RBAC | Màn Phân quyền hiển thị đúng bốn persona mục tiêu. `SYSTEM_ADMIN / Quản trị hệ thống` là vai trò thứ tư và được cấp union toàn bộ action/UI chỉ trong Development/TEST/isolated QA; không tạo persona thứ năm `DEVELOPER_OWNER`. `DEV` chỉ là cách owner gọi tài khoản kỹ thuật, không xuất hiện trong copy nghiệp vụ. Không mở role builder tùy chỉnh trong phase hiện tại | Owner muốn full access để debug nhưng chốt sơ đồ/use case chỉ có bốn nhóm quyền rõ ràng | M6 Users + Permissions Atlas, canonical RBAC seed and environment guard | Seed/assignment full access phải fail-closed ngoài non-production; authorization access vẫn tách khỏi domain-rule bypass | Users, permission matrix, Atlas reviewer persona, backend authorization design, thesis | OWNER_CONFIRMED — CODE_PAUSED |
| 2026-07-24 | Cross-screen navigation contract | Các màn hình liên quan phải nối thành luồng nghiệp vụ rõ, nhưng không thêm một thanh `Trang liên quan` lặp lại trên mọi trang. Header-tab và sidebar là điều hướng cấu trúc; CTA trong ngữ cảnh là điều hướng tác vụ, ví dụ Đơn hàng → Tạo đơn/Lịch sử, Rà soát kỳ → Hàng chờ/Chốt kỳ, Bảng giá → Giá mặt hàng, Người dùng → Nhóm & quyền. Mọi target phải là screen ID hợp lệ và phải kiểm tra quyền/trạng thái lại ở màn đích | Owner nhận thấy 28 màn hình rời rạc dù motif đã đồng bộ và cần tiền đề rõ để code route sau này không tự phát | Atlas navigation model + future Blazor route contract | Keep a single target registry, reuse semantic link/button primitives, preserve filters or selected record when useful, and never use navigation to bypass backend authorization or business guards | All 28 Atlas screens; priority flows M1, M2, M4, M5, M6, M7 | Implemented in Atlas — OWNER_REVIEW |
| 2026-07-24 | Scalable nested header path | Route cấp cháu trở xuống không tạo thêm header row và không chỉ hiển thị tên cha. Tab cha đang active biến thành path trong cùng header 72px, ví dụ `Bảng giá › Danh sách bảng giá`; ancestor dùng màu phụ, leaf dùng chữ chính và underline đúng bề rộng chữ. Tối đa ba node được hiển thị; sâu hơn thu phần giữa thành `…` | Owner cần header thể hiện đúng vị trí hiện tại và còn mở rộng được tới cháu–cháu mà không làm shell cao/dày hoặc lặp navigation | Shared shell header + nested dashboard/library routes | Introduce three header modes: primary tabs, nested path in active tab, and focused workflow. Keep sidebar as the full hierarchy authority; header path is compact context and backtracking aid | Management, period operations, pricing branches now; all future nested routes | Implemented — OWNER_REVIEW |
| 2026-07-24 | Focused order workflow header and symmetric removal | Trang tạo/sửa đơn là focused workflow: primary header 72px thay toàn bộ dashboard tab bằng `Trở về đơn hàng → Chọn mặt hàng → Kiểm tra và gửi`; content không lặp stepper. Mặt hàng đã chọn có action `X` ngay cả trong danh mục nguồn, đồng bộ với action xóa ở panel đơn đang tạo | Owner thấy global tab + stepper content tạo hai tầng điều hướng cho cùng một tác vụ; badge `Đã chọn` không cho phép hoàn tác trực tiếp từ danh sách đang thao tác | M2.09 Order Create/Edit + shared shell variant | Add a workflow-header slot to the shell; preserve sidebar and role badge; render selected catalog rows with an accessible remove action while keeping draft-panel removal | Atlas Order Create first; later reuse for regular, supplement and edit flows in Blazor | Implemented — OWNER_REVIEW |
| 2026-07-24 | Native scrollbar restoration | Toàn bộ Atlas trả scrollbar về giao diện native của trình duyệt/hệ điều hành; chỉ giữ hợp đồng `overflow`, `scrollbar-gutter`, sticky header và virtualization | Custom track trong suốt làm lộ dải trắng tại gutter bên phải sticky header; vá riêng theo browser/theme/grid sẽ tăng conflict và khó chuyển sang Radzen | Atlas global scroll chrome | Remove authored scrollbar pseudo-elements/tokens and custom chrome assertions; add a source guard that rejects future global scrollbar styling, then regression-check every paged/virtualized region | 28 Atlas screens now; future Blazor implementation keeps native scrollbar unless a separately approved cross-browser design exists | Implemented — OWNER_REVIEW |
| 2026-07-24 | Action-first approval queue | Màn duyệt bỏ toàn bộ KPI card; hàng chờ bên trái dùng scroll + virtualization, không paging. Cột/nhãn `Lý do` đổi thành `Ghi chú`, còn quyết định từ chối vẫn dùng `RejectReason` riêng | Owner xác định đây là trang thao tác liên tục, không phải dashboard để xem số liệu; `Lý do` đang mô tả chính ghi chú người đặt | Supplement/Pending Approval + source field mapping | Keep filter → virtualized queue → printable detail → decision actions. DB entity `VppRequest` has inherited `Description` and specialized `SupplementReason`; create/update supplement stores the note in `SupplementReason` and mirrors it to `Description`, so UI reads `SupplementReason ?? Description`. Never substitute `RejectReason` | M3.14 Supplement Approval, M4.16 Pending Approvals and future Radzen implementation | Implemented — OWNER_REVIEW |
| 2026-07-24 | Management list readability and period-filter parity | Tổng hợp phòng ban bỏ cột `Phòng ban`; Tổng hợp toàn công ty giữ `Phòng ban` nhưng bỏ `Ghi chú`. Cả hai dùng typography bảng giống Lịch sử đơn và dùng chung bộ chọn `Tất cả kỳ · Kỳ này · 3 tháng · 6 tháng · 12 tháng · Tùy chọn`; mặc định management chọn `Kỳ này` | Owner thấy font management vẫn nhỏ hơn Lịch sử đơn và period scope là một biến thể không cần thiết; dữ liệu scope/ghi chú đã có ở nơi khác | Department Summary + All Orders + shared history-scope primitive | Reduce each management list to eight contextual columns; restore 11px header/12px cell typography; compose all three pages from one period-scope builder while preserving department-only filter on company scope | M3 Department Summary, All Orders and future history-like pages | Implemented — OWNER_REVIEW |
| 2026-07-24 | Unified data-footer copy and geometry | Footer paging và scroll/virtualization cùng cao 40px, cùng font/căn lề. Paging dùng mẫu `Hiển thị {from}–{to} trên {total} {đối tượng}` và có page controls; virtualization dùng `Tổng cộng {total} {đối tượng}` và để trống phía phải, không dùng câu hướng dẫn `Cuộn để xem thêm` | Owner thấy `286 mặt hàng khả dụng · Cuộn để xem thêm` không đồng bộ với pager và mô tả hành vi thay vì dữ liệu | Shared paging + virtualized data-footer primitive | Add shared footer builders, contextual Vietnamese nouns, optional secondary scope after `·`, one-page pager state and renderer assertions for copy/height | All Atlas paging and virtualized grids; later map to shared Radzen grid footer after approval | Implemented — OWNER_REVIEW |
| 2026-07-24 | Apple Music-style global scrollbar | Thử nghiệm scrollbar mảnh dùng track trong suốt đã bị rút lại; không đưa motif này sang Blazor/Radzen | Gutter của scrollbar nằm trong vùng sticky header và lộ nền trắng, làm đứt màu header; chi phí vá đa trình duyệt lớn hơn giá trị thẩm mỹ | Design Atlas global chrome + future Blazor shell/data regions | Restore native browser/OS scrollbar; preserve only scrolling behavior and layout contracts | 28 Atlas screens; no Blazor retrofit | Superseded — native browser scrollbar restored |
| 2026-07-23 | History virtualized detail fixed header | The visible detail header is a fixed sibling above the isolated Radzen virtualized body; the native `thead` remains visually hidden for table semantics, so translated rows can never enter the header paint layer | Owner screenshot showed a virtualized item row painting over the detail header after internal scrolling; increasing table `z-index` did not change Chromium table paint order | History detail + virtualized-grid contract | Reuse the existing viewport observer to synchronize the scrollbar gutter, keep one shared column-track definition, and regression-test a real 500-line fixture after non-zero scroll using hit-testing plus screenshot evidence | History detail first; prefer a separate visible header for any Radzen virtualized table that reproduces the same paint-order defect | Implemented — OWNER_REVIEW |
| 2026-07-23 | RBAC + full product motif and mockup gate | `SYSTEM_ADMIN / Quản trị hệ thống` có full access chỉ trong local Development/TEST/isolated QA; production freeze hoàn toàn. Mọi mockup gồm shell hoàn chỉnh, dùng contract Apple-like chung và được sinh từ Design Atlas để global feedback cập nhật đồng loạt 28 screen | Owner muốn full access để debug, yêu cầu nghiên cứu ba task trước/current task, duyệt 28 screen trước khi code và tránh phải mở từng route để sửa cùng một motif | Global product/security/design workflow | Lock non-production RBAC boundary; add owner-derived shell/page contract, 28-screen Atlas workflow, agent-ready refactor sequence and explicit approval gate | Authorization seed/snapshot/admin UI; every authenticated route and global state | Approved scope — DESIGN_ATLAS_NEXT |
| 2026-07-23 | Design Atlas viewport and copy authority | Owner review ở zoom 100% phải khớp viewport thật; `1920×1080` chỉ là capture contract. Copy VI phải tự nhiên theo nghiệp vụ, M0 thể hiện đủ cha–con–cháu và M1 Login/Logout bám UI hiện hành | Atlas fixed canvas tạo cả body scrollbar ngang/dọc và buộc owner zoom 75%, có nguy cơ làm sai density/spacing khi code | Design Atlas + future shared shell primitives | Tách `render` fluid khỏi `capture` fixed; QA 28 screen trên ba desktop viewport; dùng internal scroll region; contextualize VI copy and lock M1 account source authority | 28 mockups now; carry viewport contract into Blazor implementation | Implemented — OWNER_REVIEW |
| 2026-07-24 | Order selector and item identity refinement | Ba thẻ loại đơn dùng viền 1px đồng đều; trạng thái chọn chỉ đổi border/background nhẹ, không có line đáy dày. Bảng đơn giữ tên+mã chung một cột và dùng nhãn `Đơn vị` đầy đủ | Owner thấy line đáy của thẻ hiện tại quá nặng và muốn quyết định rõ cách trình bày tên/mã cùng thuật ngữ đơn vị | My Orders mockup + shared data-table copy | Remove inset active rail from segment card; keep product name primary and code secondary in order contexts; reserve separate code column for library administration | M2 orders first; apply `Đơn vị` across all future screens/exports | Implemented — OWNER_REVIEW |
| 2026-07-24 | M2.08 order-grid footer removal | Bảng chi tiết đơn không lặp tổng số mặt hàng/sản phẩm hoặc nhãn loại đơn ở footer; card selector phía trên là nguồn tóm tắt và ngữ cảnh duy nhất | Owner chỉ ra `5 mặt hàng · 28 sản phẩm` và `Đơn hiện tại` lặp lại dữ liệu đã có, làm đáy bảng nặng và thừa | My Orders populated table | Remove the non-interactive summary footer while preserving the bounded table region and internal scrolling contract | M2.08 My Orders; reuse on current/supplement/previous panels | Implemented — OWNER_REVIEW |
| 2026-07-24 | M2.09 source-aligned order-create flow | Không lặp title/helper trong content; thanh trên chỉ hiển thị đúng hai bước `Chọn mặt hàng → Kiểm tra và gửi`, còn điều hướng/lưu nháp nằm trong footer thao tác. Sau khi bỏ nút quay lại khỏi header, cụm hai bước phải căn giữa vùng header khả dụng thay vì co theo nội dung và bám mép trái | Owner hỏi số bước thật, yêu cầu bỏ `Trở về đơn hàng` khỏi thanh bước và sau đó phát hiện cụm bước bị sát lề trái | Order Create mockup + source authority | Source `Page_OrderCreate` defines `StepCount => 2` và lưu nháp cục bộ bằng `localStorage`, có autosave + explicit save. Header workflow giãn hết vùng còn lại trước role badge và căn giữa cụm bước; renderer đo cả trục dọc lẫn tâm ngang. Footer bước chọn mặt hàng dùng `Quay lại → Lưu nháp → Ghi chú → Tiếp tục`; quay lại giữ nháp, không giả lập hành động hủy/xóa nháp chưa có confirmation flow | M2.09 create/edit/additional order flow | Implemented — OWNER_REVIEW |
| 2026-07-24 | M2.10 History source-alignment correction | Desktop History dùng grid gốc: cột trái `scope → KPI → chart → list`, detail drawer cố định bên phải và kéo dài toàn workspace; không để KPI span toàn trang rồi bắt đầu detail quá thấp | Owner phát hiện Atlas khác bản chạy và phần danh sách/detail chạm đáy viewport | History mockup + current Blazor source | Replace agent-invented generic split/inspector with the current `Tab_History` information architecture and bounded internal regions; preserve only approved shared motif polish | M2.10 History populated desktop | Implemented — OWNER_REVIEW |
| 2026-07-24 | Header-tab owns page identity | Bỏ page title/helper text khi header-tab hoặc sidebar grandchild đã nói rõ vị trí; bỏ refresh button thường trực, giữ retry chỉ trong error state | Owner phát hiện `Hàng chờ phê duyệt` và mô tả kỳ lặp lại navigation, đồng thời nút làm mới không cần thiết | Global authenticated page composition | Start content at the first useful control/evidence region; relocate export/save/settle actions to their owning toolbar/card and remove page-level refresh affordances | M2–M7 mockups and future Blazor shared page layout | Implemented — OWNER_REVIEW |
| 2026-07-24 | Shared order-detail surface | My Orders và History hiển thị cùng cấu trúc filter + item table; My Orders rộng hơn và có action slot theo capability, History/kỳ trước không có mutation action | Owner nhận thấy hai chi tiết đơn gần như cùng nghiệp vụ nhưng mockup đang dùng hai primitive khác nhau | M2.08 + M2.10 + future Blazor refactor | Introduce one reusable order-detail primitive with width/context variants, fixed six-column contract and backend-authoritative action policy | Current/supplement/previous order panels and History detail | Implemented — OWNER_REVIEW |
| 2026-07-24 | Atlas archetype consolidation | Mọi screen cùng motif phải compose từ shared shell, collection, master-detail, order-detail, workflow, period-operation, account và content-state archetype trước khi nhận tùy biến nghiệp vụ | Owner yêu cầu các page giống nhau phải gộp một lần để duyệt/sửa hàng loạt, tránh code và QA riêng từng màn | Design Atlas 28 screens + future Blazor component map | Add archetype matrix; introduce `collectionWorkspace` and `masterDetailWorkspace`; refactor Catalog, Management, Approval, Library and Users consumers without changing approved visual geometry | M0–M8 Atlas; implementation only after corresponding board approval | Implemented — BUSINESS_RULES_REVIEW |
| 2026-07-24 | Catalog read-only simplification | Danh mục mặt hàng chỉ là một collection tra cứu full-width; bộ lọc nằm trong header card dữ liệu, không tạo toolbar/card độc lập; không có inspector cố định hoặc `Thêm vào đơn` | Owner xác nhận Catalog không tham gia luồng tạo đơn, sau đó yêu cầu gộp filter vào chính khối danh mục | M2 Product Catalog | Compose from one collection card: title/summary → search/category/unit/clear filter → table → pager. Không hiển thị download cho tới khi có endpoint/export thật; remove order mutation affordance and persistent detail panel | M2.11 Product Catalog | Implemented — OWNER_REVIEW |
| 2026-07-24 | Approval item-review requirement | Người duyệt phải xem đầy đủ danh sách mặt hàng và số lượng trong cùng detail surface trước khi Phê duyệt/Từ chối | Owner xác nhận quyết định duyệt không được chỉ dựa trên số tổng hợp | M3 Supplement Approval + M4 Pending Approvals | Reuse compact order-detail filter/table inside approval inspector; keep decision actions after item region and enforce capability/audit in implementation | M3.14 + M4.16 | Implemented — OWNER_REVIEW |
| 2026-07-24 | Pager proximity rule | Danh sách ngắn đặt pager ngay sau hàng dữ liệu cuối và card co theo content; danh sách dài dùng body cuộn nội bộ với pager ở đáy viewport của chính grid | Owner cân nhắc đặt pager ở đáy card hay ngay dưới content; card cao toàn viewport tạo khoảng trắng làm pager rời dữ liệu | Shared table/card primitive + Product Catalog representative | Add reusable `content-fit` table variant; preserve bounded-scroll variant for long data and keep pagination outside scrolling rows | M2 Catalog first; reuse across short collection pages | Implemented — OWNER_REVIEW |
| 2026-07-24 | Data navigation mode by task intent | Order composition and item-detail regions use internal scroll + virtualization; view/lookup, approval queue and record-management lists use server paging | Owner chốt màn tương tác liên tục trên nhiều dòng phải giữ working state, còn màn chọn một record để mở chi tiết dùng paging | Shared DataGrid primitive + all Atlas data screens | Use `scroll-virtualized` for My Orders items, Order Create, selected order detail, aggregated item totals, source comparison and permission matrix; retain paging in Catalog/History list/management/approval queue/library/users/reports; add Atlas contract assertions | M2–M7 data surfaces | Implemented — OWNER_REVIEW |
| 2026-07-24 | Full-dataset filtering before paging | Search/filter/sort always apply to the full authorized server query before count and `Skip/Take`; filtering must find records outside page 1 | Owner identifies a common paging defect where client filtering only affects the loaded page | API list contract + shared Radzen DataGrid adapters | Standardize `LoadData` query/response, first-page reset, debounce, full-data distinct filter values and outside-page regression fixture; document Atlas-to-Radzen boundary | Every paging/virtualized data surface | Approved contract — IMPLEMENT_AFTER_BOARD_APPROVAL |
| 2026-07-24 | History detail top alignment | Cạnh trên detail drawer ngang chính xác với cạnh trên hàng KPI; bộ lọc phạm vi kỳ chỉ thuộc cột trái và nằm cao hơn drawer | Owner chỉ ra detail đang căn theo scope filter làm hai cột mất baseline nội dung | M2.10 History grid | Move detail from grid row 1 to row 2 while preserving bottom alignment and internal scroll; add responsive geometry assertion with 1px tolerance | M2.10 History desktop/compact desktop | Implemented — OWNER_REVIEW |
| 2026-07-24 | Atlas-wide pager/virtualization audit | Mỗi screen có data navigation mode xác định và được regression kiểm tra trên cả ba viewport; master list phân trang độc lập với vùng item detail cuộn ảo | Owner yêu cầu phân biệt rõ màn thao tác liên tục với màn tra cứu/xem dữ liệu | 28-screen Design Atlas renderer | Lock paging consumers (History list, Catalog, Management, Approval queue, Library, Users, Reports) and virtualized consumers (My Orders, Order Create, History/Management/Approval detail, period item totals, source comparison, permission matrix) | M0–M8 Atlas responsive suite | Verified — OWNER_REVIEW |
| 2026-07-24 | Representative page fullness | Paged mockup có total lớn phải render đủ một page representative thay vì chỉ vài sample row; pager vẫn nằm ngay sau content, không ghim xuống đáy viewport và không kéo giãn row | Catalog ghi 286 mặt hàng nhưng chỉ hiển thị 6 sample làm vùng trang bên dưới trông trống bất thường | Atlas deterministic fixtures + shared pager | Render 10 representative Catalog rows and summary `1–10 trên 286`; real one-page sparse datasets remain content-fit with normal page background below | M2 Catalog; apply fixture rule to future paged mockups | Implemented — OWNER_REVIEW |
| 2026-07-29 | Department Summary reuses History workspace | Tổng hợp phòng ban phải gần như y hệt Lịch sử đơn về scope kỳ, KPI, biểu đồ, filter, popup, pager, detail sheet và responsive; chỉ schema cột, API, permission và scope nghiệp vụ khác | Owner không chấp nhận hai màn cùng motif nhưng tiếp tục có hai implementation dễ drift | `HistoryOrderWorkspaceTabBase` + `HistoryWorkspaceShell` + `HistoryOrderList` column/mobile slots + department history endpoints | Route Department chỉ khai báo endpoint/permission và truyền 8 cột riêng; server khóa scope bằng department/company claim; consumer ledger giảm một grid implementation; route-real kiểm bốn viewport và visual review | M3 Department Summary | Implemented — OWNER_REVIEW |
| 2026-07-29 | Order Create virtual header paint correction | Cell Mặt hàng hai dòng không được ló tên/mã lên header khi cuộn, nhưng không được ẩn row bằng JS theo timing vì dễ gây nhảy/flash | Owner bác bỏ geometry guard; Radzen DataGrid dùng table + Blazor `Virtualize` spacer nên table painting order vẫn để row thắng sticky header ở offset lẻ | Order Create catalog virtualization | Xóa module/observer/class guard; dùng Blazor `Virtualize` cho danh sách chọn hàng và header `div` độc lập trong cùng native scroll viewport; giữ bounded DOM, filter snapshot, code popup và action | M2.09 Order Create | Implemented — OWNER_REVIEW |
| 2026-07-24 | Long-data virtualization evidence | Mọi Atlas screen dùng virtualization phải có đủ fixture row để scrollbar nội bộ thật sự xuất hiện; không chỉ gắn class/contract vào bảng 3–5 dòng | Owner nhầm My Orders là bảng thiếu pager vì mockup quá ít dữ liệu nên không thể quan sát scroll | My Orders, Order Create, History detail, Approval detail | Expand deterministic order-item fixtures, synchronize displayed totals and assert every virtualized region has `scrollHeight > clientHeight` on all review viewports | M2–M4 virtualized surfaces | Verified — OWNER_REVIEW |
| 2026-07-24 | Order export parity | My Orders và selected History order đều có hai action xuất PDF/Excel; export lấy toàn bộ order snapshot, không chỉ row trong viewport virtualization hoặc item filter đang hiển thị | Owner xác nhận cả hai ngữ cảnh đều hỗ trợ xuất đơn | Shared order-detail/export action primitive | Enable My Orders export actions; add compact export actions to History detail header; implementation endpoint must authorize selected order and generate from canonical full item set | M2.08 My Orders + M2.10 History detail | Implemented — OWNER_REVIEW |
| 2026-07-24 | Order Create notes, removal and price visibility | Nhân viên tạo đơn phải nhập được ghi chú cấp đơn và ghi chú riêng cho từng mặt hàng; hành động bỏ chỉ xuất hiện bằng nút `X` trong panel Đơn đang tạo; màn nhân viên không hiển thị đơn giá, thành tiền hoặc tạm tính | Owner phát hiện Atlas thiếu ghi chú theo dòng/nút bỏ chọn, tự thêm thông tin giá không thuộc quyền nhân viên và lặp hành động bỏ chọn ở hai khu vực; lượt duyệt sau phát hiện header cột lệch row vì scrollbar gutter chỉ nằm ở body | M2.09 Order Create + shared order-line editor | Align Atlas with the existing `OrderCreateStep2.razor` contract (`Context.Description`, `item.Description`, quantity and `RemoveItemAsync`); catalog chỉ hiển thị trạng thái `Đã chọn`, còn draft panel dùng grid `Mặt hàng → SL → Đơn vị → X` và note sub-row. Header + rows cùng nằm trong một internal scroll container với sticky header, nên scrollbar không làm lệch trục; Atlas dùng fixture 6 selected items để chứng minh scroll thật. Expose price only in explicitly authorized management/library-price contexts, enforced by backend permission rather than visual hiding alone | M2.09 Atlas now; Blazor implementation audit before coding | Implemented — OWNER_REVIEW |
| 2026-07-26 | Popover note editor and draft action hierarchy in Order Create | Mỗi dòng mặt hàng chỉ có icon ghi chú đặt ngay bên trái nút xóa; ghi chú cấp đơn rời khỏi thân panel và dùng nút `Ghi chú` trong footer. Cả hai mở popover theo motif ô lọc/Lịch sử đã duyệt. Footer chia thành `Quay lại` đứng riêng bên trái và nhóm `Lưu nháp → Ghi chú → Tiếp tục` căn phải; `Lưu nháp` dùng nền xanh nhạt, `Ghi chú` trung tính và `Tiếp tục` là primary duy nhất | Owner muốn tăng chiều cao hữu dụng của danh sách mặt hàng, bỏ text ghi chú lặp trên từng dòng, rút gọn nhãn và làm rõ cấp bậc bốn hành động đang gây rối; lượt duyệt sau yêu cầu sửa khoảng cách không đều thành mô hình 1 trái–3 phải | M2.09 Order Create + shared order-line editor | Use a two-icon row action slot `note → remove`; keep item/order note editors in one reusable anchored popover; separate back navigation from the forward action group; use 5px spacing for secondary actions and an additional 7px before primary; reuse Atlas tokens, preserve keyboard labels and separate data scopes | M2.09 Atlas now; map to accessible Blazor action hierarchy after approval | Implemented — OWNER_REVIEW |
| 2026-07-24 | Viewport-filling data workspaces | Cả màn paging và scroll/virtualization đều dùng card dữ liệu kéo đến đáy vùng nội dung; paging giữ row ở chiều cao tự nhiên và neo pager ở đáy, còn virtualization chỉ cuộn thân grid với sticky header | Owner muốn mọi data view tạo một mặt phẳng làm việc đầy đủ đến đáy thay vì kết thúc ngay sau vài dòng và để khoảng trống ngoài card | Shared collection/data-card/grid primitive | Remove route-specific content-fit behavior; let the card flex through remaining viewport height; use `margin-top:auto` for pager without stretching rows; retain bounded internal overflow for virtualized grids; renderer measures the bottom edge of every paged card against the canvas content edge | All paged and virtualized Atlas screens | Implemented — OWNER_REVIEW |
| 2026-07-24 | History-like management workspaces | Tổng hợp phòng ban và Toàn công ty dùng chung master–detail motif với Lịch sử đơn: phạm vi kỳ, KPI, biểu đồ, danh sách đơn phân trang và chi tiết đơn virtualized; danh sách bỏ tài khoản nhưng không được bỏ mất nhận diện, workflow hoặc ngữ cảnh của đơn | Owner xác nhận hai trang quản lý về bản chất vẫn là duyệt/tìm đơn; sau khi khôi phục đủ dữ liệu, owner tiếp tục ưu tiên khả năng đọc và cho phép bỏ dữ liệu đã có ở scope/detail | Shared history-like order workspace + order-detail primitive | Department list dùng 8 cột `# → Kỳ → Mã đơn → Họ tên → Loại đơn → Trạng thái → Ngày gửi → Ghi chú`; company list dùng 8 cột `# → Kỳ → Mã đơn → Họ tên → Phòng ban → Loại đơn → Trạng thái → Ngày gửi`. Cả hai dùng font/axis giống History; `Mặt hàng/Số lượng` giữ ở KPI, chart và printable detail | M3 Department Summary + All Orders | Implemented — OWNER_REVIEW |
| 2026-07-26 | Atlas catalog/library source alignment | Bỏ hoàn toàn trường `Thương hiệu`; Catalog nhân viên chỉ hiển thị mặt hàng active và không có cột trạng thái; dữ liệu quản trị bám đúng entity/DTO hiện hành. Trong Catalog chỉ đọc, tiêu đề/tóm tắt nằm bên trái và bộ lọc nằm bên phải trên cùng một header; chỉ xuống dòng khi viewport không đủ rộng | Source không có Brand, category parent, supplier tax/contact/approval status hoặc department manager field; frontend Catalog cũng chưa có download thật. Owner yêu cầu tiêu đề và bộ lọc phải ngang hàng thay vì xếp thành hai dải | M2 Catalog + M5A/M5B fixtures + data navigation contract | Catalog dùng `Mặt hàng / Danh mục / Đơn vị / Mô tả`, chỉ search/filter/sort + server paging; admin status chỉ map `IsDeleted`; category, supplier, department và lookup fixture dùng đúng field source; approval queue dùng paging còn detail item dùng virtualization. Desktop dùng một hàng `title ← → filters`; responsive mới wrap có kiểm soát | Catalog, Classes, Categories, Items, Suppliers, Departments, Supplement Approval | Implemented — VERIFIED |
| 2026-07-24 | Data-surface continuation and unified footer | Vùng trống của paged table để sạch, không dùng row guide/fake row; dòng dữ liệu cuối luôn có border dưới; virtualized list bỏ spacer rỗng và dùng footer cùng motif pager nhưng chỉ hiển thị tổng/ngữ cảnh, không có số trang | Owner thấy row guides và khoảng thở cuối scroll thiếu đồng bộ, muốn cả hai mode có điểm kết thúc rõ ràng | Shared paged-grid and virtualized-grid primitives | Keep the flexible blank surface between the last row and pager; preserve the final row separator; add a fixed summary-only footer after every virtualized region (for example `Tổng cộng 18 mặt hàng`); không dùng câu hướng dẫn cuộn và không render page-number controls cho virtualization | All paging and virtualized Atlas screens | Implemented — OWNER_REVIEW |
| 2026-07-24 | Flat Apple Music-era button hierarchy | Button toàn Atlas dùng scale và state thống nhất: standard 36px, compact table action 30px, login primary 48px; bỏ hoàn toàn border, gradient, inset highlight và shadow kiểu Liquid Glass; primary dùng xanh phẳng, secondary/quiet dùng neutral tint khi hover, destructive dùng red tint khi hover/focus | Owner muốn phong cách Apple Music cũ, phẳng và mọi icon/label/hitbox thẳng hàng quang học | Global button primitive + compact data-row action + quantity stepper | Use 8–9px radius, 16–17px symbols, one shared neutral hover token and explicit pressed/focus-visible states; make `Thêm` and `Đã chọn` share the same 66×30 interaction column footprint; flatten the quantity stepper and align its three segments; renderer rejects any `.btn` with border, shadow or background gradient | All Atlas screens; map to Radzen button classes after approval | Implemented — OWNER_REVIEW |
| 2026-07-24 | Trailing form actions and single-layer filter chrome | Footer action của form/inspector căn phải theo hướng đọc tiếng Việt; action phụ/destructive đứng trước action chính. Filter toolbar nằm bên trong data card không được bọc thêm một khung bo tròn chồng lên card | Owner thấy nút tiếp tục nằm trái làm luồng hoàn tất kém tự nhiên và khung filter oval lấn/đè lên vùng bảng | Shared inspector/form footer + nested filter toolbar | Set trailing alignment for all inspector action slots; keep hitbox/button hierarchy unchanged. Nested interaction filter owns only one bottom divider, while search/select retain their own radius; standalone collection filters may keep their independent toolbar surface | Order Create, Approval, record inspectors and future form panels | Implemented — OWNER_REVIEW |
| 2026-07-24 | Unit filter coverage for item datasets | Mọi màn hình có danh sách mặt hàng và cột `Đơn vị` phải hỗ trợ lọc `Tất cả đơn vị`; filter áp trên full authorized dataset trước paging/virtualization | Owner yêu cầu bổ sung bộ lọc đơn vị từ My Orders và lan truyền sang các page liên quan | Shared order-item toolbar + item collection filters | Add Unit after Category in My Orders, History/Management/Approval detail and Order Create; Catalog keeps its existing Unit filter; Library Items uses Category → Unit → Status, Library Prices uses Unit → Status. Không thêm Unit vào order/department summary list vì đó không phải dataset mặt hàng | M2 My Orders/Create/History, M3 detail, M4 approval detail, M5 Catalog/Items/Prices | Implemented — OWNER_REVIEW |
| 2026-07-24 | Printable canonical order sheet | Chi tiết đơn ở Lịch sử, Tổng hợp phòng ban, Toàn công ty và hàng chờ duyệt dùng chung một phiếu có thể chuyển sang PDF/A4: nhận diện đơn, bốn ô `Kỳ/Loại đơn/Người đặt/Phòng ban`, hai tổng số, ghi chú/lý do và bảng mặt hàng canonical | Owner muốn motif chi tiết đang duyệt trở thành chuẩn dùng chung và có thể in thành phiếu chi tiết đơn | Shared printable order-detail primitive | Keep the four identity fields stable across contexts; move sent time, wait duration and quota into metadata/supporting copy; show PDF/Excel on every sheet; approval adds decision actions while read-only contexts retain history action; print media hides interactive filters/actions and expands the item table | M2 History + M3 Department/Company + M4 Approval | Implemented — OWNER_REVIEW |
| 2026-07-23 | History zero-result and interaction geometry lock | Table filters, period changes and detail refreshes must not alter master/detail tracks, chart/SVG bounds or card heights; zero matching rows affect only the master-list empty state and must retain the last selected detail until another order is chosen | Owner demonstrated that clearing the selected order removed detail context and changed the page's intrinsic width, shrinking the chart/SVG/list, while select menus inherited `min-width:100%` from the fixed viewport and expanded across the page | History state rendering + shared async/transient-surface behavior | Always render the desktop detail surface; preserve current detail during requests and zero-result filters, then atomically replace it after a successful selection; use the same compact overlay indicator for chart, list and detail refresh; keep zero-summary KPI/chart/list geometry; reset inherited fixed-position insets on desktop; size menus from content/trigger and correct transformed containing-block offsets against the rendered trigger geometry | History, future master/detail dashboards and project-owned select menus | Implemented — VERIFIED |
| 2026-07-23 | Borderless inline copy action | Full-value popovers use the lightweight overlapping-squares copy symbol without a second visible button frame; the icon is smaller than the adjacent text line and remains vertically centred | Owner found the previous 28px bordered button produced three nested rectangles and visually dominated the order code | Shared disclosure/popover actions | Use a transparent 20px hit surface, 14px `filter_none` symbol at medium-light weight, neutral default color and restrained hover/focus feedback; preserve localized accessible name and keyboard focus | History code/note/item popovers, future inline copy actions | Implemented — OWNER_REVIEW |
| 2026-07-23 | History master/detail table parity | Detail items expose their own line note, use the same light-blue DataGrid header surface and column semantics as the order list, and label UOM as `Đơn vị` / `Unit`; the canonical detail order is `# → Mặt hàng → Danh mục → Đơn vị → Số lượng → Ghi chú` | Owner identified that the line-level note stored in `VppRequestDetailResDTO.Description` was omitted, the detail header looked like a separate visual system, and a trailing quantity column appeared pressed against the panel edge | History detail + shared master/detail table pattern | Add responsive `Ghi chú` as the trailing disclosure column with ellipsis, full-value popover and copy action; keep quantity right-aligned inside its own padded track before notes; remove fixed pixel widths and use proportional tracks that preserve Radzen virtualization; both master and detail must use the canonical `--rz-grid-header-background-color` tint rather than overriding it with the white elevated surface | History detail, future master/detail tables, all `UOM` resource consumers | Implemented — OWNER_REVIEW |
| 2026-07-23 | Shared optical centre contract | Badge text, chart legend dot/label, categorical table columns and detail-header status must be centred by the rendered content box, not assumed correct from inherited line-height | Owner found visible centre drift across the History legend, order type/status columns and selected-order heading even though outer tracks were already aligned | Global badges/chips/legends + dense tables | Standardize non-interactive pills at a 22px border-box height with normalized line-height; interactive legend toggles keep a stable 28px hitbox; explicitly centre internal content, keep state borders inside the fixed box, and add route-real geometry assertions for both horizontal and vertical centres | History first; retrofit all status badges, categorical chips and chart legends | Implemented — OWNER_REVIEW |
| 2026-07-23 | History data story and overflow contract | Current-period default, no repeated page title/helper label, fixed single-viewport workspace, viewport-filling master/detail regions, measured server page capacity and detail-only virtual scrolling | Owner prioritizes optical alignment, minimal copy/wrap and no horizontal or avoidable outer scrolling; detail may contain 500 items | Employee history route + shared dense-data lessons | Replace expandable rows with aligned KPI/chart/list and lazy virtualized drawer; lock the outer history shell like My Orders, let both data surfaces reach the viewport bottom, page the order list on the server and reserve row virtualization for the bounded detail item region | `dashboard?tab=1`, future exact-lookup routes | In implementation |
| 2026-07-23 | History long-list capacity and performance | Wide desktop uses a real CSS grid row span instead of absolute positioning or percentage width calculations; the order list and selected-order detail share one bottom axis. Orders are fetched with server-side `skip/top` paging sized from the measured list card, while a selected order loads at most 500 item DTOs once and Radzen renders only the visible detail rows with a small overscan buffer | Owner wants both lists to use the full screen without introducing page scrolling and asked whether 500 detail rows require paging | History master-detail + shared dense-list performance | Keep the master list paged because total order history can grow without a known bound; keep detail virtualization because the per-order maximum is bounded and local search/category filtering remains instant at 500 rows. Sticky headers and fixed footers remain visible; only the corresponding grid body scrolls when necessary | `dashboard?tab=1`, future bounded master-detail panels | In implementation |
| 2026-07-23 | History master-detail and supplement color sync | Wide desktop uses a true two-column story: KPI/chart/list stack on the left and persistent selected-order detail spans from the KPI row down on the right; compact view uses an on-demand overlay. Supplement identity uses the canonical amber warning token everywhere, while status retains its own semantic color | Owner clarified that cross-project consistency is the primary design rule and the approved mockup intended a persistent full-height right detail column, not a panel beginning beside the list only | Employee history route + shared order identity | Auto-select the first order on each page, keep detail-only virtualization, hide duplicated date/note columns in split mode, and replace history teal supplement accents with `--vpp-warning` / `--vpp-warning-muted` | `dashboard?tab=1` | Implemented — OWNER_REVIEW |
| 2026-07-23 | Apple HIG visual audit — History | Apply system-font hierarchy, compact control sizing, restrained material/shadow, visible hover/focus feedback and reduced-motion-safe transitions; fix KPI stretch, filter rhythm, flexible table width and persistent desktop detail hierarchy | Owner requested a full HIG-based review after route screenshot exposed oversized KPI cards, blank table width, loose filter spacing, missing interaction feedback and an unnecessary desktop X | Employee history route + shared visual tokens | Keep project tokens authoritative; use Apple HIG as a quality bar rather than importing Liquid Glass into the content layer; validate at route-real viewports | `dashboard?tab=1` | Implemented — OWNER_REVIEW |
| 2026-07-23 | History period control presets | Keep the horizontal sliding selection pattern with localized labels `Tất cả kỳ`, `Kỳ này`, `3 tháng`, `6 tháng`, `12 tháng`, and `Tùy chọn`; default to the backend-authoritative current period and keep the custom month range as a compact popover | The history page is most useful when it opens on the active business cycle; “Kỳ này” is clearer than a generic one-month duration, while the owner wants the existing horizontal motion retained, so labels stay compact and responsive | Employee history route + shared control rule | Use the project-blue sliding indicator for the selected preset; inactive options have no fill but retain high-contrast primary text, hover adds a neutral surface, and every label must exist in VI/EN resources. Keep the custom editor viewport-safe; do not hard-code width or add avoidable page scrolling | `dashboard?tab=1`, future period/time filters | In implementation |
| 2026-07-23 | Shared Apple-like interaction contract | Treat the already-approved Apple-like sidebar and header as the shell reference for every new control: system font metrics, compact hitboxes, neutral hover surface, project-blue active state, soft motion, keyboard focus, responsive behavior, and VI/EN localization for every new label. KPI cards are actionable summary controls, not passive decoration; hover/focus reveals affordance and click/tap opens one contextual detail popover with definitions and derived ratios | Owner asked to stop repeating shell/design requirements and make KPI space useful without adding a second dashboard layer | Global shell + all dashboard/history KPI surfaces | Use exactly one transient surface at a time: opening any select, custom range, KPI, note, or code disclosure automatically closes every previously open surface; outside click and Escape also close it. Match sidebar hover with a full neutral interaction surface; do not add decorative chevrons when cursor, hover, focus, and the action result already communicate clickability. Keep critical values visible without interaction, avoid hover-only behavior on touch, and reuse project tokens instead of importing Apple colors/materials | Sidebar, header tabs, filters, KPI cards, summary panels across authenticated routes | In implementation |
| 2026-07-23 | Full-bleed KPI interaction surface | KPI hover, focus and selected backgrounds must fill the complete card content box; never inset the interactive button inside a second white padded frame | Owner identified the remaining four-sided white band around the hovered KPI card, which made the hit area look smaller than the visible card | History KPI cards + shared actionable summary cards | Remove container padding, keep spacing inside the trigger, inherit the outer radius, and verify trigger/card edges through route-real DOM geometry | History KPI cards, future clickable KPI/summary cards | Implemented — OWNER_REVIEW |
| 2026-07-23 | Context-preserving async loading | Filter/select changes must keep the existing page geometry and last-known data visible while new data is fetched; do not render a global progress line for period changes, and keep any refresh feedback scoped to the affected content without moving the layout | Owner found the top loading line visually distracting even when switching to a genuinely different period; context preservation is more important than exposing every short network transition | Global async filter/select interaction + History | Full-page skeleton is allowed only for the initial empty render; subsequent summary/grid/detail refreshes keep content in place, use restrained scoped state only when needed, respect reduced motion, and never introduce layout shift | History filters, period selector, KPI cards, charts, tables, drawers, future dashboard controls | In implementation |
| 2026-07-23 | Unified async and content-state system | Loading, refreshing, empty, filter-empty, error, retry, disabled and success feedback must use one shared visual language across the project: the same geometry-preserving behavior, semantic icon treatment, typography, surface hierarchy, motion duration and localized VI/EN copy | Owner found each History region loading differently and requires future designs to remain consistent with the Apple-like sidebar/header | Global state primitives + all authenticated content | Initial load may use a low-contrast geometry-matched skeleton; later refresh keeps last-known content and shows one compact scoped indicator. Empty/error panels use the same state primitive and spacing, errors preserve recoverable context with retry, no full-page flicker or top progress line, transient motion uses the shared 200ms navigation easing, and reduced-motion disables shimmer/spin. Every route review must cover loading, populated, empty, filter-empty, error/retry, disabled and success at route-real viewports | History implemented first; retrofit dashboard, catalog, reports, permissions and dialogs through the shared queue | In implementation |
| 2026-07-23 | Dense table disclosure and code identity | Keep rows compact and scan-friendly: categorical type/status columns use centered axes, identifiers and notes use leading axes, numeric fields use trailing axes, and dates use centered tabular numerals. Long notes and order codes stay ellipsized in the row but open a single anchored popover on click/keyboard to reveal the full value | Apple HIG recommends succinct table content, preserving recognizability of clipped text, and using disclosure/info affordances for details instead of oversized rows. Current request codes are generated as immutable `VPP-YYYYMM-32hex` values (43 chars, database limit 64), so changing the business identifier would be riskier than optimizing display | History table + shared dense-data pattern | Use fractional responsive tracks with identity/note priority, no hard widths, compact 36–40px rows, one popover at a time, outside-click/Escape close, and full value selection without changing the column geometry | History list, order grids, future audit/report tables | In implementation |
| 2026-07-23 | Content-aware desktop table tracks and scrollbar gutter | Do not distribute all desktop width evenly: compact categorical/metric columns get content-sized minimums, while code and note columns absorb the remaining space. A scrollbar gutter is reserved only when the corresponding grid body actually overflows, so short lists do not show a false clipped strip at the right edge | Owner reports excessive column breathing room on a 24-inch screen and a small right-side crop; the previous all-`fr` grid plus unconditional `scrollbar-gutter: stable` caused both symptoms | History order list + detail grid + shared dense-data pattern | Use responsive `minmax()` tracks at wide desktop, keep the narrow split layout compact, toggle overflow gutter from measured DOM state, and revalidate header/cell edges at 1366px and 1920px without horizontal scrolling | History list, future dense tables | Implemented — OWNER_REVIEW |
| 2026-07-23 | Search active-state distinction | A search field with text keeps its neutral surface and uses a project-blue 1px outline; do not fill the entire search field blue. Filled project-blue remains reserved for mutually exclusive selected filters/actions so text-entry and selection states are visually distinct | Owner found white text on a fully blue search surface harder to scan | Global search/filter controls | Empty search uses neutral gray outline, populated/focused search uses blue outline with normal dark text, and VI/EN placeholder behavior remains unchanged | History main/detail search and future search fields | In implementation |
| 2026-07-23 | Purpose-based filter toolbar parity | Detail views must reuse the same search/select/clear-filter motif as their master list, but only expose filters that answer a real lookup task rather than mirroring every table column; master filters follow `Tìm kiếm → Loại đơn → Trạng thái → Xóa bộ lọc` | Owner wants History detail filters visually consistent with the order list, prefers type before workflow status, and previously approved filtering by user intent instead of one filter per column | History list + selected-order detail + shared filter toolbar | Detail keeps only item code/name search and category selection, adds an always-visible clear action with the same neutral/active states, uses the same 32px controls and spacing at 24-inch desktop, and moves clear to a full-width second row only when responsive width requires it; keep the master filter order stable across breakpoints | History detail, future master-detail tables | Implemented — OWNER_REVIEW |
| 2026-07-23 | Collision-aware popover placement | Transient surfaces are not assigned a permanently fixed direction. Filters, KPI details, notes, codes, and custom ranges prefer opening below their anchor, then automatically flip above when the measured viewport space is insufficient; horizontal anchoring also flips to stay inside the viewport | A row-count-based rule forced the only history row to open upward and overlap the toolbar, while fixed left/right anchoring can overflow on responsive layouts | Global popover/select/disclosure behavior | Note in the trailing column prefers end alignment and expands left; order code prefers start alignment and expands right; all surfaces preserve an 8px viewport gap and are repositioned after DOM changes or viewport resize | History transient surfaces, future tables and filters | In implementation |
| 2026-07-23 | Interactive chart legend without strike-through | Replace Radzen's default hidden-series strike-through legend with a project-owned accessible toggle group: colored swatch plus label, active series uses a restrained blue-tinted surface and outline, inactive series remains transparent and muted, and both series may be hidden when the user wants a clean chart canvas | Owner found strike-through visually noisy and explicitly wants to be able to turn off both order types; a subtle empty-state message preserves context when no series is visible | History chart + shared chart legend pattern | Hide the built-in legend, bind custom buttons to the series `Visible` property, use `aria-pressed`, preserve regular/additional semantic colors, show a localized no-series state when both are off, and never communicate state through text decoration alone | History chart, future multi-series charts | In implementation |
| 2026-07-23 | In-column chart value labels | Show non-zero quantities centered inside each visible column/stack segment using compact, high-contrast text; keep the native tooltip as the exact-value fallback | Owner wants the visible value (for example `28`) readable without hovering and requires large values not to break the chart | History chart + shared quantitative-chart pattern | Measure the rendered SVG segment, fit the label to both width and height, use locale-aware compact notation from 10,000 upward, omit only labels that cannot remain legible, and preserve the full localized value in the tooltip/accessible chart summary. Avoid `RadzenSeriesDataLabels` on stacked columns because version 11.1.4 caused the route to remain in initial loading | History chart, future stacked column charts | Implemented — OWNER_REVIEW |
| 2026-07-23 | Single-container detail hierarchy | A persistent drawer/card owns the primary boundary; summary, toolbar, virtualized table, and count footer inside it must not each become another rounded bordered card. Use full-width sections, spacing, and single-pixel separators; reserve independent surfaces for interactive controls and semantic badges | Owner identified excessive nested boxes and wrapping in the order-detail panel | History detail + shared master-detail pattern | Remove inner summary/table/footer frames and radii, keep one outer panel, preserve sticky table header and detail-only vertical scrolling for up to 500 rows | History drawer and future persistent detail panels | In implementation |
| 2026-07-23 | Detail item identity hierarchy | In dense item tables, show the human-readable item name as the primary line and the VPP code as secondary metadata below it; long codes remain compact/ellipsized and open a disclosure surface when needed | Owner pays attention to item names more than internal codes; the previous `VPPCode · Name` concatenation made every row start with noisy identifiers | History detail table + shared item identity pattern | Use a compact two-line identity cell, preserve code searchability/full-value access, and allow the detail table's vertical scroll to absorb the extra line without horizontal wrapping | History detail, catalog/order item tables, future exports previews | In implementation |
| 2026-07-23 | Collapsed hover-surface rhythm | Line-to-first-hover gap bằng first-to-second-hover gap trong tolerance thị giác | Owner muốn spacing đều khi tính cả hover surface, không chỉ icon glyph | Shared sidebar | Remove remaining header/nav overlap and compare runtime hover-surface bounding boxes | Sidebar expanded/collapsed | Implemented — OWNER_REVIEW |
| 2026-07-23 | Collapsed navigation optical offset | Root navigation group dịch xuống 6px để rounded first surface tách khỏi unified-toolbar baseline, giữ nguyên row rhythm | Hairline mới làm icon đầu nhìn bám quá sát header dù geometry cũ từng cân với logo | Shared sidebar | Reduce header/nav overlap from 10px to semantic 4px and assert first row optically clears header bottom | Sidebar expanded/collapsed | Implemented — OWNER_REVIEW |
| 2026-07-23 | Unified toolbar baseline | Logo và primary tabs cùng một surface, một horizontal hairline xuyên suốt, vertical divider chỉ bắt đầu dưới header | Seamless white chrome loại bỏ đường cắt nhưng làm logo thiếu điểm neo thị giác | Shared shell | Extend primary nav bottom divider through sidebar header using semantic inset shadow | Mọi authenticated desktop route | Implemented — OWNER_REVIEW |
| 2026-07-23 | Shared refresh reveal | Document refresh dùng coordinated sidebar/header/content reveal, không áp cho internal navigation và tôn trọng reduced motion | Owner muốn refresh đẹp như Apple và đồng bộ motion language của header/sidebar | Shared authenticated shell | Pre-paint root marker, three subtle reveal keyframes, timed cleanup and BFCache guard | Mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-23 | Sidebar/header seamless chrome | Không vẽ sidebar divider/shadow trong hàng header 72px; divider chỉ bắt đầu dưới logo/tab | Owner muốn logo và primary tab nằm trên một dải trắng liền mạch, không có đường cắt giữa hai vùng | Shared shell | Replace full-height border/shadow with token-based 1px background divider below header | Mọi authenticated desktop route | Implemented — OWNER_REVIEW |
| 2026-07-23 | My Orders full-bleed header | Outer order shell không clip primary nav; page body không giữ gutter trống; panel và grid tiếp tục sở hữu overflow | Viewport lock vô tình cắt negative inset compensation và stable page gutter, làm header lộ bốn cạnh và tách sidebar | Employee workspace + primary tab chrome | Change order-shell overflow to visible, page gutter to auto and assert sidebar/viewport edge geometry | `dashboard?tab=0` | Implemented — OWNER_REVIEW |
| 2026-07-23 | My Orders viewport and idle performance | Summary cards là selector duy nhất; page khóa theo viewport; empty CTA theo capability; observer navigation bỏ qua DataGrid row churn | Owner phát hiện page scroll, selector lặp và lag sau 1–2 phút | Employee workspace + shared interaction runtime | Remove internal RadzenTabs, propagate flex min-height, stable both-edge gutter, gate MutationObserver by interaction host | `dashboard?tab=0`, shared tab/sidebar indicator runtime | Implemented — OWNER_REVIEW |
| 2026-07-23 | My Orders shell and column axes | Navigation chrome dùng elevated token trên base content canvas; fixed numeric/UOM axes; bỏ period badge và panel type label | Owner muốn layer shell/content rõ, số liệu thẳng trục và giảm lặp copy mà không đổi workflow | Shared light shell tokens + employee order workspace | Update navigation/Radzen sidebar semantic tokens, grid header flex alignment and conditional order meta | Authenticated shell, `dashboard?tab=0` | Implemented — OWNER_REVIEW |
| 2026-07-23 | My Orders single-viewport tabs | Ba loại order dùng một reusable grid panel trong RadzenTabs; `orderView` giữ internal selection, grid body fixed-height + virtualized | Owner muốn cùng format, giảm page scroll và chịu được khoảng 500 item; `tab=0` đã thuộc primary Dashboard route | Employee workspace + routing + shared order panel | Tách RenderFragment thành component, summary-to-tab interaction, internal scroll/sticky header, supplement-attempt fallback selector; max-width 1760px giữ content gần shell ở wide screen | `dashboard?tab=0` | Implemented — OWNER_REVIEW |
| 2026-07-22 | My Orders hierarchy refinement | Giảm dominance của period title, card hóa ba summary, căn giữa max-width, thêm neutral row hover/right-aligned quantity và tách status khỏi action | Owner chỉ ra hierarchy yếu, khoảng trắng wide-screen, table thiếu affordance và command touch target quá dày | Employee workspace + shared embedded grid | CSS/markup refinement không đổi API/DTO/business flags; action vẫn visible thay vì overflow menu | `dashboard?tab=0`, embedded order detail grid | Implemented — OWNER_REVIEW |
| 2026-07-22 | Blazor reconnect alert | Auto-retry dùng status spinner thụ động; failed/paused mới chuyển thành compact actionable alert, giữ nguyên circuit behavior chính thức | Apple HIG yêu cầu feedback tương xứng mức gián đoạn, progress indicator phải cho biết app vẫn hoạt động và alert chỉ chứa thông tin thiết yếu/action hữu ích | Shared system UI | Thay ripple/blur nặng bằng state-specific icon, title/copy ngắn, one-action footer và accessibility states | Mọi route dùng global `InteractiveServer` | Implemented — OWNER_REVIEW |
| 2026-07-22 | Compact account profile avatar | Avatar lớn phía trên tên trong account popover giảm từ `72px` xuống `64px`; avatar footer vẫn giữ `24px` | Owner muốn tỷ lệ profile header gọn hơn, gần account card Apple và giảm cảm giác avatar lấn át tên/email | Shared account popover | Chỉ thay kích thước avatar profile, giữ nguyên typography, căn giữa và action list | User menu trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Vietnamese tooltip completeness | Tooltip/`aria-label` đổi ngôn ngữ phải lấy bản dịch VI hoàn chỉnh; `SwitchToEnglish` hiển thị `Chuyển sang tiếng Anh`, không để English copy trong giao diện VI | Owner phát hiện menu đã là tiếng Việt nhưng tooltip hover vẫn hiện `Switch to English` do resource VI bị bỏ sót | Global localization + accessibility | Sửa resource và thêm regression test đối chiếu VI–EN, chỉ allowlist tên riêng/mã kỹ thuật dùng chung | User menu và mọi localized tooltip/action | Implemented — OWNER_REVIEW |
| 2026-07-22 | Stable sidebar icon rail | Logo, icon menu cha và avatar dùng chung tâm rail `36px` ở cả expanded/collapsed; expand chỉ hiện thêm label, menu con/cháu vẫn lùi từng cấp | Owner phát hiện expanded đang dùng ba tâm khác nhau (`28px`, khoảng `28px`, `24px`) trong khi collapsed về `36px`, làm icon nhảy ngang và mất hàng | Shared shell + navigation geometry | Thêm primary content offset dùng chung, neo root RadzenPanelMenu/header/user footer vào cùng cột và dẫn xuất lại rail/content offset cấp con | Sidebar expanded/collapsed trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Global interface capitalization | Giữ nguyên casing từ localization thay vì ép `ALL CAPS`: tiếng Anh dùng title style cho navigation/action ngắn, tiếng Việt dùng sentence case tự nhiên; chỉ acronym, mã và dữ liệu nghiệp vụ như `VPP`, `IT`, `UOM` được viết hoa theo ngữ nghĩa | Apple HIG yêu cầu chọn quy tắc phù hợp phong cách/ngôn ngữ rồi áp dụng nhất quán; button và segmented control tiếng Anh dùng title-style capitalization | Global content + typography | Xóa toàn bộ `text-transform: uppercase` trong CSS authored, bỏ tracking rộng đi kèm, nạp casing guard sau Radzen Material theme và thêm architecture regression test | Tabs, sidebar, buttons, DataGrid headers, KPI labels, wizard, account/login và responsive cards | Implemented — OWNER_REVIEW |
| 2026-07-22 | My Orders Apple order workspace | Kỳ hiện tại, đơn chính, bổ sung và kỳ trước trở thành bốn lớp thông tin rõ; action bám backend flags, archive read-only và export chỉ hiện disabled roadmap | Owner yêu cầu thử data story gần Apple Store Order Details nhưng không làm sai nghiệp vụ hiện tại | Employee workspace | Thay command-center bằng current-cycle header + three-part summary; luôn render supplement context; previous-cycle card chỉ xem; thêm PDF/Excel coming-soon controls | `dashboard?tab=0` | Implemented — OWNER_REVIEW |
| 2026-07-19 | Workflow | Không dùng UI Lab; code trực tiếp từng route | UI thật đã tồn tại và đẹp hơn Figma prototype | Global | Living plan này | N/A | Recorded |
| 2026-07-19 | Design authority | Browser runtime thắng Figma | Tránh design/code drift và route coverage thiếu | Global | Figma chuyển thành reference | Toàn bộ route | Recorded |
| 2026-07-19 | Account/feedback | Đồng bộ account shell nhưng không thêm hero ảnh vào mọi trang | Quyết định vòng đầu trước khi owner review runtime | Global | Được thay thế bởi round 2 centered shell | Login/Register/Forgot/Reset/Confirm/Change/Logout | Superseded |
| 2026-07-19 | Account round 2 | Bỏ hero; centered grid shell, copy ngắn, VI/EN switch và validation không layout shift | Owner ưu tiên consistency, viewport-fit và ít text hơn illustration | Global | Cập nhật toàn bộ W0.2 account flow | Login/Register/Forgot/Reset/Confirm/Change/Logout | Owner review |
| 2026-07-19 | Brand/navigation | Thay chữ `G` bằng vector request-document mark; dùng semantic icon map và phân biệt Product catalog/Master data | Chữ G và icon/label cũ khó hiểu, mixed icon font gây missing glyph | Shared shell | Retrofit header/sidebar/notification center | Mọi authenticated route | Owner review |
| 2026-07-19 | Account round 3 | Dùng solid geometric `V` mark; khóa hình học underline/action/link centerline bằng browser test | Owner thấy outline mark chưa đẹp và field/link còn lệch về thị giác | Shared account shell | Cập nhật W0.2 và thay brand mark dùng chung | Account routes + authenticated shell | Owner review |
| 2026-07-19 | Account round 4 | Underline `1px`, confirm-password copy ngắn và secondary actions 50–50 | Owner phát hiện password line nặng hơn field khác và hàng link dù thẳng vẫn chưa cân đối | Shared account form | Cập nhật W0.2 shared password/link treatment | Login/Register/Reset/Change | Owner review |
| 2026-07-19 | W0.2 approval | Forgot title một dòng, tăng nhẹ title-to-field spacing; chuyển W1 | Owner duyệt account flow sau retrofit cuối | Account shell | Khóa W0.2, mở W1 Dashboard | Account routes → dashboard.my-orders | Approved |
| 2026-07-19 | W1 My Orders round 1 | Story header + ba metric hiện tại + single-source CTA + compact previous archive | Loại bỏ lặp kỳ/deadline/action và giữ current story above-the-fold | Employee workspace | Hoàn tất dashboard.my-orders để owner review | dashboard?tab=0 | Owner review |
| 2026-07-19 | W1 My Orders round 2 | Full-height tabs, shell wayfinding và period command center theo `takeaway → evidence → action` | Owner thấy hover bị crop, trạng thái/CTA lệch và câu chuyện dữ liệu chưa đủ rõ/wow | Shared shell + employee workspace | Sửa trực tiếp Blazor thật; giữ chi tiết trong order card | Header/sidebar/tabs + dashboard?tab=0 | Owner review |
| 2026-07-19 | W1 shell round 3 | Sidebar và hamburger kế thừa hover motif đã được duyệt ở tab | Owner muốn shell có cùng ngôn ngữ tương tác | Shared shell | Chuẩn hóa tint/radius/focus/active hierarchy | Header + sidebar trên mọi authenticated route | Owner review |
| 2026-07-19 | W1 My Orders round 4 | Bỏ period/copy trùng, đưa quota vào CTA và chuẩn hóa `mặt hàng` / `item` | Owner yêu cầu copy theo ngữ cảnh và evidence hữu ích hơn | Global content + employee workspace | Thêm terminology contract; cập nhật resource/report/export/test | Toàn bộ runtime UI + report/export | Owner review |
| 2026-07-19 | AI UI/UX toolchain | Dùng official docs MCP + Playwright/Chrome DevTools + Deque axe; chỉ khóa visual baseline sau owner approval | Agent cần hiểu layout/data/accessibility bằng bằng chứng, không dựa vào screenshot hoặc cài nhiều MCP trùng vai trò | Global engineering workflow | Thêm toolchain doc, AGENTS rules và layered QA gates | Mọi route UI hiện tại và tương lai | Adopted |
| 2026-07-20 | W1 shell + period round 5 | Đồng bộ notification icon, kể lại account context, nâng period thành time anchor và bỏ action copy trùng | Owner thấy utility icon lệch màu, user info chưa hữu ích, kỳ hiện tại quá chìm và bổ sung bị nói lại ba lần | Shared shell + employee workspace | Update header/user popover/period hierarchy, single-source CTA và regression assertions | Mọi authenticated route + dashboard?tab=0 | Owner review |
| 2026-07-20 | W1 My Orders round 6 | Nén command center, bỏ submitted description và hợp nhất order detail hierarchy | Owner chưa ưng tổng thể vì card quá ngang, khoảng trống vô nghĩa, evidence/archive lặp và detail header rời rạc | Employee workspace | Recompose story header/deadline/evidence/action + compact order identity header | dashboard?tab=0 | Restored baseline |
| 2026-07-20 | W1 My Orders round 7 | Apple-only content hierarchy, monochrome surface và single accent action | Owner yêu cầu bỏ art direction Apple-only và quay lại baseline round 6 | Employee workspace | Không dùng làm visual authority | dashboard?tab=0 | Reverted |
| 2026-07-21 | W1 My Orders round 8 + angular system | Khôi phục command center trong screenshot, period cùng dòng, supplement tối đa 1 và bỏ quota khỏi CTA; toàn UI ưu tiên góc vuông | Owner yêu cầu bỏ angular experiment cùng Apple-only và quay lại baseline round 6 | Global visual + business rule + employee workspace | Giữ lại business policy tối đa một supplement; hoàn tác visual angular | Toàn bộ Radzen UI | Reverted visual |
| 2026-07-21 | W1 shell round 9 + line navigation | Sidebar active state dùng vạch dọc mảnh, tab underline phủ toàn bộ hitbox, hamburger cùng motif | Owner từ chối visual và yêu cầu quay lại shell Radzen trước round 7 | Shared shell + accessibility/responsive | Hoàn tác native/line-navigation experiment | Header, sidebar, primary/secondary tabs | Reverted |
| 2026-07-21 | W1 shell round 10 — native Blazor | Chuyển shell/tab sang native Razor/HTML/SVG | Owner yêu cầu bỏ thử nghiệm native và quay lại code trước Apple-only | Shared shell + Dashboard | Hoàn tác native primitives; giữ Radzen shell/tab baseline | Header/sidebar + Dashboard | Reverted |
| 2026-07-21 | W1 rollback — owner requested round-6 baseline | Bỏ Apple-only, angular system và native shell thử nghiệm; giữ composition round 6 làm visual authority | Owner yêu cầu khôi phục đúng trạng thái trước vòng Apple-only để tiếp tục ổn định repo | W1 My Orders + shared shell | Khôi phục command center một cột, 4 evidence, compact order identity và Radzen shell/tab; business policy supplement vẫn giữ | dashboard?tab=0 + authenticated shell | Restored — OWNER_REVIEW |
| 2026-07-21 | Frontend/Figma authority correction | Blazor/Radzen là frontend chính; React chỉ là POC phụ và Figma React code layer chỉ là design evidence | Tài liệu Figma trước đó hiểu nhầm React là target sau khi owner đã quay lại Blazor vì deadline | Global workflow | Re-route Figma về source Blazor, đóng băng React plan và giữ browser Blazor làm authority | AGENTS, Figma brief/prompt/toolchain, React plan | Recorded |
| 2026-07-22 | Shared shell line indicator | Giữ RadzenPanelMenu/RadzenTabs; sidebar dùng vạch dọc xanh và tab dùng underline xanh cùng token, bỏ nền active mặc định | Owner chọn motif Aspire nhưng yêu cầu màu xanh VPP và hai navigation primitive phải đồng bộ | Shared shell | Thêm token indicator dùng chung; chỉ thay active visual, không đổi route binding/render mode | Sidebar + primary/secondary tabs trên authenticated routes | CHANGES_REQUESTED — selector chưa khớp DOM Radzen 11 |
| 2026-07-22 | Shared shell line indicator correction | Bám trực tiếp `rz-navigation-item-wrapper-active`/link `active`, vô hiệu active background qua biến Radzen trên chính panel menu và giữ vạch trong vùng không bị `overflow: hidden` cắt | Screenshot runtime cho thấy fixture cũ mô phỏng sai DOM và CSS theme Radzen được nạp sau custom styles | Shared shell + QA learning | Sửa selector theo package Radzen.Blazor 11.1.4; thêm collapsed parent detection theo active descendant | Sidebar mở/thu gọn + dashboard tabs | Implemented — OWNER_REVIEW |
| 2026-07-22 | ChatGPT-inspired sidebar shell | Đưa logo/tên GTAS VPP và toggle vào sidebar desktop; collapsed thành rail logo + nút mở, không có search; hover menu dùng cùng tint/radius/transition với tab | Owner yêu cầu đồng bộ hover và header/expand-collapse theo reference ChatGPT nhưng giữ brand VPP | Shared shell + responsive | Sidebar chiếm đủ hai grid row desktop; mobile giữ toggle ở app header để mở off-canvas an toàn | Header/sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Apple Music-inspired sidebar states | Menu dùng nền trung tính full-row khi hover/active, vạch dọc xanh VPP ở mép trái, nhóm có chevron Radzen và không có nút `+`; account chuyển xuống footer sidebar, collapsed chỉ giữ avatar | Owner cung cấp reference Apple Music và yêu cầu giữ màu xanh của GTAS VPP | Shared shell + responsive | Bỏ account khỏi header và mục logout trùng; giữ logout/context trong account popover mở lên từ footer | Header/sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Collapsed brand-to-expand affordance | Collapsed rail chỉ giữ logo; hover hoặc keyboard focus đổi logo thành icon expand, click mở sidebar; không render thêm nút toggle thứ hai | Owner muốn rail gọn như ChatGPT, tránh logo và nút mở xếp dọc | Shared shell + accessibility | Expanded giữ logo/tên và nút collapse riêng; collapsed hợp nhất brand với expand action | Header/sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Expanded text-only brand | Expanded sidebar chỉ hiện chữ `GTAS VPP`, bỏ logo và bỏ link/underline; collapsed rail vẫn giữ logo-to-expand affordance | Owner chỉ ra brand expanded còn giống link và thừa logo so với reference ChatGPT | Shared shell | Brand expanded trở thành text label không tương tác; navigation không đổi | Header/sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Sidebar utility consolidation | Chuyển language, theme và notification khỏi desktop header xuống footer sidebar phía trên user; collapsed xếp icon dọc và language chỉ hiện mã active; mobile vẫn giữ header hamburger | Owner muốn dọn toàn bộ utility icon còn lại khỏi header | Shared shell + responsive | Desktop header thu về `0px`; notification/account popover neo sang phải sidebar; giữ nguyên behavior và accessibility label | Header/sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Account-contained utilities | Bỏ utility row rời ở footer; language, theme và notification nằm trong account popover mở từ avatar, sidebar chỉ giữ một user row sạch | Owner phản hồi utility row rời rạc và yêu cầu đưa control vào trong icon user | Shared shell + responsive | UserMenu nhận theme/language callbacks; notification panel mở cạnh account popover, mobile overlay an toàn | Header/sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Vertical account action menu | Account popover đổi utility từ hàng ngang thành danh sách dọc full-width: language, theme, notifications, logout; mỗi hàng có icon, label và trailing state/badge | Owner muốn menu user giống cấu trúc action list trong reference thay vì cụm control ngang | Shared shell + accessibility | UserMenu xử lý callback language/theme trực tiếp; NotificationCenter có `MenuMode` để giữ logic inbox nhưng dùng trigger dạng row | Header/sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Upward account popover alignment | Account popover neo trực tiếp phía trên user row và nằm trong bề rộng sidebar khi expanded, thay vì mở lệch sang vùng content bên phải | Owner phát hiện popup dọc đúng cấu trúc nhưng sai hướng/điểm neo | Shared shell + responsive | Expanded dùng `left: space-2` và width theo sidebar; collapsed/mobile mở từ avatar với width đọc được | Header/sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Desktop empty-header removal | Xóa hoàn toàn khoảng header trống 60px trên desktop; mobile vẫn giữ header hamburger | Rule desktop `display:none` bị rule base phía sau ghi đè do cùng specificity | Shared shell + CSS cascade | Khóa desktop header bằng `!important` cho display/size/padding/border; giữ grid row `0 1fr` | Header/sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Collapsed line-only active state | Collapsed menu active chỉ giữ vạch xanh và icon; nền trung tính chỉ xuất hiện khi hover hoặc keyboard focus | Owner phát hiện active background cố định nhìn giống item đang bị hover | Shared shell + accessibility | Override wrapper active collapsed về transparent; hover/focus-within dùng shared neutral surface | Sidebar collapsed trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Full-width submenu interaction surface | Hover/active background của menu con phủ hết chiều ngang sidebar, không còn bị cắt ở mép trái; icon, chữ và vạch xanh vẫn giữ child indent | Owner yêu cầu hàng con như My Orders có interaction surface liền mạch theo reference Apple Music | Shared shell + RadzenPanelMenu | Đưa indent từ margin của wrapper sang padding nội dung và giữ active rail ở vị trí child indent | Sidebar expanded trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Neutral collapsed brand idle state | Logo collapsed không có nền khi idle; surface chỉ xuất hiện khi hover hoặc keyboard focus rồi chuyển sang icon expand | Owner phát hiện nền mặc định của button khiến logo trông như đang bị hover | Shared shell + accessibility | Reset background của brand button về transparent, giữ nguyên hover/focus và logo-to-expand transition | Sidebar collapsed trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Expanded-only account divider | Đường phân cách phía trên user footer chỉ hiển thị khi sidebar expanded; collapsed rail giữ nền liền mạch | Owner yêu cầu bỏ đường ngang thừa phía trên avatar khi collapse | Shared shell | Override border-top của user footer về `0` trong collapsed state, giữ rule expanded hiện tại | Sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Fuller sidebar interaction surfaces | Hover/focus surface của menu cha và menu con rõ, cao và rộng hơn; collapsed rail dùng gần trọn bề rộng 68px thay vì khối 44px | Owner muốn hiệu ứng đầy đặn như reference Apple Music ở cả expanded và collapsed | Shared shell + RadzenPanelMenu + accessibility | Tăng contrast token, chuẩn hóa row 44px, giảm inset expanded và mở collapsed surface thành 64x48px; active rail vẫn độc lập | Sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Apple Music sidebar type and geometry scale | Đồng bộ menu cha/con ở 14px, line-height 20px, weight 400; icon box 24px với glyph 20px; expanded 286px và collapsed 72px | Owner phát hiện hierarchy cũ dùng 14px/13px và icon 24px/16px nên nhìn to nhỏ không đều | Shared shell + RadzenPanelMenu | Đo live Apple Music Web: system/SF Pro stack, 14px text, 20px line-height, 24px icon box, 36px web row; chọn row 40px và width 286px theo Apple Music app reference để đủ nhãn tiếng Việt, dùng Segoe UI fallback hợp pháp trên Windows | Sidebar trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Parent-child surface edge parity | Hover/active surface của menu cha và menu con dùng cùng inset 4px và cùng radius; chỉ icon/chữ của child thụt cấp | Owner phát hiện child surface bắt đầu lệch vào trong so với parent | Shared shell + RadzenPanelMenu | Override cả Radzen level-2 margin token và submenu wrapper về cùng `space-1`; giữ content/active rail indent riêng | Sidebar expanded trên mọi authenticated route | Superseded — legacy margin remained |
| 2026-07-22 | Legacy submenu offset removal | Xóa `margin-left: 1rem !important` cũ trên toàn bộ submenu item để child surface thực sự cùng mép parent | Browser evidence cho thấy legacy `app.css` tiếp tục đẩy cả `<li>` child 16px dù wrapper/token đã đồng bộ | Shared shell + CSS cascade | Loại bỏ rule legacy tại nguồn; giữ indent bằng padding nội dung trong `vpp-sidebar.css` | Sidebar expanded trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Uniform Apple Music row gap | Parent–child và child–child đều giữ khoảng cách thị giác 4px giữa hai interaction surface | Owner phát hiện parent–first-child còn rộng hơn khoảng cách giữa các child | Shared shell + RadzenPanelMenu | Đo Apple Music Web live: row surface 36px, margin-bottom 4px; reset `--rz-panel-menu-2nd-level-vertical-offset` về 0 để Radzen không cộng khoảng đệm riêng trước/sau submenu | Sidebar expanded trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Unified collapsed control hitbox | Nav icon, user trigger và logo-to-expand trigger dùng chung surface 68×48px trên collapsed rail 72px; glyph/avatar/logo giữ nguyên kích thước | Owner phát hiện user hover và expand hover vẫn là ô 44×44px nhỏ hơn nav hover | Shared shell + accessibility | Tạo shared collapsed-control width/height tokens; giảm header/footer inline padding còn 2px và áp cùng token cho ba loại trigger | Sidebar collapsed trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Identical expanded row geometry | Mọi parent/child wrapper và link dùng cùng height 40px, padding-block 0, font/icon scale và hover surface; hierarchy chỉ thể hiện bằng content indent | Owner phát hiện parent row còn cao khoảng 52px trong khi child row 40px | Shared shell + RadzenPanelMenu | Override cả level-1/level-2 Radzen padding token và khóa wrapper/link height 40px; collapsed vẫn dùng shared 68×48px hitbox | Sidebar expanded trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Collapsed pointer-focus cleanup | Mọi icon collapsed ở trạng thái idle/route-active đều không có nền; surface chỉ hiện khi con trỏ đang hover hoặc focus bàn phím thực sự hiển thị | Owner phát hiện các mục từng click (Dashboard, Reports) giữ nền như hover vì `focus-within` còn tồn tại sau pointer focus | Shared shell + accessibility | Thay `focus-within` bằng `focus-visible`; thêm lớp reset collapsed khi không hover và không có keyboard-visible focus | Sidebar collapsed trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | User row interaction parity | User trigger expanded dùng cùng surface edge, chiều cao 40px, radius, typography và hover/focus/active treatment như menu cha/con | Owner yêu cầu hàng user dưới footer không còn cao và hẹp hơn các navigation row phía trên | Shared shell + account menu | Giảm footer inset về 4px, khóa trigger 40px và 14/20/400; cập nhật điểm neo popover theo chiều cao footer mới | Sidebar expanded trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Deterministic expanded row spacing | Parent–child và child–child luôn cách nhau đúng 4px, kể cả khi Radzen theme ghi đè margin wrapper | Owner phát hiện hai child surface liên tiếp vẫn dính sát dù token submenu offset đã về 0 | Shared shell + RadzenPanelMenu CSS cascade | Chuyển vertical rhythm sang `margin-block-start` tại cấp navigation item; wrapper chỉ giữ horizontal inset và không còn vertical margin phụ thuộc theme | Sidebar expanded trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Sidebar-parity tabs | Mọi Radzen tab dùng cùng row 40px, font 14/20/400, radius và neutral hover/active surface như sidebar; active indicator chuyển thành vạch xanh nằm dưới với inset 8px | Owner yêu cầu tab ở mọi trang đồng bộ hoàn toàn với sidebar, chỉ khác hướng của active line | Shared shell + all RadzenTabs | Tạo shared navigation tokens, áp cho primary/secondary/inspector tabs; tab bar cao 44px và giữ horizontal scroll/keyboard behavior | Dashboard, Library, Permission, nested pricing/management/component tabs, record inspector | Implemented — OWNER_REVIEW |
| 2026-07-22 | Tab-first page composition | Bỏ title/description lặp phía trên tab ở Library và Permission để tab trở thành navigation đầu trang như Dashboard | Owner xem heading “Quản trị danh mục” và mô tả là chữ thừa trước tab | Library + Permission | Xóa `VppPageHeader` khỏi hai page wrapper; giữ `PageTitle`, permission checks và tab content | `/library`, `/permission` | Implemented — OWNER_REVIEW |
| 2026-07-22 | Apple Music account popover parity | Account popover dùng cùng font, 40px action row, icon 20px/box 24px, gap 4px và neutral hover/active surface như sidebar; department context dùng active surface + rail xanh | Owner yêu cầu popup user tiếp tục bám Apple Music và đồng bộ hoàn toàn với sidebar | Shared shell + account menu | Giảm popup inset còn 4px; bỏ blue card/icon tile; logout trở về neutral menu action; giữ identity/context hierarchy và keyboard focus | Sidebar account popover trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Apple Music-inspired app mark | Dùng một rounded-square app mark có gradient xanh–teal và glyph V trắng ở cả expanded/collapsed sidebar cùng favicon | Owner yêu cầu học cách Apple Music dùng logo và áp logo đó cho shell; không thay brand GTAS VPP bằng trademark Apple | Shared shell + browser identity | Tạo SVG nội bộ sắc nét mọi DPI; expanded hiện icon + tên, collapsed giữ logo-to-expand hover; bỏ inline SVG nhiều lớp cũ | Sidebar và favicon trên mọi route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Symmetric sidebar optical spacing | Giữ tổng khoảng cách 4px nhưng chia đều 2px phía trên và 2px phía dưới mỗi interaction surface | Owner nhận thấy khoảng trống trên/dưới chưa cân bằng bằng mắt dù khoảng cách số học đã là 4px | Shared shell + RadzenPanelMenu CSS cascade | Chuyển spacing một phía trên navigation item thành half-gap đối xứng trên wrapper; giữ nguyên row 40px, inset và expand/collapse behavior | Sidebar expanded trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Sidebar-matched tab header surface | Nền toàn bộ tab header dùng cùng navigation chrome surface với sidebar, thay vì lớp elevated trắng nổi hơn page | Owner nhận thấy dải tab header sáng và tách khỏi sidebar/page hierarchy | Shared shell + all RadzenTabs | Tạo token theme-aware dùng chung: light bám `bg-base`, dark bám `bg-surface`; áp cho primary/secondary tab bar, admin tab container và scroll fade | Dashboard, Library, Permission và mọi nested tab | Implemented — OWNER_REVIEW |
| 2026-07-22 | Depth-aware sidebar indentation | Cấp cha, con và cháu có cột nội dung phân cấp rõ như Apple Music; mỗi cấp sâu thêm lùi một nhịp nhưng interaction surface vẫn full-width | Owner phát hiện submenu cấp ba như `Bảng giá → Giá` đang cùng cột với cấp hai | Shared shell + RadzenPanelMenu nested DOM | Giữ level-2 indent 32px; level-3 thêm 24px cho icon/text và active rail bằng selector structural hai lớp `.rz-navigation-menu`; không thay hover size/row height | Sidebar expanded trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Apple Music shared tab indicator motion | Khi đổi tab, underline kéo giãn từ vị trí cũ tới vị trí mới rồi co lại tại tab đích, thay vì scale-in riêng lẻ | Owner cung cấp video Apple Music và yêu cầu line/effect chuyển tab tương đương | Shared shell + all RadzenTabs + accessibility | Dùng một indicator DOM dùng chung, đo geometry theo FLIP, animate hai pha trong 180ms với easing `cubic-bezier(0.32, 0.72, 0, 1)`; hỗ trợ click, keyboard, nested tabs, resize/scroll và `prefers-reduced-motion` | Dashboard, Library, Permission và mọi nested tab | Implemented — OWNER_REVIEW |
| 2026-07-22 | Apple-inspired account profile hero | Account popover dùng avatar lớn, identity căn giữa, organization pill, quick theme control góc phải và action list chia nhóm như Apple account menu | Owner muốn thiết kế phần user giống Apple nhưng nội dung được điều chỉnh theo GTAS VPP | Shared shell + account menu + accessibility | Giữ popup 278px theo sidebar; dùng initials gradient thay ảnh chưa có nguồn, phòng ban thay View Profile, theme ở nút góc, bên dưới giữ language/notifications/divider/logout; không tạo route hoặc action giả | Sidebar account popover trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Tab indicator scope and full sidebar surface fix | Shared underline chỉ chạy trong RadzenTabs, tab bar phẳng không viền bốn cạnh; sidebar active/hover dùng một màu xuyên qua rail tới mép trái | Owner phát hiện line xanh xuất hiện dưới DataGrid pager và submenu surface bị chia hai màu tại active rail | Shared shell + RadzenTabs + RadzenPanelMenu | Bỏ selector ARIA `ul[role=tablist]` quá rộng, scope indicator dưới `.rz-tabview`, reset tab container về border-bottom/radius 0; ép inner navigation link trong suốt để wrapper sở hữu full-row surface | Mọi tab, DataGrid pager và sidebar expanded | Implemented — OWNER_REVIEW |
| 2026-07-22 | Full-bleed primary tab chrome | Nền navigation chrome và divider của tab cấp trang chạm đủ mép trên/trái/phải của content viewport; panel, card và grid vẫn giữ content inset | Owner phát hiện dải tab còn bị padding của `RadzenBody` cắt thành các góc trắng và CSS admin tô container trùng với CSS tab | Shared layout + primary RadzenTabs | Dùng một token inset responsive do layout sở hữu; primary nav bù âm inset và hoàn lại khoảng dọc cho panel; bỏ rule admin trùng border/background/padding | Dashboard, Library, Permission | Implemented — OWNER_REVIEW |
| 2026-07-22 | Centered account department identity | Phòng ban trở thành metadata căn giữa ngay dưới email, hiển thị tên đầy đủ và code bằng dấu phân cách nhẹ; bỏ icon, nhãn và nền xám | Owner muốn profile gọn như Apple account menu và phát hiện label theme rơi về key tiếng Anh `Appearance` trong UI tiếng Việt | Shared account menu + localization | Dùng key `Theme/Giao diện`; render `DepartmentName · DepartmentCode` trong identity hero, nền trong suốt và chỉ hiện code khi khác tên | Sidebar account popover trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Nested rail-to-icon rhythm correction | Icon của item con và cháu dùng cùng một khoảng cách ngắn sau active rail, không bị padding mặc định của Radzen cộng thêm | Owner thấy line đã lùi đúng nhưng icon con/cháu vẫn trôi sang phải, rõ nhất ở Dashboard và Pricing > Price lists | Shared sidebar + RadzenPanelMenu hierarchy | Wrapper tiếp tục sở hữu toàn bộ hierarchy indent; reset inline-start padding của nested link về 0 và giữ 16px ở mép phải cho chevron | Sidebar expanded, menu level 2 và level 3 | Implemented — OWNER_REVIEW |
| 2026-07-22 | Direct-nav tab indicator containment | Shared underline luôn nằm trong chính tab header, kể cả Radzen render `.rz-tabview-nav` trực tiếp không có nav container; không còn line xanh rơi xuống đáy trang | Owner phát hiện duplicate underline ở đáy Dashboard khi account popup mở | Shared RadzenTabs + tab motion | Fallback indicator host từ toàn bộ tab root sang chính nav list; bù `scrollLeft` khi host là vùng cuộn để geometry không lệch | Dashboard, Library, Permission, nested tabs | Implemented — OWNER_REVIEW |
| 2026-07-22 | Stacked department code | Department code hiển thị thành dòng nhỏ riêng dưới tên phòng ban, cùng trục giữa với avatar/name/email | Owner yêu cầu `IT` nằm dưới `Công nghệ thông tin`, không đặt cùng hàng | Shared account menu | Chuyển department metadata sang grid một cột, bỏ dấu chấm phân cách ngang | Sidebar account popover trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Nested icon and pressed-surface recovery | Khôi phục toàn bộ icon con/cháu sau khi rút ngắn rail gap; pressed/active surface phủ đều cả hàng và không bị chia tại line xanh | Owner phát hiện icon submenu biến mất và hiệu ứng click vẫn dừng ở active rail | Shared sidebar + RadzenPanelMenu | Reset negative 2nd-level icon margin của Radzen thay vì phụ thuộc padding link; tắt native active indicator và inner pressed pseudo-layer, để wrapper + custom rail sở hữu toàn bộ trạng thái | Sidebar expanded, menu level 2 và level 3 | Implemented — OWNER_REVIEW |
| 2026-07-22 | Uniform sidebar label contrast | Chữ menu cha/con/cháu giữ cùng độ tương phản ở idle, hover, focus và active; không còn cảm giác disabled khi chưa hover | Owner phát hiện submenu idle nhạt hơn rõ rệt rồi đậm lên khi hover | Shared sidebar + RadzenPanelMenu typography | Ghi đè màu nested-item mặc định của Radzen tại label bằng `--vpp-text-primary` và opacity 1 cho mọi interaction state | Sidebar expanded trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Inline department code badge | Tên phòng ban và mã phòng ban nằm cùng một hàng, căn giữa; mã ngắn hiển thị như metadata badge thay vì một dòng rời | Owner muốn đưa `IT` trở lại cạnh `Công nghệ thông tin` nhưng vẫn cần bố cục gọn và đẹp | Shared account popover | Dùng inline-flex cho department metadata; code dùng capsule nhỏ, nền neutral và border nhẹ để phân biệt với tên mà không giống action button | Sidebar account popover trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Full-row press expansion recovery | Khi nhấn menu, lớp phản hồi nở từ tâm ra hai mép của toàn bộ hàng, không dừng tại active rail; hoạt động ở cả expanded và collapsed | Owner yêu cầu khôi phục hiệu ứng giãn từ trong ra ngoài đã mất sau khi tắt ripple lệch của Radzen | Shared sidebar + RadzenPanelMenu interaction | Giữ ripple link gốc bị tắt; dựng radial press surface trên wrapper full-width và kích hoạt bằng delegated pointer/keyboard event để animation chạy trọn 360ms; đặt dưới content/custom rail và có reduced-motion fallback | Sidebar expanded/collapsed trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Shell and tab press parity + panel divider fix | Logo/collapse control, account trigger và header tab dùng cùng press expansion 360ms như sidebar; tab content không còn đường viền Radzen dư bên dưới header | Owner yêu cầu đồng bộ hiệu ứng cho logo, user, tab và chỉ ra header-tab vẫn lỗi | Shared shell controls + RadzenTabs | Mở rộng delegated press selector sang brand/toggle/user/tab; tab dùng `::before` để không xung đột shared underline `::after`; reset border/shadow/background mặc định của `.rz-tabview-panels` trên shared tab shells | Sidebar expanded/collapsed, primary/secondary tabs trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Department badge optical alignment | Badge mã phòng ban có cùng chiều cao 18px với dòng tên, căn giữa quang học và dùng màu xanh thương hiệu | Owner thấy `Công nghệ thông tin` và `IT` còn lệch, đồng thời muốn badge mang màu project | Shared account popover | Chuyển badge sang border-box 24x18 tối thiểu; dùng primary-600, nền primary 8% và border primary 22% | Sidebar account popover trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Shell icon scale parity | Logo shell và avatar account trigger dùng cùng khung 24px với icon sidebar; popup profile vẫn giữ avatar lớn để bảo toàn phân cấp | Owner muốn thu nhỏ user icon và logo theo Apple Music, đồng bộ toàn project với icon điều hướng | Shared brand mark + user trigger + accessibility | Đưa `VppBrandMark` và sidebar overrides về 24px; avatar trigger 24px/10px; chuyển touch-target 44px từ avatar sang button để kích thước nhìn không bị phình trên thiết bị cảm ứng | Sidebar expanded/collapsed, account shell và user trigger | Implemented — OWNER_REVIEW |
| 2026-07-22 | Collapse press containment | Radial press của logo/collapse/user/tab luôn bị cắt trong chính control, không tạo mảng oval tràn sang page khi sidebar đổi width và rerender | Video owner cho thấy lúc thu gọn sidebar có vùng radial rất lớn phình qua content | Shared shell press surfaces | Bổ sung positioned containing block (`position: relative`) trước isolation/overflow cho mọi shared press target | Sidebar expand/collapse, account trigger và header tabs | Implemented — OWNER_REVIEW |
| 2026-07-22 | Primary tab vertical header alignment | Primary header-tab dùng khung 60px như sidebar header và căn item 40px chính giữa; chữ/hover không còn dồn sát mép trên | Owner làm rõ lỗi cần sửa là vị trí header-tab bị kéo lên cao khi sidebar collapsed | Shared primary RadzenTabs | Đổi primary tab height từ 44px sang `--vpp-header-height` 60px; center nav items bằng padding động `(60 - 40) / 2`; secondary tabs tiếp tục 44px | Dashboard, Library, Permission primary tabs | Implemented — OWNER_REVIEW |
| 2026-07-22 | Department text baseline alignment | Chữ tên phòng ban và chữ trong badge code dùng chung baseline thị giác, không căn theo tâm hai hộp | Owner làm rõ cần `Công nghệ thông tin` ngang chữ `IT`, không phải căn giữa tên với badge | Shared account popover | Parent inline-flex chuyển sang `align-items: baseline`; code badge dùng inline-block/vertical-align baseline và line box 16px trong khung 18px | Sidebar account popover trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Direct-nav primary tab correction | RadzenTabs render nav trực tiếp vẫn giữ header 60px, item 40px căn giữa và chỉ có một shared underline | Owner gửi ảnh cho thấy tab vẫn dồn trên và có hai line xanh sau lượt căn header trước | Shared primary RadzenTabs direct/container DOM variants | Không áp `height:100%` lên direct nav; chỉ child nav trong container dùng 100%. Reset margin/border/background native của `li.rz-tabview-selected` để bỏ underline thứ hai và offset -1px | Dashboard, Library, Permission primary tabs | Implemented — OWNER_REVIEW |
| 2026-07-22 | Primary tab chrome height lock | Đáy primary tab chrome ngang đáy sidebar header ở 60px trên runtime thật, không co còn khoảng 50px do cascade/theme | Owner vẫn thấy tổng thể tab header thấp hơn khối logo dù item/direct-nav đã được chuẩn hóa | Shared primary RadzenTabs runtime cascade | Khai báo height token trực tiếp trên `.vpp-admin-tabs`; khóa height/min/max-height bằng `!important` cho shell và child nav-container | Dashboard, Library, Permission primary tabs | Implemented — OWNER_REVIEW |
| 2026-07-22 | Runtime primary tab geometry normalization | Mỗi lần Radzen render/rerender, tab host thực tế được ép 60px và row 40px được căn giữa, kể cả DOM/cascade không khớp selector CSS | Screenshot owner sau CSS height lock vẫn cho thấy line ở khoảng 50px và chữ cao hơn logo | Shared tab interaction runtime | `vpp-interactions.js` đọc header/row token, đặt inline important height/min/max và padding-block lên host/tab list; ResizeObserver tái áp dụng trước khi đo indicator | Dashboard, Library, Permission primary tabs | Implemented — OWNER_REVIEW |
| 2026-07-22 | Parent-icon-aligned submenu rail | Active rail của item con nằm đúng tại mép phải icon cha; icon/text con bắt đầu sau rail một khoảng 12px như Apple Music | Owner chỉ rõ rail trong reference không trùng cột icon con mà neo theo cạnh ngoài icon cha | Shared shell + RadzenPanelMenu hierarchy | Tính rail cấp hai từ wrapper inset 4px + parent content inset 16px + icon box 24px = 44px; content con ở 56px; mỗi cấp sâu hơn cộng icon box + gap = 36px | Sidebar expanded, submenu level 2 và level 3 | Implemented — OWNER_REVIEW |
| 2026-07-22 | Final account information order | Account popover hiển thị avatar, full name, email thật, phòng ban localized, rồi language, appearance, notifications và danger logout | Owner chốt thứ tự nội dung, yêu cầu bỏ theme icon góc phải, bỏ department code song ngữ và làm logout nổi bật đỏ | Shared shell + account menu + localization | Truyền `AuthenticationResultDTO.Email`; department dùng label `Department` theo VI/EN và không hiện code; thêm `Appearance/Giao diện`, đưa theme về action row với trạng thái hiện tại; logout đỏ idle và danger surface khi hover/focus | Sidebar account popover trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Single primary tab baseline and scoped focus | Header-tab chỉ có một underline active nằm đúng trên divider; pointer focus không tạo line xanh toàn chiều rộng, keyboard focus vẫn hiện trong hitbox tab | Owner phát hiện tab header tiếp tục có hai line xanh và mất cân bằng thị giác so với sidebar header | Shared RadzenTabs + accessibility | Tắt outline trên toàn tablist của Radzen, chuyển focus-visible vào selected tab link; đổi divider primary thành inset line không chiếm layout để nav đủ 60px, row 40px cân đúng 10px trên/dưới và shared indicator phủ cùng baseline | Dashboard, Library, Permission và mọi nested tab | Implemented — OWNER_REVIEW |
| 2026-07-22 | Apple-style sidebar reveal motion | Expand/collapse sidebar dùng một chuyển động liền mạch như account popover: width chạy easing mềm, chrome/label fade-scale-translate và icon gốc không nhảy cột | Owner yêu cầu hiệu ứng đóng/mở sidebar giống Apple và đồng bộ cảm giác với popup user | Shared shell + RadzenPanelMenu + responsive | Giữ DOM header brand ổn định; dùng motion token 220ms `cubic-bezier(0.32, 0.72, 0, 1)` và cross-fade expanded/collapsed chrome; RadzenPanelMenu vẫn chuyển về mode `Icon` thật khi collapsed để bảo toàn geometry nội bộ | Sidebar desktop/tablet/mobile trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Apple-aligned expanded content rail | Toàn bộ logo, label, icon cha/con/cháu, active rail và account identity của sidebar expanded dịch trái cùng 8px; gap icon–text và hierarchy depth giữ nguyên | Owner đối chiếu ảnh Apple Music và thấy content rail hiện tại nằm sâu về phải | Shared sidebar geometry | Giảm primary content offset từ 24px xuống 16px; mọi child rail/content offset tiếp tục dẫn xuất từ token chung nên không phát sinh lệch riêng từng cấp; collapsed control vẫn căn giữa | Sidebar expanded trên mọi authenticated route | Implemented — OWNER_REVIEW |
| 2026-07-22 | Unified 72px logo-tab header | Logo/collapse cell và primary tab chrome nằm cùng một hàng 72px; tab bar phủ từ mép sidebar tới hết viewport, underline chỉ rộng đúng phần chữ và cùng baseline với đáy sidebar header | Owner làm rõ header-tab trước đó bị hiểu sai và chỉ ra navigation icon bị trôi khi collapsed | Shared shell + primary RadzenTabs + indicator runtime | Đặt `header-height` bằng collapsed sidebar width 72px; trả RadzenPanelMenu về `Icon` thật khi collapsed; indicator đo content box bằng padding trái/phải thực tế của tab thay vì inset 8px cố định | Dashboard, Library, Permission và sidebar collapsed | Implemented — OWNER_REVIEW |
| 2026-07-22 | Centered collapsed rail and balanced primary-tab hitbox | Icon collapse nằm cùng một trục giữa 72px với logo/avatar; menu đầu tiên bắt đầu ngay sau header; label tab có khoảng đệm trên/dưới và trái/phải cân đối | Owner đối chiếu Apple Music và phát hiện PanelMenu icon-only co chiều rộng làm icon trôi trái, khoảng GTAS VPP–Dashboard quá lớn và label tab lệch lên/trái | Shared sidebar + primary RadzenTabs | Ép nav/PanelMenu/root item collapse rộng 100%; bỏ 12px top padding của nav; dùng hitbox tab 48px và inline padding 20px, đồng bộ JS geometry bằng token cục bộ | Dashboard, Library, Permission và mọi authenticated shell | Implemented — OWNER_REVIEW |
| 2026-07-22 | Radzen 11 DOM-correct collapsed rail and tab controls | Menu cha/lá collapse cùng một cột giữa; tab text/hitbox căn đúng trong header 72px bằng chính button runtime | Owner xác định regression bắt đầu từ Apple-style reveal; audit cho thấy CSS animation cũ chỉ ẩn chevron nhưng vẫn để `margin-inline-start:auto`, đồng thời tab CSS target nhầm `.rz-tabview-nav-link` thay vì `button[role=tab]` của Radzen 11.1.4 | Shared RadzenPanelMenu + RadzenTabs | Collapse loại chevron khỏi layout bằng `display:none`; bỏ transition label/arrow không còn hợp lệ khi dùng `DisplayStyle.Icon`; target trực tiếp button tab thật và để CSS sở hữu geometry, JS chỉ sở hữu shared indicator | Dashboard, Library, Permission và mọi authenticated shell | Implemented — OWNER_REVIEW |
| 2026-07-22 | Uniform brand-to-navigation rhythm and line-only primary selection | Khoảng logo đến icon đầu bằng đúng khoảng giữa các navigation row trong từng trạng thái; primary tab text căn theo toàn header 72px và selected không còn nền xám | Owner chỉ ra logo–icon đầu vẫn xa hơn icon–icon ở cả collapse/expand và hitbox 48px của tab làm label trông như bị căn trong một lớp con | Shared sidebar geometry + primary RadzenTabs | Bỏ divider 1px Radzen để row step còn 44px expanded/52px collapsed; dùng phần trống dưới brand header để nối nhịp 4px; primary button chiếm trọn header, giữ underline theo text và chỉ hiện neutral surface khi hover/focus | Dashboard, Library, Permission và mọi authenticated shell | Implemented — OWNER_REVIEW |
| 2026-07-22 | Ink-bound optical centring for shell labels | Primary tab và project label được căn theo tâm phần nét chữ thật ở cả trục dọc/ngang, không dựa vào baseline hoặc line-box | Owner làm rõ ví dụ glyph `8`: tâm phải nằm giữa hai vòng nét, đồng thời side-bearing trái/phải cũng phải được bù theo thị giác; offset cứng 2px bị từ chối | Shared shell typography + primary RadzenTabs indicator | Thử dùng Canvas `TextMetrics.actualBoundingBox*`, nhưng kết quả không khớp text shaping/fallback font của DOM thật và làm VI/EN lệch thị giác hơn | Dashboard, Library, Permission và project label trên mọi authenticated shell | Rejected — replaced by native centring in 1.95 |
| 2026-07-22 | Symmetric shell-header breathing room and shared 44px nav rhythm | Header có khoảng trống thị giác bằng nhau phía trên và phía dưới logo/tab; từ icon đầu trở đi mọi root navigation dùng cùng nhịp 44px ở cả expand/collapse | Owner làm rõ khoảng logo–icon đầu không phải navigation step 44px: logo/tab là một header row riêng, được bao bởi hai khoảng trống bằng nhau; chỉ icon 1–4 dùng nhịp 44px | Shared sidebar header + RadzenPanelMenu root rows + primary tabs | Căn logo 24px vào tâm header 72px; đặt icon đầu bắt đầu tại y=72 để hai khoảng trống đều 24px; collapse dùng navigation surface 40px thay vì control 48px, giữ logo/user control riêng; project label và tab dùng native flex/line-box centring | Mọi authenticated shell ở expand/collapse | Implemented — OWNER_REVIEW |
| 2026-07-22 | Shared directional navigation indicators | Active line là một indicator dùng chung: tab trượt thẳng trái/phải, sidebar trượt thẳng lên/xuống; không bung từ tâm, không kéo dài qua các item trung gian | Owner yêu cầu icon 1 → icon 4 phải là một chuyển động trực tiếp, không tạo cảm giác đi lần lượt 1 → 2 → 3 → 4; line của active child phải biến mất khi nhánh cha đóng | Shared RadzenTabs + RadzenPanelMenu interaction runtime | Indicator dùng direct translate 180ms từ geometry hiện tại tới đích; pointer target được ưu tiên trong lúc Radzen cập nhật class; active child bị ẩn trả về `null` và indicator bỏ `is-ready`, mở nhánh lại mới hiện đúng vị trí; primary tab giữ common width `min(60px, shortest label box)` | Mọi authenticated shell, primary header-tabs và sidebar expand/collapse | Implemented — OWNER_REVIEW |
| 2026-07-22 | Native text centring and observer performance repair | Header labels trở về native flex/line-box centring; interaction runtime không còn tự tạo DOM probe trong vùng đang được observer theo dõi | Owner thấy Canvas optical offsets lệch hơn bản đầu và web lag rõ sau shared indicator change | Shared shell typography + navigation interaction runtime | Bỏ toàn bộ Canvas glyph measurement/inline translate; dùng hitbox 72px + line-height 20px; đổi `px/rem/em` bằng phép tính từ computed font size, lọc mutation do indicator tự tạo và bỏ global character-data observation | Mọi authenticated shell, VI/EN, sidebar expand/collapse | Implemented — OWNER_REVIEW |
| 2026-07-22 | Font-metric text trim and clipped-branch indicator repair | Header-tab căn theo cap-height/baseline thật của font; sidebar line biến mất kể cả khi Radzen chỉ co/clip submenu thay vì `display:none` ngay lập tức | Owner vẫn thấy label cao hơn tâm header và line active bị rơi xuống khoảng trống sau khi đóng nhóm cha | Primary RadzenTabs typography + PanelMenu visibility runtime | Progressive enhancement `text-box: trim-both cap alphabetic` trên Chrome 133+/Safari 18.2+; visibility tính giao của target qua mọi ancestor clip; observer theo dõi thêm `aria-expanded/style`, đồng bộ lại sau transition và sau 240ms settle | Mọi authenticated shell, VI/EN, sidebar nested branches | Implemented — OWNER_REVIEW |
| 2026-07-22 | Route-real shell regression and full-bleed inset reconciliation | Primary tab header bắt đầu đúng y=0, cao 72px và visible text centre ở y=36; active-child line được đóng/mở 3 lần trên Radzen DOM thật | Playwright route thật chứng minh title đã centre trong nav nhưng nav bị kéo lên -10.5px; legacy `.rz-body` dùng 0.5rem `!important` trong khi full-bleed bù 1.25rem; restart sạch còn lộ Radzen layout gap 1.5rem đẩy body xuống 21px | Shared layout/tab shell + authenticated Playwright regression | Dùng chung `--rz-layout-body-padding-1` cho body inset/full-bleed compensation, khóa shell grid `gap: 0 !important`; thêm E2E đăng nhập fixture, click chính root wrapper, kiểm tra indicator opacity/class và geometry header tuyệt đối | `/library?tab=0`, `/dashboard?tab=0`, desktop 1366x768 | Implemented — VERIFIED |
| 2026-07-22 | Stable local watch loop | Backend file logs không còn kích hoạt project reload của `dotnet watch` | Route-real QA cho thấy Serilog cập nhật `logs/*.txt` làm watch liên tục `Loading projects` dù không có source change, gây CPU/I/O nền và cảm giác dev server lag | .NET 10 AppHost project graph | Theo Microsoft Learn, thêm `**/logs/**` vào `DefaultItemExcludes` của backend web project; logging vẫn hoạt động nhưng log không thuộc default watch/build items | Local Aspire + dotnet watch | Implemented — VERIFIED |
| 2026-07-22 | Active rail follows sibling expansion | Line active bám liên tục item con khi một nhóm nằm phía trên đóng/mở; không teleport ở cuối transition và không chạy tuần tự qua item trung gian | Video 60 FPS của owner cho thấy active `Nhóm hàng` đổi vị trí theo `Bảng điều khiển`, nhưng runtime bỏ qua geometry mới vì target DOM vẫn là cùng element | Shared PanelMenu indicator runtime + authenticated E2E | Khi `aria-expanded` đổi, theo dõi geometry đúng 240ms bằng `requestAnimationFrame`, đồng bộ trực tiếp với grid transition 200ms của Radzen rồi tự dừng; E2E lấy 16 mẫu/frame cho cả hai chiều và xác nhận brand/logo/tab cùng tâm 36px | Sidebar expanded trên authenticated routes, desktop | Implemented — VERIFIED |
| 2026-07-22 | Unified Apple-style navigation timing | Mở/đóng nhóm menu, line sidebar và underline header dùng cùng nhịp thời gian; không còn menu tới trước line hoặc line tới trước nội dung | Owner yêu cầu thời gian đóng/mở icon 1 và di chuyển line đồng bộ ở cả sidebar/header | Shared navigation motion tokens + Radzen expander + indicator runtime | Chọn 200ms theo transition gốc Radzen và Apple HIG về feedback ngắn, chính xác; dùng chung easing `cubic-bezier(0.32, 0.72, 0, 1)`, JS đọc trực tiếp CSS token và giữ reduced-motion fallback | PanelMenu nested expansion, sidebar active rail, primary/secondary tab underline | Implemented — VERIFIED |
| 2026-07-22 | Recursive Apple-style sidebar columns | Rìa trái chữ cha trùng icon con, chữ con trùng icon cháu; line nằm ngay trước icon cùng cấp; logo/nav/avatar collapsed chung tâm | Owner đối chiếu Apple Music và chỉ ra child/grandchild đang lệch phải 3.5px, avatar lệch trái do hidden-label gap | Shared PanelMenu geometry + collapsed shell controls | Dùng nhịp icon–text/rail 12px dẫn xuất đệ quy qua từng cấp; bỏ gap của hidden user label và tính brand inset theo chiều rộng rail thay vì số cứng | Sidebar expanded/collapsed trên authenticated routes | Implemented — VERIFIED |
| 2026-07-22 | Apple-close expanded content rail | Logo, root navigation icon và avatar expanded nằm gần mép sidebar như Apple Music trong khi chiều rộng shell không đổi | Owner chỉ ra content rail VPP vẫn thụt sâu hơn reference dù quan hệ cha–con đã đúng | Shared expanded sidebar geometry | Giảm primary content offset từ 16px xuống 8px; mọi child/grandchild rail tiếp tục dẫn xuất từ token chung, collapsed rail giữ nguyên tâm | Sidebar expanded trên authenticated routes | Implemented — VERIFIED |
| 2026-07-20 | React POC vertical slice | Login, protected shell, My Orders và logout dùng generated OpenAPI client; isolated fixture chỉ phục vụ QA | Cần so sánh React với Blazor trên cùng API/TEST mà không copy DTO hoặc thay nghiệp vụ | React preview | Owner chạy Aspire + tài khoản TEST để duyệt runtime; chưa cutover và chưa mở route tiếp theo | `/login`, `/app/orders` | Owner review — TEST runtime pending |
| 2026-07-19 | Account menu | Department chỉ hiện một lần; logout neutral mặc định, danger khi tương tác | Loại bỏ thông tin lặp và mảng cảnh báo quá nặng trong menu | Shared shell | Hoàn tất W0.2 user-menu polish | Mọi authenticated route | Verified |
| 2026-07-19 | Header controls | Notification bell dùng chung visual primitive với EN/VI và theme control | Trigger cũ dùng legacy tokens nên viền, nền và hover lệch khỏi header system | Shared shell | Đồng bộ CSS token + browser geometry/hover regression | Mọi authenticated route | Verified |
| 2026-07-19 | W1 demo data | Dùng workbook thật qua normalized TSV; map tháng nguồn thành rolling 12 tháng và bind đơn theo user/phòng ban | W1 cần normal/history state thực tế, seed cũ chỉ có catalog và không có đơn | TEST/DEMO fixture | Thay `MigrateAndDemo` bằng catalog/department/user/order fixture idempotent | Dashboard/History/Report/Library | Verified |
| 2026-07-19 | Account errors | Dịch theo stable error code, không render raw backend message | Bảo mật, VI/EN nhất quán và tránh technical leakage | Global | Thêm `AccountLifecycleUiMapper` | Register/Forgot/Reset/Confirm/Change | Verified |
| 2026-07-19 | Empty/data story | Không lặp CTA/status; phân biệt từng empty context | Dashboard và history hiện có vùng trắng/copy gây hiểu sai | Global | Bổ sung 6.2 và state enum rule | Dashboard/History/Period | Proposed |
| 2026-07-19 | Admin grid | Column profile theo route + detail on demand | Departments screenshot cho thấy nhiều cột và khó đọc | Shared pattern | Bổ sung 6.1 và W3 | Library/*, shared grid | Proposed |
| 2026-07-19 | Error feedback | Không để raw `OperationInvalid` lên toast | Caller dùng trực tiếp `ApiRequestException.Message`, chính là backend error code | Global | Hoàn tất W0.1 safe mapper pipeline | Toàn bộ frontend user feedback | Verified |
| 2026-07-19 | Icon system | Material Symbols chỉ render qua `VppIcon` + semantic map | Tránh mixed markup, fallback font và glyph drift giữa route | Global | Hoàn tất W0.1 icon primitive | Shared/layout/account/admin consumers | Verified |
| 2026-07-19 | Viewport contract | Không cưỡng bức scrollbar ở trang ngắn; dense content dùng scroll region có chủ đích | `overflow-y: scroll` làm shell luôn có scrollbar | Global | Hoàn tất W0.1 shell overflow foundation | Dashboard/form/grid waves | Verified |

### Retrofit queue

| Priority | Source feedback | Target route/component | Required change | Status |
|---|---|---|---|---|
| P0 | Mixed icon implementations | `VppEmptyState`, `EmptyState`, shared header/menu | Hợp nhất icon wrapper + semantic map, kiểm tra font/fallback | Verified |
| P0 | Raw `OperationInvalid` toast | Error/notification pipeline và caller | Bắt buộc mapper + localized safe message; raw code chỉ log | Verified |
| P1 | Account shell drift | Login/Register/Forgot/Reset/Confirm/Change/Logout | Dùng chung brand, typography, link/button/menu tokens; giữ recovery compact | Verified |
| P1 | Notification payload localization | Notification producer + DTO/persistence + presentation mapper | Lưu translation key/arguments hoặc bilingual payload; không dịch chuỗi English đã ghép cứng ở UI | Proposed |
| P1 | Repeated/oversized empty panels | Dashboard/History/Period | Contextual state component, one primary CTA, compact previous-period behavior | Verified — shared state + route retrofit complete |
| P1 | Overloaded management grid | `Component_ShareGrid` + Library tabs | Route-specific column profiles, picker/filter drawer, server paging, detail on demand | Verified in ATLAS-001 scope — inspector/action split complete |
| P0 | Radzen DataGrid nested accessibility roles | Opted-in order/library/report grids + `vpp-interactions.js` | Normalize wrapper/table roles, focusable scroll region and invalid generated aria values without changing unaudited grids | Verified |
| P0 | Degenerate chart data | Report + History charts | Không render series/donut khi thiếu điểm hoặc mọi giá trị bằng 0; dùng localized empty state | Verified |
| P0 | Native/duplicated period selector + undersized rem scale | My Orders, Order Create, typography token foundation | Dùng typed `SELECTOR-DECISION`, một nguồn hiển thị kỳ; khóa `html` 16px để token 12/13/14px không bị co | Verified — 2026-08-09 |

Retrofit không mặc định làm ngay giữa route hiện tại nếu không ảnh hưởng correctness/accessibility. Agent phải ghi queue và đề xuất thời điểm xử lý để tránh scope explosion.

---

## 12. QA gates

### Per-route browser matrix

- Desktop baseline: `1366×768` và `1440×900`; wide-screen evidence: `1920×1080`.
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
- `Deque.AxeCore.Playwright` không có violation `critical`/`serious`; dialog/menu/drawer/validation hiện ra phải scan lại hoặc exception được ghi rõ.
- Không unexpected console error hoặc failed API.
- Không raw exception/JSON trong user notification.
- Authorization đúng cả UI và direct API.
- Mutation không double-submit và retry idempotent khi cần.
- DataGrid lớn tuân theo contract theo ý định công việc ở mục **Quy tắc điều hướng dữ liệu**: danh sách tra cứu/quản trị dùng server paging; vùng thao tác liên tục nhiều dòng dùng virtualization và phải benchmark trên route thật trước khi khóa page size/overscan.
- Accessibility Insights FastPass/keyboard review chạy trước khi khóa shared primitive hoặc route quan trọng.
- Screenshot ở route `OWNER_REVIEW` chỉ là evidence; visual golden regression chỉ tạo sau `APPROVED` trên fixture/browser/viewport/font ổn định.
- Lighthouse/Chrome performance không có regression lớn ở route public hoặc route được chọn làm performance budget.

### Commands tối thiểu theo scope

```powershell
dotnet build gtas_vpp.slnx -c Release
dotnet test tests/Frontend.UnitTests/gtas_vpp_fe.Tests.csproj -c Release
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

## 14. Owner approval gate hiện tại

Các quyết định nền đã được owner duyệt và đang áp dụng:

- [x] Browser runtime là nguồn visual cuối; Figma chỉ hỗ trợ.
- [x] Không UI Lab; sửa trực tiếp Blazor thật theo từng route.
- [x] Account flow dùng centered grid shell và không còn PPJ illustration.
- [x] Controlled wow: mạnh ở heading/data story, tiết chế ở table/form/permission.
- [x] Desktop-first theo từng route nhưng mọi route phải không vỡ tablet/mobile.
- [x] Atlas M0–M8 và refactor R-2 đã hoàn tất theo `ATLAS-001`; kế hoạch tiếp theo là `UI-SYSTEM-001`.

Quyết định RBAC đã được owner chốt:

- [x] Authority hiện tại có ba role canonical `EMPLOYEE / MANAGER / DEV` theo `CanonicalRbac.cs`; UI refactor không thay đổi RBAC.
- [x] Mọi production mutation, role redesign hoặc `BREAK_GLASS_OWNER` đều ngoài scope UI và cần approval riêng.

Gate thiết kế mới:

- [x] Duyệt M0 — shell/design-system board bao gồm header, sidebar expanded/collapsed, user popup và interaction states.
- [x] Vòng đầu gồm 28 canonical desktop screens; loading/empty/filter-empty/error kế thừa shared templates và agent tự quyết định bản nháp đầu.
- [x] Duyệt cơ chế Design Atlas: overview/contact sheet + full-resolution screens + global token controls để sửa đồng loạt.
- [x] Chốt typography hybrid system-font/Poppins và phạm vi Dark-mode vòng đầu theo Section 6.7.
- [x] Sinh Design Atlas draft: 28 Light screens, 10 board sheets theo taxonomy M0–M8 có M5A/M5B, overview và 4 Dark representatives; automated geometry/overflow/console gate pass.
- [x] Tách chế độ duyệt fluid khỏi capture cố định; 84/84 kiểm tra responsive desktop pass ở zoom trình duyệt 100%.
- [x] M0 hiển thị đầy đủ hierarchy cha–con–cháu; M1 Login/Logout bám thiết kế account hiện tại; copy VI dùng ngữ cảnh nghiệp vụ thay vì dịch thô.
- [x] Gate lịch sử M0/board đã được thỏa trong ATLAS/UI-SYSTEM trước khi rollout shared primitives và
  route production; final visual acceptance toàn hệ thống được owner chấp thuận ngày 2026-08-04.

### 14.1 Checkpoint triển khai Atlas M0–M2

Trạng thái source Blazor sau khi owner yêu cầu triển khai M0→M2:

- [x] M0: giữ một cây `InteractiveServer`, shell/navigation/provider hiện hành và token VPP Pulse; không thêm render boundary cục bộ.
- [x] M1: Login, Register, Forgot/Reset/Change Password, ConfirmEmail và Logout dùng trực tiếp `VppAccountWorkspace`; adapter account shell đã retire, form Radzen/validation/responsive state giữ nguyên.
- [x] M2 Catalog: bộ lọc nằm cùng card header; server paging dùng `Count + LoadData + Skip/Take`; bỏ cột trạng thái nhân viên và nút tải giả chưa có endpoint.
- [x] M2 Order Create: header hai bước căn giữa; footer theo hierarchy `Quay lại | Lưu nháp → Ghi chú → Tiếp tục`; ghi chú đơn/mặt hàng dùng popover; catalog chọn mặt hàng dùng virtualization và không dùng pager.
- [x] M2 My Orders/History: kỳ được chọn bằng decision card; selector ngang chỉ còn `Đơn thường / Đơn bổ sung`; giữ detail virtualization và History server paging. PDF/Excel My Orders dùng endpoint tải thật hiện hành.
- [x] Browser QA Atlas đã được đóng trong `ATLAS-001`: đủ 28 logical screen tại `390×844`, `768×1024`, `1366×768`, `1920×1080` (112 tổ hợp), không page overflow, console error hoặc network failure ngoài allow-list đã ghi nhận.

Quy tắc dữ liệu đã khóa bằng architecture tests: collection dài hữu hạn dùng server paging; vùng chọn/chi tiết cần cuộn liên tục dùng virtualization; một grid không đồng thời hiển thị pager và continuous scroll.

Checkpoint hiện tại: F0–F7 và `FRONTEND-REFACTOR-001` đã hoàn tất; owner chấp thuận current runtime +
final board ngày 2026-08-04. Golden artifact được hoãn đến clean reproducible HEAD và phase chốt
luận văn/slide. Không đổi API/database/RBAC chỉ để khớp visual trong correction về sau.

> **Lưu ý lịch sử:** các evidence cũ trong file có thể chứa tên thư mục đã retire hoặc lệnh `.sln` của snapshot cũ. Lệnh hiện hành nằm ở Section 12 và dùng `gtas_vpp.slnx`; không sao chép command lịch sử để chạy mù quáng.

### 14.2 Owner correction record — 2026-07-31

| Contract | Trạng thái | Quyết định đã chốt |
|---|---|---|
| Select và page-size | `IMPLEMENTED — QA PASS` | Popup select không dùng vạch xanh dọc; option được chọn chỉ dùng nền nhẹ. Page-size dùng Radzen bridge canonical, không còn CSS riêng của trang quản trị. |
| Lịch sử / Tổng hợp phòng ban | `IMPLEMENTED — QA PASS` | Khi danh sách có dữ liệu, đơn đầu tiên phải được chọn và tải chi tiết ngay; hàng được chọn hiện nền xanh trước mọi click của user. DataGrid chi tiết được re-key khi snapshot đổi để không giữ trang rỗng cũ. |
| Chốt kỳ | `IMPLEMENTED — QA PASS` | Selector kỳ là `Kỳ trước · Kỳ này · Tùy chọn`; `Kỳ này` luôn là kỳ đặt hàng hiện tại từ `PeriodInfo`, `Kỳ trước` là kỳ liền trước và là lựa chọn mặc định khi mở route. Nhà cung cấp/bảng giá tự chọn mặc định giữ nền neutral, chỉ hiện active sau thao tác user; phòng ban hiển thị tên, giữ code làm giá trị lọc. Header bảng hiển thị badge `Chưa chốt kỳ / Đã chốt kỳ`; PDF/Excel chỉ render và thực thi khi `IsSettled=true` cùng `SettlementId` hiện hành. Input hash dùng lựa chọn hiệu lực thực tế để preview tự chọn vẫn xác nhận được. |
| Quản lý người dùng | `IMPLEMENTED — QA PASS` | Nhóm quyền và phòng ban chỉnh trực tiếp bằng dropdown không-search tại đúng cột, dùng cùng height/radius/surface/popup/focus/disabled với filter canonical. Tài khoản chờ duyệt có nút Duyệt rõ ràng; quyền truy cập dùng switch hai chiều dựa trên membership, giữ guard self/last-admin/backend. Bỏ action `manage_accounts`, nút vô hiệu hóa một chiều, dialog membership và model cũ. |
| Kỳ đặt hàng hiện tại | `IMPLEMENTED — VALIDATION IN PROGRESS` | Dùng hai decision card bằng nhau: card chọn kỳ và card hạn gửi theo loại đơn; desktop hai cột, mobile xếp dọc. Selector ngang chỉ còn `Đơn thường / Đơn bổ sung`, nhưng mỗi loại giữ selection riêng. Đơn thường chỉ chọn kỳ `Open`; đơn bổ sung chọn kỳ trước gần nhất thuộc `SubmissionClosed/Pricing/Settled` để vừa tạo khi còn hạn vừa xem sau khi hết hạn/chốt. URL giữ `regularPeriodId` và `supplementPeriodId`; `periodId` chỉ đọc link cũ. |
| Hiệu năng phiên dài | `IMPLEMENTED — SOAK QA PASS` | Scroll/resize/mutation của header, sidebar, History, period picker và cell popover được gộp tối đa một việc mỗi animation frame; hai observer DOM toàn cục được hợp nhất thành một hàng đợi lọc theo motif, observer/listener/animation cục bộ được hủy khi route rời DOM. Regression dùng CDP xác nhận sau 8 vòng enhanced navigation: document/node/listener không tăng tuyến tính, 250 scroll event chỉ xếp 5 frame và nửa sau không chậm hơn nửa đầu. |

Correction runtime ngày 2026-07-31 cho Quản lý người dùng: account `PendingApproval` chỉ hiển thị action `Duyệt` có nhãn; nút bật sau khi chọn đủ nhóm quyền và phòng ban, còn access switch chỉ xuất hiện sau kích hoạt. Mọi trạng thái khóa phải có lý do đọc được; guard chống tự sửa membership vẫn giữ nguyên ở UI và backend.

Mutation evidence ngày 2026-08-04 cho Chốt kỳ: LocalDB cô lập tạo kỳ trước `Pricing` có đơn hợp lệ; `Procurement` chốt revision 1, cùng user bị four-eyes từ chối mà không sinh revision, sau đó `Manager` tạo correction revision 2. API history xác nhận revision cũ bất biến và chỉ revision mới là current. Owner chọn phương án A: notice + `Xem trước lại` bắt buộc tạo snapshot/idempotency key mới trước correction tiếp theo.

### 14.2a Owner correction record — Kỳ đặt hàng và transient surfaces — 2026-08-10

| Contract | Trạng thái | Quyết định đã chốt |
|---|---|---|
| Menu thao tác và page-size | `IMPLEMENTED — ROUTE QA PASS` | Radzen sở hữu tọa độ popup; bridge dự án chỉ gắn motif, hướng mở và khoảng cách 4px. Bỏ radial ripple Material trên control quản trị, bỏ surface/viền lồng nhau, lần bấm thứ hai phải đóng, command đóng menu trước khi chạy nghiệp vụ/toast và mục đầu không tự mang nền selected. |
| Thao tác kỳ | `IMPLEMENTED — ROUTE QA PASS` | `Sửa lịch`, `Gia hạn kỳ`, đóng/mở nhận đơn và xóa kỳ dùng dialog thay cho form nằm dưới grid. Kỳ `Đã chốt` dùng action `Xem bản chốt`; trạng thái trung gian giữ trong domain nhưng UI gọi là `Đã đóng`, còn đóng sớm chỉ nằm trong overflow để giảm nhiễu. |
| Chốt kỳ | `AUTOMATED QA PASS — OWNER REVIEW` | Header quyết định gồm chọn nhà cung cấp, chọn bảng giá, trước VAT + VAT và tổng giá trị; không còn KPI/cột độ phủ. Kỳ đã chốt hiển thị `Bản N`, xem được các bản đã lưu và dùng `Điều chỉnh sau chốt` để tạo bản kế tiếp; không còn reopen. Trong số ngày cấu hình từ ngày đóng (mặc định 10), Quản lý được cập nhật/hủy đơn; chốt sớm được phép khi không còn bổ sung chờ duyệt. |

Evidence route thật cô lập ngày 2026-08-10: menu/page-size và action dialog pass tại `390×844`, `768×1024`, `1366×768`, `1920×1080`; financial settlement workspace + neutral decision option + page-size 8 vòng/view pass `2/2` tại `768×1024`, `1366×768`. Screenshot evidence nằm ngoài Git tại `TestResults/settlement-workspace-20260810-final`.

Evidence đóng record (historical snapshot của 2026-08-02): solution Release build `0 warning`; frontend unit `202/202`; settlement confirmation `4/4`; route-real isolated History + Department Summary `2/2`, User Admin `1/1`, Chốt kỳ `1/1`, My Orders shell/period summary `1/1`. Screenshot đã được kiểm tra bằng mắt tại `390×844`, `1366×768`, `1920×1080`; artifact thô nằm trong thư mục temp ignored, không commit. Không dùng các số này thay cho checkpoint 2026-08-04 ở `FRONTEND-REFACTOR-001`.

### 14.3 Owner review record — Duyệt đơn bổ sung — 2026-08-01

- `PendingApprovalWorkspace` dùng canonical `LIST-DETAIL`: master có collection header/count, filter server theo tìm kiếm + phòng ban, hàng đầu được chọn và tải detail ngay sau load/filter/paging/mutation.
- Master hiển thị full mã đơn, người đặt, phòng ban, kỳ, số mặt hàng và trạng thái; detail hiển thị full code, hai badge loại/trạng thái, metadata, lý do, bảng mặt hàng và PDF/XLSX có nhãn rõ.
- Pager của master và footer quyết định của detail bám đáy từng surface; error state tách khỏi empty/success để lỗi tải không bị hiểu nhầm là hết đơn chờ duyệt.
- Evidence: Release build `0 warning`; frontend unit `213/213`; focused isolated Playwright `1/1` tại `390×844`, `768×1024`, `1366×768`, `1920×1080`, không tràn ngang document. Screenshot đã được kiểm tra bằng mắt; artifact thô nằm trong temp ignored.

### 14.4 Owner final visual acceptance — 2026-08-04

- Owner chấp thuận current authenticated Blazor runtime và final board, không yêu cầu correction visual mới.
- Evidence gồm route manifest 44 key, frontend verify PASS, settlement mutation E2E `1/1`,
  `ShellResponsiveTests` `1/1` và 17 PNG settled trong thư mục ignored.
- Raw English `CanCreateOrderReason` được chấp nhận như localization backlog riêng, không chặn UI acceptance.
- Không tạo golden package ngay: board phản ánh owner-owned `vpp-polish.css` diff chưa có trong clean HEAD.
  Canonicalize/capture lại từ reproducible HEAD khi chọn representative contact sheet và ảnh luận văn/slide.

---

## 15. Research references

- Personal Design DNA export — local, 2026-07-18.
- [PPJ International — Vision and Mission](https://www.ppj-international.com/vision-mission.html)
- [PPJ International — Sustainability](https://www.ppj-international.com/sustainability.html)
- [Atlassian Design — Foundations](https://atlassian.design/foundations)
- [Atlassian Design — Design Tokens](https://atlassian.design/tokens/design-tokens)
- [Apple Human Interface Guidelines — Design principles](https://developer.apple.com/design/human-interface-guidelines/design-principles)
- [Apple Human Interface Guidelines — Layout](https://developer.apple.com/design/human-interface-guidelines/layout)
- [Apple Human Interface Guidelines — Color](https://developer.apple.com/design/human-interface-guidelines/color)
- [Apple Human Interface Guidelines — Lists and tables](https://developer.apple.com/design/human-interface-guidelines/lists-and-tables)
- [Apple Human Interface Guidelines — Sidebars](https://developer.apple.com/design/human-interface-guidelines/sidebars)
- [Apple Human Interface Guidelines — Tab views](https://developer.apple.com/design/human-interface-guidelines/tab-views)
- [Apple Human Interface Guidelines — Motion](https://developer.apple.com/design/human-interface-guidelines/motion)
- [Apple Human Interface Guidelines — Typography](https://developer.apple.com/design/human-interface-guidelines/typography)
- [Apple Human Interface Guidelines — Writing](https://developer.apple.com/design/human-interface-guidelines/writing)
- [Apple Human Interface Guidelines — Loading](https://developer.apple.com/design/human-interface-guidelines/loading)
- [Apple Human Interface Guidelines — Alerts](https://developer.apple.com/design/human-interface-guidelines/alerts)
- [Apple — Giving external agents access to Xcode](https://developer.apple.com/documentation/Xcode/giving-external-agents-access-to-xcode)
- [Sosumi — Apple Docs for LLMs](https://sosumi.ai/)
- [Notion — Updating the design of Notion pages](https://www.notion.com/blog/updating-the-design-of-notion-pages)
- [Figma — Inside the redesigned Figma UI3](https://www.figma.com/blog/behind-our-redesign-ui3/)
- [Linear — UI refresh](https://linear.app/changelog/2026-03-12-ui-refresh)
- [Carbon Design System — Dashboards](https://carbondesignsystem.com/data-visualization/dashboards/)
- [Tableau Blueprint — Visual Best Practices](https://help.tableau.com/current/blueprint/en-us/bp_visual_best_practices.htm)
- [Microsoft Learn — Accessible Power BI Reports](https://learn.microsoft.com/en-us/power-bi/create-reports/desktop-accessibility-creating-reports)
- [W3C — WCAG 2.2](https://www.w3.org/TR/WCAG22/)
- [Radzen Blazor DataGrid](https://blazor.radzen.com/datagrid?theme=default&wcag=true)
- [Radzen Blazor MCP Documentation](https://www.radzen.com/blazor-mcp/documentation)
- [Radzen DataGrid Performance](https://blazor.radzen.com/datagrid-performance)
- [MDN — prefers-reduced-motion](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/%40media/prefers-reduced-motion)
- [Microsoft Learn MCP Server](https://learn.microsoft.com/en-us/training/support/mcp-get-started)
- [Chrome DevTools MCP](https://github.com/ChromeDevTools/chrome-devtools-mcp)
- [Playwright — Accessibility testing](https://playwright.dev/docs/accessibility-testing)
- [Playwright — Visual comparisons](https://playwright.dev/docs/test-snapshots)
- [Deque axe-core](https://github.com/dequelabs/axe-core)
- [Deque.AxeCore.Playwright](https://www.nuget.org/packages/Deque.AxeCore.Playwright)
- [Accessibility Insights for Web](https://accessibilityinsights.io/docs/web/overview/)
- [Lighthouse CI](https://github.com/GoogleChrome/lighthouse-ci)
- [Figma MCP Server](https://help.figma.com/hc/en-us/articles/32132100833559-Guide-to-the-Dev-Mode-MCP-Server)
- [Vite — Getting Started](https://vite.dev/guide/)
- [shadcn/ui — Introduction](https://ui.shadcn.com/docs)
- [shadcn/ui — MCP Server](https://ui.shadcn.com/docs/mcp)
- [Aspire — AddViteApp](https://aspire.dev/reference/api/typescript/aspire.hosting.javascript/addviteapp/)
- [Hey API — OpenAPI TypeScript](https://heyapi.dev/openapi-ts/get-started)

---

## 16. Continuation protocol cho AI agent

Khi tiếp tục UI renovation trong thread/session mới:

1. Đọc `AGENTS.md` và mọi `AGENTS.md` gần scope.
2. Đọc toàn bộ file này.
3. Đọc `src/Frontend/Blazor/AGENTS.md`, UI repo skill, `.github/copilot-instructions.md` và route source.
4. Kiểm tra `git status`, branch và diff chưa commit.
5. Đọc `docs/execution/FRONTEND-REFACTOR-001.md`, `RouteAcceptanceManifest.cs` và evidence mới nhất;
   các bảng W0–W8 ở mục 9 chỉ là historical snapshot.
6. Final visual board đã được owner duyệt; nếu mở correction UI mới, tạo execution slice riêng và không
   tự chọn một dòng `PENDING` cũ trong route ledger lịch sử.
7. Không suy luận rằng Figma/Atlas đã cover đủ logical route.
8. Không tạo thêm UI Lab/project preview trong repository; React POC cũ chỉ tồn tại ở archive tag và không đổi architecture render mode của Blazor.
9. Không thay đổi API/DB/nghiệp vụ chỉ để đạt visual.
10. Cập nhật current handoff ở execution record và file này trước khi báo route/wave hoàn tất.

### 16.1 Local development loop bằng dotnet-watch

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

### 16.2 Bước đầu tiên của renovation

**W0.0 — Baseline capture (historical protocol, không phải next action hiện tại):**

- Chạy `.\scripts\gtas.cmd run`.
- Kiểm tra shell, login, theme/language switch và resource frontend.
- Chụp baseline route ưu tiên ở `1920×1080`, sau đó spot-check `768×1024` và `390×844`.
- Ghi đúng dữ liệu/role/permission/state đã dùng vào route ledger.
- Đánh dấu issue theo ba nhóm: correctness, usability, visual polish.
- Chỉ sau khi baseline được lưu mới bắt đầu W0 shared foundation hoặc route đầu tiên.

Baseline là bằng chứng so sánh; không được sửa screenshot để khớp thiết kế, không được xóa baseline vì route mới trông khác. Ở checkpoint hiện tại,
ưu tiên đọc current handoff của `FRONTEND-REFACTOR-001` trước khi chạy lại baseline.

Prompt tiếp tục ngắn:

```text
Tiếp tục VPP Pulse Blazor UI renovation theo
docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md.
Đọc ledger/learning/retrofit hiện tại, kiểm tra git status và làm đúng route tiếp theo.
Browser runtime là visual authority; không dùng Figma làm pixel source và không tạo UI Lab.
```

### 16.3 Compact button density — 2026-08-09

- Button thao tác chuẩn dùng chiều cao thị giác `32px`; button nhỏ và icon-only dùng `28px`; icon dùng `16px`.
- Padding ngang chuẩn `10px`, compact `8px`; input/select vẫn giữ `36px` vì đây là control nhập liệu, không phải action density.
- Hover/active/focus đổi surface hoặc viền mảnh trong chính control; không dùng shadow nâng, translate hoặc scale làm nút trông như hộp nổi.
- Login CTA và reconnect action tiếp tục là ngoại lệ lớn có chủ đích. Navigation row/tab không bị ép theo token action button.
- Thiết bị coarse pointer không được phình Radzen action thành `44px`; contract `28–32px` vẫn vượt target tối thiểu 24px và giữ layout gọn.

### 16.4 Login session-race và account control geometry — 2026-08-09

- Backend `401 session-invalid` phải gắn dấu vân tay SHA-256 của bearer token bị từ chối vào logout flow; tuyệt đối không đưa token thô vào URL hoặc log.
- Nếu logout cũ đến sau khi người dùng đã đăng nhập lại và cookie hiện tại mang token mới, endpoint frontend bỏ qua logout cũ và giữ phiên mới. Logout chủ động không có fingerprint vẫn thu hồi backend session và xóa cookie như trước.
- Login form chỉ dùng submit của form, có guard `isLoading` để không gửi trùng khi Enter/click nhanh; không render lại circuit sau khi đã bắt đầu full-navigation tới `perform-login`.
- Universal flat-button bridge chỉ áp cho Radzen action button, không áp mù lên native `<button>`. Login CTA giữ motion riêng của `ACCOUNT`; password eye dùng khung `28px`, icon `16px` và neo phía trên underline.
- Owner correction 2026-08-17: copy toàn bộ account flow dùng câu ngắn, đủ ý; cặp điều hướng phụ luôn đặt `Quay lại đăng nhập` bên trái, hành động bổ sung bên phải và không xuống dòng ở desktop.

### 16.5 Stable grouped-header navigation — 2026-08-10

- Tab cha có tab con phải giữ cùng một component và DOM root khi chuyển giữa trạng thái thu gọn/mở rộng; không đổi trực tiếp từ anchor sang component theo route.
- Mỗi tab cấp cao dùng key ổn định theo path để Blazor không tái sử dụng sai node của tab đứng cạnh, tránh mất chữ hoặc nháy khi chuyển giữa `Bảng giá` và `Phòng ban`.
- Chỉ một bề mặt cha hoặc nhóm cha-con tham gia layout tại một thời điểm; indicator tiếp tục dùng motion token điều hướng chung, không tăng duration để che lỗi render.

### 16.6 Price-list collection terminology — 2026-08-10

- Quyết định owner ngày 2026-08-11 thay thế yêu cầu hiển thị `Phiên bản`: bảng giá không còn cột, input hoặc suffix `vN` trên UI. `PriceList.Version` chỉ được giữ tạm ở backend để tương thích và sẽ có migration cleanup riêng sau consumer/data audit.
- Header bảng giá dùng đầy đủ `Nhà cung cấp` và `Số dòng giá`; không dùng viết tắt `NCC`, `SL` cho dữ liệu có thể gây hiểu nhầm.
- `Số dòng giá` là số bản ghi giá thuộc bảng giá, không phải số lượng hàng được đặt.
- Cờ bảng giá mặc định là thông tin nhận biết, hiển thị bằng badge trung tính; không dùng disabled switch vì tạo cảm giác đây là nút bật/tắt trực tiếp.
- Owner correction 2026-08-18: bảng `Giá mặt hàng`, file mẫu, import và export chỉ dùng `Mã mặt hàng` thống nhất của hệ thống; không duy trì mã mặt hàng riêng theo nhà cung cấp. Dữ liệu hiển thị mặc định gồm mặt hàng, mã, danh mục, đơn vị, đơn giá, VAT và thao tác; mô tả/audit nằm trong picker. `Trạng thái giá`, `Số lượng tối thiểu`, `Ngày giao` và cờ mặc định cấp dòng không xuất hiện trong grid, picker, filter hoặc form sửa.
- Import bảng giá đã triển khai theo `docs/execution/PRICING-IMPORT-VERSIONING-001.md`: template chuẩn tự nhận diện; file NCC dùng tên cột lạ mở bước ghép cột có sample values rồi mới preview/xác nhận. Mapping được lưu trong lịch sử lần nhập; Gemini chỉ gợi ý các cột chưa nhận diện khi có key environment, luôn giữ bước xác nhận và fallback thủ công.
- Owner refinement 2026-08-15: `Tải file mẫu` và `Nhập từ file` là action cấp collection, nằm trên header `Danh sách bảng giá` trước action chính `Thêm bảng giá`. Dialog nhập từ collection bắt buộc chọn một bảng giá đang hoạt động; `Xuất Excel` là action cấp dòng trong menu `...` vì phụ thuộc bảng giá cụ thể.
- File Excel xuất từ từng bảng giá giữ đúng cấu trúc import (`ItemCode`, `UnitPrice`, VAT, MOQ, ngày giao, mặc định, ghi chú) để có thể chỉnh rồi nhập lại. Tên cột khác mẫu đi qua bước ghép cột; thiếu `Mã mặt hàng`/`Đơn giá`, dữ liệu dòng sai, file hỏng, sai loại, quá 5 MB hoặc quá 5.000 dòng đều không được ghi database.

### 16.7 Column picker và identity-cell contract — 2026-08-14

- Execution plan canonical: [`UI-COLUMN-CONTRACT-001`](../execution/UI-COLUMN-CONTRACT-001.md), trạng thái `C0–C6 IMPLEMENTED — OWNER VISUAL REVIEW`.
- Trigger column picker chỉ hiển thị số cột đang bật, ví dụ `Cột 6`, để giữ toolbar gọn; tổng số cột vẫn hiện đầy đủ trong popup và aria-label. Số đếm không tính `#`, checkbox chọn dòng hoặc `Thao tác`; trigger cao đúng 32px để thẳng nhịp với search/filter của `FILTER-TOOLBAR`.
- Cột cấu trúc phải cố định và không pickable. Raw GUID/FK ID, `RowVersion` và concurrency token không được đưa vào picker; audit route chỉ ngoại lệ cho identifier thực sự phục vụ điều tra.
- Bảng giao dịch/đọc nhanh dùng ô hai dòng `Tên + mã`. Màn quản trị nơi mã là khóa tra cứu, import, sort hoặc copy phải tách `Tên` và `Mã` thành hai cột.
- Thứ tự chuẩn là `nhận diện → phân loại/quan hệ → trạng thái → định lượng → mô tả/audit → thao tác`; toolbar filter bám theo đúng thứ tự cột có thể lọc.
- Không tự đưa mọi property DTO lên UI. Mỗi cột phải có label thân thiện, lý do nghiệp vụ, visibility mặc định, pickability, sort/filter và responsive priority rõ ràng.
- Implementation 2026-08-14 đã audit 21 file/26 grid và mở route thật đủ 11/11 picker; architecture test khóa cột cấu trúc/kỹ thuật, thứ tự toolbar và identity-cell policy. Lookup master-detail còn một layout gate độc lập về document scroll, theo dõi ngoài column-contract slice.
- Owner refinement 2026-08-14: cả 11 picker quản trị có thêm `ID` của chính bản ghi, luôn ẩn mặc định; foreign-key ID, numeric `UserId` và `RowVersion` vẫn bị loại. Nhãn cột audit đổi thành `Ngày tạo`/`Ngày cập nhật`. Danh sách toàn bộ cột đang ẩn để duyệt nằm tại mục 8 của `UI-COLUMN-CONTRACT-001`.
- Owner refinement 2026-08-14/15 (C6): các grid quản trị dùng số liệu quan hệ đang hoạt động để hỗ trợ quyết định (`Số giá trị`, `Số mặt hàng`, `Số nhà cung cấp`, `Số người dùng`, `Số quyền`). Bảng giá hiện `Ngày cập nhật`; thay `Nhập gần nhất` bằng `Nguồn dữ liệu` (`Mặc định`, `Excel`, `CSV`, `Thủ công`) trong picker vì ngày tạo/cập nhật đã đủ thể hiện độ mới. `Lần đăng nhập gần nhất`, `Ngày cập nhật` của giá mặt hàng và `ID tài nguyên` tiếp tục chỉ nằm trong picker. Supplier dùng một cột địa chỉ ghép; field kỹ thuật, khóa ngoại và dữ liệu thương mại hoãn vẫn không xuất hiện.
- Owner refinement 2026-08-15 (`FILTER-TOOLBAR`): `Xóa bộ lọc` dùng chiều cao cố định 32px, thẳng nhịp với search, filter select và column picker trên toàn frontend.
- Owner refinement 2026-08-15/18 (`FILTER-COVERAGE`): màn quản trị chỉ thêm facet có giá trị tra cứu rõ, không sao chép máy móc mọi cột thành filter. Thứ tự canonical đã khóa theo cột hiển thị: Loại danh mục `Trạng thái`; Mặt hàng `Danh mục → Đơn vị → Nhà cung cấp → Trạng thái`; Nhà cung cấp `Trạng thái`; Phòng ban `Phòng ban cha → Trạng thái`; Danh sách bảng giá `Nhà cung cấp → Trạng thái`; Giá mặt hàng `Danh mục → Đơn vị`. Filter chạy trên full authorized dataset trước paging, clear-filter reset toàn bộ facet và toolbar phải wrap trong surface ở độ rộng laptop.
- Owner refinement 2026-08-19 (`SEARCH-NORMALIZATION`): mọi ô search toolbar phải cho cùng kết quả khi gõ tiếng Việt có dấu hoặc không dấu, kể cả `Đ/đ`. Query lớn chạy server-side bằng `Vietnamese_100_CI_AI` với LIKE được escape; snapshot/local detail dùng `VppSearchText`. Query được tách tối đa 10 từ, yêu cầu đủ mọi từ trên các trường được placeholder công bố và hỗ trợ dạng viết liền như `butlong` ↔ `Bút lông`; không dùng danh mục/đơn vị/trạng thái để tạo kết quả ngoài ý định khi các trường này đã có filter riêng. Debounce giữ 250–350ms và search luôn chạy trước `Count/Skip/Take` trên full authorized query.

### 16.8 Tạm dừng nhiều NCC và plan giới hạn số lượng — 2026-08-18

- Capability đề xuất/tách phương án chốt qua nhiều nhà cung cấp tạm ẩn bằng feature flag `Features:Settlement:MultiSupplierEnabled=false`. UI và thao tác áp dụng recommendation đều bị chặn; backend optimizer, DTO và lịch sử bản chốt được giữ để không mất dữ liệu và có thể đánh giá lại sau.
- Màn Chốt kỳ tiếp tục dùng một nhà cung cấp và một bảng giá làm phương án chính. Việc bật lại nhiều NCC cần owner duyệt thêm chi phí vận chuyển, điều khoản hợp đồng, giới hạn số NCC và acceptance route-real.
- Giới hạn số lượng đã triển khai theo [`ORDER-QUANTITY-LIMITS-001`](../execution/ORDER-QUANTITY-LIMITS-001.md): mỗi mặt hàng có mức tối đa cho **mỗi đơn**, áp dụng giống nhau và độc lập cho đơn thường/đơn bổ sung; không cộng dồn theo kỳ, người dùng hoặc phòng ban. Quản trị sửa trực tiếp trong Danh mục mặt hàng, frontend giữ nút tăng ở trạng thái mờ khi đạt trần và backend kiểm tra lại ở mọi đường tạo/sửa/khôi phục/tạo lại/điều chỉnh đơn.
- Owner refinement 2026-08-18/19: trong luồng tạo đơn, giới hạn chỉ xuất hiện tại danh mục chọn hàng và pane `Đơn đang tạo`. Danh mục dùng cột riêng `Số lượng tối đa`; pane đặt nhãn ngay dưới stepper, hiển thị số nguyên không phân cách hàng nghìn. Nhập vượt trần phải giữ nguyên để báo validation đỏ và khóa `Tiếp tục`, không hard-clamp; input chỉ có một viền focus. Không chèn giới hạn thành dòng thứ ba dưới tên/mã và không lặp lại ở bước xem lại.

### 16.9 Owner plan revision: kỳ, bổ sung và Excel — 2026-08-18

- Plan canonical: [`ORDERING-PRICING-REVISION-20260818`](../execution/ORDERING-PRICING-REVISION-20260818.md). Trạng thái `IMPLEMENTED — FOCUSED QA PASS`.
- Đơn bổ sung target mới dùng phương án B: chỉ mở capability cho kỳ đã đóng trong 5 ngày; nếu chốt sớm thì action giữ vị trí nhưng disabled và giải thích kỳ đã chốt.
- Trang Các kỳ đặt hàng target mới bỏ ý niệm rolling 3 kỳ khỏi UI. Scheduler tự mở một kỳ chuẩn; action cấp collection `Thêm kỳ` mở dialog dùng lịch mặc định nhưng cho quản lý điều chỉnh. Kỳ thủ công chưa đến ngày mở hiển thị `Chưa mở`, không dùng copy `Sắp mở`.
- File Excel target mới chỉ còn mã/tên/đơn vị/đơn giá/VAT/ghi chú. MOQ, ngày giao, giá mặc định cấp dòng và điều khoản thương mại ẩn phải được gỡ đồng bộ khỏi template, mapping, preview và apply; không chỉ ẩn cột grid.
- Một hay nhiều NCC đang quay lại decision gate. UI tiếp tục một NCC; không dùng ADR-015 làm approval để bật tính năng cho đến khi owner chọn A/B/C trong plan canonical.

### 16.10 My Orders theo kỳ được chọn — 2026-08-18

- Header `Đơn hàng của tôi` dùng ba card bằng nhau: `Kỳ đặt hàng`, `Hạn gửi đơn thường`, `Hạn gửi đơn bổ sung`. Hai mốc hạn luôn hiện cùng lúc và cùng thuộc kỳ đang chọn; chuyển loại đơn không làm hàng card đổi cấu trúc.
- Card bổ sung diễn đạt theo đúng vòng đời: khi kỳ thường còn mở hiển thị `Mở sau N ngày`; khi cửa sổ bổ sung đã mở mới hiển thị `Còn N ngày`; kỳ đang chốt hoặc đã chốt hiển thị `Đã đóng`. Ngày bên phải vẫn là hạn cuối nhận đơn bổ sung.
- Selector ngang chỉ phân loại `Đơn thường | Đơn bổ sung`; bỏ `Kỳ trước` vì kỳ đã được chọn ở card trên và lịch sử đơn đã có route riêng.
- URL `orderView=previous` cũ fallback về đơn thường; route catalog không tiếp tục quảng bá biến thể đã nghỉ.
- Migration chuyển tiếp chỉ soft-delete 09–10/2026 do rolling cũ tự sinh khi chưa có bất kỳ dữ liệu nghiệp vụ; giữ 08/2026 và mọi kỳ thủ công/có đơn.

### 16.11 Khóa lịch quá khứ và vòng đời kỳ chưa có dữ liệu — 2026-08-19

- Kỳ tạo thủ công phải có `Ngày mở` không nhỏ hơn thời điểm nghiệp vụ hiện tại và `Ngày đóng` ở tương lai. Frontend chặn sớm trong dialog; backend là nguồn kiểm tra cuối để không thể bỏ qua bằng API.
- Stable Capability Surface của từng dòng luôn giữ nhóm `Gia hạn kỳ → Vô hiệu hóa/Khôi phục → Xóa kỳ`. Hành động không hợp lệ vẫn hiện mờ kèm lý do, không biến mất theo trạng thái.
- Chỉ kỳ chưa có bất kỳ đơn, bản chốt hoặc yêu cầu điều chỉnh sau chốt mới được vô hiệu hóa. `Xóa kỳ` là hard delete hai bước: phải vô hiệu hóa trước và vẫn không có dữ liệu liên quan.
- Kỳ vô hiệu hóa vẫn xuất hiện trong danh sách với badge `Vô hiệu hóa` và filter riêng để quản lý có thể khôi phục. Chỉ khôi phục khi kỳ chưa hết hạn, không thuộc tháng quá khứ và không trùng một kỳ đang hoạt động.
- Owner refinement 2026-08-19 (`TRANSIENT` + `CAPABILITY-SURFACE`): mutation từ menu `...` phải cập nhật ngay danh sách, bộ đếm và capability của dòng sau khi API thành công; không yêu cầu refresh trang. Callback menu Radzen nằm ngoài event pipeline của route nên workspace chủ động yêu cầu render lại sau khi hoàn tất mutation.
- Owner refinement 2026-08-19 (`DIALOG-EDITOR` + `SELECTOR-DECISION`): dialog `Thêm kỳ` luôn giữ đủ 12 tháng để người dùng hiểu lịch, nhưng tháng đã qua hoặc đã có bản ghi (kể cả kỳ đang vô hiệu hóa) phải hiện mờ và không chọn được. Khi đổi năm, trạng thái tháng được tính lại ngay; lịch `Ngày mở/Ngày đóng` dùng giới hạn chọn để các ngày quá khứ cũng hiện mờ và không thể lưu. Backend tiếp tục là lớp kiểm tra trùng/quá khứ cuối cùng.

### 16.12 Tạo đơn nhận kỳ từ My Orders — 2026-08-19

- `Đơn hàng của tôi` là nơi chọn kỳ và loại đơn trước khi mở editor; mọi action tạo, sao chép, sửa và tạo lại đều truyền `periodId` sang `/dashboard/order-create`.
- Trang tạo đơn không lặp card/selector `Kỳ đặt hàng`. Kỳ đã chọn tiếp tục hiển thị dạng ngữ cảnh read-only trong pane `Đơn đang tạo`, đồng thời backend vẫn kiểm tra kỳ khi tải và gửi đơn.
- Liên kết trực tiếp không có `periodId` tiếp tục dùng kỳ mặc định do period-info trả về để giữ tương thích, nhưng editor không trở thành nơi đổi kỳ giữa chừng.

### 16.13 Điều chỉnh từng mặt hàng sau chốt — 2026-08-19

- `Điều chỉnh sau chốt` có ba khả năng rõ ràng: đổi số lượng, bỏ từng mặt hàng đang có hoặc hủy toàn bộ đơn. Không cho thêm mặt hàng mới trong luồng này; nhu cầu mới phải đi qua đơn bổ sung đúng nghiệp vụ.
- Danh sách mặt hàng giữ bề mặt ổn định: dòng bị bỏ vẫn hiện mờ với trạng thái `Sẽ bỏ` và action `Hoàn tác`. Phải giữ ít nhất một mặt hàng khi chọn điều chỉnh; nếu bỏ toàn bộ thì chuyển sang `Hủy toàn bộ đơn`.
- Frontend kiểm tra thay đổi thật, giới hạn số lượng mỗi đơn và khóa gửi khi không hợp lệ. Backend kiểm tra lại mặt hàng thuộc đúng đơn nguồn, còn trong bản chốt hiện hành, không vượt giới hạn và từ chối yêu cầu không có thay đổi.
- Yêu cầu vẫn cần một quản lý khác duyệt. Duyệt chỉ tạo bản đơn hiện hành mới và đánh dấu kỳ có thay đổi chưa chốt; quản lý phải mở preview rồi `Chốt lại kỳ` để tạo bản chốt N+1. Bản chốt cũ không bị sửa hoặc xóa.
- Hàng chờ duyệt và preview chốt lại hiển thị số mặt hàng đổi số lượng, số mặt hàng bị bỏ hoặc `Hủy toàn bộ đơn`. Nhật ký lưu delta có cấu trúc để đối chiếu mà không lộ thuật ngữ kỹ thuật trên UI.
- Verification 2026-08-19: backend focused `10/10`, frontend architecture `29/29`, route-real isolated Release `1/1`; ảnh kiểm tra gồm trạng thái bỏ/hoàn tác mặt hàng và hủy toàn bộ đơn tại `TestResults/goal-post-settlement/`.
