using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace gtas_vpp_be.Service.Services;

public sealed record DemoPersonaSeedOptions(
    string? EmployeeUsername = null,
    string? ManagerUsername = null,
    string? DevUsername = null,
    DateTime? NowUtc = null,
    bool AutoResolveUniquePersonas = false);

/// <summary>
/// Tạo dữ liệu nghiệp vụ dễ trình diễn cho ba persona canonical trong database TEST/DEMO.
/// Seeder chỉ chạm các dòng có ID và idempotency key do chính nó sở hữu.
/// </summary>
public static partial class DemoPersonaScenarioSeeder
{
    private const string IdempotencyPrefix = "persona-demo";
    private const int RequiredProductCount = 6;

    public static async Task SeedAsync(
        VPPMigrationDbContext context,
        DemoPersonaSeedOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        var employee = await ResolvePersonaAsync(
            context,
            CanonicalRbac.Employee,
            options.EmployeeUsername,
            options.AutoResolveUniquePersonas,
            cancellationToken);
        var manager = await ResolvePersonaAsync(
            context,
            CanonicalRbac.Manager,
            options.ManagerUsername,
            options.AutoResolveUniquePersonas,
            cancellationToken);
        var dev = await ResolvePersonaAsync(
            context,
            CanonicalRbac.Dev,
            options.DevUsername,
            options.AutoResolveUniquePersonas,
            cancellationToken);

        if (employee is null || manager is null || dev is null)
        {
            Log.Warning(
                "[DemoPersonaScenarioSeeder] Skipped persona scenarios because one unique active EMPLOYEE, MANAGER and DEV account was not available.");
            return;
        }

        var companyCodes = new[]
        {
            employee.User.MemberCompanyCode,
            manager.User.MemberCompanyCode,
            dev.User.MemberCompanyCode
        }.Distinct().ToArray();
        if (companyCodes.Length != 1)
        {
            throw new InvalidOperationException(
                "Demo persona accounts must belong to the same member company.");
        }

        var products = await LoadProductsAsync(context, cancellationToken);
        if (products.Count < RequiredProductCount)
        {
            throw new InvalidOperationException(
                $"Demo persona scenarios require at least {RequiredProductCount} priced catalog items.");
        }

        var nowUtc = NormalizeUtc(options.NowUtc ?? DateTime.UtcNow);
        var calculator = new PeriodCalculator();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, PeriodCalculator.BusinessTimeZone);
        var currentPeriod = calculator.Current(localNow);
        var periods = new Dictionary<int, VppPeriod>();
        foreach (var offset in new[] { 0, -2, -3, -4, -5 })
        {
            var anchor = new DateTime(currentPeriod.Year, currentPeriod.Month, 1).AddMonths(offset);
            var period = new Period(anchor.Year, anchor.Month);
            periods[offset] = await EnsurePeriodAsync(
                context,
                period,
                offset == 0,
                companyCodes[0],
                calculator,
                dev.User.Id,
                nowUtc,
                cancellationToken);
        }

        var personas = new[]
        {
            new PersonaSeed(employee, "NV", 0, manager.User.Id),
            new PersonaSeed(manager, "QL", 1, dev.User.Id),
            new PersonaSeed(dev, "DEV", 2, manager.User.Id)
        };

        foreach (var persona in personas)
        {
            var regularByOffset = new Dictionary<int, VppRequest>();
            foreach (var offset in periods.Keys.OrderDescending())
            {
                regularByOffset[offset] = await UpsertRegularRequestAsync(
                    context,
                    persona,
                    periods[offset],
                    products,
                    nowUtc,
                    cancellationToken);
            }

            await UpsertSupplementAsync(
                context,
                persona,
                regularByOffset[-2],
                VPPStatus.Approved,
                "Bổ sung vật tư phục vụ công việc phát sinh đã được xác nhận.",
                products,
                cancellationToken);
            await UpsertSupplementAsync(
                context,
                persona,
                regularByOffset[-3],
                VPPStatus.Rejected,
                "Nhu cầu bổ sung chưa đủ căn cứ để phê duyệt.",
                products,
                cancellationToken);
            await UpsertSupplementAsync(
                context,
                persona,
                regularByOffset[-4],
                VPPStatus.Cancelled,
                "Đơn vị không còn nhu cầu bổ sung.",
                products,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        Log.Information(
            "[DemoPersonaScenarioSeeder] Reconciled persona workflows for EMPLOYEE, MANAGER and DEV in company {CompanyCode}.",
            companyCodes[0]);
    }

    private static async Task<PersonaAccount?> ResolvePersonaAsync(
        VPPMigrationDbContext context,
        RbacPersonaDefinition persona,
        string? configuredUsername,
        bool autoResolveUniquePersona,
        CancellationToken cancellationToken)
    {
        var candidates = await (
                from membership in context.UserGroupMemberships.AsNoTracking()
                join user in context.Users.AsNoTracking()
                    on membership.AccountId equals (int?)user.Id
                join department in context.Departments.AsNoTracking()
                    on membership.DepartmentId equals department.Id
                where !membership.IsDeleted
                      && membership.PermissionGroupId == persona.GroupId
                      && !department.IsDeleted
                      && department.Code != null
                      && department.Code != string.Empty
                      && user.AccountStatus == AppAccountStatus.Active
                orderby user.Id
                select new PersonaAccount(user, department.Code!))
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(configuredUsername))
        {
            var normalizedUsername = configuredUsername.Trim().ToUpperInvariant();
            var matches = candidates
                .Where(candidate => candidate.User.NormalizedUserName == normalizedUsername)
                .ToList();
            return matches.Count == 1
                ? matches[0]
                : throw new InvalidOperationException(
                    $"Configured demo account '{configuredUsername}' is not one active {persona.GroupCode} account with a primary department.");
        }

        if (!autoResolveUniquePersona)
        {
            return null;
        }

        if (candidates.Count != 1)
        {
            Log.Warning(
                "[DemoPersonaScenarioSeeder] Auto resolution expected one active {Persona}; found {Count}.",
                persona.GroupCode,
                candidates.Count);
            return null;
        }

        return candidates[0];
    }

