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
/// Tùy chọn cho workbook fixture TEST/DEMO tường minh. Khi không truyền owner,
/// chỉ seed dữ liệu tham chiếu danh mục, phòng ban và giá.
/// </summary>
public sealed record DemoWorkbookSeedOptions(
    string? OwnerUsername = null,
    DateTime? NowUtc = null,
    bool AutoResolveOwner = false);

/// <summary>
/// Đối soát projection đã chuẩn hóa, không nhạy cảm của workbook PPJ VPP.
/// Runtime không mở Excel; workbook nguồn được chuyển thành file TSV xác định
/// bởi scripts/data/normalize-vpp-demo-source.py.
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
    private const string LegacyDemoDescription = "Normalized PPJ workbook demo fixture";
    private const string SupplementScenarioIdempotencyPrefix = "workbook-supplement-";
    private const string PersonaSupplementIdempotencyPrefix = "persona-demo-sup-";

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

        var owner = await ResolveOwnerAsync(context, options, cancellationToken);
        var actorUserId = owner?.User.Id ?? FallbackAuditUserId;
        var nowUtc = NormalizeUtc(options.NowUtc ?? DateTime.UtcNow);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var itemByCode = await ReconcileCatalogAndPricesAsync(
            context,
            catalogRows,
            orderRows,
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

        await SaveDemoChangesWithConcurrencyRetryAsync(context, cancellationToken);
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
        IReadOnlyList<DemoOrderRow> orderRows,
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
            category.Description = null;
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
                Description = null,
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
            uom.Description = null;
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
        var generatedLimits = orderRows
            .GroupBy(x => x.ItemCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => Math.Clamp(
                    group.Max(x => x.Quantity) * VppOrderQuantityLimits.DemoMultiplier,
                    VppOrderQuantityLimits.DemoFloor,
                    VppOrderQuantityLimits.Maximum),
                StringComparer.OrdinalIgnoreCase);

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
                    MaxQuantityPerOrder = generatedLimits.GetValueOrDefault(
                        row.ItemCode,
                        VppOrderQuantityLimits.Default),
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
            item.Description = null;
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
                Description = null,
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

        priceList.PriceListCode = VppPricingDefaults.DefaultPriceListCode;
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
        priceList.Description = null;
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
            mapping.IsDefault = true;
            mapping.Description = null;
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
            department.Description = null;
            department.UpdatedByUserId = actorUserId;
            department.UpdatedAtUtc = nowUtc;
            department.IsDeleted = false;
            result[row.DepartmentCode] = department;
        }

        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task ReconcileActiveInteractiveMembershipsAsync(
        VPPMigrationDbContext context,
        DemoOwner owner,
        IReadOnlyDictionary<string, Department> departmentByCode,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var activeInteractiveUsers = await context.Users
            .Where(user => user.AccountStatus == AppAccountStatus.Active)
            .ToListAsync(cancellationToken);
        activeInteractiveUsers = activeInteractiveUsers
            .Where(user => !IsOwnedDemoUser(user))
            .OrderBy(user => user.Id)
            .ToList();

        var memberships = await context.UserGroupMemberships
            .Where(membership => membership.AccountId != null)
            .ToListAsync(cancellationToken);
        var activeUserIds = activeInteractiveUsers.Select(user => user.Id).ToHashSet();
        var hasActiveManager = memberships.Any(membership =>
            !membership.IsDeleted
            && membership.AccountId.HasValue
            && activeUserIds.Contains(membership.AccountId.Value)
            && membership.PermissionGroupId == CanonicalRbac.Manager.GroupId);
        var ownerDepartment = departmentByCode[owner.DepartmentCode];

        foreach (var user in activeInteractiveUsers.Where(user =>
                     user.Id != owner.User.Id
                     && memberships.All(membership =>
                         membership.AccountId != user.Id || membership.IsDeleted)))
        {
            var membership = memberships.FirstOrDefault(item => item.AccountId == user.Id);
            if (membership is null)
            {
                membership = new UserGroupMembership
                {
                    Id = StableGuid($"workbook-interactive-membership|{user.Id}"),
                    UserId = user.Id,
                    AccountId = user.Id,
                    CreatedByUserId = owner.User.Id,
                    CreatedAtUtc = nowUtc
                };
                context.UserGroupMemberships.Add(membership);
                memberships.Add(membership);
            }

            var groupId = hasActiveManager
                ? CanonicalRbac.Employee.GroupId
                : CanonicalRbac.Manager.GroupId;
            membership.UserId = user.Id;
            membership.AccountId = user.Id;
            membership.PermissionGroupId = groupId;
            membership.DepartmentId = ownerDepartment.Id;
            membership.Description = null;
            membership.UpdatedByUserId = owner.User.Id;
            membership.UpdatedAtUtc = nowUtc;
            membership.IsDeleted = false;
            hasActiveManager |= groupId == CanonicalRbac.Manager.GroupId;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task ReconcileSupplementScenariosAsync(
        VPPMigrationDbContext context,
        DemoOwner owner,
        IReadOnlyList<VppRequest> regularRequests,
        Period currentPeriod,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var previousAnchor = new DateTime(currentPeriod.Year, currentPeriod.Month, 1).AddMonths(-1);
        var previousPeriod = new Period(previousAnchor.Year, previousAnchor.Month);
        var previousRequest = regularRequests.FirstOrDefault(request =>
                request.Year == previousPeriod.Year
                && request.Month == previousPeriod.Month
                && request.CreatedByUserId == owner.User.Id)
            ?? regularRequests.FirstOrDefault(request =>
                request.Year == previousPeriod.Year
                && request.Month == previousPeriod.Month);
        var desiredRequestIds = new HashSet<Guid>();

        if (previousRequest?.PeriodId is Guid previousPeriodId)
        {
            var persistedPeriod = await context.Periods.SingleAsync(
                period => period.Id == previousPeriodId,
                cancellationToken);
            var requestId = StableGuid("workbook-supplement|previous-approved");
            desiredRequestIds.Add(requestId);
            var submittedAtUtc = persistedPeriod.SubmissionDeadlineUtc.AddHours(1);
            var resolvedAtUtc = submittedAtUtc.AddHours(4);

            // Đơn bổ sung chỉ phát sinh sau khi kỳ thường đóng và phải được xử lý
            // trong chính cửa sổ bổ sung của kỳ trước.
            if (resolvedAtUtc > persistedPeriod.SupplementApprovalDeadlineUtc)
            {
                throw new InvalidOperationException(
                    $"Demo supplement window is invalid for period {persistedPeriod.Month:00}/{persistedPeriod.Year}.");
            }

            await UpsertSupplementScenarioAsync(
                context,
                previousRequest,
                "previous-approved",
                VPPStatus.Approved,
                "Bổ sung theo nhu cầu đã xác nhận.",
                owner.User.Id,
                submittedAtUtc,
                resolvedAtUtc,
                cancellationToken);
        }

        await RetireObsoleteSupplementScenariosAsync(
            context,
            desiredRequestIds,
            owner.User.Id,
            nowUtc,
            cancellationToken);
    }

    private static async Task RetireObsoleteSupplementScenariosAsync(
        VPPMigrationDbContext context,
        IReadOnlySet<Guid> desiredRequestIds,
        int actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var obsoleteRequests = await context.Requests
            .Include(request => request.RequestDetails)
            .Where(request => request.IdempotencyKey != null
                              && request.IdempotencyKey.StartsWith(SupplementScenarioIdempotencyPrefix)
                              && !desiredRequestIds.Contains(request.Id))
            .ToListAsync(cancellationToken);

        foreach (var request in obsoleteRequests)
        {
            request.IsDeleted = true;
            request.IsCurrentRevision = false;
            request.UpdatedByUserId = actorUserId;
            request.UpdatedAtUtc = nowUtc;

            foreach (var detail in request.RequestDetails.Where(detail => !detail.IsDeleted))
            {
                detail.IsDeleted = true;
                detail.UpdatedByUserId = actorUserId;
                detail.UpdatedAtUtc = nowUtc;
            }
        }
    }

    private static async Task UpsertSupplementScenarioAsync(
        VPPMigrationDbContext context,
        VppRequest baseRequest,
        string scenarioKey,
        VPPStatus status,
        string reason,
        int workflowActorUserId,
        DateTime submittedAtUtc,
        DateTime? resolvedAtUtc,
        CancellationToken cancellationToken)
    {
        var requestId = StableGuid($"workbook-supplement|{scenarioKey}");
        var request = await context.Requests
            .Include(item => item.RequestDetails)
            .FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (request is null)
        {
            request = new VppRequest
            {
                Id = requestId,
                RequestSeriesId = StableGuid($"workbook-supplement-series|{scenarioKey}"),
                RevisionNumber = 1,
                IsCurrentRevision = true,
                IsAdditionalOrder = true,
                CreatedByUserId = baseRequest.CreatedByUserId,
                CreatedAtUtc = submittedAtUtc
            };
            context.Requests.Add(request);
        }

        request.VppCode = BuildScenarioRequestCode(baseRequest.Year, baseRequest.Month, scenarioKey);
        request.Year = baseRequest.Year;
        request.Month = baseRequest.Month;
        request.PeriodId = baseRequest.PeriodId;
        request.BaseRequestId = baseRequest.Id;
        request.BaseRequestSeriesId = baseRequest.RequestSeriesId;
        request.SupplementSequence = 1;
        request.SupplementAttemptNumber = 1;
        request.SupplementReason = reason;
        request.Status = (int)status;
        request.DepartmentCode = baseRequest.DepartmentCode;
        request.MemberCompanyCode = baseRequest.MemberCompanyCode;
        request.SubmittedDate = submittedAtUtc;
        request.Description = reason;
        request.ApprovedById = status == VPPStatus.Approved ? workflowActorUserId : null;
        request.ApprovedAt = status == VPPStatus.Approved ? resolvedAtUtc : null;
        request.RejectedById = null;
        request.RejectedAt = null;
        request.RejectReason = null;
        request.CancelledById = status == VPPStatus.Cancelled ? workflowActorUserId : null;
        request.CancelledAt = status == VPPStatus.Cancelled ? resolvedAtUtc : null;
        request.CancelReason = status == VPPStatus.Cancelled ? reason : null;
        request.IdempotencyKey = $"workbook-supplement-{scenarioKey}";
        request.CommandPayloadHash = Sha256Hex(request.IdempotencyKey);
        request.IsCurrentRevision = true;
        request.UpdatedByUserId = workflowActorUserId;
        request.UpdatedAtUtc = resolvedAtUtc ?? submittedAtUtc;
        request.IsDeleted = false;

        var sourceDetails = baseRequest.RequestDetails
            .Where(detail => !detail.IsDeleted)
            .OrderBy(detail => detail.Id)
            .Take(2)
            .ToList();
        var desiredIds = new HashSet<Guid>();
        foreach (var source in sourceDetails)
        {
            var detailId = StableGuid($"workbook-supplement-detail|{scenarioKey}|{source.VppId}");
            desiredIds.Add(detailId);
            var detail = request.RequestDetails.FirstOrDefault(item => item.Id == detailId);
            if (detail is null)
            {
                detail = new VppRequestDetail
                {
                    Id = detailId,
                    RequestId = request.Id,
                    VppId = source.VppId,
                    CreatedByUserId = request.CreatedByUserId,
                    CreatedAtUtc = submittedAtUtc
                };
                request.RequestDetails.Add(detail);
            }

            detail.Qty = 1;
            detail.CurrentSinglePrice = source.CurrentSinglePrice;
            detail.Description = null;
            detail.UpdatedByUserId = workflowActorUserId;
            detail.UpdatedAtUtc = resolvedAtUtc ?? submittedAtUtc;
            detail.IsDeleted = false;
        }

        foreach (var obsolete in request.RequestDetails.Where(detail =>
                     !detail.IsDeleted && !desiredIds.Contains(detail.Id)))
        {
            obsolete.IsDeleted = true;
            obsolete.UpdatedByUserId = workflowActorUserId;
            obsolete.UpdatedAtUtc = resolvedAtUtc ?? submittedAtUtc;
        }

        await ReconcileSupplementLogsAsync(
            context,
            request,
            status,
            reason,
            workflowActorUserId,
            submittedAtUtc,
            resolvedAtUtc,
            cancellationToken);
        await SaveDemoChangesWithConcurrencyRetryAsync(context, cancellationToken);
    }

    private static async Task ReconcileSupplementLogsAsync(
        VPPMigrationDbContext context,
        VppRequest request,
        VPPStatus status,
        string reason,
        int workflowActorUserId,
        DateTime submittedAtUtc,
        DateTime? resolvedAtUtc,
        CancellationToken cancellationToken)
    {
        var existing = await context.RequestLogs
            .Where(item => item.RequestId == request.Id)
            .ToListAsync(cancellationToken);
        Upsert("SUBMITTED", "Đã gửi đơn bổ sung", request.CreatedByUserId, submittedAtUtc, reason);
        if (status == VPPStatus.Approved && resolvedAtUtc.HasValue)
            Upsert("APPROVED", "Đã duyệt đơn bổ sung", workflowActorUserId, resolvedAtUtc.Value, reason);
        if (status == VPPStatus.Cancelled && resolvedAtUtc.HasValue)
            Upsert("CANCELLED", "Đã hủy đơn bổ sung", workflowActorUserId, resolvedAtUtc.Value, reason);

        void Upsert(string action, string title, int actorUserId, DateTime occurredAtUtc, string? logReason)
        {
            var id = StableGuid($"workbook-supplement-log|{request.Id}|{action}");
            var log = existing.FirstOrDefault(item => item.Id == id);
            if (log is null)
            {
                log = new RequestLog { Id = id, RequestId = request.Id };
                context.RequestLogs.Add(log);
                existing.Add(log);
            }

            log.LogDate = occurredAtUtc;
            log.LogTitle = title;
            log.ActorUserId = actorUserId;
            log.MemberCompanyCode = request.MemberCompanyCode;
            log.Action = action;
            log.RevisionNumber = request.RevisionNumber;
            log.CorrelationId = $"workbook-{request.Id:N}";
            log.Reason = logReason;
            log.LogJS = null;
        }
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
        await ReconcileActiveInteractiveMembershipsAsync(
            context,
            owner,
            departmentByCode,
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
            .Where(x => !x.IsDeleted
                && !x.IsAdditionalOrder
                && ((x.VppCode != null && x.VppCode.StartsWith("DEMO-PPJ-"))
                    || (x.IdempotencyKey != null && x.IdempotencyKey.StartsWith("workbook-fixture-"))
                    || x.Description == LegacyDemoDescription))
            .ToListAsync(cancellationToken);
        var existingDemoByPeriodDepartment = existingDemoRequests
            .GroupBy(x => (x.Year, x.Month, DepartmentCode: x.DepartmentCode ?? string.Empty))
            .ToDictionary(x => x.Key, x => x.OrderByDescending(request => request.UpdatedAtUtc).First());

        var occupiedRequests = await context.Requests
            .Where(x => !x.IsDeleted && x.IsCurrentRevision && !x.IsAdditionalOrder)
            .Select(x => new { x.Id, x.CreatedByUserId, x.PeriodId, x.VppCode })
            .ToListAsync(cancellationToken);

        var requestCount = 0;
        var detailCount = 0;
        var seededRegularRequests = new List<VppRequest>();
        var desiredRegularRequestIds = new HashSet<Guid>();
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
            existingDemoByPeriodDepartment.TryGetValue(
                (period.Year, period.Month, departmentMonth.Key.DepartmentCode),
                out var request);

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

            var submittedAtUtc = period.StartAtUtc.AddDays(2).AddHours(2);
            if (submittedAtUtc > nowUtc)
                submittedAtUtc = nowUtc.AddMinutes(-5);

            if (request is null)
            {
                var requestId = StableGuid(
                    $"workbook-request|{period.Year:D4}{period.Month:D2}|{departmentMonth.Key.DepartmentCode}");
                request = new VppRequest
                {
                    Id = requestId,
                    RequestSeriesId = StableGuid(
                        $"workbook-request-series|{period.Year:D4}{period.Month:D2}|{departmentMonth.Key.DepartmentCode}"),
                    RevisionNumber = 1,
                    IsCurrentRevision = true,
                    IsAdditionalOrder = false,
                    CreatedByUserId = requester.Id,
                    CreatedAtUtc = submittedAtUtc
                };
                context.Requests.Add(request);
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
            request.Status = (int)VPPStatus.Submitted;
            request.DepartmentCode = departmentMonth.Key.DepartmentCode;
            request.MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode.ToString(CultureInfo.InvariantCulture);
            request.SubmittedDate = submittedAtUtc;
            request.ApprovedById = null;
            request.ApprovedAt = null;
            request.RejectedById = null;
            request.RejectedAt = null;
            request.RejectReason = null;
            request.CancelledById = null;
            request.CancelledAt = null;
            request.CancelReason = null;
            request.IdempotencyKey =
                $"workbook-fixture-{period.Year:D4}{period.Month:D2}-{departmentMonth.Key.DepartmentCode}";
            request.CommandPayloadHash = Sha256Hex(request.IdempotencyKey);
            request.Description = null;
            request.UpdatedByUserId = owner.User.Id;
            request.UpdatedAtUtc = nowUtc;
            request.IsDeleted = false;
            request.IsCurrentRevision = true;
            desiredRegularRequestIds.Add(request.Id);

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
                // Nguồn gốc workbook thuộc demo audit pipeline, không nằm trong ghi chú nghiệp vụ của user.
                detail.Description = null;
                detail.UpdatedByUserId = owner.User.Id;
                detail.UpdatedAtUtc = nowUtc;
                detail.IsDeleted = false;
                detailCount++;
            }

            await ReconcileRequestLogsAsync(
                context,
                request,
                submittedAtUtc,
                cancellationToken);
            seededRegularRequests.Add(request);
            requestCount++;
        }

        RetireObsoleteRegularRequests(
            existingDemoRequests,
            desiredRegularRequestIds,
            owner.User.Id,
            nowUtc);

        // Flush phần workbook lớn trước khi tạo scenario bổ sung. Nếu app demo đang
        // mở đồng thời, rowversion có thể đổi giữa lúc đọc và lúc ghi; retry chỉ áp
        // dụng client-wins cho projection TEST/DEMO do seeder này sở hữu.
        await SaveDemoChangesWithConcurrencyRetryAsync(context, cancellationToken);
        await ReconcileSupplementScenariosAsync(
            context,
            owner,
            seededRegularRequests,
            currentPeriod,
            nowUtc,
            cancellationToken);
        await ReconcileLegacyPersonaSupplementWindowsAsync(
            context,
            cancellationToken);

        await SaveDemoChangesWithConcurrencyRetryAsync(context, cancellationToken);
        Log.Information(
            "[DemoWorkbookSeeder] Operational fixture reconciled: {RequestCount} requests and {DetailCount} details for owner {OwnerUsername}.",
            requestCount,
            detailCount,
            owner.User.UserName);
    }

    private static async Task ReconcileLegacyPersonaSupplementWindowsAsync(
        VPPMigrationDbContext context,
        CancellationToken cancellationToken)
    {
        var requests = await context.Requests
            .Where(request => !request.IsDeleted
                              && request.IsCurrentRevision
                              && request.IsAdditionalOrder
                              && request.IdempotencyKey != null
                              && request.IdempotencyKey.StartsWith(PersonaSupplementIdempotencyPrefix))
            .ToListAsync(cancellationToken);
        if (requests.Count == 0)
        {
            return;
        }

        var periodIds = requests
            .Where(request => request.PeriodId.HasValue)
            .Select(request => request.PeriodId!.Value)
            .Distinct()
            .ToArray();
        var periods = await context.Periods
            .Where(period => periodIds.Contains(period.Id))
            .ToDictionaryAsync(period => period.Id, cancellationToken);
        var requestIds = requests.Select(request => request.Id).ToArray();
        var logs = await context.RequestLogs
            .Where(log => requestIds.Contains(log.RequestId))
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            if (!request.PeriodId.HasValue
                || !periods.TryGetValue(request.PeriodId.Value, out var period))
            {
                throw new InvalidOperationException(
                    $"Demo supplement {request.Id} is missing its period.");
            }

            var submittedAtUtc = period.SubmissionDeadlineUtc.AddHours(1);
            var resolvedAtUtc = submittedAtUtc.AddHours(4);
            if (resolvedAtUtc > period.SupplementApprovalDeadlineUtc)
            {
                throw new InvalidOperationException(
                    $"Demo supplement window is invalid for period {period.Month:00}/{period.Year}.");
            }

            // Dữ liệu persona đời cũ từng phát sinh đơn bổ sung trước khi kỳ đóng.
            // Chuẩn hóa cả yêu cầu và timeline để lần seed lại vẫn đúng nghiệp vụ mới.
            request.SubmittedDate = submittedAtUtc;
            request.ApprovedAt = request.Status == (int)VPPStatus.Approved ? resolvedAtUtc : null;
            request.RejectedAt = request.Status == (int)VPPStatus.Rejected ? resolvedAtUtc : null;
            request.CancelledAt = request.Status == (int)VPPStatus.Cancelled ? resolvedAtUtc : null;
            request.UpdatedAtUtc = request.Status == (int)VPPStatus.Pending
                ? submittedAtUtc
                : resolvedAtUtc;

            foreach (var log in logs.Where(log => log.RequestId == request.Id))
            {
                if (string.Equals(log.Action, "SUBMITTED", StringComparison.OrdinalIgnoreCase))
                {
                    log.LogDate = submittedAtUtc;
                }
                else if (string.Equals(log.Action, "APPROVED", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(log.Action, "REJECTED", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(log.Action, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                {
                    log.LogDate = resolvedAtUtc;
                }
            }
        }
    }

    private static void RetireObsoleteRegularRequests(
        IReadOnlyList<VppRequest> existingDemoRequests,
        IReadOnlySet<Guid> desiredRequestIds,
        int actorUserId,
        DateTime nowUtc)
    {
        foreach (var request in existingDemoRequests.Where(request =>
                     !desiredRequestIds.Contains(request.Id)))
        {
            request.IsDeleted = true;
            request.IsCurrentRevision = false;
            request.UpdatedByUserId = actorUserId;
            request.UpdatedAtUtc = nowUtc;

            foreach (var detail in request.RequestDetails.Where(detail => !detail.IsDeleted))
            {
                detail.IsDeleted = true;
                detail.UpdatedByUserId = actorUserId;
                detail.UpdatedAtUtc = nowUtc;
            }
        }
    }

    private static async Task SaveDemoChangesWithConcurrencyRetryAsync(
        VPPMigrationDbContext context,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await NormalizeMissingTrackedDemoChildrenAsync(context, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException exception) when (attempt < maxAttempts)
            {
                foreach (var entry in exception.Entries)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues is null)
                    {
                        if (entry.Entity is VppRequestDetail or RequestLog)
                        {
                            entry.State = entry.State == EntityState.Deleted
                                ? EntityState.Detached
                                : EntityState.Added;
                            continue;
                        }

                        throw new InvalidOperationException(
                            $"Demo-owned {entry.Metadata.ClrType.Name} was deleted during seed reconciliation.",
                            exception);
                    }

                    entry.OriginalValues.SetValues(databaseValues);
                }

                Log.Warning(
                    "[DemoWorkbookSeeder] Retrying demo reconciliation after optimistic concurrency conflict. Attempt {Attempt}/{MaxAttempts}; entries: {EntryTypes}.",
                    attempt + 1,
                    maxAttempts,
                    string.Join(", ", exception.Entries
                        .Select(entry => $"{entry.Metadata.ClrType.Name}:{entry.State}")
                        .Distinct()));
            }
        }
    }

    private static async Task NormalizeMissingTrackedDemoChildrenAsync(
        VPPMigrationDbContext context,
        CancellationToken cancellationToken)
    {
        var detailEntries = context.ChangeTracker.Entries<VppRequestDetail>()
            .Where(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            .ToArray();
        var logEntries = context.ChangeTracker.Entries<RequestLog>()
            .Where(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            .ToArray();

        var existingDetailIds = new HashSet<Guid>();
        foreach (var idBatch in detailEntries.Select(entry => entry.Entity.Id).Distinct().Chunk(1000))
        {
            existingDetailIds.UnionWith(await context.RequestDetails
                .AsNoTracking()
                .Where(detail => idBatch.Contains(detail.Id))
                .Select(detail => detail.Id)
                .ToArrayAsync(cancellationToken));
        }

        var existingLogIds = new HashSet<Guid>();
        foreach (var idBatch in logEntries.Select(entry => entry.Entity.Id).Distinct().Chunk(1000))
        {
            existingLogIds.UnionWith(await context.RequestLogs
                .AsNoTracking()
                .Where(log => idBatch.Contains(log.Id))
                .Select(log => log.Id)
                .ToArrayAsync(cancellationToken));
        }

        var normalized = 0;
        foreach (var entry in detailEntries.Where(entry => !existingDetailIds.Contains(entry.Entity.Id)))
        {
            entry.State = entry.State == EntityState.Deleted
                ? EntityState.Detached
                : EntityState.Added;
            normalized++;
        }

        foreach (var entry in logEntries.Where(entry => !existingLogIds.Contains(entry.Entity.Id)))
        {
            entry.State = entry.State == EntityState.Deleted
                ? EntityState.Detached
                : EntityState.Added;
            normalized++;
        }

        if (normalized > 0)
        {
            Log.Warning(
                "[DemoWorkbookSeeder] Normalized {Count} missing deterministic detail/log rows before demo reconciliation.",
                normalized);
        }
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
                    AccountStatus = AppAccountStatus.Active,
                    MustChangePassword = true,
                    SessionVersion = 1,
                    EmailConfirmed = false,
                    PasswordHash = null,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                    LockoutEnabled = true,
                    LockoutEnd = null,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc,
                    ActivatedAtUtc = nowUtc,
                    DisabledAtUtc = null
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
                user.UpdatedAtUtc = nowUtc;

                if (IsLegacyLockedDemoAccount(user))
                {
                    // Fixture cũ khóa account vĩnh viễn nên membership không thể bật lại.
                    // Chỉ sửa đúng dấu vết cũ; trạng thái quản trị về sau vẫn được giữ nguyên.
                    user.AccountStatus = AppAccountStatus.Active;
                    user.MustChangePassword = string.IsNullOrWhiteSpace(user.PasswordHash)
                        || user.MustChangePassword;
                    user.LockoutEnabled = true;
                    user.LockoutEnd = null;
                    user.ActivatedAtUtc ??= nowUtc;
                    user.DisabledAtUtc = null;
                    user.SessionVersion = checked(user.SessionVersion + 1);
                    user.SecurityStamp = Guid.NewGuid().ToString("N");
                    user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
                }
            }

            result[row.DepartmentCode] = user;
        }

        await context.SaveChangesAsync(cancellationToken);

        var demoUserIds = result.Values
            .Where(user => user.Id != owner.User.Id)
            .Select(user => user.Id)
            .Distinct()
            .ToArray();
        var memberships = await context.UserGroupMemberships
            .Where(x => demoUserIds.Contains(x.UserId)
                || (x.AccountId.HasValue && demoUserIds.Contains(x.AccountId.Value)))
            .ToListAsync(cancellationToken);
        foreach (var pair in result.Where(x => x.Value.Id != owner.User.Id))
        {
            var user = pair.Value;
            var department = departmentByCode[pair.Key];
            var membership = memberships.FirstOrDefault(x =>
                x.AccountId == user.Id || x.UserId == user.Id);
            if (membership is not null)
            {
                // Không tự mở lại membership đã bị quản trị viên vô hiệu hóa và
                // không ghi đè nhóm/phòng ban đã được chỉnh sau lần seed đầu tiên.
                continue;
            }

            membership = new UserGroupMembership
            {
                Id = StableGuid($"demo-membership|{user.NormalizedUserName}"),
                UserId = user.Id,
                AccountId = user.Id,
                PermissionGroupId = CanonicalRbac.Employee.GroupId,
                DepartmentId = department.Id,
                Description = null,
                CreatedByUserId = owner.User.Id,
                CreatedAtUtc = nowUtc,
                UpdatedByUserId = owner.User.Id,
                UpdatedAtUtc = nowUtc,
                IsDeleted = false
            };
            context.UserGroupMemberships.Add(membership);
            memberships.Add(membership);
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
                State = ResolvePeriodState(periodValue, isCurrent, calculator, nowUtc),
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
        period.PostCloseAdjustmentDeadlineUtc = period.SubmissionDeadlineUtc.AddDays(
            VppRequestPolicy.DefaultPostCloseAdjustmentDays);
        if (period.State != VppPeriodState.Settled)
        {
            period.State = ResolvePeriodState(periodValue, isCurrent, calculator, nowUtc);
        }
        period.Description = null;
        period.UpdatedByUserId = actorUserId;
        period.UpdatedAtUtc = nowUtc;
        period.IsDeleted = false;

        await context.SaveChangesAsync(cancellationToken);
        return period;
    }

    private static async Task ReconcileRequestLogsAsync(
        VPPMigrationDbContext context,
        VppRequest request,
        DateTime submittedAtUtc,
        CancellationToken cancellationToken)
    {
        var existing = await context.RequestLogs
            .Where(x => x.RequestId == request.Id)
            .ToListAsync(cancellationToken);
        UpsertLog("SUBMITTED", "Đã gửi đơn văn phòng phẩm", request.CreatedByUserId, submittedAtUtc);
        var obsoleteApprovedId = StableGuid($"demo-request-log|{request.Id}|APPROVED");
        var obsoleteApproved = existing.FirstOrDefault(x => x.Id == obsoleteApprovedId);
        if (obsoleteApproved is not null)
            context.RequestLogs.Remove(obsoleteApproved);

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
            log.CorrelationId = $"workbook-{request.Id:N}";
            log.Reason = null;
            log.LogJS = null;
        }
    }

    public static VppPeriodState ResolvePeriodState(
        Period period,
        bool isCurrent,
        PeriodCalculator calculator,
        DateTime nowUtc)
    {
        if (!isCurrent)
            return VppPeriodState.Pricing;

        if (nowUtc >= calculator.SupplementApprovalDeadlineUtc(
                period,
                TimeSpan.FromDays(VppRequestPolicy.DefaultSupplementApprovalGraceDays)))
        {
            return VppPeriodState.Pricing;
        }

        return nowUtc >= calculator.SubmissionDeadlineUtc(period)
            ? VppPeriodState.SubmissionClosed
            : VppPeriodState.Open;
    }

    private static async Task<DemoOwner?> ResolveOwnerAsync(
        VPPMigrationDbContext context,
        DemoWorkbookSeedOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.OwnerUsername) && !options.AutoResolveOwner)
            return null;

        AppUser? owner;
        if (!string.IsNullOrWhiteSpace(options.OwnerUsername))
        {
            var normalizedUsername = options.OwnerUsername.Trim().ToUpperInvariant();
            owner = await context.Users.FirstOrDefaultAsync(
                x => x.NormalizedUserName == normalizedUsername,
                cancellationToken);
        }
        else
        {
            var devGroupId = CanonicalRbac.Dev.GroupId;
            var ownerIds = await context.UserGroupMemberships
                .Where(membership => !membership.IsDeleted
                    && membership.AccountId.HasValue
                    && membership.PermissionGroupId == devGroupId)
                .Select(membership => membership.AccountId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
            var activeOwners = await context.Users
                .Where(user => ownerIds.Contains(user.Id)
                    && user.AccountStatus == AppAccountStatus.Active)
                .OrderBy(user => user.Id)
                .ToListAsync(cancellationToken);
            if (activeOwners.Count != 1)
            {
                throw new InvalidOperationException(
                    "Automatic demo owner resolution requires exactly one active DEV account in the TEST database.");
            }

            owner = activeOwners[0];
        }

        if (owner is null)
            throw new InvalidOperationException("The configured demo owner account does not exist.");
        if (owner.AccountStatus != AppAccountStatus.Active)
            throw new InvalidOperationException("The configured demo owner account is not active.");

        var membership = await context.UserGroupMemberships
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.AccountId == owner.Id && !x.IsDeleted, cancellationToken);
        var departmentCode = membership?.Department?.Code?.Trim();
        if (string.IsNullOrWhiteSpace(departmentCode))
        {
            throw new InvalidOperationException(
                "The configured demo owner account does not have an active primary department.");
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

    public static string BuildRequestCode(int year, int month, string departmentCode)
    {
        var safeCode = new string(departmentCode
            .Where(character => char.IsLetterOrDigit(character) || character is '+' or '-')
            .ToArray())
            .ToUpperInvariant();
        var suffix = StableGuid($"workbook-request-code|{year:D4}{month:D2}|{safeCode}")
            .ToString("N");
        return $"VPP-{year:D4}{month:D2}-{suffix}";
    }

    private static string BuildScenarioRequestCode(int year, int month, string scenarioKey) =>
        $"VPP-{year:D4}{month:D2}-{StableGuid($"workbook-scenario-code|{scenarioKey}"):N}";

    private static bool IsOwnedDemoUser(AppUser user) =>
        user.Email?.EndsWith(DemoEmailSuffix, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsLegacyLockedDemoAccount(AppUser user) =>
        IsOwnedDemoUser(user)
        && user.AccountStatus == AppAccountStatus.Disabled
        && string.IsNullOrWhiteSpace(user.PasswordHash)
        && user.LockoutEnd == DateTimeOffset.MaxValue;

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
