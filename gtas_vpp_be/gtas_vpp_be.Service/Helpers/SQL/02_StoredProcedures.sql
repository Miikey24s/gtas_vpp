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
               a.CreatedByUserId
        INTO #CopyFromGroup
        FROM
            OPENJSON(@Param)
            WITH
            (
                GroupId UNIQUEIDENTIFIER '$.GroupId',
                GroupName NVARCHAR(150) '$.GroupName',
                Description NVARCHAR(MAX) '$.Description',
                CreatedByUserId INT '$.CreatedByUserId'
            ) a;

        DECLARE @CopyFromGroup_NewGroupId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO dbo.PermissionGroups
        (
            Id,
            GroupName,
            Description,
            CreatedByUserId,
            CreatedAtUtc,
            UpdatedByUserId,
            UpdatedAtUtc,
            IsDeleted
        )
        SELECT @CopyFromGroup_NewGroupId,
               GroupName,
               Description,
               CreatedByUserId,
               GETDATE(),
               CreatedByUserId,
               GETDATE(),
               0
        FROM #CopyFromGroup;

        DROP TABLE IF EXISTS #tblInsertP06_Copy;
        SELECT DISTINCT
               PageComponentMappingId,
               @CopyFromGroup_NewGroupId AS GroupId,
               IsEnable,
               IsVisible
        INTO #tblInsertP06_Copy
        FROM dbo.GroupPageComponentMappings p06 (NOLOCK)
        WHERE p06.PermissionGroupId =
        (
            SELECT TOP 1 a.GroupId FROM #CopyFromGroup a
        );

        DECLARE @CopyFromGroup_UserId INT =
                (
                    SELECT TOP 1 CreatedByUserId FROM #CopyFromGroup
                );

        INSERT INTO dbo.GroupPageComponentMappings
        (
            PageComponentMappingId,
            PermissionGroupId,
            CreatedByUserId,
            CreatedAtUtc,
            UpdatedByUserId,
            UpdatedAtUtc,
            IsEnable,
            IsVisible,
            MemberCompanyCode
        )
        SELECT p06.PageComponentMappingId,
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
               a.CreatedByUserId
        INTO #CreateNewGroup
        FROM
            OPENJSON(@Param)
            WITH
            (
                GroupId UNIQUEIDENTIFIER '$.GroupId',
                GroupName NVARCHAR(150) '$.GroupName',
                Description NVARCHAR(MAX) '$.Description',
                CreatedByUserId INT '$.CreatedByUserId'
            ) a;

        IF NOT EXISTS
        (
            SELECT *
            FROM dbo.PermissionGroups p02
                JOIN #CreateNewGroup gr
                    ON p02.GroupName = gr.GroupName
        )
        BEGIN TRY
            DECLARE @NewGroupId UNIQUEIDENTIFIER = NEWID();
            UPDATE #CreateNewGroup
            SET GroupId = @NewGroupId;
            INSERT INTO dbo.PermissionGroups
            (
                Id,
                GroupName,
                Description,
                CreatedByUserId,
                CreatedAtUtc,
                UpdatedByUserId,
                UpdatedAtUtc,
                IsDeleted
            )
            SELECT @NewGroupId,
                   GroupName,
                   Description,
                   CreatedByUserId,
                   GETDATE(),
                   CreatedByUserId,
                   GETDATE(),
                   0
            FROM #CreateNewGroup;

            DROP TABLE IF EXISTS #tblInsertP06;
            SELECT DISTINCT
                   PageComponentMappingId,
                   (
                       SELECT TOP 1 GroupId FROM #CreateNewGroup
                   ) AS GroupId
            INTO #tblInsertP06
            FROM dbo.GroupPageComponentMappings;

            DECLARE @CreateNewGroup_UserId INT =
                    (
                        SELECT TOP 1 CreatedByUserId FROM #CreateNewGroup
                    );

            INSERT INTO dbo.GroupPageComponentMappings
            (
                PageComponentMappingId,
                PermissionGroupId,
                CreatedByUserId,
                CreatedAtUtc,
                UpdatedByUserId,
                UpdatedAtUtc,
                IsEnable,
                IsVisible,
                MemberCompanyCode
            )
            SELECT p06.PageComponentMappingId,
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
        JOIN dbo.UserGroupMemberships p04 (NOLOCK)
            ON p04.UserId = us.UserID
        JOIN dbo.PermissionGroups p02 (NOLOCK)
            ON p02.Id = p04.PermissionGroupId
        JOIN dbo.GroupPageComponentMappings p06 (NOLOCK)
            ON p06.PermissionGroupId = p02.Id
        JOIN dbo.PageComponentMappings p05 (NOLOCK)
            ON p05.Id = p06.PageComponentMappingId
        JOIN dbo.PermissionPages p01 (NOLOCK)
            ON p01.Id = p05.PermissionPageId
        JOIN dbo.PermissionComponents p03 (NOLOCK)
            ON p03.Id = p05.PermissionComponentId
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
               Components =    (
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


-- sp_Authen_Login was intentionally removed. App-owned ASP.NET Core Identity
-- is the only credential verifier; 04_RetireLegacyAuth.sql remains as an
-- idempotent upgrade guard for databases created by older releases.
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
               p06.PermissionGroupId GroupId,
               p01.PageCode,
               p01.PageName,
               p01.Description,
               p01.CreatedByUserId,
               p01.CreatedAtUtc,
               CreatedByUserName =
               (
                   SELECT TOP 1
                          FullName
                   FROM dbo.v_Users u (NOLOCK)
                   WHERE u.UserID = p01.CreatedByUserId
               ),
               p01.UpdatedByUserId,
               p01.UpdatedAtUtc,
               UpdatedByUserName =
               (
                   SELECT TOP 1
                          FullName
                   FROM dbo.v_Users u (NOLOCK)
                   WHERE u.UserID = p01.CreatedByUserId
               ),
               p03.Id ComponentId,
               p03.ComponentCode,
               p03.ComponentName,
               p06.IsVisible,
               p06.IsEnable,
               p06.MemberCompanyCode,
               p01.IsDeleted
        INTO #Permission_GetPageWithComponentByGroupId
        FROM dbo.PermissionPages p01 (NOLOCK)
            LEFT JOIN dbo.PageComponentMappings p05 (NOLOCK)
                ON p05.PermissionPageId = p01.Id
            LEFT JOIN dbo.PermissionComponents p03 (NOLOCK)
                ON p03.Id = p05.PermissionComponentId
            LEFT JOIN dbo.GroupPageComponentMappings p06 (NOLOCK)
                ON p06.PageComponentMappingId = p05.Id
        WHERE 1 = 1
              AND p03.IsDeleted = 0
              AND p06.PermissionGroupId = @GroupId;

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
                   r.CreatedByUserId,
                   r.CreatedAtUtc,
                   r.CreatedByUserName,
                   r.UpdatedByUserId,
                   r.UpdatedAtUtc,
                   r.UpdatedByUserName,
                   r.IsDeleted,
                   Components =
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
                       FROM dbo.PermissionPages p01 (NOLOCK)
                           JOIN dbo.PageComponentMappings p05 (NOLOCK)
                               ON p05.PermissionPageId = p01.Id
                           JOIN dbo.PermissionComponents p03 (NOLOCK)
                               ON p03.Id = p05.PermissionComponentId
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
                   ISNULL(p04.PermissionGroupId, CAST(0x0 AS UNIQUEIDENTIFIER)) AS GroupId,
                   GroupName = ISNULL(
                               (
                                   SELECT TOP 1
                                          p02.GroupName
                                   FROM dbo.PermissionGroups p02 (NOLOCK)
                                   WHERE p02.Id = p04.PermissionGroupId
                               ),
                               ''
                                     ),
                   UserType = IIF(ISNULL(p04.Id, CAST(0x0 AS UNIQUEIDENTIFIER)) <> CAST(0x0 AS UNIQUEIDENTIFIER),
                                    'Transportation User',
                                    'GTAS User'),
                   ISNULL(p04.IsDeleted, 0) IsDeleted,
                   ISNULL(p04.CreatedByUserId, 0) CreatedByUserId,
                   p04.CreatedAtUtc,
                   --ISNULL(CAST(p04.CreatedAtUtc AS DATETIME2), CAST(GETDATE() AS DATETIME2)) CreatedAtUtc,
                   ISNULL(p04.UpdatedByUserId, 0) UpdatedByUserId,
                   p04.UpdatedAtUtc,
                   --ISNULL(CAST(p04.UpdatedAtUtc AS DATETIME2), CAST(GETDATE() AS DATETIME2)) UpdatedAtUtc,
                   CreatedByUserName = IIF(ISNULL(p04.CreatedByUserId, 0) = 0,
                                        '',
                                    (
                                        SELECT TOP 1
                                               FullName
                                        FROM GTAS_MENU.dbo.tblUsers u (NOLOCK)
                                        WHERE u.UserID = p04.CreatedByUserId
                                    )),
                   UpdatedByUserName = IIF(ISNULL(p04.CreatedByUserId, 0) = 0,
                                        '',
                                    (
                                        SELECT TOP 1
                                               FullName
                                        FROM GTAS_MENU.dbo.tblUsers u (NOLOCK)
                                        WHERE u.UserID = p04.UpdatedByUserId
                                    )),
                   UserGroup = JSON_QUERY(
                               (
                                   SELECT p02.Id,
                                          p02.GroupName,
                                          p02.ParentGroupId,
                                          p02.Description,
                                          p02.CreatedByUserId,
                                          p02.CreatedAtUtc,
                                          p02.UpdatedByUserId,
                                          p02.UpdatedAtUtc,
                                          p02.IsDeleted
                                   FROM dbo.PermissionGroups p02 (NOLOCK)
                                   WHERE p02.Id = p04.PermissionGroupId
                                   FOR JSON PATH, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER
                               )
                                         )
            FROM GTAS_MENU.dbo.tblUsers p01 (NOLOCK)
                LEFT JOIN dbo.UserGroupMemberships p04 (NOLOCK)
                    ON p01.UserID = p04.UserId
                LEFT JOIN dbo.PermissionGroups p02 (NOLOCK)
                    ON p02.Id = p04.PermissionGroupId
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
                   p04.PermissionGroupId GroupId,
                   p02.GroupName,
                   p04.CreatedByUserId,
                   p04.CreatedAtUtc,
                   CreatedByUserName =
                   (
                       SELECT TOP 1
                              FullName
                       FROM GTAS_MENU.dbo.tblUsers u (NOLOCK)
                       WHERE u.UserID = p04.CreatedByUserId
                   ),
                   p04.UpdatedByUserId,
                   p04.UpdatedAtUtc,
                   UpdatedByUserName =
                   (
                       SELECT TOP 1
                              FullName
                       FROM GTAS_MENU.dbo.tblUsers u (NOLOCK)
                       WHERE u.UserID = p04.UpdatedByUserId
                   ),
                   p04.IsDeleted,
                   UserType = 'Transport User',
                   p04.Description,
                   UserGroup = JSON_QUERY(
                               (
                                   SELECT p02.Id,
                                          p02.GroupName,
                                          p02.ParentGroupId,
                                          p02.Description,
                                          p02.CreatedByUserId,
                                          p02.CreatedAtUtc,
                                          p02.UpdatedByUserId,
                                          p02.UpdatedAtUtc,
                                          p02.IsDeleted
                                   FROM dbo.PermissionGroups p02 (NOLOCK)
                                   WHERE p02.Id = p04.PermissionGroupId
                                   FOR JSON PATH, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER
                               )
                                         )
            FROM GTAS_MENU.dbo.tblUsers us (NOLOCK)
                JOIN dbo.UserGroupMemberships p04 (NOLOCK)
                    ON us.UserID = p04.UserId
                JOIN dbo.PermissionGroups p02 (NOLOCK)
                    ON p02.Id = p04.PermissionGroupId
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
-- PH?N 1: FIX LOGIC SP - �?Year �? C�C C?T AUDIT (77500, 5615, GETDATE)
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
    SELECT @PageId = Id FROM dbo.PermissionPages WHERE PageCode = @PageCode;

    IF @PageId IS NULL RETURN;

    -- B�c t�ch JSON
    DROP TABLE IF EXISTS #TempCodes;
    SELECT value AS ComponentCode INTO #TempCodes FROM OPENJSON(@ComponentCodesJson);

    -- Add requested components to the page definition.
    INSERT INTO dbo.PageComponentMappings (Id, PermissionPageId, PermissionComponentId)
    SELECT NEWID(), @PageId, c.Id
    FROM #TempCodes t
    INNER JOIN dbo.PermissionComponents c ON c.ComponentCode = t.ComponentCode
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.PageComponentMappings p05
        WHERE p05.PermissionPageId = @PageId AND p05.PermissionComponentId = c.Id
    );

    -- R�t quy?n c?a GROUP
    DELETE p06
    FROM dbo.GroupPageComponentMappings p06
    INNER JOIN dbo.PageComponentMappings p05 ON p06.PageComponentMappingId = p05.Id
    WHERE p05.PermissionPageId = @PageId
      AND p06.PermissionGroupId = @GroupId
      AND p05.PermissionComponentId NOT IN (
          SELECT c.Id FROM #TempCodes t
          INNER JOIN dbo.PermissionComponents c ON c.ComponentCode = t.ComponentCode
      );

    -- C?p quy?n (B? sung UpdatedByUserId v� UpdatedAtUtc)
    INSERT INTO dbo.GroupPageComponentMappings
        (PageComponentMappingId, PermissionGroupId, IsEnable, IsVisible, MemberCompanyCode, CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc)
    SELECT 
        p05.Id, @GroupId, 1, 1, @MemberCompanyCode, @UserId, GETDATE(), @UserId, GETDATE()
    FROM #TempCodes t
    INNER JOIN dbo.PermissionComponents c ON c.ComponentCode = t.ComponentCode
    INNER JOIN dbo.PageComponentMappings p05 ON p05.PermissionComponentId = c.Id AND p05.PermissionPageId = @PageId
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.GroupPageComponentMappings p06
        WHERE p06.PageComponentMappingId = p05.Id AND p06.PermissionGroupId = @GroupId
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
                            FROM dbo.PermissionPages p01 (NOLOCK)
                            WHERE p01.PageCode = @PageCode
                        )
                        BEGIN
                            SET @PageId = NEWID();
                            INSERT INTO dbo.PermissionPages
                            (
                                Id,
                                PageCode,
                                PageName,
                                Type,
                                Description,
                                CreatedByUserId,
                                CreatedAtUtc,
                                UpdatedByUserId,
                                UpdatedAtUtc,
                                IsDeleted
                            )
                            VALUES
                            (   @PageId,          -- Id - uniqueidentifier
                                @PageCode,        -- PageCode - nvarchar(50)
                                @PageName,        -- PageName - nvarchar(250)
                                @PageType,        -- Type - nvarchar(50)
                                @PageDesctiption, -- Description - nvarchar(500)
                                5615,             -- CreatedByUserId - int
                                SYSDATETIME(),    -- CreatedAtUtc - datetime2(7)
                                5615,             -- UpdatedByUserId - int
                                SYSDATETIME(),    -- UpdatedAtUtc - datetime2(7)
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
                                SELECT TOP 1 Id FROM dbo.PermissionPages p01 (NOLOCK) WHERE p01.PageCode = @PageCode
                            );
		                    PRINT '-----------------------'
		                    PRINT 'Update Page ID'
		                    PRINT '-----------------------'
                        END;
                        IF NOT EXISTS
                        (
                            SELECT *
                            FROM dbo.PermissionComponents p03 (NOLOCK)
                            WHERE p03.ComponentCode = @ComponentCode
                        )
                        BEGIN
                            SET @ComponentId = NEWID();
                            INSERT INTO dbo.PermissionComponents
                            (
                                Id,
                                ComponentName,
                                ComponentCode,
                                Description,
                                CreatedByUserId,
                                CreatedAtUtc,
                                UpdatedByUserId,
                                UpdatedAtUtc,
                                IsDeleted
                            )
                            VALUES
                            (   @ComponentId,          -- Id - uniqueidentifier
                                @ComponentName,        -- ComponentName - nvarchar(150)
                                @ComponentCode,        -- ComponentCode - nvarchar(50)
                                @ComponentDescription, -- Description - nvarchar(500)
                                5615,                  -- CreatedByUserId - int
                                SYSDATETIME(),         -- CreatedAtUtc - datetime2(7)
                                5615,                  -- UpdatedByUserId - int
                                SYSDATETIME(),         -- UpdatedAtUtc - datetime2(7)
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
                                FROM dbo.PermissionComponents p03 (NOLOCK)
                                WHERE p03.ComponentCode = @ComponentCode
                            );
		                    PRINT '-----------------------'
		                    PRINT 'Update Component ID'
		                    PRINT '-----------------------'
                        END;
                        IF NOT EXISTS
                        (
                            SELECT *
                            FROM dbo.PageComponentMappings p05 (NOLOCK)
                            WHERE p05.PermissionPageId = @PageId
                                  AND p05.PermissionComponentId = @ComponentId
                        )
                        BEGIN
                            SET @P05Id = NEWID();
                            INSERT INTO dbo.PageComponentMappings
                            (
                                Id,
                                PermissionPageId,
                                PermissionComponentId
                            )
                            VALUES
                            (   @P05Id,      -- Id - uniqueidentifier
                                @PageId,     -- PermissionPageId - uniqueidentifier
                                @ComponentId -- PermissionComponentId - uniqueidentifier
                                );
		                    PRINT '-----------------------'
		                    PRINT 'Create page-component mapping success'
		                    PRINT '-----------------------'
                        END;
                        ELSE
                        BEGIN
                            SET @P05Id =
                            (
                                SELECT TOP 1
                                       p05.Id
                                FROM dbo.PageComponentMappings p05 (NOLOCK)
                                WHERE p05.PermissionPageId = @PageId
                                      AND p05.PermissionComponentId = @ComponentId
                            );
		                    PRINT '-----------------------'
		                    PRINT 'Update page-component mapping success'
		                    PRINT '-----------------------'
                        END;

                        DROP TABLE IF EXISTS #tblGroup;
                        SELECT Id
                        INTO #tblGroup
                        FROM dbo.PermissionGroups;

                        IF NOT EXISTS
                        (
                            SELECT *
                            FROM dbo.GroupPageComponentMappings p06 (NOLOCK)
                            WHERE p06.PageComponentMappingId = @P05Id
                                  AND p06.PermissionGroupId NOT IN
                                      (
                                          SELECT * FROM #tblGroup
                                      )
                        )
                        BEGIN
                            INSERT INTO dbo.GroupPageComponentMappings
                            (
                                PageComponentMappingId,
                                PermissionGroupId,
                                CreatedByUserId,
                                CreatedAtUtc,
                                UpdatedByUserId,
                                UpdatedAtUtc,
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
		                    PRINT 'Create group permission mapping success'
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

    MERGE INTO dbo.PermissionComponents AS Target
    USING (SELECT @ComponentCode AS ComponentCode) AS Source
       ON Target.ComponentCode = Source.ComponentCode

    WHEN MATCHED THEN 
        UPDATE SET 
            Target.ComponentName = @ComponentName,
            Target.Description = @ComponentDescription,
            Target.UpdatedAtUtc = SYSDATETIME()

    WHEN NOT MATCHED BY TARGET THEN 
        INSERT (Id, ComponentCode, ComponentName, Description, CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc, IsDeleted)
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

    MERGE INTO dbo.PermissionPages AS Target
    USING (SELECT @PageCode AS PageCode) AS Source
       ON Target.PageCode = Source.PageCode

    -- N?u �? C� m? n�y -> C?p nh?t th�ng tin m?i nh?t
    WHEN MATCHED THEN 
        UPDATE SET 
            Target.PageName = @PageName,
            Target.Type = @PageType,
            Target.Description = @PageDescription,
            Target.UpdatedAtUtc = SYSDATETIME()

    -- N?u CH�A C� -> Th�m m?i
    WHEN NOT MATCHED BY TARGET THEN 
        INSERT (Id, PageCode, PageName, Type, Description, CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc, IsDeleted)
        VALUES (NEWID(), @PageCode, @PageName, @PageType, @PageDescription, 5615, SYSDATETIME(), 5615, SYSDATETIME(), 0);

    PRINT '>>> L�u Page [' + @PageCode + '] th�nh c�ng!';
END;

GO
