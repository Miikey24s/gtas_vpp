# BUSINESS-FLOW-AUDIT-20260818 — Rà soát luồng nghiệp vụ GTAS VPP

> Ngày rà soát: `2026-08-18`
>
> Phạm vi: code hiện tại của `src/Backend`, `src/Frontend/Blazor`, DTO dùng chung và các test tập trung liên quan.
>
> Tính chất: audit gốc kèm cập nhật implementation. Các mục ghi `ĐÃ SỬA` phản ánh change-set đang validation.
>
> Owner revision ngày `2026-08-18`: các quyết định mục 9 và plan [ORDERING-PRICING-REVISION-20260818](ORDERING-PRICING-REVISION-20260818.md) là target mới; phần còn lại của tài liệu tiếp tục mô tả code tại thời điểm audit.

## 0. Bản một ánh nhìn

### Kết luận

Luồng chính của hệ thống đã khép kín được chuỗi:

`Quản trị cấu hình → mở kỳ → nhân viên đặt đơn → quản lý duyệt bổ sung → chọn NCC/bảng giá → chốt kỳ → xuất báo cáo → điều chỉnh có phê duyệt → chốt lại`.

Phần lớn ràng buộc quan trọng đã nằm ở backend/database, không chỉ ở nút bấm. Sai lệch về đơn bổ sung, rolling horizon, deadline 10 ngày, Excel và điều khoản thương mại đã được sửa trong change-set hiện tại; quyết định nhiều NCC và refactor service lớn vẫn hoãn.

### Việc nên ưu tiên

| Ưu tiên | Việc cần xử lý | Tác động nếu chưa xử lý |
|---|---|---|
| **DONE** | Đơn bổ sung phương án B: chỉ tạo trong cửa sổ sau khi kỳ đóng | Mặc định 5 ngày; chốt sớm khóa ngay |
| **DONE** | Tự mở một kỳ; thêm kỳ tương lai bằng action thủ công | Hai cửa sổ 5/10 ngày cấu hình toàn cục và ghi đè theo kỳ |
| **DONE** | Tạm tắt điều khoản thương mại ẩn bằng policy; tinh gọn file Excel | Dữ liệu ẩn không còn tác động bản chốt mới |
| **P1** | Chốt phương án một NCC, mô phỏng nhiều NCC hoặc chốt thật nhiều NCC | Backend hiện vẫn tính đề xuất dù UI đã tắt |
| **HOLD** | Tách service lớn | Chờ owner kiểm tra chức năng mới trước khi refactor |

### Thống kê kết quả

- **Đạt:** 11 nhóm kiểm soát chính.
- **Rủi ro cao đã xử lý:** cửa sổ điều chỉnh 10 ngày được snapshot theo từng kỳ và enforce tại command sửa đơn.
- **Cần quyết định nghiệp vụ:** phương án nhiều NCC và refactor service lớn.
- **Nợ kỹ thuật:** 4 nhóm đáng theo dõi.
- **Kiểm tra tập trung:** `73/73` test backend đạt trong lượt rà soát.

## 1. Luồng nghiệp vụ hiện tại

### 1.1. Quản trị nền tảng

1. Quản trị hệ thống quản lý tài khoản, quyền, phòng ban, danh mục mặt hàng, NCC và bảng giá.
2. Cấu hình kỳ quy định số kỳ mở trước, ngày mở, ngày đóng, hạn duyệt bổ sung và số ngày được điều chỉnh sau ngày đóng.
3. Cấu hình mới chỉ dùng khi tạo kỳ mới; kỳ đã tồn tại giữ nguyên lịch và dữ liệu của mình.

### 1.2. Đặt hàng và ngoại lệ

1. Hệ thống tự tạo/mở các kỳ theo cấu hình.
2. Nhân viên chọn một kỳ đang mở và tạo đơn thường.
3. Mỗi nhân viên chỉ có một đơn thường hiện hành trong một kỳ.
4. Đơn bổ sung dùng cùng cấu trúc đơn; khác loại đơn, lý do, trạng thái và luồng duyệt.
5. Quản lý duyệt hoặc từ chối đơn bổ sung trong thời hạn duyệt bổ sung.

