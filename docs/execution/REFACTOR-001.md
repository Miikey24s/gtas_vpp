# REFACTOR-001 — Tối ưu backend + frontend (+database) cho khả năng đọc hiểu và tốc độ kiểm thử

Chủ plan: owner Nguyễn An Nam. Khởi tạo 2026-07-26 theo yêu cầu owner (mở sớm, chạy song song
phần không xung đột với ATLAS-001 thay vì chờ ATLAS-001 xong hẳn).

## 1. Mục tiêu và thứ tự thẩm quyền

Tiêu chí gốc (quyết định D6 của ATLAS-001, cam kết xuyên phiên):

1. Code, tên biến/hàm/file: **tiếng Anh 100%**.
2. Owner vibe-coding nhưng phải **đọc hiểu và trình bày được** source khi bảo vệ luận văn.
3. Comment **tiếng Việt ngắn** chỉ tại điểm luật nghiệp vụ khó đoán, kèm số mục luận văn (vd §2.3.1.9).
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

- **R-1 backend (`gtas_vpp_be`)**: rà từng service theo thứ tự luận văn chương 2 (Auth →
  VPPRequest → PeriodSettlement → Library → Report). Mỗi service: (a) tên method/biến EN chuẩn,
  (b) comment VI + §, (c) tách method dài theo bước nghiệp vụ, (d) xóa dead code (đã có ứng viên:
  project `MyAspire.ServiceDefaults` mồ côi — chờ owner duyệt xóa).
- **R-2 frontend (`gtas_vpp_fe`)**: sau khi wave ATLAS tương ứng đóng. Ứng viên đầu: các file đã
  ổn định qua W-B/W-C (VppStatePanel, VppIcons, LeftSidebar, NotificationCenter). `Tab_History`
  đã có kế hoạch chẻ 5 component (C-7 của W-C — tính là một lát R-2 làm sớm).
- **R-3 database**: KHÔNG đổi schema/data nếu chưa hỏi owner. Phạm vi mặc định: comment tiếng Việt
  trong stored procedure (kiểm tra bằng SSMS trước theo AGENTS.md), tài liệu hóa bảng/quan hệ vào
  CODE-READING-GUIDE (bảng ↔ mục luận văn §3.2), index/performance chỉ khi có số đo và owner duyệt
  từng mục.

## 5. Trình tự tổng

1. **R-0** (E2E speed) — bắt đầu ngay sau khi lượt full E2E chốt batch W-B/W-C xanh (cần baseline xanh để so).
2. C-7 chẻ Tab_History (lát R-2 làm sớm, đã lên lịch ở ATLAS W-C).
3. R-1 backend theo module, xen kẽ giữa các wave ATLAS W-D…W-H (backend ít bị wave UI đụng).
4. R-2 frontend cuốn chiếu theo wave ATLAS đã đóng.
5. R-3 database sau khi R-1 module tương ứng xong (đọc SP trong ngữ cảnh service đã sạch).

## 6. Decision log

| # | Ngày | Quyết định |
|---|---|---|
| R-D1 | 2026-07-26 | Owner mở REFACTOR-001 sớm, chạy song song phần không xung đột ATLAS-001; bổ sung phạm vi database + tối ưu tốc độ E2E |
| R-D2 | 2026-07-26 | **R-0 pha A hoàn thành**: full suite 28 test từ **32m48s → ~6-7 phút** (gấp ~5 lần). Kiến trúc: `Core/PlaywrightBrowserFixture` (1 Chromium/suite, BrowserContext mới mỗi test), `Core/SharedE2EAppFixture` + collection `e2e-readonly` (17 class chỉ-đọc/anonymous dùng chung 1 app+DB), `Core/IsolatedE2EStack` (mutate giữ app riêng), orderer cố định (xUnit v3 random hóa thứ tự collection), login bỏ 2×750ms sleep, poll readiness 2s→500ms. Thiết kế chi tiết: `REFACTOR-001-R0-DESIGN.md` |
| R-D3 | 2026-07-26 | **Rate limiter login nới có điều kiện QA**: policy "login" 5 lượt/phút/IP làm shared-app E2E tự nghẽn (23 login/suite một IP loopback). `Program.cs` đặt `PermitLimit = 1000` CHỈ khi `qaFixtureIdentity` hợp lệ (tín hiệu fail-closed, ném exception ngoài QA) — production/dev giữ nguyên 5/phút |
| R-D4 | 2026-07-26 | Phát hiện trong lúc R-0: theme Radzen 11 đè grid shell bằng selector `:has()` — sửa gốc bằng `FullHeight="true"` trên `RadzenSidebar` (bug tiềm ẩn, chỉ lộ khi bật header desktop W-B.2b) |
| R-D5 | 2026-07-26 | Pha B (S5: mutate dùng chung app + BACKUP/RESTORE checkpoint, ước xuống ~5-8 phút) để mở sau nếu cần — pha A đã vượt mục tiêu 10-14 phút |
