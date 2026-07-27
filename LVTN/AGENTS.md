# LVTN instructions

Áp dụng cho toàn bộ `LVTN/`.

- Đọc root `AGENTS.md` và `LVTN/README.md` trước khi làm việc.
- Nguồn chuẩn duy nhất là `NguyenAnNam_DH52201078.docx`; không sửa trực tiếp trước khi có review copy.
- Tạo bản review trong `checkpoints/` hoặc thư mục temp đã ignore; chỉ thay nguồn chuẩn sau owner approval.
- Không sửa code/API/database để ép khớp luận văn; luận văn phải mô tả source thực tế.
- Khi sửa Word, dùng documents skill, cập nhật field bằng Word, render và kiểm tra trực quan toàn bộ trang bị tác động.
- Giữ `.puml` + `.svg`, tài liệu formatting, tooling và screenshot còn được luận văn tham chiếu.
- Không commit bản ký, hồ sơ cá nhân, render, contact sheet, log hoặc checkpoint trung gian.

Kiểm tra tối thiểu:

```powershell
./scripts/gtas.cmd preflight -Scope thesis
python LVTN/tooling/check_thesis.py
```
