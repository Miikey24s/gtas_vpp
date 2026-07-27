using System;
using System.Globalization;

namespace gtas_vpp_fe.Helpers
{
    /// <summary>
    /// Tập trung logic format ngày/giờ cho frontend VPP (F-29).
    ///
    /// Thay thế cách gọi rời rạc <c>.ToString("dd/MM/yyyy")</c> trong 6 tab đơn hàng
    /// và wizard tạo đơn. Mọi màn hình PHẢI dùng các hằng này để format giống nhau
    /// giữa tab, grid, header chi tiết và một lần đổi locale áp dụng được toàn hệ thống.
    ///
    /// Culture được cố định <c>vi-VN</c> vì toàn bộ text UI và từ vựng nghiệp vụ là
    /// tiếng Việt, còn DB lưu ngày đã căn theo múi giờ local của user
    /// (server chạy Asia/Ho_Chi_Minh).
    /// </summary>
    public static class DateFormatter
    {
        /// <summary>Ngày/tháng/năm, ví dụ <c>05/04/2026</c>.</summary>
        public const string ShortDate = "dd/MM/yyyy";

        /// <summary>Giờ:phút + ngày/tháng/năm, ví dụ <c>00:00 05/04/2026</c>.</summary>
        public const string LongDate = "HH:mm dd/MM/yyyy";

        /// <summary>Chỉ tháng/năm, ví dụ <c>04/2026</c>; dùng cho badge kỳ.</summary>
        public const string MonthYear = "MM/yyyy";

        /// <summary>Giờ:phút, ví dụ <c>14:30</c>; dùng cho toast tự động lưu.</summary>
        public const string TimeOnly = "HH:mm";

        private static readonly CultureInfo ViVn = CultureInfo.GetCultureInfo("vi-VN");

        /// <summary>
        /// Format <see cref="DateTime"/> nullable bằng <paramref name="fmt"/> theo culture vi-VN.
        /// Trả về "-" khi <paramref name="d"/> null để cell rỗng render nhất quán
        /// trong grid và card.
        /// </summary>
        public static string Format(DateTime? d, string fmt)
            => d?.ToString(fmt, ViVn) ?? "-";

        /// <summary>
        /// Format <see cref="DateTime"/> không nullable. Overload tiện ích cho property
        /// hiển thị đã tính toán với nguồn được bảo đảm không null, ví dụ ngày suy ra từ kỳ.
        /// </summary>
        public static string Format(DateTime d, string fmt)
            => d.ToString(fmt, ViVn);
    }
}