### 1.3. Chốt kỳ

1. Quản lý xem nhu cầu tổng hợp theo phòng ban, người đặt hoặc mặt hàng.
2. Hệ thống chỉ lấy đơn thường hợp lệ và đơn bổ sung đã duyệt.
3. Quản lý chọn một NCC chính và bảng giá đã công bố/còn hiệu lực.
4. Hệ thống xem trước số lượng, giá trước VAT, VAT, tổng tiền và các lỗi chặn.
5. Khi xác nhận, hệ thống tạo một **bản chốt** bất biến, gắn người chốt, thời điểm, NCC, bảng giá và dữ liệu giá tại thời điểm chốt.
6. PDF/Excel có thể xuất theo đúng bản chốt được chọn.

### 1.4. Điều chỉnh sau chốt

1. Quản lý tạo yêu cầu sửa hoặc hủy một đơn trong kỳ đã chốt.
2. Một quản lý khác phải duyệt yêu cầu.
3. Khi duyệt, hệ thống tạo bản đơn mới và đánh dấu kỳ đang có thay đổi chờ chốt lại; chưa tự tạo bản chốt mới.
4. Một quản lý khác với người chốt trước mở preview, kiểm tra lại NCC/bảng giá/tổng tiền và chốt lại.
5. Hệ thống tạo **Bản chốt N+1**; bản cũ vẫn giữ trong lịch sử.

Luồng N+1 này phù hợp quyết định sản phẩm hiện tại: không “mở lại kỳ”, không ghi đè bản chốt cũ.

## 2. Ma trận đánh giá chi tiết

### 2.1. Cấu hình và lịch kỳ — **ĐÃ SỬA THEO TARGET MỚI**

**Đã đúng**

- Hệ thống tự duy trì đúng `1` kỳ chuẩn; `Số kỳ mở trước` đã rời UI.
- Ngày mở/đóng nhận giá trị `1–31`.
- Ngày 29–31 tự lùi về ngày cuối tháng bằng `DateTime.DaysInMonth`, nên xử lý đúng tháng 30/31 ngày và tháng 2 năm nhuận.
- Mặc định hạn duyệt bổ sung là `5` ngày, hạn điều chỉnh là `10` ngày.
- Timestamp được lưu theo timezone; kỳ cũ không bị tính lại khi cấu hình thay đổi.
- Quản lý có thể `Thêm kỳ` thủ công và ghi đè riêng hai giá trị `5/10` trước khi lưu.

**Bằng chứng**

- `src/Backend/Domain/VPP/VppOrderPeriodSettingsVersion.cs:22-42`
- `src/Backend/Application/Domain/PeriodScheduleCalculator.cs:13-16`
- `src/Backend/Application/Domain/PeriodScheduleCalculator.cs:88-155`

**Quyết định owner ngày 2026-08-18**

- Không tiếp tục target rolling 3 kỳ. Hệ thống chỉ tự tạo/mở một kỳ chuẩn.
- Quản lý dùng `Thêm kỳ` để tạo kỳ đặt trước từ lịch mặc định và được điều chỉnh trước khi lưu.
- Kỳ thủ công tương lai vẫn cần state nội bộ `Scheduled` để scheduler mở đúng thời điểm; UI khuyến nghị nhãn `Chưa mở`, không dùng `Sắp mở`.
- `Số kỳ mở trước` sẽ rời UI; schema được giữ tạm và ép hiệu lực về `1` cho đến migration cleanup riêng.

**Bằng chứng**

- `src/Backend/Domain/VPP/VppPeriodState.cs:14`
- `src/Backend/Application/Services/Settlement/VppPeriodService.cs:1014-1023`
- `src/Frontend/Blazor/Resources/Components.App.resx:348`

### 2.2. Tạo đơn thường — **ĐẠT**

