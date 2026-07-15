-- ============================================================================
-- 00_Init_GTAS_MENU.sql — GTAS_MENU schema/reference bootstrap
-- Idempotent: creates only the legacy compatibility database and table.
-- Human/demo accounts are intentionally not part of database bootstrap.
-- NOTE: GTAS_MENU is on the same SQL Server instance as GTAS_VPP.
-- ============================================================================

-- 1. Create the compatibility database when it does not exist.
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'GTAS_MENU')
BEGIN
    CREATE DATABASE GTAS_MENU;
END
GO

-- 2. Create the legacy user table without inserting any account.
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
