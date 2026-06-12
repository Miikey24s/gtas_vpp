# HƯỚNG DẪN LÀM LẠI LUẬN VĂN GTAS VPP

Tài liệu này được lập sau khi đối chiếu:

- `MAU_LVTN_2026.pdf`: quy định và bố cục LVTN năm 2026.
- `NguyenAnNam_DH52201078.docx`: bản Word đang sửa từ luận văn khóa trước.
- Mã nguồn project `gtas_vpp`: backend, frontend, cơ sở dữ liệu, kiểm thử và triển khai.

## 1. Kết luận quan trọng

Không nên tiếp tục sửa bản Word bằng cách tìm và thay tên đề tài. Bản hiện tại chỉ đổi trang bìa, còn hầu như toàn bộ nội dung, hình, bảng và tài liệu tham khảo vẫn thuộc đề tài **website đi chung xe**.

Có thể giữ lại bộ khung Word như section, Heading và vị trí mục lục, nhưng phải viết lại toàn bộ nội dung từ Chương 1 đến hết tài liệu tham khảo dựa trên project GTAS VPP. Không sử dụng lại đoạn văn, hình, bảng use case hoặc kết quả thử nghiệm của sinh viên trước.

Tên đề tài nên xin GVHD xác nhận theo một trong hai cách sau:

1. **XÂY DỰNG HỆ THỐNG QUẢN LÝ YÊU CẦU VĂN PHÒNG PHẨM TẠI CÔNG TY CỔ PHẦN QUỐC TẾ PHONG PHÚ**.
2. **XÂY DỰNG WEBSITE QUẢN LÝ YÊU CẦU VÀ TỔNG HỢP VĂN PHÒNG PHẨM PHONG PHÚ**.

Tên thứ nhất sát nghiệp vụ của project hơn. Không nên dùng cụm “quản lý kho” hoặc “cấp phát” vì project hiện không có nghiệp vụ tồn kho, nhập kho và xuất kho.

## 2. Các lỗi phải sửa trong file Word hiện tại

| Vị trí | Hiện trạng | Cách xử lý |
|---|---|---|
| Bìa | Tên đề tài đã đổi nhưng chưa có khung trang | Làm lại theo trang 2-3 của mẫu 2026 |
| GVHD | Bìa ghi ThS. Viên Thanh Nhã, lời cảm ơn ghi ThS. Bùi Nhật Bằng | Dùng đúng một tên GVHD đã được xác nhận |
| Người viết | Lời cảm ơn dùng “nhóm thực hiện” nhưng bìa chỉ có Nguyễn An Nam | Đổi thành “em” hoặc “sinh viên thực hiện” |
| Nội dung | Chương 1-5 đang nói về đi chung xe, tài xế, hành khách, Grab, Be, MongoDB | Xóa và viết lại theo GTAS VPP |
| Công nghệ | Đang ghi NestJS, Next.js, MongoDB, Mapbox | Thay bằng .NET 10, ASP.NET Core Web API, Blazor Server, EF Core, SQL Server, Radzen |
| Header/footer | Footer vẫn ghi “XÂY DỰNG WEBSITE ĐI CHUNG XE” | Thay toàn bộ footer bằng tên đề tài mới |
| Hình ảnh | Có 51 hình của hệ thống đi chung xe | Xóa toàn bộ, chụp và vẽ lại từ project này |
| Bảng | 20 bảng use case và 2 bảng kiểm thử thuộc hệ thống cũ | Xóa và xây dựng bảng mới |
| Mục lục hình | Không dùng trường Caption/SEQ đúng chuẩn | Tạo lại bằng Insert Caption và Table of Figures |
| Kiểu chữ mặc định | Document default vẫn là Arial 11 dù phần lớn thân bài được định dạng trực tiếp TNR 13 | Sửa style Normal và các Heading, không định dạng thủ công từng đoạn |
| Heading 4 | Chưa đúng yêu cầu gạch dưới | Sửa style Heading 4: TNR 13, thường, gạch dưới |
| Thuộc tính file | Tác giả gốc vẫn là “Tuấn Phạm” | Dùng Inspect Document để xóa metadata của tài liệu cũ |

File hiện có khoảng 100 trang, 13.809 từ, 53 tệp hình và 22 bảng. Số trang này không có giá trị cho luận văn mới vì nội dung cũ phải được thay thế.

## 3. Thông tin chuẩn của project để viết luận văn

### 3.1. Bài toán

