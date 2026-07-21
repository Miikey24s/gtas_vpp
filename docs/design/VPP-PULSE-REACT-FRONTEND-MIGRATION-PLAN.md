# VPP Pulse — React Frontend Migration Master Plan

> **Trạng thái:** `ACTIVE — REACT TARGET FRONTEND`
>
> **Phiên bản:** `1.2` — 2026-07-21
>
> **Mục tiêu:** Thiết kế và xây dựng đầy đủ frontend React tốt hơn cho GTAS VPP, bảo toàn business invariant, dữ liệu, permission và audit bắt buộc nhưng được quyền tối ưu lại information architecture, route, workflow, API contract và cách trình bày; sau đó cutover có kiểm soát khỏi Blazor/Radzen.
>
> **Execution authority:** file này + source/API/permission hiện hành + browser runtime.
>
> **Historical baseline:** `VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md` giữ các quyết định và feedback đã học từ Blazor, nhưng không còn quyết định thứ tự triển khai React.

---

## 1. Quyết định kiến trúc đã chốt

- React là frontend mục tiêu chính thức.
- Blazor/Radzen được giữ nguyên làm baseline, fallback và nguồn phát hiện capability/nghiệp vụ trong giai đoạn migration; không phải mẫu để React chép lại route, layout hoặc interaction.
- Không dùng Next.js ở giai đoạn này. GTAS VPP là ứng dụng nội bộ, backend ASP.NET Core đã độc lập, không cần SEO, React Server Components hoặc một Node production server riêng.
- Stack chuẩn: React 19, TypeScript, Vite, React Router, TanStack Query/Table, shadcn/ui, Tailwind CSS, React Hook Form, Zod, i18next, Lucide và Recharts.
- DTO/API client phải sinh từ Swagger/OpenAPI bằng `npm run api:generate`; không copy interface C# bằng tay.
- Local development dùng Aspire `AddViteApp`. Production target ưu tiên build static `dist` và phục vụ cùng origin với ASP.NET Core để giảm CORS, đơn giản cookie/auth và rollback.
- React chỉ được thay Blazor khi đạt toàn bộ cutover gates ở Section 16. Gate đo theo capability và outcome nghiệp vụ, không yêu cầu pixel/route parity 1:1.

### 1.1 Modernization, không phải port 1:1

- Được hợp nhất nhiều tab/route cũ thành một workspace rõ hơn hoặc tách một màn hình quá tải thành task flow chuyên biệt.
- Được thay table/card/modal bằng format phù hợp hơn: drawer, full-page flow, command bar, progressive disclosure hoặc data story.
- Được bỏ copy, KPI, cột, action và navigation trùng/không còn giá trị.
- Được bổ sung shortcut, bulk action, saved filter, deep link và workflow mới khi giúp hoàn thành nghiệp vụ tốt hơn.
- Được nâng API/DTO/backend liên quan nếu contract cũ gây overfetch, thiếu paging/filter, không hỗ trợ concurrency, localization hoặc workflow tối ưu; thay đổi phải có test, migration/backward-compatibility khi cần.
- Không được tự ý thay business rule, quyền, immutable history, settlement fact hoặc dữ liệu thật chỉ vì UI mới muốn đơn giản hơn.

## 2. “Full UI” trong plan này có nghĩa gì

Mỗi route và luồng phải có:

- API thật, permission thật và validation đúng nghiệp vụ;
- trạng thái loading, empty, normal, long-data, error, unauthorized, disabled, success và retry khi phù hợp;
- VI/EN cho toàn bộ text hiển thị, enum và feedback do hệ thống sở hữu;
- Light, Dark và Print dùng chung semantic token;
- desktop, tablet và mobile không vỡ layout;
- keyboard, focus, semantic HTML, accessible name và contrast phù hợp;
- action nguy hiểm có xác nhận, mutation chống double-submit và feedback bền vững;
- browser QA, unit/integration test phù hợp và owner review theo wave;
- không placeholder, fake implementation hoặc mock data trong runtime production.

Contract fixture chỉ được dùng trong automated UI test. Runtime review dùng backend và database TEST/isolated thật.

## 3. Nguồn sự thật và thứ tự ưu tiên

Khi có xung đột:

1. Business invariant, authorization và database hiện hành.
2. API contract/Swagger và backend source hiện tại, trừ khi plan mới chủ động nâng contract bằng change-set đã test.
3. Quyết định mới nhất của owner trong file này.
4. Browser runtime React đã được owner duyệt.
5. Product blueprint, Personal Design DNA và Figma.
6. Blazor UI cũ chỉ làm bằng chứng capability/gap, không phải route, workflow hay pixel authority.

Không thay API/database chỉ để che một implementation frontend yếu. Được cải tiến contract/schema khi chứng minh được lợi ích sản phẩm hoặc kỹ thuật, giữ invariant và có migration/test/rollback tương xứng.

### 3.1 Figma Make collaboration boundary

- Figma Make/Opus 4.8 được dùng để audit, thử visual direction và chỉnh React/Tailwind trên branch riêng; không phải nguồn thay thế business rule, API, permission hoặc living plan.
- Root `Guidelines.md` định tuyến context monorepo; standing context React là `gtas_vpp_fe_react/Guidelines.md`; brief đầy đủ và prompt khởi động nằm ở `VPP-PULSE-FIGMA-MAKE-BUILD-BRIEF.md` và `VPP-PULSE-FIGMA-MAKE-STARTER-PROMPT.md`.
- Branch `agents/ui-improvements-font-size-alignment` tại `da02844` là snapshot 27/06/2026, cũ hơn toàn bộ React implementation hiện hành và không được dùng làm baseline.
- Mọi phiên Figma production-code phải fetch latest `origin/Nam`, xác minh `bf2cb10` là ancestor, tạo branch `figma/*`, lập `plan.md` trước, rồi mới sửa slice được owner duyệt.
- Figma Make dạng prototype/GitHub push một chiều chỉ tạo design evidence hoặc repository trung gian; không được ghi đè GTAS source of truth. Integration vào repo này phải qua diff/QA/PR hoặc Codex review.
- Không gửi secret, cookie, token, connection string hoặc dữ liệu production vào attachment/context của Figma.

## 4. Hiện trạng React đã có

