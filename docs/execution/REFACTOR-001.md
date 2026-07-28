# REFACTOR-001 — Tối ưu backend + frontend (+database) cho khả năng đọc hiểu và tốc độ kiểm thử

Chủ plan: owner Nguyễn An Nam. Khởi tạo 2026-07-26 theo yêu cầu owner (mở sớm, chạy song song
phần không xung đột với ATLAS-001 thay vì chờ ATLAS-001 xong hẳn).

> Cập nhật 2026-07-29: phần **R-1 backend** và quy ước comment backend đã được thay thế bởi
> [`BACKEND-REFACTOR-001.md`](BACKEND-REFACTOR-001.md). Record này tiếp tục giữ lịch sử R-0 E2E,
> R-2 frontend và các decision đã hoàn thành; không dùng mục R-1 bên dưới làm execution authority mới.

## 1. Mục tiêu và thứ tự thẩm quyền

Tiêu chí gốc (quyết định D6 của ATLAS-001, cam kết xuyên phiên):

1. Code, tên biến/hàm/file: **tiếng Anh 100%**.
2. Owner vibe-coding nhưng phải **đọc hiểu và trình bày được** source khi bảo vệ luận văn.
3. Comment **tiếng Việt ngắn** chỉ tại điểm luật nghiệp vụ khó đoán, giải thích vì sao/ràng buộc.
   Không mặc định gắn số mục luận văn trong source; mapping học thuật nằm ở `docs/CODE-READING-GUIDE.md`.
4. `docs/CODE-READING-GUIDE.md` cập nhật đồng bộ từng đợt.
5. **Không đổi nghiệp vụ/API/schema** — muốn đổi phải hỏi owner trước, từng mục một.

Bổ sung 2026-07-26 (owner): thêm phạm vi **database** và **tối ưu E2E chạy nhanh hơn nhưng giữ
nguyên chất lượng bao phủ**.

Thứ tự thẩm quyền khi mâu thuẫn: luận văn → hành vi hiện tại của hệ thống (test đang xanh là
hợp đồng) → sở thích đặt tên. Refactor không bao giờ được đổi hành vi mà test đang khóa.

## 2. Nguyên tắc phối hợp với ATLAS-001 (đang chạy song song)

- **Không refactor file đang nằm trong wave ATLAS chưa nghiệm thu.** Mỗi lát refactor chỉ đụng
  module đã ổn định (backend service/DB/test-infra) hoặc module ATLAS đã đóng.
- Gate mỗi lát giống mục 8 của ATLAS-001: `dotnet build` 0 warning + unit BE/FE xanh; full E2E
  theo quy tắc batch của owner (gom nhiều lát, một lượt chốt).
- Lát nào chạm hành vi/DTO/schema: dừng, hỏi owner, ghi decision log ở mục 6.

## 3. R-0 — Tối ưu tốc độ E2E (làm TRƯỚC, hưởng lợi cho mọi wave còn lại)

Hiện trạng: full suite ~28 phút cho 28 test / ~21 test class. Nguyên nhân đo được: **mỗi test
class dựng lại toàn bộ hệ thống** (LocalDB QA migrate+seed → Aspire backend+frontend → Chromium)
≈ 1 phút overhead/class, chiếm đại đa số thời gian chạy.

Hướng làm (giữ nguyên chất lượng và các quy tắc E2E của owner — không ép cổng, không chạy
song song nhiều suite, không đổi env contract):

| Bước | Nội dung | Ước lợi |
|---|---|---|
| R-0.1 | Đo baseline chính thức: thời gian từng class (trx log) để biết chỗ nào tốn nhất | — |
| R-0.2 | **Fixture dùng chung theo nhóm**: xUnit v3 assembly/collection fixture — nhóm test CHỈ ĐỌC (`IAuthenticatedUiTest` không mutate) dùng chung một DistributedApplication + DB seed; nhóm MUTATE (`IMutatingUiTest`) giữ app riêng hoặc reset dữ liệu giữa test | ~28′ → ước 10-14′ |
| R-0.3 | Dùng chung một Playwright browser process, mỗi test một BrowserContext mới (cách ly cookie/storage vẫn tuyệt đối) | ~1-2′ |
| R-0.4 | Seed QA: tách "schema migrate" (1 lần) khỏi "data seed" (theo nhóm); mutate xong trả dữ liệu về trạng thái seed bằng respawn/checkpoint thay vì dựng DB mới | ~2-4′ |
| R-0.5 | Rà `WaitForTimeoutAsync` cứng trong test (350/500ms rải rác) → thay bằng wait theo điều kiện (đã có mẫu `data-shell-ready`) | ổn định + nhanh |

Ràng buộc chất lượng: số assertion không giảm; tính cách ly của nhóm mutate phải được chứng minh
(chạy 2 lượt liên tiếp kết quả như nhau); giữ `DisableTestParallelization` trừ khi chứng minh được
cách ly hoàn toàn theo nhóm.

## 4. R-1 backend, R-2 frontend, R-3 database — các lát đọc-hiểu

- **R-1 backend**: đã được supersede bởi `BACKEND-REFACTOR-001.md`; dùng record mới cho scope,
  comment policy, wave, verification và routing. Không tiếp tục triển khai từ mô tả cũ này.
