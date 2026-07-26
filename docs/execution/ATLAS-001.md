# ATLAS-001 — Triển khai toàn bộ Design Atlas sang frontend Blazor

- Status: PLANNED — chờ owner duyệt trước khi thực thi
- Priority: P1
- Branch: `codex/atlas-blazor-wave1` (tiếp tục, không merge/deploy)
- Lập kế hoạch: 2026-07-26 (Asia/Ho_Chi_Minh)
- Nguồn nghiệp vụ: `LVTN/checkpoints/NguyenAnNam_DH52201078_final_v4_standard.docx`
- Nguồn bố cục: Design Atlas 28 màn hình
- Liên quan: `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md` (living plan, route ledger)

---

## 1. Thứ tự thẩm quyền

Khi hai nguồn mâu thuẫn, áp dụng đúng thứ tự này và ghi lại vào decision log:

1. **Luận văn Word `final_v4_standard`** — quyết định nghiệp vụ, quyền, trạng thái, ràng buộc dữ liệu.
   Không sửa API/schema/nghiệp vụ để làm đẹp giao diện. Muốn đổi nghiệp vụ phải hỏi owner trước.
2. **Design Atlas (28 màn)** — quyết định bố cục, hệ thống phân cấp thị giác, cách điều hướng dữ liệu,
   copy tiếng Việt. Atlas mới hơn frontend, nên **Blazor phải kéo lên theo Atlas**, không phải ngược lại.
3. **Backend/API/schema hiện hành** — quyết định dữ liệu thật sự có gì. Atlas không được phép mô tả
   trường, hành động hoặc endpoint không tồn tại.
4. **Frontend Blazor hiện tại** — chỉ là điểm xuất phát. Giữ lại khi đã chính xác hơn Atlas về nghiệp
   vụ, còn lại thay theo Atlas.

Blazor chạy thật trong browser vẫn là nơi nghiệm thu visual cuối cùng.

---

## 2. Quyết định owner đã chốt — 2026-07-26

| # | Quyết định | Hệ quả |
|---|---|---|
| D1 | **Giữ 3 vai trò** `EMPLOYEE / MANAGER / DEV` | Khớp luận văn §3.3.4.4 và `CanonicalRbac.cs`. Quyết định `2026-07-24 Four-role permission model` trong living plan chuyển sang **Superseded**. Không đụng backend authorization |
| D2 | **Gộp `Tổng hợp toàn công ty` vào luồng Vận hành kỳ** | Bỏ tab riêng `Tab_AllOrdersSummary`; nội dung trở thành bước `Gom nhu cầu` |
| D3 | **Báo cáo chỉ CSV + XLSX** | Không viết endpoint PDF. Sửa luận văn §3.3.5.1 từ "xuất PDF hoặc Excel" thành "xuất CSV hoặc Excel" cho khớp §3.4 và `ReportsController`. **Atlas cũng phải bỏ nút `Xuất PDF`** ở màn Báo cáo |
| D4 | **Tách `Gom nhu cầu` và `Chọn nguồn cung` thành 2 bước riêng** | Luồng kỳ thành 4 bước: `Rà soát kỳ → Gom nhu cầu → Chọn nguồn cung → Chốt kỳ` |
| D5 | **Đưa Atlas source vào repository, read-only** | Copy vào `docs/design/atlas/`. Đây là **design reference đóng băng**, không phải UI Lab để phát triển tiếp — ghi rõ trong `AGENTS.md` |
| D6 | **Code tiếng Anh + sổ tay đọc code tiếng Việt + comment tiếng Việt tại điểm nghiệp vụ** | Xem mục 7 |

---

## 3. Bản đồ 28 màn Atlas → frontend → luận văn

Cột `Gap` là khối lượng việc thật, không phải trạng thái duyệt.

