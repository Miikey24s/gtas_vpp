# Hướng dẫn làm việc với repository GTAS VPP

Áp dụng cho AI và lập trình viên làm việc trong toàn bộ repository.

## Mô hình vận hành AI-first

- `docs/ai/AI-AGENT-OPERATING-MODEL.md` là bản đồ chính thức về context, skill, MCP, plan, kiểm thử và handoff cho repository gần như 100% AI-generated.
- Trước task phức tạp, chạy `./scripts/gtas.cmd preflight -Scope <all|frontend|backend|tests|thesis>` và dùng `docs/planning/05-EXECUTION-TEMPLATE.md` nếu cần execution record dài hạn.
- Khi sửa trong thư mục có `AGENTS.md` gần hơn, phải đọc file đó trước; hướng dẫn gần file đang sửa được ưu tiên.
- Workflow UI lặp lại được đóng gói tại `.agents/skills/gtas-vpp-ui-system/`; dùng skill này cho thay đổi Blazor/Radzen, Atlas, design token, responsive hoặc browser QA.
- Không đưa model, approval policy, sandbox, MCP credential hoặc secret theo máy vào Git. Chỉ tạo `.codex/config.toml` khi có một cấu hình repo-level thực sự ổn định và không nhạy cảm.

## Phạm vi và cấu trúc chuẩn

- Backend nằm trong `src/Backend/`; frontend chính hiện hành là Blazor/Radzen trong `src/Frontend/Blazor/`. React POC đã được lưu ở tag `archive/react-poc-2026-07-27`; `gtas_vpp_fe_react/` hiện chỉ là dependency host Playwright để giữ tương thích với tooling LVTN cũ.
- Shared DTO duy nhất là `src/Shared/`. Không tạo bản sao shared DTO trong frontend.
- Không sửa API, database hoặc nghiệp vụ chỉ để làm cho nội dung luận văn khớp; luận văn phải mô tả đúng source thực tế.
- Identifier và tên kỹ thuật giữ tiếng Anh theo convention; comment source mới hoặc comment được chạm trong scope viết tiếng Việt ngắn gọn, giải thích nghiệp vụ khó đoán thay vì kể lại code.
- Khi sửa UI, đọc và tuân thủ `.codexrules` cùng `.github/copilot-instructions.md`.
- Khi sửa backend, frontend, tests hoặc LVTN, đọc thêm `AGENTS.md` trong chính thư mục phạm vi đó.
- Khi sửa stored procedure, ưu tiên kiểm tra câu lệnh trong SQL Server Management Studio trước hoặc song song với debug trong code.

## UI renovation plan

- Blazor/Radzen là frontend chính và execution authority hiện tại. Trước mọi thay đổi trong `src/Frontend/Blazor/`, phải đọc và cập nhật `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md`.
- `docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md` là hồ sơ lưu trữ của React POC. Không thêm lại source/runtime React vào `gtas_vpp_fe_react/` nếu owner chưa mở lại phạm vi rõ ràng.
- Đọc `docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md` để chọn đúng nguồn tài liệu, browser tool và QA layer; không cài hoặc gọi nhiều MCP trùng chức năng chỉ để tăng số lượng công cụ.
- Blazor chạy thật trong browser là nguồn quyết định visual cuối. Figma là nơi nghiên cứu/prototype để owner duyệt; React phụ chỉ là evidence tham khảo, không phải pixel/route authority.
- Figma có thể import toàn repository để đọc source. Nếu môi trường Figma cần React để dựng code layer, output đó chỉ là design prototype cô lập; không ghi vào dependency host `gtas_vpp_fe_react/`, không coi đó là frontend chính và không tự ghi đè `src/Frontend/Blazor/`.
- Không tạo thêm UI Lab trong repository. Phần đã duyệt phải được triển khai trực tiếp trong `src/Frontend/Blazor/`, dùng API/DTO và database TEST hoặc isolated fixture thật.
- `docs/design/atlas/` là bản Design Atlas 28 màn được đưa vào repository ngày 2026-07-26 (quyết định D5 trong `docs/execution/ATLAS-001.md`). Đây là **design reference đóng băng và read-only**, giữ để bảo toàn nguồn của 16 hình giao diện trong luận văn; không phải UI Lab và không được dùng để phát triển tính năng mới. Chỉ sửa khi owner duyệt một thay đổi thiết kế, và phải sửa kèm route Blazor tương ứng cùng route ledger.
- Kế hoạch triển khai toàn bộ Atlas sang frontend nằm trong `docs/execution/ATLAS-001.md`; đọc mục 1 của file đó để biết thứ tự thẩm quyền khi luận văn, Atlas, backend và frontend mâu thuẫn nhau.
- Sau mỗi vòng người dùng duyệt hoặc từ chối một route, cập nhật route ledger, decision/learning log và retrofit queue trong living plan trước khi tiếp tục.
- Giữ một kiến trúc global `InteractiveServer`; không thêm `@rendermode` cục bộ nếu chưa có quyết định kiến trúc mới.

### Công cụ UI/UX cho AI agent

