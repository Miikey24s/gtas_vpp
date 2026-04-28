SELECT * FROM dbo.P01_Page

SELECT * FROM dbo.P03_Component

EXEC dbo.sp_CreateNewPageComponent @PageCode = N'',            -- nvarchar(100)
                                   @PageName = N'',            -- nvarchar(max)
                                   @PageType = N'',            -- nvarchar(100)
                                   @PageDesctiption = N'',     -- nvarchar(max)
                                   @ComponentCode = N'0001_RQ_O',       -- nvarchar(100)
                                   @ComponentName = N'Request Order',       -- nvarchar(max)
                                   @ComponentDescription = N'Request Order Component' -- nvarchar(max)

EXEC dbo.sp_CreateNewPageComponent @PageCode = N'',            -- nvarchar(100)
                                   @PageName = N'',            -- nvarchar(max)
                                   @PageType = N'',            -- nvarchar(100)
                                   @PageDesctiption = N'',     -- nvarchar(max)
                                   @ComponentCode = N'0001_RQ_H',       -- nvarchar(100)
                                   @ComponentName = N'Request History',       -- nvarchar(max)
                                   @ComponentDescription = N'Request History Component' -- nvarchar(max)