- Chỉ tạo được khi kỳ `Open` và chưa đến hạn đóng.
- Chặn tạo khi kỳ đang chốt/đã chốt hoặc đã có bản chốt.
- Chống gửi lặp bằng idempotency key.
- Mỗi người chỉ có một đơn thường hiện hành trong một kỳ.
- Trạng thái ban đầu là `Submitted`.
- Giới hạn số lượng áp dụng theo từng mặt hàng và từng đơn, dùng chung cho đơn thường/bổ sung.

**Bằng chứng**

- `src/Backend/Application/Services/Requests/VPPRequestService.cs:563-610`
- `src/Backend/Application/Services/Requests/VPPRequestService.cs:667-705`
- `src/Backend/Application/Services/Requests/OrderQuantityLimitService.cs:29-77`
- `src/Shared/Constants/VppOrderQuantityLimits.cs:6-12`

### 2.3. Đơn bổ sung — **ĐÃ SỬA THEO PHƯƠNG ÁN B**

**Code hiện tại sau revision**

- Đơn thường chỉ được tạo khi kỳ `Open`.
- Đơn bổ sung chỉ được tạo khi kỳ `SubmissionClosed` và còn trong cửa sổ sau ngày đóng.
- Đơn bổ sung mới có trạng thái `Pending`; chỉ đơn `Approved` mới đi vào bản chốt.
- Có giới hạn số lần bổ sung được duyệt và không cho tồn tại đồng thời nhiều đơn bổ sung đang chờ.
- Khi kỳ chuyển `Pricing/Settled`, tạo bổ sung bị khóa ngay dù chưa hết số ngày cấu hình.

**Quyết định owner ngày 2026-08-18**

- Chọn phương án B: đơn bổ sung dành cho kỳ đã đóng và được tạo trong 5 ngày sau ngày đóng.
- Khi quản lý chốt kỳ sớm, kỳ ngừng nhận và duyệt bổ sung ngay dù mốc 5 ngày chưa hết.
- Nếu còn đơn bổ sung `Pending`, chốt sớm phải bị chặn; backend kiểm tra lại tại transaction confirm để chống race.

**Bằng chứng**

- `src/Backend/Application/Services/Requests/VPPRequestService.cs:577-591`
- `src/Backend/Application/Services/Requests/VPPRequestService.cs:617-665`
- `src/Backend/Application/Services/Requests/VPPRequestService.cs:1872-1889`

Eligibility đã được tách theo loại đơn trong command và period-info dùng cho UI. Chi tiết tại `ORDERING-PRICING-REVISION-20260818.md#supplement-flow`.

### 2.4. Giới hạn số lượng — **ĐẠT**

- Giới hạn nằm tại mặt hàng hệ thống, không nằm riêng trong bảng giá NCC.
- Áp dụng cho mỗi dòng mặt hàng trong mỗi đơn, không cộng dồn theo kỳ.
- Khoảng dữ liệu được ràng buộc `1–1000`; dữ liệu demo tự sinh từ nhu cầu thật theo hệ số `3`, tối thiểu `500`.
- Frontend chặn sớm; backend và database vẫn là lớp bảo vệ cuối.

**Bằng chứng**

- `src/Backend/Domain/VPPMigrationDbContext.cs:103-115`
- `src/Backend/Application/Services/Requests/OrderQuantityLimitService.cs:29-77`
- `src/Backend/Application/Services/Seeding/DemoWorkbookSeeder.cs:226-250`

### 2.5. Duyệt đơn bổ sung — **ĐẠT**

- Chỉ đơn `Pending` mới được quyết định.
- Không duyệt sau hạn duyệt bổ sung hoặc khi kỳ đang chốt/đã chốt.
- Dùng transaction, row version và idempotency để tránh duyệt trùng.
- Đơn quá hạn vẫn có thể bị từ chối để dọn hàng chờ, nhưng không được duyệt vào chốt kỳ.

Điểm bất đối xứng “quá hạn chỉ từ chối, không duyệt” là hợp lý, nhưng nên được mô tả rõ trong hướng dẫn nghiệp vụ.

### 2.6. Danh mục, NCC và bảng giá — **ĐẠT PHẦN LÕI, ĐÃ KHÓA DỮ LIỆU ẨN**

