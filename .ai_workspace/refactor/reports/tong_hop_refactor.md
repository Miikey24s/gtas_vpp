# Tổng hợp quá trình Refactor dự án GTAS VPP (Dành cho Sinh viên năm 4)

Tài liệu này tổng hợp lại toàn bộ quá trình tái cấu trúc (refactoring) mã nguồn của dự án GTAS VPP từ Phase 1 đến Phase 7. Quá trình này giúp nâng cao chất lượng code, cải thiện bảo mật, dễ bảo trì hơn và chuẩn bị sẵn sàng cho môi trường chạy thực tế (Production).

Dưới đây là các bước đã thực hiện, được giải thích một cách đơn giản cùng các thuật ngữ chuyên ngành.

---

## Phase 1: Dọn dẹp mã nguồn (Cleanup)
Bước đầu tiên của mọi đợt refactor luôn là "quét nhà", loại bỏ những thứ không cần thiết để dễ nhìn hơn.
* **Xóa code thừa:** Xóa bỏ các file mẫu (template) sinh ra tự động lúc khởi tạo dự án mà không dùng tới (ví dụ: `WeatherForecast`).
* **Sửa lỗi chính tả & Quy chuẩn tên:** Đổi tên class sai chính tả từ `BussinessService` thành `BusinessService`. Việc đặt tên chuẩn xác là rất quan trọng khi làm việc nhóm.
* **Tổ chức lại thư mục:** Di chuyển các đối tượng truyền dữ liệu (DTO - Data Transfer Object) về đúng nơi quy định (`gtas_vpp_shared`) để Frontend và Backend có thể dùng chung dễ dàng.

## Phase 2: Viết Test và Tái cấu trúc tầng Service (Unit Tests & Service Layer Refactor)
Trước khi đập đi xây lại (refactor), chúng ta phải có một "tấm lưới an toàn" (safety net) để đảm bảo không làm hỏng các tính năng đang chạy. Tấm lưới đó chính là Unit Test.
* **Unit Tests (Kiểm thử mức đơn vị):** Viết test cho các hàm logic dùng các thư viện như `Moq` (để tạo đối tượng giả/mock) và `InMemory DB` (Database chạy trên RAM, test siêu nhanh).
* **Dependency Injection (DI) & Interface:** Thay vì tạo đối tượng trực tiếp (`new Class()`), dự án được đổi sang dùng Interface (như `IEnvironmentResolver`) và được inject (tiêm) vào qua constructor. Điều này giúp giảm sự phụ thuộc (loose coupling) giữa các thành phần.
* **Repository Pattern (Mẫu kho lưu trữ):** Tách toàn bộ các thao tác làm việc với Database (CRUD: Thêm, Đọc, Sửa, Xóa) ra một lớp chung gọi là `GenericRepository`. Tầng Controller (tiếp nhận request) giờ đây không cần biết lệnh SQL viết như thế nào nữa, chỉ cần gọi hàm từ Repository.

## Phase 3 & 4: Xử lý lỗi và Ghi log (Middleware & Logging)
* **ExceptionHandlingMiddleware:** Thêm một Middleware (Phần mềm trung gian) đứng chặn mọi Request. Nếu hệ thống xảy ra lỗi (Exception), Middleware này sẽ "bắt" lấy và trả về một thông báo lỗi chuẩn mực theo định dạng **RFC 7807 ProblemDetails** (chuẩn RESTful API), thay vì quăng nguyên một đống lỗi kỹ thuật đỏ ngòm ra màn hình (tránh lộ cấu trúc code cho hacker).
* **Serilog (Ghi log hệ thống):** Tích hợp Serilog để ghi lại lịch sử hoạt động của server (Ai gọi API gì? Bị lỗi ở đâu?) ra một file text. Điều này cực kỳ quan trọng để debug khi phần mềm đã đem đi triển khai (deploy).
* **Quản lý Cấu hình (Configuration):** Đẩy các cấu hình (như thông tin kết nối Jira) ra file `appsettings.json` thay vì ghi chết (hardcode) trong mã nguồn. Code không nên chứa các thông số có thể thay đổi tùy môi trường.

