using System;
using System.Globalization;

namespace gtas_vpp_fe.Helpers
{
    /// <summary>
    /// Centralized date/time formatting for the VPP frontend (F-29).
    ///
    /// Replaces ad-hoc <c>.ToString("dd/MM/yyyy")</c> sprinkled across the
    /// 6 order tabs + the Order Create wizard. Every screen MUST go through
    /// these constants so the format is identical across tabs, grids and
    /// detail headers, and so a single locale switch propagates everywhere.
    ///
    /// Culture is pinned to <c>vi-VN</c> because the entire UI text + business
    /// vocabulary is Vietnamese and the DB stores dates already aligned to
    /// the user's local timezone (server runs in Asia/Ho_Chi_Minh).
    /// </summary>
    public static class DateFormatter
    {
        /// <summary>Day/month/year — e.g. <c>05/04/2026</c>.</summary>
        public const string ShortDate = "dd/MM/yyyy";

        /// <summary>Hour:minute + day/month/year — e.g. <c>00:00 05/04/2026</c>.</summary>
        public const string LongDate = "HH:mm dd/MM/yyyy";

        /// <summary>Month/year only — e.g. <c>04/2026</c>. Used for period badges.</summary>
        public const string MonthYear = "MM/yyyy";

        /// <summary>Hours:minutes — e.g. <c>14:30</c>. Used for auto-save toasts.</summary>
        public const string TimeOnly = "HH:mm";

        private static readonly CultureInfo ViVn = CultureInfo.GetCultureInfo("vi-VN");

        /// <summary>
        /// Formats a nullable <see cref="DateTime"/> using <paramref name="fmt"/>
        /// under the vi-VN culture. Returns "-" when <paramref name="d"/> is null
        /// so empty cells render consistently in grids and cards.
        /// </summary>
        public static string Format(DateTime? d, string fmt)
            => d?.ToString(fmt, ViVn) ?? "-";

        /// <summary>
        /// Formats a non-nullable <see cref="DateTime"/>. Convenience overload
        /// for computed display properties where the source is guaranteed
        /// non-null (e.g. derived period dates).
        /// </summary>
        public static string Format(DateTime d, string fmt)
            => d.ToString(fmt, ViVn);
    }
}
