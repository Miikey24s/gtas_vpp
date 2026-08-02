using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Account;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Xunit;
using MembershipAdministrationResDTO = gtas_vpp_shared.DTOs.Res.Permission.MembershipAdministrationResDTO;

namespace gtas_vpp_fe.Tests.Features.IdentityAccess;

public sealed class UserAdministrationApiClientTests
{
    [Fact]
    public async Task Lookups_LoadCanonicalGroupsDepartmentsAndOptionalCapabilities()
    {
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetAsync = (endpoint, type) =>
            {
                endpoints.Add(endpoint);
                object? value = type == typeof(List<PermissionGroupResDTO>)
                    ? new List<PermissionGroupResDTO>()
                    : type == typeof(List<DepartmentResDTO>)
                        ? new List<DepartmentResDTO>
                        {
                            new() { Name = "Zeta" },
                            new() { Name = "Alpha" }
                        }
                        : new AccountAdministrationCapabilitiesResDTO();
                return Task.FromResult<object?>(value);
            }
        };
        var client = new UserAdministrationApiClient(api);

        var lookups = await client.GetLookupsAsync(includeCapabilities: true);

        Assert.Contains("/api/Permission/groups?getFullName=true", endpoints);
        Assert.Contains("/api/Library/departments?top=1000&showDeleted=false&orderby=Name", endpoints);
        Assert.Contains("/api/account/admin/capabilities", endpoints);
        Assert.Equal(["Alpha", "Zeta"], lookups.Departments.Select(department => department.Name));

        endpoints.Clear();
        var restrictedLookups = await client.GetLookupsAsync(includeCapabilities: false);
        Assert.DoesNotContain("/api/account/admin/capabilities", endpoints);
        Assert.False(restrictedLookups.Capabilities.InvitationEnabled);
    }

    [Fact]
    public async Task UsersQuery_EncodesFiltersPagingAndSort()
    {
        var endpoints = new List<string>();
        var api = new StubApiServices
        {
            GetWithTotalCountAsync = (value, _) =>
            {
                endpoints.Add(value);
                return Task.FromResult<(object?, int)>((new List<UserAdministrationResDTO>(), 42));
            }
        };
        var client = new UserAdministrationApiClient(api);
        var groupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var departmentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var page = await client.GetUsersAsync(new UserAdministrationQuery(
            10,
            25,
            "Nguyen Van A",
            "PendingApproval",
            groupId,
            departmentId,
            "FullName desc"));
        await client.GetUsersAsync(new UserAdministrationQuery(-5, 0));

        var endpoint = endpoints[0];
        Assert.StartsWith("/api/Permission/users?", endpoint, StringComparison.Ordinal);
        Assert.Contains("search=Nguyen%20Van%20A", endpoint, StringComparison.Ordinal);
        Assert.Contains("accountStatus=PendingApproval", endpoint, StringComparison.Ordinal);
        Assert.Contains($"groupId={groupId}", endpoint, StringComparison.Ordinal);
        Assert.Contains($"departmentId={departmentId}", endpoint, StringComparison.Ordinal);
        Assert.Contains("skip=10", endpoint, StringComparison.Ordinal);
        Assert.Contains("top=25", endpoint, StringComparison.Ordinal);
        Assert.Contains("orderby=FullName%20desc", endpoint, StringComparison.Ordinal);
        Assert.Equal(42, page.TotalCount);
        Assert.Contains("skip=0", endpoints[1], StringComparison.Ordinal);
        Assert.Contains("top=1", endpoints[1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdministrationCommands_UseTypedCanonicalEndpoints()
    {
        var calls = new List<(string Method, string Endpoint, Type? BodyType, Type ResponseType)>();
        var api = new StubApiServices
        {
            PostAsync = (endpoint, body, responseType) =>
            {
                calls.Add(("POST", endpoint, body?.GetType(), responseType));
                return Task.FromResult<object?>(null);
            },
            PutAsync = (endpoint, body, responseType) =>
            {
                calls.Add(("PUT", endpoint, body.GetType(), responseType));
                return Task.FromResult<object?>(null);
            }
        };
        var client = new UserAdministrationApiClient(api);

        await client.InviteAsync(new AdminAccountInvitationReqDTO());
        await client.ActivateAsync(new AdminAccountActivationReqDTO());
        await client.SendPasswordResetLinkAsync(new AdminPasswordResetLinkReqDTO());
        await client.DeactivateMembershipAsync(new MembershipDeactivateReqDTO());
        await client.UpsertMembershipAsync(new MembershipUpsertReqDTO());

        Assert.Equal(
            [
                ("POST", "/api/account/admin/invite", typeof(AdminAccountInvitationReqDTO), typeof(AccountLifecycleResDTO)),
                ("POST", "/api/account/admin/activate", typeof(AdminAccountActivationReqDTO), typeof(MembershipAdministrationResDTO)),
                ("POST", "/api/account/admin/send-password-reset-link", typeof(AdminPasswordResetLinkReqDTO), typeof(AccountLifecycleResDTO)),
                ("POST", "/api/Permission/memberships/deactivate", typeof(MembershipDeactivateReqDTO), typeof(MembershipAdministrationResDTO)),
                ("PUT", "/api/Permission/memberships", typeof(MembershipUpsertReqDTO), typeof(MembershipAdministrationResDTO))
            ],
            calls);
    }
}
