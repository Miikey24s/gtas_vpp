-- ============================================================================
-- 02_StoredProcedures.sql - All Stored Procedures (auto-generated from DB)
-- Idempotent: Uses CREATE OR ALTER
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.sp_Authen
    @SpType NVARCHAR(MAX),
    @Param NVARCHAR(MAX)
AS
BEGIN TRY
    -- Whitelist: only allow known sub-procedures
    IF @SpType NOT IN (
        N'sp_Authen_Login',
        N'sp_Authen_GetPermissionSinglePage',
        N'sp_Authen_TabUser_UserList',
        N'sp_Authen_TabUser_SearchUser',
        N'sp_Authen_Permission_GetPageWithComponentByGroupId',
        N'sp_Authen_CreateNewGroup',
        N'sp_Authen_CopyFromGroup'
    )
    BEGIN
        SELECT IsSuccess = CAST(0 AS BIT),
               ErrorMess = 'Invalid SpType',
               ResData = '';
        RETURN;
    END;

    DECLARE @Query NVARCHAR(MAX) = N'Execute dbo.' + @SpType + N' @Param = @Param;';

    IF (
           CHARINDEX('Copy', @SpType) > 0
           OR CHARINDEX('Create', @SpType) > 0
           OR CHARINDEX('Insert', @SpType) > 0
           OR CHARINDEX('Update', @SpType) > 0
           OR CHARINDEX('Delete', @SpType) > 0
       )
    BEGIN TRY
        SET XACT_ABORT ON;
        SET NOCOUNT ON;
        BEGIN TRAN;
        EXECUTE sys.sp_executesql @Query, N'@Param NVARCHAR(MAX)', @Param = @Param;
        COMMIT;
        RETURN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
        SELECT IsSuccess = CAST(0 AS BIT),
               ErrorMess = ERROR_MESSAGE(),
               ResData = '';
        RETURN;
    END CATCH;
    ELSE
    BEGIN
        EXECUTE sys.sp_executesql @Query, N'@Param NVARCHAR(MAX)', @Param = @Param;
    END;
END TRY
BEGIN CATCH
    SELECT IsSuccess = CAST(0 AS BIT),
           ErrorMess = ERROR_MESSAGE(),
           ResData = '';
END CATCH;

GO


CREATE OR ALTER PROCEDURE dbo.sp_Authen_CopyFromGroup @Param NVARCHAR(max)
AS
BEGIN TRY
DROP TABLE IF EXISTS #CopyFromGroup;
        SELECT a.GroupId,
               a.GroupName,
               a.Description,
               a.CreateUserId
        INTO #CopyFromGroup
        FROM
            OPENJSON(@Param)
            WITH
            (
                GroupId UNIQUEIDENTIFIER '$.GroupId',
                GroupName NVARCHAR(150) '$.GroupName',
                Description NVARCHAR(MAX) '$.Description',
                CreateUserId INT '$.CreateUserId'
            ) a;

        DECLARE @CopyFromGroup_NewGroupId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO dbo.P02_Group
        (
            Id,
            GroupName,
            Description,
            CreateUserId,
            CreateDate,
            UpdateUserId,
            UpdateDate,
            IsDeleted
        )
        SELECT @CopyFromGroup_NewGroupId,
               GroupName,
               Description,
               CreateUserId,
               GETDATE(),
               CreateUserId,
               GETDATE(),
               0
        FROM #CopyFromGroup;

        DROP TABLE IF EXISTS #tblInsertP06_Copy;
        SELECT DISTINCT
               P05_PageComponentMappingId,
               @CopyFromGroup_NewGroupId AS GroupId,
               IsEnable,
               IsVisible
        INTO #tblInsertP06_Copy
        FROM dbo.P06_GroupPageComponentMapping p06 (NOLOCK)
        WHERE p06.P02_GroupId =
        (
            SELECT TOP 1 a.GroupId FROM #CopyFromGroup a
        );

        DECLARE @CopyFromGroup_UserId INT =
                (
                    SELECT TOP 1 CreateUserId FROM #CopyFromGroup
                );

        INSERT INTO dbo.P06_GroupPageComponentMapping
        (
            P05_PageComponentMappingId,
            P02_GroupId,
            CreateUserId,
            CreateDate,
            UpdateUserId,
            UpdateDate,
            IsEnable,
            IsVisible,
            MemberCompanyCode
        )
        SELECT p06.P05_PageComponentMappingId,
               p06.GroupId,
               @CopyFromGroup_UserId,
               GETDATE(),
               @CopyFromGroup_UserId,
               GETDATE(),
               p06.IsEnable,
               p06.IsVisible,
               77500
        FROM #tblInsertP06_Copy p06;
        SELECT ResData = 'Success',
               IsSuccess = CAST(1 AS BIT),
               ErrorMess = '';
END TRY
BEGIN CATCH
SELECT ResData = '',
           IsSuccess = CAST(0 AS BIT),
           ErrorMess = @@ERROR;
END CATCH

GO