**Đã đúng**

- Bảng giá thuộc một NCC.
- Tạo bảng giá xong có hiệu lực ngay (`Published`), không dùng bước nháp/công bố riêng trên UI.
- Mã mặt hàng dùng mã chung của hệ thống; trường mã riêng của NCC đã được loại khỏi mô hình hiện tại.
- Bảng giá có thể tạo thủ công, sao chép hoặc cập nhật bằng Excel.

**Policy hiện tại**

Database vẫn giữ các cột sau để phát triển về sau:

- mã hợp đồng;
- chiết khấu;
- khoản giảm thêm;
- phụ phí;
- phí vận chuyển.

`Pricing:CommercialTermsEnabled` mặc định `false`: create/update cũ gửi giá trị khác `0` bị từ chối; dữ liệu mới được chuẩn hóa về `0`; compare/preview/chốt mới bỏ qua dữ liệu cũ. Schema và lịch sử cũ không bị sửa phá hủy.

**Bằng chứng**

- `src/Backend/Application/Services/CatalogPricing/PriceListService.cs:147-179`
- `src/Backend/Application/Services/CatalogPricing/PriceListService.cs:198-243`
- `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:503-529`
- `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:620-641`

Policy tập trung là boundary duy nhất; không comment-out rải rác và không xóa schema trong revision này.

### 2.7. Nhập bảng giá Excel — **ĐÃ TINH GỌN**

**Đã đúng**

- File mẫu có sẵn mã, tên, đơn vị và giá hiện tại của danh mục hệ thống.
- Mã không tồn tại bị chặn.
- Mã trùng trong file bị chặn.
- Tên lệch chỉ cảnh báo vì mã là khóa đối chiếu.
- Đơn vị lệch bị chặn.
- Ô giá trống giữ nguyên dữ liệu cũ.
- Có bước đánh giá trước khi áp dụng thay đổi.

Template/parser/preview/apply chỉ còn `Mã mặt hàng`, `Tên mặt hàng`, `Đơn vị`, `Đơn giá`, `VAT (%)`, `Ghi chú`. Ba trường legacy `MinimumOrderQuantity`, `LeadTimeDays`, `IsDefault` không còn là target ánh xạ; nếu file cũ vẫn có thì được cảnh báo là cột thừa và bỏ qua.

**Bằng chứng**

- `src/Backend/Application/Services/CatalogPricing/PriceListImportService.cs:194-246`
- `src/Backend/Application/Services/CatalogPricing/PriceListImportService.cs:285-360`
- `src/Backend/Application/Services/CatalogPricing/PriceListImportService.cs:483-530`

### 2.8. Chốt kỳ lần đầu — **ĐẠT**

- Chỉ lấy đơn thường `Submitted/Approved` và đơn bổ sung `Approved`.
- Đơn bổ sung `Pending` là blocker.
- Chọn đúng NCC và bảng giá đã công bố/còn hiệu lực.
- Preview tạo input hash; xác nhận lại nếu dữ liệu đổi giữa lúc xem và bấm chốt.
- Lưu snapshot giá, số lượng, VAT, tổng tiền, người chốt và thời điểm.
- Tạo bản chốt số `1` và đánh dấu kỳ `Settled`.

**Bằng chứng**

- `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:64-132`
- `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:362-500`
- `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:503-536`

### 2.9. Điều chỉnh sau chốt — **DEADLINE ĐÃ ENFORCE**

**Đã đúng**

- Chỉ dùng cho kỳ đã chốt.
- Không ghi đè đơn cũ; khi duyệt tạo revision đơn mới.
- Người tạo yêu cầu không được tự duyệt.
- Yêu cầu đã duyệt chuyển kỳ sang trạng thái “có thay đổi chờ chốt lại”.
- Chốt lại tạo Bản chốt `N+1`, thay bản hiện hành nhưng không xóa lịch sử.
- Người chốt lại phải khác người xác nhận bản chốt trước.

