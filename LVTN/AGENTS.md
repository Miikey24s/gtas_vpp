# LVTN instructions

Áp dụng cho toàn bộ `LVTN/`.

- Đọc root `AGENTS.md` và `LVTN/README.md` trước khi làm việc.
- Dùng repo skill `.agents/skills/gtas-vpp-thesis-docx/` và documents skill khi sửa `.docx`.
- Nguồn chuẩn duy nhất là `NguyenAnNam_DH52201078.docx`; không sửa trực tiếp trước khi có review copy.
- Tạo bản review trong `checkpoints/` hoặc thư mục temp đã ignore; chỉ thay nguồn chuẩn sau owner approval.
- Trang bìa giữ page border chỉ ở trang 1 (`w:display="firstPage"`) và tên đề tài ngắt dòng sau `XÂY DỰNG WEBSITE QUẢN LÝ`.
- Mục lục, danh mục hình, tài liệu tham khảo và internal reference phải là field/link có thể bấm; cập nhật field rồi kiểm tra pagination trước bàn giao.
- Tài liệu định dạng chuẩn nằm trong `references/formatting/`; không tự suy luận format từ một trang đơn lẻ.
- Không sửa code/API/database để ép khớp luận văn; luận văn phải mô tả source thực tế.
- Khi sửa Word, dùng documents skill, cập nhật field bằng Word, render và kiểm tra trực quan toàn bộ trang bị tác động.
- Giữ `.puml` + `.svg`, tài liệu formatting, tooling và screenshot còn được luận văn tham chiếu.
- Không commit bản ký, hồ sơ cá nhân, render, contact sheet, log hoặc checkpoint trung gian.

Kiểm tra tối thiểu:

```powershell
./scripts/gtas.cmd preflight -Scope thesis
python LVTN/tooling/check_thesis.py
```