CREATE OR ALTER PROCEDURE dbo.sp_Authen_CreateNewGroup @Param NVARCHAR(max)
AS
BEGIN TRY
DROP TABLE IF EXISTS #CreateNewGroup;
        SELECT a.GroupId,
               a.GroupName,
               a.Description,
               a.CreateUserId
        INTO #CreateNewGroup
        FROM
            OPENJSON(@Param)
            WITH
            (
                GroupId UNIQUEIDENTIFIER '$.GroupId',
                GroupName NVARCHAR(150) '$.GroupName',
                Description NVARCHAR(MAX) '$.Description',
                CreateUserId INT '$.CreateUserId'
            ) a;

        IF NOT EXISTS
        (
            SELECT *
            FROM dbo.P02_Group p02
                JOIN #CreateNewGroup gr
                    ON p02.GroupName = gr.GroupName
        )
        BEGIN TRY
            DECLARE @NewGroupId UNIQUEIDENTIFIER = NEWID();
            UPDATE #CreateNewGroup
            SET GroupId = @NewGroupId;
            INSERT INTO dbo.P02_Group
            (
                Id,
                GroupName,
                Description,
                CreateUserId,
                CreateDate,
                UpdateUserId,
                UpdateDate,
                IsDeleted
            )
            SELECT @NewGroupId,
                   GroupName,
                   Description,
                   CreateUserId,
                   GETDATE(),
                   CreateUserId,
                   GETDATE(),
                   0
            FROM #CreateNewGroup;

            DROP TABLE IF EXISTS #tblInsertP06;
            SELECT DISTINCT
                   P05_PageComponentMappingId,
                   (
                       SELECT TOP 1 GroupId FROM #CreateNewGroup
                   ) AS GroupId
            INTO #tblInsertP06
            FROM dbo.P06_GroupPageComponentMapping;

            DECLARE @CreateNewGroup_UserId INT =
                    (
                        SELECT TOP 1 CreateUserId FROM #CreateNewGroup
                    );

            INSERT INTO dbo.P06_GroupPageComponentMapping
            (
                P05_PageComponentMappingId,
                P02_GroupId,
                CreateUserId,
                CreateDate,
                UpdateUserId,
                UpdateDate,
                IsEnable,
                IsVisible,
                MemberCompanyCode
            )
            SELECT p06.P05_PageComponentMappingId,
                   p06.GroupId,
                   @CreateNewGroup_UserId,
                   GETDATE(),
                   @CreateNewGroup_UserId,
                   GETDATE(),
                   0,
                   0,
                   77500
            FROM #tblInsertP06 p06;

            DROP TABLE IF EXISTS #tblInsertP06;
            DROP TABLE IF EXISTS #CreateNewGroup;

            SELECT ResData = 'Success',
                   IsSuccess = CAST(1 AS BIT),
                   ErrorMess = '';
        END TRY
        BEGIN CATCH
            SELECT ResData = 'Failed',
                   IsSuccess = CAST(1 AS BIT),
                   ErrorMess = '';
        END CATCH;
END TRY
BEGIN CATCH
SELECT ResData = '',
           IsSuccess = CAST(0 AS BIT),
           ErrorMess = @@ERROR;
END CATCH

GO
CREATE OR ALTER PROCEDURE [dbo].[sp_Authen_GetPermissionSinglePage] @Param NVARCHAR(MAX)
AS
BEGIN TRY
    --DECLARE @Param NVARCHAR(max) = '{"UserId": 5615, "PageCode":"0001"}';
    DECLARE @GetPermissionSinglePage_UserId INT;
    DECLARE @GetPermissionSinglePage_PageCode NVARCHAR(100);

    SELECT @GetPermissionSinglePage_UserId = a.UserId,
           @GetPermissionSinglePage_PageCode = a.PageCode
    FROM
        OPENJSON(@Param)
        WITH        (
            UserId INT '$.userId',
            PageCode NVARCHAR(100) '$.pageCode'
        ) a;

    DROP TABLE IF EXISTS #GetPermissionSinglePage_tblRawData;
    SELECT DISTINCT
           us.UserID,
           us.UserLogin,
           us.PasswordChar,
           us.FullName,
           us.EmailAddress1,
           us.GoogleEmail,
           IsAdmin = IIF(p02.GroupName IN ('Admin', 'Administrator'), 1, 0),
           p01.Id AS PageId,
           p01.PageCode,
           p01.PageName,
           p01.Description AS PageDesctiprion,
           p02.Id AS GroupId,
           p02.GroupName,
           p03.Id AS ComponentId,
           p03.ComponentCode,
           p03.ComponentName,
           p03.Description AS ComponentDesctiprion,
           p06.IsEnable,
           p06.IsVisible
    INTO #GetPermissionSinglePage_tblRawData
    FROM GTAS_MENU.dbo.tblUsers us (NOLOCK) -- �? �?i th�nh localhost
        JOIN dbo.P04_UserGroup p04 (NOLOCK)
            ON p04.UserId = us.UserID
        JOIN dbo.P02_Group p02 (NOLOCK)
            ON p02.Id = p04.P02_GroupId
        JOIN dbo.P06_GroupPageComponentMapping p06 (NOLOCK)
            ON p06.P02_GroupId = p02.Id
        JOIN dbo.P05_PageComponentMapping p05 (NOLOCK)
            ON p05.Id = p06.P05_PageComponentMappingId
        JOIN dbo.P01_Page p01 (NOLOCK)
            ON p01.Id = p05.P01_PageId
        JOIN dbo.P03_Component p03 (NOLOCK)
            ON p03.Id = p05.P03_ComponentId
    WHERE 1 = 1
          AND us.IsInactiveFlg = 0
          AND us.IsLockedFlg = 0
          AND p01.IsDeleted = 0
          AND p02.IsDeleted = 0
          AND p04.IsDeleted = 0
          AND p03.IsDeleted = 0
          AND          (
              p06.IsVisible = 1
              OR p06.IsEnable = 1
          )
          AND us.UserID = @GetPermissionSinglePage_UserId
          AND p01.PageCode = @GetPermissionSinglePage_PageCode;

    DECLARE @GetPermissionSinglePage NVARCHAR(MAX);
    SELECT @GetPermissionSinglePage =    (
        SELECT DISTINCT
               pp.PageId,
               pp.PageCode,
               pp.PageName,
               pp.PageDesctiprion,
               List_Component =    (
        SELECT DISTINCT
               cp.ComponentId,
               cp.ComponentCode,
               cp.ComponentName,
               cp.ComponentDesctiprion,
               cp.IsEnable,
               cp.IsVisible,
               [Role] = cp.ComponentCode + CAST('_Visible_' AS NVARCHAR(MAX)) + CAST(cp.IsVisible AS NVARCHAR(MAX)),
               [Policy] = cp.ComponentCode + CAST('_Enable_' AS NVARCHAR(MAX)) + CAST(cp.IsEnable AS NVARCHAR(MAX))
        FROM #GetPermissionSinglePage_tblRawData cp (NOLOCK)
        WHERE cp.PageId = pp.PageId
        ORDER BY cp.ComponentName
        FOR JSON PATH, INCLUDE_NULL_VALUES
    )
        FROM #GetPermissionSinglePage_tblRawData pp (NOLOCK)
        WHERE 1 = 1
        ORDER BY pp.PageCode
        FOR JSON PATH, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER
    );
    SELECT ResData = @GetPermissionSinglePage,
           IsSuccess = CAST(1 AS BIT),
           ErrorMess = '';