- Tra Microsoft Learn MCP trước cho .NET, Blazor, ASP.NET Core, Aspire và tài liệu Microsoft; tra Radzen MCP trước khi sửa Radzen component/API; dùng Context7 cho package bên thứ ba khi tài liệu chính chủ chưa đủ.
- Tài liệu vận hành Radzen MCP chính thức: https://www.radzen.com/blazor-mcp/documentation; khi cần đối chiếu markup/version, kiểm tra thêm package `Radzen.Blazor` thực tế trong repository.
- Hạn mức Radzen MCP hiện tại là 50 request trong 15 ngày. Nếu Radzen MCP báo hết quota, key lỗi hoặc không còn truy cập được thì dừng toàn bộ công việc ngay và yêu cầu người dùng bổ sung key mới; không âm thầm làm tiếp bằng suy đoán.
- Khi gọi Radzen MCP, dùng đúng tên component như `RadzenDataGrid`, `RadzenTabs`, `RadzenPanelMenu`; nêu rõ model, field, quan hệ dữ liệu, binding/event và hành vi cần đạt. Tránh câu hỏi rộng kiểu "làm cả trang" vì kết quả kém chính xác và tốn quota.
- Chia truy vấn Radzen theo từng component hoặc vấn đề có thể kiểm chứng: lấy API/pattern cần thiết, đối chiếu source hiện tại, triển khai rồi test trước khi hỏi phần tiếp theo. Tái sử dụng kết quả đã có trong cùng task và chỉ gọi lại khi còn điểm chưa rõ.
- Mẫu truy vấn ưu tiên: `<Tên component> + <bối cảnh/model hiện tại> + <hành vi cần đạt> + <ràng buộc accessibility/responsive/render mode>`. Ví dụ: `RadzenTabs: giữ label không xuống dòng, tablist cuộn ngang ở zoom 400%, full hitbox hover/focus, InteractiveServer`.
- Radzen MCP là nguồn cho API, property, event và pattern đúng phiên bản; không thay thế việc đọc DOM/CSS của repository. Kết quả cuối phải được xác nhận bằng build, test và route thật trong browser.
- Dùng Playwright MCP cho DOM/accessibility snapshot, interaction, screenshot, console và network của route thật. Dùng Chrome DevTools MCP khi cần trace performance hoặc debug sâu; chỉ kết nối browser/profile TEST riêng, không chứa tài khoản cá nhân, cookie hoặc secret.
- Figma MCP chỉ bổ sung flow, token và design context. Với GTAS VPP, code/browser đã duyệt vẫn thắng Figma khi có khác biệt.
- Screenshot thông thường chỉ là evidence. Chỉ gọi là visual regression khi đã có baseline được người dùng duyệt, môi trường/browser/viewport ổn định và phép so sánh tự động.
- Accessibility phải kết hợp axe tự động với kiểm tra keyboard/focus và review thủ công; không xem một lần scan axe hoặc Lighthouse là bằng chứng WCAG đầy đủ.

## Build và kiểm thử

Chạy tối thiểu các lệnh phù hợp với phạm vi thay đổi:

```powershell
dotnet build gtas_vpp.sln -c Release
dotnet test tests/Backend.UnitTests/gtas_vpp_be.Tests.csproj -c Release
dotnet test tests/Frontend.UnitTests/gtas_vpp_fe.Tests.csproj -c Release
```

Lệnh chuẩn cho AI agent:

```powershell
./scripts/gtas.cmd test
./scripts/gtas.cmd verify
```

Không coi số lượng test lịch sử là invariant (giá trị luôn đúng); luôn chạy lại test phù hợp và báo số liệu từ output hiện tại sau khi sửa code.

## Luận văn

- Nguồn luận văn chuẩn duy nhất là `LVTN/NguyenAnNam_DH52201078.docx`, SHA-256 `103C4AB3054F518A9C5A065B4ECC5802724205F9FDD14D7E1CC7A0E6AAA6B5A0`; file này được nhập byte-for-byte từ bản v5 có bìa đã được owner duyệt ngày 27/07/2026.
- Không duy trì bản `working` hoặc checkpoint Word trong Git. Trước khi sửa, sao chép nguồn chuẩn sang `LVTN/checkpoints/local_review.docx` hoặc thư mục tạm; chỉ thay nguồn chuẩn sau khi owner duyệt.
- Tài liệu định dạng nằm tại `LVTN/references/formatting/MAU_LVTN_2026.pdf` và `LVTN/references/formatting/Luận văn tốt nghiệp (1).docx`.
- Trang bìa dùng đúng bố cục từ file `NguyenAnNam_DH52201078_cover_only.docx`: page border chỉ trang 1 (`w:display="firstPage"`), màu đen, và tên đề tài bắt buộc ngắt dòng sau `XÂY DỰNG WEBSITE QUẢN LÝ`.
- Mục lục, danh mục hình, tài liệu tham khảo và các tham chiếu nội bộ phải là liên kết có thể bấm; trước khi bàn giao phải cập nhật field và kiểm tra số trang.
- Sơ đồ kỹ thuật giữ cả `.puml` và `.svg`, phù hợp in đen trắng. Đọc `LVTN/diagrams/README.md` trước khi sửa.
- Đọc `LVTN/README.md` và dùng script trong `LVTN/tooling/`; lưu checkpoint review, render và contact sheet vào thư mục bị Git ignore.
- Mỗi lần sửa Word phải render và kiểm tra trực quan các trang bị tác động trước khi thay bản final.

## Bảo mật và vệ sinh Git

- Không commit `.env`, secret, mật khẩu, token, connection string cá nhân hoặc tài khoản test.
- Không commit `bin/`, `obj/`, `TestResults/`, log, cache, JDK/PlantUML/LibreOffice tải cục bộ, ảnh audit tự động hoặc output render tạm.
- Không commit checkpoint Word trung gian; trong Git chỉ giữ nguồn chuẩn đã được owner duyệt theo `LVTN/README.md`.
- Không xóa file Word, script tooling, `.puml`, `.svg`, screenshot luận văn hay hướng dẫn AI chỉ vì chúng không tham gia build ứng dụng.
- Trước khi commit, chạy `git diff --check`, xem toàn bộ `git status` và chỉ stage đúng phạm vi công việc.
