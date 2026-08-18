# BUSINESS-FLOW-AUDIT-20260818 — Rà soát luồng nghiệp vụ GTAS VPP

> Ngày rà soát: `2026-08-18`  
> Phạm vi: code hiện tại của `src/Backend`, `src/Frontend/Blazor`, DTO dùng chung và các test tập trung liên quan.  
> Tính chất: **audit đọc và đối chiếu**, chưa sửa nghiệp vụ trong tài liệu này.

## 0. Bản một ánh nhìn

### Kết luận

Luồng chính của hệ thống đã khép kín được chuỗi:

`Quản trị cấu hình → mở kỳ → nhân viên đặt đơn → quản lý duyệt bổ sung → chọn NCC/bảng giá → chốt kỳ → xuất báo cáo → điều chỉnh có phê duyệt → chốt lại`.

Phần lớn ràng buộc quan trọng đã nằm ở backend/database, không chỉ ở nút bấm. Tuy nhiên còn **1 sai lệch nghiệp vụ mức cao**, **5 nhóm cần chốt lại phạm vi sản phẩm** và một số nợ kỹ thuật có thể làm giao diện và backend hiểu khác nhau.

### Việc nên ưu tiên

| Ưu tiên | Việc cần xử lý | Tác động nếu chưa xử lý |
|---|---|---|
| **P0** | Chặn sửa/hủy sau chốt và chốt lại khi quá `PostCloseAdjustmentDays` | Cấu hình 10 ngày hiện chưa thực sự bảo vệ nghiệp vụ |
| **P1** | Chốt cách hiểu đơn bổ sung: tạo trước ngày đóng hay được tạo sau ngày đóng | Tránh giao diện/tài liệu mô tả khác code |
| **P1** | Đồng bộ feature flag nhiều NCC ở cả frontend và backend | Backend hiện vẫn tính đề xuất dù UI đã tắt |
| **P1** | Khóa các khoản thương mại và trường import đang bị ẩn khỏi UI | Giá trị ẩn vẫn có thể ảnh hưởng tổng tiền |
| **P1** | Quyết định giữ hay bỏ `Sắp mở` và các API thao tác kỳ đã bỏ khỏi UI | Tránh capability ẩn tồn tại lâu dài |
| **P2** | Việt hóa thông báo kỹ thuật và tách nhỏ hai service lớn | Dễ demo, dễ bảo trì và giảm rủi ro sửa dây chuyền |

### Thống kê kết quả

- **Đạt:** 11 nhóm kiểm soát chính.
- **Rủi ro cao:** 1 nhóm — thời hạn điều chỉnh sau ngày đóng chưa được enforce đầy đủ.
- **Cần quyết định nghiệp vụ:** 5 nhóm.
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

### 2.1. Cấu hình và lịch kỳ — **ĐẠT, còn 1 điểm cần quyết định**

**Đã đúng**

- Mặc định mở trước `3` kỳ; chấp nhận cấu hình `0–12` kỳ.
- Ngày mở/đóng nhận giá trị `1–31`.
- Ngày 29–31 tự lùi về ngày cuối tháng bằng `DateTime.DaysInMonth`, nên xử lý đúng tháng 30/31 ngày và tháng 2 năm nhuận.
- Mặc định hạn duyệt bổ sung là `5` ngày, hạn điều chỉnh là `10` ngày.
- Timestamp được lưu theo timezone; kỳ cũ không bị tính lại khi cấu hình thay đổi.

**Bằng chứng**

- `src/Backend/Domain/VPP/VppOrderPeriodSettingsVersion.cs:22-42`
- `src/Backend/Application/Domain/PeriodScheduleCalculator.cs:13-16`
- `src/Backend/Application/Domain/PeriodScheduleCalculator.cs:88-155`

**Cần quyết định**

- Code vẫn có trạng thái nội bộ/UI `Scheduled / Sắp mở`. Trước đây owner đã muốn bỏ cách gọi này. Nên chọn một trong hai:
  - giữ trạng thái nội bộ nhưng UI hiển thị thân thiện là `Chưa mở`; hoặc
  - bỏ hẳn nếu kỳ tương lai không cần tồn tại trước thời điểm mở.

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

### 2.3. Đơn bổ sung — **CẦN XÁC NHẬN CÁCH HIỂU**

**Code hiện tại**

- Đơn bổ sung cũng chỉ được **tạo khi kỳ còn mở**, giống đơn thường.
- Sau ngày đóng, grace `5` ngày chỉ dành cho quản lý duyệt một đơn bổ sung đã tạo trước đó.
- Đơn bổ sung mới có trạng thái `Pending`; chỉ đơn `Approved` mới đi vào bản chốt.
- Có giới hạn số lần bổ sung được duyệt và không cho tồn tại đồng thời nhiều đơn bổ sung đang chờ.

**Điểm cần owner xác nhận**

