using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.AI.Migrations
{
    /// <inheritdoc />
    public partial class AddAIVPPEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AI_VPPEmbedding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    VPPId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmbeddingText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmbeddingVector = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VectorDimension = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_VPPEmbedding", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AI_VPPEmbedding_L04_VPP_VPPId",
                        column: x => x.VPPId,
                        principalTable: "L04_VPP",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AI_VPPEmbedding_VPPId",
                table: "AI_VPPEmbedding",
                column: "VPPId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AI_VPPEmbedding");
        }
    }
}
