using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Authorization;

public interface IUserGroupMembershipQueryService
{
    Task<IReadOnlyList<UserGroupMembershipResDTO>> GetAsync(
        int? userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Gom read path membership về một nơi để controller chỉ điều phối HTTP.
/// </summary>
public sealed class UserGroupMembershipQueryService(
    VPPContext context,
    IUserNameResolver userNameResolver) : IUserGroupMembershipQueryService
{
    public async Task<IReadOnlyList<UserGroupMembershipResDTO>> GetAsync(
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<UserGroupMembership>()
            .AsNoTracking()
            .Where(row => row.AccountId.HasValue
                && row.UserId == row.AccountId.Value
                && !row.IsDeleted);
        if (userId.HasValue)
        {
            query = query.Where(row => row.UserId == userId.Value);
        }

        var rows = await query.ToListAsync(cancellationToken);
        rows = await userNameResolver.WithUserNamesAsync(rows, context);
        return rows.Adapt<List<UserGroupMembershipResDTO>>();
    }
}
