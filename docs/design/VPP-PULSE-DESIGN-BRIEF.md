# VPP Pulse — Design Brief

> **Trạng thái:** `SUPERSEDED FOR ACTIVE UI` — giữ làm historical research; OpenAI/Codex visual contract trong `UI-SYSTEM-001` là authority hiện hành
> **Phiên bản:** 0.2
> **Ngày:** 2026-07-18
> **Phạm vi:** GTAS VPP, Vietnamese-first, Interactive Server, rebrand độc lập

## 1. Quyết định thiết kế

Hướng **Editorial Industrial Clarity** dưới đây là baseline nghiên cứu năm 2026-07-18, không còn là art direction production. GTAS VPP hiện dùng OpenAI/Codex-inspired minimal system; các mục bên dưới chỉ còn giá trị lịch sử hoặc data-story insight không xung đột authority mới.

Tên làm việc của hướng này là **VPP Pulse** — “nhịp vận hành có chủ đích”. Pulse không phải dashboard nhiều hiệu ứng; nó là cách biến trạng thái, deadline, luồng duyệt và dữ liệu thành các nhịp nhìn dễ quét.

### Không làm

- Không dùng trực tiếp logo, tên, hình ảnh, dữ liệu hoặc nhận diện PPJ trong shell, seed, screenshot hay Figma final. PPJ chỉ là nguồn cảm hứng về tinh thần vận hành; quyết định rebrand độc lập đã được chốt tại D-009.
- Không biến ứng dụng nghiệp vụ thành landing page thời trang.
- Không dùng glassmorphism nặng, gradient/glow dày, animation gây chú ý, texture denim phủ toàn màn hình hoặc màu sắc tùy hứng.
- Không để sở thích cá nhân ghi đè accessibility, quyền truy cập, status nghiệp vụ hay thông tin cần để ra quyết định.

## 2. Nguồn đầu vào

### 2.1 Personal Design DNA

Kết quả khảo sát local của Nam: 60/60 câu, độ nhất quán 70%, độ phủ 56%, không phát hiện thiên lệch trái/phải. Hồ sơ đang ở mức `developing`, vì vậy chỉ dùng các tín hiệu ổn định và khá chắc chắn làm định hướng; không coi đây là điểm gu tuyệt đối.

Tín hiệu đáng tin cậy nhất:

- **Biên tập cá tính:** typography có vai trò tạo bản sắc, không chỉ để hiển thị chữ.
- **Chuyển động tối giản:** motion ngắn, có mục đích, không làm chậm thao tác.
- **Tiêu đề ngoại cỡ:** dùng scale lớn cho page title, KPI và moment quan trọng.
- **Thưa:** khung nội dung thoáng, giảm cảm giác chen chúc.
- **Trừu tượng:** ưu tiên hình khối, đường nét và ẩn dụ hơn ảnh minh họa literal.
- **Ưu tiên đường viền:** cấu trúc nên được đọc bằng edge, divider và alignment trước khi dùng shadow.

Tín hiệu phụ cần dùng thận trọng:

- Hơi nghiêng về hình học, nền sáng, đối xứng, kỹ thuật số sạch và progressive disclosure.
- Chưa đủ chắc để kết luận về bo tròn, màu ấm/lạnh, độ rực, playful/Gen Z hay contrast cực cao.

Raw JSON evidence nằm ngoài repository và không được commit. Design brief này chỉ lưu diễn giải tổng hợp cần cho quyết định UI.

### 2.2 PPJ-inspired operating DNA

Các nguồn chính thức của PPJ Group/Phong Phu International mô tả một tổ chức dệt may tích hợp dọc, đầu tư vào công nghệ số, ERP, đổi mới sản phẩm, chất lượng và sản xuất bền vững. Họ nhấn mạnh các giá trị tốc độ, sáng tạo, đam mê, chuyên nghiệp và chính trực; hệ thống vận hành phải nhanh nhưng chính xác, có trách nhiệm và có thể mở rộng.

Trong VPP Pulse, các ý này được chuyển hóa thành hành vi giao diện:

