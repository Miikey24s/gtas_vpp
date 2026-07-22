# VPP Pulse — Blazor UI Renovation Living Master Plan

> **Trạng thái:** `ACTIVE — BLAZOR/RADZEN DEADLINE PATH; REACT PAUSED`
>
> **Phiên bản:** `1.34` — 2026-07-22
>
> **Mục tiêu:** Làm nguồn thực thi ưu tiên cho frontend Blazor/Radzen trong giai đoạn deadline. React được giữ nguyên để tiếp tục sau, không xóa hoặc ghi đè.
>
> **Implementation authority:** Blazor/Radzen là implementation authority hiện tại. React chỉ ở trạng thái `PAUSED/DEFERRED`; không sửa React trong giai đoạn này nếu không có quyết định mới.
>
> **Research/reference:** Personal Design DNA, VPP Pulse/Figma, PPJ-inspired operating values, Apple HIG và các nguồn UI/data/accessibility chính thức phù hợp từng vấn đề.

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

### 3.0 Quyết định chuyển ưu tiên — 2026-07-21

- Owner tạm ngưng React vì sắp tới đợt deadline và tiếp tục hoàn thiện frontend Blazor/Radzen hiện hành.
- Không xóa `gtas_vpp_fe_react`, không hoàn tác các commit React đã có; chỉ đóng băng thay đổi mới trên React.
- Mọi UI work tiếp theo phải sửa trực tiếp `gtas_vpp_fe`, dùng API/DTO và database TEST hoặc isolated fixture thật.
- Thứ tự hiện tại: khóa lại W1 `dashboard.my-orders` theo baseline round 6 → QA/owner review/commit vertical slice → W2 employee/order flows → W3 management → W4 procurement/period.
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
- Khi Figma có Import GitHub/Code on Canvas, import toàn repository để đọc đúng Blazor, shared DTO, route catalog và living plan; không chọn `gtas_vpp_fe_react` làm frontend target.
- Figma không chạy/ship Blazor thay Codex. Nếu cần React code layer để dựng preview, output đó chỉ là prototype thiết kế cô lập; sau owner review, implementation thật vẫn được viết và QA trong `gtas_vpp_fe`.
- Toolchain phải đi theo vai trò: Sosumi/Apple HIG cho hierarchy/clarity/spacing/feedback, Microsoft Learn/Radzen cho framework/component, Playwright cho route/DOM/ARIA, Chrome DevTools cho debug/performance, axe cho accessibility và Figma cho design context.
- Nếu Radzen MCP hết quota hoặc key không hoạt động, dừng toàn bộ công việc và chờ owner cung cấp key mới.
- Mỗi route được sửa, QA, review và commit như một vertical slice nhỏ.

### 3.1.1 Frontend React — PAUSED/DEFERRED (quyết định 2026-07-21)

- Tạo frontend mới tại `gtas_vpp_fe_react`; hậu tố công nghệ giúp phân biệt rõ với `gtas_vpp_fe` Blazor trong giai đoạn hai stack cùng tồn tại.
- Các nội dung bên dưới là hồ sơ kỹ thuật và bằng chứng đã làm, không phải phạm vi triển khai của giai đoạn deadline hiện tại.
- Không thực hiện thêm thay đổi, QA hoặc migration route React cho đến khi owner mở lại phạm vi này.
- Không đổi tên, ghi đè hoặc xóa frontend Blazor. Blazor vẫn là bản luận văn/runtime authority cho đến khi React đạt route parity và owner duyệt cutover rõ ràng.
- Stack nền: React + TypeScript + Vite, shadcn/ui + Tailwind CSS, React Router, TanStack Query/Table, React Hook Form + Zod, i18next, Lucide và Recharts.
- Không dùng Next.js cho giai đoạn này: GTAS là application nội bộ, backend ASP.NET Core/JWT đã tách riêng và không cần SEO/React Server Components hoặc thêm một Node production server.
- TypeScript contract phải sinh từ Swagger/OpenAPI của backend; không tự chép DTO C# bằng tay và không tạo contract nghiệp vụ song song.
- Local orchestration dùng Aspire `AddViteApp`; production serving model chỉ được chốt sau proof-of-concept, vì Vite dev server không phải production web server.
- Proof-of-concept đầu tiên là `Login → App shell → My Orders` với API/TEST thật, đủ VI/EN, Light/Dark/Print, responsive, loading/empty/error/success, permissions và accessibility.
- Chỉ bắt đầu migrate route tiếp theo khi proof-of-concept được chứng minh tốt hơn Blazor bằng runtime review và test, không dựa vào mock screenshot.