END TRY
BEGIN CATCH
    SELECT ResData = '',
           IsSuccess = CAST(0 AS BIT),
           ErrorMess = @@ERROR;
END CATCH;
GO


CREATE OR ALTER PROCEDURE [dbo].[sp_Authen_Login] @Param NVARCHAR(MAX)
AS
BEGIN TRY
    SET @Param = REPLACE(@Param, '"{', '{');
    SET @Param = REPLACE(@Param, '}"', '}');
    SET @Param = REPLACE(@Param, '\"', '"');
    PRINT @Param;
    DECLARE @UserLogin NVARCHAR(100);
    DECLARE @PasswordChar NVARCHAR(MAX);

    SELECT @UserLogin = a.UserLogin,
           @PasswordChar = a.PasswordChar
    FROM
        OPENJSON(@Param)
        WITH
        (
            UserLogin NVARCHAR(100) '$.UserLogin',
            PasswordChar NVARCHAR(MAX) '$.PasswordChar'
        ) a;

    IF EXISTS
    (
        SELECT *
        FROM GTAS_MENU.dbo.tblUsers us (NOLOCK)
        --JOIN dbo.P04_UserGroup p04 (NOLOCK)
        --    ON p04.UserId = us.UserID
        WHERE us.UserLogin = @UserLogin
              AND us.PasswordChar = @PasswordChar
    )
    BEGIN
        DROP TABLE IF EXISTS #tblRawData;
        SELECT DISTINCT
               us.UserID,
               us.UserLogin,
               us.PasswordChar,
               us.FullName,
               us.EmailAddress1,
               us.GoogleEmail,
               IsAdmin = IIF(p02.GroupName IN ('Admin', 'Administrator'), 1, 0),
               p01.Id AS PageId,
               p01.PageCode,
               p01.PageName,
               p01.Description AS PageDesctiprion,
               p02.Id AS GroupId,
               p02.GroupName,
               p03.Id AS ComponentId,
               p03.ComponentCode,
               p03.ComponentName,
               p03.Description AS ComponentDesctiprion,
               p06.IsEnable,
               p06.IsVisible,
               com.MemberCompanyCode,
               MemberCompanyName = com.CompanyName,
               MemberCompanyShortName = com.CompanyShortName
        INTO #tblRawData
        FROM GTAS_MENU.dbo.tblUsers us (NOLOCK)
            JOIN dbo.P04_UserGroup p04 (NOLOCK)
                ON p04.UserId = us.UserID
            JOIN dbo.P02_Group p02 (NOLOCK)
                ON p02.Id = p04.P02_GroupId
            JOIN dbo.P06_GroupPageComponentMapping p06 (NOLOCK)
                ON p06.P02_GroupId = p02.Id
            JOIN dbo.P05_PageComponentMapping p05 (NOLOCK)
                ON p05.Id = p06.P05_PageComponentMappingId
            JOIN dbo.P01_Page p01 (NOLOCK)
                ON p01.Id = p05.P01_PageId
            JOIN dbo.P03_Component p03 (NOLOCK)
                ON p03.Id = p05.P03_ComponentId
            LEFT JOIN
            (
                SELECT DISTINCT
                       c.MemberCompanyCode,
                       c.CompanyName,
                       c.CompanyShortName
                FROM dbo.v_WFXCompany c (NOLOCK)
            ) AS com
                ON com.MemberCompanyCode = p06.MemberCompanyCode
        WHERE 1 = 1
              AND us.IsInactiveFlg = 0
              AND us.IsLockedFlg = 0
              AND p01.IsDeleted = 0
              AND p02.IsDeleted = 0
              AND p04.IsDeleted = 0
              AND p03.IsDeleted = 0
              AND
              (
                  p06.IsVisible = 1
                  OR p06.IsEnable = 1
              )
              AND us.UserLogin = @UserLogin;

        DECLARE @Login NVARCHAR(MAX);
        SET @Login =
        (
            SELECT DISTINCT
                   r.UserID,
                   r.UserLogin,
                   r.PasswordChar,
                   r.FullName,
                   r.EmailAddress1 AS Email,
                   r.GoogleEmail,
                   IsAdmin = CAST(r.IsAdmin AS BIT),
                   r.GroupId,
                   r.GroupName,
                   r.MemberCompanyCode,
                   r.MemberCompanyName,
                   r.MemberCompanyShortName,
                   List_PagePermission =
                   (
                       SELECT DISTINCT
                              pp.PageId,
                              pp.PageCode,
                              pp.PageName,
                              pp.PageDesctiprion,
                              List_Component =
                              (
                                  SELECT DISTINCT
                                         cp.ComponentId,
                                         cp.ComponentCode,
                                         cp.ComponentName,
                                         cp.ComponentDesctiprion,
                                         cp.IsEnable,
                                         cp.IsVisible,
                                         [Role] = cp.ComponentCode + CAST('_Visible_' AS NVARCHAR(MAX))
                                                  + CAST(cp.IsVisible AS NVARCHAR(MAX)),
                                         [Policy] = cp.ComponentCode + CAST('_Enable_' AS NVARCHAR(MAX))
                                                    + CAST(cp.IsEnable AS NVARCHAR(MAX))
                                  FROM #tblRawData cp (NOLOCK)
                                  WHERE cp.PageId = pp.PageId
                                  FOR JSON PATH, INCLUDE_NULL_VALUES
                              )
                       FROM #tblRawData pp (NOLOCK)
                       WHERE pp.UserID = r.UserID
                       FOR JSON PATH, INCLUDE_NULL_VALUES
                   )
            FROM #tblRawData r
            WHERE 1 = 1
            FOR JSON PATH, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER
        );
        SELECT IsSuccess = CAST(1 AS BIT),
               ErrorMess = '',
               ResData = CAST(@Login AS NVARCHAR(MAX));
    END;