| # | Atlas | Board | Route/tab Blazor | Hình luận văn | Gap |
|---|---|---|---|---|---|
| 1 | `shell-system` | M0 | `MainLayout` + `LeftSidebar` + `UserMenu`/`NotificationCenter` (HeaderControls đã xóa — mồ côi, trùng UserMenu) | — | B |
| 2 | `login` | M1 | `/Account/Login` | 3-28 | A |
| 3 | `forgot-password` | M1 | `/Account/ForgotPassword` | — | A |
| 4 | `reset-password` | M1 | `/Account/ResetPassword` | — | A |
| 5 | `change-password` | M1 | `/Account/ChangePassword` | — | A |
| 6 | `logout` | M1 | `/logoutprocess` | — | A |
| 7 | `register` | M1 | `/Account/Register` | — | A |
| 8 | `my-orders` | M2 | `/dashboard` → `Tab_Orders` | 3-29 | B |
| 9 | `order-create` | M2 | `/dashboard/order-create` | 3-30 | B |
| 10 | `history` | M2 | `Tab_History` (577 dòng) | 3-31 | C |
| 11 | `catalog` | M2 | `Tab_ProductCatalog` | 3-32 | A |
| 12 | `department-summary` | M3 | `Tab_DepartmentSummary` | 3-33 | B |
| 13 | `supplement-approval` | M4 | `Tab_AdminApproval` (357 dòng) | 3-34 | C |
| 14 | `period-review` | M4 | `PeriodReviewPanel` | 3-35 | B |
| 15 | `period-demand` | M4 | **chưa có** | — | **D** |
| 16 | `supply-allocation` | M4 | **chưa có** (đang lẫn trong settlement) | 3-36 | **D** |
| 17 | `settlement-flow` | M4 | `PeriodSettlementPanel` | 3-37 | C |
| 18 | `classes` | M5A | `/library` → `ClassDefinitions` | — | B |
| 19 | `categories` | M5A | `/library` → `OperationCategories` | — | B |
| 20 | `items` | M5A | `/library` → `Operations` | 3-38 | B |
| 21 | `departments` | M5A | `/library` → `Departments` | — | B |
| 22 | `suppliers` | M5B | `/library` → `Suppliers` | — | B |
| 23 | `price-lists` | M5B | `Tab_PriceListLibrary` | 3-39 | B |
| 24 | `prices` | M5B | `Tab_PriceLibrary` | — | B |
| 25 | `users` | M6 | `/permission` → `Tab_User` | 3-40 | B |
| 26 | `permissions` | M6 | `/permission` → `Tab_PagePermission` | 3-41 | B |
| 27 | `reports` | M7 | `/report` | 3-42 | C |
| 28 | `system-states` | M8 | `VppStatePanel`, `VppEmptyState`, `NotificationCenter`, `ReconnectModal` | 3-43 | B |

**Phân loại gap:**

- **A — Regression** (7 màn): đã khớp Atlas, chỉ chạy lại kiểm thử để xác nhận không hồi quy.
- **B — Căn chỉnh** (14 màn): giữ nguyên logic và API, sửa bố cục/toolbar/state/copy theo Atlas.
- **C — Tái cấu trúc** (5 màn): component quá lớn hoặc cấu trúc thông tin lệch Atlas, phải chẻ nhỏ
  rồi dựng lại; không đổi nghiệp vụ.
- **D — Màn mới** (2 màn): `period-demand` và `supply-allocation`.

Điểm đáng chú ý: `supply-allocation` **đã là Hình 3-36 trong luận văn** nhưng chưa có màn thật. Đây là
lý do mạnh nhất để ưu tiên M4.

---

## 4. Backend: chỉ một thay đổi

Tôi đã đối chiếu từng màn với controller/DTO hiện có.

**Không cần đụng backend** cho 27/28 màn. Cụ thể `supply-allocation` dựng được nguyên vẹn trên API sẵn có:

- `POST /api/PeriodSettlement/preview` nhận `PrimarySupplierId`, `PriceListId` và
  `Exceptions[] { VppId, SupplierId, Reason }`;
- trả về `PrimaryQuote`, `Quotes[]` (xếp hạng nhà cung cấp), `Blockers[]` và `Exceptions[]` kèm
  `NetUnitPrice / VatRate / GrossAmount / Blocker`.

Đúng bằng những gì Atlas vẽ: chọn nhà cung cấp trước → bảng giá còn hiệu lực → độ phủ → ngoại lệ có lý do.

**Cần bổ sung đúng một endpoint đọc** cho `period-demand`:

- DTO `AggregatedVppResDTO` / `AggregatedVppItemResDTO` **đã tồn tại** trong
  `gtas_vpp_be/gtas_vpp_shared/DTOs/Res/VPP/` nhưng **không controller hay service nào trả về** — là
  DTO mồ côi.
- Đề xuất: `GET /api/VPPRequest/period-demand?year=&month=` trả `AggregatedVppResDTO`, gom theo mặt
  hàng từ phiên bản đơn hợp lệ hiện hành của kỳ, phạm vi theo quyền phiên đăng nhập.
- Đây **không phải nghiệp vụ mới**: luận văn §3.3.3.4 đã viết "Sau khi gom nhu cầu, quản lý chọn nhà
  cung cấp chính", và §2.3.1.8 mô tả rà soát độ phủ giá. Endpoint chỉ hiện thực hóa bước đã được mô tả.
