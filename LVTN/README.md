# Luận văn GTAS VPP

Thư mục này chỉ giữ một nguồn luận văn chuẩn và các tài sản có thể tái sử dụng.

## Nguồn chuẩn

- Luận văn hiện hành: `NguyenAnNam_DH52201078.docx`.
- SHA-256: `A669C0E7B7E8416045AFDE72B97655FB20B325330802BF253075AF7209505BC5`.
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

Phần slide bảo vệ đang tiếp tục trong `presentation/`. Chỉ giữ deck hiện hành và tooling có thể tái sử dụng; bản nháp cũ, render, montage, layout JSON và báo cáo inspect là đầu ra tạm, không đưa vào Git.
