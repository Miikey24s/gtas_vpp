using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Permission;

/// <summary>
/// Command quản trị để vô hiệu hóa mềm membership đang hoạt động. Command này
/// không xóa tài khoản hoặc bất kỳ dòng membership lịch sử nào.
/// </summary>
public sealed class MembershipDeactivateReqDTO
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; init; }

    [Required, StringLength(200)]
    public string ExpectedRowVersion { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Reason { get; init; }
}