- Nếu nghiệp vụ mong muốn là “sau ngày đóng nhân viên mới phát hiện thiếu và tạo đơn bổ sung”, code hiện tại **chưa đáp ứng**.
- Nếu nghiệp vụ là “nhân viên gửi đơn bổ sung khi kỳ còn mở, quản lý có thêm 5 ngày để xử lý”, code hiện tại **đúng**.

**Bằng chứng**

- `src/Backend/Application/Services/Requests/VPPRequestService.cs:577-591`
- `src/Backend/Application/Services/Requests/VPPRequestService.cs:617-665`
- `src/Backend/Application/Services/Requests/VPPRequestService.cs:1872-1889`

**Khuyến nghị**

Chọn rõ một câu nghiệp vụ và dùng thống nhất trong UI, slide, luận văn và test. Phương án dễ kiểm soát hơn là giữ code hiện tại: đơn phải được tạo trước ngày đóng, chỉ kéo dài thời gian duyệt.

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

### 2.6. Danh mục, NCC và bảng giá — **ĐẠT PHẦN LÕI, còn dữ liệu ẩn**

**Đã đúng**

- Bảng giá thuộc một NCC.
- Tạo bảng giá xong có hiệu lực ngay (`Published`), không dùng bước nháp/công bố riêng trên UI.
- Mã mặt hàng dùng mã chung của hệ thống; trường mã riêng của NCC đã được loại khỏi mô hình hiện tại.
- Bảng giá có thể tạo thủ công, sao chép hoặc cập nhật bằng Excel.

**Rủi ro dữ liệu ẩn**

Backend/DTO/database vẫn nhận và tính các trường hiện không hiển thị cho người dùng:

- mã hợp đồng;
- chiết khấu;
- khoản giảm thêm;
- phụ phí;
- phí vận chuyển.

Nếu dữ liệu cũ, API hoặc import đưa giá trị khác `0`, các khoản này vẫn ảnh hưởng tổng chốt dù UI không giải thích cho quản lý.

**Bằng chứng**

- `src/Backend/Application/Services/CatalogPricing/PriceListService.cs:147-179`
- `src/Backend/Application/Services/CatalogPricing/PriceListService.cs:198-243`
- `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:503-529`
- `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:620-641`

**Khuyến nghị**

Trong phạm vi luận văn hiện tại, application service nên ép các khoản này về `0` và không cho public DTO thay đổi. Có thể giữ cột database để phát triển PO/hợp đồng sau này, nhưng không để dữ liệu ẩn tác động kết quả.

### 2.7. Nhập bảng giá Excel — **ĐẠT PHẦN LÕI, còn trường thừa**

**Đã đúng**

- File mẫu có sẵn mã, tên, đơn vị và giá hiện tại của danh mục hệ thống.
- Mã không tồn tại bị chặn.
- Mã trùng trong file bị chặn.
- Tên lệch chỉ cảnh báo vì mã là khóa đối chiếu.
- Đơn vị lệch bị chặn.
- Ô giá trống giữ nguyên dữ liệu cũ.
- Có bước đánh giá trước khi áp dụng thay đổi.

**Chưa đồng bộ với UI**

Parser và apply vẫn xử lý:

- `MinimumOrderQuantity`;
- `LeadTimeDays`;
- `IsDefault` ở cấp giá mặt hàng.

Các trường này đã được owner yêu cầu bỏ khỏi UI. Nếu còn nhận âm thầm qua file, người dùng khó hiểu vì sao kết quả chốt bị thay đổi.

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

### 2.9. Điều chỉnh sau chốt — **RỦI RO CAO**

**Đã đúng**

- Chỉ dùng cho kỳ đã chốt.
- Không ghi đè đơn cũ; khi duyệt tạo revision đơn mới.
- Người tạo yêu cầu không được tự duyệt.
- Yêu cầu đã duyệt chuyển kỳ sang trạng thái “có thay đổi chờ chốt lại”.
- Chốt lại tạo Bản chốt `N+1`, thay bản hiện hành nhưng không xóa lịch sử.
- Người chốt lại phải khác người xác nhận bản chốt trước.

**Lỗi chính**

Cấu hình `PostCloseAdjustmentDays = 10` được dùng để tính khả năng điều chỉnh trước khi chốt, nhưng hai luồng sau không kiểm tra deadline:

1. tạo/duyệt yêu cầu sửa hoặc hủy đơn sau chốt;
2. xác nhận chốt lại kỳ.

Hai service chỉ kiểm tra trạng thái `Settled`. Vì vậy về backend, quản lý có thể thực hiện sau 10 ngày nếu gọi đúng API.

**Bằng chứng**

- Có cấu hình: `src/Backend/Domain/VPP/VppOrderPeriodSettingsVersion.cs:33-38`
- DTO tính deadline: `src/Backend/Application/Services/Settlement/VppPeriodService.cs:854-912`
- Tạo/duyệt correction chỉ kiểm tra `Settled`: `src/Backend/Application/Services/Settlement/PostSettlementOrderCorrectionService.cs:47-99`, `156-188`
- Chốt lại chỉ kiểm tra `Settled`: `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:341-459`

