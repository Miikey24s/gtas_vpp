using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace gtas_vpp_test_support;

internal static class QaFixtureSeeder
{
    private const int SeedUserId = 910006;
    private static readonly DateTime SeedTimestamp = new(2026, 7, 15, 8, 0, 0, DateTimeKind.Unspecified);

    private static readonly IReadOnlyDictionary<int, Guid> UserGroupMappingIds =
        new Dictionary<int, Guid>
        {
            [910001] = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            [910002] = Guid.Parse("30000000-0000-0000-0000-000000000002"),
            [910003] = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            [910004] = Guid.Parse("30000000-0000-0000-0000-000000000004"),
            [910005] = Guid.Parse("30000000-0000-0000-0000-000000000005"),
            [910006] = Guid.Parse("30000000-0000-0000-0000-000000000006")
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
        var groups = await EnsureGroupsAsync(context, cancellationToken);
        await EnsureGroupPermissionsAsync(context, groups.ManagerGroupId, groups.ProcurementGroupId, cancellationToken);
        await EnsureUsersAsync(context, accounts, secrets, cancellationToken);
        await EnsureUserGroupsAsync(context, accounts, groups, cancellationToken);
        await EnsureScopeRequestsAsync(context, accounts, cancellationToken);

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
        var department = await context.LEX02_CompanyDepartmentLocations
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (department is null)
        {
            department = new LEX02_CompanyDepartmentLocation { Id = id };
            context.LEX02_CompanyDepartmentLocations.Add(department);
        }

        department.LEX02Code = code;
        department.LEX02Name = name;
        department.LEX02Type = "PhongBan";
        department.Description = "QA-001 deterministic scope fixture";
        department.CreateUserId = SeedUserId;
        department.CreateDate = SeedTimestamp;
        department.UpdateUserId = SeedUserId;
        department.UpdateDate = SeedTimestamp;
        department.IsDeleted = false;
    }

