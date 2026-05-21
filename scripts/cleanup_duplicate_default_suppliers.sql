SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @AuditUserId INT = 5615;
    DECLARE @Now DATETIME = GETDATE();

    DECLARE @DefaultSuppliers TABLE (
        Id UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(255) NOT NULL,
        ShortName NVARCHAR(100) NOT NULL,
        City NVARCHAR(100) NOT NULL
    );

    DECLARE @DuplicateSuppliers TABLE (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        SupplierShortName NVARCHAR(100) NOT NULL
    );

    DECLARE @DefaultMappingsToRestore TABLE (
        L04_VPPId UNIQUEIDENTIFIER NOT NULL,
        SupplierShortName NVARCHAR(100) NOT NULL,
        PRIMARY KEY (L04_VPPId, SupplierShortName)
    );

    INSERT INTO @DefaultSuppliers (Id, Name, ShortName, City)
    VALUES
        ('99A2D5B5-5C90-4565-85D9-2772D73A3DA1', N'VPP Thăng Long', 'VPP_HN', N'Hà Nội'),
        ('53F2E50E-E30A-47BF-9DB2-B606962750AB', N'VPP Sông Hàn', 'VPP_DN', N'Đà Nẵng'),
        ('A43EF777-6B17-43E0-9FE1-E7D76C682521', N'VPP Gia Định', 'VPP_HCM', N'Hồ Chí Minh');

    ;WITH RankedDefaultSuppliers AS (
        SELECT
            Existing.Id,
            Existing.SupplierShortName,
            ROW_NUMBER() OVER (
                PARTITION BY Existing.SupplierShortName
                ORDER BY Existing.CreateDate, Existing.Id
            ) AS RowNumber
        FROM dbo.L05_VPPSupplier Existing
        INNER JOIN @DefaultSuppliers S
            ON S.ShortName = Existing.SupplierShortName
        WHERE Existing.IsDeleted = 0
    )
    INSERT INTO @DuplicateSuppliers (Id, SupplierShortName)
    SELECT Id, SupplierShortName
    FROM RankedDefaultSuppliers
    WHERE RowNumber > 1;

    INSERT INTO @DefaultMappingsToRestore (L04_VPPId, SupplierShortName)
    SELECT DISTINCT M.L04_VPPId, S.SupplierShortName
    FROM dbo.L06_VPPSupplierMapping M
    INNER JOIN @DuplicateSuppliers D
        ON D.Id = M.L05_VPPSupplierId
    INNER JOIN dbo.L05_VPPSupplier S
        ON S.Id = M.L05_VPPSupplierId
    WHERE M.IsDeleted = 0
      AND M.IsDefault = 1;

    UPDATE M
    SET
        M.IsDeleted = 1,
        M.IsDefault = 0,
        M.UpdateUserId = @AuditUserId,
        M.UpdateDate = @Now
    FROM dbo.L06_VPPSupplierMapping M
    INNER JOIN @DuplicateSuppliers D
        ON D.Id = M.L05_VPPSupplierId
    WHERE M.IsDeleted = 0;
    DECLARE @SoftDeletedMappingCount INT = @@ROWCOUNT;

    UPDATE S
    SET
        S.IsDeleted = 1,
        S.UpdateUserId = @AuditUserId,
        S.UpdateDate = @Now
    FROM dbo.L05_VPPSupplier S
    INNER JOIN @DuplicateSuppliers D
        ON D.Id = S.Id
    WHERE S.IsDeleted = 0;
    DECLARE @SoftDeletedSupplierCount INT = @@ROWCOUNT;

    UPDATE M
    SET
        M.IsDefault = 1,
        M.UpdateUserId = @AuditUserId,
        M.UpdateDate = @Now
    FROM dbo.L06_VPPSupplierMapping M
    INNER JOIN dbo.L05_VPPSupplier S
        ON S.Id = M.L05_VPPSupplierId
    INNER JOIN @DefaultMappingsToRestore R
        ON R.L04_VPPId = M.L04_VPPId
       AND R.SupplierShortName = S.SupplierShortName
    WHERE M.IsDeleted = 0
      AND S.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.L06_VPPSupplierMapping ExistingDefault
          WHERE ExistingDefault.L04_VPPId = M.L04_VPPId
            AND ExistingDefault.IsDefault = 1
            AND ExistingDefault.IsDeleted = 0
            AND ExistingDefault.Id <> M.Id
      );
    DECLARE @RestoredDefaultCount INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT
        @SoftDeletedSupplierCount AS SoftDeletedSuppliers,
        @SoftDeletedMappingCount AS SoftDeletedMappings,
        @RestoredDefaultCount AS RestoredDefaultMappings;

    SELECT
        SupplierShortName,
        COUNT(*) AS ActiveSuppliers
    FROM dbo.L05_VPPSupplier
    WHERE IsDeleted = 0
      AND SupplierShortName IN ('VPP_HN', 'VPP_DN', 'VPP_HCM')
    GROUP BY SupplierShortName
    ORDER BY SupplierShortName;

    SELECT
        COUNT(*) AS ActiveMappingsForDefaultSupplierShortNames
    FROM dbo.L06_VPPSupplierMapping M
    INNER JOIN dbo.L05_VPPSupplier S
        ON S.Id = M.L05_VPPSupplierId
    WHERE M.IsDeleted = 0
      AND S.IsDeleted = 0
      AND S.SupplierShortName IN ('VPP_HN', 'VPP_DN', 'VPP_HCM');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
