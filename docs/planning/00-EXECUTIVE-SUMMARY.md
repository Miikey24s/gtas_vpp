# GTAS VPP — Executive Summary

> Ngày audit: 14/07/2026
> Decision/scope snapshot: 15/07/2026 — **A+ core-first + polished UI**, working deadline 15/08/2026
> Trạng thái: Kế hoạch đề xuất, chưa được phê duyệt để triển khai
> Nguồn bằng chứng: source, migration, SQL/stored procedure, cấu hình, test, UI chạy thực tế và tài liệu luận văn trong repository

## 1. Kết luận điều hành

GTAS VPP đã vượt xa mức “demo CRUD” thông thường của một luận văn năm 4. Repository có backend ASP.NET Core, Blazor Interactive Server, Radzen, EF Core, SQL Server, migration, stored procedure, SignalR, báo cáo, AI insight, Docker/deployment, test tự động và bộ luận văn 77 trang. Release build hiện sạch và các bộ unit test chính đều pass.

Không nên viết lại frontend bằng React/Next.js, đổi ngôn ngữ backend, tách microservice hoặc thay Radzen. Tỷ lệ lợi ích/chi phí tốt nhất là giữ **.NET 10 + Blazor + Radzen + SQL Server**, tổ chức lại dần thành **modular monolith**, xây một design system riêng quanh Radzen và hoàn thiện đúng các luồng nghiệp vụ cốt lõi. .NET 10 hiện là LTS active, patch 10.0.9 và được hỗ trợ đến 14/11/2028, nên công nghệ hiện tại không phải nguyên nhân khiến sản phẩm thiếu “wow” ([.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy)).

Điểm nghẽn thật sự không nằm ở framework mà ở ba nhóm rủi ro:

1. **Bảo mật và ranh giới môi trường:** credential có thể được kiểm tra ở database mặc định nhưng nghiệp vụ chạy theo claim Test/Live do client chọn; production migrator có thể seed tài khoản demo; secret từng xuất hiện trong Git history.
2. **Tính đúng nghiệp vụ:** quyền compatibility đang mở rộng quá mức; kỳ chưa có entity/state rõ; hủy đơn đồng thời là xóa; đơn đã chốt còn có thể sửa; giới hạn đơn bổ sung có race condition; chốt kỳ overwrite giá thay vì tạo snapshot bất biến; lựa chọn bảng giá/NCC chưa phản ánh đúng hợp đồng.
3. **Tính nhất quán sản phẩm:** route/quyền/UI lặp ở nhiều nơi, generic CRUD bypass invariant, CSS và component tích lũy nhiều lớp vá, KPI báo cáo chưa có định nghĩa nghiệp vụ ổn định, test tích hợp SQL Server và E2E xác thực còn thiếu.

Vì vậy thứ tự đúng là **containment bảo mật → sửa tính đúng dữ liệu → UX V2 → báo cáo/email/tiện ích → điểm nhấn có chọn lọc → luận văn/final rehearsal**. Scope đã chốt là **A+**: không hy sinh lõi để chạy theo hiệu ứng, nhưng UI polished, dashboard, Excel, notification in-app, email và registration an toàn vẫn là lát cắt bắt buộc. Toàn bộ `D-001..D-012` đã được chốt.

## 2. Baseline đã xác minh

| Hạng mục | Kết quả hiện tại | Nhận định |
|---|---:|---|
| Solution | 11 project, cùng target `net10.0` | Có thể giữ; dependency cần làm sạch dần |
| Release build | Pass, 0 warning, 0 error | Nền tảng tốt |
| Backend tests | 147/147 pass | Mốc cũ 132 đã lỗi thời |
| Frontend tests | 29/29 pass | Mốc cũ 26 đã lỗi thời |
| UI tests | 12 test được discovery; project build pass | Chưa chạy đầy đủ vì thiếu Playwright browser và test hiện có thể ghi dữ liệu thật |
| NuGet vulnerability scan | Không phát hiện package vulnerable từ các source hiện cấu hình | Không thay thế secret/security audit |
| EF Core | 14 migration; không có pending model change | Chưa xác nhận trạng thái pending trên database thật vì audit không kết nối DB |
| Formatting | `dotnet format --verify-no-changes` báo 204 lỗi whitespace trong 34 file | Chưa có quality gate nhất quán |
| UI public login | Kiểm tra 390×844, 768×1024, 1920×1080; không overflow/console error | Hướng hình ảnh hiện tại có personality tốt; còn English và branding cần xác nhận |
| Luận văn stable checkpoint | `LVTN/checkpoints/99_final.docx`, 77 trang, render và xem toàn bộ | Bố cục tốt; nội dung sẽ phải cập nhật sau khi source ổn định |
| Working thesis | Đang có thay đổi và bị process khác khóa khi audit | Không ghi đè; audit trực quan dùng checkpoint ổn định |

