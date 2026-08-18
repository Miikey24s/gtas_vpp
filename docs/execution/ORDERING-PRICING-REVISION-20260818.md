# ORDERING-PRICING-REVISION-20260818 — Điều chỉnh luồng kỳ, đơn bổ sung và bảng giá

- Status: **IMPLEMENTED — FOCUSED QA PASS**
- Date: `2026-08-18`
- Scope: kỳ đặt hàng, đơn bổ sung, điều chỉnh sau chốt, bảng giá Excel, điều khoản thương mại và lựa chọn nhà cung cấp
- Replaces current product direction in: `MULTI-PERIOD-ORDERING-001`, `MULTI-SUPPLIER-SETTLEMENT-001` where this document says otherwise
- Current-code audit: [BUSINESS-FLOW-AUDIT-20260818](BUSINESS-FLOW-AUDIT-20260818.md)

## 0. Bản một ánh nhìn

| Mục | Quyết định/khuyến nghị hiện tại | Chi tiết |
|---|---|---|
| Thời hạn chỉnh đơn 10 ngày | Đã enforce theo từng kỳ; mặc định `10`, cấu hình tại Quản trị hệ thống và được ghi đè khi `Thêm kỳ` | [Plan kỳ](#period-plan) |
| Đơn bổ sung | Chọn **phương án B**: chỉ tạo cho kỳ đã đóng, trong 5 ngày sau ngày đóng; chốt sớm thì dừng nhận bổ sung ngay | [Luồng đơn bổ sung](#supplement-flow) |
| Một hay nhiều NCC | Chưa chốt. Khuyến nghị vận hành thật bằng **một NCC** trước; nhiều NCC chỉ nên là mô phỏng cho đến khi có dữ liệu tổng chi phí đáng tin cậy | [Phân tích NCC](#supplier-analysis) |
| Điều khoản thương mại ẩn | Không comment-out code. Dùng policy/feature flag tắt tập trung, ép dữ liệu mới về `0` và không cho dữ liệu cũ âm thầm ảnh hưởng kết quả | [Tạm tắt an toàn](#commercial-terms) |
| Excel bảng giá | Đã tinh gọn file mẫu/parser/preview/apply còn 6 cột nghiệp vụ; cột thừa chỉ cảnh báo và bỏ qua | [Excel bảng giá](#excel-plan) |
| Tự mở kỳ | Hệ thống chỉ tự tạo/mở **một kỳ theo lịch bình thường**; Quản lý dùng `Thêm kỳ` khi cần đặt trước | [Plan kỳ](#period-plan) |
| Sau chốt | Quản lý tạo yêu cầu sửa/hủy đơn, quản lý khác duyệt, sau đó chốt lại để tạo `Bản chốt N+1`; không hard delete | [Điều chỉnh sau chốt](#post-settlement) |
| Refactor service lớn | **Tạm hoãn**, không trộn vào thay đổi nghiệp vụ trên | [Phạm vi tạm hoãn](#deferred-scope) |

### Trình tự triển khai đã thực hiện

1. Sửa đúng luồng đơn bổ sung vì đây là sai lệch nghiệp vụ trực tiếp.
2. Đổi scheduler sang tự mở một kỳ và thêm luồng `Thêm kỳ` thủ công.
3. Tinh gọn contract/file Excel.
4. Tạm tắt điều khoản thương mại bằng policy tập trung.
5. Enforce cửa sổ chỉnh đơn 10 ngày theo snapshot của từng kỳ.
6. Giữ nhiều NCC và refactor service ở trạng thái hoãn để không trộn thêm thay đổi lớn.

<a id="supplement-flow"></a>

## 1. Luồng đơn bổ sung — phương án B

### 1.1. Quy tắc nghiệp vụ

```text
Kỳ đang mở
  └─ Chỉ nhận đơn thường; chưa nhận đơn bổ sung cho kỳ này

Đến ngày đóng kỳ
  ├─ Dừng tạo/sửa đơn thường
  └─ Mở cửa sổ đơn bổ sung trong 5 ngày

Trong 5 ngày sau đóng
  ├─ Nhân viên có đơn thường gốc được tạo đơn bổ sung
  ├─ Quản lý duyệt hoặc từ chối
  └─ Quản lý được chốt sớm nếu không còn đơn bổ sung chờ duyệt

Khi đã chốt sớm hoặc hết 5 ngày
  ├─ Không tạo thêm đơn bổ sung
  ├─ Không duyệt thêm đơn bổ sung
  └─ Đơn còn chờ chỉ được từ chối để dọn hàng chờ
```

Điều kiện tạo đơn bổ sung chuẩn:

- kỳ ở `SubmissionClosed`, chưa `Pricing/Settled`;
- `SubmissionDeadlineUtc <= now < SupplementApprovalDeadlineUtc`;
- người dùng có một đơn thường hiện hành hợp lệ trong kỳ để làm đơn gốc;
- chưa có đơn bổ sung `Pending` khác và chưa vượt quota hiện hành;
- mọi mặt hàng vẫn chịu giới hạn số lượng theo từng đơn như đơn thường.

### 1.2. Chốt sớm và chống xung đột

- Nếu còn bất kỳ đơn bổ sung `Pending`, preview/chốt kỳ phải báo rõ số đơn đang chờ và chặn xác nhận.
- Nếu không còn đơn chờ, quản lý được chốt trước khi hết 5 ngày.
- Tại transaction xác nhận chốt, backend kiểm tra lại lần cuối để ngăn trường hợp một đơn bổ sung vừa được gửi sau lúc quản lý mở preview.
- Khi kỳ thành `Settled`, API tạo/duyệt bổ sung bị khóa ngay, dù mốc 5 ngày chưa hết.
- Từ chối đơn bổ sung quá hạn vẫn được giữ để hàng chờ không làm kỳ bị kẹt.

### 1.3. UI theo design system

- `Đơn hàng của tôi` chọn kỳ bằng một card quyết định; kỳ đang mở và kỳ còn trong cửa sổ bổ sung cùng nằm trong danh sách khi có capability hợp lệ.
- Selector ngang chỉ còn `Đơn thường | Đơn bổ sung`. Kỳ đã chọn là nguồn ngữ cảnh duy nhất; không lặp `Kỳ trước` thành một loại đơn thứ ba.
- Khi chọn `Đơn thường` trên kỳ đã đóng, action tạo đơn thường giữ vị trí nhưng bị khóa. Khi chọn `Đơn bổ sung`, card hạn chuyển sang hạn gửi bổ sung của chính kỳ đó; nếu kỳ còn mở, card ghi `Mở sau khi kỳ đóng` thay vì đếm ngược gây hiểu nhầm.
- Action `Tạo đơn bổ sung` giữ vị trí ổn định theo `CAPABILITY-SURFACE`: khả dụng thì bật; hết hạn/đã chốt/đang có đơn chờ thì mờ và có lý do ngắn.
- Trang `Duyệt đơn bổ sung` giữ list-detail, hiển thị kỳ, hạn còn lại và nguyên nhân không thể duyệt.
- Copy thân thiện: `Kỳ đã chốt nên không nhận thêm đơn bổ sung.` hoặc `Đã hết thời hạn gửi đơn bổ sung.`

### 1.4. Acceptance chính

- Trước thời điểm đóng: tạo bổ sung bị chặn.
- Đúng thời điểm đóng và trước hạn 5 ngày: tạo/duyệt được.
- Đúng hạn 5 ngày và sau hạn: tạo/duyệt bị chặn; từ chối vẫn được.
- Có `Pending`: chốt sớm bị chặn.
- Không có `Pending`: chốt sớm thành công và khóa tạo bổ sung ngay.
- Preview cũ bị vô hiệu nếu có đơn bổ sung phát sinh trước lúc confirm.

Validation 2026-08-18: build frontend/migration sạch `0 warning`; architecture/route test trọng tâm `6/6`; browser QA My Orders responsive pass trên fixture cô lập; TEST DB xác nhận 08/2026 còn hoạt động, 09–10/2026 được soft-delete và không có đơn/bản chốt bị tác động.

<a id="period-plan"></a>

## 2. Plan kỳ — tự mở một kỳ, thêm kỳ khi cần

### 2.1. Mô hình đơn giản hóa

- Scheduler không còn duy trì rolling horizon 3 kỳ.
- Mỗi tháng, hệ thống chỉ tự tạo/mở **một kỳ chuẩn** theo cấu hình mặc định.
- Quản lý cần đặt trước tháng sau thì bấm `Thêm kỳ`, chọn tháng và kiểm tra lịch mặc định trước khi lưu.
- Kỳ thêm thủ công là period instance bình thường: có lịch, trạng thái, đơn và bản chốt riêng; scheduler không tạo trùng hoặc xóa nó.
- Nếu quản lý chủ động đặt ngày mở sớm, hệ thống có thể có nhiều kỳ đang mở cùng lúc. Đây là ngoại lệ có chủ đích, không phải rolling tự động.

### 2.2. Cấu hình hệ thống

Trang Quản trị hệ thống chỉ giữ các mặc định cần thiết:

- ngày/giờ mở kỳ;
- ngày/giờ đóng kỳ;
- số ngày nhận và duyệt đơn bổ sung, mặc định `5`;
- số ngày điều chỉnh sau đóng, mặc định `10` và được backend enforce theo snapshot của từng kỳ;
- múi giờ và tháng bắt đầu áp dụng.

Ẩn `Số kỳ mở trước` khỏi UI. Trong bước chuyển tiếp, database có thể giữ `DefaultOpenPeriodCount` và service ép giá trị hiệu lực về `1`; chỉ xóa schema ở migration cleanup riêng sau consumer audit.

### 2.3. Nút `Thêm kỳ`

Dialog dùng motif form/dialog hiện hành, gồm:

1. `Kỳ đặt hàng`: tháng và năm.
2. `Ngày mở`: điền từ cấu hình mặc định, cho sửa.
3. `Ngày đóng`: điền từ cấu hình mặc định, cho sửa.
4. `Nhận đơn bổ sung (ngày)`: mặc định `5`, cho sửa từ `0–31`.
5. `Chỉnh đơn sau đóng (ngày)`: mặc định `10`, cho sửa từ `0–31` và không được ngắn hơn cửa sổ bổ sung.
6. `Lý do/Ghi chú`: lưu cùng kỳ để audit thao tác thêm thủ công.
7. Footer: `Hủy` → `Thêm kỳ`.

Validation:

- không trùng công ty + tháng + năm;
- ngày mở < ngày đóng; hạn bổ sung và hạn chỉnh đơn được tính từ ngày đóng;
- ngày 29–31 được quy đổi theo ngày cuối tháng thật, gồm năm nhuận;
- không tạo kỳ đã nằm hoàn toàn trong quá khứ;
- lưu bằng row version/idempotency để không tạo hai kỳ khi bấm lặp.

### 2.4. Trạng thái hiển thị

Kỳ đã tạo nhưng chưa đến ngày mở cần xuất hiện trong trang quản lý để còn sửa lịch. Khuyến nghị dùng nhãn **`Chưa mở`**, không dùng `Sắp mở`; backend có thể tiếp tục dùng state kỹ thuật `Scheduled`.

### 2.5. Dọn dữ liệu rolling cũ

- Migration chuyển tiếp soft-delete đúng kỳ tự sinh `09/2026` và `10/2026` khi `08/2026` vẫn là kỳ mở hiện tại.
- Guard bắt buộc: kỳ đích phải do `rolling-horizon-top-up` tạo, đang mở và chưa từng có đơn, bản chốt hoặc yêu cầu điều chỉnh.
- Không mở quyền xóa kỳ chung trong API/UI; kỳ thủ công và mọi kỳ đã phát sinh nghiệp vụ được giữ nguyên.
- Sau cleanup, scheduler thấy đã có một kỳ mở nên không tạo bù 09–10 ngay lập tức.

Thao tác chính trên mọi dòng vẫn ổn định:

- `Xem` luôn có;
- `Chốt kỳ` bật hoặc mờ theo capability;
- `...` luôn giữ cùng nhóm `Gia hạn kỳ`, `Sửa lịch`; action không hợp lệ bị mờ thay vì biến mất;
- không đưa `Xóa kỳ` trở lại giao diện quản lý thông thường. Sai sót được ngăn bằng preview/confirm; recovery đặc biệt sẽ là quyết định admin riêng nếu thật sự cần.

### 2.5. Ví dụ

- Tháng 08: hệ thống tự mở kỳ 08.
- Muốn nhân viên đặt trước kỳ 09: Quản lý bấm `Thêm kỳ`, chọn 09 và giữ/sửa lịch mặc định.
- Không thêm kỳ 10: hệ thống không tự mở kỳ 10 chỉ để đủ số lượng.
- Sang lịch tự mở của tháng 09: nếu kỳ 09 đã được thêm thủ công, scheduler dùng kỳ đó và không tạo bản trùng.

<a id="supplier-analysis"></a>

## 3. Phân tích một NCC hay nhiều NCC

| Tiêu chí | Một NCC | Nhiều NCC |
|---|---|---|
| Dễ dùng | Rất rõ: một NCC, một bảng giá, một lần giao | Quản lý phải hiểu phân bổ từng mặt hàng |
| Giá mặt hàng | Có thể cao hơn | Có thể rẻ hơn nếu mỗi NCC mạnh ở nhóm hàng khác nhau |
| Độ phủ hàng | Có thể thiếu vài mặt hàng | Ghép nhiều NCC có thể đủ hàng hơn |
| Vận chuyển | Một lần giao, dễ dự đoán | Nhiều lần giao; phí có thể xóa hết phần tiết kiệm |
| Hợp đồng/điều kiện tối thiểu | Dễ kiểm tra thủ công | Cần mô hình dữ liệu đáng tin cậy cho từng NCC |
| Xuất chứng từ/PO tương lai | Một bộ chứng từ | Phải tách theo NCC, phức tạp hơn |
| Audit/chốt lại | Đơn giản | Phải giải thích thay đổi phân bổ, giá và tổng theo NCC |
| Độ phù hợp scope hiện tại | **Cao** | Chưa đủ dữ liệu tổng chi phí để dùng thật an toàn |

### Khuyến nghị

Hiện tại nên giữ **một NCC cho bản chốt có hiệu lực**. Lý do: hệ thống mới biết chắc đơn giá và VAT; chưa biết đáng tin cậy phí vận chuyển, giá trị đơn tối thiểu, cam kết hợp đồng, khả năng giao và chi phí xử lý nhiều lần giao. Chọn “rẻ nhất theo từng mặt hàng” có thể nhìn rẻ trên màn hình nhưng tổng chi phí thực tế lại cao hơn.

Ba mức để owner cân nhắc:

1. **A — Một NCC duy nhất (khuyến nghị hiện tại):** tắt cả UI và optimizer backend; rõ, an toàn, dễ bảo vệ luận văn.
2. **B — Một NCC vận hành + mô phỏng hai NCC:** hệ thống chỉ hiển thị tham khảo giá + VAT, ghi rõ chưa gồm vận chuyển/hợp đồng; không được bấm áp dụng.
3. **C — Chốt thật tối đa hai NCC:** chỉ mở khi có đủ phí vận chuyển, minimum order, độ phủ, điều kiện hợp đồng, export/PO theo NCC và audit phân bổ.

Khi làm tối ưu thật, thuật toán lõi nên deterministic (công thức/ràng buộc kiểm thử được). AI chỉ hỗ trợ đọc file hoặc trích điều khoản hợp đồng thành dữ liệu có cấu trúc; AI không tự quyết phương án mua.

<a id="commercial-terms"></a>

## 4. Tạm tắt điều khoản thương mại ẩn

Không comment-out hàng loạt vì cách đó tạo code chết, dễ bật thiếu và không xử lý được dữ liệu cũ. Phương án reversible:

1. Thêm policy cấu hình, ví dụ `Pricing:CommercialTermsEnabled = false`.
2. Khi tắt, create/update/clone/import không nhận giá trị khác `0` cho chiết khấu, khoản giảm thêm, phụ phí và vận chuyển; mã hợp đồng để `null`.
3. Preview/chốt kỳ không dùng các giá trị cũ đang lưu. Trước implementation phải chạy audit read-only để biết có bao nhiêu dòng khác `0`; không tự sửa dữ liệu lịch sử.
4. API trả thông báo thân thiện nếu client cũ cố gửi dữ liệu thương mại đang bị tắt.
5. PDF/Excel hiện hành không hiển thị dòng phí bị tắt.
6. Giữ cột database để phát triển module hợp đồng/PO sau này; không migration phá hủy trong wave này.

Comment tiếng Việt chỉ đặt tại policy boundary, ví dụ: `// Tạm khóa điều khoản thương mại cho đến khi có module hợp đồng và vận chuyển đầy đủ.` Không comment lại thao tác hiển nhiên hoặc mô tả từng dòng code.

<a id="excel-plan"></a>

## 5. Excel bảng giá đã triển khai

### 5.1. File mẫu canonical

| Cột | Vai trò | Quy tắc |
|---|---|---|
| `Mã mặt hàng` | Khóa đối chiếu | Bắt buộc, dùng mã hệ thống, không tự tạo mặt hàng mới |
| `Tên mặt hàng` | Tham chiếu | File mẫu điền sẵn; lệch tên chỉ cảnh báo |
| `Đơn vị` | Tham chiếu | File mẫu điền sẵn; lệch đơn vị là lỗi chặn |
| `Đơn giá` | Dữ liệu cập nhật | Trống = giữ nguyên; số không âm = thêm/cập nhật |
| `VAT (%)` | Dữ liệu cập nhật | Trống = dùng mặc định hiện hành; ngoài khoảng hợp lệ = lỗi |
| `Ghi chú` | Tùy chọn | Không làm thay đổi danh mục |

Bỏ hoàn toàn khỏi template, parser, preview và apply contract:

- đặt tối thiểu;
- ngày giao;
- giá mặc định cấp dòng;
- mã hợp đồng;
- chiết khấu, khoản giảm thêm, phụ phí và vận chuyển.

### 5.2. Xử lý file không đúng mẫu

- Cột bắt buộc bị thiếu: chặn và nói đúng cột cần bổ sung.
- Cột thừa: bỏ qua nhưng hiển thị cảnh báo; không làm cả file thất bại.
- Mã không tồn tại: lỗi theo dòng và cho tải file lỗi; người dùng phải tạo mặt hàng trong danh mục rồi nhập lại.
- Mặt hàng hệ thống không có trong file: giữ nguyên, không xóa giá.
- Mã trùng trong file: lỗi chặn.
- File của NCC luôn gắn với **một NCC và một bảng giá đã chọn**; không dùng một file để phân bổ nhiều NCC.
- Preview bắt buộc có nhóm `Thêm`, `Cập nhật`, `Không đổi`, `Cảnh báo`, `Lỗi`; chỉ được áp dụng khi hết lỗi chặn.
- Confirm là transaction atomic và idempotent; lỗi giữa chừng rollback toàn bộ.

### 5.3. UI

- `Tải file mẫu` và `Cập nhật giá từ file` nằm ở collection header của trang Giá mặt hàng.
- Upload → kiểm tra → preview → xác nhận dùng dialog lớn theo design system.
- Thao tác thủ công vẫn là cách mặc định; cột `Cách nhập` chỉ dùng `Thủ công` hoặc `Excel`.
- Empty/loading/error giữ nguyên frame toolbar-header-content-footer và căn giữa theo data-surface contract.

### 5.4. Quyết định triển khai

1. Nhận `.xlsx` và `.csv`; file mẫu canonical tải xuống là `.xlsx`.
2. VAT trống giữ VAT hiện hành; dòng mới chưa có VAT dùng `0`.
3. Cột thừa chỉ cảnh báo và bỏ qua, không làm cả file thất bại.
4. Giữ import session, preview và issue theo dòng để có thể kiểm tra lại lần nhập.

<a id="post-settlement"></a>

## 6. Điều chỉnh sau chốt đã xác nhận

- Quản lý kỳ được tạo yêu cầu **sửa đơn** hoặc **hủy đơn** đã chốt; `hủy` là tạo revision trạng thái hủy, không xóa vật lý.
- Một quản lý khác xác nhận để bảo đảm four-eyes như hệ thống hiện tại.
- Khi xác nhận, chỉ tạo bản đơn mới và đánh dấu `Có thay đổi chờ chốt lại`; chưa tạo bản chốt mới.
- Quản lý mở preview, kiểm tra lại đơn, NCC, bảng giá và tổng tiền rồi bấm `Chốt lại kỳ`.
- Hệ thống tạo `Bản chốt N+1`; bản chốt cũ bất biến và vẫn xuất/xem được.
- Phạm vi hiện tại chỉ sửa số lượng, bỏ dòng hoặc hủy toàn bộ đơn. Chưa cho thêm một mặt hàng hoàn toàn mới sau chốt.

<a id="deferred-scope"></a>

## 7. Phạm vi tạm hoãn

- Chưa tách `VPPRequestService` và `PeriodSettlementService`.
- Chưa bật nhiều NCC trong vận hành thật.
- Chưa làm PO, hợp đồng, tự tính vận chuyển hoặc AI đọc hợp đồng.
- Chưa xóa schema/endpoint legacy; mọi cleanup có thay đổi behavior phải có owner approval riêng.

## 8. Execution waves sau approval

Quota probe ngày `2026-08-18` đã timeout sau hai lần thử giới hạn, nên capacity hiện là **không xác định**, không phải đã hết. Không nêu phần trăm chi phí giả khi chưa có snapshot/measurement phù hợp; phải probe lại trước khi mở wave implementation.

| Wave | Nội dung | Model + effort khuyến nghị | Gate | Trạng thái |
|---|---|---|---|---|
| P0 | Ghi nhận quyết định, cập nhật authority/plan | `gpt-5.6-terra · high` | Tài liệu không mâu thuẫn; `git diff --check` | COMPLETED |
| W1 | Policy đơn bổ sung phương án B + command/query/tests | `gpt-5.6-sol · high` | Boundary tests, race với chốt sớm, integration DB | COMPLETED |
| W2 | Auto một kỳ + `Thêm kỳ` thủ công + settings/UI/tests | `gpt-5.6-sol · high` cho domain; `gpt-5.6-terra · high` cho UI | Scheduler/duplicate/timezone + route-real | COMPLETED |
| W3 | Tinh gọn Excel template/parser/preview/apply | `gpt-5.6-sol · high` backend; `gpt-5.6-terra · high` UI | XLSX fixtures, atomic import, route-real dialog | COMPLETED |
| W4 | Feature policy tắt commercial terms + deadline 10 ngày | `gpt-5.6-sol · high` | Calculation/export/correction regression | COMPLETED |
| W5 | Một/nhiều NCC | Chọn sau decision gate | Có dữ liệu tổng chi phí và owner chọn A/B/C | PAUSED |
| W6 | Refactor service lớn | `gpt-5.6-sol · xhigh` | Characterization trước refactor, full backend/FE QA | DEFERRED BY OWNER |

Theo official OpenAI documentation hiện tại, `gpt-5.6-sol` phù hợp phần reasoning/coding phức tạp; `gpt-5.6-terra` cân bằng chất lượng và chi phí cho phần triển khai rõ, lặp lại. Đây là routing khuyến nghị, không phải xác nhận model đang active.

## 9. Definition of done cho từng wave

- Không thay đổi behavior ngoài wave đã duyệt.
- Backend là authority; UI disabled không thay cho permission/policy.
- Chuỗi VI/EN, loading/empty/error/disabled/success và `CAPABILITY-SURFACE` đầy đủ.
- Test hẹp trong vòng lặp; migration/DB check khi chạm schema; route thật tại `390×844`, `768×1024`, `1366×768`, `1920×1080` khi chạm UI.
- Review toàn bộ diff, `git diff --check`, commit local đúng scope; không gộp refactor lớn vào W1–W4.
