USE GTAS_VPP_TEST
GO

-- ============================================================================
-- PHẦN 1: FIX LOGIC SP - ĐẦY ĐỦ CÁC CỘT AUDIT (77500, 5615, GETDATE)
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_ComponentsToPage]
    @PageCode NVARCHAR(255),
    @ComponentCodesJson NVARCHAR(MAX),
    @GroupId UNIQUEIDENTIFIER,
    @MemberCompanyCode NVARCHAR(50) = '77500', 
    @UserId NVARCHAR(50) = '5615'        
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PageId UNIQUEIDENTIFIER;
    SELECT @PageId = Id FROM dbo.P01_Page WHERE PageCode = @PageCode;

    IF @PageId IS NULL RETURN;

    -- Bóc tách JSON
    DROP TABLE IF EXISTS #TempCodes;
    SELECT value AS ComponentCode INTO #TempCodes FROM OPENJSON(@ComponentCodesJson);

    -- Thêm Component vào cấu trúc Page (P05)
    INSERT INTO dbo.P05_PageComponentMapping (Id, P01_PageId, P03_ComponentId)
    SELECT NEWID(), @PageId, c.Id
    FROM #TempCodes t
    INNER JOIN dbo.P03_Component c ON c.ComponentCode = t.ComponentCode
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.P05_PageComponentMapping p05
        WHERE p05.P01_PageId = @PageId AND p05.P03_ComponentId = c.Id
    );

    -- Rút quyền của GROUP
    DELETE p06
    FROM dbo.P06_GroupPageComponentMapping p06
    INNER JOIN dbo.P05_PageComponentMapping p05 ON p06.P05_PageComponentMappingId = p05.Id
    WHERE p05.P01_PageId = @PageId
      AND p06.P02_GroupId = @GroupId
      AND p05.P03_ComponentId NOT IN (
          SELECT c.Id FROM #TempCodes t
          INNER JOIN dbo.P03_Component c ON c.ComponentCode = t.ComponentCode
      );

    -- Cấp quyền (Bổ sung UpdateUserId và UpdateDate)
    INSERT INTO dbo.P06_GroupPageComponentMapping 
        (P05_PageComponentMappingId, P02_GroupId, IsEnable, IsVisible, MemberCompanyCode, CreateUserId, CreateDate, UpdateUserId, UpdateDate)
    SELECT 
        p05.Id, @GroupId, 1, 1, @MemberCompanyCode, @UserId, GETDATE(), @UserId, GETDATE()
    FROM #TempCodes t
    INNER JOIN dbo.P03_Component c ON c.ComponentCode = t.ComponentCode
    INNER JOIN dbo.P05_PageComponentMapping p05 ON p05.P03_ComponentId = c.Id AND p05.P01_PageId = @PageId
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.P06_GroupPageComponentMapping p06
        WHERE p06.P05_PageComponentMappingId = p05.Id AND p06.P02_GroupId = @GroupId
    );
    
    DROP TABLE IF EXISTS #TempCodes;
END
GO

-- ============================================================================
-- PHẦN 2: RESET DỮ LIỆU SẠCH (PAGE & COMPONENT)
-- ============================================================================
DELETE FROM dbo.P06_GroupPageComponentMapping 
WHERE P05_PageComponentMappingId IN (
    SELECT Id FROM dbo.P05_PageComponentMapping 
    WHERE P01_PageId IN (SELECT Id FROM dbo.P01_Page WHERE PageCode LIKE '000%' OR PageCode IN ('SIDEBAR', 'DASHBOARD', 'LIBRARY', 'REPORT', 'PERMISSION'))
);
DELETE FROM dbo.P05_PageComponentMapping 
WHERE P01_PageId IN (SELECT Id FROM dbo.P01_Page WHERE PageCode LIKE '000%' OR PageCode IN ('SIDEBAR', 'DASHBOARD', 'LIBRARY', 'REPORT', 'PERMISSION'));