GTAS VPP là hệ thống web nội bộ hỗ trợ nhân viên/phòng ban gửi yêu cầu văn phòng phẩm theo kỳ, theo dõi lịch sử yêu cầu, tổng hợp nhu cầu, xử lý đơn bổ sung sau hạn, quản lý danh mục và bảng giá, đóng kỳ và phân quyền sử dụng.

Hệ thống hướng tới Công ty Cổ phần Quốc tế Phong Phú. Dữ liệu mẫu trong project sử dụng mã công ty `77500` và các phòng ban như IT, HCQT, TCKT, Purchasing, KD1, QA.

### 3.2. Nhóm người dùng

- **Nhân viên/người yêu cầu**: đăng nhập, xem danh mục, tạo đơn định kỳ, sao chép đơn kỳ trước, tạo đơn bổ sung, chỉnh sửa/hủy đơn hợp lệ, xem lịch sử.
- **Người quản lý phòng ban**: xem và tổng hợp các đơn thuộc phòng ban nếu được cấp quyền.
- **Quản trị viên/người duyệt**: xem tổng hợp toàn hệ thống, duyệt hoặc từ chối đơn bổ sung, đóng kỳ, quản lý danh mục, giá, phòng ban, người dùng và quyền.
- **Hệ thống**: xác định kỳ, kiểm tra hạn, kiểm tra trạng thái, chụp giá, ghi log và ngăn thao tác không hợp lệ.

Không nên mô tả “Trưởng phòng” như một role cố định trong code. Hệ thống cấp quyền theo nhóm, trang và component; người có quyền xem tổng hợp phòng ban mới thực hiện được chức năng đó.

### 3.3. Công nghệ thực tế

| Thành phần | Công nghệ trong project |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API |
| Frontend | Blazor Server/Interactive Server Components |
| UI | Radzen.Blazor 10.2.0, CSS tùy chỉnh, giao diện responsive |
| ORM | Entity Framework Core 10 |
| Cơ sở dữ liệu | Microsoft SQL Server 2022 |
| Ánh xạ DTO | Mapster 10 |
| Xác thực | Cookie ở frontend, JWT Bearer ở backend |
| Phân quyền | Policy và ánh xạ nhóm - trang - component |
| Logging | Serilog, log console và file |
| Triển khai | Docker Compose, Nginx reverse proxy, HTTPS, WebSocket |
| Điều phối khi phát triển | .NET Aspire AppHost |
| Kiểm thử | xUnit, Moq, EF Core InMemory/SQLite, Playwright UI tests |

Nguồn đối chiếu chính:

- `gtas_vpp_be/gtas_vpp_be/gtas_vpp_be.csproj`
- `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe.csproj`
- `gtas_vpp_be/gtas_vpp_be/Program.cs`
- `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Program.cs`
- `docker-compose.prod.yml`
- `nginx/gtas-vpp.conf`

### 3.4. Các quy tắc nghiệp vụ phải mô tả đúng

1. Hạn chốt mặc định là **00:00 ngày 5**.
2. Trước ngày 5, kỳ hiện tại của hệ thống là tháng trước; từ ngày 5 trở đi, kỳ hiện tại là tháng đang diễn ra.
3. Mỗi người chỉ có tối đa một đơn thường trong một kỳ.
4. Đơn thường chỉ được tạo cho kỳ đang mở và có trạng thái `Submitted`.
5. Đơn bổ sung chỉ được tạo cho kỳ vừa đóng, tối đa ba đơn/người/kỳ.
6. Không được tạo đơn bổ sung mới khi còn một đơn bổ sung đang chờ duyệt.
7. Đơn bổ sung ban đầu có trạng thái `Pending`; quản trị viên có thể `Approve` hoặc `Reject`.
8. Người dùng chỉ được sửa/hủy đơn của chính mình và chỉ khi trạng thái cho phép.
9. Không được tạo đơn mới cho kỳ đã đóng sổ.
10. Không thể đóng kỳ nếu còn đơn bổ sung chờ duyệt.
11. Khi đóng kỳ, hệ thống dùng bảng giá được chọn hoặc bảng giá mặc định, cập nhật giá cho từng dòng hàng và lưu thông tin người/thời điểm/bảng giá đóng kỳ.
12. Nếu bảng giá thiếu giá của bất kỳ mặt hàng nào trong kỳ, hệ thống không cho đóng kỳ.
13. Mọi thao tác tạo, sửa, hủy, duyệt, từ chối và đóng kỳ đều có log.

Trạng thái đơn:

| Mã | Trạng thái | Ý nghĩa |
|---:|---|---|
| 1 | Submitted | Đơn thường đã gửi hoặc đơn hợp lệ đang hoạt động |
| 4 | Cancelled | Đơn đã hủy |
| 6 | Pending | Đơn bổ sung chờ duyệt |
| 7 | Approved | Đơn bổ sung đã duyệt |
| 8 | Rejected | Đơn bổ sung bị từ chối |

## 4. Dàn ý luận văn đề xuất

Giữ cấu trúc 5 chương của mẫu 2026. Dàn ý dưới đây bám sát mã nguồn hiện có và đủ nội dung để đạt tối thiểu 60 trang nếu có hình, bảng và phân tích đầy đủ.

### Phần đầu quyển

1. Bìa.
2. Tờ giấy trắng.
3. Tờ nhiệm vụ LVTN.
4. Lời cảm ơn.
5. Mục lục nội dung tự động.
6. Mục lục hình ảnh/sơ đồ tự động.
7. Có thể bổ sung danh mục bảng và danh mục từ viết tắt nếu GVHD đồng ý.

### CHƯƠNG 1. GIỚI THIỆU

#### 1.1. Đặt vấn đề, mục tiêu luận văn

**1.1.1. Đặt vấn đề**

Nêu bối cảnh doanh nghiệp có nhiều phòng ban và nhu cầu văn phòng phẩm phát sinh định kỳ. Nếu tiếp nhận yêu cầu qua Excel, biểu mẫu rời hoặc trao đổi thủ công thì khó kiểm soát thời hạn, trùng đơn, thay đổi giá, tổng hợp số lượng và truy vết người thao tác.

Đoạn mở đầu có thể viết theo hướng:

> Trong doanh nghiệp có nhiều phòng ban, nhu cầu sử dụng văn phòng phẩm phát sinh thường xuyên và cần được tổng hợp theo từng kỳ. Việc tiếp nhận yêu cầu bằng các biểu mẫu rời rạc làm tăng thời gian tổng hợp, khó kiểm soát đơn gửi sau hạn, khó theo dõi lịch sử thay đổi và dễ xảy ra sai lệch khi áp dụng giá. Vì vậy, việc xây dựng một hệ thống web tập trung để chuẩn hóa quy trình yêu cầu, phê duyệt, tổng hợp và đóng kỳ là cần thiết.

Không bê nguyên đoạn mẫu trên vào bản cuối. Hãy bổ sung bối cảnh thực tế đã trao đổi với doanh nghiệp/GVHD và viết lại bằng lời của bạn.

**1.1.2. Mục tiêu**

- Xây dựng hệ thống web quản lý yêu cầu văn phòng phẩm theo kỳ.
- Chuẩn hóa quy trình đơn thường và đơn bổ sung.
- Cung cấp danh mục mặt hàng, nhà cung cấp, bảng giá và phòng ban tập trung.
- Hỗ trợ tổng hợp theo người dùng, phòng ban và toàn công ty.
- Hỗ trợ duyệt đơn bổ sung và đóng kỳ theo bảng giá.
- Đảm bảo xác thực, phân quyền và truy vết thao tác.
- Đóng gói hệ thống để có thể triển khai bằng Docker và Nginx.

#### 1.2. Những thách thức cần giải quyết

Nên tách thành các mục:

- 1.2.1. Xác định kỳ và thời hạn theo quy tắc ngày 5.
- 1.2.2. Ngăn trùng đơn và xử lý tranh chấp khi hai yêu cầu được gửi đồng thời.
- 1.2.3. Xây dựng luồng duyệt riêng cho đơn bổ sung.
- 1.2.4. Quản lý nhiều bảng giá và chụp giá tại thời điểm đóng kỳ.
- 1.2.5. Phân quyền chi tiết theo nhóm, trang và thành phần giao diện.
- 1.2.6. Đồng bộ dữ liệu người dùng, công ty và phòng ban.
- 1.2.7. Đảm bảo tính nhất quán dữ liệu bằng transaction, trạng thái và log.
- 1.2.8. Triển khai Blazor Server qua reverse proxy và WebSocket.

#### 1.3. Nội dung và phạm vi thực hiện

**Trong phạm vi:** đăng nhập; danh mục; tạo/sửa/hủy đơn; sao chép kỳ trước; đơn bổ sung; lịch sử; tổng hợp; duyệt; bảng giá; đóng kỳ; phân quyền; biểu đồ dashboard; triển khai.

**Ngoài phạm vi:** quản lý tồn kho; nhập/xuất kho; thanh toán nhà cung cấp; kế toán; quản lý vận chuyển; ứng dụng di động native; tích hợp chữ ký số.

Trang Report hiện mới là giao diện khung và chưa có dữ liệu báo cáo hoàn chỉnh. Không mô tả chức năng xuất báo cáo là đã hoàn thành.

