using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Options for the explicit TEST/DEMO workbook fixture. When no owner is
/// supplied, only catalog, department and price reference data are seeded.
/// </summary>
public sealed record DemoWorkbookSeedOptions(
    string? OwnerUsername = null,
    DateTime? NowUtc = null);

/// <summary>
/// Reconciles the normalized, non-sensitive projection of the PPJ VPP
/// workbook. The runtime never opens Excel; the source workbook is converted
/// to deterministic TSV files by scripts/data/normalize-vpp-demo-source.py.
/// </summary>
public static class DemoWorkbookSeeder
{
    private const int FallbackAuditUserId = 5615;
    private const int ExpectedCatalogItems = 547;
    private const int ExpectedDepartments = 52;
    private const int ExpectedUsers = 52;
    private const int ExpectedOrderLines = 2828;
    private const int LatestSourceMonth = 3;
    private const string DemoEmailSuffix = "@demo.gtas.local";
    private const string DemoDescription = "Normalized PPJ workbook demo fixture";

    private static readonly Guid DefaultPriceListId =
        Guid.Parse("00000000-0000-0000-0000-000000000700");

    public static async Task SeedAsync(
        VPPMigrationDbContext context,
        DemoWorkbookSeedOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        options ??= new DemoWorkbookSeedOptions();

        var catalogRows = ReadCatalogRows();
        var departmentRows = ReadDepartmentRows();
        var userRows = ReadUserRows();
        var orderRows = ReadOrderRows();
        ValidateDataset(catalogRows, departmentRows, userRows, orderRows);

        var owner = await ResolveOwnerAsync(context, options.OwnerUsername, cancellationToken);
        var actorUserId = owner?.User.Id ?? FallbackAuditUserId;
        var nowUtc = NormalizeUtc(options.NowUtc ?? DateTime.UtcNow);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var itemByCode = await ReconcileCatalogAndPricesAsync(
            context,
            catalogRows,
            actorUserId,
            nowUtc,
            cancellationToken);
        var departmentByCode = await ReconcileDepartmentsAsync(
            context,
            departmentRows,
            actorUserId,
            nowUtc,
            cancellationToken);

        if (owner is not null)
        {
            await ReconcileOperationalDataAsync(
                context,
                owner,
                departmentByCode,
                itemByCode,
                userRows,
                orderRows,
                nowUtc,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        Log.Information(
            "[DemoWorkbookSeeder] Reconciled {CatalogCount} items, {DepartmentCount} departments and {OrderCount} order lines. Operational owner: {OwnerUsername}.",
            catalogRows.Count,
            departmentRows.Count,
            owner is null ? 0 : orderRows.Count,
            owner?.User.UserName ?? "reference-only");
    }

    public static Period MapSourceMonthToPeriod(
        int sourceMonth,
        int latestSourceMonth,
        Period currentPeriod)
    {
        if (sourceMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(sourceMonth));
        if (latestSourceMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(latestSourceMonth));

        var offset = (latestSourceMonth - sourceMonth + 12) % 12;
        var anchor = new DateTime(currentPeriod.Year, currentPeriod.Month, 1).AddMonths(-offset);
        return new Period(anchor.Year, anchor.Month);
    }

    private static async Task<IReadOnlyDictionary<string, VppItem>> ReconcileCatalogAndPricesAsync(
        VPPMigrationDbContext context,
        IReadOnlyList<DemoCatalogRow> rows,
        int actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var categories = await context.VppCategories
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);
        var categoryByCode = new Dictionary<string, VppCategory>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in rows.GroupBy(x => new { x.CategoryCode, x.CategoryName }))
        {
            var category = categories.FirstOrDefault(x =>
                    string.Equals(x.VppCategoryCode, group.Key.CategoryCode, StringComparison.OrdinalIgnoreCase))
                ?? categories.FirstOrDefault(x =>
                    NormalizeKey(x.VppCategoryName) == NormalizeKey(group.Key.CategoryName));

            if (category is null)
            {
                category = new VppCategory
                {
                    Id = StableGuid($"demo-category|{group.Key.CategoryCode}"),
                    CreatedByUserId = actorUserId,
                    CreatedAtUtc = nowUtc
                };
                context.VppCategories.Add(category);
                categories.Add(category);
            }

            category.VppCategoryCode = group.Key.CategoryCode;
            category.VppCategoryName = group.Key.CategoryName;
            category.Description = DemoDescription;
            category.UpdatedByUserId = actorUserId;
            category.UpdatedAtUtc = nowUtc;
            category.IsDeleted = false;
            categoryByCode[group.Key.CategoryCode] = category;
        }

        var uomCategory = await context.LookupCategories
            .FirstOrDefaultAsync(x => x.Code == "Uom" && !x.IsDeleted, cancellationToken);
        if (uomCategory is null)
        {
            uomCategory = new LookupCategory
            {
                Id = StableGuid("demo-lookup-category|Uom"),
                Code = "Uom",
                Name = "Đơn vị tính",
                ModuleName = "VPP",
                Description = DemoDescription,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = nowUtc,
                UpdatedByUserId = actorUserId,
                UpdatedAtUtc = nowUtc,
                IsDeleted = false
            };
            context.LookupCategories.Add(uomCategory);
        }

        var uoms = await context.LookupValues
            .Where(x => x.LookupCategoryId == uomCategory.Id && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        var uomByCode = new Dictionary<string, LookupValue>(StringComparer.OrdinalIgnoreCase);
        var sort = 0;
        foreach (var group in rows.GroupBy(x => new { x.UomCode, x.UomName }).OrderBy(x => x.Key.UomName))
        {
            sort++;
            var uom = uoms.FirstOrDefault(x =>
                    string.Equals(x.Code, group.Key.UomCode, StringComparison.OrdinalIgnoreCase))
                ?? uoms.FirstOrDefault(x => NormalizeKey(x.Value) == NormalizeKey(group.Key.UomName));

            if (uom is null)
            {
                uom = new LookupValue
                {
                    Id = StableGuid($"demo-uom|{group.Key.UomCode}"),
                    Code = group.Key.UomCode,
                    Value = group.Key.UomName,
                    CreatedByUserId = actorUserId,
                    CreatedAtUtc = nowUtc
                };
                context.LookupValues.Add(uom);
                uoms.Add(uom);
            }

            uom.LookupCategoryId = uomCategory.Id;
            uom.Code = group.Key.UomCode;
            uom.Value = group.Key.UomName;
            uom.Sort = sort;
            uom.Description = DemoDescription;
            uom.UpdatedByUserId = actorUserId;
            uom.UpdatedAtUtc = nowUtc;
            uom.IsDeleted = false;
            uomByCode[group.Key.UomCode] = uom;
        }

        await context.SaveChangesAsync(cancellationToken);

        var items = await context.VppItems
            .Include(x => x.VppCategory)
            .Include(x => x.Uom)
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);
        var itemByCode = new Dictionary<string, VppItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var targetCategory = categoryByCode[row.CategoryCode];
            var targetUom = uomByCode[row.UomCode];
            var nameMatches = items
                .Where(x => NormalizeKey(x.VppName) == NormalizeKey(row.ItemName))
                .ToList();
            var item = items.FirstOrDefault(x =>
                    string.Equals(x.VppCode, row.ItemCode, StringComparison.OrdinalIgnoreCase))
                ?? nameMatches.FirstOrDefault(x =>
                    NormalizeKey(x.VppCategory?.VppCategoryName) == NormalizeKey(row.CategoryName)
                    && NormalizeKey(x.Uom?.Value) == NormalizeKey(row.UomName))
                ?? (nameMatches.Count == 1 ? nameMatches[0] : null);

            if (item is null)
            {
                item = new VppItem
                {
                    Id = StableGuid($"demo-item|{row.ItemCode}"),
                    CreatedByUserId = actorUserId,
                    CreatedAtUtc = nowUtc
                };
                context.VppItems.Add(item);
                items.Add(item);
            }

            item.VppCode = row.ItemCode;
            item.VppName = row.ItemName;
            item.VppCategoryId = targetCategory.Id;
            item.UomId = targetUom.Id;
            item.Description = $"{DemoDescription}; resolution={row.Resolution}";
            item.UpdatedByUserId = actorUserId;
            item.UpdatedAtUtc = nowUtc;
            item.IsDeleted = false;
            itemByCode[row.ItemCode] = item;
        }