| Tinh thần | Biểu hiện trong UI |
|---|---|
| Tốc độ | CTA rõ, trạng thái tức thời, filter không gây mất context, ít bước thừa |
| Chính xác | Số liệu có nhãn/đơn vị, status có text + màu + icon, xác nhận trước mutation |
| Sáng tạo | Typography, nhịp layout và motif đường chỉ; không cần màu mè |
| Chuyên nghiệp | Grid ổn định, thuật ngữ thống nhất, permission-aware, error an toàn |
| Đam mê / con người | Empty state có giọng nói thân thiện, không lạnh như hệ thống kế toán |
| Bền vững | Ưu tiên digital workflow, giảm lặp lại, hiển thị hiệu quả và traceability khi có dữ liệu |

Đây là **brand behavior inspiration**, không phải giấy phép sử dụng thương hiệu PPJ.

### 2.3 Ràng buộc GTAS VPP

- Core journey phải phục vụ: đăng nhập/đăng ký → xem kỳ → tạo yêu cầu → chọn sản phẩm → gửi → duyệt/bổ sung → procurement/settlement → report/export/notification.
- IA phải phân biệt Employee, Manager/Department, Procurement và System Admin theo permission thực tế; không suy diễn role từ visual.
- Vietnamese là ngôn ngữ mặc định; mọi chuỗi critical phải localization-ready.
- Light mode là baseline hiện hữu; dark mode/English không được làm lệch core cutline.
- UI phải chạy với Interactive Server, chịu được prerender/reconnect, không phụ thuộc asset external runtime.
- Mốc QA bắt buộc: 390×844, 768×1024 và 1920×1080; keyboard, console, network, focus và overflow đều là acceptance chứ không phải polish tùy chọn.

## 3. Art direction

### 3.1 Visual thesis

**“Một hệ thống vận hành có nhịp: chữ dẫn đường, đường chỉ nối dữ liệu, bề mặt sáng để quyết định không bị che.”**

Ba lớp thị giác:

1. **Editorial signal** — tiêu đề lớn, số liệu có scale, caption ngắn, alignment có chủ ý.
2. **Industrial structure** — border, grid, rail, tabular data, status bands và các đường nối mô phỏng luồng.
3. **Pulse accent** — một đường chỉ/đường pulse màu teal hoặc cyan dùng có giới hạn để biểu thị active, progress, live hoặc transition.

### 3.2 Màu và surface direction

Giữ nền tảng token hiện hữu, nhưng kỷ luật hóa vai trò:

| Vai trò | Hướng đề xuất |
|---|---|
| Ink | Navy/ink đậm cho heading, mã đơn, số liệu chính |
| Canvas | Nền xám rất nhạt hoặc trắng ấm nhẹ; không dùng trắng tinh cho mọi lớp |
| Primary | Sky/cyan hiện hữu cho CTA và focus |
| Pulse | Teal dùng cho progress, active rail, success accent và đường chỉ |
| Semantic | Success/warning/danger/info luôn đi cùng text/icon, không chỉ dựa vào màu |
| Texture | Chỉ dùng ở login/empty/hero phụ; không đặt texture sau bảng hoặc form |

Không thêm palette mới theo từng page. Nếu cần “wow”, tạo bằng scale, crop, alignment và pulse line trước khi thêm màu.

### 3.3 Typography

- **Display:** Poppins hiện hữu, dùng cho page title, KPI và các heading cấp cao; weight 600–700.
- **Body:** Inter hiện hữu, ưu tiên 14–16px, line-height đủ thoáng cho tiếng Việt.
- **Code/data:** JetBrains Mono chỉ cho mã đơn, mã sản phẩm, trace ID và giá trị cần đối chiếu.
- Duy trì một thang chữ hữu hạn; không dùng quá nhiều size trong cùng một viewport.
- Tiêu đề lớn phải có fallback khi wrap tiếng Việt; không để “wow” tạo clipping trên tablet/mobile.

### 3.4 Layout và density

