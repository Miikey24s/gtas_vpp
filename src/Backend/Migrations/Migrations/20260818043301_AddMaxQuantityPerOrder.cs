using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxQuantityPerOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxQuantityPerOrder",
                table: "VppItems",
                type: "int",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.Sql(
                """
                ;WITH OrderMax AS
                (
                    SELECT detail.[VppId], MAX(CAST(detail.[Qty] AS bigint)) AS [MaxQty]
                    FROM [RequestDetails] AS detail
                    INNER JOIN [Requests] AS requestHeader ON requestHeader.[Id] = detail.[RequestId]
                    WHERE detail.[IsDeleted] = 0
                      AND requestHeader.[IsDeleted] = 0
                      AND requestHeader.[IsCurrentRevision] = 1
                      AND requestHeader.[Status] NOT IN (4, 8)
                    GROUP BY detail.[VppId]
                )
                UPDATE item
                SET [MaxQuantityPerOrder] =
                    CASE
                        WHEN orderMax.[MaxQty] * 3 < 500 THEN 500
                        WHEN orderMax.[MaxQty] * 3 > 1000 THEN 1000
                        ELSE CAST(orderMax.[MaxQty] * 3 AS int)
                    END
                FROM [VppItems] AS item
                INNER JOIN OrderMax AS orderMax ON orderMax.[VppId] = item.[Id];
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VppItems_MaxQuantityPerOrder_Range",
                table: "VppItems",
                sql: "[MaxQuantityPerOrder] >= 1 AND [MaxQuantityPerOrder] <= 1000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_VppItems_MaxQuantityPerOrder_Range",
                table: "VppItems");

            migrationBuilder.DropColumn(
                name: "MaxQuantityPerOrder",
                table: "VppItems");
        }
    }
}
