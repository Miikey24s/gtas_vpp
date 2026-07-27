-- Legacy credential verification is intentionally unavailable after the
-- app-owned ASP.NET Core Identity cutover. GTAS_MENU remains read-only for
-- historical display fallback during the compatibility window.
DROP PROCEDURE IF EXISTS dbo.sp_Authen_Login;
GO