END TRY
BEGIN CATCH
    SELECT IsSuccess = CAST(0 AS BIT),
           ErrorMess = 'Error number ' + CAST(ERROR_NUMBER() AS NVARCHAR(MAX)) + ' of ' + ERROR_PROCEDURE()
                       + ' at line number ' + CAST(ERROR_LINE() AS NVARCHAR(MAX)) + ' with message ' + ERROR_MESSAGE(),
           ResData = '';
END CATCH;

GO


CREATE OR ALTER PROCEDURE dbo.sp_Authen_Permission_GetPageWithComponentByGroupId @Param NVARCHAR(max)
AS
BEGIN TRY
--DECLARE @Param NVARCHAR(max) = N'{"GroupId":"F303164E-B9B9-4093-9A84-D4BAC8401AE5"}'
        DECLARE @GroupId UNIQUEIDENTIFIER =
                (
                    SELECT TOP 1
                           a.GroupId
                    FROM
                        OPENJSON(@Param)
                        WITH
                        (
                            GroupId UNIQUEIDENTIFIER '$.GroupId'
                        ) a
                );
        DROP TABLE IF EXISTS #Permission_GetPageWithComponentByGroupId;
        SELECT p01.Id AS PageId,
               p06.P02_GroupId GroupId,
               p01.PageCode,
               p01.PageName,
               p01.Description,
               p01.CreateUserId,
               p01.CreateDate,
               CreateUserName =
               (
                   SELECT TOP 1
                          FullName
                   FROM dbo.v_Users u (NOLOCK)
                   WHERE u.UserID = p01.CreateUserId
               ),
               p01.UpdateUserId,
               p01.UpdateDate,
               UpdateUserName =
               (
                   SELECT TOP 1
                          FullName
                   FROM dbo.v_Users u (NOLOCK)
                   WHERE u.UserID = p01.CreateUserId
               ),
               p03.Id ComponentId,
               p03.ComponentCode,
               p03.ComponentName,
               p06.IsVisible,
               p06.IsEnable,
               p06.MemberCompanyCode,
               p01.IsDeleted
        INTO #Permission_GetPageWithComponentByGroupId
        FROM dbo.P01_Page p01 (NOLOCK)
            LEFT JOIN dbo.P05_PageComponentMapping p05 (NOLOCK)
                ON p05.P01_PageId = p01.Id
            LEFT JOIN dbo.P03_Component p03 (NOLOCK)
                ON p03.Id = p05.P03_ComponentId
            LEFT JOIN dbo.P06_GroupPageComponentMapping p06 (NOLOCK)
                ON p06.P05_PageComponentMappingId = p05.Id
        WHERE 1 = 1
              AND p03.IsDeleted = 0
              AND p06.P02_GroupId = @GroupId;

        DROP TABLE IF EXISTS #Permission_GetPageWithComponentByGroupId_Company;
        CREATE TABLE #Permission_GetPageWithComponentByGroupId_Company
        (
            MemberCompanyCode BIGINT,
            CompanyName NVARCHAR(MAX),
            CompanyShortName NVARCHAR(MAX)
        );
        INSERT INTO #Permission_GetPageWithComponentByGroupId_Company
        (
            MemberCompanyCode,
            CompanyName,
            CompanyShortName
        )
        VALUES
        (   77500,                          -- MemberCompanyCode - bigint
            N'PHONG PHU INTERNATIONAL JSC', -- CompanyName - nvarchar(max)
            N'PPJ'                          -- CompanyShortName - nvarchar(max)
            );

        DECLARE @Permission_GetPageWithComponentByGroupId NVARCHAR(MAX);

        SET @Permission_GetPageWithComponentByGroupId =
        (
            SELECT DISTINCT
                   r.GroupId,
                   r.PageId,
                   r.PageCode,
                   r.PageName,
                   r.Description,
                   r.CreateUserId,
                   r.CreateDate,
                   r.CreateUserName,
                   r.UpdateUserId,
                   r.UpdateDate,
                   r.UpdateUserName,
                   r.IsDeleted,
                   List_Component =
                   (
                       SELECT p03.Id ComponentId,
                              p03.ComponentCode,
                              p03.ComponentName,
                              p03.Description,
                              r1.IsVisible,
                              r1.IsEnable,
                              r1.MemberCompanyCode,
                              c.CompanyName,
                              c.CompanyShortName,
                              p01.Id PageId,
                              r1.GroupId,
                              p05.Id GroupPageComponentMappingId
                       FROM dbo.P01_Page p01 (NOLOCK)
                           JOIN dbo.P05_PageComponentMapping p05 (NOLOCK)
                               ON p05.P01_PageId = p01.Id
                           JOIN dbo.P03_Component p03 (NOLOCK)
                               ON p03.Id = p05.P03_ComponentId
                           LEFT JOIN #Permission_GetPageWithComponentByGroupId r1 (NOLOCK)
                               ON r1.PageId = p01.Id
                                  AND p03.Id = r1.ComponentId
                           LEFT JOIN #Permission_GetPageWithComponentByGroupId_Company c (NOLOCK)
                               ON c.MemberCompanyCode = r1.MemberCompanyCode
                       WHERE 1 = 1
                             AND p01.Id = r.PageId --'B168DD2A-9CC3-44CF-8071-F80180952AF6'
                       ORDER BY p03.ComponentName
                       FOR JSON PATH, INCLUDE_NULL_VALUES
                   )
            FROM #Permission_GetPageWithComponentByGroupId r
            ORDER BY r.PageCode
            FOR JSON PATH, INCLUDE_NULL_VALUES
        );

        SELECT ResData = @Permission_GetPageWithComponentByGroupId,
               IsSuccess = CAST(1 AS BIT),
               ErrorMess = '';
