# MULTI-PERIOD-ORDERING-001 — Quản lý và đặt hàng nhiều kỳ

- Status: **IMPLEMENTED HISTORY — PRODUCT DIRECTION SUPERSEDED BY NEW PLAN**
- Priority: P1
- Owner/agent: Owner GTAS VPP / Codex triển khai
- Branch/base: `Nam` @ `aac4aa1a`
- Revised at (Asia/Ho_Chi_Minh): 2026-08-10
- Approval boundary: owner đã duyệt thực thi revision ngày 2026-08-10; source, database và route-real QA đã hoàn tất, đang chờ owner duyệt UI/nghiệp vụ
- Supplier scope: DEFERRED — NCC, vận chuyển, bảng giá, optimizer và settlement award không thuộc plan này

> Owner revision `2026-08-18`: rolling horizon 3 kỳ không còn là target sản phẩm. Hệ thống hiện chỉ tự mở một kỳ; Quản lý dùng `Thêm kỳ` khi cần đặt trước. Mặc định 5 ngày duyệt bổ sung và 10 ngày chỉnh đơn được cấu hình toàn cục, đồng thời có thể ghi đè riêng khi thêm kỳ. Xem [ORDERING-PRICING-REVISION-20260818](ORDERING-PRICING-REVISION-20260818.md#period-plan). Nội dung dưới đây chỉ được giữ làm lịch sử thiết kế cũ, không còn là product authority.

## 0. Bản một ánh nhìn

| Mục | Phương án khuyến nghị | Chi tiết |
|---|---|---|
| Mô hình chính | Hệ thống duy trì danh sách kỳ đang nhận đơn; mặc định luôn có kỳ hiện tại + 2 kỳ tương lai | [Period instance](#period-instance) |
| Mặc định | Rolling 3 kỳ: đến ngày chuyển kỳ, kỳ cũ đóng và hệ thống mở thêm kỳ xa nhất để vẫn còn 3 kỳ | [Cấu hình mặc định](#period-template) |
| Tùy chỉnh | Mặc định chọn tháng liên tiếp; phần Nâng cao mới cho chọn tháng rời rạc hoặc đổi lịch riêng từng kỳ | [Create periods](#create-periods) |
| Nhân viên | Chỉ chọn trong các kỳ đang nhận đơn; không phải hiểu settings version hoặc state kỹ thuật | [Employee UX](#employee-multi-period) |
| Kỳ hiện tại có đơn | Không đổi settings thành 0. Dùng command `Đóng nhận đơn` để chuyển `Open → SubmissionClosed` | [Close vs settle](#close-versus-settle) |
| Chốt kỳ | Vẫn là nghiệp vụ procurement/settlement phía sau; không đồng nghĩa với đóng nhận đơn | [Close vs settle](#close-versus-settle) |
| Chủ sở hữu cấu hình | Quản trị hệ thống quản lý mặc định toàn công ty; Quản lý kỳ chỉ vận hành và override từng kỳ có lý do | [Plan revision](#plan-revision-2026-08-09) |
| Hạn duyệt đơn bổ sung | Đổi mặc định từ 2 thành **5 ngày**; chỉ áp dụng cho kỳ tạo sau phiên bản cấu hình mới | [Plan revision](#plan-revision-2026-08-09) |
| Thời gian chỉnh đơn sau đóng | Mặc định **10 ngày từ ngày đóng**; Quản lý được cập nhật/hủy đơn có lý do. Sau hạn này chỉ còn chọn NCC, bảng giá và chốt kỳ | [Post-close adjustment](#post-close-adjustment) |
| Khóa kỳ | Dùng `Khóa kỳ / Mở khóa kỳ`; thao tác này chỉ dừng/cho phép nhận đơn, trạng thái `Đã khóa` vẫn tách rõ với `Đã chốt` | [Plan revision](#plan-revision-2026-08-09) |
| Điều chỉnh sau chốt | Không đưa kỳ quay lại trạng thái cũ. Mỗi lần điều chỉnh hợp lệ tạo **bản chốt kế tiếp**; bản trước luôn được giữ để đối chiếu | [Post-close adjustment](#post-close-adjustment) |
| Đơn thuộc kỳ đã chốt | Chỉ người có quyền `PERIOD_SETTLE` được điều chỉnh/hủy; Employee chỉ xem. Không hard delete lịch sử | [Post-settlement correction](#post-settlement-correction) |
| Không mở kỳ tháng này | Số kỳ mục tiêu bằng 0 chỉ không tự sinh kỳ mới; kỳ đang mở phải đóng riêng từng kỳ | [Cấu hình bằng 0](#zero-template) |
| Sửa ngày | Kỳ tương lai sửa tự do; kỳ đã mở có order thì OpenAt bất biến, CloseAt chỉ extend hoặc close-early bằng command có lý do | [Edit rules](#period-edit-rules) |
| Bước tiếp theo | Owner duyệt hành vi/UI đã triển khai trước khi làm phần nhà cung cấp | [Implementation record](#implementation-record) |

<a id="plan-revision-2026-08-09"></a>

## Plan revision — phân quyền cấu hình, khóa kỳ và điều chỉnh sau chốt

Status của phần này: **IMPLEMENTED — PENDING OWNER REVIEW**.

### 1. Tách đúng trách nhiệm

| Vai trò | Được làm | Không được làm |
|---|---|---|
| Quản trị hệ thống | Sửa mặc định toàn công ty: số kỳ mở trước, ngày tự mở, ngày đóng mặc định, giờ chạy, múi giờ, hạn duyệt đơn bổ sung và tháng hiệu lực; xem lịch sử phiên bản | Không trực tiếp xử lý đơn hoặc chốt thay Quản lý nếu không được cấp thêm quyền nghiệp vụ |
| Quản lý kỳ | Xem danh mục kỳ, mở đủ horizon, mở kỳ rời rạc, sửa/gia hạn một kỳ cụ thể có lý do, khóa/mở khóa kỳ và đi tới Chốt kỳ | Không sửa policy mặc định toàn công ty |
| Nhân viên | Chọn kỳ đang mở và đặt hàng | Không thấy cấu hình hoặc action quản trị kỳ |

Phương án UI khuyến nghị:

- đổi section sidebar `Phân quyền` thành **Quản trị hệ thống**, giữ các child Người dùng, Nhóm & quyền, Nhật ký bảo mật;
- thêm child **Cấu hình đặt hàng** để quản lý settings có phiên bản;
- bỏ action/panel `Cấu hình mặc định` khỏi trang `Điều hành kỳ → Kỳ đặt hàng`;
- trang Kỳ đặt hàng chỉ hiển thị tóm tắt read-only của cấu hình đang áp dụng. Chỉ Quản trị hệ thống thấy link `Xem cấu hình`;
- backend tách `PERIOD_SETTINGS_MANAGE` khỏi `PERIOD_SETTLE`. Theo RBAC hiện tại, quyền mới chỉ cấp cho `DEV/SystemAdmin`; `MANAGER` giữ `PERIOD_SETTLE` để vận hành kỳ.

Không chuyển toàn bộ ngày của từng period sang Quản trị hệ thống: ngày mặc định là policy hệ thống, còn override lịch của **một kỳ cụ thể** là thao tác vận hành của Quản lý và bắt buộc reason/audit. Nếu gom cả hai cho System Admin, người vận hành sẽ phải nhờ admin cho mọi ngoại lệ lịch và làm sai separation of duties.

### 2. Chuẩn hạn duyệt đơn bổ sung

- `SupplementApprovalGraceDays` mặc định đổi từ 2 thành **5 ngày lịch**.
- Deadline = `Ngày đóng + 5 ngày`; thời điểm vẫn dùng `LocalTimeOfDay` và `TimeZoneId` của settings version.
- Settings mới không sửa ngược kỳ đã tạo. Muốn đổi một kỳ hiện hữu, Quản lý dùng action `Gia hạn kỳ` và nhập lý do.
- UI giải thích đây là thời gian Quản lý xử lý **đơn bổ sung đã gửi trước khi kỳ bị khóa**, không phải thời gian Employee tiếp tục tạo đơn mới.

### 3. Tên trạng thái và action

| Backend state | Nhãn UI | Action chính |
|---|---|---|
| `Draft/Scheduled` | `Sắp mở` | Sửa lịch / Xóa nếu chưa có đơn |
| `Open` | `Đang mở` | `Khóa kỳ` |
| `SubmissionClosed` | `Đã khóa` | `Mở khóa kỳ` có điều kiện; `Chốt kỳ` |
| `Pricing` | `Đang chốt` | `Tiếp tục chốt kỳ` |
| `Settled` | `Đã chốt` | `Chốt kỳ` disabled; dùng `Điều chỉnh sau chốt` để lưu bản kế tiếp |

`Khóa kỳ` là nhãn thao tác ngắn theo motif quản trị; mô tả/tooltip phải nói rõ đây chỉ là ngừng nhận đơn, chưa tạo settlement. `Mở khóa kỳ` yêu cầu lý do, ngày đóng mới, RowVersion và chỉ cho phép khi chưa vào Pricing/chưa Settled.

Cột thao tác có button `Chốt kỳ`:

- enabled cho `SubmissionClosed/Pricing`, điều hướng tới `/dashboard?tab=5&periodTab=review` kèm định danh kỳ;
- không dùng làm action chính cho `Open`; trạng thái này ưu tiên `Khóa kỳ`;
- disabled/muted cho `Settled` với tooltip `Kỳ đã chốt`;
- trang Chốt kỳ phải nhận period từ query/deep link và đồng bộ `PeriodSettlementState`, không tự nhảy sang một kỳ khác.

<a id="post-close-adjustment"></a>

### 4. Chỉnh đơn sau ngày đóng và điều chỉnh sau chốt

- `PostCloseAdjustmentDays` mặc định **10 ngày lịch**, tính từ `SubmissionDeadlineUtc`, không tính từ lúc chốt.
- Trong cửa sổ này, người có quyền `PERIOD_SETTLE` được cập nhật số lượng hoặc hủy đơn hợp lệ; bắt buộc lý do, thông báo cho nhân viên, RowVersion và idempotency. Mỗi thay đổi tạo request revision mới, không sửa đè.
- `SupplementApprovalGraceDays` mặc định 5 và không được lớn hơn `PostCloseAdjustmentDays`. Chốt sớm chỉ được phép khi không còn đơn bổ sung `Pending`. Khi hết hạn, đơn còn chờ không được duyệt thêm nhưng Quản lý vẫn có thể từ chối để kỳ không bị kẹt.
- Sau 10 ngày, dữ liệu đơn khóa; trang Chốt kỳ chỉ cho chọn nhà cung cấp, bảng giá và xác nhận chốt.
- Kỳ đã chốt không quay lại `Pricing`. `Điều chỉnh sau chốt` tạo settlement revision kế tiếp (`Bản 2`, `Bản 3`...), giữ nguyên bản trước và yêu cầu người thực hiện khác người đã chốt bản hiện tại.
- Điều chỉnh/hủy một đơn sau chốt vẫn qua yêu cầu four-eyes; khi được duyệt, hệ thống tạo đồng thời request revision và settlement revision mới.
- Cờ tác động mua sắm bên ngoài tiếp tục là guard dự phòng; khi module PO tồn tại, correction sẽ bị chặn sau khi phát hành cam kết mua sắm.

### 5. Thứ tự triển khai sau khi owner duyệt

1. RBAC/API: thêm permission settings riêng, endpoint system settings và audit/version guard.
2. System Admin UI: thêm `Quản trị hệ thống → Cấu hình đặt hàng`, chuyển panel mặc định sang route mới.
3. Period operations: đổi copy/status, thêm mở khóa kỳ và deep link Chốt kỳ.
4. Settlement revision: bỏ reopen; thêm điều chỉnh bất biến theo phiên bản và cửa sổ chỉnh đơn 10 ngày từ ngày đóng.
5. Migration/backfill an toàn, unit/integration tests, route-real desktop/laptop/mobile và accessibility gate.

### UI correction record — 2026-08-10

- Menu `⋯` không còn dựa vào trạng thái portal ở thời điểm Blazor xử lý click. Module ghi nhận menu đang mở từ `pointerdown`, nên click lần hai đóng thật và không bị Radzen đóng trước rồi Blazor mở lại.
- E2E chờ thêm 350ms sau click lần hai để bắt lỗi “đóng thoáng qua rồi tự mở lại”, tránh pass giả.
- Ngày giờ trong grid và dialog Kỳ đặt hàng dùng canonical `DateFormatter.LongDate`: `HH:mm dd/MM/yyyy`.
- Audit không có reason chỉ hiển thị một dấu `–`; bỏ copy `Chưa có ghi chú` và không render dòng thời gian rỗng.
- Evidence: frontend unit/architecture `423/423`; focused route-real toggle/date `1/1` tại `1366×768`; axe progressive-disclosure `1/1`; visual review action menu tại `390×844` và `1366×768`.

<a id="implementation-record"></a>

## Implementation record — 2026-08-09

Đã triển khai:

- rolling horizon mặc định 3 kỳ, cho phép cấu hình `0–12` và ngày `1–31` có clamp cuối tháng/năm nhuận;
- settings version chỉ dùng khi tạo kỳ mới; kỳ đã tồn tại giữ lịch riêng;
- quản lý được preview/top-up, mở kỳ rời rạc, sửa lịch kỳ tương lai, gia hạn, đóng nhận đơn và xóa kỳ chưa có đơn tại `/dashboard?tab=5&periodTab=periods`;
- màn `Điều hành kỳ → Kỳ đặt hàng` dùng motif admin `Collection/DataGrid`: collection header giữ thao tác cấp danh mục, mỗi dòng có badge và action theo capability; cấu hình mặc định và kỳ rời rạc dùng progressive disclosure thay vì hai form lớn luôn chiếm màn hình;
- kỳ rời rạc ở xa không thay thế các tháng còn thiếu trong chuỗi rolling mặc định; duplicate company/year/month bị chặn;
- nhân viên chọn một kỳ Open ở danh sách đơn hoặc trang tạo đơn; `periodId` đi xuyên qua period-info, copy kỳ trước, draft và create payload;
- kỳ đóng/đã chốt khóa create/update/cancel/restore/recreate của Employee;
- `PERIOD_SETTLE` quản lý yêu cầu Adjust/Cancel sau chốt theo four-eyes;
- khi quản lý thứ hai xác nhận, một transaction tạo request revision và settlement revision mới, giữ revision cũ bất biến, ghi request log và gửi notification cho nhân viên;
- correction sau chốt tái sử dụng **snapshot đơn giá đã chốt** cho đúng tính bất biến và chỉ cho các mặt hàng đã tồn tại; thêm mặt hàng mới phải mở lại kỳ/chốt lại thủ công.

Evidence hiện tại (checkpoint 2026-08-10):

- Release solution build: `0 warning / 0 error`;
- backend full unit: `513/513`;
- frontend unit/architecture: `425/425`;
- disposable LocalDB integration có opt-in: `22/22`, không skip;
- authenticated route-real: cấu hình hệ thống `4/4` viewport; danh mục kỳ, menu, dialog, pager, scroll, axe và deep-link Chốt kỳ `6/6`; chốt → mở lại có audit `1/1`;
- ảnh thật đã review tại `390×844`, `768×1024`, `1366×768`, `1920×1080`; mobile form cuộn được đến nút lưu, không tràn ngang document;
- EF pending-model check: không còn thay đổi model chưa có migration; migration SQL additive, không xóa dữ liệu;
- NuGet vulnerability audit và Gitleaks current-tree: pass, không còn candidate secret.

**Kết luận đơn giản:** settings chỉ nói hệ thống nên duy trì bao nhiêu kỳ mở trước; mặc định là `3`. Nguồn sự thật vẫn là các `VppPeriod` cụ thể. Khi một kỳ đến hạn đóng, hệ thống đóng kỳ đó và mở thêm kỳ tương lai để duy trì horizon, không sửa ngầm lịch sử.

<a id="period-instance"></a>

## 1. Period instance là gì?

Ví dụ ngày 05/08 hệ thống mở horizon 3 kỳ:

```text
08/2026  Open  05/08 → 05/09
09/2026  Open  05/08 → 05/10
10/2026  Open  05/08 → 05/11
```

Ngày 26/08, nhân viên đặt được cho cả 08, 09 và 10. Đến 05/09, kỳ 08 đóng và kỳ 11 được mở; danh sách đang nhận đơn trở thành 09, 10, 11. Nếu cần, quản lý dùng phần Nâng cao để đổi exact OpenAt/CloseAt của kỳ tương lai hoặc mở tháng rời rạc.

Invariants:

- company/year/month unique;
- `OpenAt < CloseAt`;
- một user tối đa một đơn thường hiện hành trong từng period;
- order luôn FK tới period cụ thể;
- create/update/restore/recreate validate trạng thái và deadline của chính period đó;
- period đã có order không bị xóa hoặc diễn giải lại bằng cấu hình mới.

## 2. Trạng thái kỳ

UI chỉ hiển thị bốn nhãn dễ hiểu:

| Nhãn UI | State backend | Ý nghĩa với người dùng |
|---|---|---|
| Sắp mở | Draft/Scheduled | Chưa đặt được |
| Đang nhận đơn | Open | Employee được tạo/sửa/hủy đơn của mình trước hạn |
| Đã đóng nhận đơn | SubmissionClosed/Pricing | Employee chỉ xem; quản lý đang chuẩn bị chốt |
| Đã chốt | Settled | Employee chỉ xem; chỉ Quản lý kỳ được điều chỉnh/hủy có kiểm soát |

State kỹ thuật vẫn giữ chi tiết để backend kiểm soát đúng workflow:

State machine đề xuất:

```text
Draft → Scheduled → Open → SubmissionClosed → Pricing → Settled
```

| State | Ý nghĩa | Employee đặt hàng? | Action quản trị chính |
|---|---|---|---|
| Draft | Đang chuẩn bị, chưa hiển thị | Không | Sửa/xóa |
| Scheduled | Đã lên lịch, chờ OpenAt | Không | Sửa lịch/hủy lịch |
| Open | Đang nhận đơn | Có | Extend deadline / Đóng nhận đơn |
| SubmissionClosed | Đã ngừng nhận đơn | Không | Chuyển Pricing theo workflow hiện hành |
| Pricing | Đang xử lý giá/nguồn cung | Không | Chốt settlement |
| Settled | Đã chốt snapshot | Không | Chỉ điều chỉnh/hủy qua revision có kiểm soát |

Các enum value mới phải append để không đổi numeric value `Open=0`, `SubmissionClosed=1`, `Pricing=2`, `Settled=3` đã tồn tại.

<a id="close-versus-settle"></a>

## 3. Đóng nhận đơn khác Chốt kỳ

Đề xuất không dùng settings `0` để thay cho Chốt kỳ vì đây là hai nghiệp vụ khác nhau:

### Đóng nhận đơn

- Transition `Open → SubmissionClosed`.
- Không cho tạo/sửa/submit/restore/recreate thêm.
- Giữ nguyên mọi order/revision đã có.
- Có thể chạy tự động tại CloseAt hoặc quản trị bấm `Đóng nhận đơn` sớm.
- Đóng sớm bắt buộc confirm + reason + audit.

### Chốt kỳ mua sắm

- Xảy ra sau khi nhận đơn đã đóng và blocker được xử lý.
- Bao gồm pricing, source selection và settlement snapshot.
- Transition cuối `Pricing → Settled`.
- Vẫn nằm trong `Quản lý kỳ/Chốt kỳ` hiện hành.

Do đó nếu kỳ 08 đang Open và đã có đơn, quản trị muốn dừng nhận thêm thì bấm **Đóng nhận đơn kỳ 08**, không sửa cấu hình thành 0 và không tự động settle.

<a id="post-settlement-correction"></a>

### Khi kỳ đã Settled

`Settled` là snapshot đã được dùng cho tổng hợp, báo cáo và quyết định mua sắm. Vì vậy không cho sửa hoặc xóa trực tiếp order gốc:

| Nhu cầu | Không làm | Cách đúng |
|---|---|---|
| Sửa số lượng/mặt hàng/ghi chú | Ghi đè revision đã chốt | Tạo `Điều chỉnh sau chốt` với before/after/delta |
| Hủy toàn bộ đơn | Hard delete hoặc đổi lịch sử thành chưa từng tồn tại | Tạo `Hủy sau chốt` như một reversal toàn phần |
| Cập nhật settlement | Sửa trực tiếp snapshot cũ | Preview lại và tạo settlement correction revision mới |

Workflow khuyến nghị:

1. Người có quyền `PERIOD_SETTLE` mở order đã chốt và chọn `Điều chỉnh sau chốt` hoặc `Hủy sau chốt`.
2. Nhập reason code, lời nhắn bắt buộc cho người đặt và internal note tùy chọn.
3. Hệ thống hiển thị dữ liệu điều chỉnh; MVP giữ snapshot đơn giá đã chốt, cấm thêm mặt hàng mới và kiểm tra MOQ. Trường hợp cần mặt hàng mới phải mở lại kỳ/chốt lại thủ công.
4. Lưu correction ở trạng thái chờ xác nhận; chưa thay đổi order/settlement hiện hành.
5. Một người có quyền `PERIOD_SETTLE` khác người khởi tạo xác nhận lại preview.
6. Trong một transaction, hệ thống tạo request revision mới, settlement correction revision mới, giữ snapshot cũ bất biến và ghi audit log.
7. Người đặt nhận thông báo `Đơn đã được điều chỉnh sau chốt` hoặc `Đơn đã được hủy sau chốt`, kèm lý do công khai, người thực hiện và thời điểm.

Quyền được giữ đơn giản:

- chỉ dùng permission hiện có `PERIOD_SETTLE` cho toàn bộ Quản lý kỳ và điều chỉnh/hủy sau chốt;
- cả người tạo và người xác nhận correction đều phải có `PERIOD_SETTLE`, nhưng phải là hai user khác nhau (four-eyes);
- Employee không nhìn thấy action; API correction chỉ cho `PERIOD_SETTLE`, còn endpoint tự sửa/hủy của Employee vẫn từ chối vì period đã đóng/settled;
- trên UI dùng từ `Hủy sau chốt`, không dùng `Xóa`, vì dữ liệu gốc luôn được giữ lại.

Supplier/PO integration để phase sau. Khi module đó tồn tại, correction mới bổ sung cảnh báo và liên kết xử lý với nhà cung cấp; phase hiện tại không hiển thị giả định PO/hợp đồng chưa được hệ thống quản lý.

<a id="period-template"></a>

## 4. Cấu hình mặc định có phiên bản

Settings do Quản trị hệ thống quản lý, chỉ điều khiển việc tự mở kỳ tương lai và không tự sửa kỳ đã tồn tại:

| Field | Range | Default | Ý nghĩa |
|---|---|---:|---|
| `DefaultOpenPeriodCount` | `0–12` | 3 | Số period hệ thống cố gắng duy trì ở trạng thái Open |
| `DefaultNewPeriodOpenDay` | `1–31` | 5 | Ngày mỗi tháng hệ thống mở thêm period tương lai nếu thiếu horizon |
| `DefaultPeriodCloseDay` | `1–31` | 5 | Ngày đóng mặc định trong tháng kế tiếp sau tháng mục tiêu |
| `SupplementApprovalGraceDays` | `0–31` | 5 | Số ngày lịch để Quản lý xử lý đơn bổ sung đã gửi trước khi khóa kỳ |
| `PostCloseAdjustmentDays` | `0–31` | 10 | Số ngày Quản lý được cập nhật/hủy đơn kể từ ngày đóng; phải lớn hơn hoặc bằng hạn duyệt bổ sung |
| `EffectiveFromMonth` | `YYYYMM` | tháng tương lai gần nhất | Settings mới chỉ áp dụng khi tạo period mới |
| `Name` | text | tự sinh | Tên phiên bản cấu hình |

Nếu tháng thiếu ngày 29–31 thì clamp về ngày cuối tháng, bao gồm năm nhuận.

Ví dụ horizon được khởi tạo ngày 05/08:

- kỳ 08 mở 05/08, đóng 05/09;
- kỳ 09 mở 05/08, đóng 05/10;
- kỳ 10 mở 05/08, đóng 05/11.

Đến 05/09, scheduler khóa kỳ 08 và tạo/mở kỳ 11 với deadline 05/12. Quản trị hệ thống được preview policy tương lai; Quản lý kỳ xem cấu hình read-only và override từng period có lý do.

<a id="zero-template"></a>

## 5. DefaultOpenPeriodCount = 0

`0` nghĩa là **tạm dừng tự mở thêm period tương lai**.

Nó không:

- đóng period đang Open;
- xóa period Draft/Scheduled đã tạo;
- khóa order đã có;
- chuyển period sang Pricing/Settled.

Nếu tháng này doanh nghiệp không muốn nhận đơn:

1. Đặt số kỳ mặc định bằng 0 để scheduler không mở thêm period.
2. Không schedule/open period mới.
3. Nếu đã có period Open, dùng command `Đóng nhận đơn` riêng.

Cách này tránh một settings tổng gây mutation hàng loạt khó đoán.

<a id="create-periods"></a>

## 6. Tạo kỳ liên tiếp hoặc rời rạc

### Luồng mặc định: rolling kỳ liên tiếp

Quản trị hệ thống cấu hình:

1. `Số kỳ mở trước` — mặc định 3.
2. `Ngày mở thêm kỳ mới` — mặc định 05 hàng tháng.
3. `Ngày đóng từng kỳ` — mặc định ngày 05 của tháng kế tiếp sau tháng mục tiêu.

Quản lý kỳ dùng cấu hình đang hiệu lực để preview rồi mở đủ kỳ hiện tại + các kỳ tương lai. Mỗi tháng, scheduler chỉ **bổ sung period còn thiếu** để đạt số kỳ mặc định; không recreate, reopen hoặc thay đổi period cũ.

Đây là luồng mặc định vì dễ hiểu và phù hợp đa số doanh nghiệp.

### Nâng cao: mở thủ công tháng rời rạc hoặc sửa lịch tương lai

Chỉ khi mở `Tùy chỉnh nâng cao`, quản lý mới được:

- chọn period rời rạc, ví dụ:

```text
[x] 05/2027
[ ] 06/2027
[ ] 07/2027
[ ] 08/2027
[x] 09/2027
[ ] 10/2027
[x] 11/2027
```

- đổi OpenAt/CloseAt riêng cho một period;
- bỏ một tháng khỏi preview hoặc mở thêm một tháng cụ thể.

Hệ thống cho phép gap nhưng:

- hiển thị cảnh báo “Lịch có tháng bị bỏ qua”;
- không tự tạo các tháng giữa;
- mỗi period vẫn cần OpenAt/CloseAt hợp lệ;
- không cho duplicate company/year/month.

Như vậy có cả simplicity mặc định và flexibility khi cần.

<a id="period-edit-rules"></a>

## 7. Quy tắc sửa ngày theo trạng thái

| Tình trạng | OpenAt | CloseAt | Cách thực hiện |
|---|---|---|---|
| Draft | Sửa được | Sửa được | Save bình thường |
| Scheduled, chưa mở | Sửa được | Sửa được | Save + rowversion |
| Open, chưa có order | Không sửa OpenAt | Có thể sửa | Confirm + reason nếu rút ngắn |
| Open, đã có order | Bất biến | Chỉ extend trực tiếp | Không có thao tác đóng sớm; scheduler đóng theo lịch đã lưu |
| SubmissionClosed trở đi | Bất biến | Bất biến | Không sửa lịch sử |

### Extend deadline

- Chỉ cho tăng CloseAt.
- Bắt buộc reason/audit nếu period đã có order.
- Không được vượt giới hạn vận hành do owner cấu hình sau này; MVP chỉ validate DateTime range.

### Close early

- Không sửa CloseAt bằng form thường.
- Command ghi CloseAt thực tế = now, chuyển SubmissionClosed và lưu reason/user/time.
- Server recompute capability trước transition để chống stale UI.

## 8. Trang Kỳ đặt hàng

Target route đã triển khai: `/dashboard?tab=5&periodTab=periods`.

Thuộc khu vực Đơn hàng, chỉ user có permission `PERIOD_SETTLE` truy cập. Employee không thấy tab và direct API bị từ chối.

### Layout

Motif `Collection/DataGrid`, giao diện chính gồm:

- collection header `Danh mục kỳ đặt hàng` với tổng số kỳ và số kỳ đang nhận đơn;
- toolbar lọc theo từ khóa kỳ/thay đổi gần nhất, trạng thái và năm; filter áp trên snapshot trước paging;
- DataGrid hiển thị kỳ, trạng thái, lịch mở/đóng, hạn duyệt bổ sung, số đơn và audit gần nhất;
- row actions `Xem / Sửa lịch / Gia hạn / Khóa kỳ / Mở khóa kỳ / Chốt kỳ / Xóa` bật hoặc khóa theo capability backend; mọi dòng luôn có `Xem chi tiết` trong menu `...`;
- `Khóa kỳ` chuyển Open sang `Đã khóa`; `Mở khóa kỳ` chỉ có trước Pricing và bắt buộc lịch mới + lý do;
- kỳ đã chốt vẫn hiển thị action `Chốt kỳ` ở trạng thái disabled để giữ cột thao tác nhất quán, thay vì đổi nhãn thành `Đã chốt`;
- `Mở kỳ` dùng progressive disclosure; cấu hình mặc định được chuyển sang `Quản trị hệ thống → Cấu hình đặt hàng`.

### Collection actions

- `Mở kỳ`;
- `Mở đủ số kỳ`;
- tóm tắt read-only của cấu hình đang hiệu lực; link `Xem cấu hình` chỉ hiện với Quản trị hệ thống;
- tháng rời rạc và lịch riêng nằm trong panel phụ chỉ mở khi quản lý yêu cầu.

### Period detail actions

- Draft/Scheduled: Sửa lịch, Xóa kỳ nếu chưa có đơn.
- Open chưa có order: Sửa lịch với OpenAt bất biến, Extend deadline, Khóa kỳ.
- Open đã có order: Extend deadline hoặc Khóa kỳ bằng command có lý do.
- SubmissionClosed: xem facts, Mở khóa kỳ có guard hoặc đi tới Chốt kỳ.
- Pricing: read-only lịch nhận đơn và Tiếp tục chốt kỳ.
- Settled: read-only facts; action tạo `Điều chỉnh/Hủy sau chốt` nằm trong chi tiết đơn của màn Chốt kỳ, không hiện edit/delete trực tiếp trong danh mục kỳ.
- Hàng chờ quản lý thứ hai xác nhận chỉ xuất hiện trong màn Chốt kỳ khi kỳ đang xem có request `Pending`; không render card empty trong trang Kỳ đặt hàng.

## 9. Employee multi-period UX

<a id="employee-multi-period"></a>

My Orders `/dashboard?tab=0` có selector kỳ từ danh sách state `Open`:

```text
Kỳ đặt hàng: [08/2026 · đóng 05/09]
               09/2026 · đặt trước · đóng 05/10
               10/2026 · đặt trước · đóng 05/11
```

- Không yêu cầu các period liên tiếp.
- Default selection ưu tiên anchor/current period nếu đang Open; nếu không thì OpenAt gần nhất.
- Khi đổi selector, order thường/bổ sung, CTA, deadline và capability đổi theo period.
- Query typed: `/dashboard?tab=0&orderPeriod=202609`.
- Create Order luôn hiển thị target period và backend validate period state `Open`.
- Kỳ đã đóng xem qua History, không lẫn vào selector đặt hàng.
- Order đã điều chỉnh/hủy sau chốt hiển thị badge, revision hiện hành, lý do công khai, người thực hiện và thời điểm; người dùng vẫn xem được lịch sử trước đó nhưng không sửa.

## 10. Bảng xử lý đầy đủ các trường hợp

| Trường hợp | Hệ thống xử lý |
|---|---|
| Tháng 08, mặc định 3 kỳ | Mở 08, 09, 10 từ 05/08; deadline lần lượt 05/09, 05/10, 05/11 |
| Sang ngày 05/09 | Đóng kỳ 08, giữ 09/10 và mở thêm kỳ 11; vẫn có 3 kỳ Open |
| Cấu hình 1 kỳ | Chỉ mở tháng hiện tại |
| Cấu hình 2 kỳ | Mở tháng hiện tại và tháng kế tiếp |
| Cấu hình 0 trước lần chạy ngày 05 | Scheduler không mở period mới |
| Đã có kỳ Open rồi mới đổi settings thành 0 | Kỳ đang Open không đổi; muốn dừng phải bấm `Đóng nhận đơn` |
| Đổi horizon từ 1 lên 3 | Settings áp dụng từ lần chạy đã chọn; nếu cần ngay, quản lý dùng `Mở thêm kỳ` và xem preview |
| Đổi horizon từ 3 xuống 1 | Không tự đóng hai kỳ đang Open; scheduler chỉ ngừng bổ sung cho đến khi số kỳ giảm còn dưới 1 |
| Đổi ngày mặc định 05 thành 10 | Chỉ áp dụng từ tháng hiệu lực được chọn; kỳ đã tạo không đổi |
| Chọn ngày 31 cho tháng 2 | Dùng 28 hoặc 29 theo năm nhuận; tháng 4/6/9/11 dùng ngày 30 |
| Muốn mở 08, 10, 12 | Dùng Nâng cao, chọn rời rạc; hệ thống không tự tạo 09 và 11 |
| Horizon có tháng rời rạc | Scheduler không lấp gap đã chọn thủ công; khi cần bổ sung sẽ nối sau period xa nhất |
| Muốn kỳ 10 đóng sớm/muộn hơn | Override riêng CloseAt của kỳ 10 khi còn Sắp mở; nếu đã Open thì áp dụng guard sửa deadline |
| Period Sắp mở | Quản lý kỳ được sửa lịch hoặc bỏ period nếu chưa có order |
| Period đang Open, chưa có order | Quản lý kỳ được đổi CloseAt; rút ngắn cần xác nhận/lý do |
| Period đang Open, đã có order | Không đổi OpenAt; được gia hạn CloseAt; hệ thống tự đóng theo lịch đã lưu |
| Employee trong thời gian Open | Được tạo/sửa/hủy đơn của chính mình theo permission hiện hành |
| Employee sau deadline/đã đóng | Chỉ xem; create/update/cancel bị backend từ chối |
| Đến ngày đóng của kỳ | Scheduler giữ toàn bộ đơn, ghi chuyển trạng thái và khóa mutation mới |
| Số kỳ Open thấp hơn cấu hình mục tiêu | Scheduler tự bổ sung ở lần chạy kế tiếp theo phiên bản cấu hình hiệu lực |
| Kỳ đã đóng nhưng chưa chốt | Chỉ xử lý pricing/chốt; không cho Employee sửa đơn |
| Employee thử sửa/hủy kỳ Settled | Không có nút; direct API trả `403` |
| Quản lý kỳ sửa đơn Settled | Tạo `Điều chỉnh sau chốt`, nhập lời nhắn, preview delta, quản lý kỳ thứ hai xác nhận, rồi tạo request + settlement revision mới |
| Quản lý kỳ muốn “xóa” đơn Settled | Thực hiện `Hủy sau chốt`; tạo reversal revision, không hard delete |
| Correction chưa được người thứ hai xác nhận | Order và settlement current chưa thay đổi |
| PO/hợp đồng đã gửi NCC | Ngoài scope phase hiện tại; chỉ bổ sung rule khi có module PO/NCC thật |
| Hai quản lý thao tác cùng lúc | RowVersion/input hash chặn dữ liệu stale; người sau phải preview lại |

## 11. Backend/API

### Read API

Mở rộng `GET /api/VPPRequest/period-info`:

- compatibility fields current/previous giữ trong cutover;
- `OpenPeriods[]`: id, year/month, OpenAt, CloseAt, isAnchor, order capability;
- `DefaultSelectedPeriodId`;
- nếu rỗng, UI hiển thị “Hiện không có kỳ nhận đơn”.

### Manage API

- `GET /api/order-periods`
- `GET/POST /api/order-periods/settings`
- `POST /api/order-periods/horizon-preview`
- `POST /api/order-periods/top-up`
- `POST /api/order-periods` — tạo kỳ rời rạc
- `PUT /api/order-periods/{id}` — sửa lịch theo capability + rowversion
- `POST /api/order-periods/{id}/extend-deadline`
- `POST /api/order-periods/{id}/close-submissions`
- `POST /api/order-periods/{id}/delete` — Draft/Scheduled, no order only

Toàn bộ mutation quản lý period và correction sau chốt yêu cầu `PERIOD_SETTLE`, company claim, rowversion và reason khi operation có tác động. `REQUEST_UPDATE_OWN`/`REQUEST_CANCEL_OWN` của Employee không có hiệu lực sau khi period đóng hoặc Settled.

### Post-settlement correction API

- `POST /api/VPPRequest/orders/{orderId}/post-settlement-corrections/preview`
- `POST /api/VPPRequest/orders/{orderId}/post-settlement-corrections`
- `POST /api/VPPRequest/post-settlement-corrections/{correctionId}/confirm`
- `POST /api/VPPRequest/post-settlement-corrections/{correctionId}/reject`

Preview trả before/after/delta, current request revision, current settlement revision và input hash. Confirm phải kiểm tra lại rowversion/hash/current revision để chống stale approval; command idempotent để retry không sinh hai revision.

### Request validation

Thay rule requested period phải bằng một current period bằng:

1. Load period theo company/year/month.
2. Period phải tồn tại và state `Open`.
3. `now < SubmissionDeadlineUtc`.
4. Validate unique order/user/period và lifecycle như hiện tại.

Không còn cần tính rolling horizon tại lúc tạo đơn; danh sách period instance là authority.

## 12. Domain/database

### VppPeriod

Giữ entity hiện có làm aggregate canonical:

- company/year/month unique;
- StartAtUtc/SubmissionDeadlineUtc/SupplementApprovalDeadlineUtc;
- state và transition audit;
- thêm Draft/Scheduled state;
- optional SettingsVersionId để biết kỳ được sinh từ cấu hình nào.

### VppOrderPeriodSettingsVersion

- company/version/name;
- DefaultOpenPeriodCount;
- DefaultNewPeriodOpenDay;
- DefaultPeriodCloseDay;
- EffectiveFromMonth;
- effective month + audit + rowversion; phiên bản mới không mutate period đã tạo.

### PostSettlementOrderCorrection

Persist correction intent trước khi mutation để hỗ trợ four-eyes:

- company/period/order/current request revision/current settlement revision;
- type `Adjust` hoặc `Reverse`;
- proposed order payload hoặc normalized delta + input hash;
- reason code, user-visible note, optional internal note;
- status `Pending`, `Confirmed`, `Rejected`, `Superseded`;
- requested/confirmed/rejected by + timestamps + rowversion.

Khi confirm, transaction mới tạo request revision và settlement correction revision. Cancel sau chốt là revision đảo toàn bộ nhu cầu, không hard delete dữ liệu gốc.

### Application ownership

`IVppPeriodService`/`VppPeriodService` chuyển về Requests/Ordering ownership vì service này tạo, mở và đóng cửa sổ nhận đơn. `PeriodSettlementService` chỉ gọi interface để chuyển `SubmissionClosed → Pricing → Settled`; không tạo một period service thứ hai.

Không cần:

- BusinessCalendarDate;
- ExplicitCalendar;
- Nth/LastBusinessDay;
- monthly OpenPeriodCount override;
- một table policy khác để tính runtime horizon.

### Migration

- additive settings-version table + nullable SettingsVersionId;
- update state check constraint để chấp nhận Draft/Scheduled;
- không backfill hoặc đổi timestamp period/request cũ;
- legacy rows giữ state/timestamp hiện hành;
- migration thử nghiệm `AddVersionedPeriodPolicies` chưa commit/apply production phải được thay bằng migration canonical, không giữ schema song song.

## 13. Xử lý implementation thử nghiệm hiện có

| Phân loại | Phần hiện có | Hành động sau approval |
|---|---|---|
| KEEP | `VppPeriod`, timestamps, transition audit, concurrent ensure | Nâng thành explicit period management |
| KEEP/ADAPT | Day 1–31, month-end clamp, timezone | Dùng cho rolling settings generator |
| REDESIGN | `period-info`, My Orders, Order Create | Trả/chọn danh sách period Open |
| DELETE | `VppPeriodPolicyVersion`, BusinessCalendarDate, ExplicitCalendar và policy preview phức tạp | Sai scope sau owner clarification |
| DELETE | `periodTab=policy`, route/nav cũ dưới Điều hành kỳ | Không giữ alias chưa approved |
| REPLACE | `PeriodPolicyWorkspace` | `OrderPeriodManagementWorkspace` tại `tab=5&periodTab=periods` |
| KEEP PERMISSION | `PERIOD_SETTLE` | Reuse cho quản lý period và correction sau chốt; không tạo permission mới |
| KEEP UNCHANGED | Pricing/supplier/settlement snapshot | Ngoài scope |

Không reset/delete cả folder trên dirty worktree; thực thi theo file/hunk ledger.

## 14. Implementation waves sau approval

Quota snapshot gần nhất `2026-08-08T13:54:09Z`: 245% weekly account-equivalent, five-hour incomplete. Scope mới lớn hơn count-only plan: forecast 70–140%, confidence medium-low; buffer 50% → upper bound 210%, `ENOUGH`. Routing khuyến nghị: `gpt-5.6-sol high` architecture/state-machine/review, `gpt-5.6-terra high` implementation rõ/lặp lại.

| Wave | Nội dung | Gate | Status |
|---|---|---|---|
| W0 | Ledger current experimental change-set và baseline | Không mất unrelated changes | COMPLETED |
| W1 | State machine, settings version, period CRUD/commands, permission và contracts | Domain/auth/concurrency tests | COMPLETED |
| W2 | Migration + period service + multi-period request validation + post-settlement correction transaction | SQL fresh/upgrade, pending-model, immutable revision/four-eyes tests | COMPLETED |
| W3 | Admin Collection/DataGrid management + employee selector + Create target period + correction/reversal UI/notifications | FE tests, progressive disclosure, row-action capability, notification deep-link tests | COMPLETED |
| W4 | Route-real QA và regression settlement/correction | 4 viewport, scroll/navigation regression, axe | AUTOMATED_QA_PASS — OWNER_REVIEW |

## 15. Verification/acceptance

### Domain/API

- Draft/Scheduled/Open/SubmissionClosed/Pricing/Settled legal transitions.
- Default generator count 0,1,3,12 và year rollover.
- Rolling transition 08/09/10 → 09/10/11 không duplicate/reopen period cũ.
- Tăng/giảm horizon chỉ ảnh hưởng lần top-up tương lai; không tự đóng period hiện hữu.
- Manual sparse periods 05/09/11.
- Duplicate target period blocked.
- OpenAt < CloseAt; day29–31 leap clamp.
- Scheduled auto-open; Open auto-close.
- Open with order: OpenAt immutable, extend deadline pass, shorten via close command only.
- Close early blocks all request mutations but preserves order/revisions.
- User creates only in state Open and before deadline.
- Existing settlement transition/tests unchanged.
- Chốt sớm từ `SubmissionClosed` pass khi không còn đơn bổ sung chờ duyệt; nếu còn `Pending` thì bị chặn.
- Hết hạn duyệt bổ sung: approve bị chặn, reject vẫn được phép trước khi chốt để không tạo kỳ bị kẹt vĩnh viễn.
- Trong 10 ngày từ ngày đóng, Quản lý điều chỉnh/hủy đơn tạo revision mới; sau hạn bị chặn nhưng vẫn chốt được.
- Settled order direct edit/delete/cancel API bị từ chối.
- Correction Pending không thay đổi request hoặc settlement hiện hành.
- Initiator tự confirm bị từ chối; confirmer khác user tạo đúng một request revision và một settlement correction revision.
- Edit tạo before/after/delta đúng; cancel tạo reversal toàn phần, không hard delete.
- Stale request/settlement revision hoặc input hash bị từ chối và bắt preview lại.
- Revision cũ bất biến; chỉ revision mới là current; report/export dùng current revision nhưng history xem được toàn bộ.
- Không có endpoint/UI đưa kỳ `Settled` quay lại `Pricing`; điều chỉnh toàn kỳ tạo version kế tiếp ngay trong trạng thái `Settled`.
- Outbox notification idempotent; người đặt nhận đúng user-visible note sau khi correction được confirm.

### UI matrix

| State | 390×844 | 768×1024 | 1366×768 | 1920×1080 |
|---|---|---|---|---|
| 3 kỳ liên tiếp Open | Required | Required | Required | Required |
| Kỳ rời rạc 05/09/11 | Required | Required | Required | Required |
| Không có kỳ Open | Required | Required | Required | Required |
| Admin current-period default detail | Required | Required | Required | Required |
| Close early confirm/reason | Required | Required | Required | Required |
| Settled correction preview/confirm/history | Required | Required | Required | Required |
| Settled reversal + user notification | Required | Required | Required | Required |
| Employee direct admin denied | Required | Required | Required | Required |

Ngoài ra: VI/EN, Light/Dark, keyboard/focus, loading/error/empty/disabled/success, no page overflow và no console/page/request failure.

### Definition of done

- Employee chọn được mọi period Open, kể cả rời rạc.
- Mặc định rolling horizon giữ 3 period Open; từng period có deadline ngày 05 của tháng kế tiếp sau tháng mục tiêu.
- Mỗi period có exact OpenAt/CloseAt và state riêng.
- Current Open period được đóng bằng command, không bằng settings 0 và không auto-settle.
- Future period dates editable theo guard; period có order không bị sửa ngầm.
- Order thuộc kỳ Settled không bị sửa/xóa trực tiếp; edit/cancel đi qua correction/reversal revision, mandatory note, notification và four-eyes.
- Không còn màn policy/calendar thử nghiệm; route canonical là `/dashboard?tab=5&periodTab=periods`.
- Supplier optimizer và PO/NCC vẫn deferred; phase hiện tại không hỏi người dùng xác nhận dữ liệu mua sắm chưa tồn tại trong hệ thống.
- Build/unit/SQL/browser gates xanh và owner review runtime.

<a id="period-plan-approval"></a>

## 16. Decision ledger cần owner duyệt

| ID | Khuyến nghị | Default |
|---|---|---|
| D1 | UI tập trung danh sách kỳ đang nhận đơn; backend lưu period instances riêng và scheduler chỉ top-up horizon | Có |
| D2 | Rolling horizon mặc định giữ tháng hiện tại + 2 tháng tiếp theo ở trạng thái Open | 3 kỳ |
| D3 | Cho phép tạo period rời rạc thủ công | Có, advanced |
| D4 | Period mới mở ngày 05; mỗi period đóng mặc định ngày 05 của tháng kế tiếp sau tháng mục tiêu | Có thể override từng kỳ |
| D5 | Current Open period dừng bằng `Đóng nhận đơn` | Open → SubmissionClosed |
| D6 | Đóng nhận đơn không đồng nghĩa Chốt kỳ | Tách rõ |
| D7 | Horizon 0 chỉ dừng top-up period mới | Không mutation kỳ hiện hữu |
| D8 | Kỳ Open có order: OpenAt bất biến, CloseAt chỉ extend hoặc close early command | Có |
| D9 | Admin page tại `tab=5&periodTab=periods`; mọi mutation dùng permission hiện có `PERIOD_SETTLE` | Employee bị cấm |
| D10 | Order thuộc kỳ Settled không sửa/xóa trực tiếp; edit/cancel là correction/reversal revision | Có |
| D11 | Chỉ user có `PERIOD_SETTLE` được correction/reversal sau chốt; bắt buộc reason + user-visible note + notification + four-eyes | Có |
| D12 | Cho chỉnh đơn 10 ngày từ ngày đóng; chốt sớm được nếu không còn bổ sung chờ duyệt; kỳ đã chốt lưu bản kế tiếp thay vì mở lại | Có |

## 17. Continuation

- Current status: `IMPLEMENTED_AWAITING_OWNER_REVIEW`.
- Current code status: W0–W3 hoàn tất; W4 đã qua automated QA, còn owner review runtime trước khi khóa visual baseline/commit feature.
- Verification: backend/frontend/LocalDB/EF/browser/axe đã ghi tại Implementation record; supplier optimizer/integration vẫn không thuộc change-set này.
- Next exact action: owner duyệt hai màn `/dashboard?tab=5&periodTab=periods` và `/dashboard?tab=5&periodTab=settle`; nếu đạt thì khóa trạng thái approved và tách commit đúng scope, sau đó mới lập plan NCC/chi phí vận chuyển.
