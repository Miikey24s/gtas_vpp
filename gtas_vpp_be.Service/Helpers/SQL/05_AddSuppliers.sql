BEGIN TRANSACTION;
BEGIN TRY
    -- 1. Khai báo bảng tạm chứa 3 nhà cung cấp mới
    DECLARE @NewSuppliers TABLE (Id UNIQUEIDENTIFIER, Name NVARCHAR(MAX), ShortName NVARCHAR(MAX), City NVARCHAR(MAX));
    
    INSERT INTO @NewSuppliers (Id, Name, ShortName, City)
    VALUES     
    (NEWID(), N'VPP Thăng Long', 'VPP_HN', N'Hà Nội'),    
    (NEWID(), N'VPP Sông Hàn', 'VPP_DN', N'Đà Nẵng'),    
    (NEWID(), N'VPP Gia Định', 'VPP_HCM', N'Hồ Chí Minh');

    -- 2. Thêm vào bảng L05_VPPSupplier 
    -- (Bổ sung UpdateUserId và UpdateDate)
    INSERT INTO dbo.L05_VPPSupplier (
        Id, SupplierShortName, SupplierName, City, 
        CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted
    )
    SELECT 
        Id, ShortName, Name, City, 
        5615, GETDATE(), 5615, GETDATE(), 0
    FROM @NewSuppliers;

    -- 3. Bulk Map: Nhân 3 NCC này với TẤT CẢ VPP đang hoạt động vào bảng L06
    -- (Bổ sung UpdateUserId và UpdateDate)
    INSERT INTO dbo.L06_VPPSupplierMapping (
        Id, Price, L04_VPPId, L05_VPPSupplierId, Description, 
        CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted
    )
    SELECT 
        NEWID(), 
        0,              -- Giá mặc định khởi tạo
        V.Id,           -- Id từ bảng VPP hiện có
        S.Id,           -- Id của 3 NCC vừa tạo
        N'Thiết lập giá mặc định theo khu vực ' + S.City,
        5615, 
        GETDATE(), 
        5615,           -- Gán giá trị bắt buộc cho UpdateUserId
        GETDATE(),      -- Gán giá trị bắt buộc cho UpdateDate
        0
    FROM dbo.L04_VPP V
    CROSS JOIN @NewSuppliers S 
    WHERE V.IsDeleted = 0;

    COMMIT TRANSACTION;
    PRINT N'Thành công: Đã thêm 3 NCC và tự động tạo Mapping cho toàn bộ danh mục VPP.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    SELECT ERROR_MESSAGE() AS Error;
END CATCH
-------------------------------------
BEGIN TRANSACTION;
BEGIN TRY
    -- Cập nhật giá ngẫu nhiên cho những dòng mapping vừa tạo (CreateUserId = 5615)
    UPDATE dbo.L06_VPPSupplierMapping
    SET 
        Price = (ABS(CHECKSUM(NEWID())) % 495001) + 5000, -- Công thức: (Random % (Max-Min+1)) + Min
        UpdateDate = GETDATE()
    WHERE CreateUserId = 5615 
      AND Price = 0; -- Chỉ cập nhật những dòng đang bị bằng 0

    COMMIT TRANSACTION;
    PRINT N'Thành công: Đã cập nhật giá ngẫu nhiên cho toàn bộ dữ liệu!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    SELECT ERROR_MESSAGE() AS Error;
END CATCH