using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Model.VPP;

public static class SettlementModelConfiguration
{
    public static void ConfigureSettlementSnapshots(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Settlement>(en =>
        {
            en.Property(x => x.RowVersion).IsRowVersion();
            en.HasOne(x => x.Period).WithMany().HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Restrict);
            en.HasOne(x => x.SupersedesSettlement).WithMany()
                .HasForeignKey(x => x.SupersedesSettlementId).OnDelete(DeleteBehavior.Restrict);
            en.HasIndex(x => new { x.MemberCompanyCode, x.Year, x.Month, x.RevisionNumber })
                .HasDatabaseName("UX_Settlements_Revision").IsUnique();
            en.HasIndex(x => new { x.MemberCompanyCode, x.Year, x.Month })
                .HasDatabaseName("UX_Settlements_Current")
                .HasFilter("[IsDeleted] = 0 AND [IsCurrentRevision] = 1").IsUnique();
            en.HasIndex(x => new { x.MemberCompanyCode, x.IdempotencyKey })
                .HasDatabaseName("UX_Settlements_Idempotency")
                .HasFilter("[IsDeleted] = 0").IsUnique();
            en.ToTable(t => t.HasCheckConstraint("CK_Settlements_Period", "[Year] BETWEEN 1 AND 9999 AND [Month] BETWEEN 1 AND 12 AND [RevisionNumber] > 0"));
            en.ToTable(t => t.HasCheckConstraint("CK_Settlements_AmountsNonNegative", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [RebateAmount] >= 0 AND [FeeAmount] >= 0 AND [ShippingAmount] >= 0 AND [VatAmount] >= 0 AND [GrandTotal] >= 0"));
        });

        modelBuilder.Entity<SettlementItem>(en =>
        {
            en.HasOne(x => x.Settlement).WithMany(x => x.Items)
                .HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Restrict);
            en.HasIndex(x => new { x.SettlementId, x.VppId }).IsUnique();
            en.ToTable(t => t.HasCheckConstraint("CK_SettlementItems_AmountsNonNegative", "[Quantity] > 0 AND [NetUnitPrice] >= 0 AND [VatRate] >= 0 AND [VatRate] <= 100 AND [NetAmount] >= 0 AND [VatAmount] >= 0 AND [GrossAmount] >= 0"));
        });

        modelBuilder.Entity<SettlementCharge>(en =>
        {
            en.HasOne(x => x.Settlement).WithMany(x => x.Charges)
                .HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Restrict);
            en.HasIndex(x => new { x.SettlementId, x.ChargeType }).IsUnique();
        });

        modelBuilder.Entity<SettlementAllocation>(en =>
        {
            en.HasOne(x => x.Settlement).WithMany(x => x.Allocations)
                .HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Restrict);
            en.HasOne(x => x.SettlementItem).WithMany()
                .HasForeignKey(x => x.SettlementItemId).OnDelete(DeleteBehavior.Restrict);
            en.HasIndex(x => new { x.SettlementId, x.RequestDetailId }).IsUnique();
            en.HasIndex(x => new { x.SettlementId, x.DepartmentCode });
            en.ToTable(t => t.HasCheckConstraint("CK_SettlementAllocations_QuantityPositive", "[Quantity] > 0"));
        });
    }
}