Mỗi kỳ lưu `PostCloseAdjustmentDeadlineUtc`; mặc định bằng ngày đóng + `10` ngày, có thể đổi ở settings hoặc dialog `Thêm kỳ`. Command điều chỉnh đơn kiểm tra snapshot này, có fallback tương thích cho kỳ cũ chưa có cột snapshot.

**Bằng chứng**

- Có cấu hình: `src/Backend/Domain/VPP/VppOrderPeriodSettingsVersion.cs:33-38`
- DTO tính deadline: `src/Backend/Application/Services/Settlement/VppPeriodService.cs:854-912`
- Tạo/duyệt correction chỉ kiểm tra `Settled`: `src/Backend/Application/Services/Settlement/PostSettlementOrderCorrectionService.cs:47-99`, `156-188`
- Chốt lại chỉ kiểm tra `Settled`: `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:341-459`

Sau hạn, quản lý vẫn được chọn NCC/bảng giá và chốt kỳ; dữ liệu đơn không còn được sửa. Điều chỉnh/chốt lại kỳ đã chốt tiếp tục dùng luồng four-eyes và bản chốt `N+1` hiện hành.

### 2.10. Phạm vi sửa đơn sau chốt — **ĐÃ XÁC NHẬN**

Hiện tại có thể đổi số lượng, bỏ dòng hoặc hủy đơn; không thể thêm một mặt hàng chưa có trong bản chốt hiện hành.

**Bằng chứng**

- `src/Backend/Application/Services/Settlement/PostSettlementOrderCorrectionService.cs:350-368`

Owner giữ ràng buộc hiện tại: chỉ sửa số lượng, bỏ dòng hoặc hủy đơn, sau đó chốt lại để tạo `Bản chốt N+1`; chưa cho thêm một mặt hàng hoàn toàn mới sau chốt.

### 2.11. Chọn nhiều NCC — **TẠM TẮT, OWNER ĐANG CÂN NHẮC**

- Frontend feature flag đang `false`, nên người dùng chỉ chốt với một NCC chính.
- Backend vẫn luôn tính đề xuất tối đa hai NCC khi dựng preview.
- Thuật toán chỉ so giá sau VAT; không tính hợp đồng hoặc vận chuyển; chỉ đề xuất nếu tiết kiệm ít nhất `2%` hoặc cần ghép để đủ mặt hàng.

**Bằng chứng**

- `src/Frontend/Blazor/appsettings.json:16`
- `src/Frontend/Blazor/Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor:90-125`
- `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:91-105`
- `src/Backend/Application/Services/Settlement/SettlementSupplierOptimizer.cs:7-14`, `31-116`

**Khuyến nghị hiện tại**

Giữ một NCC cho bản chốt có hiệu lực. Nếu cần đánh giá nhiều NCC, bước kế tiếp chỉ nên là mô phỏng read-only; chưa bật chốt thật cho đến khi có dữ liệu vận chuyển, minimum order, hợp đồng và export theo NCC. Phân tích A/B/C tại `ORDERING-PRICING-REVISION-20260818.md#supplier-analysis`.

### 2.12. Xuất PDF/Excel và lịch sử — **ĐẠT**

- Chỉ xuất sau khi có bản chốt.
- Có thể xuất đúng một revision được chỉ định, không bắt buộc chỉ bản mới nhất.
- Tên file, MIME và builder được chuẩn hóa theo export contract dùng chung.
- Lịch sử bản chốt giữ được quan hệ bản cũ → bản mới.

UI nên luôn ghi rõ đang xem/xuất `Bản chốt N` để tránh người dùng xuất nhầm lịch sử.

### 2.13. Tài khoản và quyền — **ĐẠT PHẦN LÕI**

- Vô hiệu hóa membership là soft delete và hủy phiên đăng nhập.
- Tài khoản còn hoạt động có thể được kích hoạt/cấp membership lại.
- Quyền cấu hình kỳ thuộc quản trị hệ thống; quản lý vận hành không tự sửa cấu hình toàn hệ thống.
- Controller không truy cập `DbContext` trực tiếp; đi qua application/service.

**Bằng chứng**