- **R-2 frontend (`gtas_vpp_fe`)**: sau khi wave ATLAS tương ứng đóng. Ứng viên đầu: các file đã
  ổn định qua W-B/W-C (VppStatePanel, VppIcons, LeftSidebar, NotificationCenter). `Tab_History`
  đã có kế hoạch chẻ 5 component (C-7 của W-C — tính là một lát R-2 làm sớm).
- **R-3 database**: KHÔNG đổi schema/data nếu chưa hỏi owner. Phạm vi mặc định: comment tiếng Việt
  trong stored procedure (kiểm tra bằng SSMS trước theo AGENTS.md), tài liệu hóa bảng/quan hệ vào
  CODE-READING-GUIDE (bảng ↔ mục luận văn §3.2), index/performance chỉ khi có số đo và owner duyệt
  từng mục.

## 5. Trình tự hiện hành

Trình tự cũ theo ATLAS W-B…W-H đã hoàn thành vai trò lịch sử. Thứ tự portfolio hiện hành và wave
backend nằm tại `BACKEND-REFACTOR-001.md` mục 8/12; trạng thái UI nằm tại `UI-SYSTEM-001.md`.
Không duy trì thêm một bảng thứ tự song song trong record này.

## 6. Decision log

| # | Ngày | Quyết định |
|---|---|---|
| R-D1 | 2026-07-26 | Owner mở REFACTOR-001 sớm, chạy song song phần không xung đột ATLAS-001; bổ sung phạm vi database + tối ưu tốc độ E2E |
| R-D2 | 2026-07-26 | **R-0 pha A hoàn thành**: full suite 28 test từ **32m48s → ~6-7 phút** (gấp ~5 lần). Kiến trúc: `Core/PlaywrightBrowserFixture` (1 Chromium/suite, BrowserContext mới mỗi test), `Core/SharedE2EAppFixture` + collection `e2e-readonly` (17 class chỉ-đọc/anonymous dùng chung 1 app+DB), `Core/IsolatedE2EStack` (mutate giữ app riêng), orderer cố định (xUnit v3 random hóa thứ tự collection), login bỏ 2×750ms sleep, poll readiness 2s→500ms. Thiết kế chi tiết: `REFACTOR-001-R0-DESIGN.md` |
| R-D3 | 2026-07-26 | **Rate limiter login nới có điều kiện QA**: policy "login" 5 lượt/phút/IP làm shared-app E2E tự nghẽn (23 login/suite một IP loopback). `Program.cs` đặt `PermitLimit = 1000` CHỈ khi `qaFixtureIdentity` hợp lệ (tín hiệu fail-closed, ném exception ngoài QA) — production/dev giữ nguyên 5/phút |
| R-D4 | 2026-07-26 | Phát hiện trong lúc R-0: theme Radzen 11 đè grid shell bằng selector `:has()` — sửa gốc bằng `FullHeight="true"` trên `RadzenSidebar` (bug tiềm ẩn, chỉ lộ khi bật header desktop W-B.2b) |
| R-D5 | 2026-07-26 | Pha B (S5: mutate dùng chung app + BACKUP/RESTORE checkpoint, ước xuống ~5-8 phút) để mở sau nếu cần — pha A đã vượt mục tiêu 10-14 phút |
| R-D6 | 2026-07-26 | **R-0.5 hoàn thành** (commit `a3e4ecc`): 28 call site `WaitForTimeoutAsync` còn lại → 2 (2 quiet window có chủ đích cho negative assertion, giữ kèm comment: AtlasWave1Tests 500ms "không có toast lỗi", HistoryTests 60ms "không render loading-line"). Mỗi thay thế chờ đúng tín hiệu phép đo phía sau cần (double-rAF `WaitForRenderSettleAsync` trong TestBase, điều kiện viewport/scroll/indicator/toast, marker `_bl_`). Flake xoay vòng cuối `GlobalRenderFlowTests.NotFoundAction`: root cause = click nút prerender trước khi circuit interactive trên full page load `/not-found` + console noise `ERR_ABORTED` khi điều hướng — sửa bằng `GetInteractiveButtonAsync` + allowlist chỉ `ERR_ABORTED`. Không xóa/nới assertion nào. Nghiệm thu: 3 lượt full suite 28/28 liên tiếp — chạy trong batch E2E kế tiếp |
| R-D7 | 2026-07-27 | **R-2 W-D hoàn thành phần host lớn**: `Tab_AdminApproval.razor` 419 dòng được tách thành coordinator dưới 100 dòng + `PeriodOperationsWorkspace` + `PendingApprovalWorkspace`. Dữ liệu và hành động đi qua parameter/EventCallback; API, DTO, permission và idempotency handler giữ nguyên. Architecture test khóa ranh giới component |
| R-D8 | 2026-07-27 | **R-2 W-E chốt mô hình thao tác thư viện**: inspector gọi lại inline `EditRow` và soft-delete handler hiện hữu; không thêm drawer CRUD thứ hai. Gỡ parameter/dead path hard-delete khỏi ShareGrid và nút hard-delete classes/price-lists; status filter price-list chuyển sang option VI/EN |
