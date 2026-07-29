using Radzen;

namespace gtas_vpp_fe.Components.DesignSystem.Composites;

/// <summary>
/// Mức độ phức tạp của editor quản trị; kích thước form không suy ra từ số cột của bảng.
/// </summary>
public enum VppAdminDialogSize
{
    Compact,
    Standard,
    Workspace
}

/// <summary>
/// Tạo options Radzen thống nhất cho editor quản trị.
/// </summary>
public static class VppAdminDialogProfiles
{
    public static DialogOptions Create(
        VppAdminDialogSize size,
        string? ariaLabel = null,
        bool closeOnOverlayClick = false,
        string? closeAriaLabel = null)
    {
        var suffix = size.ToString().ToLowerInvariant();

        return new DialogOptions
        {
            Width = size switch
            {
                VppAdminDialogSize.Compact => "min(640px, 96vw)",
                VppAdminDialogSize.Standard => "min(880px, 96vw)",
                VppAdminDialogSize.Workspace => "min(1360px, 90vw)",
                _ => "min(640px, 96vw)"
            },
            // Compact/standard tự co theo nội dung; workspace mới cần chiều cao cố định cho matrix/bảng con.
            Height = size == VppAdminDialogSize.Workspace
                ? "min(860px, 88vh)"
                : null,
            CssClass = $"vpp-admin-dialog vpp-admin-dialog--{suffix}",
            WrapperCssClass = "vpp-admin-dialog-wrapper",
            ContentCssClass = "vpp-admin-dialog-content",
            ShowTitle = true,
            ShowClose = true,
            CloseDialogOnEsc = true,
            CloseDialogOnOverlayClick = closeOnOverlayClick,
            AutoFocusFirstElement = true,
            Draggable = size != VppAdminDialogSize.Workspace,
            Resizable = size == VppAdminDialogSize.Workspace,
            AriaLabel = ariaLabel,
            CloseAriaLabel = closeAriaLabel
        };
    }
}
