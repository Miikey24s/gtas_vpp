using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Model.VPP;

public static class SettlementModelConfiguration
{
    public static void ConfigureSettlementSnapshots(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VPP04_Settlement>(en =>
        {
            en.Property(x => x.RowVersion).IsRowVersion();
            en.HasOne(x => x.Period).WithMany().HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Restrict);
            en.HasOne(x => x.SupersedesSettlement).WithMany()
                .HasForeignKey(x => x.SupersedesSettlementId).OnDelete(DeleteBehavior.Restrict);
            en.HasIndex(x => new { x.MemberCompanyCode, x.Y, x.M, x.RevisionNumber })
                .HasDatabaseName("UX_VPP04_Settlement_Revision").IsUnique();
            en.HasIndex(x => new { x.MemberCompanyCode, x.Y, x.M })
                .HasDatabaseName("UX_VPP04_Settlement_Current")
                .HasFilter("[IsDeleted] = 0 AND [IsCurrentRevision] = 1").IsUnique();
            en.HasIndex(x => new { x.MemberCompanyCode, x.IdempotencyKey })
                .HasDatabaseName("UX_VPP04_Settlement_Idempotency")
                .HasFilter("[IsDeleted] = 0").IsUnique();
            en.ToTable(t => t.HasCheckConstraint("CK_VPP04_Settlement_Period", "[Y] BETWEEN 1 AND 9999 AND [M] BETWEEN 1 AND 12 AND [RevisionNumber] > 0"));
            en.ToTable(t => t.HasCheckConstraint("CK_VPP04_Settlement_Amounts", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [RebateAmount] >= 0 AND [FeeAmount] >= 0 AND [ShippingAmount] >= 0 AND [VatAmount] >= 0 AND [GrandTotal] >= 0"));
        });

        modelBuilder.Entity<VPP05_SettlementItem>(en =>
        {
            en.HasOne(x => x.Settlement).WithMany(x => x.Items)
                .HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Restrict);
            en.HasIndex(x => new { x.SettlementId, x.VppId }).IsUnique();
            en.ToTable(t => t.HasCheckConstraint("CK_VPP05_SettlementItem_Amounts", "[Quantity] > 0 AND [NetUnitPrice] >= 0 AND [VatRate] >= 0 AND [VatRate] <= 100 AND [NetAmount] >= 0 AND [VatAmount] >= 0 AND [GrossAmount] >= 0"));
        });

        modelBuilder.Entity<VPP06_SettlementCharge>(en =>
        {
            en.HasOne(x => x.Settlement).WithMany(x => x.Charges)
                .HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Restrict);
            en.HasIndex(x => new { x.SettlementId, x.ChargeType }).IsUnique();
        });

        modelBuilder.Entity<VPP07_SettlementAllocation>(en =>
        {
            en.HasOne(x => x.Settlement).WithMany(x => x.Allocations)
                .HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Restrict);
            en.HasOne(x => x.SettlementItem).WithMany()
                .HasForeignKey(x => x.SettlementItemId).OnDelete(DeleteBehavior.Restrict);
            en.HasIndex(x => new { x.SettlementId, x.RequestDetailId }).IsUnique();
            en.HasIndex(x => new { x.SettlementId, x.DepartmentCode });
            en.ToTable(t => t.HasCheckConstraint("CK_VPP07_SettlementAllocation_Quantity", "[Quantity] > 0"));
        });
    }
}
