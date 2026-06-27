-- ============================================================================
-- 01_Views.sql — Tạo/Cập nhật Views cho GTAS_VPP
-- Idempotent: Dùng CREATE OR ALTER
-- ============================================================================

-- View: v_Users (đọc user từ GTAS_MENU qua cross-database)
CREATE OR ALTER VIEW dbo.v_Users
AS
SELECT UserID, UserLogin, PasswordChar, FullName, EmailAddress1, EmailAddress2,
       GoogleEmail, PhoneNo1, PhoneNo2, MemberCompanyCode, DepartmentCode, MemberCompanyName
FROM   GTAS_MENU.dbo.tblUsers
WHERE (IsInactiveFlg = 0) AND (IsLockedFlg = 0);
GO

-- View: v_WFXCompany (lấy danh sách công ty từ P06 mapping)
CREATE OR ALTER VIEW dbo.v_WFXCompany
AS
SELECT DISTINCT
    p06.MemberCompanyCode,
    COALESCE(u.MemberCompanyName, CAST(p06.MemberCompanyCode AS NVARCHAR(50))) AS CompanyName,
    CAST(NULL AS NVARCHAR(100)) AS CompanyShortName
FROM dbo.P06_GroupPageComponentMapping p06
LEFT JOIN GTAS_MENU.dbo.tblUsers u
    ON u.MemberCompanyCode = p06.MemberCompanyCode
   AND u.IsInactiveFlg = 0
WHERE p06.MemberCompanyCode IS NOT NULL;
GO
