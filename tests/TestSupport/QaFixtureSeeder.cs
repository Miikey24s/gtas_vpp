using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_test_support;

internal static class QaFixtureSeeder
{
    private const string LongOrderLineCountEnvironmentVariable = "GTAS_E2E_LONG_ORDER_LINES";
    private const int SeedUserId = 1_000_001_006;
    private static readonly DateTime SeedTimestamp = new(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc);

    private static readonly IReadOnlyDictionary<int, Guid> UserGroupMappingIds =
        new Dictionary<int, Guid>
        {
            [1_000_001_001] = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            [1_000_001_002] = Guid.Parse("30000000-0000-0000-0000-000000000002"),
            [1_000_001_003] = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            [1_000_001_004] = Guid.Parse("30000000-0000-0000-0000-000000000004"),
            [1_000_001_005] = Guid.Parse("30000000-0000-0000-0000-000000000005"),
            [1_000_001_006] = Guid.Parse("30000000-0000-0000-0000-000000000006")
        };

    private static readonly IReadOnlyDictionary<Guid, Guid> RequestDetailIds =
        new Dictionary<Guid, Guid>
        {
            [QaTestData.OwnRequestId] = Guid.Parse("50000000-0000-0000-0000-000000000001"),
            [QaTestData.DepartmentPeerRequestId] = Guid.Parse("50000000-0000-0000-0000-000000000002"),
            [QaTestData.CompanyOtherDepartmentRequestId] = Guid.Parse("50000000-0000-0000-0000-000000000003")
        };

