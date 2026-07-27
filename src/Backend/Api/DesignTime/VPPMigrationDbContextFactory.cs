using gtas_vpp_be.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace gtas_vpp_be.DesignTime;

/// <summary>
/// Sinh migration mà không nạp runtime secret hoặc kết nối database.
/// EF chỉ dùng connection string này để chọn SQL provider.
/// </summary>
public sealed class VPPMigrationDbContextFactory : IDesignTimeDbContextFactory<VPPMigrationDbContext>
{
    public VPPMigrationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VPPMigrationDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=gtas_vpp_design;Trusted_Connection=True",
                sql => sql.MigrationsAssembly("gtas_vpp_be.Migrations"))
            .Options;

        return new VPPMigrationDbContext(options);
    }
}
