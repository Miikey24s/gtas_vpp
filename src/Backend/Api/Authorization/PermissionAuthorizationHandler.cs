using Microsoft.AspNetCore.Authorization;

namespace gtas_vpp_be.Authorization;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}

public sealed class PermissionAuthorizationHandler(IPermissionService permissionService)
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService = permissionService;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authContext,
        PermissionRequirement requirement)
    {
        if (authContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var hasPermission = await _permissionService.HasPermissionAsync(
            authContext.User,
            requirement.PermissionCode);

        if (hasPermission)
        {
            authContext.Succeed(requirement);
        }
    }
}