Audit không thay đổi source, database hay cấu hình. Các file đang dirty trước phiên được giữ nguyên. Ba ảnh audit tạm đã được xóa khỏi working tree sau khi kiểm tra.

## 3. Điểm mạnh nên giữ

- Stack đồng nhất C#/.NET giúp một sinh viên có thể bảo trì toàn hệ thống mà không phải vận hành hai hệ sinh thái frontend/backend.
- Radzen phù hợp các grid, form, dialog và luồng quản trị nhiều dữ liệu; server-side paging là đúng cho 500–1.000 vật tư.
- Permission đọc từ database và kiểm tra lại tại backend theo request là nền tảng đúng; SignalR chỉ nên là cơ chế làm mới UI.
- Quy ước kỳ từ 00:00 ngày 05 đến trước 00:00 ngày 05 tháng sau đang được tính đúng trong source.
- AI report insight hiện đã đi đúng hướng: chỉ dùng dữ liệu tổng hợp, structured output, `store=false`, timeout và deterministic fallback.
- CSV đã có BOM tiếng Việt, giới hạn dòng và phòng chống formula injection.
- Pipeline deploy đã có immutable image tag, backup, health check và rollback ứng dụng.
- Luận văn đã có hệ thống diagram, tooling kiểm tra, hyperlink nội bộ và render workflow tương đối trưởng thành.

## 4. Các vấn đề nghiêm trọng nhất

### P0 — xử lý ngay sau khi phê duyệt

1. **Ranh giới Test/Live không an toàn.** Login và permission có thể dùng context mặc định, trong khi Unit of Work nghiệp vụ chọn database từ claim `Server`. Khuyến nghị mỗi deployment chỉ bind một environment/database và bỏ lựa chọn Test/Live khỏi login.
2. **Production có thể seed user demo.** `docker-compose.prod.yml` chạy migrator ở chế độ `MigrateAndSeed`; seed SQL tạo tài khoản dùng credential compatibility đã xuất hiện trong repository. Production chỉ được migrate schema/reference data an toàn.
3. **Secret trong Git history.** `.env` từng chứa giá trị không rỗng. Phải coi secret liên quan đã lộ, rotate trước rồi mới cân nhắc rewrite history. Không ghi lại giá trị secret trong tài liệu hay issue.
4. **Quyền compatibility vượt least privilege.** Một quyền báo cáo cũ có thể suy ra own/department/all/export; một quyền library có thể suy ra toàn quyền manage.
5. **Chốt kỳ không bất biến.** Chạy lại settlement có thể overwrite giá lịch sử, không lọc company đầy đủ và có fallback chọn mapping đầu tiên không xác định.

### P1 — bắt buộc trước bảo vệ

- Chuyển account hiện hữu sang app-owned authentication an toàn: password one-way hash, lockout/reset và session invalidation; `GTAS_MENU` local chỉ là nguồn cutover một lần, không duy trì adapter DB công ty. Thêm self-register/`PendingApproval`/admin activation theo D-008 A, không auto-active.
- Thêm entity `Period`, state machine và quy tắc thời gian theo `Asia/Ho_Chi_Minh`.
- Sửa lifecycle đơn: Cancelled không phải deleted; Settled không được sửa/hủy; concurrency được bảo vệ bởi constraint/transaction/rowversion.
- Đơn bổ sung phải liên kết đơn gốc, có lý do/cửa sổ/approver và quota cấu hình được; enforcement tại database/transaction.
- Thiết kế price book theo supplier/hợp đồng/hiệu lực/version/VAT; gom nhu cầu thành một giỏ hàng toàn công ty/kỳ, chọn một NCC chính, snapshot settlement và phân bổ chi phí bất biến về phòng ban/đơn gốc.
- Chuyển dần generic write sang typed command/service để invariant không bị bypass.
- Có SQL Server integration test cho auth, permission, period, supplement và settlement; E2E dùng database/fixture cô lập.
- Chốt quyền sử dụng thương hiệu PPJ, logo, dữ liệu và screenshot thật; nếu không có, rebrand/anonymize trước khi chụp luận văn.

