using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDeleteBehaviorToRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_L02_ClassDetail_L01_Class_ClassId",
                table: "L02_ClassDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_L04_VPP_L02_ClassDetail_UOMId",
                table: "L04_VPP");

            migrationBuilder.DropForeignKey(
                name: "FK_L04_VPP_L03_VPPCategory_VPPCategoryId",
                table: "L04_VPP");

            migrationBuilder.DropForeignKey(
                name: "FK_L06_VPPSupplierMapping_L04_VPP_L04_VPPId",
                table: "L06_VPPSupplierMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_L06_VPPSupplierMapping_L05_VPPSupplier_L05_VPPSupplierId",
                table: "L06_VPPSupplierMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_P04_UserGroup_LEX02_CompanyDepartmentLocation_LEX02_CompanyDepartmentLocationId",
                table: "P04_UserGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_P04_UserGroup_P02_Group_P02_GroupId",
                table: "P04_UserGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_P05_PageComponentMapping_P01_Page_P01_PageId",
                table: "P05_PageComponentMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_P05_PageComponentMapping_P03_Component_P03_ComponentId",
                table: "P05_PageComponentMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_P06_GroupPageComponentMapping_P02_Group_P02_GroupId",
                table: "P06_GroupPageComponentMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_P06_GroupPageComponentMapping_P05_PageComponentMapping_P05_PageComponentMappingId",
                table: "P06_GroupPageComponentMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_VPP02_RequestDetail_VPP01_RequestHeader_VPP01_RequestHeaderId",
                table: "VPP02_RequestDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_VPP03_Log_VPP01_RequestHeader_VPP01_RequestHeaderId",
                table: "VPP03_Log");

            migrationBuilder.AddForeignKey(
                name: "FK_L02_ClassDetail_L01_Class_ClassId",
                table: "L02_ClassDetail",
                column: "ClassId",
                principalTable: "L01_Class",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L04_VPP_L02_ClassDetail_UOMId",
                table: "L04_VPP",
                column: "UOMId",
                principalTable: "L02_ClassDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L04_VPP_L03_VPPCategory_VPPCategoryId",
                table: "L04_VPP",
                column: "VPPCategoryId",
                principalTable: "L03_VPPCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L06_VPPSupplierMapping_L04_VPP_L04_VPPId",
                table: "L06_VPPSupplierMapping",
                column: "L04_VPPId",
                principalTable: "L04_VPP",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L06_VPPSupplierMapping_L05_VPPSupplier_L05_VPPSupplierId",
                table: "L06_VPPSupplierMapping",
                column: "L05_VPPSupplierId",
                principalTable: "L05_VPPSupplier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_P04_UserGroup_LEX02_CompanyDepartmentLocation_LEX02_CompanyDepartmentLocationId",
                table: "P04_UserGroup",
                column: "LEX02_CompanyDepartmentLocationId",
                principalTable: "LEX02_CompanyDepartmentLocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_P04_UserGroup_P02_Group_P02_GroupId",
                table: "P04_UserGroup",
                column: "P02_GroupId",
                principalTable: "P02_Group",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_P05_PageComponentMapping_P01_Page_P01_PageId",
                table: "P05_PageComponentMapping",
                column: "P01_PageId",
                principalTable: "P01_Page",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_P05_PageComponentMapping_P03_Component_P03_ComponentId",
                table: "P05_PageComponentMapping",
                column: "P03_ComponentId",
                principalTable: "P03_Component",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_P06_GroupPageComponentMapping_P02_Group_P02_GroupId",
                table: "P06_GroupPageComponentMapping",
                column: "P02_GroupId",
                principalTable: "P02_Group",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_P06_GroupPageComponentMapping_P05_PageComponentMapping_P05_PageComponentMappingId",
                table: "P06_GroupPageComponentMapping",
                column: "P05_PageComponentMappingId",
                principalTable: "P05_PageComponentMapping",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VPP02_RequestDetail_VPP01_RequestHeader_VPP01_RequestHeaderId",
                table: "VPP02_RequestDetail",
                column: "VPP01_RequestHeaderId",
                principalTable: "VPP01_RequestHeader",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VPP03_Log_VPP01_RequestHeader_VPP01_RequestHeaderId",
                table: "VPP03_Log",
                column: "VPP01_RequestHeaderId",
                principalTable: "VPP01_RequestHeader",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_L02_ClassDetail_L01_Class_ClassId",
                table: "L02_ClassDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_L04_VPP_L02_ClassDetail_UOMId",
                table: "L04_VPP");

            migrationBuilder.DropForeignKey(
                name: "FK_L04_VPP_L03_VPPCategory_VPPCategoryId",
                table: "L04_VPP");

            migrationBuilder.DropForeignKey(
                name: "FK_L06_VPPSupplierMapping_L04_VPP_L04_VPPId",
                table: "L06_VPPSupplierMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_L06_VPPSupplierMapping_L05_VPPSupplier_L05_VPPSupplierId",
                table: "L06_VPPSupplierMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_P04_UserGroup_LEX02_CompanyDepartmentLocation_LEX02_CompanyDepartmentLocationId",
                table: "P04_UserGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_P04_UserGroup_P02_Group_P02_GroupId",
                table: "P04_UserGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_P05_PageComponentMapping_P01_Page_P01_PageId",
                table: "P05_PageComponentMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_P05_PageComponentMapping_P03_Component_P03_ComponentId",
                table: "P05_PageComponentMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_P06_GroupPageComponentMapping_P02_Group_P02_GroupId",
                table: "P06_GroupPageComponentMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_P06_GroupPageComponentMapping_P05_PageComponentMapping_P05_PageComponentMappingId",
                table: "P06_GroupPageComponentMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_VPP02_RequestDetail_VPP01_RequestHeader_VPP01_RequestHeaderId",
                table: "VPP02_RequestDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_VPP03_Log_VPP01_RequestHeader_VPP01_RequestHeaderId",
                table: "VPP03_Log");

            migrationBuilder.AddForeignKey(
                name: "FK_L02_ClassDetail_L01_Class_ClassId",
                table: "L02_ClassDetail",
                column: "ClassId",
                principalTable: "L01_Class",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_L04_VPP_L02_ClassDetail_UOMId",
                table: "L04_VPP",
                column: "UOMId",
                principalTable: "L02_ClassDetail",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_L04_VPP_L03_VPPCategory_VPPCategoryId",
                table: "L04_VPP",
                column: "VPPCategoryId",
                principalTable: "L03_VPPCategory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_L06_VPPSupplierMapping_L04_VPP_L04_VPPId",
                table: "L06_VPPSupplierMapping",
                column: "L04_VPPId",
                principalTable: "L04_VPP",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_L06_VPPSupplierMapping_L05_VPPSupplier_L05_VPPSupplierId",
                table: "L06_VPPSupplierMapping",
                column: "L05_VPPSupplierId",
                principalTable: "L05_VPPSupplier",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_P04_UserGroup_LEX02_CompanyDepartmentLocation_LEX02_CompanyDepartmentLocationId",
                table: "P04_UserGroup",
                column: "LEX02_CompanyDepartmentLocationId",
                principalTable: "LEX02_CompanyDepartmentLocation",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_P04_UserGroup_P02_Group_P02_GroupId",
                table: "P04_UserGroup",
                column: "P02_GroupId",
                principalTable: "P02_Group",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_P05_PageComponentMapping_P01_Page_P01_PageId",
                table: "P05_PageComponentMapping",
                column: "P01_PageId",
                principalTable: "P01_Page",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_P05_PageComponentMapping_P03_Component_P03_ComponentId",
                table: "P05_PageComponentMapping",
                column: "P03_ComponentId",
                principalTable: "P03_Component",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_P06_GroupPageComponentMapping_P02_Group_P02_GroupId",
                table: "P06_GroupPageComponentMapping",
                column: "P02_GroupId",
                principalTable: "P02_Group",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_P06_GroupPageComponentMapping_P05_PageComponentMapping_P05_PageComponentMappingId",
                table: "P06_GroupPageComponentMapping",
                column: "P05_PageComponentMappingId",
                principalTable: "P05_PageComponentMapping",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VPP02_RequestDetail_VPP01_RequestHeader_VPP01_RequestHeaderId",
                table: "VPP02_RequestDetail",
                column: "VPP01_RequestHeaderId",
                principalTable: "VPP01_RequestHeader",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VPP03_Log_VPP01_RequestHeader_VPP01_RequestHeaderId",
                table: "VPP03_Log",
                column: "VPP01_RequestHeaderId",
                principalTable: "VPP01_RequestHeader",
                principalColumn: "Id");
        }
    }
}
