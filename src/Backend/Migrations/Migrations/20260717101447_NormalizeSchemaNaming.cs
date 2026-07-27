using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations;

/// <summary>
/// Renames the legacy numbered schema in place. This migration deliberately avoids
/// DropTable/CreateTable so existing TEST/LIVE data and identities are preserved.
/// </summary>
public partial class NormalizeSchemaNaming : Migration
{
    private static readonly (string OldName, string NewName)[] TableRenames =
    [
        ("A01_SecurityAudit", "SecurityAudits"),
        ("A02_AuthBootstrapOperation", "AuthBootstrapOperations"),
        ("P01_Page", "PermissionPages"),
        ("P02_Group", "PermissionGroups"),
        ("P03_Component", "PermissionComponents"),
        ("P04_UserGroup", "UserGroupMemberships"),
        ("P05_PageComponentMapping", "PageComponentMappings"),
        ("P06_GroupPageComponentMapping", "GroupPageComponentMappings"),
        ("L01_Class", "LookupCategories"),
        ("L02_ClassDetail", "LookupValues"),
        ("L03_VPPCategory", "VppCategories"),
        ("L04_VPP", "VppItems"),
        ("L05_VPPSupplier", "Suppliers"),
        ("L06_VPPSupplierMapping", "SupplierProductMappings"),
        ("L07_PriceList", "PriceLists"),
        ("LEX02_CompanyDepartmentLocation", "Departments"),
        ("VPP00_Period", "Periods"),
        ("VPP01_RequestHeader", "Requests"),
        ("VPP02_RequestDetail", "RequestDetails"),
        ("VPP03_Log", "RequestLogs"),
        ("VPP04_Settlement", "Settlements"),
        ("VPP05_SettlementItem", "SettlementItems"),
        ("VPP06_SettlementCharge", "SettlementCharges"),
        ("VPP07_SettlementAllocation", "SettlementAllocations"),
        ("N01_Notification", "Notifications"),
        ("N02_EmailOutbox", "EmailOutboxMessages")
    ];

    private static readonly string[] BaseTables =
    [
        "PermissionPages", "PermissionGroups", "PermissionComponents",
        "UserGroupMemberships", "GroupPageComponentMappings", "LookupCategories",
        "LookupValues", "VppCategories", "VppItems", "Suppliers",
        "SupplierProductMappings", "PriceLists", "Departments", "Periods",
        "Requests", "RequestDetails", "Settlements", "SettlementItems",
        "SettlementCharges", "SettlementAllocations"
    ];

    private static readonly (string TableName, string OldName, string NewName)[] IndexRenames =
    [
        ("AuthBootstrapOperations", "IX_A02_AuthBootstrapOperation_AccountId", "IX_AuthBootstrapOperations_AccountId"),
        ("EmailOutboxMessages", "IX_N02_EmailOutbox_Due", "IX_EmailOutboxMessages_Due"),
        ("EmailOutboxMessages", "UX_N02_EmailOutbox_DeduplicationKey", "UX_EmailOutboxMessages_DeduplicationKey"),
        ("GroupPageComponentMappings", "IX_P06_GroupPageComponentMapping_P05_PageComponentMappingId", "IX_GroupPageComponentMappings_PageComponentMappingId"),
        ("LookupValues", "IX_L02_ClassDetail_ClassId", "IX_LookupValues_LookupCategoryId"),
        ("Notifications", "IX_N01_User_Company_Correlation", "IX_Notifications_UserCompanyCorrelation"),
        ("Notifications", "IX_N01_User_Company_Read_Created", "IX_Notifications_UserCompanyReadCreated"),
        ("Notifications", "UX_N01_User_Company_Type_Correlation", "UX_Notifications_UserCompanyTypeCorrelation"),
        ("PageComponentMappings", "IX_P05_PageComponentMapping_P01_PageId", "IX_PageComponentMappings_PermissionPageId"),
        ("PageComponentMappings", "IX_P05_PageComponentMapping_P03_ComponentId", "IX_PageComponentMappings_PermissionComponentId"),
        ("Periods", "IX_VPP00_Period_Company_State_Deadline", "IX_Periods_CompanyStateDeadline"),
        ("Periods", "IX_VppPeriod_Company_State_Deadline", "IX_Periods_CompanyStateDeadline"),
        ("PermissionGroups", "UX_P02_Group_GroupCode_Active", "UX_PermissionGroups_GroupCode_Active"),
        ("PriceLists", "IX_L07_PriceBook_Effective", "IX_PriceLists_Effective"),
        ("PriceLists", "UX_L07_OneDefault", "UX_PriceLists_OneDefault"),
        ("PriceLists", "UX_L07_PriceBook_Supplier_Version", "UX_PriceLists_SupplierCodeVersion"),
        ("RequestDetails", "IX_VPP02_RequestDetail_VPP01_RequestHeaderId", "IX_RequestDetails_RequestId"),
        ("RequestDetails", "IX_VPP02_RequestDetail_VPPId", "IX_RequestDetails_VppId"),
        ("RequestLogs", "IX_VPP03_Log_VPP01_RequestHeaderId", "IX_RequestLogs_RequestId"),
        ("Requests", "IX_VPP01_RequestHeader_Period_Status", "IX_Requests_PeriodStatus"),
        ("Requests", "IX_VPP01_RequestHeader_PeriodId", "IX_Requests_PeriodId"),
        ("Requests", "IX_VPP01_RequestHeader_SettledByPriceListId", "IX_Requests_SettledByPriceListId"),
        ("Requests", "IX_VPP01_RequestHeader_User_Period_Status", "IX_Requests_UserPeriodStatus"),
        ("Requests", "UX_VPP01_CurrentRevisionSeries", "UX_Requests_CurrentRevisionSeries"),
        ("Requests", "UX_VPP01_IdempotencyKey", "UX_Requests_IdempotencyKey"),
        ("Requests", "UX_VPP01_OnePendingSupplement", "UX_Requests_OnePendingSupplement"),
        ("Requests", "UX_VPP01_OneRegularPerUserPeriod", "UX_Requests_OneRegularPerUserPeriod"),
        ("Requests", "UX_VPP01_SupplementAttempt", "UX_Requests_SupplementAttempt"),
        ("Requests", "UX_VppRequestVppCode", "UX_Requests_VppCode"),
        ("SecurityAudits", "IX_A01_Action_Occurred", "IX_SecurityAudits_ActionOccurredAt"),
        ("SecurityAudits", "IX_A01_Target_Occurred", "IX_SecurityAudits_TargetOccurredAt"),
        ("SettlementAllocations", "IX_VPP07_SettlementAllocation_SettlementId_DepartmentCode", "IX_SettlementAllocations_SettlementId_DepartmentCode"),
        ("SettlementAllocations", "IX_VPP07_SettlementAllocation_SettlementId_RequestDetailId", "IX_SettlementAllocations_SettlementId_RequestDetailId"),
        ("SettlementAllocations", "IX_VPP07_SettlementAllocation_SettlementItemId", "IX_SettlementAllocations_SettlementItemId"),
        ("SettlementCharges", "IX_VPP06_SettlementCharge_SettlementId_ChargeType", "IX_SettlementCharges_SettlementId_ChargeType"),
        ("SettlementItems", "IX_VPP05_SettlementItem_SettlementId_VppId", "IX_SettlementItems_SettlementId_VppId"),
        ("Settlements", "IX_VPP04_Settlement_PeriodId", "IX_Settlements_PeriodId"),
        ("Settlements", "IX_VPP04_Settlement_SupersedesSettlementId", "IX_Settlements_SupersedesSettlementId"),
        ("Settlements", "UX_Settlement_Current", "UX_Settlements_Current"),
        ("Settlements", "UX_Settlement_Revision", "UX_Settlements_Revision"),
        ("Settlements", "UX_VPP04_Settlement_Idempotency", "UX_Settlements_Idempotency"),
        ("SupplierProductMappings", "IX_L06_PriceBookItem_Resolve", "IX_SupplierProductMappings_PriceResolution"),
        ("SupplierProductMappings", "IX_L06_VPPSupplierMapping_L04_VPPId", "IX_SupplierProductMappings_VppItemId"),
        ("SupplierProductMappings", "IX_L06_VPPSupplierMapping_L05_VPPSupplierId", "IX_SupplierProductMappings_SupplierId"),
        ("SupplierProductMappings", "UX_L06_OneDefaultPerVPPPerList", "UX_SupplierProductMappings_OneDefaultPerItemAndList"),
        ("UserGroupMemberships", "IX_P04_UserGroup_LEX02_CompanyDepartmentLocationId", "IX_UserGroupMemberships_DepartmentId"),
        ("UserGroupMemberships", "IX_P04_UserGroup_P02_GroupId", "IX_UserGroupMemberships_PermissionGroupId"),
        ("UserGroupMemberships", "UX_P04_UserGroup_OneActivePerUser", "UX_UserGroupMemberships_OneActivePerUser"),
        ("VppItems", "IX_L04_VPP_Active_Category_Code", "IX_VppItems_ActiveCategoryCode"),
        ("VppItems", "IX_L04_VPP_UOMId", "IX_VppItems_UomId"),
        ("VppItems", "IX_L04_VPP_VPPCategoryId", "IX_VppItems_VppCategoryId"),
        ("VppItems", "UX_L04_VPP_VPPCode", "UX_VppItems_VppCode")
    ];