- `src/Backend/Api/Authorization/MembershipAdministrationService.cs:327-449`
- `src/Backend/Api/Authorization/AccountLifecycleService.cs:819-880`
- Rà `src/Backend/Api/Controllers`: không có controller dùng trực tiếp `DbContext`.

## 3. Capability cũ còn tồn tại dù UI đã bỏ

### 3.1. Thao tác kỳ

Backend/DTO vẫn còn các khả năng:

- đóng nhận đơn thủ công;
- mở lại nhận đơn;
- xóa kỳ nháp/sắp mở;
- `SettlementReopenWindowDays` trong schema.

UI hiện đã bỏ hoặc làm mờ phần lớn các thao tác này theo quyết định sản phẩm mới.

**Bằng chứng**

- `src/Backend/Api/Controllers/OrderPeriodsController.cs:140-187`
- `src/Backend/Application/Services/Settlement/VppPeriodService.cs:903-912`
- `src/Backend/Domain/VPP/VppOrderPeriodSettingsVersion.cs:37-38`

**Đề xuất**

Không nên để capability “vô chủ”. Chọn rõ:

- giữ làm công cụ khôi phục chỉ dành cho quản trị hệ thống, có audit và tài liệu; hoặc
- loại API/DTO/test nếu chắc chắn không dùng.

### 3.2. Thuật ngữ kỹ thuật còn lộ ra người dùng

Một số exception vẫn dùng tiếng Anh hoặc từ nội bộ như `revision`, `settlement`, `price book`, `idempotency`, `reload`.

Ví dụ:

- `Không tìm thấy revision đơn hiện tại.`
- `Settlement revision not found.`
- `The selected price book no longer matches the preview.`

Nên chuẩn hóa thành:

- `Không tìm thấy bản đơn hiện tại.`
- `Không tìm thấy bản chốt.`
- `Bảng giá đã thay đổi. Vui lòng xem lại trước khi chốt.`

## 4. Nợ kỹ thuật và khả năng bảo trì

### 4.1. Service quá lớn

| File | Số dòng hiện tại | Nhận xét |
|---|---:|---|
| `VPPRequestService.cs` | 2.739 | Gộp query, tạo/sửa/hủy, bổ sung, duyệt, tổng hợp và policy |
| `PeriodSettlementService.cs` | 1.582 | Gộp preview, quote, confirm, correction, export và mapping |
| `PostSettlementOrderCorrectionService.cs` | 464 | Quy mô chấp nhận được nhưng còn trùng policy/deadline cần tách dùng chung |
| `PermissionController.cs` | 299 | Chưa phải điểm nóng; controller không giữ DbContext trực tiếp |

Hai service đầu là rủi ro bảo trì thật: một thay đổi nhỏ dễ ảnh hưởng nhiều luồng. Refactor nên làm sau khi owner kiểm tra phần chức năng mới, theo vertical slice và characterization test; không đổi nghiệp vụ trong lúc tách module.

### 4.2. Đề xuất ranh giới tách

`VPPRequestService`:

- `OrderCreationService`
- `OrderRevisionService`
- `SupplementApprovalService`
- `OrderQueryService`
- `OrderPeriodEligibilityPolicy`

`PeriodSettlementService`:

- `SettlementPreviewService`
- `SettlementConfirmationService`
- `SettlementPricingResolver`
- `SettlementRevisionService`
- `SettlementExportService`

## 5. Những gì chưa phải chức năng hiện có

Các mục sau nên tiếp tục ghi là **hướng phát triển**, không mô tả như hệ thống đã hoàn thành:

- phát hành PO;
- quản lý hợp đồng NCC;
- tự lấy chi phí vận chuyển;
- đọc/hiểu hợp đồng bằng AI;
- bật tối ưu chia nhiều NCC trong vận hành thật;
- khóa bản chốt khi đã phát sinh cam kết mua sắm bên ngoài.

Hiện database/service đã có một số cột hoặc guard chuẩn bị trước, nhưng chưa có module nghiệp vụ khép kín.

## 6. Trạng thái xử lý

### Wave A — tính đúng nghiệp vụ — **ĐÃ TRIỂN KHAI, ĐANG XÁC MINH**