- Chỉ đọc, không mutation, dùng lại đúng policy `Permissions.PeriodSettle`.

Nếu owner không muốn thêm endpoint, phương án dự phòng là dựng `period-demand` chỉ từ `all-orders`
và bỏ bảng "Tổng nhu cầu tạm tính" — nhưng khi đó màn mất đúng phần giá trị nhất.

---

## 5. Kế hoạch theo wave

Mỗi wave là một lát cắt dọc chạy được thật, không phải một đợt sửa CSS.

### W-A — Nền móng và gỡ chốt chặn

Không có wave nào nghiệm thu được nếu chưa mở được browser QA.

1. Sửa lỗi chặn QA: `Service frontend-http should have valid address at this point` (Aspire Testing).
   Đây là chốt chặn số một, lượt QA 26/07 chết ngay trước khi mở route.
2. Copy Atlas vào `docs/design/atlas/` (`manifest.json`, `atlas.css`, `atlas.js`, `render-atlas.cjs`,
   `serve-atlas.cjs`, `index.html`, `README.md`). Ghi vào `AGENTS.md`: read-only design reference,
   không phát triển tiếp trong repo, không phải UI Lab.
3. Sửa Atlas cho khớp quyết định owner: bỏ nút `Xuất PDF` ở màn `reports` (D3); đảm bảo `period-demand`
   và `supply-allocation` phản ánh đúng field có thật trong DTO.
4. Dựng khung `docs/CODE-READING-GUIDE.md` (mục 7).
5. Cập nhật living plan: đánh dấu quyết định 4 vai trò là `Superseded`, ghi D1–D6 vào decision log.

**Nghiệm thu:** `dotnet build -c Release` 0 warning; backend + frontend test giữ nguyên số pass; mở
được ít nhất một route đã đăng nhập bằng Playwright trên fixture TEST.

### W-B — M0 shell + M1 tài khoản (7 màn, gap A/B)

Đối chiếu shell/sidebar/header/user-popup với Atlas `shell-system`; 6 màn tài khoản chỉ chạy regression
trên 3 viewport. Đây là wave rẻ nhất, dùng để kiểm chứng bộ gate mới hoạt động.

**Nghiệm thu 2026-07-26 (tối):** W-B.2 + W-B.2b + W-C C-1…C-6 đã qua full E2E trên harness mới
(27-28/28 qua các lượt; các fail cuối là flake đo lường đã gia cố, không phải lỗi chức năng).
Header desktop 72px với tab strip + breadcrumb + role badge ĐÃ BẬT (D14 hoàn thành cùng ngày,
kèm fix `FullHeight="true"` cho grid Radzen — xem REFACTOR-001 R-D4). Đơn vị đo tốc độ suite
mới: ~6-7 phút (REFACTOR-001 R-D2).

**Tiến độ 2026-07-26:** W-B.1 xong (workflow 6 agent: 23 MUST_FIX xác nhận, 13 KEEP, dẫn chứng
file:dòng đã kiểm chứng). W-B.2 đã áp: đồng bộ copy VI theo Atlas (11 value resx + 9 key mới),
bậc nút 36/30px + màu semantic light sâu + link theo theme (token), chuẩn hóa trạng thái
lỗi/thiếu quyền (`VppStatePanel` state `denied`, retry primary, lối thoát `Về trang phù hợp`,
ErrorBoundary có nút khôi phục), popup tài khoản + hộp thư thông báo khớp Atlas (tiêu đề, badge
`Mới`, tên ngôn ngữ đầy đủ, dòng phụ footer sidebar), sidebar mặc định mở rộng có ghi nhớ (D12),
icon nav theo Atlas (D13), xóa `HeaderControls.razor` mồ côi. Header desktop hoãn theo D14 —
còn **W-B.2b**: chuyển tab strip vào primary header rồi bật desktop.

**Retrofit queue (sửa ngược Atlas, chờ owner duyệt):** (1) Atlas thiếu màn/mục nav `Toàn bộ đơn`
(`RequestAllOrdersSummary`) — bổ sung vào Atlas thay vì cắt nav Blazor; (2) màn `account-change`
không có entry point tự nguyện trong popup tài khoản — nếu duyệt, thêm hàng `Đổi mật khẩu` đồng
thời ở `atlas.js` và `UserMenu.razor`.

### W-C — M2 vòng đời đơn của nhân viên (4 màn, gap A/B/C)

