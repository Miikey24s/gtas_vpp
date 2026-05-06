using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Authorization;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}

public sealed class PermissionAuthorizationHandler(VPPContext context)
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly VPPContext _context = context;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authContext,
        PermissionRequirement requirement)
    {
        if (authContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var groupIdClaim = authContext.User.FindFirst("GroupId")?.Value;
        if (!Guid.TryParse(groupIdClaim, out var groupId))
        {
            return;
        }

        var hasPermission = await _context.Set<P06_GroupPageComponentMapping>()
            .AsNoTracking()
            .Where(mapping => mapping.P02_GroupId == groupId && mapping.IsEnable && mapping.IsVisible)
            .AnyAsync(mapping =>
                mapping.P05_PageComponentMapping != null &&
                mapping.P05_PageComponentMapping.P03_Component != null &&
                !mapping.P05_PageComponentMapping.P03_Component.IsDeleted &&
                mapping.P05_PageComponentMapping.P03_Component.ComponentCode == requirement.PermissionCode);

        if (hasPermission)
        {
            authContext.Succeed(requirement);
        }
    }
}