DELETE FROM dbo.P03_Component WHERE ComponentCode LIKE '000%' OR ComponentCode NOT LIKE '%[0-9]%';
DELETE FROM dbo.P01_Page WHERE PageCode LIKE '000%' OR PageCode IN ('SIDEBAR', 'DASHBOARD', 'LIBRARY', 'REPORT', 'PERMISSION');
GO

-- ============================================================================
-- PHẦN 3: KHỞI TẠO PAGE & COMPONENT (FULL TEXT)
-- ============================================================================
EXEC dbo.sp_SavePage @PageCode = N'SIDEBAR', @PageName = N'Sidebar Menu', @PageType = N'Menu', @PageDescription = N'Root Sidebar';
EXEC dbo.sp_SavePage @PageCode = N'DASHBOARD', @PageName = N'Dashboard', @PageType = N'Page', @PageDescription = N'Request Workspace';
EXEC dbo.sp_SavePage @PageCode = N'LIBRARY', @PageName = N'Library', @PageType = N'Page', @PageDescription = N'Categories';
EXEC dbo.sp_SavePage @PageCode = N'REPORT', @PageName = N'Report', @PageType = N'Page', @PageDescription = N'System Reports';
EXEC dbo.sp_SavePage @PageCode = N'PERMISSION', @PageName = N'Permission', @PageType = N'Page', @PageDescription = N'Security';

-- Menu Components
EXEC dbo.sp_SaveComponent @ComponentCode = N'MENU_DASHBOARD', @ComponentName = N'Menu - Dashboard', @ComponentDescription = N'View Dashboard';
EXEC dbo.sp_SaveComponent @ComponentCode = N'MENU_LIBRARY', @ComponentName = N'Menu - Library', @ComponentDescription = N'View Library';
EXEC dbo.sp_SaveComponent @ComponentCode = N'MENU_REPORT', @ComponentName = N'Menu - Report', @ComponentDescription = N'View Report';
EXEC dbo.sp_SaveComponent @ComponentCode = N'MENU_PERMISSION', @ComponentName = N'Menu - Permission', @ComponentDescription = N'View Permission';

-- Dashboard Components
EXEC dbo.sp_SaveComponent @ComponentCode = N'REQUEST_ORDER', @ComponentName = N'Request Order', @ComponentDescription = N'Orders Tab';
EXEC dbo.sp_SaveComponent @ComponentCode = N'REQUEST_HISTORY', @ComponentName = N'Request History', @ComponentDescription = N'History Tab';
EXEC dbo.sp_SaveComponent @ComponentCode = N'REQUEST_PRODUCT_CATALOG', @ComponentName = N'Request Product Catalog', @ComponentDescription = N'Catalog Tab';
EXEC dbo.sp_SaveComponent @ComponentCode = N'REQUEST_DEPARTMENT_SUMMARY', @ComponentName = N'Request Dept Summary', @ComponentDescription = N'Dept Summary Tab';
EXEC dbo.sp_SaveComponent @ComponentCode = N'REQUEST_ALL_ORDERS_SUMMARY', @ComponentName = N'Request All Orders Summary', @ComponentDescription = N'Admin Summary';
EXEC dbo.sp_SaveComponent @ComponentCode = N'REQUEST_ADMIN_APPROVAL', @ComponentName = N'Request Admin Approval', @ComponentDescription = N'Admin Approval';

-- Library Components
EXEC dbo.sp_SaveComponent @ComponentCode = N'LIBRARY_CLASS', @ComponentName = N'Library - Class', @ComponentDescription = N'Class';
EXEC dbo.sp_SaveComponent @ComponentCode = N'LIBRARY_DEPARTMENT', @ComponentName = N'Library - Department', @ComponentDescription = N'Dept';
EXEC dbo.sp_SaveComponent @ComponentCode = N'LIBRARY_ITEM', @ComponentName = N'Library - Item', @ComponentDescription = N'Item';
EXEC dbo.sp_SaveComponent @ComponentCode = N'LIBRARY_CATEGORY', @ComponentName = N'Library - Category', @ComponentDescription = N'Category';
EXEC dbo.sp_SaveComponent @ComponentCode = N'LIBRARY_SUPPLIER', @ComponentName = N'Library - Supplier', @ComponentDescription = N'Supplier';

