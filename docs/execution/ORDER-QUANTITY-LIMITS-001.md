# ORDER-QUANTITY-LIMITS-001 — Giới hạn số lượng đặt hàng linh hoạt

**Trạng thái:** `PLAN — CHỜ OWNER DUYỆT — CHƯA CODE/MIGRATION`

## 1. Bản một ánh nhìn

### Mục tiêu

Thay giới hạn kỹ thuật `1–9999` đang nằm trong ô nhập số lượng bằng quy tắc nghiệp vụ có thể cấu hình, dễ hiểu với nhân viên và không làm thay đổi các đơn cũ.

### Phương án khuyến nghị

- Mỗi mặt hàng có thể có hai mức độc lập: **tối đa trong một đơn** và **tối đa của một nhân viên trong cả kỳ**.
- Tính gộp đơn thường và đơn bổ sung của cùng nhân viên trong kỳ để không thể tách nhiều đơn nhằm vượt giới hạn.
- Có hai lớp cấu hình: mức mặc định toàn công ty và mức riêng cho từng mặt hàng.
- Cấu hình có kỳ bắt đầu áp dụng; không sửa ngược kỳ đã đóng/chốt.
- Kỳ đang mở vẫn được chỉnh an toàn: tăng hoặc bỏ giới hạn được áp dụng ngay; giảm chỉ được lưu khi không thấp hơn số đã đặt, nếu không phải chọn kỳ kế tiếp.
- Mức mặc định toàn công ty để trống nghĩa là không giới hạn. Mặt hàng riêng chọn rõ `Theo mặc định`, `Không giới hạn` hoặc `Tùy chỉnh`; không dùng `0` để tránh nhập nhầm ý nghĩa.
- Chưa thêm ngoại lệ theo từng phòng ban hoặc từng người trong đợt đầu.

### Vì sao không giới hạn “tổng số lượng của cả đơn”

Các đơn vị như Ram, Cây, Hộp và Cuộn không tương đương nhau. Một trần chung cho toàn đơn sẽ khó giải thích và dễ chặn sai nhu cầu. Giới hạn theo từng mặt hàng trong cả kỳ phản ánh đúng mục đích kiểm soát hơn.

### Các bước dự kiến

