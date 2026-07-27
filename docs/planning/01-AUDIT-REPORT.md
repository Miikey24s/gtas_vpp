# GTAS VPP — Audit Report

> Audit date: 14/07/2026
> Scope: toàn bộ repository ở trạng thái working tree hiện tại
> Mode: read-only đối với source, database và configuration

## 1. Phương pháp và giới hạn

Audit dựa trên:

- inventory toàn bộ file/project/reference;
- đọc controller, service, model, DTO, migration, SQL/stored procedure, Razor/CSS/JS, test, Docker/deploy/CI và tài liệu luận văn;
- Release build, backend/frontend test, NuGet vulnerability scan, EF model/migration inspection, `dotnet format --verify-no-changes`;
- chạy frontend độc lập và kiểm tra public login ở 390×844, 768×1024, 1920×1080, gồm console/network/overflow/accessibility cơ bản;
- render và xem trực quan toàn bộ 77 trang của stable Word checkpoint;
- đối chiếu với tài liệu chính thức của Microsoft, Radzen, OpenAI, OWASP và văn bản Việt Nam còn hiệu lực.

Các giới hạn cần hiểu đúng:

- Không kết nối hoặc thay đổi database thật. Kết luận về schema dựa trên model, migration và SQL trong Git; trạng thái migration trên server chưa được xác nhận.
- Không chạy Aspire AppHost vì cấu hình hiện tại có đường `MigrateAndSeed`, có khả năng làm thay đổi local DB.
- Không chạy authenticated/state-mutating UI E2E trên database dùng chung. Playwright browser cục bộ còn thiếu và suite hiện có test thay đổi permission/order.
- Từ 27/07/2026, nguồn chuẩn là `LVTN/NguyenAnNam_DH52201078.docx`; bản `working` và checkpoint Word cũ đã được loại. Mọi sửa đổi sau này phải thực hiện trên review copy bị Git ignore.
- Không kiểm tra credential/secret value và không đưa secret value vào báo cáo.

### 1.1 Decision overlay ngày 15/07/2026

Các finding dưới đây giữ nguyên như bằng chứng audit ngày 14/07; khi recommendation ban đầu có nhiều phương án, master plan áp dụng quyết định mới hơn sau:

- app-owned authentication, cutover một lần từ `GTAS_MENU` local do người dùng tự tạo; không reconnect hoặc duy trì adapter DB user công ty;
- single-company v1, bốn persona phẳng, một active group và primary department;
- cuối kỳ gom nhu cầu thành một whole-company basket, so sánh quote và mặc định một `PrimarySupplier`; line exception phải có quyền và reason;
- settlement snapshot net/VAT/gross cùng discount/fee và allocation về request line/phòng ban; đây là phân bổ đặt mua, không mở rộng sang kho/kế toán;
- rebrand GTAS VPP độc lập, dữ liệu/screenshot ẩn danh; durable in-app + CSV/Excel + email là bắt buộc;
- scope A+ core-first + polished UI, working deadline 15/08/2026, local deterministic là baseline và server chỉ khi gates green;
- toàn bộ `D-001..D-012` đã chốt; D-008 A đưa registration/activation/recovery vào A+ mandatory slice nhưng vẫn phải giữ zero-privilege trước admin activation.

## 2. Inventory và baseline

### 2.1 Repository

- 594 tracked files.
- 269 C# files, 58 Razor files, 37 CSS, 32 PlantUML, 28 SVG, 15 Python tooling scripts và 15 PNG.
- 11 project trong `gtas_vpp.sln`:
  - backend API;
  - Migrations;
  - Model;
  - Service;
  - shared DTO;
  - frontend;
  - backend tests;
  - frontend tests;
  - UI tests;
  - Aspire AppHost;
  - ServiceDefaults.
- 14 EF migration, từ `20260402021841_intialFirs` đến `20260713124130_AddPersistentNotifications`.
- SQL bootstrap có init, views, stored procedures, seed và cleanup duplicates.
- Luận văn có original/working/reference/final checkpoint, diagram source+SVG, screenshot và tooling.

### 2.2 Verification

| Check | Kết quả | Ý nghĩa |
|---|---|---|
| `dotnet build gtas_vpp.sln -c Release` | Pass, 0 warning/error | Build baseline tốt |
| Backend tests Release | 147/147 pass | Không dùng mốc cũ 132 |
| Frontend tests Release | 29/29 pass | Không dùng mốc cũ 26 |
| UI test discovery | 12 | Chưa đủ để khẳng định user journeys pass |
| UI smoke run | Block trước browser launch vì thiếu Playwright Chromium | Reproducibility issue của harness |
| NuGet vulnerable scan | Không có package vulnerable được phát hiện | Chỉ phản ánh advisory/package sources hiện tại |
| EF model | Không pending model changes | Model/migration trong source đồng bộ |
| `dotnet format --verify-no-changes` | 204 whitespace error / 34 files | Chưa có formatting gate |
| `git diff --check` | Pass | Không có whitespace patch error hiện tại |

### 2.3 Working tree được bảo tồn

Trước khi tạo planning docs đã có thay đổi người dùng tại:

- `LVTN/NguyenAnNam_DH52201078_working.docx`;
- `src/Frontend/Blazor/Components/App.razor`;
- `src/Frontend/Blazor/wwwroot/css/vpp-login.css`;
- `src/Frontend/Blazor/wwwroot/css/vpp-responsive.css`.