**React foundation evidence — 2026-07-20:**

- `gtas_vpp_fe_react` đã được scaffold bằng React 19 + TypeScript 6 + Vite 8; không sửa hoặc ghi đè frontend Blazor.
- Foundation đã có React Router lazy routes, TanStack Query provider, i18next VI/EN, Light/Dark/Print tokens, shadcn/ui source components, responsive shell và error/not-found boundary.
- OpenAPI client dùng `@hey-api/openapi-ts`; URL Swagger lấy từ `GTAS_OPENAPI_URL`, còn runtime `/api` dùng Aspire service discovery/proxy hoặc `.env.local` khi chạy Vite độc lập.
- `MyAspire.AppHost` đã tích hợp resource `frontend-react` bằng `AddViteApp`, reference/wait backend và external HTTP endpoint; owner vẫn tự quản lý tiến trình AppHost/dotnet-watch.
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

- entity `VPP item/product` hiển thị cho người dùng là **mặt hàng** / **item**;
- dùng `Mã mặt hàng`, `Tên mặt hàng`, `Danh mục mặt hàng`, `Tổng mặt hàng` nhất quán;
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

### 6.5 Thứ tự triển khai đề xuất trước khi sửa code

1. **W0.1 Shared foundation:** audit icon loading, notification/error pipeline, typography/spacing/link/button tokens và height/overflow contract.
2. **W0.2 Account shell:** Login, Forgot/Reset/Change Password, Register, logout menu; chốt brand lockup và các state lỗi/thành công.
3. **W1 Dashboard:** My Orders empty/non-empty, status summary, previous-period behavior và above-the-fold layout.
4. **W2 History:** filter, loading, empty-by-filter, no-history, error, retry và pagination copy.
5. **W3 Library/admin:** Departments trước, sau đó Classes/Categories/Items/Suppliers/Price Lists; áp dụng column profiles và CRUD drawer/form states.
6. **W4 còn lại:** period operations, permissions, reports và các route detail; mỗi route phải kế thừa primitive đã chốt, không tạo style riêng.

Mỗi wave phải chạy browser review ở `1920×1080`, spot-check `768×1024`/`390×844`, VI/EN, Light/Dark/Print, loading/empty/error/success/disabled và kiểm tra console/network trước khi chuyển wave.

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
| `/` redirect | PENDING | First accessible route, no blank flash |
| `/Account/Login` | APPROVED | Centered shell; inline credential feedback; VI/EN switch |
| `/Account/Register` | APPROVED | No employee-code field; localized stable validation; no desktop scroll |
| `/Account/ConfirmEmail` | APPROVED | Success/expired/invalid; compact shell |
| `/Account/ForgotPassword` | APPROVED | Anti-enumeration; compact recovery shell |
| `/Account/ResetPassword` | APPROVED | Policy/expired/replay; invalid link hides form |
| `/Account/ChangePassword` | APPROVED | Current/new/confirm; forced-change context |
| `/loginprocess` | PENDING | Progress/fallback only |
| `/logoutprocess` | APPROVED | Safe clear + redirect login; branded progress shell |
| `/Error` | PENDING | Safe message + correlation + retry |
| `/not-found` | PENDING | Return to valid workspace |
| Shell/notification/reconnect | PENDING | Context preservation, action inbox |

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
| `dashboard.my-orders` | OWNER_REVIEW | Round 6 restored: Radzen shell/tab, compact command center, four evidence points and one previous-period archive |
| `dashboard.history` | PENDING | Timeline/revision/detail |
| `dashboard.catalog` | PENDING | Browse/search/read-only detail |
| `dashboard.order-create.new` | PENDING | Select → review → submit |
| `dashboard.order-create.edit` | PENDING | Update + stale/permission guard |
| Copy previous | PENDING | Diff and source context |
| Additional request | PENDING | Reason/quota/current attempt |

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

**W1 My Orders round-7 owner feedback — 2026-07-20:**

- round 6 vẫn mang cảm giác dashboard template và còn quá nhiều lớp trang trí;
- owner yêu cầu route này chỉ học Apple, không pha Notion/Figma/Linear: nhẹ, đơn giản và tối ưu;
- gradient, left accent, progress rail, pill background, outlined CTA, evidence kéo hết chiều ngang và card lồng nhau đều làm nội dung nặng hơn giá trị thực.

