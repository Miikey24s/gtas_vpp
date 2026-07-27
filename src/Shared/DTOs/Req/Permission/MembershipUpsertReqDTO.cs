using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Permission;

/// <summary>
/// Command quản trị để tạo hoặc thay thế membership phân quyền đang hoạt động duy nhất
/// của tài khoản. Các trường audit chủ động do server sở hữu.
/// </summary>
public sealed class MembershipUpsertReqDTO
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; init; }

    public Guid GroupId { get; init; }

    public Guid PrimaryDepartmentId { get; init; }

    /// <summary>
    /// Row version Base64 do server trả về. Chỉ bỏ trống khi tài khoản chưa có
    /// membership đang hoạt động.
    /// </summary>
    [StringLength(200)]
    public string? ExpectedRowVersion { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }
}