1. Enforce thời hạn điều chỉnh 10 ngày ở toàn bộ command backend.
2. Chốt cách hiểu thời điểm tạo đơn bổ sung và sửa UI/tài liệu/test cho thống nhất.
3. Ép các khoản thương mại đang ẩn về 0; bỏ trường import ẩn khỏi contract hiện hành.

### Wave B — đồng bộ capability — **MỘT PHẦN ĐÃ TRIỂN KHAI**

1. Tắt optimizer nhiều NCC ở backend khi feature flag tắt.
2. Quyết định `Scheduled / Sắp mở`.
3. Quyết định giữ admin-recovery hay loại các endpoint đóng/mở lại/xóa kỳ.
4. Việt hóa thông báo kỹ thuật.

### Wave C — refactor giữ nguyên hành vi — **TẠM HOÃN**

1. Khóa characterization test cho luồng tạo đơn, bổ sung, chốt và chốt lại.
2. Tách `VPPRequestService` theo command/query/policy.
3. Tách `PeriodSettlementService` theo preview/confirm/pricing/export/revision.
4. Chạy lại backend, frontend và route-real browser QA.

## 7. Kiểm tra đã thực hiện

| Kiểm tra | Kết quả |
|---|---|
| Backend unit | `581 passed, 0 failed` |
| Frontend unit | `503 passed, 0 failed` |
| Backend integration | `14 passed, 11 skipped, 0 failed`; các ca skip cần LocalDB fixture được bật |
| UI route thật: cấu hình kỳ + thêm kỳ | `5 passed, 0 failed`; gồm 4 viewport và accessibility |
| UI route thật: cập nhật giá từ file | `1 passed, 0 failed`; preview ở 4 viewport và xác nhận trên DB QA cô lập |
| Release build / EF model | `0 warning, 0 error`; không có model change ngoài migration |
| Rà controller truy cập DbContext trực tiếp | `0` kết quả |
| Đối chiếu frontend flag nhiều NCC với backend optimizer | Có sai lệch — UI tắt, backend vẫn tính |
| Đối chiếu cấu hình 10 ngày với command correction | Đã enforce khi tạo và xác nhận yêu cầu sửa/hủy sau chốt; từ chối yêu cầu cũ vẫn được phép |

## 8. Quyết định còn mở sau owner review

1. Chọn phương án NCC: A một NCC, B một NCC + mô phỏng, hay C chốt thật tối đa hai NCC.
2. Các endpoint quản trị legacy có cần giữ làm công cụ khôi phục hay loại bỏ ở đợt cleanup sau.
3. Phạm vi refactor service lớn sau khi owner kiểm tra xong các chức năng mới.

## 9. Owner decision addendum — 2026-08-18

| Nội dung | Quyết định hiện tại |
|---|---|
| Deadline điều chỉnh 10 ngày | Mặc định 10 ngày; cấu hình toàn cục và ghi đè theo kỳ; đã enforce ở command sửa/hủy trước và sau chốt |
| Đơn bổ sung | Chọn phương án B: tạo/duyệt trong 5 ngày sau đóng; chốt sớm khóa bổ sung ngay |
| Một/nhiều NCC | Chưa chốt; cần phân tích trước khi bật |
| Điều khoản thương mại ẩn | Tắt tập trung bằng policy; contract/hàm legacy vẫn giữ để tương thích dữ liệu cũ |
| Excel bảng giá | Template hiện hành chỉ nhận mã, tên, đơn vị, đơn giá, VAT và ghi chú; cột legacy được bỏ qua có cảnh báo |
| Mở kỳ | Tự mở một kỳ; Quản lý thêm kỳ riêng khi cần |
| Sau chốt | Cho sửa/hủy đơn qua duyệt rồi chốt lại `N+1`; chưa thêm mặt hàng mới |
| Refactor service lớn | Tạm hoãn |

Chi tiết implementation, UI, acceptance và approval gate nằm tại [ORDERING-PRICING-REVISION-20260818](ORDERING-PRICING-REVISION-20260818.md).