**W1 round-7 Apple-led direction — 2026-07-20:**

- `Simplicity`: mỗi thành phần phải có mục đích; bỏ progress visualization khi deadline text đã đủ trả lời;
- `Hierarchy`: dùng typography và spacing để phân cấp; không dùng nhiều màu, gradient hoặc border cạnh tranh;
- `Agency`: primary action ở ngay context, dùng một filled accent button; secondary/status controls giữ monochrome;
- `Lists and tables`: text ngắn, header rõ, row dễ quét; table nền phẳng với hairline separator thay vì zebra/card decoration nặng;
- `Color`: chỉ giữ accent cho primary action và status dot; không dùng cùng màu để trang trí text, icon và background đồng thời;
- Apple HIG là bộ lọc thẩm mỹ chính, không phải mẫu để sao chép pixel; W3C/Deque, Radzen/Microsoft và nguồn data visualization chính chủ vẫn được dùng cho accessibility, component behavior và cách trình bày dữ liệu mà HIG không đặc tả đủ cho dashboard web;
- các nguồn Notion/Figma/Linear ở round 6 chỉ còn là lịch sử nghiên cứu, không còn là art direction chủ động của `dashboard.my-orders`.

**W1 round-7 MCP/reference decision — 2026-07-20:**

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
- CTA supplement dùng copy `Tạo đơn bổ sung` / `Create supplement`, không còn quota fraction; backend default/config/legacy fallback đều enforcement `MaxApprovedSupplements=1`, test và tài liệu quyết định đã đồng bộ;
- shared radius tokens, Radzen/Bootstrap compatibility variables và shell controls chuyển sang góc vuông; avatar, status dot và icon có semantics hình tròn được giữ lại;
- Release solution build pass `0 warning / 0 error`; frontend tests `144/144`, backend tests `410/410`; isolated My Orders + header/user-menu browser QA pass `2/2` và không overflow trên bốn viewport;
- route trở lại `OWNER_REVIEW`; evidence local mới nằm ngoài repository tại `%TEMP%\\gtas-vpp-w1-round8`.

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
| P1 | Repeated/oversized empty panels | Dashboard/History/Period | Contextual state component, one primary CTA, compact previous-period behavior | Dashboard implemented; History/Period pending |
| P1 | Overloaded management grid | `Component_ShareGrid` + Library tabs | Route-specific column profiles, picker/filter drawer, server paging, detail on demand | Proposed |

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
- DataGrid lớn dùng server paging/`LoadData`; virtualization chỉ sau benchmark.
- Accessibility Insights FastPass/keyboard review chạy trước khi khóa shared primitive hoặc route quan trọng.
- Screenshot ở route `OWNER_REVIEW` chỉ là evidence; visual golden regression chỉ tạo sau `APPROVED` trên fixture/browser/viewport/font ổn định.
- Lighthouse/Chrome performance không có regression lớn ở route public hoặc route được chọn làm performance budget.

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

## 14. Owner approval gate hiện tại

Các quyết định nền đã được owner duyệt và đang áp dụng:

- [x] Browser runtime là nguồn visual cuối; Figma chỉ hỗ trợ.
- [x] Không UI Lab; sửa trực tiếp Blazor thật theo từng route.
- [x] Account flow dùng centered grid shell và không còn PPJ illustration.
- [x] Controlled wow: mạnh ở heading/data story, tiết chế ở table/form/permission.
- [x] Desktop-first theo từng route nhưng mọi route phải không vỡ tablet/mobile.
- [x] W0.2 đã duyệt; W1 Dashboard đang triển khai trước các wave sau.

Gate đang chờ: owner review W1 My Orders round-6 baseline trên Blazor/Radzen. Sau khi owner xác nhận browser TEST, khóa visual baseline và commit vertical slice. React không phải gate hiện tại.

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
3. Đọc `.codexrules`, `.github/copilot-instructions.md` và route source.
4. Kiểm tra `git status`, branch và diff chưa commit.
5. Đọc ledger, feedback log và retrofit queue mới nhất.
6. Chọn đúng route `PENDING`/`CHANGES_REQUESTED` theo thứ tự đã duyệt.
7. Không suy luận rằng Figma đã cover đủ route.
8. Không tạo thêm UI Lab/project preview ngoài `gtas_vpp_fe_react` proof-of-concept đã được owner duyệt; không đổi architecture render mode của Blazor.
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
