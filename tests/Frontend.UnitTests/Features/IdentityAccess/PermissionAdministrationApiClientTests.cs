using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Permission;
using Xunit;
using PermissionGroupDto = gtas_vpp_shared.DTOs.Res.Permission.PermissionGroupResDTO;

namespace gtas_vpp_fe.Tests.Features.IdentityAccess;

public sealed class PermissionAdministrationApiClientTests
{
    [Fact]
    public async Task GroupAndAuditQueries_EncodeCanonicalFiltersAndPaging()
    {
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetWithTotalCountAsync = (endpoint, type) =>
            {
                endpoints.Add(endpoint);
                object data = type == typeof(List<PermissionGroupDto>)
                    ? new List<PermissionGroupDto>()
                    : new List<SecurityAuditResDTO>();
                return Task.FromResult<(object?, int)>((data, 0));
            }
        };
        var client = new PermissionAdministrationApiClient(api);

        await client.GetGroupsAsync(new PermissionGroupQuery(10, 15, "admin", "GroupName"));
        await client.GetSecurityAuditsAsync(new SecurityAuditQuery(
            20,
            25,
            "user42",
            "ACCOUNT_ACTIVATED",
            "Succeeded",
            "CreatedAtUtc desc"));

        Assert.Contains(endpoints, endpoint => endpoint.StartsWith("/api/Permission/groups?getFullName=true&", StringComparison.Ordinal));
        Assert.Contains(endpoints, endpoint => endpoint.Contains("GroupCode", StringComparison.Ordinal));
        Assert.Contains(endpoints, endpoint => endpoint.Contains("skip=10", StringComparison.Ordinal));
        Assert.Contains(endpoints, endpoint => endpoint.StartsWith("/api/Permission/security-audits?", StringComparison.Ordinal));
        Assert.Contains(endpoints, endpoint => endpoint.Contains("action=ACCOUNT_ACTIVATED", StringComparison.Ordinal));
        Assert.Contains(endpoints, endpoint => endpoint.Contains("outcome=Succeeded", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PermissionMutationAndDetailMethods_UseTypedCanonicalEndpoints()
    {
        var groupId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetAsync = (endpoint, type) =>
            {
                endpoints.Add(endpoint);
                object data = type == typeof(List<PermissionPageComponentResDTO>)
                    ? new List<PermissionPageComponentResDTO>()
                    : new SecurityAuditFilterOptionsResDTO();
                return Task.FromResult<object?>(data);
            },
            PatchAsync = (endpoint, _, _) =>
            {
                endpoints.Add(endpoint);
                return Task.FromResult<object?>(new BatchPatchComponentMappingsResDTO());
            }
        };
        var client = new PermissionAdministrationApiClient(api);

        await client.GetGroupPageComponentsAsync(groupId);
        await client.PatchComponentMappingsAsync(new BatchPatchComponentMappingsReqDTO());
        await client.GetSecurityAuditFilterOptionsAsync();

        Assert.Contains($"/api/Permission/groups/{groupId}/page-components", endpoints);
        Assert.Contains("/api/Permission/component-mappings/batch", endpoints);
        Assert.Contains("/api/Permission/security-audits/filter-options", endpoints);
    }
}
