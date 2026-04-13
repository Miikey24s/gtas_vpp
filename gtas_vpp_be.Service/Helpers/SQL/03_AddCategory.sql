-- Created by GitHub Copilot in SSMS - review carefully before executing

USE [GTAS_VPP_TEST];
GO

-- Tạo bảng tạm để chứa dữ liệu cần Insert
DECLARE @NewData TABLE (
    VPPCategoryName NVARCHAR(255),
    VPPCategoryCode NVARCHAR(50),
    [Description] NVARCHAR(500)
);

-- Thêm Data vào bảng tạm (kèm mã Code ngẫu nhiên và Description)
INSERT INTO @NewData (VPPCategoryName, VPPCategoryCode, [Description])
VALUES 
    (N'Băng keo_ bấm kim_ bấm lỗ', N'VPP001', N'Nhóm Băng keo, bấm kim, bấm lỗ'),
    (N'Bìa còng_ bìa phân trang_ trình ký', N'VPP002', N'Nhóm Bìa còng, bìa phân trang, trình ký'),
    (N'Kẹp bướm_ kim bấm_ kéo', N'VPP003', N'Nhóm Kẹp bướm, kim bấm, kéo'),
    (N'Bút viết', N'VPP004', N'Nhóm Bút viết các loại'),
    (N'Tập viết_ Thước kẻ', N'VPP005', N'Nhóm Tập viết, Thước kẻ'),
    (N'Giấy in các loại', N'VPP006', N'Nhóm Giấy in các loại'),
    (N'Khác', N'VPP007', N'Nhóm Văn phòng phẩm Khác'),
    (N'Phục vụ Văn phòng', N'VPP008', N'Nhóm Phục vụ Văn phòng');

-- Thực hiện Insert vào bảng chính thức (bỏ qua bản ghi nếu VPPCategoryName đã tồn tại)
INSERT INTO dbo.L03_VPPCategory (
    Id,
    VPPCategoryName,
    VPPCategoryCode, 
    [Description], 
    CreateDate, 
    CreateUserId, 
    UpdateDate, 
    UpdateUserId, 
    IsDeleted
)
SELECT 
    NEWID(),         -- Id (uniqueidentifier)
    nd.VPPCategoryName,
    nd.VPPCategoryCode,
    nd.[Description],
    SYSDATETIME(),   -- CreateDate (datetime2)
    5615,            -- CreateUserId
    SYSDATETIME(),   -- UpdateDate (datetime2)
    5615,            -- UpdateUserId
    0                -- IsDeleted (false)
FROM @NewData nd
WHERE NOT EXISTS (
    SELECT 1 
    FROM dbo.L03_VPPCategory tgt
    WHERE tgt.VPPCategoryName = nd.VPPCategoryName
);
GO