    public static VPPMigrationDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<VPPMigrationDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(Config.DatabaseSettings.MigrationsAssembly))
            .Options;
        return new VPPMigrationDbContext(options);
    }

    public static async Task MigrateAndSeedAsync(
        QaFixtureOptions options,
        QaFixtureSecrets secrets,
        QaTestAccounts accounts,
        CancellationToken cancellationToken)
    {
        await using var context = CreateContext(options.ConnectionString);
        await context.Database.MigrateAsync(cancellationToken);
        await SeedData.SeedDemo(context);

        await EnsureDepartmentsAsync(context, cancellationToken);
        await EnsureCanonicalGroupsAsync(context, cancellationToken);
        await EnsureUsersAsync(context, accounts, secrets, cancellationToken);
        await EnsureUserGroupsAsync(context, accounts, cancellationToken);
        await EnsureSecurityAuditsAsync(context, accounts, cancellationToken);
        var currentPeriod = await EnsureCurrentPeriodAsync(context, cancellationToken);
        var settlementPeriod = await EnsurePreviousSettlementPeriodAsync(
            context,
            currentPeriod,
            cancellationToken);
        await EnsureScopeRequestsAsync(context, accounts, currentPeriod, cancellationToken);
        await DemoPersonaScenarioSeeder.SeedAsync(
            context,
            new DemoPersonaSeedOptions(
                accounts.Employee.Username,
                accounts.Manager.Username,
                accounts.SystemAdmin.Username,
                currentPeriod.StartAtUtc.AddDays(10)),
            cancellationToken);
        await EnsureSettlementRequestAsync(context, accounts.Procurement, settlementPeriod, cancellationToken);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [dbo].[__GTASQARun]
            SET [SeededUtc] = SYSUTCDATETIME()
            WHERE [RunId] = {options.RunId}
              AND [Purpose] = {QaFixtureIdentityContract.Purpose};
            """, cancellationToken);
    }

    private static async Task EnsureDepartmentsAsync(
        VPPMigrationDbContext context,
        CancellationToken cancellationToken)
    {
        await UpsertDepartmentAsync(
            context,
            QaTestData.DepartmentAlphaId,
            QaTestData.DepartmentAlphaCode,
            "QA Department Alpha",
            cancellationToken);
        await UpsertDepartmentAsync(
            context,
            QaTestData.DepartmentBetaId,
            QaTestData.DepartmentBetaCode,
            "QA Department Beta",
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertDepartmentAsync(
        VPPMigrationDbContext context,
        Guid id,
        string code,
        string name,
        CancellationToken cancellationToken)
    {
        var department = await context.Departments
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (department is null)
        {
            department = new Department { Id = id };
            context.Departments.Add(department);
        }

        department.Code = code;
        department.Name = name;
        department.Description = "QA-001 deterministic scope fixture";
        department.CreatedByUserId = SeedUserId;
        department.CreatedAtUtc = SeedTimestamp;
        department.UpdatedByUserId = SeedUserId;
        department.UpdatedAtUtc = SeedTimestamp;
        department.IsDeleted = false;
    }

    private static async Task EnsureCanonicalGroupsAsync(
        VPPMigrationDbContext context,
        CancellationToken cancellationToken)
    {
        var personaIds = CanonicalRbac.Personas.Select(persona => persona.GroupId).ToArray();
        var groups = await context.PermissionGroups
            .AsNoTracking()
            .Where(group => personaIds.Contains(group.Id) && !group.IsDeleted)
            .ToListAsync(cancellationToken);

        var isCanonical = groups.Count == CanonicalRbac.Personas.Count
            && CanonicalRbac.Personas.All(persona => groups.Any(group =>
                group.Id == persona.GroupId
                && group.GroupCode == persona.GroupCode
                && group.ParentGroupId is null));
        if (!isCanonical)
        {
            throw new InvalidOperationException(
                $"QA fixture requires {CanonicalRbac.Personas.Count} reconciled canonical flat personas.");
        }
    }

    private static async Task EnsureUsersAsync(
        VPPMigrationDbContext context,
        QaTestAccounts accounts,
        QaFixtureSecrets secrets,
        CancellationToken cancellationToken)
    {
        var passwordHasher = new PasswordHasher<AppUser>();

        foreach (var account in accounts.All)
        {
            var user = new AppUser
            {
                Id = account.UserId,
                UserName = account.Username,
                NormalizedUserName = account.Username.ToUpperInvariant(),
                Email = account.Email,
                NormalizedEmail = account.Email.ToUpperInvariant(),
                FullName = account.FullName,
                MemberCompanyCode = QaTestData.CompanyCode,
                AccountStatus = AppAccountStatus.Active,
                EmailConfirmed = true,
                LockoutEnabled = true,
                SessionVersion = 1,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp,
                ActivatedAtUtc = SeedTimestamp,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };
            var passwordHash = passwordHasher.HashPassword(user, secrets.AccountPassword);

            await context.Database.ExecuteSqlRawAsync(
                """
                SET XACT_ABORT ON;

                UPDATE [dbo].[AspNetUsers]
                SET [UserName] = @username,
                    [NormalizedUserName] = @normalizedUsername,
                    [Email] = @email,
                    [NormalizedEmail] = @normalizedEmail,
                    [EmailConfirmed] = 1,
                    [PasswordHash] = @passwordHash,
                    [SecurityStamp] = @securityStamp,
                    [ConcurrencyStamp] = @concurrencyStamp,
                    [FullName] = @fullName,
                    [EmployeeCode] = NULL,
                    [MemberCompanyCode] = @companyCode,
                    [AccountStatus] = N'Active',
                    [MustChangePassword] = 0,
                    [SessionVersion] = 1,
                    [UpdatedAtUtc] = @timestamp,
                    [ActivatedAtUtc] = @timestamp,
                    [DisabledAtUtc] = NULL,
                    [LockoutEnd] = NULL,
                    [LockoutEnabled] = 1,
                    [AccessFailedCount] = 0
                WHERE [Id] = @userId;

                IF @@ROWCOUNT = 0
                BEGIN
                    SET IDENTITY_INSERT [dbo].[AspNetUsers] ON;
                    BEGIN TRY
                        INSERT INTO [dbo].[AspNetUsers]
                        (
                            [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
                            [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
                            [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd],
                            [LockoutEnabled], [AccessFailedCount], [FullName], [EmployeeCode],
                            [MemberCompanyCode], [AccountStatus], [MustChangePassword], [SessionVersion],
                            [CreatedAtUtc], [UpdatedAtUtc], [ActivatedAtUtc], [DisabledAtUtc], [LastLoginAtUtc]
                        )
                        VALUES
                        (
                            @userId, @username, @normalizedUsername, @email, @normalizedEmail,
                            1, @passwordHash, @securityStamp, @concurrencyStamp,
                            NULL, 0, 0, NULL,
                            1, 0, @fullName, NULL,
                            @companyCode, N'Active', 0, 1,
                            @timestamp, @timestamp, @timestamp, NULL, NULL
                        );
                        SET IDENTITY_INSERT [dbo].[AspNetUsers] OFF;
                    END TRY
                    BEGIN CATCH
                        SET IDENTITY_INSERT [dbo].[AspNetUsers] OFF;
                        THROW;
                    END CATCH;
                END;
                """,
                new SqlParameter("@userId", account.UserId),
                new SqlParameter("@username", account.Username),
                new SqlParameter("@normalizedUsername", account.Username.ToUpperInvariant()),
                new SqlParameter("@email", account.Email),
                new SqlParameter("@normalizedEmail", account.Email.ToUpperInvariant()),
                new SqlParameter("@passwordHash", passwordHash),
                new SqlParameter("@securityStamp", user.SecurityStamp),
                new SqlParameter("@concurrencyStamp", user.ConcurrencyStamp),
                new SqlParameter("@fullName", account.FullName),
                new SqlParameter("@companyCode", QaTestData.CompanyCode),
                new SqlParameter("@timestamp", SeedTimestamp));
        }
    }

    private static async Task EnsureUserGroupsAsync(
        VPPMigrationDbContext context,
        QaTestAccounts accounts,
        CancellationToken cancellationToken)
    {
        foreach (var account in accounts.All)
        {
            var groupId = account.Role switch
            {
                "DepartmentApprover" => CanonicalRbac.DepartmentApprover.GroupId,
                "Procurement" => CanonicalRbac.ProcurementAdmin.GroupId,
                "SystemAdmin" => CanonicalRbac.SystemAdmin.GroupId,
                _ => CanonicalRbac.Employee.GroupId
            };
            var mappingId = UserGroupMappingIds[account.UserId];
            var mapping = await context.UserGroupMemberships
                .SingleOrDefaultAsync(x => x.Id == mappingId, cancellationToken);
            if (mapping is null)
            {
                mapping = new UserGroupMembership { Id = mappingId, UserId = account.UserId };
                context.UserGroupMemberships.Add(mapping);
            }

            mapping.UserId = account.UserId;
            mapping.AccountId = account.UserId;
            mapping.PermissionGroupId = groupId;
            mapping.DepartmentId = account.DepartmentId;
            mapping.Description = $"QA-001 {account.Role} account";
            mapping.CreatedByUserId = SeedUserId;
            mapping.CreatedAtUtc = SeedTimestamp;
            mapping.UpdatedByUserId = SeedUserId;
            mapping.UpdatedAtUtc = SeedTimestamp;
            mapping.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSecurityAuditsAsync(
        VPPMigrationDbContext context,
        QaTestAccounts accounts,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new SecurityAudit
            {
                Id = Guid.Parse("31000000-0000-0000-0000-000000000001"),
                TargetUserId = accounts.SystemAdmin.UserId,
                Action = "AUTH_BOOTSTRAP_OWNER_CREATED",
                ResourceType = "AppUser",
                ResourceId = accounts.SystemAdmin.UserId.ToString(),
                Outcome = "Succeeded",
                Summary = "Fixture QA đã tạo tài khoản quản trị hệ thống chuẩn.",
                Reason = "Dữ liệu trình duyệt cô lập có thể tái lập.",
                CorrelationId = "qa-security-audit-001",
                OccurredAtUtc = SeedTimestamp
            },
            new SecurityAudit
            {
                Id = Guid.Parse("31000000-0000-0000-0000-000000000002"),
                ActorUserId = accounts.SystemAdmin.UserId,
                TargetUserId = accounts.Employee.UserId,
                Action = "MEMBERSHIP_CREATED",
                ResourceType = "UserGroupMembership",
                ResourceId = UserGroupMappingIds[accounts.Employee.UserId].ToString(),
                Outcome = "Succeeded",
                Summary = "Đã gán người dùng vào vai trò Nhân viên chuẩn.",
                Reason = "Dữ liệu trình duyệt cô lập có thể tái lập.",
                CorrelationId = "qa-security-audit-002",
                OccurredAtUtc = SeedTimestamp.AddMinutes(1)
            },
            new SecurityAudit
            {
                Id = Guid.Parse("31000000-0000-0000-0000-000000000003"),
                ActorUserId = accounts.SystemAdmin.UserId,
                Action = "PERMISSION_UI_BATCH_UPDATED",
                ResourceType = "PermissionGroup",
                ResourceId = CanonicalRbac.Employee.GroupId.ToString(),
                Outcome = "Succeeded",
                Summary = "Đã cập nhật một quyền giao diện đại diện.",
                Reason = "Dữ liệu trình duyệt cô lập có thể tái lập.",
                CorrelationId = "qa-security-audit-003",
                OccurredAtUtc = SeedTimestamp.AddMinutes(2)
            }
        };

        foreach (var definition in definitions)
        {
            var audit = await context.SecurityAudits.SingleOrDefaultAsync(
                item => item.Id == definition.Id,
                cancellationToken);
            if (audit is null)
            {
                context.SecurityAudits.Add(definition);
                continue;
            }

            audit.ActorUserId = definition.ActorUserId;
            audit.TargetUserId = definition.TargetUserId;
            audit.Action = definition.Action;
            audit.ResourceType = definition.ResourceType;
            audit.ResourceId = definition.ResourceId;
            audit.Outcome = definition.Outcome;
            audit.Summary = definition.Summary;
            audit.Reason = definition.Reason;
            audit.CorrelationId = definition.CorrelationId;
            audit.OccurredAtUtc = definition.OccurredAtUtc;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<VppPeriod> EnsureCurrentPeriodAsync(
        VPPMigrationDbContext context,
        CancellationToken cancellationToken)
    {
        var calculator = new PeriodCalculator(deadlineDay: 5);
        var businessNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            PeriodCalculator.BusinessTimeZone);
        var businessPeriod = calculator.Current(businessNow);
        var year = businessPeriod.Year;
        var month = businessPeriod.Month;
        var companyCode = QaTestData.CompanyCode.ToString();
        var period = await context.Periods.SingleOrDefaultAsync(
            x => x.MemberCompanyCode == companyCode
                 && x.Year == year
                 && x.Month == month
                 && !x.IsDeleted,
            cancellationToken);

        if (period is null)
        {
            period = new VppPeriod { Id = QaTestData.CurrentPeriodId };
            context.Periods.Add(period);
        }

        period.MemberCompanyCode = companyCode;
        period.TimeZoneId = "Asia/Ho_Chi_Minh";
        period.Year = year;
        period.Month = month;
        period.StartAtUtc = calculator.StartAtUtc(businessPeriod);
        period.SubmissionDeadlineUtc = calculator.SubmissionDeadlineUtc(businessPeriod);
        period.SupplementApprovalDeadlineUtc = calculator.SupplementApprovalDeadlineUtc(
            businessPeriod,
            TimeSpan.FromDays(2));
        period.State = VppPeriodState.Open;
        period.LastTransitionUserId = null;
        period.LastTransitionAtUtc = null;
        period.LastTransitionReason = null;
        period.Description = "QA-001 deterministic current VPP period";
        period.CreatedByUserId = SeedUserId;
        period.CreatedAtUtc = SeedTimestamp;
        period.UpdatedByUserId = SeedUserId;
        period.UpdatedAtUtc = SeedTimestamp;
        period.IsDeleted = false;

        await context.SaveChangesAsync(cancellationToken);
        return period;
    }

    private static async Task<VppPeriod> EnsurePreviousSettlementPeriodAsync(
        VPPMigrationDbContext context,
        VppPeriod currentPeriod,
        CancellationToken cancellationToken)
    {
        var calculator = new PeriodCalculator(deadlineDay: 5);
        var previousAnchor = new DateTime(currentPeriod.Year, currentPeriod.Month, 1).AddMonths(-1);
        var previous = new Period(previousAnchor.Year, previousAnchor.Month);
        var companyCode = QaTestData.CompanyCode.ToString();
        var period = await context.Periods.SingleOrDefaultAsync(
            x => x.MemberCompanyCode == companyCode
                 && x.Year == previous.Year
                 && x.Month == previous.Month
                 && !x.IsDeleted,
            cancellationToken);

        if (period is null)
        {
            period = new VppPeriod { Id = QaTestData.PreviousSettlementPeriodId };
            context.Periods.Add(period);
        }

        period.MemberCompanyCode = companyCode;
        period.TimeZoneId = "Asia/Ho_Chi_Minh";
        period.Year = previous.Year;
        period.Month = previous.Month;
        period.StartAtUtc = calculator.StartAtUtc(previous);
        period.SubmissionDeadlineUtc = calculator.SubmissionDeadlineUtc(previous);
        period.SupplementApprovalDeadlineUtc = calculator.SupplementApprovalDeadlineUtc(
            previous,
            TimeSpan.FromDays(2));
        period.State = VppPeriodState.Pricing;
        period.LastTransitionUserId = SeedUserId;
        period.LastTransitionAtUtc = SeedTimestamp;
        period.LastTransitionReason = "QA settlement fixture ready for pricing";
        period.Description = "QA-001 deterministic previous period for settlement mutation tests";
        period.CreatedByUserId = SeedUserId;
        period.CreatedAtUtc = SeedTimestamp;
        period.UpdatedByUserId = SeedUserId;
        period.UpdatedAtUtc = SeedTimestamp;
        period.IsDeleted = false;

        await context.SaveChangesAsync(cancellationToken);
        return period;
    }

    private static async Task EnsureScopeRequestsAsync(
        VPPMigrationDbContext context,
        QaTestAccounts accounts,
        VppPeriod period,
        CancellationToken cancellationToken)
    {
        var product = await context.VppItems
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id })
            .FirstAsync(cancellationToken);
        var price = await context.SupplierProductMappings
            .Where(x => x.VppItemId == product.Id && x.IsDefault && !x.IsDeleted)
            .Select(x => (long?)x.Price)
            .FirstOrDefaultAsync(cancellationToken) ?? 25_000L;

        var definitions = new[]
        {
            new RequestDefinition(
                QaTestData.OwnRequestId,
                $"QA-OWN-{period.Year:D4}{period.Month:D2}",
                accounts.Employee,
                1),
            new RequestDefinition(
                QaTestData.DepartmentPeerRequestId,
                $"QA-DEPT-{period.Year:D4}{period.Month:D2}",
                accounts.DepartmentPeer,
                2),
            new RequestDefinition(
                QaTestData.CompanyOtherDepartmentRequestId,
                $"QA-COMPANY-{period.Year:D4}{period.Month:D2}",
                accounts.OtherDepartmentEmployee,
                3)
        };
        var requestTimestamp = period.StartAtUtc.AddHours(1);

        foreach (var definition in definitions)
        {
            var header = await context.Requests
                .SingleOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (header is null)
            {
                header = new VppRequest { Id = definition.Id };
                context.Requests.Add(header);
            }

            header.VppCode = definition.Code;
            header.Year = period.Year;
            header.Month = period.Month;
            header.PeriodId = period.Id;
            header.RequestSeriesId = definition.Id;
            header.RevisionNumber = 1;
            header.IsCurrentRevision = true;
            header.SupersedesRequestId = null;
            header.SupersededByRequestId = null;
            header.BaseRequestId = null;
            header.BaseRequestSeriesId = null;
            header.SupplementSequence = null;
            header.SupplementAttemptNumber = null;
            header.SupplementReason = null;
            header.Status = (int)VPPStatus.Submitted;
            header.DepartmentCode = definition.Owner.DepartmentCode;
            header.MemberCompanyCode = QaTestData.CompanyCode.ToString();
            header.SubmittedDate = requestTimestamp;
            header.IsAdditionalOrder = false;
            header.CancelledById = null;
            header.CancelledAt = null;
            header.CancelReason = null;
            header.IdempotencyKey = null;
            header.CommandPayloadHash = null;
            header.Description = "QA-001 deterministic own/department/company scope data";
            header.CreatedByUserId = definition.Owner.UserId;
            header.CreatedAtUtc = requestTimestamp;
            header.UpdatedByUserId = definition.Owner.UserId;
            header.UpdatedAtUtc = requestTimestamp;
            header.IsDeleted = false;

            var detailId = RequestDetailIds[definition.Id];
            var detail = await context.RequestDetails
                .SingleOrDefaultAsync(x => x.Id == detailId, cancellationToken);
            if (detail is null)
            {
                detail = new VppRequestDetail { Id = detailId };
                context.RequestDetails.Add(detail);
            }

            detail.VppId = product.Id;
            detail.Qty = definition.Quantity;
            detail.CurrentSinglePrice = price;
            detail.RequestId = definition.Id;
            detail.Description = "QA-001 deterministic request line";
            detail.CreatedByUserId = definition.Owner.UserId;
            detail.CreatedAtUtc = requestTimestamp;
            detail.UpdatedByUserId = definition.Owner.UserId;
            detail.UpdatedAtUtc = requestTimestamp;
            detail.IsDeleted = false;

            if (definition.Id == QaTestData.OwnRequestId)
            {
                await EnsureLongOrderDetailsAsync(
                    context,
                    definition,
                    product.Id,
                    price,
                    requestTimestamp,
                    cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureLongOrderDetailsAsync(
        VPPMigrationDbContext context,
        RequestDefinition definition,
        Guid productId,
        long price,
        DateTime requestTimestamp,
        CancellationToken cancellationToken)
    {
        var configuredValue = Environment.GetEnvironmentVariable(LongOrderLineCountEnvironmentVariable);
        if (!int.TryParse(configuredValue, out var requestedLineCount) || requestedLineCount <= 1)
        {
            return;
        }

        var lineCount = Math.Clamp(requestedLineCount, 2, 500);
        var extraIds = Enumerable.Range(2, lineCount - 1)
            .Select(index => Guid.Parse($"51000000-0000-0000-0000-{index:X12}"))
            .ToArray();
        var existingDetails = await context.RequestDetails
            .Where(detail => extraIds.Contains(detail.Id))
            .ToDictionaryAsync(detail => detail.Id, cancellationToken);

        for (var index = 2; index <= lineCount; index++)
        {
            var detailId = extraIds[index - 2];
            if (!existingDetails.TryGetValue(detailId, out var detail))
            {
                detail = new VppRequestDetail { Id = detailId };
                context.RequestDetails.Add(detail);
            }

            detail.VppId = productId;
            detail.Qty = index;
            detail.CurrentSinglePrice = price;
            detail.RequestId = definition.Id;
            detail.Description = $"QA long-order line {index:D3}";
            detail.CreatedByUserId = definition.Owner.UserId;
            detail.CreatedAtUtc = requestTimestamp;
            detail.UpdatedByUserId = definition.Owner.UserId;
            detail.UpdatedAtUtc = requestTimestamp;
            detail.IsDeleted = false;
        }
    }

    private static async Task EnsureSettlementRequestAsync(
        VPPMigrationDbContext context,
        QaTestAccount owner,
        VppPeriod period,
        CancellationToken cancellationToken)
    {
        // Kỳ trước cần một đơn hợp lệ để E2E kiểm chứng chốt kỳ và four-eyes trên dữ liệu cô lập.
        var product = await context.VppItems
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id })
            .FirstAsync(cancellationToken);
        var price = await context.SupplierProductMappings
            .Where(mapping => mapping.VppItemId == product.Id && mapping.IsDefault && !mapping.IsDeleted)
            .Select(mapping => (long?)mapping.Price)
            .FirstOrDefaultAsync(cancellationToken) ?? 25_000L;
        var requestTimestamp = period.StartAtUtc.AddHours(1);

        var header = await context.Requests.SingleOrDefaultAsync(
            request => request.Id == QaTestData.SettlementRequestId,
            cancellationToken);
        if (header is null)
        {
            header = new VppRequest { Id = QaTestData.SettlementRequestId };
            context.Requests.Add(header);
        }

        header.VppCode = $"QA-SETTLEMENT-{period.Year:D4}{period.Month:D2}";
        header.Year = period.Year;
        header.Month = period.Month;
        header.PeriodId = period.Id;
        header.RequestSeriesId = QaTestData.SettlementRequestId;
        header.RevisionNumber = 1;
        header.IsCurrentRevision = true;
        header.SupersedesRequestId = null;
        header.SupersededByRequestId = null;
        header.BaseRequestId = null;
        header.BaseRequestSeriesId = null;
        header.SupplementSequence = null;
        header.SupplementAttemptNumber = null;
        header.SupplementReason = null;
        header.Status = (int)VPPStatus.Submitted;
        header.DepartmentCode = owner.DepartmentCode;
        header.MemberCompanyCode = QaTestData.CompanyCode.ToString();
        header.SubmittedDate = requestTimestamp;
        header.IsAdditionalOrder = false;
        header.CancelledById = null;
        header.CancelledAt = null;
        header.CancelReason = null;
        header.IdempotencyKey = null;
        header.CommandPayloadHash = null;
        header.Description = "QA-001 deterministic settlement request";
        header.CreatedByUserId = owner.UserId;
        header.CreatedAtUtc = requestTimestamp;
        header.UpdatedByUserId = owner.UserId;
        header.UpdatedAtUtc = requestTimestamp;
        header.IsDeleted = false;

        var detail = await context.RequestDetails.SingleOrDefaultAsync(
            requestDetail => requestDetail.Id == QaTestData.SettlementRequestDetailId,
            cancellationToken);
        if (detail is null)
        {
            detail = new VppRequestDetail { Id = QaTestData.SettlementRequestDetailId };
            context.RequestDetails.Add(detail);
        }

        detail.VppId = product.Id;
        detail.Qty = 4;
        detail.CurrentSinglePrice = price;
        detail.RequestId = header.Id;
        detail.Description = "QA-001 deterministic settlement request line";
        detail.CreatedByUserId = owner.UserId;
        detail.CreatedAtUtc = requestTimestamp;
        detail.UpdatedByUserId = owner.UserId;
        detail.UpdatedAtUtc = requestTimestamp;
        detail.IsDeleted = false;

        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record RequestDefinition(
        Guid Id,
        string Code,
        QaTestAccount Owner,
        int Quantity);
}
