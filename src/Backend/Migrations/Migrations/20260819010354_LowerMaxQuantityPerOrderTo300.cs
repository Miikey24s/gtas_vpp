using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class LowerMaxQuantityPerOrderTo300 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_VppItems_MaxQuantityPerOrder_Range",
                table: "VppItems");

            // Giữ các mức quản trị đã đặt trong khoảng mới; chỉ hạ giá trị vượt trần.
            // Đơn đã gửi và chi tiết lịch sử không bị sửa bởi migration này.
            migrationBuilder.Sql(
                """
                UPDATE [VppItems]
                SET [MaxQuantityPerOrder] = 300
                WHERE [MaxQuantityPerOrder] > 300;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "MaxQuantityPerOrder",
                table: "VppItems",
                type: "int",
                nullable: false,
                defaultValue: 300,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1000);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VppItems_MaxQuantityPerOrder_Range",
                table: "VppItems",
                sql: "[MaxQuantityPerOrder] >= 1 AND [MaxQuantityPerOrder] <= 300");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback mở lại khoảng cấu hình cũ nhưng không đoán lại giá trị trước khi bị hạ.
            migrationBuilder.DropCheckConstraint(
                name: "CK_VppItems_MaxQuantityPerOrder_Range",
                table: "VppItems");

            migrationBuilder.AlterColumn<int>(
                name: "MaxQuantityPerOrder",
                table: "VppItems",
                type: "int",
                nullable: false,
                defaultValue: 1000,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 300);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VppItems_MaxQuantityPerOrder_Range",
                table: "VppItems",
                sql: "[MaxQuantityPerOrder] >= 1 AND [MaxQuantityPerOrder] <= 1000");
        }
    }
}
