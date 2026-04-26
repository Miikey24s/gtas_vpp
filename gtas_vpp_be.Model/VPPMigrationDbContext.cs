using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace gtas_vpp_be.Model
{
    public class VPPMigrationDbContext : DbContext
    {
        #region Auth
        public virtual DbSet<P01_Page> P01_Pages { get; set; }
        public virtual DbSet<P02_Group> P02_Groups { get; set; }
        public virtual DbSet<P03_Component> P03_Components { get; set; }
        public virtual DbSet<P04_UserGroup> P04_UserGroups { get; set; }
        public virtual DbSet<P05_PageComponentMapping> P05_PageComponentMappings { get; set; }
        public virtual DbSet<P06_GroupPageComponentMapping> P06_GroupPageComponentMappings { get; set; }
        #endregion

        #region Library
        public virtual DbSet<L01_Class> L01_Classes { get; set; }
        public virtual DbSet<L02_ClassDetail> L02_ClassesDetail { get; set; }
        public virtual DbSet<L03_VPPCategory> L03_VPPCategories { get; set; }
        public virtual DbSet<L04_VPP> L04_VPPs { get; set; }
        public virtual DbSet<L05_VPPSupplier> L05_VPPSuppliers { get; set; }
        public virtual DbSet<L06_VPPSupplierMapping> L06_VPPSupplierMappings { get; set; }
        public virtual DbSet<LEX02_CompanyDepartmentLocation> LEX02_CompanyDepartmentLocations { get; set; }
        #endregion

        #region Data
        public virtual DbSet<VPP01_RequestHeader> VPP01_RequestHeaders { get; set; }
        public virtual DbSet<VPP02_RequestDetail> VPP02_RequestDetail { get; set; }
        public virtual DbSet<VPP03_Log> VPP03_Logs { get; set; }
        #endregion

        public VPPMigrationDbContext(DbContextOptions<VPPMigrationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
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
                en.HasOne(x => x.UOM).WithMany(x => x.VPPs_UOM).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.VPPCategory).WithMany(x => x.VPPs).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<L06_VPPSupplierMapping>(en =>
            {
                en.HasOne(x => x.L04_VPP).WithMany(x => x.L06_VPPSupplierMappings).OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.L05_VPPSupplier).WithMany(x => x.L06_VPPSupplierMappings).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VPP02_RequestDetail>(en =>
            {
                en.HasOne(x => x.VPP01_RequestHeader).WithMany(x => x.VPP02_RequestDetails).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VPP03_Log>(en =>
            {
                en.HasKey(x => x.Id);

                en.Property(x => x.Id).HasDefaultValueSql("NEWID()");
                en.Property(x => x.LogDate).HasDefaultValueSql("GETDATE()");

                en.HasOne(x => x.VPP01_RequestHeader)
                      .WithMany(x => x.VPP03_Logs)
                      .HasForeignKey(x => x.VPP01_RequestHeaderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
