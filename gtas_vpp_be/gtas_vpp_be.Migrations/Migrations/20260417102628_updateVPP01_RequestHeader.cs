using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class updateVPP01_RequestHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentCode",
                table: "VPP01_RequestHeader",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MemberCompanyCode",
                table: "VPP01_RequestHeader",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "VPP01_RequestHeader",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedDate",
                table: "VPP01_RequestHeader",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentCode",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropColumn(
                name: "MemberCompanyCode",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropColumn(
                name: "SubmittedDate",
                table: "VPP01_RequestHeader");
        }
    }
}
