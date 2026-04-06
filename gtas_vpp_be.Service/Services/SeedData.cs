using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services
{
    public static class SeedData
    {

        public static async Task Seed(VPPMigrationDbContext context)
        {
            await P01_Page(context);
            await P02_Group(context);
            await P03_Component(context);

            await LEX02_CompanyDepartmentLocationId_Empty(context);

            await P04_UserGroup(context);
            await P05_PageComponentMapping(context);
            await P06_GroupPageComponentMapping(context);
        }

        public static async Task LEX02_CompanyDepartmentLocationId_Empty(VPPMigrationDbContext context)
        {
            // 1. Kiểm tra xem bản ghi Guid.Empty đã tồn tại chưa
            bool isExist = context.LEX02_CompanyDepartmentLocations.Any(x => x.Id == Guid.Empty);

            if (!isExist)
            {
                // 2. Ép kiểu Guid.Empty ra chuỗi (00000000-0000-0000-0000-000000000000)
                string emptyId = Guid.Empty.ToString();

                // 3. Dùng lệnh SQL thuần (Raw SQL) để lách qua cơ chế tự sinh ID của EF Core
                string sql = $@"
            INSERT INTO LEX02_CompanyDepartmentLocation 
            (Id, LEX02Type, LEX02Code, LEX02Name, CreateDate, CreateUserId, UpdateDate, UpdateUserId, IsDeleted)
            VALUES 
            ('{emptyId}', 'System', 'SYS_DEFAULT', 'System Default Location', GETDATE(), 5615, GETDATE(), 5615, 0)
            ";
                await context.Database.ExecuteSqlRawAsync(sql);
            }
        }

        public static async Task P01_Page(VPPMigrationDbContext context)
        {
            if (!context.P01_Pages.Any())
            {
                var page = new List<P01_Page>
                {
                    new P01_Page
                    {
                        Id = Guid.Parse("19AB0B41-568C-4357-942D-092019EC08E8"),
                        PageCode = "0002",
                        PageName = "Dashboard",
                        Type = "Page",
                        Description = "Dashboard page",
                        CreateDate = DateTime.Now,
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Now,
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P01_Page
                    {
                        Id = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        PageCode = "0001",
                        PageName = "Sidebar",
                        Type = "Component",
                        Description = "Left Sidebar menu",
                        CreateDate = DateTime.Now,
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Now,
                        UpdateUserId = 5615,
                        IsDeleted = false
                    }
                };
                await context.P01_Pages.AddRangeAsync(page);
                await context.SaveChangesAsync();
            }
        }
        public static async Task P06_GroupPageComponentMapping(VPPMigrationDbContext context)
        {
            if (!context.P06_GroupPageComponentMappings.Any())
            {
                var mapping = new List<P06_GroupPageComponentMapping>
                {
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("42067CBC-B2EC-4543-AEEE-06E313CEE46F"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("ACE34EF0-E02A-4861-8046-0A2EC35F44FD"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("48ABBF62-83FA-4B5C-891B-0E20AAB06795"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("AC2EDDB5-785D-44BA-AD13-1C6521C9CB2F"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("1B15372C-F610-4EAC-8FB6-1EA06C9B0E3D"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("034B7FF3-E711-4A12-85E5-4458138DA63A"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("319934A3-4EB2-4943-8650-4B49694CAB05"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("A49D5F82-0510-4D42-B65B-508933A6A8F4"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("0A85AA3C-745D-40C7-8D74-830058F72457"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("9E9276EB-76C1-4899-89B1-8CDE36636CAB"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("33C23B23-3A69-4846-9DFD-98877FB13EF6"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("3C2549AC-F423-41C9-9ECA-A2786FFFAA0B"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("F10938F0-33AC-48B3-B385-A2AB9D889F1B"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("74E95ACD-0ED8-4186-B180-CE4493705D48"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    },
                    new P06_GroupPageComponentMapping
                    {
                        P05_PageComponentMappingId = Guid.Parse("8DF1C069-B427-4E17-99A4-D6576B4B8307"),
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        MemberCompanyCode = 77500,
                        CreateUserId = 5615,
                        CreateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        UpdateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:36:57.5633333"),
                        IsEnable = true,
                        IsVisible = true
                    }
                };
                await context.P06_GroupPageComponentMappings.AddRangeAsync(mapping);
                await context.SaveChangesAsync();
            }
        }
        public static async Task P05_PageComponentMapping(VPPMigrationDbContext context)
        {
            if (!context.P05_PageComponentMappings.Any())
            {
                var mapping = new List<P05_PageComponentMapping>
                {
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("42067CBC-B2EC-4543-AEEE-06E313CEE46F"),
                        P01_PageId = Guid.Parse("19AB0B41-568C-4357-942D-092019EC08E8"),
                        P03_ComponentId = Guid.Parse("0EF1CE18-6C51-4A12-BC33-02AEF29E2666")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("ACE34EF0-E02A-4861-8046-0A2EC35F44FD"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("B5CB8388-31F7-477B-99E2-3F142B5D84D8")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("48ABBF62-83FA-4B5C-891B-0E20AAB06795"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("9F9C5354-2980-4714-BA43-AB3BDF899466")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("AC2EDDB5-785D-44BA-AD13-1C6521C9CB2F"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("B8534954-D739-4AA6-954E-F1390D010EE7")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("1B15372C-F610-4EAC-8FB6-1EA06C9B0E3D"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("E572C24C-B0BA-4C3A-8196-55C518B13770")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("034B7FF3-E711-4A12-85E5-4458138DA63A"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("FA30BCD6-3050-4677-B492-696DA0179799")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("319934A3-4EB2-4943-8650-4B49694CAB05"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("53123EA1-5151-4CDB-97AC-C463854EAE27")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("A49D5F82-0510-4D42-B65B-508933A6A8F4"),
                        P01_PageId = Guid.Parse("19AB0B41-568C-4357-942D-092019EC08E8"),
                        P03_ComponentId = Guid.Parse("2D9CF8C6-0BB5-4C60-A2CD-FC90CF9B38C2")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("0A85AA3C-745D-40C7-8D74-830058F72457"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("A333DE48-7ADB-43EC-A973-CDF71296A7A1")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("9E9276EB-76C1-4899-89B1-8CDE36636CAB"),
                        P01_PageId = Guid.Parse("19AB0B41-568C-4357-942D-092019EC08E8"),
                        P03_ComponentId = Guid.Parse("AFF80218-702F-4ADB-9856-389AD9FC2B36")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("33C23B23-3A69-4846-9DFD-98877FB13EF6"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("9A9B83CC-42B5-4394-95B7-6B69D6F6E642")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("3C2549AC-F423-41C9-9ECA-A2786FFFAA0B"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("9346DEE1-B59C-4C8E-B03D-D6FF0A8E8B44")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("F10938F0-33AC-48B3-B385-A2AB9D889F1B"),
                        P01_PageId = Guid.Parse("19AB0B41-568C-4357-942D-092019EC08E8"),
                        P03_ComponentId = Guid.Parse("0B315726-8B21-4116-BEA5-A63944FD7ECB")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("74E95ACD-0ED8-4186-B180-CE4493705D48"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("4F3B116B-8DFD-471C-8561-74261791CD46")
                    },
                    new P05_PageComponentMapping
                    {
                        Id = Guid.Parse("8DF1C069-B427-4E17-99A4-D6576B4B8307"),
                        P01_PageId = Guid.Parse("FEAB06D3-2884-45F2-ABCF-44B6C117A001"),
                        P03_ComponentId = Guid.Parse("F8AD016A-2328-4A52-A91D-6C6677FCA224")
                    }
                };
                await context.P05_PageComponentMappings.AddRangeAsync(mapping);
                await context.SaveChangesAsync();
            }
        }
        public static async Task P04_UserGroup(VPPMigrationDbContext context)
        {
            if (!context.P04_UserGroups.Any())
            {
                var userGroup = new List<P04_UserGroup>
                {
                    new P04_UserGroup
                    {
                        Id = Guid.Parse("C1876CAC-863A-410E-A760-870DA4FA45FB"),
                        UserId = 306,
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        Description = string.Empty,
                        LEX02_CompanyDepartmentLocationId = Guid.Empty,
                        CreateDate = DateTime.Parse("2026-02-28 09:57:09.2333333"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-02-28 09:57:09.2333333"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P04_UserGroup
                    {
                        Id = Guid.Parse("03C99B4A-5CEF-46DE-8ECC-E1EF6EC4C36D"),
                        UserId = 5615,
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        Description = string.Empty,
                        LEX02_CompanyDepartmentLocationId = Guid.Empty,
                        CreateDate = DateTime.Parse("2026-01-28 09:34:26.6666667"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:34:26.6666667"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P04_UserGroup
                    {
                        Id = Guid.NewGuid(),
                        UserId = 4519,
                        P02_GroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        Description = string.Empty,
                        LEX02_CompanyDepartmentLocationId = Guid.Empty,
                        CreateDate = DateTime.Parse("2026-01-28 09:34:26.6666667"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-28 09:34:26.6666667"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    }
                };
                await context.P04_UserGroups.AddRangeAsync(userGroup);
                await context.SaveChangesAsync();
            }
        }
        public static async Task P03_Component(VPPMigrationDbContext context)
        {
            if (!context.P03_Components.Any())
            {
                var component = new List<P03_Component>
                {
                    new P03_Component
                    {
                        Id = Guid.Parse("0EF1CE18-6C51-4A12-BC33-02AEF29E2666"),
                        ComponentName = "User view",
                        ComponentCode = "0002_UV",
                        Description = "User view",
                        CreateDate = DateTime.Parse("2026-01-27 10:21:06.0167337"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:21:06.0167337"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("AFF80218-702F-4ADB-9856-389AD9FC2B36"),
                        ComponentName = "Library - Class",
                        ComponentCode = "0001_LIB_C",
                        Description = "Library - Class",
                        CreateDate = DateTime.Parse("2026-01-27 10:30:56.2208124"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:30:56.2208124"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("B5CB8388-31F7-477B-99E2-3F142B5D84D8"),
                        ComponentName = "Library - Operation",
                        ComponentCode = "0001_LIB_O",
                        Description = "Library - Operation",
                        CreateDate = DateTime.Parse("2026-01-27 10:17:50.7835963"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:17:50.7835963"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("E572C24C-B0BA-4C3A-8196-55C518B13770"),
                        ComponentName = "Library - Class",
                        ComponentCode = "0001_LIB_C",
                        Description = "Library - Class",
                        CreateDate = DateTime.Parse("2026-01-27 10:18:30.4239677"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:18:30.4239677"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("FA30BCD6-3050-4677-B492-696DA0179799"),
                        ComponentName = "Purchase Consumption",
                        ComponentCode = "0001_PUR",
                        Description = "Purchase Consumption",
                        CreateDate = DateTime.Parse("2026-01-27 10:15:45.7531516"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:15:45.7531516"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("9A9B83CC-42B5-4394-95B7-6B69D6F6E642"),
                        ComponentName = "Buyer Consumption",
                        ComponentCode = "0001_BUY",
                        Description = "Buyer Consumption",
                        CreateDate = DateTime.Parse("2026-01-27 10:16:02.9092955"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:16:02.9092955"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("F8AD016A-2328-4A52-A91D-6C6677FCA224"),
                        ComponentName = "Report",
                        ComponentCode = "0001_R",
                        Description = "Report",
                        CreateDate = DateTime.Parse("2026-01-27 10:19:14.7674391"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:19:14.7674391"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("4F3B116B-8DFD-471C-8561-74261791CD46"),
                        ComponentName = "Setting",
                        ComponentCode = "0001_S",
                        Description = "Setting",
                        CreateDate = DateTime.Parse("2026-01-27 10:19:03.6737571"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:19:03.6737571"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("0B315726-8B21-4116-BEA5-A63944FD7ECB"),
                        ComponentName = "Library - Operation",
                        ComponentCode = "0001_LIB_O",
                        Description = "Library - Operation",
                        CreateDate = DateTime.Parse("2026-01-27 10:35:05.2518922"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:35:05.2518922"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("9F9C5354-2980-4714-BA43-AB3BDF899466"),
                        ComponentName = "Library - Operation Category",
                        ComponentCode = "0001_LIB_OC",
                        Description = "Library - Operation Category",
                        CreateDate = DateTime.Parse("2026-01-27 10:17:38.6430541"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:17:38.6430541"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("53123EA1-5151-4CDB-97AC-C463854EAE27"),
                        ComponentName = "Library - Route",
                        ComponentCode = "0001_LIB_R",
                        Description = "Library - Route",
                        CreateDate = DateTime.Parse("2026-01-27 10:18:04.2053890"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:18:04.2053890"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("A333DE48-7ADB-43EC-A973-CDF71296A7A1"),
                        ComponentName = "Actual Consumption",
                        ComponentCode = "0001_ACT",
                        Description = "Actual Consumption",
                        CreateDate = DateTime.Parse("2026-01-27 10:16:17.9560674"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:16:17.9560674"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("9346DEE1-B59C-4C8E-B03D-D6FF0A8E8B44"),
                        ComponentName = "Library - Equipment",
                        ComponentCode = "0001_LIB_E",
                        Description = "Library - Equipment",
                        CreateDate = DateTime.Parse("2026-01-27 10:18:17.4709258"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:18:17.4709258"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("B8534954-D739-4AA6-954E-F1390D010EE7"),
                        ComponentName = "Home/Dashboard",
                        ComponentCode = "0001_HD",
                        Description = "Home/Dashboard",
                        CreateDate = DateTime.Parse("2026-01-27 10:15:10.0814992"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:15:10.0814992"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P03_Component
                    {
                        Id = Guid.Parse("2D9CF8C6-0BB5-4C60-A2CD-FC90CF9B38C2"),
                        ComponentName = "Admin view",
                        ComponentCode = "0002_ADM",
                        Description = "Admin view",
                        CreateDate = DateTime.Parse("2026-01-27 10:26:54.6723684"),
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Parse("2026-01-27 10:26:54.6723684"),
                        UpdateUserId = 5615,
                        IsDeleted = false
                    }
                };
                await context.P03_Components.AddRangeAsync(component);
                await context.SaveChangesAsync();
            }
        }
        public static async Task P02_Group(VPPMigrationDbContext context)
        {
            if (!context.P02_Groups.Any())
            {
                var group = new List<P02_Group>
                {
                    new P02_Group
                    {
                        //Id = Guid.NewGuid(),
                        Id = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C"),
                        GroupName = "Admin",
                        Description = "Administrators with full access",
                        CreateDate = DateTime.Now,
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Now,
                        UpdateUserId = 5615,
                        IsDeleted = false
                    },
                    new P02_Group
                    {
                        //Id = Guid.NewGuid(),
                        Id = Guid.Parse("388c6c3a-2801-42dc-bfc0-8a7741264596"),
                        GroupName = "User",
                        Description = "Regular users with limited access",
                        CreateDate = DateTime.Now,
                        CreateUserId = 5615,
                        UpdateDate = DateTime.Now,
                        UpdateUserId = 5615,
                        IsDeleted = false
                    }
                };
                await context.P02_Groups.AddRangeAsync(group);
                await context.SaveChangesAsync();
            }
        }
    }
}