    private static readonly (string TableName, string OldName, string NewName)[] ConstraintRenames =
    [
        ("AuthBootstrapOperations", "PK_A02_AuthBootstrapOperation", "PK_AuthBootstrapOperations"),
        ("Departments", "PK_LEX02_CompanyDepartmentLocation", "PK_Departments"),
        ("EmailOutboxMessages", "PK_N02_EmailOutbox", "PK_EmailOutboxMessages"),
        ("GroupPageComponentMappings", "PK_P06_GroupPageComponentMapping", "PK_GroupPageComponentMappings"),
        ("LookupCategories", "PK_L01_Class", "PK_LookupCategories"),
        ("LookupValues", "PK_L02_ClassDetail", "PK_LookupValues"),
        ("Notifications", "PK_N01_Notification", "PK_Notifications"),
        ("PageComponentMappings", "PK_P05_PageComponentMapping", "PK_PageComponentMappings"),
        ("Periods", "PK_VPP00_Period", "PK_Periods"),
        ("PermissionComponents", "PK_P03_Component", "PK_PermissionComponents"),
        ("PermissionGroups", "PK_P02_Group", "PK_PermissionGroups"),
        ("PermissionPages", "PK_P01_Page", "PK_PermissionPages"),
        ("PriceLists", "PK_L07_PriceList", "PK_PriceLists"),
        ("RequestDetails", "PK_VPP02_RequestDetail", "PK_RequestDetails"),
        ("RequestLogs", "PK_VPP03_Log", "PK_RequestLogs"),
        ("Requests", "PK_VPP01_RequestHeader", "PK_Requests"),
        ("SecurityAudits", "PK_A01_SecurityAudit", "PK_SecurityAudits"),
        ("SettlementAllocations", "PK_VPP07_SettlementAllocation", "PK_SettlementAllocations"),
        ("SettlementCharges", "PK_VPP06_SettlementCharge", "PK_SettlementCharges"),
        ("SettlementItems", "PK_VPP05_SettlementItem", "PK_SettlementItems"),
        ("Settlements", "PK_VPP04_Settlement", "PK_Settlements"),
        ("SupplierProductMappings", "PK_L06_VPPSupplierMapping", "PK_SupplierProductMappings"),
        ("Suppliers", "PK_L05_VPPSupplier", "PK_Suppliers"),
        ("UserGroupMemberships", "PK_P04_UserGroup", "PK_UserGroupMemberships"),
        ("VppCategories", "PK_L03_VPPCategory", "PK_VppCategories"),
        ("VppItems", "PK_L04_VPP", "PK_VppItems"),
        ("AuthBootstrapOperations", "FK_A02_AuthBootstrapOperation_AspNetUsers_AccountId", "FK_AuthBootstrapOperations_AspNetUsers_AccountId"),
        ("PriceLists", "FK_L07_PriceList_L05_VPPSupplier_SupplierId", "FK_PriceLists_Suppliers_SupplierId"),
        ("Requests", "FK_VPP01_RequestHeader_L07_PriceList_SettledByPriceListId", "FK_Requests_PriceLists_SettledByPriceListId"),
        ("Requests", "FK_VPP01_RequestHeader_VPP00_Period_PeriodId", "FK_Requests_Periods_PeriodId"),
        ("SettlementAllocations", "FK_VPP07_SettlementAllocation_VPP04_Settlement_SettlementId", "FK_SettlementAllocations_Settlements_SettlementId"),
        ("SettlementAllocations", "FK_VPP07_SettlementAllocation_VPP05_SettlementItem_SettlementItemId", "FK_SettlementAllocations_SettlementItems_SettlementItemId"),
        ("SettlementCharges", "FK_VPP06_SettlementCharge_VPP04_Settlement_SettlementId", "FK_SettlementCharges_Settlements_SettlementId"),
        ("SettlementItems", "FK_VPP05_SettlementItem_VPP04_Settlement_SettlementId", "FK_SettlementItems_Settlements_SettlementId"),
        ("Settlements", "FK_VPP04_Settlement_VPP00_Period_PeriodId", "FK_Settlements_Periods_PeriodId"),
        ("Settlements", "FK_VPP04_Settlement_VPP04_Settlement_SupersedesSettlementId", "FK_Settlements_Settlements_SupersedesSettlementId"),
        ("UserGroupMemberships", "FK_P04_UserGroup_AspNetUsers_AccountId", "FK_UserGroupMemberships_AspNetUsers_AccountId"),
        ("PriceLists", "CK_PriceLists_CommercialAmountsNonNegative", "CK_PriceLists_CommercialAmountsNonNegative"),
        ("PriceLists", "CK_L07_PriceBook_CommercialAmounts", "CK_PriceLists_CommercialAmountsNonNegative"),
        ("PriceLists", "CK_L07_PriceBook_DiscountRate", "CK_PriceLists_DiscountRateRange"),
        ("PriceLists", "CK_L07_PriceBook_EffectiveWindow", "CK_PriceLists_EffectiveWindow"),
        ("PriceLists", "CK_L07_PriceBook_VersionPositive", "CK_PriceLists_VersionPositive"),
        ("SettlementAllocations", "CK_VPP07_SettlementAllocation_Quantity", "CK_SettlementAllocations_QuantityPositive"),
        ("SettlementItems", "CK_VPP05_SettlementItem_Amounts", "CK_SettlementItems_AmountsNonNegative"),
        ("Settlements", "CK_Settlement_Period", "CK_Settlements_Period"),
        ("Settlements", "CK_VPP04_Settlement_Amounts", "CK_Settlements_AmountsNonNegative"),
        ("SupplierProductMappings", "CK_L06_LeadTime_NonNegative", "CK_SupplierProductMappings_LeadTimeDaysNonNegative"),
        ("SupplierProductMappings", "CK_L06_Moq_NonNegative", "CK_SupplierProductMappings_MinimumOrderQuantityNonNegative"),
        ("SupplierProductMappings", "CK_L06_Price_NonNegative", "CK_SupplierProductMappings_NetPriceNonNegative"),
        ("SupplierProductMappings", "CK_L06_VatRate_Range", "CK_SupplierProductMappings_VatRateRange")
    ];