-- Report & Permission
EXEC dbo.sp_SaveComponent @ComponentCode = N'REPORT_VIEW', @ComponentName = N'Report - View', @ComponentDescription = N'View Report';
EXEC dbo.sp_SaveComponent @ComponentCode = N'PERMISSION_USER', @ComponentName = N'Permission - User', @ComponentDescription = N'User Auth';
EXEC dbo.sp_SaveComponent @ComponentCode = N'PERMISSION_COMPONENT', @ComponentName = N'Permission - Component', @ComponentDescription = N'Comp Mapping';
GO

-- ============================================================================
-- PHẦN 4: GÁN QUYỀN (SP ĐÃ FIX FULL CỘT)
-- ============================================================================
-- ADMIN
EXEC dbo.sp_ComponentsToPage @PageCode = N'SIDEBAR', @GroupId = '5823b49b-5925-4a89-846a-09063a36040c', @ComponentCodesJson = N'["MENU_DASHBOARD", "MENU_LIBRARY", "MENU_REPORT", "MENU_PERMISSION"]';
EXEC dbo.sp_ComponentsToPage @PageCode = N'DASHBOARD', @GroupId = '5823b49b-5925-4a89-846a-09063a36040c', @ComponentCodesJson = N'["REQUEST_ORDER", "REQUEST_HISTORY", "REQUEST_PRODUCT_CATALOG", "REQUEST_DEPARTMENT_SUMMARY", "REQUEST_ALL_ORDERS_SUMMARY", "REQUEST_ADMIN_APPROVAL"]';
EXEC dbo.sp_ComponentsToPage @PageCode = N'LIBRARY', @GroupId = '5823b49b-5925-4a89-846a-09063a36040c', @ComponentCodesJson = N'["LIBRARY_CLASS", "LIBRARY_DEPARTMENT", "LIBRARY_ITEM", "LIBRARY_CATEGORY", "LIBRARY_SUPPLIER"]';
EXEC dbo.sp_ComponentsToPage @PageCode = N'REPORT', @GroupId = '5823b49b-5925-4a89-846a-09063a36040c', @ComponentCodesJson = N'["REPORT_VIEW"]';
EXEC dbo.sp_ComponentsToPage @PageCode = N'PERMISSION', @GroupId = '5823b49b-5925-4a89-846a-09063a36040c', @ComponentCodesJson = N'["PERMISSION_USER", "PERMISSION_COMPONENT"]';

-- USER
EXEC dbo.sp_ComponentsToPage @PageCode = N'SIDEBAR', @GroupId = '388C6C3A-2801-42DC-BFC0-8A7741264596', @ComponentCodesJson = N'["MENU_DASHBOARD", "MENU_REPORT"]';
EXEC dbo.sp_ComponentsToPage @PageCode = N'DASHBOARD', @GroupId = '388C6C3A-2801-42DC-BFC0-8A7741264596', @ComponentCodesJson = N'["REQUEST_ORDER", "REQUEST_HISTORY", "REQUEST_PRODUCT_CATALOG", "REQUEST_DEPARTMENT_SUMMARY"]';
EXEC dbo.sp_ComponentsToPage @PageCode = N'REPORT', @GroupId = '388C6C3A-2801-42DC-BFC0-8A7741264596', @ComponentCodesJson = N'["REPORT_VIEW"]';
GO

-- KIỂM TRA
SELECT p.PageCode, c.ComponentCode, gpcm.P02_GroupId
FROM dbo.P05_PageComponentMapping pcm
INNER JOIN dbo.P01_Page p ON pcm.P01_PageId = p.Id
INNER JOIN dbo.P03_Component c ON pcm.P03_ComponentId = c.Id
INNER JOIN dbo.P06_GroupPageComponentMapping gpcm ON pcm.Id = gpcm.P05_PageComponentMappingId
ORDER BY p.PageCode, c.ComponentCode;