- Khung trang có khoảng thở lớn ở cấp section; bảng và form được compact vừa đủ để xử lý nghiệp vụ.
- Desktop dùng grid 12 cột; tablet chuyển thành 8/6 cột tùy route; mobile một luồng nội dung chính.
- Card chỉ dùng khi nó tạo grouping hoặc entry point; không bọc mọi hàng dữ liệu vào card.
- Một viewport nên có một primary action rõ. Các action phụ gom theo command bar hoặc menu.
- Sidebar giữ vai trò định hướng; nội dung chính không bị co quá hẹp khi sidebar mở.

### 3.5 Motif “threadline”

Threadline là motif generic lấy cảm hứng từ sợi, chuỗi cung ứng và dòng dữ liệu:

- 1–2px, dùng trong progress, timeline, active indicator, empty state hoặc hero phụ.
- Có thể phân nhánh để biểu diễn workflow; không dùng làm trang trí ngẫu nhiên.
- Không dùng bản đồ Việt Nam, logo PPJ, denim photo hoặc asset công ty trong bản rebrand độc lập.

## 4. Nguyên tắc UX

1. **Thấy trạng thái trước khi cần hỏi.** Kỳ hiện tại, deadline, quyền, trạng thái đơn và bước kế tiếp phải xuất hiện ở vùng dễ quét.
2. **Nhận biết thay vì ghi nhớ.** Label, filter context, đơn vị, lịch sử thay đổi và next action nằm gần dữ liệu liên quan.
3. **Một hành động chính cho mỗi moment.** Nút “Gửi”, “Duyệt”, “Xác nhận” phải nổi bật hơn các action phụ.
4. **Sai thì sửa được, nhưng không âm thầm.** Mutation có confirm/undo/recovery phù hợp; error nói rõ vấn đề và bước tiếp theo, không lộ raw exception.
5. **Dữ liệu lặp lại phải so sánh được.** Các cột, status, KPI và row action giữ cùng vị trí giữa các màn hình tương tự.
6. **Progressive disclosure có điều kiện.** Không giấu thông tin cần quyết định; chỉ mở dần phần nâng cao, lịch sử, evidence hoặc setting ít dùng.
7. **Accessibility là chất lượng sản phẩm.** Focus visible, keyboard order, semantic heading, text alternative, contrast, target size và reduced motion được kiểm ngay từ Figma.
8. **Motion truyền đạt thay đổi.** Dùng 150–200ms cho state transition, loading và confirmation; không dùng animation để bù cho hierarchy yếu.

## 5. Component direction

### Foundations

- Color, type, spacing, radius, shadow, z-index, motion và breakpoint tokens dưới namespace `--vpp-*`.
- Light baseline; dark mode chỉ là biến thể token, không tạo layout riêng.
- Border-first surface; shadow chỉ để phân lớp hoặc modal.
- Radius mặc định 4–6px, modal 8px; tránh pill hóa toàn bộ UI.

### Shared components cần chốt trong Figma

1. **App shell:** sidebar expanded/collapsed, header, mobile navigation, page rail.
2. **Page header:** breadcrumb/context, title, description, primary/secondary action.
3. **Period banner:** current period, deadline, open/closed, warning and next action.
4. **KPI strip:** 3–5 metrics, label/value/delta/status, responsive collapse.
5. **Command bar:** filters, search, view mode, export and bulk action states.
6. **Data table/card hybrid:** desktop grid, mobile card, column visibility, empty/loading/error.
7. **Status system:** submitted, pending, approved, rejected, cancelled, expired; text + icon + color.
8. **Wizard/stepper:** order creation, review, validation, selected tray and submit confirmation.
9. **Timeline/evidence panel:** order history, approval transitions, audit metadata.
10. **Notification:** bounded transient toast and durable inbox; no raw server detail.
11. **Insight panel:** deterministic report first, optional AI label/evidence/fallback disclosed.
12. **State panels:** loading, empty, access denied, stale period, network/reconnect and retry.

## 6. Ưu tiên màn hình và vertical slices

### Slice A — First impression + shell

- Login / registration / pending approval / recovery.
- Authenticated shell: sidebar, header, language/theme, notification, user menu.
- Acceptance: Vietnamese-first, no raw error, no layout shift, 3 viewport, keyboard/focus.

### Slice B — Employee request