| Hạng mục | Trạng thái | Evidence |
|---|---|---|
| Vite/React/TypeScript foundation | IMPLEMENTED | `gtas_vpp_fe_react` |
| Aspire resource | IMPLEMENTED | `frontend-react → GTAS React Preview` |
| Generated OpenAPI client | IMPLEMENTED | `openapi/gtas-vpp.openapi.json`, `src/api/generated` |
| Login | IMPLEMENTED — baseline accepted | API login, inline validation, VI/EN, Light/Dark |
| Protected auth bootstrap | IMPLEMENTED | `/api/Auth/me`, `/api/Auth/me/permissions` |
| App shell | IMPLEMENTED — baseline accepted | responsive sidebar/header/user menu |
| My Orders | IMPLEMENTED — baseline accepted | period + order API, data story, responsive detail |
| Logout | IMPLEMENTED | client session cleanup + redirect |
| Automated QA | GREEN | React check/build, E2E 3 viewport, axe, AppHost build, backend 410 tests (2026-07-21) |

Baseline accepted nghĩa là hướng công nghệ và visual đã được owner chọn; route vẫn có thể được retrofit khi shared foundation hoặc nghiệp vụ liên quan thay đổi.

### 4.1 Quy ước trạng thái

- `TECH_COMPLETE`: implementation và automated gate đã đạt tại thời điểm ghi nhận, nhưng chưa đồng nghĩa owner đã duyệt visual/workflow.
- `OWNER_ACCEPTED`: owner đã duyệt route/capability trên runtime thật; feedback sau vẫn có thể tạo retrofit.
- `OWNER_REVIEW`: đã có implementation để owner kiểm tra, chưa được xem là product-complete.
- `PLANNED`: contract đã được duyệt nhưng chưa triển khai.
- `BLOCKED_CONTRACT`: frontend không được tạo workaround; phải nâng API/schema/test trước.
- `DEFERRED`: vẫn thuộc blueprint nhưng không nằm trong release slice hiện tại; phải ghi rõ điều kiện kích hoạt lại.

Không dùng từ `COMPLETE` đơn lẻ cho một wave vẫn còn `OWNER_REVIEW`, thiếu runtime evidence hoặc thiếu capability đã được master plan giữ lại.

## 5. Kiến trúc frontend chuẩn

### 5.1 Folder ownership

```text
src/
├── api/                 generated client, interceptors, error normalization
├── app/                 providers, router, global boundaries
├── auth/                session, current user, permission guards
├── components/
│   ├── ui/              shadcn primitives do repository sở hữu
│   ├── app/             shell, navigation, notification, brand
│   └── shared/          page header, state, data table, filters, dialogs
├── features/
│   ├── account/
│   ├── orders/
│   ├── management/
│   ├── periods/
│   ├── library/
│   ├── access/
│   └── reports/
├── hooks/               reusable behavior, không chứa nghiệp vụ ẩn
├── lib/                 i18n, format, theme, utilities
└── test/                shared test setup/fixtures
```

Quy tắc:

- Page chỉ compose feature/component; không chứa API orchestration dài.
- Server state dùng TanStack Query; form state dùng React Hook Form; UI state nhỏ dùng React state/context.
- Không thêm Redux/Zustand trước khi có state liên tính năng thực sự chứng minh context không đủ.
- Query key nằm gần feature, có factory ổn định; mutation thành công phải invalidate/update đúng cache.
- Route nghiệp vụ lazy-load; shared shell và auth bootstrap giữ trong initial path.
- Error Boundary đặt ở app shell và route boundary; lỗi query hiển thị state retry, không làm trắng trang.

### 5.2 API và error contract

- Generated client là nguồn type duy nhất.
- Request tự gắn language và auth transport; không truyền token thủ công trong component.
- Backend error code được map sang i18n key an toàn; không render raw JSON, stack trace hoặc `OperationInvalid`.
- 401: dọn session và quay về login với return URL nội bộ.
- 403: render permission state, không giả làm empty data.
- 409/concurrency: giải thích dữ liệu đã thay đổi và cung cấp reload/retry có kiểm soát.
- Mutation có pending lock, idempotency/concurrency contract và success feedback phù hợp.

### 5.3 Authentication target

- POC hiện tại dùng bearer token trong `sessionStorage` chỉ cho local/TEST.
- Production cutover bị chặn cho đến khi có same-origin BFF hoặc secure HttpOnly cookie session.
- Cookie-based mutation phải có CSRF/antiforgery protection; API vẫn trả 401/403, không redirect HTML.
- React route guard chỉ cải thiện UX. Backend `[Authorize]` và action permission luôn là security authority.
- Không giải mã JWT ở client để suy luận quyền; dùng `/api/Auth/me/permissions`.

### 5.4 Routing contract

- URL phải mô tả workspace/task, không giữ query `tab=0/1/...` làm canonical React route và không bắt buộc ánh xạ 1:1 với trang Blazor.
- Legacy Blazor URL được redirect sang React route tương ứng trong giai đoạn cutover.
- Filter, sort, page và scope quan trọng phải nằm trong URL search params để refresh/back/share không mất context.
- Form chưa lưu phải có navigation guard và explicit discard confirmation.

## 6. Design system React

### 6.1 Visual direction

Hướng mới là **Operational Clarity**:

- nhẹ, trung tính, nhiều khoảng thở có mục đích;
- typography và alignment tạo hierarchy, không dựa vào nhiều card/màu;
- một primary accent trên mỗi vùng thao tác;
- hairline, surface và spacing thay cho shadow nặng;
- PPJ/Personal DNA chỉ xuất hiện tinh tế qua nhịp layout, typography, threadline và cyan/teal accent;
- không glassmorphism nặng, gradient trang trí, card lồng card hoặc animation phô diễn.
- Apple, ChatGPT, Notion, Linear và Figma chỉ là nguồn tham khảo theo từng vấn đề; không có sản phẩm nào là art direction độc quyền hoặc mẫu để sao chép.
- Ưu tiên hình học vuông, radius nhỏ `4–6px`, hitbox đầy đủ và interaction grammar nhất quán theo feedback mới nhất của owner.

### 6.2 Foundations