END TRY
BEGIN CATCH
SELECT ResData = '',
           IsSuccess = CAST(0 AS BIT),
           ErrorMess = @@ERROR;
END CATCH

GO


CREATE OR ALTER PROCEDURE [dbo].[sp_Authen_TabUser_SearchUser] @Param NVARCHAR(max)
AS
BEGIN TRY
DECLARE @SearchText_TabUser_SearchUser NVARCHAR(MAX);
        SELECT @SearchText_TabUser_SearchUser = a.SearchText
        FROM
            OPENJSON(@Param)
            WITH
            (
                SearchText NVARCHAR(MAX) '$.SearchText'
            ) a;
        DECLARE @TabUser_SearchUser NVARCHAR(MAX);
        SET @TabUser_SearchUser =
        (
            SELECT ISNULL(p04.Id, CAST(0x0 AS UNIQUEIDENTIFIER)) Id,
                   p01.UserID AS UserId,
                   p01.UserLogin,
                   p01.FullName,
                   p01.EmailAddress1 Email,
                   p01.GoogleEmail,
                   p04.Description,
                   IsAdmin = IIF(p02.GroupName IN ('Admin', 'Administrator'), CAST(1 AS BIT), CAST(0 AS BIT)),
                   ISNULL(p04.P02_GroupId, CAST(0x0 AS UNIQUEIDENTIFIER)) AS GroupId,
                   GroupName = ISNULL(
                               (
                                   SELECT TOP 1
                                          p02.GroupName
                                   FROM dbo.P02_Group p02 (NOLOCK)
                                   WHERE p02.Id = p04.P02_GroupId
                               ),
                               ''
                                     ),
                   TypeOfUser = IIF(ISNULL(p04.Id, CAST(0x0 AS UNIQUEIDENTIFIER)) <> CAST(0x0 AS UNIQUEIDENTIFIER),
                                    'Transportation User',
                                    'GTAS User'),
                   ISNULL(p04.IsDeleted, 0) IsDeleted,
                   ISNULL(p04.CreateUserId, 0) CreateUserId,
                   p04.CreateDate,
                   --ISNULL(CAST(p04.CreateDate AS DATETIME2), CAST(GETDATE() AS DATETIME2)) CreateDate,
                   ISNULL(p04.UpdateUserId, 0) UpdateUserId,
                   p04.UpdateDate,
                   --ISNULL(CAST(p04.UpdateDate AS DATETIME2), CAST(GETDATE() AS DATETIME2)) UpdateDate,
                   CreateUserName = IIF(ISNULL(p04.CreateUserId, 0) = 0,
                                        '',
                                    (
                                        SELECT TOP 1
                                               FullName
                                        FROM GTAS_MENU.dbo.tblUsers u (NOLOCK)
                                        WHERE u.UserID = p04.CreateUserId
                                    )),
                   UpdateUserName = IIF(ISNULL(p04.CreateUserId, 0) = 0,
                                        '',
                                    (
                                        SELECT TOP 1
                                               FullName
                                        FROM GTAS_MENU.dbo.tblUsers u (NOLOCK)
                                        WHERE u.UserID = p04.UpdateUserId
                                    )),
                   UserGroup = JSON_QUERY(
                               (
                                   SELECT p02.Id,
                                          p02.GroupName,
                                          p02.ParentGroupId,
                                          p02.Description,
                                          p02.CreateUserId,
                                          p02.CreateDate,
                                          p02.UpdateUserId,
                                          p02.UpdateDate,
                                          p02.IsDeleted
                                   FROM dbo.P02_Group p02 (NOLOCK)
                                   WHERE p02.Id = p04.P02_GroupId
                                   FOR JSON PATH, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER
                               )
                                         )
            FROM GTAS_MENU.dbo.tblUsers p01 (NOLOCK)
                LEFT JOIN dbo.P04_UserGroup p04 (NOLOCK)
                    ON p01.UserID = p04.UserId
                LEFT JOIN dbo.P02_Group p02 (NOLOCK)
                    ON p02.Id = p04.P02_GroupId
            WHERE 1 = 1
                  AND
                  (
                      CHARINDEX(@SearchText_TabUser_SearchUser, p01.UserLogin) > 0
                      OR CHARINDEX(@SearchText_TabUser_SearchUser, p01.EmailAddress1) > 0
                      OR CHARINDEX(@SearchText_TabUser_SearchUser, p01.FullName) > 0
                  )
				  and p01.IsInactiveFlg = 0
            ORDER BY p01.FullName
            FOR JSON PATH, INCLUDE_NULL_VALUES
        );

        SELECT ResData = @TabUser_SearchUser,
               IsSuccess = CAST(1 AS BIT),
               ErrorMess = '';