Không file nào trong danh sách trên bị audit chỉnh sửa. Mọi đánh giá UI là đánh giá trạng thái hiện tại, gồm cả thay đổi chưa commit đó.

## 3. Bảng tổng hợp findings

| ID | Severity | Nhóm | Finding | Hành động chính |
|---|---|---|---|---|
| SEC-01 | Critical | Deploy/Auth | Production có thể seed demo account | Tách migrate/reference/demo seed; kiểm tra và khóa account |
| SEC-02 | Critical | Secrets | Secret từng tồn tại trong Git history | Rotate, scan history, rồi mới cân nhắc rewrite |
| SEC-03 | Critical | Environment | Auth/permission/business có thể dùng khác DB | Một environment/database cho mỗi deployment |
| AUTH-01 | High | Authorization | Compatibility suy quyền quá rộng | Explicit action+scope permissions |
| DOM-04 | High | Settlement | Settlement overwrite, không immutable/company-safe | Settlement aggregate + snapshot + revision |
| DOM-02 | High | Request | Cancel = delete; settled còn sửa/hủy | State machine và invariant |
| DOM-03 | High | Supplement | Quota/pending có race; thiếu link/reason/window | Constraint/transaction/config policy |
| DATA-02 | High | Pricing | Price list thiếu supplier/effective/version/VAT semantics | Price book/contract model |
| ARCH-05 | High | Generic CRUD | Generic PATCH/DELETE bypass invariant | Typed commands; retire generic writes |
| AUTH-02 | High | Account | Reversible TripleDES cho account mới | ASP.NET Core Identity + legacy cutover |
| AUTH-03 | High | Membership | Có thể multiple group/mất department/last-admin race | Unique constraint + transactional service |
| FE-03 | High | Navigation | Route/tab/permission/fallback drift | Một typed NavigationDefinition |
| QA-01 | High | Tests | Thiếu SQL/API integration và E2E cô lập | SQL Server fixture + role matrix E2E |
| REPORT-01 | Medium/High | KPI | KPI trộn status và mutable price | Semantic metrics từ snapshot |
| FE-06 | Medium/High | UX | CSS override/design debt tích lũy | Design tokens/layers + route-by-route migration |
| FE-07 | Medium/High | Accessibility/i18n | Semantic keyboard/focus và localization chưa đồng đều | VI-first resources + WCAG foundation |
| DEP-01 | Medium | CI/Deploy | CI thiếu format/UI/migration/doc gates | Bổ sung staged quality gates |
| DOC-01 | Medium | Thesis | Final checkpoint tốt nhưng sẽ stale | Traceability và final render sau source freeze |

## 4. Architecture và code quality

### ARCH-01 — Giữ modular monolith, không rewrite

**Hiện trạng.** Backend, frontend và shared DTO cùng .NET 10; frontend đã có auth/permission/realtime/localization/E2E. Khối lượng frontend khoảng 52 C# file/8.496 LOC, 58 Razor/5.293 LOC và hơn 7.000 LOC CSS. Backend có nhiều module nghiệp vụ đã hoạt động.

