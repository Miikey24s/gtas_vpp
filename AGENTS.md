# Hướng dẫn làm việc với repository GTAS VPP

Áp dụng cho AI và lập trình viên làm việc trong toàn bộ repository.

## Phạm vi và cấu trúc chuẩn

- Backend nằm trong `gtas_vpp_be/`; frontend nằm trong `gtas_vpp_fe/`.
- Shared DTO duy nhất là `gtas_vpp_be/gtas_vpp_shared`. Không tạo lại `gtas_vpp_fe/gtas_vpp_shared`.
- Không sửa API, database hoặc nghiệp vụ chỉ để làm cho nội dung luận văn khớp; luận văn phải mô tả đúng source thực tế.
- Khi sửa UI, đọc và tuân thủ `.codexrules` cùng `.github/copilot-instructions.md`.
- Khi sửa stored procedure, ưu tiên kiểm tra câu lệnh trong SQL Server Management Studio trước hoặc song song với debug trong code.

## Build và kiểm thử

Chạy tối thiểu các lệnh phù hợp với phạm vi thay đổi:

```powershell
dotnet build gtas_vpp.sln -c Release
dotnet test gtas_vpp_be.Tests/gtas_vpp_be.Tests.csproj -c Release
dotnet test gtas_vpp_fe.Tests/gtas_vpp_fe.Tests.csproj -c Release
```

Mốc kiểm tra gần nhất của repository sạch là 132 backend test và 26 frontend test đều pass. Không xem con số này là thay thế cho việc chạy lại test sau khi sửa code.

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
