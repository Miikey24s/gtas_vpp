using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Permission;
using PermissionGroupDto = gtas_vpp_shared.DTOs.Res.Permission.PermissionGroupResDTO;

namespace gtas_vpp_fe.Features.IdentityAccess.Api;

public sealed record PermissionGroupQuery(
    int Skip,
    int Top,
    string Search = "",
    string? OrderBy = null);

public sealed record SecurityAuditQuery(
    int Skip,
    int Top,
    string Search = "",
    string? Action = null,
    string? Outcome = null,
    string? OrderBy = null);

public sealed class PermissionAdministrationApiClient(IAPIServices api)
{
    private const string PermissionBase = "/api/Permission";

    public async Task<AdministrationPage<PermissionGroupDto>> GetGroupsAsync(
        PermissionGroupQuery query)
    {
        var result = await api.GetFromApiWithTotalCountAsync<List<PermissionGroupDto>>(
            BuildGroupsEndpoint(query));
        return new AdministrationPage<PermissionGroupDto>(result.Data ?? [], result.TotalCount);
    }

    public Task<List<PermissionPageComponentResDTO>?> GetGroupPageComponentsAsync(Guid groupId) =>
        api.GetFromApiAsync<List<PermissionPageComponentResDTO>>(
            $"{PermissionBase}/groups/{groupId}/page-components");

    public Task<BatchPatchComponentMappingsResDTO?> PatchComponentMappingsAsync(
        BatchPatchComponentMappingsReqDTO request) =>
        api.PatchFromApiAsync<BatchPatchComponentMappingsResDTO>(
            $"{PermissionBase}/component-mappings/batch",
            request);

    public Task<SecurityAuditFilterOptionsResDTO?> GetSecurityAuditFilterOptionsAsync() =>
        api.GetFromApiAsync<SecurityAuditFilterOptionsResDTO>(
            $"{PermissionBase}/security-audits/filter-options");

    public async Task<AdministrationPage<SecurityAuditResDTO>> GetSecurityAuditsAsync(
        SecurityAuditQuery query)
    {
        var result = await api.GetFromApiWithTotalCountAsync<List<SecurityAuditResDTO>>(
            BuildSecurityAuditEndpoint(query));
        return new AdministrationPage<SecurityAuditResDTO>(result.Data ?? [], result.TotalCount);
    }

    internal static string BuildGroupsEndpoint(PermissionGroupQuery query)
    {
        var queryParams = new List<string> { "getFullName=true" };
        var search = query.Search.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = EscapeDynamicString(search).ToLowerInvariant();
            var filter =
                $"((GroupCode ?? \"\").ToLower().Contains(\"{escaped}\") || " +
                $"(GroupName ?? \"\").ToLower().Contains(\"{escaped}\"))";
            queryParams.Add($"filter={Uri.EscapeDataString(filter)}");
        }

        AppendPagingAndSort(queryParams, query.Skip, query.Top, query.OrderBy);
        return $"{PermissionBase}/groups?{string.Join("&", queryParams)}";
    }

    internal static string BuildSecurityAuditEndpoint(SecurityAuditQuery query)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            queryParams.Add($"action={Uri.EscapeDataString(query.Action)}");
        }

        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            queryParams.Add($"outcome={Uri.EscapeDataString(query.Outcome)}");
        }

        AppendPagingAndSort(queryParams, query.Skip, query.Top, query.OrderBy);
        return $"{PermissionBase}/security-audits?{string.Join("&", queryParams)}";
    }

    private static void AppendPagingAndSort(
        ICollection<string> queryParams,
        int skip,
        int top,
        string? orderBy)
    {
        queryParams.Add($"skip={Math.Max(0, skip)}");
        queryParams.Add($"top={Math.Max(1, top)}");
        if (!string.IsNullOrWhiteSpace(orderBy))
        {
            queryParams.Add($"orderby={Uri.EscapeDataString(orderBy)}");
        }
    }

    private static string EscapeDynamicString(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal);
}
