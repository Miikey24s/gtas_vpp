# Bằng chứng kiểm chứng Chương 4–5 — 25/07/2026

## Mốc mã nguồn

- Mốc làm việc: `WT-2026-07-25`.
- Commit nền: `fbbaa491f5e1fcac24fbabb0d7800eb014191220` (`feat(frontend): streamline my orders workspace`, 23/07/2026).
- Trạng thái: working tree có thay đổi chưa commit thuộc đợt hoàn thiện vai trò, localization và giao diện. Vì vậy kết quả dưới đây chỉ đại diện cho đúng mốc làm việc này, không được gộp với số liệu lịch sử.
- Môi trường: Windows, .NET 10, cấu hình Release; integration test dùng SQL Server LocalDB cô lập.

## Kết quả chạy tại mốc WT-2026-07-25

| Hạng mục | Lệnh/báo cáo | Kết quả |
|---|---|---|
| Build solution | `dotnet build gtas_vpp.sln -c Release --no-restore` | Đạt; 0 cảnh báo, 0 lỗi; khoảng 5,62 giây. |
| Backend test | `dotnet test gtas_vpp_be.Tests/gtas_vpp_be.Tests.csproj -c Release --no-build` | 412 đạt, 2 không đạt, 0 bỏ qua; tổng 414. Hai lỗi thuộc hợp đồng vai trò chuẩn và giới hạn cấp quyền theo vai trò. |
| Frontend test | `dotnet test gtas_vpp_fe.Tests/gtas_vpp_fe.Tests.csproj -c Release --no-build` | 150 đạt, 1 không đạt, 0 bỏ qua; tổng 151. Lỗi do thiếu 17 khóa localization của luồng tạo đơn. |
| Integration test | Đặt `GTAS_QA_SQL_INTEGRATION=1`, sau đó chạy `dotnet test gtas_vpp_be.IntegrationTests/gtas_vpp_be.IntegrationTests.csproj -c Release --no-build` | 19 đạt, 1 không đạt, 0 bỏ qua; tổng 20. Fixture LocalDB chưa khớp mô hình vai trò đang refactor. |
| Playwright discovery | `dotnet test gtas_vpp_fe.UITests/gtas_vpp_fe.UITests.csproj -c Release --no-build --list-tests` | Phát hiện 27 kịch bản. Không chạy full E2E ở mốc này vì cổng unit/integration đang đỏ. |
| Dependency/security audit | `dotnet package list --project gtas_vpp.sln --vulnerable --include-transitive --format json --no-restore` | Không phát hiện package có lỗ hổng đã biết từ các nguồn NuGet đang cấu hình. |

## Bằng chứng E2E lịch sử — không gộp vào mốc hiện tại

Nguồn: `docs/execution/LEAN-09.md`, mốc triển khai `b681c5565723d7b977a5a4b4963785efbdbb2506`, ngày 17/07/2026.

- Hai lượt diễn tập UI xác thực cô lập: mỗi lượt 16/16 kịch bản đạt, lần lượt khoảng 427,0 giây và 406,9 giây.
- Kiểm tra responsive shell có mục tiêu: 1/1 đạt, khoảng 49,0 giây.
- Ở mốc lịch sử đó: build 0 cảnh báo/0 lỗi, backend 385/385, frontend 96/96.
- Bằng chứng này xác nhận hạ tầng và luồng E2E từng chạy thành công, nhưng không thay thế việc chạy lại sau khi sửa các lỗi của mốc `WT-2026-07-25`.

## Bằng chứng chức năng liên quan

- `docs/execution/LEAN-07.md`: báo cáo XLSX thực, gồm các sheet Summary, Items, Departments, Trend và TopProducts.
- `docs/execution/LEAN-07.md`: notification/outbox bền vững và SMTP cục bộ tương thích Mailpit; SMTP mặc định tắt, chưa phải nhà cung cấp email production.
- `docs/execution/LEAN-09.md`: bằng chứng triển khai và hai lượt Playwright E2E xác thực cô lập.
- `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md`: hồ sơ quyết định giao diện, vai trò và route ledger đang áp dụng.

## Điểm cần phản ánh trong kết luận

1. Chức năng lõi về kỳ, đơn yêu cầu, danh mục, bảng giá, tổng hợp/chốt kỳ, báo cáo, notification và outbox đã có bằng chứng mã nguồn và kiểm thử.
2. Mốc hiện tại chưa đạt cổng bàn giao vì còn 2 backend test, 1 frontend test và 1 integration test không đạt.
3. Full Playwright E2E chưa chạy lại tại mốc hiện tại; chỉ được viện dẫn riêng bằng chứng lịch sử.
4. XLSX và email outbox đã tồn tại; phần còn thiếu là mẫu PDF/XLSX chính thức, provider email/push production và kiểm chứng vận hành thật.
5. Chưa có kiểm thử tải dài hạn/dữ liệu lớn, tích hợp HR chính thức, cùng quy trình backup/restore/monitoring hoàn chỉnh trên máy chủ thật.
