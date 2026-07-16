using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.Notifications;
using gtas_vpp_be.Model.View;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace gtas_vpp_be.Service.Helpers.Context
{
    public class VPPContext : IdentityUserContext<AppUser, int>
    {
        #region Auth
        public virtual DbSet<P01_Page> P01_Pages { get; set; }
        public virtual DbSet<P02_Group> P02_Groups { get; set; }
        public virtual DbSet<P03_Component> P03_Components { get; set; }
        public virtual DbSet<P04_UserGroup> P04_UserGroups { get; set; }
        public virtual DbSet<P05_PageComponentMapping> P05_PageComponentMappings { get; set; }
        public virtual DbSet<P06_GroupPageComponentMapping> P06_GroupPageComponentMappings { get; set; }
        public virtual DbSet<A01_SecurityAudit> A01_SecurityAudits { get; set; }
        public virtual DbSet<A02_AuthBootstrapOperation> A02_AuthBootstrapOperations { get; set; }
        #endregion

        #region Library
        public virtual DbSet<L01_Class> L01_Classes { get; set; }
        public virtual DbSet<L02_ClassDetail> L02_ClassesDetail { get; set; }
        public virtual DbSet<L03_VPPCategory> L03_VPPCategories { get; set; }
        public virtual DbSet<L04_VPP> L04_VPPs { get; set; }
        public virtual DbSet<L05_VPPSupplier> L05_VPPSuppliers { get; set; }
        public virtual DbSet<L06_VPPSupplierMapping> L06_VPPSupplierMappings { get; set; }
        public virtual DbSet<L07_PriceList> L07_PriceLists { get; set; }
        public virtual DbSet<LEX02_CompanyDepartmentLocation> LEX02_CompanyDepartmentLocations { get; set; }
        #endregion

        #region Data
        public virtual DbSet<VPP00_Period> VPP00_Periods { get; set; }
        public virtual DbSet<VPP01_RequestHeader> VPP01_RequestHeaders { get; set; }
        public virtual DbSet<VPP02_RequestDetail> VPP02_RequestDetail { get; set; }
        public virtual DbSet<VPP03_Log> VPP03_Logs { get; set; }
        public virtual DbSet<VPP04_Settlement> VPP04_Settlements { get; set; }
        public virtual DbSet<VPP05_SettlementItem> VPP05_SettlementItems { get; set; }
        public virtual DbSet<VPP06_SettlementCharge> VPP06_SettlementCharges { get; set; }
        public virtual DbSet<VPP07_SettlementAllocation> VPP07_SettlementAllocations { get; set; }
        #endregion

        public virtual DbSet<N01_Notification> N01_Notifications { get; set; }

        public virtual DbSet<sp_ResDTO> Sp_ResDTOs { get; set; }
        public virtual DbSet<v_Users> v_Users { get; set; }
        public virtual DbSet<v_WFXCompany> v_WFXCompanies { get; set; }
        public VPPContext(DbContextOptions<VPPContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureTrustedAccess(modelBuilder);
            modelBuilder.Entity<v_Users>().ToView("v_Users").HasNoKey();
            modelBuilder.Entity<v_WFXCompany>().ToView("v_WFXCompany").HasNoKey();
            modelBuilder.Entity<sp_ResDTO>().HasNoKey();
            modelBuilder.Entity<P05_PageComponentMapping>(en =>
            {
                en.HasKey(x => x.Id);
                en.HasOne(x => x.P03_Component).WithMany(x => x.P05_PageComponentMappings).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.P01_Page).WithMany(x => x.P05_PageComponentMappings).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<P04_UserGroup>(en =>
            {
                en.HasOne(x => x.P02_Group).WithMany(x => x.P04_UserGroups).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.LEX02_CompanyDepartmentLocation).WithMany(x => x.P04_UserGroups).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<P06_GroupPageComponentMapping>(en =>
            {
                en.HasKey(x => new { x.P02_GroupId, x.P05_PageComponentMappingId, x.MemberCompanyCode });
                en.HasOne(x => x.P05_PageComponentMapping).WithMany(x => x.P06_GroupPageComponentMappings).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.P02_Group).WithMany(x => x.P06_GroupPageComponentMapping).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<L02_ClassDetail>(en =>
            {
                en.HasOne(x => x.Class).WithMany(x => x.L02_ClassDetails).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<L04_VPP>(en =>
            {
                en.Property(x => x.VPPCode).HasMaxLength(64).IsRequired();
                en.Property(x => x.VPPName).HasMaxLength(250).IsRequired();
                en.HasIndex(x => x.VPPCode)
                    .HasDatabaseName("UX_L04_VPP_VPPCode")
                    .IsUnique();
                en.HasIndex(x => new { x.IsDeleted, x.VPPCategoryId, x.VPPCode })
                    .HasDatabaseName("IX_L04_VPP_Active_Category_Code")
                    .IncludeProperties(x => new { x.VPPName, x.UOMId });
                en.HasOne(x => x.UOM).WithMany(x => x.VPPs_UOM).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.VPPCategory).WithMany(x => x.VPPs).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<L06_VPPSupplierMapping>(en =>
            {
                en.Property(x => x.Price).HasColumnType("decimal(19,4)");
                en.Property(x => x.NetPrice).HasColumnType("decimal(19,4)");
                en.Property(x => x.VatRate).HasColumnType("decimal(5,2)");
                en.Property(x => x.MinimumOrderQuantity).HasColumnType("decimal(19,4)");
                en.Property(x => x.SupplierSku).HasMaxLength(128);
                en.Property(x => x.RowVersion).IsRowVersion();
                en.HasOne(x => x.L04_VPP).WithMany(x => x.L06_VPPSupplierMappings).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.L05_VPPSupplier).WithMany(x => x.L06_VPPSupplierMappings).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.L07_PriceList).WithMany(x => x.L06_VPPSupplierMappings)
                    .HasForeignKey(x => x.L07_PriceListId).OnDelete(DeleteBehavior.Restrict);
                en.HasIndex(x => new { x.L07_PriceListId, x.L04_VPPId })
                    .HasDatabaseName("UX_L06_OneDefaultPerVPPPerList")
                    .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0")
                    .IsUnique();
                en.HasIndex(x => new { x.L07_PriceListId, x.L04_VPPId, x.IsDeleted })
                    .HasDatabaseName("IX_L06_PriceBookItem_Resolve")
                    .IncludeProperties(x => new { x.NetPrice, x.VatRate, x.MinimumOrderQuantity, x.LeadTimeDays });
                en.ToTable(t => t.HasCheckConstraint("CK_L06_Price_NonNegative", "[NetPrice] >= 0"));
                en.ToTable(t => t.HasCheckConstraint("CK_L06_VatRate_Range", "[VatRate] >= 0 AND [VatRate] <= 100"));
                en.ToTable(t => t.HasCheckConstraint("CK_L06_Moq_NonNegative", "[MinimumOrderQuantity] >= 0"));
                en.ToTable(t => t.HasCheckConstraint("CK_L06_LeadTime_NonNegative", "[LeadTimeDays] >= 0"));
            });
            modelBuilder.Entity<L07_PriceList>(en =>
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
                en.HasOne(x => x.Supplier).WithMany(x => x.PriceBooks)
                    .HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
                en.HasIndex(x => x.IsDefault)
                    .HasDatabaseName("UX_L07_OneDefault")
                    .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0")
                    .IsUnique();
                en.HasIndex(x => new { x.SupplierId, x.PriceListCode, x.Version })
                    .HasDatabaseName("UX_L07_PriceBook_Supplier_Version")
                    .HasFilter("[IsDeleted] = 0 AND [SupplierId] IS NOT NULL AND [PriceListCode] IS NOT NULL")
                    .IsUnique();
                en.HasIndex(x => new { x.SupplierId, x.Status, x.EffectiveFromUtc, x.EffectiveToUtc })
                    .HasDatabaseName("IX_L07_PriceBook_Effective");
                en.ToTable(t => t.HasCheckConstraint(
                    "CK_L07_PriceBook_EffectiveWindow",
                    "[EffectiveToUtc] IS NULL OR [EffectiveToUtc] > [EffectiveFromUtc]"));
                en.ToTable(t => t.HasCheckConstraint(
                    "CK_L07_PriceBook_VersionPositive",
                    "[Version] > 0"));
                en.ToTable(t => t.HasCheckConstraint(
                    "CK_L07_PriceBook_DiscountRate",
                    "[DiscountRate] >= 0 AND [DiscountRate] <= 100"));
                en.ToTable(t => t.HasCheckConstraint(
                    "CK_L07_PriceBook_CommercialAmounts",
                    "[RebateAmount] >= 0 AND [FeeAmount] >= 0 AND [ShippingAmount] >= 0"));
            });
            modelBuilder.Entity<VPP02_RequestDetail>(en =>
            {
                en.HasOne(x => x.VPP01_RequestHeader).WithMany(x => x.VPP02_RequestDetails).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VPP00_Period>(en =>
            {
                en.Property(x => x.MemberCompanyCode).HasMaxLength(50).IsRequired();
                en.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
                en.Property(x => x.State).HasConversion<int>().IsRequired();
                en.Property(x => x.RowVersion).IsRowVersion();
                en.HasIndex(x => new { x.MemberCompanyCode, x.Y, x.M })
                    .HasDatabaseName("UX_VPP00_Period_Company_Year_Month_Active")
                    .HasFilter("[IsDeleted] = 0")
                    .IsUnique();
                en.HasIndex(x => new { x.MemberCompanyCode, x.State, x.SubmissionDeadlineUtc })
                    .HasDatabaseName("IX_VPP00_Period_Company_State_Deadline");
                en.ToTable(table => table.HasCheckConstraint(
                    "CK_VPP00_Period_ValidRange",
                    "[Y] BETWEEN 1 AND 9999 AND [M] BETWEEN 1 AND 12 " +
                    "AND [SubmissionDeadlineUtc] > [StartAtUtc] " +
                    "AND [SupplementApprovalDeadlineUtc] >= [SubmissionDeadlineUtc] " +
                    "AND [State] IN (0, 1, 2, 3)"));
                en.HasMany(x => x.RequestHeaders)
                    .WithOne(x => x.Period)
                    .HasForeignKey(x => x.PeriodId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VPP01_RequestHeader>(en =>
            {
                en.Property(x => x.VPPCode).HasMaxLength(64);
                en.Property(x => x.RejectReason).HasMaxLength(500);
                en.Property(x => x.CancelReason).HasMaxLength(500);
                en.Property(x => x.SupplementReason).HasMaxLength(500);
                en.Property(x => x.IdempotencyKey).HasMaxLength(128);
                en.Property(x => x.CommandPayloadHash).HasMaxLength(64);
                en.Property(x => x.RowVersion).IsRowVersion();
                en.HasIndex(x => new { x.CreateUserId, x.Y, x.M, x.IsDeleted, x.IsAdditionalOrder, x.Status })
                    .HasDatabaseName("IX_VPP01_RequestHeader_User_Period_Status");
                en.HasIndex(x => new { x.Y, x.M, x.IsDeleted, x.Status, x.IsAdditionalOrder })
                    .HasDatabaseName("IX_VPP01_RequestHeader_Period_Status");
                en.HasIndex(x => new { x.CreateUserId, x.PeriodId })
                    .HasDatabaseName("UX_VPP01_OneRegularPerUserPeriod")
                    .HasFilter("[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 0")
                    .IsUnique();
                en.HasIndex(x => x.RequestSeriesId)
                    .HasDatabaseName("UX_VPP01_CurrentRevisionSeries")
                    .HasFilter("[IsDeleted] = 0 AND [IsCurrentRevision] = 1")
                    .IsUnique();
                en.HasIndex(x => new { x.CreateUserId, x.PeriodId, x.BaseRequestSeriesId })
                    .HasDatabaseName("UX_VPP01_OnePendingSupplement")
                    .HasFilter("[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 1 AND [Status] = 6")
                    .IsUnique();
                en.HasIndex(x => new { x.CreateUserId, x.PeriodId, x.BaseRequestSeriesId, x.SupplementAttemptNumber })
                    .HasDatabaseName("UX_VPP01_SupplementAttempt")
                    .HasFilter("[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 1 AND [SupplementAttemptNumber] IS NOT NULL")
                    .IsUnique();
                en.HasIndex(x => new { x.CreateUserId, x.IdempotencyKey })
                    .HasDatabaseName("UX_VPP01_IdempotencyKey")
                    .HasFilter("[IsDeleted] = 0 AND [IdempotencyKey] IS NOT NULL")
                    .IsUnique();
                en.HasIndex(x => x.VPPCode)
                    .HasDatabaseName("UX_VPP01_VPPCode")
                    .HasFilter("[VPPCode] IS NOT NULL")
                    .IsUnique();
                en.HasOne(x => x.SettledByPriceList).WithMany()
                    .HasForeignKey(x => x.SettledByPriceListId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VPP03_Log>(en =>
            {
                en.HasKey(x => x.Id);

                en.Property(x => x.Id).HasDefaultValueSql("NEWID()");
                en.Property(x => x.LogDate).HasDefaultValueSql("GETDATE()");
                en.Property(x => x.Action).HasMaxLength(64);
                en.Property(x => x.MemberCompanyCode).HasMaxLength(50);
                en.Property(x => x.CorrelationId).HasMaxLength(128);
                en.Property(x => x.Reason).HasMaxLength(500);

                en.HasOne(x => x.VPP01_RequestHeader)
                      .WithMany(x => x.VPP03_Logs)
                      .HasForeignKey(x => x.VPP01_RequestHeaderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.ConfigureSettlementSnapshots();
            modelBuilder.Entity<N01_Notification>(en =>
            {
                en.HasIndex(x => new { x.UserId, x.MemberCompanyCode, x.ReadAt, x.CreatedAt })
                    .HasDatabaseName("IX_N01_User_Company_Read_Created");
                en.HasIndex(x => new { x.UserId, x.MemberCompanyCode, x.CorrelationId })
                    .HasDatabaseName("IX_N01_User_Company_Correlation");
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
            modelBuilder.Entity<P02_Group>(en =>
            {
                en.Property(x => x.GroupCode).HasMaxLength(50);
                en.HasIndex(x => x.GroupCode)
                    .HasDatabaseName("UX_P02_Group_GroupCode_Active")
                    .HasFilter("[IsDeleted] = 0")
                    .IsUnique();
            });
            modelBuilder.Entity<P04_UserGroup>(en =>
            {
                en.Property(x => x.RowVersion).IsRowVersion();
                en.HasOne<AppUser>()
                    .WithMany()
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
                en.HasIndex(x => x.AccountId)
                    .HasDatabaseName("UX_P04_UserGroup_OneActivePerUser")
                    .HasFilter("[IsDeleted] = 0 AND [AccountId] IS NOT NULL")
                    .IsUnique();
                en.ToTable(table => table.HasCheckConstraint(
                    "CK_P04_UserGroup_ActivePrimaryDepartment",
                    "[IsDeleted] = 1 OR ([AccountId] IS NOT NULL AND [UserId] = [AccountId] AND [LEX02_CompanyDepartmentLocationId] <> '00000000-0000-0000-0000-000000000000')"));
            });
            modelBuilder.Entity<A01_SecurityAudit>(en =>
            {
                en.Property(x => x.Id).HasDefaultValueSql("NEWID()");
                en.Property(x => x.OccurredAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                en.HasIndex(x => new { x.TargetUserId, x.OccurredAtUtc })
                    .HasDatabaseName("IX_A01_Target_Occurred");
                en.HasIndex(x => new { x.Action, x.OccurredAtUtc })
                    .HasDatabaseName("IX_A01_Action_Occurred");
            });
            modelBuilder.Entity<A02_AuthBootstrapOperation>(en =>
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