## 5. Sản phẩm mục tiêu được đề xuất

### Kiến trúc

- **Modular monolith**, một backend và một Blazor app.
- Module logic: Identity & Access, Organization, Catalog, Pricing & Supplier, Period & Request, Settlement, Reporting, Notification & Insight.
- Shared DTO duy nhất tiếp tục ở `gtas_vpp_be/gtas_vpp_shared`, nhưng bỏ dependency EF/UI concern khỏi shared project.
- Backend policy/resource authorization là security boundary; frontend chỉ điều chỉnh trải nghiệm.
- SQL Server là source of truth; SP legacy được giữ có giới hạn cho compatibility/read, command mới dùng typed service/parameterized SQL/EF Core.
- Migration production qua reviewed idempotent SQL script hoặc bundle, không runtime auto-seed. Microsoft khuyến nghị review/test migration và ưu tiên script cho production ([EF Core applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)).

### UX

- Phong cách **“Vietnamese modern enterprise”**: giữ khả năng đọc và tính kỷ luật của phần mềm doanh nghiệp, thêm illustration/microcopy/trạng thái/nhịp điệu hình ảnh riêng.
- Giữ Radzen cho DataGrid/form/dialog; custom Razor component cho app shell, page header, period banner, KPI, workflow timeline, empty/error/skeleton và settlement wizard.
- P1 dùng tiếng Việt mặc định, ngày/giờ/VND theo `vi-VN`, đồng thời resource hóa string để sẵn sàng localization. English có chọn lọc chỉ là P2 nếu giúp demo; D-012 đã defer phủ English đầy đủ và dark mode khỏi A+ cutline. .NET hỗ trợ tách resource và culture-specific formatting trực tiếp ([ASP.NET Core localization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)).
- IA theo nhiệm vụ/vai trò: Nhân viên, Quản lý phòng ban, Mua sắm/Chốt kỳ, Quản trị hệ thống.
- Tạo branch UX riêng sau khi thống nhất baseline, nhưng merge từng route nhỏ; không giữ một “UI V2” diverge lâu ngày.

### Điểm nhấn thực tiễn

Ưu tiên non-AI trước:

- Sao chép kỳ trước có diff điều kiện/giá/trạng thái vật tư.
- Vật tư gần đây/thường dùng/yêu thích và gợi ý reorder có giải thích.
- Autosave nháp theo user+kỳ, có expiry và xóa khi logout.
- Settlement readiness + price coverage + supplier comparison matrix.
- Whole-basket quote comparison, một `PrimarySupplier`, chiết khấu/phí/VAT minh bạch và allocation drill-down về phòng ban.
- Notification action center có deep link, trạng thái và chống trùng.
- Dashboard role-based, KPI có định nghĩa và drill-down.

