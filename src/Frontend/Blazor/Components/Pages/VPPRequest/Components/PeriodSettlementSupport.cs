// PAGE LOGIC: VPPRequest/Components/PeriodSettlementSupport.cs
namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components
{
    /// <summary>
    /// Dịch mã điều kiện ngăn chốt của backend (PeriodSettlementService) sang câu
    /// tiếng Việt nêu rõ nguyên nhân. Phủ đủ toàn bộ mã backend phát sinh — mã lạ
    /// hiển thị nguyên văn để không che giấu lỗi mới.
    /// </summary>
    public static class PeriodSettlementSupport
    {
        // MAPPING: Chuyển mã blocker từ backend thành thông báo dễ hiểu cho người dùng.
        public static string DescribeBlocker(string blocker)
        {
            if (string.IsNullOrWhiteSpace(blocker))
            {
                return blocker;
            }

            var parts = blocker.Split(':');
            var code = parts[0];
            var argument = parts.Length > 1 ? parts[1] : null;

            return code switch
            {
                "PENDING_SUPPLEMENTS" => $"Còn {argument} đơn bổ sung chờ duyệt.",
                "NO_SUBMITTED_ITEMS" => "Chưa có mặt hàng hợp lệ để chốt kỳ.",
                "INVALID_SUPPLIER_EXCEPTION" => "Có lựa chọn nhà cung cấp ngoại lệ chưa hợp lệ hoặc thiếu lý do.",
                "PRIMARY_SUPPLIER_NOT_COVERED" => "Nhà cung cấp chính đã chọn không có bảng giá phủ nhu cầu kỳ này.",
                "PRICE_BOOK_NOT_COVERED" => "Bảng giá đã chọn không phủ đủ nhu cầu kỳ này.",
                "NO_COMPLETE_PRICE_COVERAGE" => "Không có bảng giá nào phủ đủ toàn bộ mặt hàng; hãy chọn nhà cung cấp chính và khai báo ngoại lệ cho phần thiếu.",
                "PRIMARY_QUOTE_HAS_UNRESOLVED_ITEMS" => "Nguồn cung chính còn mặt hàng thiếu giá chưa được khai báo ngoại lệ.",
                "EXCEPTION_MUST_USE_ANOTHER_SUPPLIER" => "Ngoại lệ phải chọn nhà cung cấp khác nhà cung cấp chính.",
                "EXCEPTION_NOT_REQUIRED" => "Có ngoại lệ khai cho mặt hàng mà nguồn cung chính đã có giá — hãy gỡ ngoại lệ đó.",
                "EXCEPTION_RESOLVER_UNAVAILABLE" => "Không xác định được bảng giá còn hiệu lực cho một ngoại lệ.",
                "SUPPLIER_EXCEPTION_UNRESOLVED" => "Một ngoại lệ chưa tìm được giá hợp lệ từ nhà cung cấp thay thế.",
                "MISSING_ITEMS" => $"Thiếu giá cho {argument} mặt hàng.",
                _ => blocker
            };
        }
    }
}
