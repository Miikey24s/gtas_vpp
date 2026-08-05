using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.Notifications;
using gtas_vpp_be.Model.VPP;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace gtas_vpp_be.Model
{
    public class VPPMigrationDbContext : IdentityUserContext<AppUser, int>
    {
        #region Auth
        public virtual DbSet<PermissionPage> PermissionPages { get; set; }
        public virtual DbSet<PermissionGroup> PermissionGroups { get; set; }
        public virtual DbSet<PermissionComponent> PermissionComponents { get; set; }
        public virtual DbSet<UserGroupMembership> UserGroupMemberships { get; set; }
        public virtual DbSet<PageComponentMapping> PageComponentMappings { get; set; }
        public virtual DbSet<GroupPageComponentMapping> GroupPageComponentMappings { get; set; }
        public virtual DbSet<SecurityAudit> SecurityAudits { get; set; }
        public virtual DbSet<AuthBootstrapOperation> AuthBootstrapOperations { get; set; }
        #endregion

        #region Library
        public virtual DbSet<LookupCategory> LookupCategories { get; set; }
        public virtual DbSet<LookupValue> LookupValues { get; set; }
        public virtual DbSet<VppCategory> VppCategories { get; set; }
        public virtual DbSet<VppItem> VppItems { get; set; }
        public virtual DbSet<Supplier> Suppliers { get; set; }
        public virtual DbSet<SupplierProductMapping> SupplierProductMappings { get; set; }
        public virtual DbSet<PriceList> PriceLists { get; set; }
        public virtual DbSet<Department> Departments { get; set; }
        #endregion

        #region Data
        public virtual DbSet<VppPeriod> Periods { get; set; }
        public virtual DbSet<VppRequest> Requests { get; set; }
        public virtual DbSet<VppRequestDetail> RequestDetails { get; set; }
        public virtual DbSet<RequestLog> RequestLogs { get; set; }
        public virtual DbSet<Settlement> Settlements { get; set; }
        public virtual DbSet<SettlementItem> SettlementItems { get; set; }
        public virtual DbSet<SettlementCharge> SettlementCharges { get; set; }
        public virtual DbSet<SettlementAllocation> SettlementAllocations { get; set; }
        #endregion

        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<EmailOutboxMessage> EmailOutboxMessages { get; set; }

        public VPPMigrationDbContext(DbContextOptions<VPPMigrationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureTrustedAccess(modelBuilder);
            modelBuilder.Entity<PageComponentMapping>(en =>
            {
                en.HasKey(x => x.Id);
                en.HasOne(x => x.PermissionComponent).WithMany(x => x.PageComponentMappings).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.PermissionPage).WithMany(x => x.PageComponentMappings).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<UserGroupMembership>(en =>
            {
                en.HasOne(x => x.PermissionGroup).WithMany(x => x.UserGroupMemberships).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.Department).WithMany(x => x.UserGroupMemberships).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Department>(en =>
            {
                en.Property(x => x.Code).HasMaxLength(50).IsRequired();
                en.Property(x => x.Name).HasMaxLength(200).IsRequired();
                en.HasOne(x => x.ParentDepartment).WithMany(x => x.ChildDepartments)
                    .HasForeignKey(x => x.ParentDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
                en.HasIndex(x => x.Code)
                    .HasDatabaseName("UX_Departments_Code_Active")
                    .HasFilter("[IsDeleted] = 0")
                    .IsUnique();
            });
            modelBuilder.Entity<GroupPageComponentMapping>(en =>
            {
                en.HasKey(x => new { x.PermissionGroupId, x.PageComponentMappingId, x.MemberCompanyCode });
                en.HasOne(x => x.PageComponentMapping).WithMany(x => x.GroupPageComponentMappings).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.PermissionGroup).WithMany(x => x.GroupPageComponentMappings).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<LookupValue>(en =>
            {
                en.HasOne(x => x.Category).WithMany(x => x.LookupValues).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<LookupCategory>(en =>
            {
            });
            modelBuilder.Entity<VppItem>(en =>
            {
                en.Property(x => x.VppCode).HasMaxLength(64).IsRequired();
                en.Property(x => x.VppName).HasMaxLength(250).IsRequired();
                en.HasIndex(x => x.VppCode)
                    .HasDatabaseName("UX_VppItems_VppCode")
                    .IsUnique();
                en.HasIndex(x => new { x.IsDeleted, x.VppCategoryId, x.VppCode })
                    .HasDatabaseName("IX_VppItems_ActiveCategoryCode")
                    .IncludeProperties(x => new { x.VppName, x.UomId });
                en.HasOne(x => x.Uom).WithMany(x => x.VppItemsByUom).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.VppCategory).WithMany(x => x.VppItems).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<SupplierProductMapping>(en =>
            {
                en.Property(x => x.Price).HasColumnType("decimal(19,4)");
                en.Property(x => x.NetPrice).HasColumnType("decimal(19,4)");
                en.Property(x => x.VatRate).HasColumnType("decimal(5,2)");
                en.Property(x => x.MinimumOrderQuantity).HasColumnType("decimal(19,4)");
                en.Property(x => x.SupplierSku).HasMaxLength(128);
                en.Property(x => x.RowVersion).IsRowVersion();
                en.HasOne(x => x.VppItem).WithMany(x => x.SupplierProductMappings).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.Supplier).WithMany(x => x.SupplierProductMappings).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.PriceList).WithMany(x => x.SupplierProductMappings)
                    .HasForeignKey(x => x.PriceListId).OnDelete(DeleteBehavior.Restrict);
                en.HasIndex(x => new { x.PriceListId, x.VppItemId })
                    .HasDatabaseName("UX_SupplierProductMappings_OneDefaultPerItemAndList")
                    .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0")
                    .IsUnique();
                en.HasIndex(x => new { x.PriceListId, x.VppItemId, x.IsDeleted })
                    .HasDatabaseName("IX_SupplierProductMappings_PriceResolution")
                    .IncludeProperties(x => new { x.NetPrice, x.VatRate, x.MinimumOrderQuantity, x.LeadTimeDays });
                en.ToTable(t => t.HasCheckConstraint("CK_SupplierProductMappings_NetPriceNonNegative", "[NetPrice] >= 0"));
                en.ToTable(t => t.HasCheckConstraint("CK_SupplierProductMappings_VatRateRange", "[VatRate] >= 0 AND [VatRate] <= 100"));
                en.ToTable(t => t.HasCheckConstraint("CK_SupplierProductMappings_MinimumOrderQuantityNonNegative", "[MinimumOrderQuantity] >= 0"));
                en.ToTable(t => t.HasCheckConstraint("CK_SupplierProductMappings_LeadTimeDaysNonNegative", "[LeadTimeDays] >= 0"));
            });
            modelBuilder.Entity<PriceList>(en =>
            {
                en.Property(x => x.PriceListCode).HasMaxLength(50);
                en.Property(x => x.PriceListName).HasMaxLength(200);
                en.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
                en.Property(x => x.VatPolicy).HasMaxLength(32).IsRequired();
                en.Property(x => x.ContractCode).HasMaxLength(128);
                en.Property(x => x.LegacyBackfillStatus).HasMaxLength(64);
                en.Property(x => x.DiscountRate).HasColumnType("decimal(5,2)");
                en.Property(x => x.RebateAmount).HasColumnType("decimal(19,4)");
                en.Property(x => x.FeeAmount).HasColumnType("decimal(19,4)");
                en.Property(x => x.ShippingAmount).HasColumnType("decimal(19,4)");
                en.Property(x => x.StatusReason).HasMaxLength(500);
                en.Property(x => x.Status).HasConversion<int>().IsRequired();
                en.Property(x => x.RowVersion).IsRowVersion();
                en.HasOne(x => x.Supplier).WithMany(x => x.PriceLists)
                    .HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
                en.HasIndex(x => x.IsDefault)
                    .HasDatabaseName("UX_PriceLists_OneDefault")
                    .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0")
                    .IsUnique();
                en.HasIndex(x => new { x.SupplierId, x.PriceListCode, x.Version })
                    .HasDatabaseName("UX_PriceLists_SupplierCodeVersion")
                    .HasFilter("[IsDeleted] = 0 AND [SupplierId] IS NOT NULL AND [PriceListCode] IS NOT NULL")
                    .IsUnique();
                en.HasIndex(x => new { x.SupplierId, x.Status, x.EffectiveFromUtc, x.EffectiveToUtc })
                    .HasDatabaseName("IX_PriceLists_Effective");
                en.ToTable(t => t.HasCheckConstraint(
                    "CK_PriceLists_EffectiveWindow",
                    "[EffectiveToUtc] IS NULL OR [EffectiveToUtc] > [EffectiveFromUtc]"));
                en.ToTable(t => t.HasCheckConstraint(
                    "CK_PriceLists_VersionPositive",
                    "[Version] > 0"));
                en.ToTable(t => t.HasCheckConstraint(
                    "CK_PriceLists_DiscountRateRange",
                    "[DiscountRate] >= 0 AND [DiscountRate] <= 100"));
                en.ToTable(t => t.HasCheckConstraint(
                    "CK_PriceLists_CommercialAmountsNonNegative",
                    "[RebateAmount] >= 0 AND [FeeAmount] >= 0 AND [ShippingAmount] >= 0"));
            });
            modelBuilder.Entity<VppCategory>(en =>
            {
            });
            modelBuilder.Entity<Supplier>(en =>
            {
            });
            modelBuilder.Entity<VppRequestDetail>(en =>
            {
                en.HasOne(x => x.Request).WithMany(x => x.RequestDetails)
                    .HasForeignKey(x => x.RequestId)
                    .OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.VppItem).WithMany()
                    .HasForeignKey(x => x.VppId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VppPeriod>(en =>
            {
                en.Property(x => x.MemberCompanyCode).HasMaxLength(50).IsRequired();
                en.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
                en.Property(x => x.State).HasConversion<int>().IsRequired();
                en.Property(x => x.RowVersion).IsRowVersion();
                en.HasIndex(x => new { x.MemberCompanyCode, x.Year, x.Month })
                    .HasDatabaseName("UX_Periods_Company_Year_Month_Active")
                    .HasFilter("[IsDeleted] = 0")
                    .IsUnique();
                en.HasIndex(x => new { x.MemberCompanyCode, x.State, x.SubmissionDeadlineUtc })
                    .HasDatabaseName("IX_Periods_CompanyStateDeadline");
                en.ToTable(table => table.HasCheckConstraint(
                    "CK_Periods_ValidRange",
                    "[Year] BETWEEN 1 AND 9999 AND [Month] BETWEEN 1 AND 12 " +
                    "AND [SubmissionDeadlineUtc] > [StartAtUtc] " +
                    "AND [SupplementApprovalDeadlineUtc] >= [SubmissionDeadlineUtc] " +
                    "AND [State] IN (0, 1, 2, 3)"));
                en.HasMany(x => x.Requests)
                    .WithOne(x => x.Period)
                    .HasForeignKey(x => x.PeriodId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VppRequest>(en =>
            {
                en.Property(x => x.VppCode).HasMaxLength(64);
                en.Property(x => x.RejectReason).HasMaxLength(500);
                en.Property(x => x.CancelReason).HasMaxLength(500);
                en.Property(x => x.SupplementReason).HasMaxLength(500);
                en.Property(x => x.IdempotencyKey).HasMaxLength(128);
                en.Property(x => x.CommandPayloadHash).HasMaxLength(64);
                en.Property(x => x.RowVersion).IsRowVersion();
                en.HasIndex(x => new { x.CreatedByUserId, x.Year, x.Month, x.IsDeleted, x.IsAdditionalOrder, x.Status })
                    .HasDatabaseName("IX_Requests_UserPeriodStatus");
                en.HasIndex(x => new { x.Year, x.Month, x.IsDeleted, x.Status, x.IsAdditionalOrder })
                    .HasDatabaseName("IX_Requests_PeriodStatus");
                en.HasIndex(
                        x => new { x.CreatedByUserId, x.PeriodId },
                        "IX_Requests_OneRegularPerUserPeriod_Model")
                    .HasDatabaseName("UX_Requests_OneRegularPerUserPeriod")
                    .HasFilter("[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 0")
                    .IsUnique();
                en.HasIndex(x => x.RequestSeriesId)
                    .HasDatabaseName("UX_Requests_CurrentRevisionSeries")
                    .HasFilter("[IsDeleted] = 0 AND [IsCurrentRevision] = 1")
                    .IsUnique();
                en.HasIndex(
                        x => new { x.CreatedByUserId, x.PeriodId },
                        "IX_Requests_OnePendingSupplement_Model")
                    .HasDatabaseName("UX_Requests_OnePendingSupplement")
                    .HasFilter("[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 1 AND [Status] = 6")
                    .IsUnique();
                en.HasIndex(x => new { x.CreatedByUserId, x.PeriodId, x.SupplementAttemptNumber })
                    .HasDatabaseName("UX_Requests_SupplementAttempt")
                    .HasFilter("[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 1 AND [SupplementAttemptNumber] IS NOT NULL")
                    .IsUnique();
                en.HasIndex(x => new { x.CreatedByUserId, x.IdempotencyKey })
                    .HasDatabaseName("UX_Requests_IdempotencyKey")
                    .HasFilter("[IsDeleted] = 0 AND [IdempotencyKey] IS NOT NULL")
                    .IsUnique();
                en.HasIndex(x => x.VppCode)
                    .HasDatabaseName("UX_Requests_VppCode")
                    .HasFilter("[VppCode] IS NOT NULL")
                    .IsUnique();
                en.HasOne(x => x.SettledByPriceList).WithMany()
                    .HasForeignKey(x => x.SettledByPriceListId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<RequestLog>(en =>
            {
                en.HasKey(x => x.Id);

                en.Property(x => x.Id).HasDefaultValueSql("NEWID()");
                en.Property(x => x.LogDate).HasDefaultValueSql("GETDATE()");
                en.Property(x => x.Action).HasMaxLength(64);
                en.Property(x => x.MemberCompanyCode).HasMaxLength(50);
                en.Property(x => x.CorrelationId).HasMaxLength(128);
                en.Property(x => x.Reason).HasMaxLength(500);

                en.HasOne(x => x.Request)
                      .WithMany(x => x.RequestLogs)
                      .HasForeignKey(x => x.RequestId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.ConfigureSettlementSnapshots();
            modelBuilder.Entity<Notification>(en =>
            {
                en.HasIndex(x => new { x.UserId, x.MemberCompanyCode, x.ReadAt, x.CreatedAt })
                    .HasDatabaseName("IX_Notifications_UserCompanyReadCreated");
                en.HasIndex(x => new { x.UserId, x.MemberCompanyCode, x.CorrelationId })
                    .HasDatabaseName("IX_Notifications_UserCompanyCorrelation");
                en.HasIndex(x => new { x.UserId, x.MemberCompanyCode, x.Type, x.CorrelationId })
                    .HasDatabaseName("UX_Notifications_UserCompanyTypeCorrelation")
                    .HasFilter("[CorrelationId] IS NOT NULL")
                    .IsUnique();
            });
            modelBuilder.Entity<EmailOutboxMessage>(en =>
            {
                en.Property(x => x.MemberCompanyCode).HasMaxLength(50).IsRequired();
                en.Property(x => x.Recipient).HasMaxLength(320).IsRequired();
                en.Property(x => x.Subject).HasMaxLength(250).IsRequired();
                en.Property(x => x.DeduplicationKey).HasMaxLength(128).IsRequired();
                en.Property(x => x.Status).HasMaxLength(24).IsRequired();
                en.Property(x => x.LastError).HasMaxLength(1000);
                en.HasIndex(x => x.DeduplicationKey)
                    .HasDatabaseName("UX_EmailOutboxMessages_DeduplicationKey")
                    .IsUnique();
                en.HasIndex(x => new { x.Status, x.NextAttemptAtUtc })
                    .HasDatabaseName("IX_EmailOutboxMessages_Due");
            });
        }

        private static void ConfigureTrustedAccess(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>(en =>
            {
                en.Property(x => x.Id).UseIdentityColumn(1_000_000_000, 1);
                en.Property(x => x.FullName).HasMaxLength(250);
                en.Property(x => x.EmployeeCode).HasMaxLength(50);
                en.Property(x => x.AccountStatus).HasConversion<string>().HasMaxLength(32);
                en.Property(x => x.SessionVersion).HasDefaultValue(1L);
                en.Property(x => x.RowVersion).IsRowVersion();
                en.HasIndex(x => x.NormalizedEmail)
                    .HasDatabaseName("UX_AspNetUsers_NormalizedEmail")
                    .HasFilter("[NormalizedEmail] IS NOT NULL")
                    .IsUnique();
                en.HasIndex(x => x.EmployeeCode)
                    .HasDatabaseName("UX_AspNetUsers_EmployeeCode")
                    .HasFilter("[EmployeeCode] IS NOT NULL")
                    .IsUnique();
            });
            modelBuilder.Entity<PermissionGroup>(en =>
            {
                en.Property(x => x.GroupCode).HasMaxLength(50);
                en.HasIndex(x => x.GroupCode)
                    .HasDatabaseName("UX_PermissionGroups_GroupCode_Active")
                    .HasFilter("[IsDeleted] = 0")
                    .IsUnique();
            });
            modelBuilder.Entity<UserGroupMembership>(en =>
            {
                en.Property(x => x.RowVersion).IsRowVersion();
                en.HasOne<AppUser>()
                    .WithMany()
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
                en.HasIndex(x => x.AccountId)
                    .HasDatabaseName("UX_UserGroupMemberships_OneActivePerUser")
                    .HasFilter("[IsDeleted] = 0 AND [AccountId] IS NOT NULL")
                    .IsUnique();
                en.ToTable(table => table.HasCheckConstraint(
                    "CK_UserGroupMemberships_ActivePrimaryDepartment",
                    "[IsDeleted] = 1 OR ([AccountId] IS NOT NULL AND [UserId] = [AccountId] AND [DepartmentId] <> '00000000-0000-0000-0000-000000000000')"));
            });
            modelBuilder.Entity<SecurityAudit>(en =>
            {
                en.Property(x => x.Id).HasDefaultValueSql("NEWID()");
                en.Property(x => x.OccurredAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                en.HasIndex(x => new { x.TargetUserId, x.OccurredAtUtc })
                    .HasDatabaseName("IX_SecurityAudits_TargetOccurredAt");
                en.HasIndex(x => new { x.Action, x.OccurredAtUtc })
                    .HasDatabaseName("IX_SecurityAudits_ActionOccurredAt");
            });
            modelBuilder.Entity<AuthBootstrapOperation>(en =>
            {
                en.Property(x => x.OperationKey).HasMaxLength(128);
                en.Property(x => x.InputFingerprint).HasMaxLength(64);
                en.Property(x => x.Status).HasMaxLength(32);
                en.HasOne<AppUser>()
                    .WithMany()
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

    }
}
