using System.Text.Json;
using gtas_vpp_shared.DTOs;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.Notifications;
using gtas_vpp_shared.DTOs.Res.Reports;
using gtas_vpp_shared.DTOs.Res.VPP;
using Xunit;

namespace gtas_vpp_be.Tests.Architecture;

public sealed class SharedContractSerializationTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AuthenticationRequest_PreservesPublicJsonShape()
    {
        AssertJsonProperties(
            new AuthenticationLoginRequest("student", "secret"),
            "password",
            "username");
    }

    [Fact]
    public void AuthenticationResponse_PreservesPublicJsonShapeAndNeverSerializesPasswordHash()
    {
        var response = new AuthenticationResultDTO();

        var properties = GetJsonProperties(response);

        Assert.Equal(
            Sort(
                "accessToken",
                "accessTokenExpiresAtUtc",
                "accountStatus",
                "departmentCode",
                "departmentName",
                "email",
                "fullName",
                "googleEmail",
                "groupId",
                "groupName",
                "isAdmin",
                "pagePermissions",
                "memberCompanyCode",
                "memberCompanyName",
                "memberCompanyShortName",
                "mustChangePassword",
                "sessionVersion",
                "userID",
                "userLogin"),
            properties);
        Assert.DoesNotContain("passwordChar", properties);
    }

    [Fact]
    public void LegacyStoredProcedureEnvelope_PreservesPublicJsonShape()
    {
        AssertJsonProperties(new StoredProcedureResultDTO(), "errorMess", "isSuccess", "resData");
    }

    [Fact]
    public void PermissionSnapshot_PreservesNestedPublicJsonShape()
    {
        var snapshot = new PermissionSnapshotResDTO
        {
            Pages =
            [
                new PermissionSnapshotPageResDTO
                {
                    Components = [new PermissionComponentResDTO()]
                }
            ]
        };

        AssertJsonProperties(
            snapshot,
            "groupId",
            "memberCompanyCode",
            "pages",
            "permissions",
            "version");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(snapshot, WebJson));
        var page = document.RootElement.GetProperty("pages")[0];
        Assert.Equal(Sort("components", "pageCode"), GetJsonProperties(page));
        var component = page.GetProperty("components")[0];
        Assert.Equal(
            Sort("componentCode", "isEnable", "isVisible"),
            GetJsonProperties(component));
    }

    [Fact]
    public void CatalogItem_PreservesFieldsPreviouslyDecoratedWithGridMetadata()
    {
        AssertJsonProperties(
            new VppItemResDTO(),
            "createdAtUtc",
            "createdByUserId",
            "defaultPrice",
            "defaultSupplierName",
            "defaultVatRate",
            "description",
            "id",
            "isDeleted",
            "supplierProductMappings",
            "uom",
            "uomCode",
            "uomId",
            "uomName",
            "updatedAtUtc",
            "updatedByUserId",
            "vppCategory",
            "vppCategoryCode",
            "vppCategoryId",
            "vppCategoryName",
            "vppCode",
            "vppName",
            "supplierCount");
    }

    [Fact]
    public void RequestHeader_PreservesInheritedAndComputedJsonFields()
    {
        AssertJsonProperties(
            new VppRequestResDTO(),
            "approvedAt",
            "approvedById",
            "baseRequestId",
            "baseRequestSeriesId",
            "canCancel",
            "canEdit",
            "canReplace",
            "cancelReason",
            "cancelledAt",
            "cancelledById",
            "createdAtUtc",
            "createdByUserId",
            "departmentCode",
            "description",
            "id",
            "isAdditionalOrder",
            "isCurrentRevision",
            "isDeadlinePassed",
            "isDeleted",
            "items",
            "month",
            "memberCompanyCode",
            "period",
            "periodId",
            "rejectReason",
            "rejectedAt",
            "rejectedById",
            "requestSeriesId",
            "requesterName",
            "revisionNumber",
            "rowVersion",
            "settledAt",
            "settledByPriceListId",
            "settledByPriceListName",
            "settledByUserId",
            "settledByUserName",
            "status",
            "statusText",
            "submittedDate",
            "submittedDateText",
            "supplementAttemptNumber",
            "supplementReason",
            "supplementSequence",
            "supersededByRequestId",
            "supersedesRequestId",
            "totalAmount",
            "totalLines",
            "totalQty",
            "updatedAtUtc",
            "updatedByUserId",
            "vppCode",
            "year");
    }

    [Fact]
    public void ReportAndNotificationEnvelopes_PreservePublicJsonShapes()
    {
        AssertJsonProperties(
            new ReportSummaryResDTO(),
            "availableYears",
            "departmentBreakdown",
            "generatedAt",
            "isSettlementReconciled",
            "month",
            "periodTrend",
            "scope",
            "settlementAllocationTotal",
            "settlementGrandTotal",
            "settlementId",
            "settlementPrimarySupplierName",
            "settlementRevisionNumber",
            "settlementVariance",
            "statusBreakdown",
            "topProducts",
            "totalAmount",
            "totalDepartments",
            "totalLines",
            "totalOrders",
            "totalQuantity",
            "totalRequesters",
            "year");

        var inbox = new NotificationInboxResDTO
        {
            Items = [new NotificationResDTO()]
        };
        AssertJsonProperties(inbox, "items", "totalCount", "unreadCount");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(inbox, WebJson));
        var item = document.RootElement.GetProperty("items")[0];
        var itemProperties = item.EnumerateObject().Select(property => property.Name).Order().ToArray();
        Assert.Equal(
            Sort(
                "correlationId",
                "createdAt",
                "id",
                "isRead",
                "message",
                "readAt",
                "route",
                "title",
                "type"),
            itemProperties);
    }

    private static void AssertJsonProperties<T>(T value, params string[] expected) =>
        Assert.Equal(Sort(expected), GetJsonProperties(value));

    private static string[] GetJsonProperties<T>(T value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, WebJson));
        return document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .Order()
            .ToArray();
    }

    private static string[] GetJsonProperties(JsonElement value) =>
        value.EnumerateObject()
            .Select(property => property.Name)
            .Order()
            .ToArray();

    private static string[] Sort(params string[] values) => values.Order().ToArray();
}
