using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Account;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using MembershipAdministrationResDTO = gtas_vpp_shared.DTOs.Res.Permission.MembershipAdministrationResDTO;

namespace gtas_vpp_fe.Features.IdentityAccess.Api;

public sealed record UserAdministrationQuery(
    int Skip,
    int Top,
    string Search = "",
    string? AccountStatus = null,
    Guid? GroupId = null,
    Guid? DepartmentId = null,
    string? OrderBy = null);

public sealed record UserAdministrationLookups(
    IReadOnlyList<PermissionGroupResDTO> Groups,
    IReadOnlyList<DepartmentResDTO> Departments,
    AccountAdministrationCapabilitiesResDTO Capabilities);

public sealed class UserAdministrationApiClient(IAPIServices api)
{
    private const string AccountAdminBase = "/api/account/admin";
    private const string PermissionBase = "/api/Permission";

    public async Task<UserAdministrationLookups> GetLookupsAsync(bool includeCapabilities)
    {
        var groupTask = api.GetFromApiAsync<List<PermissionGroupResDTO>>(
            $"{PermissionBase}/groups?getFullName=true");
        var departmentTask = api.GetFromApiAsync<List<DepartmentResDTO>>(
            "/api/Library/departments?top=1000&showDeleted=false&orderby=Name");
        var capabilityTask = includeCapabilities
            ? api.GetFromApiAsync<AccountAdministrationCapabilitiesResDTO>(
                $"{AccountAdminBase}/capabilities")
            : Task.FromResult<AccountAdministrationCapabilitiesResDTO?>(null);

        await Task.WhenAll(groupTask, departmentTask, capabilityTask);
        return new UserAdministrationLookups(
            await groupTask ?? [],
            (await departmentTask ?? [])
                .OrderBy(department => department.Name)
                .ToArray(),
            await capabilityTask ?? new AccountAdministrationCapabilitiesResDTO());
    }

    public async Task<AdministrationPage<UserAdministrationResDTO>> GetUsersAsync(
        UserAdministrationQuery query)
    {
        var result = await api.GetFromApiWithTotalCountAsync<List<UserAdministrationResDTO>>(
            BuildUsersEndpoint(query));
        return new AdministrationPage<UserAdministrationResDTO>(result.Data ?? [], result.TotalCount);
    }

    public Task<AccountLifecycleResDTO?> InviteAsync(AdminAccountInvitationReqDTO request) =>
        api.PostFromApiAsync<AccountLifecycleResDTO>($"{AccountAdminBase}/invite", request);

    public Task<MembershipAdministrationResDTO?> ActivateAsync(AdminAccountActivationReqDTO request) =>
        api.PostFromApiAsync<MembershipAdministrationResDTO>($"{AccountAdminBase}/activate", request);

    public Task<AccountLifecycleResDTO?> SendPasswordResetLinkAsync(AdminPasswordResetLinkReqDTO request) =>
        api.PostFromApiAsync<AccountLifecycleResDTO>(
            $"{AccountAdminBase}/send-password-reset-link",
            request);

    public Task<MembershipAdministrationResDTO?> DeactivateMembershipAsync(
        MembershipDeactivateReqDTO request) =>
        api.PostFromApiAsync<MembershipAdministrationResDTO>(
            $"{PermissionBase}/memberships/deactivate",
            request);

    public Task<MembershipAdministrationResDTO?> UpsertMembershipAsync(MembershipUpsertReqDTO request) =>
        api.PutFromApiAsync<MembershipAdministrationResDTO>(
            $"{PermissionBase}/memberships",
            request);

    internal static string BuildUsersEndpoint(UserAdministrationQuery query)
    {
        var queryParams = new List<string>();
        AddTextQuery(queryParams, "search", query.Search);
        AddTextQuery(queryParams, "accountStatus", query.AccountStatus);

        if (query.GroupId.HasValue)
        {
            queryParams.Add($"groupId={query.GroupId.Value}");
        }

        if (query.DepartmentId.HasValue)
        {
            queryParams.Add($"departmentId={query.DepartmentId.Value}");
        }

        queryParams.Add($"skip={Math.Max(0, query.Skip)}");
        queryParams.Add($"top={Math.Max(1, query.Top)}");
        AddTextQuery(queryParams, "orderby", query.OrderBy);

        return $"{PermissionBase}/users?{string.Join("&", queryParams)}";
    }

    private static void AddTextQuery(ICollection<string> queryParams, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            queryParams.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
        }
    }
}