    private static async Task<GroupIds> EnsureGroupsAsync(
        VPPMigrationDbContext context,
        CancellationToken cancellationToken)
    {
        var employeeGroupId = await context.P02_Groups
            .Where(x => x.GroupName == "User" && !x.IsDeleted)
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);
        var systemAdminGroupId = await context.P02_Groups
            .Where(x => x.GroupName == "Admin" && !x.IsDeleted)
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);

        await UpsertGroupAsync(
            context,
            QaTestData.ManagerGroupId,
            "QA Manager",
            "Department-scoped QA manager",
            cancellationToken);
        await UpsertGroupAsync(
            context,
            QaTestData.ProcurementGroupId,
            "QA Procurement",
            "Company-scoped QA procurement",
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new GroupIds(
            employeeGroupId,
            QaTestData.ManagerGroupId,
            QaTestData.ProcurementGroupId,
            systemAdminGroupId);
    }

    private static async Task UpsertGroupAsync(
        VPPMigrationDbContext context,
        Guid id,
        string name,
        string description,
        CancellationToken cancellationToken)
    {
        var group = await context.P02_Groups.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (group is null)
        {
            group = new P02_Group { Id = id };
            context.P02_Groups.Add(group);
        }

        group.GroupName = name;
        group.Description = description;
        group.CreateUserId = SeedUserId;
        group.CreateDate = SeedTimestamp;
        group.UpdateUserId = SeedUserId;
        group.UpdateDate = SeedTimestamp;
        group.IsDeleted = false;
    }

    private static async Task EnsureGroupPermissionsAsync(
        VPPMigrationDbContext context,
        Guid managerGroupId,
        Guid procurementGroupId,
        CancellationToken cancellationToken)
    {
        string[] managerCodes =
        [
            "MENU_DASHBOARD", "MENU_REPORT", "REQUEST_ORDER", "REQUEST_PRODUCT_CATALOG",
            "REQUEST_HISTORY", "REQUEST_DEPARTMENT_SUMMARY", "REQUEST_ADMIN_APPROVAL", "REPORT_VIEW"
        ];
        string[] procurementCodes =
        [
            "MENU_DASHBOARD", "MENU_LIBRARY", "MENU_REPORT", "REQUEST_HISTORY",
            "REQUEST_ALL_ORDERS_SUMMARY", "REQUEST_ADMIN_APPROVAL", "PERIOD_SETTLE",
            "LIBRARY_CLASS", "LIBRARY_CATEGORY", "LIBRARY_ITEM", "LIBRARY_SUPPLIER",
            "LIBRARY_PRICE", "LIBRARY_PRICE_LIST", "LIBRARY_DEPARTMENT", "REPORT_VIEW"
        ];

        await EnsurePermissionsForGroupAsync(context, managerGroupId, managerCodes, cancellationToken);
        await EnsurePermissionsForGroupAsync(context, procurementGroupId, procurementCodes, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsurePermissionsForGroupAsync(
        VPPMigrationDbContext context,
        Guid groupId,
        IReadOnlyCollection<string> componentCodes,
        CancellationToken cancellationToken)
    {
        var mappingIds = await (
                from mapping in context.P05_PageComponentMappings
                join component in context.P03_Components
                    on mapping.P03_ComponentId equals component.Id
                where componentCodes.Contains(component.ComponentCode!)
                      && !component.IsDeleted
                select mapping.Id)
            .ToListAsync(cancellationToken);

        if (mappingIds.Count != componentCodes.Count)
        {
            throw new InvalidOperationException(
                $"QA fixture could not resolve all permissions for group '{groupId}'.");
        }

        var existing = await context.P06_GroupPageComponentMappings
            .Where(x => x.P02_GroupId == groupId && x.MemberCompanyCode == QaTestData.CompanyCode)
            .ToListAsync(cancellationToken);

        foreach (var mappingId in mappingIds)
        {
            var permission = existing.SingleOrDefault(x => x.P05_PageComponentMappingId == mappingId);
            if (permission is null)
            {
                permission = new P06_GroupPageComponentMapping
                {
                    P02_GroupId = groupId,
                    P05_PageComponentMappingId = mappingId,
                    MemberCompanyCode = QaTestData.CompanyCode
                };
                context.P06_GroupPageComponentMappings.Add(permission);
            }

            permission.IsEnable = true;
            permission.IsVisible = true;
            permission.CreateUserId = SeedUserId;
            permission.CreateDate = SeedTimestamp;
            permission.UpdateUserId = SeedUserId;
            permission.UpdateDate = SeedTimestamp;
        }
    }

    private static async Task EnsureUsersAsync(
        VPPMigrationDbContext context,
        QaTestAccounts accounts,
        QaFixtureSecrets secrets,
        CancellationToken cancellationToken)
    {
        var encoder = new TripleDesPasswordEncoder(
            Options.Create(new PasswordEncoderOptions { Key = secrets.PasswordEncryptionKey }));
        var encryptedPassword = encoder.Encrypt(secrets.AccountPassword);

        foreach (var account in accounts.All)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                UPDATE [GTAS_MENU].[dbo].[tblUsers]
                SET [UserLogin] = @username,
                    [PasswordChar] = @password,
                    [FullName] = @fullName,
                    [EmailAddress1] = @email,
                    [EmailAddress2] = NULL,
                    [GoogleEmail] = NULL,
                    [PhoneNo1] = NULL,
                    [PhoneNo2] = NULL,
                    [IsInactiveFlg] = 0,
                    [IsLockedFlg] = 0,
                    [MemberCompanyCode] = @companyCode,
                    [DepartmentCode] = @departmentCode,
                    [MemberCompanyName] = @companyName
                WHERE [UserID] = @userId;

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO [GTAS_MENU].[dbo].[tblUsers]
                    (
                        [UserID], [UserLogin], [PasswordChar], [FullName], [EmailAddress1],
                        [IsInactiveFlg], [IsLockedFlg], [MemberCompanyCode],
                        [DepartmentCode], [MemberCompanyName]
                    )
                    VALUES
                    (
                        @userId, @username, @password, @fullName, @email,
                        0, 0, @companyCode, @departmentCode, @companyName
                    );
                END;
                """,
                new SqlParameter("@userId", account.UserId),
                new SqlParameter("@username", account.Username),
                new SqlParameter("@password", encryptedPassword),
                new SqlParameter("@fullName", account.FullName),
                new SqlParameter("@email", account.Email),
                new SqlParameter("@companyCode", QaTestData.CompanyCode),
                new SqlParameter("@departmentCode", account.DepartmentCode),
                new SqlParameter("@companyName", QaTestData.CompanyName));
        }
    }

    private static async Task EnsureUserGroupsAsync(
        VPPMigrationDbContext context,
        QaTestAccounts accounts,
        GroupIds groups,
        CancellationToken cancellationToken)
    {
        foreach (var account in accounts.All)
        {
            var groupId = account.Role switch
            {
                "Manager" => groups.ManagerGroupId,
                "Procurement" => groups.ProcurementGroupId,
                "SystemAdmin" => groups.SystemAdminGroupId,
                _ => groups.EmployeeGroupId
            };
            var mappingId = UserGroupMappingIds[account.UserId];
            var mapping = await context.P04_UserGroups
                .SingleOrDefaultAsync(x => x.Id == mappingId, cancellationToken);
            if (mapping is null)
            {
                mapping = new P04_UserGroup { Id = mappingId, UserId = account.UserId };
                context.P04_UserGroups.Add(mapping);
            }

            mapping.UserId = account.UserId;
            mapping.P02_GroupId = groupId;
            mapping.LEX02_CompanyDepartmentLocationId = account.DepartmentId;
            mapping.Description = $"QA-001 {account.Role} account";
            mapping.CreateUserId = SeedUserId;
            mapping.CreateDate = SeedTimestamp;
            mapping.UpdateUserId = SeedUserId;
            mapping.UpdateDate = SeedTimestamp;
            mapping.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureScopeRequestsAsync(
        VPPMigrationDbContext context,
        QaTestAccounts accounts,
        CancellationToken cancellationToken)
    {
        var product = await context.L04_VPPs
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id })
            .FirstAsync(cancellationToken);
        var price = await context.L06_VPPSupplierMappings
            .Where(x => x.L04_VPPId == product.Id && x.IsDefault && !x.IsDeleted)
            .Select(x => (long?)x.Price)
            .FirstOrDefaultAsync(cancellationToken) ?? 25_000L;

        var definitions = new[]
        {
            new RequestDefinition(
                QaTestData.OwnRequestId,
                "QA-OWN-202607",
                accounts.Employee,
                1),
            new RequestDefinition(
                QaTestData.DepartmentPeerRequestId,
                "QA-DEPT-202607",
                accounts.DepartmentPeer,
                2),
            new RequestDefinition(
                QaTestData.CompanyOtherDepartmentRequestId,
                "QA-COMPANY-202607",
                accounts.OtherDepartmentEmployee,
                3)
        };

        foreach (var definition in definitions)
        {
            var header = await context.VPP01_RequestHeaders
                .SingleOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (header is null)
            {
                header = new VPP01_RequestHeader { Id = definition.Id };
                context.VPP01_RequestHeaders.Add(header);
            }

            header.VPPCode = definition.Code;
            header.Y = 2026;
            header.M = 7;
            header.Status = (int)VPPStatus.Submitted;
            header.DepartmentCode = definition.Owner.DepartmentCode;
            header.MemberCompanyCode = QaTestData.CompanyCode.ToString();
            header.SubmittedDate = SeedTimestamp;
            header.IsAdditionalOrder = false;
            header.Description = "QA-001 deterministic own/department/company scope data";
            header.CreateUserId = definition.Owner.UserId;
            header.CreateDate = SeedTimestamp;
            header.UpdateUserId = definition.Owner.UserId;
            header.UpdateDate = SeedTimestamp;
            header.IsDeleted = false;

            var detailId = RequestDetailIds[definition.Id];
            var detail = await context.VPP02_RequestDetail
                .SingleOrDefaultAsync(x => x.Id == detailId, cancellationToken);
            if (detail is null)
            {
                detail = new VPP02_RequestDetail { Id = detailId };
                context.VPP02_RequestDetail.Add(detail);
            }

            detail.VPPId = product.Id;
            detail.Qty = definition.Quantity;
            detail.CurrentSinglePrice = price;
            detail.VPP01_RequestHeaderId = definition.Id;
            detail.Description = "QA-001 deterministic request line";
            detail.CreateUserId = definition.Owner.UserId;
            detail.CreateDate = SeedTimestamp;
            detail.UpdateUserId = definition.Owner.UserId;
            detail.UpdateDate = SeedTimestamp;
            detail.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record GroupIds(
        Guid EmployeeGroupId,
        Guid ManagerGroupId,
        Guid ProcurementGroupId,
        Guid SystemAdminGroupId);

    private sealed record RequestDefinition(
        Guid Id,
        string Code,
        QaTestAccount Owner,
        int Quantity);
}
