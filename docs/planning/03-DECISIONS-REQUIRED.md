# GTAS VPP — Decisions Required

> Chỉ gồm những vấn đề không thể kết luận an toàn từ source hoặc tài liệu chính thức.
> Agent không được tự triển khai task bị block khi quyết định tương ứng còn `OPEN`.

> Decision snapshot cập nhật ngày 15/07/2026. `DECIDED` nghĩa là đã chốt cho thesis release; `OPEN` chỉ còn chặn đúng task ghi trong bảng.

## Cách trả lời

Người dùng có thể trả lời ngắn theo mẫu ở cuối file, ví dụ: `D-001: A; D-002: A; ...`, rồi bổ sung điều kiện nếu cần. Mỗi quyết định sau khi chốt phải được ghi thành ADR trong `docs/decisions/` khi Target mode bắt đầu.

## Danh sách trạng thái

| ID | Quyết định | Khuyến nghị | Task bị ảnh hưởng trực tiếp | Status |
|---|---|---|---|---|
| D-001 | Mức tương thích user DB công ty | App-owned Identity; one-time cutover từ `GTAS_MENU` local, không giữ adapter công ty | AUTH-003, AUTH-006 | DECIDED |
| D-002 | Single-company hay multi-company v1 | A — single-company | AUTH-001, AUTH-002, PER-001 | DECIDED |
| D-003 | Chủ thể đơn thường và xử lý sau hủy | A — per user/revision; cuối kỳ gom toàn công ty và phân bổ lại | PER-001, REQ-001 | DECIDED |
| D-004 | Role/approval/separation of duties | A — 4 flat personas; one active group + primary department | AUTH-001, AUTH-002, SUP-001, SET-002, UI-004, UI-005 | DECIDED |
| D-005 | Policy đơn bổ sung | A — link đơn gốc, reason, one pending, max 3 cấu hình per user/base/period | PER-001, SUP-001, UI-005 | DECIDED |
| D-006 | Granularity chọn NCC | B+ — một NCC chính cho giỏ hàng toàn công ty/kỳ (mỗi effective revision); ngoại lệ có reason | PRICE-001, SET-001 | DECIDED |
| D-007 | Giá/VAT/hợp đồng | A — net price + VAT rate; snapshot net/VAT/gross | PRICE-001, SET-001, REPORT-001 | DECIDED |
| D-008 | Registration identity, activation và recovery | A — self-register → PendingApproval → admin map/activate; username/email unique, employee code unique khi có; email hoặc admin fallback | AUTH-005, UI-007 | DECIDED |
| D-009 | Quyền dùng brand/dữ liệu công ty | B — rebrand GTAS VPP độc lập và anonymize | UI-002, DOC-002 | DECIDED |
| D-010 | Kênh notification và export bắt buộc | A+B — in-app + CSV/Excel + email; PDF/Teams/Zalo defer | REPORT-003, NOTIF-002, NOTIF-003 | DECIDED |
| D-011 | Trạng thái production/secret/demo seed | Có DigitalOcean, đã `MigrateAndSeed`, credential legacy chỉ demo, user là owner | SEC-001, DEP-002 | DECIDED |
| D-012 | Deadline bảo vệ và mức scope | A+ — core-first + polished UI; target 15/08/2026; local-first/server nếu green | DOC-002, REL-001 | DECIDED |

Cột trên chỉ liệt kê **direct decision touchpoints** và phải đồng bộ với task registry. Khi decision đã `DECIDED`, task không còn bị block bởi quyết định đó; các task downstream hoặc P2/P3 bị cắt/chọn vẫn được nêu trong phần chi tiết nhưng không vì thế mà toàn bộ plan bị block.

## D-001 — Account độc lập hay còn phải tương thích `GTAS_MENU`

**Bối cảnh.** Source hiện tự tạo/localize user DB nhưng vẫn mang model/SP/password compatibility từ công ty. Người dùng nói dự án đã tách riêng để làm luận văn, nhưng chưa rõ có cần quay lại kết nối DB user công ty.

**Lựa chọn.**

- **A — App-owned Identity, legacy là adapter/import có thời hạn.**
  - Ưu: password/token/lockout/reset chuẩn; demo độc lập; kiến trúc mở cho provider công ty sau này.
  - Nhược: cần migration/cutover, mapping user↔employee và feature flag.
