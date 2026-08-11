using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_be.Service.Services;

public sealed record PriceListColumnMappingSuggestion(int ColumnIndex, string TargetField);

public interface IPriceListColumnMappingSuggester
{
    bool IsAvailable { get; }

    Task<IReadOnlyList<PriceListColumnMappingSuggestion>> SuggestAsync(
        IReadOnlyList<PriceListImportSourceColumnResDTO> columns,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Mặc định không gửi tên cột hoặc dữ liệu mẫu ra ngoài. Adapter AI chỉ được thay thế
/// khi quản trị đã cấu hình provider và chấp thuận chính sách dữ liệu.
/// </summary>
public sealed class DisabledPriceListColumnMappingSuggester : IPriceListColumnMappingSuggester
{
    public bool IsAvailable => false;

    public Task<IReadOnlyList<PriceListColumnMappingSuggestion>> SuggestAsync(
        IReadOnlyList<PriceListImportSourceColumnResDTO> columns,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PriceListColumnMappingSuggestion>>([]);
}
