using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Model.VPP;

public static class PostSettlementOrderCorrectionModelConfiguration
{
    public static void ConfigurePostSettlementOrderCorrections(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostSettlementOrderCorrection>(entity =>
        {
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne(x => x.Period).WithMany().HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Settlement).WithMany().HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Items).WithOne(x => x.Correction)
                .HasForeignKey(x => x.CorrectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.MemberCompanyCode, x.RequestSeriesId })
                .HasDatabaseName("UX_PostSettlementCorrections_PendingSeries")
                .HasFilter("[IsDeleted] = 0 AND [Status] = 0")
                .IsUnique();
            entity.HasIndex(x => new { x.MemberCompanyCode, x.PeriodId, x.Status });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_PostSettlementCorrections_State",
                "[Action] IN (0, 1) AND [Status] IN (0, 1, 2, 3) AND [RequestRevisionNumber] > 0"));
        });

        modelBuilder.Entity<PostSettlementOrderCorrectionItem>(entity =>
        {
            entity.HasIndex(x => new { x.CorrectionId, x.VppId }).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_PostSettlementCorrectionItems_Qty",
                "[Qty] > 0"));
        });
    }
}
