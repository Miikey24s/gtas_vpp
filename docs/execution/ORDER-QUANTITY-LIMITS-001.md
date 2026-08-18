# ORDER-QUANTITY-LIMITS-001 — Giới hạn số lượng đặt hàng linh hoạt

**Trạng thái:** `COMPLETED — ĐÃ KIỂM TRA ROUTE THỰC TẾ`

**Ngày triển khai:** `2026-08-18`

## 1. Bản một ánh nhìn

### Mục tiêu

Thay giới hạn kỹ thuật `1–9999` bằng trần an toàn theo từng mặt hàng để chặn số lượng vô lý như `1.000.000–2.000.000`, nhưng vẫn đủ rộng cho nhu cầu văn phòng thông thường.

### Phương án khuyến nghị

- Mỗi mặt hàng có một mức **tối đa trong một đơn**.
- Cùng một giới hạn áp dụng độc lập cho cả đơn thường và đơn bổ sung; không cộng dồn hai đơn thành giới hạn của cả kỳ.
- Đơn thường và đơn bổ sung đi qua cùng một bộ kiểm tra `Qty`; trường `IsAdditionalOrder` không tạo ra hai công thức giới hạn khác nhau.
- Trần hệ thống là `1.000` cho một mặt hàng trong một đơn; mặt hàng mới mặc định nhận mức này.
- Dữ liệu hiện có được sinh mức ban đầu theo công thức đơn giản: `số lớn nhất từng đặt × 3`, sau đó ép trong khoảng `500–1.000`.
- Quản trị hệ thống có thể sửa mức của từng mặt hàng trong khoảng `1–1.000`; không cần policy hiệu lực theo kỳ hoặc màn cấu hình nhiều tầng.
- Đơn đã gửi giữ nguyên. Draft và các lần sửa/gửi tiếp theo phải đạt giới hạn hiện tại của mặt hàng.
- Chưa thêm ngoại lệ theo từng phòng ban hoặc từng người trong đợt đầu.

### Vì sao không giới hạn “tổng số lượng của cả đơn”

Các đơn vị như Ram, Cây, Hộp và Cuộn không tương đương nhau. Một trần chung cho tổng số lượng của toàn đơn sẽ khó giải thích và dễ chặn sai nhu cầu. Giới hạn riêng cho từng mặt hàng trong từng đơn phản ánh đúng mục đích kiểm soát hơn.

### Các bước đã thực hiện

