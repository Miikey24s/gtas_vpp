# Hướng dẫn làm việc với repository GTAS VPP

Áp dụng cho AI và lập trình viên làm việc trong toàn bộ repository.

## Phạm vi và cấu trúc chuẩn

- Backend nằm trong `gtas_vpp_be/`; frontend Blazor hiện hành nằm trong `gtas_vpp_fe/`; frontend React chạy song song nằm trong `gtas_vpp_fe_react/`.
- Shared DTO duy nhất là `gtas_vpp_be/gtas_vpp_shared`. Không tạo lại `gtas_vpp_fe/gtas_vpp_shared`.
- Không sửa API, database hoặc nghiệp vụ chỉ để làm cho nội dung luận văn khớp; luận văn phải mô tả đúng source thực tế.
- Khi sửa UI, đọc và tuân thủ `.codexrules` cùng `.github/copilot-instructions.md`.
- Khi sửa stored procedure, ưu tiên kiểm tra câu lệnh trong SQL Server Management Studio trước hoặc song song với debug trong code.

## UI renovation plan

- React là frontend mục tiêu. Trước mọi thay đổi trong `gtas_vpp_fe_react/`, phải đọc và cập nhật `docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md`.
- `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md` chỉ còn là historical baseline/ledger cho Blazor; chỉ cập nhật file đó khi sửa hoặc ghi nhận riêng frontend Blazor.
- Đọc `docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md` để chọn đúng nguồn tài liệu, browser tool và QA layer; không cài hoặc gọi nhiều MCP trùng chức năng chỉ để tăng số lượng công cụ.
- React chạy thật trong browser là nguồn quyết định visual cuối cho frontend mới; Figma và Blazor chỉ là tài liệu nghiên cứu/baseline, không phải nguồn pixel/route authority.
- React là modernization, không port 1:1. Được tối ưu route, workflow, component và API contract khi giữ business invariant/permission/audit và có test/migration phù hợp.
- Không tạo thêm UI Lab hoặc frontend preview khác. Triển khai trực tiếp trong `gtas_vpp_fe_react/`, dùng API/DTO và database TEST hoặc isolated fixture thật.
- Sau mỗi vòng người dùng duyệt hoặc từ chối một route, cập nhật route ledger, decision/learning log và retrofit queue trong living plan trước khi tiếp tục.
- Giữ một kiến trúc global `InteractiveServer`; không thêm `@rendermode` cục bộ nếu chưa có quyết định kiến trúc mới.

### Công cụ UI/UX cho AI agent

- Tra Microsoft Learn MCP trước cho .NET, Blazor, ASP.NET Core, Aspire và tài liệu Microsoft; tra Radzen MCP trước khi sửa Radzen component/API; dùng Context7 cho package bên thứ ba khi tài liệu chính chủ chưa đủ.
- Hạn mức Radzen MCP hiện tại là 50 request trong 15 ngày. Nếu Radzen MCP báo hết quota, key lỗi hoặc không còn truy cập được thì dừng toàn bộ công việc ngay và yêu cầu người dùng bổ sung key mới; không âm thầm làm tiếp bằng suy đoán.
- Dùng Playwright MCP cho DOM/accessibility snapshot, interaction, screenshot, console và network của route thật. Dùng Chrome DevTools MCP khi cần trace performance hoặc debug sâu; chỉ kết nối browser/profile TEST riêng, không chứa tài khoản cá nhân, cookie hoặc secret.
- Figma MCP chỉ bổ sung flow, token và design context. Với GTAS VPP, code/browser đã duyệt vẫn thắng Figma khi có khác biệt.
- Screenshot thông thường chỉ là evidence. Chỉ gọi là visual regression khi đã có baseline được người dùng duyệt, môi trường/browser/viewport ổn định và phép so sánh tự động.
- Accessibility phải kết hợp axe tự động với kiểm tra keyboard/focus và review thủ công; không xem một lần scan axe hoặc Lighthouse là bằng chứng WCAG đầy đủ.

## Build và kiểm thử

Chạy tối thiểu các lệnh phù hợp với phạm vi thay đổi:

```powershell
dotnet build gtas_vpp.sln -c Release
dotnet test gtas_vpp_be.Tests/gtas_vpp_be.Tests.csproj -c Release
dotnet test gtas_vpp_fe.Tests/gtas_vpp_fe.Tests.csproj -c Release
```

Mốc gần nhất được ghi trong W1 change-set là 397 backend test và 143 frontend test đều pass. Đây chỉ là evidence theo thời điểm; luôn chạy lại test phù hợp sau khi sửa code.

## Luận văn

- Không ghi đè bản gốc `LVTN/NguyenAnNam_DH52201078.docx`.
- Bản làm việc là `LVTN/NguyenAnNam_DH52201078_working.docx`; bản bàn giao là `LVTN/checkpoints/99_final.docx`.
- Giữ `MAU_LVTN_2026.pdf` và `Luận văn tốt nghiệp (1).docx` làm tài liệu đối chiếu định dạng.
- Không thêm page border cho bìa theo quyết định hiện tại của người dùng.
- Mục lục, danh mục hình, tài liệu tham khảo và các tham chiếu nội bộ phải là liên kết có thể bấm; trước khi bàn giao phải cập nhật field và kiểm tra số trang.
- Sơ đồ kỹ thuật giữ cả `.puml` và `.svg`, phù hợp in đen trắng. Đọc `LVTN/diagrams/README.md` trước khi sửa.
- Dùng script trong `LVTN/tooling/`; lưu render và contact sheet vào thư mục tạm đã bị Git ignore.
- Mỗi lần sửa Word phải render và kiểm tra trực quan các trang bị tác động trước khi thay bản final.

## Bảo mật và vệ sinh Git

- Không commit `.env`, secret, mật khẩu, token, connection string cá nhân hoặc tài khoản test.
- Không commit `bin/`, `obj/`, `TestResults/`, log, cache, JDK/PlantUML/LibreOffice tải cục bộ, ảnh audit tự động hoặc output render tạm.
- Chỉ giữ checkpoint Word cuối cùng trong Git; checkpoint trung gian để ở local hoặc ngoài repository.
- Không xóa file Word, script tooling, `.puml`, `.svg`, screenshot luận văn hay hướng dẫn AI chỉ vì chúng không tham gia build ứng dụng.
- Trước khi commit, chạy `git diff --check`, xem toàn bộ `git status` và chỉ stage đúng phạm vi công việc.
