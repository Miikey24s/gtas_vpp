-- ============================================
-- Script to Fix Orphan Records in P0x Tables
-- Created: 2026-04-23
-- Description: Removes orphan records that reference non-existent foreign keys
-- WARNING: Run Check_Foreign_Key_Integrity.sql first to identify issues!
-- ============================================

-- IMPORTANT: Review orphan records before running this script!
-- Run Check_Foreign_Key_Integrity.sql first to see what will be deleted

PRINT '========================================';
PRINT 'FIX ORPHAN RECORDS';
PRINT '========================================';
PRINT '';

-- ============================================
-- STEP 1: BACKUP ALL TABLES
-- ============================================
PRINT 'STEP 1: Creating backups...';
PRINT '----------------------------------------';

DECLARE @BackupDate NVARCHAR(20) = FORMAT(GETDATE(), 'yyyyMMdd_HHmmss');

-- Backup P04_UserGroup
DECLARE @BackupP04 NVARCHAR(200) = 'dbo.P04_UserGroup_Backup_' + @BackupDate;
DECLARE @SqlP04 NVARCHAR(MAX) = 'SELECT * INTO ' + @BackupP04 + ' FROM dbo.P04_UserGroup';
EXEC sp_executesql @SqlP04;
PRINT 'Backup created: ' + @BackupP04;

-- Backup P02_Group
DECLARE @BackupP02 NVARCHAR(200) = 'dbo.P02_Group_Backup_' + @BackupDate;
DECLARE @SqlP02 NVARCHAR(MAX) = 'SELECT * INTO ' + @BackupP02 + ' FROM dbo.P02_Group';
EXEC sp_executesql @SqlP02;
PRINT 'Backup created: ' + @BackupP02;

-- Backup P05_PageComponentMapping
DECLARE @BackupP05 NVARCHAR(200) = 'dbo.P05_PageComponentMapping_Backup_' + @BackupDate;
DECLARE @SqlP05 NVARCHAR(MAX) = 'SELECT * INTO ' + @BackupP05 + ' FROM dbo.P05_PageComponentMapping';
EXEC sp_executesql @SqlP05;
PRINT 'Backup created: ' + @BackupP05;

-- Backup P06_GroupPageComponentMapping
DECLARE @BackupP06 NVARCHAR(200) = 'dbo.P06_GroupPageComponentMapping_Backup_' + @BackupDate;
DECLARE @SqlP06 NVARCHAR(MAX) = 'SELECT * INTO ' + @BackupP06 + ' FROM dbo.P06_GroupPageComponentMapping';
EXEC sp_executesql @SqlP06;
PRINT 'Backup created: ' + @BackupP06;

PRINT '';

-- ============================================
-- STEP 2: FIX P04_UserGroup -> P02_Group orphans
-- ============================================
PRINT 'STEP 2: Fixing P04_UserGroup orphans...';
PRINT '----------------------------------------';

-- Option A: Delete orphan records (RECOMMENDED)
DELETE FROM dbo.P04_UserGroup
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.P02_Group p02 
    WHERE p02.Id = P04_UserGroup.P02_GroupId
);

PRINT 'Deleted orphan P04_UserGroup records: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- ============================================
-- STEP 3: FIX P02_Group ParentGroupId orphans
-- ============================================
PRINT 'STEP 3: Fixing P02_Group ParentGroupId orphans...';
PRINT '----------------------------------------';

-- Option: Set ParentGroupId to NULL for orphans
UPDATE dbo.P02_Group
SET ParentGroupId = NULL
WHERE ParentGroupId IS NOT NULL 
    AND ParentGroupId != CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)
    AND NOT EXISTS (
        SELECT 1 FROM dbo.P02_Group parent 
        WHERE parent.Id = P02_Group.ParentGroupId
    );

PRINT 'Fixed orphan ParentGroupId (set to NULL): ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- ============================================
-- STEP 4: FIX P05_PageComponentMapping -> P01_Page orphans
-- ============================================
PRINT 'STEP 4: Fixing P05_PageComponentMapping -> P01_Page orphans...';
PRINT '----------------------------------------';

-- First, delete P06 records that reference orphan P05 records
DELETE FROM dbo.P06_GroupPageComponentMapping
WHERE P05_PageComponentMappingId IN (
    SELECT p05.Id
    FROM dbo.P05_PageComponentMapping p05
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.P01_Page p01 
        WHERE p01.Id = p05.P01_PageId
    )
);

PRINT 'Deleted P06 records referencing orphan P05: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- Then delete orphan P05 records
DELETE FROM dbo.P05_PageComponentMapping
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.P01_Page p01 
    WHERE p01.Id = P05_PageComponentMapping.P01_PageId
);

PRINT 'Deleted orphan P05_PageComponentMapping (missing Page): ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- ============================================
-- STEP 5: FIX P05_PageComponentMapping -> P03_Component orphans
-- ============================================
PRINT 'STEP 5: Fixing P05_PageComponentMapping -> P03_Component orphans...';
PRINT '----------------------------------------';

-- First, delete P06 records that reference orphan P05 records
DELETE FROM dbo.P06_GroupPageComponentMapping
WHERE P05_PageComponentMappingId IN (
    SELECT p05.Id
    FROM dbo.P05_PageComponentMapping p05
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.P03_Component p03 
        WHERE p03.Id = p05.P03_ComponentId
    )
);