1. [Chốt quy tắc nghiệp vụ](#3-các-quyết-định-đã-duyệt).
2. [Thêm cột giới hạn vào danh mục mặt hàng](#4-thiết-kế-dữ-liệu-đề-xuất).
3. [Kiểm tra ở mọi đường tạo/sửa đơn](#5-quy-tắc-tính-và-kiểm-tra).
4. [Hiển thị giới hạn thân thiện trên UI](#6-ui-theo-design-system).
5. [Chạy migration/test an toàn](#8-kế-hoạch-thực-thi).

### Rủi ro chính

- Đơn bổ sung, sửa đơn, khôi phục đơn và điều chỉnh sau đóng kỳ đều phải dùng chung một service kiểm tra.
- Hạ giới hạn có thể làm draft hoặc đơn đang chỉnh sửa không còn hợp lệ; UI phải báo rõ và backend phải kiểm tra lại khi gửi.

## 2. Hiện trạng trước khi triển khai

- Frontend `OrderCreateStep2.razor` đang đặt `max="9999"`.
- `OrderEditorSession` đang `Math.Clamp(..., 1, 9999)`.
- Backend `VPPRequestService.ValidateItems` mới kiểm tra số lượng lớn hơn `0` và không trùng mặt hàng; chưa có trần nghiệp vụ.
- Các luồng tạo, sửa, tạo lại, khôi phục và điều chỉnh sau đóng kỳ đều đi qua dữ liệu `Qty`, nên cần một nơi kiểm tra dùng chung.
- Đơn thường và đơn bổ sung dùng cùng entity/dòng chi tiết; khác biệt nghiệp vụ chính nằm ở `IsAdditionalOrder`, trạng thái và lý do bổ sung.
- `MinimumOrderQuantity` hiện có thuộc báo giá/NCC và là **số lượng tối thiểu khi mua từ NCC**, không phải giới hạn tối đa nhân viên được đặt.
- `SupplierProductMapping` chỉ được biết sau khi chọn NCC/bảng giá ở bước chốt kỳ. Vì vậy `Tối đa mỗi đơn` không được đặt trong bảng giá NCC; nếu làm vậy, cùng một đơn của nhân viên có thể hợp lệ hoặc không hợp lệ chỉ vì quản lý đổi NCC khi chốt kỳ.

## 3. Các quyết định đã duyệt

| Mã | Đề xuất | Khuyến nghị |
|---|---|---|
| Q1 | Giới hạn theo dòng đơn hay cả kỳ | Theo từng mặt hàng trong từng đơn |
| Q2 | Áp dụng cho đơn bổ sung thế nào | Dùng cùng giới hạn với đơn thường, nhưng kiểm tra độc lập từng đơn |
| Q3 | Mức `0` có nghĩa gì | Không cho nhập `0`; dùng trạng thái mặt hàng để ngừng đặt |
| Q4 | Mức mặc định là bao nhiêu | `1.000` cho mỗi mặt hàng trong một đơn |
| Q5 | Cho đổi giới hạn khi đang có kỳ mở không | Có; đơn đã gửi giữ nguyên, draft và lần sửa/gửi tiếp theo phải đạt giới hạn mới |
| Q6 | Quản lý có được vượt giới hạn không | Không vượt âm thầm; nếu cần sẽ làm thao tác ngoại lệ riêng, bắt buộc lý do/audit ở phase sau |
| Q7 | Có cấu hình riêng theo phòng ban/người dùng không | Chưa làm ở phase đầu; chỉ mở rộng khi có nghiệp vụ thật |
| Q8 | Hiển thị cấu hình ở đâu | Trong Danh mục mặt hàng hệ thống; không đặt trong Bảng giá NCC |
| Q9 | Mức giới hạn ban đầu lấy từ đâu | `Clamp(MaxQtyTừngĐặt × 3, 500, 1.000)`; chưa có lịch sử thì dùng `1.000` |

### 3.1 Cách áp dụng cho hai loại đơn

| Loại đơn | Ví dụ giới hạn 500/đơn | Cách kiểm tra |
|---|---:|---|
| Đơn thường | Tối đa 500 | Chỉ kiểm tra số lượng trong đơn thường |
| Đơn bổ sung | Tối đa 500 | Chỉ kiểm tra số lượng trong đơn bổ sung |

Không lấy số lượng đơn thường trừ khỏi đơn bổ sung. Nếu sau này doanh nghiệp cần giới hạn tổng cấp phát theo kỳ, đó là một quy tắc khác và phải được duyệt riêng.

Việc kiểm tra không tách thành hai service. `OrderQuantityLimitService` nhận danh sách dòng của một đơn và áp dụng cùng giới hạn mặt hàng, bất kể `IsAdditionalOrder` là `true` hay `false`.

## 4. Thiết kế dữ liệu đã triển khai

### 4.1 Cột giới hạn trên mặt hàng

Thêm trực tiếp vào `VppItems` theo hướng additive:

- `MaxQuantityPerOrder int not null`, mặc định `1.000`.
- Check constraint: giá trị từ `1` đến `1.000`.
- Không tạo bảng policy riêng, không tạo phiên bản và không gắn kỳ hiệu lực.

Đây là thuộc tính của danh mục mặt hàng hệ thống, không thuộc bảng giá hay nhà cung cấp. Cùng một mặt hàng luôn có cùng trần an toàn dù quản lý chọn NCC/bảng giá nào khi chốt kỳ.

### 4.2 Sinh giá trị cho dữ liệu hiện có

Chạy backfill xác định, idempotent cho các mặt hàng hiện có:

```text
Mức sinh = Clamp(Số lượng lớn nhất từng đặt × 3, 500, 1.000)
```

- Chỉ đọc revision hiện hành của các đơn không bị hủy, từ chối hoặc soft-delete.
- Đơn thường và đơn bổ sung được xem như nhau vì cùng trường `Qty`.
- Mặt hàng chưa từng được đặt nhận mức `1.000`.
- Kết quả luôn là số nguyên và không vượt trần hệ thống.
- Backfill chỉ chạy một lần; về sau không tự đổi giá trị quản trị đã sửa.

Ví dụ:

| Số lớn nhất từng đặt | Nhân 3 | Mức lưu |
|---:|---:|---:|
| 72 | 216 | 500 |
| 200 | 600 | 600 |
| 756 | 2.268 | 1.000 |

Chọn hệ số `3` thay vì `5` vì vẫn tạo khoảng dự phòng lớn nhưng ít đẩy mọi mặt hàng lên trần `1.000`.

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

> Giấy A4 được đặt tối đa 500 Ram trong mỗi đơn. Số lượng hiện tại là 1.200 Ram, vui lòng giảm còn 500 Ram hoặc ít hơn.

Không hiển thị tên class, mã kỹ thuật hoặc lỗi SQL cho người dùng.

## 6. UI theo design system

### 6.1 Nhân viên đặt hàng

- Dưới tên/đơn vị mặt hàng hiển thị dòng phụ `Tối đa 500/đơn`.
- Stepper dùng `Tối đa/đơn`; nút `+` mờ khi đạt giới hạn nhưng vẫn giữ Stable Capability Surface.
- Nhập vượt mức tự đưa về mức hợp lệ và hiện validation ngay cạnh dòng, không chỉ toast.
- Bước xem lại hiển thị cảnh báo nếu dữ liệu draft cũ không còn hợp lệ.

### 6.2 Quản trị hệ thống

- `Danh mục mặt hàng`: thêm trường số `Tối đa/đơn`, cho nhập từ `1–1.000`.
- Grid quản trị thêm cột pickable `Tối đa/đơn`, đọc trực tiếp từ mặt hàng; không thêm vào bảng giá NCC.
- Khi sửa, helper text giải thích ngắn: `Giới hạn cho một mặt hàng trong mỗi đơn`.
- Nếu đang có draft vượt mức mới, hệ thống không tự sửa draft; nhân viên sẽ được báo khi mở hoặc gửi lại.
- Tái sử dụng quyền quản trị danh mục mặt hàng hiện có; không thêm permission riêng chỉ cho một trường dữ liệu.

## 7. API và module ownership

- Shared: DTO mặt hàng bổ sung `MaxQuantityPerOrder` cho luồng quản trị và đặt hàng.
- Backend Requests: `OrderQuantityLimitService` đọc giới hạn từ mặt hàng và validate mutation.
- Backend Catalog/Admin: cập nhật giới hạn qua cùng authorization quản trị danh mục hiện có.
- Frontend Requests: `OrderEditorSession` nhận giới hạn từ dữ liệu mặt hàng; không tự suy luận nghiệp vụ.
- Frontend Admin: trang cấu hình dùng component/form theo design system hiện hành.

Không đặt business rule trong Razor và không dùng giới hạn NCC `MinimumOrderQuantity` cho mục đích này.

## 8. Kết quả thực thi

| Phase | Nội dung | Kiểm tra bắt buộc |
|---|---|---|
| Q0 | Characterization hiện trạng, chốt Q1–Q9 | Tests khóa behavior hiện tại và đường mutation |
| Q1 | Thêm cột `VppItems.MaxQuantityPerOrder`, migration additive và backfill ×3 trong khoảng 500–1.000 | EF pending-model, SQL review, fresh/upgrade LocalDB, backfill idempotent |
| Q2 | Validation chung cho create/update/recreate/restore/post-close | Unit + integration, boundary và revision cases |
| Q3 | Catalog/order DTO + UI đặt hàng | Frontend tests, responsive route-real, draft cũ |
| Q4 | UI quản trị + permission/audit | RBAC tests, route-real System Admin |
| Q5 | Full verify và owner review | `gtas verify`, diff/SQL/recovery review |

### 8.1 Bằng chứng kiểm tra hiện tại

- Backend unit tests: `577/577` đạt.
- Frontend unit tests: `503/503` đạt.
- Frontend UI contract tests: `2/2` đạt.
- Backend integration tests mặc định: `14` đạt, `11` test LocalDB opt-in được bỏ qua theo cấu hình suite.
- EF model: không còn pending model changes.
- Migration đã chạy thành công cho cả database mới và database nâng cấp từ migration liền trước trên LocalDB.
- `gtas verify -Scope backend` và `gtas verify -Scope frontend` đều đạt; build Release không có warning/error, NuGet audit và secret scan đều sạch.
- Authenticated E2E `OrderQuantityLimitUiTests.QuantityLimit_IsConfigurableAndVisibleAcrossAdminAndOrderFlow`: `1/1` đạt trên database/host cô lập.
- Route quản trị đã xác nhận trường `Tối đa/đơn` có khoảng `1–1.000` và mặc định `1.000`.
- Route tạo đơn đã xác nhận nhãn `Tối đa 1.000/đơn`, input dùng đúng trần, nút tăng khóa khi đạt mức tối đa và bước xem lại giữ đúng số lượng.
- Đã kiểm tra trực quan ảnh route thật ở viewport `1366×768`; dialog quản trị và bước xem lại đơn không vỡ layout.
- Không khởi động lại hoặc chiếm quyền process `dotnet watch` của owner; E2E sử dụng AppHost riêng và tự dọn môi trường cô lập.

## 9. Ngoài phạm vi phase đầu

- Hạn mức ngân sách bằng tiền.
- Hạn mức riêng theo phòng ban hoặc từng tài khoản.
- Tự đề xuất số lượng bằng AI.
- Tồn kho, định mức cấp phát hoặc phê duyệt ngoại lệ tự động.
- Thay đổi `MinimumOrderQuantity` của NCC.

Các mục này chỉ mở sau khi có dữ liệu nghiệp vụ và owner duyệt riêng.
