using gtas_vpp_be.Model;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    [DbContext(typeof(VPPMigrationDbContext))]
    [Migration("20260511163500_AddUniqueIndex_VPP01_OneRegularPerUserPeriod")]
    public partial class AddUniqueIndex_VPP01_OneRegularPerUserPeriod : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX UX_VPP01_OneRegularPerUserPeriod
ON VPP01_RequestHeader (CreateUserId, Y, M)
WHERE IsDeleted = 0 AND IsAdditionalOrder = 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX UX_VPP01_OneRegularPerUserPeriod
ON VPP01_RequestHeader;");
        }
    }
}
