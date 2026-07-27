# Test instructions

Áp dụng cho toàn bộ `tests/`.

- Đọc root `AGENTS.md` và production code tương ứng; test bảo vệ behavior, không thay assertion để che implementation sai.
- Unit test phải deterministic, không phụ thuộc thời gian hiện tại, thứ tự chạy hoặc dữ liệu máy cá nhân.
- Integration/UI test dùng TEST database hoặc fixture do harness sở hữu; không trỏ vào LIVE/production/shared manual database.
- Authenticated UI test cần `GTAS_E2E_ISOLATED=1`; test mutation còn cần `GTAS_E2E_MUTATION_OPT_IN=I_UNDERSTAND_THIS_MUTATES_QA_DATA`.
- Screenshot, trace, video và browser storage là output tạm; lưu vào `TestResults` hoặc thư mục temp đã ignore, không commit khi chưa có owner-approved baseline.
- Locator ưu tiên role/name/test id ổn định; không phụ thuộc class Radzen nội bộ nếu behavior người dùng có selector semantic.
- UI phải kiểm tra interaction, console/network, accessibility và geometry quan trọng; screenshot đơn lẻ không chứng minh correctness.

Lệnh chuẩn:

```powershell
./scripts/gtas.cmd preflight -Scope tests
./scripts/gtas.cmd test
dotnet test tests/Backend.IntegrationTests/gtas_vpp_be.IntegrationTests.csproj -c Release
```
