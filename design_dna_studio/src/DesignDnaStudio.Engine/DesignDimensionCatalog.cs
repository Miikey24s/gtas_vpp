namespace DesignDnaStudio.Engine;

public sealed record DesignDimensionDefinition(
    DesignDimension Dimension,
    string Group,
    string NegativePole,
    string PositivePole,
    string Description);

public static class DesignDimensionCatalog
{
    public static IReadOnlyList<DesignDimensionDefinition> All { get; } =
    [
        new(DesignDimension.Darkness, "Màu sắc", "Sáng", "Tối", "Mức sáng tổng thể và sở thích bề mặt sáng hoặc tối."),
        new(DesignDimension.Vividness, "Màu sắc", "Trầm", "Rực rỡ", "Độ bão hòa màu và năng lượng thị giác."),
        new(DesignDimension.Warmth, "Màu sắc", "Lạnh", "Ấm", "Nhiệt độ của bảng màu chủ đạo."),
        new(DesignDimension.Contrast, "Màu sắc", "Tương phản thấp", "Tương phản cao", "Mức tách biệt giữa bề mặt, chữ và màu nhấn."),
        new(DesignDimension.Multicolor, "Màu sắc", "Đơn sắc", "Đa sắc", "Độ rộng bảng màu trong một bố cục."),
        new(DesignDimension.GradientGlow, "Màu sắc", "Màu phẳng", "Gradient / phát sáng", "Mức sử dụng gradient, quầng sáng và điểm nhấn phát quang."),
        new(DesignDimension.Roundedness, "Hình khối", "Góc cạnh", "Bo tròn", "Hình học góc và độ mềm của container."),
        new(DesignDimension.Depth, "Hình khối", "Phẳng", "Phân lớp", "Độ sâu thị giác và mức chồng lớp bề mặt."),
        new(DesignDimension.ShadowDominance, "Hình khối", "Ưu tiên đường viền", "Ưu tiên đổ bóng", "Cấu trúc được thể hiện bằng đường viền hay độ nổi."),
        new(DesignDimension.MaterialTexture, "Hình khối", "Kỹ thuật số sạch", "Chất liệu hữu hình", "Mức sử dụng hạt, giấy, vải hoặc hiệu ứng xúc giác."),
        new(DesignDimension.OrganicForm, "Hình khối", "Hình học", "Hữu cơ", "Hình học đều đặn so với đường nét tự nhiên, mềm mại."),
        new(DesignDimension.Asymmetry, "Hình khối", "Đối xứng", "Bất đối xứng", "Cân bằng và sự lệch có chủ đích trong bố cục."),
        new(DesignDimension.EditorialTypography, "Chữ", "Trung tính", "Biên tập cá tính", "Mức cá tính và khác biệt của typography."),
        new(DesignDimension.TypeWeight, "Chữ", "Mảnh", "Đậm", "Trọng lượng thị giác của tiêu đề và nhãn quan trọng."),
        new(DesignDimension.TypeSpacing, "Chữ", "Gọn", "Thoáng", "Khoảng chữ, chiều cao dòng và nhịp điệu quanh nội dung."),
        new(DesignDimension.OversizedDisplay, "Chữ", "Tỷ lệ quen thuộc", "Tiêu đề ngoại cỡ", "Mức sử dụng tiêu đề và con số lớn giàu biểu cảm."),
        new(DesignDimension.Density, "Bố cục", "Thưa", "Dày", "Lượng thông tin hiển thị trong một vùng giao diện."),
        new(DesignDimension.ExperimentalLayout, "Bố cục", "Lưới truyền thống", "Thử nghiệm / bento", "Lưới dễ đoán so với bố cục giàu biểu cảm."),
        new(DesignDimension.DynamicLayering, "Bố cục", "Tĩnh", "Phân lớp động", "Mức chồng, lệch và chuyển động không gian trong bố cục."),
        new(DesignDimension.ExplicitLabels, "Bố cục", "Ưu tiên biểu tượng", "Nhãn chữ rõ ràng", "Mức phụ thuộc vào icon so với nhãn chữ hiển thị trực tiếp."),
        new(DesignDimension.ProgressiveDisclosure, "Bố cục", "Hiện toàn bộ", "Mở dần theo nhu cầu", "Nội dung nâng cao được hiện sẵn hay chỉ mở khi cần."),
        new(DesignDimension.LiteralImagery, "Hình ảnh", "Trừu tượng", "Cụ thể", "Họa tiết trừu tượng so với chủ thể có thật."),
        new(DesignDimension.Photography, "Hình ảnh", "Minh họa", "Nhiếp ảnh", "Sở thích giữa hình minh họa và ảnh chụp."),
        new(DesignDimension.Playfulness, "Cá tính", "Doanh nghiệp", "Tinh nghịch / Gen Z", "Mức dí dỏm, bất ngờ và trẻ trung."),
        new(DesignDimension.ExpressiveMotion, "Cá tính", "Chuyển động tối giản", "Chuyển động biểu cảm", "Cường độ và cá tính của chuyển động giao diện."),
        new(DesignDimension.TextileMateriality, "PPJ / GTAS", "Kỹ thuật số chung", "Dệt may / denim", "Dấu ấn sợi, đường may, denim và quy trình may mặc."),
        new(DesignDimension.IndustrialPrecision, "PPJ / GTAS", "Thủ công tự do", "Chính xác công nghiệp", "Cảm giác chính xác kỹ thuật, kiểm soát quy trình và làm đúng từ đầu."),
        new(DesignDimension.LocalVietnameseExpression, "PPJ / GTAS", "Trung tính toàn cầu", "Bản sắc Việt Nam", "Liên hệ rõ với địa điểm, con người và văn hóa Việt Nam.")
    ];

    public static DesignDimensionDefinition Get(DesignDimension dimension) => All[(int)dimension];
}
