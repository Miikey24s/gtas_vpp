# PRICING-IMPORT-VERSIONING-001 — Import bảng giá và tinh gọn khái niệm phiên bản

- Status: DEFERRED
- Priority: P2
- A+ cutline class: deferred
- Path: STANDARD
- Owner/agent: Owner GTAS VPP / Codex lập plan
- Branch/base: `Nam` @ `aac4aa1a`
- Planned at (Asia/Ho_Chi_Minh): 2026-08-11
- Implementation approval: Chưa mở; owner yêu cầu lưu plan để thực hiện sau
- Related scope: Catalog and pricing, settlement history, request history, order-period settings
- Related plans: `VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md`, `MULTI-PERIOD-ORDERING-001.md`
- Quota snapshot: 2026-08-11 01:49, weekly-only, 867% Plus-equivalent còn lại; five-hour coverage chưa đầy đủ
- Forecast khi thực thi: 13–33% Plus-equivalent, confidence thấp do chưa có measurement cùng loại; buffer 50% → 20–50%, decision `ENOUGH`

## 0. Bản một ánh nhìn

| Mục | Phương án đã chốt để triển khai sau | Chi tiết |
|---|---|---|
| Kết quả cần đạt | Nhập hàng loạt giá từ Excel/CSV, có preview và audit; người dùng chỉ thấy “phiên bản” khi thật sự cần giữ nhiều bản nghiệp vụ | [Objective](#plan-detail-objective) |
| Bảng giá | Bỏ cột/trường nhập `Phiên bản` khỏi UI; tạm giữ field database để tương thích, cleanup bằng migration riêng sau audit | [Phân loại phiên bản](#version-classification) |
| Import chuẩn | File theo template được parse deterministic, không gọi AI | [Luồng import](#price-import-flow) |
| Import linh hoạt | AI chỉ gợi ý ánh xạ cột và ghép mặt hàng từ file NCC không theo mẫu; quản lý phải xác nhận trước khi ghi | [AI boundary](#ai-boundary) |
| Lịch sử | Mỗi lần import là một `Lần nhập`, không phải một phiên bản bảng giá | [Audit](#import-audit) |
| Phiên bản cần giữ | Bản chốt kỳ giữ user-visible; revision đơn và cấu hình kỳ giữ backend nhưng đổi UI thành lịch sử thay đổi/lịch sử áp dụng | [Phân loại phiên bản](#version-classification) |
| Phiên bản kỹ thuật | `RowVersion`, `CalculationVersion` và model/prompt version không hiển thị cho người dùng | [Phân loại phiên bản](#version-classification) |
| Không thuộc scope | PO, hợp đồng, tự động lấy phí vận chuyển và tối ưu nhiều NCC vẫn là hướng phát triển riêng | [Non-goals](#plan-detail-objective) |
| Bước tiếp theo | Khi owner mở lại task: audit consumer/schema trước, sau đó làm import template trước AI và migration cleanup | [Continuation](#plan-detail-continuation) |

**Thuật ngữ:** `revision` = bản dữ liệu bất biến thay thế bản cũ; `RowVersion` = token kỹ thuật chống ghi đè đồng thời; `import batch` = một lần nhập file có kết quả và audit riêng.

<a id="plan-detail-objective"></a>

## 1. Objective và non-goals

### Objective

1. Cho phép Quản trị/Quản lý thư viện nhập hàng loạt dòng giá vào một bảng giá thay vì nhập từng mặt hàng.
2. Kiểm tra, xem trước và xác nhận trước khi database thay đổi.
3. Tinh gọn thuật ngữ để người dùng không phải hiểu nhiều loại “phiên bản” không cần thiết.
4. Vẫn giữ các cơ chế lịch sử và concurrency bắt buộc ở backend.

### Why

- Bảng giá thực tế thường có hàng trăm hoặc hàng nghìn dòng; nhập tay không phù hợp.
- `PriceList.Version` hiện vừa cho nhập/sửa thủ công, vừa được tăng khi clone, trong khi cùng bản vẫn có thể bị sửa trực tiếp. Semantics này không phải revision bất biến và gây hiểu nhầm.
- Settlement đã chụp đơn giá, VAT và phí vào snapshot bất biến, nên lịch sử chốt không phụ thuộc việc hiển thị phiên bản bảng giá.
- UI hiện dùng cùng từ “phiên bản” cho bản chốt, đơn, cấu hình kỳ, bảng giá, rowversion và calculation version dù ý nghĩa khác nhau.

### Non-goals

- Không triển khai phát hành PO hoặc quản lý hợp đồng.
- Không tự động đọc điều khoản vận chuyển từ hợp đồng NCC.
- Không tối ưu chia nhu cầu cho nhiều NCC.
- Không để AI tự tạo mặt hàng, tự quyết giá hoặc ghi database mà không có preview/xác nhận.
- Không drop ngay cột/schema hiện có trong cùng wave UI/import.

<a id="version-classification"></a>

## 2. Phân loại chuẩn cho toàn project

| Dữ liệu/cơ chế | Database | UI/copy khuyến nghị | Quyết định |
|---|---|---|---|
| Settlement revision | Giữ `RevisionNumber`, current/superseded chain và snapshot bất biến | `Bản chốt 1`, `Bản chốt 2`; chỉ hiện khi kỳ đã chốt | **KEEP — user-visible** |
| Request revision | Giữ chuỗi revision bất biến để sửa/hủy/khôi phục và audit | `Lịch sử thay đổi`; mô tả hành động, người và thời gian; không nhấn mạnh số revision | **KEEP backend — SIMPLIFY UI** |
| Order-period settings version | Giữ effective-dated settings vì kỳ cũ không được diễn giải lại | `Cấu hình hiện tại`, `Áp dụng từ MM/yyyy`, `Lịch sử áp dụng`; không dùng `v1/v2` làm thông tin chính | **KEEP backend — RENAME UI** |
| Price-list version | Tạm giữ `Version=1` và contract hiện hành để compatibility; ngừng cho user nhập/sửa | Bỏ cột, form field và suffix `· vN`; dùng tên/mã bảng giá và `Cập nhật lúc` | **DEPRECATE — REMOVE UI** |
| Price-list clone | Tạo bảng giá mới độc lập; không mặc định coi là version kế tiếp | `Sao chép thành bảng giá mới` | **RENAME** |
| Import batch | Thêm audit record riêng nếu thực thi | `Lần nhập`, file, người nhập, thời gian, kết quả | **ADD — NOT A VERSION** |
| `RowVersion` | Giữ cho optimistic concurrency | Không hiển thị trong grid/form thông thường; chỉ truyền ngầm qua API | **KEEP INTERNAL** |
| `CalculationVersion` | Giữ để biết công thức đã tạo snapshot | Không hiển thị; chỉ dùng audit/support kỹ thuật | **KEEP INTERNAL** |
| Framework/session/model/prompt version | Giữ đúng tầng kỹ thuật khi cần | Không dùng chung copy `Phiên bản` trong nghiệp vụ | **KEEP INTERNAL** |
| Contract amendment tương lai | Chỉ thêm khi có module hợp đồng thật | `Hợp đồng`, `Phụ lục`, `Lần điều chỉnh` theo thuật ngữ pháp lý | **FUTURE** |

### Quy tắc đặt tên

- Dùng **Bản chốt** khi dữ liệu tài chính đã được xác nhận và bản cũ phải bất biến.
- Dùng **Lịch sử thay đổi** cho đơn đặt hàng.
- Dùng **Lịch sử áp dụng** cho cấu hình có thời điểm hiệu lực.
- Dùng **Lần nhập** cho thao tác import file.
- Dùng **Lần cập nhật** hoặc audit log cho entity CRUD thông thường.
- Không dùng “phiên bản” để diễn giải `RowVersion` hoặc concurrency token.

<a id="price-import-flow"></a>

## 3. Luồng import bảng giá

### Entry point

- Action `Import bảng giá` thuộc collection/action bar của trang `Giá mặt hàng` hoặc context của bảng giá đang chọn.
- Action thứ cấp `Tải file mẫu` đặt cạnh import, không nằm trong row action.
- Người dùng phải chọn đúng `Nhà cung cấp` và `Bảng giá` trước khi tải file, trừ khi file chuẩn chứa ID/code có thể xác minh chắc chắn.

### Template tối thiểu

| Cột | Bắt buộc | Xử lý |
|---|---|---|
| `ItemCode` | Có | Match chính xác với mã mặt hàng hệ thống |
| `SupplierSku` | Không | Mã hàng của NCC; dùng hỗ trợ tra cứu |
| `ItemName` | Không | Chỉ đối chiếu/hiển thị; không match tự động nếu mã sai |
| `UnitPrice` | Có | Decimal không âm, tiền tệ VND theo scope hiện tại |
| `VatRate` | Không | Dùng giá trị dòng hoặc policy mặc định của bảng giá |
| `MinimumOrderQuantity` | Không | Số dương hoặc rỗng |
| `LeadTimeDays` | Không | Số ngày không âm |
| `IsDefault` | Không | Chỉ một dòng mặc định cho một mặt hàng trong bảng giá |
| `Note` | Không | Ghi chú import, không thay metadata danh mục |

### Các bước UI

1. Chọn file `.xlsx` hoặc `.csv` trong giới hạn dung lượng cấu hình.
2. Parse file và hiển thị mapping cột; template chuẩn tự mapping.
3. Validate toàn bộ file trước khi ghi.
4. Preview theo nhóm: `Thêm mới`, `Cập nhật`, `Không đổi`, `Cảnh báo`, `Lỗi`.
5. Cho lọc/xem chi tiết dòng lỗi và tải báo cáo lỗi nếu cần.
6. Chỉ enable `Xác nhận nhập` khi không còn blocking error.
7. Ghi transaction duy nhất; lỗi giữa chừng phải rollback toàn bộ.
8. Hiển thị summary và lưu lịch sử import.

### Matching và mutation

- Match mặc định bằng `ItemCode` trong phạm vi bảng giá đang chọn.
- `SupplierSku` có thể hỗ trợ nhưng không được ghi đè một match `ItemCode` khác.
- Match gần đúng theo tên chỉ là gợi ý; bắt buộc người dùng xác nhận mapping.
- Không tự tạo mặt hàng mới từ file bảng giá.
- Mode đầu tiên chỉ cần `Thêm mới và cập nhật`; chưa làm `Thay thế toàn bộ` để tránh vô hiệu hóa nhầm dòng không có trong file.
- Dòng không đổi không phát sinh update/audit noise.
- Mọi mutation dùng rowversion/transaction và kiểm tra lại bảng giá vẫn hoạt động tại thời điểm confirm.

<a id="import-audit"></a>

## 4. Audit cho lần nhập

Đề xuất entity additive `PriceListImportBatch` khi implementation bắt đầu:

- `Id`, `PriceListId`, `SupplierId`;
- tên file đã làm sạch, SHA-256 file, định dạng;
- người nhập, thời gian bắt đầu/kết thúc;
- trạng thái `Validating`, `Ready`, `Completed`, `Failed`;
- tổng dòng, thêm mới, cập nhật, không đổi, cảnh báo, lỗi;
- mapping schema/version nội bộ và thông báo kết quả;
- không lưu file nguồn vào database; lưu ngoài database chỉ khi có policy retention rõ.

Chi tiết lỗi có thể lưu ở `PriceListImportIssue` hoặc payload audit giới hạn kích thước. Không gọi batch này là phiên bản bảng giá và không dùng batch làm nguồn tính giá; dữ liệu giá đã xác nhận vẫn nằm ở bảng dòng giá hiện hành.

<a id="ai-boundary"></a>

## 5. Ranh giới AI

### Không dùng AI

- Đọc template chuẩn.
- Parse số, VAT, ngày giao và MOQ.
- Kiểm tra mã mặt hàng, duplicate, range và ràng buộc database.
- Tính preview/diff và ghi transaction.

### Có thể dùng AI ở wave sau

- Đề xuất ánh xạ tên cột lạ từ file NCC sang schema chuẩn.
- Nhận biết cột giá trước/sau VAT, đơn vị, SKU và ghi chú.
- Gợi ý mặt hàng gần giống khi file thiếu mã chuẩn.
- Giải thích cảnh báo bằng ngôn ngữ thân thiện.

### Guard bắt buộc

- AI chỉ tạo proposal có cấu trúc; không gọi API mutation.
- Hiển thị cột nguồn và giá trị nguồn cạnh proposal.
- Người dùng xác nhận mọi mapping không chắc chắn.
- Không gửi hợp đồng/dữ liệu nhạy cảm ra provider nếu chưa có policy và approval riêng.
- Import vẫn hoạt động hoàn chỉnh khi AI tắt hoặc lỗi.

<a id="plan-detail-scope"></a>

## 6. Expected scope khi thực thi

### Frontend

- `Tab_PriceLibrary`: action import, dialog/wizard upload–mapping–preview–result.
- `Tab_PriceListLibrary` và `Dialog_PriceListEditor`: bỏ copy/cột/input phiên bản; clone đổi thành bảng giá mới.
- Resource VI/EN và design-system content states cho upload/validation/error/success.

### Backend/API

- Typed import preview/confirm endpoints trong CatalogPricing ownership.
- Parser/template validator, diff engine và transactional upsert service.
- Authorization `LIBRARY_MANAGE`, company/supplier/price-list scope và idempotency cho confirm.

### Database

- Wave import: additive `PriceListImportBatch` (+ issue storage nếu cần), indexes và retention policy.
- Wave compatibility: `PriceList.Version` vẫn tồn tại và được set nội bộ.
- Wave cleanup riêng: backfill/audit consumer → bỏ user-write contract → bỏ unique key chứa Version → drop column/check constraint nếu owner duyệt.

### Documentation/thesis

- Cập nhật luận văn sau khi feature thật đã pass, không mô tả import/AI như chức năng hiện có trước đó.
- PO/hợp đồng/multi-NCC optimizer tiếp tục nằm ở hướng phát triển.

<a id="plan-detail-implementation"></a>

## 7. Implementation waves

| Wave | Nội dung | Model + effort khuyến nghị | Gate | Status |
|---|---|---|---|---|
| W0 | Refresh current tree, consumer/schema audit và chốt import template | `gpt-5.6-sol high` | Không mất dirty worktree; owner duyệt template/matching | DEFERRED |
| W1 | Đổi UI terminology, ẩn PriceList.Version, giữ compatibility backend | `gpt-5.6-terra high` | FE unit + route-real pricing review | DEFERRED |
| W2 | Import template deterministic: preview, validation, atomic upsert, audit batch | `gpt-5.6-sol high` | Backend/unit/integration + file fixtures | DEFERRED |
| W3 | Wizard UI, error report, VI/EN, responsive/accessibility | `gpt-5.6-terra high` | 4 viewport + keyboard + long file states | DEFERRED |
| W4 | AI mapping tùy chọn cho file không theo mẫu | `gpt-5.6-sol high` | Eval fixture, human confirmation, AI-off fallback | DEFERRED |
| W5 | Migration cleanup PriceList.Version/legacy lifecycle nếu vẫn hợp lý | `gpt-5.6-sol high` | SQL fresh/upgrade, reconciliation, recovery | DEFERRED |

Không ghép W5 vào W1–W3. Việc bỏ schema là một database cutover riêng và có thể bị hủy mà không ảnh hưởng tính năng import.

<a id="plan-detail-verification"></a>

## 8. Acceptance và verification

### Import

- Import 1, 100, 1.000+ dòng không yêu cầu nhập tay lại.
- Duplicate item trong cùng file bị chặn rõ dòng.
- Mã không tồn tại, giá âm, VAT sai, MOQ/lead time sai và default conflict được báo trước confirm.
- Preview counts khớp mutation thực tế.
- Confirm retry không tạo duplicate batch hoặc dòng giá.
- Một lỗi database làm rollback toàn bộ batch.
- File giống hệt có hash cảnh báo nhập trùng.
- Không log nội dung file hoặc dữ liệu nhạy cảm ngoài phạm vi audit.

### Version terminology

- Grid/form/select bảng giá không còn `Phiên bản` hoặc suffix `vN`.
- Settlement vẫn hiển thị `Bản chốt N` và xem được bản cũ.
- Request UI dùng `Lịch sử thay đổi`, không bắt employee hiểu revision number.
- Settings UI ưu tiên tháng hiệu lực/lịch sử áp dụng, không ưu tiên `vN`.
- `RowVersion` và `CalculationVersion` không xuất hiện trong UI nghiệp vụ.

### Required gates

- Backend unit + integration trên database disposable.
- EF pending-model check cho từng wave schema.
- File fixtures XLSX/CSV: UTF-8, dấu tiếng Việt, số định dạng VN/US, cột thừa/thiếu, file lớn.
- Frontend unit/architecture và route-real Playwright `390×844`, `768×1024`, `1366×768`, `1920×1080`.
- Keyboard/focus, loading, validation, filtered/error/success và no horizontal page overflow.
- `git diff --check` và review chỉ đúng scope.

<a id="plan-detail-risks"></a>

## 9. Risks và mitigation

| Risk | Tác động | Mitigation |
|---|---|---|
| Drop PriceList.Version quá sớm | Gãy API, snapshot, unique key hoặc dữ liệu legacy | Ẩn UI trước; migration cleanup riêng sau consumer/data audit |
| AI map nhầm cột hoặc mặt hàng | Ghi sai giá hàng loạt | Proposal-only, source preview, human confirm, deterministic validation |
| Partial import | Bảng giá ở trạng thái nửa cũ/nửa mới | Validate trước, transaction atomic, idempotency và batch result |
| Match theo tên quá tự tin | Gắn giá vào sai item | ItemCode exact là authority; fuzzy name chỉ gợi ý |
| File NCC chứa dữ liệu nhạy cảm | Rò rỉ dữ liệu | AI-off mặc định, policy/provider approval và minimization trước upload |
| Thuật ngữ cũ còn rải rác | UI vẫn rối | Localization/source scan + architecture test cho copy bị retire |

<a id="plan-detail-continuation"></a>

## 10. Continuation

- Current status: `DEFERRED`; plan đã được owner yêu cầu lưu ngày 2026-08-11.
- Chưa có code, API, migration, database mutation hoặc Word update thuộc task này.
- Dirty worktree hiện có nhiều thay đổi ngoài scope; khi resume phải refresh Git và không reset file owner.
- Next exact action khi owner mở task: chạy preflight backend/frontend, audit toàn bộ consumer của `PriceList.Version`, chốt file template và matching rule trước W1.
- Không làm lại: quyết định phân loại tại mục 2 và nguyên tắc import deterministic trước AI đã được ghi nhận.

