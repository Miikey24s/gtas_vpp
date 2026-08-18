# ORDER-QUANTITY-LIMITS-001 — Giới hạn số lượng đặt hàng linh hoạt

**Trạng thái:** `PLAN — CHỜ OWNER DUYỆT — CHƯA CODE/MIGRATION`

## 1. Bản một ánh nhìn

### Mục tiêu

Thay giới hạn kỹ thuật `1–9999` đang nằm trong ô nhập số lượng bằng quy tắc nghiệp vụ có thể cấu hình, dễ hiểu với nhân viên và không làm thay đổi các đơn cũ.

### Phương án khuyến nghị

- Mỗi mặt hàng có một mức **tối đa trong một đơn**.
- Cùng một giới hạn áp dụng độc lập cho cả đơn thường và đơn bổ sung; không cộng dồn hai đơn thành giới hạn của cả kỳ.
- Ví dụ giới hạn 20 Ram/đơn: đơn thường được đặt tối đa 20 Ram và đơn bổ sung cũng được đặt tối đa 20 Ram. Tổng hai đơn có thể là 40 Ram; đây là behavior được chấp nhận theo quyết định nghiệp vụ hiện tại.
- Có hai lớp cấu hình: mức mặc định toàn công ty và mức riêng cho từng mặt hàng.
- Cấu hình có kỳ bắt đầu áp dụng; không sửa ngược kỳ đã đóng/chốt.
- Kỳ đang mở vẫn được chỉnh: đơn đã gửi giữ nguyên; draft và các lần sửa/gửi tiếp theo phải đạt giới hạn mới.
- Mức mặc định toàn công ty để trống nghĩa là không giới hạn. Mặt hàng riêng chọn rõ `Theo mặc định`, `Không giới hạn` hoặc `Tùy chỉnh`; không dùng `0` để tránh nhập nhầm ý nghĩa.
- Chưa thêm ngoại lệ theo từng phòng ban hoặc từng người trong đợt đầu.

### Vì sao không giới hạn “tổng số lượng của cả đơn”

Các đơn vị như Ram, Cây, Hộp và Cuộn không tương đương nhau. Một trần chung cho tổng số lượng của toàn đơn sẽ khó giải thích và dễ chặn sai nhu cầu. Giới hạn riêng cho từng mặt hàng trong từng đơn phản ánh đúng mục đích kiểm soát hơn.

### Các bước dự kiến

