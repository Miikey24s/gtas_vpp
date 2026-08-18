using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Model.Auth;

/// <summary>
/// Gom mapping bảo mật dùng chung để DbContext chạy ứng dụng và DbContext migration không bị lệch nhau.
/// </summary>
public static class TrustedAccessModelConfiguration
{
    /// <summary>
    /// Khóa các ràng buộc tài khoản, nhóm quyền, membership và audit tại cùng một nguồn cấu hình.
    /// </summary>
    public static void ConfigureTrustedAccess(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(x => x.Id).UseIdentityColumn(1_000_000_000, 1);
            entity.Property(x => x.FullName).HasMaxLength(250);
            entity.Property(x => x.EmployeeCode).HasMaxLength(50);
            entity.Property(x => x.AccountStatus).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.SessionVersion).HasDefaultValue(1L);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.NormalizedEmail)
                .HasDatabaseName("UX_AspNetUsers_NormalizedEmail")
                .HasFilter("[NormalizedEmail] IS NOT NULL")
                .IsUnique();
            entity.HasIndex(x => x.EmployeeCode)
                .HasDatabaseName("UX_AspNetUsers_EmployeeCode")
                .HasFilter("[EmployeeCode] IS NOT NULL")
                .IsUnique();
        });
        modelBuilder.Entity<PermissionGroup>(entity =>
        {
            entity.Property(x => x.GroupCode).HasMaxLength(50);
            entity.HasIndex(x => x.GroupCode)
                .HasDatabaseName("UX_PermissionGroups_GroupCode_Active")
                .HasFilter("[IsDeleted] = 0")
                .IsUnique();
        });
        modelBuilder.Entity<UserGroupMembership>(entity =>
        {
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.AccountId)
                .HasDatabaseName("UX_UserGroupMemberships_OneActivePerUser")
                .HasFilter("[IsDeleted] = 0 AND [AccountId] IS NOT NULL")
                .IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_UserGroupMemberships_ActivePrimaryDepartment",
                "[IsDeleted] = 1 OR ([AccountId] IS NOT NULL AND [UserId] = [AccountId] AND [DepartmentId] <> '00000000-0000-0000-0000-000000000000')"));
        });
        modelBuilder.Entity<SecurityAudit>(entity =>
        {
            entity.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            entity.Property(x => x.OccurredAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => new { x.TargetUserId, x.OccurredAtUtc })
                .HasDatabaseName("IX_SecurityAudits_TargetOccurredAt");
            entity.HasIndex(x => new { x.Action, x.OccurredAtUtc })
                .HasDatabaseName("IX_SecurityAudits_ActionOccurredAt");
        });
        modelBuilder.Entity<AuthBootstrapOperation>(entity =>
        {
            entity.Property(x => x.OperationKey).HasMaxLength(128);
            entity.Property(x => x.InputFingerprint).HasMaxLength(64);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
