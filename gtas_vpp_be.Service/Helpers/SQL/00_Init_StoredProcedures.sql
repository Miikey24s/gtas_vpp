USE GTAS_VPP_TEST;
GO
-- ================================================
-- 1. TẠO MOCK DATABASE 'GTAS_MENU' VÀ BẢNG 'tblUsers' Ở LOCAL
-- ================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'GTAS_MENU')
BEGIN
    CREATE DATABASE GTAS_MENU;
    PRINT N'Đã tạo Database GTAS_MENU giả lập';
END
GO

IF EXISTS (SELECT * FROM GTAS_MENU.sys.tables WHERE name = 'tblUsers')
BEGIN
    EXEC('USE GTAS_MENU; DROP TABLE tblUsers;');
END

EXEC('
USE GTAS_MENU;
CREATE TABLE tblUsers (
    UserID INT PRIMARY KEY,
    UserLogin NVARCHAR(100),
    PasswordChar NVARCHAR(100),
    FullName NVARCHAR(250),
    EmailAddress1 NVARCHAR(250),
    EmailAddress2 NVARCHAR(250),
    GoogleEmail NVARCHAR(250),
    PhoneNo1 NVARCHAR(50),
    PhoneNo2 NVARCHAR(50),
    IsInactiveFlg BIT DEFAULT 0,
    IsLockedFlg BIT DEFAULT 0
);

INSERT INTO tblUsers (UserID, UserLogin, PasswordChar, FullName, EmailAddress1, GoogleEmail, IsInactiveFlg, IsLockedFlg)
VALUES 
(4519, ''google'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Google'', ''google@ppj-international.com'', ''google@ppj-international.com'', 0, 0),
(1, ''admin'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Admin System'', ''admin@local.com'', ''admin@local.com'', 0, 0);
');
PRINT N'Đã Xóa bảng cũ, Tạo lại bảng tblUsers mới có đủ cột và chèn dữ liệu mẫu trong DB GTAS_MENU';
GO

-- ================================================
-- 2. KHỞI TẠO LINKED SERVER BỊ THIẾU (TRỞ VỀ LOCALHOST)
-- ================================================
-- Mục đích: Đánh lừa câu truy vấn [172.16.39.162].GTAS_MENU.dbo.tblUsers trong Authen.sql trở về localhost

-- Xoá Linked Server cũ nếu cấu hình bị sai
IF EXISTS (SELECT srv.name FROM sys.servers srv WHERE srv.name = N'172.16.39.162')
BEGIN
    EXEC master.dbo.sp_dropserver @server=N'172.16.39.162', @droplogins='droplogins';
END
GO

PRINT N'Đang tạo Linked Server giả lập "172.16.39.162" (trỏ về chính máy local, bỏ qua check chứng chỉ SSL)...';

-- Dùng MSOLEDBSQL và thêm chuỗi kết nối bỏ qua TrustServerCertificate để tránh lỗi SSL
DECLARE @localServerName NVARCHAR(128) = @@SERVERNAME;
EXEC master.dbo.sp_addlinkedserver 
    @server = N'172.16.39.162', 
    @srvproduct=N'', 
    @provider=N'MSOLEDBSQL', 
    @datasrc=@localServerName,
    @provstr=N'TrustServerCertificate=yes;Encrypt=Optional;';

EXEC master.dbo.sp_addlinkedsrvlogin @rmtsrvname = N'172.16.39.162', @useself = N'True';

EXEC master.dbo.sp_serveroption @server=N'172.16.39.162', @optname=N'rpc', @optvalue=N'true';
EXEC master.dbo.sp_serveroption @server=N'172.16.39.162', @optname=N'rpc out', @optvalue=N'true';
EXEC master.dbo.sp_serveroption @server=N'172.16.39.162', @optname=N'data access', @optvalue=N'true';
GO

-- ================================================
-- 3. TẠO VIEW v_Users (cross-database từ GTAS_MENU)
-- ================================================

IF OBJECT_ID('dbo.v_Users', 'V') IS NOT NULL
    DROP VIEW dbo.v_Users;
GO

CREATE VIEW dbo.v_Users AS
SELECT 
    UserID,
    UserLogin,
    PasswordChar,
    FullName,
    EmailAddress1,
    EmailAddress2,
    GoogleEmail,
    PhoneNo1,
    PhoneNo2
FROM GTAS_MENU.dbo.tblUsers
WHERE IsInactiveFlg = 0 AND IsLockedFlg = 0;
GO

PRINT N'Đã tạo View v_Users trong DB chính (đọc cross-database từ GTAS_MENU.dbo.tblUsers)';
GO
-- Tạo view dbo.v_WFXCompany (sp login)
CREATE OR ALTER VIEW dbo.v_WFXCompany
AS
SELECT DISTINCT
p06.MemberCompanyCode,
CAST(NULL AS NVARCHAR(250)) AS CompanyName,
CAST(NULL AS NVARCHAR(100)) AS CompanyShortName
FROM dbo.P06_GroupPageComponentMapping p06
WHERE p06.MemberCompanyCode IS NOT NULL;
GO
-- ================================================
-- 4. KHỞI TẠO CÁC STORED PROCEDURE RỖNG 
-- ================================================

IF  OBJECT_ID('dbo.sp_Authen', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_Authen AS BEGIN SET NOCOUNT ON; END');
    PRINT 'Created empty dbo.sp_Authen';
END
GO

IF OBJECT_ID('dbo.sp_Authen_Login', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_Authen_Login AS BEGIN SET NOCOUNT ON; END');
    PRINT 'Created empty dbo.sp_Authen_Login';
END
GO

IF OBJECT_ID('dbo.sp_Authen_TabUser_UserList', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_Authen_TabUser_UserList AS BEGIN SET NOCOUNT ON; END');
    PRINT 'Created empty dbo.sp_Authen_TabUser_UserList';
END
GO

IF OBJECT_ID('dbo.sp_Authen_TabUser_SearchUser', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_Authen_TabUser_SearchUser AS BEGIN SET NOCOUNT ON; END');
    PRINT 'Created empty dbo.sp_Authen_TabUser_SearchUser';
END
GO

IF OBJECT_ID('dbo.sp_Authen_GetPermissionSinglePage', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_Authen_GetPermissionSinglePage AS BEGIN SET NOCOUNT ON; END');
    PRINT 'Created empty dbo.sp_Authen_GetPermissionSinglePage';
END
GO

IF OBJECT_ID('dbo.sp_Authen_CreateNewGroup', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_Authen_CreateNewGroup AS BEGIN SET NOCOUNT ON; END');
    PRINT 'Created empty dbo.sp_Authen_CreateNewGroup';
END
GO

IF OBJECT_ID('dbo.sp_Authen_CopyFromGroup', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_Authen_CopyFromGroup AS BEGIN SET NOCOUNT ON; END');
    PRINT 'Created empty dbo.sp_Authen_CopyFromGroup';
END
GO

IF OBJECT_ID('dbo.sp_Authen_Permission_GetPageWithComponentByGroupId', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_Authen_Permission_GetPageWithComponentByGroupId AS BEGIN SET NOCOUNT ON; END');
    PRINT 'Created empty dbo.sp_Authen_Permission_GetPageWithComponentByGroupId';
END
GO

IF OBJECT_ID('dbo.sp_CreateNewPageComponent', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_CreateNewPageComponent AS BEGIN SET NOCOUNT ON; END');
    PRINT 'Created empty dbo.sp_CreateNewPageComponent';
END
GO