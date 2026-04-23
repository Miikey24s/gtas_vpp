-- ============================================
-- Script to Remove Duplicate ComponentCode in P03_Component
-- Target: 0001_LIB_C and 0001_LIB_O (each has 2 records, need to delete 1)
-- Strategy: Keep the oldest record (earliest CreateDate), delete the newer one
-- ============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT '========================================';
PRINT 'FIX DUPLICATE COMPONENTS';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '========================================';
PRINT '';

BEGIN TRY
    BEGIN TRANSACTION;

    -- ============================================
    -- STEP 1: Show current duplicates
    -- ============================================
    PRINT 'STEP 1: Current duplicate records';
    PRINT '----------------------------------------';
    
    SELECT 
        ComponentCode,
        Id,
        ComponentName,
        CreateDate,
        CASE 
            WHEN ROW_NUMBER() OVER (PARTITION BY ComponentCode ORDER BY CreateDate ASC, Id ASC) = 1 
            THEN 'KEEP' 
            ELSE 'DELETE' 
        END as Action
    FROM dbo.P03_Component
    WHERE ComponentCode IN ('0001_LIB_C', '0001_LIB_O')
    ORDER BY ComponentCode, CreateDate;
    
    PRINT '';

    -- ============================================
    -- STEP 2: Create backup
    -- ============================================
    PRINT 'STEP 2: Creating backup table';
    PRINT '----------------------------------------';
    
    IF OBJECT_ID('dbo.P03_Component_Backup_Before_Delete', 'U') IS NOT NULL
        DROP TABLE dbo.P03_Component_Backup_Before_Delete;
    
    SELECT * 
    INTO dbo.P03_Component_Backup_Before_Delete
    FROM dbo.P03_Component
    WHERE ComponentCode IN ('0001_LIB_C', '0001_LIB_O');
    
    DECLARE @BackupCount INT = (SELECT COUNT(*) FROM dbo.P03_Component_Backup_Before_Delete);
    PRINT 'Backup created: ' + CAST(@BackupCount AS VARCHAR) + ' records saved';
    PRINT '';

    -- ============================================
    -- STEP 3: Check foreign key dependencies
    -- ============================================
    PRINT 'STEP 3: Checking foreign key dependencies';
    PRINT '----------------------------------------';
    
    SELECT 
        p03.ComponentCode,
        p03.Id as ComponentId,
        p03.CreateDate,
        COUNT(p05.Id) as UsedInMappings,
        CASE 
            WHEN ROW_NUMBER() OVER (PARTITION BY p03.ComponentCode ORDER BY p03.CreateDate ASC, p03.Id ASC) = 1 
            THEN 'KEEP' 
            ELSE 'DELETE' 
        END as Action
    FROM dbo.P03_Component p03
    LEFT JOIN dbo.P05_PageComponentMapping p05 ON p05.P03_ComponentId = p03.Id
    WHERE p03.ComponentCode IN ('0001_LIB_C', '0001_LIB_O')
    GROUP BY p03.ComponentCode, p03.Id, p03.CreateDate
    ORDER BY p03.ComponentCode, p03.CreateDate;
    
    PRINT '';

    -- ============================================
    -- STEP 4: Update foreign keys to point to the record we're keeping
    -- ============================================
    PRINT 'STEP 4: Updating foreign key references';
    PRINT '----------------------------------------';
    
    -- Get IDs to keep and delete
    DECLARE @KeepDeleteMapping TABLE (
        ComponentCode NVARCHAR(100),
        KeepId UNIQUEIDENTIFIER,
        DeleteId UNIQUEIDENTIFIER
    );
    
    INSERT INTO @KeepDeleteMapping (ComponentCode, KeepId, DeleteId)
    SELECT 
        keep.ComponentCode,
        keep.Id as KeepId,
        del.Id as DeleteId
    FROM (
        SELECT 
            ComponentCode,
            Id,
            ROW_NUMBER() OVER (PARTITION BY ComponentCode ORDER BY CreateDate ASC, Id ASC) as RowNum
        FROM dbo.P03_Component
        WHERE ComponentCode IN ('0001_LIB_C', '0001_LIB_O')
    ) keep
    INNER JOIN (
        SELECT 
            ComponentCode,
            Id,
            ROW_NUMBER() OVER (PARTITION BY ComponentCode ORDER BY CreateDate ASC, Id ASC) as RowNum
        FROM dbo.P03_Component
        WHERE ComponentCode IN ('0001_LIB_C', '0001_LIB_O')
    ) del ON keep.ComponentCode = del.ComponentCode AND keep.RowNum = 1 AND del.RowNum > 1;
    
    -- Show mapping
    SELECT 
        ComponentCode,
        KeepId,
        DeleteId,
        'Will update P05 references from DeleteId to KeepId' as Action
    FROM @KeepDeleteMapping;
    
    -- Update P05_PageComponentMapping
    UPDATE p05
    SET p05.P03_ComponentId = m.KeepId
    FROM dbo.P05_PageComponentMapping p05
    INNER JOIN @KeepDeleteMapping m ON m.DeleteId = p05.P03_ComponentId;
    
    DECLARE @UpdatedCount INT = @@ROWCOUNT;
    PRINT 'Updated ' + CAST(@UpdatedCount AS VARCHAR) + ' foreign key references in P05_PageComponentMapping';
    PRINT '';

    -- ============================================
    -- STEP 5: Delete duplicate records
    -- ============================================
    PRINT 'STEP 5: Deleting duplicate records';
    PRINT '----------------------------------------';
    
    DELETE FROM dbo.P03_Component
    WHERE Id IN (SELECT DeleteId FROM @KeepDeleteMapping);
    
    DECLARE @DeletedCount INT = @@ROWCOUNT;
    PRINT 'Deleted ' + CAST(@DeletedCount AS VARCHAR) + ' duplicate records';
    PRINT '';

    -- ============================================
    -- STEP 6: Verify results
    -- ============================================
    PRINT 'STEP 6: Verification';
    PRINT '----------------------------------------';
    
    SELECT 
        ComponentCode,
        COUNT(*) as Total,
        CASE WHEN COUNT(*) = 1 THEN 'OK' ELSE 'STILL DUPLICATE!' END as Status
    FROM dbo.P03_Component
    WHERE ComponentCode IN ('0001_LIB_C', '0001_LIB_O')
    GROUP BY ComponentCode
    ORDER BY ComponentCode;
    
    -- Check if any duplicates remain
    IF EXISTS (
        SELECT 1 
        FROM dbo.P03_Component
        WHERE ComponentCode IN ('0001_LIB_C', '0001_LIB_O')
        GROUP BY ComponentCode
        HAVING COUNT(*) > 1
    )
    BEGIN
        PRINT '';
        PRINT 'WARNING: Duplicates still exist! Rolling back...';
        ROLLBACK TRANSACTION;
        RETURN;
    END
    
    PRINT '';
    PRINT 'SUCCESS: All duplicates removed!';
    PRINT '';

    -- ============================================
    -- STEP 7: Show final state
    -- ============================================
    PRINT 'STEP 7: Final records';
    PRINT '----------------------------------------';
    
    SELECT 
        ComponentCode,
        Id,
        ComponentName,
        Description,
        CreateDate,
        'KEPT' as Status
    FROM dbo.P03_Component
    WHERE ComponentCode IN ('0001_LIB_C', '0001_LIB_O')
    ORDER BY ComponentCode;
    
    COMMIT TRANSACTION;
    
    PRINT '';
    PRINT '========================================';
    PRINT 'COMPLETED SUCCESSFULLY';
    PRINT 'Finished at: ' + CONVERT(VARCHAR, GETDATE(), 120);
    PRINT '========================================';
    PRINT '';
    PRINT 'Backup table: dbo.P03_Component_Backup_Before_Delete';
    PRINT 'You can drop this table after verifying everything works correctly.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    PRINT '';
    PRINT '========================================';
    PRINT 'ERROR OCCURRED';
    PRINT '========================================';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR);
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT '';
    PRINT 'Transaction rolled back. No changes made.';
    PRINT 'Backup table preserved: dbo.P03_Component_Backup_Before_Delete';
END CATCH;