1. [Chốt quy tắc nghiệp vụ](#3-các-quyết-định-cần-owner-duyệt).
2. [Thêm cấu hình và dữ liệu hiệu lực](#4-thiết-kế-dữ-liệu-đề-xuất).
3. [Kiểm tra ở mọi đường tạo/sửa đơn](#5-quy-tắc-tính-và-kiểm-tra).
4. [Hiển thị giới hạn thân thiện trên UI](#6-ui-theo-design-system).
5. [Chạy migration/test an toàn](#8-kế-hoạch-thực-thi).

### Rủi ro chính

- Hai thao tác gửi đồng thời có thể cùng nhìn thấy số lượng còn lại; backend phải kiểm tra lại trong transaction.
- Đơn bổ sung, sửa đơn, khôi phục đơn và điều chỉnh sau đóng kỳ đều phải dùng chung một policy service.
- Hạ giới hạn giữa kỳ có thể thấp hơn số đã đặt; backend phải kiểm tra số đã dùng và hướng người quản trị chuyển hiệu lực sang kỳ kế tiếp.

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
| Q1 | Giới hạn theo dòng đơn hay cả kỳ | Cả kỳ: mỗi mặt hàng / mỗi nhân viên / mỗi kỳ |
| Q2 | Đơn bổ sung có cộng vào giới hạn không | Có; đơn chờ duyệt cũng tạm giữ hạn mức |
| Q3 | Mức `0` có nghĩa gì | Không cho nhập `0`; dùng trạng thái mặt hàng để ngừng đặt |
| Q4 | Không cấu hình thì xử lý thế nào | Không giới hạn để giữ behavior hiện tại |
| Q5 | Cho đổi giới hạn kỳ đang mở không | Có, nhưng chỉ khi mức mới không thấp hơn số đã đặt; nếu thấp hơn thì bắt đầu từ kỳ kế tiếp |
| Q6 | Quản lý có được vượt giới hạn không | Không vượt âm thầm; nếu cần sẽ làm thao tác ngoại lệ riêng, bắt buộc lý do/audit ở phase sau |
| Q7 | Có cấu hình riêng theo phòng ban/người dùng không | Chưa làm ở phase đầu; chỉ mở rộng khi có nghiệp vụ thật |
| Q8 | Có thêm giới hạn cho một đơn không | Có; dùng cùng policy mặt hàng, tách rõ với giới hạn cả kỳ |
| Q9 | Hiển thị cấu hình ở đâu | Trong Danh mục mặt hàng hệ thống; không đặt trong Bảng giá NCC |

### 3.1 Phân biệt hai loại giới hạn

| Loại | Ví dụ | Mục đích |
|---|---|---|
| `Tối đa/đơn` | Một đơn không quá 10 Ram giấy A4 | Chặn nhập nhầm số lượng quá lớn trong một lần |
| `Tối đa/kỳ` | Một nhân viên không quá 20 Ram trong kỳ 08/2026 | Kiểm soát tổng nhu cầu dù người dùng tạo đơn thường và đơn bổ sung |

Nếu cấu hình cả hai thì `Tối đa/đơn` không được lớn hơn `Tối đa/kỳ`. Trong chế độ `Tùy chỉnh`, ô nào để trống thì riêng chiều giới hạn đó không áp dụng.

## 4. Thiết kế dữ liệu đề xuất

### 4.1 Bảng policy hiệu lực theo kỳ

Tạo entity nội bộ `OrderQuantityLimitPolicy` theo hướng additive:

- `Id`
- `MemberCompanyCode`
- `VppItemId` nullable: `null` là mức mặc định toàn công ty; có giá trị là mức riêng của mặt hàng
- `Mode`: `Unlimited` hoặc `Custom`; mặt hàng không có policy riêng thì kế thừa mức mặc định
- `MaxQuantityPerOrder` nullable: số lượng tối đa của mặt hàng trong một đơn
- `MaxQuantityPerUserPerPeriod` nullable: `null` là không giới hạn
- `EffectiveFromYear`, `EffectiveFromMonth`
- audit chuẩn từ `BaseModel`

Ràng buộc:

- Các mức tối đa phải lớn hơn `0` nếu có giá trị.
- Khi cùng có giá trị, `MaxQuantityPerOrder <= MaxQuantityPerUserPerPeriod`.
- `Unlimited` yêu cầu hai mức tối đa đều null; `Custom` yêu cầu có ít nhất một mức tối đa.
- Một công ty chỉ có một policy cho cùng mặt hàng và cùng kỳ bắt đầu.
- Foreign key tới `VppItem` dùng restrict; không hard-delete policy lịch sử.
- Index theo `MemberCompanyCode + VppItemId + EffectiveFromYear + EffectiveFromMonth`.

### 4.2 Cách chọn policy

Với kỳ cần đặt:

1. Lấy policy riêng của mặt hàng mới nhất có kỳ hiệu lực không lớn hơn kỳ đang đặt.
2. Nếu không có policy riêng, lấy policy mặc định toàn công ty theo cùng quy tắc.
3. `Unlimited` nghĩa là không giới hạn; `Custom` áp dụng từng mức đã nhập.
4. Nếu không có cả policy riêng lẫn mặc định, giữ behavior hiện tại là không giới hạn.

Thiết kế này giữ được lịch sử theo kỳ mà không cần sửa các đơn cũ hoặc snapshot hàng loạt toàn catalog.

## 5. Quy tắc tính và kiểm tra

### 5.1 Số lượng đã dùng

Tính tổng `Qty` của cùng `CreatedByUserId + VppItemId + PeriodId`, chỉ lấy revision hiện hành và các đơn còn hiệu lực:

- tính: đã gửi, chờ duyệt bổ sung, đã duyệt;
- không tính: nháp local, bị từ chối, bị hủy, đã bị thay thế, soft-delete.

### 5.2 Khi tạo hoặc sửa

```text
Số lượng dòng hiện tại <= Tối đa/đơn
Số còn có thể đặt trong kỳ = Tối đa/kỳ - Số đã dùng ở các đơn khác
```

- Khi sửa một đơn, loại chính series của đơn đó khỏi “số đã dùng”, rồi kiểm tra toàn bộ số lượng mới.
- Đơn thường và đơn bổ sung dùng cùng công thức.
- Một dòng phải đồng thời đạt cả giới hạn của một đơn và giới hạn còn lại trong kỳ.
- Tạo lại/khôi phục đơn và điều chỉnh sau đóng kỳ cũng phải gọi chung `OrderQuantityLimitService`.
- Backend là authority cuối; `max` trên input chỉ hỗ trợ người dùng.
- Kiểm tra và ghi đơn phải nằm trong transaction phù hợp để tránh hai request đồng thời cùng vượt trần.

### 5.3 Thông báo thân thiện

Ví dụ:

> Giấy A4 được đặt tối đa 10 Ram mỗi đơn và 20 Ram trong kỳ 08/2026. Bạn đã đặt 15 Ram, còn có thể đặt 5 Ram.

Không hiển thị tên class, policy code hoặc lỗi SQL cho người dùng.

## 6. UI theo design system

### 6.1 Nhân viên đặt hàng

- Dưới tên/đơn vị mặt hàng hiển thị dòng phụ `Tối đa 10/đơn · 20/kỳ · Còn 5` khi có giới hạn.
- Stepper dùng giá trị nhỏ hơn giữa `Tối đa/đơn` và số còn lại trong kỳ; nút `+` mờ khi đạt giới hạn nhưng vẫn giữ Stable Capability Surface.
- Nhập vượt mức tự đưa về mức hợp lệ và hiện validation ngay cạnh dòng, không chỉ toast.
- Bước xem lại hiển thị cảnh báo nếu dữ liệu draft cũ không còn hợp lệ.

### 6.2 Quản trị hệ thống

- `Cấu hình đặt hàng`: nhập mức mặc định và chọn kỳ bắt đầu áp dụng.
- `Danh mục mặt hàng`: action cấu hình giới hạn riêng với ba lựa chọn `Theo mặc định`, `Không giới hạn`, `Tùy chỉnh`.
- Grid quản trị thêm hai cột pickable `Tối đa/đơn` và `Tối đa/kỳ`. Đây là dữ liệu chính sách đặt hàng được join từ policy, không thêm trực tiếp vào bảng giá NCC.
- Hiển thị trước câu dễ hiểu: `Áp dụng từ kỳ 09/2026 · Tối đa 10/đơn · 20/người/kỳ`.
- Khi chọn kỳ đang mở, form hiển thị số đã đặt cao nhất và chặn mức mới thấp hơn số đó; gợi ý sẵn kỳ kế tiếp thay vì báo lỗi kỹ thuật.
- Quyền mới đề xuất `ORDER_QUANTITY_LIMIT_MANAGE`, mặc định chỉ cấp Quản trị hệ thống.

## 7. API và module ownership

- Shared: DTO đọc/ghi policy và thông tin giới hạn còn lại cho catalog đặt hàng.
- Backend Requests: `OrderQuantityLimitService` resolve policy, tính usage và validate mutation.
- Backend Catalog/Admin: query/mutation policy có authorization riêng.
- Frontend Requests: `OrderEditorSession` nhận giới hạn đã resolve; không tự suy luận nghiệp vụ.
- Frontend Admin: trang cấu hình dùng component/form theo design system hiện hành.

Không đặt business rule trong Razor và không dùng giới hạn NCC `MinimumOrderQuantity` cho mục đích này.

## 8. Kế hoạch thực thi

| Phase | Nội dung | Kiểm tra bắt buộc |
|---|---|---|
| Q0 | Characterization hiện trạng, chốt Q1–Q7 | Tests khóa behavior hiện tại và đường mutation |
| Q1 | Entity, mapping, migration additive, API policy | EF pending-model, SQL review, fresh/upgrade LocalDB |
| Q2 | Resolver + validation chung cho create/update/recreate/restore/post-close | Unit + integration, concurrency và revision cases |
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
