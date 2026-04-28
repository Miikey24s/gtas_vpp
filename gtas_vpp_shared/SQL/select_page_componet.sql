USE GTAS_VPP_TEST
GO

SELECT 
    p.PageCode,
    p.PageName,
    p.[Type] AS PageType,
    c.ComponentCode,
    c.ComponentName,
    gpcm.IsEnable,
    gpcm.IsVisible,
    gpcm.P02_GroupId
FROM 
    dbo.P05_PageComponentMapping pcm
INNER JOIN 
    dbo.P01_Page p ON pcm.P01_PageId = p.Id
INNER JOIN 
    dbo.P03_Component c ON pcm.P03_ComponentId = c.Id
LEFT JOIN 
    dbo.P06_GroupPageComponentMapping gpcm ON pcm.Id = gpcm.P05_PageComponentMappingId
ORDER BY 
    p.PageCode, c.ComponentCode;


select* from P01_Page
select* from P03_Component