using gtas_vpp_be.Model.AI;
using gtas_vpp_be.Model.Library;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.AI.Data;

public class AIDbContext : DbContext
{
    public DbSet<AI_VPPEmbedding> VPPEmbeddings => Set<AI_VPPEmbedding>();

    public AIDbContext(DbContextOptions<AIDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<L04_VPP>(e =>
        {
            e.ToTable("L04_VPP", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Ignore(x => x.UOM);
            e.Ignore(x => x.VPPCategory);
            e.Ignore(x => x.L06_VPPSupplierMappings);
        });

        modelBuilder.Entity<AI_VPPEmbedding>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.CreatedDate).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.UpdatedDate).HasDefaultValueSql("GETDATE()");

            e.HasIndex(x => x.VPPId).IsUnique();

            e.HasOne(x => x.VPP)
                .WithMany()
                .HasForeignKey(x => x.VPPId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
