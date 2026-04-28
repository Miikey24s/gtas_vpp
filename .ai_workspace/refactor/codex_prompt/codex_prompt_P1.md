Bạn là AI Worker (Codex) trong hệ thống refactor dự án GTAS VPP.

# Bắt buộc đọc trước khi làm
- `.ai_workspace/context.md` — hiểu cấu trúc project
- `.ai_workspace/architecture.md` — chuẩn coding phải tuân thủ
- `.ai_workspace/tasks.json` — danh sách task

# Nhiệm vụ: Thực thi Phase 1 (Cleanup)

Thực hiện lần lượt 6 tasks sau. Sau mỗi task, verify bằng `dotnet build gtas_vpp/gtas_vpp.slnx`. Nếu build fail → fix trước khi làm task tiếp.

## P1.1 — Xóa template files
Xóa 4 file sau (chúng là template mặc định, không dùng):
- `code-be/gtas_vpp_be/WeatherForecast.cs`
- `code-be/gtas_vpp_be/Controllers/WeatherForecastController.cs`
- `code-fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Counter.razor`
- `code-fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Weather.razor`

## P1.2 — Fix typo BussinessService → BusinessService
Rename trên TOÀN solution:
- File `BussinessService.cs` → `BusinessService.cs`
- Class `BussinessService` → `BusinessService`
- Interface `IBussinessService` → `IBusinessService`
- Cập nhật tất cả references: using statements, DI registration trong `Program.cs` (BE), constructor injection trong controllers, biến `_bussinessService`

## P1.3 — Di chuyển inline DTOs
Trong file `code-be/gtas_vpp_be/Controllers/VPPRequestController.cs`, cuối file có 2 class DTO inline:
1. `CopyPreviousMonthReqDTO` (có 2 property: Year, Month)
2. `RejectOrderReqDTO` (có 1 property: Reason)

Thực hiện:
- Tạo file `gtas_vpp/gtas_vpp_shared/DTOs/Req/VPP/CopyPreviousMonthReqDTO.cs` với namespace `gtas_vpp_shared.DTOs.Req.VPP`
- Tạo file `gtas_vpp/gtas_vpp_shared/DTOs/Req/VPP/RejectOrderReqDTO.cs` với namespace `gtas_vpp_shared.DTOs.Req.VPP`
- Xóa 2 class đó khỏi VPPRequestController.cs
- Thêm `using gtas_vpp_shared.DTOs.Req.VPP;` vào VPPRequestController.cs nếu chưa có

## P1.4 — Xóa StructLayout attribute
Xóa `[StructLayout(LayoutKind.Auto)]` và `using System.Runtime.InteropServices;` khỏi 2 file:
- `code-be/gtas_vpp_be.Service/Services/UnitOfWork.cs`
- `code-be/gtas_vpp_be.Model/VPPMigrationDbContext.cs`

## P1.5 — Xóa [Inject] sai chỗ
Trong `code-be/gtas_vpp_be.Service/Services/BaseServices.cs`:
- Dòng `[Inject] public IUnitOfWorkFactory _unitOfWorkFactory { get; set; } = default!;` → đổi thành `private readonly IUnitOfWorkFactory _unitOfWorkFactory;`
- Xóa `using Microsoft.AspNetCore.Components;`
- Constructor đã nhận `IUnitOfWorkFactory` qua parameter rồi (dòng 34), nên chỉ cần xóa attribute và đổi access modifier

## P1.6 — Dọn csproj exclude rules
Trong `code-fe/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe.csproj`, xóa các ItemGroup chứa:
- `<Compile Remove="Component_Library.razor.cs" />`
- `<Compile Remove="Component_ShareGrid.razor.cs" />`
- `<Compile Remove="Page_Library.razor.cs" />`
- `<Content Remove="Components\Pages\Counter.razor" />`
- `<Content Remove="Components\Pages\Error.razor" />`
- `<Content Remove="Components\Pages\Weather.razor" />`
- `<Content Remove="Component_Library.razor" />`
- `<Content Remove="Component_ShareGrid.razor" />`
- `<Content Remove="Page_Library.razor" />`

# Sau khi hoàn thành TẤT CẢ tasks
1. Chạy `dotnet build gtas_vpp/gtas_vpp.slnx` lần cuối
2. Cập nhật `.ai_workspace/tasks.json`: đổi status của P1.1 → P1.6 thành `"done"`, thêm `"completedAt": "2026-04-26"`
3. Ghi tóm tắt kết quả vào `.ai_workspace/reports/P1_cleanup.md`

# Quy tắc
- KHÔNG thay đổi logic code, chỉ cleanup
- Nếu build fail sau task nào → ghi rõ lỗi vào report, fix, rồi tiếp
- Viết ngắn gọn, không giải thích dài dòng
