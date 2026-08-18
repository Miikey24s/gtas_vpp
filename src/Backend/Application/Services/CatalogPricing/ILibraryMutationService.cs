using System.Text.Json;

namespace gtas_vpp_be.Service.Services;

public interface ILibraryMutationService
{
    Task<LibraryMutationResult> CreateAsync(
        string tableCode,
        JsonElement payload,
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<LibraryMutationResult> UpdateAsync(
        string tableCode,
        JsonElement payload,
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<LibraryMutationResult> PatchAsync(
        string tableCode,
        Guid id,
        JsonElement payload,
        int actorUserId,
        CancellationToken cancellationToken = default);
}

public enum LibraryMutationStatus
{
    Success,
    BadRequest,
    NotFound
}

public sealed record LibraryMutationResult(
    LibraryMutationStatus Status,
    object? Value = null,
    string? Message = null);
