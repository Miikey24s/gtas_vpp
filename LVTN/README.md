# Luận văn GTAS VPP

Thư mục này chỉ giữ một nguồn luận văn chuẩn và các tài sản có thể tái sử dụng.

## Nguồn chuẩn

- Luận văn hiện hành: `NguyenAnNam_DH52201078.docx`.
- SHA-256: `103C4AB3054F518A9C5A065B4ECC5802724205F9FDD14D7E1CC7A0E6AAA6B5A0`.
- Nguồn được owner duyệt: `NguyenAnNam_DH52201078_final_v5_fixed_cover.docx`, ngày 27/07/2026.
- Không duy trì thêm bản `working` hoặc checkpoint Word trong Git. Khi cần sửa, tạo bản review trong `checkpoints/` hoặc thư mục tạm; hai vị trí này bị Git bỏ qua.

## Cấu trúc

| Thư mục | Mục đích |
|---|---|
| `administrative/` | Biểu mẫu hành chính được phép lưu trong repository. |
| `data/` | Dữ liệu nguồn local phục vụ demo; file thô có thông tin vận hành bị Git bỏ qua. |
| `diagrams/` | Source `.puml`, bản in `.svg` và quy ước sơ đồ. |
| `evidence/` | Trạng thái evidence còn giá trị và chưa thể tái tạo hoàn toàn từ source. |
| `private-local/` | File ký, hồ sơ cá nhân hoặc tài liệu chỉ lưu trên máy; toàn thư mục bị Git bỏ qua. |
| `references/formatting/` | PDF mẫu và Word tham khảo định dạng. |
| `screenshots/` | Hình giao diện đang được luận văn hoặc Atlas tham chiếu. |
| `tooling/` | Script audit, render, cập nhật field và tạo contact sheet còn tái sử dụng. |

## Quy trình sửa Word

1. Sao chép file chuẩn sang `LVTN/checkpoints/local_review.docx`.
2. Chỉ chỉnh bản review, không ghi trực tiếp lên file chuẩn.
3. Cập nhật field bằng Microsoft Word, render toàn bộ và kiểm tra trực quan.
4. Chạy `python LVTN/tooling/check_thesis.py <đường-dẫn-docx>`.
5. Chỉ thay file chuẩn sau khi owner duyệt bản review.

Phần slide bảo vệ sau này dùng `presentation/`, nhưng chưa tạo thư mục hoặc deck cho đến khi owner mở phạm vi.