#### 1.4. Kết quả cần đạt

Nên dùng bảng có tiêu chí đo được:

| Kết quả | Tiêu chí đánh giá | Bằng chứng |
|---|---|---|
| Đăng nhập và phân quyền | Người không có quyền không thấy/không truy cập được chức năng | Test và ảnh màn hình |
| Tạo đơn thường | Đúng kỳ, một đơn/người/kỳ, không cho gửi sau hạn | Unit/integration test |
| Đơn bổ sung | Đúng kỳ trước, tối đa 3, có luồng duyệt | Test nghiệp vụ |
| Quản lý danh mục | CRUD mặt hàng, loại, đơn vị, nhà cung cấp, phòng ban | Ảnh giao diện và API |
| Quản lý giá | Nhiều bảng giá, chọn mặc định, clone bảng giá | Test service và giao diện |
| Đóng kỳ | Chặn khi còn đơn chờ duyệt hoặc thiếu giá | Test settlement |
| Tổng hợp | Xem đơn cá nhân, phòng ban, toàn công ty | Ảnh và dữ liệu thử |
| Triển khai | Backend, frontend, SQL Server chạy qua Docker/Nginx | Sơ đồ và cấu hình |

Không tự đặt tiêu chí như “phản hồi dưới 2 giây” hoặc “chịu 1.000 người dùng” nếu chưa thực hiện benchmark/load test.

### CHƯƠNG 2. PHƯƠNG PHÁP THỰC HIỆN

#### 2.1. Các hệ thống/phương pháp tương tự

Khảo sát 2-3 nhóm giải pháp, ví dụ:

- Excel/Google Forms và quy trình duyệt thủ công.
- Module Purchase/Inventory của Odoo hoặc ERPNext.
- Một hệ thống đề nghị mua hàng/văn phòng phẩm nội bộ khác có nguồn công khai.

Mỗi hệ thống phải có: mô tả, chức năng liên quan, ưu điểm, hạn chế và bài học áp dụng. Dùng nguồn chính thức, ghi ngày truy cập và không sao chép nội dung quảng cáo.

#### 2.2. Công nghệ sử dụng

Nên trình bày:

- 2.2.1. .NET 10 và ASP.NET Core Web API.
- 2.2.2. Blazor Server và SignalR.
- 2.2.3. Radzen Blazor.
- 2.2.4. Entity Framework Core và Mapster.
- 2.2.5. SQL Server 2022.
- 2.2.6. Cookie, JWT và policy authorization.
- 2.2.7. Serilog.
- 2.2.8. Docker Compose, Nginx và HTTPS.
- 2.2.9. xUnit và Playwright.

Với mỗi công nghệ, chỉ cần trả lời ba câu: dùng để làm gì, vì sao phù hợp, được đặt ở đâu trong kiến trúc.

#### 2.3. Phân tích yêu cầu

**2.3.1. Yêu cầu chức năng**

Danh sách use case nên làm:

1. Đăng nhập và chọn môi trường Test/Live.
2. Xem danh mục văn phòng phẩm.
3. Tạo đơn yêu cầu định kỳ.
4. Sao chép mặt hàng từ đơn kỳ trước.
5. Chỉnh sửa đơn.
6. Hủy đơn.
7. Tạo đơn bổ sung.
8. Xem lịch sử đơn.
9. Xem tổng hợp theo phòng ban.
10. Xem tổng hợp toàn công ty.
11. Duyệt/từ chối đơn bổ sung.
12. Quản lý danh mục, nhà cung cấp và phòng ban.
13. Quản lý bảng giá và giá theo nhà cung cấp.
14. Tổng kết và đóng kỳ.
15. Quản lý người dùng, nhóm và quyền thành phần.

Không cần làm 20 bảng use case dài như bản cũ. Chọn 8-10 use case quan trọng để mô tả chi tiết; các use case CRUD tương tự có thể gom nhóm.

Mẫu bảng use case:

| Thuộc tính | Nội dung |
|---|---|
| Tên use case | Tạo đơn yêu cầu định kỳ |
| Tác nhân | Nhân viên |
| Tiền điều kiện | Đã đăng nhập, có quyền tạo đơn, kỳ chưa đóng |
| Luồng chính | Chọn loại đơn -> chọn sản phẩm -> nhập số lượng -> rà soát -> gửi |
| Luồng thay thế | Sao chép đơn kỳ trước rồi điều chỉnh |
| Ngoại lệ | Trùng đơn, sai kỳ, hết hạn, mặt hàng đã xóa, số lượng không hợp lệ |
| Hậu điều kiện | Tạo header/detail, chụp giá hiện tại, ghi log |

