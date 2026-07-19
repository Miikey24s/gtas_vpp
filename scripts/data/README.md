# Chuẩn hoá dữ liệu Demo VPP

Chạy khi workbook nguồn thay đổi:

```powershell
python .\scripts\data\normalize-vpp-demo-source.py --source ".\LVTN\data\DANG KY VPP - CAC DON VI.xlsx"
```

Script chỉ đọc Excel và sinh TSV tại `gtas_vpp_be/gtas_vpp_be.Service/Helpers/Data/Demo/`. Cần Python 3 và `openpyxl`. TSV là dữ liệu runtime; workbook gốc không được backend đọc và ghi chú chứa tên cá nhân không được xuất ra.
