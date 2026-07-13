# Công cụ xử lý luận văn Word

Thư mục này giữ các script có thể tái sử dụng để AI hoặc người chỉnh sửa tiếp tục hoàn thiện luận văn. Output render, profile LibreOffice và contact sheet là dữ liệu tạm, không commit vào Git.

## File chuẩn

- Bản gốc, chỉ đọc: `../NguyenAnNam_DH52201078.docx`
- Bản đang làm việc: `../NguyenAnNam_DH52201078_working.docx`
- Bản bàn giao: `../checkpoints/99_final.docx`
- Mẫu định dạng: `../MAU_LVTN_2026.pdf`
- File Word tham khảo: `../Luận văn tốt nghiệp (1).docx`

Các đường dẫn trong lệnh dưới đây được tính từ thư mục gốc repository.

## Quy trình đề xuất

1. Sao lưu bản working hoặc tạo checkpoint local trước khi sửa.
2. Dùng script định dạng với input và output riêng; không chạy thử trực tiếp lên bản gốc.
3. Cập nhật field và xuất PDF bằng Microsoft Word.
4. Render PDF thành ảnh, xem các trang bị tác động và tạo contact sheet nếu cần.
5. Chạy các script audit trước khi thay `99_final.docx`.

Ví dụ:

```powershell
python LVTN/tooling/apply_template_format_2026.py `
  LVTN/NguyenAnNam_DH52201078_working.docx `
  LVTN/checkpoints/local_review.docx

powershell -ExecutionPolicy Bypass -File LVTN/tooling/update_render_word.ps1 `
  -DocxPath LVTN/checkpoints/local_review.docx `
  -PdfPath tmp/local_review.pdf `
  -UpdateFields

python LVTN/tooling/render_pdf_pages.py tmp/local_review.pdf tmp/local_review_pages `
  --first 1 --last 76 --scale 2

python LVTN/tooling/make_contact_sheets.py tmp/local_review_pages
python LVTN/tooling/audit_template_compliance_2026.py LVTN/checkpoints/local_review.docx
python LVTN/tooling/audit_99_final.py
```

`finalize_navigation.py` nhận đường dẫn source và output theo thứ tự. Nếu không truyền tham số, script dùng bản working làm source và `99_final.docx` làm output:

```powershell
python LVTN/tooling/finalize_navigation.py `
  LVTN/NguyenAnNam_DH52201078_working.docx `
  LVTN/checkpoints/99_final.docx
```

## Phụ thuộc

- Python với `python-docx`, `lxml`, `Pillow` và `pypdfium2`, tùy script.
- Microsoft Word trên Windows cho `update_render_word.ps1`.
- PlantUML và Java cài ở máy local để render sơ đồ; không đưa binary hoặc JAR vào repository.

Quy tắc định dạng đã chốt nằm trong `template_compliance_mau_2026.md`; quy tắc sơ đồ nằm tại `../diagrams/README.md`.