**2.3.2. Yêu cầu phi chức năng**

- Bảo mật: HTTPS, cookie HttpOnly, JWT có thời hạn, phân quyền phía server.
- Toàn vẹn dữ liệu: transaction, unique constraint, state machine, optimistic concurrency bằng RowVersion.
- Khả dụng: giao diện responsive, trạng thái tải/rỗng/lỗi rõ ràng.
- Khả năng bảo trì: tách Controller, Service, Domain, Model, DTO và Shared.
- Theo dõi: log nghiệp vụ và Serilog.
- Triển khai: container hóa, health check, reverse proxy và lưu Data Protection key.
- Khả năng truy cập: kiểm tra bàn phím, nhãn điều khiển và độ tương phản nếu đưa vào tiêu chí.

**2.3.3. Quy trình nghiệp vụ**

Vẽ tối thiểu các activity diagram:

- Quy trình tạo đơn thường.
- Quy trình tạo và duyệt đơn bổ sung.
- Quy trình đóng kỳ.
- Quy trình quản lý bảng giá.
- Quy trình cấp quyền cho nhóm người dùng.

**2.3.4. Sơ đồ chức năng và use case tổng quát**

Tách hệ thống thành bốn nhóm: Yêu cầu VPP, Danh mục/giá, Quản lý kỳ, Người dùng/phân quyền.

### CHƯƠNG 3. THIẾT KẾ

#### 3.1. Kiến trúc hệ thống

Sơ đồ triển khai nên thể hiện:

`Trình duyệt -> Nginx/HTTPS -> Blazor Server -> ASP.NET Core Web API -> SQL Server`.

Blazor Server duy trì kết nối SignalR/WebSocket. Frontend gọi backend qua HTTP API. Backend xác thực JWT, xử lý nghiệp vụ qua service và truy cập SQL Server bằng EF Core. Docker Compose chạy ba container database, backend và frontend.

#### 3.2. Thiết kế dữ liệu

Các bảng chính:

| Nhóm | Bảng | Vai trò |
|---|---|---|
| Đơn hàng | `VPP01_RequestHeader` | Thông tin chung của đơn, kỳ, trạng thái, phòng ban, đóng kỳ |
| Đơn hàng | `VPP02_RequestDetail` | Mặt hàng, số lượng, giá chụp tại thời điểm xử lý |
| Đơn hàng | `VPP03_Log` | Nhật ký thao tác |
| Danh mục | `L01_Class`, `L02_ClassDetail` | Nhóm cấu hình và chi tiết, gồm đơn vị tính |
| Danh mục | `L03_VPPCategory` | Loại văn phòng phẩm |
| Danh mục | `L04_VPP` | Mặt hàng văn phòng phẩm |
| Nhà cung cấp | `L05_VPPSupplier` | Thông tin nhà cung cấp |
| Giá | `L06_VPPSupplierMapping` | Giá mặt hàng theo nhà cung cấp và bảng giá |
| Giá | `L07_PriceList` | Phiên bản/bảng giá, có bảng mặc định |
| Tổ chức | `LEX02_CompanyDepartmentLocation` | Công ty/phòng ban |
| Phân quyền | `P01` đến `P06` | Trang, nhóm, component và các ánh xạ quyền |

ERD phải thể hiện khóa chính, khóa ngoại và quan hệ 1-n/N-n. Không chụp nguyên code model làm sơ đồ.

#### 3.3. Thiết kế xử lý

Nên có sequence diagram cho:

- Đăng nhập: Browser -> Frontend -> Auth API -> Stored procedure/User DB -> JWT -> Cookie.
- Tạo đơn: UI -> VPPRequestController -> VPPRequestService -> PeriodCalculator -> EF Core -> SQL Server.
- Duyệt đơn bổ sung: Admin UI -> API -> state machine -> cập nhật audit -> log.
- Đóng kỳ: chọn kỳ/bảng giá -> kiểm tra đơn chờ -> kiểm tra đủ giá -> cập nhật detail/header -> log -> commit.
- Kiểm tra quyền: route/page -> PermissionState -> permission API -> ẩn/hiện component.

#### 3.4. Thiết kế giao diện

Chụp lại ảnh từ chính project, dùng dữ liệu mẫu không nhạy cảm:

1. Đăng nhập.
2. Dashboard - Đơn của tôi.
3. Wizard tạo đơn bước 1: bối cảnh yêu cầu.
4. Wizard bước 2: danh mục và chọn sản phẩm.
5. Wizard bước 3: rà soát đơn.
6. Lịch sử.
7. Danh mục sản phẩm.
8. Tổng hợp phòng ban.
9. Tổng hợp tất cả đơn.
10. Đơn bổ sung chờ duyệt.
11. Tổng kết và đóng kỳ.
12. Quản lý loại/mặt hàng/nhà cung cấp/phòng ban.
13. Quản lý bảng giá và giá.
14. Quản lý người dùng.
15. Phân quyền trang/component.
16. Giao diện responsive trên desktop/tablet/mobile.

Mỗi hình cần có 1-2 đoạn giải thích mục đích, dữ liệu hiển thị và thao tác chính. Không để một trang chỉ có ảnh.

### CHƯƠNG 4. THỬ NGHIỆM

#### 4.1. Phương pháp và môi trường thử nghiệm

Mô tả:

- Unit test cho quy tắc kỳ, state machine, mã đơn, giá và hiển thị trạng thái.
- Service/integration test cho tạo đơn, race condition, duyệt/từ chối, đóng kỳ.
- Controller test cho login, API đơn và danh mục.
- UI/E2E test bằng Playwright cho đăng nhập, tạo đơn, lịch sử, tổng hợp và duyệt.
- Kiểm tra giao diện nhiều kích thước và accessibility nếu dùng kết quả trong thư mục `audit`.

Repository hiện có 93 phương thức được đánh dấu `[Fact]`/`[Theory]`. Đây là số phương thức kiểm thử trong mã nguồn, không tự động đồng nghĩa với 93 test đã chạy thành công. Bản luận văn phải ghi kết quả thực tế sau khi chạy test ở phiên bản cuối.

Kết quả kiểm tra tại workspace ngày **11/06/2026**:

| Bộ test | Kết quả |
|---|---:|
| Backend `gtas_vpp_be.Tests` | 130 passed, 0 failed, 0 skipped |
| Frontend unit `gtas_vpp_fe.Tests` | 14 passed, 0 failed, 0 skipped |
| UI/E2E `gtas_vpp_fe.UITests` | Chưa chạy trong lần kiểm tra này vì cần đủ frontend, backend và database |

Các `[Theory]` sinh ra nhiều test case nên tổng số test chạy lớn hơn số phương thức test trong mã nguồn. Hãy chạy lại và chụp kết quả ở phiên bản cuối trước khi đưa số liệu vào luận văn.

#### 4.2. Kịch bản kiểm thử nghiệp vụ

Các nhóm bắt buộc:

- Đăng nhập đúng/sai.
- Tạo đơn thường hợp lệ.
- Chặn đơn trống, số lượng <= 0 và trùng sản phẩm.
- Chặn hai đơn thường cùng người/cùng kỳ, kể cả gửi đồng thời.
- Cho phép sao chép kỳ trước nhưng loại mặt hàng đã bị xóa.
- Tạo đơn bổ sung đúng/sai kỳ.
- Chặn quá ba đơn bổ sung hoặc còn đơn Pending.
- Duyệt/từ chối đơn bổ sung và kiểm tra audit.
- Chặn đóng kỳ khi còn Pending.
- Chặn đóng kỳ nếu bảng giá thiếu mặt hàng.
- Đóng kỳ thành công và kiểm tra giá, người đóng, thời gian, bảng giá.
- Phân quyền ẩn/hiện trang và component.
- CRUD danh mục và soft delete.
- Responsive desktop/tablet/mobile.

Mẫu bảng kết quả:

| ID | Mục tiêu | Dữ liệu đầu vào | Bước thực hiện | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|

#### 4.3. Các trường hợp ngoại lệ

Lập bảng theo nhóm: xác thực, quyền, kỳ, trạng thái đơn, mặt hàng bị xóa, thiếu giá, xung đột dữ liệu, mất kết nối API/SignalR và lỗi cơ sở dữ liệu.

### CHƯƠNG 5. KẾT LUẬN

#### 5.1. Kết quả đối chiếu với mục tiêu

Lấy lại bảng mục tiêu ở Chương 1 và thêm cột: Hoàn thành/Chưa hoàn thành/Bằng chứng.

#### 5.2. Các vấn đề còn tồn đọng

Nêu trung thực:

- Trang Report chưa có dữ liệu và xuất file thực tế.
- Chưa có module tồn kho và cấp phát.
- Chưa có benchmark tải chính thức.
- Dữ liệu người dùng còn phụ thuộc hệ thống/bảng GTAS_MENU và stored procedure.
- Một số luồng cần tiếp tục hoàn thiện kiểm thử trên môi trường production.