- `catalog`: đã căn theo Atlas ở lượt trước, chỉ regression.
- `my-orders`, `order-create`: căn theo Atlas (header hai bước căn giữa, footer
  `Quay lại | Lưu nháp → Ghi chú → Tiếp tục`, popover ghi chú, virtualization vùng chọn mặt hàng).
- `history`: chẻ `Tab_History` (577 dòng) thành các phần theo đúng bước nghiệp vụ.

**Tiến độ 2026-07-26:** W-C.1 phân tích xong (workflow 6 agent: 35 MUST_FIX kiểm chứng, 15 KEEP,
13 ALLOWED_DIFF; phát hiện 4 key resx được gọi mà chưa định nghĩa — bug hiển thị thật do regex
của LocalizationResourceTests không bắt dạng `Loc["Key", x]`). Đã áp C-1→C-6: resx nền tảng
(16 value + 24 key mới), my-orders (cột Danh mục, toolbar lọc + footer đếm theo orderItemsRegion,
action bar quiet×4+danger, hero "Tạo đơn kỳ này"), popover ghi chú order-create (hint phạm vi +
footer Hủy/Lưu ghi chú), catalog bỏ cột "#", history (drawer "Phiếu chi tiết đơn" + Người đặt/
Phòng ban + nút xuất PDF/Excel + lọc đơn vị + KPI nhãn riêng + badge trạng thái qua
VppStatusContract), account M1 (description theo Atlas, alert chống dò tài khoản, "Tạo tài khoản").
Còn lại: **C-7** chẻ Tab_History (đợt riêng); **[OWNER-GATE]** nhãn "Email công ty" vs "Email"
(Forgot/Register) — giữ nhãn cũ chờ owner chốt.

**Retrofit queue W-C (sửa ngược Atlas, chờ owner duyệt):** hero my-orders biến thể chưa có đơn
("Tạo đơn kỳ này" + "Sao chép kỳ trước"); 3 thẻ tóm tắt là radiogroup điều hướng; empty state
my-orders; rút gọn mã đơn dài + copy; lý do đơn bổ sung ≥5 ký tự; màn review OrderCreateStep3
chưa có trong manifest; giỏ rỗng order-create; fixture history badge "Đã chốt" sai hợp đồng
trạng thái; Dialog_RequestHistory chưa được Atlas thiết kế; mâu thuẫn legend chart vs badge loại
đơn; drawer vẽ tên phòng ban nhưng DTO chỉ có mã; hint chính sách mật khẩu thiếu "ký tự đặc biệt";
alert register sai (backend có luồng đăng ký chờ duyệt thật); biến thể change-password bắt buộc.

**Ràng buộc nghiệp vụ:** màn nhân viên **không hiển thị đơn giá, thành tiền hoặc tạm tính** — luận văn
§3.3.2.3 và §3.3.1.2. Đây là ràng buộc quyền, không phải lựa chọn thị giác.

### W-D — M3 + M4 vận hành kỳ (6 màn, gap lớn nhất)

Wave nặng nhất, làm theo đúng thứ tự bước nghiệp vụ:

1. `department-summary` — căn theo Atlas, phạm vi phòng ban.
2. `supplement-approval` — chẻ `Tab_AdminApproval` (357 dòng), tách hàng chờ khỏi khung vận hành kỳ.
3. `period-review` — điều kiện đạt / cảnh báo / điều kiện ngăn chốt, nêu rõ nguyên nhân thay vì lỗi chung.
4. `period-demand` — **màn mới**, kèm endpoint ở mục 4; hấp thụ nội dung `Tab_AllOrdersSummary` (D2)
   rồi gỡ tab cũ.
5. `supply-allocation` — **màn mới**, tách khỏi `PeriodSettlementPanel`; nhà cung cấp trước → bảng giá
   còn hiệu lực → độ phủ → ngoại lệ bắt buộc lý do và ghi nhật ký.
6. `settlement-flow` — xem trước, xác nhận snapshot bất biến, hiệu chỉnh tạo phiên bản mới theo nguyên
   tắc bốn mắt (§2.3.1.9). Giữ `idempotency key` và `InputHash` như backend đang làm.

Sau wave này, thanh điều hướng kỳ hiển thị đủ 4 bước như Atlas `periodFlowNav`.

### W-E — M5A + M5B thư viện dữ liệu (7 màn, gap B)

7 tab `/library` dùng chung một workspace: bảng chính + inspector + drawer thêm/sửa. Trạng thái quản trị
chỉ ánh xạ soft-delete (`Hoạt động` / `Ngừng áp dụng`); riêng bảng giá có vòng đời `Draft / Published /
Expired`. Không bao giờ render `PasswordHash`, token hay security stamp.