- **B — Giữ `GTAS_MENU` là source chính, chỉ thêm registration vào đó.**
  - Ưu: ít thay đổi schema hiện tại.
  - Nhược: giữ reversible password/legacy coupling; registration/reset khó an toàn; luận văn phụ thuộc model công ty.
- **C — Hỗ trợ hai source active lâu dài.**
  - Ưu: linh hoạt.
  - Nhược: duplicate identity/conflict/revocation và test matrix lớn; over-engineering cho một sinh viên.

**Khuyến nghị:** A. Identity chỉ quản lý account/password/token; P02/P04/P06 tiếp tục là business RBAC. Legacy verifier/import tắt sau cutover, không drop ngay.

**Quyết định 15/07/2026:** Chọn A theo biến thể tối giản. `GTAS_MENU` là schema local do người dùng tự tạo để mô phỏng công ty, không cần reconnect DB user công ty. Giữ local account/business-RBAC cần thiết bằng **một lần import/mapping có kiểm kê và collision report**; account demo phải được reset/seed credential mới, không mang password legacy sang. Sau cutover tắt TripleDES/SP login, không duy trì external-user provider giả tạo.

**Nếu chưa quyết định:** Không thể thiết kế registration, unique identity, password migration hoặc session invalidation.

**Task bị ảnh hưởng:** `AUTH-003`, `AUTH-004`, `AUTH-006`, `UI-004`, `DOC-002`.

## D-002 — Single-company hay multi-company thật trong v1

**Bối cảnh.** Source có company code nhưng membership/query chưa tenant-safe, SP còn hard-code company. UI Test/Live không phải multi-tenancy.

**Lựa chọn.**

- **A — Single-company v1.**
  - Ưu: scope rõ, ít schema/query/permission risk, phù hợp thesis.
  - Nhược: chưa demo nhiều pháp nhân.
- **B — Multi-company thật.**
  - Ưu: scale product tốt hơn.
  - Nhược: company phải vào mọi membership/period/request/settlement/permission/query/index; test isolation lớn.
- **C — Giữ trạng thái “nửa multi-company”.**
  - Ưu: gần source hiện tại.
  - Nhược: nguy hiểm nhất vì tạo cảm giác isolation nhưng có thể leak.

**Khuyến nghị:** A; ghi rõ luận văn là single-company v1, thiết kế key không cản migration sau này.

**Quyết định 15/07/2026:** Chọn A — single-company thesis release.

**Nếu chưa quyết định:** Block environment, membership, period và settlement schema.

**Task bị ảnh hưởng:** trực tiếp `AUTH-001`, `AUTH-002`, `PER-001`; downstream `SET-002`, `REPORT-001`. `ENV-001` độc lập với tenant model.

## D-003 — Một đơn thường thuộc ai và làm gì sau khi hủy

**Bối cảnh.** Source unique theo user/kỳ. Cancel hiện đồng thời soft-delete nên user có thể tạo đơn khác, nhưng lịch sử bị mất.

**Lựa chọn.**

- **A — Một regular request per user/period; hủy giữ lịch sử; tạo replacement revision có link.**
  - Ưu: bám source, audit tốt, không nhầm với “xóa”.
  - Nhược: UI/revision cần rõ.
- **B — Một request per department/period.**
  - Ưu: gần quy trình tổng hợp phòng.
  - Nhược: cần collaboration/ownership/locking; thay đổi lớn so với source.
- **C — Hủy xong tạo mới độc lập, không link.**
  - Ưu: đơn giản.
  - Nhược: mất traceability, dễ lách giới hạn.

**Khuyến nghị:** A. Department summary là query/aggregate, không biến department thành owner.

**Quyết định 15/07/2026:** Chọn A. Mỗi nhân viên vẫn sở hữu một regular request/kỳ; khi hết nhận đơn, hệ thống gom các request hợp lệ thành một whole-company procurement basket. Hàng và chi phí dự kiến được phân bổ ngược về request/phòng ban bằng immutable allocation snapshot, không đổi owner của đơn gốc và không được mô tả như bằng chứng đã nhập kho/giao hàng.

**Nếu chưa quyết định:** Không thể chốt unique index, lifecycle, copy/replacement và report denominator.

**Task bị ảnh hưởng:** `REQ-001`, `PER-001`, `REPORT-001`, `UI-003`.