Nếu checkpoint core còn thời gian, AI feature duy nhất được đưa vào release là **Vietnamese report narrative + anomaly explanation** dựa trên aggregate đã phân quyền, luôn có evidence link, feature flag, budget/cache/audit và deterministic fallback. OpenAI khuyến nghị structured output để bám schema và human review trước khi dùng output trong thực tế ([Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs), [Safety best practices](https://developers.openai.com/api/docs/guides/safety-best-practices)). Không cho AI mutation, duyệt đơn, chọn NCC cuối cùng hay đóng kỳ.

## 6. Phạm vi phù hợp cho luận văn

### Làm ngay vì rủi ro cao

- Secret/demo account/Test-Live containment.
- Chốt các ADR nghiệp vụ bị block.
- Explicit permission matrix và invariant user/group.
- Isolated integration/E2E test strategy.

### Bắt buộc trước bảo vệ — A+ release cutline

- App-owned account lifecycle/cutover và registration/activation/recovery an toàn.
- Period + request lifecycle + supplement concurrency.
- Price book/supplier, whole-company basket, một NCC chính, chiết khấu/VAT/phí và immutable settlement allocation.
- UX foundation, GTAS VPP rebrand, Vietnamese-first, accessibility và polished core journeys.
- KPI/dashboard đúng dữ liệu, CSV/Excel, durable in-app notification và email core có sandbox/provider fallback.
- CI/migration/backup/restore rehearsal.
- Đồng bộ luận văn, diagram, screenshot và test evidence.

### Điểm nhấn nếu core đã ổn định

- Smart reorder/copy previous period.
- Vật tư yêu thích/gần đây, anomaly/variance heatmap và saved views.
- AI report narrative có evidence.

### Để sau luận văn

- Multi-company thật, Redis/backplane, scale-out nhiều instance.
- Natural-language order mutation, chatbot/RAG, OCR hóa đơn.
- Purchase Order/kho/kế toán/AP đầy đủ.
- Mobile native/PWA offline.
- Optimizer tự động chia đơn nhiều NCC theo MOQ/lead time.
- PDF/Teams/Zalo, full English/dark mode và production-grade mail deliverability nếu external gate chưa sẵn sàng.

### Không nên làm

- Rewrite React/Next hoặc đổi backend language.
- Microservice/event bus/Kafka khi chưa có tải và team tương ứng.
- Thêm một component library song song chỉ để đổi giao diện.
- Big-bang folder refactor hoặc drop schema legacy trong cùng release cutover.
- AI ra quyết định nghiệp vụ cuối cùng.

## 7. Cơ sở Việt Nam và tuân thủ

- Vì hệ thống xử lý thông tin tài khoản/nhân viên/phòng ban, kế hoạch yêu cầu data minimization, audit access, retention/anonymization cho dữ liệu demo và kiểm soát dữ liệu gửi sang AI. Luật Bảo vệ dữ liệu cá nhân số 91/2025/QH15 có hiệu lực từ 01/01/2026; đây là checklist kỹ thuật, không thay thế tư vấn pháp lý ([Cổng TTĐT Chính phủ](https://xaydungchinhsach.chinhphu.vn/quoc-hoi-da-thong-qua-luat-bao-ve-du-lieu-ca-nhan-119250626153701582.htm)).
- AI feature phải có nhãn, human review, traceability và không tự quyết định nghiệp vụ. Luật Trí tuệ nhân tạo số 134/2025/QH15 có hiệu lực từ 01/03/2026, nên cần một legal/compliance review ngắn trước production thật ([văn bản chính thức](https://vanban.chinhphu.vn/?classid=1&docid=216334&pageid=27160&typegroupid=3)).
- Database/demo/screenshot luận văn phải dùng dữ liệu ẩn danh hoặc dữ liệu giả nếu chưa có quyền công ty.

## 8. Thứ tự thực thi đề xuất

1. Ghi ADR cho toàn bộ `D-001..D-012`; `AUTH-005`/`UI-007` đã đủ điều kiện triển khai trong A+ mandatory slice.
2. Target task đầu tiên: `BASE-001` — chốt Git/source/test baseline, inventory và decision register; chưa thay đổi nghiệp vụ hay cần chờ branding/deadline.
3. Phase 0 containment: `SEC-001`, `SEC-002`, `ENV-001`; `ENV-001` không chờ quyết định single/multi-company vì đường client chọn Test/Live phải được đóng độc lập.
4. Phase 1: permission/account integrity.
5. Phase 2: period/request/supplement/catalog correctness.
6. Phase 3: pricing/settlement immutable.
7. Phase 4: UI/UX foundation và các core journey tách nhỏ.
8. Phase 5: dashboard + Excel + durable notification + email core là bắt buộc; WOW/AI chỉ được chọn sau khi CP4 green.
9. Phase 6–7: hardening, deploy rehearsal, thesis synchronization và defense rehearsal.

Chi tiết task, dependency, phạm vi, test, acceptance và rollback nằm trong `04-MASTER-IMPLEMENTATION-PLAN.md`. Target mode phải xử lý từng task độc lập, cập nhật trạng thái bằng `05-EXECUTION-TEMPLATE.md` và không tự suy đoán các quyết định đang mở.

## 9. Điều kiện để bắt đầu coding

Chỉ cần người dùng phê duyệt **toàn bộ master plan** để bắt đầu `BASE-001`. Việc chọn A+ ở lượt này chốt release cutline, chưa phải lệnh sửa source. Trong task đầu tiên, agent phải ghi nhận owner của working tree đang dirty, khóa baseline và tạo branch theo task mà không stage thay đổi ngoài scope.

Các gate còn lại áp dụng đúng nơi cần thiết: visual approval theo route trước khi khóa screenshot; có backup và database test cô lập trước DB mutation; có owner/authority trước incident hoặc thao tác DigitalOcean. Local deterministic luôn là demo baseline; chỉ chọn server làm đường demo chính khi security/deploy/restore/email rehearsal green.