### W-F — M6 người dùng và phân quyền (2 màn, gap B)

Ma trận quyền đúng 3 vai trò (D1). Mỗi tài khoản một phân công quyền đang hiệu lực; đặt lại mật khẩu,
đổi nhóm quyền, vô hiệu hóa đều ghi nhật ký bảo mật (§3.3.4.3).

### W-G — M7 báo cáo (1 màn, gap C)

Bỏ mọi dấu vết PDF ở cả code lẫn Atlas (D3). Giữ CSV UTF-8 + XLSX với 5 sheet
`Summary / Items / Departments / Trend / TopProducts`. Kỳ đã chốt lấy số từ dữ liệu chốt kỳ, không tính lại.

### W-H — M8 trạng thái, hardening, và đóng gói luận văn

1. Rà soát 6 trạng thái dùng chung: hộp thư, mất kết nối, thiếu quyền, lỗi, rỗng, đang tải.
2. QA chéo Dark/Print/mobile.
3. **Chụp lại 16 hình runtime** cho luận văn từ hệ thống chạy thật, thay ảnh thiết kế Atlas. Đây chính
   là việc mà `LVTN/generated/missing-ui-screens.md` đang treo: hiện luận văn ghi rõ các hình là *thiết
   kế giao diện*, chưa phải bằng chứng kiểm thử.
4. Sửa câu chữ §3.3.5.1 theo D3.

---

## 6. Đồng bộ Atlas ↔ frontend (không phải 100%)

Quy ước để hai bên không trôi khỏi nhau mà cũng không phải khớp từng pixel:

| Phải khớp | Được phép khác |
|---|---|
| Danh sách màn hình và tên màn | Dữ liệu mẫu (Atlas dùng fixture cố định) |
| Cấu trúc thông tin: vùng nào chứa gì, thứ tự các bước | Số dòng, số liệu KPI cụ thể |
| Cách điều hướng dữ liệu: paging hay scroll ảo cho từng vùng | Chiều rộng cột chính xác |
| Cấp bậc hành động: đâu là primary, secondary, quiet | Kỹ thuật render (Radzen vs HTML thuần) |
| Copy tiếng Việt của nhãn, tiêu đề, trạng thái | Hiệu ứng chuyển động |
| Trường dữ liệu hiển thị (phải có thật trong DTO) | Chi tiết bo góc, đổ bóng |

Sau mỗi wave: cập nhật route ledger trong living plan, cập nhật Atlas nếu owner duyệt khác thiết kế, và
cập nhật sổ tay đọc code. Ba thứ này đi cùng nhau trong một lần, không tách.

---

## 7. Chiến lược đọc-hiểu code (D6)

Mục tiêu: mở luận văn ra, thấy Hình 3-36, tìm được ngay dòng code tương ứng và giải thích được nó làm gì.

**`docs/CODE-READING-GUIDE.md`** — tiếng Việt, gồm:

1. Bảng tra chính: `Hình luận văn → Màn Atlas → Route Blazor → File component → Endpoint API → Mục nghiệp vụ`.
2. Sơ đồ luồng chữ cho 5 nghiệp vụ hay bị hỏi khi bảo vệ: tạo đơn, đơn bổ sung, chốt kỳ, hiệu chỉnh
   kết quả chốt kỳ, phân quyền.
3. Từ điển thuật ngữ Anh–Việt: `period / kỳ`, `settlement / chốt kỳ`, `supplement / đơn bổ sung`,
   `revision / phiên bản`, `rowversion / cơ chế chống ghi đè`, `soft delete / xóa mềm`.
4. Giải thích vì sao chọn từng kiểu điều hướng dữ liệu — câu hỏi rất dễ bị hỏi.

**Comment tiếng Việt tại điểm nghiệp vụ.** Code, tên biến, tên hàm, tên file: tiếng Anh 100%. Chỉ chèn
comment tiếng Việt ngắn ở chỗ luật nghiệp vụ khó đoán từ code, kèm số mục luận văn:

```csharp
// Quy tắc bốn mắt: người xác nhận hiệu chỉnh không được là người tạo phiên bản
// đang hiện hành. Xem luận văn §2.3.1.9.
if (currentRevision.CreatedBy == actingUserId)
```

Không comment những dòng tự hiển nhiên. Mật độ comment giữ như phần code hiện có của repo.

---

## 8. Cổng kiểm thử mỗi wave