## D-004 — Vai trò, người duyệt và separation of duties

**Bối cảnh.** Source có group/permission linh hoạt nhưng compatibility mở quá rộng; chưa có business definition chắc chắn ai duyệt supplement, chốt kỳ hoặc được tạo correction/reopen sau chốt.

**Lựa chọn.**

- **A — 4 flat personas:** Employee, Department Approver, Procurement/Period Admin, System Admin; manager duyệt supplement, procurement settle, system admin không tự có procurement.
  - Ưu: least privilege, demo dễ hiểu, phù hợp doanh nghiệp.
  - Nhược: cần migration permission/mapping.
- **B — Chỉ Admin/User.**
  - Ưu: đơn giản.
  - Nhược: admin quá mạnh, không thể hiện scope/phê duyệt thực tế.
- **C — Hierarchical group inheritance.**
  - Ưu: linh hoạt tổ chức lớn.
  - Nhược: source hiện chưa evaluate inheritance; khó test/giải thích; có nguy cơ privilege escalation.

**Khuyến nghị:** A, flat permissions. Four-eyes: người tạo không tự duyệt supplement của mình; người settlement không được thay đổi permission bằng cùng role. Correction/reopen chỉ thuộc Procurement/Period Admin có permission riêng, reason bắt buộc và nếu có hai người phù hợp thì người xác nhận correction khác người khởi tạo.

**Quyết định 15/07/2026:** Chọn A, một active group và một primary department cho mỗi account trong v1.

**Quy ước triển khai đã chốt:** v1 chỉ có một active group + một primary department cho mỗi account; nếu cần nhiều nhóm/phòng ban sẽ mở thành decision riêng sau thesis, không suy diễn bằng inheritance.

**Nếu chưa quyết định:** Không thể chốt permission seed, approval workflow, dashboard và E2E roles.

**Task bị ảnh hưởng:** trực tiếp `AUTH-001`, `AUTH-002`, `SUP-001`, `SET-002`, `UI-004`, `UI-005`; downstream `QA-003`.

## D-005 — Chính sách “tối đa 3 đơn bổ sung”

**Bối cảnh.** Con số 3 đang hard-code/check trước transaction; source không cho biết căn cứ nghiệp vụ, quota tính theo gì hoặc rejected/cancelled có tính không.

**Lựa chọn.**

- **A — Configured max, mặc định 3, tính per user + regular request + period; one pending; rejected/cancelled không chiếm quota cuối nhưng mọi attempt có audit; reason bắt buộc; phải có regular gốc.**
  - Ưu: bám hiện trạng, linh hoạt, kiểm toán được.
  - Nhược: cần sequence/revision/constraint transaction.
- **B — Max per department/period.**
  - Ưu: kiểm soát phòng ban.
  - Nhược: cạnh tranh quota giữa nhân viên, khó giải thích fairness.
- **C — Không giới hạn số lượng, chỉ approval.**
  - Ưu: đơn giản rule.
  - Nhược: spam/quy trình mua sắm kém ổn định.

**Khuyến nghị:** A. Deadline cấu hình trong Period, không hard-code ngày khác; approver và rejected/resubmit semantics phải nằm trong audit/state transition.

**Quyết định 15/07/2026:** Chọn A. Default tạo supplement không muộn hơn regular submission deadline; approval có thể tiếp tục trong `SubmissionClosed` đến `SupplementApprovalDeadline` cấu hình của Period. Rejected/cancelled không chiếm quota cuối nhưng mọi attempt/revision được audit. Để chống spam mà không biến thành quota nghiệp vụ, thêm `MaxAttempts` cấu hình riêng (mặc định 6 cho demo); `MaxApproved=1` là quota nghiệp vụ mặc định hiện hành.

**Nếu chưa quyết định:** Không thể thiết kế constraint/quota/report supplement rate.

**Task bị ảnh hưởng:** `SUP-001`, `PER-001`, `REPORT-001`, `UI-005`.

## D-006 — Chọn NCC cho toàn kỳ hay từng vật tư

**Bối cảnh.** UI hiện chỉ chọn price list; service có thể chọn nhiều mapping/NCC ngầm. Người dùng muốn admin chọn NCC, nhưng chưa xác định granularity.

**Lựa chọn.**

