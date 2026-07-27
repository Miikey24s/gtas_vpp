using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustedAccessIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "P04_UserGroup",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "P04_UserGroup",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "GroupCode",
                table: "P02_Group",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                DECLARE @ReservedIdentityFloor int = 1000000000;
                DECLARE @HasReservedIdCollision bit = 0;
                DECLARE @CollisionSql nvarchar(max);

                SELECT @CollisionSql = STRING_AGG(
                    CONVERT(nvarchar(max),
                        N'IF EXISTS (SELECT 1 FROM '
                        + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                        + N' WHERE ' + QUOTENAME(c.name) + N' >= @Floor) SET @Found = 1;'),
                    CHAR(10))
                FROM sys.tables t
                JOIN sys.columns c ON c.object_id = t.object_id
                JOIN sys.types ty ON ty.user_type_id = c.user_type_id
                WHERE ty.name = N'int'
                  AND (c.name = N'UserID' OR c.name LIKE N'%UserId');

                IF NULLIF(@CollisionSql, N'') IS NOT NULL
                BEGIN
                    EXEC sys.sp_executesql
                        @CollisionSql,
                        N'@Floor int, @Found bit OUTPUT',
                        @Floor = @ReservedIdentityFloor,
                        @Found = @HasReservedIdCollision OUTPUT;
                END;

                IF DB_ID(N'GTAS_MENU') IS NOT NULL
                   AND OBJECT_ID(N'GTAS_MENU.dbo.tblUsers', N'U') IS NOT NULL
                BEGIN
                    EXEC sys.sp_executesql
                        N'IF EXISTS (SELECT 1 FROM GTAS_MENU.dbo.tblUsers WHERE UserID >= @Floor) SET @Found = 1;',
                        N'@Floor int, @Found bit OUTPUT',
                        @Floor = @ReservedIdentityFloor,
                        @Found = @HasReservedIdCollision OUTPUT;
                END;

                IF @HasReservedIdCollision = 1
                BEGIN
                    THROW 51000,
                        'AUTH_IDENTITY_ID_RANGE_COLLISION: an existing user/actor ID occupies the reserved app-owned range.',
                        1;
                END;

                IF EXISTS (SELECT 1 FROM dbo.P04_UserGroup WHERE IsDeleted = 0)
                BEGIN
                    THROW 51000,
                        'AUTH_ACTIVE_LEGACY_MEMBERSHIP: contain or explicitly map every active legacy membership before this migration.',
                        1;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.P02_Group
                    WHERE IsDeleted = 0
                      AND Id NOT IN
                      (
                          '388C6C3A-2801-42DC-BFC0-8A7741264596',
                          '5823B49B-5925-4A89-846A-09063A36040C'
                      )
                )
                BEGIN
                    THROW 51000,
                        'AUTH_UNKNOWN_ACTIVE_GROUP: resolve non-canonical active groups before the flat RBAC cutover.',
                        1;
                END;

                UPDATE dbo.P02_Group
                SET GroupCode = CASE Id
                        WHEN '388C6C3A-2801-42DC-BFC0-8A7741264596' THEN N'EMPLOYEE'
                        WHEN '5823B49B-5925-4A89-846A-09063A36040C' THEN N'SYSTEM_ADMIN'
                        ELSE N'LEGACY_' + REPLACE(CONVERT(nvarchar(36), Id), N'-', N'')
                    END,
                    ParentGroupId = NULL
                WHERE GroupCode IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "GroupCode",
                table: "P02_Group",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "A01_SecurityAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_A01_SecurityAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1000000000, 1"),
                    FullName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MemberCompanyCode = table.Column<long>(type: "bigint", nullable: false),
                    AccountStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    SessionVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisabledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "A02_AuthBootstrapOperation",
                columns: table => new
                {
                    OperationKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InputFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_A02_AuthBootstrapOperation", x => x.OperationKey);
                    table.ForeignKey(
                        name: "FK_A02_AuthBootstrapOperation_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_P04_UserGroup_OneActivePerUser",
                table: "P04_UserGroup",
                column: "AccountId",
                unique: true,
                filter: "[IsDeleted] = 0 AND [AccountId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_P04_UserGroup_ActivePrimaryDepartment",
                table: "P04_UserGroup",
                sql: "[IsDeleted] = 1 OR ([AccountId] IS NOT NULL AND [UserId] = [AccountId] AND [LEX02_CompanyDepartmentLocationId] <> '00000000-0000-0000-0000-000000000000')");

            migrationBuilder.CreateIndex(
                name: "UX_P02_Group_GroupCode_Active",
                table: "P02_Group",
                column: "GroupCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_A01_Action_Occurred",
                table: "A01_SecurityAudit",
                columns: new[] { "Action", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_A01_Target_Occurred",
                table: "A01_SecurityAudit",
                columns: new[] { "TargetUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_A02_AuthBootstrapOperation_AccountId",
                table: "A02_AuthBootstrapOperation",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AspNetUsers_EmployeeCode",
                table: "AspNetUsers",
                column: "EmployeeCode",
                unique: true,
                filter: "[EmployeeCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AspNetUsers_NormalizedEmail",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_P04_UserGroup_AspNetUsers_AccountId",
                table: "P04_UserGroup",
                column: "AccountId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.AspNetUsers)
                   OR EXISTS (SELECT 1 FROM dbo.A01_SecurityAudit)
                   OR EXISTS (SELECT 1 FROM dbo.A02_AuthBootstrapOperation)
                BEGIN
                    THROW 51000,
                        'AUTH_DESTRUCTIVE_DOWN_BLOCKED: roll forward after identity/audit data exists.',
                        1;
                END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_P04_UserGroup_AspNetUsers_AccountId",
                table: "P04_UserGroup");

            migrationBuilder.DropTable(
                name: "A01_SecurityAudit");

            migrationBuilder.DropTable(
                name: "A02_AuthBootstrapOperation");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "UX_P04_UserGroup_OneActivePerUser",
                table: "P04_UserGroup");

            migrationBuilder.DropCheckConstraint(
                name: "CK_P04_UserGroup_ActivePrimaryDepartment",
                table: "P04_UserGroup");

            migrationBuilder.DropIndex(
                name: "UX_P02_Group_GroupCode_Active",
                table: "P02_Group");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "P04_UserGroup");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "P04_UserGroup");

            migrationBuilder.DropColumn(
                name: "GroupCode",
                table: "P02_Group");
        }
    }
}