- Font chuẩn React: Geist Variable; số liệu dùng tabular numerals.
- Semantic color tokens: background, surface, foreground, muted, border, primary, success, warning, danger, info.
- Light là baseline; Dark thay token, không thay layout.
- Print là mode code thật: trắng/đen, bỏ navigation/action, giữ hierarchy, table header và page-break hợp lý.
- Radius nhỏ/vừa, nhất quán; không dùng pill cho mọi control.
- Motion dùng ba lớp: CSS/Tailwind cho micro-interaction; Motion for React cho enter/exit và layout; React Router View Transitions cho chuyển route.
- Nhịp mặc định 120/180/260 ms; chỉ giải thích hover/state/change, không animation trang trí; toàn cục tôn trọng `prefers-reduced-motion` và có Playwright gate riêng.
- Không dùng React Canary `<ViewTransition>`; chỉ dùng API stable của React Router/browser và Motion stable.
- Focus ring rõ và thống nhất; target tương tác tối thiểu hợp lý trên touch.

### 6.4 Motion contract (animation dùng có chủ đích)

Motion là **feedback cho trạng thái và quan hệ không gian**, không phải hiệu ứng trang trí. Mọi animation mới phải trả lời được một trong ba câu hỏi: người dùng vừa làm gì, dữ liệu vừa thay đổi gì, hoặc giao diện đang chuyển sang vùng nào.

- **Micro (CSS/Tailwind, 120 ms):** hover/focus/pressed, màu viền, opacity, icon rotation nhỏ. Chỉ animate `transform`, `opacity`, màu hoặc shadow; không animate `width/height/top/left` gây layout thrash.
- **Component (Motion, 180 ms):** enter/exit của empty/error/loading, drawer/dialog, list reorder và thay đổi layout cục bộ. Dùng `m` với `LazyMotion`, `AnimatePresence` cho unmount và `layout`/`layoutId` khi cần giữ quan hệ giữa hai trạng thái.
- **Route (View Transition, 260 ms):** chuyển giữa các route hoặc vùng có cùng ngữ cảnh. Chỉ dùng progressive enhancement; browser không hỗ trợ vẫn phải render bình thường.
- **Accessibility:** `MotionConfig reducedMotion="user"`, CSS `prefers-reduced-motion: reduce`, và Playwright chạy một vòng reduced-motion. Khi giảm chuyển động, giữ feedback bằng màu, focus, thứ tự nội dung hoặc cross-fade rất ngắn; không làm mất thông tin.
- **Data-heavy screens:** không stagger từng dòng bảng, không animate KPI mỗi lần refetch, không dùng parallax/auto-loop. Chỉ animate vùng vừa thay đổi và giữ input/focus ổn định.