- **A — Per item + batch shortcut theo supplier/price book.**
  - Ưu: thực tế, so sánh rõ, hỗ trợ thiếu hàng/giá; tạo điểm nhấn luận văn.
  - Nhược: schema/preview/UI/settlement phức tạp hơn; có thể dẫn đến nhiều NCC.
- **B — Một NCC/price book cho toàn kỳ.**
  - Ưu: MVP đơn giản, dễ vận hành và giải thích.
  - Nhược: coverage thấp/giá kém tối ưu, khó xử lý item NCC không bán.
- **C — Chọn theo category.**
  - Ưu: cân bằng số thao tác.
  - Nhược: category không luôn trùng hợp đồng/coverage; thêm rule trung gian.

**Khuyến nghị audit ban đầu:** A, nhưng có “Apply to all compatible items” để UX gần B. Không mở full PO module trước bảo vệ.

**Nếu deadline rất gần:** chọn B làm release scope, nhưng settlement item vẫn snapshot SupplierId để migration A sau này.

**Quyết định 15/07/2026:** Chọn B+ theo quy trình thực tế: gom toàn bộ nhu cầu công ty, so sánh **whole-basket quote** và mặc định một `PrimarySupplier` cho mỗi effective settlement revision của kỳ để tận dụng mua sỉ/chiết khấu. Correction có thể đổi supplier bằng revision mới có reason/actor; revision cũ bất biến và latest effective revision là lựa chọn vận hành của kỳ. Line exception chỉ dùng khi NCC chính thiếu hàng/không có giá, phải có actor/time/reason và quyền riêng; không có silent fallback. Mỗi settlement item vẫn snapshot SupplierId để lịch sử đúng và schema không bị khóa.

**Nếu chưa quyết định:** Block price book schema, settlement API/UI/report và demo story.

**Task bị ảnh hưởng:** `PRICE-001`, `SET-001`, `SET-002`, `REPORT-001/002`, `UI-006`.

## D-007 — Giá gồm VAT chưa, VAT và hiệu lực được xác định thế nào

**Bối cảnh.** Source có default VAT constant 8%; price model thiếu effective/version semantics. Không thể suy ra hợp đồng thực tế.

**Lựa chọn.**

- **A — Lưu unit price chưa VAT + VAT rate theo price-book item/contract; gross là derived và snapshot cả net/rate/gross lúc settlement.**
  - Ưu: minh bạch, report/reconcile tốt, hỗ trợ thay đổi chính sách.
  - Nhược: cần dữ liệu VAT chính xác và migration.
- **B — Mọi giá đã gồm VAT.**
  - Ưu: UI đơn giản.
  - Nhược: khó so sánh hợp đồng và không giải thích thuế.
- **C — Global VAT constant.**
  - Ưu: ít trường.
  - Nhược: sai khi item/hợp đồng/thời điểm khác nhau; hard-code 8% không bền.

**Khuyến nghị:** A. Price book phải versioned, EffectiveFrom/To, Published/Expired; overlap mơ hồ là blocker.

**Quyết định 15/07/2026:** Chọn A. Giá lưu chưa VAT, VAT lấy từ price-book item/contract và snapshot net/rate/gross khi chốt.

**Nếu chưa quyết định:** Không thể định nghĩa amount/KPI/export/snapshot.

**Task bị ảnh hưởng:** `PRICE-001`, `SET-001/002`, `REPORT-001/002`, `DOC-002`.

## D-008 — Registration identity, activation và recovery

**Bối cảnh.** Người dùng muốn thêm đăng ký; hệ thống nội bộ không nên public auto-active. Chưa rõ có email service và unique business identifier nào đáng tin.

**Lựa chọn.**

- **A — Self-register → PendingApproval → admin map employee/department/group → activate. Username và email unique; employee code unique nếu có. Email confirm/reset khi có provider; nếu chưa có, admin activation/reset v1.**
  - Ưu: demo registration, vẫn least privilege.
  - Nhược: admin workload và cần quy trình duplicate/mapping.
- **B — Admin/invite-only.**
  - Ưu: doanh nghiệp an toàn nhất.
  - Nhược: ít “self-service”, demo registration kém rõ.
- **C — Public auto-active.**
  - Ưu: ít bước.
  - Nhược: abuse và account không thuộc nhân viên/phòng ban; không khuyến nghị.

