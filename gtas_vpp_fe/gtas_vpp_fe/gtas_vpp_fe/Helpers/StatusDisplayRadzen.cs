using Radzen;

namespace gtas_vpp_fe.Helpers
{
    /// <summary>
    /// FE-side bridge from <see cref="StatusDisplay"/> to Radzen's
    /// <see cref="BadgeStyle"/> enum.
    /// </summary>
    public static class StatusDisplayRadzen
    {
        public static BadgeStyle BadgeStyleFor(int status) =>
            Enum.TryParse<BadgeStyle>(StatusDisplay.GetBadgeStyleName(status), ignoreCase: false, out var bs)
                ? bs
                : BadgeStyle.Light;
    }
}
