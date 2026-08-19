# PRICING-IMPORT-VERSIONING-001 — Import bảng giá và tinh gọn khái niệm phiên bản

- Status: **CORE IMPLEMENTED — CURRENT PRICE WORKBOOK IS THE IMPORT TEMPLATE**
- Priority: P2
- A+ cutline class: deferred
- Path: STANDARD
- Owner/agent: Owner GTAS VPP / Codex triển khai
- Branch/base: `codex/pricing-import-versioning` @ `119ccf4d`
- Planned at (Asia/Ho_Chi_Minh): 2026-08-11
- Implementation approval: Owner mở triển khai ngày 2026-08-11; thực hiện trong worktree riêng để không xung đột task PPTX
- Related scope: Catalog and pricing, settlement history, request history, order-period settings
- Related plans: `VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md`, `MULTI-PERIOD-ORDERING-001.md`
- Current revision: `ORDERING-PRICING-REVISION-20260818.md#excel-plan` đã được triển khai; file Excel xuất từ bảng giá đang chọn hoặc bảng giá mặc định đang hoạt động cũng chính là file mẫu để chỉnh sửa và nhập lại.
- Quota snapshot: 2026-08-11 01:49, weekly-only, 867% Plus-equivalent còn lại; five-hour coverage chưa đầy đủ
- Forecast khi thực thi: 13–33% Plus-equivalent, confidence thấp do chưa có measurement cùng loại; buffer 50% → 20–50%, decision `ENOUGH`

## 0. Bản một ánh nhìn

