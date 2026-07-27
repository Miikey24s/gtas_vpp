using Radzen;

namespace gtas_vpp_fe.Helpers
{
    /// <summary>
    /// Bridge phía FE từ <see cref="StatusDisplay"/> sang enum
    /// <see cref="BadgeStyle"/> của Radzen.
    /// </summary>
    public static class StatusDisplayRadzen
    {
        public static BadgeStyle BadgeStyleFor(int status) =>
            Enum.TryParse<BadgeStyle>(StatusDisplay.GetBadgeStyleName(status), ignoreCase: false, out var bs)
                ? bs
                : BadgeStyle.Light;
    }
}
