# Cấu trúc chuẩn Slide Bảo vệ Luận văn Tốt nghiệp CNTT (10–15 Slides)

Thời lượng bảo vệ tiêu chuẩn: **10 – 15 phút** thuyết trình + 10 phút hỏi đáp hội đồng.

---

## 1. Khung phân bổ Slide (Deck 10–15 Slides)

| Slide # | Chủ đề | Mục tiêu & Nội dung chính | Thời lượng gợi ý |
| :---: | :--- | :--- | :---: |
| **01** | **Trang bìa (Title)** | Tên đề tài, sinh viên, GVHD, Hội đồng, Trường/Khoa. | 30s |
| **02** | **Đặt vấn đề & Bối cảnh thực tế** | Nêu rõ nỗi đau (pain points): quản lý thủ công qua bảng tính, phân mảnh, thất thoát, không đồng bộ dữ liệu. | 1.5 min |
| **03** | **Mục tiêu & Phạm vi đề tài** | Chuẩn hóa quy trình đặt hàng theo kỳ, tự động hóa duyệt, chốt kỳ và báo cáo; giới hạn phạm vi rõ ràng. | 1.0 min |
| **04** | **Kiến trúc hệ thống (Architecture)** | Mô hình phân tầng (Blazor $\rightarrow$ Web API $\rightarrow$ Application Services $\rightarrow$ SQL Server / EF Core). Giải thích lý do chọn kiến trúc. | 1.5 min |
| **05** | **Thiết kế CSDL & Mô hình dữ liệu** | Các thực thể cốt lõi (Kỳ đặt hàng, Đơn hàng, Chi tiết đơn, Bảng giá, Phân quyền). Ràng buộc toàn vẹn dữ liệu. | 1.5 min |
| **06–08** | **Tính năng nổi bật & Quy trình nghiệp vụ** | - Quy trình đặt hàng theo kỳ & lịch mở/đóng.<br>- Cơ chế duyệt đơn bổ sung & kiểm soát ngoại lệ.<br>- Quy trình chốt kỳ, áp giá NCC và lập báo cáo. | 3.0 min |
| **09** | **Các giải pháp kỹ thuật đặc biệt** | Kiểm tra quyền nhiều lớp (Blazor + API), chống trùng lặp dữ liệu, concurrency check, tối ưu truy vấn. | 1.5 min |
| **10** | **Kịch bản Demo hệ thống (Demo Flow)** | Tóm tắt các luồng demo thực tế (Role Nhân viên $\rightarrow$ Quản lý $\rightarrow$ Admin). | 1.0 min |
| **11** | **Kết quả đạt được & So sánh** | So sánh trước và sau khi triển khai hệ thống (thời gian xử lý, độ chính xác, tính minh bạch). | 1.0 min |
| **12** | **Hạn chế & Hướng phát triển** | Nêu trung thực các điểm chưa làm và lộ trình mở rộng tương lai (tích hợp nhà cung cấp ngoài, mobile app,...). | 1.0 min |
| **13** | **Kết luận & Cảm ơn** | Lời cảm ơn hội đồng, thầy cô hướng dẫn và sẵn sàng trả lời câu hỏi. | 30s |

---

## 2. Tiêu chuẩn thuyết trình & Bố cục Visual

1. **Nguyên tắc 1 ý/slide**: Mỗi slide có 1 takeaway nổi bật. Người nghe liếc qua hiểu ngay thông điệp.
2. **Hạn chế chữ, tăng trực quan**:
   - Thay vì đoạn văn dài $\rightarrow$ Dùng 3–4 card/hộp tóm tắt.
   - Sử dụng sơ đồ luồng (flow diagram), ảnh chụp màn hình UI thực tế, bảng biểu so sánh ngắn gọn.
3. **Màu sắc & Typography**:
   - Sử dụng bảng màu chuyên nghiệp (tối đa 3 màu chủ đạo: Nền sáng/tối đồng nhất, màu nhận diện thương hiệu, màu nhấn highlight).
   - Font chữ không chân hiện đại (Inter, Roboto, Segoe UI). Tiêu đề 28–36pt, nội dung 14–20pt.
4. **Speaker Notes**:
   - Phải có sẵn kịch bản nói cho từng slide để kiểm soát tốc độ nói và không bị vấp khi trình bày trước hội đồng.