- My Orders summary.
- Create order: period banner → product search/catalog → selected tray → review/submit.
- History and request detail/timeline.
- Acceptance: stale period, duplicate request, validation, mobile card, submit confirmation.

### Slice C — Management and procurement

- Department summary/all orders.
- Period review/pending approvals.
- Supplement/reject/approve states.
- Product catalog/price list/supplier and settlement preview.
- Acceptance: role-aware action visibility, snapshot/evidence explanation, no accidental mutation.

### Slice D — Report and showcase

- Report filters, KPI, trend/status/department/product breakdown.
- Export, durable notifications and optional explainable insights.
- Selected “wow” treatment: one high-value data story, not an unrelated animation.

### Route priority for Figma

1. Login + shell.
2. My Orders.
3. Create Order — both steps and validation.
4. Management/Period Operations.
5. Report.
6. Library/catalog.
7. Permission/admin and secondary account flows.

## 7. Figma file plan

Tạo một file design độc lập tên **GTAS VPP — VPP Pulse** với các page sau:

**Figma:** [GTAS VPP — VPP Pulse](https://www.figma.com/design/jguNtPeThzRM0N3o8536dE)

| Page | Nội dung | Gate |
|---|---|---|
| `00 — Brief & DNA` | DNA summary, PPJ-inspired behavior, constraints, do/don’t | Review direction |
| `01 — Foundations` | Color, type, spacing, radius, elevation, motion, responsive grid | Token review |
| `02 — Components` | Direction board và 8 reusable components có properties/variants | Component review |
| `03 — Shell` | Authenticated Shell + My Orders desktop | Shell QA |
| `04 — Employee Journey` | Create Order bước chọn sản phẩm ở desktop và mobile | Core prototype |
| `05 — Management` | Giữ chỗ cho Slice P3 sau khi Shell/Employee được triển khai | Role review |
| `06 — Reports` | Giữ chỗ cho Slice P3 sau khi data patterns được chứng minh | Data review |
| `90 — QA & Handoff` | Responsive, runtime states, design-to-code mapping và rollout gates | Handoff |

Base frames: 1440×900 để thiết kế, kiểm chứng lại ở 1920×1080; mobile 390×844; tablet 768×1024.

### Prototype đã hoàn thành

- 6 variable collections, 93 variables, 12 text styles và 2 effect styles; Light/Dark modes không thiếu giá trị, toàn bộ variable có Web code syntax.
- 8 reusable components: Button, Nav Item, KPI Card, Status Badge, Order Row, Product Row, Selection Item và Product Mobile Card.
- Shell desktop 1440×1024 cho My Orders, dùng dữ liệu và action đang tồn tại trong `Tab_Orders`.
- Create Order bước chọn sản phẩm ở desktop 1440×1024 và mobile 390×844, bám cấu trúc `Page_OrderCreate`/`OrderCreateStep2`.
- QA/Handoff page khóa Interactive Server, state matrix, responsive contract 390/768/1920, mapping sang Radzen/Blazor và vertical slices.
- Validation cuối không phát hiện thiếu variable mode, `ALL_SCOPES`, thiếu Web code syntax, font ngoài Poppins/Inter/JetBrains Mono hay component thiếu property chính.

### Handoff contract

- Mỗi component có tên, variant, state, usage note và accessibility note.
- Mỗi screen có route, role, data state, primary action và responsive behavior.
- Không dùng hình ảnh placeholder không có nguồn; motif abstract có thể dựng bằng vector/CSS.
- Figma là historical research context; browser Blazor runtime là visual authority, source/API là behavior/business authority.
- Sau khi prototype được duyệt, triển khai từng slice, mỗi slice có build/test/browser evidence riêng.

## 8. Các điểm lệch cần xử lý khi bước vào code

Audit nhanh hiện tại cho thấy nền tảng token, Radzen component và responsive test đã tốt, nhưng còn các vấn đề cần đưa vào backlog:

- Login đang có art direction minh họa mạnh, trong khi shell nội bộ còn khá generic; cần tạo một cầu nối bằng threadline/typography, không kéo toàn bộ minh họa vào bảng dữ liệu.
- Internal pages có hierarchy phẳng và nhiều khoảng trắng chưa có chủ đích; KPI, deadline và next action cần được nhóm lại.
- Một số accent/action chưa thống nhất (ví dụ CTA bổ sung khác primary rhythm); cần map về semantic/action tokens.
- Cần quét lại chuỗi tiếng Anh lọt trong critical flow, đặc biệt empty/error/validation.
- Cần loại bỏ hoặc thay thế asset/tên PPJ khi bắt đầu rebrand final theo D-009; không chụp screenshot thesis trước khi sweep.
- Các route có bảng lớn cần mobile card/scroll strategy rõ, không chỉ thu nhỏ grid.

## 9. Acceptance trước UI production

### Đã đạt trong design phase

- Art direction, palette và PPJ-independent rebrand không xung đột; Figma final không dùng logo/tên/asset PPJ.
- Foundations và component direction bao phủ loading, empty, error, disabled, success, permission và reconnect.
- My Orders desktop cùng Create Order bước chọn sản phẩm desktop/mobile đã có trace từ source → component → screen → implementation target.
- Responsive contract 390×844, 768×1024 và 1920×1080 đã được khóa trong QA/Handoff.
- Global Interactive Server được giữ nguyên; không tạo page-level descendant `@rendermode` exception.

### Phải đạt khi bước vào code/release

- Hoàn thiện Login/account states và Create Order bước review/submit trước khi gọi Employee Journey là end-to-end.
- Triển khai tablet 768×1024 theo contract và kiểm tra thực tế, không chỉ dựa vào Figma.
- Management/Report áp lại pattern đã chứng minh; không redesign nghiệp vụ hoặc quyền.
- Build/test, browser console/network, keyboard/focus, localization VI/EN, permission matrix và SignalR/reconnect smoke đều phải pass.
- Không merge/deploy nếu còn PPJ asset, raw JSON notification, horizontal overflow hoặc primary action không rõ.

## 10. Kế hoạch triển khai code

| Slice | Phạm vi | Target chính | Gate |
|---|---|---|---|
| P0 | Token + shared component alignment | `vpp-tokens.css`, `vpp-layout.css`, `VppPageHeader`, `VppMetricCard`, `VppStatusBadge`, state components | Build + frontend tests + component visual QA |
| P1 | Shell + My Orders | `MainLayout`, `LeftSidebar`, `Tab_Orders`, desktop grid/mobile card | 3 viewport + permission/action + console/network |
| P2 | Create Request | `Page_OrderCreate`, `OrderCreateStep2`, `OrderCreateStep3`, wizard CSS | draft/validation/submit + keyboard + stale-period/error |
| P3 | Management + Reports | summary/approval/period/report routes | role matrix + dense data/overflow + export states |
| P4 | Dark mode + motion polish | semantic tokens, reduced motion, secondary screens | correctness/responsive đã pass trước polish |

Không thêm frontend framework hoặc package UI mới ở P0–P2; ưu tiên Radzen và `--vpp-*` hiện hữu. Mỗi slice phải được triển khai, test và browser-QA riêng trước khi chuyển tiếp.

## 11. Nguồn nghiên cứu

- [PPJ Group — Về Tập đoàn, tầm nhìn, sứ mệnh và giá trị cốt lõi](https://ppj-group.com.vn/ve-tap-doan/)
- [PPJ Group — Lịch sử hình thành và phát triển](https://ppj-group.com.vn/lich-su-hinh-thanh-phat-trien/)
- [PPJ Group — Cải tiến và đổi mới](https://ppj-group.com.vn/phat-trien-ben-vung/cai-tien-va-doi-moi/)
- [Phong Phu International — Vision & Mission](https://www.ppj-international.com/vision-mission.html)
- [Phong Phu International — Sustainability](https://www.ppj-international.com/sustainability.html)
- [Nielsen Norman Group — Ten Usability Heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/)
- [Nielsen Norman Group — Figma visual design analysis](https://www.nngroup.com/videos/analyzing-figmas-shortcut/)
- [Material Design — Accessibility](https://m1.material.io/usability/accessibility.html)
- [W3C — WCAG 2.2 Recommendation](https://www.w3.org/TR/WCAG22/)
