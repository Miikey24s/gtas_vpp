namespace gtas_vpp_fe.Features.Settlement.State;

// Tập trung cờ tính năng chốt kỳ để có thể tạm ẩn capability mà không xóa dữ liệu hoặc API nền.
public sealed record SettlementFeatureOptions(bool MultiSupplierSelectionEnabled);
