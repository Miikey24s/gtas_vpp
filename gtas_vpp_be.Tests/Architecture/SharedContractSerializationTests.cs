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
        var response = new sp_Authentication_Login();

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
                "list_PagePermission",
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
        AssertJsonProperties(new sp_ResDTO(), "errorMess", "isSuccess", "resData");
    }

    [Fact]
    public void PermissionSnapshot_PreservesNestedPublicJsonShape()
    {
        var snapshot = new PermissionSnapshotResDTO
        {
            Pages =
            [
                new PermissionPageResDTO
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
            new L04_VPPResDTO(),
            "createDate",
            "createUserId",
            "defaultPrice",
            "defaultSupplierName",
            "defaultVatRate",
            "description",
            "id",
            "isDeleted",
            "l06_VPPSupplierMappings",
            "uom",
            "uomId",
            "updateDate",
            "updateUserId",
            "vppCategory",
            "vppCategoryId",
            "vppCode",
            "vppName");
    }

    [Fact]
    public void RequestHeader_PreservesInheritedAndComputedJsonFields()
    {
        AssertJsonProperties(
            new VPP01_RequestHeaderResDTO(),
            "canCancel",
            "canEdit",
            "createDate",
            "createUserId",
            "departmentCode",
            "description",
            "id",
            "isAdditionalOrder",
            "isDeadlinePassed",
            "isDeleted",
            "items",
            "m",
            "memberCompanyCode",
            "period",
            "requesterName",
            "settledAt",
            "settledByPriceListId",
            "settledByPriceListName",
            "settledByUserId",
            "settledByUserName",
            "status",
            "statusText",
            "submittedDate",
            "submittedDateText",
            "totalAmount",
            "totalLines",
            "totalQty",
            "updateDate",
            "updateUserId",
            "vppCode",
            "y");
    }

    [Fact]
    public void ReportAndNotificationEnvelopes_PreservePublicJsonShapes()
    {
        AssertJsonProperties(
            new ReportSummaryResDTO(),
            "availableYears",
            "departmentBreakdown",
            "generatedAt",
            "month",
            "periodTrend",
            "scope",
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
