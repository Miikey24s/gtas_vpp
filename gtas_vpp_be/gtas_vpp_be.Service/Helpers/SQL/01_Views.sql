-- ============================================================================
-- 01_Views.sql — Tạo/Cập nhật Views cho GTAS_VPP
-- Idempotent: Dùng CREATE OR ALTER
-- ============================================================================

-- View: v_Users (đọc user từ GTAS_MENU qua cross-database)
CREATE OR ALTER VIEW dbo.v_Users
AS
SELECT app.Id AS UserID,
       app.UserName AS UserLogin,
       app.FullName,
       app.Email AS EmailAddress1,
       CAST(NULL AS NVARCHAR(256)) AS EmailAddress2,
       CAST(NULL AS NVARCHAR(256)) AS GoogleEmail,
       app.PhoneNumber AS PhoneNo1,
       CAST(NULL AS NVARCHAR(50)) AS PhoneNo2,
       app.MemberCompanyCode,
       dept.LEX02Code AS DepartmentCode,
       CAST(app.MemberCompanyCode AS NVARCHAR(250)) AS MemberCompanyName
FROM dbo.AspNetUsers app
LEFT JOIN dbo.P04_UserGroup membership
    ON membership.AccountId = app.Id
   AND membership.UserId = app.Id
   AND membership.IsDeleted = 0
LEFT JOIN dbo.LEX02_CompanyDepartmentLocation dept
    ON dept.Id = membership.LEX02_CompanyDepartmentLocationId
   AND dept.IsDeleted = 0
UNION ALL
SELECT legacy.UserID,
       legacy.UserLogin,
       legacy.FullName,
       legacy.EmailAddress1,
       legacy.EmailAddress2,
       legacy.GoogleEmail,
       legacy.PhoneNo1,
       legacy.PhoneNo2,
       legacy.MemberCompanyCode,
       legacy.DepartmentCode,
       legacy.MemberCompanyName
FROM GTAS_MENU.dbo.tblUsers legacy
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.AspNetUsers app WHERE app.Id = legacy.UserID
);
GO

-- View: v_WFXCompany (lấy danh sách công ty từ P06 mapping)
CREATE OR ALTER VIEW dbo.v_WFXCompany
AS
SELECT DISTINCT
    p06.MemberCompanyCode,
    CAST(p06.MemberCompanyCode AS NVARCHAR(50)) AS CompanyName,
    CAST(NULL AS NVARCHAR(100)) AS CompanyShortName
FROM dbo.P06_GroupPageComponentMapping p06;
GO