1. [Chốt quy tắc nghiệp vụ](#3-các-quyết-định-cần-owner-duyệt).
2. [Thêm cấu hình và dữ liệu hiệu lực](#4-thiết-kế-dữ-liệu-đề-xuất).
3. [Kiểm tra ở mọi đường tạo/sửa đơn](#5-quy-tắc-tính-và-kiểm-tra).
4. [Hiển thị giới hạn thân thiện trên UI](#6-ui-theo-design-system).
5. [Chạy migration/test an toàn](#8-kế-hoạch-thực-thi).

### Rủi ro chính

- Đơn bổ sung, sửa đơn, khôi phục đơn và điều chỉnh sau đóng kỳ đều phải dùng chung một policy service.
- Hạ giới hạn giữa kỳ có thể làm draft hoặc đơn đang chỉnh sửa không còn hợp lệ; UI phải báo rõ và backend phải kiểm tra lại khi gửi.

## 2. Hiện trạng đã kiểm tra

- Frontend `OrderCreateStep2.razor` đang đặt `max="9999"`.
- `OrderEditorSession` đang `Math.Clamp(..., 1, 9999)`.
- Backend `VPPRequestService.ValidateItems` mới kiểm tra số lượng lớn hơn `0` và không trùng mặt hàng; chưa có trần nghiệp vụ.
- Các luồng tạo, sửa, tạo lại, khôi phục và điều chỉnh sau đóng kỳ đều đi qua dữ liệu `Qty`, nên cần một nơi kiểm tra dùng chung.
- `MinimumOrderQuantity` hiện có thuộc báo giá/NCC và là **số lượng tối thiểu khi mua từ NCC**, không phải giới hạn tối đa nhân viên được đặt.
- `SupplierProductMapping` chỉ được biết sau khi chọn NCC/bảng giá ở bước chốt kỳ. Vì vậy `Tối đa mỗi đơn` không được đặt trong bảng giá NCC; nếu làm vậy, cùng một đơn của nhân viên có thể hợp lệ hoặc không hợp lệ chỉ vì quản lý đổi NCC khi chốt kỳ.

## 3. Các quyết định cần owner duyệt

| Mã | Đề xuất | Khuyến nghị |
|---|---|---|
| Q1 | Giới hạn theo dòng đơn hay cả kỳ | Theo từng mặt hàng trong từng đơn |
| Q2 | Áp dụng cho đơn bổ sung thế nào | Dùng cùng giới hạn với đơn thường, nhưng kiểm tra độc lập từng đơn |
| Q3 | Mức `0` có nghĩa gì | Không cho nhập `0`; dùng trạng thái mặt hàng để ngừng đặt |
| Q4 | Không cấu hình thì xử lý thế nào | Không giới hạn để giữ behavior hiện tại |
| Q5 | Cho đổi giới hạn kỳ đang mở không | Có; đơn đã gửi giữ nguyên, draft và lần sửa/gửi tiếp theo phải đạt giới hạn mới |
| Q6 | Quản lý có được vượt giới hạn không | Không vượt âm thầm; nếu cần sẽ làm thao tác ngoại lệ riêng, bắt buộc lý do/audit ở phase sau |
| Q7 | Có cấu hình riêng theo phòng ban/người dùng không | Chưa làm ở phase đầu; chỉ mở rộng khi có nghiệp vụ thật |
| Q8 | Hiển thị cấu hình ở đâu | Trong Danh mục mặt hàng hệ thống; không đặt trong Bảng giá NCC |

### 3.1 Cách áp dụng cho hai loại đơn

| Loại đơn | Giới hạn 20 Ram/đơn | Cách kiểm tra |
|---|---:|---|
| Đơn thường | Tối đa 20 Ram | Chỉ kiểm tra số lượng trong đơn thường |
| Đơn bổ sung | Tối đa 20 Ram | Chỉ kiểm tra số lượng trong đơn bổ sung |

Không lấy số lượng đơn thường trừ khỏi đơn bổ sung. Nếu sau này doanh nghiệp cần giới hạn tổng cấp phát theo kỳ, đó là một policy khác và phải được duyệt riêng.

## 4. Thiết kế dữ liệu đề xuất

### 4.1 Bảng policy hiệu lực theo kỳ

Tạo entity nội bộ `OrderQuantityLimitPolicy` theo hướng additive:

- `Id`
- `MemberCompanyCode`
- `VppItemId` nullable: `null` là mức mặc định toàn công ty; có giá trị là mức riêng của mặt hàng
- `Mode`: `Unlimited` hoặc `Custom`; mặt hàng không có policy riêng thì kế thừa mức mặc định
- `MaxQuantityPerOrder` nullable: số lượng tối đa của mặt hàng trong một đơn
- `EffectiveFromYear`, `EffectiveFromMonth`
- audit chuẩn từ `BaseModel`

Ràng buộc:

- Các mức tối đa phải lớn hơn `0` nếu có giá trị.
- `Unlimited` yêu cầu `MaxQuantityPerOrder` là null; `Custom` yêu cầu có `MaxQuantityPerOrder`.
- Một công ty chỉ có một policy cho cùng mặt hàng và cùng kỳ bắt đầu.
- Foreign key tới `VppItem` dùng restrict; không hard-delete policy lịch sử.
- Index theo `MemberCompanyCode + VppItemId + EffectiveFromYear + EffectiveFromMonth`.

### 4.2 Cách chọn policy

Với kỳ cần đặt:

1. Lấy policy riêng của mặt hàng mới nhất có kỳ hiệu lực không lớn hơn kỳ đang đặt.
2. Nếu không có policy riêng, lấy policy mặc định toàn công ty theo cùng quy tắc.
3. `Unlimited` nghĩa là không giới hạn; `Custom` áp dụng `MaxQuantityPerOrder`.
4. Nếu không có cả policy riêng lẫn mặc định, giữ behavior hiện tại là không giới hạn.

Thiết kế này giữ được lịch sử theo kỳ mà không cần sửa các đơn cũ hoặc snapshot hàng loạt toàn catalog.

## 5. Quy tắc tính và kiểm tra

### 5.1 Khi tạo hoặc sửa

```text
Số lượng dòng hiện tại <= Tối đa/đơn
```

- Đơn thường và đơn bổ sung dùng cùng công thức, nhưng mỗi đơn được kiểm tra riêng.
- Không truy vấn hoặc cộng số lượng từ đơn còn lại trong cùng kỳ.
- Tạo lại/khôi phục đơn và điều chỉnh sau đóng kỳ cũng phải gọi chung `OrderQuantityLimitService`.
- Backend là authority cuối; `max` trên input chỉ hỗ trợ người dùng.

### 5.2 Thông báo thân thiện

Ví dụ:

> Giấy A4 được đặt tối đa 20 Ram trong mỗi đơn. Số lượng hiện tại là 25 Ram, vui lòng giảm còn 20 Ram hoặc ít hơn.

Không hiển thị tên class, policy code hoặc lỗi SQL cho người dùng.

## 6. UI theo design system

### 6.1 Nhân viên đặt hàng

- Dưới tên/đơn vị mặt hàng hiển thị dòng phụ `Tối đa 20/đơn` khi có giới hạn.
- Stepper dùng `Tối đa/đơn`; nút `+` mờ khi đạt giới hạn nhưng vẫn giữ Stable Capability Surface.
- Nhập vượt mức tự đưa về mức hợp lệ và hiện validation ngay cạnh dòng, không chỉ toast.
- Bước xem lại hiển thị cảnh báo nếu dữ liệu draft cũ không còn hợp lệ.

### 6.2 Quản trị hệ thống

- `Cấu hình đặt hàng`: nhập mức mặc định và chọn kỳ bắt đầu áp dụng.
- `Danh mục mặt hàng`: action cấu hình giới hạn riêng với ba lựa chọn `Theo mặc định`, `Không giới hạn`, `Tùy chỉnh`.
- Grid quản trị thêm cột pickable `Tối đa/đơn`. Đây là dữ liệu chính sách đặt hàng được join từ policy, không thêm trực tiếp vào bảng giá NCC.
- Hiển thị trước câu dễ hiểu: `Áp dụng từ kỳ 09/2026 · Tối đa 20/đơn`.
- Khi chọn kỳ đang mở, form cảnh báo rằng giới hạn mới áp dụng cho draft và các lần sửa/gửi tiếp theo; đơn đã gửi không bị tự động thay đổi.
- Quyền mới đề xuất `ORDER_QUANTITY_LIMIT_MANAGE`, mặc định chỉ cấp Quản trị hệ thống.

## 7. API và module ownership

- Shared: DTO đọc/ghi policy và mức giới hạn đã resolve cho catalog đặt hàng.
- Backend Requests: `OrderQuantityLimitService` resolve policy và validate mutation.
- Backend Catalog/Admin: query/mutation policy có authorization riêng.
- Frontend Requests: `OrderEditorSession` nhận giới hạn đã resolve; không tự suy luận nghiệp vụ.
- Frontend Admin: trang cấu hình dùng component/form theo design system hiện hành.

Không đặt business rule trong Razor và không dùng giới hạn NCC `MinimumOrderQuantity` cho mục đích này.

## 8. Kế hoạch thực thi

| Phase | Nội dung | Kiểm tra bắt buộc |
|---|---|---|
| Q0 | Characterization hiện trạng, chốt Q1–Q8 | Tests khóa behavior hiện tại và đường mutation |
| Q1 | Entity, mapping, migration additive, API policy | EF pending-model, SQL review, fresh/upgrade LocalDB |
| Q2 | Resolver + validation chung cho create/update/recreate/restore/post-close | Unit + integration, boundary và revision cases |
| Q3 | Catalog/order DTO + UI đặt hàng | Frontend tests, responsive route-real, draft cũ |
| Q4 | UI quản trị + permission/audit | RBAC tests, route-real System Admin |
| Q5 | Full verify và owner review | `gtas verify`, diff/SQL/recovery review |

## 9. Ngoài phạm vi phase đầu

- Hạn mức ngân sách bằng tiền.
- Hạn mức riêng theo phòng ban hoặc từng tài khoản.
- Tự đề xuất số lượng bằng AI.
- Tồn kho, định mức cấp phát hoặc phê duyệt ngoại lệ tự động.
- Thay đổi `MinimumOrderQuantity` của NCC.

Các mục này chỉ mở sau khi có dữ liệu nghiệp vụ và owner duyệt riêng.
