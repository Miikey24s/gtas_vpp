# VPP Pulse — Product-wide UI/UX & Data Storytelling Blueprint

> **Trạng thái:** `HISTORICAL PRODUCT INVENTORY` — giữ route/data/state coverage; visual direction đã được supersede bởi OpenAI/Codex contract trong `UI-SYSTEM-001`
> **Phiên bản:** 0.5
> **Ngày:** 2026-07-18
> **Phạm vi:** Toàn bộ GTAS VPP — Auth, Employee, Management, Procurement, Reports, Library, Permission và system states
> **Nguồn visual:** [GTAS VPP — VPP Pulse](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE)

## 0. Figma deliverable hiện tại

Bản Figma đã mở rộng từ art-direction baseline thành product blueprint có thể duyệt trước khi triển khai code:

- 30+ desktop/state frames và 2 mobile evidence.
- 8 workspace: Auth, Employee, Management, Procurement, Reports, Library, Access Control và Shell/System.
- Product Map, Data Patterns, role/route matrix, state matrix, responsive contract và implementation sequence.
- Không thay đổi production UI, API, schema hoặc nghiệp vụ trong giai đoạn này.

| Nội dung | Figma root | Coverage chính |
|---|---|---|
| Product Map | [`60:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=60-2) | IA, personas, journeys, screen coverage |
| Data Patterns | [`62:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=62-2) | Table/paging, progressive disclosure, data story, visualization policy |
| Component Hardening | [`85:12`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=85-12) | Input, filter chip, callout, notification, grid row, state panel, shell/sidebar/topbar |
| Shell & System | [`71:72`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=71-72) | Notification, reconnect, 403/error/logout |
| Auth & Account | [`64:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=64-2) | Login, register, pending activation, recovery, password, mobile |
| Employee | [`32:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=32-2), [`41:106`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=41-106), [`72:100`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=72-100) | Create request desktop/mobile, My Orders, detail/history, success |
| Management | [`65:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=65-2) | Department dashboard, approval queue, decision, all orders |
| Procurement | [`66:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=66-2) | Period, aggregate demand, supplier compare, settlement |
| Reports | [`67:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=67-2) | Overview story, trend/status, department/product, insight/export |
| Library | [`68:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=68-2) | Products, suppliers, price lists/prices |
| Access Control | [`69:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=69-2) | User activation, flat personas, permission matrix |
| QA & Handoff | [`74:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=74-2) | Coverage, states, responsive rules, acceptance, implementation order |
| Review Prototypes | [`93:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=93-2) | 4 top-level core flows, 11 review frames và 7 verified navigation hotspot |
| AI & Intelligence | [`103:2`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=103-2) | Report insight, anomaly review, Ask the Report, governance và safety states |
| Full Masterplan Coverage | [`111:269`](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE?node-id=111-269) | A+ evidence, designed/not coded, deferred và out-of-scope |

QA Figma ngày 2026-07-18:

- 11 page nghiệp vụ/blueprint được audit; vòng coverage ban đầu có 1.397 frame và 1.836 text node.
- Vòng Figma engineering cuối ghi nhận 50 instance, 3.526 node có variable binding và 29 component/component-set node trong các root bàn giao.
- Không có generic `Frame` name, `Lorem ipsum`, `TODO`, `TBD`, `FIXME`, `sample text` hoặc mixed-font text node trong phạm vi audit cuối.
- Các root mới được gắn run `dsb-vpp-pulse-002` để có thể tiếp tục và kiểm tra lại xác định.
- Screenshot QA đã phát hiện và sửa notification/reconnect overlay bị auto-layout đẩy khỏi canvas.
- Các concept screen đã bind semantic color/spacing/radius token. Component library và reference implementation là nguồn chuyển sang code sau khi direction được duyệt; không cần detach component để triển khai.
- Page prototype riêng giữ các destination ở top-level frame cùng page theo đúng Figma navigation contract: Auth, Employee Request, Management Decision và Procurement Settlement.
- Status Badge (`22:17`) đã sửa từ fixed 90px sang hug-content với min-width 110px; screenshot QA xác nhận cả 5 trạng thái không còn clipping/overlap.
- Chi tiết kiến trúc AI, data boundary, secret, provider và acceptance nằm trong [VPP-PULSE-AI-BRIEF.md](./VPP-PULSE-AI-BRIEF.md); phạm vi full-plan nằm trong [VPP-PULSE-FULL-MASTERPLAN-DESIGN-AUDIT.md](./VPP-PULSE-FULL-MASTERPLAN-DESIGN-AUDIT.md).

## 1. Quyết định tổng quát

Figma baseline trước chỉ chứng minh art direction và hai vertical slice. Bản mở rộng phải cho người dùng nhìn thấy **toàn bộ sản phẩm sẽ tổ chức ra sao, dữ liệu nào xuất hiện, vì sao nó xuất hiện và người dùng sẽ đi tiếp bằng cách nào** trước khi sửa production UI.

GTAS VPP dùng ba lớp trình bày khác nhau:

| Lớp | Mục đích | Hình thức chính | Không làm |
|---|---|---|---|
| Operational UI | Tra cứu, nhập liệu, duyệt và mutation chính xác | Table, form, card, timeline, dialog | Không thêm chart nếu chart không giúp quyết định |
| Role dashboard | Theo dõi trạng thái hiện tại và việc cần làm | KPI, alert, 2–3 views, action queue | Không biến thành report dài hoặc chứa mọi chi tiết |
| Report/data story | Giải thích xu hướng, tương quan, bất thường và nguyên nhân | Narrative headline, chart phù hợp, annotation, drill-down, exact table | Không dùng một chart trả lời nhiều câu hỏi cùng lúc |

**Data storytelling** trong dự án này không phải animation hoặc một dashboard nhiều biểu đồ. Nó là cấu trúc:

1. **Takeaway:** điều quan trọng nhất người dùng cần biết.
2. **Evidence:** số liệu hoặc visual hỗ trợ takeaway.
3. **Context:** scope, thời gian, định nghĩa, nguồn và thời điểm tạo dữ liệu.
4. **Drill-down:** xem phòng ban, đơn, sản phẩm hoặc revision tạo ra kết quả.
5. **Next action:** xử lý, duyệt, sửa dữ liệu gốc, export hoặc tiếp tục theo dõi.

## 2. Nguyên tắc nghiên cứu được áp dụng

Các nguồn chuyên môn thống nhất ở những điểm sau:

- Bắt đầu bằng audience, câu hỏi và quyết định; không bắt đầu bằng chart type.
- Dashboard là overview một màn hình, chỉ giữ highlight; chi tiết được drill-down sang report/table.
- Một data story nên có một takeaway chính. Cùng một dataset có thể cần nhiều visual vì mỗi visual trả lời một câu hỏi khác nhau.
- Dùng annotation, title dạng kết luận và context gần visual thay vì buộc người dùng tự giải mã.
- Giới hạn số view trên dashboard; giảm clutter, màu và gridline không cần thiết.
- Aggregate trước, cho phép xem underlying data sau; visual không được thay thế bảng dữ liệu chính xác.
- Màu dùng để highlight hoặc cảnh báo, không dùng làm mã thông tin duy nhất.
- Thiết kế riêng cho mobile/tablet/desktop thay vì chỉ thu nhỏ một dashboard desktop.

Nguồn chính:

- [Storytelling with Data — core process](https://www.storytellingwithdata.com/public-workshops/)
- [Storytelling with Data — start with the takeaway, not the graph](https://www.storytellingwithdata.com/blog/different-takeaway-different-graph)
- [Tableau — best practices for data stories](https://help.tableau.com/current/pro/desktop/en-gb/story_best_practices.htm)
- [Tableau — effective dashboards](https://help.tableau.com/current/pro/desktop/en-us/dashboards_best_practices.htm)
- [Tableau Blueprint — visual best practices](https://help.tableau.com/current/blueprint/en-us/bp_visual_best_practices.htm)
- [Power BI — dashboard as one-page overview](https://learn.microsoft.com/mt-mt/power-bi/create-reports/service-dashboards)
- [Tableau — accessible dashboards](https://help.tableau.com/current/pro/desktop/en-gb/accessibility_dashboards.htm)
- [W3C WAI — accessible data tables](https://www.w3.org/WAI/tutorials/tables/one-header/)
- [Radzen DataGrid — paging, filtering and virtualization](https://blazor.radzen.com/datagrid?theme=default&wcag=true)

## 3. Decision framework: show dữ liệu bằng gì

### 3.1 Format selection matrix

| Câu hỏi người dùng | Format ưu tiên | Ví dụ GTAS VPP | Vì sao |
|---|---|---|---|
| Giá trị chính hiện tại là bao nhiêu? | KPI + label + context | Tổng đơn, pending, tổng số lượng, grand total | Đọc tức thời |
| Danh sách nào cần tra cứu hoặc thao tác? | Data table / mobile cards | Đơn hàng, user, sản phẩm, price list | Giữ giá trị chính xác và action |
| Thay đổi theo thời gian thế nào? | Line hoặc column theo kỳ | Total quantity/amount theo tháng | Nhìn trend, seasonality, inflection |
| Nhóm nào lớn hơn/nhỏ hơn? | Sorted horizontal bar | Department, category, top product | So sánh độ dài dễ hơn angle/area |
| Tỷ trọng trạng thái là gì? | Segmented/100% stacked bar + counts | Submitted, pending, approved, rejected | Dễ so sánh hơn donut, vẫn có exact values |
| Điểm bất thường nằm ở đâu? | Ranked exception list; scatter khi đủ dữ liệu | Department có amount/quantity lệch, settlement blockers | Actionable trước, exploratory sau |
| Sự kiện đã diễn ra theo thứ tự nào? | Timeline | Request revision, submit, approve, reject, cancel, settle | Giữ actor/time/reason |
| Supplier/price book nào tốt hơn? | Ranked comparison table + bullet bars | Coverage, grand total, lead time, blockers | Nhiều tiêu chí, cần exact values |
| Tiến trình nghiệp vụ đang ở bước nào? | Stepper/state rail | Create Request, settlement, activation | Không dùng chart cho workflow |
| Cấu hình/quyền nào đang áp dụng? | Matrix/table + switch/badge | Role/page/component permission | Cần đọc chính xác, audit được |

### 3.2 Những visual không dùng mặc định

- Donut/pie: chỉ cân nhắc khi 2–3 phần và người dùng chỉ cần tỷ trọng xấp xỉ. Report Status hiện tại nên đổi sang segmented bar + legend counts.
- Radar/spider: không dùng để so supplier hoặc department vì khó so sánh và không thể hiện exact value tốt.
- Gauge/speedometer: không dùng cho KPI; chiếm diện tích và thiếu baseline rõ.
- Bubble packing/treemap: không dùng cho catalog hoặc top products; chỉ cân nhắc exploratory view nếu category count lớn và vẫn có table thay thế.
- Dual-axis chart: tránh vì dễ tạo tương quan giả. Nếu cần so order count và amount, dùng metric switch hoặc small multiples.
- Funnel: chỉ dùng khi có conversion thật giữa các stage. Workflow request không phải funnel marketing.

## 4. Pagination, virtualization, drill-down và progressive disclosure

### 4.1 Paging strategy

| Dataset | Chiến lược | Page size | Lý do |
|---|---|---:|---|
| My Orders / History | Client list hoặc server paging khi tăng | 10/20/50 | Dataset của một user nhỏ; giữ thao tác dễ hiểu |
| Department / All Orders / Approval | Server-side paging | 20/50/100 | Nhiều user/kỳ; filter/sort phải ở server |
| Period Review / Settlement orders | Server-side paging + master-detail | 20/50/100 | Bảng rộng, có details và mutation nhạy cảm |
| Product Catalog browsing | Server-side paging | 20/50/100 | Người dùng cần biết tổng số, page và filter state |
| Product selection trong wizard | Paging mặc định; virtualization chỉ khi benchmark chứng minh cần | 20/50/100 | Paging ổn định hơn cho keyboard, selection và Blazor Server |
| Users / Permission / Library | Server-side paging + persisted settings | 20/50/100 | Admin quay lại cần giữ filter/column/page |
| Notification inbox | Cursor/load-more hoặc paging 20 | 20 | Dòng thời gian mới nhất trước, không cần random page |

### 4.2 Khi nào dùng side panel/drawer

- Dùng right-side detail panel khi cần giữ nguyên filter/list context và detail không chứa mutation dài: request summary, user summary, product/price snapshot.
- Dùng modal/dialog cho action ngắn, có điểm kết thúc rõ: reject reason, activate user, reset password, confirm settle.
- Dùng full page khi task nhiều bước hoặc có nhiều data: Create Request, Settlement Preview, Report Drill-down.
- Mobile: side panel trở thành full-screen sheet/page; không giữ drawer hẹp chứa table phức tạp.

### 4.3 Progressive disclosure

- Row chính chỉ show dữ liệu để nhận dạng, so sánh và action.
- Audit IDs, created/updated metadata, revision hash, calculation version nằm trong inspector/evidence panel.
- Advanced filters nằm trong collapsible filter panel nhưng filter đang active luôn có chips/tóm tắt trên màn.
- AI/rules insight chỉ xuất hiện sau deterministic KPI/chart và có source, generated time, evidence/fallback.

## 5. Information architecture

### 5.1 Global navigation

```text
GTAS VPP
├── Workspace
│   ├── My Orders
│   ├── History
│   └── Product Catalog
├── Management
│   ├── Department Orders
│   └── All Orders
├── Period Operations
│   ├── Pending Approvals
│   └── Period Review & Settlement
├── Reports
├── Master Data
│   ├── Classes / Categories / Products
│   ├── Suppliers
│   ├── Price Lists / Prices
│   └── Departments
├── Access Control
│   ├── Users & Activation
│   └── Roles & UI Permissions
└── Account
    ├── Notifications
    ├── Profile / Password
    └── Logout
```

Navigation vẫn permission-aware. Bốn persona flat:

| Persona | Landing focus | Không tự có |
|---|---|---|
| Employee | Period, own request, next action | Department/all-company data |
| Department Approver | Department status, supplement approvals | Settlement, permission admin |
| Procurement / Period Admin | All-company demand, price, settlement, reports | Technical access administration |
| System Admin | Account activation, role and UI permission | Business/procurement omniscience mặc định |

## 6. Product-wide screen inventory

### 6.1 Auth & account

| Screen/state | Data show | Primary action | Figma coverage cần dựng |
|---|---|---|---|
| Login | Username, password, safe error, environment badge local only | Login | Desktop/mobile + invalid credential |
| Register | Username, full name, email, optional employee code, password | Submit registration | Default, validation, success/pending |
| Pending Approval | Registration reference, submitted time, what happens next | Back to login | Normal + email unavailable fallback |
| Confirm Email | Confirmation result | Continue to login | Success/expired/invalid |
| Forgot Password | Email | Send recovery | Default/sent/anti-enumeration |
| Reset Password | New password + policy | Reset | Validation/success/expired token |
| Change Password | Current/new/confirm | Save | Default/error/success |
| Logout | Progress and fallback | Return to login | Redirect success/failed clear state |

### 6.2 Employee workspace

| Screen/state | Data show | Display strategy |
|---|---|---|
| My Orders overview | PeriodState, deadline, quota, current/previous/additional orders, total lines/qty | Period banner + KPI + actionable request cards/table |
| My Orders empty | Why user cannot/has not ordered, period rule | State panel + allowed action |
| Request detail | Code, period, status, items, quantities, prices, amount, permissions | Summary rail + exact detail table |
| Request history | Revisions, actor, action, time, reason | Timeline + revision comparison table |
| Create Request — select | Product search, category, UOM, selected items, qty, notes | Catalog table/cards + selected tray |
| Create Request — review | Lines, total qty, mode, notes, validation | Review summary + exact items table |
| Submit success | Request code, next state, deep links | Confirmation panel, not only toast |
| Product Catalog | Code/name/category/UOM/default supplier/price/description | Search/filter + server paging table/cards |

### 6.3 Department management

| Screen/state | Data show | Display strategy |
|---|---|---|
| Department dashboard | Order count, pending, total lines/qty, deadline exposure | KPI + status segmented bar + action queue |
| Department orders | Department, requester, request code, status, period, lines/qty, submitted date | Server-paged table + right detail panel |
| Supplement approval detail | Base request khi có, supplement reason/attempt/quota, changed items, actor/time | Diff summary + exact item table + approve/reject actions |
| Reject dialog | Reason, consequence, attempt/quota note | Short modal with required validation |
| Department data story | Which requesters/products drive demand or pending work | Ranked bars + annotated exception list + table drill-down |

### 6.4 Procurement & period operations

| Screen/state | Data show | Display strategy |
|---|---|---|
| Pending approvals | Pending count, total lines/qty, department/requester/code/time | Priority queue table; age/deadline annotation |
| Period review | Year/month/price list, orders/lines/qty/amount, settlement state | Filter command bar + KPI + server table |
| Whole-company demand | Product/category/UOM, total qty, unit/total price, requester breakdown | Aggregated product table + expandable allocation |
| Supplier comparison | Coverage, subtotal, discount, rebate, fee, shipping, VAT, grand total, lead time, blockers | Ranked comparison table + bullet bars |
| Settlement preview | Primary supplier/list/version, request/line/item counts, exceptions/blockers, input hash | Decision summary + exception queue + evidence panel |
| Confirm settlement | Selected supplier, totals, pending supplement, immutable snapshot notice | Confirm modal/full-page review depending blockers |
| Settled period | Supplier, grand total, revision, reconciliation, settled actor/time | Read-only summary + allocation/export links |
| Correction/revision | Old/new values, reason, actor, variance | Side-by-side diff + audit timeline |

### 6.5 Reports & data storytelling

Report giữ scope `own | department | all`, year, month và generated time.

| Story question | Data fields | Primary visual | Drill-down |
|---|---|---|---|
| Tổng quan hiện tại ra sao? | TotalOrders, TotalLines, TotalQuantity, TotalAmount, TotalRequesters | KPI strip + takeaway headline | Exact report table/export |
| Nhu cầu thay đổi theo kỳ thế nào? | Period, OrderCount, TotalQuantity, TotalAmount | Metric switch: column/line; không dual axis | Period requests/products |
| Trạng thái nào cần chú ý? | Status, OrderCount | Segmented horizontal bar + counts/% | Filtered order table |
| Phòng ban nào nổi bật? | Code, OrderCount, TotalQuantity, TotalAmount | Sorted horizontal bar; optional outlier scatter | Department orders |
| Sản phẩm nào chi phối nhu cầu? | ProductCode/Name, TotalQuantity/Amount | Top-N horizontal bars + exact table | Product/request breakdown |
| Settlement có reconcile không? | SettlementGrandTotal, AllocationTotal, Variance, supplier, revision | Reconciliation callout + variance bar | Settlement evidence |
| Hệ thống đề xuất điều gì? | Summary, Highlights, Risks, Recommendations, Source, Model, GeneratedAt | Narrative panel sau deterministic views | Evidence links; rules/AI label |

### 6.6 Master data / Library

| Workspace | Dữ liệu chính | Display strategy |
|---|---|---|
| Classes/categories/lookups | Code, name/value, sort, description, active/deleted, audit | Two-pane master-detail hoặc linked tables |
| Products | Code, name, category, UOM, description, active/deleted | Server table + inspector/form drawer |
| Suppliers | Code/name/contact/active/audit | Server table + inspector |
| Price Lists | Code/name/supplier/version/status/effective dates/currency/default/items | Table + lifecycle badge + detail drawer |
| Prices | Product/supplier/list/version/net price/VAT/MOQ/lead/SKU/default | Filtered server table; edit dialog |
| Price comparison | Whole-basket quote fields | Ranked comparison table, not generic cards |
| Departments | Code/name/member company/active/audit | Table + membership impact warning |

### 6.7 Access control

| Screen/state | Dữ liệu chính | Display strategy |
|---|---|---|
| User queue | Login/name/email/employee/department/group/account status/active/session version | Status tabs + server table |
| Pending activation | Identity, registration time, mapping completeness | Action queue + activation wizard |
| User inspector | Identity, department, group, audit, status, sessions | Side panel with guarded actions |
| Role list | Four canonical groups, description, read-only model | Compact table/cards |
| Page/component permission | Page code/name, component code/name, permission kind, visible/enabled | Hierarchical matrix/table, not chart |
| Session reset/password reset | Actor, reason, result | Confirm dialog + durable audit feedback |

### 6.8 Global/system states

- Loading skeleton matching destination geometry.
- Empty with explanation and allowed next action.
- Access denied without leaking resource existence.
- Error with safe message, correlation ID and retry/back.
- Reconnect modal with retry/reload state.
- Offline/export/email delayed state.
- Notification inbox: unread/total, type/title/message/route/correlation/time/read state.
- 404 with navigation back to safe landing.

## 7. Data story templates for GTAS VPP

### Story A — “Kỳ này có đang vận hành an toàn không?”

1. Takeaway: “Còn 18 ngày; 7 đơn đang chờ; 2 phòng ban chưa gửi.”
2. Evidence: period/deadline + status segmented bar + department exception list.
3. Drill-down: filtered pending requests.
4. Action: notify/approve/open department detail.

### Story B — “Nhu cầu tăng do đâu?”

1. Takeaway: “Tổng số lượng tăng 22% so với kỳ trước, chủ yếu từ giấy in và phòng IT.”
2. Evidence: period trend + product/department ranked bars.
3. Context: scope, period, metric definition, generated time.
4. Drill-down: requests/items producing the change.

### Story C — “NCC nào phù hợp để chốt kỳ?”

1. Takeaway: “NCC A rẻ nhất nhưng thiếu 3 mặt hàng; NCC B phủ 100% và giao nhanh hơn 2 ngày.”
2. Evidence: ranked quote table, coverage bullet, total cost and lead time.
3. Blockers: missing items, invalid price list, pending supplements.
4. Action: choose primary supplier, add reasoned exceptions or fix price data.

### Story D — “Settlement có khớp không?”

1. Takeaway: reconciled/not reconciled and variance.
2. Evidence: settlement total vs allocation total, revision and supplier snapshot.
3. Drill-down: department/request/product allocations.
4. Action: export, correction revision or investigate exception.

## 8. Figma architecture mở rộng

### 8.1 Pages

| Page | Deliverable |
|---|---|
| `00 — Cover` | Cover và art direction |
| `00 — Brief & DNA` | Personal DNA, brand behavior, product thesis |
| `00.5 — Product Map` | IA, persona, route/action map, screen inventory |
| `01 — Foundations` | Tokens, typography, responsive/data-viz palettes |
| `02 — Components` | Full component/state library |
| `02.5 — Data Patterns` | Table/paging/filter, chart selection, annotation, side panel, evidence |
| `03 — Shell & System` | Authenticated shell, nav, notifications, reconnect/error |
| `04 — Auth & Account` | Login/register/activation/recovery/password/logout |
| `05 — Employee` | My Orders, detail/history, create/review/success, catalog |
| `06 — Management` | Department dashboard/orders, approval queue/detail/reject |
| `07 — Procurement` | Period review, demand, supplier compare, settlement/revision |
| `08 — Reports` | Overview + four data stories + insight/export states |
| `09 — Library` | Products/suppliers/price lists/prices/lookups/departments |
| `10 — Access Control` | Users/activation/roles/permission matrix |
| `90 — QA & Handoff` | Role/state/viewport matrix, code mapping, acceptance |

### 8.2 Required screen frames

Minimum high-fidelity desktop frames:

1. Login.
2. Register + Pending Approval.
3. My Orders overview.
4. Request detail/history.
5. Create Request step 1.
6. Create Request step 2 + success.
7. Department dashboard/orders.
8. Approval queue + approval detail.
9. Period Review.
10. Whole-company demand.
11. Supplier comparison.
12. Settlement preview + settled state.
13. Report overview.
14. Report trend/status story.
15. Report department/product story.
16. Library product/price workspace.
17. User activation/admin workspace.
18. Permission matrix.

Required responsive frames:

- Mobile: Login, My Orders, Create step 1, Request detail, Approval queue, Report overview.
- Tablet: Shell, Create step 1, Department orders, Period Review, Report overview.
- Desktop QA target: 1920×1080 for every core workspace; Figma authoring frame may remain 1440×1024.

### 8.3 State coverage per core screen

Mỗi core screen phải có annotation hoặc variant cho:

- Loading.
- Empty.
- Error/retry.
- Disabled + reason.
- Permission-limited.
- Success/confirmation.
- Responsive behavior.
- Data source/generated time.
- Primary action and destructive action guard.

## 9. Design-to-code boundaries

- Figma quyết định visual hierarchy, component/state contract, responsive behavior và data presentation.
- Source/DTO/API quyết định field, business rule, permission, mutation và lifecycle.
- Không thêm field/metric chỉ vì mockup cần đẹp. Metric mới phải có deterministic definition và source.
- Không sửa API/schema/nghiệp vụ trong design goal.
- Global Interactive Server được giữ nguyên; không thêm descendant `@rendermode`.
- Radzen DataGrid/Chart/Button/Dialog là implementation baseline; không thêm chart framework trước khi chứng minh Radzen không đáp ứng.

## 10. Acceptance trước khi sửa production UI

Blueprint/Figma chỉ được coi là đủ khi:

- Toàn bộ route/persona/workspace có vị trí trong IA và screen inventory.
- Mỗi report visual ghi rõ câu hỏi, metric, dimension, scope, as-of time và drill-down.
- Mỗi table ghi rõ paging/filter/sort/column strategy và mobile alternative.
- Management/Procurement thể hiện approval, blockers, settlement evidence và immutable revisions.
- Auth/Permission thể hiện PendingApproval, mapping, least privilege và guarded admin actions.
- 390/768/1920 có behavior contract; không dùng auto-shrink làm responsive strategy.
- Mọi chart có title dạng takeaway, exact values hoặc accessible table alternative.
- Figma review được hoàn thành trước P0 production UI implementation.