**Quyết định 15/07/2026:** Chọn A cho thesis release. User tự đăng ký với username và email bắt buộc unique; employee code unique khi được cung cấp. Account mới vào `PendingApproval`, zero privilege; admin phải map employee/primary department/active group rồi mới activate. Email confirmation/reset dùng sender đã cấu hình; nếu provider thật chưa sẵn sàng thì dùng admin-only activation/reset có audit, không auto-active. Rate limit và anti-enumeration là bắt buộc.

Email notification ở D-010 vẫn triển khai độc lập; D-008 A đưa registration/activation/recovery vào A+ mandatory slice nhưng không thay đổi auth/request/settlement hiện hữu.

**Task bị ảnh hưởng:** `AUTH-005`, `UI-007`; `NOTIF-001` chỉ dùng in-app activation event và không phụ thuộc lựa chọn external channel.

## D-009 — Quyền sử dụng thương hiệu, logo, tên công ty và dữ liệu

**Bối cảnh.** Login/luận văn có PPJ Group/Phong Phú và có thể có screenshot/dữ liệu công ty, trong khi dự án đã tách riêng.

**Lựa chọn.**

- **A — Có quyền rõ ràng để tiếp tục dùng trong luận văn/demo.**
  - Ưu: giữ context thật.
  - Nhược: vẫn cần anonymize personal/secret data.
- **B — Không có/không chắc. Rebrand thành GTAS VPP demo và anonymize toàn bộ seed/screenshot.**
  - Ưu: giảm rủi ro IP/privacy, sản phẩm độc lập hơn.
  - Nhược: cần thay asset/text/screenshot/Word.

**Khuyến nghị:** Nếu không có xác nhận bằng văn bản hoặc quy định trường/công ty rõ, chọn B. Không đoán rằng “dùng cho học tập” tự động cho phép.

**Quyết định 15/07/2026:** Chọn B. Rebrand thành GTAS VPP độc lập; thay logo/tên/asset PPJ và anonymize seed, screenshot, Word/evidence trước final.

**Nếu chưa quyết định:** Không nên đóng băng design system hoặc chụp screenshot final.

**Task bị ảnh hưởng:** `UI-002`, `DOC-002`, `DOC-003`, `REL-001`.

## D-010 — Kênh notification và loại export bắt buộc

**Bối cảnh.** Source có in-app/SignalR/CSV. Người dùng muốn thực tiễn hơn nhưng chưa nêu kênh doanh nghiệp và mẫu biểu.

**Lựa chọn.**

- **A — In-app durable inbox + CSV/Excel trước bảo vệ; PDF/email/Teams/Zalo defer nếu không có yêu cầu/mẫu.**
  - Ưu: thực tiễn, test ổn định, không cần external credential.
  - Nhược: ít integration “bên ngoài”.
- **B — Thêm email.**
  - Ưu: phổ biến.
  - Nhược: provider/domain/template/delivery/PII/secret/queue.
- **C — Teams/Zalo OA.**
  - Ưu: tiện cho tổ chức cụ thể.
  - Nhược: external API approval, credential và scope tăng mạnh.

**Khuyến nghị:** A. Chỉ thêm channel khác khi có use case, credential sandbox và acceptance rõ.

**Quyết định 15/07/2026:** Chọn A+B: durable in-app + CSV/Excel + **email notification bắt buộc** vì người dùng rời ứng dụng vẫn cần nhận tin. PDF, Teams và Zalo OA để sau. Email dùng adapter/outbox/template/retry; in-app vẫn là source of truth và deep link luôn recheck permission.

**Mặc định thực thi:** Nếu repository/trường không có mẫu Excel nghiệp vụ bắt buộc, dùng workbook chuẩn gồm metadata, summary, item detail và department allocations; không chờ thêm quyết định. Mẫu mới chỉ là change request sau này.

**Nếu chưa quyết định:** Không block durable in-app inbox, CSV hiện có hoặc role dashboard core. Chỉ block Excel/PDF và external channel tasks; luận văn không được claim những integration chưa chọn.

**Task bị ảnh hưởng:** trực tiếp `REPORT-003`, `NOTIF-002`, `NOTIF-003`; `REPORT-004` đã `DEFERRED`, và `DOC-002` chỉ ghi feature đã thực sự hoàn thành.