END TRY
BEGIN CATCH
SELECT ResData = '',
           IsSuccess = CAST(0 AS BIT),
           ErrorMess = @@ERROR;
END CATCH

GO


CREATE OR ALTER PROCEDURE [dbo].[sp_Authen_TabUser_UserList] @Param NVARCHAR(max)
AS
BEGIN TRY
	DECLARE @TabUser_TransportUserList NVARCHAR(MAX);
        SET @TabUser_TransportUserList =
        (
            SELECT p04.Id,
                   us.UserID AS UserId,
                   us.UserLogin,
                   us.FullName,
                   us.EmailAddress1 Email,
                   us.GoogleEmail,
                   IsAdmin = IIF(p02.GroupName IN ('Admin', 'Administrator'), CAST(1 AS BIT), CAST(0 AS BIT)),
                   p04.P02_GroupId GroupId,
                   p02.GroupName,
                   p04.CreateUserId,
                   p04.CreateDate,
                   CreateUserName =
                   (
                       SELECT TOP 1
                              FullName
                       FROM GTAS_MENU.dbo.tblUsers u (NOLOCK)
                       WHERE u.UserID = p04.CreateUserId
                   ),
                   p04.UpdateUserId,
                   p04.UpdateDate,
                   UpdateUserName =
                   (
                       SELECT TOP 1
                              FullName
                       FROM GTAS_MENU.dbo.tblUsers u (NOLOCK)
                       WHERE u.UserID = p04.UpdateUserId
                   ),
                   p04.IsDeleted,
                   TypeOfUser = 'Transport User',
                   p04.Description,
                   UserGroup = JSON_QUERY(
                               (
                                   SELECT p02.Id,
                                          p02.GroupName,
                                          p02.ParentGroupId,
                                          p02.Description,
                                          p02.CreateUserId,
                                          p02.CreateDate,
                                          p02.UpdateUserId,
                                          p02.UpdateDate,
                                          p02.IsDeleted
                                   FROM dbo.P02_Group p02 (NOLOCK)
                                   WHERE p02.Id = p04.P02_GroupId
                                   FOR JSON PATH, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER
                               )
                                         )
            FROM GTAS_MENU.dbo.tblUsers us (NOLOCK)
                JOIN dbo.P04_UserGroup p04 (NOLOCK)
                    ON us.UserID = p04.UserId
                JOIN dbo.P02_Group p02 (NOLOCK)
                    ON p02.Id = p04.P02_GroupId
			WHERE us.IsInactiveFlg = 0
            FOR JSON PATH, INCLUDE_NULL_VALUES
        );

        SELECT ResData = @TabUser_TransportUserList,
               IsSuccess = CAST(1 AS BIT),
               ErrorMess = '';
