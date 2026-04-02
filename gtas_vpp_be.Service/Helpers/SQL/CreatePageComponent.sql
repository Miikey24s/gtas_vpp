ALTER PROCEDURE dbo.sp_CreateNewPageComponent
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