#### 5.3. Hướng phát triển

- Báo cáo và xuất Excel/PDF.
- Quy trình duyệt nhiều cấp theo phòng ban/ngân sách.
- Tích hợp tồn kho và mua hàng.
- Thông báo email/Teams khi gần hạn hoặc có đơn chờ duyệt.
- Dashboard phân tích chi phí theo kỳ, phòng ban, nhóm hàng.
- Load test, monitoring và cảnh báo vận hành.
- Tích hợp SSO của doanh nghiệp.

### PHỤ LỤC

Có thể đưa vào:

- Hướng dẫn cài đặt và chạy Docker.
- Danh sách API chính.
- Cấu trúc database chi tiết.
- Các bảng testcase đầy đủ.
- Hướng dẫn sử dụng ngắn cho từng nhóm người dùng.

### TÀI LIỆU THAM KHẢO

Nên tham khảo nguồn chính thức của Microsoft .NET/ASP.NET Core/Blazor/EF Core/SQL Server, Radzen, Docker, Nginx, xUnit và Playwright. Ghi tác giả/tổ chức, tên tài liệu, URL và ngày truy cập. Chỉ đưa nguồn thực sự đã dùng trong luận văn.

## 5. Danh sách hình và bảng nên làm

### Hình đề xuất

- Hình 2-1: Sơ đồ chức năng tổng quát.
- Hình 2-2: Use case tổng quát.
- Hình 2-3: Activity tạo đơn thường.
- Hình 2-4: Activity đơn bổ sung.
- Hình 2-5: Activity đóng kỳ.
- Hình 3-1: Kiến trúc tổng thể.
- Hình 3-2: Sơ đồ triển khai Docker/Nginx.
- Hình 3-3: ERD.
- Hình 3-4: Sơ đồ trạng thái đơn.
- Hình 3-5 đến 3-9: Sequence diagram các nghiệp vụ chính.
- Hình 3-10 trở đi: ảnh giao diện thực tế.

### Bảng đề xuất

- Bảng 1-1: Kết quả cần đạt và tiêu chí đánh giá.
- Bảng 2-1: So sánh giải pháp tương tự.
- Bảng 2-2: Danh sách tác nhân.
- Bảng 2-3: Danh sách yêu cầu chức năng.
- Bảng 2-4: Yêu cầu phi chức năng.
- Bảng 2-5 trở đi: use case chi tiết.
- Bảng 3-1: Các thành phần kiến trúc.
- Bảng 3-2: Danh sách bảng dữ liệu.
- Bảng 3-3: Ma trận quyền.
- Bảng 4-1: Môi trường kiểm thử.
- Bảng 4-2 trở đi: testcase và kết quả.
- Bảng 5-1: Đối chiếu mục tiêu và kết quả.

## 6. Phân bổ số trang

Mục tiêu hợp lý là 70-90 trang, không tính việc cố kéo giãn bằng ảnh quá lớn.

| Phần | Số trang gợi ý |
|---|---:|
| Bìa, nhiệm vụ, lời cảm ơn, mục lục | 5-8 |
| Chương 1 | 7-10 |
| Chương 2 | 18-25 |
| Chương 3 | 25-32 |
| Chương 4 | 10-15 |
| Chương 5 | 4-6 |
| Phụ lục, tài liệu tham khảo | 5-12 |

## 7. Cách làm lại file Word

### Bước 1. Tạo bản làm việc

Giữ nguyên file cũ để đối chiếu. Tạo một bản mới, ví dụ:

`NguyenAnNam_DH52201078_LVTN_2026.docx`

Xóa toàn bộ nội dung cũ từ Chương 1 đến hết tài liệu tham khảo. Xóa cả ảnh, bảng, caption và nội dung mục lục hình cũ. Giữ trang bìa/lời cảm ơn/mục lục chỉ như vị trí tham khảo rồi sửa lại.

### Bước 2. Thiết lập trang

- Khổ A4, Portrait.
- Top: 2 cm.
- Bottom: 2 cm.
- Left: 3 cm.
- Right: 2 cm.
- Gutter: 0 cm.
- Tạo Page Border đơn giản cho trang bìa theo mẫu.

File hiện tại đã có đúng lề 3-2-2-2 cm ở các section nhưng chưa có Page Border.

### Bước 3. Sửa Styles

Không chỉnh font thủ công từng đoạn. Vào Home -> Styles -> Modify:

| Style | Định dạng |
|---|---|
| Normal | Times New Roman 13, Justify, line spacing Multiple 1.3, After 6 pt |
| Heading 1 | TNR 24, Bold, UPPERCASE, Align Right, Page break before |
| Heading 2 | TNR 15, Bold, UPPERCASE |
| Heading 3 | TNR 14, Bold |
| Heading 4 | TNR 13, Underline, không bold |
| Tiêu đề mục lục | TNR 18, Bold, UPPERCASE, Center |

Chỉ đặt khoảng cách 6 pt ở một phía, nên dùng `After 6 pt`; không đặt cả Before và After 6 pt vì khoảng cách sẽ thành 12 pt.

### Bước 4. Tạo section cho từng chương

- Phần đầu quyển là một section, không header/footer.
- Trước Chương 1 dùng Section Break -> Next Page.
- Mỗi chương tiếp theo cũng dùng Section Break -> Next Page.
- Bật Different First Page cho section chương.
- Trang đầu chương không có header; các trang sau có header riêng.
- Footer của phần nội dung chứa tên đề tài in hoa, nghiêng và số trang canh phải.
- Đánh số trang bắt đầu từ 1 tại Chương 1 và liên tục đến hết quyển.

Header gợi ý:

- `Chương 1: GIỚI THIỆU`
- `Chương 2: PHƯƠNG PHÁP THỰC HIỆN`
- `Chương 3: THIẾT KẾ`
- `Chương 4: THỬ NGHIỆM`
- `Chương 5: KẾT LUẬN`

Nhớ tắt Link to Previous ở header khi chuyển chương. Footer có thể liên kết để giữ cùng tên đề tài và số trang liên tục.

### Bước 5. Tạo mục lục tự động

- Gán đúng Heading 1-4 cho tiêu đề.
- References -> Table of Contents -> Custom Table of Contents.
- Chọn hiển thị tối đa 4 cấp nếu GVHD yêu cầu.
- Không gõ dấu chấm và số trang bằng tay.

### Bước 6. Tạo caption và mục lục hình

- Chọn ảnh -> Wrap Text -> In Line with Text -> canh giữa.
- References -> Insert Caption -> tạo label `Hình`.
- Numbering -> Include chapter number -> separator `-`.
- Caption dạng `Hình 3-1: Kiến trúc tổng thể`.
- Canh giữa caption; phần `Hình 3-1` được gạch dưới, đậm, nghiêng theo mẫu.
- References -> Insert Table of Figures để tạo mục lục hình.

Làm tương tự với nhãn `Bảng` nếu tạo danh mục bảng.

### Bước 7. Cập nhật toàn bộ trường tự động

Trước khi xuất PDF:

1. Nhấn `Ctrl+A`.
2. Nhấn `F9`.
3. Chọn Update entire table cho mục lục.
4. Kiểm tra lại số hình, số bảng, số trang và hyperlink.

### Bước 8. Xóa dấu vết file cũ

Vào File -> Info -> Check for Issues -> Inspect Document. Xóa document properties, comments, tracked changes, hidden text và thông tin cá nhân của tác giả cũ. Sau đó kiểm tra lại Header/Footer bằng Navigation Pane và Print Preview.

## 8. Thứ tự thực hiện để đỡ phải sửa lại

1. Chốt tên đề tài, GVHD và phạm vi với thầy/cô.
2. Tạo bản Word mới và thiết lập Styles/Section trước.
3. Viết Chương 1 và danh sách yêu cầu ở Chương 2.
4. Vẽ use case, activity, kiến trúc và ERD từ mã nguồn.
5. Viết Chương 3 song song với chụp giao diện.
6. Chạy test phiên bản cuối, lưu kết quả rồi viết Chương 4.
7. Viết kết luận dựa trên kết quả thật, không viết trước.
8. Hoàn thiện tài liệu tham khảo, phụ lục, mục lục hình/bảng.
9. Cập nhật tất cả field, xuất PDF và kiểm tra từng trang.

## 9. Những điều không được ghi sai

- Không ghi frontend là Next.js.
- Không ghi backend là NestJS.
- Không ghi database là MongoDB.
- Không ghi hệ thống có GPS, chat, thanh toán, tài xế hoặc hành khách.
- Không ghi Report/Export đã hoàn chỉnh khi màn hình hiện chỉ là placeholder.
- Không ghi có quản lý kho hoặc cấp phát nếu chưa bổ sung chức năng.
- Không ghi số liệu hiệu năng/chịu tải khi chưa đo.
- Không dùng ảnh, testcase hoặc tài liệu tham khảo của đề tài đi chung xe.
- Không mô tả một chức năng chỉ vì nó có trong giao diện; phải đối chiếu controller/service/test để xác nhận hoạt động.