END TRY
BEGIN CATCH
    SELECT ResData = '',
           IsSuccess = CAST(0 AS BIT),
           ErrorMess = @@ERROR;
END CATCH;

GO

-- ============================================================================
-- PH?N 1: FIX LOGIC SP - �?Y �? C�C C?T AUDIT (77500, 5615, GETDATE)
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

    -- B�c t�ch JSON
    DROP TABLE IF EXISTS #TempCodes;
    SELECT value AS ComponentCode INTO #TempCodes FROM OPENJSON(@ComponentCodesJson);

    -- Th�m Component v�o c?u tr�c Page (P05)
    INSERT INTO dbo.P05_PageComponentMapping (Id, P01_PageId, P03_ComponentId)
    SELECT NEWID(), @PageId, c.Id
    FROM #TempCodes t
    INNER JOIN dbo.P03_Component c ON c.ComponentCode = t.ComponentCode
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.P05_PageComponentMapping p05
        WHERE p05.P01_PageId = @PageId AND p05.P03_ComponentId = c.Id
    );

    -- R�t quy?n c?a GROUP
    DELETE p06
    FROM dbo.P06_GroupPageComponentMapping p06
    INNER JOIN dbo.P05_PageComponentMapping p05 ON p06.P05_PageComponentMappingId = p05.Id
    WHERE p05.P01_PageId = @PageId
      AND p06.P02_GroupId = @GroupId
      AND p05.P03_ComponentId NOT IN (
          SELECT c.Id FROM #TempCodes t
          INNER JOIN dbo.P03_Component c ON c.ComponentCode = t.ComponentCode
      );

    -- C?p quy?n (B? sung UpdateUserId v� UpdateDate)
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
CREATE OR ALTER PROCEDURE dbo.sp_CreateNewPageComponent
                        @PageCode NVARCHAR(100),
                        @PageName NVARCHAR(MAX),
                        @PageType NVARCHAR(100),
                        @PageDesctiption NVARCHAR(MAX),
                        @ComponentCode NVARCHAR(100),
                        @ComponentName NVARCHAR(MAX),
                        @ComponentDescription NVARCHAR(MAX)
                    AS
                    BEGIN
                        DECLARE @PageId UNIQUEIDENTIFIER;
                        DECLARE @ComponentId UNIQUEIDENTIFIER;
                        DECLARE @P05Id UNIQUEIDENTIFIER;
                        IF NOT EXISTS
                        (
                            SELECT *
                            FROM dbo.P01_Page p01 (NOLOCK)
                            WHERE p01.PageCode = @PageCode
                        )
                        BEGIN
                            SET @PageId = NEWID();
                            INSERT INTO dbo.P01_Page
                            (
                                Id,
                                PageCode,
                                PageName,
                                Type,
                                Description,
                                CreateUserId,
                                CreateDate,
                                UpdateUserId,
                                UpdateDate,
                                IsDeleted
                            )
                            VALUES
                            (   @PageId,          -- Id - uniqueidentifier
                                @PageCode,        -- PageCode - nvarchar(50)
                                @PageName,        -- PageName - nvarchar(250)
                                @PageType,        -- Type - nvarchar(50)
                                @PageDesctiption, -- Description - nvarchar(500)
                                5615,             -- CreateUserId - int
                                SYSDATETIME(),    -- CreateDate - datetime2(7)
                                5615,             -- UpdateUserId - int
                                SYSDATETIME(),    -- UpdateDate - datetime2(7)
                                0                 -- IsDeleted - bit
                                );
		                    PRINT '-----------------------'
		                    PRINT 'Create New Page Success'
		                    PRINT '-----------------------'
                        END;
                        ELSE
                        BEGIN
                            SET @PageId =
                            (
                                SELECT TOP 1 Id FROM dbo.P01_Page p01 (NOLOCK) WHERE p01.PageCode = @PageCode
                            );
		                    PRINT '-----------------------'
		                    PRINT 'Update Page ID'
		                    PRINT '-----------------------'
                        END;
                        IF NOT EXISTS
                        (
                            SELECT *
                            FROM dbo.P03_Component p03 (NOLOCK)
                            WHERE p03.ComponentCode = @ComponentCode
                        )
                        BEGIN
                            SET @ComponentId = NEWID();
                            INSERT INTO dbo.P03_Component
                            (
                                Id,
                                ComponentName,
                                ComponentCode,
                                Description,
                                CreateUserId,
                                CreateDate,
                                UpdateUserId,
                                UpdateDate,
                                IsDeleted
                            )
                            VALUES
                            (   @ComponentId,          -- Id - uniqueidentifier
                                @ComponentName,        -- ComponentName - nvarchar(150)
                                @ComponentCode,        -- ComponentCode - nvarchar(50)
                                @ComponentDescription, -- Description - nvarchar(500)
                                5615,                  -- CreateUserId - int
                                SYSDATETIME(),         -- CreateDate - datetime2(7)
                                5615,                  -- UpdateUserId - int
                                SYSDATETIME(),         -- UpdateDate - datetime2(7)
                                0                      -- IsDeleted - bit
                                );
		                    PRINT '-----------------------'
		                    PRINT 'Create New Component Success'
		                    PRINT '-----------------------'
                        END;
                        ELSE
                        BEGIN
                            SET @ComponentId =
                            (
                                SELECT TOP 1
                                       p03.Id
                                FROM dbo.P03_Component p03 (NOLOCK)
                                WHERE p03.ComponentCode = @ComponentCode
                            );
		                    PRINT '-----------------------'
		                    PRINT 'Update Component ID'
		                    PRINT '-----------------------'
                        END;
                        IF NOT EXISTS
                        (
                            SELECT *
                            FROM dbo.P05_PageComponentMapping p05 (NOLOCK)
                            WHERE p05.P01_PageId = @PageId
                                  AND p05.P03_ComponentId = @ComponentId
                        )
                        BEGIN
                            SET @P05Id = NEWID();
                            INSERT INTO dbo.P05_PageComponentMapping
                            (
                                Id,
                                P01_PageId,
                                P03_ComponentId
                            )
                            VALUES
                            (   @P05Id,      -- Id - uniqueidentifier
                                @PageId,     -- P01_PageId - uniqueidentifier
                                @ComponentId -- P03_ComponentId - uniqueidentifier
                                );
		                    PRINT '-----------------------'
		                    PRINT 'Create P05 Success'
		                    PRINT '-----------------------'
                        END;
                        ELSE
                        BEGIN
                            SET @P05Id =
                            (
                                SELECT TOP 1
                                       p05.Id
                                FROM dbo.P05_PageComponentMapping p05 (NOLOCK)
                                WHERE p05.P01_PageId = @PageId
                                      AND p05.P03_ComponentId = @ComponentId
                            );
		                    PRINT '-----------------------'
		                    PRINT 'Update P05 Success'
		                    PRINT '-----------------------'
                        END;

                        DROP TABLE IF EXISTS #tblGroup;
                        SELECT Id
                        INTO #tblGroup
                        FROM dbo.P02_Group;

                        IF NOT EXISTS
                        (
                            SELECT *
                            FROM dbo.P06_GroupPageComponentMapping p06 (NOLOCK)
                            WHERE p06.P05_PageComponentMappingId = @P05Id
                                  AND p06.P02_GroupId NOT IN
                                      (
                                          SELECT * FROM #tblGroup
                                      )
                        )
                        BEGIN
                            INSERT INTO dbo.P06_GroupPageComponentMapping
                            (
                                P05_PageComponentMappingId,
                                P02_GroupId,
                                CreateUserId,
                                CreateDate,
                                UpdateUserId,
                                UpdateDate,
                                IsEnable,
                                IsVisible,
                                MemberCompanyCode
                            )
                            SELECT @P05Id,
                                   Id,
                                   5615,
                                   GETDATE(),
                                   5615,
                                   GETDATE(),
                                   0,
                                   0,
                                   77500
                            FROM #tblGroup;
		                    PRINT '-----------------------'
		                    PRINT 'Create New P06 Success'
		                    PRINT '-----------------------'
                        END;
                    END;
                