Nguồn tham khảo chính thức: [Motion for React](https://motion.dev/docs/react), [Motion layout animation](https://motion.dev/docs/react-layout-animations), [MDN View Transition API](https://developer.mozilla.org/en-US/docs/Web/API/View_Transition_API), [MDN prefers-reduced-motion](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/%40media/prefers-reduced-motion). Không thêm Rive/Lottie/GSAP hoặc MCP animation riêng trước khi có nhu cầu sản phẩm cụ thể.

### 6.3 Shared component backlog

| Primitive | Trách nhiệm |
|---|---|
| `PageHeader` | eyebrow, title, description ngắn, primary/secondary action |
| `RouteState` | loading, empty, error, forbidden, not-found, offline |
| `StatusBadge` | enum → icon/text/color, không màu-only |
| `MetricStrip` | 2–5 evidence metric, không card KPI rời rạc vô nghĩa |
| `DataTable` | server paging/sort/filter, column profile, mobile representation |
| `FilterBar` | search, structured filters, active chips, reset |
| `EntityDrawer` | quick view/short edit, focus trap và unsaved guard |
| `ConfirmAction` | destructive/high-impact confirmation |
| `FormField` | label/help/error slot cố định, không layout jump |
| `NotificationCenter` | unread/read-all, localized payload, resilient retry |
| `PermissionGate` | hide/disable/forbidden theo contract rõ ràng |
| `PrintHeader` | title, scope, generated-at, filters và source metadata |

## 7. Content, VI/EN và terminology

- Mọi UI-owned text phải đi qua i18next; không hard-code English/Vietnamese trong component.
- Dùng terminology thống nhất: `đơn`, `mặt hàng`, `số lượng`, `đơn vị tính`, `kỳ đặt hàng`, `đơn bổ sung`, `phòng ban`, `nhà cung cấp`, `bảng giá`.
- Stable enum/error/status code được dịch ở presentation layer.
- Không duy trì hai database VI/EN.
- Master data cần song ngữ dùng một entity gốc + translation contract (`languageCode`, `name`, `description`) hoặc field song ngữ đã được backend duyệt; không machine-translate âm thầm trong UI.
- Dữ liệu chưa có bản dịch hiển thị bản gốc và optional translated field rõ nguồn; không giả vờ đã dịch chính xác.
- Print/export phải dùng cùng ngôn ngữ và filter/scope đang chọn.

### 7.1 Contract dữ liệu nghiệp vụ VI/EN

VI/EN gồm hai lớp độc lập nhưng phải phối hợp:

1. **UI-owned content:** label, heading, validation, enum, error và feedback dùng i18next.
2. **Business data:** tên/mô tả mặt hàng, danh mục, phòng ban, nhà cung cấp và lookup được lưu trong database, không thể dịch chỉ bằng resource frontend.

Kiến trúc đã chốt:

- dùng một database và giữ entity gốc làm canonical/original data; không tạo database VI và EN riêng;
- entity cần dịch dùng translation table có foreign key thật, tối thiểu gồm `LanguageCode`, `Name`, `Description`, `TranslationStatus`, `Source`, audit fields và unique key `(EntityId, LanguageCode)`;
- ưu tiên table typed theo aggregate như `VppItemTranslations`, `VppCategoryTranslations`, `DepartmentTranslations`, `SupplierTranslations`; không dùng một bảng EAV generic không có foreign key nghiệp vụ;
- ngôn ngữ request lấy từ request culture/`Accept-Language`, chỉ chấp nhận `vi` và `en` trong release đầu;
- fallback bắt buộc: requested language → original language → original value; không trả chuỗi rỗng và không giả vờ machine translation là dữ liệu đã duyệt;
- read DTO trả `displayName`/`displayDescription`, original value, resolved language và fallback flag; collection `translations` chỉ trả ở contract quản trị cần chỉnh sửa;
- search phải khớp code, original text và translation; sort/export/print dùng resolved display text theo ngôn ngữ đang chọn;
- bản dịch AI chỉ được lưu dưới trạng thái draft và cần người có quyền library manage duyệt; không tự ghi đè original data;
- migration phải backfill ngôn ngữ gốc, idempotent trên TEST/LIVE học tập và có test unique/FK/fallback/search/sort;
- form quản trị hiển thị original cạnh VI/EN translation, trạng thái bản dịch và cảnh báo fallback; người dùng nghiệp vụ bình thường chỉ thấy `displayName` phù hợp.

### 7.2 Dữ liệu lịch sử và bản in

- **Reference/master data** (mặt hàng, danh mục, lookup, phòng ban, nhà cung cấp, bảng giá) là dữ liệu mutable nên đọc theo ngôn ngữ request và có thể fallback.
- **Order/settlement/history snapshot** là bằng chứng bất biến theo thời điểm nghiệp vụ; không tự đổi tên lịch sử chỉ vì người quản trị sửa bản dịch hiện tại. Bản in và export lịch sử giữ snapshot đã chốt, còn các màn hình chọn reference data dùng `displayName` theo VI/EN.
- Khi cần đối chiếu, UI có thể hiển thị thêm mã định danh ổn định (`VppCode`, department code, supplier short name), không dùng dịch ngược để thay thế evidence gốc.

Không xem VI/EN hoàn tất chỉ vì frontend đổi được label. Gate hoàn tất cần schema + API + generated client + CRUD quản trị + search/export/print + automated test.

## 8. Data storytelling contract

Mỗi màn hình dữ liệu theo thứ tự:

```text
Takeaway → Evidence → Context/Comparison → Next action → Exact detail
```

Quy tắc:

- Không lặp cùng status/action ở heading, badge, description và button.
- Dashboard chỉ trả lời 1–3 câu hỏi vận hành; detail/table trả lời số liệu chính xác.
- Một chart chỉ trả lời một câu hỏi; luôn có title, unit, scope, as-of time và accessible summary.
- Dùng table cho lookup/audit/exact comparison; chart không thay thế table nghiệp vụ.
- Side drawer cho quick detail hoặc edit ngắn; full page cho create/edit nhiều bước, settlement, report drill-down.
- Server paging mặc định cho library/permission/report lớn; virtualization chỉ thêm sau khi đo performance.

## 9. React route map mục tiêu

Route map dưới đây là target capability ban đầu, không phải bản sao cố định. Trong lúc triển khai có thể hợp nhất/tách route nếu task flow tốt hơn; mọi thay đổi phải cập nhật ledger và legacy redirect.

### 9.1 Anonymous và account

| React route | Contract | Status |
|---|---|---|
| `/login` | `/api/Auth/login` | IMPLEMENTED — baseline accepted |
| `/register` | `/api/account/register` | IMPLEMENTED — OWNER_REVIEW |
| `/registration/pending` | registration result/status contract | PLANNED — reference, submitted time, next step, email fallback |
| `/account/confirm-email` | `/api/account/confirm-email` | IMPLEMENTED — OWNER_REVIEW |
| `/forgot-password` | `/api/account/password-recovery` | IMPLEMENTED — OWNER_REVIEW |
| `/reset-password` | `/api/account/password-reset` | IMPLEMENTED — OWNER_REVIEW |
| `/change-password` | `/api/account/password-change` | IMPLEMENTED — OWNER_REVIEW |
| `/logout` | `/api/Auth/logout` | IMPLEMENTED |
| `/not-found`, `/error` | client/system state | IMPLEMENTED — AUTOMATED_QA_PASS |

### 9.2 Employee workspace

| React route | Permission | Status |
|---|---|---|
| `/app/orders` | `REQUEST_VIEW_OWN` | IMPLEMENTED — baseline accepted |
| `/app/orders/history` | `REQUEST_VIEW_OWN` | MERGED — `/app/orders` overview + per-order immutable history |
| `/app/orders/new` | `REQUEST_CREATE` + catalog lookup | IMPLEMENTED — OWNER_REVIEW |
| `/app/orders/:orderId` | `REQUEST_VIEW_OWN` hoặc scoped view | IMPLEMENTED — OWNER_REVIEW |
| `/app/orders/:orderId/edit` | `REQUEST_UPDATE_OWN` | IMPLEMENTED — OWNER_REVIEW |
| `/app/orders/:orderId/history` | scoped view | IMPLEMENTED — OWNER_REVIEW |
| `/app/catalog` | `REQUEST_CATALOG_VIEW` | IMPLEMENTED — OWNER_REVIEW |

Create route phải bao phủ: đơn thường, copy kỳ trước, edit, bổ sung, validation quantity, previous-items, submit, conflict và deadline closed.

### 9.3 Department/company management

| React route | Permission | Status |
|---|---|---|
| `/app/management/department` | `REQUEST_VIEW_DEPARTMENT` | IMPLEMENTED — OWNER_REVIEW |
| `/app/management/company` | `REQUEST_VIEW_ALL` | IMPLEMENTED — OWNER_REVIEW |
| `/app/management/supplements` | `REQUEST_APPROVE` / `REQUEST_REJECT` | IMPLEMENTED — OWNER_REVIEW |
| `/app/management/orders/:orderId` | scoped view | MERGED — canonical `/app/orders/:orderId?from=...` |

### 9.4 Period/procurement

| React route | Permission | Status |
|---|---|---|
| `/app/periods` | `PERIOD_SETTLE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/periods/:year/:month` | `PERIOD_SETTLE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/periods/:year/:month/preview` | `PERIOD_SETTLE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/periods/:year/:month/settlement` | `PERIOD_SETTLE` | IMPLEMENTED — OWNER_REVIEW |

Phải bao phủ preview, exception, confirm, correction, immutable history và reconciliation evidence.

### 9.5 Master data / Library

| React route | Permission | Status |
|---|---|---|
| `/app/library/classes` | `LIBRARY_VIEW/MANAGE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/library/categories` | `LIBRARY_VIEW/MANAGE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/library/items` | `LIBRARY_VIEW/MANAGE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/library/suppliers` | `LIBRARY_VIEW/MANAGE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/library/departments` | `LIBRARY_VIEW/MANAGE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/library/price-lists` | `LIBRARY_VIEW/MANAGE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/library/prices` | `LIBRARY_VIEW/MANAGE` | IMPLEMENTED — OWNER_REVIEW |

### 9.6 Access control

| React route | Permission | Status |
|---|---|---|
| `/app/access/users` | `PERMISSION_VIEW/MANAGE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/access/groups` | `PERMISSION_VIEW/MANAGE` | IMPLEMENTED — OWNER_REVIEW |
| `/app/access/permissions` | `PERMISSION_VIEW/MANAGE` | IMPLEMENTED — OWNER_REVIEW |

### 9.7 Reports và intelligence

| React route | Permission | Status |
|---|---|---|
| `/app/reports` | `REPORT_VIEW_OWN/DEPARTMENT/ALL` | IMPLEMENTED — OWNER_REVIEW |
| `/app/reports/insights` | report scope + feature config | IMPLEMENTED — OWNER_REVIEW |

Report phải bao phủ scope, filter, summary, trend/status, department/product, exact table, export XLSX, print và AI-disabled/error/evidence states.

R7 dùng một `ReportWorkspace` chung cho hai route để không lặp query/filter/data story. `/app/reports` ưu tiên số liệu và bằng chứng; `/app/reports/insights` mở cùng workspace với vùng phân tích được ưu tiên. Mỗi chart chỉ trả lời một câu hỏi, luôn đi kèm số liệu exact-data và thời điểm tạo báo cáo.

### 9.8 Notification và system workspace

| React route/state | Contract | Status |
|---|---|---|
| Header notification popover | unread, recent items, read/read-all, realtime/polling | TECH_COMPLETE — OWNER_REVIEW |
| `/app/notifications` | durable history, cursor/load-more, read/expired/deep-link, retry | PLANNED |
| Offline banner | browser/network state + safe retry | PLANNED |
| Reconnect/session-expired dialog | preserve safe context, login return URL, no white screen | PLANNED |
| `/forbidden` | permission boundary + safe next action | PLANNED |
| `/error` | safe message, correlation ID, retry/support context | TECH_COMPLETE — cần chuẩn hóa contract |

### 9.9 Cross-cutting capability ledger

Các capability sau không được ẩn dưới một route status tổng quát:

- **Data table:** default columns, column picker, filter, sort, paging, long-data, empty-filter, mobile representation và detail-on-demand cho từng workspace.
- **Export/print/email:** loading, row/file limit, formula-safe XLSX, selected language/scope/filter, retry, sandbox/unavailable và error state.
- **Persona/permission:** landing focus, visible data, hidden scope và allowed mutation cho Employee, Department Approver, Procurement/Period Admin và System Admin.
- **Admin safety:** session invalidation, password reset, last-admin protection, membership/primary department impact và durable audit feedback.
- **Legacy cutover:** một ma trận URL Blazor cũ → canonical React route, gồm query/tab mapping, deep-link refresh và fallback an toàn.
- **AI:** deterministic evidence trước; Report Insight hiện hành không đại diện cho toàn bộ Anomaly Review, Ask the Report và Governance.

## 10. Implementation waves

| Wave | Phạm vi | Gate hoàn tất |
|---|---|---|
| R0 | Re-baseline, architecture, tokens, shared contracts | TECH_COMPLETE — plan authority và foundation đã khóa; owner review tiếp tục theo route |
| R1 | Auth/account + shell baseline | TECH_COMPLETE — automated QA pass; Pending Approval và resilience được chuyển sang R10 |
| R2 | Employee order journey | TECH_COMPLETE — automated QA pass; OWNER_REVIEW |
| R3 | Department/company management | TECH_COMPLETE — automated QA pass; OWNER_REVIEW và data-story retrofit |
| R4 | Period/procurement/settlement | TECH_COMPLETE — automated QA pass; OWNER_REVIEW và cross-route acceptance audit |
| R5 | Library/master data | TECH_COMPLETE — automated QA pass; business-data VI/EN được chuyển sang R9 |
| R6 | Access control | TECH_COMPLETE — automated QA pass; OWNER_REVIEW và admin-safety audit |
| R7 | Reports/insight/print/export baseline | TECH_COMPLETE — automated QA pass; export/email edge states và advanced AI chưa hoàn tất |
| R8 | Technical hardening/cutover package | TECH_READY — local production gate đạt; owner review, legacy redirect matrix và production activation còn mở |
| R9 | Business data VI/EN | IN_PROGRESS — typed schema/API/admin UI/search/display đã GREEN; export/print theo snapshot và runtime owner review còn mở |
| R10 | Account/system completion | PLANNED — Pending Approval, durable inbox route, offline/reconnect/session/error contract |
| R11 | Cross-route product hardening | PLANNED — data-table profiles, persona matrix, export/email states, admin safety, redirects |
| R12 | Advanced intelligence | PLANNED — anomaly review, Ask the Report, governance, budget/retention/kill switch |

Thứ tự trong wave ưu tiên một end-to-end journey hoạt động trước khi mở rộng breadth. Owner review theo checkpoint; feedback shared primitive phải được retrofit các route đã làm.

## 11. Route execution loop

Mỗi route đi qua:

1. Audit capability từ Blazor, API, DTO, permission, data và test; liệt kê cả pain point cần bỏ.
2. Chọn task flow React tối ưu; không mặc định giữ route/layout cũ.
3. Ghi invariant, outcome, contract/state/action và modernization decision vào ledger.
4. Xác định takeaway và exact data cần hiển thị.
5. Nâng API/DTO có kiểm soát nếu contract hiện tại cản trở outcome.
6. Bổ sung shared primitive tối thiểu cần thiết.
7. Implement bằng generated client, real permission và i18n.
8. Unit/component test cho mapper/form/guard.
9. Playwright: normal + state quan trọng + 3 viewport + console/network + axe.
10. TEST/isolated runtime smoke với tài khoản đúng role.
11. Cập nhật decision/learning/retrofit queue.
12. Owner review theo wave và commit logic nhỏ.

Không dừng ở mockup, screenshot hoặc skeleton. Nếu route chưa có API đủ, ghi `BLOCKED_CONTRACT`, sửa backend contract và test trước khi tiếp tục.

## 12. QA matrix

### Automated gates

```powershell
cd gtas_vpp_fe_react
npm run api:generate
npm run format:check
npm run check
npm run test:e2e
npm audit --audit-level=high

cd ..
dotnet build MyAspire.AppHost\MyAspire.AppHost.csproj -c Release
dotnet test gtas_vpp_be.Tests\gtas_vpp_be.Tests.csproj -c Release
```

### Browser matrix

- Mobile `390×844`.
- Tablet `768×1024`.
- Desktop `1366×768`, baseline review `1920×1080`.
- VI/EN, Light/Dark; Print ở route có tài liệu/bảng/report.
- Keyboard-only, focus return cho dialog/menu/drawer.
- Không horizontal page overflow.
- Axe không có `critical`/`serious`; manual keyboard/contrast/meaning review vẫn bắt buộc.
- Không unexpected console error hoặc failed API.

### Mutation safety

- Shared TEST database không chạy destructive/mutating E2E tùy tiện.
- Isolated mutation yêu cầu:

```powershell
$env:GTAS_E2E_ISOLATED = '1'
$env:GTAS_E2E_MUTATION_OPT_IN = 'I_UNDERSTAND_THIS_MUTATES_QA_DATA'
```

## 13. Performance budgets

- Route nghiệp vụ phải lazy-load; không import toàn bộ chart/table/editor vào initial login bundle.
- Không request lặp do query key hoặc focus refetch không chủ đích.
- Table lớn dùng server paging; virtualization chỉ sau đo đạc.
- Không render toàn bộ hidden tab/page data.
- Production build không có warning; bundle growth đáng kể phải được giải thích.
- Lighthouse/accessibility/performance chỉ là chỉ báo; browser task completion và measured regression mới quyết định.

## 14. Data/fixture strategy

- Primary review dùng database TEST với `MigrateAndDemo` khi cần dữ liệu thực tế từ workbook.
- Demo seed idempotent, bind owner thật và giữ demo identities disabled.
- Automated read-only states có thể dùng typed contract fixture.
- Mutation/concurrency/settlement dùng disposable isolated fixture.
- Không commit credential, token, cookie, connection string hoặc browser storage state.

## 15. Git và review protocol

- Một commit logic cho một shared primitive hoặc vertical slice ổn định.
- Không trộn thay đổi Blazor không liên quan vào commit React.
- Không commit screenshot/trace/report test mặc định.
- Trước commit: `git diff --check`, review diff, status và test phù hợp.
- Trong giai đoạn owner yêu cầu build full trước review, agent được tiếp tục qua route trong cùng wave; không được tự cutover/deploy/xóa Blazor.
- Feedback mới cập nhật file này và retrofit queue trước khi sửa tiếp.

## 16. Cutover và rollback gates

Chỉ chuyển production/default frontend khi:

- toàn bộ capability mục tiêu đạt outcome, invariant, permission và state coverage; không cần route/pixel parity 1:1;
- auth chuyển khỏi browser token storage sang BFF/HttpOnly cookie + antiforgery;
- legacy URL redirect và deep-link refresh hoạt động;
- React production assets được host bằng cấu hình production thật, không dùng `vite preview`;
- core E2E journeys pass trên isolated environment;
- report/export/print và VI/EN reconcile dữ liệu;
- accessibility/security/dependency/build gates green;
- DigitalOcean smoke, health, static fallback và cache headers được kiểm chứng;
- có config/rollback để quay về Blazor mà không rollback database;
- luận văn, diagram, screenshot và handoff mô tả đúng frontend thực tế.

Blazor chỉ được xóa hoặc archive sau khi React đã chạy ổn định và owner yêu cầu riêng. Migration frontend không được rewrite database history.

## 17. Decision và learning log

| Date | Decision/learning | Hệ quả |
|---|---|---|
| 2026-07-20 | Owner chọn React sau POC Login → Shell → My Orders | React trở thành target; lập lại master plan từ đầu |
| 2026-07-20 | Không dùng Next.js | Vite SPA + ASP.NET Core API/static hosting; giảm production moving parts |
| 2026-07-20 | React visual nhẹ và rõ hơn Blazor/Radzen | Operational hierarchy trở thành visual baseline; không khóa vào một nguồn tham khảo duy nhất |
| 2026-07-20 | Không review từng route trước khi agent tiếp tục | Build full theo wave; owner review checkpoint và retrofit sau |
| 2026-07-20 | Blazor không bị xóa trong migration | Có behavior baseline và rollback an toàn |
| 2026-07-20 | Browser token chỉ là POC | Cookie/BFF + antiforgery là cutover blocker |
| 2026-07-20 | React là modernization, không port 1:1 | Được thiết kế lại route/workflow/API khi tối ưu hơn; chỉ invariant và outcome bắt buộc giữ |
| 2026-07-20 | Motion là UX feedback, không phải trang trí | CSS/Tailwind + Motion stable + React Router View Transitions; reduced-motion là quality gate |
| 2026-07-20 | Không cài Motion+ MCP trả phí | Tài liệu chính thức + dependency `motion` + Playwright hiện tại đủ cho implementation và QA |
| 2026-07-20 | Notification contract phải typed từ backend | Thêm response DTO/Swagger annotation, sinh lại client; React không giữ interface thủ công |
| 2026-07-20 | Realtime chỉ bật qua config | Aspire bật SignalR; môi trường không bật vẫn có polling 60 giây và thao tác đầy đủ |
| 2026-07-20 | Catalog dùng server paging và hai representation theo viewport | Desktop ưu tiên bảng exact-data; mobile dùng card, cùng typed OpenAPI contract và URL-independent query state |
| 2026-07-20 | Tạo đơn là một task workspace, không dùng wizard nhiều bước | Danh mục và giỏ nhu cầu cùng màn hình; summary/action luôn thấy, backend period flags quyết định regular/copy/supplement |
| 2026-07-20 | Draft tạo đơn lưu cục bộ theo user/kỳ/mode | Regular và supplement không ghi đè nhau; submit dùng idempotency theo payload và navigation guard giải thích bản nháp vẫn còn |
| 2026-07-20 | Sonner rich-color mặc định thiếu contrast ở success toast | Override semantic success foreground; axe gate giữ nguyên, không loại trừ rule màu |
| 2026-07-20 | Không tạo route lịch sử tổng trùng dữ liệu | `/app/orders` làm overview theo kỳ; lịch sử bất biến nằm ở từng đơn để giữ context và giảm lặp |
| 2026-07-20 | Update/cancel dùng revision contract của backend | `rowVersion` + idempotency bắt buộc; edit tạo revision mới, cancel giữ evidence thay vì sửa/xóa lịch sử |
| 2026-07-20 | R2 automated gate đạt 5 Playwright journeys | Bao phủ 3 viewport, account, reduced motion, regular/copy, supplement, edit revision, cancel, axe và console |
| 2026-07-20 | Management detail dùng canonical order route + safe source context | Không nhân đôi màn hình; back-navigation trở về department/company/supplement queue đúng nguồn |
| 2026-07-20 | Pending supplement listing chấp nhận APPROVE hoặc REJECT | Sửa backend guard bị lệch: reject-only role có thể xem hàng chờ, nhưng từng mutation vẫn có policy riêng |
| 2026-07-20 | Department scope không nhận filter phòng ban từ browser | Backend luôn ghi đè bằng claim hiện tại; company scope mới cho phép lọc department code |
| 2026-07-20 | Settlement history phải dùng dữ liệu bất biến thật | Bổ sung typed endpoint `revisions/{year}/{month}`; React hiển thị toàn bộ revision, không suy diễn lịch sử từ bản hiện hành |
| 2026-07-20 | Supplier exception là phần của preview, không phải form CRUD rời | Chọn quote chính trước; chỉ hiện editor cho `missingVppIds`, backend resolve price-as-of và blocker trước khi cho xác nhận |
| 2026-07-20 | Không dùng HTTP 404 cho trạng thái “chưa có settlement” | `current/{year}/{month}` trả 204 No Content; đây là trạng thái bình thường và không làm bẩn browser console |
| 2026-07-20 | R4 automated gate bao phủ settlement lifecycle | E2E xác nhận lần đầu, snapshot, quote thiếu độ phủ, supplier exception, four-eyes correction, revision history, axe và console/network gate |
| 2026-07-20 | shadcn MCP được kiểm tra trực tiếp trước R5 | Registry `@shadcn` hoạt động; Data Table/Sheet/Dialog dùng pattern registry nhưng code vẫn thuộc repository và được QA trên route thật |
| 2026-07-20 | Master data dùng shared workspace, price book dùng task flow chuyên biệt | Class/category/supplier/department chia sẻ list-form-status; catalog/price list/price mapping giữ typed contract và state nghiệp vụ riêng |
| 2026-07-20 | Không cho browser ghép Dynamic LINQ filter từ ô tìm kiếm | Bổ sung `search` an toàn cho price-list API; LINQ xử lý code/name/supplier/contract phía server |
| 2026-07-20 | Price mapping cần archive và restore đối xứng | Bổ sung typed PATCH `/api/VPPPrice/{id}/deleted`; fix readback để dữ liệu vừa archive vẫn trả DTO hợp lệ |
| 2026-07-20 | Price admin luôn có một price-list context cụ thể | Không hiển thị lựa chọn “tất cả bảng giá” sai nghĩa; mutation chỉ bật với bảng giá Draft đã chọn |
| 2026-07-20 | R5 automated gate bao phủ master-data-to-publish lifecycle | E2E tạo class, item, price book, item price, publish, reduced-motion, axe, console/network; backend service tests kiểm tra search và restore |
| 2026-07-20 | R6 tách ba workspace thay vì một lưới phân quyền tổng hợp | `users` xử lý account lifecycle/membership, `groups` giải thích bốn canonical persona bất biến, `permissions` chỉ quản trị UI visibility/enable trong role ceiling; action grant vẫn read-only theo backend matrix |
| 2026-07-20 | R6 dùng switch/tabs/table hiện có sau khi kiểm tra shadcn MCP | Không thêm UI package hoặc animation dependency; mutation dùng Dialog/Sheet, state transition dùng motion foundation và reduced-motion gate hiện có |
| 2026-07-20 | Swagger schema ID chỉ phân biệt hai DTO Auth trùng tên | Giữ tên generated contract ổn định cho phần còn lại, dùng `AuthPermissionGroupResDTO`/`AuthUserGroupMembershipResDTO` để typed client không va chạm |
| 2026-07-20 | Backend trả `AdministrationMode` và `CanConfigure` cho component permission | React không lặp lại role ceiling/action-matrix rule; UI chỉ bật switch khi backend xác nhận mapping có thể cấu hình |
| 2026-07-20 | R6 automated gate đạt 9 Playwright journeys toàn bộ | Luồng mới bao phủ activate account, canonical role overview, component toggle, reduced-motion, axe, console/network; backend đạt 401 tests |
| 2026-07-20 | R7 dùng Recharts qua shadcn Chart foundation đã kiểm tra MCP | Giữ Recharts 3.9.2 hiện có; area/bar chart dùng accessibility layer, `ResponsiveContainer` và animation `auto` để tôn trọng reduced-motion; không thêm chart/animation package khác |
| 2026-07-20 | AI insight chỉ chạy theo yêu cầu và không giữ key ở browser | React gọi backend typed endpoint; backend tự chọn provider hoặc rules fallback. UI luôn hiện source, model, thời điểm và bằng chứng từ cùng bộ lọc báo cáo, không trình bày fallback như kết luận AI |
| 2026-07-20 | Báo cáo là một workspace chia sẻ cho số liệu và insight | Hai route dùng chung filter/query để số liệu, XLSX, print và insight không lệch scope; trend trả lời biến động theo kỳ, status trả lời vị trí trong workflow, department trả lời nơi tạo giá trị, top items giữ bảng exact-data |
| 2026-07-20 | R7 automated gate đạt 10 Playwright journeys toàn bộ | Report journey bao phủ permission scope, filter, reconciliation, chart/table, XLSX, print, VI/EN, AI idle/error/rules/AI, reduced-motion, axe và console/network; backend đạt 401 tests |
| 2026-07-20 | Motion research trước R8 | Giữ `motion@12.42.2` + CSS + React Router/browser View Transitions; không thêm package/MCP animation. Áp dụng motion contract 3 lớp, reduced-motion và QA bằng Playwright/axe |
| 2026-07-20 | Route motion dùng transform-only | AppShell có chuyển route nhẹ bằng `y` transform; không fade toàn bộ main để không làm sai contrast trong lúc axe quét. E2E mutation serialize 1 worker vì fixture dùng chung và cần deterministic |
| 2026-07-20 | Production chạy React và Blazor song song | React static container ở loopback `5100` là public target; Blazor giữ healthy ở `5000`. Nginx có config React và Blazor, `switch-frontend.sh` đổi target và tự restore config trước nếu smoke fail; không rollback database |
| 2026-07-20 | CI/CD phát hành ba immutable image | Workflow verify .NET + React check/E2E + Nginx syntax, sau đó build backend, Blazor fallback và React target. Public smoke kiểm tra React production entry, deep-link và immutable cache trước khi ghi deploy state |
| 2026-07-20 | Radix phải import theo từng primitive, không dùng umbrella package | Giảm shared UI chunk khoảng `116 KiB` xuống `47 KiB` gzip; thêm gate `140 KiB/chunk`, `30 KiB CSS` và `550 KiB tổng JS/CSS`, build hiện đạt `506.1 KiB` gzip |
| 2026-07-20 | Không fade nội dung có chữ trong surface/toast | Tất cả entrance motion đọc được dùng transform-only; disabled control giữ contrast đầy đủ. Axe không còn bắt trạng thái giữa transition |
| 2026-07-20 | R8 automated cutover package đạt local production gate | Docker image build sạch, container healthy, React deep-link/cache smoke pass, bốn Nginx config pass syntax, Actionlint/Compose/Bash pass, React E2E `10/10`, backend `405/405`; production vẫn cần owner phê duyệt và kích hoạt |
| 2026-07-21 | Owner duyệt đối chiếu lại plan React với Blazor baseline/full blueprint | Không coi R0–R8 là product-complete khi còn owner review; khôi phục data VI/EN, Pending Approval, durable inbox, resilience, table/export/persona/admin/redirect và advanced AI vào execution ledger |
| 2026-07-21 | Dữ liệu nghiệp vụ song ngữ dùng một DB + typed translation tables | Original data được bảo toàn; API resolve theo request culture, có fallback/search/audit và UI quản trị VI/EN; không dùng hai DB hoặc machine translation âm thầm |
| 2026-07-21 | R9 dùng typed translation tables có trạng thái Approved/Draft | Chỉ bản dịch Approved xuất hiện ở luồng vận hành; owner/library manager duyệt trước, AI draft không tự ghi đè dữ liệu gốc |
| 2026-07-21 | Tìm kiếm master data phải khớp cả original và bản dịch đã duyệt | Paging/search server-side dùng cùng language contract; thêm regression test tìm supplier bằng tên EN khi request culture là EN |
| 2026-07-21 | Lịch sử đơn/settlement giữ snapshot bất biến | Đổi bản dịch reference data không rewrite evidence; print/export lịch sử phải dùng snapshot, còn catalog/library dùng display text đã resolve |
| 2026-07-21 | Figma Make phải re-baseline từ latest `Nam` | Bỏ branch/chat Figma cũ; dùng `Guidelines.md` + plan-first + branch `figma/*`, không push trực tiếp hoặc xem prototype là source authority |
| 2026-07-21 | Bỏ art direction Apple-only | Dùng Operational Clarity; học theo từng pattern tốt từ nhiều nguồn nhưng quyết định cuối dựa trên owner feedback, token và browser runtime GTAS |

## 18. Immediate execution queue

1. **R9 — Business data VI/EN:** migration, typed translation contract, language resolver, localized catalog/library API, generated client, admin editor, search/display và test đã triển khai; tiếp tục chốt export/print policy + runtime review trước khi đóng wave.
2. **R10 — Account/system completion:** public Pending Approval, durable notification inbox, offline/reconnect/session-expired/forbidden/error states.
3. **R11 — Cross-route product hardening:** per-route table profiles, export/email state matrix, persona/permission review, admin safety và legacy redirects.
4. **Owner review R1–R8:** kiểm tra runtime theo persona/route/viewport; feedback shared primitive được retrofit cả route cũ.
5. **Figma Make design review:** audit shared foundation trên branch `figma/*` bằng App shell + My Orders + một dense CRUD route; chỉ tích hợp sau `plan.md`, diff review và automated/browser QA.
6. **R12 — Advanced intelligence:** triển khai sau core review nhưng vẫn giữ trong full plan; không trình bày Report Insight hiện tại như toàn bộ AI scope.
7. **Cutover:** chỉ chạy sau toàn bộ gate ở Section 16 và owner cho phép push/deploy.

Prompt tiếp tục:

```text
Tiếp tục Goal React full UI theo
docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md.
Đọc route ledger, decision log, git status và làm vertical slice tiếp theo.
React là target; Blazor là baseline/fallback. Không cutover hoặc xóa Blazor trước R8.
```

## 19. Nguồn chính

- [React documentation](https://react.dev/)
- [React Router — Data loading and routing](https://reactrouter.com/start/data/data-loading)
- [TanStack Query — Important defaults](https://tanstack.com/query/latest/docs/framework/react/guides/important-defaults)
- [TanStack Table — Virtualization](https://tanstack.com/table/latest/docs/guide/virtualization)
- [shadcn/ui — Open code component system](https://ui.shadcn.com/docs)
- [Vite — Static deployment](https://vite.dev/guide/static-deploy.html)
- [Vite — Backend integration](https://vite.dev/guide/backend-integration)
- [ASP.NET Core cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0)
- [ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)
- [W3C WCAG 2.2](https://www.w3.org/TR/WCAG22/)
- [Figma Make guidelines](https://help.figma.com/hc/en-us/articles/33665861260823-Add-guidelines-to-Figma-Make)
- [Figma Make plan-first workflow](https://help.figma.com/hc/en-us/articles/35710574222487-Beyond-the-basics-Using-Figma-Make)
- [Tailwind CSS source detection](https://tailwindcss.com/docs/detecting-classes-in-source-files)
- `VPP-PULSE-DESIGN-BRIEF.md`
- `VPP-PULSE-PRODUCT-BLUEPRINT.md`
- `VPP-PULSE-UI-UX-AI-TOOLCHAIN.md`
