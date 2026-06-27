-- ============================================================================
-- 00_Init_GTAS_MENU.sql — Tạo DB GTAS_MENU + bảng tblUsers
-- Idempotent: Chỉ tạo nếu chưa tồn tại
-- NOTE: Không cần Linked Server vì GTAS_MENU cùng instance với GTAS_VPP
-- ============================================================================

-- 1. Tạo Database GTAS_MENU nếu chưa có
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'GTAS_MENU')
BEGIN
    CREATE DATABASE GTAS_MENU;
END
GO

-- 2. Tạo bảng tblUsers (14 cột — đúng schema thật)
IF NOT EXISTS (SELECT * FROM GTAS_MENU.sys.tables WHERE name = 'tblUsers')
BEGIN
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
        IsLockedFlg BIT DEFAULT 0,
        MemberCompanyCode BIGINT NULL,
        DepartmentCode NVARCHAR(50) NULL,
        MemberCompanyName NVARCHAR(250) NULL
    );
    ');
END
GO

-- 3. Insert users (12 users: google + 5 admin + 5 user + 1 IT user)
IF NOT EXISTS (SELECT 1 FROM GTAS_MENU.dbo.tblUsers WHERE UserID = 1)
BEGIN
    EXEC('
    INSERT INTO GTAS_MENU.dbo.tblUsers (UserID, UserLogin, PasswordChar, FullName, EmailAddress1, GoogleEmail, IsInactiveFlg, IsLockedFlg, MemberCompanyCode, DepartmentCode, MemberCompanyName)
    VALUES
    (1,    ''admin'',        ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Admin System'',  ''admin@local.com'',          ''admin@local.com'',          0, 0, NULL, NULL, NULL),
    (4519, ''google'',       ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Google'',        ''google@ppj-international.com'', ''google@ppj-international.com'', 0, 0, 77500, ''IT'', ''PHONG PHU INTERNALTIONAL JSC''),
    (4520, ''test_admin_1'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Test Admin 1'',  ''test_admin_1@local.com'',   ''test_admin_1@local.com'',   0, 0, 77500, ''IT'',         ''PHONG PHU INTERNALTIONAL JSC''),
    (4521, ''test_admin_2'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Test Admin 2'',  ''test_admin_2@local.com'',   ''test_admin_2@local.com'',   0, 0, 77500, ''IT'',         ''PHONG PHU INTERNALTIONAL JSC''),
    (4522, ''test_admin_3'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Test Admin 3'',  ''test_admin_3@local.com'',   ''test_admin_3@local.com'',   0, 0, 77500, ''IT'',         ''PHONG PHU INTERNALTIONAL JSC''),
    (4523, ''test_admin_4'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Test Admin 4'',  ''test_admin_4@local.com'',   ''test_admin_4@local.com'',   0, 0, 77500, ''IT'',         ''PHONG PHU INTERNALTIONAL JSC''),
    (4524, ''test_admin_5'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Test Admin 5'',  ''test_admin_5@local.com'',   ''test_admin_5@local.com'',   0, 0, 77500, ''IT'',         ''PHONG PHU INTERNALTIONAL JSC''),
    (4530, ''test_user_11'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Test User 11'',  ''test_user_11@local.com'',   ''test_user_11@local.com'',   0, 0, 77500, ''IT'',         ''PHONG PHU INTERNALTIONAL JSC''),
    (4531, ''test_user_12'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Test User 12'',  ''test_user_12@local.com'',   ''test_user_12@local.com'',   0, 0, 77500, ''IT'',         ''PHONG PHU INTERNALTIONAL JSC''),
    (4532, ''test_user_13'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Test User 13'',  ''test_user_13@local.com'',   ''test_user_13@local.com'',   0, 0, 77500, ''IT'',         ''PHONG PHU INTERNALTIONAL JSC''),
    (4533, ''test_user_14'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Test User 14'',  ''test_user_14@local.com'',   ''test_user_14@local.com'',   0, 0, 77500, ''IT'',         ''PHONG PHU INTERNALTIONAL JSC''),
    (4534, ''test_user_15'', ''wiSEc6nf/dK/Vu0E738j8Q=='', ''Test User 15'',  ''test_user_15@local.com'',   ''test_user_15@local.com'',   0, 0, 77500, ''IT'',         ''PHONG PHU INTERNALTIONAL JSC'');
    ');
END
GO
