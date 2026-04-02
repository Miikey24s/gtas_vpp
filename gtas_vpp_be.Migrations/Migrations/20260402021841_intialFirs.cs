using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class intialFirs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "L01_Class",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClassModul = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_L01_Class", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "L03_VPPCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VPPCategoryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VPPCategoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_L03_VPPCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "L05_VPPSupplier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplierName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ward = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_L05_VPPSupplier", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LEX02_CompanyDepartmentLocation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LEX02Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LEX02Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LEX02Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEX02_CompanyDepartmentLocation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "P01_Page",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PageName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P01_Page", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "P02_Group",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P02_Group", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "P03_Component",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ComponentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P03_Component", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VPP01_RequestHeader",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VPPCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Y = table.Column<int>(type: "int", nullable: false),
                    M = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VPP01_RequestHeader", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "L02_ClassDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClassDetailCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClassDetailValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtraField1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtraField3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_L02_ClassDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_L02_ClassDetail_L01_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "L01_Class",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "P04_UserGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    P02_GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LEX02_CompanyDepartmentLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P04_UserGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_P04_UserGroup_LEX02_CompanyDepartmentLocation_LEX02_CompanyDepartmentLocationId",
                        column: x => x.LEX02_CompanyDepartmentLocationId,
                        principalTable: "LEX02_CompanyDepartmentLocation",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_P04_UserGroup_P02_Group_P02_GroupId",
                        column: x => x.P02_GroupId,
                        principalTable: "P02_Group",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "P05_PageComponentMapping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    P01_PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    P03_ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P05_PageComponentMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_P05_PageComponentMapping_P01_Page_P01_PageId",
                        column: x => x.P01_PageId,
                        principalTable: "P01_Page",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_P05_PageComponentMapping_P03_Component_P03_ComponentId",
                        column: x => x.P03_ComponentId,
                        principalTable: "P03_Component",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "L04_VPP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VPPCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VPPName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UOMId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VPPCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_L04_VPP", x => x.Id);
                    table.ForeignKey(
                        name: "FK_L04_VPP_L02_ClassDetail_UOMId",
                        column: x => x.UOMId,
                        principalTable: "L02_ClassDetail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_L04_VPP_L03_VPPCategory_VPPCategoryId",
                        column: x => x.VPPCategoryId,
                        principalTable: "L03_VPPCategory",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "P06_GroupPageComponentMapping",
                columns: table => new
                {
                    P05_PageComponentMappingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    P02_GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberCompanyCode = table.Column<long>(type: "bigint", nullable: false),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsEnable = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P06_GroupPageComponentMapping", x => new { x.P02_GroupId, x.P05_PageComponentMappingId, x.MemberCompanyCode });
                    table.ForeignKey(
                        name: "FK_P06_GroupPageComponentMapping_P02_Group_P02_GroupId",
                        column: x => x.P02_GroupId,
                        principalTable: "P02_Group",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_P06_GroupPageComponentMapping_P05_PageComponentMapping_P05_PageComponentMappingId",
                        column: x => x.P05_PageComponentMappingId,
                        principalTable: "P05_PageComponentMapping",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "L06_VPPSupplierMapping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    L04_VPPId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    L05_VPPSupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_L06_VPPSupplierMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_L06_VPPSupplierMapping_L04_VPP_L04_VPPId",
                        column: x => x.L04_VPPId,
                        principalTable: "L04_VPP",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_L06_VPPSupplierMapping_L05_VPPSupplier_L05_VPPSupplierId",
                        column: x => x.L05_VPPSupplierId,
                        principalTable: "L05_VPPSupplier",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VPP02_RequestDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VPPId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    CurrentSinglePrice = table.Column<long>(type: "bigint", nullable: false),
                    VPP01_RequestHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VPP02_RequestDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VPP02_RequestDetail_L04_VPP_VPPId",
                        column: x => x.VPPId,
                        principalTable: "L04_VPP",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VPP02_RequestDetail_VPP01_RequestHeader_VPP01_RequestHeaderId",
                        column: x => x.VPP01_RequestHeaderId,
                        principalTable: "VPP01_RequestHeader",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_L02_ClassDetail_ClassId",
                table: "L02_ClassDetail",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_L04_VPP_UOMId",
                table: "L04_VPP",
                column: "UOMId");

            migrationBuilder.CreateIndex(
                name: "IX_L04_VPP_VPPCategoryId",
                table: "L04_VPP",
                column: "VPPCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_L06_VPPSupplierMapping_L04_VPPId",
                table: "L06_VPPSupplierMapping",
                column: "L04_VPPId");

            migrationBuilder.CreateIndex(
                name: "IX_L06_VPPSupplierMapping_L05_VPPSupplierId",
                table: "L06_VPPSupplierMapping",
                column: "L05_VPPSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_P04_UserGroup_LEX02_CompanyDepartmentLocationId",
                table: "P04_UserGroup",
                column: "LEX02_CompanyDepartmentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_P04_UserGroup_P02_GroupId",
                table: "P04_UserGroup",
                column: "P02_GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_P05_PageComponentMapping_P01_PageId",
                table: "P05_PageComponentMapping",
                column: "P01_PageId");

            migrationBuilder.CreateIndex(
                name: "IX_P05_PageComponentMapping_P03_ComponentId",
                table: "P05_PageComponentMapping",
                column: "P03_ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_P06_GroupPageComponentMapping_P05_PageComponentMappingId",
                table: "P06_GroupPageComponentMapping",
                column: "P05_PageComponentMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_VPP02_RequestDetail_VPP01_RequestHeaderId",
                table: "VPP02_RequestDetail",
                column: "VPP01_RequestHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_VPP02_RequestDetail_VPPId",
                table: "VPP02_RequestDetail",
                column: "VPPId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "L06_VPPSupplierMapping");

            migrationBuilder.DropTable(
                name: "P04_UserGroup");

            migrationBuilder.DropTable(
                name: "P06_GroupPageComponentMapping");

            migrationBuilder.DropTable(
                name: "VPP02_RequestDetail");

            migrationBuilder.DropTable(
                name: "L05_VPPSupplier");

            migrationBuilder.DropTable(
                name: "LEX02_CompanyDepartmentLocation");

            migrationBuilder.DropTable(
                name: "P02_Group");

            migrationBuilder.DropTable(
                name: "P05_PageComponentMapping");

            migrationBuilder.DropTable(
                name: "L04_VPP");

            migrationBuilder.DropTable(
                name: "VPP01_RequestHeader");

            migrationBuilder.DropTable(
                name: "P01_Page");

            migrationBuilder.DropTable(
                name: "P03_Component");

            migrationBuilder.DropTable(
                name: "L02_ClassDetail");

            migrationBuilder.DropTable(
                name: "L03_VPPCategory");

            migrationBuilder.DropTable(
                name: "L01_Class");
        }
    }
}
