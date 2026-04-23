USE GTAS_MENU;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Tìm UserID lớn nhất hiện có để tự động cấp phát ID mới
    DECLARE @MaxUserID INT;
    SELECT @MaxUserID = ISNULL(MAX(UserID), 0) FROM GTAS_MENU.dbo.tblUsers;

    -- 2. Đặt các biến hằng số (Đã đổi ActionUserID thành 5615 theo ý bạn)
    DECLARE @AdminGroupID UNIQUEIDENTIFIER = '5823B49B-5925-4A89-846A-09063A36040C';
    DECLARE @UserGroupID UNIQUEIDENTIFIER = '388C6C3A-2801-42DC-BFC0-8A7741264596';
    DECLARE @CompanyLocID UNIQUEIDENTIFIER = 'EE69E297-5E4F-4B90-A33A-AD1CFDD2F5C4';
    DECLARE @ActionUserID INT = 5615; 

    -- 3. Tạo bảng tạm
    CREATE TABLE #NewUsers (
        NewUserID INT,
        GroupID UNIQUEIDENTIFIER
    );

    -- 4. CTE sinh 100 dòng ảo VÀ tính toán trước UserID mới
    WITH 
    E1(N) AS (SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1),
    E2(N) AS (SELECT 1 FROM E1 a, E1 b),
    Tally AS (
        SELECT TOP 100 
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum,
            @MaxUserID + ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS GeneratedUserID -- Tính toán UserID tự tăng bằng tay
        FROM E2
    )

    -- 5. Dùng MERGE để Insert, CHÚ Ý đã bổ sung cột UserID
    MERGE INTO GTAS_MENU.dbo.tblUsers AS Target
    USING (
        SELECT 
            GeneratedUserID,
            RowNum,
            'test_' + IIF(RowNum <= 10, 'admin_', 'user_') + CAST(RowNum AS VARCHAR) AS LoginName,
            'Test ' + IIF(RowNum <= 10, 'Admin ', 'User ') + CAST(RowNum AS VARCHAR) AS FName,
            IIF(RowNum <= 10, @AdminGroupID, @UserGroupID) AS GrpID
        FROM Tally
    ) AS Source
    ON 1 = 0 
    WHEN NOT MATCHED THEN
        INSERT (
            UserID, -- Đã thêm cột UserID
            UserLogin, PasswordChar, FullName, EmailAddress1, GoogleEmail, 
            IsInactiveFlg, IsLockedFlg, MemberCompanyCode, DepartmentCode, MemberCompanyName
        )
        VALUES (
            Source.GeneratedUserID, -- Đưa giá trị ID vừa tính toán vào đây
            Source.LoginName, 'wiSEc6nf/dK/Vu0E738j8Q==', Source.FName, 
            Source.LoginName + '@local.com', Source.LoginName + '@local.com', 
            0, 0, '77500', 'IT', 'PHONG PHU INTERNALTIONAL JSC'
        )
    OUTPUT inserted.UserID, Source.GrpID INTO #NewUsers (NewUserID, GroupID);

    -- 6. Insert đồng loạt vào bảng P04
    INSERT INTO GTAS_VPP_TEST.dbo.P04_UserGroup (
        Id, UserId, P02_GroupId, LEX02_CompanyDepartmentLocationId, Description, 
        CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted
    )
    SELECT 
        NEWID(), NewUserID, GroupID, @CompanyLocID, 'Auto generated test user', 
        @ActionUserID, GETDATE(), @ActionUserID, GETDATE(), 0
    FROM #NewUsers;

    -- Dọn dẹp
    DROP TABLE #NewUsers;

    COMMIT TRANSACTION;
    PRINT 'SUCCESS: Đã khởi tạo thành công 100 tài khoản (10 Admin, 90 User)!';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 
        ROLLBACK TRANSACTION;
    
    PRINT 'ERROR: ' + ERROR_MESSAGE();
END CATCH;
GO