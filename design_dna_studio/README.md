# DesignDNA Studio

Ứng dụng nghiên cứu gu thiết kế dành riêng cho GTAS VPP. Studio biến lựa chọn thị giác theo cặp thành một hồ sơ sở thích có trọng số, độ tin cậy và diễn giải rõ ràng; dữ liệu này giúp các vòng UI/UX tiếp theo bớt phụ thuộc vào mô tả cảm tính.

## Phạm vi hiện tại

- Blazor Web App `.NET 10`, chạy toàn bộ bằng Interactive Server.
- Khảo sát so sánh cặp theo hai giai đoạn: hiệu chỉnh có kiểm soát và lựa chọn thích nghi.
- Ba mức độ sâu 30/40/60 câu; lượt bỏ qua không được tính vào tiến độ, probe schedule hoặc mô hình. Event vẫn được giữ để không hỏi lại đúng cặp không thể đánh giá; khi 12 cặp hiệu chuẩn đã dùng hết, phiên chuyển sang pool thích nghi mà không cộng câu trả lời.
- 28 chiều thiết kế có thể giải thích, phát hiện thiên lệch trái/phải và kiểm tra nhất quán bằng cặp lặp ẩn.
- Hồ sơ kết quả được tính cục bộ bằng mô hình xác định; không gửi câu trả lời đến dịch vụ AI bên ngoài.
- Kết quả đã chốt được đọc từ snapshot bất biến và có thể xuất JSON kèm toàn bộ evidence event.
- Evidence export `v2` giữ study/session metadata, event ID/reference graph, asset version, preview spec và vector 28 chiều tại thời điểm trả lời để có thể audit sau khi catalog thay đổi. `ExportedAt` chỉ là thời điểm tạo file; profile và evidence bên trong vẫn lấy từ snapshot đã chốt.
- Database SQLite và file nghiên cứu tách biệt hoàn toàn khỏi database GTAS VPP.

Studio là công cụ nghiên cứu nội bộ, không phải một phần của runtime production GTAS VPP. Mặc định ứng dụng chỉ nhận kết nối loopback (`localhost`) và không mở bề mặt đăng ký tài khoản. Kết quả là bằng chứng hỗ trợ quyết định thiết kế, không phải “điểm gu” tuyệt đối.

Mỗi lượt nhập tên mặc định tạo một người tham gia mới, kể cả khi trùng tên. Muốn theo dõi hồ sơ theo thời gian và tăng version, phải chọn rõ hồ sơ cũ trên màn hình bắt đầu; tên hiển thị của từng phiên đã hoàn tất vẫn được snapshot để export cũ không bị đổi theo lần đặt tên sau.

## Chạy local

Yêu cầu [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Không cần Docker hay SQL Server.

Từ thư mục gốc repository:

```powershell
.\scripts\design-dna.cmd run
```

Ứng dụng dùng `dotnet watch` để Hot Reload. URL local mặc định là `http://localhost:5094`; terminal luôn hiển thị URL thực tế.

Kiểm tra build và test:

```powershell
.\scripts\design-dna.cmd test
```

Lệnh test tự restore `dotnet-ef` từ `dotnet-tools.json` trước khi kiểm tra migration drift.

Xem toàn bộ lệnh:

```powershell
.\scripts\design-dna.cmd help
```

## Dữ liệu và reset

Mặc định dữ liệu chỉ nằm trên máy hiện tại:

```text
%LOCALAPPDATA%\GTAS\DesignDNAStudio
└── design-dna.db (+ SQLite journal/WAL files while running)
```

Không commit thư mục này. Muốn bắt đầu lại với database sạch:

```powershell
.\scripts\design-dna.cmd reset
```

Lệnh yêu cầu nhập chính xác `RESET`, chỉ xóa các file database của Studio và không xóa thư mục cha. Hãy dừng ứng dụng trước khi reset. Lần chạy kế tiếp sẽ tự migrate và seed bộ khảo sát tích hợp.

Có thể cô lập một bộ dữ liệu thử nghiệm bằng `-DataRoot`; đường dẫn chỉ áp dụng cho tiến trình hiện tại và không được ghi vào cấu hình repo. Thư mục custom phải nằm trên ổ local, ở ngoài repository, đồng thời phải mới/rỗng hoặc đã có marker do script tạo để tránh vô tình dùng rồi reset một thư mục chứa dữ liệu khác:

```powershell
.\scripts\design-dna.cmd run -DataRoot "$env:TEMP\DesignDNAStudio"
.\scripts\design-dna.cmd reset -DataRoot "$env:TEMP\DesignDNAStudio"
```

## Cấu trúc

```text
src/DesignDnaStudio.Engine/  Mô hình chiều thiết kế, học sở thích và chọn cặp
src/DesignDnaStudio.Web/     Blazor UI, EF Core SQLite và seed study
tests/DesignDnaStudio.Tests/ Unit test cho engine và độ tin cậy
```

Các thay đổi schema phải tạo bằng EF Core migration; không sửa trực tiếp file SQLite. Không đưa secret, dữ liệu người tham gia thật hoặc database local vào Git.
