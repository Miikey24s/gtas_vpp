using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddVPP03_Log : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VPP03_Log",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    VPP01_RequestHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogJS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    LogTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VPP03_Log", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VPP03_Log_VPP01_RequestHeader_VPP01_RequestHeaderId",
                        column: x => x.VPP01_RequestHeaderId,
                        principalTable: "VPP01_RequestHeader",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VPP03_Log_VPP01_RequestHeaderId",
                table: "VPP03_Log",
                column: "VPP01_RequestHeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VPP03_Log");
        }
    }
}