    private static async Task<IReadOnlyList<ProductSeed>> LoadProductsAsync(
        VPPMigrationDbContext context,
        CancellationToken cancellationToken)
    {
        var itemIds = await context.VppItems
            .AsNoTracking()
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.VppCode)
            .ThenBy(item => item.Id)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var mappings = await context.SupplierProductMappings
            .AsNoTracking()
            .Where(mapping => !mapping.IsDeleted && itemIds.Contains(mapping.VppItemId))
            .OrderByDescending(mapping => mapping.IsDefault)
            .ThenBy(mapping => mapping.Id)
            .ToListAsync(cancellationToken);

        return itemIds
            .Select(itemId =>
            {
                var mapping = mappings.FirstOrDefault(candidate => candidate.VppItemId == itemId);
                return mapping is null
                    ? null
                    : new ProductSeed(itemId, Math.Max(1L, decimal.ToInt64(decimal.Round(mapping.Price))));
            })
            .Where(product => product is not null)
            .Cast<ProductSeed>()
            .Take(12)
            .ToArray();
    }

    private static async Task<VppPeriod> EnsurePeriodAsync(
        VPPMigrationDbContext context,
        Period periodValue,
        bool isCurrent,
        long memberCompanyCode,
        PeriodCalculator calculator,
        int actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var companyCode = memberCompanyCode.ToString(CultureInfo.InvariantCulture);
        var period = await context.Periods.SingleOrDefaultAsync(
            candidate => candidate.MemberCompanyCode == companyCode
                         && candidate.Year == periodValue.Year
                         && candidate.Month == periodValue.Month
                         && !candidate.IsDeleted,
            cancellationToken);
        if (period is not null)
        {
            return period;
        }

        period = new VppPeriod
        {
            Id = StableGuid($"persona-demo-period|{companyCode}|{periodValue.Year:D4}{periodValue.Month:D2}"),
            MemberCompanyCode = companyCode,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            Year = periodValue.Year,
            Month = periodValue.Month,
            StartAtUtc = calculator.StartAtUtc(periodValue),
            SubmissionDeadlineUtc = calculator.SubmissionDeadlineUtc(periodValue),
            SupplementApprovalDeadlineUtc = calculator.SupplementApprovalDeadlineUtc(
                periodValue,
                TimeSpan.FromDays(2)),
            State = isCurrent ? VppPeriodState.Open : VppPeriodState.Pricing,
            LastTransitionUserId = isCurrent ? null : actorUserId,
            LastTransitionAtUtc = isCurrent ? null : nowUtc,
            LastTransitionReason = isCurrent ? null : "Dữ liệu demo lịch sử đã hoàn tất tiếp nhận.",
            Description = "Kỳ dữ liệu demo cho ba persona canonical.",
            CreatedByUserId = actorUserId,
            CreatedAtUtc = nowUtc,
            UpdatedByUserId = actorUserId,
            UpdatedAtUtc = nowUtc,
            IsDeleted = false
        };
        context.Periods.Add(period);
        await context.SaveChangesAsync(cancellationToken);
        return period;
    }

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private sealed record PersonaAccount(AppUser User, string DepartmentCode);

    private sealed record PersonaSeed(
        PersonaAccount Account,
        string Code,
        int ProductOffset,
        int WorkflowActorUserId);

    private sealed record ProductSeed(Guid ItemId, long Price);
}
