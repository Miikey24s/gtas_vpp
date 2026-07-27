# Công cụ xử lý luận văn Word

Thư mục này chỉ giữ các script còn tái sử dụng cho audit, render và cập nhật field. Các script dựng bản `candidate`, `v2`, `v3`, `v4` và LEAN cũ đã bị loại vì gắn chặt với tài liệu trung gian không còn tồn tại.

## File chuẩn

- Luận văn chuẩn, không sửa trực tiếp: `../NguyenAnNam_DH52201078.docx`.
- Mẫu định dạng: `../references/formatting/MAU_LVTN_2026.pdf`.
- Word tham khảo: `../references/formatting/Luận văn tốt nghiệp (1).docx`.
- Bản review local: `../checkpoints/local_review.docx` — Git bỏ qua toàn bộ thư mục `checkpoints/`.

## Quy trình an toàn

```powershell
Copy-Item LVTN/NguyenAnNam_DH52201078.docx LVTN/checkpoints/local_review.docx

powershell -ExecutionPolicy Bypass -File LVTN/tooling/update_render_word.ps1 `
  -DocxPath LVTN/checkpoints/local_review.docx `
  -PdfPath tmp/local_review.pdf `
  -UpdateFields

python LVTN/tooling/render_pdf_pages.py tmp/local_review.pdf tmp/local_review_pages `
  --first 1 --last 88 --scale 2

python LVTN/tooling/make_contact_sheets.py tmp/local_review_pages
python LVTN/tooling/audit_template_compliance_2026.py LVTN/checkpoints/local_review.docx
python LVTN/tooling/check_thesis.py LVTN/checkpoints/local_review.docx
```

Sau khi xem toàn bộ trang render, chỉ thay file chuẩn khi owner đã duyệt bản review.

## Script còn giữ

| Nhóm | Script |
|---|---|
| Audit | `audit_thesis.py`, `check_thesis.py`, `audit_template_compliance_2026.py`, `audit_figure_list_format.py`, `inspect_front_format.py` |
| Word fields | `update_word_all_fields.ps1`, `update_word_toc.ps1`, `update_word_figure_pages.ps1`, `update_render_word.ps1` |
| Liên kết | `link_docx_references.py` |
| Render | `render_pdf_pages.py`, `make_contact_sheets.py` |
| Định dạng | `apply_template_format_2026.py`, `template_compliance_mau_2026.md` |
| Ảnh giao diện | `capture_atlas_thesis_screens.cjs` |

Phụ thuộc Python gồm `python-docx`, `lxml`, `Pillow` và `pypdfium2`. Render/update field cuối dùng Microsoft Word trên Windows. PlantUML/Java chỉ cài local, không đưa binary vào repository.