| Cổng | Yêu cầu |
|---|---|
| Build | `dotnet build gtas_vpp.sln -c Release` — 0 warning, 0 error |
| Backend test | `gtas_vpp_be.Tests` — không giảm so với mốc trước |
| Frontend test | `gtas_vpp_fe.Tests` — không giảm |
| UI test | `gtas_vpp_fe.UITests` targeted cho route trong wave |
| Browser QA | 390×844, 768×1024, 1920×1080; thu console error và failed request trước khi kết luận pass |
| Accessibility | axe WCAG 2 A/AA; không có violation `critical` hoặc `serious`; kèm kiểm tra bàn phím và focus thủ công |
| Tài liệu | Route ledger + decision log + sổ tay đọc code cập nhật cùng lần |

Chỉ chụp ảnh không phải visual regression — chỉ gọi là regression khi đã có baseline được owner duyệt.

---

## 9. Rủi ro

| Rủi ro | Xử lý |
|---|---|
| Aspire Testing chặn browser QA | Là việc đầu tiên của W-A. Chưa mở được route thì mọi wave sau không nghiệm thu được |
| Radzen MCP hết quota (50 request / 15 ngày) | Chia truy vấn theo từng component, tái dùng kết quả trong cùng wave. Hết quota thì dừng và xin key mới, không đoán |
| Hình luận văn lỗi thời sau khi sửa UI | Chỉ chụp lại ở W-H, sau khi UI đã ổn định. Chụp sớm sẽ phải chụp lại nhiều lần |
| `Tab_History` 577 dòng và `Tab_AdminApproval` 357 dòng | Chẻ nhỏ trước khi sửa bố cục, không sửa trực tiếp trên file lớn |
| Mất Atlas do cache Codex bị dọn | Giải quyết ngay ở W-A bằng D5 |
| Phình phạm vi sang React | `gtas_vpp_fe_react/` vẫn `PAUSED/DEFERRED`, không đụng |

---

## 10. Quyết định bổ sung — 2026-07-26 (lượt 2)

| # | Quyết định | Hệ quả |
|---|---|---|
| D7 | **Bổ sung endpoint `GET /api/VPPRequest/period-demand`** | Hiện thực hóa `AggregatedVppResDTO` đang mồ côi. Chỉ đọc, policy `Permissions.PeriodSettle`, phạm vi theo quyền phiên đăng nhập. Làm trong W-D |
| D8 | **Giữ `Tổng hợp toàn công ty` làm chế độ xem trong `Gom nhu cầu`** | Không xóa `Tab_AllOrdersSummary`. Màn `period-demand` có hai chế độ: `Theo đơn` (nội dung tab cũ) và `Theo mặt hàng` (tổng nhu cầu gom). Tab riêng ở thanh điều hướng bị gỡ, code được tái dùng |
| D9 | **Chưa đụng luận văn trong phạm vi ATLAS-001** | Không sửa bất kỳ file `.docx` nào. Bản mới nhất là `checkpoints/NguyenAnNam_DH52201078_final_v4_standard.docx`; `_working.docx` đã cũ, không dùng làm nguồn. Sai lệch §3.3.5.1 chỉ được ghi vào mục 11 để owner xử lý sau |

Điều chỉnh kéo theo: **W-H bỏ mục 4** (sửa câu chữ §3.3.5.1). Việc chụp lại 16 hình runtime vẫn giữ,
nhưng chỉ xuất ảnh ra thư mục, không chèn vào file Word.

| # | Quyết định | Hệ quả |
|---|---|---|
| D10 | **Làm thật xuất PDF + Excel theo đơn** (lượt 3, thay phương án gỡ nút của W-A.7) | Backend: `GET /api/VPPRequest/orders/{id}/export.pdf` và `/export.xlsx`, cùng quy tắc phạm vi với xem chi tiết đơn, **không chứa giá**. XLSX tự sinh theo pattern `ReportWorkbookBuilder`; PDF dùng QuestPDF Community + font Poppins (OFL) nhúng trong assembly để container Linux render đúng tiếng Việt. FE: nút trong `VppOrderWorkspacePanel` (cả ba panel My Orders). Atlas: khôi phục `orderExportActions` trên card chi tiết đơn và đảo guard renderer về "bắt buộc 2 nút" |

Các quyết định W-B.2 dưới đây do AI tự quyết trong phạm vi ủy quyền design của owner
("nếu có design nào tốt hơn, bạn có thể tự quyết định") — owner có thể phủ quyết, khi đó revert từng mục:

