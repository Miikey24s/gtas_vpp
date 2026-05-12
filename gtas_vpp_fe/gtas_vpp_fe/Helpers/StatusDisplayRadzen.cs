using gtas_vpp_shared.UI;
using Radzen;

namespace gtas_vpp_fe.Helpers
{
    /// <summary>
    /// FE-side bridge from <see cref="StatusDisplay"/> (shared, UI-agnostic)
    /// to Radzen's <see cref="BadgeStyle"/> enum.
    ///
    /// Keeps the shared project free of any UI framework dependency while
    /// still letting Razor markup write <c>BadgeStyle="@StatusDisplayRadzen.BadgeStyleFor(status)"</c>.
    /// </summary>
    public static class StatusDisplayRadzen
    {
        public static BadgeStyle BadgeStyleFor(int status) =>
            Enum.TryParse<BadgeStyle>(StatusDisplay.GetBadgeStyleName(status), ignoreCase: false, out var bs)
                ? bs
                : BadgeStyle.Light;
    }
}