        await context.SaveChangesAsync(cancellationToken);
        await ReconcileDefaultPricesAsync(
            context,
            rows,
            itemByCode,
            actorUserId,
            nowUtc,
            cancellationToken);
        return itemByCode;
    }

    private static async Task ReconcileDefaultPricesAsync(
        VPPMigrationDbContext context,
        IReadOnlyList<DemoCatalogRow> rows,
        IReadOnlyDictionary<string, VppItem> itemByCode,
        int actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers.FirstOrDefaultAsync(
            x => x.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName && !x.IsDeleted,
            cancellationToken);
        if (supplier is null)
        {
            supplier = new Supplier
            {
                Id = StableGuid("demo-supplier|VPP_HCM"),
                SupplierShortName = VppPricingDefaults.DefaultSupplierShortName,
                SupplierName = "VPP Gia Định",
                City = "Hồ Chí Minh",
                Description = DemoDescription,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = nowUtc,
                UpdatedByUserId = actorUserId,
                UpdatedAtUtc = nowUtc,
                IsDeleted = false
            };
            context.Suppliers.Add(supplier);
        }

        var priceList = await context.PriceLists
            .FirstOrDefaultAsync(x => x.Id == DefaultPriceListId && !x.IsDeleted, cancellationToken);
        if (priceList is null)
        {
            priceList = new PriceList
            {
                Id = DefaultPriceListId,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = nowUtc
            };
            context.PriceLists.Add(priceList);
        }

        priceList.PriceListCode = "DEFAULT";
        priceList.PriceListName = "Bảng giá mặc định từ dữ liệu đăng ký VPP";
        priceList.IsDefault = true;
        priceList.SupplierId = supplier.Id;
        priceList.Version = 1;
        priceList.EffectiveFromUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        priceList.Status = PriceListStatus.Published;
        priceList.CurrencyCode = "VND";
        priceList.VatPolicy = "item-rate";
        priceList.PublishedAtUtc ??= nowUtc;
        priceList.PublishedByUserId ??= actorUserId;
        priceList.Description = DemoDescription;
        priceList.UpdatedByUserId = actorUserId;
        priceList.UpdatedAtUtc = nowUtc;
        priceList.IsDeleted = false;

        await context.SaveChangesAsync(cancellationToken);

        var itemIds = itemByCode.Values.Select(x => x.Id).ToArray();
        var mappings = await context.SupplierProductMappings
            .Where(x => x.PriceListId == priceList.Id && itemIds.Contains(x.VppItemId) && !x.IsDeleted)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            var item = itemByCode[row.ItemCode];
            var candidates = mappings.Where(x => x.VppItemId == item.Id).ToList();
            var mapping = candidates.FirstOrDefault(x => x.SupplierId == supplier.Id) ?? candidates.FirstOrDefault();
            if (mapping is null)
            {
                mapping = new SupplierProductMapping
                {
                    Id = StableGuid($"demo-price|{row.ItemCode}"),
                    VppItemId = item.Id,
                    CreatedByUserId = actorUserId,
                    CreatedAtUtc = nowUtc
                };
                context.SupplierProductMappings.Add(mapping);
                mappings.Add(mapping);
            }

            foreach (var duplicate in candidates.Where(x => x.Id != mapping.Id))
            {
                duplicate.IsDefault = false;
                duplicate.IsDeleted = true;
                duplicate.UpdatedByUserId = actorUserId;
                duplicate.UpdatedAtUtc = nowUtc;
            }

            mapping.SupplierId = supplier.Id;
            mapping.PriceListId = priceList.Id;
            mapping.Price = row.UnitPrice;
            mapping.NetPrice = row.UnitPrice;
            mapping.VatRate = row.VatPercent;
            mapping.MinimumOrderQuantity = 1m;
            mapping.LeadTimeDays = 2;
            mapping.SupplierSku = row.ItemCode;
            mapping.IsDefault = true;
            mapping.Description = DemoDescription;
            mapping.UpdatedByUserId = actorUserId;
            mapping.UpdatedAtUtc = nowUtc;
            mapping.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, Department>> ReconcileDepartmentsAsync(
        VPPMigrationDbContext context,
        IReadOnlyList<DemoDepartmentRow> rows,
        int actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var departments = await context.Departments
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);
        var result = new Dictionary<string, Department>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var acceptedCodes = row.LegacyCodes
                .Append(row.DepartmentCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var department = departments.FirstOrDefault(x =>
                    string.Equals(x.Code, row.DepartmentCode, StringComparison.OrdinalIgnoreCase))
                ?? departments.FirstOrDefault(x => x.Code is not null && acceptedCodes.Contains(x.Code))
                ?? departments.FirstOrDefault(x => NormalizeKey(x.Name) == NormalizeKey(row.DepartmentName));

            if (department is null)
            {
                department = new Department
                {
                    Id = StableGuid($"demo-department|{row.DepartmentCode}"),
                    CreatedByUserId = actorUserId,
                    CreatedAtUtc = nowUtc
                };
                context.Departments.Add(department);
                departments.Add(department);
            }

            department.Code = row.DepartmentCode;
            department.Name = row.DepartmentName;
            department.Description = $"{DemoDescription}; sheets={string.Join(',', row.SourceSheets)}";
            department.UpdatedByUserId = actorUserId;
            department.UpdatedAtUtc = nowUtc;
            department.IsDeleted = false;
            result[row.DepartmentCode] = department;
        }

        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task ReconcileOperationalDataAsync(
        VPPMigrationDbContext context,
        DemoOwner owner,
        IReadOnlyDictionary<string, Department> departmentByCode,
        IReadOnlyDictionary<string, VppItem> itemByCode,
        IReadOnlyList<DemoUserRow> userRows,
        IReadOnlyList<DemoOrderRow> orderRows,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (!departmentByCode.ContainsKey(owner.DepartmentCode))
        {
            throw new InvalidOperationException(
                $"Demo owner department '{owner.DepartmentCode}' is not present in demo-departments.tsv.");
        }

        var userByDepartment = await ReconcileDemoUsersAsync(
            context,
            owner,
            departmentByCode,
            userRows,
            nowUtc,
            cancellationToken);

        var calculator = new PeriodCalculator();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, PeriodCalculator.BusinessTimeZone);
        var currentPeriod = calculator.Current(localNow);
        var periodBySourceMonth = new Dictionary<int, VppPeriod>();

        foreach (var sourceMonth in orderRows.Select(x => x.SourceMonth).Distinct().Order())
        {
            var mapped = MapSourceMonthToPeriod(sourceMonth, LatestSourceMonth, currentPeriod);
            periodBySourceMonth[sourceMonth] = await ReconcilePeriodAsync(
                context,
                mapped,
                mapped == currentPeriod,
                calculator,
                owner.User.Id,
                nowUtc,
                cancellationToken);
        }

        var existingDemoRequests = await context.Requests
            .Include(x => x.RequestDetails)
            .Where(x => x.VppCode != null && x.VppCode.StartsWith("DEMO-PPJ-") && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        var existingDemoByCode = existingDemoRequests.ToDictionary(
            x => x.VppCode!,
            StringComparer.OrdinalIgnoreCase);

        var occupiedRequests = await context.Requests
            .Where(x => !x.IsDeleted && x.IsCurrentRevision && !x.IsAdditionalOrder)
            .Select(x => new { x.Id, x.CreatedByUserId, x.PeriodId, x.VppCode })
            .ToListAsync(cancellationToken);

        var requestCount = 0;
        var detailCount = 0;
        foreach (var departmentMonth in orderRows
                     .GroupBy(x => new { x.DepartmentCode, x.SourceMonth })
                     .OrderBy(x => x.Key.SourceMonth)
                     .ThenBy(x => x.Key.DepartmentCode))
        {
            if (!userByDepartment.TryGetValue(departmentMonth.Key.DepartmentCode, out var requester))
            {
                throw new InvalidDataException(
                    $"No demo requester is bound to department '{departmentMonth.Key.DepartmentCode}'.");
            }

            var period = periodBySourceMonth[departmentMonth.Key.SourceMonth];
            var requestCode = BuildRequestCode(period.Year, period.Month, departmentMonth.Key.DepartmentCode);
            existingDemoByCode.TryGetValue(requestCode, out var request);

            var occupied = occupiedRequests.FirstOrDefault(x =>
                x.CreatedByUserId == requester.Id
                && x.PeriodId == period.Id
                && x.Id != request?.Id);
            if (occupied is not null)
            {
                Log.Warning(
                    "[DemoWorkbookSeeder] Skipped {RequestCode}; user {Username} already has request {ExistingCode} in the mapped period.",
                    requestCode,
                    requester.UserName,
                    occupied.VppCode ?? occupied.Id.ToString());
                continue;
            }

            var isCurrent = period.Year == currentPeriod.Year && period.Month == currentPeriod.Month;
            var submittedAtUtc = period.StartAtUtc.AddDays(2).AddHours(2);
            if (submittedAtUtc > nowUtc)
                submittedAtUtc = nowUtc.AddMinutes(-5);

            if (request is null)
            {
                var requestId = StableGuid($"demo-request|{requestCode}");
                request = new VppRequest
                {
                    Id = requestId,
                    RequestSeriesId = StableGuid($"demo-request-series|{requestCode}"),
                    RevisionNumber = 1,
                    IsCurrentRevision = true,
                    IsAdditionalOrder = false,
                    CreatedByUserId = requester.Id,
                    CreatedAtUtc = submittedAtUtc
                };
                context.Requests.Add(request);
                existingDemoByCode[requestCode] = request;
                occupiedRequests.Add(new
                {
                    request.Id,
                    request.CreatedByUserId,
                    request.PeriodId,
                    request.VppCode
                });
            }

            request.VppCode = requestCode;
            request.Year = period.Year;
            request.Month = period.Month;
            request.PeriodId = period.Id;
            request.Status = (int)(isCurrent ? VPPStatus.Submitted : VPPStatus.Approved);
            request.DepartmentCode = departmentMonth.Key.DepartmentCode;
            request.MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode.ToString(CultureInfo.InvariantCulture);
            request.SubmittedDate = submittedAtUtc;
            request.ApprovedById = isCurrent ? null : owner.User.Id;
            request.ApprovedAt = isCurrent ? null : submittedAtUtc.AddDays(1);
            request.IdempotencyKey = $"demo-ppj-{period.Year:D4}{period.Month:D2}-{departmentMonth.Key.DepartmentCode}";
            request.CommandPayloadHash = Sha256Hex(request.IdempotencyKey);
            request.Description = DemoDescription;
            request.UpdatedByUserId = owner.User.Id;
            request.UpdatedAtUtc = nowUtc;
            request.IsDeleted = false;

            var currentItemCodes = departmentMonth.Select(x => x.ItemCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var obsolete in request.RequestDetails.Where(x =>
                         !x.IsDeleted
                         && itemByCode.Values.FirstOrDefault(item => item.Id == x.VppId)?.VppCode is string code
                         && !currentItemCodes.Contains(code)))
            {
                obsolete.IsDeleted = true;
                obsolete.UpdatedByUserId = owner.User.Id;
                obsolete.UpdatedAtUtc = nowUtc;
            }

            foreach (var row in departmentMonth)
            {
                if (!itemByCode.TryGetValue(row.ItemCode, out var item))
                    throw new InvalidDataException($"Order item '{row.ItemCode}' is missing from demo-catalog.tsv.");

                var detail = request.RequestDetails.FirstOrDefault(x => x.VppId == item.Id && !x.IsDeleted);
                if (detail is null)
                {
                    detail = new VppRequestDetail
                    {
                        Id = StableGuid($"demo-detail|{requestCode}|{row.ItemCode}"),
                        RequestId = request.Id,
                        VppId = item.Id,
                        CreatedByUserId = requester.Id,
                        CreatedAtUtc = submittedAtUtc
                    };
                    request.RequestDetails.Add(detail);
                }

                detail.Qty = row.Quantity;
                detail.CurrentSinglePrice = row.UnitPrice;
                // Workbook provenance belongs in the demo audit pipeline, not in the user's business note.
                detail.Description = null;
                detail.UpdatedByUserId = owner.User.Id;
                detail.UpdatedAtUtc = nowUtc;
                detail.IsDeleted = false;
                detailCount++;
            }

            await ReconcileRequestLogsAsync(
                context,
                request,
                owner.User.Id,
                submittedAtUtc,
                isCurrent,
                cancellationToken);
            requestCount++;
        }

        await context.SaveChangesAsync(cancellationToken);
        Log.Information(
            "[DemoWorkbookSeeder] Operational fixture reconciled: {RequestCount} requests and {DetailCount} details for owner {OwnerUsername}.",
            requestCount,
            detailCount,
            owner.User.UserName);
    }

    private static async Task<IReadOnlyDictionary<string, AppUser>> ReconcileDemoUsersAsync(
        VPPMigrationDbContext context,
        DemoOwner owner,
        IReadOnlyDictionary<string, Department> departmentByCode,
        IReadOnlyList<DemoUserRow> rows,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var users = await context.Users.ToListAsync(cancellationToken);
        var result = new Dictionary<string, AppUser>(StringComparer.OrdinalIgnoreCase)
        {
            [owner.DepartmentCode] = owner.User
        };

        foreach (var row in rows.Where(x =>
                     !string.Equals(x.DepartmentCode, owner.DepartmentCode, StringComparison.OrdinalIgnoreCase)))
        {
            var normalizedUsername = row.Username.ToUpperInvariant();
            var user = users.FirstOrDefault(x => x.NormalizedUserName == normalizedUsername);
            if (user is not null && !IsOwnedDemoUser(user))
            {
                throw new InvalidOperationException(
                    $"Demo username '{row.Username}' is already owned by a non-demo account.");
            }

            if (user is null)
            {
                user = new AppUser
                {
                    UserName = row.Username,
                    NormalizedUserName = normalizedUsername,
                    Email = row.Email,
                    NormalizedEmail = row.Email.ToUpperInvariant(),
                    FullName = row.FullName,
                    EmployeeCode = row.EmployeeCode,
                    MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
                    AccountStatus = AppAccountStatus.Disabled,
                    MustChangePassword = false,
                    SessionVersion = 1,
                    EmailConfirmed = false,
                    PasswordHash = null,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                    LockoutEnabled = true,
                    LockoutEnd = DateTimeOffset.MaxValue,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc,
                    DisabledAtUtc = nowUtc
                };
                context.Users.Add(user);
                users.Add(user);
            }
            else
            {
                user.UserName = row.Username;
                user.NormalizedUserName = normalizedUsername;
                user.Email = row.Email;
                user.NormalizedEmail = row.Email.ToUpperInvariant();
                user.FullName = row.FullName;
                user.EmployeeCode = row.EmployeeCode;
                user.MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode;
                user.AccountStatus = AppAccountStatus.Disabled;
                user.PasswordHash = null;
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                user.UpdatedAtUtc = nowUtc;
                user.DisabledAtUtc ??= nowUtc;
            }

            result[row.DepartmentCode] = user;
        }

        await context.SaveChangesAsync(cancellationToken);

        var memberships = await context.UserGroupMemberships
            .Where(x => x.AccountId != null && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var pair in result.Where(x => x.Value.Id != owner.User.Id))
        {
            var user = pair.Value;
            var department = departmentByCode[pair.Key];
            var membership = memberships.FirstOrDefault(x => x.AccountId == user.Id);
            if (membership is null)
            {
                membership = new UserGroupMembership
                {
                    Id = StableGuid($"demo-membership|{user.NormalizedUserName}"),
                    UserId = user.Id,
                    AccountId = user.Id,
                    PermissionGroupId = CanonicalRbac.Employee.GroupId,
                    DepartmentId = department.Id,
                    Description = DemoDescription,
                    CreatedByUserId = owner.User.Id,
                    CreatedAtUtc = nowUtc,
                    UpdatedByUserId = owner.User.Id,
                    UpdatedAtUtc = nowUtc,
                    IsDeleted = false
                };
                context.UserGroupMemberships.Add(membership);
                memberships.Add(membership);
            }
            else
            {
                membership.UserId = user.Id;
                membership.AccountId = user.Id;
                membership.PermissionGroupId = CanonicalRbac.Employee.GroupId;
                membership.DepartmentId = department.Id;
                membership.Description = DemoDescription;
                membership.UpdatedByUserId = owner.User.Id;
                membership.UpdatedAtUtc = nowUtc;
                membership.IsDeleted = false;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task<VppPeriod> ReconcilePeriodAsync(
        VPPMigrationDbContext context,
        Period periodValue,
        bool isCurrent,
        PeriodCalculator calculator,
        int actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var memberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode.ToString(CultureInfo.InvariantCulture);
        var period = await context.Periods.FirstOrDefaultAsync(x =>
                x.MemberCompanyCode == memberCompanyCode
                && x.Year == periodValue.Year
                && x.Month == periodValue.Month
                && !x.IsDeleted,
            cancellationToken);

        if (period is null)
        {
            period = new VppPeriod
            {
                Id = StableGuid($"demo-period|{memberCompanyCode}|{periodValue}"),
                MemberCompanyCode = memberCompanyCode,
                Year = periodValue.Year,
                Month = periodValue.Month,
                State = isCurrent ? VppPeriodState.Open : VppPeriodState.SubmissionClosed,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = nowUtc
            };
            context.Periods.Add(period);
        }

        period.TimeZoneId = "Asia/Ho_Chi_Minh";
        period.StartAtUtc = calculator.StartAtUtc(periodValue);
        period.SubmissionDeadlineUtc = calculator.SubmissionDeadlineUtc(periodValue);
        period.SupplementApprovalDeadlineUtc = calculator.SupplementApprovalDeadlineUtc(
            periodValue,
            TimeSpan.FromDays(VppRequestPolicy.DefaultSupplementApprovalGraceDays));
        period.Description = DemoDescription;
        period.UpdatedByUserId = actorUserId;
        period.UpdatedAtUtc = nowUtc;
        period.IsDeleted = false;

        await context.SaveChangesAsync(cancellationToken);
        return period;
    }

    private static async Task ReconcileRequestLogsAsync(
        VPPMigrationDbContext context,
        VppRequest request,
        int approverUserId,
        DateTime submittedAtUtc,
        bool isCurrent,
        CancellationToken cancellationToken)
    {
        var existing = await context.RequestLogs
            .Where(x => x.RequestId == request.Id)
            .ToListAsync(cancellationToken);
        UpsertLog("SUBMITTED", "Đã gửi đơn văn phòng phẩm", request.CreatedByUserId, submittedAtUtc);
        if (!isCurrent)
        {
            UpsertLog("APPROVED", "Đơn đã được duyệt", approverUserId, submittedAtUtc.AddDays(1));
        }

        void UpsertLog(string action, string title, int actorUserId, DateTime occurredAtUtc)
        {
            var id = StableGuid($"demo-request-log|{request.Id}|{action}");
            var log = existing.FirstOrDefault(x => x.Id == id);
            if (log is null)
            {
                log = new RequestLog { Id = id, RequestId = request.Id };
                context.RequestLogs.Add(log);
                existing.Add(log);
            }

            log.LogDate = occurredAtUtc;
            log.LogTitle = title;
            log.ActorUserId = actorUserId;
            log.MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode.ToString(CultureInfo.InvariantCulture);
            log.Action = action;
            log.RevisionNumber = 1;
            log.CorrelationId = $"demo-{request.Id:N}";
            log.Reason = "Dữ liệu demo đã được chuẩn hoá từ bảng đăng ký VPP.";
            log.LogJS = "{\"source\":\"normalized-workbook-demo\"}";
        }
    }

    private static async Task<DemoOwner?> ResolveOwnerAsync(
        VPPMigrationDbContext context,
        string? ownerUsername,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ownerUsername))
            return null;

        var normalizedUsername = ownerUsername.Trim().ToUpperInvariant();
        var owner = await context.Users.FirstOrDefaultAsync(
            x => x.NormalizedUserName == normalizedUsername,
            cancellationToken);
        if (owner is null)
            throw new InvalidOperationException($"Demo owner account '{ownerUsername}' does not exist.");
        if (owner.AccountStatus != AppAccountStatus.Active)
            throw new InvalidOperationException($"Demo owner account '{ownerUsername}' is not active.");

        var membership = await context.UserGroupMemberships
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.AccountId == owner.Id && !x.IsDeleted, cancellationToken);
        var departmentCode = membership?.Department?.Code?.Trim();
        if (string.IsNullOrWhiteSpace(departmentCode))
        {
            throw new InvalidOperationException(
                $"Demo owner account '{ownerUsername}' does not have an active primary department.");
        }

        return new DemoOwner(owner, departmentCode);
    }

    private static void ValidateDataset(
        IReadOnlyList<DemoCatalogRow> catalogRows,
        IReadOnlyList<DemoDepartmentRow> departmentRows,
        IReadOnlyList<DemoUserRow> userRows,
        IReadOnlyList<DemoOrderRow> orderRows)
    {
        RequireCount("catalog item", catalogRows.Count, ExpectedCatalogItems);
        RequireCount("department", departmentRows.Count, ExpectedDepartments);
        RequireCount("demo user", userRows.Count, ExpectedUsers);
        RequireCount("order line", orderRows.Count, ExpectedOrderLines);

        RequireUnique(catalogRows.Select(x => x.ItemCode), "catalog item code");
        RequireUnique(departmentRows.Select(x => x.DepartmentCode), "department code");
        RequireUnique(userRows.Select(x => x.Username), "demo username");
        RequireUnique(userRows.Select(x => x.Email), "demo e-mail");
        RequireUnique(userRows.Select(x => x.EmployeeCode), "demo employee code");

        var itemCodes = catalogRows.Select(x => x.ItemCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var departmentCodes = departmentRows.Select(x => x.DepartmentCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var userDepartmentCodes = userRows.Select(x => x.DepartmentCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingItems = orderRows.Select(x => x.ItemCode)
            .Where(x => !itemCodes.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missingItems.Length > 0)
            throw new InvalidDataException($"Demo orders reference missing items: {string.Join(", ", missingItems)}");

        var missingDepartments = orderRows.Select(x => x.DepartmentCode)
            .Where(x => !departmentCodes.Contains(x) || !userDepartmentCodes.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missingDepartments.Length > 0)
        {
            throw new InvalidDataException(
                $"Demo orders reference departments without metadata/users: {string.Join(", ", missingDepartments)}");
        }

        if (catalogRows.Any(x => x.UnitPrice <= 0 || x.VatPercent is < 0 or > 100))
            throw new InvalidDataException("Demo catalog contains an invalid price or VAT percent.");
        if (orderRows.Any(x => x.Quantity <= 0 || x.UnitPrice <= 0 || x.SourceMonth is < 1 or > 12))
            throw new InvalidDataException("Demo orders contain an invalid month, quantity or price.");
    }

    private static void RequireCount(string entityName, int actual, int expected)
    {
        if (actual != expected)
        {
            throw new InvalidDataException(
                $"Demo {entityName} count drifted: expected {expected}, found {actual}. Regenerate and review the source audit.");
        }
    }

    private static void RequireUnique(IEnumerable<string> values, string label)
    {
        var duplicate = values
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new InvalidDataException($"Duplicate {label} '{duplicate.Key}' in the normalized demo dataset.");
    }

    private static IReadOnlyList<DemoCatalogRow> ReadCatalogRows() =>
        ReadTsv("demo-catalog.tsv", columns => new DemoCatalogRow(
            columns["CategoryCode"],
            columns["CategoryName"],
            columns["ItemCode"],
            columns["ItemName"],
            columns["UomCode"],
            columns["UomName"],
            ParseDecimal(columns, "VatPercent"),
            ParseLong(columns, "UnitPrice"),
            columns["Resolution"]));

    private static IReadOnlyList<DemoDepartmentRow> ReadDepartmentRows() =>
        ReadTsv("demo-departments.tsv", columns => new DemoDepartmentRow(
            columns["DepartmentCode"],
            columns["DepartmentName"],
            SplitList(columns["LegacyCodes"]),
            SplitList(columns["SourceSheets"])));

    private static IReadOnlyList<DemoUserRow> ReadUserRows() =>
        ReadTsv("demo-users.tsv", columns => new DemoUserRow(
            columns["DepartmentCode"],
            columns["Username"],
            columns["FullName"],
            columns["Email"],
            columns["EmployeeCode"]));

    private static IReadOnlyList<DemoOrderRow> ReadOrderRows() =>
        ReadTsv("demo-orders.tsv", columns => new DemoOrderRow(
            columns["DepartmentCode"],
            ParseInt(columns, "SourceMonth"),
            columns["ItemCode"],
            ParseInt(columns, "Quantity"),
            ParseLong(columns, "UnitPrice"),
            ParseInt(columns, "SourceRowCount"),
            ParseBool(columns, "IsSynthesized")));

    private static IReadOnlyList<T> ReadTsv<T>(
        string fileName,
        Func<IReadOnlyDictionary<string, string>, T> projector)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Helpers", "Data", "Demo", fileName);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Required normalized demo dataset '{fileName}' was not copied to output.", filePath);

        using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidDataException($"Demo dataset '{fileName}' has no header.");

        var headers = headerLine.Split('\t');
        var result = new List<T>();
        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = line.Split('\t');
            if (values.Length != headers.Length)
            {
                throw new InvalidDataException(
                    $"Demo dataset '{fileName}' line {lineNumber} has {values.Length} columns; expected {headers.Length}.");
            }

            var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Length; index++)
                columns[headers[index]] = values[index];
            result.Add(projector(columns));
        }

        return result;
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> columns, string name) =>
        int.TryParse(columns[name], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidDataException($"'{columns[name]}' is not a valid integer for {name}.");

    private static long ParseLong(IReadOnlyDictionary<string, string> columns, string name) =>
        long.TryParse(columns[name], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidDataException($"'{columns[name]}' is not a valid long integer for {name}.");

    private static decimal ParseDecimal(IReadOnlyDictionary<string, string> columns, string name) =>
        decimal.TryParse(columns[name], NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidDataException($"'{columns[name]}' is not a valid decimal for {name}.");

    private static bool ParseBool(IReadOnlyDictionary<string, string> columns, string name) =>
        bool.TryParse(columns[name], out var value)
            ? value
            : throw new InvalidDataException($"'{columns[name]}' is not a valid boolean for {name}.");

    private static string[] SplitList(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string BuildRequestCode(int year, int month, string departmentCode)
    {
        var safeCode = new string(departmentCode
            .Where(character => char.IsLetterOrDigit(character) || character is '+' or '-')
            .ToArray())
            .ToUpperInvariant();
        return $"DEMO-PPJ-{year:D4}{month:D2}-{safeCode}";
    }

    private static bool IsOwnedDemoUser(AppUser user) =>
        user.Email?.EndsWith(DemoEmailSuffix, StringComparison.OrdinalIgnoreCase) == true;

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = false;
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSpace = false;
            }
            else if (!previousWasSpace && builder.Length > 0)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record DemoOwner(AppUser User, string DepartmentCode);

    private sealed record DemoCatalogRow(
        string CategoryCode,
        string CategoryName,
        string ItemCode,
        string ItemName,
        string UomCode,
        string UomName,
        decimal VatPercent,
        long UnitPrice,
        string Resolution);

    private sealed record DemoDepartmentRow(
        string DepartmentCode,
        string DepartmentName,
        IReadOnlyList<string> LegacyCodes,
        IReadOnlyList<string> SourceSheets);

    private sealed record DemoUserRow(
        string DepartmentCode,
        string Username,
        string FullName,
        string Email,
        string EmployeeCode);

    private sealed record DemoOrderRow(
        string DepartmentCode,
        int SourceMonth,
        string ItemCode,
        int Quantity,
        long UnitPrice,
        int SourceRowCount,
        bool IsSynthesized);
}
