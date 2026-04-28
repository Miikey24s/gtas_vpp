DECLARE @InputParam NVARCHAR(MAX) = N'{"GroupId":"5823B49B-5925-4A89-846A-09063A36040C"}';

EXEC [dbo].[sp_Authen_Permission_GetPageWithComponentByGroupId] 
    @Param = @InputParam;

GO
---------------------
DECLARE @Param_UserList NVARCHAR(MAX) = N'{}';

EXEC [dbo].[sp_Authen_TabUser_UserList] 
    @Param = @Param_UserList;

GO
---------------------
DECLARE @Param_SearchUser NVARCHAR(MAX) = N'{"SearchText": "google"}';

EXEC [dbo].[sp_Authen_TabUser_SearchUser] 
    @Param = @Param_SearchUser;

GO