| # | Quyết định | Hệ quả |
|---|---|---|
| D11 | **Font body chuyển về system stack theo Atlas** (`-apple-system … 'Segoe UI'`, Inter làm fallback) | Trước đây sidebar đã dùng system stack còn nội dung dùng Inter — hai nửa app lệch font nhau. Đồng nhất theo Atlas; `--vpp-tracking-tight: -0.025em` cho heading. Sửa một chỗ trong `vpp-tokens.css` |
| D12 | **Sidebar desktop mặc định mở rộng, ghi nhớ lựa chọn** | Khớp baseline Atlas (286px). Lần đầu: mở rộng nếu viewport ≥768px (JS `vppViewport.isDesktop`); các lần sau đọc `ProtectedLocalStorage["VPP_SidebarExpanded"]` như theme. Mobile giữ thu gọn/overlay |
| D13 | **Icon nav theo Atlas, nhưng sibling tĩnh phải khác glyph nhau** | Atlas render động một cháu mỗi lần nên trùng glyph không lộ; Blazor render tĩnh cả hai. Lệch có chủ đích: `Tổng hợp phòng ban`=`table_view` (Atlas) còn `Toàn bộ đơn`=`list_alt`; `Rà soát kỳ`=`fact_check` (Atlas) còn `Duyệt đơn bổ sung`=`approval`; `Danh sách bảng giá`=`price_change` (Atlas) còn `Giá mặt hàng` giữ `payments` |
| D14 | **Header desktop hoãn bật đến W-B.2b** | Bật header breadcrumb+badge khi thanh tab 72px còn trong body sẽ tạo hai tầng chrome chồng nhau. Markup header (breadcrumb `cha › con` + role badge 3 persona qua Loc) đã xong và phục vụ mobile; desktop bật cùng lúc với việc chuyển tab strip vào header (kèm cập nhật E2E selector + QA browser). *Cập nhật cùng ngày: W-B.2b đã làm — desktop bật header với tab strip khu vực.* |
| D15 | **Owner chốt nhãn "Email công ty"** (Quên mật khẩu + Đăng ký) | Giữ nhãn Blazor hiện tại; retrofit ngược Atlas (`atlas.js` account-forgot/account-register field "Email" → "Email công ty" — đã sửa). Backend chỉ validate định dạng (`[EmailAddress]`, AccountRegistrationReqDTO), KHÔNG ràng buộc domain — demo bằng email thường vẫn hoạt động; nhãn chỉ là hướng dẫn nghiệp vụ |

## 12. Nhật ký gỡ chốt chặn W-A.1 — Aspire `frontend-http`

Triệu chứng: `InvalidDataException: Service frontend-http should have valid address at this point`
ném từ bên trong `DistributedApplication.StartAsync`, tái hiện ổn định 2 lần, chặn toàn bộ browser QA.

Chẩn đoán (kiểm chứng bằng decompile `Aspire.Hosting` 13.4.6 và đối chiếu source tag `v13.4.6`):

1. `DistributedApplicationTestingBuilder` mặc định ép `DcpPublisher:RandomizePorts=true`, sau đó
   `DcpExecutor.PrepareServices` xóa `Spec.Port` của mọi endpoint proxy — cổng khai trong
   `launchSettings.json` **không bao giờ được dùng** trong test run. Đó là lý do hai thí nghiệm
   ban đầu (chờ resource `Running`; đổi `0.0.0.0` → `localhost`) không thay đổi gì.
2. Địa chỉ service chỉ còn phụ thuộc DCP báo `Status.EffectivePort`. `DcpExecutor` chờ tối đa
   ~2 phút; hết hạn chỉ log warning rồi đi tiếp, và pass kế tiếp (`allowPending:false`) ném
   exception trên — tức exception là **tiếng vọng** của một lần cấp cổng thất bại bị nuốt.
3. Nguyên nhân môi trường trên máy này: 724 thư mục session `aspire-dcp*` cũ trong `%TEMP%`,
   state `~/.dcp` có lock file mồ côi từ 14/07, và process con của các lần chạy bị hủy còn ghi
   log 3 giờ sau. Cùng lớp lỗi với aspire#18548/#18563; DCP fix chỉ có từ Aspire 13.5.

Khắc phục đã áp dụng:

- Dọn một lần: xóa `~/.dcp` và toàn bộ `%TEMP%\aspire-dcp*` (Aspire tự tạo lại).
- `gtas_vpp_fe.UITests/Core/TestBase.cs`: thêm `--DcpPublisher:RandomizePorts=false` để giữ cổng
  cố định — địa chỉ đầy đủ ngay khi tạo Service, không còn phụ thuộc sự kiện cấp cổng của DCP.
  An toàn vì test đã `DisableTestParallelization`; đổi lại **không được** chạy test song song với
  `dotnet run` AppHost.
