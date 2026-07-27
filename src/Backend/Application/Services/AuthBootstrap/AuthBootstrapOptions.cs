namespace gtas_vpp_be.Service.Services.AuthBootstrap;

/// <summary>
/// Input tường minh cho lần bootstrap clean-owner đầu tiên. Tính năng bị tắt trừ khi
/// operator chủ động bật cho đúng một lần chạy. Không bao giờ log hoặc serialize
/// instance của type này vì nó chứa mật khẩu ban đầu.
/// </summary>
public sealed class AuthBootstrapOptions
{
    public const string SectionName = "AuthBootstrap";

    public bool Enabled { get; set; }

    public string OperationKey { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string InitialPassword { get; set; } = string.Empty;

    public string PrimaryDepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// Tiện ích chỉ dành cho local với database TEST/DEMO hoàn toàn mới. Bootstrap
    /// production phải trỏ đến phòng ban tổ chức đã tồn tại.
    /// </summary>
    public bool CreatePrimaryDepartmentIfMissing { get; set; }

    public string PrimaryDepartmentName { get; set; } = string.Empty;

    public override string ToString() => nameof(AuthBootstrapOptions);
}