**Cách sửa nên chọn**

- Tạo một policy dùng chung, ví dụ `PostCloseAdjustmentPolicy`.
- Deadline = `SubmissionDeadlineUtc + PostCloseAdjustmentDays` của đúng settings đã gắn với kỳ.
- Kiểm tra ở cả create correction, approve correction và confirm correction settlement.
- Trả câu thân thiện: `Đã hết thời hạn điều chỉnh của kỳ này.`
- Bổ sung test ở đúng trước hạn, đúng hạn và sau hạn.

### 2.10. Phạm vi sửa đơn sau chốt — **CẦN QUYẾT ĐỊNH**

Hiện tại có thể đổi số lượng, bỏ dòng hoặc hủy đơn; không thể thêm một mặt hàng chưa có trong bản chốt hiện hành.

**Bằng chứng**

- `src/Backend/Application/Services/Settlement/PostSettlementOrderCorrectionService.cs:350-368`

Đây là ràng buộc an toàn hợp lý. Nếu muốn xử lý “quên hẳn một mặt hàng”, nên dùng một quy trình bổ sung sau chốt riêng thay vì lặng lẽ cho thêm vào đơn cũ.

### 2.11. Chọn nhiều NCC — **TẠM TẮT ĐÚNG Ở UI, CHƯA TẮT HẲN Ở BACKEND**

- Frontend feature flag đang `false`, nên người dùng chỉ chốt với một NCC chính.
- Backend vẫn luôn tính đề xuất tối đa hai NCC khi dựng preview.
- Thuật toán chỉ so giá sau VAT; không tính hợp đồng hoặc vận chuyển; chỉ đề xuất nếu tiết kiệm ít nhất `2%` hoặc cần ghép để đủ mặt hàng.

**Bằng chứng**

- `src/Frontend/Blazor/appsettings.json:16`
- `src/Frontend/Blazor/Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor:90-125`
- `src/Backend/Application/Services/Settlement/PeriodSettlementService.cs:91-105`
- `src/Backend/Application/Services/Settlement/SettlementSupplierOptimizer.cs:7-14`, `31-116`

**Khuyến nghị**

Khi tính năng đang tạm tắt, backend cũng nên nhận feature flag và không chạy optimizer/không nhận exception nhiều NCC. Khi làm lại sau này mới bật đồng bộ frontend, backend, export và test.

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

## 6. Thứ tự xử lý đề xuất

### Wave A — sửa tính đúng nghiệp vụ

1. Enforce thời hạn điều chỉnh 10 ngày ở toàn bộ command backend.
2. Chốt cách hiểu thời điểm tạo đơn bổ sung và sửa UI/tài liệu/test cho thống nhất.
3. Ép các khoản thương mại đang ẩn về 0; bỏ trường import ẩn khỏi contract hiện hành.

### Wave B — đồng bộ capability

1. Tắt optimizer nhiều NCC ở backend khi feature flag tắt.
2. Quyết định `Scheduled / Sắp mở`.
3. Quyết định giữ admin-recovery hay loại các endpoint đóng/mở lại/xóa kỳ.
4. Việt hóa thông báo kỹ thuật.

### Wave C — refactor giữ nguyên hành vi

1. Khóa characterization test cho luồng tạo đơn, bổ sung, chốt và chốt lại.
2. Tách `VPPRequestService` theo command/query/policy.
3. Tách `PeriodSettlementService` theo preview/confirm/pricing/export/revision.
4. Chạy lại backend, frontend và route-real browser QA.

## 7. Kiểm tra đã thực hiện

| Kiểm tra | Kết quả |
|---|---|
| Test tập trung cho kỳ, giới hạn số lượng, chốt/chốt lại, import giá, tài khoản và export | `73 passed, 0 failed` |
| Rà controller truy cập DbContext trực tiếp | `0` kết quả |
| Đối chiếu frontend flag nhiều NCC với backend optimizer | Có sai lệch — UI tắt, backend vẫn tính |
| Đối chiếu cấu hình 10 ngày với command correction | Có sai lệch — chưa enforce |

## 8. Quyết định cần owner đối chiếu

1. Đơn bổ sung được **tạo trước ngày đóng** hay **được phép tạo trong 5 ngày sau ngày đóng**?
2. UI có cần trạng thái `Chưa mở` cho kỳ tương lai không?
3. Sau chốt có cho thêm mặt hàng hoàn toàn mới không, hay chỉ sửa/hủy phần đã chốt?
4. Các endpoint đóng/mở lại/xóa kỳ có giữ làm công cụ khôi phục cho quản trị hệ thống không?
5. Có đồng ý tạm ép hợp đồng/chiết khấu/phụ phí/vận chuyển về `0` cho đến khi module tương ứng được làm đầy đủ không?

