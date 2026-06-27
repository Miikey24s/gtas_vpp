using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalColumns_VPP01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "VPP01_RequestHeader",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "VPP01_RequestHeader",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "VPP01_RequestHeader",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "VPP01_RequestHeader",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedById",
                table: "VPP01_RequestHeader",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropColumn(
                name: "RejectedById",
                table: "VPP01_RequestHeader");
        }
    }
}
