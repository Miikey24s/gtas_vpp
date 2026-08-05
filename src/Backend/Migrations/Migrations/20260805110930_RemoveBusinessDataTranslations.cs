using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBusinessDataTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Không âm thầm xóa nội dung đã nhập ở môi trường khác. Nếu có dữ liệu
            // dịch thật, quản trị viên phải sao lưu và xử lý riêng trước khi migrate.
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [DepartmentTranslations])
                    OR EXISTS (SELECT 1 FROM [LookupCategoryTranslations])
                    OR EXISTS (SELECT 1 FROM [LookupValueTranslations])
                    OR EXISTS (SELECT 1 FROM [PriceListTranslations])
                    OR EXISTS (SELECT 1 FROM [SupplierTranslations])
                    OR EXISTS (SELECT 1 FROM [VppCategoryTranslations])
                    OR EXISTS (SELECT 1 FROM [VppItemTranslations])
                BEGIN
                    THROW 51000, 'Business-data translation tables are not empty. Back up and clear them before applying this migration.', 1;
                END;

                IF EXISTS (SELECT 1 FROM [Departments] WHERE [OriginalLanguageCode] <> N'vi')
                    OR EXISTS (SELECT 1 FROM [LookupCategories] WHERE [OriginalLanguageCode] <> N'vi')
                    OR EXISTS (SELECT 1 FROM [LookupValues] WHERE [OriginalLanguageCode] <> N'vi')
                    OR EXISTS (SELECT 1 FROM [PriceLists] WHERE [OriginalLanguageCode] <> N'vi')
                    OR EXISTS (SELECT 1 FROM [Suppliers] WHERE [OriginalLanguageCode] <> N'vi')
                    OR EXISTS (SELECT 1 FROM [VppCategories] WHERE [OriginalLanguageCode] <> N'vi')
                    OR EXISTS (SELECT 1 FROM [VppItems] WHERE [OriginalLanguageCode] <> N'vi')
                BEGIN
                    THROW 51001, 'Non-default original language metadata exists. Review it before applying this migration.', 1;
                END;
                """);

            migrationBuilder.DropTable(
                name: "DepartmentTranslations");

            migrationBuilder.DropTable(
                name: "LookupCategoryTranslations");

            migrationBuilder.DropTable(
                name: "LookupValueTranslations");

            migrationBuilder.DropTable(
                name: "PriceListTranslations");

            migrationBuilder.DropTable(
                name: "SupplierTranslations");

            migrationBuilder.DropTable(
                name: "VppCategoryTranslations");

            migrationBuilder.DropTable(
                name: "VppItemTranslations");

            migrationBuilder.DropColumn(
                name: "OriginalLanguageCode",
                table: "VppItems");

            migrationBuilder.DropColumn(
                name: "OriginalLanguageCode",
                table: "VppCategories");

            migrationBuilder.DropColumn(
                name: "OriginalLanguageCode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OriginalLanguageCode",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "OriginalLanguageCode",
                table: "LookupValues");

            migrationBuilder.DropColumn(
                name: "OriginalLanguageCode",
                table: "LookupCategories");

            migrationBuilder.DropColumn(
                name: "OriginalLanguageCode",
                table: "Departments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguageCode",
                table: "VppItems",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "vi");

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguageCode",
                table: "VppCategories",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "vi");

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguageCode",
                table: "Suppliers",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "vi");

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguageCode",
                table: "PriceLists",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "vi");

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguageCode",
                table: "LookupValues",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "vi");

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguageCode",
                table: "LookupCategories",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "vi");

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguageCode",
                table: "Departments",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "vi");

            migrationBuilder.CreateTable(
                name: "DepartmentTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentTranslations_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LookupCategoryTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    LookupCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCategoryTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookupCategoryTranslations_LookupCategories_LookupCategoryId",
                        column: x => x.LookupCategoryId,
                        principalTable: "LookupCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LookupValueTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    LookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupValueTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookupValueTranslations_LookupValues_LookupValueId",
                        column: x => x.LookupValueId,
                        principalTable: "LookupValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceListTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListTranslations_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierTranslations_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VppCategoryTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    VppCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VppCategoryTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VppCategoryTranslations_VppCategories_VppCategoryId",
                        column: x => x.VppCategoryId,
                        principalTable: "VppCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VppItemTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    VppItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VppItemTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VppItemTranslations_VppItems_VppItemId",
                        column: x => x.VppItemId,
                        principalTable: "VppItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_DepartmentTranslations_EntityLanguage",
                table: "DepartmentTranslations",
                columns: new[] { "DepartmentId", "LanguageCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_LookupCategoryTranslations_EntityLanguage",
                table: "LookupCategoryTranslations",
                columns: new[] { "LookupCategoryId", "LanguageCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_LookupValueTranslations_EntityLanguage",
                table: "LookupValueTranslations",
                columns: new[] { "LookupValueId", "LanguageCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_PriceListTranslations_EntityLanguage",
                table: "PriceListTranslations",
                columns: new[] { "PriceListId", "LanguageCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_SupplierTranslations_EntityLanguage",
                table: "SupplierTranslations",
                columns: new[] { "SupplierId", "LanguageCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_VppCategoryTranslations_EntityLanguage",
                table: "VppCategoryTranslations",
                columns: new[] { "VppCategoryId", "LanguageCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_VppItemTranslations_EntityLanguage",
                table: "VppItemTranslations",
                columns: new[] { "VppItemId", "LanguageCode" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
