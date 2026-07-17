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
        // Keep reference and demo markers independent. A failed profile must
        // never be recorded as applied, and production reference bootstrap
        // must not be coupled to the optional demo fixture.
        private const string ReferenceSeedVersion = "2026-07-15-reference-2-flat-rbac";
        private const string DemoSeedVersion = "2026-07-15-demo-1";
        private static readonly Guid DefaultPriceListId = Guid.Parse("00000000-0000-0000-0000-000000000700");

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
        public static async Task SeedReference(VPPMigrationDbContext context)
        {
            await EnsureSeedHistoryTableAsync(context);

            // These objects live partly in the compatibility GTAS_MENU
            // database, so validate/reconcile them on every run even when the
            // profile marker already exists. This repairs a dropped view/table
            // instead of trusting a stale marker.
            await RunSqlRequired(context, "Helpers/SQL/00_Init_GTAS_MENU.sql");
            await RunSqlRequired(context, "Helpers/SQL/01_Views.sql");
            await RunSqlRequired(context, "Helpers/SQL/02_StoredProcedures.sql");
            await RunSqlRequired(context, "Helpers/SQL/04_RetireLegacyAuth.sql");

            var alreadyApplied = await IsSeedVersionAppliedAsync(context, ReferenceSeedVersion);
            Log.Information(
                "[SeedData] Reconciling reference seed {SeedVersion}; marker present: {AlreadyApplied}.",
                ReferenceSeedVersion,
                alreadyApplied);

            await SeedLEX02_Empty(context);
            await SeedP01_Page(context);
            await SeedP02_Group(context);
            await SeedP03_Component(context);
            await SeedP05_PageComponentMapping(context);
            await SeedP06_GroupPageComponentMapping(context);
            if (!alreadyApplied)
                await MarkSeedVersionAppliedAsync(context, ReferenceSeedVersion);
            Log.Information("[SeedData] Reference database bootstrap completed.");
        }

        public static async Task SeedDemo(VPPMigrationDbContext context)
        {
            await SeedReference(context);
            await EnsureSeedHistoryTableAsync(context);

            // Demo mode deliberately contains no users, credentials, e-mail
            // addresses or user-to-role assignments. Account provisioning is
            // owned by the authenticated registration/admin workflow.
            // Reconcile this idempotent fixture on every explicit demo run so
            // a stale history marker cannot hide missing catalog/permission data.
            Log.Information("[SeedData] Reconciling non-sensitive demo fixture...");
            await RunSqlRequired(context, "Helpers/SQL/03_SeedLibraryData.sql");
            await SeedDefaultPricesFromFile(context);

            await MarkSeedVersionAppliedAsync(context, DemoSeedVersion);

            Log.Information("[SeedData] Non-sensitive demo fixture completed.");
        }

        private static Task EnsureSeedHistoryTableAsync(VPPMigrationDbContext context)
        {
            return context.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[dbo].[__GTASSeedHistory]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[__GTASSeedHistory]
                    (
                        [SeedVersion] nvarchar(128) NOT NULL
                            CONSTRAINT [PK___GTASSeedHistory] PRIMARY KEY,
                        [AppliedUtc] datetime2 NOT NULL
                    );
                END
                """);
        }

        private static async Task<bool> IsSeedVersionAppliedAsync(
            VPPMigrationDbContext context,
            string seedVersion)
        {
            var count = await context.Database
                .SqlQuery<int>($"""
                    SELECT COUNT(*) AS [Value]
                    FROM [dbo].[__GTASSeedHistory]
                    WHERE [SeedVersion] = {seedVersion}
                    """)
                .SingleAsync();

            return count > 0;
        }

        private static Task MarkSeedVersionAppliedAsync(
            VPPMigrationDbContext context,
            string seedVersion)
        {
            return context.Database.ExecuteSqlInterpolatedAsync($"""
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM [dbo].[__GTASSeedHistory]
                    WHERE [SeedVersion] = {seedVersion}
                )
                BEGIN
                    INSERT INTO [dbo].[__GTASSeedHistory] ([SeedVersion], [AppliedUtc])
                    VALUES ({seedVersion}, SYSUTCDATETIME());
                END
                """);
        }

        /// <summary>
        /// Required SQL files are fail-fast; errors must reach the migrator.
        /// </summary>
        private static Task RunSqlRequired(VPPMigrationDbContext context, string path)
            => SqlBatchExecutor.ExecuteSqlFileAsync(context.Database, path);

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
                throw new InvalidDataException("prices.txt did not contain any usable price rows.");
            }

            var now = DateTime.Now;
            var hcmSupplier = await context.L05_VPPSuppliers
                .FirstOrDefaultAsync(x => x.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName && !x.IsDeleted);
            if (hcmSupplier == null)
            {
                throw new InvalidOperationException(
                    $"Default supplier '{VppPricingDefaults.DefaultSupplierShortName}' is required by the demo price seed.");
            }

            var defaultPriceList = await context.L07_PriceLists
                .FirstOrDefaultAsync(x => x.Id == DefaultPriceListId && !x.IsDeleted);
            await using var transaction = await context.Database.BeginTransactionAsync();
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
            }

            var activeProducts = await context.L04_VPPs
                .Include(x => x.VPPCategory)
                .Where(x => !x.IsDeleted)
                .ToListAsync();
            if (activeProducts.Count == 0)
            {
                throw new InvalidDataException("Demo catalog seed produced no active VPP items.");
            }
            var productIds = activeProducts.Select(x => x.Id).ToArray();

            var activeMappings = await context.L06_VPPSupplierMappings
                .Where(x => x.L07_PriceListId == defaultPriceList.Id
                         && productIds.Contains(x.L04_VPPId)
                         && !x.IsDeleted)
                .ToListAsync();

            foreach (var mapping in activeMappings.Where(x =>
                         x.IsDefault && x.L05_VPPSupplierId != hcmSupplier.Id))
            {
                mapping.IsDefault = false;
                mapping.UpdateUserId = DefaultUserId;
                mapping.UpdateDate = now;
            }

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

                var mappingChanged = false;
                if (priceRow != null && mapping.Price != priceRow.Price)
                {
                    mapping.Price = priceRow.Price;
                    updatedPriceCount++;
                    mappingChanged = true;
                }

                if (!mapping.IsDefault)
                {
                    defaultedCount++;
                    mapping.IsDefault = true;
                    mappingChanged = true;
                }

                if (mappingChanged)
                {
                    mapping.UpdateUserId = DefaultUserId;
                    mapping.UpdateDate = now;
                }
            }

            if (unmatchedProducts.Count > 0)
            {
                throw new InvalidDataException(
                    $"prices.txt did not match {unmatchedProducts.Count} active VPP item(s): " +
                    string.Join(", ", unmatchedProducts.Take(10)));
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

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
            var existingCodes = await context.P01_Pages
                .Select(x => x.PageCode)
                .ToListAsync();
            var missing = pages.Where(x => !existingCodes.Contains(x.PageCode)).ToList();
            if (missing.Count == 0) return;

            await context.P01_Pages.AddRangeAsync(missing);
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P01_Page: {Count} pages", missing.Count);
        }

        // ════════════════════════════════════════════════════════════
        //  P02_Group — 2 groups
        // ════════════════════════════════════════════════════════════
        private static async Task SeedP02_Group(VPPMigrationDbContext context)
        {
            var now = DateTime.Now;
            var personaIds = CanonicalRbac.Personas.Select(x => x.GroupId).ToArray();
            var existing = await context.P02_Groups
                .Where(x => personaIds.Contains(x.Id))
                .ToListAsync();
            var added = 0;
            var updated = 0;

            foreach (var persona in CanonicalRbac.Personas)
            {
                var group = existing.SingleOrDefault(x => x.Id == persona.GroupId);
                if (group is null)
                {
                    await context.P02_Groups.AddAsync(new P02_Group
                    {
                        Id = persona.GroupId,
                        GroupCode = persona.GroupCode,
                        GroupName = persona.GroupName,
                        Description = persona.Description,
                        ParentGroupId = null,
                        CreateUserId = DefaultUserId,
                        CreateDate = now,
                        UpdateUserId = DefaultUserId,
                        UpdateDate = now,
                        IsDeleted = false
                    });
                    added++;
                    continue;
                }

                if (group.GroupCode == persona.GroupCode
                    && group.GroupName == persona.GroupName
                    && group.Description == persona.Description
                    && group.ParentGroupId is null
                    && !group.IsDeleted)
                {
                    continue;
                }

                group.GroupCode = persona.GroupCode;
                group.GroupName = persona.GroupName;
                group.Description = persona.Description;
                group.ParentGroupId = null;
                group.IsDeleted = false;
                group.UpdateUserId = DefaultUserId;
                group.UpdateDate = now;
                updated++;
            }

            if (added == 0 && updated == 0) return;
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P02_Group reconciled: {Added} added, {Updated} updated", added, updated);
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

            foreach (var action in CanonicalRbac.Actions.Where(x => x.PermissionCode != Permissions.PeriodSettle))
            {
                components.Add(C(
                    action.PermissionCode,
                    action.Name,
                    action.Description,
                    CanonicalRbacSeedIds.ActionComponent(action.PermissionCode),
                    now));
            }

            var periodSettle = CanonicalRbac.Actions.Single(x => x.PermissionCode == Permissions.PeriodSettle);
            var desiredPeriodSettle = components.Single(x => x.ComponentCode == Permissions.PeriodSettle);
            desiredPeriodSettle.ComponentName = periodSettle.Name;
            desiredPeriodSettle.Description = periodSettle.Description;

            var desiredCodes = components.Select(x => x.ComponentCode).ToArray();
            var existing = await context.P03_Components
                .Where(x => desiredCodes.Contains(x.ComponentCode))
                .ToListAsync();
            var existingByCode = existing
                .GroupBy(x => x.ComponentCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(x => x.IsDeleted).ThenBy(x => x.Id).First(),
                    StringComparer.OrdinalIgnoreCase);
            var added = 0;
            var updated = 0;

            foreach (var desired in components)
            {
                if (!existingByCode.TryGetValue(desired.ComponentCode, out var component))
                {
                    await context.P03_Components.AddAsync(desired);
                    added++;
                    continue;
                }

                if (component.ComponentCode == desired.ComponentCode
                    && component.ComponentName == desired.ComponentName
                    && component.Description == desired.Description
                    && !component.IsDeleted)
                {
                    continue;
                }

                component.ComponentCode = desired.ComponentCode;
                component.ComponentName = desired.ComponentName;
                component.Description = desired.Description;
                component.IsDeleted = false;
                component.UpdateUserId = DefaultUserId;
                component.UpdateDate = now;
                updated++;
            }

            if (added == 0 && updated == 0) return;
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P03_Component reconciled: {Added} added, {Updated} updated", added, updated);
        }

        private static P03_Component C(string code, string name, string desc, Guid id, DateTime now)
            => new() { Id = id, ComponentCode = code, ComponentName = name, Description = desc,
                       CreateUserId = DefaultUserId, CreateDate = now,
                       UpdateUserId = DefaultUserId, UpdateDate = now, IsDeleted = false };

        // ════════════════════════════════════════════════════════════
        //  P05_PageComponentMapping
        // ════════════════════════════════════════════════════════════
        private static async Task SeedP05_PageComponentMapping(VPPMigrationDbContext context)
        {
            var specs = GetP05SeedMappings();
            var pageCodes = specs.Select(x => x.PageCode).Distinct().ToArray();
            var componentCodes = specs.Select(x => x.ComponentCode).Distinct().ToArray();
            var pages = await context.P01_Pages
                .Where(x => pageCodes.Contains(x.PageCode))
                .ToListAsync();
            var components = await context.P03_Components
                .Where(x => componentCodes.Contains(x.ComponentCode))
                .ToListAsync();
            var pageByCode = pages
                .GroupBy(x => x.PageCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(page => page.IsDeleted).ThenBy(page => page.Id).First(),
                    StringComparer.OrdinalIgnoreCase);
            var componentByCode = components
                .GroupBy(x => x.ComponentCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(component => component.IsDeleted).ThenBy(component => component.Id).First(),
                    StringComparer.OrdinalIgnoreCase);

            var desired = specs.Select(spec =>
            {
                if (!pageByCode.TryGetValue(spec.PageCode, out var page))
                    throw new InvalidOperationException($"Cannot seed permission mapping: page '{spec.PageCode}' is missing.");
                if (!componentByCode.TryGetValue(spec.ComponentCode, out var component))
                    throw new InvalidOperationException($"Cannot seed permission mapping: component '{spec.ComponentCode}' is missing.");
                return P5(spec.MappingId, page.Id, component.Id);
            }).ToList();

            var desiredIds = desired.Select(x => x.Id).ToArray();
            var existing = await context.P05_PageComponentMappings
                .Where(x => desiredIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);
            var added = 0;
            var updated = 0;

            foreach (var mapping in desired)
            {
                if (!existing.TryGetValue(mapping.Id, out var current))
                {
                    await context.P05_PageComponentMappings.AddAsync(mapping);
                    added++;
                    continue;
                }

                if (current.P01_PageId == mapping.P01_PageId
                    && current.P03_ComponentId == mapping.P03_ComponentId)
                {
                    continue;
                }

                current.P01_PageId = mapping.P01_PageId;
                current.P03_ComponentId = mapping.P03_ComponentId;
                updated++;
            }

            if (added == 0 && updated == 0) return;
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P05_PageComponentMapping reconciled: {Added} added, {Updated} updated", added, updated);
        }

        private static P05_PageComponentMapping P5(Guid id, Guid pageId, Guid componentId)
            => new() { Id = id, P01_PageId = pageId, P03_ComponentId = componentId };

        // ════════════════════════════════════════════════════════════
        //  P06_GroupPageComponentMapping
        //  Exact action and UI grants for the four canonical personas
        // ════════════════════════════════════════════════════════════
        private static async Task SeedP06_GroupPageComponentMapping(VPPMigrationDbContext context)
        {
            var now = DateTime.Now;
            var mappingIdByCode = GetP05SeedMappings().ToDictionary(
                x => x.ComponentCode,
                x => x.MappingId,
                StringComparer.OrdinalIgnoreCase);
            var groupIds = CanonicalRbac.Personas.Select(x => x.GroupId).ToArray();
            var existing = await context.P06_GroupPageComponentMappings
                .Where(x => groupIds.Contains(x.P02_GroupId)
                    && x.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode)
                .ToListAsync();
            var existingByKey = existing.ToDictionary(
                x => (x.P02_GroupId, x.P05_PageComponentMappingId));
            var targetKeys = new HashSet<(Guid GroupId, Guid MappingId)>();
            var added = 0;
            var updated = 0;

            foreach (var persona in CanonicalRbac.Personas)
            {
                foreach (var componentCode in CanonicalRbac.GetAllSeedComponents(persona.GroupId))
                {
                    if (!mappingIdByCode.TryGetValue(componentCode, out var mappingId))
                        throw new InvalidOperationException($"Cannot grant '{componentCode}' to '{persona.GroupCode}': P05 mapping is missing.");

                    var key = (persona.GroupId, mappingId);
                    targetKeys.Add(key);
                    if (!existingByKey.TryGetValue(key, out var mapping))
                    {
                        await context.P06_GroupPageComponentMappings.AddAsync(P6(mappingId, persona.GroupId, now));
                        added++;
                        continue;
                    }

                    if (mapping.IsEnable && mapping.IsVisible)
                        continue;

                    mapping.IsEnable = true;
                    mapping.IsVisible = true;
                    mapping.UpdateUserId = DefaultUserId;
                    mapping.UpdateDate = now;
                    updated++;
                }
            }

            foreach (var obsolete in existing.Where(x => !targetKeys.Contains((x.P02_GroupId, x.P05_PageComponentMappingId))))
            {
                if (!obsolete.IsEnable && !obsolete.IsVisible)
                    continue;

                obsolete.IsEnable = false;
                obsolete.IsVisible = false;
                obsolete.UpdateUserId = DefaultUserId;
                obsolete.UpdateDate = now;
                updated++;
            }

            if (added == 0 && updated == 0) return;
            await context.SaveChangesAsync();
            Log.Information("[SeedData] P06_GroupPageComponentMapping reconciled: {Added} added, {Updated} updated", added, updated);
        }

        private static P06_GroupPageComponentMapping P6(Guid p05Id, Guid groupId, DateTime now)
            => new() { P05_PageComponentMappingId = p05Id, P02_GroupId = groupId,
                       MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode, IsEnable = true, IsVisible = true,
                       CreateUserId = DefaultUserId, CreateDate = now,
                       UpdateUserId = DefaultUserId, UpdateDate = now };

        private static IReadOnlyList<P05SeedMapping> GetP05SeedMappings()
        {
            var mappings = new List<P05SeedMapping>
            {
                new(P05_SB_Dashboard, "SIDEBAR", Permissions.MenuDashboard),
                new(P05_SB_Library, "SIDEBAR", Permissions.MenuLibrary),
                new(P05_SB_Report, "SIDEBAR", Permissions.MenuReport),
                new(P05_SB_Permission, "SIDEBAR", Permissions.MenuPermission),
                new(P05_DB_Order, "DASHBOARD", Permissions.RequestOrder),
                new(P05_DB_Catalog, "DASHBOARD", Permissions.RequestProductCatalog),
                new(P05_DB_History, "DASHBOARD", Permissions.RequestHistory),
                new(P05_DB_DeptSum, "DASHBOARD", Permissions.RequestDepartmentSummary),
                new(P05_DB_AllSum, "DASHBOARD", Permissions.RequestAllOrdersSummary),
                new(P05_DB_Approval, "DASHBOARD", Permissions.RequestAdminApproval),
                new(P05_LB_Class, "LIBRARY", Permissions.LibraryClass),
                new(P05_LB_Category, "LIBRARY", Permissions.LibraryCategory),
                new(P05_LB_Item, "LIBRARY", Permissions.LibraryItem),
                new(P05_LB_Supplier, "LIBRARY", Permissions.LibrarySupplier),
                new(P05_LB_Price, "LIBRARY", Permissions.LibraryPrice),
                new(P05_LB_PriceList, "LIBRARY", Permissions.LibraryPriceList),
                new(P05_LB_Dept, "LIBRARY", Permissions.LibraryDepartment),
                new(P05_AP_PeriodSettle, "DASHBOARD", Permissions.PeriodSettle),
                new(P05_PM_User, "PERMISSION", Permissions.PermissionUser),
                new(P05_PM_Component, "PERMISSION", Permissions.PermissionComponent),
                new(P05_RP_View, "REPORT", Permissions.ReportView)
            };

            mappings.AddRange(CanonicalRbac.Actions
                .Where(x => x.PermissionCode != Permissions.PeriodSettle)
                .Select(x => new P05SeedMapping(
                    CanonicalRbacSeedIds.ActionPageMapping(x.PermissionCode),
                    x.PageCode,
                    x.PermissionCode)));
            return mappings;
        }

        private sealed record P05SeedMapping(Guid MappingId, string PageCode, string ComponentCode);
    }
}