## D-011 — Production đã chạy demo seed chưa và secret legacy có phải dữ liệu thật không

**Bối cảnh.** Đây là xác nhận sự cố, không phải lựa chọn thiết kế. Source chứng minh deployment có khả năng seed demo và Git history từng chứa secret, nhưng audit không được kết nối production.

**Cần người dùng trả lời.**

1. Có production/staging/demo server đang hoạt động không?
2. Server đó đã từng chạy `MigrateAndSeed` chưa?
3. Credential compatibility/legacy key trong repository từng dùng với account/dữ liệu công ty thật hay chỉ demo?
4. Ai có quyền rotate DB/Radzen/OpenAI/khác và kiểm tra account?

**Khuyến nghị:** Tạm coi đã compromised cho đến khi chứng minh ngược lại; rotate/disable/audit theo runbook, không in secret vào chat/log.

**Xác nhận 15/07/2026:** Có DigitalOcean đang hoạt động; đã từng chạy `MigrateAndSeed`; credential compatibility chỉ dùng demo, không phải dữ liệu công ty thật; người dùng là owner có quyền rotate/kiểm tra. Đây là P0 cần làm ngay sau khi master plan được phê duyệt, nhưng câu trả lời này không tự cấp quyền triển khai trong phiên planning.

**Nếu chưa xác nhận:** `SEC-001`/`SEC-002` phải dừng ở planning/local code; không được tự chạm hệ thống ngoài repository.

**Task bị ảnh hưởng:** trực tiếp `SEC-001`, `DEP-002`; downstream `REL-001`. `SEC-002` và `DEP-001` vẫn có thể làm trên repository/local environment.

## D-012 — Deadline bảo vệ và release scope

**Bối cảnh.** Plan có nhiều phase; không biết ngày nộp Word, ngày demo/bảo vệ, thời gian review của GVHD và môi trường demo.

**Thông tin người dùng đã cung cấp.**

- working deadline: một tháng, dùng mốc 15/08/2026;
- demo local hoặc server có Internet; local deterministic là fallback;
- người dùng làm full-time cho luận văn.

Ngày gửi GVHD/nộp final/bảo vệ chính thức chưa được nêu; đây là lịch trường, không block master plan. Khi có lịch thật, chỉ cần điều chỉnh timebox DOC/REL, không tự mở rộng feature.

**Lựa chọn scope.**

- **A — Core-first:** hoàn tất P0, auth, period/request, pricing/settlement, UX core, report và thesis; chỉ một AI feature.
- **B — Feature-heavy:** thêm nhiều AI/integration, chấp nhận giảm hardening.
- **C — UI-heavy:** ưu tiên visual, giữ nghiệp vụ cũ.

**Khuyến nghị:** A. Task “wow” chỉ bắt đầu khi checkpoint core pass.

**Quyết định 15/07/2026:** Chọn **A+ — core-first + polished UI**, làm full-time với working deadline **15/08/2026**; demo local hoặc server. P0 và minimum P1 auth/data/settlement correctness là safety floor; UI/UX V2, dashboard, Excel, in-app/email và demo journey vẫn là scope bắt buộc. Local deterministic là baseline; DigitalOcean là đường demo chính chỉ khi security/deploy/restore gates green.

Nếu tiến độ trượt, cắt P2/P3 trước, không cắt safety floor. Full English/dark mode, PDF, Teams/Zalo, full PO/inventory/accounting và AI mutation nằm ngoài release này.

**Nếu chưa quyết định:** Có thể thực thi theo dependency nhưng không thể chốt cutline “trước bảo vệ / sau luận văn”.

**Task bị ảnh hưởng:** trực tiếp `DOC-002`, `REL-001`. A+ đưa `AUTH-005`, `UI-001..007`, REPORT-001..003, NOTIF-001/002 và QA tương ứng vào mandatory cutline; NOTIF-003 phụ thuộc server/provider external gate. `REPORT-004` đã defer; WOW/AI chỉ được chọn sau CP4 và không block release nếu bị cắt.

## Snapshot quyết định

Toàn bộ `D-001..D-012` đã được chốt cho thesis release; không còn decision blocker trong master plan. Normative ADR và quan hệ direct/downstream được khóa tại [`docs/decisions/000-index.md`](../decisions/000-index.md).