| Mục | Trạng thái triển khai hiện tại | Chi tiết |
|---|---|---|
| Kết quả cần đạt | Nhập hàng loạt giá từ Excel/CSV, có preview và audit; người dùng chỉ thấy “phiên bản” khi thật sự cần giữ nhiều bản nghiệp vụ | [Objective](#plan-detail-objective) |
| Bảng giá | Bỏ cột/trường nhập `Phiên bản` khỏi UI; giữ field database làm compatibility contract sau consumer/schema audit | [Phân loại phiên bản](#version-classification) |
| Import chuẩn | Đã có Excel/CSV, template, preview, validation, audit và xác nhận transaction | [Luồng import](#price-import-flow) |
| Import linh hoạt | File cột lạ có màn ghép cột thủ công; Gemini tự gợi ý khi có `GEMINI_API_KEY`, nhưng người dùng vẫn phải kiểm tra trước preview | [AI boundary](#ai-boundary) |
| Mã mặt hàng | Catalog, bảng giá, file nhập/xuất và bản chốt dùng chung `ItemCode`; không còn mã mặt hàng riêng theo nhà cung cấp trong UI, API hoặc database mới nhất | [Acceptance](#plan-detail-verification) |
| Lịch sử | Mỗi lần import là một `Lần nhập`, không phải một phiên bản bảng giá | [Audit](#import-audit) |
| Phiên bản cần giữ | Bản chốt kỳ giữ user-visible; revision đơn và cấu hình kỳ giữ backend nhưng đổi UI thành lịch sử thay đổi/lịch sử áp dụng | [Phân loại phiên bản](#version-classification) |
| Phiên bản kỹ thuật | `RowVersion`, `CalculationVersion` và model/prompt version không hiển thị cho người dùng | [Phân loại phiên bản](#version-classification) |
| Liên kết chốt kỳ | Tối ưu có kiểm soát tối đa hai NCC đã chuyển sang `MULTI-SUPPLIER-SETTLEMENT-001`; PO, hợp đồng và phí vận chuyển vẫn chưa thuộc scope | [Non-goals](#plan-detail-objective) |
| Bước tiếp theo | Hoàn tất live check Gemini bằng key owner cấp; không drop `PriceList.Version` trong scope hiện tại | [Continuation](#plan-detail-continuation) |

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
- Không dùng AI để tự quyết NCC, không chia số lượng một mặt hàng và không tối ưu quá hai NCC; phần đề xuất hai NCC deterministic thuộc execution record riêng.
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
| `ItemName` | Không | Chỉ đối chiếu/hiển thị; không match tự động nếu mã sai |
| `UnitName` | Có sẵn trong file mẫu | Đối chiếu với đơn vị hiện tại; sai đơn vị bị chặn |
| `UnitPrice` | Không bắt buộc từng dòng | Để trống nghĩa là giữ nguyên giá hiện tại; nhập số không âm để thêm/cập nhật |
| `VatRate` | Không | Dùng giá trị dòng hoặc policy mặc định của bảng giá |
| `Note` | Không | Ghi chú import, không thay metadata danh mục |

File mẫu được sinh từ toàn bộ danh mục đang hoạt động và điền sẵn `ItemCode`, `ItemName`, `UnitName`, giá/VAT hiện tại nếu bảng giá đã có. Vì vậy cùng một file dùng được cho tạo giá hàng loạt, sửa giá bằng Excel và nhập lại; thao tác thủ công vẫn là mặc định.

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
- Parse mã mặt hàng, đơn giá và VAT theo schema đã duyệt.
- Kiểm tra mã mặt hàng, duplicate, range và ràng buộc database.
- Tính preview/diff và ghi transaction.

### Có thể dùng AI ở wave sau

- Đề xuất ánh xạ tên cột lạ từ file NCC sang schema chuẩn.
- Nhận biết cột giá trước/sau VAT, đơn vị và ghi chú.
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
- PO, hợp đồng và phí vận chuyển tiếp tục nằm ở hướng phát triển. Tối ưu tối đa hai NCC theo giá + VAT đã được tách sang `MULTI-SUPPLIER-SETTLEMENT-001`.

<a id="plan-detail-implementation"></a>

## 7. Implementation waves

| Wave | Nội dung | Model + effort khuyến nghị | Gate | Status |
|---|---|---|---|---|
| W0 | Refresh current tree, consumer/schema audit và chốt import template | `gpt-5.6-sol high` | Không mất dirty worktree; owner duyệt template/matching | COMPLETED |
| W1 | Đổi UI terminology, ẩn PriceList.Version, giữ compatibility backend | `gpt-5.6-terra high` | FE unit + route-real pricing review | COMPLETED |
| W2 | Import template deterministic: preview, validation, atomic upsert, audit batch | `gpt-5.6-sol high` | Backend/unit/integration + file fixtures | COMPLETED |
| W3 | Wizard UI, VI/EN, responsive/accessibility và lịch sử lần nhập | `gpt-5.6-terra high` | 4 viewport + keyboard + validation/success states | COMPLETED |
| W4a | Mapping cột linh hoạt, sample values, manual confirmation và provider-neutral AI seam | `gpt-5.6-sol high` | File cột lạ, human confirmation, AI-off fallback | COMPLETED |
| W4b | Bật adapter Gemini thật cho gợi ý cột | `gpt-5.6-sol high` | Structured output, confidence gate, manual fallback, live key check | IMPLEMENTED — LIVE CHECK PENDING |
| W5 | Migration cleanup PriceList.Version/legacy lifecycle | `gpt-5.6-sol high` | Consumer/schema audit | CANCELLED — KEEP INTERNAL |

Không ghép W5 vào W1–W3. Việc bỏ schema là một database cutover riêng và có thể bị hủy mà không ảnh hưởng tính năng import.

<a id="plan-detail-verification"></a>

## 8. Acceptance và verification

### Import

- Import 1, 100, 1.000+ dòng không yêu cầu nhập tay lại.
- File nhập/xuất chỉ nhận diện mặt hàng bằng `ItemCode`; không còn cột mã mặt hàng riêng theo nhà cung cấp.
- Duplicate item trong cùng file bị chặn rõ dòng.
- Mã không tồn tại, giá âm và VAT sai được báo trước confirm; MOQ/ngày giao/mặc định cấp dòng là cột không còn được hỗ trợ theo revision plan mới.
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
| File NCC chứa dữ liệu nhạy cảm | Rò rỉ dữ liệu | Chỉ gửi tên cột và tối đa 3 giá trị mẫu đã giới hạn độ dài; key chỉ đọc từ environment; người dùng xác nhận trước import |
| Thuật ngữ cũ còn rải rác | UI vẫn rối | Localization/source scan + architecture test cho copy bị retire |

<a id="plan-detail-continuation"></a>

## 10. Continuation

- Current status: `ACTIVE`; W0–W4a hoàn tất, W4b đã implement và còn live key check trong worktree `D:\WORK\gtas_vpp.worktrees\pricing-import-versioning`.
- Snapshot triển khai chỉ chứa source/test/design plan cần thiết; không chứa `presentation/`, `LVTN/` hoặc plan PPTX đang chạy.
- Audit xác nhận `PriceList.Version` còn tham gia snapshot chốt kỳ, price resolution và unique key. W1 chỉ bỏ khỏi UI; kết luận W5 là giữ field này ở backend.
- Template/matching giữ đúng quyết định mục 3: `ItemCode` exact là authority, import v1 chỉ thêm mới/cập nhật, không tự tạo mặt hàng và không thay thế toàn bộ.
- W2/W3 đã có audit batch additive, API preview/confirm/template/history, parser Excel/CSV, dialog responsive và import route-real trên bốn viewport.
- Collection file actions 2026-08-15: `Tải file mẫu` và `Nhập từ file` nằm tại header danh sách bảng giá; dialog chọn bảng giá đích đang hoạt động. Mỗi dòng có `Xuất Excel`; file xuất dùng cùng workbook contract với file mẫu nên có thể sửa và nhập lại.
- Sai format được xử lý theo hai tầng: file `.xlsx/.csv` có tên cột khác thì ghép cột thủ công/AI gợi ý rồi preview; file sai loại, hỏng, quá giới hạn hoặc thiếu dữ liệu bắt buộc bị chặn và không tạo mutation.
- W4a phân tích header không tạo audit batch, hiển thị sample values, cho ghép từng cột bằng decision select rồi mới preview; mapping đã dùng được lưu trong audit batch.
- W4b dùng Gemini structured output cho các cột chưa nhận diện, chỉ nhận gợi ý confidence từ 0,75, chặn target trùng/sai contract và tự fallback về ghép thủ công khi thiếu key, timeout, quota hoặc JSON lỗi. Key đọc từ `GEMINI_API_KEY`/`GOOGLE_API_KEY`, không nằm trong source hoặc appsettings.
- W5 dừng sau audit: `PriceList.Version` vẫn là compatibility contract của snapshot, price resolution và unique key; giữ nội bộ là quyết định an toàn, không còn kế hoạch drop trong scope này.
- Cleanup mã mặt hàng riêng theo nhà cung cấp hoàn tất bằng migration `20260817181622_RemoveSupplierSku`: active model/API/import/export/settlement chỉ còn `ItemCode`; LocalDB disposable đã verify fresh latest, rollback về migration trước và re-apply mà không đổi số dòng hai bảng. `Down()` chỉ tái tạo cột nullable, không khôi phục giá trị cũ đã bị drop.
- Verification checkpoint: backend/API + frontend build sạch; focused unit/architecture pass; SQL LocalDB migrate down/up + preview/confirm pass; Playwright import chuẩn và file cột lạ tại `390×844`, `768×1024`, `1366×768`, `1920×1080` pass.
- Next exact action: owner copy riêng key vào clipboard để chạy live check không lưu key; sau đó chốt W4b và commit riêng.