GO
CREATE OR ALTER PROCEDURE dbo.sp_SaveComponent
    @ComponentCode NVARCHAR(100),
    @ComponentName NVARCHAR(MAX),
    @ComponentDescription NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    MERGE INTO dbo.P03_Component AS Target
    USING (SELECT @ComponentCode AS ComponentCode) AS Source
       ON Target.ComponentCode = Source.ComponentCode

    WHEN MATCHED THEN 
        UPDATE SET 
            Target.ComponentName = @ComponentName,
            Target.Description = @ComponentDescription,
            Target.UpdateDate = SYSDATETIME()

    WHEN NOT MATCHED BY TARGET THEN 
        INSERT (Id, ComponentCode, ComponentName, Description, CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted)
        VALUES (NEWID(), @ComponentCode, @ComponentName, @ComponentDescription, 5615, SYSDATETIME(), 5615, SYSDATETIME(), 0);

    PRINT '>>> L�u Component [' + @ComponentCode + '] th�nh c�ng!';
END;

GO
CREATE OR ALTER PROCEDURE dbo.sp_SavePage
    @PageCode NVARCHAR(100),
    @PageName NVARCHAR(MAX),
    @PageType NVARCHAR(100),
    @PageDescription NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    MERGE INTO dbo.P01_Page AS Target
    USING (SELECT @PageCode AS PageCode) AS Source
       ON Target.PageCode = Source.PageCode

    -- N?u �? C� m? n�y -> C?p nh?t th�ng tin m?i nh?t
    WHEN MATCHED THEN 
        UPDATE SET 
            Target.PageName = @PageName,
            Target.Type = @PageType,
            Target.Description = @PageDescription,
            Target.UpdateDate = SYSDATETIME()

    -- N?u CH�A C� -> Th�m m?i
    WHEN NOT MATCHED BY TARGET THEN 
        INSERT (Id, PageCode, PageName, Type, Description, CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted)
        VALUES (NEWID(), @PageCode, @PageName, @PageType, @PageDescription, 5615, SYSDATETIME(), 5615, SYSDATETIME(), 0);

    PRINT '>>> L�u Page [' + @PageCode + '] th�nh c�ng!';
END;

GO