**Đánh giá.** Rewrite sẽ làm mất test, permission, SignalR, deployment và nghiệp vụ đã tích lũy, trong khi không tự tạo UX tốt. .NET 10 đang LTS active đến 2028 ([support policy](https://dotnet.microsoft.com/en-us/platform/support/policy)).

**Đề xuất.** Modular monolith theo module nghiệp vụ; refactor theo “đường đi của thay đổi” thay vì di chuyển folder big-bang.

### ARCH-02 — Layering đang lẫn và class quá lớn

**Bằng chứng.** `VPPRequestService.cs` khoảng 972 dòng, `VPPRequestController.cs` 968, `LibraryController.cs` 810, `PermissionController.cs` 739, `Component_ShareGrid.razor.cs` 724 và `Tab_PagePermission.razor.cs` 663. Controller truy vấn `DbContext`, service phụ thuộc `HttpContext`, generic repository bọc EF và nhiều DTO/command model chồng nhau.

**Tác động.** Business rule khó tìm, cùng invariant có thể được thực thi khác nhau ở nhiều endpoint; AI coding dễ thêm đường ghi mới không đồng bộ.

**Đề xuất.** Với mỗi module được sửa, tạo typed application command/query/service, current-user abstraction và characterization tests; chỉ xóa đường cũ sau FE cutover.

### ARCH-03 — Shared DTO và project dependency bị coupling

**Bằng chứng.** Shared project tham chiếu EF Core; frontend cũng tham chiếu EF Core và Mapster dù chỉ nên tiêu thụ API contract. API tham chiếu Migrations, Model, Service, Shared.

**Tác động.** UI contract bị kéo theo persistence concerns, tăng khả năng dùng entity/EF attribute sai layer.

**Đề xuất.** Vẫn giữ đúng một shared DTO project theo `AGENTS.md`, nhưng dọn EF/UI labels khỏi đó; API orchestration tham chiếu abstractions thay vì migration project nếu có thể.

### ARCH-04 — Aspire/ServiceDefaults đang orphan

**Bằng chứng.** `MyAspire.ServiceDefaults` nằm trong solution nhưng backend/frontend không reference hoặc gọi `AddServiceDefaults`/`MapDefaultEndpoints`.

**Tác động.** Tạo cảm giác có observability/discovery nhưng thực tế không dùng; tăng surface bảo trì.

**Đề xuất.** Quyết định rõ ở `OBS-001`: tích hợp tối thiểu health/telemetry có đo lường hoặc loại khỏi solution sau khi xác nhận. Không giữ project “trang trí luận văn”.

### ARCH-05 — Generic CRUD là đường bypass invariant

**Bằng chứng.** `src/Backend/Api/Controllers/LibraryController.cs:634-807` map entity/DTO generic và PATCH writable property; delete đi qua `BaseGenericController.cs:123-127` và `GenericRepository.cs:104-126` để hard delete. Typed price service tồn tại song song.

**Tác động.** Rule default price, effective date, FK, soft delete và audit có thể bị bỏ qua.

**Đề xuất.** Giữ reusable server-grid/read definition nếu hữu ích; mutation phải qua typed command/service. Retire generic writes từng catalog, không một lần.

## 5. Authentication, authorization và security

### SEC-01 — Production migrator có thể tạo account demo

**Bằng chứng.** `docker-compose.prod.yml:46-57` dùng `MigrateAndSeed`; `deploy/deploy.sh:309-312` chạy migrator mỗi deploy; `src/Backend/Application/Helpers/SQL/00_Init_GTAS_MENU.sql:39-56` tạo nhiều account cùng credential compatibility; README/test chứa thông tin giải mã legacy.

**Tác động.** Database production mới hoặc chưa có ID trùng có thể nhận account có credential công khai.

**Đề xuất.** Tách `MigrateSchema`, `SeedReferenceData`, `SeedDemoData`; production chỉ chạy hai bước đầu đã review. Bootstrap admin qua one-time secret/CLI và bắt đổi password. Kiểm tra database đã deploy là một incident task cần người dùng cho phép.

### SEC-02 — Secret trong Git history

**Bằng chứng.** `.env` tồn tại trong nhiều commit cũ và từng có trường secret không rỗng; current tree đã ignore/để placeholder đúng hơn.

**Tác động.** Xóa ở HEAD không thu hồi secret đã lộ.

**Đề xuất.** Rotate database/Radzen/API keys bị ảnh hưởng; scan full history; hạn chế access; sau rotation mới cân nhắc history rewrite. Theo OWASP, secret phải có lifecycle, rotation và audit ([Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)).

### SEC-03 — Credential, permission và business data có thể dùng khác environment

**Bằng chứng.** `AuthController.cs:30-108,154-222` xác thực qua context/SP mặc định rồi nhận Test/Live từ client vào JWT; `EnvironmentResolver.cs:21-27` chọn business connection theo claim; `PermissionService.cs:22-101`, report và notification dùng `VPPContext` mặc định; registration ở `Program.cs:91-104` không thống nhất resolver.

**Tác động.** Identity/permission từ Test có thể được dùng cho operation Live hoặc ngược lại.

**Đề xuất.** Mỗi deployment chỉ bind một DB/environment. Bỏ selector và claim `Server`. Nếu bắt buộc multi-environment, key phải server-side và toàn bộ context cùng resolve; cần cross-database integration tests.

### AUTH-01 — Permission compatibility mở quyền quá rộng

**Bằng chứng.** `PermissionService.cs:140-196`: `REPORT_VIEW` suy ra own+department+all+export; một `LIBRARY_*` suy ra full manage; permission quản trị view có thể suy ra write. Nhóm User seed có `REPORT_VIEW`.

**Tác động.** Vi phạm least privilege; frontend có thể ẩn nút nhưng API vẫn cấp scope rộng.

**Đề xuất.** Lưu explicit permissions như `REPORT_VIEW_OWN`, `REPORT_VIEW_DEPARTMENT`, `REPORT_VIEW_ALL`, `REPORT_EXPORT`, module-specific view/create/update/delete/approve/settle. Compatibility chỉ là migration bridge có telemetry và ngày tắt. Blazor UI authorization không thay thế backend authorization ([Blazor security](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0)); resource ownership/department phải được kiểm tra sau khi load resource ([resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0)).

### AUTH-06 — Stored procedure tải quyền: giữ có giới hạn, không dùng làm security boundary duy nhất

**Kết luận trực tiếp.** Cách công ty yêu cầu “tải quyền bằng SP từ DB” vẫn hợp lý nếu SP chỉ là **compatibility/read-model** ổn định cho dữ liệu legacy. Ưu điểm là tận dụng schema hiện có, gom query phức tạp và dễ đối chiếu bằng SSMS. Nhược điểm là contract JSON/SP khó version, logic quyền có thể bị chia đôi giữa SQL/C#/UI, deployment dễ lệch version và test unit không bắt được hành vi SQL thật.

**Thiết kế mục tiêu.** Database vẫn là source of truth; backend tải explicit permission/action/scope qua typed repository/service và enforce bằng policy/resource handler tại từng request. Frontend chỉ nhận snapshot để điều hướng/ẩn action. Cache nếu có chỉ ở request scope hoặc TTL ngắn kèm `PermissionVersion/SecurityVersion`; mutation quyền tăng version, notification theo user yêu cầu refresh, và request tiếp theo luôn fail closed nếu version/account/membership không hợp lệ. SignalR không được coi là cơ chế thu hồi quyền.

**Điều kiện giữ SP.** Contract có version, input parameterized, không hard-code company/actor, fail-fast, SQL integration test và SSMS execution plan/result check. SP create/copy/mutation generic nên được thay bằng typed service; SP đọc legacy chỉ nghỉ sau khi có parity test, telemetry không còn consumer và migration/cutover đã được duyệt.

### AUTH-02 — Password/account lifecycle chưa an toàn

**Bằng chứng.** `TripleDesPasswordEncoder.cs:19-56` dùng reversible TripleDES với MD5-derived key; user table thiếu unique username/email, confirmation/reset/lockout lifecycle; `AuthController` chỉ có login/snapshot.

**Tác động.** Không an toàn để tạo account mới; khó thu hồi session và phục hồi tài khoản.

**Phương án.**

1. Giữ toàn bộ legacy table/cipher: ít code nhưng không chấp nhận cho password mới.
2. Tự viết hash/token/lockout: linh hoạt nhưng tái tạo security framework.
3. **Khuyến nghị đã chốt:** ASP.NET Core Identity chỉ quản lý account/password/token/lockout; giữ P02/P04/P06 làm business RBAC, không tạo role hierarchy song song. Dùng one-time verifier/import cho dữ liệu `GTAS_MENU` local, rồi tắt TripleDES/SP login; không xây provider chain lâu dài. Registration triển khai theo D-008 A: self-register → PendingApproval → admin mapping/activation, email hoặc admin recovery fallback.

Identity hỗ trợ user lifecycle, confirmation/reset và lockout ([Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0), [account confirmation/reset](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-10.0)).

### AUTH-03 — Membership có thể mất department, multiple group hoặc mất admin cuối

**Bằng chứng.** FE dùng `P04_UserGroupReqDTO` thiếu department; API upsert khác có thể ghi `Guid.Empty` tại `PermissionController.cs:637-648,680-713`. Không có filtered unique index một active group/user; check-before-insert ngoài transaction. Last-manager guard đếm permission mapping chứ không bảo đảm active account và không an toàn concurrency.

**Đề xuất.** Một command DTO; DB unique filtered index; validate company/department/group FK; transactional mutation + 409; serializable/application lock cho last-admin; break-glass account được audit.

### AUTH-04 — Tenant model chưa đủ nếu multi-company

**Bằng chứng.** P04 membership không có company; user list không lọc tenant; login lấy company từ group mapping; SP create/copy group hard-code company code.

**Đề xuất.** Nếu v1 chỉ một công ty, tuyên bố single-company và bỏ multi-tenant giả. Nếu multi-company thật, company phải nằm trong membership, period, request, settlement, permission key và mọi query. Quyết định này block schema.

### AUTH-05 — JWT/session stale và error leakage

**Bằng chứng.** JWT giữ GroupId/company/department 60 phút; permission có đọc lại DB nhưng account lock/status và business scope vẫn stale. Login rate limit chỉ theo IP; DTO chưa giới hạn rõ; error từ SP có thể trả `ex.Message`; login SP dùng `NOLOCK`/`PRINT` trên credential path.

**Đề xuất.** JWT chỉ chứa stable subject/session/security version; `ICurrentUserContext` resolve active membership mỗi request; generic auth errors + trace ID; account+IP throttling; bỏ `NOLOCK`/`PRINT` khỏi auth; notification sau commit là best-effort, không biến mutation thành 500.

## 6. Database, migration và stored procedure

### DATA-01 — Seed có thể đánh dấu thành công dù SQL lỗi

**Bằng chứng.** `SeedData.cs:83-121,168-180` catch rồi tiếp tục/ghi version; `SqlBatchExecutor.cs:22-28` warning khi file bắt buộc thiếu.

**Tác động.** Database có thể half-initialized nhưng deployment báo pass.

**Đề xuất.** Schema/reference seed fail-fast, transaction/probe, chỉ ghi version khi tất cả bước bắt buộc thành công. Demo seed là command riêng.

### DATA-02 — Price schema chưa phản ánh hợp đồng

**Bằng chứng.** `L07_PriceList` chỉ có code/name/default; `L06_VPPSupplierMapping` thiếu effective dates, contract, VAT, MOQ, lead time và supplier item code. CLR `decimal Price` map SQL `bigint`; chỉ unique default mapping, không bảo vệ duplicate active mapping.

**Tác động.** Không xác định giá nào hợp lệ tại thời điểm settlement; admin không có bằng chứng lựa chọn.

**Đề xuất.** PriceBook thuộc một supplier/contract/version, có effective interval/status/currency/VAT policy; PriceBookItem có supplier SKU, unit price, VAT, MOQ/lead time. Preflight duplicate/orphan trước constraint.

### DATA-03 — Concurrency chưa được bảo vệ ở invariant quan trọng

**Bằng chứng.** Regular/supplement quota và group assignment dùng check-then-insert ngoài transaction/constraint.

**Đề xuất.** Unique/index + short transaction + `rowversion`; retry/409 rõ ràng. EF Core hỗ trợ SQL Server `rowversion` làm concurrency token ([EF concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)).

### DATA-04 — Catalog paging đúng hướng, search/index cần chứng minh

**Hiện trạng.** Radzen LoadData/server paging phù hợp 500–1.000 item ([Radzen LoadData](https://blazor.radzen.com/datagrid-loaddata?theme=material3), [DataGrid performance](https://blazor.radzen.com/datagrid-performance)). Tuy nhiên order product lookup tải toàn bộ rồi lọc in-memory; generic grid có đường load/error không ổn định.

**Đề xuất.** Server paging/filter/sort/search toàn bộ; query projection; index theo active/category/code/name; kiểm tra execution plan bằng SQL Server. Với search không dấu, chốt collation/search-normalized strategy sau khi thử dữ liệu thật; không tự thêm full-text search nếu 1.000 item chưa cần.

### DATA-05 — Production migration strategy cần đổi

**Hiện trạng.** Deploy có migrator riêng là tốt hơn runtime backend migration, nhưng đang kết hợp seed demo và chưa có generated-script review gate.

**Đề xuất.** Generate idempotent SQL script/bundle trong CI, review, apply trên disposable copy, pre/post probe, backup/restore rehearsal. Microsoft khuyến nghị review/test migration và coi runtime migration là không phù hợp production ([EF migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)). Stored procedure phải được test trong SQL Server/SSMS theo repository instruction.

## 7. Business rules

### DOM-01 — Kỳ 05–04 tính đúng nhưng không có aggregate/state

**Bằng chứng.** `PeriodCalculator.cs:32-64` tính đúng ví dụ kỳ 07/2026. Trạng thái “closed” lại suy ra từ request có `SettledAt` ở `VPPRequestService.cs:234-239` và `PeriodSettlementService.cs:137-169`.

**Hệ quả.** Kỳ không đơn không thể đóng; có thể chốt kỳ tương lai/hiện tại; scheduler downtime khó phục hồi; không có SubmissionClosed/Pricing/Settled state.

**Đề xuất.** `Period` entity với business timezone, start/deadline/state/rowversion/company; state `Open → SubmissionClosed → Pricing → Settled`, correction/reopen là command có audit. Time check luôn server-side; scheduler chỉ nhắc/transition idempotent.

### DOM-02 — Cancelled bị xóa và settled còn mutable

**Bằng chứng.** `VPPRequestService.cs:345-390` đặt Cancelled và `IsDeleted=true`, làm đơn mất khỏi history và giải phóng unique slot. Update/cancel tại `VPPRequestService.cs:291-390` không chặn `SettledAt` nếu còn trước deadline.

**Đề xuất.** Cancel là business status, không delete. Đơn settled immutable. Nếu cho thay thế đơn hủy, tạo revision/replacement liên kết; quyết định cần người dùng xác nhận.

### DOM-03 — Supplement quota có race và policy chưa rõ

**Bằng chứng.** Max 3 và one-pending check ở `VPPRequestService.cs:185-241,677-715` trước transaction; không constraint/app lock. Không link base request, reason/window/sequence/resubmit semantics.

**Đề xuất.** Link regular/base request, reason bắt buộc, configured max (default 3 nếu xác nhận), one pending, deadline và approver rõ; rejected/cancelled quota semantics là ADR; transaction/constraint test với bốn request đồng thời.

### DOM-04 — Settlement hiện overwrite thay vì snapshot

**Bằng chứng.** `PeriodSettlementService.cs:24-129,137-169,265-275` không scope company đầy đủ, dùng một price list cho kỳ, fallback dòng đầu, rerun overwrite price/list; test `PeriodSettlementServiceTests.cs:93-113` đang bảo vệ hành vi overwrite.

**Đề xuất.** `Settlement` + `SettlementItem` immutable, idempotency key, actor/time/reason, supplier/pricebook/version/net/VAT/gross snapshot. Correction tạo revision mới; catalog change không đổi lịch sử.

### DOM-05 — UI settlement không cho chọn NCC như mô tả

**Bằng chứng.** `PeriodReviewPanel.razor:31-57` và `.razor.cs:289-315` chỉ gửi Year/Month/PriceListId; `VPP_SettlePeriodReqDTO.cs:3-8` không có supplier/selection. Service group theo item và chọn default/first mapping, nên một settlement có thể lẫn NCC mà confirmation chỉ nói price list.

**Đề xuất đã chốt theo D-006.** Dùng mô hình B+: so sánh whole-basket quote và chọn một `PrimarySupplier` cho toàn công ty/kỳ để phản ánh mua sỉ. Chỉ cho line exception khi NCC chính thiếu hàng/không có giá/MOQ không đáp ứng, kèm actor/time/reason và permission; tuyệt đối không silent fallback. Preview phải hiển thị supplier/contract/effectivity/coverage/missing/ambiguous/net/VAT/gross, discount/fee và allocation trước confirm. Mỗi settlement item vẫn snapshot `SupplierId` để giữ lịch sử và khả năng mở rộng sau này.

### REPORT-01 — KPI hiện trộn nghĩa

**Bằng chứng.** `ReportService.cs:48-179` aggregate mọi non-deleted status và dùng `Qty * CurrentSinglePrice`; price hiện có thể bị overwrite. Default FE chọn broadest permitted scope.

**Đề xuất.** Metric contract ghi rõ included status/time/scope/value basis: requested, submitted, approved và settled tách nhau. Report scope mặc định own/least-privilege; drill-down dùng cùng filter và reconcile với SQL fixture.

### NOTIF-01 — Notification durable nhưng chưa idempotent/full inbox

**Bằng chứng.** Correlation index không unique; publish không dedupe. FE fetch 20 item mới nhất và chỉ có dropdown. SignalR event có thể queue nhiều full refresh.

**Đề xuất.** Unique `(UserId, Company, Type, CorrelationId)`, idempotent insert, full paged inbox/archive/search/preferences/deep link, burst coalescing. Durable inbox là source of truth; SignalR chỉ hint. Redis/backplane chỉ khi nhiều instance thật.

### AI-01 — AI insight là nền tảng tốt nhưng thiếu governance

**Bằng chứng.** `ReportInsightService` dùng aggregate, provider abstraction, structured JSON validation, timeout, in-memory daily quota gate và deterministic fallback; OpenAI vẫn được giữ tương thích nhưng không còn là provider duy nhất.

**Thiếu.** Permission riêng, cache/persistent budget, prompt/output version/audit, evidence link, user disclosure, evaluation set và privacy review. Rate limit cơ bản hiện mới là per-process daily gate và provider HTTP fallback.

**Đề xuất.** Giữ đây là AI feature chính; không mở tool/mutation. Structured output đảm bảo schema chứ không đảm bảo nội dung đúng, nên phải deterministic-number-first, evidence và human review ([Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs), [Safety](https://developers.openai.com/api/docs/guides/safety-best-practices)).

## 8. Frontend, UI và UX

### FE-01 — Giữ Blazor/Radzen; tạo personality ở layer sản phẩm

**Đánh giá phương án.**

| Phương án | Lợi ích | Chi phí/rủi ro | Kết luận |
|---|---|---|---|
| Giữ nguyên UI | Ít thay đổi | Không giải quyết IA/CSS/personality | Không đủ |
| Radzen + custom design system/Razor | Tận dụng grid/form hiện có; tạo brand/UX riêng | Cần migration CSS/component có kỷ luật | **Chọn** |
| Thêm library UI thứ hai | Có component mới nhanh | Style/a11y/bundle/state xung đột | Không khuyến nghị |
| Rewrite React/Next | Ecosystem rộng | Viết lại auth/permission/realtime/test, hai stack | Loại trước luận văn |

Radzen hỗ trợ custom theme và server data operations ([themes](https://blazor.radzen.com/themes), [LoadData](https://blazor.radzen.com/datagrid-loaddata?theme=material3)).

### FE-02 — Login hiện có hướng tốt nhưng chưa Việt hóa/brand-safe

Public login ở ba viewport không có horizontal overflow, console error hay failed negotiation. Split layout, illustration bàn làm việc/bản đồ Việt Nam tạo personality tốt và có thể làm north-star. Tuy nhiên `html lang="en"` và visible labels còn English; logo/tên PPJ cần quyền sử dụng sau khi tách công ty.

### FE-03 — Route/permission/action drift

**Bằng chứng.** Route/tab/fallback/menu lặp trong `Helpers/RouteCatalog.cs:48-186,246-251`, `Services/PermissionState.cs:12-30`, `LeftSidebar.razor.cs:18-44`, `Component_VPPRequest.razor.cs:20-27`, `Routes.razor.cs:57-120`. `periodTab` không nằm trong query allowlist dù sidebar tạo link; preferred routes thiếu settlement và còn URL legacy.

**Đề xuất.** Một typed `NavigationDefinition` phục vụ sidebar, router guard, tab, fallback, audit và role-route tests.

### FE-04 — Admin permission/user mutation quá tức thời

**Bằng chứng.** `Tab_PagePermission.razor` toggle trực tiếp; action disabled/enable guard không nhất quán; group có hard-delete. `Tab_User.razor` đổi group/status ngay và failure rollback không chắc chắn; client ghi `DateTime.Now`.

**Đề xuất.** Staged diff, affected users, confirmation, server-authoritative result, self-lockout/last-admin guard và audit. Bỏ hard-delete khỏi routine UI.

### FE-05 — Generic grid thiếu domain validation và action semantics

**Bằng chứng.** `Component_ShareGrid.razor:79-239` reflection editor không có validator theo domain; hard delete; inspector dựa double click; action permission gộp `CanModifyGrid`.

**Đề xuất.** Reusable read/grid shell + typed domain column/editor/validation/action definitions; explicit Details button, soft delete/restore.

### FE-06 — CSS/design debt cao

**Bằng chứng.** `App.razor` tải Bootstrap, Radzen và 17 custom CSS; gần 7.000 CSS LOC, khoảng 591 `!important`, 123 hex literals, 119 inline style; nhiều breakpoint và override xung đột. Có khả năng RadzenTheme và explicit base stylesheet trùng.

**Đề xuất.** Layer `tokens/base/layout/components/pages/utilities/overrides`, chỉ 3 validation widths, migrate route-by-route, visual approval trước khi xóa legacy. Không big-bang CSS rewrite.

### FE-07 — Accessibility/localization chưa đồng đều

**Bằng chứng.** `FocusOnNavigate` tìm `h1` nhưng nhiều trang không có; page title/error/notfound còn English/plain; user avatar và order stepper dùng clickable `div`; thiếu keyboard/ARIA semantics. UI có raw exception message ở nhiều path.

**Đề xuất.** `VppPageHeader`, semantic buttons/steps, focus return, skip link, `lang=vi`, resource string và ProblemDetails-to-safe-localized-message. Target WCAG 2.2 AA và axe 0 critical/serious.

### FE-08 — Order wizard có stale/race/privacy issue

**Bằng chứng.** Edit dùng create permission; period chỉ refresh khi null nên tab mở qua mốc 05 có thể stale; autosave fire-and-forget + timer; localStorage draft sống qua logout; raw exception toast; product lookup tải toàn bộ; context nói 3 steps nhưng UI thực tế 2.

**Đề xuất.** Revalidate period/version server-side lúc submit; debounce/cancel autosave; key theo user+period, expiry/clear logout; server search; create/edit-own permission tách; confirm discard.

### FE-09 — Async/realtime có thể hiển thị dữ liệu cũ

**Bằng chứng.** Paged loads thiếu cancellation/request version; rapid filter có thể nhận response cũ sau response mới. Hai SignalR connection/circuit; initial connect failure chưa có retry rõ; inbox event trigger full refresh.

**Đề xuất.** Cancellation/request sequence, applied vs draft filters, coalesce/delta event, degraded state và polling fallback cấu hình được.

## 9. Reporting, notification và tiện ích “wow”

### Giá trị cao, ít rủi ro

1. Copy previous period kèm diff eligibility/catalog.
2. Recent/favorite/frequent items và reorder suggestion từ lịch sử.
3. Draft autosave an toàn.
4. Settlement readiness checklist.
5. Price coverage/variance và supplier comparison matrix.
6. Role-based approval/action inbox.
7. Saved filters/columns và drill-down.

### AI phù hợp

- Vietnamese executive narrative từ aggregate role-scoped.
- Giải thích biến động/điểm bất thường, nhưng mọi con số do deterministic query tính.
- Evidence link đến row/period/filter.
- Feature flag, fallback, budget, cache, permission, audit và user review.

### AI chưa nên làm

- Natural-language mutation của order.
- Tự approve/reject/settle hoặc auto-select supplier cuối cùng.
- RAG chatbot tổng quát.
- OCR hóa đơn/hợp đồng.

Các mục trên có dữ liệu/privacy implications. Luật Bảo vệ dữ liệu cá nhân 91/2025/QH15 đã có hiệu lực từ 01/01/2026 và Luật AI 134/2025/QH15 từ 01/03/2026; plan yêu cầu legal/compliance checkpoint trước production thật ([Luật dữ liệu cá nhân](https://xaydungchinhsach.chinhphu.vn/quoc-hoi-da-thong-qua-luat-bao-ve-du-lieu-ca-nhan-119250626153701582.htm), [Luật AI](https://vanban.chinhphu.vn/?classid=1&docid=216334&pageid=27160&typegroupid=3)).

## 10. Performance và scale

### Điểm đúng

- Server paging là đủ cho 500–1.000 item; chưa cần Elasticsearch.
- Modular monolith/single node phù hợp thesis; chưa cần Redis/Kafka.
- CSV giới hạn 50.000 row giảm rủi ro unbounded export.

### Rủi ro

- Order search materialize toàn bộ catalog.
- Sidebar rerender mỗi giây chỉ để hiện clock.
- Hai SignalR hubs/circuit và full refresh theo event.
- Generic query/paging cần kiểm tra projection/index/execution plan.
- Chưa có measured API p95/render baseline; không nên tự đặt số SLA thiếu dữ liệu.

### Đề xuất

- Đo baseline trước, đặt budget từ số đo, target không regress >10% nếu không có phê duyệt.
- Server projection/search/paging; cancellation; load test 1.000/10.000 item synthetic.
- Loại clock seconds hoặc isolate component.
- Scale-out/backplane chỉ sau khi có multi-instance requirement và metrics.

## 11. Testing và quality gates

### QA-01 — Coverage và loại test chưa cân bằng

Coverage line tại thời điểm audit là **11,99% tổng thể**, `gtas_vpp_be.Service` **49,71%** và API `gtas_vpp_be` **18,70%**. Số liệu được tái lập ngày 14/07/2026 bằng:

```powershell
dotnet test tests/Backend.UnitTests/gtas_vpp_be.Tests.csproj -c Release --no-restore `
  --collect:"XPlat Code Coverage" `
  --results-directory tmp/planning-audit/coverage-20260714
```

Cobertura evidence nằm trong thư mục Git-ignored `tmp/planning-audit/coverage-20260714/`. Phần lớn test dùng EF InMemory, không thể xác minh SQL Server filtered index, transaction, SP, collation hoặc rowversion.

**Đề xuất.** Không chạy theo blanket 100%. Tập trung SQL Server integration cho P0/P1 invariants, WebApplicationFactory auth matrix, concurrency và settlement snapshot.

### QA-02 — UI E2E chưa đủ tin cậy

- 12 test nhưng phần lớn assertion nằm ở một vài pixel/layout test Library.
- Có `Task.Delay`, assertion yếu như `rowCount >= 0`, luồng create/approve chưa xác nhận kết quả nghiệp vụ.
- Permission toggle có thể làm dirty shared DB nếu process chết trước finally restore.
- Accessibility smoke chỉ public login, chưa có authenticated axe.

**Đề xuất.** Isolated seeded SQL Server/database, account theo role, teardown/idempotent fixture, semantic selectors/waits, role×route matrix, console/network capture, authenticated axe và screenshots 390/768/1920.

### QA-03 — Formatting/analysis chưa thành gate

Build 0 warning nhưng lệnh `dotnet format gtas_vpp.sln --verify-no-changes --no-restore` ngày 14/07/2026 trả **204 dòng `error WHITESPACE` trong 34 file unique**. Đây là cách đếm dòng diagnostic, không phải 204 lỗi semantic độc lập. Cần baseline cleanup riêng, sau đó bật verify gate; không trộn cleanup toàn repo vào business change.

## 12. Deployment, operations và observability

### DEP-01 — CI/deployment gates chưa khép kín

Pipeline deploy có nhiều kiểm soát tốt, nhưng release gate hiện chưa bao phủ formatter/analyzer, relational integration, authenticated UI/a11y, reviewed migration artifact, secret history scan và document/diagram verification. Production migrator còn kết hợp demo seed. Vì vậy trạng thái hiện tại là “deploy script trưởng thành hơn test gate”, chưa đủ làm bằng chứng release an toàn.

#### Điểm mạnh

- CI restore/build/backend/frontend tests.
- Deploy action pin SHA, immutable GHCR tag, backup/verify, migrator, health check và application rollback.
- Backend runtime production không tự migrate trực tiếp.

#### Khoảng trống

- CI chưa chạy format/analyzer, UI suite, SQL integration, generated migration review, coverage, doc/diagram checks hoặc secret history scan.
- Production migrator kết hợp demo seed.
- SQL Server Developer edition chỉ phù hợp academic/demo, không phải licensed production.
- Off-host backup là optional; backup cùng Droplet không chống host loss.
- ServiceDefaults/Aspire chưa tích hợp; logging/trace/audit correlation chưa thành end-to-end operational story.

#### Đề xuất

- Staged CI gates để tránh pipeline quá chậm: PR fast gates, nightly/integration/visual gates, release rehearsal.
- Production migration reviewed script/bundle + pre/post probe.
- Off-host encrypted backup nếu triển khai thật; restore rehearsal bắt buộc.
- OpenTelemetry/health/structured logs ở mức vừa đủ; không triển khai observability stack nặng chỉ để có sơ đồ đẹp.

## 13. Thesis documentation

### DOC-01 — Nguồn luận văn chuẩn đã được owner duyệt

`LVTN/NguyenAnNam_DH52201078.docx`:

- 88 trang;
- 46 hình, 23 Word table;
- 117 hyperlink, gồm 99 internal và 18 external;
- không missing anchor/broken target;
- không tracked changes;
- page border chỉ áp dụng cho trang bìa; tên đề tài ngắt đúng hai dòng theo bản v5 owner duyệt;
- render Word toàn bộ cho thấy không có blank page ngoài ý muốn; khi thay bìa, trang 2–88 khớp pixel với v5 nguồn.

### DOC-02 — Tooling audit có một false positive

`audit_figure_list_format.py` đánh dấu 37/37 entry vì đòi direct run font/size, trong khi document dùng style inheritance và `audit_template_compliance_2026.py` chấp nhận. Đây là tooling drift; cần hợp nhất single source of truth và test tool trước final gate.

### DOC-03 — Nội dung sẽ stale sau source changes

Build/test count trong final checkpoint hiện đã đúng 147/29/12 discovery, nhưng architecture, schema, UI, screenshot, diagram, KPI và business rules sẽ thay đổi theo plan. Không nên “viết xong Word trước rồi sửa code cho khớp”.

### DOC-04 — Kế hoạch thesis đúng thứ tự

1. Ngay bây giờ: tạo requirement/decision/traceability skeleton, danh mục evidence và chapter gap list.
2. Sau mỗi phase: cập nhật ADR, test evidence và diagram source+SVG.
3. Sau schema/API freeze: ERD/class/sequence/activity/architecture và mô tả security/deploy.
4. Sau UI freeze: screenshot/caption/cross-reference.
5. Cuối cùng: update Word fields trong Microsoft Word, render toàn bộ, visual QA, link/page check và defense script rehearsal.

Phải giữ nguyên `LVTN/NguyenAnNam_DH52201078.docx`; chỉ làm trên review copy trong `LVTN/checkpoints/` hoặc thư mục tạm, rồi thay nguồn chuẩn sau khi owner duyệt theo `AGENTS.md`.

## 14. Kết luận audit

GTAS VPP **đủ nền tảng để trở thành luận văn tốt và sản phẩm demo thuyết phục**, nhưng chưa nên tập trung vào restyle/AI ngay. Chất lượng hiện tại là “feature-rich nhưng invariant chưa khép kín”. Việc cần làm là giảm các đường đi mâu thuẫn, biến business rule thành constraint/state/snapshot có test, rồi mới phủ UX riêng và các tiện ích nổi bật.

Master plan tương ứng nằm ở `04-MASTER-IMPLEMENTATION-PLAN.md`; các vấn đề source không thể tự quyết được cô lập trong `03-DECISIONS-REQUIRED.md`.