    private static readonly (string TableName, string ColumnName, string NewName)[] DefaultConstraintRenames =
    [
        ("RequestLogs", "Id", "DF_RequestLogs_Id"),
        ("RequestLogs", "LogDate", "DF_RequestLogs_LogDate"),
        ("Requests", "Status", "DF_Requests_Status"),
        ("Requests", "IsAdditionalOrder", "DF_Requests_IsAdditionalOrder"),
        ("Requests", "IsCurrentRevision", "DF_Requests_IsCurrentRevision"),
        ("Requests", "RequestSeriesId", "DF_Requests_RequestSeriesId"),
        ("Requests", "RevisionNumber", "DF_Requests_RevisionNumber"),
        ("SecurityAudits", "Id", "DF_SecurityAudits_Id"),
        ("SecurityAudits", "OccurredAtUtc", "DF_SecurityAudits_OccurredAtUtc"),
        ("SupplierProductMappings", "IsDefault", "DF_SupplierProductMappings_IsDefault")
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        DropLegacyDependencies(migrationBuilder);

        foreach (var (oldName, newName) in TableRenames)
        {
            RenameTable(migrationBuilder, oldName, newName);
        }

        foreach (var table in BaseTables)
        {
            RenameColumn(migrationBuilder, table, "CreateUserId", "CreatedByUserId");
            RenameColumn(migrationBuilder, table, "CreateDate", "CreatedAtUtc");
            RenameColumn(migrationBuilder, table, "UpdateUserId", "UpdatedByUserId");
            RenameColumn(migrationBuilder, table, "UpdateDate", "UpdatedAtUtc");
        }

        RenameColumn(migrationBuilder, "Departments", "LEX02Code", "Code");
        RenameColumn(migrationBuilder, "Departments", "LEX02Name", "Name");
        RenameColumn(migrationBuilder, "Departments", "ParentId", "ParentDepartmentId");
        DropColumnIfExists(migrationBuilder, "Departments", "LEX02Type");

        RenameColumn(migrationBuilder, "LookupCategories", "ClassCode", "Code");
        RenameColumn(migrationBuilder, "LookupCategories", "ClassName", "Name");
        RenameColumn(migrationBuilder, "LookupCategories", "ClassModul", "ModuleName");
        RenameColumn(migrationBuilder, "LookupValues", "ClassId", "LookupCategoryId");
        RenameColumn(migrationBuilder, "LookupValues", "ClassDetailCode", "Code");
        RenameColumn(migrationBuilder, "LookupValues", "ClassDetailValue", "Value");

        RenameColumn(migrationBuilder, "VppItems", "UOMId", "UomId");
        RenameColumn(migrationBuilder, "VppItems", "VPPCategoryId", "VppCategoryId");
        RenameColumn(migrationBuilder, "VppItems", "VPPCode", "VppCode");
        RenameColumn(migrationBuilder, "VppItems", "VPPName", "VppName");
        RenameColumn(migrationBuilder, "VppCategories", "VPPCategoryCode", "VppCategoryCode");
        RenameColumn(migrationBuilder, "VppCategories", "VPPCategoryName", "VppCategoryName");
        RenameColumn(migrationBuilder, "SupplierProductMappings", "L04_VPPId", "VppItemId");
        RenameColumn(migrationBuilder, "SupplierProductMappings", "L05_VPPSupplierId", "SupplierId");
        RenameColumn(migrationBuilder, "SupplierProductMappings", "L07_PriceListId", "PriceListId");

        RenameColumn(migrationBuilder, "UserGroupMemberships", "P02_GroupId", "PermissionGroupId");
        RenameColumn(migrationBuilder, "UserGroupMemberships", "LEX02_CompanyDepartmentLocationId", "DepartmentId");
        RenameColumn(migrationBuilder, "PageComponentMappings", "P01_PageId", "PermissionPageId");
        RenameColumn(migrationBuilder, "PageComponentMappings", "P03_ComponentId", "PermissionComponentId");
        RenameColumn(migrationBuilder, "GroupPageComponentMappings", "P05_PageComponentMappingId", "PageComponentMappingId");
        RenameColumn(migrationBuilder, "GroupPageComponentMappings", "P02_GroupId", "PermissionGroupId");

        RenameColumn(migrationBuilder, "Periods", "Y", "Year");
        RenameColumn(migrationBuilder, "Periods", "M", "Month");
        RenameColumn(migrationBuilder, "Requests", "Y", "Year");
        RenameColumn(migrationBuilder, "Requests", "M", "Month");
        RenameColumn(migrationBuilder, "Requests", "VPPCode", "VppCode");
        RenameColumn(migrationBuilder, "RequestDetails", "VPPId", "VppId");
        RenameColumn(migrationBuilder, "RequestDetails", "VPP01_RequestHeaderId", "RequestId");
        RenameColumn(migrationBuilder, "RequestLogs", "VPP01_RequestHeaderId", "RequestId");
        RenameColumn(migrationBuilder, "Settlements", "Y", "Year");
        RenameColumn(migrationBuilder, "Settlements", "M", "Month");

        AddNormalizedConstraints(migrationBuilder);

        // Department is a real organizational unit in the current TEST data.
        // Keep history, repoint memberships/children, and retire duplicate active codes.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.Departments', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.Departments
    SET Code = LEFT(CASE WHEN NULLIF(LTRIM(RTRIM(Code)), N'') IS NULL
                         THEN N'DEPT-' + LEFT(REPLACE(CONVERT(nvarchar(36), Id), N'-', N''), 32)
                         ELSE LTRIM(RTRIM(Code)) END, 50),
        Name = LEFT(CASE WHEN NULLIF(LTRIM(RTRIM(Name)), N'') IS NULL
                         THEN N'Unnamed department' ELSE LTRIM(RTRIM(Name)) END, 200);
    ALTER TABLE dbo.Departments ALTER COLUMN Code nvarchar(50) NOT NULL;
    ALTER TABLE dbo.Departments ALTER COLUMN Name nvarchar(200) NOT NULL;

    UPDATE membership
    SET DepartmentId = survivor.Id
    FROM dbo.UserGroupMemberships AS membership
    INNER JOIN dbo.Departments AS duplicate ON duplicate.Id = membership.DepartmentId
    CROSS APPLY
    (
        SELECT TOP (1) candidate.Id
        FROM dbo.Departments AS candidate
        WHERE candidate.Code = duplicate.Code
          AND candidate.IsDeleted = 0
        ORDER BY candidate.Id
    ) AS survivor
    WHERE duplicate.IsDeleted = 0
      AND survivor.Id <> duplicate.Id;

    UPDATE child
    SET ParentDepartmentId = survivor.Id
    FROM dbo.Departments AS child
    INNER JOIN dbo.Departments AS duplicate ON duplicate.Id = child.ParentDepartmentId
    CROSS APPLY
    (
        SELECT TOP (1) candidate.Id
        FROM dbo.Departments AS candidate
        WHERE candidate.Code = duplicate.Code
          AND candidate.IsDeleted = 0
        ORDER BY candidate.Id
    ) AS survivor
    WHERE duplicate.IsDeleted = 0
      AND survivor.Id <> duplicate.Id;

    UPDATE duplicate
    SET IsDeleted = 1
    FROM dbo.Departments AS duplicate
    WHERE duplicate.IsDeleted = 0
      AND EXISTS
      (
          SELECT 1
          FROM dbo.Departments AS survivor
          WHERE survivor.Code = duplicate.Code
            AND survivor.IsDeleted = 0
            AND survivor.Id < duplicate.Id
      );

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Departments')
          AND name = N'UX_Departments_Code_Active'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_Departments_Code_Active
            ON dbo.Departments(Code)
            WHERE IsDeleted = 0;
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = N'FK_Departments_Departments_ParentDepartmentId'
    )
    BEGIN
        ALTER TABLE dbo.Departments
            ADD CONSTRAINT FK_Departments_Departments_ParentDepartmentId
            FOREIGN KEY (ParentDepartmentId) REFERENCES dbo.Departments(Id)
            ON DELETE NO ACTION;
    END;
END;
""");

        NormalizeDatabaseObjectNames(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropNormalizedConstraints(migrationBuilder);
        RestoreLegacyDatabaseObjectNames(migrationBuilder);

        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.Departments', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Departments_Departments_ParentDepartmentId')
        ALTER TABLE dbo.Departments DROP CONSTRAINT FK_Departments_Departments_ParentDepartmentId;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Departments') AND name = N'UX_Departments_Code_Active')
        DROP INDEX UX_Departments_Code_Active ON dbo.Departments;
    IF COL_LENGTH(N'dbo.Departments', N'LEX02Type') IS NULL
    BEGIN
        ALTER TABLE dbo.Departments ADD LEX02Type nvarchar(32) NULL;
        UPDATE dbo.Departments SET LEX02Type = N'PhongBan' WHERE LEX02Type IS NULL;
    END;
END;
""");

        RenameColumn(migrationBuilder, "Settlements", "Month", "M");
        RenameColumn(migrationBuilder, "Settlements", "Year", "Y");
        RenameColumn(migrationBuilder, "RequestLogs", "RequestId", "VPP01_RequestHeaderId");
        RenameColumn(migrationBuilder, "RequestDetails", "RequestId", "VPP01_RequestHeaderId");
        RenameColumn(migrationBuilder, "RequestDetails", "VppId", "VPPId");
        RenameColumn(migrationBuilder, "Requests", "VppCode", "VPPCode");
        RenameColumn(migrationBuilder, "Requests", "Month", "M");
        RenameColumn(migrationBuilder, "Requests", "Year", "Y");
        RenameColumn(migrationBuilder, "Periods", "Month", "M");
        RenameColumn(migrationBuilder, "Periods", "Year", "Y");

        RenameColumn(migrationBuilder, "GroupPageComponentMappings", "PermissionGroupId", "P02_GroupId");
        RenameColumn(migrationBuilder, "GroupPageComponentMappings", "PageComponentMappingId", "P05_PageComponentMappingId");
        RenameColumn(migrationBuilder, "PageComponentMappings", "PermissionComponentId", "P03_ComponentId");
        RenameColumn(migrationBuilder, "PageComponentMappings", "PermissionPageId", "P01_PageId");
        RenameColumn(migrationBuilder, "UserGroupMemberships", "DepartmentId", "LEX02_CompanyDepartmentLocationId");
        RenameColumn(migrationBuilder, "UserGroupMemberships", "PermissionGroupId", "P02_GroupId");

        RenameColumn(migrationBuilder, "SupplierProductMappings", "PriceListId", "L07_PriceListId");
        RenameColumn(migrationBuilder, "SupplierProductMappings", "SupplierId", "L05_VPPSupplierId");
        RenameColumn(migrationBuilder, "SupplierProductMappings", "VppItemId", "L04_VPPId");
        RenameColumn(migrationBuilder, "VppItems", "VppName", "VPPName");
        RenameColumn(migrationBuilder, "VppItems", "VppCode", "VPPCode");
        RenameColumn(migrationBuilder, "VppItems", "VppCategoryId", "VPPCategoryId");
        RenameColumn(migrationBuilder, "VppItems", "UomId", "UOMId");
        RenameColumn(migrationBuilder, "VppCategories", "VppCategoryName", "VPPCategoryName");
        RenameColumn(migrationBuilder, "VppCategories", "VppCategoryCode", "VPPCategoryCode");
        RenameColumn(migrationBuilder, "LookupValues", "Value", "ClassDetailValue");
        RenameColumn(migrationBuilder, "LookupValues", "Code", "ClassDetailCode");
        RenameColumn(migrationBuilder, "LookupValues", "LookupCategoryId", "ClassId");
        RenameColumn(migrationBuilder, "LookupCategories", "ModuleName", "ClassModul");
        RenameColumn(migrationBuilder, "LookupCategories", "Name", "ClassName");
        RenameColumn(migrationBuilder, "LookupCategories", "Code", "ClassCode");
        RenameColumn(migrationBuilder, "Departments", "ParentDepartmentId", "ParentId");
        RenameColumn(migrationBuilder, "Departments", "Name", "LEX02Name");
        RenameColumn(migrationBuilder, "Departments", "Code", "LEX02Code");

        foreach (var table in BaseTables)
        {
            RenameColumn(migrationBuilder, table, "UpdatedAtUtc", "UpdateDate");
            RenameColumn(migrationBuilder, table, "UpdatedByUserId", "UpdateUserId");
            RenameColumn(migrationBuilder, table, "CreatedAtUtc", "CreateDate");
            RenameColumn(migrationBuilder, table, "CreatedByUserId", "CreateUserId");
        }

        for (var index = TableRenames.Length - 1; index >= 0; index--)
        {
            var (oldName, newName) = TableRenames[index];
            RenameTable(migrationBuilder, newName, oldName);
        }
    }

    private static void RenameTable(MigrationBuilder migrationBuilder, string oldName, string newName) =>
        migrationBuilder.Sql($"""
IF OBJECT_ID(N'dbo.{oldName}', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.{newName}', N'U') IS NULL
    EXEC sys.sp_rename N'dbo.{oldName}', N'{newName}';
""");

    private static void DropLegacyDependencies(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.L02_ClassDetail', N'U') IS NOT NULL
    AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_L02_ClassDetail_L01_Class_ClassId')
    ALTER TABLE dbo.L02_ClassDetail DROP CONSTRAINT FK_L02_ClassDetail_L01_Class_ClassId;
IF OBJECT_ID(N'dbo.L04_VPP', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_L04_VPP_L02_ClassDetail_UOMId')
        ALTER TABLE dbo.L04_VPP DROP CONSTRAINT FK_L04_VPP_L02_ClassDetail_UOMId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_L04_VPP_L03_VPPCategory_VPPCategoryId')
        ALTER TABLE dbo.L04_VPP DROP CONSTRAINT FK_L04_VPP_L03_VPPCategory_VPPCategoryId;
END;
IF OBJECT_ID(N'dbo.L06_VPPSupplierMapping', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_L06_VPPSupplierMapping_L04_VPP_L04_VPPId')
        ALTER TABLE dbo.L06_VPPSupplierMapping DROP CONSTRAINT FK_L06_VPPSupplierMapping_L04_VPP_L04_VPPId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_L06_VPPSupplierMapping_L05_VPPSupplier_L05_VPPSupplierId')
        ALTER TABLE dbo.L06_VPPSupplierMapping DROP CONSTRAINT FK_L06_VPPSupplierMapping_L05_VPPSupplier_L05_VPPSupplierId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_L06_VPPSupplierMapping_L07_PriceList_L07_PriceListId')
        ALTER TABLE dbo.L06_VPPSupplierMapping DROP CONSTRAINT FK_L06_VPPSupplierMapping_L07_PriceList_L07_PriceListId;
END;
IF OBJECT_ID(N'dbo.P04_UserGroup', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_P04_UserGroup_LEX02_CompanyDepartmentLocation_LEX02_CompanyDepartmentLocationId')
        ALTER TABLE dbo.P04_UserGroup DROP CONSTRAINT FK_P04_UserGroup_LEX02_CompanyDepartmentLocation_LEX02_CompanyDepartmentLocationId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_P04_UserGroup_P02_Group_P02_GroupId')
        ALTER TABLE dbo.P04_UserGroup DROP CONSTRAINT FK_P04_UserGroup_P02_Group_P02_GroupId;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_P04_UserGroup_ActivePrimaryDepartment')
        ALTER TABLE dbo.P04_UserGroup DROP CONSTRAINT CK_P04_UserGroup_ActivePrimaryDepartment;
END;
IF OBJECT_ID(N'dbo.P05_PageComponentMapping', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_P05_PageComponentMapping_P01_Page_P01_PageId')
        ALTER TABLE dbo.P05_PageComponentMapping DROP CONSTRAINT FK_P05_PageComponentMapping_P01_Page_P01_PageId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_P05_PageComponentMapping_P03_Component_P03_ComponentId')
        ALTER TABLE dbo.P05_PageComponentMapping DROP CONSTRAINT FK_P05_PageComponentMapping_P03_Component_P03_ComponentId;
END;
IF OBJECT_ID(N'dbo.P06_GroupPageComponentMapping', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_P06_GroupPageComponentMapping_P02_Group_P02_GroupId')
        ALTER TABLE dbo.P06_GroupPageComponentMapping DROP CONSTRAINT FK_P06_GroupPageComponentMapping_P02_Group_P02_GroupId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_P06_GroupPageComponentMapping_P05_PageComponentMapping_P05_PageComponentMappingId')
        ALTER TABLE dbo.P06_GroupPageComponentMapping DROP CONSTRAINT FK_P06_GroupPageComponentMapping_P05_PageComponentMapping_P05_PageComponentMappingId;
END;
IF OBJECT_ID(N'dbo.VPP02_RequestDetail', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VPP02_RequestDetail_L04_VPP_VPPId')
        ALTER TABLE dbo.VPP02_RequestDetail DROP CONSTRAINT FK_VPP02_RequestDetail_L04_VPP_VPPId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VPP02_RequestDetail_VPP01_RequestHeader_VPP01_RequestHeaderId')
        ALTER TABLE dbo.VPP02_RequestDetail DROP CONSTRAINT FK_VPP02_RequestDetail_VPP01_RequestHeader_VPP01_RequestHeaderId;
END;
IF OBJECT_ID(N'dbo.VPP03_Log', N'U') IS NOT NULL
    AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VPP03_Log_VPP01_RequestHeader_VPP01_RequestHeaderId')
    ALTER TABLE dbo.VPP03_Log DROP CONSTRAINT FK_VPP03_Log_VPP01_RequestHeader_VPP01_RequestHeaderId;

IF OBJECT_ID(N'dbo.VPP00_Period', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_VPP00_Period_ValidRange')
        ALTER TABLE dbo.VPP00_Period DROP CONSTRAINT CK_VPP00_Period_ValidRange;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.VPP00_Period') AND name = N'UX_VPP00_Period_Company_Year_Month_Active')
        DROP INDEX UX_VPP00_Period_Company_Year_Month_Active ON dbo.VPP00_Period;
END;
IF OBJECT_ID(N'dbo.VPP04_Settlement', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_VPP04_Settlement_Period')
        ALTER TABLE dbo.VPP04_Settlement DROP CONSTRAINT CK_VPP04_Settlement_Period;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.VPP04_Settlement') AND name = N'UX_VPP04_Settlement_Current')
        DROP INDEX UX_VPP04_Settlement_Current ON dbo.VPP04_Settlement;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.VPP04_Settlement') AND name = N'UX_VPP04_Settlement_Revision')
        DROP INDEX UX_VPP04_Settlement_Revision ON dbo.VPP04_Settlement;
END;
IF OBJECT_ID(N'dbo.VPP01_RequestHeader', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.VPP01_RequestHeader') AND name = N'UX_VPP01_VPPCode')
    DROP INDEX UX_VPP01_VPPCode ON dbo.VPP01_RequestHeader;
""");

    private static void AddNormalizedConstraints(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.UserGroupMemberships', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_UserGroupMemberships_ActivePrimaryDepartment')
        ALTER TABLE dbo.UserGroupMemberships ADD CONSTRAINT CK_UserGroupMemberships_ActivePrimaryDepartment
            CHECK ([IsDeleted] = 1 OR ([AccountId] IS NOT NULL AND [UserId] = [AccountId] AND [DepartmentId] <> '00000000-0000-0000-0000-000000000000'));
END;
IF OBJECT_ID(N'dbo.Periods', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Periods_ValidRange')
        ALTER TABLE dbo.Periods ADD CONSTRAINT CK_Periods_ValidRange
            CHECK ([Year] BETWEEN 1 AND 9999 AND [Month] BETWEEN 1 AND 12 AND [SubmissionDeadlineUtc] > [StartAtUtc] AND [SupplementApprovalDeadlineUtc] >= [SubmissionDeadlineUtc] AND [State] IN (0, 1, 2, 3));
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Periods') AND name = N'IX_Periods_CompanyStateDeadline')
        CREATE INDEX IX_Periods_CompanyStateDeadline ON dbo.Periods(MemberCompanyCode, State, SubmissionDeadlineUtc);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Periods') AND name = N'UX_Periods_Company_Year_Month_Active')
        CREATE UNIQUE INDEX UX_Periods_Company_Year_Month_Active ON dbo.Periods(MemberCompanyCode, Year, Month) WHERE IsDeleted = 0;
END;
IF OBJECT_ID(N'dbo.Settlements', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Settlements_Period')
        ALTER TABLE dbo.Settlements ADD CONSTRAINT CK_Settlements_Period
            CHECK ([Year] BETWEEN 1 AND 9999 AND [Month] BETWEEN 1 AND 12 AND [RevisionNumber] > 0);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Settlements') AND name = N'UX_Settlements_Current')
        CREATE UNIQUE INDEX UX_Settlements_Current ON dbo.Settlements(MemberCompanyCode, Year, Month) WHERE IsDeleted = 0 AND IsCurrentRevision = 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Settlements') AND name = N'UX_Settlements_Revision')
        CREATE UNIQUE INDEX UX_Settlements_Revision ON dbo.Settlements(MemberCompanyCode, Year, Month, RevisionNumber);
END;
IF OBJECT_ID(N'dbo.Requests', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Requests') AND name = N'UX_Requests_VppCode')
    CREATE UNIQUE INDEX UX_Requests_VppCode ON dbo.Requests(VppCode);

IF OBJECT_ID(N'dbo.LookupValues', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LookupValues_LookupCategories_LookupCategoryId')
    ALTER TABLE dbo.LookupValues ADD CONSTRAINT FK_LookupValues_LookupCategories_LookupCategoryId
        FOREIGN KEY (LookupCategoryId) REFERENCES dbo.LookupCategories(Id) ON DELETE NO ACTION;
IF OBJECT_ID(N'dbo.VppItems', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VppItems_LookupValues_UomId')
        ALTER TABLE dbo.VppItems ADD CONSTRAINT FK_VppItems_LookupValues_UomId
            FOREIGN KEY (UomId) REFERENCES dbo.LookupValues(Id) ON DELETE NO ACTION;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VppItems_VppCategories_VppCategoryId')
        ALTER TABLE dbo.VppItems ADD CONSTRAINT FK_VppItems_VppCategories_VppCategoryId
            FOREIGN KEY (VppCategoryId) REFERENCES dbo.VppCategories(Id) ON DELETE NO ACTION;
END;
IF OBJECT_ID(N'dbo.SupplierProductMappings', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SupplierProductMappings_PriceLists_PriceListId')
        ALTER TABLE dbo.SupplierProductMappings ADD CONSTRAINT FK_SupplierProductMappings_PriceLists_PriceListId
            FOREIGN KEY (PriceListId) REFERENCES dbo.PriceLists(Id) ON DELETE NO ACTION;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SupplierProductMappings_Suppliers_SupplierId')
        ALTER TABLE dbo.SupplierProductMappings ADD CONSTRAINT FK_SupplierProductMappings_Suppliers_SupplierId
            FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id) ON DELETE NO ACTION;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SupplierProductMappings_VppItems_VppItemId')
        ALTER TABLE dbo.SupplierProductMappings ADD CONSTRAINT FK_SupplierProductMappings_VppItems_VppItemId
            FOREIGN KEY (VppItemId) REFERENCES dbo.VppItems(Id) ON DELETE NO ACTION;
END;
IF OBJECT_ID(N'dbo.UserGroupMemberships', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserGroupMemberships_Departments_DepartmentId')
        ALTER TABLE dbo.UserGroupMemberships ADD CONSTRAINT FK_UserGroupMemberships_Departments_DepartmentId
            FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id) ON DELETE NO ACTION;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserGroupMemberships_PermissionGroups_PermissionGroupId')
        ALTER TABLE dbo.UserGroupMemberships ADD CONSTRAINT FK_UserGroupMemberships_PermissionGroups_PermissionGroupId
            FOREIGN KEY (PermissionGroupId) REFERENCES dbo.PermissionGroups(Id) ON DELETE NO ACTION;
END;
IF OBJECT_ID(N'dbo.PageComponentMappings', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PageComponentMappings_PermissionComponents_PermissionComponentId')
        ALTER TABLE dbo.PageComponentMappings ADD CONSTRAINT FK_PageComponentMappings_PermissionComponents_PermissionComponentId
            FOREIGN KEY (PermissionComponentId) REFERENCES dbo.PermissionComponents(Id) ON DELETE NO ACTION;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PageComponentMappings_PermissionPages_PermissionPageId')
        ALTER TABLE dbo.PageComponentMappings ADD CONSTRAINT FK_PageComponentMappings_PermissionPages_PermissionPageId
            FOREIGN KEY (PermissionPageId) REFERENCES dbo.PermissionPages(Id) ON DELETE NO ACTION;
END;
IF OBJECT_ID(N'dbo.GroupPageComponentMappings', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GroupPageComponentMappings_PageComponentMappings_PageComponentMappingId')
        ALTER TABLE dbo.GroupPageComponentMappings ADD CONSTRAINT FK_GroupPageComponentMappings_PageComponentMappings_PageComponentMappingId
            FOREIGN KEY (PageComponentMappingId) REFERENCES dbo.PageComponentMappings(Id) ON DELETE NO ACTION;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GroupPageComponentMappings_PermissionGroups_PermissionGroupId')
        ALTER TABLE dbo.GroupPageComponentMappings ADD CONSTRAINT FK_GroupPageComponentMappings_PermissionGroups_PermissionGroupId
            FOREIGN KEY (PermissionGroupId) REFERENCES dbo.PermissionGroups(Id) ON DELETE NO ACTION;
END;
IF OBJECT_ID(N'dbo.RequestDetails', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RequestDetails_Requests_RequestId')
        ALTER TABLE dbo.RequestDetails ADD CONSTRAINT FK_RequestDetails_Requests_RequestId
            FOREIGN KEY (RequestId) REFERENCES dbo.Requests(Id) ON DELETE NO ACTION;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RequestDetails_VppItems_VppId')
        ALTER TABLE dbo.RequestDetails ADD CONSTRAINT FK_RequestDetails_VppItems_VppId
            FOREIGN KEY (VppId) REFERENCES dbo.VppItems(Id) ON DELETE NO ACTION;
END;
IF OBJECT_ID(N'dbo.RequestLogs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RequestLogs_Requests_RequestId')
    ALTER TABLE dbo.RequestLogs ADD CONSTRAINT FK_RequestLogs_Requests_RequestId
        FOREIGN KEY (RequestId) REFERENCES dbo.Requests(Id) ON DELETE NO ACTION;
""");

    private static void DropNormalizedConstraints(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.LookupValues', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LookupValues_LookupCategories_LookupCategoryId')
    ALTER TABLE dbo.LookupValues DROP CONSTRAINT FK_LookupValues_LookupCategories_LookupCategoryId;
IF OBJECT_ID(N'dbo.VppItems', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VppItems_LookupValues_UomId')
        ALTER TABLE dbo.VppItems DROP CONSTRAINT FK_VppItems_LookupValues_UomId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VppItems_VppCategories_VppCategoryId')
        ALTER TABLE dbo.VppItems DROP CONSTRAINT FK_VppItems_VppCategories_VppCategoryId;
END;
IF OBJECT_ID(N'dbo.SupplierProductMappings', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SupplierProductMappings_PriceLists_PriceListId')
        ALTER TABLE dbo.SupplierProductMappings DROP CONSTRAINT FK_SupplierProductMappings_PriceLists_PriceListId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SupplierProductMappings_Suppliers_SupplierId')
        ALTER TABLE dbo.SupplierProductMappings DROP CONSTRAINT FK_SupplierProductMappings_Suppliers_SupplierId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SupplierProductMappings_VppItems_VppItemId')
        ALTER TABLE dbo.SupplierProductMappings DROP CONSTRAINT FK_SupplierProductMappings_VppItems_VppItemId;
END;
IF OBJECT_ID(N'dbo.UserGroupMemberships', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserGroupMemberships_Departments_DepartmentId')
        ALTER TABLE dbo.UserGroupMemberships DROP CONSTRAINT FK_UserGroupMemberships_Departments_DepartmentId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserGroupMemberships_PermissionGroups_PermissionGroupId')
        ALTER TABLE dbo.UserGroupMemberships DROP CONSTRAINT FK_UserGroupMemberships_PermissionGroups_PermissionGroupId;
END;
IF OBJECT_ID(N'dbo.PageComponentMappings', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PageComponentMappings_PermissionComponents_PermissionComponentId')
        ALTER TABLE dbo.PageComponentMappings DROP CONSTRAINT FK_PageComponentMappings_PermissionComponents_PermissionComponentId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PageComponentMappings_PermissionPages_PermissionPageId')
        ALTER TABLE dbo.PageComponentMappings DROP CONSTRAINT FK_PageComponentMappings_PermissionPages_PermissionPageId;
END;
IF OBJECT_ID(N'dbo.GroupPageComponentMappings', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GroupPageComponentMappings_PageComponentMappings_PageComponentMappingId')
        ALTER TABLE dbo.GroupPageComponentMappings DROP CONSTRAINT FK_GroupPageComponentMappings_PageComponentMappings_PageComponentMappingId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GroupPageComponentMappings_PermissionGroups_PermissionGroupId')
        ALTER TABLE dbo.GroupPageComponentMappings DROP CONSTRAINT FK_GroupPageComponentMappings_PermissionGroups_PermissionGroupId;
END;
IF OBJECT_ID(N'dbo.RequestDetails', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RequestDetails_Requests_RequestId')
        ALTER TABLE dbo.RequestDetails DROP CONSTRAINT FK_RequestDetails_Requests_RequestId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RequestDetails_VppItems_VppId')
        ALTER TABLE dbo.RequestDetails DROP CONSTRAINT FK_RequestDetails_VppItems_VppId;
END;
IF OBJECT_ID(N'dbo.RequestLogs', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RequestLogs_Requests_RequestId')
    ALTER TABLE dbo.RequestLogs DROP CONSTRAINT FK_RequestLogs_Requests_RequestId;

IF OBJECT_ID(N'dbo.UserGroupMemberships', N'U') IS NOT NULL
    AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_UserGroupMemberships_ActivePrimaryDepartment')
    ALTER TABLE dbo.UserGroupMemberships DROP CONSTRAINT CK_UserGroupMemberships_ActivePrimaryDepartment;
IF OBJECT_ID(N'dbo.Periods', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Periods_ValidRange')
        ALTER TABLE dbo.Periods DROP CONSTRAINT CK_Periods_ValidRange;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Periods') AND name = N'UX_Periods_Company_Year_Month_Active')
        DROP INDEX UX_Periods_Company_Year_Month_Active ON dbo.Periods;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Periods') AND name = N'IX_Periods_CompanyStateDeadline')
        DROP INDEX IX_Periods_CompanyStateDeadline ON dbo.Periods;
END;
IF OBJECT_ID(N'dbo.Settlements', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Settlements_Period')
        ALTER TABLE dbo.Settlements DROP CONSTRAINT CK_Settlements_Period;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Settlements') AND name = N'UX_Settlements_Current')
        DROP INDEX UX_Settlements_Current ON dbo.Settlements;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Settlements') AND name = N'UX_Settlements_Revision')
        DROP INDEX UX_Settlements_Revision ON dbo.Settlements;
END;
IF OBJECT_ID(N'dbo.Requests', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Requests') AND name = N'UX_Requests_VppCode')
    DROP INDEX UX_Requests_VppCode ON dbo.Requests;
""");

    private static void NormalizeDatabaseObjectNames(MigrationBuilder migrationBuilder)
    {
        foreach (var (tableName, oldName, newName) in IndexRenames)
        {
            RenameIndex(migrationBuilder, tableName, oldName, newName);
        }

        foreach (var (tableName, oldName, newName) in ConstraintRenames)
        {
            RenameConstraint(migrationBuilder, tableName, oldName, newName);
        }

        foreach (var (tableName, columnName, newName) in DefaultConstraintRenames)
        {
            RenameDefaultConstraint(migrationBuilder, tableName, columnName, newName);
        }
    }

    private static void RestoreLegacyDatabaseObjectNames(MigrationBuilder migrationBuilder)
    {
        for (var index = ConstraintRenames.Length - 1; index >= 0; index--)
        {
            var (tableName, oldName, newName) = ConstraintRenames[index];
            RenameConstraint(migrationBuilder, tableName, newName, oldName);
        }

        for (var index = IndexRenames.Length - 1; index >= 0; index--)
        {
            var (tableName, oldName, newName) = IndexRenames[index];
            RenameIndex(migrationBuilder, tableName, newName, oldName);
        }
    }

    private static void RenameIndex(
        MigrationBuilder migrationBuilder,
        string tableName,
        string oldName,
        string newName)
    {
        if (oldName == newName)
        {
            return;
        }

        migrationBuilder.Sql($"""
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NOT NULL
   AND EXISTS
   (
       SELECT 1 FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'dbo.{tableName}') AND name = N'{oldName}'
   )
BEGIN
    IF EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.{tableName}') AND name = N'{newName}'
    )
        DROP INDEX [{oldName}] ON dbo.[{tableName}];
    ELSE
        EXEC sys.sp_rename N'dbo.{tableName}.{oldName}', N'{newName}', N'INDEX';
END;
""");
    }

    private static void RenameConstraint(
        MigrationBuilder migrationBuilder,
        string tableName,
        string oldName,
        string newName)
    {
        if (oldName == newName)
        {
            return;
        }

        migrationBuilder.Sql($"""
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NOT NULL
   AND EXISTS
   (
       SELECT 1 FROM sys.objects
       WHERE parent_object_id = OBJECT_ID(N'dbo.{tableName}') AND name = N'{oldName}'
   )
   AND NOT EXISTS
   (
       SELECT 1 FROM sys.objects
       WHERE parent_object_id = OBJECT_ID(N'dbo.{tableName}') AND name = N'{newName}'
   )
    EXEC sys.sp_rename N'dbo.{oldName}', N'{newName}', N'OBJECT';
""");
    }

    private static void RenameDefaultConstraint(
        MigrationBuilder migrationBuilder,
        string tableName,
        string columnName,
        string newName) =>
        migrationBuilder.Sql($"""
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NOT NULL
BEGIN
    DECLARE @CurrentDefaultConstraint{tableName}{columnName} sysname;
    SELECT @CurrentDefaultConstraint{tableName}{columnName} = defaultConstraint.name
    FROM sys.default_constraints AS defaultConstraint
    INNER JOIN sys.columns AS columnDefinition
        ON columnDefinition.object_id = defaultConstraint.parent_object_id
       AND columnDefinition.column_id = defaultConstraint.parent_column_id
    WHERE defaultConstraint.parent_object_id = OBJECT_ID(N'dbo.{tableName}')
      AND columnDefinition.name = N'{columnName}';

    IF @CurrentDefaultConstraint{tableName}{columnName} IS NOT NULL
       AND @CurrentDefaultConstraint{tableName}{columnName} <> N'{newName}'
       AND NOT EXISTS
       (
           SELECT 1 FROM sys.objects
           WHERE parent_object_id = OBJECT_ID(N'dbo.{tableName}') AND name = N'{newName}'
       )
    BEGIN
        DECLARE @QualifiedDefaultConstraint{tableName}{columnName} nvarchar(517)
            = N'dbo.' + QUOTENAME(@CurrentDefaultConstraint{tableName}{columnName});
        EXEC sys.sp_rename @QualifiedDefaultConstraint{tableName}{columnName}, N'{newName}', N'OBJECT';
    END;
END;
""");

    private static void RenameColumn(MigrationBuilder migrationBuilder, string tableName, string oldName, string newName) =>
        migrationBuilder.Sql($"""
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NOT NULL
   AND EXISTS
   (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.{tableName}')
         AND name COLLATE Latin1_General_100_BIN2 = N'{oldName}' COLLATE Latin1_General_100_BIN2
   )
   AND NOT EXISTS
   (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.{tableName}')
         AND name COLLATE Latin1_General_100_BIN2 = N'{newName}' COLLATE Latin1_General_100_BIN2
   )
    EXEC sys.sp_rename N'dbo.{tableName}.{oldName}', N'{newName}', N'COLUMN';
""");

    private static void DropColumnIfExists(MigrationBuilder migrationBuilder, string tableName, string columnName) =>
        migrationBuilder.Sql($"""
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NOT NULL
    ALTER TABLE dbo.{tableName} DROP COLUMN {columnName};
""");
}