## Phase 5: Vá lỗi bảo mật & Dọn dẹp (Security Fixes)
Đây là giai đoạn cực kỳ quan trọng để chống lại các cuộc tấn công cơ bản.
* **Chống SQL Injection:** Kỹ thuật chèn mã SQL độc hại. Thay vì ghép chuỗi câu lệnh SQL trực tiếp từ dữ liệu người dùng nhập (rất nguy hiểm), code đã được sửa lại để dùng Parameter (tham số hóa). Entity Framework sẽ tự động làm sạch (sanitize) dữ liệu này.
* **CORS Policy (Chính sách chia sẻ tài nguyên chéo nguồn):** Chặn các trang web lạ gọi trộm API của hệ thống. Chỉ cấp quyền (`AllowedOrigins`) cho các địa chỉ Frontend hợp lệ (ví dụ: `https://localhost:5001`).
* **Bảo mật Cookie:** Cấu hình Cookie trên Frontend bật các cờ `HttpOnly` (chống JavaScript đọc trộm cookie) và `Secure` (chỉ gửi cookie qua đường truyền mã hóa HTTPS).
* **Viết thêm Test:** Bổ sung test cho các Controller để kiểm tra xem hệ thống có trả về mã 401 (Unauthorized - Chưa đăng nhập) hay 403 (Forbidden - Không có quyền) đúng như thiết kế hay không.

## Phase 6: Khóa chặt các cửa ngõ (Final Fixes)
* **Xóa API truy vấn tự do:** Gỡ bỏ hoàn toàn tính năng `SQLController.Query`. Việc cho phép Frontend gửi một câu SQL tự do xuống Backend chạy là cực kỳ rủi ro. Thay vào đó, hệ thống chỉ cho phép chạy các thủ tục lưu trữ (Stored Procedure) nằm trong danh sách trắng (Whitelist) đã được phê duyệt.
* **Dọn dẹp cảnh báo (Warnings):** Sửa tất cả các cảnh báo của trình biên dịch liên quan đến biến có thể bị `null`. Một dự án chất lượng cao thì số cảnh báo (warnings) nên là 0.

## Phase 7: Đánh bóng và Hoàn thiện (Final Polish)
* **Sửa lỗi Memory Leak (Rò rỉ bộ nhớ) trên Blazor (Frontend):** Khi người dùng chuyển trang, các component cũ bị hủy nhưng đôi khi chúng vẫn "dính" lấy các sự kiện (ví dụ: theo dõi sự kiện đổi URL `LocationChanged`). Nếu không gỡ ra (`-=`), RAM sẽ bị đầy dần. Giải pháp là implement interface `IDisposable` và gỡ sự kiện trong hàm `Dispose()`.
* **Bảo mật JWT Key (Json Web Token):** JWT Key là "chìa khóa vạn năng" dùng để mã hóa và xác thực người dùng. Code cũ đang lưu trực tiếp chuỗi này trong source code (Hardcode) - điều này là **tối kỵ**. Đã refactor để ép buộc trên môi trường Production phải truyền JWT Key qua biến môi trường (Environment Variable), nếu không truyền hệ thống sẽ báo lỗi và dừng chạy ngay lập tức.
* **Kết quả cuối cùng:** Dự án hoàn thành xuất sắc với **0 errors, 0 warnings, 27/27 tests đều PASS**. Được đánh dấu `✅ APPROVED FOR PRODUCTION`.

---
**Tóm tắt bài học:** Quá trình refactor không phải là làm thay đổi tính năng của ứng dụng (người dùng không thấy gì khác biệt), mà là quá trình nâng cấp "nội thất" bên trong: làm cho mã nguồn sạch hơn, an toàn hơn trước hacker, chạy ổn định hơn (không rò rỉ RAM), và để những lập trình viên vào sau có thể dễ dàng đọc hiểu và phát triển tiếp.