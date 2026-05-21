using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_shared.Constants;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace gtas_vpp_be.Service.Services
{
    public static class SeedData
    {
        // ── Constants ──────────────────────────────────────────────
        private const int DefaultUserId = 5615;
        private static readonly Guid DefaultPriceListId = Guid.Parse("00000000-0000-0000-0000-000000000700");
        private static readonly Guid AdminGroupId = Guid.Parse("5823B49B-5925-4A89-846A-09063A36040C");
        private static readonly Guid UserGroupId  = Guid.Parse("388C6C3A-2801-42DC-BFC0-8A7741264596");

        // ── Page IDs ───────────────────────────────────────────────
        private static readonly Guid PageDashboard  = Guid.Parse("DE4FCAAE-E585-4B10-9E4E-DBC41D9629D2");
        private static readonly Guid PageSidebar    = Guid.Parse("A9825502-EC31-4AB2-9CC5-7A810F9B7BE8");
        private static readonly Guid PageLibrary    = Guid.Parse("5D88463F-CC1C-40E7-BAAD-018DE589D596");
        private static readonly Guid PagePermission = Guid.Parse("20B988D3-7C9A-41EB-BCA0-D94ACE25AC43");
        private static readonly Guid PageReport     = Guid.Parse("F4C3AECB-7100-48CA-AA37-EAA65EBA8752");

        // ── Component IDs ──────────────────────────────────────────
        private static readonly Guid CompMenuDashboard      = Guid.Parse("127B705A-44E1-42F9-9B64-E77BAD63605F");
        private static readonly Guid CompMenuLibrary        = Guid.Parse("78796E6D-FD41-4ADF-8A43-1100F0FE4D7F");
        private static readonly Guid CompMenuReport         = Guid.Parse("B2F10C28-E04F-457B-B41B-43CD604DB529");
        private static readonly Guid CompMenuPermission     = Guid.Parse("9A96C843-43A2-4F8B-AE13-F5B511D8A9D2");
        private static readonly Guid CompRequestOrder       = Guid.Parse("DF771279-8491-4C09-872E-A860474C340E");
        private static readonly Guid CompRequestCatalog     = Guid.Parse("22A7CA16-2FE3-46F2-9D19-CE4D0D225552");
        private static readonly Guid CompRequestHistory     = Guid.Parse("14670749-6D7E-4815-910E-EA649E61CF2E");
        private static readonly Guid CompRequestDeptSummary = Guid.Parse("F3DDBF74-BFAB-4BF7-9F63-DEE5A1D9EE89");
        private static readonly Guid CompRequestAllSummary  = Guid.Parse("3D32B4E5-10E3-4D3F-8E8C-39E72959849F");
        private static readonly Guid CompRequestApproval    = Guid.Parse("B7132472-913E-40E4-AFAE-1972FB8FA77A");
        private static readonly Guid CompLibClass           = Guid.Parse("8657EA31-D139-4E61-B597-64C4E8E88615");
        private static readonly Guid CompLibCategory        = Guid.Parse("B1F59264-AB93-4E39-BB4C-2397726C69BE");
        private static readonly Guid CompLibItem            = Guid.Parse("A88BF4B4-F5BB-4E5A-8ADA-73C27606E7E2");
        private static readonly Guid CompLibSupplier        = Guid.Parse("02C64E1C-FDB0-4FEB-B788-CEBB06CF94C9");
        private static readonly Guid CompLibPrice           = Guid.Parse("2FFBB515-BF2A-42ED-B4B1-34FBA41017FF");
        private static readonly Guid CompLibPriceList       = Guid.Parse("4C72920C-125F-4923-8408-352030382B26");
        private static readonly Guid CompLibDepartment      = Guid.Parse("3179CAE5-10AF-4F8A-BED1-F8AB7C68D881");
        private static readonly Guid CompPeriodSettle       = Guid.Parse("E7BA9473-961E-4407-986A-94C0FF206039");
        private static readonly Guid CompPermUser           = Guid.Parse("7A1EF33F-FAB9-47D6-88BF-9D69E90DC519");
        private static readonly Guid CompPermComponent      = Guid.Parse("45391DDC-5D7F-429B-B57F-3C4E7278209A");
        private static readonly Guid CompReportView         = Guid.Parse("70603737-45C6-4937-A422-4E4FB0EC52CD");

        // ── P05 Mapping IDs ────────────────────────────────────────
        private static readonly Guid P05_SB_Dashboard  = Guid.Parse("55A469CC-4499-4677-903C-81798BC0F53A");
        private static readonly Guid P05_SB_Library    = Guid.Parse("26AF4773-9D60-4AE6-B014-33D623FBA968");
        private static readonly Guid P05_SB_Report     = Guid.Parse("97E6C9EE-CFFB-4332-AEC3-56824BF2FEFD");
        private static readonly Guid P05_SB_Permission = Guid.Parse("CAB28621-070E-417A-8780-9F0835B0AD5F");
        private static readonly Guid P05_DB_Order      = Guid.Parse("9347D472-AD40-40A5-BAD4-DFC4176531A7");
        private static readonly Guid P05_DB_Catalog    = Guid.Parse("0CEA22F8-2463-4C6D-887A-AA4344B08F93");
        private static readonly Guid P05_DB_History    = Guid.Parse("98D45C13-9637-4BB6-8351-19164FB2627A");
        private static readonly Guid P05_DB_DeptSum    = Guid.Parse("EE872A0B-737E-4D10-87D4-68E567814623");
        private static readonly Guid P05_DB_AllSum     = Guid.Parse("3625955F-4E4B-466D-A56D-A5EC08346F67");
        private static readonly Guid P05_DB_Approval   = Guid.Parse("0FA7B816-82ED-49A1-A7E8-35443175778F");
        private static readonly Guid P05_LB_Class      = Guid.Parse("4C2F1AD1-637B-4DDF-95C5-0D84F5D42ABD");
        private static readonly Guid P05_LB_Category   = Guid.Parse("25B3721D-CDF2-41B2-A5F1-A24A5826E3C4");
        private static readonly Guid P05_LB_Item       = Guid.Parse("0F5560C3-12F5-483D-87AB-FB9DC30D0E54");
        private static readonly Guid P05_LB_Supplier   = Guid.Parse("ED1D4ECD-413C-44CB-9CF3-08008D7C058D");
        private static readonly Guid P05_LB_Price      = Guid.Parse("5658FDBD-686D-4BA8-9BB8-8C629671E5FB");
        private static readonly Guid P05_LB_PriceList  = Guid.Parse("BA035879-1C78-4F48-8297-DA3C8AA7245B");
        private static readonly Guid P05_LB_Dept       = Guid.Parse("00BEAA55-C999-413E-AB6E-C43C29578812");
        private static readonly Guid P05_AP_PeriodSettle = Guid.Parse("8D9A6954-56AF-4B5C-B9F2-39E4EDD0AA3F");
        private static readonly Guid P05_PM_User       = Guid.Parse("19B50733-B09B-460A-9D3A-D855C1C857FD");
        private static readonly Guid P05_PM_Component  = Guid.Parse("F76984E3-E231-4267-9EA5-AFDFEBD268A3");
        private static readonly Guid P05_RP_View       = Guid.Parse("2EFEF4F1-7F17-409B-B156-8DC60B7B8081");

        // ════════════════════════════════════════════════════════════
        //  MAIN ENTRY POINT
        //  Luồng tối ưu: SQL infra → SQL data (LEX02 departments) → C# auth
        // ════════════════════════════════════════════════════════════
        public static async Task Seed(VPPMigrationDbContext context)
        {
            Log.Information("[SeedData] Starting database seeding...");

            // ── PHASE 1: Infrastructure SQL ──────────────────────
            // Tạo GTAS_MENU DB + tblUsers (cần cho Views + SPs)
            await RunSqlSafe(context, "Helpers/SQL/00_Init_GTAS_MENU.sql");

            // Views (dùng cross-database query tới GTAS_MENU, không cần Linked Server)
            await RunSqlSafe(context, "Helpers/SQL/01_Views.sql");

            // Stored Procedures (12 SPs, CREATE OR ALTER)
            await RunSqlSafe(context, "Helpers/SQL/02_StoredProcedures.sql");

            // ── PHASE 2: Library + LEX02 data (SQL) ─────────────
            // PHẢI chạy trước P04_UserGroup vì P04 cần LEX02 department IDs
            await SeedLEX02_Empty(context);
            await RunSqlSafe(context, "Helpers/SQL/03_SeedLibraryData.sql");
            await SeedDefaultPricesFromFile(context);

            // ── PHASE 3: Auth data (C#) ─────────────────────────
            // P01 → P02 → P03 → P04 (lookup LEX02 IDs) → P05 → P06
            await SeedP01_Page(context);
            await SeedP02_Group(context);
            await SeedP03_Component(context);
            await SeedP04_UserGroup(context); // Lookup LEX02 department IDs dynamically
            await SeedP05_PageComponentMapping(context);
            await SeedP06_GroupPageComponentMapping(context);

            Log.Information("[SeedData] Database seeding completed.");
        }

        /// <summary>
        /// Chạy SQL file an toàn — log warning nếu lỗi, không crash app.
        /// </summary>
        private static async Task RunSqlSafe(VPPMigrationDbContext context, string path)
        {
            try
            {
                await SqlBatchExecutor.ExecuteSqlFileAsync(context.Database, path);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[SeedData] {File} failed. Skipping...", Path.GetFileName(path));
            }
        }

        private sealed record DefaultPriceSeedRow(
            string CategoryName,
            string ItemName,
            string UomName,
            decimal Price);

        private static async Task SeedDefaultPricesFromFile(VPPMigrationDbContext context)
        {
            var priceRows = ReadDefaultPriceRows();
            if (priceRows.Count == 0)
            {
                return;
            }

            var now = DateTime.Now;
            var hcmSupplier = await context.L05_VPPSuppliers
                .FirstOrDefaultAsync(x => x.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName && !x.IsDeleted);
            if (hcmSupplier == null)
            {
                Log.Warning("[SeedData] Default supplier {SupplierShortName} not found; prices.txt seed skipped.",
                    VppPricingDefaults.DefaultSupplierShortName);
                return;
            }

            var defaultPriceList = await context.L07_PriceLists
                .FirstOrDefaultAsync(x => x.Id == DefaultPriceListId && !x.IsDeleted);
            if (defaultPriceList == null)
            {
                defaultPriceList = new L07_PriceList
                {
                    Id = DefaultPriceListId,
                    PriceListCode = "DEFAULT",
                    PriceListName = "Default Price List",
                    IsDefault = true,
                    CreateUserId = DefaultUserId,
                    CreateDate = now,
                    UpdateUserId = DefaultUserId,
                    UpdateDate = now,
                    IsDeleted = false
                };
                context.L07_PriceLists.Add(defaultPriceList);
                await context.SaveChangesAsync();
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            var activeProducts = await context.L04_VPPs
                .Include(x => x.VPPCategory)
                .Where(x => !x.IsDeleted)
                .ToListAsync();
            var productIds = activeProducts.Select(x => x.Id).ToArray();

            var activeMappings = await context.L06_VPPSupplierMappings
                .Where(x => x.L07_PriceListId == defaultPriceList.Id
                         && productIds.Contains(x.L04_VPPId)
                         && !x.IsDeleted)
                .ToListAsync();

            foreach (var mapping in activeMappings.Where(x => x.IsDefault))
            {
                mapping.IsDefault = false;
                mapping.UpdateUserId = DefaultUserId;
                mapping.UpdateDate = now;
            }

            await context.SaveChangesAsync();

            var hcmMappings = activeMappings
                .Where(x => x.L05_VPPSupplierId == hcmSupplier.Id)
                .GroupBy(x => x.L04_VPPId)
                .ToDictionary(x => x.Key, x => x.OrderBy(m => m.CreateDate).First());

            var duplicateHcmMappings = activeMappings
                .Where(x => x.L05_VPPSupplierId == hcmSupplier.Id)
                .GroupBy(x => x.L04_VPPId)
                .SelectMany(x => x.OrderBy(m => m.CreateDate).Skip(1))
                .ToList();
            foreach (var duplicate in duplicateHcmMappings)
            {
                duplicate.IsDeleted = true;
                duplicate.IsDefault = false;
                duplicate.UpdateUserId = DefaultUserId;
                duplicate.UpdateDate = now;
            }

            var pricesByItemAndCategory = priceRows
                .GroupBy(x => (Item: NormalizePriceKey(x.ItemName), Category: NormalizePriceKey(x.CategoryName)))
                .ToDictionary(x => x.Key, x => x.First());
            var pricesByItem = priceRows
                .GroupBy(x => NormalizePriceKey(x.ItemName))
                .ToDictionary(x => x.Key, x => x.ToList());

            var updatedPriceCount = 0;
            var defaultedCount = 0;
            var unmatchedProducts = new List<string>();

            foreach (var product in activeProducts)
            {
                var priceRow = FindPriceRow(product, pricesByItemAndCategory, pricesByItem);
                if (priceRow == null)
                {
                    unmatchedProducts.Add(product.VPPName ?? product.Id.ToString());
                }

                if (!hcmMappings.TryGetValue(product.Id, out var mapping))
                {
                    mapping = new L06_VPPSupplierMapping
                    {
                        Id = Guid.NewGuid(),
                        L04_VPPId = product.Id,
                        L05_VPPSupplierId = hcmSupplier.Id,
                        L07_PriceListId = defaultPriceList.Id,
                        Price = priceRow?.Price ?? 0,
                        IsDefault = true,
                        Description = "Seeded default price from prices.txt",
                        CreateUserId = DefaultUserId,
                        CreateDate = now,
                        UpdateUserId = DefaultUserId,
                        UpdateDate = now,
                        IsDeleted = false
                    };
                    context.L06_VPPSupplierMappings.Add(mapping);
                    hcmMappings[product.Id] = mapping;
                    defaultedCount++;
                    if (priceRow != null)
                    {
                        updatedPriceCount++;
                    }
                    continue;
                }

                if (priceRow != null && mapping.Price != priceRow.Price)
                {
                    mapping.Price = priceRow.Price;
                    updatedPriceCount++;
                }

                if (!mapping.IsDefault)
                {
                    defaultedCount++;
                }

                mapping.IsDefault = true;
                mapping.UpdateUserId = DefaultUserId;
                mapping.UpdateDate = now;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (unmatchedProducts.Count > 0)
            {
                Log.Warning("[SeedData] prices.txt did not match {Count} active VPP item(s): {Items}",
                    unmatchedProducts.Count,
                    string.Join(", ", unmatchedProducts.Take(10)));
            }

            Log.Information(
                "[SeedData] prices.txt seed completed. Items: {ItemCount}, price updates: {UpdatedPriceCount}, default supplier rows: {DefaultedCount}, VAT source: C# constant {VatRate:P0}.",
                activeProducts.Count,
                updatedPriceCount,
                defaultedCount,
                VppPricingDefaults.VatRate);
        }

        private static DefaultPriceSeedRow? FindPriceRow(
            L04_VPP product,
            IReadOnlyDictionary<(string Item, string Category), DefaultPriceSeedRow> pricesByItemAndCategory,
            IReadOnlyDictionary<string, List<DefaultPriceSeedRow>> pricesByItem)
        {
            var itemKey = NormalizePriceKey(product.VPPName);
            var categoryKey = NormalizePriceKey(product.VPPCategory?.VPPCategoryName);

            if (pricesByItemAndCategory.TryGetValue((itemKey, categoryKey), out var categoryMatch))
            {
                return categoryMatch;
            }

            if (!pricesByItem.TryGetValue(itemKey, out var itemMatches))
            {
                return null;
            }

            return itemMatches.Select(x => x.Price).Distinct().Count() == 1
                ? itemMatches.First()
                : null;
        }

        private static List<DefaultPriceSeedRow> ReadDefaultPriceRows()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Helpers", "Data", "prices.txt");
            if (!File.Exists(filePath))
            {
                Log.Warning("[SeedData] prices.txt not found at {Path}; default price seed skipped.", filePath);
                return new List<DefaultPriceSeedRow>();
            }

            var result = new List<DefaultPriceSeedRow>();
            string? currentCategory = null;
            var lineNumber = 0;

            foreach (var line in File.ReadLines(filePath))
            {
                lineNumber++;
                var columns = line.Split('\t');
                if (columns.Length == 0)
                {
                    continue;
                }

                var itemName = columns.ElementAtOrDefault(0)?.Trim() ?? string.Empty;
                var uomName = columns.ElementAtOrDefault(1)?.Trim() ?? string.Empty;
                var vatText = columns.ElementAtOrDefault(2)?.Trim() ?? string.Empty;
                var priceText = columns.ElementAtOrDefault(3)?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(itemName) || itemName.Equals("Tên VPP", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(priceText))
                {
                    currentCategory = itemName;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(currentCategory))
                {
                    Log.Warning("[SeedData] prices.txt line {LineNumber} has price without category: {ItemName}", lineNumber, itemName);
                    continue;
                }

                if (!TryParsePrice(priceText, out var price))
                {
                    Log.Warning("[SeedData] prices.txt line {LineNumber} has invalid price '{PriceText}' for item {ItemName}.",
                        lineNumber,
                        priceText,
                        itemName);
                    continue;
                }

                if (TryParseVatRate(vatText, out var vatRate) && vatRate != VppPricingDefaults.VatRate)
                {
                    Log.Warning("[SeedData] prices.txt line {LineNumber} has VAT {VatRate:P0}; DB seed uses C# VAT constant {DefaultVatRate:P0}.",
                        lineNumber,
                        vatRate,
                        VppPricingDefaults.VatRate);
                }

                result.Add(new DefaultPriceSeedRow(currentCategory, itemName, uomName, price));
            }

            return result;
        }

        private static bool TryParsePrice(string value, out decimal price)
        {
            var normalized = value.Trim().Replace(".", string.Empty).Replace(",", string.Empty);
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
        }

        private static bool TryParseVatRate(string value, out decimal vatRate)
        {
            vatRate = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Trim().TrimEnd('%');
            if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var percent))
            {
                return false;
            }

            vatRate = percent / 100m;
            return true;
        }

        private static string NormalizePriceKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value
                .Normalize(NormalizationForm.FormC)
                .Trim()
                .ToLowerInvariant()
                .Replace('_', ',')
                .Replace('’', '\'')
                .Replace('`', '\'');

            while (normalized.Contains("''", StringComparison.Ordinal))
            {
                normalized = normalized.Replace("''", "'", StringComparison.Ordinal);
            }

            normalized = Regex.Replace(normalized, @"\s+", " ");
            normalized = Regex.Replace(normalized, @"\s+,", ",");
            normalized = Regex.Replace(normalized, @",\s*$", string.Empty);

            return normalized;
        }

        // ════════════════════════════════════════════════════════════
        //  LEX02 — Guid.Empty row (bypass EF auto-gen)
        // ════════════════════════════════════════════════════════════
        private static async Task SeedLEX02_Empty(VPPMigrationDbContext context)
        {
            bool isExist = context.LEX02_CompanyDepartmentLocations.Any(x => x.Id == Guid.Empty);
            if (!isExist)
            {
                string emptyId = Guid.Empty.ToString();
                string sql = $@"
                    INSERT INTO LEX02_CompanyDepartmentLocation 
                    (Id, LEX02Type, LEX02Code, LEX02Name, CreateDate, CreateUserId, UpdateDate, UpdateUserId, IsDeleted)
                    VALUES 
                    ('{emptyId}', 'System', 'SYS_DEFAULT', 'System Default Location', GETDATE(), {DefaultUserId}, GETDATE(), {DefaultUserId}, 0)
                ";
                await context.Database.ExecuteSqlRawAsync(sql);
                Log.Information("[SeedData] LEX02: Seeded Guid.Empty row");
            }
        }

        // ════════════════════════════════════════════════════════════
        //  P01_Page — 5 pages
        // ════════════════════════════════════════════════════════════
        private static async Task SeedP01_Page(VPPMigrationDbContext context)
        {
            if (context.P01_Pages.Any()) return;

            var now = DateTime.Now;
            var pages = new List<P01_Page>
            {
                new() { Id = PageSidebar, PageCode = "SIDEBAR", PageName = "Sidebar Menu", Type = "Menu",
                        Description = "Root Sidebar", CreateUserId = DefaultUserId, CreateDate = now,
                        UpdateUserId = DefaultUserId, UpdateDate = now, IsDeleted = false },
                new() { Id = PageDashboard, PageCode = "DASHBOARD", PageName = "Dashboard", Type = "Page",
                        Description = "Request Workspace", CreateUserId = DefaultUserId, CreateDate = now,
                        UpdateUserId = DefaultUserId, UpdateDate = now, IsDeleted = false },
                new() { Id = PageLibrary, PageCode = "LIBRARY", PageName = "Library", Type = "Page",
                        Description = "Categories", CreateUserId = DefaultUserId, CreateDate = now,
                        UpdateUserId = DefaultUserId, UpdateDate = now, IsDeleted = false },
                new() { Id = PagePermission, PageCode = "PERMISSION", PageName = "Permission", Type = "Page",
                        Description = "Security", CreateUserId = DefaultUserId, CreateDate = now,
                        UpdateUserId = DefaultUserId, UpdateDate = now, IsDeleted = false },
                new() { Id = PageReport, PageCode = "REPORT", PageName = "Report", Type = "Page",
                        Description = "System Reports", CreateUserId = DefaultUserId, CreateDate = now,
                        UpdateUserId = DefaultUserId, UpdateDate = now, IsDeleted = false }
            };
            await context.P01_Pages.AddRangeAsync(pages);
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P01_Page: {Count} pages", pages.Count);
        }

        // ════════════════════════════════════════════════════════════
        //  P02_Group — 2 groups
        // ════════════════════════════════════════════════════════════
        private static async Task SeedP02_Group(VPPMigrationDbContext context)
        {
            if (context.P02_Groups.Any()) return;

            var now = DateTime.Now;
            var groups = new List<P02_Group>
            {
                new() { Id = AdminGroupId, GroupName = "Admin",
                        Description = "Administrators with full access",
                        CreateUserId = DefaultUserId, CreateDate = now,
                        UpdateUserId = DefaultUserId, UpdateDate = now, IsDeleted = false },
                new() { Id = UserGroupId, GroupName = "User",
                        Description = "Regular users with limited access",
                        CreateUserId = DefaultUserId, CreateDate = now,
                        UpdateUserId = DefaultUserId, UpdateDate = now, IsDeleted = false }
            };
            await context.P02_Groups.AddRangeAsync(groups);
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P02_Group: {Count} groups", groups.Count);
        }

        // ════════════════════════════════════════════════════════════
        //  P03_Component
        // ════════════════════════════════════════════════════════════
        private static async Task SeedP03_Component(VPPMigrationDbContext context)
        {
            var now = DateTime.Now;
            var components = new List<P03_Component>
            {
                C("MENU_DASHBOARD",           "Menu - Dashboard",           "View Dashboard",   CompMenuDashboard, now),
                C("MENU_LIBRARY",             "Menu - Library",             "View Library",     CompMenuLibrary, now),
                C("MENU_REPORT",              "Menu - Report",              "View Report",      CompMenuReport, now),
                C("MENU_PERMISSION",          "Menu - Permission",          "View Permission",  CompMenuPermission, now),
                C("REQUEST_ORDER",            "Request Order",              "Orders Tab",       CompRequestOrder, now),
                C("REQUEST_PRODUCT_CATALOG",  "Request Product Catalog",    "Catalog Tab",      CompRequestCatalog, now),
                C("REQUEST_HISTORY",          "Request History",            "History Tab",      CompRequestHistory, now),
                C("REQUEST_DEPARTMENT_SUMMARY","Request Dept Summary",      "Dept Summary Tab", CompRequestDeptSummary, now),
                C("REQUEST_ALL_ORDERS_SUMMARY","Request All Orders Summary","Admin Summary",    CompRequestAllSummary, now),
                C("REQUEST_ADMIN_APPROVAL",   "Request Admin Approval",     "Admin Approval",   CompRequestApproval, now),
                C("LIBRARY_CLASS",            "Library - Class",            "Class",            CompLibClass, now),
                C("LIBRARY_CATEGORY",         "Library - Category",         "Category",         CompLibCategory, now),
                C("LIBRARY_ITEM",             "Library - Item",             "Item",             CompLibItem, now),
                C("LIBRARY_SUPPLIER",         "Library - Supplier",         "Supplier",         CompLibSupplier, now),
                C("LIBRARY_PRICE",            "Library - Price",            "Price",            CompLibPrice, now),
                C("LIBRARY_PRICE_LIST",       "Library - Price List",       "Price List",       CompLibPriceList, now),
                C("LIBRARY_DEPARTMENT",       "Library - Department",       "Dept",             CompLibDepartment, now),
                C("PERIOD_SETTLE",            "Period Settlement",          "Settle Period",    CompPeriodSettle, now),
                C("PERMISSION_USER",          "Permission - User",          "User Auth",        CompPermUser, now),
                C("PERMISSION_COMPONENT",     "Permission - Component",     "Comp Mapping",     CompPermComponent, now),
                C("REPORT_VIEW",              "Report - View",              "View Report",      CompReportView, now)
            };
            var existingCodes = await context.P03_Components.Select(x => x.ComponentCode).ToListAsync();
            var missing = components.Where(x => !existingCodes.Contains(x.ComponentCode)).ToList();
            if (missing.Count == 0) return;

            await context.P03_Components.AddRangeAsync(missing);
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P03_Component: {Count} components", missing.Count);
        }

        private static P03_Component C(string code, string name, string desc, Guid id, DateTime now)
            => new() { Id = id, ComponentCode = code, ComponentName = name, Description = desc,
                       CreateUserId = DefaultUserId, CreateDate = now,
                       UpdateUserId = DefaultUserId, UpdateDate = now, IsDeleted = false };

        // ════════════════════════════════════════════════════════════
        //  P04_UserGroup — 12 users, lookup department IDs dynamically
        //  Tại thời điểm này, 03_SeedLibraryData.sql đã chạy → LEX02 departments đã tồn tại
        // ════════════════════════════════════════════════════════════
        private static async Task SeedP04_UserGroup(VPPMigrationDbContext context)
        {
            if (context.P04_UserGroups.Any()) return;

            // Lookup department IDs by code (dynamic, không hardcode GUID)
            // Dùng GroupBy vì có thể có duplicate LEX02Code (e.g. "SOURCING")
            var deptLookup = context.LEX02_CompanyDepartmentLocations
                .Where(x => !x.IsDeleted)
                .AsEnumerable()
                .GroupBy(x => x.LEX02Code ?? "")
                .ToDictionary(g => g.Key, g => g.First().Id);

            // Fallback: nếu không tìm thấy department → dùng Guid.Empty
            Guid GetDept(string code) => deptLookup.GetValueOrDefault(code, Guid.Empty);

            var now = DateTime.Now;
            var userGroups = new List<P04_UserGroup>
            {
                // google → Admin, IT
                UG(4519, AdminGroupId, GetDept("IT"), now),
                // test_admin_1-5 → Admin, mỗi người 1 phòng ban
                UG(4520, AdminGroupId, GetDept("HCQT"), now),
                UG(4521, AdminGroupId, GetDept("TCKT"), now),
                UG(4522, AdminGroupId, GetDept("PURCHASING"), now),
                UG(4523, AdminGroupId, GetDept("KD1"), now),
                UG(4524, AdminGroupId, GetDept("QA"), now),
                // test_user_11-15 → User, paired departments
                UG(4530, UserGroupId, GetDept("HCQT"), now),
                UG(4531, UserGroupId, GetDept("TCKT"), now),
                UG(4532, UserGroupId, GetDept("PURCHASING"), now),
                UG(4533, UserGroupId, GetDept("KD1"), now),
                UG(4534, UserGroupId, GetDept("QA"), now),
                // admin (user 1) → User, IT (extra IT user)
                UG(1, UserGroupId, GetDept("IT"), now),
            };
            await context.P04_UserGroups.AddRangeAsync(userGroups);
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P04_UserGroup: {Count} user-group mappings", userGroups.Count);
        }

        private static P04_UserGroup UG(int userId, Guid groupId, Guid deptId, DateTime now)
            => new() { Id = Guid.NewGuid(), UserId = userId, P02_GroupId = groupId,
                       LEX02_CompanyDepartmentLocationId = deptId,
                       Description = "Auto seeded",
                       CreateUserId = DefaultUserId, CreateDate = now,
                       UpdateUserId = DefaultUserId, UpdateDate = now, IsDeleted = false };

        // ════════════════════════════════════════════════════════════
        //  P05_PageComponentMapping
        // ════════════════════════════════════════════════════════════
        private static async Task SeedP05_PageComponentMapping(VPPMigrationDbContext context)
        {
            var mappings = new List<P05_PageComponentMapping>
            {
                P5(P05_SB_Dashboard,  PageSidebar,    CompMenuDashboard),
                P5(P05_SB_Library,    PageSidebar,    CompMenuLibrary),
                P5(P05_SB_Report,     PageSidebar,    CompMenuReport),
                P5(P05_SB_Permission, PageSidebar,    CompMenuPermission),
                P5(P05_DB_Order,      PageDashboard,  CompRequestOrder),
                P5(P05_DB_Catalog,    PageDashboard,  CompRequestCatalog),
                P5(P05_DB_History,    PageDashboard,  CompRequestHistory),
                P5(P05_DB_DeptSum,    PageDashboard,  CompRequestDeptSummary),
                P5(P05_DB_AllSum,     PageDashboard,  CompRequestAllSummary),
                P5(P05_DB_Approval,   PageDashboard,  CompRequestApproval),
                P5(P05_LB_Class,      PageLibrary,    CompLibClass),
                P5(P05_LB_Category,   PageLibrary,    CompLibCategory),
                P5(P05_LB_Item,       PageLibrary,    CompLibItem),
                P5(P05_LB_Supplier,   PageLibrary,    CompLibSupplier),
                P5(P05_LB_Price,      PageLibrary,    CompLibPrice),
                P5(P05_LB_PriceList,  PageLibrary,    CompLibPriceList),
                P5(P05_LB_Dept,       PageLibrary,    CompLibDepartment),
                P5(P05_AP_PeriodSettle, PageDashboard, CompPeriodSettle),
                P5(P05_PM_User,       PagePermission, CompPermUser),
                P5(P05_PM_Component,  PagePermission, CompPermComponent),
                P5(P05_RP_View,       PageReport,     CompReportView)
            };
            var existingIds = await context.P05_PageComponentMappings.Select(x => x.Id).ToListAsync();
            var missing = mappings.Where(x => !existingIds.Contains(x.Id)).ToList();
            if (missing.Count == 0) return;

            await context.P05_PageComponentMappings.AddRangeAsync(missing);
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P05_PageComponentMapping: {Count} mappings", missing.Count);
        }

        private static P05_PageComponentMapping P5(Guid id, Guid pageId, Guid componentId)
            => new() { Id = id, P01_PageId = pageId, P03_ComponentId = componentId };

        // ════════════════════════════════════════════════════════════
        //  P06_GroupPageComponentMapping
        //  Admin: all page-component mappings, User: selected mappings
        // ════════════════════════════════════════════════════════════
        private static async Task SeedP06_GroupPageComponentMapping(VPPMigrationDbContext context)
        {
            var now = DateTime.Now;
            var mappings = new List<P06_GroupPageComponentMapping>();

            // Admin group → all page-component mappings
            var allP05Ids = new[]
            {
                P05_SB_Dashboard, P05_SB_Library, P05_SB_Report, P05_SB_Permission,
                P05_DB_Order, P05_DB_Catalog, P05_DB_History, P05_DB_DeptSum,
                P05_DB_AllSum, P05_DB_Approval, P05_AP_PeriodSettle,
                P05_LB_Class, P05_LB_Category, P05_LB_Item, P05_LB_Supplier, P05_LB_Price, P05_LB_PriceList, P05_LB_Dept,
                P05_PM_User, P05_PM_Component,
                P05_RP_View
            };
            foreach (var p05Id in allP05Ids)
                mappings.Add(P6(p05Id, AdminGroupId, now));

            // User group → 8 limited mappings
            var userP05Ids = new[]
            {
                P05_SB_Dashboard, P05_SB_Report, P05_SB_Library,
                P05_DB_Order, P05_DB_Catalog, P05_DB_History,
                P05_DB_DeptSum, P05_RP_View
            };
            foreach (var p05Id in userP05Ids)
                mappings.Add(P6(p05Id, UserGroupId, now));

            var existingKeys = await context.P06_GroupPageComponentMappings
                .Select(x => new { x.P05_PageComponentMappingId, x.P02_GroupId, x.MemberCompanyCode })
                .ToListAsync();
            var missing = mappings
                .Where(x => !existingKeys.Any(k => k.P05_PageComponentMappingId == x.P05_PageComponentMappingId
                                                && k.P02_GroupId == x.P02_GroupId
                                                && k.MemberCompanyCode == x.MemberCompanyCode))
                .ToList();
            if (missing.Count == 0) return;

            await context.P06_GroupPageComponentMappings.AddRangeAsync(missing);
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P06_GroupPageComponentMapping: {Count} mappings", missing.Count);
        }

        private static P06_GroupPageComponentMapping P6(Guid p05Id, Guid groupId, DateTime now)
            => new() { P05_PageComponentMappingId = p05Id, P02_GroupId = groupId,
                       MemberCompanyCode = 77500, IsEnable = true, IsVisible = true,
                       CreateUserId = DefaultUserId, CreateDate = now,
                       UpdateUserId = DefaultUserId, UpdateDate = now };
    }
}
