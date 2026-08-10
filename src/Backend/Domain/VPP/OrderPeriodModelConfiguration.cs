using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Model.VPP;

public static class OrderPeriodModelConfiguration
{
    public static void ConfigureOrderPeriodSettings(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VppOrderPeriodSettingsVersion>(entity =>
        {
            entity.Property(x => x.MemberCompanyCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.LocalTimeOfDay).HasColumnType("time");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.MemberCompanyCode, x.VersionNumber })
                .HasDatabaseName("UX_OrderPeriodSettings_Company_Version")
                .HasFilter("[IsDeleted] = 0")
                .IsUnique();
            entity.HasIndex(x => new
            {
                x.MemberCompanyCode,
                x.EffectiveFromYear,
                x.EffectiveFromMonth,
                x.VersionNumber
            })
                .HasDatabaseName("IX_OrderPeriodSettings_Company_Effective")
                .HasFilter("[IsDeleted] = 0");
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_OrderPeriodSettings_Ranges",
                "[VersionNumber] > 0 " +
                "AND [DefaultOpenPeriodCount] BETWEEN 0 AND 12 " +
                "AND [DefaultNewPeriodOpenDay] BETWEEN 1 AND 31 " +
                "AND [DefaultPeriodCloseDay] BETWEEN 1 AND 31 " +
                "AND [SupplementApprovalGraceDays] BETWEEN 0 AND 31 " +
                "AND [PostCloseAdjustmentDays] BETWEEN 0 AND 31 " +
                "AND [SupplementApprovalGraceDays] <= [PostCloseAdjustmentDays] " +
                "AND [SettlementReopenWindowDays] BETWEEN 0 AND 30 " +
                "AND [EffectiveFromYear] BETWEEN 1 AND 9999 " +
                "AND [EffectiveFromMonth] BETWEEN 1 AND 12"));
        });

        modelBuilder.Entity<VppPeriod>(entity =>
        {
            entity.HasOne(x => x.SettingsVersion)
                .WithMany(x => x.Periods)
                .HasForeignKey(x => x.SettingsVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.SettingsVersionId)
                .HasDatabaseName("IX_Periods_SettingsVersionId");
        });
    }
}