PRINT 'Deleted P06 records referencing orphan P05: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- Then delete orphan P05 records
DELETE FROM dbo.P05_PageComponentMapping
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.P03_Component p03 
    WHERE p03.Id = P05_PageComponentMapping.P03_ComponentId
);

PRINT 'Deleted orphan P05_PageComponentMapping (missing Component): ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- ============================================
-- STEP 6: FIX P06_GroupPageComponentMapping -> P02_Group orphans
-- ============================================
PRINT 'STEP 6: Fixing P06_GroupPageComponentMapping -> P02_Group orphans...';
PRINT '----------------------------------------';

DELETE FROM dbo.P06_GroupPageComponentMapping
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.P02_Group p02 
    WHERE p02.Id = P06_GroupPageComponentMapping.P02_GroupId
);

PRINT 'Deleted orphan P06 (missing Group): ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- ============================================
-- STEP 7: FIX P06_GroupPageComponentMapping -> P05_PageComponentMapping orphans
-- ============================================
PRINT 'STEP 7: Fixing P06_GroupPageComponentMapping -> P05_PageComponentMapping orphans...';
PRINT '----------------------------------------';

DELETE FROM dbo.P06_GroupPageComponentMapping
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.P05_PageComponentMapping p05 
    WHERE p05.Id = P06_GroupPageComponentMapping.P05_PageComponentMappingId
);

PRINT 'Deleted orphan P06 (missing PageComponentMapping): ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- ============================================
-- STEP 8: FIX CIRCULAR REFERENCES in P02_Group
-- ============================================
PRINT 'STEP 8: Fixing circular references in P02_Group...';
PRINT '----------------------------------------';

-- Detect and break circular references by setting ParentGroupId to NULL
WITH RecursiveGroups AS (
    SELECT 
        Id,
        GroupName,
        ParentGroupId,
        CAST(Id AS NVARCHAR(MAX)) as Path,
        0 as Level
    FROM dbo.P02_Group
    WHERE ParentGroupId IS NOT NULL 
        AND ParentGroupId != CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)
    
    UNION ALL
    
    SELECT 
        rg.Id,
        rg.GroupName,
        p02.ParentGroupId,
        rg.Path + ' -> ' + CAST(p02.Id AS NVARCHAR(MAX)),
        rg.Level + 1
    FROM RecursiveGroups rg
    INNER JOIN dbo.P02_Group p02 ON p02.Id = rg.ParentGroupId
    WHERE rg.Level < 20
        AND p02.ParentGroupId IS NOT NULL
        AND p02.ParentGroupId != CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)
        AND rg.Path NOT LIKE '%' + CAST(p02.Id AS NVARCHAR(MAX)) + '%'
)
UPDATE dbo.P02_Group
SET ParentGroupId = NULL
WHERE Id IN (
    SELECT Id 
    FROM RecursiveGroups
    WHERE Path LIKE '%' + CAST(ParentGroupId AS NVARCHAR(MAX)) + '%'
);

PRINT 'Fixed circular references (set ParentGroupId to NULL): ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- ============================================
-- STEP 9: VERIFY - Run integrity check again
-- ============================================
PRINT 'STEP 9: Verification...';
PRINT '----------------------------------------';
PRINT 'Run Check_Foreign_Key_Integrity.sql again to verify all issues are fixed.';
PRINT '';

-- Quick verification
DECLARE @OrphanCount INT = 0;

-- Check P04 orphans
SELECT @OrphanCount = @OrphanCount + COUNT(*)
FROM dbo.P04_UserGroup p04
WHERE NOT EXISTS (SELECT 1 FROM dbo.P02_Group p02 WHERE p02.Id = p04.P02_GroupId);

-- Check P05 orphans (Page)
SELECT @OrphanCount = @OrphanCount + COUNT(*)
FROM dbo.P05_PageComponentMapping p05
WHERE NOT EXISTS (SELECT 1 FROM dbo.P01_Page p01 WHERE p01.Id = p05.P01_PageId);

-- Check P05 orphans (Component)
SELECT @OrphanCount = @OrphanCount + COUNT(*)
FROM dbo.P05_PageComponentMapping p05
WHERE NOT EXISTS (SELECT 1 FROM dbo.P03_Component p03 WHERE p03.Id = p05.P03_ComponentId);

-- Check P06 orphans (Group)
SELECT @OrphanCount = @OrphanCount + COUNT(*)
FROM dbo.P06_GroupPageComponentMapping p06
WHERE NOT EXISTS (SELECT 1 FROM dbo.P02_Group p02 WHERE p02.Id = p06.P02_GroupId);

-- Check P06 orphans (Mapping)
SELECT @OrphanCount = @OrphanCount + COUNT(*)
FROM dbo.P06_GroupPageComponentMapping p06
WHERE NOT EXISTS (SELECT 1 FROM dbo.P05_PageComponentMapping p05 WHERE p05.Id = p06.P05_PageComponentMappingId);

IF @OrphanCount = 0
    PRINT 'SUCCESS: No orphan records found!';
ELSE
    PRINT 'WARNING: Still found ' + CAST(@OrphanCount AS NVARCHAR(10)) + ' orphan records. Run Check_Foreign_Key_Integrity.sql for details.';

PRINT '';
PRINT '========================================';
PRINT 'FIX COMPLETED';
PRINT '========================================';
PRINT '';
PRINT 'Backups created with suffix: ' + @BackupDate;
PRINT 'To restore, copy data back from backup tables.';
