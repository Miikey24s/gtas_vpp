# VPP Pulse — Prompt duy nhất cho Figma Make workspace trống

Tạo một Figma Make project mới với bộ shadcn/ui mặc định, chọn Claude Opus 4.8
Build và dán nguyên prompt tiếng Việt dưới đây. Prompt đã tự chứa toàn bộ context
cần thiết; không attach hoặc tham chiếu file trong repository GTAS VPP.

```text
Bạn là Principal Product Designer, UX Researcher và Senior React Frontend
Architect. Hãy tự nghiên cứu, thiết kế và dựng lại từ đầu một prototype UI/UX đầy
đủ, điều hướng được cho sản phẩm GTAS VPP ngay trong Figma Make.

QUAN TRỌNG VỀ MÔI TRƯỜNG

- Đây là Figma Make workspace trống, chỉ có React/Tailwind và bộ shadcn/ui chuẩn.
- Không có Guidelines.md, AGENTS.md, source GTAS VPP, router, OpenAPI hay i18n để
  đọc. Không tìm các file đó và không dừng lại để báo thiếu source.
- Toàn bộ context cần thiết nằm trong prompt này. Hãy dùng mock data/types rõ ràng
  cho prototype, nhưng không được tuyên bố mock là API hoặc production contract
  đã tích hợp.
- Mục tiêu hiện tại là prototype trực quan để owner review thiết kế. Không kết nối
  backend, không commit/push/deploy và không tạo secret/API key.
- Không chỉ audit hoặc viết plan. Hãy nghiên cứu rồi trực tiếp xây giao diện hoàn
  chỉnh trong cùng lượt làm việc.

1. NHIỆM VỤ NGHIÊN CỨU VÀ QUYỀN TỰ CHỦ THIẾT KẾ

Trước khi dựng, hãy chủ động nghiên cứu bằng những nguồn/công cụ mà bạn có về:

- enterprise SaaS và internal operations tool hiện đại;
- procurement/request/approval workflow;
- data-dense table, master-detail, command workspace và dashboard storytelling;
- accessibility, responsive, print, localization và reduced motion;
- design system/component pattern mới, phổ biến và phù hợp React + Tailwind +
  shadcn/ui.

Nếu workspace có khả năng truy cập web, ưu tiên tài liệu chính thức và ví dụ sản
phẩm hiện hành; nếu không có, dùng kiến thức thiết kế sẵn có. Không bịa rằng đã
truy cập một nguồn mà công cụ thực tế không mở được.

Không cần gửi một báo cáo research dài hoặc chờ owner chọn style. Hãy tự tổng hợp
nguyên tắc tốt, chọn MỘT art direction tốt nhất và áp dụng thẳng vào prototype.
Bạn được toàn quyền quyết định information architecture, bố cục, typography, màu
sắc, spacing, hình khối, radius, icon, component, sidebar/header, table/chart,
data storytelling, animation và micro-interaction. Không có style cũ, Apple,
ChatGPT, Notion, Linear, Figma, Personal Design DNA hay screenshot nào là khuôn
mẫu bắt buộc. Không sao chép một sản phẩm có sẵn.

Thiết kế phải có bản sắc riêng, hiện đại, chuyên nghiệp, dễ học, phù hợp một hệ
thống vận hành nội bộ nhiều dữ liệu và đủ ấn tượng để làm bộ mặt luận văn. “Wow”
phải phục vụ clarity và workflow, không chỉ là hiệu ứng trang trí.

2. BỐI CẢNH SẢN PHẨM

GTAS VPP là hệ thống nội bộ quản lý nhu cầu và mua sắm văn phòng phẩm trong một
công ty. Hành trình tổng quát:

Nhân viên đăng ký tài khoản → quản trị viên ánh xạ phòng ban/nhóm quyền và kích
hoạt → nhân viên xem kỳ đặt hàng → chọn mặt hàng và gửi đơn thường → có thể tạo
đơn bổ sung khi phát sinh → người duyệt phòng ban duyệt/từ chối đơn bổ sung → bộ
phận mua hàng tổng hợp nhu cầu toàn công ty → chọn bảng giá/nhà cung cấp → preview
và xác nhận quyết toán → hệ thống lưu snapshot/revision/audit bất biến → báo cáo,
export và notification phục vụ theo dõi.

Hệ thống là single-company v1, nhưng dữ liệu luôn có department scope. Mỗi tài
khoản có một nhóm quyền active và một primary department.

3. BỐN PERSONA PHẲNG

Navigation và dữ liệu phải thay đổi theo permission, không chỉ ẩn nút bằng visual.

1) Employee — Nhân viên
- Landing: kỳ hiện tại, đơn của tôi, next action.
- Xem catalog; tạo/sửa/hủy đơn của chính mình khi rule cho phép.
- Không thấy dữ liệu phòng ban hoặc toàn công ty.

2) Department Approver — Người duyệt phòng ban
- Xem tổng hợp và đơn thuộc phòng ban.
- Duyệt hoặc từ chối đơn bổ sung, xem queue và deadline.
- Không tự có quyền settlement hoặc quản trị permission.
- Người tạo không được tự duyệt đơn bổ sung của chính mình.

3) Procurement / Period Admin — Mua hàng / Quản trị kỳ
- Xem nhu cầu toàn công ty, kỳ đặt hàng, nhà cung cấp, bảng giá, settlement,
  correction/revision, report và export.
- Không tự có quyền quản trị tài khoản/permission kỹ thuật.

4) System Admin — Quản trị hệ thống
- Xử lý đăng ký chờ duyệt, kích hoạt/vô hiệu hóa account, reset password/session,
  ánh xạ phòng ban/nhóm quyền và cấu hình UI permission.
- Không mặc định có quyền xem toàn bộ nghiệp vụ mua hàng hoặc settlement.

Permission codes dùng để hiểu scope:
REQUEST_VIEW_OWN, REQUEST_VIEW_DEPARTMENT, REQUEST_VIEW_ALL, REQUEST_CREATE,
REQUEST_UPDATE_OWN, REQUEST_CANCEL_OWN, REQUEST_APPROVE, REQUEST_REJECT,
REQUEST_CATALOG_VIEW, LIBRARY_VIEW, LIBRARY_MANAGE, PERMISSION_VIEW,
PERMISSION_MANAGE, REPORT_VIEW_OWN, REPORT_VIEW_DEPARTMENT, REPORT_VIEW_ALL,
REPORT_EXPORT, PERIOD_SETTLE.

4. BUSINESS RULE KHÔNG ĐƯỢC THIẾT KẾ SAI

Account:
- Trạng thái: PendingApproval, Active, Disabled.
- Self-register tạo PendingApproval; System Admin chọn primary department + một
  active group rồi mới activate.
- Register gồm username, full name, company email, password, confirm password;
  employee code là optional và không cần hiển thị trong form chính.
- Forgot password luôn trả kết quả an toàn, không tiết lộ email/account tồn tại.

Order/request:
- Trạng thái: Submitted, Cancelled, Pending, Approved, Rejected.
- Đơn thường là base request. Đơn bổ sung bắt buộc liên kết base request hợp lệ.
- Chỉ tối đa 1 đơn bổ sung được Approved cho mỗi đơn thường.
- Tối đa 6 lần thử tạo/gửi lại supplement; tại một thời điểm không được có hai
  supplement Pending song song.
- Supplement reason bắt buộc từ 5 đến 500 ký tự.
- Chỉ tạo/sửa/hủy khi kỳ và deadline cho phép. Kỳ Closed hoặc Settled làm request
  bất biến.
- Mutation có thể gặp conflict do người khác vừa thay đổi dữ liệu; UI cần xử lý
  retry/reload có ngữ cảnh, không ghi đè âm thầm.

Approval:
- Department Approver xem base order, lý do supplement, attempt, quota, phần thay
  đổi và exact item detail trước khi Approve/Reject.
- Reject bắt buộc lý do. Approval hết deadline phải bị chặn rõ ràng.
- Không self-approval.

Period/settlement:
- Kỳ có các trạng thái vận hành như Open, Closed, Settled và deadline.
- Preview phải chỉ ra orders/lines/items/quantity/amount, supplier/price list,
  exception, blocker, pending supplement và reconciliation.
- Confirm settlement tạo snapshot tài chính/phân bổ bất biến.
- Correction không sửa lịch sử cũ: tạo revision mới, bắt buộc reason, actor,
  timestamp và before/after diff.

Price book:
- Trạng thái Draft, Published, Expired.
- Published price book bất biến; chỉnh sửa bằng version mới.
- Có supplier, effective dates, currency, default flag, item coverage, unit/net
  price, VAT, MOQ, lead time, SKU và discount/rebate/fee/shipping khi so sánh.

VI/EN business data:
- Một database, không phải hai database VI/EN.
- Entity giữ original language và translation rows.
- Translation có Draft/Approved; chỉ Approved được hiển thị trong vận hành.
- Source có Manual, Import, AiDraft.
- Nếu chưa có bản dịch Approved thì fallback về bản gốc và có thể báo nhỏ rằng
  đang dùng original text; không dịch máy âm thầm.
- Lịch sử order/settlement giữ snapshot text bất biến, không đổi theo bản dịch
  master data về sau.

5. ROUTE VÀ SCREEN INVENTORY PHẢI BAO PHỦ

Bạn được cải tiến IA hoặc hợp nhất màn hình nếu workflow tốt hơn, nhưng prototype
phải có destination/flow tương ứng và điều hướng được.

Public/Auth:
- /login
- /register
- /registration/pending
- /account/confirm-email
- /forgot-password
- /reset-password
- /change-password
- logout progress/fallback

Employee:
- /app/orders — My Orders overview, current/previous order, period/deadline,
  normal/empty/error và single next action.
- /app/orders/new — select items → selected tray → review → submit success; hỗ
  trợ regular, copy previous order và supplement.
- /app/orders/:orderId — exact detail, totals, permission-aware actions.
- /app/orders/:orderId/edit
- /app/orders/:orderId/history — immutable timeline/revision comparison.
- /app/catalog — search/filter/paging, item/category/UOM/supplier/price/detail.

Department/company management:
- /app/management/department — department summary, requester/order queue,
  status/deadline story và drill-down.
- /app/management/company — all-company demand và department comparison.
- /app/management/supplements — pending approval queue, detail/diff,
  approve/reject dialogs.

Procurement/period:
- /app/periods — period list, state, deadline, readiness/blockers.
- /app/periods/:year/:month — period overview và whole-company demand.
- /app/periods/:year/:month/preview — settlement preview, supplier/price-list
  decision, blocker/exception queue.
- /app/periods/:year/:month/settlement — read-only settled snapshot,
  reconciliation, revision/correction history và export.

Library/master data:
- /app/library/classes
- /app/library/categories
- /app/library/items
- /app/library/suppliers
- /app/library/departments
- /app/library/price-lists
- /app/library/prices

Các workspace này cần CRUD thật về mặt UI: server-style search/filter/sort/paging,
column management, active/deleted, detail inspector/drawer/page, create/edit form,
validation, confirm delete/restore, audit metadata và long-data behavior.

Access control:
- /app/access/users — registration queue, user table, account status, department,
  group, session; activate/disable/reset password/reset session.
- /app/access/groups — bốn persona canonical, description, member/capability
  summary và immutable/guarded rules.
- /app/access/permissions — page/component matrix với visible/enabled/action,
  group selector, search/filter và read-only/manage modes.

Reports/intelligence:
- /app/reports — scope own/department/all, year/month, generated time, summary,
  exact data table, print và XLSX export.
- /app/reports/insights — dùng cùng filter/data nhưng ưu tiên deterministic
  highlights, risks, recommendations và evidence links; phải có AI disabled,
  unavailable, error và source/model/generated-at disclosure. Không để AI tự
  quyết định nghiệp vụ.

System states:
- Notification popover + /app/notifications inbox: unread/read-all, type, title,
  message, route/deep link, correlation, time, expired/retry.
- /forbidden, /error có correlation ID, /not-found.
- Offline banner, reconnect/session-expired dialog, delayed export/email state.

6. DATA STORYTELLING CẦN THỂ HIỆN

Không mặc định biến mọi dữ liệu thành KPI cards hoặc chart. Mỗi màn hình nên giúp
người dùng trả lời: “Đang xảy ra gì?”, “Vì sao?”, “Cần làm gì tiếp?”, “Chi tiết
chính xác ở đâu?”. Tự chọn visualization phù hợp nhất.

My Orders cần làm rõ:
- kỳ hiện tại, deadline và period state;
- user đã gửi đơn chưa, có thể sửa/hủy/bổ sung không;
- tổng mặt hàng, tổng số lượng, lần gửi gần nhất;
- order detail và next action chỉ xuất hiện một nguồn, tránh copy/CTA lặp.

Department/company cần làm rõ:
- bao nhiêu đơn/phòng ban đã gửi, đang chờ hoặc chưa gửi;
- deadline exposure và queue nào cần xử lý;
- sản phẩm/phòng ban/requester nào tạo phần lớn nhu cầu;
- drill-down về exact order rows.

Procurement cần làm rõ:
- supplier nào rẻ nhất, supplier nào coverage tốt nhất, lead time và blocker;
- tổng chi phí gồm discount/rebate/fee/shipping/VAT;
- settlement có reconcile hay không và variance đến từ đâu.

Reports cần có:
- TotalOrders, TotalLines, TotalQuantity, TotalAmount, TotalRequesters;
- trend theo kỳ;
- status distribution;
- department/product ranking;
- settlement total, allocation total và variance;
- chart luôn có exact table hoặc accessible summary tương đương.

7. MOCK DATA NHẤT QUÁN CHO PROTOTYPE

Dùng cùng một dataset xuyên route, không random mỗi màn hình:

- Company: GTAS VPP.
- Current user: Nguyễn An Nam, phòng IT, đổi được persona bằng “Demo role switcher”
  chỉ xuất hiện trong prototype.
- Departments: IT, Hành chính, Kế toán, Nhân sự, Kinh doanh, Kho vận.
- Current period: 07/2026; deadline 00:00 05/08/2026; state Open.
- Previous period: 06/2026.
- Base order: DEMO-PPJ-202607-IT; Submitted; 6 mặt hàng; tổng số lượng 28;
  submitted 19:00 06/07/2026.
- Items:
  1. Bìa lỗ TQ GM — 3 Xấp
  2. Giấy note vàng 3×3 UNC — 3 Xấp
  3. Pin đồng hồ Pin 2A Energizer — 2 Vỉ
  4. Băng keo trong 5cm 100y — 5 Cuộn
  5. Giấy A4 trắng 80 Excell — 5 Ram
  6. Bút bi xanh 027 — 10 Cây
- Suppliers: Văn phòng phẩm An Phát, Thiên Long Distribution, Minh Khang Office.
- Tạo thêm dữ liệu hợp lý cho pending supplement, rejected supplement,
  departments chưa gửi, price-list comparison, settlement revision, pending
  account và notifications. Không dùng lorem ipsum, “User 1”, “Item A” hoặc số
  liệu vô nghĩa.
- VI/EN phải đổi cả UI copy và business display data; nếu bản dịch demo chưa
  Approved thì mô phỏng fallback về original.

8. DESIGN SYSTEM VÀ APP SHELL

Tự tạo design system thống nhất gồm color/typography/spacing/elevation/radius,
iconography, focus, status, motion và responsive rules. Dùng lại component thay
vì CSS vá riêng từng trang.

App Shell cần:
- permission-aware navigation theo workspace;
- expanded/collapsed sidebar hoặc một navigation model tốt hơn do bạn tự chọn;
- header utilities cho VI/EN, Light/Dark/Print, notification và account;
- user menu không lặp thông tin vô ích;
- page title/context/action rõ ràng;
- hover/active/focus/pressed/disabled/loading nhất quán, hit area đầy đủ;
- desktop-first nhưng mobile/tablet có navigation phù hợp, không chỉ thu nhỏ.

Bạn tự quyết branding/logo GTAS VPP cho prototype. Không dùng logo PPJ hoặc tài
sản có thể cần quyền thương hiệu.

9. MODE, RESPONSIVE, ACCESSIBILITY VÀ MOTION

- Thiết kế Light, Dark và Print thật. Print phải monochrome-friendly, bỏ
  navigation/action không cần thiết, giữ title/filter/generated time/table header
  và page-break hợp lý.
- Review desktop 1440×900 và 1920×1080; tablet 768×1024; mobile 390×844.
- Không bắt mọi bảng dài nằm hết trong một viewport bằng cách thu nhỏ chữ. Dùng
  paging, sticky region, column priority, inspector, drawer hoặc mobile card/list
  có chủ đích.
- Semantic structure, keyboard navigation, focus visible/focus return, label,
  accessible name, contrast và status không phụ thuộc riêng vào màu.
- Có reduced-motion. Motion phải giải thích feedback, hierarchy hoặc spatial
  relation; không làm chậm tác vụ và không stagger hàng trăm table rows.
- Validation/error slot không làm layout nhảy khó chịu.

10. STATE MATRIX

Tạo shared state gallery và áp dụng state phù hợp cho từng màn hình:

- loading/skeleton đúng hình học destination;
- empty dataset;
- filtered empty;
- normal;
- long text/long data;
- validation error;
- network/server error + retry;
- unauthorized/session expired;
- forbidden;
- disabled/read-only;
- pending mutation/double-submit lock;
- optimistic/concurrency conflict;
- success có durable confirmation;
- offline/reconnect;
- destructive confirmation;
- print/export progress, success và failure.

Không hiển thị raw JSON, exception class, stack trace hoặc code lỗi khó hiểu cho
người dùng.

11. CÁCH THỰC THI TRONG FIGMA MAKE

- Bắt đầu bằng research nội bộ ngắn rồi xây ngay, không dừng để xin duyệt plan.
- Dùng React + Tailwind + shadcn/ui hiện có trong workspace.
- Tạo router/navigation prototype điều hướng được cho toàn bộ screen inventory.
- Tạo mock repository/service layer và dataset dùng chung; tách rõ “prototype
  mock” để sau này thay bằng generated OpenAPI client.
- Mỗi route phải có ít nhất một màn hình hoàn chỉnh. Luồng quan trọng phải có
  interaction/state tiêu biểu, không chỉ một static dashboard showcase.
- Ưu tiên xây design system + App Shell + Auth + Employee journey trước, sau đó
  lan cùng system sang Management, Procurement, Library, Access và Reports.
- Nếu một contract chưa có trong prompt, đưa ra giả định tối thiểu, ghi lại cuối
  cùng và tiếp tục thiết kế; không dừng toàn bộ dự án.
- Tự kiểm tra navigation, theme, language, viewport và console trước khi kết thúc.

KẾT QUẢ CUỐI CÙNG

Tôi cần mở prototype và review trực tiếp, không cần một bài giải thích dài. Hãy
hoàn thành:

1. design system/state gallery;
2. prototype điều hướng được qua toàn bộ workspace và route nêu trên;
3. VI/EN, Light/Dark/Print;
4. desktop/tablet/mobile representative screens;
5. mock data nhất quán và permission-aware demo role switcher;
6. các flow quan trọng: auth, create order, supplement approval, settlement,
   master-data CRUD, account activation/permission và report/export;
7. cuối cùng chỉ ghi ngắn gọn art direction đã chọn, nghiên cứu đã áp dụng, các
   giả định và contract thực sự còn thiếu.

Hãy làm lại từ đầu ngay bây giờ. Không tìm source code GTAS VPP bên ngoài
workspace; vẫn được nghiên cứu design pattern trên web nếu công cụ hỗ trợ. Không
hỏi tôi chọn style trước và không dừng ở bước “set up foundation”. Tiếp tục cho
đến khi prototype đủ đầy để owner bắt đầu review màn hình.
```
