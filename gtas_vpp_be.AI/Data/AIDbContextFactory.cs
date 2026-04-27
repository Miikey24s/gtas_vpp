using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace gtas_vpp_be.AI.Data;

public sealed class AIDbContextFactory : IDesignTimeDbContextFactory<AIDbContext>
{
    public AIDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AIDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=GTAS_VPP_TEST;Trusted_Connection=True;TrustServerCertificate=True;");

        return new AIDbContext(optionsBuilder.Options);
    }
}
