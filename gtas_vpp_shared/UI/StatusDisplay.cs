namespace gtas_vpp_shared.UI
{
    /// <summary>
    /// Single source of truth for VPP order status presentation (F-16).
    ///
    /// Lives in the shared DTO assembly so both the backend (controllers,
    /// DTO projections) and the Blazor FE (grids, badges) map a status
    /// code to the *same* label and visual style without duplicating
    /// switch statements in 5+ files.
    ///
    /// No dependency on Radzen or any UI framework — <see cref="GetBadgeStyleName"/>
    /// returns the enum member name so the FE can translate it locally.
    /// </summary>
    public static class StatusDisplay
    {
        public static string GetText(int status) => status switch
        {
            1 => "Submitted",
            4 => "Cancelled",
            6 => "Pending",
            7 => "Approved",
            8 => "Rejected",
            _ => "-"
        };

        /// <summary>
        /// CSS class usable directly on a &lt;span&gt;/&lt;div&gt; badge element.
        /// Paired with the <c>.vpp-badge-*</c> rules in <c>gtas_vpp_fe.css</c>.
        /// </summary>
        public static string GetCssClass(int status) => status switch
        {
            1 => "vpp-badge-submitted",
            4 => "vpp-badge-cancelled",
            6 => "vpp-badge-pending",
            7 => "vpp-badge-approved",
            8 => "vpp-badge-rejected",
            _ => "vpp-badge-default"
        };

        /// <summary>
        /// Radzen <c>BadgeStyle</c> member name (e.g. "Success", "Danger").
        /// The FE wraps this with a small extension that calls <c>Enum.Parse</c>;
        /// kept as a string here so the shared project stays Radzen-free.
        /// </summary>
        public static string GetBadgeStyleName(int status) => status switch
        {
            1 => "Success",
            4 => "Danger",
            6 => "Warning",
            7 => "Success",
            8 => "Danger",
            _ => "Light"
        };
    }
}
