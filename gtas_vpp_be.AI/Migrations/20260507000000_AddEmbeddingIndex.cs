using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.AI.Migrations
{
    /// <inheritdoc />
    public partial class AddEmbeddingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AI_VPPEmbedding_VectorDimension_ModelName",
                table: "AI_VPPEmbedding",
                columns: new[] { "VectorDimension", "ModelName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AI_VPPEmbedding_VectorDimension_ModelName",
                table: "AI_VPPEmbedding");
        }
    }
}
