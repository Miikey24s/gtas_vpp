namespace gtas_vpp_be.Service.Services;

public interface ILibraryQueryService
{
    Task<LibraryQueryResult> QueryAsync(
        string tableCode,
        LibraryQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<LibraryQueryResult> GetByIdAsync(
        string tableCode,
        Guid id,
        bool showDeleted,
        CancellationToken cancellationToken = default);
}

public sealed record LibraryQueryRequest(
    Guid? Id,
    string? SearchText,
    Guid? LookupCategoryId,
    string? Filter,
    int? Skip,
    int? Top,
    string? OrderBy,
    string? Distinct,
    string? DistinctFilter,
    bool ShowDeleted);

public sealed record LibraryQueryResult(
    object? Value,
    int? TotalCount = null,
    string? ErrorMessage = null,
    bool NotFound = false)
{
    public bool IsSuccess => ErrorMessage is null && !NotFound;
}