- Khi Aspire 13.5 ổn định: cân nhắc nâng toàn bộ `Aspire.*` + SDK (mang theo DCP 0.25.x có fix gốc).

**Cập nhật 2026-07-26 18:20 — bằng chứng quyết định từ DCP diagnostics log:**

Sau khi dọn state và tắt randomize, lượt chạy tiếp theo treo vô hạn (app không bao giờ mở). Bật
`DcpPublisher__DiagnosticsLogFolder` thu được nguyên nhân gốc thật sự:

```
dcp.NotificationReceiver: Failed to subscribe ... transport error (named pipe AppData\Local\dcp...)
dcp: Unable to confirm the API server is responding:
     Get "https://127.0.0.1:56589/api": tls: failed to verify ...
dcp: the program finished with an error, ExitCode=1
     failed to initialize the controller manager
```

Tức: DCP API server mở được, nhưng **controller không xác minh được chứng chỉ TLS tự ký của chính
API server đó** và thoát mã 1 → không service nào được cấp cổng. Đây là hỏng hóc môi trường máy ở
tầng chứng chỉ DCP (log đầy đủ lưu ngoài repo, scratchpad phiên 2026-07-26).

**Quyết định vận hành:** W-A.1 tạm gác (PARKED). Browser QA của các wave chuyển sang chế độ trực
tiếp: chạy app bằng `dotnet run` AppHost hoặc chạy riêng backend+frontend, QA bằng Playwright MCP
trên route thật. `gtas_vpp_fe.UITests` (Aspire Testing) chỉ bật lại khi một trong hai điều kiện:
(a) sửa được TLS trust của DCP trên máy (nghi can kế tiếp: thư mục `%LOCALAPPDATA%\dcp` chưa được
dọn cùng `~/.dcp`, hoặc phần mềm can thiệp TLS trên máy), hoặc (b) nâng Aspire 13.5.
Fix `--DcpPublisher:RandomizePorts=false` trong `TestBase.cs` vẫn giữ — nó đúng độc lập với lỗi TLS.

**Cập nhật 2026-07-26 19:05 — ĐÃ GIẢI QUYẾT (root cause: Norton chặn TLS loopback):**

- Nguyên nhân gốc xác nhận: **Norton Antivirus chèn TLS interception vào mọi process con**
  (inject `NODE_EXTRA_CA_CERTS=C:\ProgramData\Norton\Antivirus\wscert.pem` và
  `SSLKEYLOGFILE=\\.\nllMonFltProxy\...`), MITM cả `https://127.0.0.1` → controller của DCP
  không tin chứng chỉ tự ký của chính API server DCP → thoát mã 1 → không cấp cổng →
  `frontend-http should have valid address`.
- Owner cấu hình lại Norton (2 lần; lần 1 lúc ~18:5x chưa đủ — DCP log vẫn ghi
  `x509: certificate signed by unknown authority` lúc 18:51:15). Sau lần chỉnh thứ hai,
  hai biến môi trường Norton chèn **biến mất** khỏi process mới, và `LoginTests` pass 1/1
  (1 phút 02 giây) với harness **hoàn toàn nguyên bản**.
- `--DcpPublisher:RandomizePorts=false` đã bị **hoàn nguyên trước đó** theo quy tắc E2E của owner
  (cấm ép cổng cố định để test pass); dòng "vẫn giữ" phía trên hết hiệu lực. `TestBase.cs`
  hiện 100% stock.
- Bài học: khi DCP/Aspire lỗi chứng chỉ trên loopback, kiểm tra ngay phần mềm bảo mật chèn TLS
  (dấu vết: `NODE_EXTRA_CA_CERTS`/`SSLKEYLOGFILE` trong env của process con) trước khi nghi ngờ
  state DCP hay version Aspire.

---

## 11. Sai lệch tài liệu đang treo — chờ owner xử lý ngoài phạm vi này

| Vị trí | Nội dung hiện tại | Thực tế trong source | Đề xuất |
|---|---|---|---|
| Luận văn §3.3.5.1 | "người dùng có quyền có thể xuất PDF hoặc Excel" | `ReportsController` chỉ có `export` (CSV) và `export.xlsx`; không có endpoint PDF | Sửa thành "xuất CSV hoặc Excel" cho khớp §3.4 |

Không sửa file Word trong ATLAS-001 (D9). Ghi lại ở đây để không bị quên khi owner mở lại phạm vi luận văn.
